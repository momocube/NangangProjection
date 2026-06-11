using System.Text.Json;
using TouchService.Output;
using TouchService.Ui;

namespace TouchService.Api;

/// <summary>
/// 本機管理 HTTP API（與 WS 同 port；WebSocketHub 的 apiHandler）。
/// **不屬於對外協議**——PROTOCOL.md 的 v1 承諾不涵蓋這裡，UI 設定頁專用。
///
/// 端點：
///   GET  /config        目前生效設定（去敏感：不含 log / 檔案路徑）
///   POST /config        套用設定 patch（驗證全過才套用；熱套用 + 寫回 config.json）
///   GET  /monitors      螢幕清單（Windows EnumDisplayMonitors；非 Windows 空陣列）
///   POST /locks/unlock  解鎖一格（body {"face":2,"col":7,"row":13}；語意同 stdin unlock）
///   POST /locks/save    鎖定寫回 config.json（語意同 stdin savelocks）
///
/// 來源限制：**全部寫入類端點 + /monitors 僅接受 loopback**，非 loopback 一律 403
/// （服務在場館內網跑、WS port 對網段開放，但設定只准本機 UI 改）。
/// </summary>
public sealed class ConfigApi
{
    readonly TouchServiceHost _host;

    public ConfigApi(TouchServiceHost host) => _host = host;

    public WebSocketHub.ApiResponse? Handle(WebSocketHub.ApiRequest req)
    {
        switch (req.Method, req.Path)
        {
            case ("GET", "/config"):
                return WebSocketHub.ApiResponse.Json("200 OK", _host.GetEffectiveConfigJson());

            case ("POST", "/config"):
                if (!req.IsLoopback) return Forbidden();
                var (ok, message) = _host.ApplyConfigPatch(req.Body);
                return Result(ok ? "200 OK" : "400 Bad Request", ok, message);

            case ("GET", "/monitors"):
                if (!req.IsLoopback) return Forbidden();
                return WebSocketHub.ApiResponse.Json("200 OK",
                    JsonSerializer.Serialize(MonitorEnumerator.Enumerate(),
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

            case ("POST", "/locks/unlock"):
                if (!req.IsLoopback) return Forbidden();
                return HandleUnlock(req.Body);

            case ("POST", "/locks/save"):
                if (!req.IsLoopback) return Forbidden();
                var saveMsg = _host.SaveLocks();
                return Result("200 OK", saveMsg.StartsWith("已寫回"), saveMsg);

            default:
                return null;   // 不歸 API 管 → hub 回 404
        }
    }

    WebSocketHub.ApiResponse HandleUnlock(string body)
    {
        int face, col, row;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            face = root.GetProperty("face").GetInt32();
            col = root.GetProperty("col").GetInt32();
            row = root.GetProperty("row").GetInt32();
        }
        catch (Exception ex)
        {
            return Result("400 Bad Request", false, $"body 需為 {{\"face\":N,\"col\":N,\"row\":N}}：{ex.Message}");
        }

        var msg = _host.UnlockCell(face, col, row);
        return Result("200 OK", msg.StartsWith("已解鎖"), msg);
    }

    static WebSocketHub.ApiResponse Forbidden() =>
        Result("403 Forbidden", false, "此端點僅接受 loopback（127.0.0.1）來源");

    static WebSocketHub.ApiResponse Result(string status, bool ok, string message) =>
        WebSocketHub.ApiResponse.Json(status,
            JsonSerializer.Serialize(new { ok, message }));
}
