using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 次螢幕 (2048x768) 上的感應晶片格子。
/// 每格 16x32 px，只有左側與上側有邊線，觸摸後亮紅 ActiveDuration 秒。
/// 由 SensorTestManager 在執行期動態建立並管理。
/// </summary>
[RequireComponent(typeof(Image))]
public class SensorTileCell : MonoBehaviour
{
    /// <summary>亮紅持續秒數，由 SensorTestManager.activeDuration 統一設定。</summary>
    public static float ActiveDuration = 1.5f;

    [HideInInspector] public int col;
    [HideInInspector] public int row;
    [HideInInspector] public int index; // row * 128 + col

    Image     _fill;
    Coroutine _activeRoutine;
    Color     _colIdleFace;   // per-face idle tint，由 SetFaceColor 設定

    static readonly Color ColBorder  = new Color(0.28f, 0.28f, 0.28f, 1f);
    static readonly Color ColIdle    = new Color(0.06f, 0.06f, 0.08f, 1f);
    static readonly Color ColActive  = new Color(0.90f, 0.10f, 0.10f, 1f);
    static readonly Color ColLocked  = new Color(0.85f, 0.72f, 0.00f, 1f);
    static readonly Color ColPress   = new Color(1.00f, 0.55f, 0.00f, 1f);  // 橘色常亮（按壓中）
    static readonly Color ColPending = new Color(0.55f, 0.55f, 0.60f, 1f);  // 灰白（孤點等待中）

    bool _locked;
    bool _isPressing;
    bool _isPending;

    void Awake()
    {
        _colIdleFace = ColIdle;

        // 根 Image = 邊框顏色（左側 + 上側各 1 px 會外露）
        var border = GetComponent<Image>();
        border.color          = ColBorder;
        border.raycastTarget  = false;

        // 內層填色，往左 1 px、往上 1 px 縮排 → 僅左側與上側顯示邊線
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(transform, false);
        var rt = fillGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(1f,  0f);   // 左側 1 px
        rt.offsetMax = new Vector2(0f, -1f);   // 上側 1 px
        _fill = fillGO.AddComponent<Image>();
        _fill.color         = _colIdleFace;
        _fill.raycastTarget = false;
    }

    /// <summary>由 SensorTestManager 在建格時呼叫，以 gizmoColor 衍生出深色底色區分各感應面。</summary>
    public void SetFaceColor(Color faceColor)
    {
        _colIdleFace = new Color(faceColor.r * 0.20f, faceColor.g * 0.20f, faceColor.b * 0.20f, 1f);
        if (_fill != null) RefreshColor();
    }

    /// <summary>由 SensorTestManager 呼叫，設定鎖定狀態。</summary>
    public void SetLocked(bool locked)
    {
        _locked     = locked;
        _isPressing = false;
        if (_locked && _activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
            _activeRoutine = null;
        }
        RefreshColor();
    }

    /// <summary>Canvas 重新顯示時強制還原 idle 狀態（中斷的 Coroutine 不會自動還原顏色）。</summary>
    public void ResetToIdle()
    {
        _isPressing = false;
        _isPending  = false;
        if (_activeRoutine != null) { StopCoroutine(_activeRoutine); _activeRoutine = null; }
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

    /// <summary>由 SensorTestManager 呼叫，觸發亮紅（持續 ActiveDuration 秒）。按壓中不蓋橘燈。</summary>
    public void Activate()
    {
        if (_locked)     return;
        if (_isPressing) return;  // 按壓中不蓋橘燈
        if (!gameObject.activeInHierarchy) return;
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
        yield return new WaitForSeconds(ActiveDuration);
        _activeRoutine = null;
        RefreshColor();
    }

    void RefreshColor()
    {
        if (_fill == null) return;
        if (_locked)     { _fill.color = ColLocked;  return; }
        if (_isPending)  { _fill.color = ColPending; return; }
        if (_isPressing) { _fill.color = ColPress;   return; }
        _fill.color = _colIdleFace;
    }
}
