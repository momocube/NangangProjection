using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TouchService.Config;
using TouchService.Core;
using TouchService.Diagnostics;
using TouchService.Logging;
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

    Socket? _listenSocket;
    Thread? _receiveThread;
    Thread? _timerThread;

    public VenueProfile Venue { get; private set; } = null!;
    public TouchPipeline Pipeline { get; private set; } = null!;
    public StatsCollector Stats { get; } = new();
    public WebSocketHub? WsHub { get; private set; }

    /// <summary>runtime 鎖定黑名單（config + auto + manual 共用同一個過濾器）。</summary>
    public LockedCellFilter Locks { get; private set; } = LockedCellFilter.Empty;

    /// <summary>實際監聽的 UDP port（config 給 0 時由 OS 配發，測試用）。</summary>
    public int ActualListenPort { get; private set; }

    /// <summary>configPath：savelocks 寫回的目標（null = 無法寫回，例如純測試 host）。</summary>
    public TouchServiceHost(ServiceConfig config, string baseDir, FileLogger log, string? configPath = null)
    {
        _config = config;
        _log = log;
        BaseDir = baseDir;
        _configPath = configPath;
    }

    public string BaseDir { get; }

    public void Start()
    {
        // 1. 場域 profile
        var venuePath = Path.IsPathRooted(_config.VenueProfile)
            ? _config.VenueProfile
            : Path.Combine(BaseDir, _config.VenueProfile);
        Venue = VenueProfile.Load(venuePath);
        _log.Info($"[Host] 場域 profile：{Venue.Venue}（{Venue.Canvas.Width}×{Venue.Canvas.Height}，{Venue.Faces.Count} 面）← {venuePath}");

        // 2. 管線元件
        var router = new SensorFaceRouter(Venue);
        var tracker = new TouchTracker(
            _config.MaxSlots, _config.MatchRadiusPx, _config.ReleaseTimeoutMs, _config.MoveMinDistancePx);
        var serializer = new FrameSerializer(Venue);
        Locks = BuildLockedCellFilter();

        // 啟動自檢窗（autoLock）：窗內被觸發 ≥ minPackets 的格子自動鎖（依據：開機時場上理應無人）
        AutoLockSelfCheck? autoLock = null;
        if (_config.AutoLock.Enabled && _config.AutoLock.WindowSeconds > 0)
        {
            autoLock = new AutoLockSelfCheck(
                _clock.ElapsedMilliseconds, _config.AutoLock.WindowSeconds, _config.AutoLock.MinPackets);
            _log.Info($"[AutoLock] 自檢窗開始：{_config.AutoLock.WindowSeconds}s 內被觸發 ≥{autoLock.MinPackets} 包的格子" +
                      "將自動鎖定（此期間不發布觸控事件；keepalive 照發）");
        }
        else
        {
            _log.Info("[AutoLock] 自檢停用（autoLock.enabled=false 或 windowSeconds ≤ 0），啟動即正常發布");
        }

        var sinks = new List<ITouchSink>();
        if (_config.UdpOutput.Enabled && _config.UdpOutput.Endpoints.Count > 0)
        {
            var udp = new UdpJsonPublisher(_config.UdpOutput.Endpoints, _log);
            sinks.Add(udp);
            _log.Info($"[Host] raw UDP JSON 輸出 → {string.Join(", ", _config.UdpOutput.Endpoints)}");
        }
        if (_config.WebSocketOutput.Enabled)
        {
            WsHub = new WebSocketHub(
                _config.WebSocketOutput.Port,
                BuildVenueJson(),
                BuildHelloJson,   // 每個 client 連上時取當下鎖定清單 / 自檢狀態
                FindTestPage(),
                _log);
            WsHub.Start();
            sinks.Add(WsHub);
        }
        if (_config.TuioOutput.Enabled)
        {
            // v1 規格預留插槽：enabled = true 會在這裡直接 throw NotImplementedException
            sinks.Add(new TuioPublisher(_config.TuioOutput.Endpoint, _log));
        }

        Pipeline = new TouchPipeline(
            router, tracker, serializer, sinks, Stats, _log, _config.KeepaliveIntervalMs, Locks, autoLock);
        Pipeline.AutoLockCompleted += _ => PushLocks();   // 自檢結束 → 通知所有 WS client 最新鎖定清單

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
            try
            {
                len = _listenSocket!.ReceiveFrom(buf, ref remote);
                srcPort = ((IPEndPoint)remote).Port;
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
                Pipeline.OnDatagram(buf.AsSpan(0, len), _clock.ElapsedMilliseconds, srcPort);
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

    /// <summary>鎖定清單的對外 payload（hello 與 locks push 共用格式）。</summary>
    object LockedCellsPayload() =>
        Locks.Snapshot().Select(e => new
        {
            face = e.Face,
            cell = new[] { e.Col, e.Row },
            source = LockedCellFilter.SourceName(e.Source),
            packets = e.Packets,
        }).ToArray();

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
            sb.AppendLine($"{"face",-22}{"cell",-11}{"來源",-8}封包數(auto)");
            foreach (var e in entries)
            {
                string face = e.Face >= 0 && e.Face < Venue.Faces.Count
                    ? $"{e.Face} {Venue.Faces[e.Face].Name}"
                    : e.Face.ToString();
                sb.AppendLine(
                    $"{face,-22}{$"({e.Col},{e.Row})",-11}{LockedCellFilter.SourceName(e.Source),-8}" +
                    (e.Source == LockSource.Auto ? e.Packets.ToString() : "-"));
            }
            sb.AppendLine("解鎖：unlock <face> <col> <row>；寫回 config：savelocks（auto+manual+config 合併）。");
        }
        sb.Append("───────────────────────────────────────────────");
        return sb.ToString();
    }

    /// <summary>stdin `lock` 指令：手動加鎖（runtime，來源 manual）。</summary>
    public string LockCell(int face, int col, int row)
    {
        if (face < 0 || face >= Venue.Faces.Count)
            return $"face={face} 超出場域面數（0~{Venue.Faces.Count - 1}），未加鎖";
        var f = Venue.Faces[face];
        if (col < 0 || col >= f.ColCount || row < 0 || row >= f.RowCount)
            return $"cell=({col},{row}) 超出 face {face}（{f.Name}）格數 {f.ColCount}×{f.RowCount}，未加鎖";

        if (!Locks.TryAdd(face, col, row, LockSource.Manual))
        {
            var existed = Locks.Find(face, col, row);
            return $"face={face} cell=({col},{row}) 已鎖定（來源 {LockedCellFilter.SourceName(existed!.Source)}），未重複加鎖";
        }
        _log.Info($"[Host] 手動鎖定 face={face} cell=({col},{row})（manual，目前共 {Locks.Count} 顆）");
        PushLocks();
        return $"已鎖定 face={face} cell=({col},{row})（manual）。寫回 config 用 savelocks。";
    }

    /// <summary>stdin `unlock` 指令：解鎖（任何來源都可解；config 來源只影響 runtime，不改檔）。</summary>
    public string UnlockCell(int face, int col, int row)
    {
        if (!Locks.TryRemove(face, col, row, out var removed))
            return $"face={face} cell=({col},{row}) 本來就沒有鎖定";

        _log.Info($"[Host] 解鎖 face={face} cell=({col},{row})（原來源 {LockedCellFilter.SourceName(removed!.Source)}，剩 {Locks.Count} 顆）");
        PushLocks();
        var note = removed.Source == LockSource.Config
            ? "（config 來源：只影響本次執行，config.json 未修改——重啟會再鎖回；要永久解除請刪 config 條目或 savelocks）"
            : "";
        return $"已解鎖 face={face} cell=({col},{row})（原來源 {LockedCellFilter.SourceName(removed.Source)}）{note}";
    }

    /// <summary>
    /// stdin `savelocks` 指令：把目前 runtime 全部鎖定（config+auto+manual 合併去重）
    /// 寫回 config.json 的 lockedCells。用 JsonNode 只換 lockedCells 一個欄位，
    /// 其他欄位（含未知欄位）原樣保留，縮排維持 2 空格。
    /// </summary>
    public string SaveLocks()
    {
        if (_configPath == null)
            return "此 host 啟動時未提供 config 路徑，無法寫回";
        if (!File.Exists(_configPath))
            return $"config 不存在：{_configPath}，未寫回";

        var entries = Locks.Snapshot();
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

        _log.Info($"[Host] savelocks：寫回 {entries.Count} 顆鎖定到 {_configPath}");
        return $"已寫回 {entries.Count} 顆鎖定到 {_configPath} 的 lockedCells（下次啟動以 config 來源載入）";
    }

    /// <summary>stdin `touches` 指令：列出當下所有活躍 touch（現場找噪訊晶片座標用）。</summary>
    public string TouchesSnapshot()
    {
        var touches = new List<ActiveTouchSnapshot>();
        Pipeline.SnapshotTouches(_clock.ElapsedMilliseconds, touches);
        return FormatTouches(touches, Venue.Faces);
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
        Pipeline?.Dispose();   // 連帶 dispose 所有 sink（含 WsHub）
        _log.Info("[Host] 已停止");
    }
}
