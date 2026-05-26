using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI 骨架建立（partial）。
/// 包含 BuildPrimaryUI / BuildSensitivityBar / BuildLoadingPanel 及靜態輔助方法。
/// 這些方法幾乎不需要改動，獨立成檔以降低讀主檔的成本。
/// </summary>
public partial class SensorTestManager
{
    // ── 主螢幕：UI 骨架 ───────────────────────────────────────────────────────

    void BuildPrimaryUI()
    {
        if (controlGridParent == null) return;
        var root = controlGridParent.parent as RectTransform;
        if (root == null) return;

        _uiFont = uiFontOverride != null ? uiFontOverride : TMP_Settings.defaultFontAsset;
        var font = _uiFont;

        // 深色背景
        CreatePanel(root, "PrimaryBG", new Color(0.05f, 0.05f, 0.08f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 0);

        // 頂部標題列 (58 px)
        var header = CreatePanel(root, "Header", new Color(0.09f, 0.11f, 0.17f),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -58f), Vector2.zero);

        // 標題（showPresetSelector = false 時佔滿整列）
        float titleRight = showPresetSelector ? 0.55f : 1f;
        var titleArea = CreatePanel(header, "TitleArea", Color.clear,
            new Vector2(0f, 0f), new Vector2(titleRight, 1f), Vector2.zero, Vector2.zero);
        titleArea.GetComponent<Image>().raycastTarget = false;
        var titleTxt = AddText(titleArea.transform, "TitleText",
            "感應測試控制台",
            18, TextAlignmentOptions.MidlineLeft, new Color(0.80f, 0.85f, 1.00f), font);
        (titleTxt.transform as RectTransform).offsetMin = new Vector2(12f, 0f);

        // 場域選擇（右側 45%，多場域才顯示）
        if (showPresetSelector)
        {
            var presetArea = CreatePanel(header, "PresetArea", Color.clear,
                new Vector2(0.55f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            presetArea.GetComponent<Image>().raycastTarget = false;
            BuildPresetSelector(presetArea, font);
        }

        // 底部狀態列 (38 px)
        var status = CreatePanel(root, "StatusBar", new Color(0.07f, 0.09f, 0.13f),
            Vector2.zero, new Vector2(1f, 0f),
            Vector2.zero, new Vector2(0f, 38f));
        _statusText = AddText(status.transform, "StatusText",
            "感應測試",
            13, TextAlignmentOptions.MidlineLeft, new Color(0.60f, 0.65f, 0.75f), font);
        (_statusText.transform as RectTransform).offsetMin = new Vector2(12f, 0f);

        // 按鈕列 (52 px)
        var btnBar = CreatePanel(root, "ButtonBar", new Color(0.06f, 0.08f, 0.12f),
            Vector2.zero, new Vector2(1f, 0f),
            new Vector2(0f, 38f), new Vector2(0f, 90f));

        CreateButton(btnBar, "BtnQuit",    "離開遊戲",   OnQuit,               0f,    1f/8f, font);
        CreateButton(btnBar, "BtnMini",    "縮小",       () => MiniWindowController.Instance?.Minimize(), 1f/8f, 2f/8f, font);
        _smartLockBtnText =
        CreateButton(btnBar, "BtnSmart",   "智慧鎖格",   OnSmartLock,          2f/8f, 3f/8f, font);
        _smartLockBtnImage = _smartLockBtnText.transform.parent.GetComponent<Image>();
        CreateButton(btnBar, "BtnUnlock",  "解除鎖格",   OnUnlockAll,          3f/8f, 4f/8f, font);
        _gameTileBtnText =
        CreateButton(btnBar, "BtnGameTile","晶片示意 ON", OnToggleGameTile,     4f/8f, 5f/8f, font);
        _gameTileBtnImage = _gameTileBtnText.transform.parent.GetComponent<Image>();
        _clickModeBtnText =
        CreateButton(btnBar, "BtnClickMode","滑鼠點擊 ON",OnToggleClickMode,    5f/8f, 6f/8f, font);
        _clickModeBtnImage = _clickModeBtnText.transform.parent.GetComponent<Image>();
        _gridLineBtnText =
        CreateButton(btnBar, "BtnGridLine","格線 ON",     OnToggleGridLine,     6f/8f, 7f/8f, font);
        _gridLineBtnImage = _gridLineBtnText.transform.parent.GetComponent<Image>();
        _screenOverlayBtnText =
        CreateButton(btnBar, "BtnScreenOverlay","疊加 ON",OnToggleScreenOverlay,7f/8f, 1f,    font);
        _screenOverlayBtnImage = _screenOverlayBtnText.transform.parent.GetComponent<Image>();

        // 靈敏度列 (38 px)
        var sensBar = CreatePanel(root, "SensitivityBar", new Color(0.06f, 0.08f, 0.12f),
            Vector2.zero, new Vector2(1f, 0f),
            new Vector2(0f, 90f), new Vector2(0f, 128f));
        BuildSensitivityBar(sensBar, font);

        // 手勢列 (38 px)：點擊 / 按壓 / 滑動勾選 + 按壓判定 ms 調整
        // 與其他 bar 相同，疊加在格子上方（不調整 controlGridParent 大小）
        var gestureBar = CreatePanel(root, "GestureBar", new Color(0.06f, 0.08f, 0.12f),
            Vector2.zero, new Vector2(1f, 0f),
            new Vector2(0f, 128f), new Vector2(0f, 166f));
        BuildGestureBar(gestureBar, font);

        BuildLoadingPanel(root, font);
        BuildClickLogPanel(root, font);

        // 讓格子容器填滿可用區域，四邊留 20px 空隙與 UI 區分
        // 底部：bars 166px + 20px pad；頂部：header 58px + 20px pad
        controlGridParent.anchorMin = Vector2.zero;
        controlGridParent.anchorMax = Vector2.one;
        controlGridParent.offsetMin = new Vector2(0f, 186f);
        controlGridParent.offsetMax = new Vector2(0f, -78f);
    }

    void BuildPresetSelector(RectTransform parent, TMP_FontAsset font)
    {
        // 上半（50%）：[◀] 場域名稱 [▶]
        var presetRow = CreatePanel(parent, "PresetRow", Color.clear,
            new Vector2(0f, 0.50f), Vector2.one, Vector2.zero, Vector2.zero);
        presetRow.GetComponent<Image>().raycastTarget = false;

        CreateButton(presetRow, "BtnPresetPrev", "◀", OnPresetPrev, 0f,    0.20f, font);
        var labelBg = CreatePanel(presetRow, "PresetLabelBg", new Color(0.10f, 0.12f, 0.18f),
            new Vector2(0.20f, 0.10f), new Vector2(0.80f, 0.90f), Vector2.zero, Vector2.zero);
        labelBg.GetComponent<Image>().raycastTarget = false;
        _presetLabel = AddText(labelBg.transform, "PresetLabel", "—",
            12, TextAlignmentOptions.Midline, new Color(0.85f, 0.90f, 1.00f), font);
        CreateButton(presetRow, "BtnPresetNext", "▶", OnPresetNext, 0.80f, 1.00f, font);

        // 下半（50%）：逐面選擇（由 BuildFaceSelector() 動態填入）
        _faceSelectorParent = CreatePanel(parent, "FaceRow", Color.clear,
            Vector2.zero, new Vector2(1f, 0.50f), Vector2.zero, Vector2.zero);
        _faceSelectorParent.GetComponent<Image>().raycastTarget = false;
    }

    void BuildFaceSelector()
    {
        if (_faceSelectorParent == null || _uiFont == null) return;

        for (int i = _faceSelectorParent.childCount - 1; i >= 0; i--)
            Destroy(_faceSelectorParent.GetChild(i).gameObject);

        int faceCount = _faces.Count;
        int btnCount  = faceCount + 1;   // +1 for "全"
        float btnW    = 1f / btnCount;

        _faceSelectorBtnImages = new Image[btnCount];

        // "全覽" 按鈕
        var allTxt = CreateButton(_faceSelectorParent, "BtnFaceAll", "全",
            () => OnFaceView(-1), 0f, btnW, _uiFont);
        allTxt.fontSize = 11;
        _faceSelectorBtnImages[0] = allTxt.transform.parent.GetComponent<Image>();

        for (int i = 0; i < faceCount; i++)
        {
            int   capturedFi = i;
            string rawName   = _faces[i].config.faceName;
            // 最多取前兩個字元以符合按鈕寬度
            string label = rawName.Length > 2 ? rawName.Substring(0, 2) : rawName;
            float xMin = (i + 1) * btnW;
            float xMax = (i + 2) * btnW;
            var txt = CreateButton(_faceSelectorParent, $"BtnFace{i}", label,
                () => OnFaceView(capturedFi), xMin, xMax, _uiFont);
            txt.fontSize = 11;
            _faceSelectorBtnImages[i + 1] = txt.transform.parent.GetComponent<Image>();
        }

        UpdateFaceSelectorButtons();
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

    // ── 感應冷卻 / 疊加透明調整列 ────────────────────────────────────────────

    void BuildSensitivityBar(RectTransform parent, TMP_FontAsset font)
    {
        // ── Section 1：感應冷卻 + 跳變模式 (0 → 0.33) ──────────────────────────────
        var cooldownLabel = CreatePanel(parent, "CooldownLabel", Color.clear,
            new Vector2(0.000f, 0f), new Vector2(0.060f, 1f), Vector2.zero, Vector2.zero);
        cooldownLabel.GetComponent<Image>().raycastTarget = false;
        AddText(cooldownLabel.transform, "LabelText", "感應冷卻",
            13, TextAlignmentOptions.MidlineRight, new Color(0.60f, 0.65f, 0.75f), font);

        _cooldownToggleBtnText =
            CreateButton(parent, "BtnCooldownToggle", "冷卻 ON", OnToggleCooldown, 0.060f, 0.160f, font);
        _cooldownToggleBtnImage = _cooldownToggleBtnText.transform.parent.GetComponent<Image>();

        CreateButton(parent, "BtnCooldownDec", "-", OnDecreaseCooldown, 0.160f, 0.200f, font);

        var cooldownValPanel = CreatePanel(parent, "CooldownValue", new Color(0.10f, 0.12f, 0.16f),
            new Vector2(0.200f, 0.1f), new Vector2(0.265f, 0.9f), Vector2.zero, Vector2.zero);
        cooldownValPanel.GetComponent<Image>().raycastTarget = false;
        _cooldownText = AddText(cooldownValPanel.transform, "ValueText", "1.0 秒",
            14, TextAlignmentOptions.Midline, new Color(0.95f, 0.95f, 1.00f), font);

        CreateButton(parent, "BtnCooldownInc", "+", OnIncreaseCooldown, 0.265f, 0.285f, font);

        var jumpDiv = CreatePanel(parent, "JumpModeDiv", new Color(0.30f, 0.32f, 0.40f),
            new Vector2(0.286f, 0.1f), new Vector2(0.290f, 0.9f), Vector2.zero, Vector2.zero);
        jumpDiv.GetComponent<Image>().raycastTarget = false;

        _jumpModeBtnText =
            CreateButton(parent, "BtnJumpMode", "跳變", OnToggleJumpMode, 0.291f, 0.333f, font);
        _jumpModeBtnImage = _jumpModeBtnText.transform.parent.GetComponent<Image>();

        var div1 = CreatePanel(parent, "BarDivider1", new Color(0.30f, 0.32f, 0.40f),
            new Vector2(0.333f, 0.1f), new Vector2(0.340f, 0.9f), Vector2.zero, Vector2.zero);
        div1.GetComponent<Image>().raycastTarget = false;

        // ── Section 2：疊加透明 (0.34 → 0.66) ───────────────────────────────────
        var alphaLabel = CreatePanel(parent, "OverlayAlphaLabel", Color.clear,
            new Vector2(0.340f, 0f), new Vector2(0.415f, 1f), Vector2.zero, Vector2.zero);
        alphaLabel.GetComponent<Image>().raycastTarget = false;
        AddText(alphaLabel.transform, "LabelText", "疊加透明",
            13, TextAlignmentOptions.MidlineRight, new Color(0.60f, 0.65f, 0.75f), font);

        CreateButton(parent, "BtnOverlayDec", "-", OnDecreaseOverlayAlpha, 0.415f, 0.465f, font);

        var alphaValPanel = CreatePanel(parent, "OverlayAlphaValue", new Color(0.10f, 0.12f, 0.16f),
            new Vector2(0.465f, 0.1f), new Vector2(0.575f, 0.9f), Vector2.zero, Vector2.zero);
        alphaValPanel.GetComponent<Image>().raycastTarget = false;
        _overlayAlphaText = AddText(alphaValPanel.transform, "ValueText", "100 %",
            14, TextAlignmentOptions.Midline, new Color(0.95f, 0.95f, 1.00f), font);

        CreateButton(parent, "BtnOverlayInc", "+", OnIncreaseOverlayAlpha, 0.575f, 0.625f, font);

        var div2 = CreatePanel(parent, "BarDivider2", new Color(0.30f, 0.32f, 0.40f),
            new Vector2(0.660f, 0.1f), new Vector2(0.667f, 0.9f), Vector2.zero, Vector2.zero);
        div2.GetComponent<Image>().raycastTarget = false;

        // ── Section 3：調暗 (0.667 → 0.862) ─────────────────────────────────────
        var dimLabel = CreatePanel(parent, "DimLabel", Color.clear,
            new Vector2(0.667f, 0f), new Vector2(0.720f, 1f), Vector2.zero, Vector2.zero);
        dimLabel.GetComponent<Image>().raycastTarget = false;
        AddText(dimLabel.transform, "LabelText", "調暗",
            13, TextAlignmentOptions.MidlineRight, new Color(0.60f, 0.65f, 0.75f), font);

        CreateButton(parent, "BtnDimDec", "-", OnDecreaseDim, 0.720f, 0.760f, font);

        var dimValPanel = CreatePanel(parent, "DimValue", new Color(0.10f, 0.12f, 0.16f),
            new Vector2(0.760f, 0.1f), new Vector2(0.848f, 0.9f), Vector2.zero, Vector2.zero);
        dimValPanel.GetComponent<Image>().raycastTarget = false;
        _dimAlphaText = AddText(dimValPanel.transform, "ValueText", "0 %",
            14, TextAlignmentOptions.Midline, new Color(0.95f, 0.95f, 1.00f), font);

        CreateButton(parent, "BtnDimInc", "+", OnIncreaseDim, 0.848f, 0.862f, font);

        var div3 = CreatePanel(parent, "BarDivider3", new Color(0.30f, 0.32f, 0.40f),
            new Vector2(0.863f, 0.1f), new Vector2(0.869f, 0.9f), Vector2.zero, Vector2.zero);
        div3.GetComponent<Image>().raycastTarget = false;

        // ── Section 4：訊號記錄 (0.872 → 1.0，可由 Inspector 關閉）────────────────
        if (showLogButton)
        {
            _loggerBtnText =
                CreateButton(parent, "BtnSignalLog", "訊號記錄", OnToggleLog, 0.872f, 1.000f, font);
            _loggerBtnImage = _loggerBtnText.transform.parent.GetComponent<Image>();
        }

        UpdateCooldownToggleButton();
        UpdateCooldownText();
        UpdateJumpModeButton();
        UpdateOverlayAlphaText();
        UpdateDimAlphaText();
    }



    // ── 載入面板 ──────────────────────────────────────────────────────────────

    void BuildLoadingPanel(RectTransform root, TMP_FontAsset font)
    {
        // 全螢幕半透明遮罩（建在 root 最上層）
        var panelRT = CreatePanel(root, "LoadingPanel",
            new Color(0.02f, 0.02f, 0.04f, 0.88f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        panelRT.GetComponent<Image>().raycastTarget = true;  // 擋住下層按鈕
        _loadingPanel = panelRT.gameObject;

        // 中央卡片
        var card = CreatePanel(panelRT, "Card", new Color(0.08f, 0.10f, 0.16f),
            new Vector2(0.25f, 0.38f), new Vector2(0.75f, 0.62f),
            Vector2.zero, Vector2.zero);

        // 標題文字
        var titleGO = new GameObject("Title");
        titleGO.layer = 5;
        titleGO.transform.SetParent(card, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.55f);
        titleRT.anchorMax = new Vector2(1f, 0.92f);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;
        _loadingTitle = titleGO.AddComponent<TextMeshProUGUI>();
        _loadingTitle.text         = "切換場域中...";
        _loadingTitle.fontSize     = 18;
        _loadingTitle.alignment    = TextAlignmentOptions.Midline;
        _loadingTitle.color        = new Color(0.80f, 0.85f, 1.00f);
        _loadingTitle.overflowMode = TextOverflowModes.Overflow;
        if (font != null) _loadingTitle.font = font;

        // 進度百分比文字
        var progGO = new GameObject("Progress");
        progGO.layer = 5;
        progGO.transform.SetParent(card, false);
        var progRT = progGO.AddComponent<RectTransform>();
        progRT.anchorMin = new Vector2(0f, 0.38f);
        progRT.anchorMax = new Vector2(1f, 0.55f);
        progRT.offsetMin = progRT.offsetMax = Vector2.zero;
        _loadingProgressText = progGO.AddComponent<TextMeshProUGUI>();
        _loadingProgressText.text         = "0 %";
        _loadingProgressText.fontSize     = 13;
        _loadingProgressText.alignment    = TextAlignmentOptions.Midline;
        _loadingProgressText.color        = new Color(0.55f, 0.60f, 0.75f);
        _loadingProgressText.overflowMode = TextOverflowModes.Overflow;
        if (font != null) _loadingProgressText.font = font;

        // 進度條底板
        var barBg = CreatePanel(card, "BarBG", new Color(0.12f, 0.14f, 0.20f),
            new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.35f),
            Vector2.zero, Vector2.zero);

        // 進度條填充（anchorMax.x 動態控制寬度）
        var fillGO = new GameObject("Fill");
        fillGO.layer = 5;
        fillGO.transform.SetParent(barBg, false);
        _loadingBarFill = fillGO.AddComponent<RectTransform>();
        _loadingBarFill.anchorMin = Vector2.zero;
        _loadingBarFill.anchorMax = new Vector2(0f, 1f);
        _loadingBarFill.offsetMin = _loadingBarFill.offsetMax = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color         = new Color(0.30f, 0.55f, 0.90f);
        fillImg.raycastTarget = false;

        _loadingPanel.SetActive(false);
    }

    void ShowLoading(bool show, string title = "切換場域中...")
    {
        if (_loadingPanel == null) return;
        if (_loadingTitle        != null) _loadingTitle.text        = title;
        if (_loadingProgressText != null) _loadingProgressText.text = "0 %";
        if (_loadingBarFill      != null) _loadingBarFill.anchorMax = new Vector2(0f, 1f);
        _loadingPanel.SetActive(show);
    }

    void SetLoadingProgress(float t)
    {
        if (_loadingBarFill      != null) _loadingBarFill.anchorMax      = new Vector2(Mathf.Clamp01(t), 1f);
        if (_loadingProgressText != null) _loadingProgressText.text = $"{Mathf.RoundToInt(t * 100f)} %";
    }

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

    static TextMeshProUGUI CreateButton(RectTransform parent, string name, string label,
        UnityEngine.Events.UnityAction onClick, float anchorXMin, float anchorXMax, TMP_FontAsset font)
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
            TextAlignmentOptions.Midline, new Color(0.85f, 0.90f, 1.00f), font);
        txt.raycastTarget = false;
        return txt;
    }

    static TextMeshProUGUI AddText(Transform parent, string name, string content,
        int fontSize, TextAlignmentOptions alignment, Color color, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text           = content;
        txt.fontSize       = fontSize;
        txt.alignment      = alignment;
        txt.color          = color;
        txt.raycastTarget  = false;
        txt.overflowMode   = TextOverflowModes.Overflow;
        if (font != null) txt.font = font;
        return txt;
    }
}
