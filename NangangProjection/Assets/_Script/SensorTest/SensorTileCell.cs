using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 次螢幕 (2048x768) 上的感應晶片格子。
/// 每格 16x32 px，只有左側與上側有邊線，觸摸後亮紅 1.5 秒。
/// 由 SensorTestManager 在執行期動態建立並管理。
/// </summary>
[RequireComponent(typeof(Image))]
public class SensorTileCell : MonoBehaviour
{
    [HideInInspector] public int col;
    [HideInInspector] public int row;
    [HideInInspector] public int index; // row * 128 + col

    Image     _fill;
    Coroutine _activeRoutine;

    static readonly Color ColBorder = new Color(0.28f, 0.28f, 0.28f, 1f);
    static readonly Color ColIdle   = new Color(0.06f, 0.06f, 0.08f, 1f);
    static readonly Color ColActive = new Color(0.90f, 0.10f, 0.10f, 1f);
    static readonly Color ColLocked = new Color(0.85f, 0.72f, 0.00f, 1f);

    bool _locked;

    void Awake()
    {
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
        _fill.color         = ColIdle;
        _fill.raycastTarget = false;
    }

    /// <summary>由 SensorTestManager 呼叫，設定鎖定狀態。</summary>
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
        _fill.color = ColIdle;  // 鎖定只阻止觸發，不在遊戲畫面顯示
    }
}
