using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 雙螢幕感應測試主控。
///
/// 主螢幕 Display 0 (1920x1080)：控制格 128x24，右鍵鎖定 / 解鎖格子。
/// 次螢幕 Display 1 (2048x768)：感應格 128x24，每格 16x32 px，觸摸後亮紅 1.5 秒。
///
/// 座標校準來自 2026-04-08 四角測試：
///   左 X ≈ 1928  右 X ≈ 3960
///   上 Y ≈ 16    下 Y ≈ 752
/// </summary>
public class SensorTestManager : MonoBehaviour
{
    [Header("原始座標校準（現場四角測試）")]
    public float rawMinX = 1928f;
    public float rawMaxX = 3960f;
    public float rawMinY = 16f;
    public float rawMaxY = 752f;

    [Header("格子數量")]
    public int cols = 128;
    public int rows = 24;

    [Header("場景引用（Inspector 設定）")]
    public RectTransform controlGridParent;   // Canvas_Primary 的子容器
    public RectTransform sensorGridParent;    // Canvas_Secondary 的子容器

    [Header("遊戲場景（留空則不自動載入）")]
    public string gameSceneName = "Map_CybercubeLogo_J_2048x768";

    // 執行期
    SensorTileCell[]  _sensorTiles;
    ControlTileCell[] _controlTiles;
    bool[]            _locked;
    int               _lockedCount;
    Text              _statusText;

    // 智慧鎖格
    bool   _smartLockActive;
    Text   _smartLockBtnText;
    Image  _smartLockBtnImage;

    // 晶片示意（Display 2 感應格）
    GameObject _canvasSecondary;
    bool       _gameTileVisible = true;
    Text       _gameTileBtnText;
    Image      _gameTileBtnImage;

    // Display 2 感應格畫面捕捉（→ SensorGridRT）
    Camera     _sensorGridCam;
    GameObject _display2MirrorCanvas;

    // 點擊模式切換
    SensorClickSimulator _clickSim;
    Text                 _clickModeBtnText;
    Image                _clickModeBtnImage;

    // 感應冷卻調整
    Text  _cooldownText;
    Text  _cooldownToggleBtnText;
    Image _cooldownToggleBtnImage;

    // 拖曳互動
    int  _dragLastIdx    = -1;
    int  _dragInitIdx    = -1;  // 右鍵拖曳起始格
    bool _dragIsLock;            // 右鍵拖曳方向：true=鎖定 false=解鎖
    bool _dragInitPending;       // 延一幀確認方向（避免與 ToggleLock 時序競爭）

    // Display 2 視窗控制（獨立執行檔專用，Editor 內無效）
    System.IntPtr _display2Hwnd = System.IntPtr.Zero;

    [Header("Display 2 視窗偵測")]
    [Tooltip("Display 2 在 Windows 桌面的 X 起始位置（與 SensorClickSimulator 一致，預設 1920）。\n用來從多個 UnityWndClass 視窗中定位 Display 2 視窗。")]
    public int display2WindowOffsetX = 1920;

    // ── Windows API ──────────────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int left, top, right, bottom; }

    delegate bool EnumWindowsProc(System.IntPtr hWnd, System.IntPtr lParam);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc proc, System.IntPtr lParam);
    [DllImport("user32.dll")] static extern int  GetClassName(System.IntPtr hWnd, System.Text.StringBuilder sb, int max);
    [DllImport("user32.dll")] static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] static extern bool GetWindowRect(System.IntPtr hWnd, out RECT lpRect);
    const int SW_HIDE = 0;
    const int SW_SHOW = 5;

    // ── 生命週期 ──────────────────────────────────────────────────────────────

    void Awake()
    {
        // 嘗試啟用 Display 2 (2048x768)
        if (Display.displays.Length > 1)
            Display.displays[1].Activate(2048, 768, 60);

        Debug.Log($"[SensorTest] 偵測到 {Display.displays.Length} 個螢幕 | " +
                  $"Canvas_Primary → Display 1 | Canvas_Secondary → Display 2");
    }

    IEnumerator Start()
    {
        int total = cols * rows;
        _locked       = new bool[total];
        _sensorTiles  = new SensorTileCell[total];
        _controlTiles = new ControlTileCell[total];

        BuildSensorBackground();
        BuildSensorGrid();
        BuildPrimaryUI();
        BuildControlGrid();

        // 記錄 Canvas_Secondary 供晶片示意開關使用
        if (sensorGridParent != null)
            _canvasSecondary = sensorGridParent.GetComponentInParent<Canvas>()?.gameObject;

        yield return null;           // 等一幀讓 RectTransform 完成佈局
        FixControlCellSize();
        SetupSensorGridRT();         // 建立感應格 RT 捕捉與 Display 2 鏡像
        _clickSim = FindObjectOfType<SensorClickSimulator>();
        UpdateClickModeButton();
        UpdateCooldownToggleButton();
        UpdateCooldownText();

#if !UNITY_EDITOR
        StartCoroutine(FindDisplay2WindowCoroutine());
#endif

        UdpSensorReceiver.OnPacketReceived += OnUdpPacket;
        SetStatusIdle();

        if (!string.IsNullOrEmpty(gameSceneName))
        {
            // 場景需先加入 File → Build Settings，否則略過並提示
            bool found = false;
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                if (path.Contains(gameSceneName)) { found = true; break; }
            }
            if (found)
                SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Additive);
            else
                Debug.LogWarning($"[SensorTest] 場景 '{gameSceneName}' 未加入 Build Settings，略過載入。請至 File → Build Settings 加入，或清空 gameSceneName 欄位。");
        }
    }

    void OnDestroy()
    {
        UdpSensorReceiver.OnPacketReceived -= OnUdpPacket;
        SensorGridRT.Release();
    }

    // ── 次螢幕：感應格 ────────────────────────────────────────────────────────

    void BuildSensorBackground()
    {
        if (sensorGridParent == null) return;
        var go = new GameObject("SensorBG");
        go.layer = 5;
        go.transform.SetParent(sensorGridParent.parent, false);
        go.transform.SetAsFirstSibling();
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color         = new Color(0.04f, 0.04f, 0.05f);
        img.raycastTarget = false;
    }

    void BuildSensorGrid()
    {
        if (sensorGridParent == null) return;
        SetupGridLayout(sensorGridParent, new Vector2(16f, 32f));

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            int idx = r * cols + c;
            var go = new GameObject($"ST{idx}");
            go.layer = 5;
            go.transform.SetParent(sensorGridParent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>();
            var cell = go.AddComponent<SensorTileCell>();
            cell.col   = c;
            cell.row   = r;
            cell.index = idx;
            _sensorTiles[idx] = cell;
        }
    }

    // ── 主螢幕：控制格 ────────────────────────────────────────────────────────

    void BuildPrimaryUI()
    {
        if (controlGridParent == null) return;
        var root = controlGridParent.parent as RectTransform;
        if (root == null) return;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 深色背景
        CreatePanel(root, "PrimaryBG", new Color(0.05f, 0.05f, 0.08f),
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, 0);

        // 頂部標題列 (58 px)
        var header = CreatePanel(root, "Header", new Color(0.09f, 0.11f, 0.17f),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -58f), Vector2.zero);
        AddText(header.transform, "TitleText",
            "感應測試控制台  |  128×24 格 = 3072 晶片  |  2048×768 px",
            20, TextAnchor.MiddleCenter, new Color(0.80f, 0.85f, 1.00f), font);

        // 底部狀態列 (38 px)
        var status = CreatePanel(root, "StatusBar", new Color(0.07f, 0.09f, 0.13f),
            Vector2.zero, new Vector2(1f, 0f),
            Vector2.zero, new Vector2(0f, 38f));
        _statusText = AddText(status.transform, "StatusText",
            "感應測試 | 右鍵鎖定 / 解鎖  左鍵模擬觸碰",
            13, TextAnchor.MiddleLeft, new Color(0.60f, 0.65f, 0.75f), font);
        // 左邊留 12 px 邊距
        (_statusText.transform as RectTransform).offsetMin = new Vector2(12f, 0f);

        // 按鈕列 (52 px)，位於狀態列上方
        var btnBar = CreatePanel(root, "ButtonBar", new Color(0.06f, 0.08f, 0.12f),
            Vector2.zero, new Vector2(1f, 0f),
            new Vector2(0f, 38f), new Vector2(0f, 90f));

        CreateButton(btnBar, "BtnQuit",   "離開遊戲", OnQuit,      0f,    1f/5f, font);
        _smartLockBtnText =
        CreateButton(btnBar, "BtnSmart",  "智慧鎖格", OnSmartLock, 1f/5f, 2f/5f, font);
        _smartLockBtnImage = _smartLockBtnText.transform.parent.GetComponent<Image>();
        CreateButton(btnBar, "BtnUnlock", "解除鎖格", OnUnlockAll, 2f/5f, 3f/5f, font);
        _gameTileBtnText =
        CreateButton(btnBar, "BtnGameTile", "晶片示意 ✓", OnToggleGameTile, 3f/5f, 4f/5f, font);
        _gameTileBtnImage = _gameTileBtnText.transform.parent.GetComponent<Image>();
        _clickModeBtnText =
        CreateButton(btnBar, "BtnClickMode", "滑鼠點擊 ✓", OnToggleClickMode, 4f/5f, 1f, font);
        _clickModeBtnImage = _clickModeBtnText.transform.parent.GetComponent<Image>();

        // 靈敏度列 (38 px)，位於按鈕列上方
        var sensBar = CreatePanel(root, "SensitivityBar", new Color(0.06f, 0.08f, 0.12f),
            Vector2.zero, new Vector2(1f, 0f),
            new Vector2(0f, 90f), new Vector2(0f, 128f));
        BuildSensitivityBar(sensBar, font);
    }

    void BuildControlGrid()
    {
        if (controlGridParent == null) return;
        SetupGridLayout(controlGridParent, new Vector2(15f, 30f));  // 初始值，稍後修正

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            int idx = r * cols + c;
            var go = new GameObject($"CT{idx}");
            go.layer = 5;
            go.transform.SetParent(controlGridParent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>();
            var cell = go.AddComponent<ControlTileCell>();
            cell.Init(c, r);
            cell.onRightClick = ToggleLock;
            cell.onLeftClick  = OnLeftClick;
            cell.onHover      = OnTileHover;
            _controlTiles[idx] = cell;
        }
    }

    /// <summary>等待 RectTransform 佈局後，用實際尺寸重算格子大小。</summary>
    void FixControlCellSize()
    {
        if (controlGridParent == null) return;
        var glg = controlGridParent.GetComponent<GridLayoutGroup>();
        if (glg == null) return;
        float w = Mathf.Floor(controlGridParent.rect.width  / cols);
        float h = Mathf.Floor(controlGridParent.rect.height / rows);
        if (w > 0 && h > 0)
            glg.cellSize = new Vector2(w, h);
    }

    // ── UI 輔助方法 ───────────────────────────────────────────────────────────

    static void SetupGridLayout(RectTransform parent, Vector2 cellSize)
    {
        var glg = parent.GetComponent<GridLayoutGroup>()
                  ?? parent.gameObject.AddComponent<GridLayoutGroup>();
        glg.cellSize        = cellSize;
        glg.spacing         = Vector2.zero;
        glg.padding         = new RectOffset(0, 0, 0, 0);
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 128;
        glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment  = TextAnchor.UpperLeft;
    }

    static RectTransform CreatePanel(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax,
        int siblingIndex = -1)
    {
        var go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        if (siblingIndex >= 0) go.transform.SetSiblingIndex(siblingIndex);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        var img = go.AddComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;
        return rt;
    }

    static Text CreateButton(RectTransform parent, string name, string label,
        UnityEngine.Events.UnityAction onClick,
        float anchorXMin, float anchorXMax, Font font)
    {
        var go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorXMin, 0f);
        rt.anchorMax = new Vector2(anchorXMax, 1f);
        rt.offsetMin = new Vector2( 4f,  4f);
        rt.offsetMax = new Vector2(-4f, -4f);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.18f, 0.25f);
        var btn = go.AddComponent<Button>();
        var cb = ColorBlock.defaultColorBlock;
        cb.normalColor      = new Color(0.15f, 0.18f, 0.25f);
        cb.highlightedColor = new Color(0.22f, 0.26f, 0.36f);
        cb.pressedColor     = new Color(0.10f, 0.12f, 0.18f);
        cb.colorMultiplier  = 1f;
        btn.colors          = cb;
        btn.targetGraphic   = img;
        btn.onClick.AddListener(onClick);
        var txt = AddText(go.transform, "Label", label, 15,
            TextAnchor.MiddleCenter, new Color(0.85f, 0.90f, 1.00f), font);
        txt.raycastTarget = false;
        return txt;
    }

    static Text AddText(Transform parent, string name, string content,
        int fontSize, TextAnchor alignment, Color color, Font font)
    {
        var go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var txt = go.AddComponent<Text>();
        txt.text          = content;
        txt.fontSize      = fontSize;
        txt.alignment     = alignment;
        txt.color         = color;
        txt.raycastTarget = false;
        if (font != null) txt.font = font;
        return txt;
    }

    // ── 狀態邏輯 ──────────────────────────────────────────────────────────────

    void ToggleLock(int col, int row)
    {
        int idx = row * cols + col;
        _locked[idx] = !_locked[idx];
        _lockedCount += _locked[idx] ? 1 : -1;

        _sensorTiles[idx]?.SetLocked(_locked[idx]);
        _controlTiles[idx]?.SetLocked(_locked[idx]);

        Debug.Log($"[SensorTest] #{idx + 1} ({col},{row}) " +
                  $"{(_locked[idx] ? "鎖定" : "解鎖")} | 共鎖定 {_lockedCount} 格");
        SetStatusIdle();
    }

    void OnUdpPacket(int rawX, int rawY)
    {
        float nx = Mathf.InverseLerp(rawMinX, rawMaxX, rawX);
        float ny = Mathf.InverseLerp(rawMinY, rawMaxY, rawY);

        // 過濾明顯超出範圍的雜訊
        if (nx < -0.1f || nx > 1.1f || ny < -0.1f || ny > 1.1f) return;

        int c   = Mathf.Clamp(Mathf.FloorToInt(nx * cols), 0, cols - 1);
        int r   = Mathf.Clamp(Mathf.FloorToInt(ny * rows), 0, rows - 1);
        ActivateOrSmartLock(r * cols + c);
    }

    /// <summary>CoordTestPanel 呼叫：以原始感應座標觸發對應格子（與真實 UDP 封包路徑相同）。</summary>
    public void TriggerRawCoord(int rawX, int rawY) => OnUdpPacket(rawX, rawY);

    void OnLeftClick(int col, int row) => ActivateOrSmartLock(row * cols + col);

    void ActivateOrSmartLock(int idx)
    {
        if (_smartLockActive && !_locked[idx])
        {
            _locked[idx] = true;
            _lockedCount++;
            _sensorTiles[idx]?.SetLocked(true);
            _controlTiles[idx]?.SetLocked(true);
            return;
        }
        if (_locked[idx]) return;
        // Display 1 控制格：永遠顯示觸發動畫
        _controlTiles[idx]?.Activate();
        // Display 2 感應格：只在晶片示意開啟時才啟動（關閉時 GameObject inactive，StartCoroutine 會拋例外）
        if (_gameTileVisible)
            _sensorTiles[idx]?.Activate();

        // 廣播到遊戲場景（VFX 互動）
        SensorBus.Touch(idx % cols, idx / cols);
    }

    void Update()
    {
        if (_smartLockActive && _statusText != null)
            _statusText.text = $"智慧鎖格 開啟中  |  被觸碰的格子將自動鎖定  |  已鎖定：{_lockedCount} 格";

        HandleDragInteract();
    }

    /// <summary>
    /// 按住左 / 右鍵拖曳時，持續對滑鼠掃過的每個新格子觸發互動。
    /// 左鍵拖曳：模擬觸碰（與 OnPointerDown 相同邏輯）。
    /// 右鍵拖曳：依首格狀態決定整趟全部鎖定或全部解鎖。
    ///
    /// 注意：右鍵起始方向延一幀確認，避免與 ToggleLock（OnPointerDown）時序競爭。
    /// </summary>
    void HandleDragInteract()
    {
        bool leftHeld  = Input.GetMouseButton(0);
        bool rightHeld = Input.GetMouseButton(1);

        if (!leftHeld && !rightHeld)
        {
            _dragLastIdx     = -1;
            _dragInitPending = false;
            return;
        }

        if (controlGridParent == null) return;

        // Screen Space Overlay Canvas 傳 null；Camera / World Space 傳對應 camera
        Canvas canvas = controlGridParent.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                controlGridParent, Input.mousePosition, cam, out Vector2 localPos))
        {
            _dragLastIdx     = -1;
            _dragInitPending = false;
            return;
        }

        // localPos 以 pivot 為原點；轉換成左上為 (0,0) 的局部座標
        Rect rect = controlGridParent.rect;
        float px = localPos.x - rect.xMin;
        float py = rect.yMax - localPos.y;   // UI Y 軸向下

        if (px < 0f || py < 0f || px >= rect.width || py >= rect.height)
        {
            _dragLastIdx     = -1;
            _dragInitPending = false;
            return;
        }

        var glg = controlGridParent.GetComponent<GridLayoutGroup>();
        if (glg == null) return;

        int c      = Mathf.Clamp((int)(px / glg.cellSize.x), 0, cols - 1);
        int rowIdx = Mathf.Clamp((int)(py / glg.cellSize.y), 0, rows - 1);
        int idx    = rowIdx * cols + c;

        // 按下第一幀：OnPointerDown 已處理首格，這裡只記錄起始位置
        // 右鍵方向「待下一幀確認」，因為 ToggleLock 在同幀內的執行順序不確定
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            _dragLastIdx     = idx;
            _dragInitIdx     = idx;
            _dragInitPending = rightHeld;
            return;
        }

        // 右鍵第二幀以後：ToggleLock 必已執行，此時 _locked[_dragInitIdx] 是真正的目標狀態
        if (_dragInitPending)
        {
            _dragIsLock      = _locked[_dragInitIdx]; // 首格目前的鎖定狀態即為整趟方向
            _dragInitPending = false;
        }

        // 同一格不重複觸發
        if (idx == _dragLastIdx) return;
        _dragLastIdx = idx;

        if (leftHeld)
        {
            ActivateOrSmartLock(idx);
        }
        else if (rightHeld)
        {
            // 保持首格決定的方向，避免一來一回互相 toggle
            if (_locked[idx] != _dragIsLock)
                ToggleLock(c, rowIdx);
        }
    }

    void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void OnSmartLock()
    {
        _smartLockActive = !_smartLockActive;
        UpdateSmartLockButton();
        if (_smartLockActive)
        {
            if (_statusText != null)
                _statusText.text = $"智慧鎖格 開啟中  |  被觸碰的格子將自動鎖定  |  已鎖定：{_lockedCount} 格";
        }
        else
        {
            SetStatusIdle();
        }
    }

    void UpdateSmartLockButton()
    {
        if (_smartLockBtnText != null)
            _smartLockBtnText.text = _smartLockActive ? "智慧鎖格 ✓" : "智慧鎖格";
        if (_smartLockBtnImage != null)
            _smartLockBtnImage.color = _smartLockActive
                ? new Color(0.20f, 0.45f, 0.20f)
                : new Color(0.15f, 0.18f, 0.25f);
    }

    void OnUnlockAll()
    {
        int total = cols * rows;
        for (int i = 0; i < total; i++)
        {
            if (!_locked[i]) continue;
            _locked[i] = false;
            _sensorTiles[i]?.SetLocked(false);
            _controlTiles[i]?.SetLocked(false);
        }
        _lockedCount = 0;
        SetStatusIdle();
        Debug.Log("[SensorTest] 解除全部鎖定");
    }

    void OnTileHover(int col, int row, int idx, bool locked)
    {
        if (_statusText == null) return;
        string ls = locked ? " 🔒 已鎖定" : "";
        _statusText.text =
            $"格子 #{idx + 1}   欄 {col}   列 {row}{ls}   |  " +
            $"已鎖定：{_lockedCount} 格  |  右鍵鎖定 / 解鎖";
    }

    void SetStatusIdle()
    {
        if (_statusText == null || _smartLockActive) return;
        _statusText.text =
            $"感應測試  |  128×24 格，共 3072 個晶片  |  " +
            $"已鎖定：{_lockedCount} 格  |  右鍵鎖定 / 解鎖  左鍵模擬觸碰";
    }

    // ── 晶片示意（Display 2 感應格）開關 ──────────────────────────────────────

    /// <summary>由 ConsoleGameOverlay 自動呼叫（遊戲載入/卸載時），或按鈕手動觸發。</summary>
    public void SetGameTileVisible(bool visible)
    {
        _gameTileVisible = visible;
        if (_canvasSecondary != null)
            _canvasSecondary.SetActive(visible);
        // 感應格 RT 鏡像 Canvas 同步顯示 / 隱藏
        // 隱藏時遊戲相機接管 Display 2，不需要疊加 RT 畫面
        if (_display2MirrorCanvas != null)
            _display2MirrorCanvas.SetActive(visible);
#if !UNITY_EDITOR
        if (_display2Hwnd != System.IntPtr.Zero)
            ShowWindow(_display2Hwnd, visible ? SW_SHOW : SW_HIDE);
#endif
        UpdateGameTileButton();
    }

    // ── 感應格 RT 捕捉（SensorGridRT）────────────────────────────────────────

    /// <summary>
    /// 將 Canvas_Secondary 切換至 ScreenSpaceCamera 模式，
    /// 以 SensorCam 渲染感應格到 SensorGridRT（2048×768）。
    /// 同時在 Display 2 建立 mirror canvas，確保 Display 2 畫面不變。
    /// 呼叫時機：Start() 的 yield return null 之後（RectTransform 佈局完成後）。
    /// </summary>
    void SetupSensorGridRT()
    {
        if (_canvasSecondary == null) return;

        var sensorCanvas = _canvasSecondary.GetComponent<Canvas>();
        if (sensorCanvas == null) return;

        // 建立 RT
        var rt = SensorGridRT.GetOrCreate(2048, 768);

        // 建立 SensorCam：渲染 Canvas_Secondary 到 RT
        var camGO = new GameObject("SensorGridCam");
        camGO.layer = 5;
        camGO.transform.SetParent(transform, false);
        _sensorGridCam = camGO.AddComponent<Camera>();
        _sensorGridCam.clearFlags       = CameraClearFlags.SolidColor;
        _sensorGridCam.backgroundColor  = new Color(0.04f, 0.04f, 0.05f);
        _sensorGridCam.cullingMask      = 1 << 5;   // UI layer only
        _sensorGridCam.orthographic     = true;
        _sensorGridCam.orthographicSize = 384f;      // 768 / 2（世界座標高度，供 ScreenSpaceCamera 計算）
        _sensorGridCam.nearClipPlane    = -100f;
        _sensorGridCam.farClipPlane     = 100f;
        _sensorGridCam.targetTexture    = rt;
        _sensorGridCam.targetDisplay    = 0;         // targetTexture 存在時此值無實際效果
        _sensorGridCam.depth            = -10;       // 早於其他相機渲染

        // 停用 CanvasScaler 避免縮放干擾像素完美佈局
        var scaler = _canvasSecondary.GetComponent<CanvasScaler>();
        if (scaler != null) scaler.enabled = false;

        // 切換 Canvas_Secondary 至 ScreenSpaceCamera 模式
        sensorCanvas.renderMode  = RenderMode.ScreenSpaceCamera;
        sensorCanvas.worldCamera = _sensorGridCam;

        // 建立 Display 2 鏡像 Canvas（ScreenSpaceOverlay，顯示 RT → Display 2 畫面不變）
        _display2MirrorCanvas = BuildDisplay2Mirror(rt);

        Debug.Log("[SensorTest] SensorGridRT 捕捉設定完成，Display 2 鏡像 Canvas 建立");
    }

    /// <summary>建立 Display 2 上的全螢幕 RawImage Canvas，顯示 SensorGridRT。</summary>
    static GameObject BuildDisplay2Mirror(RenderTexture rt)
    {
        var go = new GameObject("Canvas_Display2RTMirror");
        go.layer = 5;
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = 1;
        canvas.sortingOrder  = -10;  // 低優先（遊戲相機在上；Mirror 隱藏時不影響）

        var imgGO = new GameObject("RTImage");
        imgGO.layer = 5;
        imgGO.transform.SetParent(go.transform, false);
        var imgRT = imgGO.AddComponent<RectTransform>();
        imgRT.anchorMin = Vector2.zero;
        imgRT.anchorMax = Vector2.one;
        imgRT.offsetMin = imgRT.offsetMax = Vector2.zero;
        var rawImg = imgGO.AddComponent<RawImage>();
        rawImg.texture       = rt;
        rawImg.raycastTarget = false;

        return go;
    }

    void OnToggleGameTile()
    {
        SetGameTileVisible(!_gameTileVisible);
    }

    void UpdateGameTileButton()
    {
        if (_gameTileBtnText != null)
            _gameTileBtnText.text = _gameTileVisible ? "晶片示意 ✓" : "晶片示意";
        if (_gameTileBtnImage != null)
            _gameTileBtnImage.color = _gameTileVisible
                ? new Color(0.15f, 0.18f, 0.25f)
                : new Color(0.30f, 0.18f, 0.12f); // 橘紅色表示已關閉
    }

    // ── 滑鼠點擊 / 事件觸發 切換 ─────────────────────────────────────────────

    void OnToggleClickMode()
    {
        _clickSim?.CycleMode();
        UpdateClickModeButton();
    }

    void UpdateClickModeButton()
    {
        if (_clickModeBtnText == null) return;
        if (_clickSim == null)
        {
            _clickModeBtnText.text = "點擊模式";
            if (_clickModeBtnImage != null)
                _clickModeBtnImage.color = new Color(0.25f, 0.18f, 0.18f); // 暗紅：未設定
            return;
        }
        _clickModeBtnText.text = _clickSim.ModeLabel;
        if (_clickModeBtnImage != null)
            _clickModeBtnImage.color = _clickSim.ModeColor;
    }

    // ── 感應冷卻調整 ──────────────────────────────────────────────────────────

    void BuildSensitivityBar(RectTransform parent, Font font)
    {
        // 說明標籤（左側 0-18%）
        var labelPanel = CreatePanel(parent, "CooldownLabel", Color.clear,
            new Vector2(0f, 0f), new Vector2(0.18f, 1f),
            Vector2.zero, Vector2.zero);
        labelPanel.GetComponent<Image>().raycastTarget = false;
        AddText(labelPanel.transform, "LabelText", "感應冷卻",
            13, TextAnchor.MiddleRight, new Color(0.60f, 0.65f, 0.75f), font);

        // ON/OFF 開關按鈕（18-35%）
        _cooldownToggleBtnText =
            CreateButton(parent, "BtnCooldownToggle", "冷卻 ON", OnToggleCooldown, 0.18f, 0.35f, font);
        _cooldownToggleBtnImage = _cooldownToggleBtnText.transform.parent.GetComponent<Image>();

        // "−" 按鈕（35-48%）
        CreateButton(parent, "BtnCooldownDec", "−", OnDecreaseCooldown, 0.35f, 0.48f, font);

        // 數值顯示（48-63%）
        var valPanel = CreatePanel(parent, "CooldownValue", new Color(0.10f, 0.12f, 0.16f),
            new Vector2(0.48f, 0.1f), new Vector2(0.63f, 0.9f),
            Vector2.zero, Vector2.zero);
        valPanel.GetComponent<Image>().raycastTarget = false;
        _cooldownText = AddText(valPanel.transform, "ValueText", "1.0 秒",
            14, TextAnchor.MiddleCenter, new Color(0.95f, 0.95f, 1.00f), font);

        // "+" 按鈕（63-76%）
        CreateButton(parent, "BtnCooldownInc", "+", OnIncreaseCooldown, 0.63f, 0.76f, font);

        // 提示標籤（右側 76-100%）
        var hintPanel = CreatePanel(parent, "CooldownHint", Color.clear,
            new Vector2(0.76f, 0f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero);
        hintPanel.GetComponent<Image>().raycastTarget = false;
        AddText(hintPanel.transform, "HintText", "越小觸發越頻繁",
            11, TextAnchor.MiddleLeft, new Color(0.40f, 0.44f, 0.52f), font);

        UpdateCooldownToggleButton();
        UpdateCooldownText();
    }

    void OnToggleCooldown()
    {
        if (_clickSim == null) return;
        _clickSim.cooldownEnabled = !_clickSim.cooldownEnabled;
        UpdateCooldownToggleButton();
        UpdateCooldownText();
    }

    void UpdateCooldownToggleButton()
    {
        if (_cooldownToggleBtnText == null) return;
        bool on = _clickSim == null || _clickSim.cooldownEnabled;
        _cooldownToggleBtnText.text = on ? "冷卻 ON" : "冷卻 OFF";
        if (_cooldownToggleBtnImage != null)
            _cooldownToggleBtnImage.color = on
                ? new Color(0.15f, 0.35f, 0.20f)   // 綠：冷卻限制中
                : new Color(0.35f, 0.20f, 0.15f);   // 橘紅：自由多點觸控
    }

    void OnDecreaseCooldown()
    {
        if (_clickSim == null) return;
        _clickSim.clickCooldown =
            Mathf.Max(0.1f, Mathf.Round((_clickSim.clickCooldown - 0.1f) * 10f) / 10f);
        UpdateCooldownText();
    }

    void OnIncreaseCooldown()
    {
        if (_clickSim == null) return;
        _clickSim.clickCooldown =
            Mathf.Min(5.0f, Mathf.Round((_clickSim.clickCooldown + 0.1f) * 10f) / 10f);
        UpdateCooldownText();
    }

    void UpdateCooldownText()
    {
        if (_cooldownText == null) return;
        bool   on = _clickSim != null && _clickSim.cooldownEnabled;
        float  v  = _clickSim != null ? _clickSim.clickCooldown : 1f;
        _cooldownText.text  = $"{v:F1} 秒";
        _cooldownText.color = on
            ? new Color(0.95f, 0.95f, 1.00f)
            : new Color(0.40f, 0.40f, 0.42f); // 冷卻關閉時變暗
    }

    // ── Display 2 視窗控制（獨立執行檔專用）────────────────────────────────────

#if !UNITY_EDITOR
    /// <summary>
    /// 啟動後等待 0.5 秒，讓 Unity 完成 Display 2 視窗建立，
    /// 再透過 EnumWindows + GetWindowRect 找到 Display 2 的 HWND。
    /// 判斷依據：視窗左側座標 >= display2WindowOffsetX，代表該視窗在 Display 2 上。
    /// 不依賴前景視窗狀態，定位更可靠。
    /// </summary>
    IEnumerator FindDisplay2WindowCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        System.IntPtr found = System.IntPtr.Zero;
        int threshold = display2WindowOffsetX;          // 通常 1920

        EnumWindows((hwnd, _) =>
        {
            var sb = new System.Text.StringBuilder(128);
            GetClassName(hwnd, sb, 128);
            if (sb.ToString() != "UnityWndClass") return true;

            // 用視窗位置判斷是否在 Display 2
            GetWindowRect(hwnd, out RECT rect);
            if (rect.left >= threshold)
            {
                found = hwnd;
                return false;                           // 找到後停止枚舉
            }
            return true;
        }, System.IntPtr.Zero);

        _display2Hwnd = found;

        if (found != System.IntPtr.Zero)
            Debug.Log($"[SensorTest] Display 2 HWND = 0x{found.ToInt64():X}（晶片示意可控制顯示/隱藏）");
        else
            Debug.LogWarning("[SensorTest] 找不到 Display 2 視窗。請確認：獨立執行檔、雙螢幕已啟用、display2WindowOffsetX 設定正確。");
    }
#endif
}
