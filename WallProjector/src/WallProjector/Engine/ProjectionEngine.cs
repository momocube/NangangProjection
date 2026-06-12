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
/// V2 三模式：wrap（iframe 包覆）/ cdp（直開原站 + DevTools 注入旋轉）/ file（本地
/// 影片/PDF/圖片，經 /media 端點）。
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
        /// <summary>cdp 模式：旋轉已注入成功（rotation=0 啟動即視為完成）。</summary>
        public bool CdpInjected;
        /// <summary>wrap 模式：header 預檢偵測到目標站擋 iframe（管理頁據此提示切 CDP）。</summary>
        public string? EmbedBlockReason;
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
    /// 管理頁寫入：更新槽設定（url / rotation / enabled / mode / 檔案播放選項），
    /// 即時生效並寫回 projections.json。
    /// 槽正在跑時：enabled=true → 重啟套用新設定；enabled=false → 停止。
    /// wrap 模式且 url/mode 有變 → 背景跑 iframe 封鎖預檢（結果進 state，管理頁提示切 CDP）。
    /// </summary>
    public (bool Ok, string Message) UpdateSlot(int face, string? url, int? rotation, bool? enabled,
                                                string? mode = null, bool? loop = null,
                                                bool? muted = null, bool? controls = null)
    {
        string? precheckUrl = null;
        (bool, string) result;
        lock (_gate)
        {
            if (face < 0) return (false, "face 不可為負");
            if (_ts.Venue != null && face >= _ts.Venue.Faces.Count)
                return (false, $"face {face} 超出 venue faces 範圍（0~{_ts.Venue.Faces.Count - 1}）");

            var slot = _config.UpsertSlot(face);
            bool sourceChanged = false;
            if (url != null && slot.Url != url.Trim()) { slot.Url = url.Trim(); sourceChanged = true; }
            if (rotation.HasValue)
            {
                int norm = SlotConfig.NormalizeRotation(rotation.Value);
                if (norm != rotation.Value)
                    return (false, $"rotation 必須是 0/90/180/270（收到 {rotation.Value}）");
                slot.Rotation = norm;
            }
            if (mode != null)
            {
                string normMode = SlotConfig.NormalizeMode(mode);
                if (!normMode.Equals(mode.Trim(), StringComparison.OrdinalIgnoreCase))
                    return (false, $"mode 必須是 wrap/cdp/file（收到 \"{mode}\"）");
                if (slot.Mode != normMode) { slot.Mode = normMode; sourceChanged = true; }
            }
            if (loop.HasValue) slot.Loop = loop.Value;
            if (muted.HasValue) slot.Muted = muted.Value;
            if (controls.HasValue) slot.Controls = controls.Value;
            if (enabled.HasValue) slot.Enabled = enabled.Value;
            SaveConfigLocked();

            var rt = Runtime(face);
            bool active = rt.Status is SlotStatus.Running or SlotStatus.Restarting;

            // wrap 模式：url/mode 變了就重新預檢（背景，不佔 _gate）；非 wrap 清掉提示
            if (slot.Mode == SlotConfig.ModeWrap && !string.IsNullOrWhiteSpace(slot.Url))
            {
                if (sourceChanged) { rt.EmbedBlockReason = null; precheckUrl = slot.Url; }
            }
            else rt.EmbedBlockReason = null;

            if (slot.Enabled && !string.IsNullOrWhiteSpace(slot.Url))
            {
                rt.RestartAttempts = 0;
                var (ok, msg) = StartLocked(face, slot);   // 跑著就重啟套用、沒跑就啟動
                result = (ok, $"face {face} 設定已儲存；{msg}");
            }
            else if (active)
            {
                KillProcessLocked(rt);
                rt.Status = SlotStatus.Stopped;
                result = (true, $"face {face} 設定已儲存（enabled=false，已停止）");
            }
            else result = (true, $"face {face} 設定已儲存");
        }

        if (precheckUrl != null) PrecheckEmbedBlock(face, precheckUrl);
        return result;
    }

    /// <summary>背景跑 wrap 模式的 iframe 封鎖預檢，結果寫回 runtime（url 沒被改走才寫）。</summary>
    void PrecheckEmbedBlock(int face, string url)
    {
        Task.Run(() =>
        {
            string? reason = null;
            try { reason = _frameChecker(url); }
            catch { /* 預檢失敗不擋（同 FrameBlockChecker 哲學） */ }
            lock (_gate)
            {
                var slot = _config.GetSlot(face);
                if (slot != null && slot.Mode == SlotConfig.ModeWrap && slot.Url == url)
                    Runtime(face).EmbedBlockReason = reason;
            }
            if (reason != null)
                _log.Warn($"[FrameCheck] face {face} 目標站擋 iframe：{reason}（建議切 CDP 模式）");
        });
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
        string slotUrl;
        lock (_gate)
        {
            slot = _config.GetSlot(face);
            faceName = FaceNameLocked(face);
            slotUrl = slot?.Url ?? "";
        }
        if (slot == null || string.IsNullOrWhiteSpace(slotUrl))
            return HttpServer.Response.Html("404 Not Found", WrapPageBuilder.BuildNotConfigured(face));

        string? blockReason = _frameChecker(slotUrl);
        lock (_gate)
        {
            // 預檢結果同步進 runtime——管理頁顯示「此站擋嵌入，建議切 CDP」
            var cur = _config.GetSlot(face);
            if (cur != null && cur.Mode == SlotConfig.ModeWrap && cur.Url == slotUrl)
                Runtime(face).EmbedBlockReason = blockReason;
        }
        if (blockReason != null)
            _log.Warn($"[Wrap] face {face} 目標站拒絕嵌入：{blockReason}（建議切 CDP 模式）");
        return HttpServer.Response.Html("200 OK",
            WrapPageBuilder.Build(face, faceName, slotUrl, slot.Rotation, blockReason));
    }

    // ── media 頁（file 模式）────────────────────────────────────────────────

    /// <summary>
    /// file 模式播放/檢視頁：依副檔名分流（影片/圖片/PDF）。
    /// 槽不是 file 模式或沒設定 → 404；檔案不存在/不支援 → 旋轉錯誤頁（現場讀得到）。
    /// </summary>
    public HttpServer.Response MediaPage(int face)
    {
        SlotConfig? slot;
        string faceName, resolved = "";
        lock (_gate)
        {
            slot = _config.GetSlot(face);
            faceName = FaceNameLocked(face);
            if (slot != null) resolved = ResolveMediaPathLocked(slot) ?? "";
        }
        if (slot == null || slot.Mode != SlotConfig.ModeFile || string.IsNullOrWhiteSpace(slot.Url))
            return HttpServer.Response.Html("404 Not Found", WrapPageBuilder.BuildNotConfigured(face));

        if (resolved.Length == 0 || !File.Exists(resolved))
            return HttpServer.Response.Html("200 OK", MediaPageBuilder.BuildErrorPage(
                face, faceName, slot.Rotation, "檔案不存在",
                $"{slot.Url}（解析為 {(resolved.Length == 0 ? "無效路徑" : resolved)}）"));

        return MediaPageBuilder.KindOf(resolved) switch
        {
            MediaPageBuilder.MediaKind.Video => HttpServer.Response.Html("200 OK",
                MediaPageBuilder.BuildVideoPage(face, faceName, slot.Rotation,
                                                slot.Loop, slot.Muted, slot.Controls)),
            MediaPageBuilder.MediaKind.Image => HttpServer.Response.Html("200 OK",
                MediaPageBuilder.BuildImagePage(face, faceName, slot.Rotation)),
            MediaPageBuilder.MediaKind.Pdf => HttpServer.Response.Html("200 OK",
                MediaPageBuilder.BuildPdfPage(face, faceName, slot.Rotation)),
            _ => HttpServer.Response.Html("200 OK", MediaPageBuilder.BuildErrorPage(
                face, faceName, slot.Rotation, "不支援的副檔名",
                $"{Path.GetFileName(resolved)}——只支援 {MediaPageBuilder.SupportedExtensions}")),
        };
    }

    /// <summary>
    /// file 模式檔案 bytes：支援 Range（影片 seek 必需）。
    /// **路徑白名單 = config 裡該槽明示的那一個檔案**——路徑由 config 而非 request 決定，
    /// serve 時 resolve 成絕對路徑再用（request 帶不進任何路徑成分，目錄遍歷結構上不可能）。
    /// </summary>
    public HttpServer.Response MediaRaw(int face, string? rangeHeader)
    {
        string? resolved = null;
        lock (_gate)
        {
            var slot = _config.GetSlot(face);
            if (slot != null && slot.Mode == SlotConfig.ModeFile && !string.IsNullOrWhiteSpace(slot.Url))
                resolved = ResolveMediaPathLocked(slot);
        }
        if (resolved == null || !File.Exists(resolved))
            return HttpServer.Response.NotFound();

        long fileLength = new FileInfo(resolved).Length;
        string contentType = MediaPageBuilder.ContentTypeOf(resolved);
        var range = HttpRange.Parse(rangeHeader, fileLength);

        return range.Kind switch
        {
            HttpRange.Kind.Unsatisfiable => new HttpServer.Response(
                "416 Range Not Satisfiable", "text/plain; charset=utf-8", Array.Empty<byte>())
            { ExtraHeaders = new[] { ("Content-Range", $"bytes */{fileLength}") } },

            HttpRange.Kind.Partial => HttpServer.Response.File(
                "206 Partial Content", contentType, resolved, range.Offset, range.Length,
                ("Accept-Ranges", "bytes"),
                ("Content-Range", $"bytes {range.Offset}-{range.Offset + range.Length - 1}/{fileLength}")),

            _ => HttpServer.Response.File(
                "200 OK", contentType, resolved, 0, fileLength,
                ("Accept-Ranges", "bytes")),
        };
    }

    /// <summary>槽的 file 路徑解析（持 _gate）：file:// URI 轉路徑、相對路徑以 config 所在目錄為基準。</summary>
    string? ResolveMediaPathLocked(SlotConfig slot)
    {
        var raw = slot.Url.Trim().Trim('"');
        if (raw.Length == 0) return null;
        try
        {
            if (raw.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                raw = new Uri(raw).LocalPath;
            string baseDir = Path.GetDirectoryName(Path.GetFullPath(_configPath)) ?? ".";
            return Path.GetFullPath(raw, baseDir);
        }
        catch
        {
            return null;   // 非法路徑字元等
        }
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

        // file 模式：檔案不存在 → 管理頁一眼可辨（不用等啟動失敗）
        string? fileError = null;
        if (slot != null && slot.Mode == SlotConfig.ModeFile && !string.IsNullOrWhiteSpace(slot.Url))
        {
            var p = ResolveMediaPathLocked(slot);
            if (p == null || !File.Exists(p)) fileError = $"檔案不存在：{slot.Url}";
            else if (MediaPageBuilder.KindOf(p) == MediaPageBuilder.MediaKind.Unsupported)
                fileError = $"不支援的副檔名：{Path.GetFileName(p)}（支援 {MediaPageBuilder.SupportedExtensions}）";
        }

        return new
        {
            index, name, floor,
            canvas = canvasRect,
            screenRect,
            guessRotation,
            slot = slot == null ? null : new
            {
                url = slot.Url, rotation = slot.Rotation, enabled = slot.Enabled,
                mode = slot.Mode, loop = slot.Loop, muted = slot.Muted, controls = slot.Controls,
            },
            mode = slot?.Mode ?? SlotConfig.ModeWrap,
            cdpPort = slot != null && slot.Mode == SlotConfig.ModeCdp ? CdpPortFor(index, slot) : (int?)null,
            cdpInjected = rt?.CdpInjected ?? false,
            embedBlocked = slot?.Mode == SlotConfig.ModeWrap ? rt?.EmbedBlockReason : null,
            fileError,
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
                lines.Add($"face {slot.Face} [{name}] {(slot.Enabled ? "on " : "off")} mode={slot.Mode,-4} " +
                          $"rot={slot.Rotation,-3} {status,-10} {slot.Url}{extra}");
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

        // ── 模式分流：瀏覽器要開的 URL + CDP port ──
        string mode = SlotConfig.NormalizeMode(slot.Mode);
        int? cdpPort = null;
        string launchUrl;
        switch (mode)
        {
            case SlotConfig.ModeCdp:
                // 直開目標站（不經 wrap 頁 iframe，X-Frame-Options 擋不到）；旋轉用 CDP 注入
                if (!Uri.TryCreate(slot.Url, UriKind.Absolute, out var cdpUri) ||
                    (cdpUri.Scheme != "http" && cdpUri.Scheme != "https"))
                {
                    rt.Status = SlotStatus.Failed;
                    rt.LastError = $"cdp 模式的 URL 必須是 http(s)（收到 {slot.Url}）";
                    return (false, $"face {face} 啟動失敗：{rt.LastError}");
                }
                launchUrl = slot.Url;
                cdpPort = CdpPortFor(face, slot);
                break;

            case SlotConfig.ModeFile:
                // 本地檔案先驗存在 + 副檔名（啟動前就把錯誤打在管理頁，不開空瀏覽器）
                string? mediaPath = ResolveMediaPathLocked(slot);
                if (mediaPath == null || !File.Exists(mediaPath))
                {
                    rt.Status = SlotStatus.Failed;
                    rt.LastError = $"檔案不存在：{slot.Url}" +
                                   (mediaPath != null ? $"（解析為 {mediaPath}）" : "（路徑無效）");
                    return (false, $"face {face} 啟動失敗：{rt.LastError}");
                }
                if (MediaPageBuilder.KindOf(mediaPath) == MediaPageBuilder.MediaKind.Unsupported)
                {
                    rt.Status = SlotStatus.Failed;
                    rt.LastError = $"不支援的副檔名：{Path.GetFileName(mediaPath)}" +
                                   $"（支援 {MediaPageBuilder.SupportedExtensions}；PPT 先另存 PDF）";
                    return (false, $"face {face} 啟動失敗：{rt.LastError}");
                }
                launchUrl = $"http://127.0.0.1:{HttpPort}/media/{face}";
                break;

            default:   // wrap（V1 行為）
                launchUrl = $"http://127.0.0.1:{HttpPort}/wrap/{face}";
                break;
        }

        string profileDir = Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(_configPath)) ?? ".",
            _config.BrowserProfileDir, $"slot-{face}");

        var proc = BrowserLauncher.Launch(_browserExe, launchUrl, profileDir, rect, _log, cdpPort);
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
        rt.CdpInjected = mode == SlotConfig.ModeCdp && slot.Rotation == 0;   // rotation=0 不需注入

        // 視窗對位走背景（EnumWindows 輪詢最多 15s，不能佔住 _gate）。
        // CDP 模式視窗 title 是目標站自己的（沒有 [WP-slot-N] 標記）→ 以 pid 比對。
        bool topmost = _config.Topmost;
        int procId = proc.Id;
        Task.Run(() =>
        {
            bool ok = WindowPositioner.TryPosition(WrapPageBuilder.TitleMarker(face), rect, topmost, _log,
                                                   pid: mode == SlotConfig.ModeCdp ? procId : null);
            lock (_gate) { if (Runtime(face).Process == proc) Runtime(face).Positioned = ok; }
        });

        // CDP 旋轉注入走背景（連線輪詢最多 15s）；失敗 → 明確 Failed，不靜默
        if (mode == SlotConfig.ModeCdp && slot.Rotation != 0)
            InjectCdpRotationAsync(face, proc, cdpPort!.Value, slot.Rotation);

        return (true, $"face {face} [{faceInfo.Name}] 已啟動 @ ({rect.X},{rect.Y}) {rect.Width}x{rect.Height} " +
                      $"rot={slot.Rotation} mode={mode}" +
                      (cdpPort.HasValue ? $" cdpPort={cdpPort.Value}" : ""));
    }

    /// <summary>cdp 模式的 debug port：槽指定 cdpPort 優先，否則 cdpBasePort + face。</summary>
    public int CdpPortFor(int face, SlotConfig slot) => slot.CdpPort ?? _config.CdpBasePort + face;

    /// <summary>
    /// 背景：等 CDP port 起來 → 注入旋轉（addScriptToEvaluateOnNewDocument 讓 SPA
    /// 導航後旋轉存活 + Runtime.evaluate 立即套用當前頁）。
    /// 失敗（port 沒起來 / timeout / CDP 錯誤）→ 槽標 Failed + 收掉瀏覽器——
    /// 寧可黑屏 + 管理頁紅字，不讓內容歪著掛在牆上沒人發現。
    /// </summary>
    void InjectCdpRotationAsync(int face, Process proc, int cdpPort, int rotation)
    {
        Task.Run(async () =>
        {
            try
            {
                await using var cdp = await CdpClient.ConnectAsync(cdpPort, TimeSpan.FromSeconds(15));
                await CdpInjector.InjectRotationAsync(cdp, rotation);
                lock (_gate)
                {
                    if (Runtime(face).Process == proc) Runtime(face).CdpInjected = true;
                }
                _log.Info($"[CDP] face {face} 旋轉 {rotation}° 已注入（port {cdpPort}）");
            }
            catch (Exception ex)
            {
                _log.Error($"[CDP] face {face} 旋轉注入失敗（port {cdpPort}）：{ex.Message}");
                lock (_gate)
                {
                    var rt = Runtime(face);
                    if (rt.Process != proc) return;   // 已被重啟/停止，別動新狀態
                    rt.Status = SlotStatus.Failed;
                    rt.LastError = $"CDP 連線/注入失敗（port {cdpPort}）：{ex.Message}";
                    KillProcessLocked(rt);            // 不讓內容歪著留在牆上
                }
            }
        });
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
