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
    Coroutine _activeRoutine;
    Coroutine _holdRoutine;      // 按住連續觸發
    bool      _locked;
    bool      _hovered;

    /// <summary>按住時的觸發間隔（秒），模擬感應器連續送封包的頻率。</summary>
    const float HoldInterval = 0.05f; // 20 次/秒

    // 格子顏色
    static readonly Color ColIdle    = new Color(0f,    0f,    0f,    0f);      // 完全透明
    static readonly Color ColActive  = new Color(0.90f, 0.10f, 0.10f, 0.90f);  // 紅色（90% 不透）
    static readonly Color ColLocked  = new Color(0.85f, 0.72f, 0.00f, 0.90f);  // 黃色（90% 不透）
    static readonly Color ColHover   = new Color(0.20f, 0.22f, 0.30f, 0.40f);  // 半透明藍色懸停
    static readonly Color ColPress   = new Color(1.00f, 0.55f, 0.00f, 0.90f);  // 橘色常亮（按壓中）
    static readonly Color ColPending = new Color(0.55f, 0.55f, 0.60f, 0.50f);  // 灰白（孤點等待中）

    bool _isPressing;
    bool _isPending;

    /// <summary>SensorTestManager 指定：右鍵點擊時通知 (col, row)。</summary>
    public System.Action<int, int> onRightClick;

    /// <summary>SensorTestManager 指定：滑鼠進入時通知 (col, row, index, isLocked)。</summary>
    public System.Action<int, int, int, bool> onHover;

    /// <summary>SensorTestManager 指定：左鍵點擊時通知 (col, row)。</summary>
    public System.Action<int, int> onLeftClick;

    /// <summary>SensorTestManager 指定：滑鼠按下時立即通知（PressStart 語意）。</summary>
    public System.Action<int, int> onPressStart;

    /// <summary>SensorTestManager 指定：滑鼠放開時通知（PressEnd 語意）。</summary>
    public System.Action<int, int> onPressEnd;

    /// <summary>SensorTestManager 指定：按住期間每 HoldInterval 秒通知（VFX keep-alive，無視覺效果）。</summary>
    public System.Action<int, int> onHoldTick;

    void Awake()
    {
        var root = GetComponent<Image>();
        root.color = Color.clear;

        var fillGO = new GameObject("Fill");
        fillGO.layer = gameObject.layer;
        fillGO.transform.SetParent(transform, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        _fill               = fillGO.AddComponent<Image>();
        _fill.color         = ColIdle;
        _fill.raycastTarget = false;
    }

    /// <summary>建立後立即呼叫，設定格子座標與標籤數字。stride = 每列格數（欄數）。</summary>
    public void Init(int c, int r, int stride = 128)
    {
        col   = c;
        row   = r;
        index = r * stride + c;
    }

    /// <summary>由 SensorTestManager 呼叫，切換鎖定狀態。</summary>
    public void SetLocked(bool locked)
    {
        _locked     = locked;
        _isPressing = false;
        _isPending  = false;
        if (_locked && _activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
            _activeRoutine = null;
        }
        RefreshColor();
    }

    /// <summary>由 SensorTestManager 呼叫，設定孤點等待（灰白）狀態。</summary>
    public void SetPendingState(bool pending)
    {
        _isPending = pending;
        if (pending)
        {
            if (_activeRoutine != null) { StopCoroutine(_activeRoutine); _activeRoutine = null; }
            _isPressing = false;
        }
        RefreshColor();
    }

    /// <summary>由 SensorTestManager 呼叫，觸發亮紅（持續 SensorTileCell.ActiveDuration 秒）。按壓中不蓋橘燈。</summary>
    public void Activate()
    {
        if (_locked)     return;
        if (_isPressing) return;  // 按壓中不蓋橘燈
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(ActiveRoutine());
    }

    /// <summary>由 SensorTestManager 呼叫，設定按壓橘燈（true = 亮橘，false = 熄滅）。</summary>
    public void SetPressState(bool pressing)
    {
        if (_locked) return;
        _isPressing = pressing;
        if (pressing)
        {
            if (_activeRoutine != null) { StopCoroutine(_activeRoutine); _activeRoutine = null; }
            if (_fill != null) _fill.color = ColPress;
        }
        else
        {
            RefreshColor();
        }
    }

    IEnumerator ActiveRoutine()
    {
        _fill.color = ColActive;
        yield return new WaitForSeconds(SensorTileCell.ActiveDuration);
        _activeRoutine = null;
        RefreshColor();
    }

    void RefreshColor()
    {
        if (_fill == null) return;
        if (_locked)                { _fill.color = ColLocked;  return; }
        if (_isPending)             { _fill.color = ColPending; return; }
        if (_isPressing)            { _fill.color = ColPress;   return; }
        if (_activeRoutine != null) { _fill.color = ColActive;  return; }
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
            SetPressState(true);              // 立即橘燈（_isPressing=true → Activate() 跳過紅閃）
            onLeftClick?.Invoke(col, row);    // 建立 VFX slot（首次 Touch）
            onPressStart?.Invoke(col, row);   // 標記 _pressActive，防止 slot 逾時
            if (_holdRoutine != null) StopCoroutine(_holdRoutine);
            _holdRoutine = StartCoroutine(HoldRoutine());
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (_holdRoutine != null) { StopCoroutine(_holdRoutine); _holdRoutine = null; }
            SetPressState(false);
            onPressEnd?.Invoke(col, row);
        }
    }

    IEnumerator HoldRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(HoldInterval);
            onHoldTick?.Invoke(col, row);   // VFX keep-alive，不重覆觸發紅閃
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
        if (_holdRoutine != null)
        {
            StopCoroutine(_holdRoutine);
            _holdRoutine = null;
            if (_isPressing)
            {
                SetPressState(false);
                onPressEnd?.Invoke(col, row);   // 滑鼠拖離時也結束按壓，避免懸掛
            }
        }
        if (!_locked && _activeRoutine == null && !_isPressing) _fill.color = ColIdle;
    }
}
