using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TouchService.Config;
using TouchService.Core;
using TouchService.Logging;
using TouchService.Tracking;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// 觸發歷史對外介面：GET /history（排序 / top / 欄位、唯讀不限 loopback；
/// v9 起已鎖格退出排行）、GET /locks（鎖定清單含鎖後活動）、
/// POST /locks/lock（語意同 stdin lock、loopback only）、stdin `history` 的排行格式。
/// </summary>
public class HistoryApiTests
{
    static (TouchServiceHost Host, string ConfigPath, FileLogger Log) NewHost(string configJson)
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-history-api").FullName;
        var configPath = Path.Combine(dir, "config.json");
        File.WriteAllText(configPath, configJson);
        var cfg = ServiceConfig.LoadOrCreate(configPath);
        cfg.ListenPort = 0;
        cfg.VenueProfile = Path.Combine(AppContext.BaseDirectory, "venues", "704.json");
        cfg.UdpOutput = new UdpOutputConfig { Enabled = false };
        cfg.WebSocketOutput = new WebSocketOutputConfig { Enabled = true, Port = 0 };
        cfg.Log = new LogConfig { Console = false, Dir = dir, Level = "Warn" };
        var log = new FileLogger(dir, LogLevel.Warn, console: false);
        var host = new TouchServiceHost(cfg, AppContext.BaseDirectory, log, configPath);
        host.Start();
        return (host, configPath, log);
    }

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

    static async Task<(HttpStatusCode Status, string Body)> Get(TouchServiceHost host, string path)
    {
        var resp = await Http.GetAsync($"http://127.0.0.1:{host.WsHub!.Port}{path}");
        return (resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    static async Task<(HttpStatusCode Status, string Body)> Post(TouchServiceHost host, string path, string json)
    {
        var resp = await Http.PostAsync($"http://127.0.0.1:{host.WsHub!.Port}{path}",
            new StringContent(json, Encoding.UTF8, "application/json"));
        return (resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    static RoutedPoint Pt(int face, int col, int row, int px, int py) => new()
    {
        FaceIndex = face, Col = col, Row = row, PixelX = px, PixelY = py, NormX = 0, NormY = 0,
    };

    static TouchEventData Ev(int id, TouchPhase phase, RoutedPoint p) =>
        new() { Id = id, Phase = phase, Point = p };

    /// <summary>灌歷史：高分 cell（狂叫孤立）+ 低分 cell（一次短觸發）。</summary>
    static void Seed(TouchServiceHost host)
    {
        var noisy = Pt(2, 7, 13, 869, 850);
        for (int k = 0; k < 40; k++)
        {
            host.History.OnEvents(new[] { Ev(1000 + k, TouchPhase.Down, noisy) }, k * 100L);
            host.History.OnEvents(new[] { Ev(1000 + k, TouchPhase.Up, noisy) }, k * 100L + 50);
        }
        var calm = Pt(2, 20, 40, 1300, 1280);
        host.History.OnEvents(new[] { Ev(1, TouchPhase.Down, calm) }, 0);
        host.History.OnEvents(new[] { Ev(1, TouchPhase.Up, calm) }, 30);
    }

    // ── GET /history ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistory_score降冪_欄位齊全_鎖定後退出排行()
    {
        var (host, _, log) = NewHost("""{ "autoLock": { "enabled": false } }""");
        using (host)
        using (log)
        {
            Seed(host);

            var (status, body) = await Get(host, "/history");
            Assert.Equal(HttpStatusCode.OK, status);

            using (var doc = JsonDocument.Parse(body))
            {
                var root = doc.RootElement;
                Assert.True(root.GetProperty("uptimeMs").GetInt64() >= 0);

                var cells = root.GetProperty("cells");
                Assert.Equal(2, cells.GetArrayLength());

                // score 降冪：狂叫孤立 cell 排第一
                var first = cells[0];
                Assert.Equal(2, first.GetProperty("face").GetInt32());
                Assert.Equal(7, first.GetProperty("cell")[0].GetInt32());
                Assert.Equal(13, first.GetProperty("cell")[1].GetInt32());
                Assert.True(first.GetProperty("score").GetInt32() >
                            cells[1].GetProperty("score").GetInt32());

                // 規格要求欄位齊全
                Assert.Equal(40, first.GetProperty("count30m").GetInt32());
                Assert.Equal(40, first.GetProperty("total").GetInt64());
                Assert.Equal(1.0, first.GetProperty("isolationRatio").GetDouble(), 3);
                Assert.True(first.GetProperty("occupancyRatio").GetDouble() > 0);
                Assert.True(first.GetProperty("lastTriggerAgoMs").GetInt64() >= 0);
                Assert.True(first.GetProperty("lastLifetimeMs").GetInt64() >= 0);
                Assert.False(first.GetProperty("locked").GetBoolean());
            }

            // v9：鎖定後該格退出排行（歷史照常累積；鎖後活動判讀走 GET /locks）
            host.LockCell(2, 7, 13);
            var (_, body2) = await Get(host, "/history");
            using (var doc = JsonDocument.Parse(body2))
            {
                var cells = doc.RootElement.GetProperty("cells");
                Assert.Equal(1, cells.GetArrayLength());
                Assert.Equal(20, cells[0].GetProperty("cell")[0].GetInt32());   // 只剩 calm cell
            }

            // GET /locks：鎖定格帶保存狀態 + 鎖後活動（剛鎖、還沒再發 → 0 / null）
            var (locksStatus, locksBody) = await Get(host, "/locks");
            Assert.Equal(HttpStatusCode.OK, locksStatus);
            using (var doc = JsonDocument.Parse(locksBody))
            {
                var cell = Assert.Single(doc.RootElement.GetProperty("cells").EnumerateArray());
                Assert.Equal(2, cell.GetProperty("face").GetInt32());
                Assert.Equal(7, cell.GetProperty("cell")[0].GetInt32());
                Assert.Equal("manual", cell.GetProperty("source").GetString());
                Assert.False(cell.GetProperty("saved").GetBoolean());           // manual = 臨時（未保存）
                Assert.Equal(0, cell.GetProperty("droppedSinceLock").GetInt64());
                Assert.Equal(JsonValueKind.Null, cell.GetProperty("lastDropAgoMs").ValueKind);
                Assert.True(cell.GetProperty("lockedAgoMs").GetInt64() >= 0);
            }
        }
    }

    [Fact]
    public async Task GetHistory_top參數限制筆數_非法值回預設()
    {
        var (host, _, log) = NewHost("""{ "autoLock": { "enabled": false } }""");
        using (host)
        using (log)
        {
            Seed(host);

            var (status, body) = await Get(host, "/history?top=1");
            Assert.Equal(HttpStatusCode.OK, status);
            using (var doc = JsonDocument.Parse(body))
                Assert.Equal(1, doc.RootElement.GetProperty("cells").GetArrayLength());

            // 非法 top：回預設（50），不報錯
            (status, body) = await Get(host, "/history?top=banana");
            Assert.Equal(HttpStatusCode.OK, status);
            using (var doc = JsonDocument.Parse(body))
                Assert.Equal(2, doc.RootElement.GetProperty("cells").GetArrayLength());
        }
    }

    [Fact]
    public async Task GetHistory_空歷史回空陣列()
    {
        var (host, _, log) = NewHost("""{ "autoLock": { "enabled": false } }""");
        using (host)
        using (log)
        {
            var (status, body) = await Get(host, "/history");
            Assert.Equal(HttpStatusCode.OK, status);
            using var doc = JsonDocument.Parse(body);
            Assert.Equal(0, doc.RootElement.GetProperty("cells").GetArrayLength());
        }
    }

    // ── POST /locks/lock ─────────────────────────────────────────────────────

    [Fact]
    public async Task PostLocksLock_語意同stdin_lock指令()
    {
        var (host, _, log) = NewHost("""{ "autoLock": { "enabled": false } }""");
        using (host)
        using (log)
        {
            // 正常鎖定（manual 來源、runtime 即時生效）
            var (status, body) = await Post(host, "/locks/lock", """{ "face": 2, "col": 7, "row": 13 }""");
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Contains("\"ok\":true", body);
            Assert.True(host.Locks.IsLocked(2, 7, 13));
            Assert.Equal(LockSource.Manual, host.Locks.Find(2, 7, 13)!.Source);

            // 重複鎖定：ok:false（已鎖、未重複加鎖）
            (_, body) = await Post(host, "/locks/lock", """{ "face": 2, "col": 7, "row": 13 }""");
            Assert.Contains("\"ok\":false", body);

            // 超出場域範圍：拒絕（與 stdin lock 同防呆；中文訊息在 JSON 裡是 \uXXXX，要解析後比對）
            (_, body) = await Post(host, "/locks/lock", """{ "face": 99, "col": 0, "row": 0 }""");
            Assert.Contains("\"ok\":false", body);
            using (var doc = JsonDocument.Parse(body))
                Assert.Contains("超出", doc.RootElement.GetProperty("message").GetString());
            Assert.False(host.Locks.IsLocked(99, 0, 0));

            // 壞 body → 400
            (status, _) = await Post(host, "/locks/lock", """{ "face": 2 }""");
            Assert.Equal(HttpStatusCode.BadRequest, status);

            // 與 unlock 成對：解掉剛鎖的
            (_, body) = await Post(host, "/locks/unlock", """{ "face": 2, "col": 7, "row": 13 }""");
            Assert.Contains("\"ok\":true", body);
            Assert.False(host.Locks.IsLocked(2, 7, 13));
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
    public async Task 非loopback_lock被403_history唯讀可看()
    {
        var lanIp = FindNonLoopbackIPv4();
        if (lanIp == null) return;   // 測試機沒有非 loopback IPv4：跳過（場館機必有）

        var (host, _, log) = NewHost("""{ "autoLock": { "enabled": false } }""");
        using (host)
        using (log)
        {
            string baseUrl = $"http://{lanIp}:{host.WsHub!.Port}";

            var lockResp = await Http.PostAsync($"{baseUrl}/locks/lock",
                new StringContent("""{ "face": 2, "col": 7, "row": 13 }""", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.Forbidden, lockResp.StatusCode);
            Assert.False(host.Locks.IsLocked(2, 7, 13));

            // 唯讀排行不限 loopback：場館內網監看 OK
            var histResp = await Http.GetAsync($"{baseUrl}/history?top=10");
            Assert.Equal(HttpStatusCode.OK, histResp.StatusCode);
        }
    }

    // ── stdin `history` 指令（console 排行格式）──────────────────────────────

    [Fact]
    public void HistorySnapshot_排行格式與top筆數()
    {
        var (host, _, log) = NewHost("""{ "autoLock": { "enabled": false } }""");
        using (host)
        using (log)
        {
            // 空歷史
            Assert.Contains("尚無觸發歷史", host.HistorySnapshot());

            Seed(host);

            var text = host.HistorySnapshot();   // 預設 top 20
            Assert.Contains("疑似噪訊排行 top 20", text);
            Assert.Contains("(7,13)", text);
            Assert.Contains("(20,40)", text);
            Assert.Contains("2 Floor", text);    // face 名稱
            Assert.Contains("score", text);
            Assert.Contains("頻率(40) + 孤立比率(35) + 活躍時長佔比(25)", text);

            // top=1：只列最高分那筆
            var top1 = host.HistorySnapshot(1);
            Assert.Contains("(7,13)", top1);
            Assert.DoesNotContain("(20,40)", top1);

            // v9：鎖定後該格退出排行（鎖後活動改看 locks 的「鎖後封包」欄）
            host.LockCell(2, 7, 13);
            var locked = host.HistorySnapshot();
            Assert.DoesNotContain("(7,13)", locked);
            Assert.Contains("(20,40)", locked);
            Assert.Contains("不列入排行", locked);
            var locksText = host.LocksSnapshot();
            Assert.Contains("(7,13)", locksText);
            Assert.Contains("鎖後封包", locksText);
            Assert.Contains("鎖後安靜", locksText);   // 剛鎖、還沒再發
        }
    }
}
