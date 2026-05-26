using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

/// <summary>
/// 感應觸碰 → Display 2 點擊事件。
/// 訂閱 SensorBus.OnCanvas* 事件（含面 origin 的畫布像素座標），支援三種主模式：
///
///   監聽       — 走 SensorBus 廣播；若 externalEnabled=true 同時送 UDP
///   PostMessage — 直送目標視窗訊息，游標不動
///   滑鼠       — SendInput 模擬，子行為由 mouseBehavior 決定（點擊／按壓／拖曳）
///
/// 注意：改用 Canvas 事件後座標已包含多面的 originX/Y，
/// 704 等多面場域點擊位置正確（不再只根據面內 col/row 計算）。
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

    // 固定標記：每個由本腳本發出的 SendInput 事件都帶此 dwExtraInfo，
    // 供 Display1GraphicRaycaster 識別感應器合成事件，避免誤觸 Display 1 UI。
    internal const long SensorInputExtraInfo = unchecked((long)0x4E4E4753524C4F4EL);

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

        // 使用 Canvas 事件：座標已含面的 originX/Y，多面場域（704）位置正確
        SensorBus.OnCanvasTouch      -= OnCanvasTouch;
        SensorBus.OnCanvasTouch      += OnCanvasTouch;
        SensorBus.OnCanvasTap        -= OnCanvasTap;
        SensorBus.OnCanvasTap        += OnCanvasTap;
        SensorBus.OnCanvasPressStart -= OnCanvasPressStart;
        SensorBus.OnCanvasPressStart += OnCanvasPressStart;
        SensorBus.OnCanvasPressEnd   -= OnCanvasPressEnd;
        SensorBus.OnCanvasPressEnd   += OnCanvasPressEnd;
        SensorBus.OnCanvasDrag       -= OnCanvasDrag;
        SensorBus.OnCanvasDrag       += OnCanvasDrag;
    }

    void OnDisable()
    {
        SensorBus.OnCanvasTouch      -= OnCanvasTouch;
        SensorBus.OnCanvasTap        -= OnCanvasTap;
        SensorBus.OnCanvasPressStart -= OnCanvasPressStart;
        SensorBus.OnCanvasPressEnd   -= OnCanvasPressEnd;
        SensorBus.OnCanvasDrag       -= OnCanvasDrag;
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

    // ── 觸碰處理（畫布像素座標，含面 origin）────────────────────────────────

    void OnCanvasTouch(float cx, float cy, float cw, float ch)
    {
        int absX = display2OffsetX + Mathf.RoundToInt(cx);
        int absY = display2OffsetY + Mathf.RoundToInt(cy);

        switch (_mode)
        {
            case ClickMode.Listen:
                if (!CheckCooldown()) return;
                if (debugLog) Debug.Log($"[SensorClick] 監聽 畫布({cx:F0},{cy:F0})");
                OnSimEvent?.Invoke($"監聽  畫布({cx:F0},{cy:F0})");
                if (externalEnabled) ExternalUdpSend(cx, cy);
                break;

            case ClickMode.PostMessage:
                if (!CheckCooldown()) return;
                PostMessageClick(absX, absY, cx, cy);
                break;

            case ClickMode.Mouse:
                switch (mouseBehavior)
                {
                    case MouseBehavior.Click:
                        if (!CheckCooldown()) return;
                        SendInputClick(absX, absY, cx, cy);
                        break;
                    case MouseBehavior.Press:
                        if (_pressCoroutine == null)
                            _pressCoroutine = StartCoroutine(PressCoroutine(absX, absY, cx, cy));
                        break;
                    case MouseBehavior.Drag:
                        HandleDragTouch(absX, absY, cx, cy);
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
        if (_knownDisplay2Hwnd == System.IntPtr.Zero) return false;
        return hwnd == _knownDisplay2Hwnd;
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

    void PostMessageClick(int absX, int absY, float cx, float cy)
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

        if (debugLog) Debug.Log($"[SensorClick] PostMessage 畫布({cx:F0},{cy:F0}) → 視窗局部({lx},{ly})");
        OnSimEvent?.Invoke($"PostMessage  畫布({cx:F0},{cy:F0})  視窗局部({lx},{ly})");
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
        inputs[0].u.mi.dwExtraInfo = new System.IntPtr(SensorInputExtraInfo);
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    void SendMouseDown(int normX, int normY)
    {
        var inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi.dx = normX; inputs[0].u.mi.dy = normY;
        inputs[0].u.mi.dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK;
        inputs[0].u.mi.dwExtraInfo = new System.IntPtr(SensorInputExtraInfo);
        Display1GraphicRaycaster.NotifySensorClick();
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    void SendMouseUp()
    {
        var inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi.dwFlags = MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK;
        inputs[0].u.mi.dwExtraInfo = new System.IntPtr(SensorInputExtraInfo);
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    // ── 滑鼠點擊 ─────────────────────────────────────────────────────────────

    void SendInputClick(int absX, int absY, float cx, float cy)
    {
        var (normX, normY) = AbsToNorm(absX, absY);

        uint flags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK;
        var inputs = new INPUT[2];

        var marker = new System.IntPtr(SensorInputExtraInfo);
        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi.dx = normX; inputs[0].u.mi.dy = normY;
        inputs[0].u.mi.dwFlags = MOUSEEVENTF_LEFTDOWN | flags;
        inputs[0].u.mi.dwExtraInfo = marker;

        inputs[1].type = INPUT_MOUSE;
        inputs[1].u.mi.dx = normX; inputs[1].u.mi.dy = normY;
        inputs[1].u.mi.dwFlags = MOUSEEVENTF_LEFTUP | flags;
        inputs[1].u.mi.dwExtraInfo = marker;

        Display1GraphicRaycaster.NotifySensorClick();
        uint sent = SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        LastSendInputTime = Time.realtimeSinceStartup;
        if (sent != 2)
            Debug.LogWarning($"[SensorClick] SendInput 只送出 {sent}/2（錯誤碼 {Marshal.GetLastWin32Error()}）");
        else if (debugLog)
            Debug.Log($"[SensorClick] 點擊 畫布({cx:F0},{cy:F0}) → 螢幕({absX},{absY})");

        OnSimEvent?.Invoke($"滑鼠·點擊  畫布({cx:F0},{cy:F0})  螢幕({absX},{absY})");
    }

    // ── 滑鼠按壓 ─────────────────────────────────────────────────────────────

    IEnumerator PressCoroutine(int absX, int absY, float cx, float cy)
    {
        var (normX, normY) = AbsToNorm(absX, absY);

        SendMouseDown(normX, normY);
        LastSendInputTime = Time.realtimeSinceStartup;
        OnSimEvent?.Invoke($"滑鼠·按壓開始  畫布({cx:F0},{cy:F0})  持續 {pressHoldDuration:F2}s");
        if (debugLog) Debug.Log($"[SensorClick] 按壓開始 畫布({cx:F0},{cy:F0}) → 螢幕({absX},{absY})");

        yield return new WaitForSeconds(pressHoldDuration);

        SendMouseUp();
        OnSimEvent?.Invoke($"滑鼠·按壓結束  畫布({cx:F0},{cy:F0})");
        if (debugLog) Debug.Log($"[SensorClick] 按壓結束 畫布({cx:F0},{cy:F0})");

        _pressCoroutine = null;
    }

    // ── 滑鼠拖曳 ─────────────────────────────────────────────────────────────

    void HandleDragTouch(int absX, int absY, float cx, float cy)
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
            OnSimEvent?.Invoke($"滑鼠·拖曳開始  畫布({cx:F0},{cy:F0})  螢幕({absX},{absY})");
            if (debugLog) Debug.Log($"[SensorClick] 拖曳開始 畫布({cx:F0},{cy:F0}) → 螢幕({absX},{absY})");
        }
        else
        {
            SendMouseMove(normX, normY);
            OnSimEvent?.Invoke($"滑鼠·拖曳移動  畫布({cx:F0},{cy:F0})  螢幕({absX},{absY})");
        }

        _dragReleaseCoroutine = StartCoroutine(DragReleaseCoroutine(cx, cy));
    }

    IEnumerator DragReleaseCoroutine(float cx, float cy)
    {
        yield return new WaitForSeconds(dragReleaseDelay);

        if (_isDragging)
        {
            SendMouseUp();
            _isDragging = false;
            OnSimEvent?.Invoke($"滑鼠·拖曳結束  畫布({cx:F0},{cy:F0})  超時 {dragReleaseDelay:F2}s");
            if (debugLog) Debug.Log($"[SensorClick] 拖曳結束 畫布({cx:F0},{cy:F0})（超時）");
        }

        _dragReleaseCoroutine = null;
    }

    // ── External UDP ──────────────────────────────────────────────────────────

    void ExternalUdpSend(float cx, float cy)
    {
        if (_udp == null) return;
        string json  = $"{{\"x\":{Mathf.RoundToInt(cx)},\"y\":{Mathf.RoundToInt(cy)}}}";
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        try
        {
            _udp.Send(bytes, bytes.Length, _udpEndpoint);
            if (debugLog) Debug.Log($"[SensorClick] UDP 外送 畫布({cx:F0},{cy:F0}) → {externalIP}:{externalPort}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SensorClick] UDP 外送失敗: {e.Message}");
        }
    }

    // ── 智慧觸碰事件（由 SensorBus.OnCanvas* 觸發）───────────────────────────

    void OnCanvasTap(float cx, float cy, float cw, float ch)
    {
        if (!tapClickEnabled) return;
        int absX = display2OffsetX + Mathf.RoundToInt(cx);
        int absY = display2OffsetY + Mathf.RoundToInt(cy);

        switch (_mode)
        {
            case ClickMode.Listen:
                if (!CheckCooldown()) return;
                if (debugLog) Debug.Log($"[SensorClick] 智慧·輕觸 監聽 畫布({cx:F0},{cy:F0})");
                OnSimEvent?.Invoke($"智慧·輕觸  監聽  畫布({cx:F0},{cy:F0})");
                if (externalEnabled) ExternalUdpSend(cx, cy);
                break;
            case ClickMode.PostMessage:
                if (!CheckCooldown()) return;
                PostMessageClick(absX, absY, cx, cy);
                OnSimEvent?.Invoke($"智慧·輕觸  PostMessage  畫布({cx:F0},{cy:F0})");
                break;
            case ClickMode.Mouse:
                SendInputClick(absX, absY, cx, cy);
                OnSimEvent?.Invoke($"智慧·輕觸  滑鼠  畫布({cx:F0},{cy:F0})");
                break;
        }
    }

    void OnCanvasPressStart(float cx, float cy, float cw, float ch)
    {
        if (!pressClickEnabled) return;
        int absX = display2OffsetX + Mathf.RoundToInt(cx);
        int absY = display2OffsetY + Mathf.RoundToInt(cy);

        switch (_mode)
        {
            case ClickMode.Mouse:
            {
                if (_smartPressStarted) return;
                var (normX, normY) = AbsToNorm(absX, absY);
                SendMouseDown(normX, normY);
                LastSendInputTime = Time.realtimeSinceStartup;
                _smartPressStarted = true;
                if (debugLog) Debug.Log($"[SensorClick] 智慧·按壓開始 畫布({cx:F0},{cy:F0})");
                OnSimEvent?.Invoke($"智慧·按壓開始  畫布({cx:F0},{cy:F0})");
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
                if (debugLog) Debug.Log($"[SensorClick] 智慧·按壓開始 PostMessage 畫布({cx:F0},{cy:F0})");
                OnSimEvent?.Invoke($"智慧·按壓開始  PostMessage  畫布({cx:F0},{cy:F0})");
                break;
            }
        }
    }

    void OnCanvasPressEnd(float cx, float cy, float cw, float ch)
    {
        if (!pressClickEnabled) return;
        if (!_smartPressStarted) return;
        _smartPressStarted = false;

        int absX = display2OffsetX + Mathf.RoundToInt(cx);
        int absY = display2OffsetY + Mathf.RoundToInt(cy);

        switch (_mode)
        {
            case ClickMode.Mouse:
            {
                SendMouseUp();
                if (debugLog) Debug.Log($"[SensorClick] 智慧·按壓結束 畫布({cx:F0},{cy:F0})");
                OnSimEvent?.Invoke($"智慧·按壓結束  畫布({cx:F0},{cy:F0})");
                break;
            }
            case ClickMode.PostMessage:
            {
                var hwnd = WindowFromPoint(new POINT { x = absX, y = absY });
                if (!IsValidDisplay2Hwnd(hwnd)) break;
                GetWindowRect(hwnd, out RECT rect);
                var lp = MakeLParam(absX - rect.left, absY - rect.top);
                PostMessage(hwnd, WM_LBUTTONUP, System.IntPtr.Zero, lp);
                if (debugLog) Debug.Log($"[SensorClick] 智慧·按壓結束 PostMessage 畫布({cx:F0},{cy:F0})");
                OnSimEvent?.Invoke($"智慧·按壓結束  PostMessage  畫布({cx:F0},{cy:F0})");
                break;
            }
        }
    }

    void OnCanvasDrag(float cx, float cy, float cw, float ch)
    {
        if (!dragClickEnabled) return;
        int absX = display2OffsetX + Mathf.RoundToInt(cx);
        int absY = display2OffsetY + Mathf.RoundToInt(cy);

        switch (_mode)
        {
            case ClickMode.Mouse:
            {
                if (!_smartPressStarted) return;
                var (normX, normY) = AbsToNorm(absX, absY);
                SendMouseMove(normX, normY);
                if (debugLog) Debug.Log($"[SensorClick] 智慧·拖曳 畫布({cx:F0},{cy:F0})");
                OnSimEvent?.Invoke($"智慧·拖曳  畫布({cx:F0},{cy:F0})");
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
                if (debugLog) Debug.Log($"[SensorClick] 智慧·拖曳 PostMessage 畫布({cx:F0},{cy:F0})");
                OnSimEvent?.Invoke($"智慧·拖曳  PostMessage  畫布({cx:F0},{cy:F0})");
                break;
            }
            case ClickMode.Listen:
            {
                if (debugLog) Debug.Log($"[SensorClick] 智慧·拖曳 監聽 畫布({cx:F0},{cy:F0})");
                OnSimEvent?.Invoke($"智慧·拖曳  監聽  畫布({cx:F0},{cy:F0})");
                if (externalEnabled) ExternalUdpSend(cx, cy);
                break;
            }
        }
    }
}
