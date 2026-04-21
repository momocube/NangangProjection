using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

/// <summary>
/// 感應觸碰 → Display 2 點擊事件。
/// 訂閱 SensorBus.OnTouch（已通過鎖定檢查），支援四種模式循環：
///
///   內部監聽  — 只走 SensorBus，不發 OS 事件（自製 Unity 軟體訂閱 SensorBus 即可）
///   PostMessage — 直送目標視窗訊息，游標不動（瀏覽器 / 一般程式）
///   滑鼠點擊  — SendInput 模擬真實硬體點擊，游標移動（外部 Unity / DirectInput 遊戲）
///   外部監聽  — 以 UDP 封包傳出觸碰座標，供有對接介面的外部軟體接收
/// </summary>
public class SensorClickSimulator : MonoBehaviour
{
    public enum ClickMode { Internal, PostMessage, SendInput, External }

    [Header("Display 2 在 Windows 桌面的起始位置")]
    [Tooltip("Display 2 左上角的 Windows 絕對座標。通常 X = Display 1 的寬度（預設 1920），Y = 0。")]
    public int display2OffsetX = 1920;
    public int display2OffsetY = 0;

    [Header("外部監聽 UDP 目標")]
    [Tooltip("外部監聽模式下，觸碰封包傳送目標（可為 127.0.0.1 或同網段 IP）。")]
    public string externalIP   = "127.0.0.1";
    public int    externalPort = 20002;

    [Header("點擊冷卻")]
    [Tooltip("啟用後每幀只允許一個點擊（模擬真實滑鼠）；關閉後同幀所有觸碰均觸發（原生多點觸控）。\n對三種模式（內部監聽 / PostMessage / 滑鼠點擊）均有效。")]
    public bool  cooldownEnabled = true;
    public float clickCooldown   = 1f;

    [Header("目前模式（唯讀，由按鈕切換）")]
    [SerializeField] ClickMode _mode = ClickMode.SendInput;

    const int CellW = 16;
    const int CellH = 32;

    float      _lastClickTime = float.NegativeInfinity;
    UdpClient  _udp;
    IPEndPoint _udpEndpoint;

    // ── 公開介面（供 SensorTestManager 按鈕使用）────────────────────────────

    public ClickMode Mode => _mode;

    public string ModeLabel => _mode switch
    {
        ClickMode.Internal    => "內部監聽",
        ClickMode.PostMessage => "PostMessage",
        ClickMode.SendInput   => "滑鼠點擊",
        ClickMode.External    => "外部監聽",
        _                     => "?"
    };

    public Color ModeColor => _mode switch
    {
        ClickMode.Internal    => new Color(0.15f, 0.18f, 0.25f),   // 灰（不送 OS 事件）
        ClickMode.PostMessage => new Color(0.15f, 0.35f, 0.20f),   // 綠（游標不動）
        ClickMode.SendInput   => new Color(0.15f, 0.25f, 0.40f),   // 藍（游標移動）
        ClickMode.External    => new Color(0.30f, 0.20f, 0.40f),   // 紫（UDP 外傳）
        _                     => Color.gray
    };

    public void CycleMode() => _mode = (ClickMode)(((int)_mode + 1) % 4);

    // ── 生命週期 ──────────────────────────────────────────────────────────────

    // 先 -= 再 += 防止 disable/enable 多次導致重複訂閱
    void OnEnable()
    {
        _udp      = new UdpClient();
        _udpEndpoint = new IPEndPoint(IPAddress.Parse(externalIP), externalPort);
        SensorBus.OnTouch -= OnTouch;
        SensorBus.OnTouch += OnTouch;
    }

    void OnDisable()
    {
        SensorBus.OnTouch -= OnTouch;
        _udp?.Close();
        _udp = null;
    }

    // ── 觸碰處理 ──────────────────────────────────────────────────────────────

    void OnTouch(int col, int row)
    {
        if (cooldownEnabled)
        {
            if (Time.time - _lastClickTime < clickCooldown) return;
            _lastClickTime = Time.time;
        }

        int absX = display2OffsetX + col * CellW + CellW / 2;
        int absY = display2OffsetY + row * CellH + CellH / 2;

        switch (_mode)
        {
            case ClickMode.Internal:
                Debug.Log($"[SensorClick] 內部監聽 格({col},{row})");
                break;
            case ClickMode.PostMessage:
                PostMessageClick(absX, absY, col, row);
                break;
            case ClickMode.SendInput:
                SendInputClick(absX, absY, col, row);
                break;
            case ClickMode.External:
                ExternalUdpSend(col, row);
                break;
        }
    }

    // ── PostMessage ───────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int left, top, right, bottom; }

    [DllImport("user32.dll")] static extern System.IntPtr WindowFromPoint(POINT p);
    [DllImport("user32.dll")] static extern bool PostMessage(System.IntPtr hWnd, uint Msg, System.IntPtr wParam, System.IntPtr lParam);
    [DllImport("user32.dll")] static extern bool GetWindowRect(System.IntPtr hWnd, out RECT rect);

    const uint WM_LBUTTONDOWN = 0x0201;
    const uint WM_LBUTTONUP   = 0x0202;
    const uint MK_LBUTTON     = 0x0001;

    static System.IntPtr MakeLParam(int x, int y) =>
        (System.IntPtr)((y << 16) | (x & 0xFFFF));

    void PostMessageClick(int absX, int absY, int col, int row)
    {
        var hwnd = WindowFromPoint(new POINT { x = absX, y = absY });
        if (hwnd == System.IntPtr.Zero)
        {
            Debug.LogWarning($"[SensorClick] PostMessage: 找不到目標視窗 ({absX},{absY})");
            return;
        }

        GetWindowRect(hwnd, out RECT rect);
        int lx = absX - rect.left;
        int ly = absY - rect.top;
        var lp = MakeLParam(lx, ly);

        PostMessage(hwnd, WM_LBUTTONDOWN, (System.IntPtr)MK_LBUTTON, lp);
        PostMessage(hwnd, WM_LBUTTONUP,   System.IntPtr.Zero,        lp);

        Debug.Log($"[SensorClick] PostMessage 格({col},{row}) → 視窗局部({lx},{ly})");
    }

    // ── SendInput ─────────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData, dwFlags, time;
        public System.IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk, wScan;
        public uint dwFlags, time;
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

    [DllImport("user32.dll")] static extern int  GetSystemMetrics(int nIndex);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
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

    void SendInputClick(int absX, int absY, int col, int row)
    {
        // 記錄目前游標位置，點擊後立即還原，避免游標跳動
        GetCursorPos(out POINT savedPos);

        int vdLeft   = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int vdTop    = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int vdWidth  = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int vdHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        int normX = (int)((long)(absX - vdLeft) * 65535 / vdWidth);
        int normY = (int)((long)(absY - vdTop)  * 65535 / vdHeight);

        // 還原座標（用於 SetCursorPos，需要實際螢幕像素）
        int normSavedX = (int)((long)(savedPos.x - vdLeft) * 65535 / vdWidth);
        int normSavedY = (int)((long)(savedPos.y - vdTop)  * 65535 / vdHeight);

        uint flags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK;
        var inputs = new INPUT[4];

        // 移到目標位置
        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi.dx = normX; inputs[0].u.mi.dy = normY;
        inputs[0].u.mi.dwFlags = MOUSEEVENTF_MOVE | flags;

        // 點擊
        inputs[1].type = INPUT_MOUSE;
        inputs[1].u.mi.dx = normX; inputs[1].u.mi.dy = normY;
        inputs[1].u.mi.dwFlags = MOUSEEVENTF_LEFTDOWN | flags;

        inputs[2].type = INPUT_MOUSE;
        inputs[2].u.mi.dx = normX; inputs[2].u.mi.dy = normY;
        inputs[2].u.mi.dwFlags = MOUSEEVENTF_LEFTUP | flags;

        // 立即移回原位
        inputs[3].type = INPUT_MOUSE;
        inputs[3].u.mi.dx = normSavedX; inputs[3].u.mi.dy = normSavedY;
        inputs[3].u.mi.dwFlags = MOUSEEVENTF_MOVE | flags;

        uint sent = SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
        if (sent != 4)
            Debug.LogWarning($"[SensorClick] SendInput 只送出 {sent}/4（錯誤碼 {Marshal.GetLastWin32Error()}）");
        else
            Debug.Log($"[SensorClick] SendInput 格({col},{row}) → 螢幕({absX},{absY})");
    }

    // ── External UDP ──────────────────────────────────────────────────────────

    void ExternalUdpSend(int col, int row)
    {
        if (_udp == null) return;
        string json  = $"{{\"col\":{col},\"row\":{row}}}";
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        try
        {
            _udp.Send(bytes, bytes.Length, _udpEndpoint);
            Debug.Log($"[SensorClick] 外部監聽 格({col},{row}) → {externalIP}:{externalPort}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SensorClick] 外部監聽 傳送失敗: {e.Message}");
        }
    }
}
