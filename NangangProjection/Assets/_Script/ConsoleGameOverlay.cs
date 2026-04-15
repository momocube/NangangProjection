using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主控台畫面疊加（Display 1 格子後方）。
/// 掛在 sensor_test 場景的 SensorTestManager 物件上。
///
/// 層級（由下到上渲染）：
///   PrimaryBG → 【RT 疊加 RawImage】 → 控制格（idle 透明，可透視）→ Header/ButtonBar/StatusBar
///
/// RT 優先順序：
///   1. GameDisplayRT.Current  → 遊戲場景畫面（遊戲載入時）
///   2. SensorGridRT.Current   → 感應格即時預覽（僅感應測試畫面時）
///   3. null                   → 不顯示疊加（疊加 alpha = 0）
///
/// 遊戲載入時自動隱藏 Display 2 的感應晶片格（讓遊戲畫面乾淨）。
/// </summary>
public class ConsoleGameOverlay : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("主控台 Canvas 內的控制格容器（controlGridParent）")]
    public RectTransform controlGridParent;

    [Header("疊加顯示設定")]
    [Range(0f, 1f)]
    [Tooltip("RT 疊加的不透明度（控制格 idle 已透明，建議設 1.0 全顯示）")]
    public float overlayAlpha = 1.0f;

    // 內部狀態
    private RawImage      _overlayImage;
    private RenderTexture _lastRT;
    private SensorTestManager _stm;

    private void Start()
    {
        _stm = GetComponent<SensorTestManager>();
        BuildOverlayImage();
    }

    private void Update()
    {
        // 優先使用遊戲 RT，其次使用感應格 RT
        RenderTexture rt = GameDisplayRT.Current ?? SensorGridRT.Current;

        // RT 變化時（遊戲載入 / 卸載）同步疊加層與 Canvas_Secondary
        if (rt != _lastRT)
        {
            _lastRT = rt;

            if (_overlayImage != null)
                _overlayImage.texture = rt;

            // 遊戲載入 → 自動隱藏 Display 2 感應格（避免蓋住遊戲畫面）
            // 遊戲卸載 → 恢復顯示感應格
            bool gameLoaded = GameDisplayRT.Current != null;
            _stm?.SetGameTileVisible(!gameLoaded);
        }

        if (_overlayImage != null)
        {
            var c = _overlayImage.color;
            c.a = (rt != null) ? overlayAlpha : 0f;
            _overlayImage.color = c;
        }
    }

    private void BuildOverlayImage()
    {
        if (controlGridParent == null)
        {
            Debug.LogWarning("[ConsoleGameOverlay] controlGridParent 未設定");
            return;
        }

        Canvas canvas = controlGridParent.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("GameDisplayOverlay");
        go.layer = 5;
        go.transform.SetParent(canvas.GetComponent<RectTransform>(), false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        _overlayImage               = go.AddComponent<RawImage>();
        _overlayImage.color         = new Color(1f, 1f, 1f, 0f); // 初始透明
        _overlayImage.raycastTarget = false;
        _overlayImage.texture       = GameDisplayRT.Current;

        // ★ 插在控制格「前面」（較低的 sibling index = 渲染在後 = 晶片格蓋在上面）
        int gridIdx = controlGridParent.GetSiblingIndex();
        go.transform.SetSiblingIndex(gridIdx); // 控制格自動後移一位

        Debug.Log($"[ConsoleGameOverlay] 疊加層建立，index={go.transform.GetSiblingIndex()}，晶片格 index={controlGridParent.GetSiblingIndex()}");
    }
}
