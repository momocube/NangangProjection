using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TouchService.Config;
using TouchService.Core;
using TouchService.Logging;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// 校正 API（/calibration/*，全部僅 loopback）+ raw 檢視串流：
/// start/stop/status roundtrip、gating（非校正階段不推 raw）、raw 訊息 schema、
/// raw 僅 loopback、save 寫檔 roundtrip + config venueProfile patch + 驗證 400、
/// 非 loopback 403。
/// </summary>
public class CalibrationApiTests
{
    static (TouchServiceHost Host, string Dir, string ConfigPath, FileLogger Log) NewHost(
        string configJson = """{ "autoLock": { "enabled": false } }""")
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-calib").FullName;
        var configPath = Path.Combine(dir, "config.json");
        File.WriteAllText(configPath, configJson);

        // venue 放進 BaseDir（save 會寫 BaseDir/venues/<名稱>.json，不污染測試輸出目錄）
        Directory.CreateDirectory(Path.Combine(dir, "venues"));
        File.Copy(Path.Combine(AppContext.BaseDirectory, "venues", "704.json"),
                  Path.Combine(dir, "venues", "704.json"));

        var cfg = ServiceConfig.LoadOrCreate(configPath);
        cfg.ListenPort = 0;
        cfg.VenueProfile = "venues/704.json";
        cfg.UdpOutput = new UdpOutputConfig { Enabled = false };
        cfg.WebSocketOutput = new WebSocketOutputConfig { Enabled = true, Port = 0 };
        cfg.Log = new LogConfig { Console = false, Dir = dir, Level = "Warn" };
        var log = new FileLogger(dir, LogLevel.Warn, console: false);
        var host = new TouchServiceHost(cfg, dir, log, configPath);
        host.Start();
        return (host, dir, configPath, log);
    }

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

    static async Task<(HttpStatusCode Status, string Body)> Post(TouchServiceHost host, string path,
                                                                 string json = "", string ip = "127.0.0.1")
    {
        var resp = await Http.PostAsync($"http://{ip}:{host.WsHub!.Port}{path}",
            new StringContent(json, Encoding.UTF8, "application/json"));
        return (resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    static async Task<(HttpStatusCode Status, string Body)> Get(TouchServiceHost host, string path)
    {
        var resp = await Http.GetAsync($"http://127.0.0.1:{host.WsHub!.Port}{path}");
        return (resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    /// <summary>背景持續收 WS 訊息進 queue（避免「期望沒有訊息」的 receive 殘留問題）。</summary>
    static Task PumpMessages(ClientWebSocket ws, ConcurrentQueue<string> sink) =>
        Task.Run(async () =>
        {
            var buf = new byte[1 << 20];
            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    int off = 0;
                    WebSocketReceiveResult r;
                    do
                    {
                        r = await ws.ReceiveAsync(new ArraySegment<byte>(buf, off, buf.Length - off),
                                                  CancellationToken.None);
                        off += r.Count;
                    } while (!r.EndOfMessage);
                    if (r.MessageType == WebSocketMessageType.Close) return;
                    sink.Enqueue(Encoding.UTF8.GetString(buf, 0, off));
                }
            }
            catch { /* 斷線（host dispose）：結束 */ }
        });

    static void SendRaw(int port, int x, int y, UdpClient sock)
    {
        var pkt = SensorPacketParser.Build(0, new[] { new RawPoint(x, y) });
        sock.Send(pkt, pkt.Length, new IPEndPoint(IPAddress.Loopback, port));
    }

    static async Task<List<JsonDocument>> WaitForRawMessages(
        ConcurrentQueue<string> sink, int atLeast, int timeoutMs = 3000)
    {
        var result = new List<JsonDocument>();
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            while (sink.TryDequeue(out var msg))
            {
                var doc = JsonDocument.Parse(msg);
                if (doc.RootElement.TryGetProperty("type", out var t) && t.GetString() == "raw")
                    result.Add(doc);
                else doc.Dispose();
            }
            if (result.Count >= atLeast) return result;
            await Task.Delay(20);
        }
        return result;
    }

    // ── 工作階段 start / stop / status ───────────────────────────────────────

    [Fact]
    public async Task StartStopStatus_roundtrip()
    {
        var (host, _, _, log) = NewHost();
        using (host)
        using (log)
        {
            var (s0, b0) = await Get(host, "/calibration/status");
            Assert.Equal(HttpStatusCode.OK, s0);
            Assert.Contains("\"active\":false", b0);

            var (s1, b1) = await Post(host, "/calibration/start");
            Assert.Equal(HttpStatusCode.OK, s1);
            using var doc = JsonDocument.Parse(b1);
            Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
            Assert.Equal(15 * 60, doc.RootElement.GetProperty("timeoutSeconds").GetInt32());

            var (_, b2) = await Get(host, "/calibration/status");
            Assert.Contains("\"active\":true", b2);

            var (s3, b3) = await Post(host, "/calibration/stop");
            Assert.Equal(HttpStatusCode.OK, s3);
            Assert.Contains("\"ok\":true", b3);

            var (_, b4) = await Get(host, "/calibration/status");
            Assert.Contains("\"active\":false", b4);
        }
    }

    // ── raw 串流：gating + schema ────────────────────────────────────────────

    [Fact]
    public async Task Raw串流_非校正階段不推_校正階段推_stop後停()
    {
        var (host, _, _, log) = NewHost();
        using (host)
        using (log)
        {
            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri($"ws://127.0.0.1:{host.WsHub!.Port}/"), CancellationToken.None);
            var sink = new ConcurrentQueue<string>();
            _ = PumpMessages(ws, sink);

            using var sock = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));

            // 未 start：送包不推 raw（raw 5000,300 在 704 場域是 unrouted，不會產生 frame 事件）
            SendRaw(host.ActualListenPort, 5000, 300, sock);
            SendRaw(host.ActualListenPort, 5000, 300, sock);
            var none = await WaitForRawMessages(sink, atLeast: 1, timeoutMs: 400);
            Assert.Empty(none);

            // start：同一座標重送 4 包 → 收到 raw 且 n 遞增到 ≥3（穩定）
            await Post(host, "/calibration/start");
            for (int i = 0; i < 4; i++)
            {
                SendRaw(host.ActualListenPort, 5000, 300, sock);
                await Task.Delay(30);
            }
            var raws = await WaitForRawMessages(sink, atLeast: 3);
            Assert.True(raws.Count >= 3, $"預期 ≥3 包 raw，收到 {raws.Count}");
            foreach (var doc in raws)
            {
                var root = doc.RootElement;
                Assert.Equal(5000, root.GetProperty("x").GetInt32());
                Assert.Equal(300, root.GetProperty("y").GetInt32());
                Assert.Equal("127.0.0.1", root.GetProperty("src").GetString());
                Assert.Equal(JsonValueKind.Null, root.GetProperty("face").ValueKind);   // unrouted → null
            }
            Assert.Contains(raws, d => d.RootElement.GetProperty("n").GetInt32() >= 3);
            raws.ForEach(d => d.Dispose());

            // 路由得到的點 → face 帶 index（704 face 2 Floor 中央）
            SendRaw(host.ActualListenPort, 3264, 1664, sock);
            var routed = await WaitForRawMessages(sink, atLeast: 1);
            Assert.NotEmpty(routed);
            Assert.Equal(2, routed[0].RootElement.GetProperty("face").GetInt32());
            routed.ForEach(d => d.Dispose());

            // stop：再送不推
            await Post(host, "/calibration/stop");
            await Task.Delay(50);
            while (sink.TryDequeue(out _)) { }
            SendRaw(host.ActualListenPort, 5000, 300, sock);
            var afterStop = await WaitForRawMessages(sink, atLeast: 1, timeoutMs: 400);
            Assert.Empty(afterStop);
        }
    }

    static IPAddress? FindNonLoopbackIPv4()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up ||
                ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ua.Address))
                    return ua.Address;
        }
        return null;
    }

    [Fact]
    public async Task Raw串流_僅loopback_LAN訂閱端收不到()
    {
        var lanIp = FindNonLoopbackIPv4();
        if (lanIp == null) return;   // 測試機沒有非 loopback IPv4：跳過（場館機必有）

        var (host, _, _, log) = NewHost();
        using (host)
        using (log)
        {
            using var local = new ClientWebSocket();
            await local.ConnectAsync(new Uri($"ws://127.0.0.1:{host.WsHub!.Port}/"), CancellationToken.None);
            var localSink = new ConcurrentQueue<string>();
            _ = PumpMessages(local, localSink);

            using var lan = new ClientWebSocket();
            await lan.ConnectAsync(new Uri($"ws://{lanIp}:{host.WsHub!.Port}/"), CancellationToken.None);
            var lanSink = new ConcurrentQueue<string>();
            _ = PumpMessages(lan, lanSink);

            await Post(host, "/calibration/start");
            using var sock = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            SendRaw(host.ActualListenPort, 5000, 300, sock);

            var localRaw = await WaitForRawMessages(localSink, atLeast: 1);
            Assert.NotEmpty(localRaw);
            localRaw.ForEach(d => d.Dispose());

            await Task.Delay(200);
            var lanRaw = await WaitForRawMessages(lanSink, atLeast: 1, timeoutMs: 100);
            Assert.Empty(lanRaw);
        }
    }

    [Fact]
    public async Task 非loopback_全部403()
    {
        var lanIp = FindNonLoopbackIPv4();
        if (lanIp == null) return;

        var (host, _, _, log) = NewHost();
        using (host)
        using (log)
        {
            foreach (var path in new[] { "/calibration/start", "/calibration/stop", "/calibration/save" })
            {
                var (status, _) = await Post(host, path, "{}", ip: lanIp.ToString());
                Assert.Equal(HttpStatusCode.Forbidden, status);
            }
            var resp = await Http.GetAsync($"http://{lanIp}:{host.WsHub!.Port}/calibration/status");
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }
    }

    // ── save：寫檔 roundtrip + config patch + 驗證 ──────────────────────────

    const string SaveBody = """
        {
          "venue": "e2e-demo",
          "faces": [
            { "name": "Wall", "cols": 4, "rows": 2, "orientation": "vertical",
              "originX": 0, "originY": 0,
              "topLeftRaw": [1300, 500], "bottomRightRaw": [1000, 530] }
          ]
        }
        """;

    [Fact]
    public async Task Save_寫檔roundtrip_config更新且其他欄位保留()
    {
        var (host, dir, configPath, log) = NewHost("""
            { "autoLock": { "enabled": false }, "customFutureField": { "keep": true } }
            """);
        using (host)
        using (log)
        {
            var (status, body) = await Post(host, "/calibration/save", SaveBody);
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Contains("\"ok\":true", body);
            Assert.Contains("重啟", body);

            // venue 檔案寫出且可被 VenueProfile.Load 載入（兩角 → flip 正確）
            var venuePath = Path.Combine(dir, "venues", "e2e-demo.json");
            Assert.True(File.Exists(venuePath));
            var loaded = VenueProfile.Load(venuePath);
            Assert.Equal("e2e-demo", loaded.Venue);
            Assert.Equal(128, loaded.Canvas.Width);
            Assert.Equal(32, loaded.Canvas.Height);
            var f = loaded.Faces.Single();
            Assert.Equal((1000, 1300, 500, 530), (f.RawMinX, f.RawMaxX, f.RawMinY, f.RawMaxY));
            Assert.True(f.FlipX);    // topLeftRaw.x > bottomRightRaw.x → 感應方向反向
            Assert.False(f.FlipY);
            Assert.Equal(ChipOrientation.Vertical, f.Orientation);

            // config.json：venueProfile 指到新檔、其他欄位（含未知欄位）原樣保留
            using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
            Assert.Equal("venues/e2e-demo.json", doc.RootElement.GetProperty("venueProfile").GetString());
            Assert.True(doc.RootElement.GetProperty("customFutureField").GetProperty("keep").GetBoolean());
        }
    }

    [Theory]
    [InlineData("""{ "venue": "a/b", "faces": [ { "name": "W", "cols": 4, "rows": 2, "orientation": "vertical", "topLeftRaw": [1000, 500], "bottomRightRaw": [1300, 530] } ] }""",
                "名稱")]                       // 名稱非法
    [InlineData("""{ "venue": "ok", "faces": [ { "name": "W", "cols": 0, "rows": 2, "orientation": "vertical", "topLeftRaw": [1000, 500], "bottomRightRaw": [1300, 530] } ] }""",
                "cols")]                       // cols 超範圍
    [InlineData("""{ "venue": "ok", "faces": [ { "name": "W", "cols": 4, "rows": 2, "orientation": "vertical", "topLeftRaw": [1000, 500], "bottomRightRaw": [1000, 530] } ] }""",
                "rawMinX")]                    // 兩角 rawX 相同
    [InlineData("""{ "venue": "ok", "faces": [] }""", "至少")]   // 沒有面
    [InlineData("not json", "解析失敗")]
    public async Task Save_驗證錯誤_400且不動檔案(string body, string expectInMessage)
    {
        var (host, dir, configPath, log) = NewHost();
        using (host)
        using (log)
        {
            var before = File.ReadAllText(configPath);
            var (status, respBody) = await Post(host, "/calibration/save", body);
            Assert.Equal(HttpStatusCode.BadRequest, status);
            Assert.Contains("\"ok\":false", respBody);
            Assert.Contains(expectInMessage, respBody);

            Assert.Equal(before, File.ReadAllText(configPath));               // config 沒被動
            Assert.False(File.Exists(Path.Combine(dir, "venues", "ok.json")));   // 沒寫出半套 venue
        }
    }
}
