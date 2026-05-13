using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 掛在 Display 1 ControlTex RawImage 上，接收滑鼠事件並轉換為格子事件。
/// hitTest、onXxx 由 SensorTestManager 注入。
/// </summary>
public class ControlGridPointerHandler : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IPointerMoveHandler, IPointerEnterHandler, IPointerExitHandler
{
    // ── 由 SensorTestManager 注入 ────────────────────────────────────────────

    /// <summary>
    /// 轉換 controlGridParent 的 local 座標 → (faceIdx, cellIdx)；
    /// 找不到時回 (-1, -1)。
    /// </summary>
    public System.Func<Vector2, (int fi, int ci)> hitTest;

    public System.Action<int, int>        onLeftDown;
    public System.Action<int, int>        onLeftUp;
    public System.Action<int, int>        onHoldTick;
    public System.Action<int, int, bool>  onRightDown;  // (fi, ci, isCtrl)

    /// <summary>hover 格子改變時通知 (fi, ci)；離開時 fi=-1, ci=-1。</summary>
    public System.Action<int, int>        onHoverChanged;

    // ── 引用 ─────────────────────────────────────────────────────────────────

    /// <summary>由 SensorTestManager 在 BuildControlDisplay 後設定，用於座標轉換。</summary>
    public RectTransform controlGridParent;

    // ── 內部 ─────────────────────────────────────────────────────────────────

    const float HoldInterval = 0.05f;

    int _hoverFace = -1, _hoverCell = -1;
    int _pressFace = -1, _pressCell = -1;
    Coroutine _holdRoutine;

    // ── 指標事件 ─────────────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData e)
    {
        var (fi, ci) = GetCell(e);
        if (fi < 0) return;

        if (e.button == PointerEventData.InputButton.Right)
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            onRightDown?.Invoke(fi, ci, ctrl);
            return;
        }

        if (e.button == PointerEventData.InputButton.Left)
        {
            _pressFace = fi;
            _pressCell = ci;
            onLeftDown?.Invoke(fi, ci);
            if (_holdRoutine != null) StopCoroutine(_holdRoutine);
            _holdRoutine = StartCoroutine(HoldRoutine(fi, ci));
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        if (_holdRoutine != null) { StopCoroutine(_holdRoutine); _holdRoutine = null; }
        if (_pressFace >= 0)
        {
            onLeftUp?.Invoke(_pressFace, _pressCell);
            _pressFace = _pressCell = -1;
        }
    }

    public void OnPointerMove(PointerEventData e)
    {
        var (fi, ci) = GetCell(e);
        UpdateHover(fi, ci);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        var (fi, ci) = GetCell(e);
        UpdateHover(fi, ci);
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (_holdRoutine != null)
        {
            StopCoroutine(_holdRoutine);
            _holdRoutine = null;
            if (_pressFace >= 0)
            {
                onLeftUp?.Invoke(_pressFace, _pressCell);
                _pressFace = _pressCell = -1;
            }
        }
        UpdateHover(-1, -1);
    }

    // ── 工具 ─────────────────────────────────────────────────────────────────

    void UpdateHover(int fi, int ci)
    {
        if (fi == _hoverFace && ci == _hoverCell) return;
        _hoverFace = fi;
        _hoverCell = ci;
        onHoverChanged?.Invoke(fi, ci);
    }

    (int fi, int ci) GetCell(PointerEventData e)
    {
        if (hitTest == null) return (-1, -1);
        var parent = controlGridParent;
        if (parent == null) return (-1, -1);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent, e.position, e.pressEventCamera, out Vector2 local);
        return hitTest(local);
    }

    IEnumerator HoldRoutine(int fi, int ci)
    {
        while (true)
        {
            yield return new WaitForSeconds(HoldInterval);
            onHoldTick?.Invoke(fi, ci);
        }
    }
}
