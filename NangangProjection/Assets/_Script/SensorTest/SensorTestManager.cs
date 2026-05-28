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
    [Tooltip("觸發後亮紅持續秒數。")]
    public float activeDuration = 1.5f;

    // ── 場域資料 ──────────────────────────────────────────────────────────────

    class FaceData
    {
        public SensorFaceConfig config;
        public bool[]           locked;
        public int              lockedCount;
    }

    // 執行緒安全鎖定快照：ParsePacket 背景執行緒用此在入佇列前過濾敏感晶片訊號。
    // volatile 參考 + 不可變內容 = 無需 lock 的 publish-immutable-state 模式。
    sealed class LockedSnapshot
    {
        public readonly SensorFaceConfig[] configs;
        public readonly bool[][]           locked;
        public LockedSnapshot(SensorFaceConfig[] c, bool[][] l) { configs = c; locked = l; }
    }

    public static SensorTestManager Instance { get; private set; }

    int                  _presetIdx;
    DisplayCanvasConfig  _canvasConfig;
    bool                 _rotateDisplay90;
    List<FaceData>       _faces = new List<FaceData>();
    volatile LockedSnapshot _lockedSnapshot;
    bool                 _snapshotDirty;

    // key = faceIdx * 100000 + cellIdx；value = 1 秒滑動窗口的觸發時間戳
    readonly Dictionary<int, Queue<float>> _cellHitTimes = new Dictionary<int, Queue<float>>();
    readonly Dictionary<int, float> _chipLastTouchTimes = new Dictionary<int, float>();
    readonly List<int> _staleHitKeys = new List<int>();
    readonly List<int> _staleChipTouchKeys = new List<int>();

    // ── 跳變模式 ─────────────────────────────────────────────────────────────────
    bool _jumpModeEnabled = false;
    readonly HashSet<int> _jumpModeOccupied = new HashSet<int>();

    int TotalLocked    { get { int n = 0; foreach (var f in _faces) n += f.lockedCount; return n; } }
    int TotalCells     { get { int n = 0; foreach (var f in _faces) n += f.config.colCount * f.config.rowCount; return n; } }

    // ── 執行期 UI 狀態 ─────────────────────────────────────────────────────────

    TextMeshProUGUI  _statusText;
    string           _lastStatusText;   // dirty flag：避免 TMP 每幀重算 layout
    float            _lastStatusUpdateTime;  // 狀態列更新限流（10Hz）
    float            _cellHitCleanTimer;     // _cellHitTimes 定期清理計時器
    bool  _smartLockActive;
    TextMeshProUGUI  _smartLockBtnText;
    Image _smartLockBtnImage;

    [SerializeField] bool  _gameTileVisible = false;
    TextMeshProUGUI  _gameTileBtnText;
    Image _gameTileBtnImage;

    SensorClickSimulator _clickSim;
    VirtualCellManager   _vcellManager;
    TextMeshProUGUI      _clickModeBtnText;
    Image                _clickModeBtnImage;

    TextMeshProUGUI  _cooldownText;
    TextMeshProUGUI  _cooldownToggleBtnText;
    Image _cooldownToggleBtnImage;

    TextMeshProUGUI  _jumpModeBtnText;
    Image            _jumpModeBtnImage;

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

    // ── 遊戲場景狀態 ──────────────────────────────────────────────────────────

    bool _gameSceneLoaded = false;   // true 後不再用 ShowWindow 控制 Display 2 視窗

    // ── 載入面板 ──────────────────────────────────────────────────────────────

    GameObject    _loadingPanel;
    TextMeshProUGUI _loadingTitle;
    TextMeshProUGUI _loadingProgressText;
    RectTransform _loadingBarFill;
    bool          _isSwitching;

    // ── 晶片示意 ──────────────────────────────────────────────────────────────

    GameObject _canvasSecondary;

    // ── Texture2D 格子系統 ────────────────────────────────────────────────────

    TileGridTexture _tileGrid;
    GameObject      _d2SensorRawImage;
    GameObject      _controlTexRawImage;
    int             _hoverFace = -1, _hoverCell = -1;

    public TileGridTexture TileGrid => _tileGrid;

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
    bool _dragCtrl;        // Ctrl 鍵在右鍵按下那幀的狀態（決定整次拖曳為晶片鎖還是 VCell 鎖）
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
    [DllImport("user32.dll")] static extern int  GetWindowLong(System.IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] static extern int  SetWindowLong(System.IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] static extern bool SetLayeredWindowAttributes(System.IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    const int  SW_HIDE      = 0;
    const int  SW_SHOW      = 5;
    const int  GWL_EXSTYLE    = -20;
    const int  WS_EX_LAYERED  = 0x00080000;
    const int  WS_EX_TRANSPARENT = 0x00000020;
    const uint LWA_ALPHA      = 0x00000002;

    // ── 生命週期 ──────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;

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

        // Canvas_Primary 的 GraphicRaycaster 換成 Display1GraphicRaycaster，
        // 防止感應器 OS 點擊事件透過 Unity Raw Input 誤觸 Display 1 按鈕。
        SwapCanvasPrimaryRaycaster();
    }

    void SwapCanvasPrimaryRaycaster()
    {
        var primaryCanvas = controlGridParent != null
            ? controlGridParent.GetComponentInParent<Canvas>()
            : null;
        if (primaryCanvas == null) return;

        var gr = primaryCanvas.GetComponent<GraphicRaycaster>();
        if (gr != null && gr is not Display1GraphicRaycaster)
        {
            DestroyImmediate(gr);
            primaryCanvas.gameObject.AddComponent<Display1GraphicRaycaster>();
        }
    }

    IEnumerator Start()
    {
        // includeInactive:true — Awake 已將 Canvas_Secondary 設為 inactive，
        // 預設的 GetComponentInParent 在 Unity 2021+ 不搜尋 inactive 父物件
        _canvasSecondary = sensorGridParent?.GetComponentInParent<Canvas>(true)?.gameObject;

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
        BuildGridTextures();

        yield return null;   // 等一幀讓佈局完成
        _clickSim    = FindObjectOfType<SensorClickSimulator>();
        _vcellManager = GetComponent<VirtualCellManager>();
        if (_vcellManager != null)
        {
            _vcellManager.Initialize(presets[_presetIdx], _canvasConfig);
            _vcellManager.SetTileGrid(_tileGrid);
            _vcellManager.SetChipLockedQuery((fi, ci) =>
                fi < _faces.Count && ci < _faces[fi].locked.Length && _faces[fi].locked[ci]);
        }
        _overlay    = GetComponent<ConsoleGameOverlay>();
        _dimOverlay = GetComponent<Display2DimOverlay>();
        if (_overlay == null)
            Debug.LogError("[SensorTestManager] ConsoleGameOverlay 元件不在此 GameObject 上，格點疊加層不會顯示。請將元件移到 SensorTestManager 同一個 GO。");
        NotifyOverlay();
        SyncD2TileAlpha();
        RebuildSensorVCellOverlay();
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
            {
                yield return SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Additive);
                _gameSceneLoaded = true;
            }
            else
                Debug.LogWarning($"[SensorTest] 場景 '{gameSceneName}' 未加入 Build Settings，略過載入。");
        }

        SetGameTileVisible(_gameTileVisible);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        UdpSensorReceiver.OnPacketReceived -= OnUdpPacket;
        DestroyClickLog();
    }

    /// <summary>遊戲 RT 狀態改變時由 ConsoleGameOverlay 呼叫，同步 D2 感應格 idle 透明度。</summary>
    public void RefreshSensorTileIdleColors()
    {
        float alpha = 1f - (_overlay != null ? _overlay.overlayAlpha : 0f);
        _tileGrid?.SetSensorIdleAlpha(alpha);
    }

    /// <summary>overlayAlpha 改變時同步 D2 感應格透明度（1-alpha → 格子 idle alpha）。</summary>
    void SyncD2TileAlpha()
    {
        if (_overlay == null) return;
        _tileGrid?.SetSensorIdleAlpha(1f - _overlay.overlayAlpha);
#if !UNITY_EDITOR
        ApplyD2WindowAlpha(_overlay.overlayAlpha);
#endif
    }

#if !UNITY_EDITOR
    /// <summary>
    /// WS_EX_LAYERED：讓 D2 視窗對 OS 合成器半透明（alpha=1 全不透明，0 全透明）。
    /// WS_EX_TRANSPARENT：讓 D2 視窗 click-through，WindowFromPoint 穿透到背景視窗，
    ///   PostMessage / SendInput 才能正確打到背景程式，而不是被 Unity 自己攔截。
    /// D2 Canvas_Secondary 純顯示用，本來就不需要接收任何 OS 點擊事件。
    /// </summary>
    void ApplyD2WindowAlpha(float alpha)
    {
        if (_display2Hwnd == System.IntPtr.Zero) return;
        int ex    = GetWindowLong(_display2Hwnd, GWL_EXSTYLE);
        int newEx = ex | WS_EX_LAYERED | WS_EX_TRANSPARENT;
        if (ex != newEx)
            SetWindowLong(_display2Hwnd, GWL_EXSTYLE, newEx);
        byte b = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
        SetLayeredWindowAttributes(_display2Hwnd, 0, b, LWA_ALPHA);
    }
#endif

    // ── 場域預設切換 ──────────────────────────────────────────────────────────

    /// <summary>初始化場域資料（不建格子）。</summary>
    void InitPresetData(int idx)
    {
        _presetIdx    = idx;
        _canvasConfig = presets[idx].canvasConfig;
        _viewFaceIdx  = -1;   // 切換場域時重設為全覽
        // _rotateDisplay90 由 CalcControlScale() 自動決定，此處不手動設定
        _chipLastTouchTimes.Clear();
        _faces.Clear();
        foreach (var fc in presets[idx].faces)
        {
            if (fc == null) continue;
            var fd = new FaceData
            {
                config      = fc,
                locked      = new bool[fc.colCount * fc.rowCount],
                lockedCount = 0
            };
            _faces.Add(fd);
        }
        RebuildLockedSnapshot();   // 場域切換後以新面資料建立乾淨快照
    }

    void DestroyGridTiles()
    {
        _smartTouches.Clear();
        _cellHitTimes.Clear();
        _chipLastTouchTimes.Clear();
        _jumpModeOccupied.Clear();
        _pendingCells.Clear();
        _faces.Clear();
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

        TeardownGridDisplay();
        DestroyGridTiles();

        InitPresetData(idx);
        _vcellManager?.Initialize(presets[idx], _canvasConfig);
        SetLoadingProgress(0.1f);
        yield return null;

        BuildGridTextures();
        _vcellManager?.SetTileGrid(_tileGrid);
        SetLoadingProgress(0.95f);
        yield return null;

        SetGameTileVisible(_gameTileVisible);
        NotifyOverlay();
        SyncD2TileAlpha();
        RebuildSensorVCellOverlay();
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
        // 硬體輸出 raw sensor 值，用 ContainsRaw 找面後 RawToPixel 轉畫布像素
        int bestFace = -1;
        int pixelX = rawX, pixelY = rawY;
        for (int fi = 0; fi < _faces.Count; fi++)
        {
            var cfg = _faces[fi].config;
            if (!cfg.ContainsRaw(rawX, rawY)) continue;
            (pixelX, pixelY) = cfg.RawToPixel(rawX, rawY);
            bestFace = fi;
            break;
        }

        if (bestFace < 0)
        {
            if (!_smartLockActive)
                SetStatusText($"raw({rawX},{rawY})  →  超出所有面的範圍（未觸發）");
            return;
        }

        var cfg2 = _faces[bestFace].config;
        var (col, row) = cfg2.PixelToCell(pixelX, pixelY);
        int cellIdx = row * cfg2.colCount + col;

        if (!_smartLockActive)
        {
            float nowT = Time.realtimeSinceStartup;
            if (nowT - _lastStatusUpdateTime >= 0.1f)
            {
                _lastStatusUpdateTime = nowT;
                SetStatusText($"raw({rawX},{rawY})  →  {cfg2.faceName} 面  欄{col} 列{row}" +
                    $"  [{_lastHitPerSec} 次/秒]");
            }
        }

        if (!ProcessGhostTouch(bestFace, cellIdx)) return;
        ActivateCell(bestFace, cellIdx, skipVisual: true);
        UpdateSmartTouch(bestFace, cellIdx);
    }

    // ── 鎖定快照（執行緒安全，供 UDP 背景執行緒過濾）────────────────────────

    /// <summary>
    /// 從主執行緒的鎖定狀態建立不可變快照並原子發佈。
    /// 呼叫後背景執行緒下一個封包即生效。
    /// </summary>
    void RebuildLockedSnapshot()
    {
        int n = _faces.Count;
        var configs = new SensorFaceConfig[n];
        var locked  = new bool[n][];
        for (int f = 0; f < n; f++)
        {
            configs[f] = _faces[f].config;
            locked[f]  = (bool[])_faces[f].locked.Clone();
        }
        _lockedSnapshot = new LockedSnapshot(configs, locked);
    }

    /// <summary>
    /// 背景執行緒安全：判斷 (rawX, rawY) 是否對應鎖定格子。
    /// 使用 volatile 快照，O(face 數) 查詢，無需 lock。
    /// </summary>
    public bool IsRawPixelLocked(int rawX, int rawY)
    {
        var snap = _lockedSnapshot;
        if (snap == null) return false;
        for (int f = 0; f < snap.configs.Length; f++)
        {
            var cfg = snap.configs[f];
            if (!cfg.ContainsRaw(rawX, rawY)) continue;
            var (px, py) = cfg.RawToPixel(rawX, rawY);
            var (col, row) = cfg.PixelToCell(px, py);
            int idx = row * cfg.colCount + col;
            return idx < snap.locked[f].Length && snap.locked[f][idx];
        }
        return false;
    }

    // ── 觸發 / 鎖定 ──────────────────────────────────────────────────────────

    void ActivateCell(int faceIdx, int cellIdx, bool skipVisual = false)
    {
        var fd = _faces[faceIdx];
        if (_smartLockActive && !fd.locked[cellIdx])
        {
            fd.locked[cellIdx] = true;
            fd.lockedCount++;
            _tileGrid?.SetLocked(faceIdx, cellIdx, true);
            _snapshotDirty = true;   // Update() 統一重建，避免每次觸碰都 clone 陣列
            return;
        }
        if (fd.locked[cellIdx]) return;

        int col = cellIdx % fd.config.colCount;
        int row = cellIdx / fd.config.colCount;
        int key = faceIdx * 100000 + cellIdx;
        float now = Time.realtimeSinceStartup;

        if (!skipVisual)
        {
            // 用 clickCooldown 做視覺 + 廣播的統一 debounce（物理滑鼠與感應訊號共用）
            bool cooldownOn = _clickSim != null && _clickSim.cooldownEnabled;
            float debounceS = cooldownOn ? _clickSim.clickCooldown : 0f;
            bool shouldActivate = debounceS <= 0f
                || !_chipLastTouchTimes.TryGetValue(key, out float lastTouchTime)
                || now - lastTouchTime > debounceS;
            _chipLastTouchTimes[key] = now;

            if (!shouldActivate) return;

            _tileGrid?.SetActive(faceIdx, cellIdx);
        }

        // 跳變模式：放在 skipVisual 判斷外，確保 UDP 路徑（skipVisual=true）也能被攔截
        if (_jumpModeEnabled)
        {
            if (_jumpModeOccupied.Contains(key))
                return;
            _jumpModeOccupied.Add(key);
        }

        // VCell 啟用：直接通知 VCellManager（不廣播 SensorBus 晶片事件）
        if (_vcellManager != null && _vcellManager.Enabled)
        {
            _vcellManager.HandleChipTouch(faceIdx, cellIdx);
        }
        else
        {
            BroadcastCanvasCell(fd, col, row, TouchKind.Touch);
            SensorBus.Touch(col, row);
        }

        // 滑動窗口：計算此格最近 1 秒的觸發次數
        if (!_cellHitTimes.TryGetValue(key, out var q))
        {
            q = new Queue<float>();
            _cellHitTimes[key] = q;
        }
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

    void CleanCellHitTimes()
    {
        float now = Time.realtimeSinceStartup;
        _staleHitKeys.Clear();
        foreach (var kv in _cellHitTimes)
        {
            var q = kv.Value;
            while (q.Count > 0 && now - q.Peek() > 1f) q.Dequeue();
            if (q.Count == 0) _staleHitKeys.Add(kv.Key);
        }
        foreach (var k in _staleHitKeys) _cellHitTimes.Remove(k);

        _staleChipTouchKeys.Clear();
        float chipDebounceS = (_clickSim != null && _clickSim.cooldownEnabled)
            ? _clickSim.clickCooldown
            : touchEndTimeoutMs / 1000f;
        foreach (var kv in _chipLastTouchTimes)
            if (now - kv.Value > chipDebounceS)
                _staleChipTouchKeys.Add(kv.Key);
        foreach (var k in _staleChipTouchKeys) _chipLastTouchTimes.Remove(k);
    }

    void ToggleLock(int faceIdx, int col, int row)
    {
        var fd  = _faces[faceIdx];
        int idx = row * fd.config.colCount + col;
        fd.locked[idx]   = !fd.locked[idx];
        fd.lockedCount  += fd.locked[idx] ? 1 : -1;
        _tileGrid?.SetLocked(faceIdx, idx, fd.locked[idx]);
        _vcellManager?.RefreshLockOverlaysForChip(faceIdx, idx);
        Debug.Log($"[SensorTest] face={faceIdx} #{idx + 1} ({col},{row}) " +
                  $"{(fd.locked[idx] ? "鎖定" : "解鎖")} | 共鎖定 {TotalLocked} 格");
        SetStatusIdle();
        RebuildLockedSnapshot();   // 手動鎖定立即生效，下一個 UDP 封包即過濾
    }

    void OnLeftClick(int faceIdx, int cellIdx) => ActivateCell(faceIdx, cellIdx);

    void OnHoldTick(int faceIdx, int cellIdx) => ActivateCell(faceIdx, cellIdx, skipVisual: true);

    void OnMousePressStart(int faceIdx, int cellIdx)
    {
        var fd = _faces[faceIdx];
        if (fd.locked[cellIdx]) return;
        _tileGrid?.SetPressing(faceIdx, cellIdx, true);
        if (_vcellManager == null || !_vcellManager.Enabled)
        {
            int col = cellIdx % fd.config.colCount;
            int row = cellIdx / fd.config.colCount;
            BroadcastCanvasCell(fd, col, row, TouchKind.PressStart);
            SensorBus.PressStart(col, row);
        }
    }

    void OnMousePressEnd(int faceIdx, int cellIdx)
    {
        var fd = _faces[faceIdx];
        _tileGrid?.SetPressing(faceIdx, cellIdx, false);
        if (_vcellManager == null || !_vcellManager.Enabled)
        {
            int col = cellIdx % fd.config.colCount;
            int row = cellIdx / fd.config.colCount;
            BroadcastCanvasCell(fd, col, row, TouchKind.PressEnd);
            SensorBus.PressEnd(col, row);
        }
    }

    enum TouchKind { Touch, Tap, PressStart, PressEnd, Drag }

    void BroadcastCanvasCell(FaceData fd, int col, int row, TouchKind kind)
    {
        if (fd == null || fd.config == null || _canvasConfig == null) return;

        int cellW = fd.config.GizmoCellW;
        int cellH = fd.config.GizmoCellH;
        Vector2Int topLeft = fd.config.CellToPixel(col, row);
        float x = topLeft.x + cellW * 0.5f;
        float y = topLeft.y + cellH * 0.5f;
        float w = _canvasConfig.canvasWidth;
        float h = _canvasConfig.canvasHeight;

        switch (kind)
        {
            case TouchKind.Touch:      SensorBus.CanvasTouch(x, y, w, h); break;
            case TouchKind.Tap:        SensorBus.CanvasTap(x, y, w, h); break;
            case TouchKind.PressStart: SensorBus.CanvasPressStart(x, y, w, h); break;
            case TouchKind.PressEnd:   SensorBus.CanvasPressEnd(x, y, w, h); break;
            case TouchKind.Drag:       SensorBus.CanvasDrag(x, y, w, h); break;
        }
    }

    /// <summary>CoordTestPanel 呼叫：以原始感應座標觸發（與真實 UDP 封包路徑相同）。</summary>
    public void TriggerRawCoord(int rawX, int rawY) => OnUdpPacket(rawX, rawY);

    /// <summary>
    /// 遊戲程式碼呼叫：以 raw sensor 座標亮起對應格子並廣播 CanvasTouch。
    /// 跳過 ghost touch 過濾與智慧鎖格，直接點亮視覺並送出事件。
    /// 回傳 false 代表座標超出所有面的感應範圍。
    /// </summary>
    public bool LightUpRaw(int rawX, int rawY)
    {
        for (int fi = 0; fi < _faces.Count; fi++)
        {
            var cfg = _faces[fi].config;
            if (!cfg.ContainsRaw(rawX, rawY)) continue;
            var (px, py) = cfg.RawToPixel(rawX, rawY);
            var (col, row) = cfg.PixelToCell(px, py);
            int cellIdx = row * cfg.colCount + col;
            var fd = _faces[fi];
            _tileGrid?.SetActive(fi, cellIdx);
            BroadcastCanvasCell(fd, col, row, TouchKind.Touch);
            return true;
        }
        return false;
    }

    public int    FaceCount              => _faces.Count;
    public string GetFaceName(int fi)    => (fi >= 0 && fi < _faces.Count) ? _faces[fi].config.faceName : "?";

    /// <summary>
    /// CoordTestPanel 呼叫：計算原始座標對應的面 / 欄 / 列，不觸發動畫。
    /// 回傳 false 代表超出所有面的感應範圍（會被過濾掉）。
    /// </summary>
    public bool GetCellForRaw(int rawX, int rawY, out int faceIdx, out int col, out int row)
    {
        for (int fi = 0; fi < _faces.Count; fi++)
        {
            var cfg = _faces[fi].config;
            if (!cfg.ContainsRaw(rawX, rawY)) continue;
            var (px, py) = cfg.RawToPixel(rawX, rawY);
            (col, row) = cfg.PixelToCell(px, py);
            faceIdx = fi;
            return true;
        }
        faceIdx = col = row = 0;
        return false;
    }

    // ── Update：拖曳互動 ──────────────────────────────────────────────────────

    void Update()
    {
        if (_smartLockActive)
        {
            float nowT = Time.realtimeSinceStartup;
            if (nowT - _lastStatusUpdateTime >= 0.1f)
            {
                _lastStatusUpdateTime = nowT;
                SetStatusText($"智慧鎖格 開啟中  |  被觸碰的格子將自動鎖定  |  已鎖定：{TotalLocked} 格");
            }
        }

        // 幀時間監控：記錄期間若 deltaTime > 33ms 視為掉幀
        if (_logRecording && Time.deltaTime > 0.033f)
        {
            _logFrameDropCount++;
            LogDiagEvent($"掉幀  {Time.deltaTime * 1000f:F0}ms  ≈{1f / Time.deltaTime:F0}fps  frame#{Time.frameCount}");
        }

        HandleDragInteract();
        UpdateClickLogPanel();
        FlushExpiredSmartTouches();
        CleanExpiredPendingCells();

        // 每 30 秒清一次長期不再觸碰的 _cellHitTimes entry
        _cellHitCleanTimer += Time.deltaTime;
        if (_cellHitCleanTimer >= 30f)
        {
            _cellHitCleanTimer = 0f;
            CleanCellHitTimes();
        }

        // 智慧鎖格批次更新快照（避免每次觸碰都 clone 陣列）
        if (_snapshotDirty)
        {
            _snapshotDirty = false;
            RebuildLockedSnapshot();
        }
    }

    void HandleDragInteract()
    {
        if (Display1GraphicRaycaster.ShouldSuppressDisplay1Pointer()) return;
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
            // 右鍵按下當幀捕捉 Ctrl 狀態，決定整次拖曳使用晶片鎖還是 VCell 鎖
            _dragCtrl = rightHeld &&
                        (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
            return;
        }

        if (_dragInitPending)
        {
            // Ctrl 或無 VCell 功能 → 晶片鎖；否則 → VCell 鎖
            if (_dragCtrl || _vcellManager == null || !_vcellManager.Enabled)
                _dragIsLock = _faces[_dragInitFace].locked[_dragInitIdx];
            else
                _dragIsLock = _vcellManager.IsVCellLocked(_dragInitFace, _dragInitIdx);
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
            if (_dragCtrl || _vcellManager == null || !_vcellManager.Enabled)
            {
                if (_faces[fi].locked[idx] != _dragIsLock)
                    ToggleLock(fi, col, row);
            }
            else
            {
                if (_vcellManager.IsVCellLocked(fi, idx) != _dragIsLock)
                    _vcellManager.ToggleVCellLock(fi, idx);
            }
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
        BuildControlDisplay();
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
            SetStatusText($"智慧鎖格 開啟中  |  被觸碰的格子將自動鎖定  |  已鎖定：{TotalLocked} 格");
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
        for (int fi = 0; fi < _faces.Count; fi++)
        {
            var fd = _faces[fi];
            for (int i = 0; i < fd.locked.Length; i++)
            {
                if (!fd.locked[i]) continue;
                fd.locked[i] = false;
                _tileGrid?.SetLocked(fi, i, false);
            }
            fd.lockedCount = 0;
        }
        _vcellManager?.UnlockAllVCells();
        SetStatusIdle();
        Debug.Log("[SensorTest] 解除全部鎖定");
        RebuildLockedSnapshot();   // 全部解鎖：清空快照，背景執行緒不再過濾任何訊號
    }

    void OnTileHover(int faceIdx, int col, int row, int idx, bool locked)
    {
        string ls = locked ? " 🔒 已鎖定" : "";
        SetStatusText(
            $"格子 #{idx + 1}   欄 {col}   列 {row}{ls}   |  " +
            $"已鎖定：{TotalLocked} 格  |  右鍵鎖定 / 解鎖");
    }

    void SetStatusText(string s)
    {
        if (_statusText == null || s == _lastStatusText) return;
        _lastStatusText  = s;
        _statusText.text = s;
    }

    void SetStatusIdle()
    {
        if (_statusText == null || _smartLockActive) return;
        int total = TotalCells;
        SetStatusText(
            $"感應測試  |  {total} 個晶片  |  " +
            $"已鎖定：{TotalLocked} 格  |  右鍵鎖定 / 解鎖  左鍵模擬觸碰");
    }

    // ── 晶片示意（Display 2）開關 ─────────────────────────────────────────────

    public bool GameTileVisible => _gameTileVisible;

    public void SetGameTileVisible(bool visible)
    {
        _gameTileVisible = visible;
        if (_canvasSecondary != null) _canvasSecondary.SetActive(visible);
#if !UNITY_EDITOR
        if (visible && !_display2Activated && Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
            _display2Activated = true;
            StartCoroutine(FindDisplay2WindowCoroutine());
        }
        else if (_display2Hwnd != System.IntPtr.Zero && !_gameSceneLoaded)
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

    void OnToggleJumpMode()
    {
        _jumpModeEnabled = !_jumpModeEnabled;
        if (!_jumpModeEnabled) _jumpModeOccupied.Clear();
        UpdateJumpModeButton();
    }

    void UpdateJumpModeButton()
    {
        if (_jumpModeBtnText == null) return;
        _jumpModeBtnText.text = _jumpModeEnabled ? "跳變 ON" : "跳變";
        if (_jumpModeBtnImage != null)
            _jumpModeBtnImage.color = _jumpModeEnabled
                ? new Color(0.15f, 0.35f, 0.20f)
                : new Color(0.15f, 0.18f, 0.25f);
    }

    void OnDecreaseOverlayAlpha()
    {
        if (_overlay == null) return;
        _overlay.overlayAlpha = Mathf.Clamp(Mathf.Round((_overlay.overlayAlpha - 0.1f) * 10f) / 10f, 0f, 1f);
        UpdateOverlayAlphaText();
        SyncD2TileAlpha();
    }

    void OnIncreaseOverlayAlpha()
    {
        if (_overlay == null) return;
        _overlay.overlayAlpha = Mathf.Clamp(Mathf.Round((_overlay.overlayAlpha + 0.1f) * 10f) / 10f, 0f, 1f);
        UpdateOverlayAlphaText();
        SyncD2TileAlpha();
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
            ApplyD2WindowAlpha(_overlay != null ? _overlay.overlayAlpha : 1f);
            ShowWindow(found, _gameTileVisible ? SW_SHOW : SW_HIDE);
        }
        else
            Debug.LogWarning("[SensorTest] 找不到 Display 2 視窗。");
    }
#endif
}
