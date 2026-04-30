using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 雙螢幕感應測試主控（多場域預設版）。
///
/// Inspector 中指定 VenuePresetConfig[] presets，執行時可透過控制台
/// 上方的 [◀] / [▶] 按鈕切換場域。每次切換都會重建格子並更新 RT 大小。
///
/// 單場域（2048×768 舞台）行為與舊版完全相同。
///
/// 拆分檔：
///   SensorTestManager_UIBuilder.cs  — UI 骨架建立（幾乎不改動）
///   SensorTestManager_GridBuilder.cs — 格子建立 + RT 捕捉（偶爾改動）
/// </summary>
public partial class SensorTestManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("場域預設（Inspector 設定，至少一個）")]
    public VenuePresetConfig[] presets;

    [Header("UI 選項")]
    [Tooltip("顯示標題列右側的場域切換 ◀/▶（多場域場景使用）")]
    public bool showPresetSelector = true;

    [Header("場景引用（Inspector 設定）")]
    public RectTransform controlGridParent;   // Canvas_Primary 的子容器
    public RectTransform sensorGridParent;    // Canvas_Secondary 的子容器

    [Header("遊戲場景（留空則不自動載入）")]
    public string gameSceneName = "Map_CybercubeLogo_J_2048x768";

    [Header("UI 字型")]
    [Tooltip("控制台 UI 使用的 TMP 字型，留空則使用 TMP Settings 預設。")]
    public TMP_FontAsset uiFontOverride;

    [Header("Display 2 視窗偵測")]
    [Tooltip("Display 2 在 Windows 桌面的 X 起始位置（預設 1920）。")]
    public int display2WindowOffsetX = 1920;

    [Header("格子亮燈")]
    [Tooltip("觸發後亮紅持續秒數（同步套用至 SensorTileCell / ControlTileCell）。")]
    public float activeDuration = 1.5f;

    // ── 場域資料 ──────────────────────────────────────────────────────────────

    class FaceData
    {
        public SensorFaceConfig config;
        public SensorTileCell[]  sensorTiles;
        public ControlTileCell[] controlTiles;
        public bool[]            locked;
        public int               lockedCount;
        // 由畫布校準值計算得出的有效感應範圍（取代 config.rawMin/Max）
        public float rawMinX, rawMaxX, rawMinY, rawMaxY;
    }

    int                  _presetIdx;
    DisplayCanvasConfig  _canvasConfig;
    bool                 _rotateDisplay90;
    List<FaceData>       _faces = new List<FaceData>();

    // key = faceIdx * 100000 + cellIdx；value = 1 秒滑動窗口的觸發時間戳
    readonly Dictionary<int, Queue<float>> _cellHitTimes = new Dictionary<int, Queue<float>>();

    int TotalLocked    { get { int n = 0; foreach (var f in _faces) n += f.lockedCount; return n; } }
    int TotalCells     { get { int n = 0; foreach (var f in _faces) n += f.config.colCount * f.config.rowCount; return n; } }

    // ── 執行期 UI 狀態 ─────────────────────────────────────────────────────────

    TextMeshProUGUI  _statusText;
    bool  _smartLockActive;
    TextMeshProUGUI  _smartLockBtnText;
    Image _smartLockBtnImage;

    [SerializeField] bool  _gameTileVisible = false;
    TextMeshProUGUI  _gameTileBtnText;
    Image _gameTileBtnImage;

    SensorClickSimulator _clickSim;
    TextMeshProUGUI      _clickModeBtnText;
    Image                _clickModeBtnImage;

    TextMeshProUGUI  _cooldownText;
    TextMeshProUGUI  _cooldownToggleBtnText;
    Image _cooldownToggleBtnImage;

    bool  _gridLineVisible      = true;
    TextMeshProUGUI  _gridLineBtnText;
    Image _gridLineBtnImage;

    bool  _screenOverlayVisible = true;
    TextMeshProUGUI  _screenOverlayBtnText;
    Image _screenOverlayBtnImage;

    ConsoleGameOverlay _overlay;
    TextMeshProUGUI    _overlayAlphaText;

    Display2DimOverlay _dimOverlay;
    TextMeshProUGUI    _dimAlphaText;

    TextMeshProUGUI  _presetLabel;

    TMP_FontAsset _uiFont;
    RectTransform _faceSelectorParent;
    Image[]       _faceSelectorBtnImages;

    // ── 載入面板 ──────────────────────────────────────────────────────────────

    GameObject    _loadingPanel;
    TextMeshProUGUI _loadingTitle;
    TextMeshProUGUI _loadingProgressText;
    RectTransform _loadingBarFill;
    bool          _isSwitching;

    // ── 晶片示意 ──────────────────────────────────────────────────────────────

    GameObject _canvasSecondary;
    Camera     _sensorGridCam;
    GameObject _display2MirrorCanvas;

    // ── 控制格縮放（等比，置中）────────────────────────────────────────────────

    float _controlGridScale   = 1f;
    float _controlGridOffsetX = 0f;
    float _controlGridOffsetY = 0f;

    // ── 逐面檢視 ──────────────────────────────────────────────────────────────

    int   _viewFaceIdx     = -1;   // -1 = 全覽；>=0 = 單面
    float _faceViewOriginX, _faceViewOriginY;
    float _faceViewWidth,   _faceViewHeight;

    // ── 拖曳互動 ──────────────────────────────────────────────────────────────

    int  _dragLastFace    = -1;
    int  _dragLastIdx     = -1;
    int  _dragInitFace    = -1;
    int  _dragInitIdx     = -1;
    bool _dragIsLock;
    bool _dragInitPending;

    // ── Display 2 視窗控制 ────────────────────────────────────────────────────

    System.IntPtr _display2Hwnd      = System.IntPtr.Zero;
    bool          _display2Activated = false;
    public System.IntPtr Display2Hwnd => _display2Hwnd;

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
        // 只在晶片示意預設開啟時才啟動 Display 2；預設關閉則等使用者手動開啟
        if (_gameTileVisible && Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
            _display2Activated = true;
        }

        Debug.Log($"[SensorTest] 偵測到 {Display.displays.Length} 個螢幕 | Display 2 啟動（原生解析度）");

        // 在第一幀渲染前就隱藏晶片示意，避免啟動時閃現
        if (!_gameTileVisible)
        {
            var canvas = sensorGridParent?.GetComponentInParent<Canvas>()?.gameObject;
            if (canvas != null) canvas.SetActive(false);
        }
    }

    IEnumerator Start()
    {
        _canvasSecondary = sensorGridParent?.GetComponentInParent<Canvas>()?.gameObject;

        // 先建 UI 骨架（確保按鈕列一定顯示，presets 有問題時也能看到錯誤訊息）
        BuildPrimaryUI();

        if (presets == null || presets.Length == 0)
        {
            Debug.LogError("[SensorTest] 未指定任何 VenuePresetConfig！請在 Inspector 設定 presets。");
            if (_statusText != null)
                _statusText.text = "錯誤：請在 Inspector 的 SensorTestManager → Presets 設定場域！";
            yield break;
        }

        // 載入第一個場域
        InitPresetData(0);
        BuildSensorBackground();
        BuildSensorGrid();
        BuildControlGrid();

        yield return null;   // 等一幀讓佈局完成

        SetupSensorGridRT();
        _clickSim   = FindObjectOfType<SensorClickSimulator>();
        _overlay    = GetComponent<ConsoleGameOverlay>();
        _dimOverlay = GetComponent<Display2DimOverlay>();
        NotifyOverlay();
        UpdateClickModeButton();
        UpdateCooldownToggleButton();
        UpdateCooldownText();
        UpdateOverlayAlphaText();
        UpdateDimAlphaText();
        UpdatePresetLabel();
        BuildFaceSelector();
        UpdateGestureBar();
        UpdateTapBoundaryText();

#if !UNITY_EDITOR
        if (_display2Activated)
            StartCoroutine(FindDisplay2WindowCoroutine());
#endif

        SensorTileCell.ActiveDuration = activeDuration;

        UdpSensorReceiver.OnPacketReceived += OnUdpPacket;
        SetStatusIdle();

        if (!string.IsNullOrEmpty(gameSceneName))
        {
            bool found = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                if (SceneUtility.GetScenePathByBuildIndex(i).Contains(gameSceneName)) { found = true; break; }
            }
            if (found)
                yield return SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Additive);
            else
                Debug.LogWarning($"[SensorTest] 場景 '{gameSceneName}' 未加入 Build Settings，略過載入。");
        }

        SetGameTileVisible(_gameTileVisible);
    }

    void OnDestroy()
    {
        UdpSensorReceiver.OnPacketReceived -= OnUdpPacket;
        SensorGridRT.Release();
        DestroyClickLog();
    }

    // ── 場域預設切換 ──────────────────────────────────────────────────────────

    /// <summary>初始化場域資料（不建格子）。</summary>
    void InitPresetData(int idx)
    {
        _presetIdx    = idx;
        _canvasConfig = presets[idx].canvasConfig;
        _viewFaceIdx  = -1;   // 切換場域時重設為全覽
        // _rotateDisplay90 由 CalcControlScale() 自動決定，此處不手動設定
        _faces.Clear();
        foreach (var fc in presets[idx].faces)
        {
            if (fc == null) continue;
            var fd = new FaceData
            {
                config       = fc,
                sensorTiles  = new SensorTileCell[fc.colCount * fc.rowCount],
                controlTiles = new ControlTileCell[fc.colCount * fc.rowCount],
                locked       = new bool[fc.colCount * fc.rowCount],
                lockedCount  = 0
            };
            ComputeFaceRawBounds(fd, presets[idx]);
            _faces.Add(fd);
        }
    }

    /// <summary>
    /// 依畫布四角校準值與面的幾何位置，計算各面有效的原始感應範圍。
    /// 若 VenuePresetConfig 未設定校準值（canvasRawMaxX == 0），回退至 SensorFaceConfig 欄位。
    /// </summary>
    void ComputeFaceRawBounds(FaceData fd, VenuePresetConfig preset)
    {
        var cfg = fd.config;
        if (preset.canvasRawMaxX == 0f)
        {
            // 未校準：沿用 SensorFaceConfig 內的值（向後相容）
            fd.rawMinX = cfg.rawMinX;
            fd.rawMaxX = cfg.rawMaxX;
            fd.rawMinY = cfg.rawMinY;
            fd.rawMaxY = cfg.rawMaxY;
            return;
        }

        float cw = preset.sensorAreaWidth  > 0 ? preset.sensorAreaWidth
                 : (_canvasConfig != null ? _canvasConfig.canvasWidth  : 2048f);
        float ch = preset.sensorAreaHeight > 0 ? preset.sensorAreaHeight
                 : (_canvasConfig != null ? _canvasConfig.canvasHeight : 768f);

        float facePixelW = cfg.TotalPixelWidth;
        float facePixelH = cfg.TotalPixelHeight;

        if (cfg.orientation == SensorFaceConfig.ChipOrientation.橫)
        {
            // rawX ↔ 畫面 X，rawY ↔ 畫面 Y
            fd.rawMinX = Mathf.Lerp(preset.canvasRawMinX, preset.canvasRawMaxX, cfg.originX / cw);
            fd.rawMaxX = Mathf.Lerp(preset.canvasRawMinX, preset.canvasRawMaxX, (cfg.originX + facePixelW) / cw);
            fd.rawMinY = Mathf.Lerp(preset.canvasRawMinY, preset.canvasRawMaxY, cfg.originY / ch);
            fd.rawMaxY = Mathf.Lerp(preset.canvasRawMinY, preset.canvasRawMaxY, (cfg.originY + facePixelH) / ch);
        }
        else
        {
            // rawX ↔ 畫面 Y，rawY ↔ 畫面 X
            fd.rawMinX = Mathf.Lerp(preset.canvasRawMinX, preset.canvasRawMaxX, cfg.originY / ch);
            fd.rawMaxX = Mathf.Lerp(preset.canvasRawMinX, preset.canvasRawMaxX, (cfg.originY + facePixelH) / ch);
            fd.rawMinY = Mathf.Lerp(preset.canvasRawMinY, preset.canvasRawMaxY, cfg.originX / cw);
            fd.rawMaxY = Mathf.Lerp(preset.canvasRawMinY, preset.canvasRawMaxY, (cfg.originX + facePixelW) / cw);
        }
    }

    void DestroyGridTiles()
    {
        _smartTouches.Clear();    // 切換場域時清除殘留觸碰狀態
        _cellHitTimes.Clear();
        _pendingCells.Clear();    // tile GameObject 即將被 Destroy，無需清視覺
        foreach (var fd in _faces)
        {
            if (fd.sensorTiles  != null) foreach (var t in fd.sensorTiles)  if (t) Destroy(t.gameObject);
            if (fd.controlTiles != null) foreach (var t in fd.controlTiles) if (t) Destroy(t.gameObject);
        }
        _faces.Clear();
    }

    void TeardownSensorGridRT()
    {
        if (_sensorGridCam != null) { Destroy(_sensorGridCam.gameObject); _sensorGridCam = null; }
        if (_display2MirrorCanvas != null) { Destroy(_display2MirrorCanvas); _display2MirrorCanvas = null; }
        SensorGridRT.Release();
        // 還原 Canvas_Secondary 至 ScreenSpaceOverlay（重建時再切換）
        if (_canvasSecondary != null)
        {
            var c = _canvasSecondary.GetComponent<Canvas>();
            if (c != null) c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = _canvasSecondary.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.enabled = true;
        }
    }

    void SwitchPreset(int idx)
    {
        if (_isSwitching) return;
        if (presets == null || idx < 0 || idx >= presets.Length) return;
        if (idx == _presetIdx && _faces.Count > 0) return;
        StartCoroutine(SwitchPresetCoroutine(idx));
    }

    IEnumerator SwitchPresetCoroutine(int idx)
    {
        _isSwitching = true;
        ShowLoading(true, $"載入場域：{presets[idx]?.presetName ?? "?"}");
        yield return null;   // 讓 Loading Panel 先渲染出來

        TeardownSensorGridRT();
        DestroyGridTiles();

        if (sensorGridParent != null)
        {
            var bg = sensorGridParent.parent?.Find("SensorBG");
            if (bg != null) Destroy(bg.gameObject);
        }

        InitPresetData(idx);
        BuildSensorBackground();
        SetLoadingProgress(0.1f);
        yield return null;

        yield return StartCoroutine(BuildSensorGridAsync(0.1f, 0.55f));
        yield return StartCoroutine(BuildControlGridAsync(0.55f, 0.95f));

        SetLoadingProgress(0.97f);
        yield return null;

        SetupSensorGridRT();
        NotifyOverlay();
        BuildFaceSelector();
        UpdatePresetLabel();
        SetStatusIdle();

        SetLoadingProgress(1f);
        yield return null;

        ShowLoading(false);
        _isSwitching = false;

        Debug.Log($"[SensorTest] 切換場域 → {presets[idx].presetName}");
    }

    // ── UDP 解析 ──────────────────────────────────────────────────────────────

    void OnUdpPacket(int rawX, int rawY)
    {
        bool anyHit = false;
        for (int fi = 0; fi < _faces.Count; fi++)
        {
            var fd  = _faces[fi];
            var cfg = fd.config;

            // 未校準的面（rawMin >= rawMax）跳過，避免 InverseLerp(0,0,x) 全部回傳 0 造成多面觸發
            if (fd.rawMinX >= fd.rawMaxX || fd.rawMinY >= fd.rawMaxY) continue;

            float nx = Mathf.InverseLerp(fd.rawMinX, fd.rawMaxX, rawX);
            float ny = Mathf.InverseLerp(fd.rawMinY, fd.rawMaxY, rawY);
            if (nx < -0.1f || nx > 1.1f || ny < -0.1f || ny > 1.1f) continue;

            var (col, row) = cfg.RawToCell(rawX, rawY, fd.rawMinX, fd.rawMaxX, fd.rawMinY, fd.rawMaxY);
            int cellIdx = row * cfg.colCount + col;
            if (!ProcessGhostTouch(fi, cellIdx)) continue;  // 孤點尚未確認：同時跳過 ActivateCell + SmartTouch
            ActivateCell(fi, cellIdx, skipVisual: true);  // 視覺交由 SmartTouch 解算後觸發
            UpdateSmartTouch(fi, cellIdx);
            anyHit = true;

            // Debug：在 status bar 顯示觸發資訊，方便校準座標
            if (_statusText != null && !_smartLockActive)
                _statusText.text = $"raw({rawX},{rawY})  →  {cfg.faceName} 面  欄{col} 列{row}" +
                    $"  [{_lastHitPerSec} 次/秒]" +
                    $"  |  rawX範圍[{fd.rawMinX:0}-{fd.rawMaxX:0}]  rawY範圍[{fd.rawMinY:0}-{fd.rawMaxY:0}]";
        }

        if (!anyHit && _statusText != null && !_smartLockActive)
            _statusText.text = $"raw({rawX},{rawY})  →  超出所有面的範圍（未觸發）";
    }

    // ── 觸發 / 鎖定 ──────────────────────────────────────────────────────────

    void ActivateCell(int faceIdx, int cellIdx, bool skipVisual = false)
    {
        var fd = _faces[faceIdx];
        if (_smartLockActive && !fd.locked[cellIdx])
        {
            fd.locked[cellIdx] = true;
            fd.lockedCount++;
            fd.sensorTiles[cellIdx]?.SetLocked(true);
            fd.controlTiles[cellIdx]?.SetLocked(true);
            return;
        }
        if (fd.locked[cellIdx]) return;

        if (!skipVisual)
        {
            fd.controlTiles[cellIdx]?.Activate();
            if (_gameTileVisible)
                fd.sensorTiles[cellIdx]?.Activate();
        }

        int col = cellIdx % fd.config.colCount;
        int row = cellIdx / fd.config.colCount;
        SensorBus.Touch(col, row);

        // 滑動窗口：計算此格最近 1 秒的觸發次數
        int key = faceIdx * 100000 + cellIdx;
        if (!_cellHitTimes.TryGetValue(key, out var q))
        {
            q = new Queue<float>();
            _cellHitTimes[key] = q;
        }
        float now = Time.realtimeSinceStartup;
        q.Enqueue(now);
        while (q.Count > 0 && now - q.Peek() > 1f) q.Dequeue();
        _lastHitKey    = key;
        _lastHitPerSec = q.Count;
        _lastHitCol    = col;
        _lastHitRow    = row;
        _lastHitFace   = fd.config.faceName;
    }

    int    _lastHitKey;
    int    _lastHitPerSec;
    int    _lastHitCol, _lastHitRow;
    string _lastHitFace;

    void ToggleLock(int faceIdx, int col, int row)
    {
        var fd  = _faces[faceIdx];
        int idx = row * fd.config.colCount + col;
        fd.locked[idx]   = !fd.locked[idx];
        fd.lockedCount  += fd.locked[idx] ? 1 : -1;
        fd.sensorTiles[idx]?.SetLocked(fd.locked[idx]);
        fd.controlTiles[idx]?.SetLocked(fd.locked[idx]);
        Debug.Log($"[SensorTest] face={faceIdx} #{idx + 1} ({col},{row}) " +
                  $"{(fd.locked[idx] ? "鎖定" : "解鎖")} | 共鎖定 {TotalLocked} 格");
        SetStatusIdle();
    }

    void OnLeftClick(int faceIdx, int cellIdx) => ActivateCell(faceIdx, cellIdx);

    void OnHoldTick(int faceIdx, int cellIdx) => ActivateCell(faceIdx, cellIdx, skipVisual: true);

    void OnMousePressStart(int faceIdx, int cellIdx)
    {
        var fd = _faces[faceIdx];
        if (fd.locked[cellIdx]) return;
        if (_gameTileVisible) fd.sensorTiles[cellIdx]?.SetPressState(true);
        int col = cellIdx % fd.config.colCount;
        int row = cellIdx / fd.config.colCount;
        SensorBus.PressStart(col, row);
    }

    void OnMousePressEnd(int faceIdx, int cellIdx)
    {
        var fd = _faces[faceIdx];
        fd.controlTiles[cellIdx]?.SetPressState(false);
        if (_gameTileVisible) fd.sensorTiles[cellIdx]?.SetPressState(false);
        int col = cellIdx % fd.config.colCount;
        int row = cellIdx / fd.config.colCount;
        SensorBus.PressEnd(col, row);
    }

    /// <summary>CoordTestPanel 呼叫：以原始感應座標觸發（與真實 UDP 封包路徑相同）。</summary>
    public void TriggerRawCoord(int rawX, int rawY) => OnUdpPacket(rawX, rawY);

    /// <summary>
    /// CoordTestPanel 呼叫：計算原始座標對應的面 / 欄 / 列，不觸發動畫。
    /// 回傳 false 代表超出所有面的感應範圍（會被過濾掉）。
    /// </summary>
    public bool GetCellForRaw(int rawX, int rawY, out int faceIdx, out int col, out int row)
    {
        for (int fi = 0; fi < _faces.Count; fi++)
        {
            var cfg = _faces[fi].config;
            float nx = Mathf.InverseLerp(cfg.rawMinX, cfg.rawMaxX, rawX);
            float ny = Mathf.InverseLerp(cfg.rawMinY, cfg.rawMaxY, rawY);
            if (nx < -0.1f || nx > 1.1f || ny < -0.1f || ny > 1.1f) continue;
            (col, row) = cfg.RawToCell(rawX, rawY);
            faceIdx = fi;
            return true;
        }
        faceIdx = col = row = 0;
        return false;
    }

    // ── Update：拖曳互動 ──────────────────────────────────────────────────────

    void Update()
    {
        if (_smartLockActive && _statusText != null)
            _statusText.text = $"智慧鎖格 開啟中  |  被觸碰的格子將自動鎖定  |  已鎖定：{TotalLocked} 格";

        HandleDragInteract();
        UpdateClickLogPanel();
        FlushExpiredSmartTouches();
        CleanExpiredPendingCells();
    }

    void HandleDragInteract()
    {
        // 忽略 SensorClickSimulator 剛剛注入的合成 OS 滑鼠事件（50ms 時間窗），防止回饋迴圈
        if (_clickSim != null &&
            Time.realtimeSinceStartup - _clickSim.LastSendInputTime < 0.05f)
            return;

        bool leftHeld  = Input.GetMouseButton(0);
        bool rightHeld = Input.GetMouseButton(1);

        if (!leftHeld && !rightHeld)
        {
            _dragLastFace = -1; _dragLastIdx = -1;
            _dragInitPending = false;
            return;
        }

        if (!HitTestControlGrid(Input.mousePosition, out int fi, out int col, out int row))
        {
            _dragLastFace = -1; _dragLastIdx = -1;
            _dragInitPending = false;
            return;
        }

        int idx = row * _faces[fi].config.colCount + col;

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            _dragLastFace    = fi;
            _dragLastIdx     = idx;
            _dragInitFace    = fi;
            _dragInitIdx     = idx;
            _dragInitPending = rightHeld;
            return;
        }

        if (_dragInitPending)
        {
            _dragIsLock      = _faces[_dragInitFace].locked[_dragInitIdx];
            _dragInitPending = false;
        }

        if (fi == _dragLastFace && idx == _dragLastIdx) return;
        _dragLastFace = fi;
        _dragLastIdx  = idx;

        if (leftHeld)
        {
            ActivateCell(fi, idx);
        }
        else if (rightHeld && fi == _dragInitFace)
        {
            if (_faces[fi].locked[idx] != _dragIsLock)
                ToggleLock(fi, col, row);
        }
    }

    /// <summary>滑鼠螢幕座標 → 控制格命中的面 + 格子（col/row）。</summary>
    bool HitTestControlGrid(Vector2 screenPos, out int faceIdx, out int col, out int row)
    {
        faceIdx = col = row = 0;
        if (controlGridParent == null || _canvasConfig == null || _faces.Count == 0) return false;

        Canvas canvas = controlGridParent.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                controlGridParent, screenPos, cam, out Vector2 local)) return false;

        Rect rect = controlGridParent.rect;
        float px  = local.x - rect.xMin;
        float py  = rect.yMax - local.y;

        if (px < 0f || py < 0f || px >= rect.width || py >= rect.height) return false;

        // 等比縮放下：先去除 offset，再轉換回畫布座標
        if (_controlGridScale <= 0f) return false;

        float cpx, cpy;
        if (_rotateDisplay90)
        {
            float dispX = (px - _controlGridOffsetX) / _controlGridScale;
            float dispY = (py - _controlGridOffsetY) / _controlGridScale;
            cpx = _faceViewOriginX + _faceViewWidth - dispY;
            cpy = _faceViewOriginY + dispX;
        }
        else
        {
            cpx = _faceViewOriginX + (px - _controlGridOffsetX) / _controlGridScale;
            cpy = _faceViewOriginY + (py - _controlGridOffsetY) / _controlGridScale;
        }

        if (cpx < _faceViewOriginX || cpy < _faceViewOriginY ||
            cpx >= _faceViewOriginX + _faceViewWidth ||
            cpy >= _faceViewOriginY + _faceViewHeight) return false;

        int faceStart = _viewFaceIdx >= 0 ? _viewFaceIdx : 0;
        int faceEnd   = _viewFaceIdx >= 0 ? _viewFaceIdx + 1 : _faces.Count;

        for (int fi = faceStart; fi < faceEnd; fi++)
        {
            var cfg = _faces[fi].config;
            if (cpx < cfg.originX || cpy < cfg.originY ||
                cpx >= cfg.originX + cfg.TotalPixelWidth ||
                cpy >= cfg.originY + cfg.TotalPixelHeight) continue;

            faceIdx = fi;
            col = Mathf.Clamp((int)((cpx - cfg.originX) / cfg.GizmoCellW), 0, cfg.colCount - 1);
            row = Mathf.Clamp((int)((cpy - cfg.originY) / cfg.GizmoCellH), 0, cfg.rowCount - 1);
            return true;
        }
        return false;
    }

    // ── 按鈕回呼 ─────────────────────────────────────────────────────────────

    // ── 逐面檢視 ──────────────────────────────────────────────────────────────

    void OnFaceView(int fi)
    {
        if (_viewFaceIdx == fi) return;
        _viewFaceIdx = fi;
        BuildControlGrid();
        NotifyOverlay();
        UpdateFaceSelectorButtons();
    }

    void UpdateFaceSelectorButtons()
    {
        if (_faceSelectorBtnImages == null) return;
        for (int i = 0; i < _faceSelectorBtnImages.Length; i++)
        {
            bool active = (i == 0) ? (_viewFaceIdx == -1) : (_viewFaceIdx == i - 1);
            if (_faceSelectorBtnImages[i] != null)
                _faceSelectorBtnImages[i].color = active
                    ? new Color(0.20f, 0.35f, 0.55f)
                    : new Color(0.12f, 0.14f, 0.20f);
        }
    }

    // ── 按鈕回呼 ─────────────────────────────────────────────────────────────

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
                _statusText.text = $"智慧鎖格 開啟中  |  被觸碰的格子將自動鎖定  |  已鎖定：{TotalLocked} 格";
        }
        else
            SetStatusIdle();
    }

    void UpdateSmartLockButton()
    {
        if (_smartLockBtnText  != null) _smartLockBtnText.text = _smartLockActive ? "智慧鎖格 ON" : "智慧鎖格";
        if (_smartLockBtnImage != null)
            _smartLockBtnImage.color = _smartLockActive
                ? new Color(0.20f, 0.45f, 0.20f)
                : new Color(0.15f, 0.18f, 0.25f);
    }

    void OnUnlockAll()
    {
        foreach (var fd in _faces)
        {
            for (int i = 0; i < fd.locked.Length; i++)
            {
                if (!fd.locked[i]) continue;
                fd.locked[i] = false;
                fd.sensorTiles[i]?.SetLocked(false);
                fd.controlTiles[i]?.SetLocked(false);
            }
            fd.lockedCount = 0;
        }
        SetStatusIdle();
        Debug.Log("[SensorTest] 解除全部鎖定");
    }

    void OnTileHover(int faceIdx, int col, int row, int idx, bool locked)
    {
        if (_statusText == null) return;
        string ls = locked ? " 🔒 已鎖定" : "";
        _statusText.text =
            $"格子 #{idx + 1}   欄 {col}   列 {row}{ls}   |  " +
            $"已鎖定：{TotalLocked} 格  |  右鍵鎖定 / 解鎖";
    }

    void SetStatusIdle()
    {
        if (_statusText == null || _smartLockActive) return;
        int total = TotalCells;
        _statusText.text =
            $"感應測試  |  {total} 個晶片  |  " +
            $"已鎖定：{TotalLocked} 格  |  右鍵鎖定 / 解鎖  左鍵模擬觸碰";
    }

    // ── 晶片示意（Display 2）開關 ─────────────────────────────────────────────

    public bool GameTileVisible => _gameTileVisible;

    public void SetGameTileVisible(bool visible)
    {
        _gameTileVisible = visible;
        if (_canvasSecondary != null) _canvasSecondary.SetActive(visible);
        if (_display2MirrorCanvas != null) _display2MirrorCanvas.SetActive(visible);
        if (visible)
            foreach (var f in _faces)
                foreach (var t in f.sensorTiles)
                    if (t != null && !f.locked[t.index % f.locked.Length]) t.ResetToIdle();
#if !UNITY_EDITOR
        if (visible && !_display2Activated && Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
            _display2Activated = true;
            StartCoroutine(FindDisplay2WindowCoroutine());
        }
        else if (_display2Hwnd != System.IntPtr.Zero)
            ShowWindow(_display2Hwnd, visible ? SW_SHOW : SW_HIDE);
#endif
        UpdateGameTileButton();
    }

    void UpdateGameTileButton()
    {
        if (_gameTileBtnText  != null) _gameTileBtnText.text = _gameTileVisible ? "晶片示意 ON" : "晶片示意";
        if (_gameTileBtnImage != null)
            _gameTileBtnImage.color = _gameTileVisible
                ? new Color(0.15f, 0.18f, 0.25f)
                : new Color(0.30f, 0.18f, 0.12f);
    }

    // ── 點擊模式切換 ──────────────────────────────────────────────────────────

    void OnToggleGameTile()  => SetGameTileVisible(!_gameTileVisible);

    void OnToggleGridLine()
    {
        _gridLineVisible = !_gridLineVisible;
        _overlay?.SetGridLineVisible(_gridLineVisible);
        UpdateGridLineButton();
    }

    void UpdateGridLineButton()
    {
        if (_gridLineBtnText  != null) _gridLineBtnText.text = _gridLineVisible ? "格線 ON" : "格線";
        if (_gridLineBtnImage != null)
            _gridLineBtnImage.color = _gridLineVisible
                ? new Color(0.15f, 0.18f, 0.25f)
                : new Color(0.30f, 0.18f, 0.12f);
    }

    void OnToggleScreenOverlay()
    {
        _screenOverlayVisible = !_screenOverlayVisible;
        _overlay?.SetScreenOverlayVisible(_screenOverlayVisible);
        UpdateScreenOverlayButton();
    }

    void UpdateScreenOverlayButton()
    {
        if (_screenOverlayBtnText  != null) _screenOverlayBtnText.text = _screenOverlayVisible ? "疊加 ON" : "疊加";
        if (_screenOverlayBtnImage != null)
            _screenOverlayBtnImage.color = _screenOverlayVisible
                ? new Color(0.15f, 0.18f, 0.25f)
                : new Color(0.30f, 0.18f, 0.12f);
    }

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
            if (_clickModeBtnImage != null) _clickModeBtnImage.color = new Color(0.25f, 0.18f, 0.18f);
            return;
        }
        _clickModeBtnText.text = _clickSim.ModeLabel;
        if (_clickModeBtnImage != null) _clickModeBtnImage.color = _clickSim.ModeColor;
    }

    // ── 感應冷卻調整 ──────────────────────────────────────────────────────────

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
                ? new Color(0.15f, 0.35f, 0.20f)
                : new Color(0.35f, 0.20f, 0.15f);
    }

    void OnDecreaseCooldown()
    {
        if (_clickSim == null) return;
        _clickSim.clickCooldown = Mathf.Max(0.1f, Mathf.Round((_clickSim.clickCooldown - 0.1f) * 10f) / 10f);
        UpdateCooldownText();
    }

    void OnIncreaseCooldown()
    {
        if (_clickSim == null) return;
        _clickSim.clickCooldown = Mathf.Min(5.0f, Mathf.Round((_clickSim.clickCooldown + 0.1f) * 10f) / 10f);
        UpdateCooldownText();
    }

    void UpdateCooldownText()
    {
        if (_cooldownText == null) return;
        bool  on = _clickSim != null && _clickSim.cooldownEnabled;
        float v  = _clickSim != null ? _clickSim.clickCooldown : 1f;
        _cooldownText.text  = $"{v:F1} 秒";
        _cooldownText.color = on ? new Color(0.95f, 0.95f, 1.00f) : new Color(0.40f, 0.40f, 0.42f);
    }

    void OnDecreaseOverlayAlpha()
    {
        if (_overlay == null) return;
        _overlay.overlayAlpha = Mathf.Clamp(Mathf.Round((_overlay.overlayAlpha - 0.1f) * 10f) / 10f, 0f, 1f);
        UpdateOverlayAlphaText();
    }

    void OnIncreaseOverlayAlpha()
    {
        if (_overlay == null) return;
        _overlay.overlayAlpha = Mathf.Clamp(Mathf.Round((_overlay.overlayAlpha + 0.1f) * 10f) / 10f, 0f, 1f);
        UpdateOverlayAlphaText();
    }

    void UpdateOverlayAlphaText()
    {
        if (_overlayAlphaText == null) return;
        int pct = Mathf.RoundToInt((_overlay != null ? _overlay.overlayAlpha : 1f) * 100f);
        _overlayAlphaText.text = $"{pct} %";
    }

    void OnDecreaseDim()
    {
        if (_dimOverlay == null) return;
        _dimOverlay.dimAlpha = Mathf.Clamp(Mathf.Round((_dimOverlay.dimAlpha - 0.1f) * 10f) / 10f, 0f, 1f);
        UpdateDimAlphaText();
    }

    void OnIncreaseDim()
    {
        if (_dimOverlay == null) return;
        _dimOverlay.dimAlpha = Mathf.Clamp(Mathf.Round((_dimOverlay.dimAlpha + 0.1f) * 10f) / 10f, 0f, 1f);
        UpdateDimAlphaText();
    }

    void UpdateDimAlphaText()
    {
        if (_dimAlphaText == null) return;
        int pct = Mathf.RoundToInt((_dimOverlay != null ? _dimOverlay.dimAlpha : 0f) * 100f);
        _dimAlphaText.text = $"{pct} %";
    }

    // ── Display 2 視窗控制（獨立執行檔專用）────────────────────────────────────

#if !UNITY_EDITOR
    IEnumerator FindDisplay2WindowCoroutine()
    {
        int           threshold = display2WindowOffsetX;
        System.IntPtr found     = System.IntPtr.Zero;
        float         elapsed   = 0f;

        while (found == System.IntPtr.Zero && elapsed < 3f)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;

            EnumWindows((hwnd, _) =>
            {
                var sb = new System.Text.StringBuilder(128);
                GetClassName(hwnd, sb, 128);
                if (sb.ToString() != "UnityWndClass") return true;
                GetWindowRect(hwnd, out RECT rect);
                if (rect.left >= threshold) { found = hwnd; return false; }
                return true;
            }, System.IntPtr.Zero);
        }

        _display2Hwnd = found;
        _clickSim?.SetDisplay2Hwnd(found);
        if (found != System.IntPtr.Zero)
        {
            Debug.Log($"[SensorTest] Display 2 HWND = 0x{found.ToInt64():X}");
            ShowWindow(found, _gameTileVisible ? SW_SHOW : SW_HIDE);
        }
        else
            Debug.LogWarning("[SensorTest] 找不到 Display 2 視窗。");
    }
#endif
}
