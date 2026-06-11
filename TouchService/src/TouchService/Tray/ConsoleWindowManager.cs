using System.Runtime.InteropServices;
using TouchService.Logging;

namespace TouchService.Tray;

/// <summary>
/// console 視窗顯示/隱藏（GetConsoleWindow + ShowWindow）。
/// 托盤化的核心：Windows 啟動時隱藏 console（除非 --console flag），
/// 右鍵選單「顯示主控台」隨時叫回。非 Windows / 無 console（service 模式、
/// stdout 重導）全部 no-op，不 crash。
/// </summary>
public static class ConsoleWindowManager
{
    const int SW_HIDE = 0;
    const int SW_SHOW = 5;

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);

    /// <summary>有 console 視窗且可見（非 Windows / 無 console = false）。</summary>
    public static bool IsVisible()
    {
        if (!OperatingSystem.IsWindows()) return false;
        var hwnd = GetConsoleWindow();
        return hwnd != IntPtr.Zero && IsWindowVisible(hwnd);
    }

    /// <summary>隱藏 console 視窗（托盤化啟動時呼叫）。無 console（hwnd=0）no-op。</summary>
    public static void Hide(FileLogger log)
    {
        if (!OperatingSystem.IsWindows()) return;
        var hwnd = GetConsoleWindow();
        if (hwnd == IntPtr.Zero)
        {
            log.Info("[Tray] 無 console 視窗可隱藏（service 模式 / 重導），略過");
            return;
        }
        ShowWindow(hwnd, SW_HIDE);
        log.Info("[Tray] console 視窗已隱藏（托盤右鍵「顯示主控台」可叫回；--console 啟動可不隱藏）");
    }

    /// <summary>顯示/隱藏切換（托盤右鍵選單）。回傳切換後是否可見；無 console 回 false。</summary>
    public static bool Toggle(FileLogger log)
    {
        if (!OperatingSystem.IsWindows()) return false;
        var hwnd = GetConsoleWindow();
        if (hwnd == IntPtr.Zero) return false;

        bool show = !IsWindowVisible(hwnd);
        ShowWindow(hwnd, show ? SW_SHOW : SW_HIDE);
        log.Info($"[Tray] console 視窗{(show ? "顯示" : "隱藏")}");
        return show;
    }
}
