using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TouchService.Config;
using TouchService.Logging;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// UI 終端的 log 串流：ring buffer 上限 300、{"type":"log",...} schema、
/// loopback client 連上補發 backlog、log push **僅 loopback**
/// （LAN 訂閱端不收 log type——既有 WS harness 從非 loopback IP 連入驗證）。
/// </summary>
public class LogStreamTests
{
    // ── LogStreamer 純邏輯 ───────────────────────────────────────────────────

    [Fact]
    public void RingBuffer_上限300_最舊先丟()
    {
        using var streamer = new LogStreamer();
        for (int i = 0; i < 350; i++)
            streamer.OnLog(DateTime.Now, LogLevel.Info, $"line {i}");

        var backlog = streamer.Backlog();
        Assert.Equal(LogStreamer.Capacity, backlog.Count);
        Assert.Equal(300, LogStreamer.Capacity);

        // 最舊的 50 行被擠掉：第一筆是 line 50、最後一筆 line 349
        Assert.Contains("line 50", Encoding.UTF8.GetString(backlog[0]));
        Assert.Contains("line 349", Encoding.UTF8.GetString(backlog[^1]));
    }

    [Fact]
    public void Serialize_schema欄位完整()
    {
        var bytes = LogStreamer.Serialize(1765432100123, LogLevel.Warn, "晶片 face=2 cell=(7,13) 噪訊");
        using var doc = JsonDocument.Parse(bytes);
        var root = doc.RootElement;
        Assert.Equal("log", root.GetProperty("type").GetString());
        Assert.Equal(1765432100123, root.GetProperty("ts").GetInt64());
        Assert.Equal("Warn", root.GetProperty("level").GetString());
        Assert.Equal("晶片 face=2 cell=(7,13) 噪訊", root.GetProperty("line").GetString());
    }

    // ── host 整合（真實 WS）─────────────────────────────────────────────────

    static (TouchServiceHost Host, FileLogger Log) NewHost()
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-logstream").FullName;
        var configPath = Path.Combine(dir, "config.json");
        File.WriteAllText(configPath, """{ "autoLock": { "enabled": false } }""");
        var cfg = ServiceConfig.LoadOrCreate(configPath);
        cfg.ListenPort = 0;
        cfg.VenueProfile = Path.Combine(AppContext.BaseDirectory, "venues", "704.json");
        cfg.UdpOutput = new UdpOutputConfig { Enabled = false };
        cfg.WebSocketOutput = new WebSocketOutputConfig { Enabled = true, Port = 0 };
        cfg.Log = new LogConfig { Console = false, Dir = dir, Level = "Warn" };   // Warn 起跳：測試可控
        var log = new FileLogger(dir, LogLevel.Warn, console: false);
        var host = new TouchServiceHost(cfg, AppContext.BaseDirectory, log, configPath);
        host.Start();
        return (host, log);
    }

    static async Task<string> ReceiveText(ClientWebSocket ws)
    {
        var buf = new byte[1 << 20];
        int off = 0;
        while (true)
        {
            var r = await ws.ReceiveAsync(buf.AsMemory(off), CancellationToken.None)
                .AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            off += r.Count;
            if (r.EndOfMessage) return Encoding.UTF8.GetString(buf, 0, off);
        }
    }

    [Fact]
    public async Task loopback連上_先收hello再補發backlog_之後即時推log()
    {
        var (host, log) = NewHost();
        using (host)
        using (log)
        {
            log.Warn("backlog-line-1");
            log.Warn("backlog-line-2");
            await Task.Delay(50);   // streamer enqueue 是同步的，ring 立即有；保險等一拍

            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri($"ws://127.0.0.1:{host.WsHub!.Port}/"), CancellationToken.None);

            var hello = await ReceiveText(ws);
            Assert.Contains("\"type\":\"hello\"", hello);

            // backlog：連上即補發（順序保留）
            var b1 = await ReceiveText(ws);
            Assert.Contains("\"type\":\"log\"", b1);
            Assert.Contains("backlog-line-1", b1);
            var b2 = await ReceiveText(ws);
            Assert.Contains("backlog-line-2", b2);

            // 即時推送
            log.Warn("live-line");
            var live = await ReceiveText(ws);
            Assert.Contains("\"type\":\"log\"", live);
            Assert.Contains("live-line", live);
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
    public async Task 非loopback_client不收log_type_frame照常收()
    {
        var lanIp = FindNonLoopbackIPv4();
        if (lanIp == null) return;   // 測試機沒有非 loopback IPv4：跳過（場館機必有）

        var (host, log) = NewHost();
        using (host)
        using (log)
        {
            log.Warn("secret-before-connect");   // 進 ring，但 LAN client 不該收到 backlog

            using var lan = new ClientWebSocket();
            await lan.ConnectAsync(new Uri($"ws://{lanIp}:{host.WsHub!.Port}/"), CancellationToken.None);
            var hello = await ReceiveText(lan);
            Assert.Contains("\"type\":\"hello\"", hello);

            // 觸發一筆 log + 一筆全 client 廣播：LAN client 收到的下一包必須是廣播、不是 log
            log.Warn("secret-live");
            await Task.Delay(100);   // 給 push 執行緒時間（若有誤推，這包會先到）
            host.WsHub!.Publish(Encoding.UTF8.GetBytes("""{"v":1,"seq":99,"events":[]}"""));

            var next = await ReceiveText(lan);
            Assert.DoesNotContain("\"type\":\"log\"", next);
            Assert.DoesNotContain("secret", next);
            Assert.Contains("\"seq\":99", next);
        }
    }

    [Fact]
    public async Task loopback與非loopback並存_log只到loopback()
    {
        var lanIp = FindNonLoopbackIPv4();
        if (lanIp == null) return;

        var (host, log) = NewHost();
        using (host)
        using (log)
        {
            using var local = new ClientWebSocket();
            await local.ConnectAsync(new Uri($"ws://127.0.0.1:{host.WsHub!.Port}/"), CancellationToken.None);
            await ReceiveText(local);   // hello（ring 此時為空，無 backlog）

            using var lan = new ClientWebSocket();
            await lan.ConnectAsync(new Uri($"ws://{lanIp}:{host.WsHub!.Port}/"), CancellationToken.None);
            await ReceiveText(lan);     // hello

            log.Warn("only-for-loopback");

            // loopback 收到 log
            var msg = await ReceiveText(local);
            Assert.Contains("\"type\":\"log\"", msg);
            Assert.Contains("only-for-loopback", msg);

            // LAN：下一包是廣播 frame，不是 log
            await Task.Delay(100);
            host.WsHub!.Publish(Encoding.UTF8.GetBytes("""{"v":1,"seq":123,"events":[]}"""));
            var lanNext = await ReceiveText(lan);
            Assert.Contains("\"seq\":123", lanNext);
            Assert.DoesNotContain("only-for-loopback", lanNext);
        }
    }
}
