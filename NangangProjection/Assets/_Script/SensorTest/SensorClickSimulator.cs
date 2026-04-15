using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 感應觸碰 → Display 2 上的 Windows OS 等級滑鼠點擊。
///
/// 訂閱 SensorBus.OnTouch（已通過 SensorTestManager 鎖定檢查的 col/row），
/// 因此智慧鎖格、右鍵鎖格均正常阻擋點擊事件。
///
/// 使用 SendInput + MOUSEEVENTF_ABSOLUTE + MOUSEEVENTF_VIRTUALDESK
/// 正確對應多螢幕虛擬桌面座標。
/// </summary>
public class SensorClickSimulator : MonoBehaviour
{
    [Header("Display 2 在 Windows 桌面的起始位置")]
    [Tooltip("Display 2 左上角的 Windows 絕對座標。通常 X = Display 1 的寬度（預設 1920），Y = 0。")]
    public int display2OffsetX = 1920;
    public int display2OffsetY = 0;

    [Header("點擊冷卻（秒）")]
    [Tooltip("兩次點擊之間的最短間隔，防止同一區域連續觸發。")]
    public float clickCooldown = 1f;

    const int CellW = 16;
    const int CellH = 32;

    float _lastClickTime = float.NegativeInfinity;

    // ── Windows API（SendInput）──────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int   dx, dy;
        public uint  mouseData, dwFlags, time;
        public System.IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk, wScan;
        public uint   dwFlags, time;
        public System.IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct HARDWAREINPUT { public uint uMsg; public ushort wParamL, wParamH; }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT    mi;
        [FieldOffset(0)] public KEYBDINPUT    ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT { public int type; public InputUnion u; }

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")] static extern int GetSystemMetrics(int nIndex);
    const int SM_XVIRTUALSCREEN  = 76;
    const int SM_YVIRTUALSCREEN  = 77;
    const int SM_CXVIRTUALSCREEN = 78;
    const int SM_CYVIRTUALSCREEN = 79;

    const int  INPUT_MOUSE             = 0;
    const uint MOUSEEVENTF_MOVE        = 0x0001;
    const uint MOUSEEVENTF_LEFTDOWN    = 0x0002;
    const uint MOUSEEVENTF_LEFTUP      = 0x0004;
    const uint MOUSEEVENTF_ABSOLUTE    = 0x8000;
    const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;

    // ── 生命週期 ──────────────────────────────────────────────────────────────

    // 訂閱 SensorBus.OnTouch（已通過鎖定檢查），而非 UdpSensorReceiver.OnPacketReceived
    void OnEnable()  => SensorBus.OnTouch += OnTouch;
    void OnDisable() => SensorBus.OnTouch -= OnTouch;

    // ── 觸碰處理 ──────────────────────────────────────────────────────────────

    void OnTouch(int col, int row)
    {
        if (Time.time - _lastClickTime < clickCooldown) return;

        int absX = display2OffsetX + col * CellW + CellW / 2;
        int absY = display2OffsetY + row * CellH + CellH / 2;

        _lastClickTime = Time.time;
        SimulateClick(absX, absY);

        Debug.Log($"[SensorClick] 格({col},{row}) → 螢幕({absX},{absY})");
    }

    // ── 點擊實作 ──────────────────────────────────────────────────────────────

    void SimulateClick(int absX, int absY)
    {
        int vdLeft   = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int vdTop    = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int vdWidth  = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int vdHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        int normX = (int)((long)(absX - vdLeft) * 65535 / vdWidth);
        int normY = (int)((long)(absY - vdTop)  * 65535 / vdHeight);

        uint flags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK;
        var inputs = new INPUT[3];

        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi.dx = normX; inputs[0].u.mi.dy = normY;
        inputs[0].u.mi.dwFlags = MOUSEEVENTF_MOVE | flags;

        inputs[1].type = INPUT_MOUSE;
        inputs[1].u.mi.dx = normX; inputs[1].u.mi.dy = normY;
        inputs[1].u.mi.dwFlags = MOUSEEVENTF_LEFTDOWN | flags;

        inputs[2].type = INPUT_MOUSE;
        inputs[2].u.mi.dx = normX; inputs[2].u.mi.dy = normY;
        inputs[2].u.mi.dwFlags = MOUSEEVENTF_LEFTUP | flags;

        uint sent = SendInput(3, inputs, Marshal.SizeOf(typeof(INPUT)));
        if (sent != 3)
            Debug.LogWarning($"[SensorClick] SendInput 只送出 {sent}/3（錯誤碼 {Marshal.GetLastWin32Error()}）");
    }
}
