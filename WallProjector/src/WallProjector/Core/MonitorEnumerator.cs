using System.Runtime.InteropServices;

namespace WallProjector.Core;

/// <summary>
/// 螢幕清單診斷：EnumDisplayMonitors + GetMonitorInfoW + GetDpiForMonitor。
/// 寫法參考 TouchService 的同名類別，但 WallProjector 自帶一份（不跨專案引用——
/// 兩個 exe 獨立部署），並多了 **DPI / 縮放%**：等比例錯位的第一嫌疑就是縮放，
/// 讓現場直接對照「screen 設定 vs 螢幕物理事實」，不用開 Windows 設定猜。
///
/// 本 process 已在入口宣告 Per-Monitor V2（<see cref="DpiAwareness"/>），
/// 所以 rcMonitor 拿到的是**物理像素** rect（virtual desktop 座標，非主螢幕可為負）。
/// 非 Windows 回空清單。
/// </summary>
public static class MonitorEnumerator
{
    /// <summary>單顆螢幕：物理 rect + 有效 DPI（縮放% = dpi / 96）。</summary>
    public sealed record MonitorInfo(int Index, string Name, int X, int Y, int Width, int Height,
                                     bool Primary, int Dpi, int ScalePercent);

    const uint MONITORINFOF_PRIMARY = 1;
    const int MDT_EFFECTIVE_DPI = 0;   // 使用者設定的縮放反映在這個 DPI 類型

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct MONITORINFOEX
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc proc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool GetMonitorInfoW(IntPtr hMonitor, ref MONITORINFOEX info);

    [DllImport("shcore.dll")]
    static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);

    public static List<MonitorInfo> Enumerate()
    {
        var list = new List<MonitorInfo>();
        if (!OperatingSystem.IsWindows()) return list;

        bool Callback(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr lParam)
        {
            var info = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>() };
            if (GetMonitorInfoW(hMonitor, ref info))
            {
                int dpi = QueryDpi(hMonitor);
                list.Add(new MonitorInfo(
                    list.Count,
                    info.szDevice,
                    info.rcMonitor.Left,
                    info.rcMonitor.Top,
                    info.rcMonitor.Right - info.rcMonitor.Left,
                    info.rcMonitor.Bottom - info.rcMonitor.Top,
                    (info.dwFlags & MONITORINFOF_PRIMARY) != 0,
                    dpi,
                    (int)Math.Round(dpi * 100.0 / 96.0)));
            }
            return true;   // 繼續列舉
        }

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Callback, IntPtr.Zero);
        return list;
    }

    /// <summary>該螢幕的有效 DPI；shcore 不可用（Win8.0）或呼叫失敗 → fallback 96（=100%）。</summary>
    static int QueryDpi(IntPtr hMonitor)
    {
        try
        {
            if (GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out uint dx, out _) == 0 && dx > 0)
                return (int)dx;
        }
        catch { /* shcore.dll 不存在等：fallback */ }
        return 96;
    }
}
