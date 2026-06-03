using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 將 Display 1 視窗縮小成右下角無邊框迷你面板（Always On Top、可拖曳）。
/// 將此 Component 掛到場景中任一 GameObject 即可；不需要額外 Inspector 設定。
/// </summary>
public class MiniWindowController : MonoBehaviour
{
    public static MiniWindowController Instance { get; private set; }

    [Header("▶ 控制台縮小視窗 ─ 提供右下角迷你視窗縮小按鈕。停用此元件 → 縮小按鈕消失")]
    [Tooltip("Display 2 的 X 起始像素（需與 SensorTestManager.display2WindowOffsetX 一致）。")]
    public int display2WindowOffsetX = 1920;

    [Tooltip("迷你面板按鈕的 TMP 字體 Asset（留空則使用 TMP Settings 預設）。")]
    public TMP_FontAsset uiFontOverride;

    const int MiniW = 280;
    const int MiniH = 80;

#if !UNITY_EDITOR
    [StructLayout(LayoutKind.Sequential)] struct RECT  { public int left, top, right, bottom; }
    [StructLayout(LayoutKind.Sequential)] struct POINT { public int x, y; }

    delegate bool EnumWindowsProc(System.IntPtr hWnd, System.IntPtr lParam);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc proc, System.IntPtr lParam);
    [DllImport("user32.dll")] static extern int  GetClassName(System.IntPtr hWnd, System.Text.StringBuilder sb, int max);
    [DllImport("user32.dll")] static extern bool GetWindowRect(System.IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll")] static extern int  GetSystemMetrics(int nIndex);
    [DllImport("user32.dll")] static extern int  GetWindowLong(System.IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] static extern int  SetWindowLong(System.IntPtr hWnd, int nIndex, int dwNewLong);

    static readonly System.IntPtr HWND_TOPMOST   = new System.IntPtr(-1);
    static readonly System.IntPtr HWND_NOTOPMOST = new System.IntPtr(-2);
    const uint SWP_SHOWWINDOW   = 0x0040;
    const uint SWP_FRAMECHANGED = 0x0020;  // 通知 Windows 框架已改變，強制重繪非客戶區
    const int  SM_CXSCREEN      = 0;
    const int  SM_CYSCREEN      = 1;
    const int  GWL_STYLE        = -16;
    const int  WS_CAPTION       = 0x00C00000;  // 標題列（含細框）
    const int  WS_THICKFRAME    = 0x00040000;  // 可縮放粗框

    System.IntPtr _hwnd = System.IntPtr.Zero;
    RECT _savedRect;
    int  _savedStyle;
    POINT _dragStartCursor;
    RECT  _dragStartWinRect;
#endif

    bool   _isMini;
    Canvas _miniCanvas;

    void Awake()
    {
        Instance = this;
        BuildMiniCanvas();
    }

    IEnumerator Start()
    {
        yield return null;   // 等一幀讓視窗完成初始化
#if !UNITY_EDITOR
        // 強制全螢幕：防止上次以迷你模式退出後，Unity 從 Registry 讀到 280×80 並用小視窗啟動
        var res = Screen.currentResolution;  // 桌面解析度，不受 Unity 視窗大小影響
        Screen.SetResolution(res.width, res.height, FullScreenMode.FullScreenWindow);
        yield return null;   // 等 Unity 套用解析度變更

        int threshold = display2WindowOffsetX;
        EnumWindows((hwnd, _) =>
        {
            var sb = new System.Text.StringBuilder(128);
            GetClassName(hwnd, sb, 128);
            if (sb.ToString() != "UnityWndClass") return true;
            GetWindowRect(hwnd, out RECT rect);
            if (rect.left < threshold) { _hwnd = hwnd; return false; }
            return true;
        }, System.IntPtr.Zero);

        if (_hwnd == System.IntPtr.Zero)
            Debug.LogWarning("[MiniWindow] 找不到 Display 1 Unity 視窗 HWND。");
        else
            Debug.Log($"[MiniWindow] Display 1 HWND = 0x{_hwnd.ToInt64():X}");
#endif
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── 縮小 / 還原 ───────────────────────────────────────────────────────────

    public void Minimize()
    {
        if (_isMini) return;
        _isMini = true;

#if !UNITY_EDITOR
        if (_hwnd != System.IntPtr.Zero)
        {
            GetWindowRect(_hwnd, out _savedRect);
            // 移除標題列與縮放框，變成無邊框視窗
            _savedStyle = GetWindowLong(_hwnd, GWL_STYLE);
            SetWindowLong(_hwnd, GWL_STYLE, _savedStyle & ~(WS_CAPTION | WS_THICKFRAME));
            int scrW = GetSystemMetrics(SM_CXSCREEN);
            int scrH = GetSystemMetrics(SM_CYSCREEN);
            int x    = scrW - MiniW - 8;
            int y    = scrH - MiniH - 8;
            SetWindowPos(_hwnd, HWND_TOPMOST, x, y, MiniW, MiniH, SWP_SHOWWINDOW | SWP_FRAMECHANGED);
        }
#endif

        if (_miniCanvas != null) _miniCanvas.gameObject.SetActive(true);
    }

    public void Restore()
    {
        if (!_isMini) return;
        _isMini = false;

#if !UNITY_EDITOR
        if (_hwnd != System.IntPtr.Zero)
        {
            // 先還原視窗樣式再還原大小，避免非客戶區計算錯誤
            SetWindowLong(_hwnd, GWL_STYLE, _savedStyle);
            SetWindowPos(_hwnd, HWND_NOTOPMOST,
                _savedRect.left, _savedRect.top,
                _savedRect.right  - _savedRect.left,
                _savedRect.bottom - _savedRect.top,
                SWP_SHOWWINDOW | SWP_FRAMECHANGED);
        }
#endif

        if (_miniCanvas != null) _miniCanvas.gameObject.SetActive(false);
    }

    // ── 拖曳（由 MiniPanelDragHandler 呼叫）──────────────────────────────────

    public void OnMiniBeginDrag()
    {
#if !UNITY_EDITOR
        if (_hwnd == System.IntPtr.Zero) return;
        GetCursorPos(out _dragStartCursor);
        GetWindowRect(_hwnd, out _dragStartWinRect);
#endif
    }

    public void OnMiniDrag()
    {
#if !UNITY_EDITOR
        if (_hwnd == System.IntPtr.Zero) return;
        GetCursorPos(out POINT cur);
        int dx = cur.x - _dragStartCursor.x;
        int dy = cur.y - _dragStartCursor.y;
        SetWindowPos(_hwnd, HWND_TOPMOST,
            _dragStartWinRect.left + dx,
            _dragStartWinRect.top  + dy,
            MiniW, MiniH, SWP_SHOWWINDOW);
#endif
    }

    // ── 迷你 Canvas 建立 ──────────────────────────────────────────────────────

    void BuildMiniCanvas()
    {
        var canvasGO = new GameObject("MiniWindowCanvas");
        canvasGO.transform.SetParent(transform, false);
        _miniCanvas = canvasGO.AddComponent<Canvas>();
        _miniCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _miniCanvas.sortingOrder = 999;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // 深色背景（可拖曳整個面板）
        var bgGO  = new GameObject("MiniPanel");
        bgGO.layer = 5;
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRT  = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.09f, 0.11f, 0.17f, 1f);
        var drag  = bgGO.AddComponent<MiniPanelDragHandler>();
        drag.controller = this;

        // 還原按鈕
        var btnGO  = new GameObject("BtnRestore");
        btnGO.layer = 5;
        btnGO.transform.SetParent(bgGO.transform, false);
        var btnRT  = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.06f, 0.12f);
        btnRT.anchorMax = new Vector2(0.94f, 0.88f);
        btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.20f, 0.35f, 0.55f, 1f);
        var btn    = btnGO.AddComponent<Button>();
        var cb     = ColorBlock.defaultColorBlock;
        cb.normalColor      = new Color(0.20f, 0.35f, 0.55f);
        cb.highlightedColor = new Color(0.28f, 0.46f, 0.70f);
        cb.pressedColor     = new Color(0.14f, 0.25f, 0.40f);
        cb.colorMultiplier  = 1f;
        btn.colors        = cb;
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(Restore);

        var txtGO = new GameObject("Label");
        txtGO.layer = 5;
        txtGO.transform.SetParent(btnGO.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        var txt   = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text          = "還原控制台";
        txt.fontSize      = 16;
        txt.alignment     = TextAlignmentOptions.Midline;
        txt.color         = new Color(0.85f, 0.90f, 1.00f);
        txt.raycastTarget = false;
        txt.overflowMode  = TextOverflowModes.Overflow;
        var font = uiFontOverride != null ? uiFontOverride : TMP_Settings.defaultFontAsset;
        if (font != null) txt.font = font;

        canvasGO.SetActive(false);
    }
}

/// <summary>迷你面板的拖曳事件轉發器。</summary>
public class MiniPanelDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public MiniWindowController controller;
    public void OnBeginDrag(PointerEventData _) => controller?.OnMiniBeginDrag();
    public void OnDrag(PointerEventData _)      => controller?.OnMiniDrag();
}
