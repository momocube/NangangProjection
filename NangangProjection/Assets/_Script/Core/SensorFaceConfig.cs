using UnityEngine;

/// <summary>
/// 每個感應面的設定資料（ScriptableObject）。
/// 建立方式：Project 視窗右鍵 → Create → NangangProjection → SensorFaceConfig
/// </summary>
[CreateAssetMenu(fileName = "SensorFaceConfig", menuName = "NangangProjection/SensorFaceConfig")]
public class SensorFaceConfig : ScriptableObject
{
    /// <summary>
    /// 晶片安裝方向。影響每格的像素佔位尺寸：
    /// 橫：每格 16×32 px（寬×高）
    /// 直：每格 32×16 px（寬×高）
    /// colCount/rowCount 永遠代表畫面上的 X/Y 格數，與方向無關。
    /// </summary>
    public enum ChipOrientation { 橫, 直 }

    [Header("識別")]
    public string faceName = "Face";
    public Color gizmoColor = Color.green;
    public bool showInVisualizer = true;

    [Header("畫面位置（像素，此面左上角在合併畫布的位置）")]
    public int originX;
    public int originY;

    [Header("晶片安裝方向")]
    public ChipOrientation orientation = ChipOrientation.橫;

    [Header("畫面格數（X = 左右欄數，Y = 上下列數）")]
    public int colCount = 128;  // 畫面 X 方向格數
    public int rowCount = 24;   // 畫面 Y 方向格數

    [Header("感應原始值校準（Editor 工具 / 備查用；不影響感應輸入，感應輸入永遠是畫布絕對像素）")]
    public int rawMinX;
    public int rawMaxX;
    public int rawMinY;
    public int rawMaxY;
    public bool flipX;
    public bool flipY;

    // ──────────────────────────────────────────
    // 畫面像素佔位
    // 橫：每格 cellPixelWidth(16) × cellPixelHeight(32)
    // 直：每格 cellPixelHeight(32) × cellPixelWidth(16)（晶片躺倒，寬高對調）
    // ──────────────────────────────────────────

    int ScreenCellW => orientation == ChipOrientation.橫
        ? SensorHardwareConst.CellPixelWidth
        : SensorHardwareConst.CellPixelHeight;

    int ScreenCellH => orientation == ChipOrientation.橫
        ? SensorHardwareConst.CellPixelHeight
        : SensorHardwareConst.CellPixelWidth;

    public int TotalPixelWidth  => colCount * ScreenCellW;
    public int TotalPixelHeight => rowCount * ScreenCellH;

    // 供 FaceVisualizer 取用
    public int GizmoCellW => ScreenCellW;
    public int GizmoCellH => ScreenCellH;

    // ──────────────────────────────────────────
    // Raw sensor 座標 → 畫布像素座標
    // rawMinX < rawMaxX：校準模式，用 rawMin/Max 正規化後映射到畫布
    // 否則：直接把 raw 值當畫布像素座標（無校準模式）
    // ──────────────────────────────────────────

    public bool HasRawCalibration => rawMinX < rawMaxX && rawMinY < rawMaxY;

    /// <summary>raw sensor 座標是否落在此面感應範圍（校準模式）或畫布像素範圍（直接模式）</summary>
    public bool ContainsRaw(int rawX, int rawY)
    {
        if (!HasRawCalibration) return ContainsPixel(rawX, rawY);
        return rawX >= rawMinX && rawX <= rawMaxX &&
               rawY >= rawMinY && rawY <= rawMaxY;
    }

    /// <summary>
    /// raw sensor 座標 → 畫布像素座標。<br/>
    /// 直向（orientation=直）時 rawX→canvasY、rawY→canvasX。<br/>
    /// flipX/flipY 翻轉對應軸的正規化值。
    /// </summary>
    public (int pixelX, int pixelY) RawToPixel(int rawX, int rawY)
    {
        if (!HasRawCalibration) return (rawX, rawY);

        float nx = Mathf.Clamp01((float)(rawX - rawMinX) / (rawMaxX - rawMinX));
        float ny = Mathf.Clamp01((float)(rawY - rawMinY) / (rawMaxY - rawMinY));
        if (flipX) nx = 1f - nx;
        if (flipY) ny = 1f - ny;

        // rawX 永遠對應 canvasX，rawY 永遠對應 canvasY
        // orientation 只影響視覺格子尺寸（ScreenCellW/H），不影響 raw sensor 軸映射
        int px = originX + Mathf.RoundToInt(nx * TotalPixelWidth);
        int py = originY + Mathf.RoundToInt(ny * TotalPixelHeight);
        return (px, py);
    }

    // ──────────────────────────────────────────
    // 畫布像素座標 → 格子座標
    // ──────────────────────────────────────────

    /// <summary>畫布像素座標是否落在此面的範圍內</summary>
    public bool ContainsPixel(int pixelX, int pixelY)
    {
        return pixelX >= originX && pixelX < originX + TotalPixelWidth &&
               pixelY >= originY && pixelY < originY + TotalPixelHeight;
    }

    /// <summary>畫布像素座標 → 格子座標（col = 畫面 X，row = 畫面 Y）</summary>
    public (int col, int row) PixelToCell(int pixelX, int pixelY)
    {
        int col = Mathf.Clamp((pixelX - originX) / ScreenCellW, 0, colCount - 1);
        int row = Mathf.Clamp((pixelY - originY) / ScreenCellH, 0, rowCount - 1);
        return (col, row);
    }

    // ──────────────────────────────────────────
    // 格子 → 畫布像素座標
    // ──────────────────────────────────────────

    /// <summary>格子左上角的像素座標（合併畫布座標系）</summary>
    public Vector2Int CellToPixel(int col, int row)
    {
        return new Vector2Int(
            originX + col * ScreenCellW,
            originY + row * ScreenCellH
        );
    }
}
