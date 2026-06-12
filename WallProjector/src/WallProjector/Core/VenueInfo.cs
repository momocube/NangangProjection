using System.Text.Json;
using System.Text.Json.Serialization;

namespace WallProjector.Core;

/// <summary>
/// TouchService GET /venue 回應的最小投影視角：畫布尺寸 + 各面在畫布上的幾何。
/// raw 校準值（rawMin/Max、flip）是輸入層的事，投影工具不需要——只解 origin 與格數。
///
/// 面的畫布像素尺寸不在 /venue JSON 裡（TouchService 端是計算屬性），
/// 公式與 TouchService SensorFaceConfig 對拍：
///   horizontal：每格 16(寬)×32(高) px；vertical：每格 32×16（晶片躺倒，寬高對調）。
/// </summary>
public sealed class VenueInfo
{
    public string Venue { get; set; } = "";
    public CanvasSize Canvas { get; set; } = new();
    public List<FaceInfo> Faces { get; set; } = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static VenueInfo Parse(string json)
    {
        var v = JsonSerializer.Deserialize<VenueInfo>(json, JsonOptions)
            ?? throw new InvalidDataException("venue JSON 解析失敗");
        if (v.Canvas.Width <= 0 || v.Canvas.Height <= 0)
            throw new InvalidDataException("venue canvas 尺寸非法");
        if (v.Faces.Count == 0)
            throw new InvalidDataException("venue 沒有任何面");
        return v;
    }

    /// <summary>地板面 index：名稱含 Floor（與 TouchService mouseMode 預設排除邏輯同源）；
    /// 找不到時退而求其次取面積最大者（十字展開圖中央通常最大）。</summary>
    public int FloorIndex()
    {
        for (int i = 0; i < Faces.Count; i++)
            if (Faces[i].IsFloor) return i;

        int best = 0;
        long bestArea = -1;
        for (int i = 0; i < Faces.Count; i++)
        {
            long area = (long)Faces[i].PixelWidth * Faces[i].PixelHeight;
            if (area > bestArea) { bestArea = area; best = i; }
        }
        return best;
    }

    /// <summary>
    /// 旋轉預設值的合理猜測（UI 預設用，**以現場實看為準**）：
    /// 十字展開圖把牆臂攤平在地板四周——臂上內容的「上」應指向「離地板遠的方向」，
    /// 人站場內看牆才是正的。以面中心相對地板面中心的方位分類：
    ///   左臂 → 270（逆時針 90，內容上緣轉向畫布 -x）、右臂 → 90、
    ///   上臂 → 0（畫布 -y 本來就是內容的上）、下臂 → 180、地板 → 0。
    /// </summary>
    public int GuessRotation(int faceIndex)
    {
        if (faceIndex < 0 || faceIndex >= Faces.Count) return 0;
        int floor = FloorIndex();
        if (faceIndex == floor) return 0;

        var f = Faces[faceIndex];
        var fl = Faces[floor];
        double dx = (f.OriginX + f.PixelWidth / 2.0) - (fl.OriginX + fl.PixelWidth / 2.0);
        double dy = (f.OriginY + f.PixelHeight / 2.0) - (fl.OriginY + fl.PixelHeight / 2.0);

        if (Math.Abs(dx) >= Math.Abs(dy))
            return dx < 0 ? 270 : 90;     // 左臂 / 右臂
        return dy < 0 ? 0 : 180;          // 上臂 / 下臂
    }
}

public sealed class CanvasSize
{
    public int Width { get; set; }
    public int Height { get; set; }
}

public sealed class FaceInfo
{
    public string Name { get; set; } = "Face";
    public int OriginX { get; set; }
    public int OriginY { get; set; }

    /// <summary>"horizontal" / "vertical"（/venue 以 camelCase 字串列舉序列化）</summary>
    public string Orientation { get; set; } = "horizontal";

    public int ColCount { get; set; }
    public int RowCount { get; set; }

    [JsonIgnore]
    public bool IsVertical =>
        string.Equals(Orientation, "vertical", StringComparison.OrdinalIgnoreCase);

    /// <summary>每格畫布像素寬（橫 16 / 直 32）——對拍 TouchService SensorFaceConfig.ScreenCellW</summary>
    [JsonIgnore] public int CellW => IsVertical ? 32 : 16;
    /// <summary>每格畫布像素高（橫 32 / 直 16）</summary>
    [JsonIgnore] public int CellH => IsVertical ? 16 : 32;

    [JsonIgnore] public int PixelWidth => ColCount * CellW;
    [JsonIgnore] public int PixelHeight => RowCount * CellH;

    [JsonIgnore]
    public bool IsFloor => Name.Contains("floor", StringComparison.OrdinalIgnoreCase);
}
