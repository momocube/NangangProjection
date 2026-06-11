using System.Text.Json.Serialization;

namespace TouchService.Core;

/// <summary>晶片安裝方向。橫：每格 16×32 px；直：每格 32×16 px（晶片躺倒，寬高對調）。</summary>
public enum ChipOrientation
{
    /// <summary>橫（Unity enum 值 0）</summary>
    Horizontal = 0,
    /// <summary>直（Unity enum 值 1）</summary>
    Vertical = 1,
}

/// <summary>
/// 單一感應面配置＋座標數學。
/// 移植自 Unity 專案 Assets/_Script/Core/SensorFaceConfig.cs（ScriptableObject → POCO+JSON）。
/// 座標公式（ContainsRaw / RawToPixel / PixelToCell / CellToPixel）與 Unity 端逐行對拍，
/// 含 float 正規化與 round-half-to-even（Mathf.RoundToInt 語意）。
/// </summary>
public sealed class SensorFaceConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Face";

    /// <summary>此面左上角在合併畫布的位置（px）</summary>
    public int OriginX { get; set; }
    public int OriginY { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<ChipOrientation>))]
    public ChipOrientation Orientation { get; set; } = ChipOrientation.Horizontal;

    /// <summary>畫面 X 方向格數（與安裝方向無關）</summary>
    public int ColCount { get; set; }
    /// <summary>畫面 Y 方向格數</summary>
    public int RowCount { get; set; }

    /// <summary>感應原始值校準範圍（格子中心像素，含 +1920 X 偏移；見 docs/CALIBRATION.md）</summary>
    public int RawMinX { get; set; }
    public int RawMaxX { get; set; }
    public int RawMinY { get; set; }
    public int RawMaxY { get; set; }

    /// <summary>感應器物理方向與畫布方向相反時翻轉</summary>
    public bool FlipX { get; set; }
    public bool FlipY { get; set; }

    // ──────────────────────────────────────────
    // 畫面像素佔位
    // 橫：每格 CellPixelWidth(16) × CellPixelHeight(32)
    // 直：每格 CellPixelHeight(32) × CellPixelWidth(16)
    // ──────────────────────────────────────────

    [JsonIgnore]
    public int ScreenCellW => Orientation == ChipOrientation.Horizontal
        ? SensorHardwareConst.CellPixelWidth
        : SensorHardwareConst.CellPixelHeight;

    [JsonIgnore]
    public int ScreenCellH => Orientation == ChipOrientation.Horizontal
        ? SensorHardwareConst.CellPixelHeight
        : SensorHardwareConst.CellPixelWidth;

    [JsonIgnore] public int TotalPixelWidth  => ColCount * ScreenCellW;
    [JsonIgnore] public int TotalPixelHeight => RowCount * ScreenCellH;

    // ──────────────────────────────────────────
    // Raw sensor 座標 → 畫布像素座標
    // rawMin < rawMax：校準模式，用 rawMin/Max 正規化後映射到畫布
    // 否則：直接把 raw 值當畫布像素座標（無校準模式）
    // ──────────────────────────────────────────

    [JsonIgnore]
    public bool HasRawCalibration => RawMinX < RawMaxX && RawMinY < RawMaxY;

    /// <summary>raw sensor 座標是否落在此面感應範圍（校準模式）或畫布像素範圍（直接模式）</summary>
    public bool ContainsRaw(int rawX, int rawY)
    {
        if (!HasRawCalibration) return ContainsPixel(rawX, rawY);
        return rawX >= RawMinX && rawX <= RawMaxX &&
               rawY >= RawMinY && rawY <= RawMaxY;
    }

    /// <summary>
    /// raw sensor 座標 → 畫布像素座標。
    /// rawX 永遠對應 canvasX、rawY 永遠對應 canvasY；
    /// orientation 只影響格子尺寸（ScreenCellW/H），不影響 raw 軸映射。
    /// flipX/flipY 翻轉對應軸的正規化值。
    /// </summary>
    public (int PixelX, int PixelY) RawToPixel(int rawX, int rawY)
    {
        if (!HasRawCalibration) return (rawX, rawY);

        float nx = Clamp01((float)(rawX - RawMinX) / (RawMaxX - RawMinX));
        float ny = Clamp01((float)(rawY - RawMinY) / (RawMaxY - RawMinY));
        if (FlipX) nx = 1f - nx;
        if (FlipY) ny = 1f - ny;

        int px = OriginX + RoundToInt(nx * TotalPixelWidth);
        int py = OriginY + RoundToInt(ny * TotalPixelHeight);
        return (px, py);
    }

    // ──────────────────────────────────────────
    // 畫布像素座標 → 格子座標
    // ──────────────────────────────────────────

    /// <summary>畫布像素座標是否落在此面的範圍內</summary>
    public bool ContainsPixel(int pixelX, int pixelY)
    {
        return pixelX >= OriginX && pixelX < OriginX + TotalPixelWidth &&
               pixelY >= OriginY && pixelY < OriginY + TotalPixelHeight;
    }

    /// <summary>畫布像素座標 → 格子座標（col = 畫面 X，row = 畫面 Y）</summary>
    public (int Col, int Row) PixelToCell(int pixelX, int pixelY)
    {
        int col = Math.Clamp((pixelX - OriginX) / ScreenCellW, 0, ColCount - 1);
        int row = Math.Clamp((pixelY - OriginY) / ScreenCellH, 0, RowCount - 1);
        return (col, row);
    }

    /// <summary>格子左上角的像素座標（合併畫布座標系）</summary>
    public (int X, int Y) CellToPixel(int col, int row)
    {
        return (OriginX + col * ScreenCellW,
                OriginY + row * ScreenCellH);
    }

    // ── Unity Mathf 對拍 helper ─────────────────────────────────────────────

    /// <summary>Unity Mathf.Clamp01 等價。</summary>
    static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

    /// <summary>Unity Mathf.RoundToInt 等價：round half to even（banker's rounding）。</summary>
    static int RoundToInt(float v) => (int)MathF.Round(v);
}
