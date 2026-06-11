using System.Runtime.InteropServices;

namespace TouchService.Ui;

/// <summary>
/// 螢幕清單（GET /monitors 用）：Windows 走 EnumDisplayMonitors + GetMonitorInfoW
/// （virtual desktop 座標，非主螢幕可為負），非 Windows 回空清單。
/// 設定頁用它填「目標螢幕」下拉，選定後把 rect 套進 mouseMode.screen。
/// </summary>
public static class MonitorEnumerator
{
    public sealed record MonitorInfo(int Index, string Name, int X, int Y, int Width, int Height, bool Primary);

    const uint MONITORINFOF_PRIMARY = 1;

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

    public static List<MonitorInfo> Enumerate()
    {
        var list = new List<MonitorInfo>();
        if (!OperatingSystem.IsWindows()) return list;

        bool Callback(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr lParam)
        {
            var info = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>() };
            if (GetMonitorInfoW(hMonitor, ref info))
            {
                list.Add(new MonitorInfo(
                    list.Count,
                    info.szDevice,
                    info.rcMonitor.Left,
                    info.rcMonitor.Top,
                    info.rcMonitor.Right - info.rcMonitor.Left,
                    info.rcMonitor.Bottom - info.rcMonitor.Top,
                    (info.dwFlags & MONITORINFOF_PRIMARY) != 0));
            }
            return true;   // 繼續列舉
        }

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Callback, IntPtr.Zero);
        return list;
    }
}
