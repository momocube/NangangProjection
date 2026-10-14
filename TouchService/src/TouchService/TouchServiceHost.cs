using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TouchService.Api;
using TouchService.Calibration;
using TouchService.Config;
using TouchService.Core;
using TouchService.Diagnostics;
using TouchService.Logging;
using TouchService.Mouse;
using TouchService.Output;
using TouchService.Tracking;

namespace TouchService;

/// <summary>
/// 服務組裝與生命週期：config → venue → pipeline → UDP listen / WS server / timer。
/// 從 Program.cs 抽出來讓 E2E 測試可以在測試進程內啟動完整服務（真實 socket）。
/// </summary>
public sealed class TouchServiceHost : IDisposable
{
    readonly ServiceConfig _config;
    readonly FileLogger _log;
    readonly Stopwatch _clock = Stopwatch.StartNew();
    readonly CancellationTokenSource _cts = new();
    readonly string? _configPath;
    readonly IMouseInjector? _injectorOverride;   // 測試注入 FakeInjector 用
    readonly object _configGate = new();          // 序列化 POST /config 的套用 + 寫回

    Socket? _listenSocket;
    Thread? _receiveThread;
    Thread? _timerThread;
    MouseDriver? _mouseDriver;   // touchMode=mouse 時建立，建立後活到 host 結束（grid 時只卸觀察者）
    LogStreamer? _logStreamer;   // UI 終端 log 串流（ring 300 + loopback-only push）
    CalibrationStreamer? _calibration;   // 校正工作階段 + raw 檢視串流（loopback-only push）

    public VenueProfile Venue { get; private set; } = null!;
    public TouchPipeline Pipeline { get; private set; } = null!;
    public StatsCollector Stats { get; } = new();
    public WebSocketHub? WsHub { get; private set; }

    /// <summary>指令處理（stdin 與 POST /command 共用，輸出文字一致）。Start() 後可用。</summary>
    public CommandProcessor Commands { get; private set; } = null!;

    /// <summary>優雅關閉請求（stdin/API quit、托盤「結束服務」）。Program 掛 quit event。</summary>
    public event Action? ShutdownRequested;

    public void RequestShutdown() => ShutdownRequested?.Invoke();

    /// <summary>
    /// UI 送出的切換 venue 請求(POST /venue/switch)。參數是 relative path 例如 "venues/stage.json"。
    /// Program 掛此 event 執行重啟流程。
    /// </summary>
    public event Action<string>? VenueSwitchRequested;

    public bool RequestVenueSwitch(string relPath)
    {
        lock (_configGate)
        {
            if (NoiseCapture.IsRunning) return false;
            VenueSwitchRequested?.Invoke(relPath);
            return true;
        }
    }

    /// <summary>掃 baseDir/venues 得所有可用 venue。testpage 下拉選單 / API GET /venues 用。</summary>
    public IReadOnlyList<VenueEntry> ListAvailableVenues()
        => VenueProfile.DiscoverAll(Path.Combine(BaseDir, "venues"));

    /// <summary>當前 config.venueProfile(相對路徑)。方便 UI 標示「目前生效」。</summary>
    public string CurrentVenueRelPath => _config.VenueProfile;

    /// <summary>runtime 鎖定黑名單（config + auto + manual 共用同一個過濾器）。</summary>
    public LockedCellFilter Locks { get; private set; } = LockedCellFilter.Empty;

    /// <summary>觸發歷史 + 疑似噪訊評分（GET /history、stdin `history`、UI 診斷 tab）。</summary>
    public TriggerHistory History { get; private set; } = null!;

    /// <summary>服務端清場掃描草稿與逐格聚合統計。</summary>
    public NoiseCaptureJournal NoiseCapture { get; private set; } = null!;

    /// <summary>實際監聽的 UDP port（config 給 0 時由 OS 配發，測試用）。</summary>
    public int ActualListenPort { get; private set; }

    /// <summary>
    /// configPath：savelocks / POST /config 寫回的目標（null = 無法寫回，例如純測試 host）。
    /// mouseInjector：測試注入 FakeInjector；null = 依平台選（Windows SendInput / 其他 no-op）。
    /// </summary>
    public TouchServiceHost(ServiceConfig config, string baseDir, FileLogger log, string? configPath = null,
                            IMouseInjector? mouseInjector = null)
    {
        _config = config;
        _log = log;
        BaseDir = baseDir;
        _configPath = configPath;
        _injectorOverride = mouseInjector;
    }

    public string BaseDir { get; }

    public void Start()
    {
        // 0. log 串流（UI 終端）：先掛——啟動期的 log 也進 ring，loopback client 連上補發
        _logStreamer = new LogStreamer();
        _log.EntryWritten += _logStreamer.OnLog;

        // 校正工作階段（raw 檢視串流）：常駐建立、非 active 零成本（POST /calibration/start 啟用）
        _calibration = new CalibrationStreamer(_log);

        // 1. 場域 profile
        var venuePath = Path.IsPathRooted(_config.VenueProfile)
            ? _config.VenueProfile
            : Path.Combine(BaseDir, _config.VenueProfile);
        Venue = VenueProfile.Load(venuePath);
        _log.Info($"[Host] 場域 profile：{Venue.Venue}（{Venue.Canvas.Width}×{Venue.Canvas.Height}，{Venue.Faces.Count} 面）← {venuePath}");

        if (_config.MouseModeLegacyFaceConverted)
            _log.Info($"[Host] 偵測到舊版 config 的 mouseMode.face（單數）——已自動轉成 faces:[{string.Join(",", _config.MouseMode.Faces ?? new List<int>())}]" +
                      "（下次由設定頁套用後會以新 schema 寫回 config.json）");

        // 2. 管線元件
        var router = new SensorFaceRouter(Venue);
        var tracker = new TouchTracker(
            _config.MaxSlots, _config.MatchRadiusPx, _config.ReleaseTimeoutMs, _config.MoveMinDistancePx);
        var serializer = new FrameSerializer(Venue);
        Locks = BuildLockedCellFilter();
        string venueSignature = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(Venue, VenueProfile.JsonOptions))));
        NoiseCapture = new NoiseCaptureJournal(Venue.Venue, venueSignature, Locks, _configPath,
            () => _clock.ElapsedMilliseconds, _log);

        // 啟動自檢窗（autoLock）：窗內命中任一判準（packets ≥ minPackets OR 分離 episodes ≥ minEpisodes）
        // 的格子自動鎖（依據：開機時場上理應無人）。episode 切分取 releaseTimeout 啟動值。
        AutoLockSelfCheck? autoLock = null;
        if (_config.AutoLock.Enabled && _config.AutoLock.WindowSeconds > 0)
        {
            autoLock = new AutoLockSelfCheck(
                _clock.ElapsedMilliseconds, _config.AutoLock.WindowSeconds, _config.AutoLock.MinPackets,
                _config.AutoLock.MinEpisodes, _config.ReleaseTimeoutMs);
            string criteria = autoLock.MinEpisodes > 0
                ? $"被觸發 ≥{autoLock.MinPackets} 包、或分離觸發 ≥{autoLock.MinEpisodes} 段（間隔 >{_config.ReleaseTimeoutMs}ms 切段）"
                : $"被觸發 ≥{autoLock.MinPackets} 包";
            _log.Info($"[AutoLock] 自檢窗開始：{_config.AutoLock.WindowSeconds}s 內{criteria}的格子" +
                      "將自動鎖定（此期間不發布觸控事件；keepalive 照發）");
        }
        else
        {
            _log.Info("[AutoLock] 自檢停用（autoLock.enabled=false 或 windowSeconds ≤ 0），啟動即正常發布");
        }

        // 觸發歷史（per-cell 聚合，固定大小、runtime-only 重啟歸零——刻意的，
        // 可疑晶片會很快重新冒上排行）。鄰近半徑 = 2×matchRadius（孤立因子，rationale 見類別註解）
        History = new TriggerHistory(
            _clock.ElapsedMilliseconds, _config.ReleaseTimeoutMs, _config.MatchRadiusPx * 2);

        // 指令處理（stdin + POST /command 共用）——在 hub 起來前就緒，API 一上線即可用
        var logDir = Path.IsPathRooted(_config.Log.Dir) ? _config.Log.Dir : Path.Combine(BaseDir, _config.Log.Dir);
        Commands = new CommandProcessor(this, logDir, RequestShutdown);

        var sinks = new List<ITouchSink>();
        if (_config.UdpOutput.Enabled && _config.UdpOutput.Endpoints.Count > 0)
        {
            var udp = new UdpJsonPublisher(_config.UdpOutput.Endpoints, _log);
            sinks.Add(udp);
            _log.Info($"[Host] raw UDP JSON 輸出 → {string.Join(", ", _config.UdpOutput.Endpoints)}");
        }
        if (_config.WebSocketOutput.Enabled)
        {
            var configApi = new ConfigApi(this);   // 本機管理 API（/config、/monitors、/locks/*、/command）
            WsHub = new WebSocketHub(
                _config.WebSocketOutput.Port,
                BuildVenueJson(),
                BuildHelloJson,   // 每個 client 連上時取當下鎖定清單 / 自檢狀態
                FindTestPage(),
                _log,
                configApi.Handle,
                () => _logStreamer!.Backlog());   // loopback client 連上補發最近 300 行 log
            WsHub.Start();
            sinks.Add(WsHub);
            _logStreamer.Attach(WsHub);           // log push（僅 loopback client）
            _calibration.Attach(WsHub);           // raw push（僅 loopback client；校正階段才有訊息）
        }
        if (_config.TuioOutput.Enabled)
        {
            // v1 規格預留插槽：enabled = true 會在這裡直接 throw NotImplementedException
            sinks.Add(new TuioPublisher(_config.TuioOutput.Endpoint, _log));
        }

        Pipeline = new TouchPipeline(
            router, tracker, serializer, sinks, Stats, _log, _config.KeepaliveIntervalMs, Locks, autoLock,
            History, _calibration, NoiseCapture);
        Pipeline.AutoLockCompleted += _ => PushLocks();   // 自檢結束 → 通知所有 WS client 最新鎖定清單

        // 觸控模式（grid / mouse）：mouse 時掛 MouseDriver 為 typed 事件觀察者（額外 sink，發布照常）
        lock (_configGate) ApplyTouchModeLocked();

        // 3. UDP 監聽（感應模組廣播）
        _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listenSocket.Bind(new IPEndPoint(IPAddress.Any, _config.ListenPort));
        ActualListenPort = ((IPEndPoint)_listenSocket.LocalEndPoint!).Port;
        _log.Info($"[Host] 監聽感應 UDP port {ActualListenPort}");

        _receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "UdpSensor" };
        _receiveThread.Start();

        // 4. timer：release timeout 掃描 + keepalive
        _timerThread = new Thread(TimerLoop) { IsBackground = true, Name = "FlushTimer" };
        _timerThread.Start();

        _log.Info($"[Host] 啟動完成（releaseTimeout={_config.ReleaseTimeoutMs}ms matchRadius={_config.MatchRadiusPx}px maxSlots={_config.MaxSlots}）");
    }

    /// <summary>receive 執行緒：阻塞收包，直接走完整條管線（事件驅動，無佇列）。</summary>
    void ReceiveLoop()
    {
        var buf = new byte[2048];
        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

        while (!_cts.IsCancellationRequested)
        {
            int len;
            int srcPort = 0;
            string? srcAddr = null;
            try
            {
                len = _listenSocket!.ReceiveFrom(buf, ref remote);
                srcPort = ((IPEndPoint)remote).Port;
                // 來源 IP 字串只在校正 raw tap 進行中才解析（平時不為每個 datagram 配置字串）
                if (Pipeline.CalibrationTapActive)
                    srcAddr = ((IPEndPoint)remote).Address.ToString();
            }
            catch (SocketException se)
            {
                if (!_cts.IsCancellationRequested)
                    _log.Warn($"[Host] socket 錯誤：{se.SocketErrorCode}");
                continue;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            try
            {
                Pipeline.OnDatagram(buf.AsSpan(0, len), _clock.ElapsedMilliseconds, srcPort, srcAddr);
            }
            catch (Exception ex)
            {
                _log.Error($"[Host] 管線處理失敗：{ex}");
            }
        }
    }

    void TimerLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            Thread.Sleep(_config.FlushIntervalMs);
            try
            {
                Pipeline.Tick(_clock.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _log.Error($"[Host] timer tick 失敗：{ex}");
            }
        }
    }

    byte[] BuildVenueJson() =>
        JsonSerializer.SerializeToUtf8Bytes(Venue, VenueProfile.JsonOptions);

    /// <summary>
    /// WS 連上時下發的 hello：venue 輪廓（含畫布像素幾何，testpage 直接畫）
    /// + 當下鎖定清單（lockedCells，含來源）+ 自檢窗狀態。每個 client 連上時即時生成。
    /// </summary>
    byte[] BuildHelloJson()
    {
        var (autoLockActive, autoLockRemaining) = AutoLockStatus();
        var hello = new
        {
            type = "hello",
            v = 1,
            venue = Venue.Venue,
            canvas = new { w = Venue.Canvas.Width, h = Venue.Canvas.Height },
            releaseTimeoutMs = _config.ReleaseTimeoutMs,
            faces = Venue.Faces.Select((f, i) => new
            {
                index = i,
                name = f.Name,
                originX = f.OriginX,
                originY = f.OriginY,
                width = f.TotalPixelWidth,
                height = f.TotalPixelHeight,
                cols = f.ColCount,
                rows = f.RowCount,
                cellW = f.ScreenCellW,
                cellH = f.ScreenCellH,
            }).ToArray(),
            lockedCells = LockedCellsPayload(),
            autoLockActive,
            autoLockRemainingSeconds = autoLockRemaining,
        };
        return JsonSerializer.SerializeToUtf8Bytes(hello);
    }

    /// <summary>
    /// 鎖定清單的對外 payload（hello / locks push / GET /locks 共用格式）。
    /// v9 起帶保存狀態與鎖後活動：saved = config 來源（已寫進 config.json）、
    /// 其餘（auto / manual）= 臨時鎖（重啟即消失）；droppedSinceLock / lastDropAgoMs =
    /// 鎖定那一刻之後該格又發了幾包、最後一包多久前（null = 鎖後沒再發過）——
    /// 鎖後仍發包 = 鐵證噪訊、鎖後安靜 = 可能誤鎖。
    /// </summary>
    object LockedCellsPayload()
    {
        long nowMs = _clock.ElapsedMilliseconds;
        return Locks.Snapshot().Select(e => new
        {
            face = e.Face,
            cell = new[] { e.Col, e.Row },
            source = LockedCellFilter.SourceName(e.Source),
            saved = e.Source == LockSource.Config,
            packets = e.Packets,
            lockedAgoMs = Math.Max(0, nowMs - e.LockedAtMs),
            droppedSinceLock = e.DroppedSinceLock,
            lastDropAgoMs = e.LastDropMs >= 0 ? (long?)Math.Max(0, nowMs - e.LastDropMs) : null,
        }).ToArray();
    }

    /// <summary>GET /locks：即時鎖定清單 JSON（UI 臨時鎖區輪詢鎖後活動用）。</summary>
    public string LocksJson() =>
        JsonSerializer.Serialize(new { cells = LockedCellsPayload() });

    (bool Active, int RemainingSeconds) AutoLockStatus() =>
        Pipeline?.GetAutoLockStatus(_clock.ElapsedMilliseconds) ?? (false, 0);

    /// <summary>鎖定變動（自檢結束 / lock / unlock）→ 對所有 WS client 推 locks 訊息。</summary>
    void PushLocks()
    {
        if (WsHub == null) return;
        var (autoLockActive, autoLockRemaining) = AutoLockStatus();
        WsHub.Publish(JsonSerializer.SerializeToUtf8Bytes(new
        {
            type = "locks",
            lockedCells = LockedCellsPayload(),
            autoLockActive,
            autoLockRemainingSeconds = autoLockRemaining,
        }));
    }

    string? FindTestPage()
    {
        var p = Path.Combine(BaseDir, "testpage", "index.html");
        return File.Exists(p) ? p : null;
    }

    public string StatsSnapshot() =>
        Stats.Snapshot(Pipeline?.ActiveTouches ?? 0, WsHub?.ClientCount ?? 0, Locks.AutoCount);

    /// <summary>
    /// config lockedCells → 過濾器 + 啟動 log（鎖定晶片 N 顆與清單；格式壞項 / 超出場域範圍提出警告）。
    /// </summary>
    LockedCellFilter BuildLockedCellFilter()
    {
        var filter = new LockedCellFilter(_config.LockedCells);

        if (filter.Skipped > 0)
            _log.Warn($"[Host] config lockedCells 有 {filter.Skipped} 筆格式不完整（需 face ≥ 0、cell = [col,row]），已跳過");

        if (filter.Count == 0)
        {
            _log.Info("[Host] 鎖定晶片 0 顆（lockedCells 為空，不過濾）");
            return filter;
        }

        var entries = filter.Snapshot();
        var list = string.Join("、", entries.Select(e => $"face={e.Face} cell=({e.Col},{e.Row})"));
        _log.Info($"[Host] 鎖定晶片 {filter.Count} 顆（命中點進 tracker 前丟棄）：{list}");

        // 防呆：填錯座標（超出場域範圍）永遠不會命中，啟動時就提醒
        foreach (var e in entries)
        {
            if (e.Face >= Venue.Faces.Count)
                _log.Warn($"[Host] lockedCells face={e.Face} 超出場域面數（共 {Venue.Faces.Count} 面）——此項永遠不會命中");
            else if (e.Col >= Venue.Faces[e.Face].ColCount || e.Row >= Venue.Faces[e.Face].RowCount)
                _log.Warn($"[Host] lockedCells face={e.Face} cell=({e.Col},{e.Row}) 超出該面格數" +
                          $"（{Venue.Faces[e.Face].ColCount}×{Venue.Faces[e.Face].RowCount}）——此項永遠不會命中");
        }
        return filter;
    }

    // ── 觸控模式（grid / mouse）與 config 熱套用 ──────────────────────────────

    /// <summary>
    /// mouseMode.faces 解析成有效啟用面集合：
    /// faces 有給 → 過濾掉超範圍 index（log warn）；
    /// 缺欄位（null）→ 預設 = 全部面但排除名稱含 Floor 的面（704 = 地板關、六個牆面開）。
    /// </summary>
    public IReadOnlyList<int> ResolveMouseFaces()
    {
        var mm = _config.MouseMode;
        if (mm.Faces != null)
        {
            var valid = mm.Faces.Where(f => f >= 0 && f < Venue.Faces.Count).Distinct().OrderBy(f => f).ToList();
            foreach (var bad in mm.Faces.Where(f => f < 0 || f >= Venue.Faces.Count).Distinct())
                _log.Warn($"[Mouse] mouseMode.faces 含超範圍 index {bad}（場域共 {Venue.Faces.Count} 面）——忽略此項");
            return valid;
        }
        return Enumerable.Range(0, Venue.Faces.Count)
            .Where(i => !Venue.Faces[i].Name.Contains("Floor", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// 依 _config.TouchMode 掛/卸 MouseDriver（持 _configGate 呼叫）。
    /// mouse：建立（或熱更新參數）driver 並掛上 pipeline 觀察者；
    /// grid：卸觀察者，若 owner 還按著先 LEFTUP（熱切不留卡死的按鍵）。
    /// driver 建立後活到 host 結束——重複 mouse↔grid 切換不重建執行緒。
    /// </summary>
    void ApplyTouchModeLocked()
    {
        if (_config.TouchMode == "mouse")
        {
            var faces = ResolveMouseFaces();
            if (_mouseDriver == null)
            {
                _mouseDriver = new MouseDriver(CreateInjector(), Venue, _config.MouseMode, faces, _log);
                _mouseDriver.Start();   // 8ms 平滑迴圈
            }
            else
            {
                _mouseDriver.UpdateConfig(_config.MouseMode, faces);
            }
            Pipeline.SetEventObserver(_mouseDriver);
            var m = _config.MouseMode;
            string faceList = faces.Count == 0
                ? "（無——所有面停用，滑鼠 sink 不動作）"
                : string.Join("、", faces.Select(f => $"{f} {Venue.Faces[f].Name}"));
            _log.Info($"[Mouse] 滑鼠模式啟用：canvas 十字映射 → 螢幕 rect " +
                      $"({m.Screen.X},{m.Screen.Y} {m.Screen.Width}×{m.Screen.Height})，" +
                      $"content 校正 offset=({m.Content.OffsetX},{m.Content.OffsetY}) scale=({m.Content.ScaleX},{m.Content.ScaleY})，" +
                      $"smoothing={m.Smoothing} moveMinPx={m.MoveMinPx}；啟用面：{faceList}" +
                      "（發布端不受影響，照常全量輸出）");
        }
        else
        {
            Pipeline.SetEventObserver(null);
            _mouseDriver?.ForceRelease();
            _log.Info("[Mouse] 採格模式（grid）：純發布，無滑鼠注入");
        }
    }

    IMouseInjector CreateInjector()
    {
        if (_injectorOverride != null) return _injectorOverride;
        if (OperatingSystem.IsWindows()) return new Win32MouseInjector();
        _log.Warn("[Mouse] 非 Windows 平台不支援滑鼠注入——滑鼠模式為 no-op（事件照常發布，不影響服務）");
        return new NullMouseInjector();
    }

    /// <summary>
    /// GET /config：目前生效設定（去敏感：不含 log 設定 / 檔案路徑）。
    /// restartOnly 區塊 = 需重啟欄位，UI 唯讀顯示並標註「改 config.json 後重啟」。
    /// </summary>
    public string GetEffectiveConfigJson()
    {
        lock (_configGate)
        {
            return JsonSerializer.Serialize(new
            {
                touchMode = _config.TouchMode,
                mouseMode = new
                {
                    faces = ResolveMouseFaces(),   // 已解析的啟用面（缺欄位時為預設：排除 Floor）
                    screen = new
                    {
                        x = _config.MouseMode.Screen.X,
                        y = _config.MouseMode.Screen.Y,
                        width = _config.MouseMode.Screen.Width,
                        height = _config.MouseMode.Screen.Height,
                    },
                    content = new
                    {
                        offsetX = _config.MouseMode.Content.OffsetX,
                        offsetY = _config.MouseMode.Content.OffsetY,
                        scaleX = _config.MouseMode.Content.ScaleX,
                        scaleY = _config.MouseMode.Content.ScaleY,
                    },
                    smoothing = _config.MouseMode.Smoothing,
                    moveMinPx = _config.MouseMode.MoveMinPx,
                },
                releaseTimeoutMs = _config.ReleaseTimeoutMs,
                matchRadiusPx = _config.MatchRadiusPx,
                autoLock = new
                {
                    enabled = _config.AutoLock.Enabled,
                    windowSeconds = _config.AutoLock.WindowSeconds,
                    minPackets = _config.AutoLock.MinPackets,
                    minEpisodes = _config.AutoLock.MinEpisodes,
                },
                noiseCapture = new
                {
                    lookbackSeconds = _config.NoiseCapture.LookbackSeconds,
                    captureSeconds = _config.NoiseCapture.CaptureSeconds,
                },
                ui = new { autoLaunch = _config.Ui.AutoLaunch, trayEnabled = _config.Ui.TrayEnabled },
                venue = Venue.Venue,
                restartOnly = new
                {
                    listenPort = _config.ListenPort,
                    webSocketPort = _config.WebSocketOutput.Port,
                    udpEndpoints = _config.UdpOutput.Enabled ? _config.UdpOutput.Endpoints : new List<string>(),
                },
            });
        }
    }

    /// <summary>
    /// POST /config：套用設定 patch（只動 body 有給的欄位）。
    /// 流程：解析 + **全部驗證通過才套用**（不留半套狀態）→ 熱套用 runtime
    /// （touchMode / mouseMode.* / releaseTimeoutMs / matchRadiusPx 下個 touch 生效；
    /// autoLock 只寫回 config、下次啟動生效）→ 寫回 config.json（JsonNode 保留其他欄位）。
    /// </summary>
    public (bool Ok, string Message) ApplyConfigPatch(string json)
    {
        JsonObject patch;
        try
        {
            patch = JsonNode.Parse(json) as JsonObject
                    ?? throw new JsonException("body 必須是 JSON object");
        }
        catch (Exception ex)
        {
            return (false, $"JSON 解析失敗：{ex.Message}");
        }

        lock (_configGate)
        {
            if (NoiseCapture.IsRunning)
                return (false, "清場掃描期間不能修改服務設定");
            // ── 1. 以目前生效值為基底，套上 patch 的欄位（先全部驗證，全過才套用）──
            var errors = new List<string>();

            string touchMode = _config.TouchMode;
            if (TryReadString(patch, "touchMode", errors, out var tm))
            {
                touchMode = tm.Trim().ToLowerInvariant();
                if (touchMode is not ("grid" or "mouse"))
                    errors.Add($"touchMode 必須是 \"grid\" 或 \"mouse\"（收到 \"{tm}\"）");
            }

            int releaseTimeout = ReadInt(patch, "releaseTimeoutMs", _config.ReleaseTimeoutMs, 10, 10_000, errors);
            double matchRadius = ReadNumber(patch, "matchRadiusPx", _config.MatchRadiusPx, 1, 10_000, errors);

            var mm = _config.MouseMode;
            List<int>? mFaces = mm.Faces?.ToList();   // null = 預設（排除 Floor），交給 ResolveMouseFaces
            int sx = mm.Screen.X, sy = mm.Screen.Y, sw = mm.Screen.Width, sh = mm.Screen.Height;
            double cox = mm.Content.OffsetX, coy = mm.Content.OffsetY;
            double csx = mm.Content.ScaleX, csy = mm.Content.ScaleY;
            double smoothing = mm.Smoothing;
            int moveMinPx = mm.MoveMinPx;
            if (patch["mouseMode"] is JsonObject pm)
            {
                if (pm["faces"] is JsonArray pf)
                {
                    var parsed = new List<int>();
                    foreach (var node in pf)
                    {
                        if (node?.GetValueKind() != JsonValueKind.Number ||
                            !int.TryParse(node.ToJsonString(), out var fv) ||
                            fv < 0 || fv >= Venue.Faces.Count)
                        {
                            errors.Add($"mouseMode.faces 每項必須是 0~{Venue.Faces.Count - 1} 的整數（venue faces index）");
                            parsed = null!;
                            break;
                        }
                        if (!parsed.Contains(fv)) parsed.Add(fv);
                    }
                    if (parsed != null) mFaces = parsed;   // 空陣列合法 = 全部停用
                }
                else if (pm.ContainsKey("faces"))
                {
                    errors.Add("mouseMode.faces 必須是整數陣列（例 [0,1,3,4,5,6]）");
                }
                else if (pm["face"] is { } legacyNode)
                {
                    // 向後相容：舊欄位 face（單數 int）→ faces:[face]
                    int legacy = ReadInt(pm, "face", -1, 0, Venue.Faces.Count - 1, errors,
                        $"mouseMode.face（舊欄位）必須是 0~{Venue.Faces.Count - 1}");
                    if (legacy >= 0) mFaces = new List<int> { legacy };
                    _ = legacyNode;
                }

                smoothing = ReadNumber(pm, "smoothing", smoothing, 0.01, 1.0, errors,
                    "mouseMode.smoothing 必須在 0.01~1（1 = 不平滑直接到位）");
                moveMinPx = ReadInt(pm, "moveMinPx", moveMinPx, 0, 1000, errors);
                if (pm["screen"] is JsonObject ps)
                {
                    sx = ReadInt(ps, "x", sx, -1_000_000, 1_000_000, errors);
                    sy = ReadInt(ps, "y", sy, -1_000_000, 1_000_000, errors);
                    sw = ReadInt(ps, "width", sw, 1, 1_000_000, errors, "mouseMode.screen.width 必須 ≥ 1");
                    sh = ReadInt(ps, "height", sh, 1, 1_000_000, errors, "mouseMode.screen.height 必須 ≥ 1");
                }
                else if (pm.ContainsKey("screen"))
                {
                    errors.Add("mouseMode.screen 必須是 object {x,y,width,height}");
                }
                if (pm["content"] is JsonObject pc)
                {
                    cox = ReadNumber(pc, "offsetX", cox, -1_000_000, 1_000_000, errors);
                    coy = ReadNumber(pc, "offsetY", coy, -1_000_000, 1_000_000, errors);
                    csx = ReadNumber(pc, "scaleX", csx, 0.01, 100, errors,
                        "mouseMode.content.scaleX 必須在 0.01~100（1 = 滿版）");
                    csy = ReadNumber(pc, "scaleY", csy, 0.01, 100, errors,
                        "mouseMode.content.scaleY 必須在 0.01~100（1 = 滿版）");
                }
                else if (pm.ContainsKey("content"))
                {
                    errors.Add("mouseMode.content 必須是 object {offsetX,offsetY,scaleX,scaleY}");
                }
            }
            else if (patch.ContainsKey("mouseMode"))
            {
                errors.Add("mouseMode 必須是 object");
            }

            var al = _config.AutoLock;
            bool alEnabled = al.Enabled;
            int alWindow = al.WindowSeconds, alMin = al.MinPackets, alMinEp = al.MinEpisodes;
            if (patch["autoLock"] is JsonObject pa)
            {
                alEnabled = ReadBool(pa, "enabled", alEnabled, errors);
                alWindow = ReadInt(pa, "windowSeconds", alWindow, 0, 86_400, errors);
                alMin = ReadInt(pa, "minPackets", alMin, 1, 1_000_000, errors);
                alMinEp = ReadInt(pa, "minEpisodes", alMinEp, 0, 1_000_000, errors);   // 0 = 停用 episode 判準
            }
            else if (patch.ContainsKey("autoLock"))
            {
                errors.Add("autoLock 必須是 object");
            }

            if (errors.Count > 0)
                return (false, string.Join("；", errors));

            // ── 2. 熱套用 runtime ────────────────────────────────────────────
            _config.TouchMode = touchMode;
            _config.ReleaseTimeoutMs = releaseTimeout;
            _config.MatchRadiusPx = (float)matchRadius;
            _config.MouseMode = new MouseModeConfig
            {
                Faces = mFaces,
                Screen = new ScreenRectConfig { X = sx, Y = sy, Width = sw, Height = sh },
                Content = new ContentRectConfig { OffsetX = cox, OffsetY = coy, ScaleX = csx, ScaleY = csy },
                Smoothing = smoothing,
                MoveMinPx = moveMinPx,
            };
            _config.AutoLock = new AutoLockConfig
            {
                Enabled = alEnabled, WindowSeconds = alWindow, MinPackets = alMin, MinEpisodes = alMinEp,
            };

            Pipeline.UpdateTuning(_config.MatchRadiusPx, _config.ReleaseTimeoutMs);   // 下個 touch / flush 生效
            ApplyTouchModeLocked();   // mouse↔grid 熱切（grid 化時 owner 還按著會先 LEFTUP）

            // ── 3. 寫回 config.json（保留其他欄位）──────────────────────────
            var persistError = PersistManagedFields();
            _log.Info($"[Config] POST /config 已套用：touchMode={touchMode} " +
                      $"releaseTimeout={releaseTimeout}ms matchRadius={matchRadius}px" +
                      (persistError == null ? "（已寫回 config.json）" : $"（寫回失敗：{persistError}）"));

            return persistError == null
                ? (true, "已套用並寫回 config.json（autoLock 欄位下次啟動生效）")
                : (true, $"runtime 已套用，但寫回 config.json 失敗：{persistError}（重啟後會還原）");
        }
    }

    /// <summary>
    /// 把 API 管的欄位（touchMode / mouseMode / releaseTimeoutMs / matchRadiusPx / autoLock）
    /// 寫回 config.json——與 SaveLocks 同一套 JsonNode 機制：只換目標欄位，其他欄位
    /// （含未知欄位）原樣保留。回傳 null = 成功，否則錯誤訊息。持 _configGate 呼叫。
    /// </summary>
    string? PersistManagedFields()
    {
        if (_configPath == null) return "host 啟動時未提供 config 路徑";
        if (!File.Exists(_configPath)) return $"config 不存在：{_configPath}";

        try
        {
            var root = JsonNode.Parse(
                File.ReadAllText(_configPath),
                documentOptions: new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                })!.AsObject();

            root["touchMode"] = _config.TouchMode;
            root["releaseTimeoutMs"] = _config.ReleaseTimeoutMs;
            root["matchRadiusPx"] = _config.MatchRadiusPx;
            // faces 寫回解析後的清單（v6 schema；舊欄位 face 整個 mouseMode 物件替換時自然消失）。
            // 注意要用 JsonNode 轉換——JsonArray.Add(int) 會選到泛型 Add<T>，
            // 序列化時要求 TypeInfoResolver 直接炸（.NET 8 行為）
            var facesArr = new JsonArray();
            foreach (var f in ResolveMouseFaces()) facesArr.Add((JsonNode)f);
            root["mouseMode"] = new JsonObject
            {
                ["faces"] = facesArr,
                ["screen"] = new JsonObject
                {
                    ["x"] = _config.MouseMode.Screen.X,
                    ["y"] = _config.MouseMode.Screen.Y,
                    ["width"] = _config.MouseMode.Screen.Width,
                    ["height"] = _config.MouseMode.Screen.Height,
                },
                ["content"] = new JsonObject
                {
                    ["offsetX"] = _config.MouseMode.Content.OffsetX,
                    ["offsetY"] = _config.MouseMode.Content.OffsetY,
                    ["scaleX"] = _config.MouseMode.Content.ScaleX,
                    ["scaleY"] = _config.MouseMode.Content.ScaleY,
                },
                ["smoothing"] = _config.MouseMode.Smoothing,
                ["moveMinPx"] = _config.MouseMode.MoveMinPx,
            };
            root["autoLock"] = new JsonObject
            {
                ["enabled"] = _config.AutoLock.Enabled,
                ["windowSeconds"] = _config.AutoLock.WindowSeconds,
                ["minPackets"] = _config.AutoLock.MinPackets,
                ["minEpisodes"] = _config.AutoLock.MinEpisodes,
            };

            File.WriteAllText(_configPath,
                root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    // ── patch 欄位讀取 helper（型別錯 / 超出範圍 → errors，回傳目前值）─────────

    static bool TryReadString(JsonObject obj, string key, List<string> errors, out string value)
    {
        value = "";
        if (obj[key] is not { } node) return false;
        if (node.GetValueKind() != JsonValueKind.String)
        {
            errors.Add($"{key} 必須是字串");
            return false;
        }
        value = node.GetValue<string>();
        return true;
    }

    static int ReadInt(JsonObject obj, string key, int current, int min, int max,
                       List<string> errors, string? customError = null)
    {
        if (obj[key] is not { } node) return current;
        if (node.GetValueKind() != JsonValueKind.Number ||
            !int.TryParse(node.ToJsonString(), out var v) || v < min || v > max)
        {
            errors.Add(customError ?? $"{key} 必須是 {min}~{max} 的整數");
            return current;
        }
        return v;
    }

    static double ReadNumber(JsonObject obj, string key, double current, double min, double max,
                             List<string> errors, string? customError = null)
    {
        if (obj[key] is not { } node) return current;
        if (node.GetValueKind() != JsonValueKind.Number ||
            !double.TryParse(node.ToJsonString(),
                System.Globalization.CultureInfo.InvariantCulture, out var v) ||
            double.IsNaN(v) || v < min || v > max)
        {
            errors.Add(customError ?? $"{key} 必須是 {min}~{max} 的數值");
            return current;
        }
        return v;
    }

    static bool ReadBool(JsonObject obj, string key, bool current, List<string> errors)
    {
        if (obj[key] is not { } node) return current;
        var kind = node.GetValueKind();
        if (kind is not (JsonValueKind.True or JsonValueKind.False))
        {
            errors.Add($"{key} 必須是 true/false");
            return current;
        }
        return node.GetValue<bool>();
    }

    // ── 鎖定管理指令（stdin locks / lock / unlock / savelocks）───────────────

    /// <summary>stdin `locks` 指令：列出目前所有鎖定（face / cell / 來源 / auto 封包數）。</summary>
    public string LocksSnapshot()
    {
        var entries = Locks.Snapshot();
        var sb = new StringBuilder();
        int nConfig = entries.Count(e => e.Source == LockSource.Config);
        int nAuto = entries.Count(e => e.Source == LockSource.Auto);
        int nManual = entries.Count(e => e.Source == LockSource.Manual);
        sb.AppendLine($"── 鎖定晶片：{entries.Count} 顆（config {nConfig} / auto {nAuto} / manual {nManual}）──────");

        var (autoLockActive, remaining) = AutoLockStatus();
        if (autoLockActive)
            sb.AppendLine($"（啟動自檢進行中，剩 {remaining}s——自動鎖定結果窗結束時才會出現）");

        if (entries.Count == 0)
        {
            sb.AppendLine("（目前沒有任何鎖定）");
        }
        else
        {
            long nowMs = _clock.ElapsedMilliseconds;
            sb.AppendLine($"{"face",-22}{"cell",-11}{"來源",-8}{"封包(auto)",-12}{"鎖後封包",-10}最後活動");
            foreach (var e in entries)
            {
                string face = e.Face >= 0 && e.Face < Venue.Faces.Count
                    ? $"{e.Face} {Venue.Faces[e.Face].Name}"
                    : e.Face.ToString();
                string lastDrop = e.LastDropMs >= 0
                    ? $"{(nowMs - e.LastDropMs) / 1000.0:F1}s 前"
                    : "鎖後安靜";
                sb.AppendLine(
                    $"{face,-22}{$"({e.Col},{e.Row})",-11}{LockedCellFilter.SourceName(e.Source),-8}" +
                    $"{(e.Source == LockSource.Auto ? e.Packets.ToString() : "-"),-12}" +
                    $"{e.DroppedSinceLock,-10}{lastDrop}");
            }
            sb.AppendLine("鎖後封包 = 鎖定那一刻之後該格又發的包數（仍在發 = 鐵證噪訊；鎖後安靜 = 可能誤鎖）。");
            sb.AppendLine("解鎖：unlock <face> <col> <row>（全部：unlockall）；寫回 config：savelocks（auto+manual+config 合併）。");
        }
        sb.Append("───────────────────────────────────────────────");
        return sb.ToString();
    }

    /// <summary>stdin `lock` 指令：手動加鎖（runtime，來源 manual）。</summary>
    public string LockCell(int face, int col, int row)
    {
        lock (_configGate)
        {
        if (NoiseCapture.IsRunning) return "清場掃描期間不能手動增刪鎖定";
        if (face < 0 || face >= Venue.Faces.Count)
            return $"face={face} 超出場域面數（0~{Venue.Faces.Count - 1}），未加鎖";
        var f = Venue.Faces[face];
        if (col < 0 || col >= f.ColCount || row < 0 || row >= f.RowCount)
            return $"cell=({col},{row}) 超出 face {face}（{f.Name}）格數 {f.ColCount}×{f.RowCount}，未加鎖";

        if (!Locks.TryAdd(face, col, row, LockSource.Manual, nowMs: _clock.ElapsedMilliseconds))
        {
            var existed = Locks.Find(face, col, row);
            return $"face={face} cell=({col},{row}) 已鎖定（來源 {LockedCellFilter.SourceName(existed!.Source)}），未重複加鎖";
        }
        _log.Info($"[Host] 手動鎖定 face={face} cell=({col},{row})（manual，目前共 {Locks.Count} 顆）");
        PushLocks();
        return $"已鎖定 face={face} cell=({col},{row})（manual）。寫回 config 用 savelocks。";
        }
    }

    /// <summary>stdin `unlock` 指令：解鎖（任何來源都可解；config 來源只影響 runtime，不改檔）。</summary>
    public string UnlockCell(int face, int col, int row)
    {
        lock (_configGate)
        {
        if (NoiseCapture.IsRunning) return "清場掃描期間不能手動增刪鎖定";
        if (!Locks.TryRemove(face, col, row, out var removed))
            return $"face={face} cell=({col},{row}) 本來就沒有鎖定";

        _log.Info($"[Host] 解鎖 face={face} cell=({col},{row})（原來源 {LockedCellFilter.SourceName(removed!.Source)}，剩 {Locks.Count} 顆）");
        PushLocks();
        var note = removed.Source == LockSource.Config
            ? "（config 來源：只影響本次執行，config.json 未修改——重啟會再鎖回；要永久解除請刪 config 條目或 savelocks）"
            : "";
        return $"已解鎖 face={face} cell=({col},{row})（原來源 {LockedCellFilter.SourceName(removed.Source)}）{note}";
        }
    }

    /// <summary>
    /// stdin `unlockall` / POST /locks/unlockall（v9）：清空當前**全部**鎖定（config+auto+manual）。
    /// 只動 runtime——config.json 不寫、重啟會回到 config 的鎖定清單；要永久生效再 savelocks。
    /// 破壞性操作的確認步驟在 UI（兩段式按鈕），後端冪等執行。
    /// </summary>
    public string UnlockAllCells()
    {
        lock (_configGate)
        {
        if (NoiseCapture.IsRunning) return "清場掃描期間不能解除鎖定";
        int n = Locks.Clear();
        if (n == 0) return "目前沒有任何鎖定，無可解除";

        _log.Warn($"[Host] 解鎖全部：解除 {n} 格鎖定（runtime；config.json 未修改——重啟會回到 config 的鎖定清單，要永久生效請 savelocks）");
        PushLocks();
        return $"已解除全部 {n} 格鎖定（runtime 即時生效）。config.json 未修改——重啟會回到 config 的鎖定清單；要讓「目前狀態」永久生效請再按保存（savelocks）。";
        }
    }

    /// <summary>
    /// POST /locks/promote（v9「轉正式並保存」單格版）：把一格臨時鎖寫進 config.json 的
    /// lockedCells（只 append 這一格、其他欄位原樣保留）+ runtime 來源改標 config。
    /// 與 savelocks 的差異：savelocks 寫回**全部**鎖定，promote 只轉這一格——
    /// 臨時鎖區逐格確認時用。
    /// </summary>
    public (bool Ok, string Message) PromoteLock(int face, int col, int row)
    {
        lock (_configGate)
        {
        if (NoiseCapture.IsRunning) return (false, "清場掃描期間不能保存或修改鎖定");
        var entry = Locks.Find(face, col, row);
        if (entry == null)
            return (false, $"face={face} cell=({col},{row}) 沒有鎖定，無法轉正式");
        if (entry.Source == LockSource.Config)
            return (true, $"face={face} cell=({col},{row}) 已是保存狀態（config 來源），不需重複轉正式");

        if (_configPath == null) return (false, "此 host 啟動時未提供 config 路徑，無法寫回");
        if (!File.Exists(_configPath)) return (false, $"config 不存在：{_configPath}，未寫回");

        lock (_configGate)
        {
            try
            {
                var root = JsonNode.Parse(
                    File.ReadAllText(_configPath),
                    documentOptions: new JsonDocumentOptions
                    {
                        CommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                    })!.AsObject();

                if (root["lockedCells"] is not JsonArray arr)
                    root["lockedCells"] = arr = new JsonArray();

                bool exists = arr.OfType<JsonObject>().Any(o =>
                    (int?)o["face"] == face &&
                    o["cell"] is JsonArray c && c.Count == 2 &&
                    (int?)c[0] == col && (int?)c[1] == row);
                if (!exists)
                {
                    arr.Add(new JsonObject
                    {
                        ["face"] = face,
                        ["cell"] = new JsonArray(col, row),
                    });
                    File.WriteAllText(_configPath,
                        root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch (Exception ex)
            {
                return (false, $"寫回失敗：{ex.Message}");
            }
        }

        Locks.Reclassify(face, col, row, LockSource.Config);   // 已落盤 → 標成已保存
        _log.Info($"[Host] 轉正式：face={face} cell=({col},{row}) 寫進 config lockedCells（下次啟動以 config 來源載入）");
        PushLocks();
        return (true, $"face={face} cell=({col},{row}) 已轉正式並寫進 config.json（重啟後仍鎖定）");
        }
    }

    /// <summary>
    /// stdin `savelocks` 指令：把目前 runtime 全部鎖定（config+auto+manual 合併去重）
    /// 寫回 config.json 的 lockedCells。用 JsonNode 只換 lockedCells 一個欄位，
    /// 其他欄位（含未知欄位）原樣保留，縮排維持 2 空格。
    /// v9：寫回成功後 runtime 來源全部改標 config——已落盤的鎖就是「已保存」，
    /// UI 臨時鎖區自然清空（= 全部轉正式）。
    /// </summary>
    public string SaveLocks()
    {
        lock (_configGate)
        {
        if (NoiseCapture.IsRunning) return "清場掃描期間不能保存鎖定";
        if (_configPath == null)
            return "此 host 啟動時未提供 config 路徑，無法寫回";
        if (!File.Exists(_configPath))
            return $"config 不存在：{_configPath}，未寫回";

        var entries = Locks.Snapshot();
        lock (_configGate)
        {
            try
            {
                var root = System.Text.Json.Nodes.JsonNode.Parse(
                    File.ReadAllText(_configPath),
                    documentOptions: new JsonDocumentOptions
                    {
                        CommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                    })!.AsObject();

                var arr = new System.Text.Json.Nodes.JsonArray();
                foreach (var e in entries)
                {
                    arr.Add(new System.Text.Json.Nodes.JsonObject
                    {
                        ["face"] = e.Face,
                        ["cell"] = new System.Text.Json.Nodes.JsonArray(e.Col, e.Row),
                    });
                }
                root["lockedCells"] = arr;

                File.WriteAllText(_configPath,
                    root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return $"寫回失敗：{ex.Message}";
            }
        }

        if (Locks.ReclassifyAll(LockSource.Config) > 0) PushLocks();   // 已落盤 → 全部標成已保存
        _log.Info($"[Host] savelocks：寫回 {entries.Count} 顆鎖定到 {_configPath}");
        return $"已寫回 {entries.Count} 顆鎖定到 {_configPath} 的 lockedCells（下次啟動以 config 來源載入）";
        }
    }

    // ── 清場掃描（逐格 raw capture + 臨時鎖）───────────────────────────────

    public bool NoiseCaptureActive => NoiseCapture?.IsRunning == true;

    public string NoiseCaptureJson()
    {
        var snapshot = NoiseCapture.Snapshot();
        return JsonSerializer.Serialize(new
        {
            available = _configPath != null,
            snapshot,
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public (bool Ok, string Message, string Json) StartNoiseCapture(string mode)
    {
        lock (_configGate)
        {
            if (_calibration is { Active: true })
                return (false, "請先結束校正再開始清場掃描", NoiseCaptureJson());
            var result = Pipeline.StartNoiseCapture(mode, _clock.ElapsedMilliseconds);
            if (result.Ok && !result.AlreadyRunning) PushLocks();
            return (result.Ok, result.Message, NoiseCaptureJson());
        }
    }

    public (bool Ok, string Message, string Json) StopNoiseCapture(string draftId)
    {
        lock (_configGate)
        {
            var current = NoiseCapture.Snapshot();
            if (current == null || current.DraftId != draftId)
                return (false, "草稿已變更，請重新載入", NoiseCaptureJson());
            var stopped = Pipeline.StopNoiseCapture();
            if (!stopped.Ok) return (false, stopped.Message, NoiseCaptureJson());
            bool saved = NoiseCapture.FlushNow(TimeSpan.FromSeconds(5));
            PushLocks();
            string message = saved ? stopped.Message :
                "清場掃描已停止，互動已恢復；草稿磁碟保存失敗，資料仍在記憶體中，請檢查狀態訊息";
            return (true, message, NoiseCaptureJson());
        }
    }

    public (bool Ok, string Message, string Json) ApplyNoiseCapture(
        string draftId, IEnumerable<(int Face, int Col, int Row)> cells)
    {
        lock (_configGate)
        {
            var result = NoiseCapture.Apply(draftId, cells);
            if (result.Ok) PushLocks();
            return (result.Ok, result.Message, NoiseCaptureJson());
        }
    }

    public (bool Ok, string Message, string Json) UndoNoiseCapture(
        string draftId, IEnumerable<(int Face, int Col, int Row)> cells)
    {
        lock (_configGate)
        {
            var result = NoiseCapture.Undo(draftId, cells);
            if (result.Ok) PushLocks();
            return (result.Ok, result.Message, NoiseCaptureJson());
        }
    }

    public (bool Ok, string Message, string Json) ClearNoiseCapture(string draftId)
    {
        lock (_configGate)
        {
            var result = NoiseCapture.Clear(draftId);
            if (result.Ok) PushLocks();
            return (result.Ok, result.Message, NoiseCaptureJson());
        }
    }

    public (bool Ok, string Message, int Changed, string Json) SaveNoiseCapture(
        string draftId, IEnumerable<(int Face, int Col, int Row)>? requested)
    {
        lock (_configGate)
        {
            if (NoiseCapture.IsRunning)
                return (false, "掃描中不能保存鎖定", 0, NoiseCaptureJson());
            if (_configPath == null || !File.Exists(_configPath))
                return (false, "找不到設定檔，不能保存清場鎖定", 0, NoiseCaptureJson());

            var snapshot = NoiseCapture.Snapshot();
            if (snapshot == null || snapshot.DraftId != draftId)
                return (false, "草稿已變更，請重新載入", 0, NoiseCaptureJson());
            if (!snapshot.Compatible)
                return (false, "場域校準已改變，不能保存草稿鎖定", 0, NoiseCaptureJson());

            var requestedSet = requested?.ToHashSet();
            var toSave = snapshot.Cells
                .Where(c => c.LockedByDraft && !c.Saved &&
                    (requestedSet == null || requestedSet.Contains((c.Face, c.Col, c.Row))))
                .Select(c => (c.Face, c.Col, c.Row)).Distinct().ToArray();
            if (toSave.Length == 0)
                return (true, "沒有本次掃描尚未保存且仍鎖定的格子", 0, NoiseCaptureJson());

            try
            {
                var root = JsonNode.Parse(File.ReadAllText(_configPath),
                    documentOptions: new JsonDocumentOptions
                    {
                        CommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                    })!.AsObject();
                if (root["lockedCells"] is not JsonArray arr)
                    root["lockedCells"] = arr = new JsonArray();
                foreach (var (face, col, row) in toSave)
                {
                    bool exists = arr.OfType<JsonObject>().Any(o =>
                        (int?)o["face"] == face && o["cell"] is JsonArray cell && cell.Count == 2 &&
                        (int?)cell[0] == col && (int?)cell[1] == row);
                    if (!exists)
                        arr.Add(new JsonObject
                        {
                            ["face"] = face,
                            ["cell"] = new JsonArray(col, row),
                        });
                }
                string temp = _configPath + ".noise-capture.tmp";
                File.WriteAllText(temp, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                File.Move(temp, _configPath, overwrite: true);
            }
            catch (Exception ex)
            {
                return (false, $"保存清場鎖定失敗：{ex.Message}", 0, NoiseCaptureJson());
            }

            var marked = NoiseCapture.MarkSaved(draftId, toSave);
            if (!marked.Ok)
                return (false, "設定已寫入，但草稿狀態未能更新；請重新載入確認", 0, NoiseCaptureJson());
            Locks.ReclassifyAllForCapture(draftId, LockSource.Config);
            PushLocks();
            bool journalSaved = NoiseCapture.FlushNow(TimeSpan.FromSeconds(5));
            string message = journalSaved
                ? $"已正式保存 {toSave.Length} 格清場鎖定"
                : $"已寫入設定 {toSave.Length} 格；草稿保存失敗，請檢查狀態訊息";
            return (true, message, toSave.Length, NoiseCaptureJson());
        }
    }

    // ── 校正精靈（/calibration/*，全部僅 loopback；見 docs/CALIBRATION.md）──────

    static readonly JsonSerializerOptions CalibrationJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>回應 JSON 不轉義非 ASCII（中文訊息給 UI 直接顯示；純 application/json，無 HTML 嵌入疑慮）。</summary>
    static readonly JsonSerializerOptions CalibrationResponseOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>POST /calibration/start：開始（或延長）校正工作階段。raw 檢視串流啟動。</summary>
    public (bool Ok, string Json) CalibrationStart()
    {
        lock (_configGate)
        {
            if (NoiseCapture.IsRunning)
                return (false, JsonSerializer.Serialize(new { ok = false, message = "清場掃描期間不能開始校正" }, CalibrationResponseOptions));
            int timeoutSeconds = _calibration!.Start(_clock.ElapsedMilliseconds);
            return (true, JsonSerializer.Serialize(new
            {
                ok = true,
                message = $"校正工作階段開始：raw 檢視串流已啟動（{timeoutSeconds / 60} 分鐘無 stop 自動結束；服務照常發布）",
                timeoutSeconds,
            }, CalibrationResponseOptions));
        }
    }

    /// <summary>POST /calibration/stop：結束校正工作階段。</summary>
    public string CalibrationStop()
    {
        _calibration!.Stop("API stop");
        return JsonSerializer.Serialize(
            new { ok = true, message = "校正工作階段已結束（raw 檢視串流停止）" },
            CalibrationResponseOptions);
    }

    /// <summary>GET /calibration/status：{active, remainingSeconds}。</summary>
    public string CalibrationStatusJson()
    {
        var (active, remainingSeconds) = _calibration!.Status(_clock.ElapsedMilliseconds);
        return JsonSerializer.Serialize(new { active, remainingSeconds });
    }

    /// <summary>
    /// POST /calibration/save：兩角校正資料 → venue JSON 寫檔 + config.json 的
    /// venueProfile 指過去。**不做熱載**——重啟語意最乾淨，回應明確提示需重啟。
    /// 驗證全過才動檔案（名稱合法性 / cols/rows 範圍 / 兩角 raw 不可相同等，
    /// 規則在 CalibrationMath.BuildVenue）。
    /// </summary>
    public (bool Ok, string Message) SaveCalibration(string json)
    {
        CalibrationSaveRequest req;
        try
        {
            req = JsonSerializer.Deserialize<CalibrationSaveRequest>(json, CalibrationJsonOptions)
                  ?? throw new JsonException("body 必須是 JSON object");
        }
        catch (Exception ex)
        {
            return (false, $"JSON 解析失敗：{ex.Message}");
        }

        var (profile, errors) = CalibrationMath.BuildVenue(req);
        if (profile == null)
            return (false, string.Join("；", errors));

        if (_configPath == null)
            return (false, "此 host 啟動時未提供 config 路徑，無法套用校正");

        var venuesDir = Path.Combine(BaseDir, "venues");
        var fileName = profile.Venue + ".json";
        var venuePath = Path.Combine(venuesDir, fileName);
        bool overwrote = File.Exists(venuePath);
        try
        {
            Directory.CreateDirectory(venuesDir);
            File.WriteAllText(venuePath, CalibrationMath.SerializeVenueJson(profile));
        }
        catch (Exception ex)
        {
            return (false, $"venue 寫檔失敗：{ex.Message}");
        }

        // config.json 的 venueProfile 指到新檔（JsonNode 只動一個欄位，其他原樣保留）
        string relPath = "venues/" + fileName;
        string? patchError;
        lock (_configGate) patchError = PatchConfigVenueProfile(relPath);
        if (patchError != null)
            return (false, $"venue 已寫入 {relPath}，但 config.json 更新失敗：{patchError}");

        _log.Info($"[Calibration] 校正套用：寫入 {venuePath}（{profile.Faces.Count} 面、" +
                  $"畫布 {profile.Canvas.Width}×{profile.Canvas.Height}）" +
                  $"+ config venueProfile → {relPath}。重啟服務後生效。");

        return (true, $"已寫入 {relPath}（{profile.Faces.Count} 面、畫布 " +
                      $"{profile.Canvas.Width}×{profile.Canvas.Height}" +
                      (overwrote ? "，覆寫既有檔" : "") +
                      "）並更新 config.json 的 venueProfile——**重啟服務後生效**" +
                      "（托盤右鍵「結束服務」後重新啟動 TouchService.exe）");
    }

    /// <summary>config.json 的 venueProfile 指到新 venue（持 _configGate 呼叫）。null = 成功。</summary>
    string? PatchConfigVenueProfile(string relPath)
    {
        if (!File.Exists(_configPath)) return $"config 不存在：{_configPath}";
        try
        {
            var root = JsonNode.Parse(
                File.ReadAllText(_configPath!),
                documentOptions: new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                })!.AsObject();
            root["venueProfile"] = relPath;
            File.WriteAllText(_configPath!,
                root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>stdin `touches` 指令：列出當下所有活躍 touch（現場找噪訊晶片座標用）。</summary>
    public string TouchesSnapshot()
    {
        var touches = new List<ActiveTouchSnapshot>();
        Pipeline.SnapshotTouches(_clock.ElapsedMilliseconds, touches);
        return FormatTouches(touches, Venue.Faces);
    }

    // ── 觸發歷史 / 疑似噪訊排行（stdin `history` + GET /history）─────────────

    /// <summary>
    /// GET /history 的 JSON（依 score 降冪 top 筆）。
    /// v9：已鎖格不列入排行（歷史照常累積；鎖後活動判讀走 GET /locks 的
    /// droppedSinceLock / lastDropAgoMs）。locked 欄位保留給訂閱端相容（恆 false，
    /// 僅查詢與鎖定操作併發的窄窗可能短暫為 true）。
    /// </summary>
    public string HistoryJson(int top)
    {
        long nowMs = _clock.ElapsedMilliseconds;
        var entries = History.Snapshot(nowMs, top, Locks.IsLocked);
        return JsonSerializer.Serialize(new
        {
            uptimeMs = Math.Max(0, nowMs - History.StartMs),
            cells = entries.Select(e => new
            {
                face = e.Face,
                cell = new[] { e.Col, e.Row },
                score = e.Score,
                count30m = e.Count30Min,
                total = e.TotalTouches,
                isolationRatio = Math.Round(e.IsolationRatio, 3),
                occupancyRatio = Math.Round(e.OccupancyRatio, 4),
                lastTriggerAgoMs = e.LastTriggerAgoMs,
                lastLifetimeMs = e.LastLifetimeMs,
                totalActiveMs = e.TotalActiveMs,
                active = e.Active,
                locked = Locks.IsLocked(e.Face, e.Col, e.Row),
            }).ToArray(),
        });
    }

    /// <summary>stdin `history [n]` 指令：console 版疑似噪訊排行（預設 top 20；已鎖格不列入，看 locks）。</summary>
    public string HistorySnapshot(int top = 20)
    {
        long nowMs = _clock.ElapsedMilliseconds;
        var entries = History.Snapshot(nowMs, top, Locks.IsLocked);
        var rows = entries.Select(e => (e, Locks.IsLocked(e.Face, e.Col, e.Row))).ToList();
        return FormatHistory(rows, Venue.Faces, top);
    }

    /// <summary>
    /// 格式化疑似噪訊排行（靜態純函式，可測試）。
    /// score = 頻率(40) + 孤立(35) + 佔用(25)，依據見 docs/ARCHITECTURE.md §3；
    /// 分數只是排序用，不自動鎖——營運中有真人在踩，誤鎖比漏鎖傷。
    /// </summary>
    public static string FormatHistory(
        IReadOnlyList<(TriggerHistory.CellEntry Entry, bool Locked)> rows,
        IReadOnlyList<SensorFaceConfig> faces, int top)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"── 疑似噪訊排行 top {top}（score ≥60 留意、≥80 高度可疑；只排序不自動鎖）──");

        if (rows.Count == 0)
        {
            sb.AppendLine("（尚無觸發歷史——服務啟動後還沒有任何 cell 被觸發）");
        }
        else
        {
            sb.AppendLine($"{"score",5}  {"face",-20}{"cell",-11}{"30min",6}{"總次",6}  {"孤立%",5} {"佔用%",5}  {"最後觸發",-10}{"最後存活",-10}鎖定");
            foreach (var (e, locked) in rows)
            {
                string face = e.Face >= 0 && e.Face < faces.Count
                    ? $"{e.Face} {faces[e.Face].Name}"
                    : e.Face.ToString();
                string ago = e.Active ? "進行中" : $"{e.LastTriggerAgoMs / 1000.0:F1}s 前";
                sb.AppendLine(
                    $"{e.Score,5}  {face,-20}{$"({e.Col},{e.Row})",-11}" +
                    $"{e.Count30Min,6}{e.TotalTouches,6}  " +
                    $"{e.IsolationRatio * 100,5:F0} {e.OccupancyRatio * 100,5:F1}  " +
                    $"{ago,-10}{$"{e.LastLifetimeMs / 1000.0:F1}s",-10}" +
                    (locked ? "已鎖" : "-"));
            }
            sb.AppendLine("score = 30 分鐘頻率(40) + 孤立比率(35) + 活躍時長佔比(25)；歷史 runtime-only，重啟歸零。");
            sb.AppendLine("已鎖定的格子不列入排行（鎖後活動看 locks 的「鎖後封包」欄）。");
            sb.AppendLine("鎖定：lock <face> <col> <row>；確認後 savelocks 寫回 config 持久化。");
        }
        sb.Append("───────────────────────────────────────────────");
        return sb.ToString();
    }

    /// <summary>
    /// 格式化活躍 touch 清單（靜態純函式，可測試）。
    /// 存活久的排前面——噪訊晶片的特徵就是存活異常久且 cell 固定不動。
    /// </summary>
    public static string FormatTouches(List<ActiveTouchSnapshot> touches, IReadOnlyList<SensorFaceConfig> faces)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"── 活躍 touch：{touches.Count} 筆 ─────────────────────────────");

        if (touches.Count == 0)
        {
            sb.AppendLine("（目前沒有活躍 touch）");
        }
        else
        {
            touches.Sort((a, b) => b.AgeMs.CompareTo(a.AgeMs));   // 存活久的在前
            sb.AppendLine($"{"id",5}  {"face",-20}{"cell",-11}{"px",-14}存活");
            foreach (var t in touches)
            {
                string face = t.Point.FaceIndex >= 0 && t.Point.FaceIndex < faces.Count
                    ? $"{t.Point.FaceIndex} {faces[t.Point.FaceIndex].Name}"
                    : t.Point.FaceIndex.ToString();
                sb.AppendLine(
                    $"{t.Id,5}  {face,-20}" +
                    $"{$"({t.Point.Col},{t.Point.Row})",-11}" +
                    $"{$"({t.Point.PixelX},{t.Point.PixelY})",-14}" +
                    $"{t.AgeMs / 1000.0:F1}s");
            }
            sb.AppendLine("噪訊晶片特徵：存活持續累加、cell 固定不動（真人腳踩會移動且幾秒內就 up）。");
            sb.AppendLine("鎖定方法：把該筆 face 與 cell 填進 config.json 的 lockedCells 後重啟服務。");
        }

        sb.Append("───────────────────────────────────────────────");
        return sb.ToString();
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _listenSocket?.Close(); } catch { /* ignore */ }
        _receiveThread?.Join(1000);
        _timerThread?.Join(1000);
        NoiseCapture?.Dispose();
        Pipeline?.Dispose();      // 連帶 dispose 所有 sink（含 WsHub）
        _mouseDriver?.Dispose();  // owner 還按著會先 LEFTUP，不留卡死的按鍵
        _calibration?.Dispose();
        if (_logStreamer != null)
        {
            _log.EntryWritten -= _logStreamer.OnLog;
            _logStreamer.Dispose();
        }
        _log.Info("[Host] 已停止");
    }
}
