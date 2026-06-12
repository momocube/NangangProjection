using System.Text;
using System.Text.Json;
using WallProjector.Config;
using WallProjector.Core;
using WallProjector.Engine;
using WallProjector.Web;
using Xunit;

namespace WallProjector.Tests;

/// <summary>
/// 管理 API：GET 狀態、POST 變更（寫回 projections.json + 即時生效）、非 loopback 403、
/// wrap 頁路由。瀏覽器/視窗操作不在這裡測（Windows 現場驗證清單，見 README）。
/// </summary>
public class AdminApiTests : IDisposable
{
    readonly string _dir = Directory.CreateTempSubdirectory("wp-api-tests").FullName;
    readonly Logger _log = new() { MinLevel = Logger.Level.Error };

    (AdminApi Api, ProjectionEngine Engine, string ConfigPath) Make(bool withVenue = true)
    {
        var configPath = Path.Combine(_dir, $"projections-{Guid.NewGuid():N}.json");
        var config = new ProjectorConfig();
        var ts = new TouchServiceState
        {
            Venue = withVenue ? VenueInfo.Parse(File.ReadAllText("venues/704.json")) : null,
            Screen = new ScreenRectConfig { X = 0, Y = 0, Width = 1920, Height = 1080 },
            Content = new ContentRectConfig(),
            Source = withVenue ? "live" : "fallback",
            Reachable = withVenue,
            Warning = withVenue ? null : "TouchService 不可達",
        };
        // frameChecker 注入 null 檢查（測試不打網路）
        var engine = new ProjectionEngine(config, configPath, ts, _log, frameChecker: _ => null);
        return (new AdminApi(engine), engine, configPath);
    }

    static HttpServer.Request Req(string method, string path, string body = "", bool loopback = true) =>
        new(method, path, "", body, loopback);

    static JsonElement Parse(HttpServer.Response resp) =>
        JsonDocument.Parse(Encoding.UTF8.GetString(resp.Body)).RootElement;

    // ── GET /api/state ──────────────────────────────────────────────────────

    [Fact]
    public void state_回venue全faces_含地板標註與旋轉猜測()
    {
        var (api, engine, _) = Make();
        using var _e = engine;

        var resp = api.Handle(Req("GET", "/api/state"));
        Assert.Equal("200 OK", resp.Status);

        var root = Parse(resp);
        var faces = root.GetProperty("faces");
        Assert.Equal(7, faces.GetArrayLength());

        var floor = faces[2];
        Assert.True(floor.GetProperty("floor").GetBoolean());
        Assert.Equal(0, floor.GetProperty("guessRotation").GetInt32());

        var left = faces[0];
        Assert.Equal("Wall Left", left.GetProperty("name").GetString());
        Assert.False(left.GetProperty("floor").GetBoolean());
        Assert.Equal(270, left.GetProperty("guessRotation").GetInt32());
        // rect 與 ScreenMapper 一致
        Assert.Equal(0, left.GetProperty("screenRect").GetProperty("x").GetInt32());
        Assert.Equal(180, left.GetProperty("screenRect").GetProperty("y").GetInt32());
        Assert.Equal(457, left.GetProperty("screenRect").GetProperty("w").GetInt32());
        Assert.Equal(576, left.GetProperty("screenRect").GetProperty("h").GetInt32());

        Assert.True(root.GetProperty("touchService").GetProperty("reachable").GetBoolean());
        Assert.Equal("live", root.GetProperty("touchService").GetProperty("source").GetString());
    }

    [Fact]
    public void state_無venue_warning帶出_faces空()
    {
        var (api, engine, _) = Make(withVenue: false);
        using var _e = engine;

        var root = Parse(api.Handle(Req("GET", "/api/state")));
        Assert.False(root.GetProperty("touchService").GetProperty("reachable").GetBoolean());
        Assert.NotNull(root.GetProperty("touchService").GetProperty("warning").GetString());
        Assert.Equal(0, root.GetProperty("faces").GetArrayLength());
    }

    // ── POST 寫入 ───────────────────────────────────────────────────────────

    [Fact]
    public void slot更新_寫回projections_json()
    {
        var (api, engine, configPath) = Make();
        using var _e = engine;

        var resp = api.Handle(Req("POST", "/api/slot",
            """{ "face": 4, "url": "https://example.com/", "rotation": 90 }"""));
        Assert.Equal("200 OK", resp.Status);
        Assert.True(Parse(resp).GetProperty("ok").GetBoolean());

        // 即時寫回檔案
        var saved = ProjectorConfig.Load(configPath);
        var slot = saved.GetSlot(4)!;
        Assert.Equal("https://example.com/", slot.Url);
        Assert.Equal(90, slot.Rotation);
        Assert.False(slot.Enabled);   // 沒給 enabled 不動

        // state 反映
        var face4 = Parse(api.Handle(Req("GET", "/api/state"))).GetProperty("faces")[4];
        Assert.Equal("https://example.com/", face4.GetProperty("slot").GetProperty("url").GetString());
    }

    [Fact]
    public void slot更新_rotation非法_400()
    {
        var (api, engine, _) = Make();
        using var _e = engine;

        var resp = api.Handle(Req("POST", "/api/slot",
            """{ "face": 4, "url": "https://example.com/", "rotation": 45 }"""));
        Assert.Equal("400 Bad Request", resp.Status);
    }

    [Fact]
    public void slot更新_face超出venue範圍_400()
    {
        var (api, engine, _) = Make();
        using var _e = engine;

        var resp = api.Handle(Req("POST", "/api/slot", """{ "face": 99, "url": "https://x.example/" }"""));
        Assert.Equal("400 Bad Request", resp.Status);
    }

    [Fact]
    public void slot更新_body壞掉_400()
    {
        var (api, engine, _) = Make();
        using var _e = engine;

        Assert.Equal("400 Bad Request", api.Handle(Req("POST", "/api/slot", "not json")).Status);
        Assert.Equal("400 Bad Request", api.Handle(Req("POST", "/api/slot", "{}")).Status);
    }

    [Fact]
    public void start_無venue資料_明確失敗訊息()
    {
        var (api, engine, _) = Make(withVenue: false);
        using var _e = engine;

        // 先塞 slot（無 venue 時不擋 face 範圍——TouchService 回來後就能用）
        api.Handle(Req("POST", "/api/slot", """{ "face": 4, "url": "https://example.com/" }"""));
        var resp = api.Handle(Req("POST", "/api/start", """{ "face": 4 }"""));

        Assert.Equal("400 Bad Request", resp.Status);
        Assert.Contains("venue", Parse(resp).GetProperty("message").GetString()!);
    }

    [Fact]
    public void stop_設enabled_false並寫回()
    {
        var (api, engine, configPath) = Make();
        using var _e = engine;

        api.Handle(Req("POST", "/api/slot",
            """{ "face": 4, "url": "https://example.com/", "enabled": false }"""));
        // 手動把 enabled 拉起來（不經 start——start 會真的開瀏覽器）
        var cfg = ProjectorConfig.Load(configPath);
        Assert.False(cfg.GetSlot(4)!.Enabled);

        var resp = api.Handle(Req("POST", "/api/stop", """{ "face": 4 }"""));
        Assert.Equal("200 OK", resp.Status);
        Assert.False(ProjectorConfig.Load(configPath).GetSlot(4)!.Enabled);
    }

    // ── loopback 限制 ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("/api/slot", """{ "face": 0, "url": "https://x.example/" }""")]
    [InlineData("/api/start", """{ "face": 0 }""")]
    [InlineData("/api/stop", """{ "face": 0 }""")]
    [InlineData("/api/restore", "{}")]
    [InlineData("/api/reconnect", "{}")]
    public void 寫入端點_非loopback_403(string path, string body)
    {
        var (api, engine, _) = Make();
        using var _e = engine;

        var resp = api.Handle(Req("POST", path, body, loopback: false));
        Assert.Equal("403 Forbidden", resp.Status);
        Assert.Contains("loopback", Parse(resp).GetProperty("message").GetString()!);
    }

    [Fact]
    public void 唯讀端點_非loopback_照常服務()
    {
        var (api, engine, _) = Make();
        using var _e = engine;

        Assert.Equal("200 OK", api.Handle(Req("GET", "/api/state", loopback: false)).Status);
        Assert.Equal("200 OK", api.Handle(Req("GET", "/", loopback: false)).Status);
    }

    // ── wrap 頁路由 ─────────────────────────────────────────────────────────

    [Fact]
    public void wrap_已設定槽_回旋轉頁()
    {
        var (api, engine, _) = Make();
        using var _e = engine;

        api.Handle(Req("POST", "/api/slot",
            """{ "face": 0, "url": "https://example.com/", "rotation": 270 }"""));
        var resp = api.Handle(Req("GET", "/wrap/0"));

        Assert.Equal("200 OK", resp.Status);
        var html = Encoding.UTF8.GetString(resp.Body);
        Assert.Contains("rotate(270deg)", html);
        Assert.Contains("[WP-slot-0]", html);
    }

    [Fact]
    public void wrap_未設定_404占位頁()
    {
        var (api, engine, _) = Make();
        using var _e = engine;

        var resp = api.Handle(Req("GET", "/wrap/3"));
        Assert.Equal("404 Not Found", resp.Status);
        Assert.Contains("尚未設定", Encoding.UTF8.GetString(resp.Body));
    }

    [Fact]
    public void wrap_非數字_404()
    {
        var (api, engine, _) = Make();
        using var _e = engine;
        Assert.Equal("404 Not Found", api.Handle(Req("GET", "/wrap/abc")).Status);
    }

    // ── HTTP server 整合（真 socket）────────────────────────────────────────

    [Fact]
    public async Task http整合_loopback_GET與POST走完整條線()
    {
        var (api, engine, configPath) = Make();
        using var _e = engine;
        using var server = new HttpServer(0, api.Handle, _log);   // port 0 = OS 配發
        server.Start();

        using var http = new HttpClient();
        var baseUrl = $"http://127.0.0.1:{server.Port}";

        // 管理頁
        var page = await http.GetStringAsync($"{baseUrl}/");
        Assert.Contains("WallProjector", page);

        // GET state
        var state = await http.GetStringAsync($"{baseUrl}/api/state");
        Assert.Contains("\"faces\"", state);

        // POST slot（loopback 來源 → 放行）
        var resp = await http.PostAsync($"{baseUrl}/api/slot",
            new StringContent("""{ "face": 1, "url": "https://example.com/", "rotation": 180 }""",
                Encoding.UTF8, "application/json"));
        Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal(180, ProjectorConfig.Load(configPath).GetSlot(1)!.Rotation);

        // 404
        var nf = await http.GetAsync($"{baseUrl}/nothing");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, nf.StatusCode);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
