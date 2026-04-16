using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 雙螢幕感應測試主控（多場域預設版）。
///
/// Inspector 中指定 VenuePresetConfig[] presets，執行時可透過控制台
/// 上方的 [◀] / [▶] 按鈕切換場域。每次切換都會重建格子並更新 RT 大小。
///
/// 單場域（2048×768 舞台）行為與舊版完全相同。
/// </summary>
public class SensorTestManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("場域預設（Inspector 設定，至少一個）")]
    public VenuePresetConfig[] presets;

    [Header("場景引用（Inspector 設定）")]
    public RectTransform controlGridParent;   // Canvas_Primary 的子容器
    public RectTransform sensorGridParent;    // Canvas_Secondary 的子容器

    [Header("遊戲場景（留空則不自動載入）")]
    public string gameSceneName = "Map_CybercubeLogo_J_2048x768";

    [Header("Display 2 視窗偵測")]
    [Tooltip("Display 2 在 Windows 桌面的 X 起始位置（預設 1920）。")]
    public int display2WindowOffsetX = 1920;

    // ── 場域資料 ──────────────────────────────────────────────────────────────

    class FaceData
    {
        public SensorFaceConfig config;
        public SensorTileCell[]  sensorTiles;
        public ControlTileCell[] controlTiles;
        public bool[]            locked;
        public int               lockedCount;
    }

    int                  _presetIdx;
    DisplayCanvasConfig  _canvasConfig;
    List<FaceData>       _faces = new List<FaceData>();

    int TotalLocked    { get { int n = 0; foreach (var f in _faces) n += f.lockedCount; return n; } }
    int TotalCells     { get { int n = 0; foreach (var f in _faces) n += f.config.colCount * f.config.rowCount; return n; } }

    // ── 執行期 UI 狀態 ─────────────────────────────────────────────────────────

    Text  _statusText;
    bool  _smartLockActive;
    Text  _smartLockBtnText;
    Image _smartLockBtnImage;

    bool  _gameTileVisible = true;
    Text  _gameTileBtnText;
    Image _gameTileBtnImage;

    SensorClickSimulator _clickSim;
    Text                 _clickModeBtnText;
    Image                _clickModeBtnImage;

    Text  _cooldownText;
    Text  _cooldownToggleBtnText;
    Image _cooldownToggleBtnImage;

    ConsoleGameOverlay _overlay;
    Text               _overlayAlphaText;

    Text  _presetLabel;

    // ── 晶片示意 ──────────────────────────────────────────────────────────────

    GameObject _canvasSecondary;
    Camera     _sensorGridCam;
    GameObject _display2MirrorCanvas;

    // ── 拖曳互動 ──────────────────────────────────────────────────────────────

    int  _dragLastFace    = -1;
    int  _dragLastIdx     = -1;
    int  _dragInitFace    = -1;
    int  _dragInitIdx     = -1;
    bool _dragIsLock;
    bool _dragInitPending;

    // ── Display 2 視窗控制 ────────────────────────────────────────────────────

    System.IntPtr _display2Hwnd = System.IntPtr.Zero;

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
        // 先讀第一個 preset 取畫布尺寸
        int w = 2048, h = 768;
        if (presets != null && presets.Length > 0 && presets[0]?.canvasConfig != null)
        {
            w = presets[0].canvasConfig.canvasWidth;
            h = presets[0].canvasConfig.canvasHeight;
        }
        if (Display.displays.Length > 1)
            Display.displays[1].Activate(w, h, 60);

        Debug.Log($"[SensorTest] 偵測到 {Display.displays.Length} 個螢幕 | Display 2 啟動 {w}×{h}");
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
        _clickSim = FindObjectOfType<SensorClickSimulator>();
        _overlay  = GetComponent<ConsoleGameOverlay>();
        UpdateClickModeButton();
        UpdateCooldownToggleButton();
        UpdateCooldownText();
        UpdateOverlayAlphaText();
        UpdatePresetLabel();

#if !UNITY_EDITOR
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
                SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Additive);
            else
                Debug.LogWarning($"[SensorTest] 場景 '{gameSceneName}' 未加入 Build Settings，略過載入。");
        }
    }

    void OnDestroy()
    {
        UdpSensorReceiver.OnPacketReceived -= OnUdpPacket;
        SensorGridRT.Release();
    }

    // ── 場域預設切換 ──────────────────────────────────────────────────────────

    /// <summary>初始化場域資料（不建格子）。</summary>
    void InitPresetData(int idx)
    {
        _presetIdx    = idx;
        _canvasConfig = presets[idx].canvasConfig;
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
            _faces.Add(fd);
        }
    }

    void DestroyGridTiles()
    {
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
        if (presets == null || idx < 0 || idx >= presets.Length) return;
        if (idx == _presetIdx && _faces.Count > 0) return;

        // 重置鎖定狀態、感應格背景
        TeardownSensorGridRT();
        DestroyGridTiles();

        // 清除感應格背景（重建）
        if (sensorGridParent != null)
        {
            var bg = sensorGridParent.parent?.Find("SensorBG");
            if (bg != null) Destroy(bg.gameObject);
        }

        InitPresetData(idx);
        BuildSensorBackground();
        BuildSensorGrid();
        BuildControlGrid();
        SetupSensorGridRT();
        UpdatePresetLabel();
        SetStatusIdle();

        Debug.Log($"[SensorTest] 切換場域 → {presets[idx].presetName}");
    }

    // ── 感應格（Display 2）建立 ───────────────────────────────────────────────

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
        if (sensorGridParent == null || _canvasConfig == null) return;

        // 清除舊格子（只清 sensorGridParent 下的格子物件，SensorBG 在其 parent 下）
        for (int i = sensorGridParent.childCount - 1; i >= 0; i--)
            Destroy(sensorGridParent.GetChild(i).gameObject);

        // 移除 GridLayoutGroup（改用絕對定位）
        var oldGlg = sensorGridParent.GetComponent<GridLayoutGroup>();
        if (oldGlg != null) Destroy(oldGlg);

        foreach (var fd in _faces)
        {
            var cfg = fd.config;
            for (int r = 0; r < cfg.rowCount; r++)
            for (int c = 0; c < cfg.colCount; c++)
            {
                int idx = r * cfg.colCount + c;
                var go = new GameObject($"ST_f{_faces.IndexOf(fd)}_{idx}");
                go.layer = 5;
                go.transform.SetParent(sensorGridParent, false);
                var rt = go.AddComponent<RectTransform>();
                PositionSensorCell(rt, cfg, c, r);
                go.AddComponent<Image>();
                var cell = go.AddComponent<SensorTileCell>();
                cell.col   = c;
                cell.row   = r;
                cell.index = idx;
                fd.sensorTiles[idx] = cell;
            }
        }
    }

    /// <summary>在 Canvas_Secondary（感應格）中以像素座標定位格子（1px = 1 canvas unit）。</summary>
    static void PositionSensorCell(RectTransform rt, SensorFaceConfig cfg, int c, int r)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(
            cfg.originX + c * cfg.GizmoCellW,
          -(cfg.originY + r * cfg.GizmoCellH)
        );
        rt.sizeDelta = new Vector2(cfg.GizmoCellW, cfg.GizmoCellH);
    }

    // ── 控制格（Display 1）建立 ───────────────────────────────────────────────

    void BuildControlGrid()
    {
        if (controlGridParent == null || _canvasConfig == null) return;

        // 清除舊格子
        for (int i = controlGridParent.childCount - 1; i >= 0; i--)
            Destroy(controlGridParent.GetChild(i).gameObject);

        // 移除 GridLayoutGroup（改用絕對定位）
        var oldGlg = controlGridParent.GetComponent<GridLayoutGroup>();
        if (oldGlg != null) Destroy(oldGlg);

        float w = controlGridParent.rect.width;
        float h = controlGridParent.rect.height;
        if (w <= 0 || h <= 0)
        {
            // rect 尚未佈局，等下一幀重建（理論上只有第一幀才會發生）
            StartCoroutine(RebuildControlGridNextFrame());
            return;
        }

        float sx = w / _canvasConfig.canvasWidth;
        float sy = h / _canvasConfig.canvasHeight;

        for (int fi = 0; fi < _faces.Count; fi++)
        {
            var fd  = _faces[fi];
            var cfg = fd.config;
            int capturedFi = fi;

            for (int r = 0; r < cfg.rowCount; r++)
            for (int c = 0; c < cfg.colCount; c++)
            {
                int idx = r * cfg.colCount + c;
                var go = new GameObject($"CT_f{fi}_{idx}");
                go.layer = 5;
                go.transform.SetParent(controlGridParent, false);
                var rt = go.AddComponent<RectTransform>();
                PositionControlCell(rt, cfg, c, r, sx, sy);
                go.AddComponent<Image>();
                var cell = go.AddComponent<ControlTileCell>();
                cell.Init(c, r, cfg.colCount);

                int capturedIdx = idx;
                cell.onRightClick = (col2, row2) => ToggleLock(capturedFi, col2, row2);
                cell.onLeftClick  = (col2, row2) => OnLeftClick(capturedFi, row2 * cfg.colCount + col2);
                cell.onHover      = (col2, row2, i2, lk) => OnTileHover(capturedFi, col2, row2, i2, lk);
                fd.controlTiles[idx] = cell;
            }
        }
    }

    IEnumerator RebuildControlGridNextFrame()
    {
        yield return null;
        BuildControlGrid();
    }

    /// <summary>在 controlGridParent 中按比例縮放定位格子（畫布像素 → 控制區像素）。</summary>
    static void PositionControlCell(RectTransform rt, SensorFaceConfig cfg,
        int c, int r, float sx, float sy)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(
            (cfg.originX + c * cfg.GizmoCellW) * sx,
          -((cfg.originY + r * cfg.GizmoCellH) * sy)
        );
        rt.sizeDelta = new Vector2(
            Mathf.Max(1f, cfg.GizmoCellW * sx),
            Mathf.Max(1f, cfg.GizmoCellH * sy)
        );
    }

    // ── 主螢幕：UI 骨架 ───────────────────────────────────────────────────────

    void BuildPrimaryUI()
    {
        if (controlGridParent == null) return;
        var root = controlGridParent.parent as RectTransform;
        if (root == null) return;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 深色背景
        CreatePanel(root, "PrimaryBG", new Color(0.05f, 0.05f, 0.08f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 0);

        // 頂部標題列 (58 px)
        var header = CreatePanel(root, "Header", new Color(0.09f, 0.11f, 0.17f),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -58f), Vector2.zero);

        // 標題（左側 55%）
        var titleArea = CreatePanel(header, "TitleArea", Color.clear,
            new Vector2(0f, 0f), new Vector2(0.55f, 1f), Vector2.zero, Vector2.zero);
        titleArea.GetComponent<Image>().raycastTarget = false;
        var titleTxt = AddText(titleArea.transform, "TitleText",
            "感應測試控制台",
            18, TextAnchor.MiddleLeft, new Color(0.80f, 0.85f, 1.00f), font);
        (titleTxt.transform as RectTransform).offsetMin = new Vector2(12f, 0f);

        // 場域選擇（右側 45%）
        var presetArea = CreatePanel(header, "PresetArea", Color.clear,
            new Vector2(0.55f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        presetArea.GetComponent<Image>().raycastTarget = false;
        BuildPresetSelector(presetArea, font);

        // 底部狀態列 (38 px)
        var status = CreatePanel(root, "StatusBar", new Color(0.07f, 0.09f, 0.13f),
            Vector2.zero, new Vector2(1f, 0f),
            Vector2.zero, new Vector2(0f, 38f));
        _statusText = AddText(status.transform, "StatusText",
            "感應測試",
            13, TextAnchor.MiddleLeft, new Color(0.60f, 0.65f, 0.75f), font);
        (_statusText.transform as RectTransform).offsetMin = new Vector2(12f, 0f);

        // 按鈕列 (52 px)
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

        // 靈敏度列 (38 px)
        var sensBar = CreatePanel(root, "SensitivityBar", new Color(0.06f, 0.08f, 0.12f),
            Vector2.zero, new Vector2(1f, 0f),
            new Vector2(0f, 90f), new Vector2(0f, 128f));
        BuildSensitivityBar(sensBar, font);
    }

    void BuildPresetSelector(RectTransform parent, Font font)
    {
        // [◀]  場域名稱  [▶]
        CreateButton(parent, "BtnPresetPrev", "◀", OnPresetPrev, 0f,    0.20f, font);

        var labelBg = CreatePanel(parent, "PresetLabelBg", new Color(0.10f, 0.12f, 0.18f),
            new Vector2(0.20f, 0.15f), new Vector2(0.80f, 0.85f), Vector2.zero, Vector2.zero);
        labelBg.GetComponent<Image>().raycastTarget = false;
        _presetLabel = AddText(labelBg.transform, "PresetLabel", "—",
            13, TextAnchor.MiddleCenter, new Color(0.85f, 0.90f, 1.00f), font);

        CreateButton(parent, "BtnPresetNext", "▶", OnPresetNext, 0.80f, 1.00f, font);
    }

    void OnPresetPrev() => SwitchPreset((_presetIdx - 1 + presets.Length) % presets.Length);
    void OnPresetNext() => SwitchPreset((_presetIdx + 1) % presets.Length);

    void UpdatePresetLabel()
    {
        if (_presetLabel == null || presets == null || presets.Length == 0) return;
        var p = presets[_presetIdx];
        int w = p?.canvasConfig?.canvasWidth  ?? 0;
        int h = p?.canvasConfig?.canvasHeight ?? 0;
        _presetLabel.text = $"{p?.presetName ?? "?"}\n{w}×{h}";
    }

    // ── UDP 解析 ──────────────────────────────────────────────────────────────

    void OnUdpPacket(int rawX, int rawY)
    {
        for (int fi = 0; fi < _faces.Count; fi++)
        {
            var fd  = _faces[fi];
            var cfg = fd.config;

            // 過濾明顯超出此面範圍的封包（各面 rawMin/Max 不同，避免跨面觸發）
            float nx = Mathf.InverseLerp(cfg.rawMinX, cfg.rawMaxX, rawX);
            float ny = Mathf.InverseLerp(cfg.rawMinY, cfg.rawMaxY, rawY);
            if (nx < -0.1f || nx > 1.1f || ny < -0.1f || ny > 1.1f) continue;

            var (col, row) = cfg.RawToCell(rawX, rawY);
            ActivateCell(fi, row * cfg.colCount + col);
        }
    }

    // ── 觸發 / 鎖定 ──────────────────────────────────────────────────────────

    void ActivateCell(int faceIdx, int cellIdx)
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

        fd.controlTiles[cellIdx]?.Activate();
        if (_gameTileVisible)
            fd.sensorTiles[cellIdx]?.Activate();

        int col = cellIdx % fd.config.colCount;
        int row = cellIdx / fd.config.colCount;
        SensorBus.Touch(col, row);
    }

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
    }

    void HandleDragInteract()
    {
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

        float sx = rect.width  / _canvasConfig.canvasWidth;
        float sy = rect.height / _canvasConfig.canvasHeight;

        for (int fi = 0; fi < _faces.Count; fi++)
        {
            var cfg = _faces[fi].config;
            float ox = cfg.originX          * sx;
            float oy = cfg.originY          * sy;
            float fw = cfg.TotalPixelWidth  * sx;
            float fh = cfg.TotalPixelHeight * sy;
            if (px < ox || py < oy || px >= ox + fw || py >= oy + fh) continue;

            faceIdx = fi;
            col = Mathf.Clamp((int)((px - ox) / (cfg.GizmoCellW * sx)), 0, cfg.colCount - 1);
            row = Mathf.Clamp((int)((py - oy) / (cfg.GizmoCellH * sy)), 0, cfg.rowCount - 1);
            return true;
        }
        return false;
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
        if (_smartLockBtnText  != null) _smartLockBtnText.text = _smartLockActive ? "智慧鎖格 ✓" : "智慧鎖格";
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

    public void SetGameTileVisible(bool visible)
    {
        _gameTileVisible = visible;
        if (_canvasSecondary != null) _canvasSecondary.SetActive(visible);
        if (_display2MirrorCanvas != null) _display2MirrorCanvas.SetActive(visible);
#if !UNITY_EDITOR
        if (_display2Hwnd != System.IntPtr.Zero)
            ShowWindow(_display2Hwnd, visible ? SW_SHOW : SW_HIDE);
#endif
        UpdateGameTileButton();
    }

    void UpdateGameTileButton()
    {
        if (_gameTileBtnText  != null) _gameTileBtnText.text = _gameTileVisible ? "晶片示意 ✓" : "晶片示意";
        if (_gameTileBtnImage != null)
            _gameTileBtnImage.color = _gameTileVisible
                ? new Color(0.15f, 0.18f, 0.25f)
                : new Color(0.30f, 0.18f, 0.12f);
    }

    // ── 感應格 RT 捕捉 ────────────────────────────────────────────────────────

    void SetupSensorGridRT()
    {
        if (_canvasSecondary == null || _canvasConfig == null) return;

        var sensorCanvas = _canvasSecondary.GetComponent<Canvas>();
        if (sensorCanvas == null) return;

        int w = _canvasConfig.canvasWidth;
        int h = _canvasConfig.canvasHeight;

        var rt = SensorGridRT.GetOrCreate(w, h);

        // 建立 SensorCam
        var camGO = new GameObject("SensorGridCam");
        camGO.layer = 5;
        camGO.transform.SetParent(transform, false);
        _sensorGridCam = camGO.AddComponent<Camera>();
        _sensorGridCam.clearFlags       = CameraClearFlags.SolidColor;
        _sensorGridCam.backgroundColor  = new Color(0.04f, 0.04f, 0.05f);
        _sensorGridCam.cullingMask      = 1 << 5;
        _sensorGridCam.orthographic     = true;
        _sensorGridCam.orthographicSize = h / 2f;
        _sensorGridCam.aspect           = (float)w / h;
        _sensorGridCam.nearClipPlane    = -100f;
        _sensorGridCam.farClipPlane     = 100f;
        _sensorGridCam.targetTexture    = rt;
        _sensorGridCam.targetDisplay    = 0;
        _sensorGridCam.depth            = -10;

        var scaler = _canvasSecondary.GetComponent<CanvasScaler>();
        if (scaler != null) scaler.enabled = false;

        sensorCanvas.renderMode  = RenderMode.ScreenSpaceCamera;
        sensorCanvas.worldCamera = _sensorGridCam;

        _display2MirrorCanvas = BuildDisplay2Mirror(rt);

        Debug.Log($"[SensorTest] SensorGridRT 捕捉設定完成（{w}×{h}）");
    }

    static GameObject BuildDisplay2Mirror(RenderTexture rt)
    {
        var go = new GameObject("Canvas_Display2RTMirror");
        go.layer = 5;
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = 1;
        canvas.sortingOrder  = -10;

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

    // ── 點擊模式切換 ──────────────────────────────────────────────────────────

    void OnToggleGameTile()  => SetGameTileVisible(!_gameTileVisible);

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

    void BuildSensitivityBar(RectTransform parent, Font font)
    {
        // 說明標籤（0-10%）
        var cooldownLabel = CreatePanel(parent, "CooldownLabel", Color.clear,
            new Vector2(0f, 0f), new Vector2(0.10f, 1f), Vector2.zero, Vector2.zero);
        cooldownLabel.GetComponent<Image>().raycastTarget = false;
        AddText(cooldownLabel.transform, "LabelText", "感應冷卻",
            13, TextAnchor.MiddleRight, new Color(0.60f, 0.65f, 0.75f), font);

        _cooldownToggleBtnText =
            CreateButton(parent, "BtnCooldownToggle", "冷卻 ON", OnToggleCooldown, 0.10f, 0.24f, font);
        _cooldownToggleBtnImage = _cooldownToggleBtnText.transform.parent.GetComponent<Image>();

        CreateButton(parent, "BtnCooldownDec", "−", OnDecreaseCooldown, 0.24f, 0.32f, font);

        var valPanel = CreatePanel(parent, "CooldownValue", new Color(0.10f, 0.12f, 0.16f),
            new Vector2(0.32f, 0.1f), new Vector2(0.43f, 0.9f), Vector2.zero, Vector2.zero);
        valPanel.GetComponent<Image>().raycastTarget = false;
        _cooldownText = AddText(valPanel.transform, "ValueText", "1.0 秒",
            14, TextAnchor.MiddleCenter, new Color(0.95f, 0.95f, 1.00f), font);

        CreateButton(parent, "BtnCooldownInc", "+", OnIncreaseCooldown, 0.43f, 0.51f, font);

        var divider = CreatePanel(parent, "BarDivider", new Color(0.30f, 0.32f, 0.40f),
            new Vector2(0.51f, 0.1f), new Vector2(0.52f, 0.9f), Vector2.zero, Vector2.zero);
        divider.GetComponent<Image>().raycastTarget = false;

        var alphaLabel = CreatePanel(parent, "OverlayAlphaLabel", Color.clear,
            new Vector2(0.52f, 0f), new Vector2(0.62f, 1f), Vector2.zero, Vector2.zero);
        alphaLabel.GetComponent<Image>().raycastTarget = false;
        AddText(alphaLabel.transform, "LabelText", "疊加透明",
            13, TextAnchor.MiddleRight, new Color(0.60f, 0.65f, 0.75f), font);

        CreateButton(parent, "BtnOverlayDec", "−", OnDecreaseOverlayAlpha, 0.62f, 0.71f, font);

        var alphaValPanel = CreatePanel(parent, "OverlayAlphaValue", new Color(0.10f, 0.12f, 0.16f),
            new Vector2(0.71f, 0.1f), new Vector2(0.84f, 0.9f), Vector2.zero, Vector2.zero);
        alphaValPanel.GetComponent<Image>().raycastTarget = false;
        _overlayAlphaText = AddText(alphaValPanel.transform, "ValueText", "100 %",
            14, TextAnchor.MiddleCenter, new Color(0.95f, 0.95f, 1.00f), font);

        CreateButton(parent, "BtnOverlayInc", "+", OnIncreaseOverlayAlpha, 0.84f, 0.93f, font);

        var alphaHint = CreatePanel(parent, "OverlayAlphaHint", Color.clear,
            new Vector2(0.93f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        alphaHint.GetComponent<Image>().raycastTarget = false;
        AddText(alphaHint.transform, "HintText", "0~100%",
            11, TextAnchor.MiddleLeft, new Color(0.40f, 0.44f, 0.52f), font);

        UpdateCooldownToggleButton();
        UpdateCooldownText();
        UpdateOverlayAlphaText();
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

    // ── Display 2 視窗控制（獨立執行檔專用）────────────────────────────────────

#if !UNITY_EDITOR
    IEnumerator FindDisplay2WindowCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        System.IntPtr found   = System.IntPtr.Zero;
        int           threshold = display2WindowOffsetX;

        EnumWindows((hwnd, _) =>
        {
            var sb = new System.Text.StringBuilder(128);
            GetClassName(hwnd, sb, 128);
            if (sb.ToString() != "UnityWndClass") return true;
            GetWindowRect(hwnd, out RECT rect);
            if (rect.left >= threshold) { found = hwnd; return false; }
            return true;
        }, System.IntPtr.Zero);

        _display2Hwnd = found;
        if (found != System.IntPtr.Zero)
            Debug.Log($"[SensorTest] Display 2 HWND = 0x{found.ToInt64():X}");
        else
            Debug.LogWarning("[SensorTest] 找不到 Display 2 視窗。");
    }
#endif

    // ── UI 輔助方法 ───────────────────────────────────────────────────────────

    static RectTransform CreatePanel(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
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
        UnityEngine.Events.UnityAction onClick, float anchorXMin, float anchorXMax, Font font)
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
}

