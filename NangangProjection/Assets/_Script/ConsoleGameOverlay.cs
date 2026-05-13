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
///   1. GameDisplayRT.Current         → 遊戲場景畫面（遊戲載入時）
///   2. TileGridTexture.SensorTex     → 感應格即時預覽（僅感應測試畫面時）
///   3. null                          → 不顯示疊加（疊加 alpha = 0）
///
/// 遊戲載入時自動讓感應格 idle 格子透明（讓遊戲畫面透出）。
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
    private Texture       _lastTex;
    private RenderTexture _lastGameRT;
    private SensorTestManager _stm;
    private float         _overlayDisplayW, _overlayDisplayH;

    // 旋轉狀態（由 UpdateOverlayTransform 同步）
    private bool _rotate90;

    // 格線
    private RawImage   _gridLineImage;
    private Texture2D  _gridLineTex;

    private void Start()
    {
        _stm = GetComponent<SensorTestManager>();
    }

    private void Update()
    {
        // 優先順序：
        //   1. GameDisplayRT      — 遊戲場景 Mirror Camera RT
        //   2. SensorTex          — 晶片示意 ON 時，TileGridTexture CPU 紋理
        //   3. Display2ScreenCapture — 晶片示意 OFF 時，GDI 桌面截圖（顯示 D2 桌面/瀏覽器）
        bool tilesVisible = _stm == null || _stm.GameTileVisible;
        RenderTexture gameRT = GameDisplayRT.Current;
        Texture tex = gameRT ?? (tilesVisible ? (Texture)_stm?.TileGrid?.SensorTex : null);

        // 追蹤遊戲 RT 載入/卸載：遊戲存在時讓感應格 idle 透明度跟隨 overlayAlpha
        if (gameRT != _lastGameRT)
        {
            _lastGameRT = gameRT;
            _stm?.RefreshSensorTileIdleColors();
        }

        if (tex == null && Display2ScreenCapture.Instance != null)
            tex = Display2ScreenCapture.Instance.CaptureTexture;

        if (tex != _lastTex)
        {
            _lastTex = tex;
            if (_overlayImage != null)
                _overlayImage.texture = tex;
        }

        if (_overlayImage != null)
        {
            var c = _overlayImage.color;
            c.a = (tex != null) ? overlayAlpha : 0f;
            _overlayImage.color = c;

            // 旋轉 -90° 後：u→(1-u)、v→(1-v) 才能和控制格位置對齊
            _overlayImage.uvRect = _rotate90
                ? new Rect(1f, 1f, -1f, -1f)
                : new Rect(0f, 0f,  1f,  1f);
        }
    }

    private void BuildOverlayImage()
    {
        if (_overlayImage != null) return; // 已存在則跳過
        if (controlGridParent == null)
        {
            Debug.LogWarning("[ConsoleGameOverlay] controlGridParent 未設定");
            return;
        }

        Canvas canvas = controlGridParent.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("GameDisplayOverlay");
        go.layer = 5;
        // ★ 掛在 controlGridParent 內部：座標系與格子相同，UpdateOverlayTransform 的
        //   offsetX/Y 就是在 controlGridParent 空間裡量的，不需要額外補 Header 偏移。
        go.transform.SetParent(controlGridParent, false);

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
        _overlayImage.texture       = null;

        // ★ 插在控制格所有子物件「最前面」（sibling index 0 = 渲染在最後 = 晶片格蓋在上面）
        go.transform.SetSiblingIndex(0);

        // 重置快取，讓 Update() 強制重新賦值 texture（_lastTex 殘留舊值會跳過賦值）
        _lastTex = null;

        Debug.Log($"[ConsoleGameOverlay] 疊加層建立於 controlGridParent 內部，index=0");
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
        _rotate90 = rotate90;
        BuildOverlayImage(); // 若被 BuildControlGrid 清除則重建
        if (_overlayImage == null) return;
        _overlayDisplayW = displayW;
        _overlayDisplayH = displayH;
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

    // ── 格線 Texture2D ────────────────────────────────────────────────────────

    /// <summary>
    /// 由 SensorTestManager 呼叫，在 Texture2D 上畫格線並貼至 GridLineOverlay。
    /// texW/texH = faceView 像素尺寸（canvas 座標 1:1），RawImage 負責縮放。
    /// </summary>
    public void UpdateGridLines(
        float offsetX, float offsetY,
        float displayW, float displayH,
        bool rotate90, int canvasW, int canvasH, float scale,
        float faceViewOriginX, float faceViewOriginY,
        int texW, int texH,
        SensorFaceConfig[] faces,
        int vcellSizePx = 0)
    {
        EnsureGridLineImage();

        // 位置與旋轉同 overlay
        var rt = _gridLineImage.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        if (!rotate90)
        {
            rt.localEulerAngles = Vector3.zero;
            rt.sizeDelta        = new Vector2(displayW, displayH);
        }
        else
        {
            rt.localEulerAngles = new Vector3(0f, 0f, -90f);
            rt.sizeDelta        = new Vector2(canvasW * scale, canvasH * scale);
        }
        rt.anchoredPosition = new Vector2(offsetX + displayW * 0.5f, -(offsetY + displayH * 0.5f));

        if (texW <= 0 || texH <= 0) return;

        // 重建 Texture2D（尺寸改變時）
        if (_gridLineTex == null || _gridLineTex.width != texW || _gridLineTex.height != texH)
        {
            if (_gridLineTex != null) Destroy(_gridLineTex);
            _gridLineTex            = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            _gridLineTex.filterMode = FilterMode.Bilinear; // Point 在 scale<1 時會跳像素
            _gridLineImage.texture  = _gridLineTex;
        }

        var pixels  = new Color32[texW * texH];
        var lineCol = new Color32(160, 165, 190, 110);

        if (faces != null)
        {
            int ox = Mathf.RoundToInt(faceViewOriginX);
            int oy = Mathf.RoundToInt(faceViewOriginY);

            foreach (var face in faces)
            {
                if (face == null) continue;
                int cw  = face.GizmoCellW;
                int ch  = face.GizmoCellH;
                int lox = face.originX - ox;
                int loy = face.originY - oy;

                // 垂直線（欄邊界），畫 2px 寬確保縮放後仍可見
                // rotate90: Texture_Y→Screen_X、Texture_X→Screen_-Y，故 ty=y（不翻轉）、tx=texW-rawX（翻轉X）
                for (int col = 0; col <= face.colCount; col++)
                {
                    int rawX = lox + col * cw;
                    if (rawX < 0 || rawX > texW) continue;
                    int y0 = Mathf.Clamp(loy,                      0, texH - 1);
                    int y1 = Mathf.Clamp(loy + face.rowCount * ch, 0, texH - 1);
                    for (int y = y0; y <= y1; y++)
                    {
                        if (rotate90)
                        {
                            int ty = y;
                            for (int dx = 0; dx < 2; dx++)
                            {
                                int tx = Mathf.Clamp(texW - rawX - dx, 0, texW - 1);
                                pixels[ty * texW + tx] = lineCol;
                            }
                        }
                        else
                        {
                            int ty = texH - 1 - y;
                            for (int dx = 0; dx < 2; dx++)
                            {
                                int tx = Mathf.Clamp(rawX + dx, 0, texW - 1);
                                pixels[ty * texW + tx] = lineCol;
                            }
                        }
                    }
                }

                // 水平線（列邊界），畫 2px 高確保縮放後仍可見
                for (int row = 0; row <= face.rowCount; row++)
                {
                    int rowY = loy + row * ch;
                    if (rowY < 0 || rowY > texH) continue;
                    int x0 = Mathf.Clamp(lox,                      0, texW - 1);
                    int x1 = Mathf.Clamp(lox + face.colCount * cw, 0, texW - 1);
                    for (int x = x0; x <= x1; x++)
                    {
                        if (rotate90)
                        {
                            int tx = Mathf.Clamp(texW - x, 0, texW - 1);
                            for (int dy = 0; dy < 2; dy++)
                            {
                                int ty = Mathf.Clamp(rowY + dy, 0, texH - 1);
                                pixels[ty * texW + tx] = lineCol;
                            }
                        }
                        else
                        {
                            for (int dy = 0; dy < 2; dy++)
                            {
                                int ty = Mathf.Clamp(texH - 1 - rowY - dy, 0, texH - 1);
                                pixels[ty * texW + x] = lineCol;
                            }
                        }
                    }
                }
            }
        }

        // VCell borders (bright light blue, 3 px, drawn over chip borders)
        if (vcellSizePx > 0 && faces != null)
        {
            var vcCol = new Color32(120, 200, 255, 195);
            int ox2 = Mathf.RoundToInt(faceViewOriginX);
            int oy2 = Mathf.RoundToInt(faceViewOriginY);
            foreach (var face in faces)
            {
                if (face == null) continue;
                int cw  = face.GizmoCellW;
                int ch  = face.GizmoCellH;
                if (vcellSizePx % cw != 0 || vcellSizePx % ch != 0) continue;
                int lox = face.originX - ox2;
                int loy = face.originY - oy2;
                int vcsX = face.colCount / (vcellSizePx / cw);
                int vcsY = face.rowCount / (vcellSizePx / ch);
                // vertical VCell borders
                for (int vc = 0; vc <= vcsX; vc++)
                {
                    int rawX = lox + vc * vcellSizePx;
                    if (rawX < 0 || rawX > texW) continue;
                    int y0 = Mathf.Clamp(loy, 0, texH - 1);
                    int y1 = Mathf.Clamp(loy + face.rowCount * ch, 0, texH - 1);
                    for (int y = y0; y <= y1; y++)
                    {
                        if (rotate90) { int ty = y; for (int dx = 0; dx < 3; dx++) { int tx = Mathf.Clamp(texW - rawX - dx, 0, texW - 1); pixels[ty * texW + tx] = vcCol; } }
                        else          { int ty = texH - 1 - y; for (int dx = 0; dx < 3; dx++) { int tx = Mathf.Clamp(rawX + dx, 0, texW - 1); pixels[ty * texW + tx] = vcCol; } }
                    }
                }
                // horizontal VCell borders
                for (int vr = 0; vr <= vcsY; vr++)
                {
                    int rowY = loy + vr * vcellSizePx;
                    if (rowY < 0 || rowY > texH) continue;
                    int x0 = Mathf.Clamp(lox, 0, texW - 1);
                    int x1 = Mathf.Clamp(lox + face.colCount * cw, 0, texW - 1);
                    for (int x = x0; x <= x1; x++)
                    {
                        if (rotate90) { int tx = Mathf.Clamp(texW - x, 0, texW - 1); for (int dy = 0; dy < 3; dy++) { int ty = Mathf.Clamp(rowY + dy, 0, texH - 1); pixels[ty * texW + tx] = vcCol; } }
                        else          { for (int dy = 0; dy < 3; dy++) { int ty = Mathf.Clamp(texH - 1 - rowY - dy, 0, texH - 1); pixels[ty * texW + x] = vcCol; } }
                    }
                }
            }
        }

        _gridLineTex.SetPixels32(pixels);
        _gridLineTex.Apply();
    }

    public void SetGridLineVisible(bool visible)
    {
        if (_gridLineImage != null)
            _gridLineImage.gameObject.SetActive(visible);
    }

    public void SetScreenOverlayVisible(bool visible)
    {
        if (_overlayImage != null)
            _overlayImage.gameObject.SetActive(visible);
    }

    void EnsureGridLineImage()
    {
        if (_gridLineImage != null) return;
        var go = new GameObject("GridLineOverlay");
        go.layer = 5;
        go.transform.SetParent(controlGridParent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        _gridLineImage               = go.AddComponent<RawImage>();
        _gridLineImage.raycastTarget = false;
        go.transform.SetSiblingIndex(1); // 在 GameDisplayOverlay(0) 之後、格子之前
    }
}
