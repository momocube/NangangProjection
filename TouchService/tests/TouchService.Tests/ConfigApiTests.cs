using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TouchService.Config;
using TouchService.Core;
using TouchService.Logging;
using TouchService.Mouse;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// 本機管理 HTTP API（與 WS 同 port）：GET/POST /config roundtrip + 寫回保留其他欄位、
/// 非法值 400 拒收、非 loopback 403、/monitors 非 Windows 空陣列、locks API、
/// touchMode 熱切（按住中 mouse→grid 自動 LEFTUP）。
/// </summary>
public class ConfigApiTests
{
    sealed class FakeInjector : IMouseInjector
    {
        public readonly List<string> Calls = new();
        public void Move(int x, int y) => Calls.Add($"move({x},{y})");
        public void LeftDown() => Calls.Add("down");
        public void LeftUp() => Calls.Add("up");
    }

    static (TouchServiceHost Host, string ConfigPath, FileLogger Log, FakeInjector Injector) NewHost(
        string configJson)
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-api").FullName;
        var configPath = Path.Combine(dir, "config.json");
        File.WriteAllText(configPath, configJson);
        var cfg = ServiceConfig.LoadOrCreate(configPath);
        cfg.ListenPort = 0;
        cfg.VenueProfile = Path.Combine(AppContext.BaseDirectory, "venues", "704.json");
        cfg.UdpOutput = new UdpOutputConfig { Enabled = false };
        cfg.WebSocketOutput = new WebSocketOutputConfig { Enabled = true, Port = 0 };
        cfg.Log = new LogConfig { Console = false, Dir = dir, Level = "Warn" };
        var log = new FileLogger(dir, LogLevel.Warn, console: false);
        var injector = new FakeInjector();
        var host = new TouchServiceHost(cfg, AppContext.BaseDirectory, log, configPath, injector);
        host.Start();
        return (host, configPath, log, injector);
    }

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

    static string Url(TouchServiceHost host, string path) =>
        $"http://127.0.0.1:{host.WsHub!.Port}{path}";

    static async Task<(HttpStatusCode Status, string Body)> Get(TouchServiceHost host, string path)
    {
        var resp = await Http.GetAsync(Url(host, path));
        return (resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    static async Task<(HttpStatusCode Status, string Body)> Post(TouchServiceHost host, string path, string json)
    {
        var resp = await Http.PostAsync(Url(host, path),
            new StringContent(json, Encoding.UTF8, "application/json"));
        return (resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    // ── GET /config ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetConfig_直出目前生效值含需重啟欄位()
    {
        var (host, _, log, _) = NewHost("""{ "autoLock": { "enabled": false }, "releaseTimeoutMs": 190 }""");
        using (host)
        using (log)
        {
            var (status, body) = await Get(host, "/config");
            Assert.Equal(HttpStatusCode.OK, status);

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            Assert.Equal("grid", root.GetProperty("touchMode").GetString());
            Assert.True(root.GetProperty("interactionEnabled").GetBoolean());  // 舊 config 缺欄位時預設啟用
            Assert.Equal(190, root.GetProperty("releaseTimeoutMs").GetInt32());
            Assert.Equal(96, root.GetProperty("matchRadiusPx").GetSingle());
            Assert.False(root.GetProperty("autoLock").GetProperty("enabled").GetBoolean());
            var mm = root.GetProperty("mouseMode");
            Assert.Equal(0.35, mm.GetProperty("smoothing").GetDouble(), 3);
            // faces 缺欄位 → 預設：全部面排除 Floor（704 = face 2 地板）
            var faces = mm.GetProperty("faces").EnumerateArray().Select(f => f.GetInt32()).ToArray();
            Assert.Equal(new[] { 0, 1, 3, 4, 5, 6 }, faces);
            // content 預設滿版 0/0/1/1
            Assert.Equal(1.0, mm.GetProperty("content").GetProperty("scaleX").GetDouble(), 3);
            Assert.Equal(0.0, mm.GetProperty("content").GetProperty("offsetX").GetDouble(), 3);
            Assert.True(root.GetProperty("ui").GetProperty("trayEnabled").GetBoolean());
            Assert.Equal("704", root.GetProperty("venue").GetString());
            // 需重啟欄位（UI 唯讀顯示用）；去敏感：不直出 log 設定
            Assert.True(root.GetProperty("restartOnly").TryGetProperty("listenPort", out _));
            Assert.False(root.TryGetProperty("log", out _));
        }
    }

    // ── POST /config roundtrip ───────────────────────────────────────────────

    [Fact]
    public async Task PostConfig_roundtrip_熱套用且寫回保留其他欄位()
    {
        var (host, configPath, log, _) = NewHost("""
            {
              "listenPort": 30001,
              "customFutureField": { "keep": true },
              "autoLock": { "enabled": false }
            }
            """);
        using (host)
        using (log)
        {
            var (status, body) = await Post(host, "/config", """
                {
                  "touchMode": "mouse",
                  "interactionEnabled": false,
                  "releaseTimeoutMs": 220,
                  "matchRadiusPx": 120,
                  "mouseMode": {
                    "faces": [4, 0],
                    "screen": { "x": -1920, "y": 0, "width": 1920, "height": 1080 },
                    "content": { "offsetX": 12, "offsetY": -8, "scaleX": 0.9, "scaleY": 0.95 },
                    "smoothing": 0.5,
                    "moveMinPx": 3
                  },
                  "autoLock": { "enabled": true, "windowSeconds": 8, "minPackets": 12, "minEpisodes": 5 }
                }
                """);
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Contains("\"ok\":true", body);

            // GET 看到新生效值
            var (_, getBody) = await Get(host, "/config");
            using (var doc = JsonDocument.Parse(getBody))
            {
                var root = doc.RootElement;
                Assert.Equal("mouse", root.GetProperty("touchMode").GetString());
                Assert.False(root.GetProperty("interactionEnabled").GetBoolean());
                Assert.Equal(220, root.GetProperty("releaseTimeoutMs").GetInt32());
                Assert.Equal(120, root.GetProperty("matchRadiusPx").GetSingle());
                var mm = root.GetProperty("mouseMode");
                Assert.Equal(new[] { 0, 4 },   // 解析後排序去重
                    mm.GetProperty("faces").EnumerateArray().Select(f => f.GetInt32()).ToArray());
                Assert.Equal(-1920, mm.GetProperty("screen").GetProperty("x").GetInt32());
                Assert.Equal(12, mm.GetProperty("content").GetProperty("offsetX").GetDouble(), 3);
                Assert.Equal(0.9, mm.GetProperty("content").GetProperty("scaleX").GetDouble(), 3);
                Assert.Equal(0.5, mm.GetProperty("smoothing").GetDouble(), 3);
            }

            // 寫回 config.json：可 roundtrip、其他欄位（含未知欄位）原樣保留
            var reloaded = ServiceConfig.LoadOrCreate(configPath);
            Assert.Equal("mouse", reloaded.TouchMode);
            Assert.False(reloaded.InteractionEnabled);
            Assert.Equal(220, reloaded.ReleaseTimeoutMs);
            Assert.Equal(new List<int> { 0, 4 }, reloaded.MouseMode.Faces);
            Assert.Equal(-1920, reloaded.MouseMode.Screen.X);
            Assert.Equal(0.9, reloaded.MouseMode.Content.ScaleX, 3);
            Assert.Equal(-8, reloaded.MouseMode.Content.OffsetY, 3);
            Assert.True(reloaded.AutoLock.Enabled);
            Assert.Equal(8, reloaded.AutoLock.WindowSeconds);
            Assert.Equal(12, reloaded.AutoLock.MinPackets);
            Assert.Equal(5, reloaded.AutoLock.MinEpisodes);
            Assert.Equal(30001, reloaded.ListenPort);
            Assert.Contains("customFutureField", File.ReadAllText(configPath));
        }
    }

    [Fact]
    public async Task PostConfig_部分patch_只動有給的欄位()
    {
        var (host, _, log, _) = NewHost("""{ "autoLock": { "enabled": false }, "releaseTimeoutMs": 190 }""");
        using (host)
        using (log)
        {
            var (status, _) = await Post(host, "/config", """{ "matchRadiusPx": 64 }""");
            Assert.Equal(HttpStatusCode.OK, status);

            var (_, body) = await Get(host, "/config");
            using var doc = JsonDocument.Parse(body);
            Assert.Equal(64, doc.RootElement.GetProperty("matchRadiusPx").GetSingle());
            Assert.Equal(190, doc.RootElement.GetProperty("releaseTimeoutMs").GetInt32());   // 未給：不動
            Assert.Equal("grid", doc.RootElement.GetProperty("touchMode").GetString());
        }
    }

    [Fact]
    public async Task PostConfig_舊欄位face單數_相容轉成faces()
    {
        var (host, configPath, log, _) = NewHost("""{ "autoLock": { "enabled": false } }""");
        using (host)
        using (log)
        {
            // 舊版 UI / 腳本還送 face（單數）→ 視為 faces:[face]
            var (status, _) = await Post(host, "/config", """{ "mouseMode": { "face": 4 } }""");
            Assert.Equal(HttpStatusCode.OK, status);

            var (_, body) = await Get(host, "/config");
            using var doc = JsonDocument.Parse(body);
            Assert.Equal(new[] { 4 },
                doc.RootElement.GetProperty("mouseMode").GetProperty("faces")
                    .EnumerateArray().Select(f => f.GetInt32()).ToArray());

            // 寫回的是新 schema（faces 陣列，無 face 單數欄位）
            var raw = File.ReadAllText(configPath);
            Assert.Contains("\"faces\"", raw);
            Assert.DoesNotContain("\"face\":", raw);
        }
    }

    // ── 非法值拒收（400，且不留半套狀態）─────────────────────────────────────

    [Theory]
    [InlineData("""{ "touchMode": "banana" }""")]
    [InlineData("""{ "interactionEnabled": "false" }""")]
    [InlineData("""{ "releaseTimeoutMs": 0 }""")]
    [InlineData("""{ "releaseTimeoutMs": "fast" }""")]
    [InlineData("""{ "matchRadiusPx": -5 }""")]
    [InlineData("""{ "mouseMode": { "faces": [99] } }""")]
    [InlineData("""{ "mouseMode": { "faces": [-1] } }""")]
    [InlineData("""{ "mouseMode": { "faces": "walls" } }""")]
    [InlineData("""{ "mouseMode": { "faces": [0, "x"] } }""")]
    [InlineData("""{ "mouseMode": { "face": 99 } }""")]
    [InlineData("""{ "mouseMode": { "smoothing": 0 } }""")]
    [InlineData("""{ "mouseMode": { "smoothing": 1.5 } }""")]
    [InlineData("""{ "mouseMode": { "screen": { "width": 0 } } }""")]
    [InlineData("""{ "mouseMode": { "content": { "scaleX": 0 } } }""")]
    [InlineData("""{ "mouseMode": { "content": { "scaleY": -1 } } }""")]
    [InlineData("""{ "mouseMode": { "content": "full" } }""")]
    [InlineData("""{ "autoLock": { "minPackets": 0 } }""")]
    [InlineData("""{ "autoLock": { "minEpisodes": -1 } }""")]
    [InlineData("not json at all")]
    public async Task PostConfig_非法值_400且現值不變(string badBody)
    {
        var (host, configPath, log, _) = NewHost("""{ "autoLock": { "enabled": false } }""");
        using (host)
        using (log)
        {
            var before = await Get(host, "/config");
            var fileBefore = File.ReadAllText(configPath);

            var (status, body) = await Post(host, "/config", badBody);
            Assert.Equal(HttpStatusCode.BadRequest, status);
            Assert.Contains("\"ok\":false", body);

            // 驗證全過才套用：生效值與檔案都不變
            var after = await Get(host, "/config");
            Assert.Equal(before.Body, after.Body);
            Assert.Equal(fileBefore, File.ReadAllText(configPath));
        }
    }

    // ── loopback 限制 ────────────────────────────────────────────────────────

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
    public async Task 寫入類API與monitors_非loopback來源403()
    {
        var lanIp = FindNonLoopbackIPv4();
        if (lanIp == null) return;   // 測試機沒有非 loopback IPv4：跳過（場館機必有）

        var (host, _, log, _) = NewHost("""{ "autoLock": { "enabled": false } }""");
        using (host)
        using (log)
        {
            string baseUrl = $"http://{lanIp}:{host.WsHub!.Port}";

            // hub 綁 0.0.0.0：從本機打 LAN IP，remote address = LAN IP（非 loopback）
            var post = await Http.PostAsync($"{baseUrl}/config",
                new StringContent("""{ "matchRadiusPx": 64 }""", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);

            var monitors = await Http.GetAsync($"{baseUrl}/monitors");
            Assert.Equal(HttpStatusCode.Forbidden, monitors.StatusCode);

            var unlock = await Http.PostAsync($"{baseUrl}/locks/unlock",
                new StringContent("""{ "face": 0, "col": 0, "row": 0 }""", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.Forbidden, unlock.StatusCode);

            var save = await Http.PostAsync($"{baseUrl}/locks/save", new StringContent(""));
            Assert.Equal(HttpStatusCode.Forbidden, save.StatusCode);

            // 讀取類（GET /config）不在限制內：場館內網監看 OK
            var get = await Http.GetAsync($"{baseUrl}/config");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        }
    }

    // ── /monitors ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Monitors_非Windows回空陣列()
    {
        var (host, _, log, _) = NewHost("""{ "autoLock": { "enabled": false } }""");
        using (host)
        using (log)
        {
            var (status, body) = await Get(host, "/monitors");
            Assert.Equal(HttpStatusCode.OK, status);
            using var doc = JsonDocument.Parse(body);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            if (!OperatingSystem.IsWindows())
                Assert.Equal(0, doc.RootElement.GetArrayLength());
        }
    }

    // ── locks API（語意同 stdin unlock / savelocks）─────────────────────────

    [Fact]
    public async Task LocksUnlock與Save_runtime生效且寫回config()
    {
        var (host, configPath, log, _) = NewHost("""
            { "autoLock": { "enabled": false },
              "lockedCells": [ { "face": 2, "cell": [7, 13] }, { "face": 0, "cell": [1, 2] } ] }
            """);
        using (host)
        using (log)
        {
            // unlock 一格
            var (status, body) = await Post(host, "/locks/unlock", """{ "face": 2, "col": 7, "row": 13 }""");
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Contains("\"ok\":true", body);
            Assert.False(host.Locks.IsLocked(2, 7, 13));
            Assert.True(host.Locks.IsLocked(0, 1, 2));

            // 重複 unlock：ok:false（本來就沒鎖）
            (_, body) = await Post(host, "/locks/unlock", """{ "face": 2, "col": 7, "row": 13 }""");
            Assert.Contains("\"ok\":false", body);

            // 壞 body → 400
            (status, _) = await Post(host, "/locks/unlock", """{ "face": 2 }""");
            Assert.Equal(HttpStatusCode.BadRequest, status);

            // save：剩餘鎖定寫回 config
            (status, body) = await Post(host, "/locks/save", "");
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Contains("\"ok\":true", body);
            var reloaded = ServiceConfig.LoadOrCreate(configPath);
            var cell = Assert.Single(reloaded.LockedCells);
            Assert.Equal(0, cell.Face);
        }
    }

    // ── touchMode 熱切（host 層）────────────────────────────────────────────

    // Wall Left（face 0）raw 範圍內的點
    static readonly RawPoint WallRaw = new(2200, 1600);

    [Fact]
    public async Task 手動停用觸摸互動_放開現有滑鼠並保留鎖格過濾_重新啟用只接新訊號()
    {
        var (host, configPath, log, injector) = NewHost("""
            {
              "autoLock": { "enabled": false },
              "touchMode": "mouse",
              "releaseTimeoutMs": 60000,
              "mouseMode": { "faces": [0] }
            }
            """);
        using (host)
        using (log)
        {
            var router = new TouchService.Core.SensorFaceRouter(host.Venue);
            Assert.True(router.TryRoute(WallRaw.X, WallRaw.Y, out var first));
            var otherWallRaw = new RawPoint(2400, 2200);
            Assert.True(router.TryRoute(otherWallRaw.X, otherWallRaw.Y, out var other));
            Assert.Equal(first.FaceIndex, other.FaceIndex);
            Assert.NotEqual((first.Col, first.Row), (other.Col, other.Row));

            host.Pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { WallRaw }), 0);
            Assert.Equal(1, host.Pipeline.ActiveTouches);
            Assert.Equal("down", injector.Calls[^1]);

            var (status, body) = await Post(host, "/config", """{ "interactionEnabled": false }""");
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Contains("\"ok\":true", body);
            Assert.Equal(0, host.Pipeline.ActiveTouches);
            Assert.Equal("up", injector.Calls[^1]);  // 先清掉既有按住狀態
            Assert.False(ServiceConfig.LoadOrCreate(configPath).InteractionEnabled);

            // 停用期間仍可新增/累計鎖格；被鎖格不建立觸點，也不模擬滑鼠。
            Assert.True(host.Locks.TryAdd(first.FaceIndex, first.Col, first.Row,
                LockSource.Manual, nowMs: 100));
            int callsAfterRelease = injector.Calls.Count;
            host.Pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { WallRaw }), 200);
            Assert.Equal(callsAfterRelease, injector.Calls.Count);
            var locked = Assert.Single(host.Locks.Snapshot());
            Assert.Equal(1, locked.DroppedSinceLock);
            Assert.Equal(0, host.Pipeline.ActiveTouches);

            // 未鎖格在停用期間不會輸出；重新開啟後，只從新收到的封包建立互動。
            host.Pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { otherWallRaw }), 300);
            Assert.Equal(callsAfterRelease, injector.Calls.Count);
            Assert.True(host.History.TrackedCellCount >= 2); // 停用輸出時仍累積觸發歷史
            (status, _) = await Post(host, "/config", """{ "interactionEnabled": true }""");
            Assert.Equal(HttpStatusCode.OK, status);
            host.Pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { otherWallRaw }), 400);
            Assert.Equal(1, host.Pipeline.ActiveTouches);
            Assert.Equal("down", injector.Calls[^1]);
            Assert.True(ServiceConfig.LoadOrCreate(configPath).InteractionEnabled);
        }
    }

    [Fact]
    public async Task touchMode熱切_按住中mouse轉grid_自動LeftUp()
    {
        // releaseTimeout 拉長：避免 host 真實 timer 在測試中把 touch 收掉
        var (host, _, log, injector) = NewHost(
            """{ "autoLock": { "enabled": false }, "releaseTimeoutMs": 60000 }""");
        using (host)
        using (log)
        {
            // grid（預設）：觸點零注入
            host.Pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { WallRaw }), 0);
            Assert.Empty(injector.Calls);

            // 熱切到 mouse（只勾 face 0 = Wall Left）
            var (status, _) = await Post(host, "/config",
                """{ "touchMode": "mouse", "mouseMode": { "faces": [0] } }""");
            Assert.Equal(HttpStatusCode.OK, status);

            // 既有 touch 的 move 事件不接手（owner 必須由 down 建立）；新 down 取得游標
            host.Pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { new RawPoint(3264, 1664) }), 100);   // Floor：忽略
            Assert.Empty(injector.Calls);

            host.Pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { new RawPoint(2400, 2200) }), 200);   // Wall Left 新點
            Assert.Equal("down", injector.Calls[^1]);

            // 按住中熱切回 grid → 自動 LEFTUP，不留卡死的按鍵
            (status, _) = await Post(host, "/config", """{ "touchMode": "grid" }""");
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal("up", injector.Calls[^1]);

            // grid 之後：觸點零注入
            int n = injector.Calls.Count;
            host.Pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { WallRaw }), 300);
            Assert.Equal(n, injector.Calls.Count);
        }
    }
}
