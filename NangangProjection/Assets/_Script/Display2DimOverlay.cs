using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Display 2 調暗疊加層。
/// 在 Display 2 最上層渲染一個純黑 Image，透過 dimAlpha 控制不透明度（0=不調暗，1=全黑）。
///
/// Canvas 沒有 GraphicRaycaster，Image.raycastTarget = false，
/// 對所有 Windows 點擊事件（PostMessage / SendInput）完全無影響。
/// </summary>
public class Display2DimOverlay : MonoBehaviour
{
    public static Display2DimOverlay Instance { get; private set; }

    [Header("▶ 地板畫面調暗 ─ 調整地板投影亮度（0=不調暗，1=全黑）。停用此元件 → 無法從這裡調整地板亮度")]
    [Range(0f, 1f)]
    public float dimAlpha = 0f;

    private Image _dimImage;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        BuildOverlay();
    }

    private void Update()
    {
        if (_dimImage == null) return;
        var c = _dimImage.color;
        if (c.a == dimAlpha) return;
        c.a = dimAlpha;
        _dimImage.color = c;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildOverlay()
    {
        var canvasGO = new GameObject("Canvas_DimOverlay");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = 1;
        canvas.sortingOrder  = 999;
        // GraphicRaycaster 刻意不加，確保 Canvas 不攔截任何 Unity UI 事件

        var imgGO = new GameObject("DimImage");
        imgGO.layer = 5;
        imgGO.transform.SetParent(canvasGO.transform, false);

        var rt        = imgGO.AddComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;

        _dimImage               = imgGO.AddComponent<Image>();
        _dimImage.color         = new Color(0f, 0f, 0f, 0f);
        _dimImage.raycastTarget = false;
    }
}
