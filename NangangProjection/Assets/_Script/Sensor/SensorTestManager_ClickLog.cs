using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Display 1 互動記錄面板（partial）。
/// 訂閱 SensorBus Tap / PressStart / PressEnd / Drag 事件（已過濾鎖住格子），
/// 自動顯示於控制台左上角，無需手動開啟。
/// 支援 col/row 模式（非 VCell）與 canvas 像素模式（VCell）。
/// </summary>
public partial class SensorTestManager
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("互動記錄面板")]
    [Tooltip("取消勾選可隱藏面板（不影響其他功能）")]
    public bool showClickLogPanel = true;

    [Tooltip("面板顯示幾筆記錄")]
    [Range(3, 10)]
    public int clickLogMaxLines = 6;

    [Tooltip("相同事件在此秒數內算同一筆（Collapse）")]
    public float clickLogCollapseWindow = 0.5f;

    // ── 內部狀態 ──────────────────────────────────────────────────────────────

    enum InteractionKind { Tap, PressStart, PressEnd, Drag }

    struct ClickLogEntry
    {
        public InteractionKind kind;
        public int   a, b;       // col/row（isCanvas=false）或 canvas px（isCanvas=true）
        public bool  isCanvas;
        public int   count;
        public float firstTime, lastTime;
    }

    // key = (kind, a, b)，ValueTuple 自帶正確 Equals / GetHashCode
    readonly Dictionary<(InteractionKind, int, int), ClickLogEntry> _logActiveWindow
        = new Dictionary<(InteractionKind, int, int), ClickLogEntry>();

    readonly List<string> _logFlushedLines = new List<string>();
    readonly Queue<float>  _logHitQueue    = new Queue<float>();

    // 預分配，避免 UpdateClickLogPanel 每幀 new
    readonly List<(InteractionKind, int, int)> _expiredLogKeys
        = new List<(InteractionKind, int, int)>();
    readonly System.Text.StringBuilder _clickLogSB = new System.Text.StringBuilder(512);
    int _lastLogHps = -1;

    TextMeshProUGUI _clickLogBodyText;
    TextMeshProUGUI _clickLogHpsText;

    // ── 建立面板 ──────────────────────────────────────────────────────────────

    void BuildClickLogPanel(RectTransform root, TMP_FontAsset font)
    {
        // 無論 showClickLogPanel 為何，都先訂閱事件
        SubscribeClickLog();
        if (!showClickLogPanel) return;

        const float titleH = 22f;
        const float lineH  = 16f;
        float bodyH  = clickLogMaxLines * lineH;
        float totalH = titleH + bodyH + 6f;

        var panel = CreatePanel(root, "ClickLogPanel",
            new Color(0.02f, 0.03f, 0.07f, 0.88f),
            new Vector2(0f, 1f), new Vector2(0.44f, 1f),
            new Vector2(2f,  -(58f + 2f + totalH)),
            new Vector2(-2f, -(58f + 2f)));

        var titleBar = CreatePanel(panel, "TitleBar",
            new Color(0.07f, 0.10f, 0.18f),
            new Vector2(0f, 1f), Vector2.one,
            new Vector2(0f, -titleH), Vector2.zero);
        titleBar.GetComponent<Image>().raycastTarget = false;

        var titleTxt = AddText(titleBar.transform, "Title", "互動記錄",
            11, TextAlignmentOptions.MidlineLeft,
            new Color(0.55f, 0.65f, 0.85f), font);
        (titleTxt.transform as RectTransform).offsetMin = new Vector2(6f, 0f);

        _clickLogHpsText = AddText(titleBar.transform, "Hps", "—",
            11, TextAlignmentOptions.MidlineRight,
            new Color(0.55f, 0.90f, 0.60f), font);
        (_clickLogHpsText.transform as RectTransform).offsetMax = new Vector2(-6f, 0f);

        var bodyPanel = CreatePanel(panel, "Body", Color.clear,
            Vector2.zero, new Vector2(1f, 1f),
            new Vector2(0f, 3f), new Vector2(0f, -titleH));
        bodyPanel.GetComponent<Image>().raycastTarget = false;

        _clickLogBodyText = AddText(bodyPanel.transform, "Body", "",
            11, TextAlignmentOptions.TopLeft,
            new Color(0.82f, 0.87f, 0.97f), font);
        var bodyRT = _clickLogBodyText.transform as RectTransform;
        bodyRT.offsetMin = new Vector2(6f,  2f);
        bodyRT.offsetMax = new Vector2(-4f, -2f);
    }

    // ── 訂閱 / 解除 ───────────────────────────────────────────────────────────

    void SubscribeClickLog()
    {
        SensorBus.OnTap              += OnClickLogTap;
        SensorBus.OnPressStart       += OnClickLogPressStart;
        SensorBus.OnPressEnd         += OnClickLogPressEnd;
        SensorBus.OnDrag             += OnClickLogDrag;
        SensorBus.OnCanvasTap        += OnClickLogCanvasTap;
        SensorBus.OnCanvasPressStart += OnClickLogCanvasPressStart;
        SensorBus.OnCanvasPressEnd   += OnClickLogCanvasPressEnd;
        SensorBus.OnCanvasDrag       += OnClickLogCanvasDrag;
    }

    void DestroyClickLog()
    {
        SensorBus.OnTap              -= OnClickLogTap;
        SensorBus.OnPressStart       -= OnClickLogPressStart;
        SensorBus.OnPressEnd         -= OnClickLogPressEnd;
        SensorBus.OnDrag             -= OnClickLogDrag;
        SensorBus.OnCanvasTap        -= OnClickLogCanvasTap;
        SensorBus.OnCanvasPressStart -= OnClickLogCanvasPressStart;
        SensorBus.OnCanvasPressEnd   -= OnClickLogCanvasPressEnd;
        SensorBus.OnCanvasDrag       -= OnClickLogCanvasDrag;
    }

    // ── 事件處理（前綴 OnClickLog 避免與 SensorTestManager_Logger 的 OnLog* 衝突）────

    void OnClickLogTap(int col, int row)        => RecordClickLog(InteractionKind.Tap,        col, row, false);
    void OnClickLogPressStart(int col, int row) => RecordClickLog(InteractionKind.PressStart,  col, row, false);
    void OnClickLogPressEnd(int col, int row)   => RecordClickLog(InteractionKind.PressEnd,    col, row, false);
    void OnClickLogDrag(int col, int row)       => RecordClickLog(InteractionKind.Drag,        col, row, false);

    void OnClickLogCanvasTap(float x, float y, float cw, float ch)
        => RecordClickLog(InteractionKind.Tap,        Mathf.RoundToInt(x), Mathf.RoundToInt(y), true);
    void OnClickLogCanvasPressStart(float x, float y, float cw, float ch)
        => RecordClickLog(InteractionKind.PressStart,  Mathf.RoundToInt(x), Mathf.RoundToInt(y), true);
    void OnClickLogCanvasPressEnd(float x, float y, float cw, float ch)
        => RecordClickLog(InteractionKind.PressEnd,    Mathf.RoundToInt(x), Mathf.RoundToInt(y), true);
    void OnClickLogCanvasDrag(float x, float y, float cw, float ch)
        => RecordClickLog(InteractionKind.Drag,        Mathf.RoundToInt(x), Mathf.RoundToInt(y), true);

    void RecordClickLog(InteractionKind kind, int a, int b, bool isCanvas)
    {
        float now = Time.time;
        _logHitQueue.Enqueue(now);

        var key = (kind, a, b);
        if (_logActiveWindow.TryGetValue(key, out var entry))
        {
            if (now - entry.lastTime <= clickLogCollapseWindow)
            {
                entry.count++;
                entry.lastTime = now;
                _logActiveWindow[key] = entry;
                return;
            }
            FlushClickLogEntry(entry);
        }

        _logActiveWindow[key] = new ClickLogEntry
        {
            kind      = kind,
            a         = a,
            b         = b,
            isCanvas  = isCanvas,
            count     = 1,
            firstTime = now,
            lastTime  = now,
        };
    }

    // ── 每幀更新（由 Update 呼叫）────────────────────────────────────────────

    void UpdateClickLogPanel()
    {
        if (_clickLogBodyText == null && _clickLogHpsText == null) return;

        float now         = Time.time;
        bool  bodyChanged = false;

        _expiredLogKeys.Clear();
        foreach (var kv in _logActiveWindow)
            if (now - kv.Value.lastTime > clickLogCollapseWindow)
                _expiredLogKeys.Add(kv.Key);
        foreach (var k in _expiredLogKeys)
        {
            FlushClickLogEntry(_logActiveWindow[k]);
            _logActiveWindow.Remove(k);
            bodyChanged = true;
        }

        while (_logHitQueue.Count > 0 && now - _logHitQueue.Peek() > 1f)
            _logHitQueue.Dequeue();

        int hps = _logHitQueue.Count;
        if (_clickLogHpsText != null && hps != _lastLogHps)
        {
            _lastLogHps = hps;
            _clickLogHpsText.text = $"{hps} /s";
        }

        bool hasActive = _logActiveWindow.Count > 0;
        if (_clickLogBodyText != null && (hasActive || bodyChanged))
        {
            _clickLogSB.Clear();
            int shown = 0;

            foreach (var kv in _logActiveWindow)
            {
                if (shown >= clickLogMaxLines) break;
                var e = kv.Value;
                string coord = e.isCanvas ? $"px({e.a},{e.b})" : $"欄{e.a,3} 列{e.b,2}";
                _clickLogSB.Append($"▶ {ClickLogLabel(e.kind)}  {coord}  ···\n");
                shown++;
            }

            foreach (var line in _logFlushedLines)
            {
                if (shown >= clickLogMaxLines) break;
                _clickLogSB.AppendLine(line);
                shown++;
            }

            _clickLogBodyText.text = _clickLogSB.ToString();
        }
    }

    // ── 內部工具 ──────────────────────────────────────────────────────────────

    void FlushClickLogEntry(ClickLogEntry e)
    {
        string coord = e.isCanvas ? $"px({e.a},{e.b})" : $"欄{e.a,3} 列{e.b,2}";
        string line  = e.count == 1
            ? $"[{e.firstTime:F1}s]  {ClickLogLabel(e.kind)}  {coord}"
            : $"[{e.firstTime:F1}s]  {ClickLogLabel(e.kind)}  {coord}  ×{e.count}";

        _logFlushedLines.Insert(0, line);

        int cap = clickLogMaxLines * 2;
        if (_logFlushedLines.Count > cap)
            _logFlushedLines.RemoveAt(_logFlushedLines.Count - 1);
    }

    static string ClickLogLabel(InteractionKind kind)
    {
        switch (kind)
        {
            case InteractionKind.Tap:        return "Tap    ";
            case InteractionKind.PressStart: return "Press▼";
            case InteractionKind.PressEnd:   return "Press▲";
            case InteractionKind.Drag:       return "Drag   ";
            default:                         return "?      ";
        }
    }
}
