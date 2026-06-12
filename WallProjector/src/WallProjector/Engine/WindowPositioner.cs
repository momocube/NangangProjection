using System.Runtime.InteropServices;
using System.Text;
using WallProjector.Core;

namespace WallProjector.Engine;

/// <summary>視窗對位結果：找沒找到 + 對位後實際讀回的 rect + 是否與目標不符（±2px 容差）。</summary>
public readonly record struct PositionResult(bool Positioned, WindowRect? ActualRect, bool RectMismatch);

/// <summary>
/// Win32 視窗精確對位：EnumWindows 找到 title 含 slot 標記**或屬於指定 pid**的視窗 →
/// 去 WS_CAPTION / WS_THICKFRAME（真 kiosk，--window-size 含邊框不精確）→
/// SetWindowPos 到牆面 rect（HWND_TOPMOST 依 config）。
///
/// pid 比對是 CDP 模式必需：CDP 直開目標站（不經 wrap 頁），視窗 title 是目標站
/// 自己的——但 pid 比對可能失敗（Edge 有 launcher 行為時視窗實際屬 child process），
/// 所以 V3 起 CDP 模式同時注入 title 標記作備援（見 CdpInjector.InjectTitleMarkerAsync）。
///
/// V3：對位後 GetWindowRect 讀回實際 rect 與目標比對（±2px），不吻合 WARN 兩個 rect
/// 都印——「有沒有對準」變成看得到的數據（DPI 虛擬化等問題一眼可辨）；timeout 時
/// 列出當下所有可見頂層視窗（title + pid），現場一看 log 就知道視窗到底叫什麼、屬於誰。
///
/// 非 Windows：no-op（log 後回未對位）——Linux 開發機可建可測，現場行為屬
/// Windows 驗證清單（見 README）。
/// </summary>
public static class WindowPositioner
{
    /// <summary>實際 rect 與目標 rect 的吻合容差（px）——去邊框後理論上應全等，留 2px 吸收圓整。</summary>
    public const int RectTolerancePx = 2;

    /// <summary>
    /// 啟動後輪詢找視窗（瀏覽器開窗有延遲），找到即對位並讀回實際 rect 驗證。
    /// 比對條件：title 含 titleMarker，或（pid 非 null 時）視窗屬該 process。
    /// </summary>
    public static PositionResult TryPosition(string titleMarker, WindowRect rect, bool topmost, Logger log,
                                             int timeoutMs = 15000, int pollMs = 250, int? pid = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            log.Info($"[Window] 非 Windows，跳過視窗對位（{titleMarker}）");
            return new PositionResult(false, null, false);
        }

        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            var hwnd = FindWindow(titleMarker, pid);
            if (hwnd != IntPtr.Zero)
            {
                var actual = ApplyKioskStyle(hwnd, rect, topmost, log);
                bool mismatch = actual.HasValue && !RectsMatch(rect, actual.Value);
                return new PositionResult(true, actual, mismatch);
            }
            Thread.Sleep(pollMs);
        }
        log.Warn($"[Window] {timeoutMs}ms 內找不到標題含 {titleMarker}" +
                 (pid.HasValue ? $" 或 pid={pid.Value}" : "") + " 的視窗——" +
                 "視窗仍以 --window-position/--window-size 粗對位（含邊框、DIP 單位受縮放影響）");
        LogVisibleTopWindows(log);
        return new PositionResult(false, null, false);
    }

    /// <summary>rect 比對（純函式，可測）：四個分量都在容差內才算吻合。</summary>
    public static bool RectsMatch(WindowRect expected, WindowRect actual, int tolerancePx = RectTolerancePx) =>
        Math.Abs(expected.X - actual.X) <= tolerancePx &&
        Math.Abs(expected.Y - actual.Y) <= tolerancePx &&
        Math.Abs(expected.Width - actual.Width) <= tolerancePx &&
        Math.Abs(expected.Height - actual.Height) <= tolerancePx;

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

    /// <summary>去邊框 + SetWindowPos，之後 GetWindowRect 讀回實際 rect 與目標比對
    /// ——吻合 INFO、不吻合 WARN（兩個 rect 都印）。回 null = 讀回失敗。</summary>
    static WindowRect? ApplyKioskStyle(IntPtr hwnd, WindowRect rect, bool topmost, Logger log)
    {
        long style = GetWindowLongPtrW(hwnd, GWL_STYLE).ToInt64();
        style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
        SetWindowLongPtrW(hwnd, GWL_STYLE, new IntPtr(style));

        SetWindowPos(hwnd, topmost ? HWND_TOPMOST : HWND_NOTOPMOST,
            rect.X, rect.Y, rect.Width, rect.Height, SWP_FRAMECHANGED | SWP_SHOWWINDOW);

        if (!GetWindowRect(hwnd, out RECT r))
        {
            log.Warn($"[Window] 已對位 hwnd=0x{hwnd:X} 但 GetWindowRect 讀回失敗——無法驗證實際落點");
            return null;
        }
        var actual = new WindowRect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);

        if (RectsMatch(rect, actual))
        {
            log.Info($"[Window] 已對位 hwnd=0x{hwnd:X}：({rect.X},{rect.Y}) {rect.Width}x{rect.Height}" +
                     $"{(topmost ? "（置頂）" : "")}，已去邊框，實際讀回吻合（±{RectTolerancePx}px）");
        }
        else
        {
            // 不吻合最常見原因：DPI 虛擬化（本 process 沒拿到 PMv2？看啟動 log 的 [DPI] 行）
            // 或視窗最小尺寸限制。兩個 rect 都印，現場照著差值就能判斷是不是縮放倍數。
            log.Warn($"[Window] 對位後實際 rect 與目標不符 hwnd=0x{hwnd:X}：" +
                     $"目標 ({rect.X},{rect.Y}) {rect.Width}x{rect.Height}、" +
                     $"實際 ({actual.X},{actual.Y}) {actual.Width}x{actual.Height}" +
                     "——檢查投影螢幕 Windows 縮放%（管理頁螢幕清單）與啟動 log 的 [DPI] 行");
        }
        return actual;
    }

    /// <summary>timeout 診斷：列出當下所有可見、有標題的頂層視窗（最多 20 個）。</summary>
    static void LogVisibleTopWindows(Logger log)
    {
        var lines = new List<string>();
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) return true;
            int len = GetWindowTextLengthW(hwnd);
            if (len <= 0) return true;
            var sb = new StringBuilder(len + 1);
            GetWindowTextW(hwnd, sb, sb.Capacity);
            GetWindowThreadProcessId(hwnd, out uint winPid);
            lines.Add($"  pid={winPid,-7} \"{sb}\"");
            return lines.Count < 20;   // 最多 20 個，夠認出視窗就好
        }, IntPtr.Zero);
        log.Warn($"[Window] 當下可見頂層視窗（title / pid，最多 20 個）：\n" +
                 (lines.Count > 0 ? string.Join("\n", lines) : "  （沒有任何有標題的可見視窗）"));
    }

    delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

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

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
}
