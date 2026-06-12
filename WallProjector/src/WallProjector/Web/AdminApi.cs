using System.Reflection;
using System.Text.Json;
using WallProjector.Engine;

namespace WallProjector.Web;

/// <summary>
/// 管理 HTTP API + 管理頁路由。
///
/// 端點：
///   GET  /              管理頁（內嵌 HTML）
///   GET  /wrap/{face}   投影 wrap 頁（瀏覽器殼載入的內容）
///   GET  /api/state     全狀態 JSON（唯讀、不限 loopback——內網監看 OK，同 TouchService GET /config 慣例）
///   POST /api/slot      更新槽設定 {face, url?, rotation?, enabled?}（寫回 projections.json、即時生效）
///   POST /api/start     啟動槽 {face}（設 enabled=true）
///   POST /api/stop      停止槽 {face}（設 enabled=false）
///   POST /api/restore   還原所有 enabled 槽
///   POST /api/reconnect 重新探測 TouchService
///
/// **寫入操作僅 loopback**（同 TouchService 慣例），非 loopback 一律 403。
/// </summary>
public sealed class AdminApi
{
    readonly ProjectionEngine _engine;
    readonly Lazy<byte[]> _adminPage = new(LoadAdminPage);

    public AdminApi(ProjectionEngine engine) => _engine = engine;

    public HttpServer.Response Handle(HttpServer.Request req)
    {
        // wrap 頁
        if (req.Method == "GET" && req.Path.StartsWith("/wrap/", StringComparison.Ordinal))
        {
            return int.TryParse(req.Path["/wrap/".Length..], out int wface)
                ? _engine.WrapPage(wface)
                : HttpServer.Response.NotFound();
        }

        switch (req.Method, req.Path)
        {
            case ("GET", "/") or ("GET", "/index.html"):
                return new HttpServer.Response("200 OK", "text/html; charset=utf-8", _adminPage.Value);

            case ("GET", "/api/state"):
                return HttpServer.Response.Json("200 OK", _engine.StateJson());

            case ("POST", "/api/slot"):
            {
                if (!req.IsLoopback) return Forbidden();
                int face; string? url; int? rotation; bool? enabled;
                try
                {
                    using var doc = JsonDocument.Parse(req.Body);
                    var root = doc.RootElement;
                    face = root.GetProperty("face").GetInt32();
                    url = root.TryGetProperty("url", out var u) ? u.GetString() : null;
                    rotation = root.TryGetProperty("rotation", out var r) ? r.GetInt32() : null;
                    enabled = root.TryGetProperty("enabled", out var e) ? e.GetBoolean() : null;
                }
                catch (Exception ex)
                {
                    return Result("400 Bad Request", false,
                        $"body 需為 {{\"face\":N, \"url\"?, \"rotation\"?, \"enabled\"?}}：{ex.Message}");
                }
                var (ok, msg) = _engine.UpdateSlot(face, url, rotation, enabled);
                return Result(ok ? "200 OK" : "400 Bad Request", ok, msg);
            }

            case ("POST", "/api/start"):
            {
                if (!req.IsLoopback) return Forbidden();
                if (!TryFace(req.Body, out int face, out var err)) return err!;
                var (ok, msg) = _engine.Start(face);
                return Result(ok ? "200 OK" : "400 Bad Request", ok, msg);
            }

            case ("POST", "/api/stop"):
            {
                if (!req.IsLoopback) return Forbidden();
                if (!TryFace(req.Body, out int face, out var err)) return err!;
                var (ok, msg) = _engine.Stop(face);
                return Result(ok ? "200 OK" : "400 Bad Request", ok, msg);
            }

            case ("POST", "/api/restore"):
                if (!req.IsLoopback) return Forbidden();
                return Result("200 OK", true, _engine.RestoreAll());

            case ("POST", "/api/reconnect"):
                if (!req.IsLoopback) return Forbidden();
                return Result("200 OK", true, _engine.Reconnect());

            default:
                return HttpServer.Response.NotFound();
        }
    }

    static bool TryFace(string body, out int face, out HttpServer.Response? error)
    {
        face = -1;
        error = null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            face = doc.RootElement.GetProperty("face").GetInt32();
            return true;
        }
        catch (Exception ex)
        {
            error = Result("400 Bad Request", false, $"body 需為 {{\"face\":N}}：{ex.Message}");
            return false;
        }
    }

    static HttpServer.Response Forbidden() =>
        Result("403 Forbidden", false, "此端點僅接受 loopback（127.0.0.1）來源");

    static HttpServer.Response Result(string status, bool ok, string message) =>
        HttpServer.Response.Json(status, JsonSerializer.Serialize(new { ok, message }));

    static byte[] LoadAdminPage()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var s = asm.GetManifestResourceStream("WallProjector.Web.adminpage.html")
            ?? throw new InvalidOperationException("內嵌管理頁資源遺失（WallProjector.Web.adminpage.html）");
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }
}
