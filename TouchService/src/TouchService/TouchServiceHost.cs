using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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

        Pipeline = new TouchPipeline(router, tracker, serializer, sinks, Stats, _log, _config.KeepaliveIntervalMs);

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
