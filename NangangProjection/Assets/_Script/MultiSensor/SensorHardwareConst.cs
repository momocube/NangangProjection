/// <summary>
/// 感應硬體的物理常數。
/// 除非更換設備，否則不需要修改這裡的數值。
/// 128 欄 × 24 列，每格 16×32 px。
/// </summary>
public static class SensorHardwareConst
{
    /// <summary>每個晶片格的像素寬度（橫向，px）</summary>
    public const int CellPixelWidth = 16;

    /// <summary>每個晶片格的像素高度（縱向，px）</summary>
    public const int CellPixelHeight = 32;
}
