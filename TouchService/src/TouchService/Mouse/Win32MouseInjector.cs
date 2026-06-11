using System.Runtime.InteropServices;

namespace TouchService.Mouse;

/// <summary>
/// Windows SendInput 滑鼠注入：MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK，
/// 螢幕座標正規化為 0–65535 對 virtual desktop 邊界
/// （GetSystemMetrics SM_X/YVIRTUALSCREEN + SM_CX/CYVIRTUALSCREEN，多螢幕負座標可用）。
/// 每次 Move 重查 metrics（呼叫成本極低；解析度/螢幕配置變更即時跟上）。
/// 非 Windows 上所有方法直接 return（host 正常情況不會在非 Windows 建立本類，雙保險）。
/// </summary>
public sealed class Win32MouseInjector : IMouseInjector
{
    const int SM_XVIRTUALSCREEN  = 76;
    const int SM_YVIRTUALSCREEN  = 77;
    const int SM_CXVIRTUALSCREEN = 78;
    const int SM_CYVIRTUALSCREEN = 79;

    const uint MOUSEEVENTF_MOVE        = 0x0001;
    const uint MOUSEEVENTF_LEFTDOWN    = 0x0002;
    const uint MOUSEEVENTF_LEFTUP      = 0x0004;
    const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
    const uint MOUSEEVENTF_ABSOLUTE    = 0x8000;

    const uint INPUT_MOUSE = 0;

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // union 內 MOUSEINPUT 最大，Sequential type + MOUSEINPUT 與原生 INPUT 同 layout（x64 = 40 bytes）
    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public uint type;
        public MOUSEINPUT mi;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    public void Move(int x, int y)
    {
        if (!OperatingSystem.IsWindows()) return;
        int vx = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int vy = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int vw = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int vh = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        var (nx, ny) = NormalizeToVirtualDesk(x, y, vx, vy, vw, vh);
        Send(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK, nx, ny);
    }

    public void LeftDown()
    {
        if (!OperatingSystem.IsWindows()) return;
        Send(MOUSEEVENTF_LEFTDOWN, 0, 0);
    }

    public void LeftUp()
    {
        if (!OperatingSystem.IsWindows()) return;
        Send(MOUSEEVENTF_LEFTUP, 0, 0);
    }

    /// <summary>
    /// 螢幕座標 → SendInput 0–65535 正規化（純函式，單元測試對拍）。
    /// 公式為 MSDN 慣用 (x - vx) * 65536 / vw，超出 virtual desktop 邊界 clamp。
    /// </summary>
    public static (int Nx, int Ny) NormalizeToVirtualDesk(int x, int y, int vx, int vy, int vw, int vh)
    {
        if (vw <= 0 || vh <= 0) return (0, 0);
        long nx = (long)(x - vx) * 65536 / vw;
        long ny = (long)(y - vy) * 65536 / vh;
        return ((int)Math.Clamp(nx, 0, 65535), (int)Math.Clamp(ny, 0, 65535));
    }

    static void Send(uint flags, int dx, int dy)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            mi = new MOUSEINPUT { dx = dx, dy = dy, dwFlags = flags },
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }
}
