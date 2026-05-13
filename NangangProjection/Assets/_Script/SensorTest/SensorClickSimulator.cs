using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

/// <summary>
/// 感應觸碰 → Display 2 點擊事件。
/// 訂閱 SensorBus.OnTouch（已通過鎖定檢查），支援三種主模式：
///
///   監聽       — 走 SensorBus 廣播；若 externalEnabled=true 同時送 UDP
///   PostMessage — 直送目標視窗訊息，游標不動
///   滑鼠       — SendInput 模擬，子行為由 mouseBehavior 決定（點擊／按壓／拖曳）
/// </summary>
public class SensorClickSimulator : MonoBehaviour
{
    public enum ClickMode    { Listen, PostMessage, Mouse }
    public enum MouseBehavior { Click, Press, Drag }

    [Header("Display 2 在 Windows 桌面的起始位置")]
    public int display2OffsetX = 1920;
    public int display2OffsetY = 0;

    [Header("監聽模式")]
    [Tooltip("開啟後在監聽模式同時送出 UDP 座標。")]
    public bool   externalEnabled = false;
    public string externalIP      = "127.0.0.1";
    public int    externalPort    = 20002;

    [Header("點擊冷卻（監聽 / PostMessage / 滑鼠·點擊 有效）")]
    public bool  cooldownEnabled = true;
    public float clickCooldown   = 1f;

    [Header("智慧觸碰效果勾選（配合 SmartTouch 辨識）")]
    [Tooltip("輕觸解算時發出 OS 點擊事件。")]
    public bool tapClickEnabled   = true;
    [Tooltip("按壓開始 / 結束時發出 OS LEFTDOWN / LEFTUP 事件。")]
    public bool pressClickEnabled = true;
    [Tooltip("拖曳手勢時移動滑鼠（Mouse / PostMessage 模式，需同時開啟按壓才有效果）。")]
    public bool dragClickEnabled  = true;

    [Header("滑鼠模式")]
    public MouseBehavior mouseBehavior = MouseBehavior.Click;
    [Tooltip("按壓：LEFTDOWN 後持續按住的秒數。")]
    public float pressHoldDuration = 0.3f;
    [Tooltip("拖曳：最後一次觸碰後等待多少秒才放開滑鼠。")]
    public float dragReleaseDelay  = 0.5f;

    [Header("Debug")]
    public bool debugLog = false;

    [Header("目前模式（唯讀，由按鈕切換）")]
    [SerializeField] ClickMode _mode = ClickMode.PostMessage;

    /// <summary>記錄模組可訂閱：每次發出 OS 事件時通知（描述字串）。</summary>
    public static event System.Action<string> OnSimEvent;

    const int CellW = 16;
    const int CellH = 32;

    float     _lastClickTime = float.NegativeInfinity;
    UdpClient _udp;
    IPEndPoint _udpEndpoint;

    // 按壓 / 拖曳狀態
    Coroutine _pressCoroutine;
    bool      _isDragging;
    Coroutine _dragReleaseCoroutine;

    // 智慧按壓狀態（由 SmartTouch PressStart / PressEnd 驅動）
    bool _smartPressStarted;

    // Display 2 HWND 白名單（由 SensorTestManager 注入，防止 PostMessage 打到 Display 1）
    System.IntPtr _knownDisplay2Hwnd = System.IntPtr.Zero;

    // 上一次送出 SendInput 的時間（供 HandleDragInteract 時間窗守衛使用）
    public float LastSendInputTime { get; private set; }

    // ── 公開介面 ──────────────────────────────────────────────────────────────

    public ClickMode Mode => _mode;

    public string ModeLabel => _mode switch
    {
        ClickMode.Listen      => "監聽",
        ClickMode.PostMessage => "PostMessage",
        ClickMode.Mouse       => mouseBehavior switch
        {
            MouseBehavior.Click => "滑鼠·點擊",
            MouseBehavior.Press => "滑鼠·按壓",
            MouseBehavior.Drag  => "滑鼠·拖曳",
            _                   => "滑鼠"
        },
        _ => "?"
    };

    public Color ModeColor => _mode switch
    {
        ClickMode.Listen      => new Color(0.15f, 0.18f, 0.25f),
        ClickMode.PostMessage => new Color(0.15f, 0.35f, 0.20f),
        ClickMode.Mouse       => mouseBehavior switch
        {
            MouseBehavior.Click => new Color(0.15f, 0.25f, 0.40f),
            MouseBehavior.Press => new Color(0.35f, 0.25f, 0.10f),
            MouseBehavior.Drag  => new Color(0.25f, 0.15f, 0.40f),
            _                   => Color.gray
        },
        _ => Color.gray
    };

    public void CycleMode() => _mode = (ClickMode)(((int)_mode + 1) % 3);

    /// <summary>由 SensorTestManager 在找到 Display 2 視窗後注入，確保 PostMessage 只打到正確的目標視窗。</summary>
    public void SetDisplay2Hwnd(System.IntPtr hwnd) => _knownDisplay2Hwnd = hwnd;

    // ── 生命週期 ──────────────────────────────────────────────────────────────

    void OnEnable()
    {
        _udp         = new UdpClient();
        _udpEndpoint = new IPEndPoint(IPAddress.Parse(externalIP), externalPort);
        SensorBus.OnTouch      -= OnTouch;
        SensorBus.OnTouch      += OnTouch;
        SensorBus.OnTap        -= OnSmartTap;
        SensorBus.OnTap        += OnSmartTap;
        SensorBus.OnPressStart -= OnSmartPressStart;
        SensorBus.OnPressStart += OnSmartPressStart;
        SensorBus.OnPressEnd   -= OnSmartPressEnd;
        SensorBus.OnPressEnd   += OnSmartPressEnd;
        SensorBus.OnDrag       -= OnSmartDrag;
        SensorBus.OnDrag       += OnSmartDrag;
    }

    void OnDisable()
    {
        SensorBus.OnTouch      -= OnTouch;
        SensorBus.OnTap        -= OnSmartTap;
        SensorBus.OnPressStart -= OnSmartPressStart;
        SensorBus.OnPressEnd   -= OnSmartPressEnd;
        SensorBus.OnDrag       -= OnSmartDrag;
        _udp?.Close();
        _udp = null;

        if (_isDragging)
        {
            SendMouseUp();
            _isDragging = false;
        }

        if (_smartPressStarted)
        {
            SendMouseUp();
            _smartPressStarted = false;
        }
    }

    // ── 觸碰處理 ──────────────────────────────────────────────────────────────

    void OnTouch(int col, int row)
    {
        int absX = display2OffsetX + col * CellW + CellW / 2;
        int absY = display2OffsetY + row * CellH + CellH / 2;

        switch (_mode)
        {
            case ClickMode.Listen:
                if (!CheckCooldown()) return;
                if (debugLog) Debug.Log($"[SensorClick] 監聽 格({col},{row})");
                OnSimEvent?.Invoke($"監聽  格({col},{row})");
                if (externalEnabled) ExternalUdpSend(col, row);
                break;

            case ClickMode.PostMessage:
                if (!CheckCooldown()) return;
                PostMessageClick(absX, absY, col, row);
                break;

            case ClickMode.Mouse:
                switch (mouseBehavior)
                {
                    case MouseBehavior.Click:
                        if (!CheckCooldown()) return;
                        SendInputClick(absX, absY, col, row);
                        break;
                    case MouseBehavior.Press:
                        if (_pressCoroutine == null)
                            _pressCoroutine = StartCoroutine(PressCoroutine(absX, absY, col, row));
                        break;
                    case MouseBehavior.Drag:
                        HandleDragTouch(absX, absY, col, row);
                        break;
                }
                break;
        }
    }

    bool CheckCooldown()
    {
        if (!cooldownEnabled) return true;
        if (Time.time - _lastClickTime < clickCooldown) return false;
        _lastClickTime = Time.time;
        return true;
    }

    // ── PostMessage ───────────────────────────────────────────────────────────

    /// <summary>
    /// 驗證 hwnd 是合法的 Display 2 目標。
    /// 若已知 Display 2 HWND，則只允許完全匹配；未知時退回原本的「不是 Zero 即可」邏輯。
    /// </summary>
    bool IsValidDisplay2Hwnd(System.IntPtr hwnd)
    {
        if (hwnd == System.IntPtr.Zero) return false;
#if !UNITY_EDITOR
        if (_knownDisplay2Hwnd != System.IntPtr.Zero && hwnd != _knownDisplay2Hwnd)
            return false;
#endif
        return true;
    }

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
        if (!IsValidDisplay2Hwnd(hwnd))
        {
            if (debugLog) Debug.LogWarning($"[SensorClick] PostMessage: 目標視窗無效或不是 Display 2 ({absX},{absY}) hwnd=0x{hwnd.ToInt64():X}");
            return;
        }

        GetWindowRect(hwnd, out RECT rect);
        int lx = absX - rect.left;
        int ly = absY - rect.top;
        var lp = MakeLParam(lx, ly);


        PostMessage(hwnd, WM_LBUTTONDOWN, (System.IntPtr)MK_LBUTTON, lp);
        PostMessage(hwnd, WM_LBUTTONUP,   System.IntPtr.Zero,        lp);

        if (debugLog) Debug.Log($"[SensorClick] PostMessage 格({col},{row}) → 視窗局部({lx},{ly})");
        OnSimEvent?.Invoke($"PostMessage  格({col},{row})  視窗局部({lx},{ly})");
    }

    // ── SendInput 基礎 ────────────────────────────────────────────────────────

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

    (int normX, int normY) AbsToNorm(int absX, int absY)
    {
        int vdLeft   = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int vdTop    = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int vdWidth  = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int vdHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        return (
            (int)((long)(absX - vdLeft) * 65535 / vdWidth),
            (int)((long)(absY - vdTop)  * 65535 / vdHeight)
        );
    }

    void SendMouseMove(int normX, int normY)
    {
        var inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi.dx = normX; inputs[0].u.mi.dy = normY;
        inputs[0].u.mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK;
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    void SendMouseDown(int normX, int normY)
    {
        var inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi.dx = normX; inputs[0].u.mi.dy = normY;
        inputs[0].u.mi.dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK;
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    void SendMouseUp()
    {
        var inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi.dwFlags = MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK;
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    // ── 滑鼠點擊 ─────────────────────────────────────────────────────────────

    void SendInputClick(int absX, int absY, int col, int row)
    {
        var (normX, normY) = AbsToNorm(absX, absY);

        uint flags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK;
        var inputs = new INPUT[2];

        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi.dx = normX; inputs[0].u.mi.dy = normY;
        inputs[0].u.mi.dwFlags = MOUSEEVENTF_LEFTDOWN | flags;

        inputs[1].type = INPUT_MOUSE;
        inputs[1].u.mi.dx = normX; inputs[1].u.mi.dy = normY;
        inputs[1].u.mi.dwFlags = MOUSEEVENTF_LEFTUP | flags;


        uint sent = SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        LastSendInputTime = Time.realtimeSinceStartup;
        if (sent != 2)
            Debug.LogWarning($"[SensorClick] SendInput 只送出 {sent}/2（錯誤碼 {Marshal.GetLastWin32Error()}）");
        else if (debugLog)
            Debug.Log($"[SensorClick] 點擊 格({col},{row}) → 螢幕({absX},{absY})");

        OnSimEvent?.Invoke($"滑鼠·點擊  格({col},{row})  螢幕({absX},{absY})");
    }

    // ── 滑鼠按壓 ─────────────────────────────────────────────────────────────

    IEnumerator PressCoroutine(int absX, int absY, int col, int row)
    {
        var (normX, normY) = AbsToNorm(absX, absY);

        SendMouseDown(normX, normY);
        LastSendInputTime = Time.realtimeSinceStartup;
        OnSimEvent?.Invoke($"滑鼠·按壓開始  格({col},{row})  持續 {pressHoldDuration:F2}s");
        if (debugLog) Debug.Log($"[SensorClick] 按壓開始 格({col},{row}) → 螢幕({absX},{absY})");

        yield return new WaitForSeconds(pressHoldDuration);

        SendMouseUp();
        OnSimEvent?.Invoke($"滑鼠·按壓結束  格({col},{row})");
        if (debugLog) Debug.Log($"[SensorClick] 按壓結束 格({col},{row})");

        _pressCoroutine = null;
    }

    // ── 滑鼠拖曳 ─────────────────────────────────────────────────────────────

    void HandleDragTouch(int absX, int absY, int col, int row)
    {
        if (_dragReleaseCoroutine != null)
        {
            StopCoroutine(_dragReleaseCoroutine);
            _dragReleaseCoroutine = null;
        }

        var (normX, normY) = AbsToNorm(absX, absY);

        if (!_isDragging)
        {
            SendMouseDown(normX, normY);
            LastSendInputTime = Time.realtimeSinceStartup;
            _isDragging = true;
            OnSimEvent?.Invoke($"滑鼠·拖曳開始  格({col},{row})  螢幕({absX},{absY})");
            if (debugLog) Debug.Log($"[SensorClick] 拖曳開始 格({col},{row}) → 螢幕({absX},{absY})");
        }
        else
        {
            SendMouseMove(normX, normY);
            OnSimEvent?.Invoke($"滑鼠·拖曳移動  格({col},{row})  螢幕({absX},{absY})");
        }

        _dragReleaseCoroutine = StartCoroutine(DragReleaseCoroutine(col, row));
    }

    IEnumerator DragReleaseCoroutine(int col, int row)
    {
        yield return new WaitForSeconds(dragReleaseDelay);

        if (_isDragging)
        {
            SendMouseUp();
            _isDragging = false;
            OnSimEvent?.Invoke($"滑鼠·拖曳結束  格({col},{row})  超時 {dragReleaseDelay:F2}s");
            if (debugLog) Debug.Log($"[SensorClick] 拖曳結束 格({col},{row})（超時）");
        }

        _dragReleaseCoroutine = null;
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
            if (debugLog) Debug.Log($"[SensorClick] UDP 外送 格({col},{row}) → {externalIP}:{externalPort}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SensorClick] UDP 外送失敗: {e.Message}");
        }
    }

    // ── 智慧觸碰事件（由 SensorBus.OnTap / OnPressStart / OnPressEnd 觸發）────

    void OnSmartTap(int col, int row)
    {
        if (!tapClickEnabled) return;
        int absX = display2OffsetX + col * CellW + CellW / 2;
        int absY = display2OffsetY + row * CellH + CellH / 2;

        switch (_mode)
        {
            case ClickMode.Listen:
                if (!CheckCooldown()) return;
                if (debugLog) Debug.Log($"[SensorClick] 智慧·輕觸 監聽 格({col},{row})");
                OnSimEvent?.Invoke($"智慧·輕觸  監聽  格({col},{row})");
                if (externalEnabled) ExternalUdpSend(col, row);
                break;
            case ClickMode.PostMessage:
                if (!CheckCooldown()) return;
                PostMessageClick(absX, absY, col, row);
                OnSimEvent?.Invoke($"智慧·輕觸  PostMessage  格({col},{row})");
                break;
            case ClickMode.Mouse:
                SendInputClick(absX, absY, col, row);
                OnSimEvent?.Invoke($"智慧·輕觸  滑鼠  格({col},{row})");
                break;
        }
    }

    void OnSmartPressStart(int col, int row)
    {
        if (!pressClickEnabled) return;
        int absX = display2OffsetX + col * CellW + CellW / 2;
        int absY = display2OffsetY + row * CellH + CellH / 2;

        switch (_mode)
        {
            case ClickMode.Mouse:
            {
                if (_smartPressStarted) return;
                var (normX, normY) = AbsToNorm(absX, absY);
                SendMouseDown(normX, normY);
                LastSendInputTime = Time.realtimeSinceStartup;
                _smartPressStarted = true;
                if (debugLog) Debug.Log($"[SensorClick] 智慧·按壓開始 格({col},{row})");
                OnSimEvent?.Invoke($"智慧·按壓開始  格({col},{row})");
                break;
            }
            case ClickMode.PostMessage:
            {
                if (_smartPressStarted) return;
                var hwnd = WindowFromPoint(new POINT { x = absX, y = absY });
                if (!IsValidDisplay2Hwnd(hwnd)) break;
                GetWindowRect(hwnd, out RECT rect);
                var lp = MakeLParam(absX - rect.left, absY - rect.top);
        
                PostMessage(hwnd, WM_LBUTTONDOWN, (System.IntPtr)MK_LBUTTON, lp);
                _smartPressStarted = true;
                if (debugLog) Debug.Log($"[SensorClick] 智慧·按壓開始 PostMessage 格({col},{row})");
                OnSimEvent?.Invoke($"智慧·按壓開始  PostMessage  格({col},{row})");
                break;
            }
        }
    }

    void OnSmartPressEnd(int col, int row)
    {
        if (!pressClickEnabled) return;
        if (!_smartPressStarted) return;
        _smartPressStarted = false;

        int absX = display2OffsetX + col * CellW + CellW / 2;
        int absY = display2OffsetY + row * CellH + CellH / 2;

        switch (_mode)
        {
            case ClickMode.Mouse:
            {
                SendMouseUp();
                if (debugLog) Debug.Log($"[SensorClick] 智慧·按壓結束 格({col},{row})");
                OnSimEvent?.Invoke($"智慧·按壓結束  格({col},{row})");
                break;
            }
            case ClickMode.PostMessage:
            {
                var hwnd = WindowFromPoint(new POINT { x = absX, y = absY });
                if (!IsValidDisplay2Hwnd(hwnd)) break;
                GetWindowRect(hwnd, out RECT rect);
                var lp = MakeLParam(absX - rect.left, absY - rect.top);
                PostMessage(hwnd, WM_LBUTTONUP, System.IntPtr.Zero, lp);
                if (debugLog) Debug.Log($"[SensorClick] 智慧·按壓結束 PostMessage 格({col},{row})");
                OnSimEvent?.Invoke($"智慧·按壓結束  PostMessage  格({col},{row})");
                break;
            }
        }
    }

    void OnSmartDrag(int col, int row)
    {
        if (!dragClickEnabled) return;
        int absX = display2OffsetX + col * CellW + CellW / 2;
        int absY = display2OffsetY + row * CellH + CellH / 2;

        switch (_mode)
        {
            case ClickMode.Mouse:
            {
                if (!_smartPressStarted) return;
                var (normX, normY) = AbsToNorm(absX, absY);
                SendMouseMove(normX, normY);
                if (debugLog) Debug.Log($"[SensorClick] 智慧·拖曳 格({col},{row})");
                OnSimEvent?.Invoke($"智慧·拖曳  格({col},{row})");
                break;
            }
            case ClickMode.PostMessage:
            {
                if (!_smartPressStarted) return;
                var hwnd = WindowFromPoint(new POINT { x = absX, y = absY });
                if (!IsValidDisplay2Hwnd(hwnd)) break;
                GetWindowRect(hwnd, out RECT rect);
                var lp = MakeLParam(absX - rect.left, absY - rect.top);
                const uint WM_MOUSEMOVE = 0x0200;
                PostMessage(hwnd, WM_MOUSEMOVE, (System.IntPtr)MK_LBUTTON, lp);
                if (debugLog) Debug.Log($"[SensorClick] 智慧·拖曳 PostMessage 格({col},{row})");
                OnSimEvent?.Invoke($"智慧·拖曳  PostMessage  格({col},{row})");
                break;
            }
            case ClickMode.Listen:
            {
                if (debugLog) Debug.Log($"[SensorClick] 智慧·拖曳 監聽 格({col},{row})");
                OnSimEvent?.Invoke($"智慧·拖曳  監聽  格({col},{row})");
                if (externalEnabled) ExternalUdpSend(col, row);
                break;
            }
        }
    }
}
