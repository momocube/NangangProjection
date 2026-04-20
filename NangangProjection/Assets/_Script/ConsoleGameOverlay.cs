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
        // ★ 與 controlGridParent 同層（掛在同一個父節點下），才能精確對齊晶片格範圍
        go.transform.SetParent(controlGridParent.parent, false);

        var overlayRT = go.AddComponent<RectTransform>();
        // 初始使用 top-left anchor + center pivot；
        // 實際位置／大小／旋轉由 SensorTestManager 呼叫 UpdateOverlayTransform() 設定
        overlayRT.anchorMin        = new Vector2(0f, 1f);
        overlayRT.anchorMax        = new Vector2(0f, 1f);
        overlayRT.pivot            = new Vector2(0.5f, 0.5f);
        overlayRT.sizeDelta        = Vector2.zero;
        overlayRT.anchoredPosition = Vector2.zero;

        _overlayImage               = go.AddComponent<RawImage>();
        _overlayImage.color         = new Color(1f, 1f, 1f, 0f); // 初始透明
        _overlayImage.raycastTarget = false;
        _overlayImage.texture       = GameDisplayRT.Current;

        // ★ 插在控制格「前面」（較低的 sibling index = 渲染在後 = 晶片格蓋在上面）
        int gridIdx = controlGridParent.GetSiblingIndex();
        go.transform.SetSiblingIndex(gridIdx); // 控制格自動後移一位

        Debug.Log($"[ConsoleGameOverlay] 疊加層建立，index={go.transform.GetSiblingIndex()}，晶片格 index={controlGridParent.GetSiblingIndex()}");
    }

    /// <summary>
    /// 由 SensorTestManager 呼叫，同步疊加層的位置、大小與旋轉，使 RT 與控制格範圍對齊。
    ///
    /// 不旋轉時：RawImage 覆蓋 (offsetX, offsetY) 起算 displayW×displayH 的矩形。
    /// 旋轉 90° CW 時：RT 本身為直向（canvasW×canvasH），RawImage 旋轉 -90° 後視覺變橫向
    ///   （displayW = canvasH×scale，displayH = canvasW×scale），與控制格排列完全對齊。
    /// </summary>
    public void UpdateOverlayTransform(
        float offsetX, float offsetY,
        float displayW, float displayH,
        bool rotate90, int canvasW, int canvasH, float scale)
    {
        if (_overlayImage == null) return;
        var rt = _overlayImage.rectTransform;

        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);

        if (!rotate90)
        {
            rt.localEulerAngles = Vector3.zero;
            rt.sizeDelta        = new Vector2(displayW, displayH);
        }
        else
        {
            // RT 是直向（canvasW×canvasH px）。
            // 將 RawImage 旋轉 -90°（CW）後，視覺寬 = canvasH*scale = displayW，高 = canvasW*scale = displayH。
            rt.localEulerAngles = new Vector3(0f, 0f, -90f);
            rt.sizeDelta        = new Vector2(canvasW * scale, canvasH * scale); // 旋轉前的直向尺寸
        }

        // 疊加中心 = 顯示區域中心（在 controlGridParent.parent 的 top-left 原點座標系內）
        rt.anchoredPosition = new Vector2(
            offsetX + displayW * 0.5f,
          -(offsetY + displayH * 0.5f)
        );
    }
}
