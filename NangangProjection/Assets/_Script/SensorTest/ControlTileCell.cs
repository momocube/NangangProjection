using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 主螢幕 (1920x1080) 控制格子。
/// 鏡像對應次螢幕的 SensorTileCell；右鍵點擊可鎖定 / 解鎖。
/// 滑鼠懸停時顯示格子編號於狀態列。
/// </summary>
[RequireComponent(typeof(Image))]
public class ControlTileCell : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public int col;
    [HideInInspector] public int row;
    [HideInInspector] public int index; // row * 128 + col

    Image     _fill;
    Text      _label;
    Coroutine _activeRoutine;
    Coroutine _holdRoutine;      // 按住連續觸發
    bool      _locked;
    bool      _hovered;

    /// <summary>按住時的觸發間隔（秒），模擬感應器連續送封包的頻率。</summary>
    const float HoldInterval = 0.05f; // 20 次/秒

    // 格子顏色（idle/border 透明，讓後方遊戲畫面 / 感應格 RT 透出）
    static readonly Color ColBorder = new Color(0.35f, 0.35f, 0.40f, 0.20f);  // 極淡邊框
    static readonly Color ColIdle   = new Color(0f,    0f,    0f,    0f);      // 完全透明
    static readonly Color ColActive = new Color(0.90f, 0.10f, 0.10f, 0.90f);  // 紅色（90% 不透）
    static readonly Color ColLocked = new Color(0.85f, 0.72f, 0.00f, 0.90f);  // 黃色（90% 不透）
    static readonly Color ColHover  = new Color(0.20f, 0.22f, 0.30f, 0.40f);  // 半透明藍色懸停

    /// <summary>SensorTestManager 指定：右鍵點擊時通知 (col, row)。</summary>
    public System.Action<int, int> onRightClick;

    /// <summary>SensorTestManager 指定：滑鼠進入時通知 (col, row, index, isLocked)。</summary>
    public System.Action<int, int, int, bool> onHover;

    /// <summary>SensorTestManager 指定：左鍵點擊時通知 (col, row)。</summary>
    public System.Action<int, int> onLeftClick;

    void Awake()
    {
        // 根 Image = 邊框
        var border = GetComponent<Image>();
        border.color = ColBorder;

        // 內層填色
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(transform, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(1f,  0f);
        fillRT.offsetMax = new Vector2(0f, -1f);
        _fill = fillGO.AddComponent<Image>();
        _fill.color         = ColIdle;
        _fill.raycastTarget = false;

        // 格子編號標籤
        var labelGO = new GameObject("Num");
        labelGO.transform.SetParent(fillGO.transform, false);
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        _label = labelGO.AddComponent<Text>();
        _label.alignment          = TextAnchor.MiddleCenter;
        _label.fontSize           = 8;
        _label.resizeTextForBestFit = true;
        _label.resizeTextMinSize  = 1;
        _label.resizeTextMaxSize  = 8;
        _label.color              = new Color(0.40f, 0.40f, 0.45f);
        _label.raycastTarget      = false;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null) _label.font = font;
    }

    /// <summary>建立後立即呼叫，設定格子座標與標籤數字。stride = 每列格數（欄數）。</summary>
    public void Init(int c, int r, int stride = 128)
    {
        col   = c;
        row   = r;
        index = r * stride + c;
        if (_label != null) _label.text = (index + 1).ToString();
    }

    /// <summary>由 SensorTestManager 呼叫，切換鎖定狀態。</summary>
    public void SetLocked(bool locked)
    {
        _locked = locked;
        if (_locked && _activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
            _activeRoutine = null;
        }
        RefreshColor();
    }

    /// <summary>由 SensorTestManager 呼叫，觸發 1.5 秒亮紅。</summary>
    public void Activate()
    {
        if (_locked) return;
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(ActiveRoutine());
    }

    IEnumerator ActiveRoutine()
    {
        _fill.color = ColActive;
        yield return new WaitForSeconds(1.5f);
        _activeRoutine = null;
        RefreshColor();
    }

    void RefreshColor()
    {
        if (_fill == null) return;
        if (_locked)                { _fill.color = ColLocked; return; }
        if (_activeRoutine != null) { _fill.color = ColActive; return; }
        _fill.color = _hovered ? ColHover : ColIdle;
    }

    // ── 滑鼠事件 ─────────────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            onRightClick?.Invoke(col, row);
            return;
        }
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 立即觸發一次，再開始連續觸發（模擬感應器持續送封包）
            onLeftClick?.Invoke(col, row);
            if (_holdRoutine != null) StopCoroutine(_holdRoutine);
            _holdRoutine = StartCoroutine(HoldRoutine());
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (_holdRoutine != null) { StopCoroutine(_holdRoutine); _holdRoutine = null; }
        }
    }

    IEnumerator HoldRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(HoldInterval);
            onLeftClick?.Invoke(col, row);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        if (!_locked && _activeRoutine == null) _fill.color = ColHover;
        onHover?.Invoke(col, row, index, _locked);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        if (_holdRoutine != null) { StopCoroutine(_holdRoutine); _holdRoutine = null; }
        if (!_locked && _activeRoutine == null) _fill.color = ColIdle;
    }
}
