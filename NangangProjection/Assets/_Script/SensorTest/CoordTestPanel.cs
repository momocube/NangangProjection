using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 座標測試面板：在 Display 0 右上角提供 rawX / rawY 輸入框，
/// 按「觸發」後直接呼叫 SensorTestManager.TriggerRawCoord，
/// 並顯示對應的欄列號，用於校準驗證。
///
/// 使用方式：將此 Component 加到場景中任意 GameObject（建議與 SensorTestManager 同一個）。
/// 不需要 Inspector 拖拽，Start() 會自動尋找 SensorTestManager 與主螢幕 Canvas。
/// </summary>
public class CoordTestPanel : MonoBehaviour
{
    // Inspector 可選填；留空時自動 FindObjectOfType
    public SensorTestManager sensorTestManager;

    InputField _inputX;
    InputField _inputY;
    Text       _resultLabel;

    // ---------------------------------------------------------------
    IEnumerator Start()
    {
        // 等一幀，確保 SensorTestManager.Start() 已建好 Canvas
        yield return null;

        if (sensorTestManager == null)
            sensorTestManager = FindObjectOfType<SensorTestManager>();

        if (sensorTestManager == null)
        {
            Debug.LogWarning("[CoordTestPanel] 找不到 SensorTestManager，面板不建立。");
            yield break;
        }

        Canvas primary = FindPrimaryCanvas();
        if (primary == null)
        {
            Debug.LogWarning("[CoordTestPanel] 找不到 Display 0 的 ScreenSpaceOverlay Canvas。");
            yield break;
        }

        BuildPanel(primary.GetComponent<RectTransform>());
    }

    // ---------------------------------------------------------------
    Canvas FindPrimaryCanvas()
    {
        foreach (var c in FindObjectsOfType<Canvas>())
            if (c.targetDisplay == 0 && c.renderMode == RenderMode.ScreenSpaceOverlay)
                return c;
        return null;
    }

    // ---------------------------------------------------------------
    void BuildPanel(RectTransform canvasRT)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // ── 根面板：右上角，280×126 px，緊貼 header 下方 ────────────
        var panel = NewGO("CoordTestPanel", canvasRT);
        var panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1f, 1f);
        panelRT.anchorMax = new Vector2(1f, 1f);
        panelRT.pivot     = new Vector2(1f, 1f);
        panelRT.offsetMax = new Vector2( -4f,  -62f);   // 距右 4px，距上 62px（header 58 + 4）
        panelRT.offsetMin = new Vector2(-284f, -188f);   // 280px 寬，126px 高
        panel.AddComponent<Image>().color = new Color(0.09f, 0.09f, 0.12f, 0.95f);

        // ── 標題（24 px）────────────────────────────────────────────
        MakeLabel(panel.transform, "Title", "座標測試",
            0f, 1f, 0f, -24f,
            13, TextAnchor.MiddleLeft, new Color(0.80f, 0.85f, 1.00f), font, padLeft: 6f);

        // ── 原始 X（28 px）──────────────────────────────────────────
        MakeLabel(panel.transform, "LabelX", "原始 X",
            0f, 0.22f, -24f, -52f,
            12, TextAnchor.MiddleCenter, new Color(0.60f, 0.65f, 0.75f), font);
        _inputX = MakeInputField(panel.transform, "InputX",
            0.22f, 1f, -24f, -52f, "rawX", font);

        // ── 原始 Y（28 px）──────────────────────────────────────────
        MakeLabel(panel.transform, "LabelY", "原始 Y",
            0f, 0.22f, -52f, -80f,
            12, TextAnchor.MiddleCenter, new Color(0.60f, 0.65f, 0.75f), font);
        _inputY = MakeInputField(panel.transform, "InputY",
            0.22f, 1f, -52f, -80f, "rawY", font);

        // ── 觸發按鈕 + 結果標籤（28 px）────────────────────────────
        var btn = MakeButton(panel.transform, "BtnTrigger", "觸 發",
            0f, 0.28f, -80f, -108f, font);
        btn.onClick.AddListener(OnTrigger);

        _resultLabel = MakeLabel(panel.transform, "Result", "→",
            0.30f, 1f, -80f, -108f,
            12, TextAnchor.MiddleLeft, new Color(0.55f, 0.58f, 0.65f), font);
    }

    // ---------------------------------------------------------------
    void OnTrigger()
    {
        if (!int.TryParse(_inputX.text, out int rawX) ||
            !int.TryParse(_inputY.text, out int rawY))
        {
            ShowResult("→ 請輸入整數", error: true);
            return;
        }

        var mgr = sensorTestManager;
        float nx = Mathf.InverseLerp(mgr.rawMinX, mgr.rawMaxX, rawX);
        float ny = Mathf.InverseLerp(mgr.rawMinY, mgr.rawMaxY, rawY);

        if (nx < -0.1f || nx > 1.1f || ny < -0.1f || ny > 1.1f)
        {
            ShowResult("→ 超出範圍（過濾）", error: true);
            return;
        }

        int c = Mathf.Clamp(Mathf.FloorToInt(nx * mgr.cols), 0, mgr.cols - 1);
        int r = Mathf.Clamp(Mathf.FloorToInt(ny * mgr.rows), 0, mgr.rows - 1);

        ShowResult($"→ 欄{c} 列{r}", error: false);
        mgr.TriggerRawCoord(rawX, rawY);
    }

    void ShowResult(string msg, bool error)
    {
        _resultLabel.text  = msg;
        _resultLabel.color = error
            ? new Color(1.00f, 0.40f, 0.40f)
            : new Color(0.50f, 1.00f, 0.60f);
    }

    // ---------------------------------------------------------------
    // ── UI 工具方法 ──────────────────────────────────────────────────

    static GameObject NewGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return go;
    }

    static RectTransform AttachRT(GameObject go,
        float axMin, float axMax, float oyTop, float oyBot,
        float oxLeft = 0f, float oxRight = 0f)
    {
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(axMin, 1f);
        rt.anchorMax = new Vector2(axMax, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.offsetMin = new Vector2(oxLeft,  oyBot);
        rt.offsetMax = new Vector2(oxRight, oyTop);
        return rt;
    }

    static Text MakeLabel(Transform parent, string name, string text,
        float axMin, float axMax, float oyTop, float oyBot,
        int fontSize, TextAnchor anchor, Color color, Font font,
        float padLeft = 0f)
    {
        var go = NewGO(name, parent);
        AttachRT(go, axMin, axMax, oyTop, oyBot, padLeft);
        var t = go.AddComponent<Text>();
        t.text          = text;
        t.font          = font;
        t.fontSize      = fontSize;
        t.color         = color;
        t.alignment     = anchor;
        t.raycastTarget = false;
        return t;
    }

    static InputField MakeInputField(Transform parent, string name,
        float axMin, float axMax, float oyTop, float oyBot,
        string placeholder, Font font)
    {
        var go = NewGO(name, parent);
        AttachRT(go, axMin, axMax, oyTop, oyBot, oxLeft: 2f, oxRight: -2f);
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.20f, 0.28f);

        // 輸入文字
        var textGO = NewGO("Text", go.transform);
        var tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(4f, 2f); tRT.offsetMax = new Vector2(-4f, -2f);
        var tComp = textGO.AddComponent<Text>();
        tComp.font = font; tComp.fontSize = 13;
        tComp.color = new Color(0.90f, 0.92f, 1.00f);
        tComp.alignment = TextAnchor.MiddleLeft;
        tComp.supportRichText = false;

        // Placeholder
        var phGO = NewGO("Placeholder", go.transform);
        var phRT = phGO.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(4f, 2f); phRT.offsetMax = new Vector2(-4f, -2f);
        var phComp = phGO.AddComponent<Text>();
        phComp.font = font; phComp.fontSize = 13;
        phComp.color = new Color(0.45f, 0.48f, 0.55f);
        phComp.fontStyle = FontStyle.Italic;
        phComp.text = placeholder;
        phComp.alignment = TextAnchor.MiddleLeft;
        phComp.supportRichText = false;

        var field = go.AddComponent<InputField>();
        field.textComponent = tComp;
        field.placeholder   = phComp;
        field.contentType   = InputField.ContentType.IntegerNumber;
        field.targetGraphic = bg;
        return field;
    }

    static Button MakeButton(Transform parent, string name, string label,
        float axMin, float axMax, float oyTop, float oyBot, Font font)
    {
        var go = NewGO(name, parent);
        AttachRT(go, axMin, axMax, oyTop, oyBot, oxLeft: 2f, oxRight: -2f);
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.35f, 0.55f);

        var textGO = NewGO("Text", go.transform);
        var tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;
        var tComp = textGO.AddComponent<Text>();
        tComp.text = label; tComp.font = font;
        tComp.fontSize = 13; tComp.alignment = TextAnchor.MiddleCenter;
        tComp.color = new Color(0.85f, 0.90f, 1.00f);
        tComp.raycastTarget = false;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        var colors = btn.colors;
        colors.normalColor      = new Color(0.25f, 0.35f, 0.55f);
        colors.highlightedColor = new Color(0.35f, 0.45f, 0.70f);
        colors.pressedColor     = new Color(0.15f, 0.25f, 0.40f);
        colors.selectedColor    = new Color(0.25f, 0.35f, 0.55f);
        btn.colors = colors;
        return btn;
    }
}
