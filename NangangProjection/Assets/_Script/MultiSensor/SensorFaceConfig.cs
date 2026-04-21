using UnityEngine;

/// <summary>
/// 每個感應面的設定資料（ScriptableObject）。
/// 建立方式：Project 視窗右鍵 → Create → NangangProjection → SensorFaceConfig
/// </summary>
[CreateAssetMenu(fileName = "SensorFaceConfig", menuName = "NangangProjection/SensorFaceConfig")]
public class SensorFaceConfig : ScriptableObject
{
    /// <summary>
    /// 晶片安裝方向。
    /// 橫：晶片實體橫放，rawX 對應畫面 X 軸（欄），rawY 對應畫面 Y 軸（列）。
    /// 直：晶片實體直放，rawX 對應畫面 Y 軸（列），rawY 對應畫面 X 軸（欄）。
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

    [Header("原始感應值範圍（由 VenuePresetConfig 畫布校準值自動算出，不需手動填寫）")]
    [HideInInspector] public float rawMinX;
    [HideInInspector] public float rawMaxX = 4095;
    [HideInInspector] public float rawMinY;
    [HideInInspector] public float rawMaxY = 4095;

    [Header("座標翻轉（方向裝反時勾選）")]
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
    // 原始感應值 → 格子座標
    // ──────────────────────────────────────────

    /// <summary>換算原始感應值 → 格子座標（col = 畫面 X，row = 畫面 Y）</summary>
    public (int col, int row) RawToCell(float rawX, float rawY)
        => RawToCell(rawX, rawY, rawMinX, rawMaxX, rawMinY, rawMaxY);

    /// <summary>換算原始感應值 → 格子座標，使用外部傳入的有效範圍（由 VenuePresetConfig 畫布校準值計算）</summary>
    public (int col, int row) RawToCell(float rawX, float rawY,
        float minX, float maxX, float minY, float maxY)
    {
        float nx = Mathf.Clamp01((rawX - minX) / (maxX - minX));
        float ny = Mathf.Clamp01((rawY - minY) / (maxY - minY));

        if (flipX) nx = 1f - nx;
        if (flipY) ny = 1f - ny;

        int col, row;
        if (orientation == ChipOrientation.橫)
        {
            // rawX → 畫面 X（欄），rawY → 畫面 Y（列）
            col = Mathf.Clamp(Mathf.FloorToInt(nx * colCount), 0, colCount - 1);
            row = Mathf.Clamp(Mathf.FloorToInt(ny * rowCount), 0, rowCount - 1);
        }
        else
        {
            // rawX → 畫面 Y（列），rawY → 畫面 X（欄）
            col = Mathf.Clamp(Mathf.FloorToInt(ny * colCount), 0, colCount - 1);
            row = Mathf.Clamp(Mathf.FloorToInt(nx * rowCount), 0, rowCount - 1);
        }
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
