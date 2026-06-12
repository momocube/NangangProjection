using System.Runtime.InteropServices;
using System.Text;
using WallProjector.Core;

namespace WallProjector.Engine;

/// <summary>
/// Win32 視窗精確對位：EnumWindows 找到 title 含 slot 標記**或屬於指定 pid**的視窗 →
/// 去 WS_CAPTION / WS_THICKFRAME（真 kiosk，--window-size 含邊框不精確）→
/// SetWindowPos 到牆面 rect（HWND_TOPMOST 依 config）。
///
/// pid 比對是 CDP 模式必需：CDP 直開目標站（不經 wrap 頁），視窗 title 是目標站
/// 自己的、沒有 [WP-slot-N] 標記——但每槽獨立 --user-data-dir 保證我們 launch 的
/// process 就是瀏覽器主 process，視窗屬它所有，pid 比對可靠。
///
/// 非 Windows：no-op（log 後回 false）——Linux 開發機可建可測，現場行為屬
/// Windows 驗證清單（見 README）。
/// </summary>
public static class WindowPositioner
{
    /// <summary>
    /// 啟動後輪詢找視窗（瀏覽器開窗有延遲），找到即對位。
    /// 比對條件：title 含 titleMarker，或（pid 非 null 時）視窗屬該 process。
    /// 回 true = 已找到並完成 SetWindowPos。
    /// </summary>
    public static bool TryPosition(string titleMarker, WindowRect rect, bool topmost, Logger log,
                                   int timeoutMs = 15000, int pollMs = 250, int? pid = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            log.Info($"[Window] 非 Windows，跳過視窗對位（{titleMarker}）");
            return false;
        }

        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            var hwnd = FindWindow(titleMarker, pid);
            if (hwnd != IntPtr.Zero)
            {
                ApplyKioskStyle(hwnd, rect, topmost, log);
                return true;
            }
            Thread.Sleep(pollMs);
        }
        log.Warn($"[Window] {timeoutMs}ms 內找不到標題含 {titleMarker}" +
                 (pid.HasValue ? $" 或 pid={pid.Value}" : "") + " 的視窗——" +
                 "視窗仍以 --window-position/--window-size 粗對位（含邊框）");
        return false;
    }

    // ── Win32 ──────────────────────────────────────────────────────────────

    const int GWL_STYLE = -16;
    const long WS_CAPTION = 0x00C00000;
    const long WS_THICKFRAME = 0x00040000;
    const long WS_MINIMIZEBOX = 0x00020000;
    const long WS_MAXIMIZEBOX = 0x00010000;
    const long WS_SYSMENU = 0x00080000;

    static readonly IntPtr HWND_TOPMOST = new(-1);
    static readonly IntPtr HWND_NOTOPMOST = new(-2);
    const uint SWP_FRAMECHANGED = 0x0020;
    const uint SWP_SHOWWINDOW = 0x0040;

    static IntPtr FindWindow(string marker, int? pid)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) return true;
            int len = GetWindowTextLengthW(hwnd);
            if (len <= 0) return true;   // 無標題（Chromium 的訊息視窗等）：跳過

            bool match = false;
            if (pid.HasValue)
            {
                GetWindowThreadProcessId(hwnd, out uint winPid);
                match = winPid == (uint)pid.Value;
            }
            if (!match)
            {
                var sb = new StringBuilder(len + 1);
                GetWindowTextW(hwnd, sb, sb.Capacity);
                match = sb.ToString().Contains(marker, StringComparison.Ordinal);
            }
            if (match)
            {
                found = hwnd;
                return false;   // 停止列舉
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    static void ApplyKioskStyle(IntPtr hwnd, WindowRect rect, bool topmost, Logger log)
    {
        long style = GetWindowLongPtrW(hwnd, GWL_STYLE).ToInt64();
        style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
        SetWindowLongPtrW(hwnd, GWL_STYLE, new IntPtr(style));

        SetWindowPos(hwnd, topmost ? HWND_TOPMOST : HWND_NOTOPMOST,
            rect.X, rect.Y, rect.Width, rect.Height, SWP_FRAMECHANGED | SWP_SHOWWINDOW);

        log.Info($"[Window] 已對位 hwnd=0x{hwnd:X}：({rect.X},{rect.Y}) {rect.Width}x{rect.Height}" +
                 $"{(topmost ? "（置頂）" : "")}，已去邊框");
    }

    delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowTextW(IntPtr hwnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    static extern int GetWindowTextLengthW(IntPtr hwnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    static extern IntPtr GetWindowLongPtrW(IntPtr hwnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    static extern IntPtr SetWindowLongPtrW(IntPtr hwnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);
}
