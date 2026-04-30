using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Display 1 即時點擊記錄面板（partial）。
/// 訂閱 SensorBus.OnTouch，以 Collapse 方式顯示最近幾筆觸碰事件，
/// 並用 1 秒滑動窗口計算實際觸發頻率（次/秒）。
/// </summary>
public partial class SensorTestManager
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("點擊記錄面板")]
    [Tooltip("取消勾選可隱藏面板（不影響其他功能）")]
    public bool showClickLogPanel = true;

    [Tooltip("面板顯示幾筆記錄")]
    [Range(3, 10)]
    public int clickLogMaxLines = 6;

    [Tooltip("相同座標在此秒數內算同一筆（Collapse）")]
    public float clickLogCollapseWindow = 0.3f;

    // ── 內部狀態 ──────────────────────────────────────────────────────────────

    struct ClickLogEntry
    {
        public int   col, row, count;
        public float firstTime, lastTime;
    }

    // key=(col,row)，存正在 collapse 中的觸碰
    readonly Dictionary<(int, int), ClickLogEntry> _logActiveWindow
        = new Dictionary<(int, int), ClickLogEntry>();

    // 已 flush 的行（最新在前）
    readonly List<string> _logFlushedLines = new List<string>();

    // 全局 1 秒滑動窗口，用來算 HPS
    readonly Queue<float> _logHitQueue = new Queue<float>();

    TextMeshProUGUI _clickLogBodyText;
    TextMeshProUGUI _clickLogHpsText;

    // ── 建立面板 ──────────────────────────────────────────────────────────────

    void BuildClickLogPanel(RectTransform root, TMP_FontAsset font)
    {
        if (!showClickLogPanel) return;

        const float titleH = 22f;
        const float lineH  = 16f;
        float bodyH  = clickLogMaxLines * lineH;
        float totalH = titleH + bodyH + 6f;

        // 半透明浮動面板，Header 正下方左側
        var panel = CreatePanel(root, "ClickLogPanel",
            new Color(0.02f, 0.03f, 0.07f, 0.88f),
            new Vector2(0f, 1f), new Vector2(0.44f, 1f),
            new Vector2(2f,  -(58f + 2f + totalH)),
            new Vector2(-2f, -(58f + 2f)));

        // 標題列
        var titleBar = CreatePanel(panel, "TitleBar",
            new Color(0.07f, 0.10f, 0.18f),
            new Vector2(0f, 1f), Vector2.one,
            new Vector2(0f, -titleH), Vector2.zero);
        titleBar.GetComponent<Image>().raycastTarget = false;

        var titleTxt = AddText(titleBar.transform, "Title", "點擊記錄",
            11, TextAlignmentOptions.MidlineLeft,
            new Color(0.55f, 0.65f, 0.85f), font);
        (titleTxt.transform as RectTransform).offsetMin = new Vector2(6f, 0f);

        _clickLogHpsText = AddText(titleBar.transform, "Hps", "—",
            11, TextAlignmentOptions.MidlineRight,
            new Color(0.55f, 0.90f, 0.60f), font);
        (_clickLogHpsText.transform as RectTransform).offsetMax = new Vector2(-6f, 0f);

        // 記錄內文區
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

        SensorBus.OnTouch += OnClickLogTouch;
    }

    // ── 觸碰接收 ──────────────────────────────────────────────────────────────

    void OnClickLogTouch(int col, int row)
    {
        float now = Time.time;
        _logHitQueue.Enqueue(now);

        var key = (col, row);

        if (_logActiveWindow.TryGetValue(key, out var entry))
        {
            if (now - entry.lastTime <= clickLogCollapseWindow)
            {
                // 合併：同座標同窗口，只累計次數
                entry.count++;
                entry.lastTime = now;
                _logActiveWindow[key] = entry;
                return;
            }
            // 窗口已過期 → 先 flush 舊的
            FlushLogEntry(entry);
        }

        // 建立新條目
        _logActiveWindow[key] = new ClickLogEntry
        {
            col = col, row = row, count = 1,
            firstTime = now, lastTime = now
        };
    }

    // ── 每幀更新（由 Update 呼叫）────────────────────────────────────────────

    void UpdateClickLogPanel()
    {
        if (_clickLogBodyText == null && _clickLogHpsText == null) return;

        float now = Time.time;

        // 將超過 collapseWindow 的 active 條目 flush
        var expired = new List<(int, int)>();
        foreach (var kv in _logActiveWindow)
            if (now - kv.Value.lastTime > clickLogCollapseWindow)
                expired.Add(kv.Key);
        foreach (var k in expired)
        {
            FlushLogEntry(_logActiveWindow[k]);
            _logActiveWindow.Remove(k);
        }

        // HPS：清除 1 秒前的時間戳
        while (_logHitQueue.Count > 0 && now - _logHitQueue.Peek() > 1f)
            _logHitQueue.Dequeue();

        if (_clickLogHpsText != null)
            _clickLogHpsText.text = $"{_logHitQueue.Count} /s";

        // 組合顯示文字
        if (_clickLogBodyText != null)
        {
            var sb = new StringBuilder();
            int shown = 0;

            // Active（正在 collapse）→ 顯示即時頻率
            foreach (var kv in _logActiveWindow)
            {
                if (shown >= clickLogMaxLines) break;
                var e = kv.Value;
                if (e.count > 1)
                {
                    float dur    = now - e.firstTime;
                    float msEach = dur > 0.001f ? dur * 1000f / e.count : 0f;
                    sb.Append($"▶ 欄{e.col,3} 列{e.row,2}  {msEach:F0}ms/次···\n");
                }
                else
                {
                    sb.Append($"▶ 欄{e.col,3} 列{e.row,2}  ···\n");
                }
                shown++;
            }

            // Flushed（已完成）→ 最新在前
            foreach (var line in _logFlushedLines)
            {
                if (shown >= clickLogMaxLines) break;
                sb.AppendLine(line);
                shown++;
            }

            _clickLogBodyText.text = sb.ToString();
        }
    }

    // ── 清除 ──────────────────────────────────────────────────────────────────

    void DestroyClickLog()
    {
        SensorBus.OnTouch -= OnClickLogTouch;
    }

    // ── 內部 ──────────────────────────────────────────────────────────────────

    void FlushLogEntry(ClickLogEntry e)
    {
        string line;
        if (e.count == 1)
        {
            line = $"[{e.firstTime:F1}s]  欄{e.col,3} 列{e.row,2}";
        }
        else
        {
            float dur  = e.lastTime - e.firstTime;
            float msEach = dur > 0.001f ? dur * 1000f / e.count : 0f;
            line = $"[{e.firstTime:F1}s]  欄{e.col,3} 列{e.row,2}  ×{e.count}  avg {msEach:F0}ms/次";
        }

        _logFlushedLines.Insert(0, line);

        // 保留最多 maxLines × 2 筆（渲染只顯示 maxLines）
        int cap = clickLogMaxLines * 2;
        if (_logFlushedLines.Count > cap)
            _logFlushedLines.RemoveAt(_logFlushedLines.Count - 1);
    }
}
