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

    Socket? _listenSocket;
    Thread? _receiveThread;
    Thread? _timerThread;

    public VenueProfile Venue { get; private set; } = null!;
    public TouchPipeline Pipeline { get; private set; } = null!;
    public StatsCollector Stats { get; } = new();
    public WebSocketHub? WsHub { get; private set; }

    /// <summary>實際監聽的 UDP port（config 給 0 時由 OS 配發，測試用）。</summary>
    public int ActualListenPort { get; private set; }

    public TouchServiceHost(ServiceConfig config, string baseDir, FileLogger log)
    {
        _config = config;
        _log = log;
        BaseDir = baseDir;
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
        var lockedCells = BuildLockedCellFilter();

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
                BuildHelloJson(),
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
            router, tracker, serializer, sinks, Stats, _log, _config.KeepaliveIntervalMs, lockedCells);

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

    /// <summary>WS 連上時下發的 hello：venue 輪廓（含畫布像素幾何，testpage 直接畫）。</summary>
    byte[] BuildHelloJson()
    {
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
        };
        return JsonSerializer.SerializeToUtf8Bytes(hello);
    }

    string? FindTestPage()
    {
        var p = Path.Combine(BaseDir, "testpage", "index.html");
        return File.Exists(p) ? p : null;
    }

    public string StatsSnapshot() =>
        Stats.Snapshot(Pipeline?.ActiveTouches ?? 0, WsHub?.ClientCount ?? 0);

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

        var list = string.Join("、", filter.Entries.Select(e => $"face={e.Face} cell=({e.Cell![0]},{e.Cell[1]})"));
        _log.Info($"[Host] 鎖定晶片 {filter.Count} 顆（命中點進 tracker 前丟棄）：{list}");

        // 防呆：填錯座標（超出場域範圍）永遠不會命中，啟動時就提醒
        foreach (var e in filter.Entries)
        {
            if (e.Face >= Venue.Faces.Count)
                _log.Warn($"[Host] lockedCells face={e.Face} 超出場域面數（共 {Venue.Faces.Count} 面）——此項永遠不會命中");
            else if (e.Cell![0] >= Venue.Faces[e.Face].ColCount || e.Cell[1] >= Venue.Faces[e.Face].RowCount)
                _log.Warn($"[Host] lockedCells face={e.Face} cell=({e.Cell[0]},{e.Cell[1]}) 超出該面格數" +
                          $"（{Venue.Faces[e.Face].ColCount}×{Venue.Faces[e.Face].RowCount}）——此項永遠不會命中");
        }
        return filter;
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
