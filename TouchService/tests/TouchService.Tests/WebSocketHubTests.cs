using System.Net.WebSockets;
using System.Text;
using TouchService.Logging;
using TouchService.Output;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// 自寫 RFC6455 WebSocketHub 協議驗證：用 .NET 內建 ClientWebSocket 當對手端
/// （handshake / hello / broadcast / 長度 126 與 127 編碼路徑 / close），
/// 加 HTTP GET /venue 端點。
/// </summary>
public class WebSocketHubTests : IDisposable
{
    readonly string _tmpDir = Directory.CreateTempSubdirectory("touchservice-ws").FullName;
    readonly FileLogger _log;

    public WebSocketHubTests()
    {
        _log = new FileLogger(_tmpDir, LogLevel.Warn, console: false);
    }

    public void Dispose() => _log.Dispose();

    WebSocketHub NewHub(byte[]? hello = null)
    {
        var hub = new WebSocketHub(
            port: 0,
            venueJson: Encoding.UTF8.GetBytes("""{"venue":"test"}"""),
            helloJson: hello ?? Encoding.UTF8.GetBytes("""{"type":"hello","venue":"test"}"""),
            testPagePath: null,
            _log);
        hub.Start();
        return hub;
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
    public async Task Handshake成功_連上即收到hello()
    {
        using var hub = NewHub();
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri($"ws://127.0.0.1:{hub.Port}/"), CancellationToken.None);

        var hello = await ReceiveText(ws);
        Assert.Contains("\"type\":\"hello\"", hello);
        Assert.Equal(1, hub.ClientCount);
    }

    [Fact]
    public async Task Broadcast送達所有client_短與長payload()
    {
        using var hub = NewHub();
        using var ws1 = new ClientWebSocket();
        using var ws2 = new ClientWebSocket();
        await ws1.ConnectAsync(new Uri($"ws://127.0.0.1:{hub.Port}/"), CancellationToken.None);
        await ws2.ConnectAsync(new Uri($"ws://127.0.0.1:{hub.Port}/"), CancellationToken.None);
        await ReceiveText(ws1);
        await ReceiveText(ws2);

        // 短 frame（len < 126：1-byte 長度路徑）
        var small = """{"v":1,"seq":1,"events":[]}""";
        hub.Publish(Encoding.UTF8.GetBytes(small));
        Assert.Equal(small, await ReceiveText(ws1));
        Assert.Equal(small, await ReceiveText(ws2));

        // 中 frame（126 ≤ len < 65536：2-byte extended 長度路徑）
        var medium = "{\"pad\":\"" + new string('x', 500) + "\"}";
        hub.Publish(Encoding.UTF8.GetBytes(medium));
        Assert.Equal(medium, await ReceiveText(ws1));

        // 大 frame（len ≥ 65536：8-byte extended 長度路徑）
        var large = "{\"pad\":\"" + new string('y', 70000) + "\"}";
        hub.Publish(Encoding.UTF8.GetBytes(large));
        Assert.Equal(large, await ReceiveText(ws1));
    }

    [Fact]
    public async Task Close握手_client正常斷線_hub移除client()
    {
        using var hub = NewHub();
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri($"ws://127.0.0.1:{hub.Port}/"), CancellationToken.None);
        await ReceiveText(ws);

        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(WebSocketState.Closed, ws.State);

        // hub 端非同步移除，給它一點時間
        for (int i = 0; i < 50 && hub.ClientCount > 0; i++) await Task.Delay(20);
        Assert.Equal(0, hub.ClientCount);
    }

    [Fact]
    public async Task HttpGetVenue_回傳JSON含CORS()
    {
        using var hub = NewHub();
        using var http = new HttpClient();
        var resp = await http.GetAsync($"http://127.0.0.1:{hub.Port}/venue");

        Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("*", resp.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("""{"venue":"test"}""", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task 未知路徑_404()
    {
        using var hub = NewHub();
        using var http = new HttpClient();
        var resp = await http.GetAsync($"http://127.0.0.1:{hub.Port}/nope");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, resp.StatusCode);
    }
}
