using System.Text.Json;
using System.Text.Json.Nodes;
using TouchService.Core;

namespace TouchService.Calibration;

/// <summary>
/// 校正精靈的 save 請求：場域名稱 + 各面兩角 raw 取樣（UI 引導踩出來的值）。
/// canvas 缺省 = 各面 bounding box 自動計算（單面 origin 0,0 = 面即畫布）。
/// </summary>
public sealed class CalibrationSaveRequest
{
    public string Venue { get; set; } = "";
    public CalibrationCanvasRequest? Canvas { get; set; }
    public List<CalibrationFaceRequest> Faces { get; set; } = new();
}

public sealed class CalibrationCanvasRequest
{
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// 單面校正資料：格數 / 方向 / 畫布 origin + 兩角 raw 值
/// （topLeftRaw = 踩「左上角第一顆晶片」收到的 raw、bottomRightRaw = 「右下角最後一顆」）。
/// </summary>
public sealed class CalibrationFaceRequest
{
    public string Name { get; set; } = "";
    public int Cols { get; set; }
    public int Rows { get; set; }
    public string Orientation { get; set; } = "vertical";
    public int OriginX { get; set; }
    public int OriginY { get; set; }

    /// <summary>[rawX, rawY]：左上角第一顆晶片的 raw 取樣值</summary>
    public int[]? TopLeftRaw { get; set; }

    /// <summary>[rawX, rawY]：右下角最後一顆晶片的 raw 取樣值</summary>
    public int[]? BottomRightRaw { get; set; }
}

/// <summary>
/// 校正精靈的純數學（無 IO、可單元測試）：
///   兩角 raw 取樣 → rawMin/Max + flip（CALIBRATION.md 語意：rawMin/Max =
///   第一顆 / 最後一顆晶片的中心 raw 值；角落 raw 大小關係反向 = 感應方向
///   與畫布方向相反 = flip）→ 組 VenueProfile → 序列化成 venue JSON。
/// </summary>
public static class CalibrationMath
{
    /// <summary>cols/rows 上限（防呆；704 最大面 44×128，留充足餘裕）。</summary>
    public const int MaxColsRows = 1024;

    /// <summary>raw 座標上限（封包是 2 byte big-endian）。</summary>
    public const int MaxRaw = 65535;

    /// <summary>origin 上限（畫布像素；防呆用，遠大於任何實際場域）。</summary>
    public const int MaxOrigin = 1_000_000;

    /// <summary>
    /// 單軸：第一顆晶片中心 raw（左上）+ 最後一顆中心 raw（右下）→ rawMin/Max + flip。
    /// first &lt; last = 感應方向與畫布同向（flip=false）；first &gt; last = 反向（flip=true）。
    /// 兩值相等 → 無法定義範圍，回傳 false（rawMin 必須 ≠ rawMax）。
    /// </summary>
    public static bool TryComputeAxis(int firstRaw, int lastRaw, out int rawMin, out int rawMax, out bool flip)
    {
        if (firstRaw == lastRaw)
        {
            rawMin = rawMax = firstRaw;
            flip = false;
            return false;
        }
        flip = firstRaw > lastRaw;
        rawMin = Math.Min(firstRaw, lastRaw);
        rawMax = Math.Max(firstRaw, lastRaw);
        return true;
    }

    /// <summary>
    /// 場域名稱合法性（檔名安全）：非空、≤ 64 字、無路徑字元 / 控制字元、不是 "." ".."。
    /// 回傳 null = 合法，否則錯誤訊息。
    /// </summary>
    public static string? ValidateVenueName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "場域名稱不可為空";
        if (name.Length > 64) return "場域名稱過長（≤ 64 字）";
        if (name is "." or "..") return "場域名稱不可為 . 或 ..";
        foreach (var c in name)
        {
            if (char.IsControl(c) || c is '/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|')
                return $"場域名稱含非法字元「{(char.IsControl(c) ? "控制字元" : c.ToString())}」（不可含 / \\ : * ? \" < > |）";
        }
        return null;
    }

    /// <summary>
    /// save 請求 → VenueProfile。全部驗證通過才回傳 profile（errors 為空）；
    /// 任一錯誤 → profile 為 null、errors 列出全部問題（不留半套）。
    /// canvas 缺省 = 各面 bounding box（單面 origin 0,0 = 面即畫布）；
    /// 有給時必須 ≥ bounding box（面不可超出畫布）。
    /// </summary>
    public static (VenueProfile? Profile, List<string> Errors) BuildVenue(CalibrationSaveRequest req)
    {
        var errors = new List<string>();

        if (ValidateVenueName(req.Venue) is { } nameError) errors.Add(nameError);
        if (req.Faces.Count == 0) errors.Add("faces 至少要有一面");

        var faces = new List<SensorFaceConfig>();
        for (int i = 0; i < req.Faces.Count; i++)
        {
            var f = req.Faces[i];
            string tag = $"faces[{i}]";

            if (string.IsNullOrWhiteSpace(f.Name)) errors.Add($"{tag} 面名稱不可為空");
            if (f.Cols is < 1 or > MaxColsRows) errors.Add($"{tag} cols 必須是 1~{MaxColsRows}（收到 {f.Cols}）");
            if (f.Rows is < 1 or > MaxColsRows) errors.Add($"{tag} rows 必須是 1~{MaxColsRows}（收到 {f.Rows}）");
            if (f.OriginX is < 0 or > MaxOrigin) errors.Add($"{tag} originX 必須是 0~{MaxOrigin}（收到 {f.OriginX}）");
            if (f.OriginY is < 0 or > MaxOrigin) errors.Add($"{tag} originY 必須是 0~{MaxOrigin}（收到 {f.OriginY}）");

            ChipOrientation orientation = default;
            switch (f.Orientation?.Trim().ToLowerInvariant())
            {
                case "vertical": orientation = ChipOrientation.Vertical; break;
                case "horizontal": orientation = ChipOrientation.Horizontal; break;
                default: errors.Add($"{tag} orientation 必須是 \"vertical\" 或 \"horizontal\"（收到 \"{f.Orientation}\"）"); break;
            }

            if (!TryReadCorner(f.TopLeftRaw, out int tlX, out int tlY))
                errors.Add($"{tag} topLeftRaw 必須是 [rawX, rawY]（0~{MaxRaw}）");
            if (!TryReadCorner(f.BottomRightRaw, out int brX, out int brY))
                errors.Add($"{tag} bottomRightRaw 必須是 [rawX, rawY]（0~{MaxRaw}）");

            if (errors.Count > 0) continue;   // 此面基本欄位有錯，後面的軸計算不做

            bool okX = TryComputeAxis(tlX, brX, out int rawMinX, out int rawMaxX, out bool flipX);
            bool okY = TryComputeAxis(tlY, brY, out int rawMinY, out int rawMaxY, out bool flipY);
            if (!okX) errors.Add($"{tag} 兩角的 rawX 相同（{tlX}）——rawMinX 必須 ≠ rawMaxX，請確認兩角踩的是不同晶片");
            if (!okY) errors.Add($"{tag} 兩角的 rawY 相同（{tlY}）——rawMinY 必須 ≠ rawMaxY，請確認兩角踩的是不同晶片");
            if (!okX || !okY) continue;

            faces.Add(new SensorFaceConfig
            {
                Name = f.Name.Trim(),
                OriginX = f.OriginX,
                OriginY = f.OriginY,
                Orientation = orientation,
                ColCount = f.Cols,
                RowCount = f.Rows,
                RawMinX = rawMinX,
                RawMaxX = rawMaxX,
                RawMinY = rawMinY,
                RawMaxY = rawMaxY,
                FlipX = flipX,
                FlipY = flipY,
            });
        }

        if (errors.Count > 0) return (null, errors);

        // canvas：缺省 = bounding box；有給必須 ≥ bounding box
        int needW = faces.Max(f => f.OriginX + f.TotalPixelWidth);
        int needH = faces.Max(f => f.OriginY + f.TotalPixelHeight);
        int canvasW = needW, canvasH = needH;
        if (req.Canvas != null)
        {
            if (req.Canvas.Width < needW || req.Canvas.Height < needH)
            {
                errors.Add($"canvas {req.Canvas.Width}×{req.Canvas.Height} 小於各面 bounding box {needW}×{needH}（面不可超出畫布）");
                return (null, errors);
            }
            canvasW = req.Canvas.Width;
            canvasH = req.Canvas.Height;
        }

        return (new VenueProfile
        {
            Venue = req.Venue.Trim(),
            Canvas = new CanvasSize { Width = canvasW, Height = canvasH },
            Faces = faces,
        }, errors);
    }

    static bool TryReadCorner(int[]? corner, out int x, out int y)
    {
        x = y = 0;
        if (corner is not { Length: 2 }) return false;
        if (corner[0] is < 0 or > MaxRaw || corner[1] is < 0 or > MaxRaw) return false;
        x = corner[0];
        y = corner[1];
        return true;
    }

    /// <summary>
    /// VenueProfile → venue JSON 字串（與 venues/704.json 同格式：camelCase、
    /// orientation 小寫、2 空格縮排）。手組 JsonNode 確保欄位順序 / 大小寫穩定
    /// （golden 測試直接比對字串）。
    /// </summary>
    public static string SerializeVenueJson(VenueProfile profile)
    {
        var faces = new JsonArray();
        foreach (var f in profile.Faces)
        {
            faces.Add(new JsonObject
            {
                ["name"] = f.Name,
                ["originX"] = f.OriginX,
                ["originY"] = f.OriginY,
                ["orientation"] = f.Orientation == ChipOrientation.Vertical ? "vertical" : "horizontal",
                ["colCount"] = f.ColCount,
                ["rowCount"] = f.RowCount,
                ["rawMinX"] = f.RawMinX,
                ["rawMaxX"] = f.RawMaxX,
                ["rawMinY"] = f.RawMinY,
                ["rawMaxY"] = f.RawMaxY,
                ["flipX"] = f.FlipX,
                ["flipY"] = f.FlipY,
            });
        }
        var root = new JsonObject
        {
            ["venue"] = profile.Venue,
            ["canvas"] = new JsonObject
            {
                ["width"] = profile.Canvas.Width,
                ["height"] = profile.Canvas.Height,
            },
            ["faces"] = faces,
        };
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
