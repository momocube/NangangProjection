using System.Diagnostics;
using System.Text.Json;
using WallProjector.Config;
using WallProjector.Core;
using WallProjector.Web;

namespace WallProjector.Engine;

/// <summary>槽的執行狀態。</summary>
public enum SlotStatus
{
    /// <summary>未啟動（含被手動停止）</summary>
    Stopped,
    /// <summary>瀏覽器 process 存活中</summary>
    Running,
    /// <summary>瀏覽器死亡，退避等待重啟中（watchdog）</summary>
    Restarting,
    /// <summary>重啟 5 次仍失敗 / 啟動失敗，放棄（手動 start 重置）</summary>
    Failed,
}

/// <summary>
/// 投影引擎：每面牆一個投影槽——rect 計算、瀏覽器殼啟動、視窗對位、watchdog 重啟。
/// 所有槽狀態以單一 _gate lock 序列化（管理 API thread、stdin thread、watchdog timer 共用）。
/// </summary>
public sealed class ProjectionEngine : IDisposable
{
    sealed class SlotRuntime
    {
        public Process? Process;
        public SlotStatus Status = SlotStatus.Stopped;
        public string? LastError;
        public int RestartAttempts;
        public long LastStartTick;          // Environment.TickCount64
        public long NextRestartTick;
        public WindowRect? Rect;
        public bool Positioned;
    }

    readonly object _gate = new();
    readonly ProjectorConfig _config;
    readonly string _configPath;
    readonly Logger _log;
    readonly Dictionary<int, SlotRuntime> _runtimes = new();
    readonly Timer _watchdog;
    readonly Func<string, string?> _frameChecker;
    TouchServiceState _ts;
    Func<TouchServiceState>? _reconnect;
    string? _browserExe;
    bool _disposed;

    /// <summary>本工具 HTTP port（wrap URL 組成用；HttpServer 啟動後回填實際 port）。</summary>
    public int HttpPort { get; set; }

    const int MaxRestartAttempts = 5;
    const long AttemptResetUptimeMs = 60_000;   // 存活超過 1 分鐘視為穩定，退避歸零

    public ProjectionEngine(ProjectorConfig config, string configPath, TouchServiceState ts, Logger log,
                            Func<string, string?>? frameChecker = null)
    {
        _config = config;
        _configPath = configPath;
        _ts = ts;
        _log = log;
        HttpPort = config.HttpPort;
        _frameChecker = frameChecker ??
            (url => FrameBlockChecker.Check(url, $"http://127.0.0.1:{HttpPort}",
                                            TimeSpan.FromSeconds(4), _log));
        _watchdog = new Timer(_ => WatchdogTick(), null, 2000, 2000);
    }

    /// <summary>註冊 TouchService 重連函式（管理頁「重新連線」按鈕 / stdin restore 用）。</summary>
    public void SetReconnect(Func<TouchServiceState> reconnect) => _reconnect = reconnect;

    public TouchServiceState TouchState { get { lock (_gate) return _ts; } }

    // ── 操作 ────────────────────────────────────────────────────────────────

    /// <summary>啟動所有 enabled 槽（autoRestore / stdin restore / 管理頁全部還原）。</summary>
    public string RestoreAll()
    {
        var results = new List<string>();
        lock (_gate)
        {
            foreach (var slot in _config.Projections.Where(p => p.Enabled).OrderBy(p => p.Face))
            {
                var rt = Runtime(slot.Face);
                rt.RestartAttempts = 0;
                var (_, msg) = StartLocked(slot.Face, slot);
                results.Add(msg);
            }
        }
        if (results.Count == 0) return "沒有 enabled 的投影槽";
        return string.Join("\n", results);
    }

    /// <summary>啟動一個槽（設 enabled=true 並持久化——還原語意以 enabled 為準）。</summary>
    public (bool Ok, string Message) Start(int face)
    {
        lock (_gate)
        {
            var slot = _config.GetSlot(face);
            if (slot == null || string.IsNullOrWhiteSpace(slot.Url))
                return (false, $"face {face} 尚未設定 URL");

            if (!slot.Enabled)
            {
                slot.Enabled = true;
                SaveConfigLocked();
            }
            Runtime(face).RestartAttempts = 0;   // 手動啟動重置退避
            return StartLocked(face, slot);
        }
    }

    /// <summary>停止一個槽（設 enabled=false 並持久化）。</summary>
    public (bool Ok, string Message) Stop(int face)
    {
        lock (_gate)
        {
            var slot = _config.GetSlot(face);
            if (slot != null && slot.Enabled)
            {
                slot.Enabled = false;
                SaveConfigLocked();
            }

            var rt = Runtime(face);
            KillProcessLocked(rt);
            bool wasActive = rt.Status is SlotStatus.Running or SlotStatus.Restarting;
            rt.Status = SlotStatus.Stopped;
            rt.LastError = null;
            return (true, wasActive ? $"face {face} 已停止" : $"face {face} 本來就沒在跑（enabled 已關）");
        }
    }

    /// <summary>
    /// 管理頁寫入：更新槽設定（url / rotation / enabled），即時生效並寫回 projections.json。
    /// 槽正在跑時：enabled=true → 重啟套用新設定；enabled=false → 停止。
    /// </summary>
    public (bool Ok, string Message) UpdateSlot(int face, string? url, int? rotation, bool? enabled)
    {
        lock (_gate)
        {
            if (face < 0) return (false, "face 不可為負");
            if (_ts.Venue != null && face >= _ts.Venue.Faces.Count)
                return (false, $"face {face} 超出 venue faces 範圍（0~{_ts.Venue.Faces.Count - 1}）");

            var slot = _config.UpsertSlot(face);
            if (url != null) slot.Url = url.Trim();
            if (rotation.HasValue)
            {
                int norm = SlotConfig.NormalizeRotation(rotation.Value);
                if (norm != rotation.Value)
                    return (false, $"rotation 必須是 0/90/180/270（收到 {rotation.Value}）");
                slot.Rotation = norm;
            }
            if (enabled.HasValue) slot.Enabled = enabled.Value;
            SaveConfigLocked();

            var rt = Runtime(face);
            bool active = rt.Status is SlotStatus.Running or SlotStatus.Restarting;

            if (slot.Enabled && !string.IsNullOrWhiteSpace(slot.Url))
            {
                rt.RestartAttempts = 0;
                var (ok, msg) = StartLocked(face, slot);   // 跑著就重啟套用、沒跑就啟動
                return (ok, $"face {face} 設定已儲存；{msg}");
            }
            if (active)
            {
                KillProcessLocked(rt);
                rt.Status = SlotStatus.Stopped;
                return (true, $"face {face} 設定已儲存（enabled=false，已停止）");
            }
            return (true, $"face {face} 設定已儲存");
        }
    }

    /// <summary>重新探測 TouchService（佈局/校正變動後按）。不自動重啟跑著的槽——rect 變了要重啟才套用。</summary>
    public string Reconnect()
    {
        if (_reconnect == null) return "重連函式未註冊";
        var ts = _reconnect();
        lock (_gate) _ts = ts;
        return ts.Reachable
            ? $"已連上 TouchService（venue={ts.Venue?.Venue}）。跑著的槽 rect 不會自動變——需要重啟槽套用。"
            : $"仍連不上 TouchService（{ts.Source}）：{ts.Warning}";
    }

    // ── wrap 頁 ─────────────────────────────────────────────────────────────

    public HttpServer.Response WrapPage(int face)
    {
        SlotConfig? slot;
        string faceName;
        lock (_gate)
        {
            slot = _config.GetSlot(face);
            faceName = FaceNameLocked(face);
        }
        if (slot == null || string.IsNullOrWhiteSpace(slot.Url))
            return HttpServer.Response.Html("404 Not Found", WrapPageBuilder.BuildNotConfigured(face));

        string? blockReason = _frameChecker(slot.Url);
        if (blockReason != null)
            _log.Warn($"[Wrap] face {face} 目標站拒絕嵌入：{blockReason}");
        return HttpServer.Response.Html("200 OK",
            WrapPageBuilder.Build(face, faceName, slot.Url, slot.Rotation, blockReason));
    }

    // ── 狀態快照 ────────────────────────────────────────────────────────────

    public string StateJson()
    {
        lock (_gate)
        {
            var faces = new List<object>();
            if (_ts.Venue != null)
            {
                for (int i = 0; i < _ts.Venue.Faces.Count; i++)
                {
                    var f = _ts.Venue.Faces[i];
                    var rect = ScreenMapper.FaceScreenRect(f, _ts.Venue, _ts.Screen, _ts.Content);
                    faces.Add(FaceEntryLocked(i, f.Name, f.IsFloor,
                        new { x = f.OriginX, y = f.OriginY, w = f.PixelWidth, h = f.PixelHeight },
                        new { x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height },
                        _ts.Venue.GuessRotation(i)));
                }
                // venue 沒有的孤兒槽（佈局變動殘留）也列出來，可清理
                foreach (var slot in _config.Projections.Where(p => p.Face >= _ts.Venue.Faces.Count || p.Face < 0))
                    faces.Add(FaceEntryLocked(slot.Face, $"face {slot.Face}（不在 venue 內）", false, null, null, 0));
            }
            else
            {
                foreach (var slot in _config.Projections.OrderBy(p => p.Face))
                    faces.Add(FaceEntryLocked(slot.Face, $"face {slot.Face}（無 venue 資料）", false, null, null, 0));
            }

            var state = new
            {
                httpPort = HttpPort,
                topmost = _config.Topmost,
                restartOnCrash = _config.RestartOnCrash,
                autoRestore = _config.AutoRestore,
                touchService = new
                {
                    baseUrl = _config.TouchServiceBaseUrl,
                    reachable = _ts.Reachable,
                    source = _ts.Source,
                    warning = _ts.Warning,
                    venue = _ts.Venue?.Venue,
                    canvas = _ts.Venue == null ? null
                        : new { width = _ts.Venue.Canvas.Width, height = _ts.Venue.Canvas.Height },
                    screen = new { x = _ts.Screen.X, y = _ts.Screen.Y, width = _ts.Screen.Width, height = _ts.Screen.Height },
                    content = new { offsetX = _ts.Content.OffsetX, offsetY = _ts.Content.OffsetY, scaleX = _ts.Content.ScaleX, scaleY = _ts.Content.ScaleY },
                },
                faces,
            };
            return JsonSerializer.Serialize(state);
        }
    }

    object FaceEntryLocked(int index, string name, bool floor, object? canvasRect, object? screenRect, int guessRotation)
    {
        var slot = _config.GetSlot(index);
        _runtimes.TryGetValue(index, out var rt);
        return new
        {
            index, name, floor,
            canvas = canvasRect,
            screenRect,
            guessRotation,
            slot = slot == null ? null : new { url = slot.Url, rotation = slot.Rotation, enabled = slot.Enabled },
            status = (rt?.Status ?? SlotStatus.Stopped).ToString().ToLowerInvariant(),
            restartAttempts = rt?.RestartAttempts ?? 0,
            lastError = rt?.LastError,
        };
    }

    /// <summary>stdin `status` 的人話輸出。</summary>
    public string StatusText()
    {
        lock (_gate)
        {
            var lines = new List<string>
            {
                $"TouchService：{(_ts.Reachable ? "已連線" : "不可達")}（{_ts.Source}）" +
                    (_ts.Venue == null ? "、無 venue 資料" : $"、venue={_ts.Venue.Venue}"),
                $"screen=({_ts.Screen.X},{_ts.Screen.Y}) {_ts.Screen.Width}x{_ts.Screen.Height}  " +
                    $"content=offset({_ts.Content.OffsetX},{_ts.Content.OffsetY}) scale({_ts.Content.ScaleX},{_ts.Content.ScaleY})",
                $"管理頁 http://127.0.0.1:{HttpPort}/",
            };
            if (_config.Projections.Count == 0)
            {
                lines.Add("（無投影槽設定）");
            }
            else foreach (var slot in _config.Projections.OrderBy(p => p.Face))
            {
                _runtimes.TryGetValue(slot.Face, out var rt);
                string name = FaceNameLocked(slot.Face);
                string status = (rt?.Status ?? SlotStatus.Stopped).ToString();
                string extra = rt?.LastError != null ? $"  !{rt.LastError}" : "";
                lines.Add($"face {slot.Face} [{name}] {(slot.Enabled ? "on " : "off")} rot={slot.Rotation,-3} " +
                          $"{status,-10} {slot.Url}{extra}");
            }
            return string.Join("\n", lines);
        }
    }

    // ── 內部 ────────────────────────────────────────────────────────────────

    void SaveConfigLocked()
    {
        try { _config.Save(_configPath); }
        catch (Exception ex) { _log.Error($"[Config] 寫回 {_configPath} 失敗：{ex.Message}"); }
    }

    SlotRuntime Runtime(int face)
    {
        if (!_runtimes.TryGetValue(face, out var rt))
            _runtimes[face] = rt = new SlotRuntime();
        return rt;
    }

    string FaceNameLocked(int face) =>
        _ts.Venue != null && face >= 0 && face < _ts.Venue.Faces.Count
            ? _ts.Venue.Faces[face].Name
            : $"face {face}";

    /// <summary>啟動槽（持 _gate 呼叫）。已在跑會先 kill 再重拉（重啟語意）。</summary>
    (bool Ok, string Message) StartLocked(int face, SlotConfig slot)
    {
        var rt = Runtime(face);
        KillProcessLocked(rt);

        if (_ts.Venue == null)
        {
            rt.Status = SlotStatus.Failed;
            rt.LastError = "無 venue 資料（TouchService 不可達且無 cache/fallback），無法計算牆面 rect";
            return (false, $"face {face} 啟動失敗：{rt.LastError}");
        }
        if (face < 0 || face >= _ts.Venue.Faces.Count)
        {
            rt.Status = SlotStatus.Failed;
            rt.LastError = $"face {face} 不在 venue faces 範圍（0~{_ts.Venue.Faces.Count - 1}）";
            return (false, rt.LastError);
        }

        var faceInfo = _ts.Venue.Faces[face];
        var rect = ScreenMapper.FaceScreenRect(faceInfo, _ts.Venue, _ts.Screen, _ts.Content);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            rt.Status = SlotStatus.Failed;
            rt.LastError = $"rect 計算結果為空（{rect}）——檢查 screen/content 校正";
            return (false, $"face {face} 啟動失敗：{rt.LastError}");
        }

        _browserExe ??= BrowserLauncher.FindBrowser(_log);
        if (_browserExe == null)
        {
            rt.Status = SlotStatus.Failed;
            rt.LastError = "找不到 Edge / Chrome";
            return (false, $"face {face} 啟動失敗：{rt.LastError}");
        }

        string wrapUrl = $"http://127.0.0.1:{HttpPort}/wrap/{face}";
        string profileDir = Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(_configPath)) ?? ".",
            _config.BrowserProfileDir, $"slot-{face}");

        var proc = BrowserLauncher.Launch(_browserExe, wrapUrl, profileDir, rect, _log);
        if (proc == null)
        {
            rt.Status = SlotStatus.Failed;
            rt.LastError = "瀏覽器啟動失敗（見 log）";
            return (false, $"face {face} 啟動失敗：{rt.LastError}");
        }

        rt.Process = proc;
        rt.Status = SlotStatus.Running;
        rt.LastError = null;
        rt.LastStartTick = Environment.TickCount64;
        rt.Rect = rect;
        rt.Positioned = false;

        // 視窗對位走背景（EnumWindows 輪詢最多 15s，不能佔住 _gate）
        bool topmost = _config.Topmost;
        Task.Run(() =>
        {
            bool ok = WindowPositioner.TryPosition(WrapPageBuilder.TitleMarker(face), rect, topmost, _log);
            lock (_gate) { if (Runtime(face).Process == proc) Runtime(face).Positioned = ok; }
        });

        return (true, $"face {face} [{faceInfo.Name}] 已啟動 @ ({rect.X},{rect.Y}) {rect.Width}x{rect.Height} rot={slot.Rotation}");
    }

    void KillProcessLocked(SlotRuntime rt)
    {
        if (rt.Process == null) return;
        try
        {
            if (!rt.Process.HasExited)
                rt.Process.Kill(entireProcessTree: true);   // 瀏覽器是 process tree，殺 root 不夠
        }
        catch (Exception ex) { _log.Debug($"[Engine] kill process 失敗（多半已退出）：{ex.Message}"); }
        rt.Process.Dispose();
        rt.Process = null;
    }

    /// <summary>watchdog（2s tick）：偵測瀏覽器死亡 → 指數退避重啟（1/2/4/8/16s，最多 5 次）。</summary>
    void WatchdogTick()
    {
        if (_disposed) return;
        lock (_gate)
        {
            long now = Environment.TickCount64;
            foreach (var (face, rt) in _runtimes)
            {
                if (rt.Status == SlotStatus.Running && rt.Process is { HasExited: true })
                {
                    if (now - rt.LastStartTick > AttemptResetUptimeMs)
                        rt.RestartAttempts = 0;   // 穩定跑過一陣子才死：退避歸零

                    if (!_config.RestartOnCrash)
                    {
                        rt.Status = SlotStatus.Stopped;
                        rt.LastError = "瀏覽器已結束（restartOnCrash=false，不重啟）";
                        _log.Warn($"[Watchdog] face {face} {rt.LastError}");
                        continue;
                    }
                    if (rt.RestartAttempts >= MaxRestartAttempts)
                    {
                        rt.Status = SlotStatus.Failed;
                        rt.LastError = $"瀏覽器連續死亡 {MaxRestartAttempts} 次，放棄重啟（手動 start 重試）";
                        _log.Error($"[Watchdog] face {face} {rt.LastError}");
                        continue;
                    }

                    int delaySec = 1 << rt.RestartAttempts;   // 1,2,4,8,16
                    rt.RestartAttempts++;
                    rt.Status = SlotStatus.Restarting;
                    rt.NextRestartTick = now + delaySec * 1000L;
                    rt.LastError = $"瀏覽器死亡，{delaySec}s 後重啟（第 {rt.RestartAttempts}/{MaxRestartAttempts} 次）";
                    _log.Warn($"[Watchdog] face {face} {rt.LastError}");
                }
                else if (rt.Status == SlotStatus.Restarting && now >= rt.NextRestartTick)
                {
                    var slot = _config.GetSlot(face);
                    if (slot == null || !slot.Enabled)
                    {
                        rt.Status = SlotStatus.Stopped;
                        continue;
                    }
                    _log.Info($"[Watchdog] face {face} 重啟中…");
                    StartLocked(face, slot);
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _watchdog.Dispose();
        lock (_gate)
        {
            foreach (var rt in _runtimes.Values)
            {
                KillProcessLocked(rt);
                rt.Status = SlotStatus.Stopped;
            }
        }
    }
}
