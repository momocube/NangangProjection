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
    // 畫布像素座標 → 格子座標
    // rawX/rawY 直接是第二螢幕的畫布像素座標
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
