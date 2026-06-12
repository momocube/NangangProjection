using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WallProjector.Config;
using WallProjector.Core;
using WallProjector.Engine;
using WallProjector.Web;
using Xunit;

namespace WallProjector.Tests;

/// <summary>
/// file 模式（/media）：副檔名分流、四種 rotation 播放頁 golden、
/// 路徑白名單（遍歷攻擊案例）、Range request（影片 seek）、/assets 防遍歷。
/// </summary>
public class MediaTests : IDisposable
{
    readonly string _dir = Directory.CreateTempSubdirectory("wp-media-tests").FullName;
    readonly Logger _log = new() { MinLevel = Logger.Level.Error };

    (AdminApi Api, ProjectionEngine Engine, string ConfigPath) Make()
    {
        var configPath = Path.Combine(_dir, $"projections-{Guid.NewGuid():N}.json");
        var ts = new TouchServiceState
        {
            Venue = VenueInfo.Parse(File.ReadAllText("venues/704.json")),
            Screen = new ScreenRectConfig { X = 0, Y = 0, Width = 1920, Height = 1080 },
            Content = new ContentRectConfig(),
            Source = "live",
            Reachable = true,
        };
        var engine = new ProjectionEngine(new ProjectorConfig(), configPath, ts, _log,
                                          frameChecker: _ => null);
        var assetsDir = Path.Combine(_dir, "assets");
        Directory.CreateDirectory(assetsDir);
        File.WriteAllText(Path.Combine(assetsDir, "pdf.min.mjs"), "// fake pdf.js for tests");
        return (new AdminApi(engine, new[] { assetsDir }), engine, configPath);
    }

    static HttpServer.Request Req(string method, string path, string body = "",
                                  params (string Name, string Value)[] headers) =>
        new(method, path, "", body, IsLoopback: true)
        {
            Headers = headers.ToDictionary(h => h.Name, h => h.Value, StringComparer.OrdinalIgnoreCase),
        };

    string WriteFile(string name, byte[] bytes)
    {
        var p = Path.Combine(_dir, name);
        File.WriteAllBytes(p, bytes);
        return p;
    }

    void SetFileSlot(AdminApi api, int face, string path, int rotation = 0,
                     bool? loop = null, bool? muted = null, bool? controls = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["face"] = face, ["mode"] = "file", ["rotation"] = rotation, ["url"] = path,
        };
        if (loop.HasValue) payload["loop"] = loop.Value;
        if (muted.HasValue) payload["muted"] = muted.Value;
        if (controls.HasValue) payload["controls"] = controls.Value;
        var resp = api.Handle(Req("POST", "/api/slot", JsonSerializer.Serialize(payload)));
        Assert.Equal("200 OK", resp.Status);
    }

    static string Html(HttpServer.Response resp) => Encoding.UTF8.GetString(resp.Body);

    // ── 副檔名分流 ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("a.mp4", MediaPageBuilder.MediaKind.Video)]
    [InlineData("a.webm", MediaPageBuilder.MediaKind.Video)]
    [InlineData("a.MOV", MediaPageBuilder.MediaKind.Video)]
    [InlineData("a.png", MediaPageBuilder.MediaKind.Image)]
    [InlineData("a.jpg", MediaPageBuilder.MediaKind.Image)]
    [InlineData("a.jpeg", MediaPageBuilder.MediaKind.Image)]
    [InlineData("a.webp", MediaPageBuilder.MediaKind.Image)]
    [InlineData("a.gif", MediaPageBuilder.MediaKind.Image)]
    [InlineData("a.pdf", MediaPageBuilder.MediaKind.Pdf)]
    [InlineData("a.pptx", MediaPageBuilder.MediaKind.Unsupported)]
    [InlineData("a.exe", MediaPageBuilder.MediaKind.Unsupported)]
    [InlineData("a", MediaPageBuilder.MediaKind.Unsupported)]
    public void 副檔名分流(string name, MediaPageBuilder.MediaKind expected) =>
        Assert.Equal(expected, MediaPageBuilder.KindOf(name));

    [Theory]
    [InlineData("a.mp4", "video/mp4")]
    [InlineData("a.webm", "video/webm")]
    [InlineData("a.mov", "video/quicktime")]
    [InlineData("a.pdf", "application/pdf")]
    [InlineData("a.png", "image/png")]
    [InlineData("a.jpg", "image/jpeg")]
    public void contentType對應(string name, string expected) =>
        Assert.Equal(expected, MediaPageBuilder.ContentTypeOf(name));

    // ── 播放頁 golden（四種 rotation——旋轉沿用 wrap 頁同一套 CSS 數學）──────

    [Theory]
    [InlineData(0, "width:100vw;height:100vh;")]
    [InlineData(90, "width:100vh;height:100vw;transform-origin:top left;transform:rotate(90deg) translateY(-100vw);")]
    [InlineData(180, "width:100vw;height:100vh;transform:rotate(180deg);")]
    [InlineData(270, "width:100vh;height:100vw;transform-origin:top left;transform:rotate(270deg) translateX(-100vh);")]
    public void 影片頁_四種rotation_stageCss同wrap頁(int rotation, string expectedCss)
    {
        var html = MediaPageBuilder.BuildVideoPage(3, "Wall X", rotation,
                                                   loop: true, muted: true, controls: false);
        Assert.Contains(expectedCss, html);
        Assert.Contains("[WP-slot-3]", html);             // 視窗對位標記
        Assert.Contains("src=\"/media/3/raw\"", html);    // bytes 走 raw 端點
    }

    [Fact]
    public void 影片頁_選項反映_loop_muted_controls()
    {
        var on = MediaPageBuilder.BuildVideoPage(0, "W", 0, loop: true, muted: true, controls: true);
        Assert.Contains("autoplay", on);
        Assert.Contains(" loop", on);
        Assert.Contains(" muted", on);
        Assert.Contains(" controls", on);

        var off = MediaPageBuilder.BuildVideoPage(0, "W", 0, loop: false, muted: false, controls: false);
        Assert.Contains("autoplay", off);
        Assert.DoesNotContain(" loop", off);
        Assert.DoesNotContain(" muted", off);
        Assert.DoesNotContain(" controls", off);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(270)]
    public void PDF頁_旋轉_翻頁按鈕_本地pdfjs(int rotation)
    {
        var html = MediaPageBuilder.BuildPdfPage(5, "Wall Y", rotation);
        Assert.Contains(RotationCss.StageCss(rotation), html);
        Assert.Contains("/assets/pdf.min.mjs", html);        // vendored，離線可用
        Assert.Contains("/assets/pdf.worker.min.mjs", html);
        Assert.Contains("id=\"prev\"", html);                // 翻頁按鈕（觸控可按）
        Assert.Contains("id=\"next\"", html);
        Assert.Contains("[WP-slot-5]", html);
    }

    [Fact]
    public void 圖片頁_滿版contain()
    {
        var html = MediaPageBuilder.BuildImagePage(1, "Wall Z", 180);
        Assert.Contains("object-fit:contain", html);
        Assert.Contains(RotationCss.StageCss(180), html);
        Assert.Contains("src=\"/media/1/raw\"", html);
    }

    // ── /media 路由（engine + AdminApi）────────────────────────────────────

    [Fact]
    public void media頁_影片槽_回影片頁含選項()
    {
        var (api, engine, _) = Make();
        using var _e = engine;
        var mp4 = WriteFile("clip.mp4", new byte[] { 1, 2, 3 });
        SetFileSlot(api, 4, mp4, rotation: 90, loop: false, controls: true);

        var resp = api.Handle(Req("GET", "/media/4"));
        Assert.Equal("200 OK", resp.Status);
        var html = Html(resp);
        Assert.Contains("<video", html);
        Assert.DoesNotContain(" loop", html);
        Assert.Contains(" controls", html);
        Assert.Contains("rotate(90deg)", html);
    }

    [Fact]
    public void media頁_相對路徑_以config所在目錄為基準()
    {
        var (api, engine, _) = Make();
        using var _e = engine;
        WriteFile("rel.png", new byte[] { 9 });
        SetFileSlot(api, 0, "rel.png");   // 相對路徑：config 在 _dir 內

        Assert.Contains("<img", Html(api.Handle(Req("GET", "/media/0"))));
        Assert.Equal("200 OK", api.Handle(Req("GET", "/media/0/raw")).Status);
    }

    [Fact]
    public void media頁_檔案不存在_明確錯誤頁_保留旋轉()
    {
        var (api, engine, _) = Make();
        using var _e = engine;
        SetFileSlot(api, 4, Path.Combine(_dir, "missing.mp4"), rotation: 270);

        var resp = api.Handle(Req("GET", "/media/4"));
        Assert.Equal("200 OK", resp.Status);
        var html = Html(resp);
        Assert.Contains("檔案不存在", html);
        Assert.Contains("rotate(270deg)", html);   // 錯誤頁也轉正，現場讀得到
    }

    [Fact]
    public void media頁_不支援副檔名_錯誤頁含PPT提示()
    {
        var (api, engine, _) = Make();
        using var _e = engine;
        var pptx = WriteFile("deck.pptx", new byte[] { 1 });
        SetFileSlot(api, 4, pptx);

        var html = Html(api.Handle(Req("GET", "/media/4")));
        Assert.Contains("不支援的副檔名", html);
        Assert.Contains("PDF", html);   // PPT → PDF SOP 提示
    }

    [Fact]
    public void media頁_非file模式槽_404()
    {
        var (api, engine, _) = Make();
        using var _e = engine;
        api.Handle(Req("POST", "/api/slot", """{ "face": 4, "url": "https://example.com/" }"""));

        Assert.Equal("404 Not Found", api.Handle(Req("GET", "/media/4")).Status);
        Assert.Equal("404 Not Found", api.Handle(Req("GET", "/media/4/raw")).Status);
        Assert.Equal("404 Not Found", api.Handle(Req("GET", "/media/2")).Status);   // 未設定
    }

    [Fact]
    public void media路由_遍歷攻擊形狀_一律404()
    {
        var (api, engine, _) = Make();
        using var _e = engine;
        var mp4 = WriteFile("ok.mp4", new byte[] { 1 });
        SetFileSlot(api, 4, mp4);

        // request 帶不進任何路徑成分——非 {face} / {face}/raw 形狀全部 404
        Assert.Equal("404 Not Found", api.Handle(Req("GET", "/media/4/../../etc/passwd")).Status);
        Assert.Equal("404 Not Found", api.Handle(Req("GET", "/media/../projections.json")).Status);
        Assert.Equal("404 Not Found", api.Handle(Req("GET", "/media/4/raw/extra")).Status);
        Assert.Equal("404 Not Found", api.Handle(Req("GET", "/media/abc/raw")).Status);
        Assert.Equal("404 Not Found", api.Handle(Req("GET", "/media/")).Status);
    }

    // ── raw + Range ─────────────────────────────────────────────────────────

    [Fact]
    public void raw_全檔_200_帶AcceptRanges()
    {
        var (api, engine, _) = Make();
        using var _e = engine;
        var bytes = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
        SetFileSlot(api, 4, WriteFile("v.mp4", bytes));

        var resp = api.Handle(Req("GET", "/media/4/raw"));
        Assert.Equal("200 OK", resp.Status);
        Assert.Equal("video/mp4", resp.ContentType);
        Assert.Contains(("Accept-Ranges", "bytes"), resp.ExtraHeaders);
        Assert.NotNull(resp.FileBody);
        Assert.Equal((0L, 100L), (resp.FileBody!.Value.Offset, resp.FileBody!.Value.Length));
    }

    [Theory]
    [InlineData("bytes=10-19", 10, 10, "bytes 10-19/100")]
    [InlineData("bytes=90-", 90, 10, "bytes 90-99/100")]
    [InlineData("bytes=-5", 95, 5, "bytes 95-99/100")]
    [InlineData("bytes=0-999", 0, 100, "bytes 0-99/100")]   // 超出檔尾 clamp
    public void raw_Range_206_ContentRange正確(string range, long offset, long length, string contentRange)
    {
        var (api, engine, _) = Make();
        using var _e = engine;
        var bytes = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
        SetFileSlot(api, 4, WriteFile("v.mp4", bytes));

        var resp = api.Handle(Req("GET", "/media/4/raw", headers: ("Range", range)));
        Assert.Equal("206 Partial Content", resp.Status);
        Assert.Contains(("Content-Range", contentRange), resp.ExtraHeaders);
        Assert.Equal((offset, length), (resp.FileBody!.Value.Offset, resp.FileBody!.Value.Length));
    }

    [Theory]
    [InlineData("bytes=100-")]    // 起點 == 檔長
    [InlineData("bytes=200-300")]
    [InlineData("bytes=5-2")]     // a > b
    [InlineData("bytes=-0")]
    public void raw_Range不可滿足_416(string range)
    {
        var (api, engine, _) = Make();
        using var _e = engine;
        SetFileSlot(api, 4, WriteFile("v.mp4", new byte[100]));

        var resp = api.Handle(Req("GET", "/media/4/raw", headers: ("Range", range)));
        Assert.Equal("416 Range Not Satisfiable", resp.Status);
        Assert.Contains(("Content-Range", "bytes */100"), resp.ExtraHeaders);
    }

    [Fact]
    public void rangeParser_多段與非bytes_忽略回全檔()
    {
        Assert.Equal(HttpRange.Kind.Full, HttpRange.Parse("bytes=0-1,5-9", 100).Kind);
        Assert.Equal(HttpRange.Kind.Full, HttpRange.Parse("items=0-5", 100).Kind);
        Assert.Equal(HttpRange.Kind.Full, HttpRange.Parse(null, 100).Kind);
        Assert.Equal(HttpRange.Kind.Unsatisfiable, HttpRange.Parse("bytes=abc", 100).Kind);
    }

    [Fact]
    public async Task raw_整合_真socket_206部分bytes正確_影片seek路徑()
    {
        var (api, engine, _) = Make();
        using var _e = engine;
        var bytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        SetFileSlot(api, 4, WriteFile("seek.mp4", bytes));

        using var server = new HttpServer(0, api.Handle, _log);
        server.Start();
        using var http = new HttpClient();

        // 全檔
        var full = await http.GetByteArrayAsync($"http://127.0.0.1:{server.Port}/media/4/raw");
        Assert.Equal(bytes, full);

        // Range（瀏覽器 seek 的請求形狀）
        var reqMsg = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{server.Port}/media/4/raw");
        reqMsg.Headers.Range = new RangeHeaderValue(100, 109);
        var resp = await http.SendAsync(reqMsg);
        Assert.Equal(System.Net.HttpStatusCode.PartialContent, resp.StatusCode);
        Assert.Equal("bytes 100-109/256", resp.Content.Headers.ContentRange!.ToString());
        Assert.Equal(bytes[100..110], await resp.Content.ReadAsByteArrayAsync());
    }

    // ── /assets（vendored pdf.js）───────────────────────────────────────────

    [Fact]
    public void assets_白名單檔案_可取得()
    {
        var (api, engine, _) = Make();
        using var _e = engine;
        var resp = api.Handle(Req("GET", "/assets/pdf.min.mjs"));
        Assert.Equal("200 OK", resp.Status);
        Assert.StartsWith("text/javascript", resp.ContentType);
    }

    [Theory]
    [InlineData("/assets/../projections.json")]
    [InlineData("/assets/..%2fprojections.json")]
    [InlineData("/assets/sub/pdf.min.mjs")]
    [InlineData("/assets/..\\secret.txt")]
    [InlineData("/assets/nothere.mjs")]
    [InlineData("/assets/")]
    public void assets_遍歷與不存在_404(string path)
    {
        var (api, engine, _) = Make();
        using var _e = engine;
        Assert.Equal("404 Not Found", api.Handle(Req("GET", path)).Status);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
