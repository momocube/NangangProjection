using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TouchService.Config;
using TouchService.Logging;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// CommandProcessor：stdin 與 POST /command 走同一個處理器、輸出文字一致；
/// /command 僅 loopback（403）；壞 body 400；quit 觸發優雅關閉回呼。
/// </summary>
public class CommandProcessorTests
{
    static (TouchServiceHost Host, FileLogger Log) NewHost()
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-cmd").FullName;
        var configPath = Path.Combine(dir, "config.json");
        File.WriteAllText(configPath, """{ "autoLock": { "enabled": false } }""");
        var cfg = ServiceConfig.LoadOrCreate(configPath);
        cfg.ListenPort = 0;
        cfg.VenueProfile = Path.Combine(AppContext.BaseDirectory, "venues", "704.json");
        cfg.UdpOutput = new UdpOutputConfig { Enabled = false };
        cfg.WebSocketOutput = new WebSocketOutputConfig { Enabled = true, Port = 0 };
        cfg.Log = new LogConfig { Console = false, Dir = dir, Level = "Warn" };
        var log = new FileLogger(dir, LogLevel.Warn, console: false);
        var host = new TouchServiceHost(cfg, AppContext.BaseDirectory, log, configPath);
        host.Start();
        return (host, log);
    }

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

    static async Task<(HttpStatusCode Status, string Body)> PostCommand(
        TouchServiceHost host, string json, string ip = "127.0.0.1")
    {
        var resp = await Http.PostAsync($"http://{ip}:{host.WsHub!.Port}/command",
            new StringContent(json, Encoding.UTF8, "application/json"));
        return (resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("stats")]
    [InlineData("touches")]
    [InlineData("history")]
    [InlineData("history 5")]
    [InlineData("locks")]
    [InlineData("unlock 0 1 2")]   // 沒鎖過：冪等訊息「本來就沒有鎖定」
    [InlineData("badcommand")]
    [InlineData("history zero")]
    [InlineData("lock 1")]         // 參數不足：用法說明
    public async Task stdin與API同指令同輸出(string cmd)
    {
        var (host, log) = NewHost();
        using (host)
        using (log)
        {
            // stats 含時間相關數值（pkt rate 等），先取 API 再取 stdin 可能有微小時間差——
            // 對非 stats 指令做全等比對；stats 只比前綴行（結構一致）
            var (status, body) = await PostCommand(host, JsonSerializer.Serialize(new { cmd }));
            Assert.Equal(HttpStatusCode.OK, status);
            using var doc = JsonDocument.Parse(body);
            var apiOutput = doc.RootElement.GetProperty("output").GetString();
            Assert.NotNull(apiOutput);

            var stdinOutput = host.Commands.Execute(cmd);
            if (cmd == "stats")
                Assert.Equal(FirstLine(stdinOutput), FirstLine(apiOutput!));
            else
                Assert.Equal(stdinOutput, apiOutput);
        }

        static string FirstLine(string s) => s.Split('\n')[0];
    }

    [Fact]
    public void 未知指令與用法錯誤_輸出說明文字()
    {
        var (host, log) = NewHost();
        using (host)
        using (log)
        {
            Assert.Contains("未知指令", host.Commands.Execute("frobnicate"));
            Assert.Contains("用法：history", host.Commands.Execute("history -3"));
            Assert.Contains("用法：lock", host.Commands.Execute("lock 1 2"));
            Assert.Contains("用法：unlock", host.Commands.Execute("unlock x y z"));
            Assert.Equal("", host.Commands.Execute(""));
            Assert.Equal("", host.Commands.Execute("   "));
        }
    }

    [Fact]
    public void quit指令_觸發ShutdownRequested()
    {
        var (host, log) = NewHost();
        using (host)
        using (log)
        {
            bool requested = false;
            host.ShutdownRequested += () => requested = true;

            var output = host.Commands.Execute("quit");
            Assert.True(requested);
            Assert.Contains("關閉", output);
        }
    }

    [Fact]
    public async Task PostCommand_壞body_400()
    {
        var (host, log) = NewHost();
        using (host)
        using (log)
        {
            var (status, body) = await PostCommand(host, """{ "nope": 1 }""");
            Assert.Equal(HttpStatusCode.BadRequest, status);
            Assert.Contains("cmd", body);

            (status, _) = await PostCommand(host, "not json");
            Assert.Equal(HttpStatusCode.BadRequest, status);
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
    public async Task PostCommand_非loopback來源403()
    {
        var lanIp = FindNonLoopbackIPv4();
        if (lanIp == null) return;   // 測試機沒有非 loopback IPv4：跳過（場館機必有）

        var (host, log) = NewHost();
        using (host)
        using (log)
        {
            var (status, _) = await PostCommand(host, """{ "cmd": "stats" }""", lanIp.ToString());
            Assert.Equal(HttpStatusCode.Forbidden, status);
        }
    }
}
