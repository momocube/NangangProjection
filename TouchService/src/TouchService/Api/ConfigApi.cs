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
///   GET  /history       疑似噪訊排行（?top=50，預設 50；唯讀、不限 loopback——場館內網監看 OK；
///                       v9 起已鎖格不列入排行）
///   GET  /locks         即時鎖定清單（含保存狀態 saved 與鎖後活動 droppedSinceLock /
///                       lastDropAgoMs；唯讀、不限 loopback——與 hello lockedCells 同格式）
///   POST /locks/lock    手動鎖定一格（body {"face":2,"col":7,"row":13}；語意同 stdin lock，來源 manual）
///   POST /locks/unlock  解鎖一格（body {"face":2,"col":7,"row":13}；語意同 stdin unlock）
///   POST /locks/unlockall 解鎖全部（v9；runtime 即時生效、config.json 不動——語意同 stdin unlockall）
///   POST /locks/promote 單格轉正式（v9；body 同 lock——該格 append 進 config lockedCells + 來源改標 config）
///   POST /locks/save    鎖定寫回 config.json（語意同 stdin savelocks；v9 起寫回後來源全部改標 config）
///   POST /command       UI 終端指令（body {"cmd":"history 20"} → {"output":"..."}；
///                       與 stdin 共用 CommandProcessor，輸出文字一致）
///   POST /calibration/start   開始校正工作階段（raw 檢視串流 → loopback WS；15 分鐘自動 stop）
///   POST /calibration/stop    結束校正工作階段
///   GET  /calibration/status  {active, remainingSeconds}
///   POST /calibration/save    兩角校正 → 寫 venues/&lt;名稱&gt;.json + config venueProfile（需重啟生效）
///
/// 來源限制：**全部寫入類端點 + /monitors + /command + /calibration/* 僅接受 loopback**，
/// 非 loopback 一律 403（服務在場館內網跑、WS port 對網段開放，但設定只准本機 UI 改）。
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

            case ("GET", "/history"):
                // 唯讀排行，不限 loopback（與 GET /config 同：場館內網監看 OK）
                return WebSocketHub.ApiResponse.Json("200 OK",
                    _host.HistoryJson(ParseTop(req.Query, defaultTop: 50)));

            case ("GET", "/locks"):
                // 唯讀鎖定清單（含鎖後活動），不限 loopback（與 GET /history 同：內網監看 OK）
                return WebSocketHub.ApiResponse.Json("200 OK", _host.LocksJson());

            case ("POST", "/locks/lock"):
                if (!req.IsLoopback) return Forbidden();
                return HandleLockOp(req.Body, LockOp.Lock);

            case ("POST", "/locks/unlock"):
                if (!req.IsLoopback) return Forbidden();
                return HandleLockOp(req.Body, LockOp.Unlock);

            case ("POST", "/locks/unlockall"):
                if (!req.IsLoopback) return Forbidden();
                var unlockAllMsg = _host.UnlockAllCells();
                return Result("200 OK", true, unlockAllMsg);

            case ("POST", "/locks/promote"):
                if (!req.IsLoopback) return Forbidden();
                return HandleLockOp(req.Body, LockOp.Promote);

            case ("POST", "/locks/save"):
                if (!req.IsLoopback) return Forbidden();
                var saveMsg = _host.SaveLocks();
                return Result("200 OK", saveMsg.StartsWith("已寫回"), saveMsg);

            case ("POST", "/command"):
                // UI 終端：與 stdin 同一個 CommandProcessor，輸出文字一致。僅 loopback。
                if (!req.IsLoopback) return Forbidden();
                return HandleCommand(req.Body);

            // ── 校正精靈（全部僅 loopback：raw 訊號與場域寫檔只准本機 UI）─────
            case ("POST", "/calibration/start"):
                if (!req.IsLoopback) return Forbidden();
                return WebSocketHub.ApiResponse.Json("200 OK", _host.CalibrationStart());

            case ("POST", "/calibration/stop"):
                if (!req.IsLoopback) return Forbidden();
                return WebSocketHub.ApiResponse.Json("200 OK", _host.CalibrationStop());

            case ("GET", "/calibration/status"):
                if (!req.IsLoopback) return Forbidden();
                return WebSocketHub.ApiResponse.Json("200 OK", _host.CalibrationStatusJson());

            case ("POST", "/calibration/save"):
                if (!req.IsLoopback) return Forbidden();
                var (saveOk, saveMessage) = _host.SaveCalibration(req.Body);
                return Result(saveOk ? "200 OK" : "400 Bad Request", saveOk, saveMessage);

            // ── Venue 切換(UI 網頁下拉+按鈕觸發;僅 loopback)──────────────
            case ("GET", "/venues"):
                return WebSocketHub.ApiResponse.Json("200 OK", BuildVenuesJson());

            case ("POST", "/venue/switch"):
                if (!req.IsLoopback) return Forbidden();
                return HandleVenueSwitch(req.Body);

            default:
                return null;   // 不歸 API 管 → hub 回 404
        }
    }

    string BuildVenuesJson()
    {
        var venues = _host.ListAvailableVenues();
        var current = _host.CurrentVenueRelPath;
        var items = venues.Select(v => new
        {
            path = v.RelativePath,
            name = v.VenueName,
            current = string.Equals(v.RelativePath, current, StringComparison.OrdinalIgnoreCase),
        }).ToArray();
        return JsonSerializer.Serialize(new { currentPath = current, venues = items }, ResultJsonOptions);
    }

    WebSocketHub.ApiResponse HandleVenueSwitch(string body)
    {
        string? path;
        try
        {
            using var doc = JsonDocument.Parse(body);
            path = doc.RootElement.GetProperty("path").GetString();
        }
        catch (Exception ex)
        {
            return Result("400 Bad Request", false, $"body 需為 {{\"path\":\"venues/xxx.json\"}}:{ex.Message}");
        }
        if (string.IsNullOrWhiteSpace(path))
            return Result("400 Bad Request", false, "path 不可為空");

        // 只接受掃到的清單裡的 relative path,擋 path traversal
        var venues = _host.ListAvailableVenues();
        var match = venues.FirstOrDefault(v => string.Equals(v.RelativePath, path, StringComparison.OrdinalIgnoreCase));
        if (match == null)
            return Result("400 Bad Request", false, $"venue 不存在或不合法:{path}");

        if (string.Equals(match.RelativePath, _host.CurrentVenueRelPath, StringComparison.OrdinalIgnoreCase))
            return Result("200 OK", true, $"venue 已經是 {match.VenueName},不需切換");

        _host.RequestVenueSwitch(match.RelativePath);
        return Result("200 OK", true, $"已請求切換到 {match.VenueName}({match.RelativePath}),服務即將重啟…");
    }

    enum LockOp { Lock, Unlock, Promote }

    /// <summary>POST /locks/lock、/locks/unlock、/locks/promote 共用（body 同格式 {face,col,row}）。</summary>
    WebSocketHub.ApiResponse HandleLockOp(string body, LockOp op)
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

        switch (op)
        {
            case LockOp.Promote:
                var (ok, message) = _host.PromoteLock(face, col, row);
                return Result("200 OK", ok, message);
            case LockOp.Unlock:
                var unlockMsg = _host.UnlockCell(face, col, row);
                return Result("200 OK", unlockMsg.StartsWith("已解鎖"), unlockMsg);
            default:
                var lockMsg = _host.LockCell(face, col, row);
                return Result("200 OK", lockMsg.StartsWith("已鎖定"), lockMsg);
        }
    }

    /// <summary>POST /command：body {"cmd":"..."} → {"output":"..."}（CommandProcessor 與 stdin 共用）。</summary>
    WebSocketHub.ApiResponse HandleCommand(string body)
    {
        string cmd;
        try
        {
            using var doc = JsonDocument.Parse(body);
            cmd = doc.RootElement.GetProperty("cmd").GetString()
                  ?? throw new JsonException("cmd 必須是字串");
        }
        catch (Exception ex)
        {
            return Result("400 Bad Request", false, $"body 需為 {{\"cmd\":\"指令\"}}：{ex.Message}");
        }

        var output = _host.Commands.Execute(cmd);
        return WebSocketHub.ApiResponse.Json("200 OK", JsonSerializer.Serialize(new { output }));
    }

    /// <summary>解析 query 的 top 參數（缺省 / 非法 → defaultTop；上限 500 防整包傾倒）。</summary>
    static int ParseTop(string query, int defaultTop)
    {
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = pair.IndexOf('=');
            if (eq <= 0 || !pair.AsSpan(0, eq).Equals("top", StringComparison.OrdinalIgnoreCase)) continue;
            if (int.TryParse(pair.AsSpan(eq + 1), out int v) && v > 0) return Math.Min(v, 500);
        }
        return defaultTop;
    }

    static WebSocketHub.ApiResponse Forbidden() =>
        Result("403 Forbidden", false, "此端點僅接受 loopback（127.0.0.1）來源");

    /// <summary>回應 JSON 不轉義非 ASCII（訊息是給 UI 直接顯示的中文；純 application/json，無 HTML 嵌入疑慮）。</summary>
    static readonly JsonSerializerOptions ResultJsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static WebSocketHub.ApiResponse Result(string status, bool ok, string message) =>
        WebSocketHub.ApiResponse.Json(status,
            JsonSerializer.Serialize(new { ok, message }, ResultJsonOptions));
}
