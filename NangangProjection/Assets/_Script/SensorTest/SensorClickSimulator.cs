using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 將硬體感應器的 UDP 座標，轉換為 Display 2 上的 Windows OS 等級滑鼠點擊。
/// 掛在 sensor_test 場景的任意 GameObject 上即可運作（不依賴 SensorTestManager）。
///
/// 使用 SendInput + MOUSEEVENTF_ABSOLUTE + MOUSEEVENTF_VIRTUALDESK，
/// 正確對應多螢幕虛擬桌面座標，取代舊版 mouse_event。
/// </summary>
public class SensorClickSimulator : MonoBehaviour
{
    [Header("座標校準（與 SensorTestManager 一致）")]
    public float rawMinX = 1928f;
    public float rawMaxX = 3960f;
    public float rawMinY = 16f;
    public float rawMaxY = 752f;

    [Header("Display 2 在 Windows 桌面的起始位置")]
    [Tooltip("Display 2 左上角的 Windows 絕對座標。通常 X = Display 1 的寬度（預設 1920），Y = 0。")]
    public int display2OffsetX = 1920;
    public int display2OffsetY = 0;

    [Header("點擊冷卻（秒）")]
    [Tooltip("兩次點擊之間的最短間隔，防止同一區域連續觸發。")]
    public float clickCooldown = 1f;

    // 格子規格（固定，與硬體一致）
    const int Cols  = 128;
    const int Rows  = 24;
    const int CellW = 16;
    const int CellH = 32;

    float _lastClickTime = float.NegativeInfinity;

    // ── Windows API（SendInput）──────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int   dx, dy;
        public uint  mouseData;
        public uint  dwFlags;
        public uint  time;
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
    struct HARDWAREINPUT
    {
        public uint   uMsg;
        public ushort wParamL, wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT    mi;
        [FieldOffset(0)] public KEYBDINPUT    ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public int        type;
        public InputUnion u;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    // 虛擬桌面範圍（所有螢幕的聯合矩形）
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
    const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;  // 座標對應整個虛擬桌面，多螢幕必要

    // ── 生命週期 ──────────────────────────────────────────────────────────────

    void OnEnable()  => UdpSensorReceiver.OnPacketReceived += OnUdpPacket;
    void OnDisable() => UdpSensorReceiver.OnPacketReceived -= OnUdpPacket;

    // ── 感應處理 ──────────────────────────────────────────────────────────────

    void OnUdpPacket(int rawX, int rawY)
    {
        if (Time.time - _lastClickTime < clickCooldown) return;

        float nx = Mathf.InverseLerp(rawMinX, rawMaxX, rawX);
        float ny = Mathf.InverseLerp(rawMinY, rawMaxY, rawY);
        if (nx < -0.1f || nx > 1.1f || ny < -0.1f || ny > 1.1f) return;

        int col = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(nx) * Cols), 0, Cols - 1);
        int row = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(ny) * Rows), 0, Rows - 1);

        int absX = display2OffsetX + col * CellW + CellW / 2;
        int absY = display2OffsetY + row * CellH + CellH / 2;

        _lastClickTime = Time.time;
        SimulateClick(absX, absY);

        Debug.Log($"[SensorClick] raw({rawX},{rawY}) → 格({col},{row}) → 螢幕({absX},{absY})");
    }

    // ── 點擊實作 ──────────────────────────────────────────────────────────────

    void SimulateClick(int absX, int absY)
    {
        // 取得虛擬桌面的起始點與尺寸（含所有螢幕）
        int vdLeft   = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int vdTop    = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int vdWidth  = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int vdHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        // 正規化到 0~65535（SendInput ABSOLUTE + VIRTUALDESK 的座標範圍）
        int normX = (int)((long)(absX - vdLeft) * 65535 / vdWidth);
        int normY = (int)((long)(absY - vdTop)  * 65535 / vdHeight);

        uint flags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK;

        var inputs = new INPUT[3];

        // 1. 移動到目標位置
        inputs[0].type       = INPUT_MOUSE;
        inputs[0].u.mi.dx    = normX;
        inputs[0].u.mi.dy    = normY;
        inputs[0].u.mi.dwFlags = MOUSEEVENTF_MOVE | flags;

        // 2. 按下左鍵
        inputs[1].type       = INPUT_MOUSE;
        inputs[1].u.mi.dx    = normX;
        inputs[1].u.mi.dy    = normY;
        inputs[1].u.mi.dwFlags = MOUSEEVENTF_LEFTDOWN | flags;

        // 3. 放開左鍵
        inputs[2].type       = INPUT_MOUSE;
        inputs[2].u.mi.dx    = normX;
        inputs[2].u.mi.dy    = normY;
        inputs[2].u.mi.dwFlags = MOUSEEVENTF_LEFTUP | flags;

        uint sent = SendInput(3, inputs, Marshal.SizeOf(typeof(INPUT)));
        if (sent != 3)
            Debug.LogWarning($"[SensorClick] SendInput 只送出 {sent}/3 個事件（GetLastError = {Marshal.GetLastWin32Error()}）");
    }
}
