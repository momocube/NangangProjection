using System.Runtime.InteropServices;
using TouchService.Logging;

namespace TouchService.Tray;

/// <summary>托盤互動 → 動作的對映（純資料，訊息/選單 dispatch 可單元測試）。</summary>
public enum TrayAction { None, OpenUi, ToggleConsole, Quit }

/// <summary>
/// 托盤訊息/選單 → 動作的純對映（平台無關，單元測試在任何平台可跑；
/// WindowsTrayHost 的 message loop 呼叫這裡——「呼叫時機」由這層測試釘住）。
/// </summary>
public static class TrayMenu
{
    public const uint WM_LBUTTONDBLCLK = 0x0203;
    public const uint WM_RBUTTONUP = 0x0205;
    public const uint WM_CONTEXTMENU = 0x007B;

    public const uint CMD_OPEN_UI = 1;
    public const uint CMD_TOGGLE_CONSOLE = 2;
    public const uint CMD_QUIT = 3;

    /// <summary>NotifyIcon 回呼（lParam 的滑鼠訊息）→ 動作。雙擊 = 開 UI；右鍵交給選單流程。</summary>
    public static TrayAction MapTrayMouseMessage(uint mouseMsg) => mouseMsg switch
    {
        WM_LBUTTONDBLCLK => TrayAction.OpenUi,
        _ => TrayAction.None,
    };

    /// <summary>右鍵是否該開 context menu。</summary>
    public static bool IsContextMenuTrigger(uint mouseMsg) =>
        mouseMsg is WM_RBUTTONUP or WM_CONTEXTMENU;

    /// <summary>右鍵選單 TrackPopupMenu(TPM_RETURNCMD) 回傳的指令 → 動作（0 = 取消）。</summary>
    public static TrayAction MapMenuCommand(uint cmd) => cmd switch
    {
        CMD_OPEN_UI => TrayAction.OpenUi,
        CMD_TOGGLE_CONSOLE => TrayAction.ToggleConsole,
        CMD_QUIT => TrayAction.Quit,
        _ => TrayAction.None,
    };
}

/// <summary>
/// Windows 托盤實作：P/Invoke Shell_NotifyIcon（**不是** WinForms NotifyIcon——
/// 保持單一 TFM net8.0 / console OutputType / exe 不變胖；取捨見 docs/ARCHITECTURE.md §3）。
///
/// 結構：獨立 STA thread 建一個 hidden window + GetMessage 迴圈，掛 NotifyIcon。
///   - 雙擊圖示 → openUi（複用 UiLauncher）
///   - 右鍵 → 選單：開啟介面 / 顯示|隱藏主控台 / 結束服務（TrackPopupMenu TPM_RETURNCMD）
///   - icon：ExtractAssociatedIcon(自身 exe)，失敗 fallback 系統預設圖示——不需要額外檔案
///   - TaskbarCreated 廣播（explorer 重啟）→ 重掛 icon，托盤不消失
/// 全部 Win32 呼叫只在 Windows 上執行（factory 守住；建構子雙保險）。
/// 純對映（MapTrayMouseMessage / MapMenuCommand）是 static，任何平台可單元測試。
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class WindowsTrayHost : ITrayHost
{
    const uint WM_APP_TRAY = 0x8000 + 1;   // WM_APP + 1：NotifyIcon 回呼訊息
    const uint WM_CLOSE = 0x0010;
    const uint WM_DESTROY = 0x0002;

    const uint NIF_MESSAGE = 0x01;
    const uint NIF_ICON = 0x02;
    const uint NIF_TIP = 0x04;
    const uint NIM_ADD = 0x00;
    const uint NIM_DELETE = 0x02;

    const uint MF_STRING = 0x0000;
    const uint MF_SEPARATOR = 0x0800;
    const uint TPM_RIGHTBUTTON = 0x0002;
    const uint TPM_RETURNCMD = 0x0100;
    const uint TPM_NONOTIFY = 0x0080;

    readonly FileLogger _log;
    readonly Action _openUi;
    readonly Action _toggleConsole;
    readonly Action _quit;
    readonly Func<bool> _isConsoleVisible;
    readonly ManualResetEventSlim _ready = new(false);

    Thread? _thread;
    IntPtr _hwnd;
    IntPtr _icon;
    uint _taskbarCreatedMsg;
    WndProcDelegate? _wndProc;   // 必須保留參考——GC 掉 delegate 會讓 native callback 爆掉
    volatile bool _disposed;

    public WindowsTrayHost(FileLogger log, Action openUi, Action toggleConsole, Action quit,
                           Func<bool> isConsoleVisible)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WindowsTrayHost 僅 Windows（factory 應已守住）");
        _log = log;
        _openUi = openUi;
        _toggleConsole = toggleConsole;
        _quit = quit;
        _isConsoleVisible = isConsoleVisible;
    }

    void Dispatch(TrayAction action)
    {
        try
        {
            switch (action)
            {
                case TrayAction.OpenUi: _openUi(); break;
                case TrayAction.ToggleConsole: _toggleConsole(); break;
                case TrayAction.Quit: _quit(); break;
            }
        }
        catch (Exception ex)
        {
            _log.Error($"[Tray] 動作 {action} 失敗：{ex.Message}");
        }
    }

    // ── 生命週期 ─────────────────────────────────────────────────────────────

    public void Start()
    {
        if (_thread != null) return;
        _thread = new Thread(TrayThread) { IsBackground = true, Name = "TrayHost" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();

        if (!_ready.Wait(3000))
            _log.Warn("[Tray] 托盤初始化逾時（不影響服務；UI 仍可由瀏覽器手動開）");
    }

    void TrayThread()
    {
        try
        {
            _wndProc = WndProc;
            _taskbarCreatedMsg = RegisterWindowMessageW("TaskbarCreated");

            var cls = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                hInstance = GetModuleHandleW(null),
                lpszClassName = "TouchServiceTrayWnd",
            };
            if (RegisterClassExW(ref cls) == 0)
                throw new InvalidOperationException($"RegisterClassEx 失敗（{Marshal.GetLastWin32Error()}）");

            // 一般 hidden top-level window（不用 message-only：要收 TaskbarCreated 廣播）
            _hwnd = CreateWindowExW(0, "TouchServiceTrayWnd", "TouchService",
                0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, cls.hInstance, IntPtr.Zero);
            if (_hwnd == IntPtr.Zero)
                throw new InvalidOperationException($"CreateWindowEx 失敗（{Marshal.GetLastWin32Error()}）");

            _icon = LoadTrayIcon();
            AddIcon();
            _log.Info("[Tray] 托盤圖示已掛上（雙擊開介面；右鍵：開啟介面 / 主控台 / 結束服務）");
            _ready.Set();

            while (GetMessageW(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessageW(ref msg);
            }
        }
        catch (Exception ex)
        {
            _log.Warn($"[Tray] 托盤啟動失敗（服務照常；console 視窗不受影響）：{ex.Message}");
            _ready.Set();
        }
    }

    IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_APP_TRAY)
        {
            uint mouseMsg = (uint)(lParam.ToInt64() & 0xFFFF);
            if (TrayMenu.IsContextMenuTrigger(mouseMsg)) ShowContextMenu();
            else Dispatch(TrayMenu.MapTrayMouseMessage(mouseMsg));
            return IntPtr.Zero;
        }
        if (msg == _taskbarCreatedMsg && _taskbarCreatedMsg != 0)
        {
            AddIcon();   // explorer 重啟：重掛 icon
            return IntPtr.Zero;
        }
        if (msg == WM_DESTROY)
        {
            var nid = MakeIconData();
            Shell_NotifyIconW(NIM_DELETE, ref nid);
            PostQuitMessage(0);
            return IntPtr.Zero;
        }
        return DefWindowProcW(hwnd, msg, wParam, lParam);
    }

    void ShowContextMenu()
    {
        IntPtr menu = CreatePopupMenu();
        try
        {
            AppendMenuW(menu, MF_STRING, TrayMenu.CMD_OPEN_UI, "開啟介面");
            AppendMenuW(menu, MF_STRING, TrayMenu.CMD_TOGGLE_CONSOLE,
                _isConsoleVisible() ? "隱藏主控台" : "顯示主控台");
            AppendMenuW(menu, MF_SEPARATOR, 0, null);
            AppendMenuW(menu, MF_STRING, TrayMenu.CMD_QUIT, "結束服務");

            SetForegroundWindow(_hwnd);   // MSDN 慣例：否則點選單外面選單不會自動關
            GetCursorPos(out var pt);
            uint cmd = TrackPopupMenuEx(menu,
                TPM_RIGHTBUTTON | TPM_RETURNCMD | TPM_NONOTIFY, pt.X, pt.Y, _hwnd, IntPtr.Zero);
            Dispatch(TrayMenu.MapMenuCommand(cmd));
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    /// <summary>自身 exe 的關聯圖示（單檔發布也可取）；失敗 fallback 系統預設應用程式圖示。</summary>
    IntPtr LoadTrayIcon()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (exe != null)
            {
                ushort idx = 0;
                var h = ExtractAssociatedIconW(IntPtr.Zero, new System.Text.StringBuilder(exe, 260), ref idx);
                if (h != IntPtr.Zero) return h;
            }
        }
        catch { /* fallback */ }
        return LoadIconW(IntPtr.Zero, new IntPtr(32512));   // IDI_APPLICATION
    }

    NOTIFYICONDATA MakeIconData() => new()
    {
        cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
        hWnd = _hwnd,
        uID = 1,
        uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
        uCallbackMessage = WM_APP_TRAY,
        hIcon = _icon,
        szTip = "TouchService（雙擊開介面）",
    };

    void AddIcon()
    {
        var nid = MakeIconData();
        if (!Shell_NotifyIconW(NIM_ADD, ref nid))
            _log.Warn("[Tray] Shell_NotifyIcon NIM_ADD 失敗（托盤圖示可能不顯示）");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_thread != null && _hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _thread.Join(2000);
        }
        _ready.Dispose();
    }

    // ── P/Invoke ─────────────────────────────────────────────────────────────

    delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern ushort RegisterClassExW(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateWindowExW(uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int w, int h,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetMessageW(out MSG lpMsg, IntPtr hWnd, uint min, uint max);

    [DllImport("user32.dll")]
    static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr DispatchMessageW(ref MSG lpMsg);

    [DllImport("user32.dll")]
    static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern uint RegisterWindowMessageW(string lpString);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern bool Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr ExtractAssociatedIconW(IntPtr hInst, System.Text.StringBuilder pszIconPath, ref ushort piIcon);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr LoadIconW(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool AppendMenuW(IntPtr hMenu, uint uFlags, uint uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll")]
    static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    static extern uint TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr GetModuleHandleW(string? lpModuleName);
}
