using System.Runtime.InteropServices;

namespace WallProjector.Core;

/// <summary>
/// Per-Monitor V2 DPI awareness 宣告——**必須在任何視窗/螢幕 API 之前**呼叫
/// （process 級、一次性，Main 第一行）。
///
/// 為什麼：沒宣告時本 process 是 DPI-unaware，投影輸出螢幕 Windows 縮放 ≠ 100%
/// （4K 輸出預設 150~300% 很常見）時，EnumWindows / SetWindowPos / GetWindowRect
/// 的座標全被 DPI 虛擬化——視窗實際落點與尺寸 = 設定值 × 縮放倍數，造成「等比例
/// 錯位」（704 現場回報：本該貼滿牆的視窗變成牆一半、地板一半）。同機的
/// TouchService 滑鼠模式用 SendInput 0–65535 正規化座標、不經 DPI 虛擬化，所以
/// 滑鼠對位準、視窗對位歪——symptom 與此一致。宣告 PMv2 後本 process 拿到的
/// 全是物理像素，SetWindowPos 不再被虛擬化。
///
/// 三層 fallback（新 → 舊，越上面越準）：
/// 1. SetProcessDpiAwarenessContext(PER_MONITOR_AWARE_V2)（user32，Win10 1703+）
/// 2. SetProcessDpiAwareness(PROCESS_PER_MONITOR_DPI_AWARE)（shcore，Win8.1+）
/// 3. SetProcessDPIAware()（user32，Vista+——system-aware，多螢幕異縮放仍會歪，聊勝於無）
/// </summary>
public static class DpiAwareness
{
    // DPI_AWARENESS_CONTEXT 是 pseudo-handle（負整數），-4 = PER_MONITOR_AWARE_V2
    static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(-4);
    const int PROCESS_PER_MONITOR_DPI_AWARE = 2;   // shcore PROCESS_DPI_AWARENESS enum

    /// <summary>
    /// 宣告 DPI awareness，回傳實際生效層級的描述（入口 log 用——Logger 在宣告
    /// 之後才建立，所以這裡回字串不直接 log）。非 Windows 為 no-op。
    /// </summary>
    public static string Apply()
    {
        if (!OperatingSystem.IsWindows())
            return "非 Windows，跳過 DPI awareness 宣告";

        // 1) Per-Monitor V2：每顆螢幕各自的物理座標，跨螢幕拖動也正確
        try
        {
            if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
                return "Per-Monitor V2（SetProcessDpiAwarenessContext）";
        }
        catch { /* user32 沒這個 entry point（< Win10 1703）：走下一層 */ }

        // 2) Per-Monitor（V1）：shcore；回 0 (S_OK) 才算成功
        //    （E_ACCESSDENIED = 已被宣告過——理論上不會發生，照樣往下走保險）
        try
        {
            if (SetProcessDpiAwareness(PROCESS_PER_MONITOR_DPI_AWARE) == 0)
                return "Per-Monitor（SetProcessDpiAwareness／shcore）";
        }
        catch { /* shcore.dll 不存在（< Win8.1）：走下一層 */ }

        // 3) System DPI aware：以主螢幕 DPI 為準——比 unaware 好，但副螢幕縮放不同時仍歪
        try
        {
            if (SetProcessDPIAware())
                return "System DPI aware（SetProcessDPIAware）——副螢幕縮放不同時仍可能錯位";
        }
        catch { /* 不應該發生（Vista+ 都有）；落到失敗訊息 */ }

        return "宣告失敗——process 維持 DPI-unaware（縮放 ≠ 100% 的螢幕上座標會被虛擬化，視窗會等比例錯位）";
    }

    [DllImport("user32.dll")]
    static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    [DllImport("shcore.dll")]
    static extern int SetProcessDpiAwareness(int value);

    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();
}
