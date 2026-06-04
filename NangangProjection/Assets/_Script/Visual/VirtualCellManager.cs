using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 虛擬格管理器。掛在 SensorTestManager 同一 GameObject 上。
///
/// SensorTestManager 在 VCell 啟用時直接呼叫 HandleChipTouch（不走 SensorBus），
/// 以計時器判斷每格晶片的活躍狀態，彙整後廣播 VirtualCellBus 事件。
///
/// 雙層鎖：
///   晶片鎖（Ctrl+右鍵）→ 晶片不貢獻觸碰，由 SensorTestManager 攔截於 ActivateCell
///   VCell 鎖（右鍵）   → 整格不廣播，由本腳本攔截於 HandleChipTouch
/// </summary>
public class VirtualCellManager : MonoBehaviour
{
    [Header("虛擬大格模式")]
    [Header("停用此元件 → 回到小晶片格模式")]
    [Header("硬體設定")]
    [Tooltip("感應器每輪偵測間隔（ms）。超過 2 倍此值視為離手。")]
    public int pollingIntervalMs = SensorHardwareConst.PollingIntervalMs;

    [Header("視覺設定")]
    [Tooltip("VCell 模式啟用時，落腳的個別晶片是否在 TileGridTexture 上亮紅。關閉後只顯示 VCell amber 色塊，晶片格不閃燈。")]
    [SerializeField] bool showChipVisualInVCellMode = true;

    // ── 執行期狀態 ────────────────────────────────────────────────────────────

    VirtualCellMode _mode = VirtualCellMode.None;
    int   _cellSizePx;
    float _canvasW, _canvasH;
    float _releaseTimeout;   // seconds

    class FaceVCellData
    {
        public SensorFaceConfig config;
        public int chipsPerVCellX, chipsPerVCellY;
        public int vcellsX, vcellsY;
        public bool[]  vcellLocked;   // [vrow * vcellsX + vcol]
        public bool[]  vcellActive;
        public float[] chipLastTouch; // [row * colCount + col]，Time.realtimeSinceStartup
    }

    readonly List<FaceVCellData> _faceData = new List<FaceVCellData>();

    /// <summary>VCell 功能是否已初始化且啟用。</summary>
    public bool Enabled => _mode != VirtualCellMode.None;

    // ── 視覺 Overlay（Task 6）────────────────────────────────────────────────────

    System.Func<int, int, bool> _isChipLocked;   // (faceIdx, chipIdx) → locked?

    RawImage  _controlOverlayImage;
    RawImage  _sensorOverlayImage;
    Texture2D _vcellOverlayTex;
    int       _vcellTexW, _vcellTexH;
    Color32[] _vcellPixelBuf;

    RawImage  _sensorLineImage;
    Texture2D _sensorLineTex;

    TileGridTexture _tileGrid;

    // ── 初始化 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 依場域預設重新初始化。場域切換時由 SensorTestManager 呼叫。
    /// mode=None 時直接清除並停用，不做任何驗證。
    /// </summary>
    public void Initialize(VenuePresetConfig preset, DisplayCanvasConfig canvas)
    {
        _faceData.Clear();
        _mode = VirtualCellMode.None;

        if (preset == null || preset.virtualCellMode == VirtualCellMode.None || canvas == null)
            return;

        _cellSizePx     = preset.virtualCellMode == VirtualCellMode.Size32 ? 32 : 64;
        _canvasW        = canvas.canvasWidth;
        _canvasH        = canvas.canvasHeight;
        _releaseTimeout = pollingIntervalMs * 2f / 1000f;

        // ── 建立各面資料 + 整除驗證 ──────────────────────────────────────────
        foreach (var fc in preset.faces)
        {
            if (fc == null) continue;

            int cellW  = fc.GizmoCellW;
            int cellH  = fc.GizmoCellH;
            int cpvX   = _cellSizePx / cellW;
            int cpvY   = _cellSizePx / cellH;

            if (_cellSizePx % cellW != 0 || _cellSizePx % cellH != 0 ||
                fc.colCount % cpvX != 0   || fc.rowCount % cpvY != 0)
            {
                Debug.LogError(
                    $"[VCell] 面 '{fc.faceName}' 尺寸（{fc.colCount}欄×{fc.rowCount}列，" +
                    $"每格 {cellW}×{cellH}px）無法整除 VCell {_cellSizePx}px，VCell 停用。");
                _faceData.Clear();
                return;
            }

            int vx = fc.colCount / cpvX;
            int vy = fc.rowCount / cpvY;

            _faceData.Add(new FaceVCellData
            {
                config         = fc,
                chipsPerVCellX = cpvX,
                chipsPerVCellY = cpvY,
                vcellsX        = vx,
                vcellsY        = vy,
                vcellLocked    = new bool[vx * vy],
                vcellActive    = new bool[vx * vy],
                chipLastTouch  = new float[fc.colCount * fc.rowCount],
            });
        }

        // ── 畫布區域重疊驗證 ─────────────────────────────────────────────────
        if (!ValidateNoOverlap(preset.faces))
        {
            _faceData.Clear();
            return;
        }

        _mode = preset.virtualCellMode;
        Debug.Log(
            $"[VCell] 初始化完成：{_mode}，{_faceData.Count} 個面，" +
            $"釋放逾時 {pollingIntervalMs * 2} ms");
    }

    bool ValidateNoOverlap(SensorFaceConfig[] faces)
    {
        for (int i = 0; i < faces.Length; i++)
        {
            for (int j = i + 1; j < faces.Length; j++)
            {
                var a = faces[i]; var b = faces[j];
                if (a == null || b == null) continue;
                var ra = new Rect(a.originX, a.originY, a.TotalPixelWidth,  a.TotalPixelHeight);
                var rb = new Rect(b.originX, b.originY, b.TotalPixelWidth,  b.TotalPixelHeight);
                if (ra.Overlaps(rb))
                {
                    Debug.LogError(
                        $"[VCell] 面 '{a.faceName}' 與 '{b.faceName}' 畫布區域重疊，VCell 停用。");
                    return false;
                }
            }
        }
        return true;
    }

    // ── Touch 接收 ────────────────────────────────────────────────────────────

    /// <summary>
    /// SensorTestManager 在 VCell 啟用時直接呼叫（替代 SensorBus.CanvasTouch 廣播路徑）。
    /// 晶片未被晶片鎖才會到達此處（ActivateCell 已攔截）。
    /// </summary>
    public void HandleChipTouch(int faceIdx, int chipIdx)
    {
        if (!Enabled || faceIdx >= _faceData.Count) return;

        var fv = _faceData[faceIdx];
        fv.chipLastTouch[chipIdx] = Time.realtimeSinceStartup;

        int col      = chipIdx % fv.config.colCount;
        int row      = chipIdx / fv.config.colCount;
        int vcol     = col / fv.chipsPerVCellX;
        int vrow     = row / fv.chipsPerVCellY;
        int vcellIdx = vrow * fv.vcellsX + vcol;

        if (fv.vcellLocked[vcellIdx]) return;

        // 個別晶片視覺：TileGridTexture 已有防重繪（Active 時只更新 timer），100Hz 呼叫安全
        if (showChipVisualInVCellMode)
            _tileGrid?.SetActive(faceIdx, chipIdx);

        // 每次 chip touch 都廣播 VCell 中心座標，維持連續更新（驅動 SensorClickSimulator / 遊戲效果）
        var center = VCellCenter(fv, vcol, vrow);
        SensorBus.CanvasTouch(center.x, center.y, _canvasW, _canvasH);

        if (!fv.vcellActive[vcellIdx])
        {
            fv.vcellActive[vcellIdx] = true;
            BroadcastTouch(faceIdx, fv, vcol, vrow);
            RefreshLockOverlaysForVCell(faceIdx, vcellIdx);
        }
    }

    // ── Update：釋放逾時偵測 ──────────────────────────────────────────────────

    void Update()
    {
        if (!Enabled) return;

        float now = Time.realtimeSinceStartup;

        for (int fi = 0; fi < _faceData.Count; fi++)
        {
            var fv  = _faceData[fi];
            var cfg = fv.config;
            for (int vrow = 0; vrow < fv.vcellsY; vrow++)
            for (int vcol = 0; vcol < fv.vcellsX; vcol++)
            {
                int vcellIdx = vrow * fv.vcellsX + vcol;
                if (!fv.vcellActive[vcellIdx]) continue;

                int  baseCol = vcol * fv.chipsPerVCellX;
                int  baseRow = vrow * fv.chipsPerVCellY;
                bool anyWarm = false;

                for (int dc = 0; dc < fv.chipsPerVCellX && !anyWarm; dc++)
                for (int dr = 0; dr < fv.chipsPerVCellY && !anyWarm; dr++)
                {
                    int cidx = (baseRow + dr) * cfg.colCount + (baseCol + dc);
                    if (now - fv.chipLastTouch[cidx] <= _releaseTimeout)
                        anyWarm = true;
                }

                if (!anyWarm)
                {
                    fv.vcellActive[vcellIdx] = false;
                    if (!fv.vcellLocked[vcellIdx])
                        BroadcastRelease(fv, vcol, vrow);
                    RefreshLockOverlaysForVCell(fi, vcellIdx);
                }
            }
        }
    }

    // ── 廣播 ──────────────────────────────────────────────────────────────────

    void BroadcastTouch(int faceIdx, FaceVCellData fv, int vcol, int vrow)
    {
        var c = VCellCenter(fv, vcol, vrow);
        VirtualCellBus.VirtualCellTouch(c.x, c.y, _canvasW, _canvasH, _cellSizePx);
    }

    void BroadcastRelease(FaceVCellData fv, int vcol, int vrow)
    {
        var c = VCellCenter(fv, vcol, vrow);
        VirtualCellBus.VirtualCellRelease(c.x, c.y, _canvasW, _canvasH, _cellSizePx);
    }

    Vector2 VCellCenter(FaceVCellData fv, int vcol, int vrow)
        => new Vector2(
            fv.config.originX + vcol * _cellSizePx + _cellSizePx * 0.5f,
            fv.config.originY + vrow * _cellSizePx + _cellSizePx * 0.5f);

    // ── 鎖定 API（SensorTestManager 呼叫）────────────────────────────────────

    /// <summary>查詢某晶片所屬 VCell 是否被 VCell-鎖定。</summary>
    public bool IsVCellLocked(int faceIdx, int chipIdx)
    {
        if (!Enabled || faceIdx >= _faceData.Count) return false;
        ChipToVCellIdx(_faceData[faceIdx], chipIdx, out int vcellIdx);
        return _faceData[faceIdx].vcellLocked[vcellIdx];
    }

    /// <summary>切換某晶片所屬 VCell 的鎖定狀態。</summary>
    public void ToggleVCellLock(int faceIdx, int chipIdx)
    {
        if (!Enabled || faceIdx >= _faceData.Count) return;
        var fv = _faceData[faceIdx];
        ChipToVCellIdx(fv, chipIdx, out int vcellIdx);
        fv.vcellLocked[vcellIdx] = !fv.vcellLocked[vcellIdx];
        RefreshLockOverlaysForVCell(faceIdx, vcellIdx);
    }

    /// <summary>解除所有 VCell 鎖定。</summary>
    public void UnlockAllVCells()
    {
        foreach (var fv in _faceData)
            System.Array.Clear(fv.vcellLocked, 0, fv.vcellLocked.Length);
        RefreshAllLockOverlays();
    }

    // ── 私有工具 ──────────────────────────────────────────────────────────────

    void ChipToVCellIdx(FaceVCellData fv, int chipIdx, out int vcellIdx)
    {
        int col  = chipIdx % fv.config.colCount;
        int row  = chipIdx / fv.config.colCount;
        int vcol = col / fv.chipsPerVCellX;
        int vrow = row / fv.chipsPerVCellY;
        vcellIdx = vrow * fv.vcellsX + vcol;
    }

    // ── 視覺 Overlay 公開 API ─────────────────────────────────────────────────

    /// <summary>設定 TileGridTexture 參考，用於 VCell 觸發時閃燈。場域切換後須重新呼叫。</summary>
    public void SetTileGrid(TileGridTexture tileGrid) => _tileGrid = tileGrid;

    /// <summary>設定晶片鎖定查詢委派，用於自動偵測「全格晶片都被晶片鎖」。</summary>
    public void SetChipLockedQuery(System.Func<int, int, bool> query) => _isChipLocked = query;

    /// <summary>取得晶片所屬 VCell 的索引（faceIdx 內部編號）。未啟用或超出範圍時回傳 -1。</summary>
    public int GetVCellIdx(int faceIdx, int chipIdx)
    {
        if (!Enabled || faceIdx >= _faceData.Count) return -1;
        ChipToVCellIdx(_faceData[faceIdx], chipIdx, out int vcellIdx);
        return vcellIdx;
    }

    /// <summary>取得 chipIdx 所屬 VCell 的中心畫布像素座標。供 SmartTouch 在 VCell 模式廣播 Canvas 事件用。</summary>
    public bool TryGetVCellCenter(int faceIdx, int chipIdx, out float cx, out float cy, out float cw, out float ch)
    {
        cx = cy = 0f;
        cw = _canvasW;
        ch = _canvasH;
        if (!Enabled || faceIdx >= _faceData.Count) return false;
        var fv   = _faceData[faceIdx];
        int col  = chipIdx % fv.config.colCount;
        int row  = chipIdx / fv.config.colCount;
        int vcol = col / fv.chipsPerVCellX;
        int vrow = row / fv.chipsPerVCellY;
        var c    = VCellCenter(fv, vcol, vrow);
        cx = c.x;
        cy = c.y;
        return true;
    }

    /// <summary>
    /// 在 controlGridParent 內建立每個 VCell 的半透明鎖定覆蓋 Image。
    /// BuildControlGrid 清除所有子物件後呼叫（舊 GO 已被 Destroy），此處只需 Clear 列表並重建。
    /// </summary>
    public void BuildControlLockOverlays(
        RectTransform parent, int viewFaceIdx,
        float scale, float offsetX, float offsetY,
        bool rotate90, float faceViewWidth,
        float faceViewOriginX, float faceViewOriginY)
    {
        if (_controlOverlayImage != null) { Destroy(_controlOverlayImage.gameObject); _controlOverlayImage = null; }
        if (!Enabled || parent == null) return;

        EnsureOverlayTexture();

        var go = new GameObject("VCellControlOverlay");
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        var img = go.AddComponent<RawImage>();
        img.texture       = _vcellOverlayTex;
        img.raycastTarget = false;

        // Mirror the same positioning used by ControlTexOverlay
        int   cW       = (int)_canvasW;
        int   cH       = (int)_canvasH;
        float displayW = (rotate90 ? cH : cW) * scale;
        float displayH = (rotate90 ? cW : cH) * scale;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        if (!rotate90)
        {
            rt.localEulerAngles = Vector3.zero;
            rt.sizeDelta        = new Vector2(displayW, displayH);
            img.uvRect          = new Rect(0f, 0f, 1f, 1f);
        }
        else
        {
            rt.localEulerAngles = new Vector3(0f, 0f, -90f);
            rt.sizeDelta        = new Vector2(cW * scale, cH * scale);
            img.uvRect          = new Rect(1f, 1f, -1f, -1f);
        }
        rt.anchoredPosition = new Vector2(
            offsetX + displayW * 0.5f,
            -(offsetY + displayH * 0.5f));

        _controlOverlayImage = img;
        RefreshAllLockOverlays();
    }

    /// <summary>
    /// 在 sensorGridParent 內建立每個 VCell 的半透明鎖定覆蓋 Image（1:1 畫布像素）。
    /// 並重建感應格 VCell 格線 Texture2D。
    /// 每次呼叫前會先銷毀舊物件（OnFaceView 不重建 sensorGrid，需手動 Destroy）。
    /// </summary>
    public void BuildSensorVCellOverlay(RectTransform parent, int canvasW, int canvasH, SensorFaceConfig[] faces)
    {
        if (_sensorOverlayImage != null) { Destroy(_sensorOverlayImage.gameObject); _sensorOverlayImage = null; }
        if (_sensorLineImage    != null) { Destroy(_sensorLineImage.gameObject);    _sensorLineImage    = null; }
        if (_sensorLineTex      != null) { Destroy(_sensorLineTex);                 _sensorLineTex      = null; }

        if (!Enabled || parent == null) return;

        // ── 感應格 VCell 格線（RawImage）────────────────────────────────────────
        var lineGO = new GameObject("VCellSensorLines");
        lineGO.layer = 5;
        lineGO.transform.SetParent(parent, false);
        var lineRT = lineGO.AddComponent<RectTransform>();
        lineRT.anchorMin = lineRT.anchorMax = new Vector2(0f, 1f);
        lineRT.pivot     = new Vector2(0f, 1f);
        lineRT.anchoredPosition = Vector2.zero;
        lineRT.sizeDelta = new Vector2(canvasW, canvasH);
        _sensorLineImage = lineGO.AddComponent<RawImage>();
        _sensorLineImage.raycastTarget = false;

        _sensorLineTex = new Texture2D(canvasW, canvasH, TextureFormat.RGBA32, false);
        _sensorLineTex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[canvasW * canvasH];
        var vcCol  = new Color32(120, 200, 255, 160);

        if (faces != null)
        {
            foreach (var face in faces)
            {
                if (face == null) continue;
                int cw = face.GizmoCellW;
                int ch = face.GizmoCellH;
                if (_cellSizePx % cw != 0 || _cellSizePx % ch != 0) continue;
                int vcsX = face.colCount / (_cellSizePx / cw);
                int vcsY = face.rowCount / (_cellSizePx / ch);

                for (int vc = 0; vc <= vcsX; vc++)
                {
                    int rawX = face.originX + vc * _cellSizePx;
                    if (rawX < 0 || rawX >= canvasW) continue;
                    int y0 = face.originY;
                    int y1 = face.originY + face.rowCount * ch;
                    for (int y = Mathf.Clamp(y0, 0, canvasH - 1); y <= Mathf.Clamp(y1, 0, canvasH - 1); y++)
                    {
                        int ty = canvasH - 1 - y;
                        for (int dx = 0; dx < 2; dx++)
                        {
                            int tx = Mathf.Clamp(rawX + dx, 0, canvasW - 1);
                            pixels[ty * canvasW + tx] = vcCol;
                        }
                    }
                }
                for (int vr = 0; vr <= vcsY; vr++)
                {
                    int rawY = face.originY + vr * _cellSizePx;
                    if (rawY < 0 || rawY >= canvasH) continue;
                    int x0 = face.originX;
                    int x1 = face.originX + face.colCount * cw;
                    int ty  = Mathf.Clamp(canvasH - 1 - rawY, 0, canvasH - 1);
                    for (int x = Mathf.Clamp(x0, 0, canvasW - 1); x <= Mathf.Clamp(x1, 0, canvasW - 1); x++)
                    {
                        pixels[ty * canvasW + x] = vcCol;
                        if (ty > 0) pixels[(ty - 1) * canvasW + x] = vcCol;
                    }
                }
            }
        }
        _sensorLineTex.SetPixels32(pixels);
        _sensorLineTex.Apply();
        _sensorLineImage.texture = _sensorLineTex;

        // ── VCell 狀態覆蓋（單 RawImage＋小 Texture2D，FilterMode.Point）─────
        EnsureOverlayTexture();
        var oGO = new GameObject("VCellSensorOverlay");
        oGO.layer = 5;
        oGO.transform.SetParent(parent, false);
        var oRT = oGO.AddComponent<RectTransform>();
        oRT.anchorMin = oRT.anchorMax = new Vector2(0f, 1f);
        oRT.pivot     = new Vector2(0f, 1f);
        oRT.anchoredPosition = Vector2.zero;
        oRT.sizeDelta        = new Vector2(canvasW, canvasH);
        _sensorOverlayImage  = oGO.AddComponent<RawImage>();
        _sensorOverlayImage.texture       = _vcellOverlayTex;
        _sensorOverlayImage.raycastTarget = false;

        RefreshAllLockOverlays();
    }

    void EnsureOverlayTexture()
    {
        _vcellTexW = Mathf.Max(1, (int)_canvasW / _cellSizePx);
        _vcellTexH = Mathf.Max(1, (int)_canvasH / _cellSizePx);

        if (_vcellOverlayTex != null &&
            _vcellOverlayTex.width  == _vcellTexW &&
            _vcellOverlayTex.height == _vcellTexH)
            return;

        if (_vcellOverlayTex != null) Destroy(_vcellOverlayTex);
        _vcellOverlayTex            = new Texture2D(_vcellTexW, _vcellTexH, TextureFormat.RGBA32, false);
        _vcellOverlayTex.filterMode = FilterMode.Point;
        _vcellPixelBuf              = new Color32[_vcellTexW * _vcellTexH];
        System.Array.Clear(_vcellPixelBuf, 0, _vcellPixelBuf.Length);
        _vcellOverlayTex.SetPixels32(_vcellPixelBuf);
        _vcellOverlayTex.Apply();
    }

    // ── Overlay 顏色計算 ──────────────────────────────────────────────────────

    Color VCellOverlayColor(int fi, FaceVCellData fv, int vi)
    {
        if (fv.vcellLocked[vi])
            return new Color(0.30f, 0.60f, 1.0f, 0.38f);   // VCell 鎖：實心淺藍
        if (AllChipsLocked(fi, fv, vi))
            return new Color(0.30f, 0.60f, 1.0f, 0.22f);   // 全晶片鎖：較淡淺藍
        if (fv.vcellActive[vi])
            return new Color(1.00f, 0.80f, 0.25f, 0.25f);  // 觸碰中：暖琥珀金
        return Color.clear;
    }

    bool AllChipsLocked(int fi, FaceVCellData fv, int vi)
    {
        if (_isChipLocked == null) return false;
        int vcol    = vi % fv.vcellsX;
        int vrow    = vi / fv.vcellsX;
        int baseCol = vcol * fv.chipsPerVCellX;
        int baseRow = vrow * fv.chipsPerVCellY;
        for (int dc = 0; dc < fv.chipsPerVCellX; dc++)
        for (int dr = 0; dr < fv.chipsPerVCellY; dr++)
        {
            if (!_isChipLocked(fi, (baseRow + dr) * fv.config.colCount + (baseCol + dc)))
                return false;
        }
        return true;
    }

    /// <summary>重新整理所有 VCell 狀態覆蓋（鎖定或觸碰狀態全量重繪）。</summary>
    public void RefreshAllLockOverlays()
    {
        if (_vcellOverlayTex == null || _vcellPixelBuf == null) return;

        System.Array.Clear(_vcellPixelBuf, 0, _vcellPixelBuf.Length);
        for (int fi = 0; fi < _faceData.Count; fi++)
        {
            var fv = _faceData[fi];
            for (int vrow = 0; vrow < fv.vcellsY; vrow++)
            for (int vcol = 0; vcol < fv.vcellsX; vcol++)
            {
                int vi  = vrow * fv.vcellsX + vcol;
                var col = VCellOverlayColor(fi, fv, vi);
                if (col.a <= 0f) continue;
                int texX = fv.config.originX / _cellSizePx + vcol;
                int texY = _vcellTexH - 1 - (fv.config.originY / _cellSizePx + vrow);
                if ((uint)texX < (uint)_vcellTexW && (uint)texY < (uint)_vcellTexH)
                    _vcellPixelBuf[texY * _vcellTexW + texX] = (Color32)col;
            }
        }
        _vcellOverlayTex.SetPixels32(_vcellPixelBuf);
        _vcellOverlayTex.Apply();
    }

    /// <summary>重新整理特定 VCell 的覆蓋顏色（VCell 鎖或觸碰狀態單格變更後呼叫）。</summary>
    public void RefreshLockOverlaysForVCell(int faceIdx, int vcellIdx)
    {
        if (_vcellOverlayTex == null || _vcellPixelBuf == null || faceIdx >= _faceData.Count) return;
        var fv   = _faceData[faceIdx];
        int vcol = vcellIdx % fv.vcellsX;
        int vrow = vcellIdx / fv.vcellsX;
        int texX = fv.config.originX / _cellSizePx + vcol;
        int texY = _vcellTexH - 1 - (fv.config.originY / _cellSizePx + vrow);
        if ((uint)texX >= (uint)_vcellTexW || (uint)texY >= (uint)_vcellTexH) return;
        _vcellPixelBuf[texY * _vcellTexW + texX] = (Color32)VCellOverlayColor(faceIdx, fv, vcellIdx);
        _vcellOverlayTex.SetPixels32(_vcellPixelBuf);
        _vcellOverlayTex.Apply();
    }

    /// <summary>晶片鎖切換後呼叫，自動偵測該格所屬 VCell 的「全晶片鎖」狀態並更新覆蓋。</summary>
    public void RefreshLockOverlaysForChip(int faceIdx, int chipIdx)
    {
        if (!Enabled || faceIdx >= _faceData.Count) return;
        ChipToVCellIdx(_faceData[faceIdx], chipIdx, out int vi);
        RefreshLockOverlaysForVCell(faceIdx, vi);
    }
}
