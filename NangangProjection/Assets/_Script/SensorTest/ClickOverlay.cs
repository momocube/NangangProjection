using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 遊戲畫面（Display 2）透明點擊疊加層。
/// 佔滿整個 Canvas_Secondary，捕捉滑鼠點擊後換算成格子座標，
/// 透過 SensorTestManager.TriggerCell 走相同的鎖定邏輯。
///
/// 由 SensorTestManager.BuildClickOverlay() 在執行期動態建立。
/// </summary>
[RequireComponent(typeof(UnityEngine.UI.Image))]
public class ClickOverlay : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public SensorTestManager manager;

    /// <summary>兩次點擊之間的最短間隔（秒）。</summary>
    [HideInInspector] public float clickCooldown = 1f;

    [HideInInspector] public int cols = 128;
    [HideInInspector] public int rows = 24;

    float         _lastClickTime = float.NegativeInfinity;
    RectTransform _rt;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 冷卻檢查
        if (Time.time - _lastClickTime < clickCooldown) return;

        // 將螢幕座標轉換為 RectTransform 本地座標
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rt, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        // 正規化 (0~1)，Y 軸翻轉（Canvas 下→上，格子 上→下）
        Rect r  = _rt.rect;
        float nx = (local.x - r.xMin) / r.width;
        float ny = 1f - (local.y - r.yMin) / r.height;

        int col = Mathf.Clamp(Mathf.FloorToInt(nx * cols), 0, cols - 1);
        int row = Mathf.Clamp(Mathf.FloorToInt(ny * rows), 0, rows - 1);

        _lastClickTime = Time.time;
        manager?.TriggerCell(col, row);

        Debug.Log($"[ClickOverlay] 點擊 ({col},{row})  cooldown={clickCooldown}s");
    }
}
