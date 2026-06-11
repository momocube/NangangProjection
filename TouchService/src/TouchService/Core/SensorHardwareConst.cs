namespace TouchService.Core;

/// <summary>
/// 感應硬體的物理常數。
/// 移植自 Unity 專案 Assets/_Script/Core/SensorHardwareConst.cs。
/// 除非更換設備，否則不需要修改這裡的數值。
/// </summary>
public static class SensorHardwareConst
{
    /// <summary>每個晶片格的像素寬度（橫向安裝時，px）</summary>
    public const int CellPixelWidth = 16;

    /// <summary>每個晶片格的像素高度（橫向安裝時，px）</summary>
    public const int CellPixelHeight = 32;

    /// <summary>
    /// UDP 感應器同點重送週期實測值（ms）。
    /// 2026-06-10 南港現場 Wireshark 量測（閥值 700/1）：p50 = 81.7ms、主峰 80ms。
    /// 注意 gap 分佈有長尾（120-160ms 約佔 >10%），release timeout 不可低於 170ms，
    /// 詳見 docs/PROTOCOL.md「Release timeout 取值依據」。
    /// </summary>
    public const int MeasuredResendIntervalMs = 80;
}
