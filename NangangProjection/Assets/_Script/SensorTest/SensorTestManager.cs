using System.Collections;
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

        UdpSensorReceiver.OnPacketReceived += OnUdpPacket;
        SetStatusIdle();

        if (!string.IsNullOrEmpty(gameSceneName))
            SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Additive);
    }

    void OnDestroy()
    {
        UdpSensorReceiver.OnPacketReceived -= OnUdpPacket;
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

        CreateButton(btnBar, "BtnQuit",   "離開遊戲", OnQuit,      0f,    1f/4f, font);
        _smartLockBtnText =
        CreateButton(btnBar, "BtnSmart",  "智慧鎖格", OnSmartLock, 1f/4f, 2f/4f, font);
        _smartLockBtnImage = _smartLockBtnText.transform.parent.GetComponent<Image>();
        CreateButton(btnBar, "BtnUnlock", "解除鎖格", OnUnlockAll, 2f/4f, 3f/4f, font);
        _gameTileBtnText =
        CreateButton(btnBar, "BtnGameTile", "晶片示意 ✓", OnToggleGameTile, 3f/4f, 1f, font);
        _gameTileBtnImage = _gameTileBtnText.transform.parent.GetComponent<Image>();

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
        _sensorTiles[idx]?.Activate();
        _controlTiles[idx]?.Activate();

        // 廣播到遊戲場景（VFX 互動）
        SensorBus.Touch(idx % cols, idx / cols);
    }

    void Update()
    {
        if (!_smartLockActive) return;
        if (_statusText != null)
            _statusText.text = $"智慧鎖格 開啟中  |  被觸碰的格子將自動鎖定  |  已鎖定：{_lockedCount} 格";
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
        UpdateGameTileButton();
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
}
