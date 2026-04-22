using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 格子建立、感應格 RT 捕捉、縮放計算（partial）。
/// 包含 BuildSensorGrid / BuildControlGrid (sync + async) / SetupSensorGridRT 及定位靜態方法。
/// 獨立成檔以降低讀主檔的成本。
/// </summary>
public partial class SensorTestManager
{
    // ── 縮放計算 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 計算等比縮放參數：自動選擇 0° 或 90° CW 旋轉，讓畫布在容器（w×h）中佔據最大面積。
    /// 逐面模式時，以該面的像素尺寸作為內容範圍。
    /// </summary>
    void CalcControlScale(float w, float h)
    {
        int cW, cH;
        if (_viewFaceIdx >= 0 && _viewFaceIdx < _faces.Count)
        {
            var fc = _faces[_viewFaceIdx].config;
            cW = fc.TotalPixelWidth;
            cH = fc.TotalPixelHeight;
            _faceViewOriginX = fc.originX;
            _faceViewOriginY = fc.originY;
        }
        else
        {
            cW = _canvasConfig.canvasWidth;
            cH = _canvasConfig.canvasHeight;
            _faceViewOriginX = 0f;
            _faceViewOriginY = 0f;
        }
        _faceViewWidth  = cW;
        _faceViewHeight = cH;

        float sNormal  = Mathf.Min(w / cW, h / cH);
        float sRotated = Mathf.Min(w / cH, h / cW);
        // 讀取 preset 的旋轉設定；preset 不存在時以填滿面積自動選擇
        var _activePreset = (presets != null && _presetIdx < presets.Length) ? presets[_presetIdx] : null;
        _rotateDisplay90    = _activePreset != null ? _activePreset.rotateDisplay90 : sRotated > sNormal;
        float displayW      = _rotateDisplay90 ? cH : cW;
        float displayH      = _rotateDisplay90 ? cW : cH;
        _controlGridScale   = _rotateDisplay90 ? sRotated : sNormal;
        _controlGridOffsetX = (w - displayW * _controlGridScale) * 0.5f;
        _controlGridOffsetY = (h - displayH * _controlGridScale) * 0.5f;
    }

    /// <summary>將目前的縮放 / 旋轉參數推送給 ConsoleGameOverlay。</summary>
    void NotifyOverlay()
    {
        if (_overlay == null || _canvasConfig == null || _controlGridScale <= 0f) return;
        int cW = _canvasConfig.canvasWidth;
        int cH = _canvasConfig.canvasHeight;
        float displayW = (_rotateDisplay90 ? cH : cW) * _controlGridScale;
        float displayH = (_rotateDisplay90 ? cW : cH) * _controlGridScale;
        _overlay.UpdateOverlayTransform(
            _controlGridOffsetX, _controlGridOffsetY,
            displayW, displayH,
            _rotateDisplay90, cW, cH, _controlGridScale);

        // 格線 Texture2D
        SensorFaceConfig[] visibleFaces;
        if (_viewFaceIdx >= 0 && _viewFaceIdx < _faces.Count)
        {
            visibleFaces = new[] { _faces[_viewFaceIdx].config };
        }
        else
        {
            visibleFaces = new SensorFaceConfig[_faces.Count];
            for (int i = 0; i < _faces.Count; i++) visibleFaces[i] = _faces[i].config;
        }
        _overlay.UpdateGridLines(
            _controlGridOffsetX, _controlGridOffsetY,
            displayW, displayH,
            _rotateDisplay90, cW, cH, _controlGridScale,
            _faceViewOriginX, _faceViewOriginY,
            (int)_faceViewWidth, (int)_faceViewHeight,
            visibleFaces);
    }

    // ── 感應格（Display 2）建立 ───────────────────────────────────────────────

    void BuildSensorBackground()
    {
        if (sensorGridParent == null) return;
        var go = new GameObject("SensorBG");
        go.layer = 5;
        go.transform.SetParent(sensorGridParent.parent, false);
        go.transform.SetAsFirstSibling();
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color         = new Color(0.04f, 0.04f, 0.05f);
        img.raycastTarget = false;
    }

    void BuildSensorGrid()
    {
        if (sensorGridParent == null || _canvasConfig == null) return;

        // 清除舊格子（只清 sensorGridParent 下的格子物件，SensorBG 在其 parent 下）
        for (int i = sensorGridParent.childCount - 1; i >= 0; i--)
            Destroy(sensorGridParent.GetChild(i).gameObject);

        // 移除 GridLayoutGroup（改用絕對定位）
        var oldGlg = sensorGridParent.GetComponent<GridLayoutGroup>();
        if (oldGlg != null) Destroy(oldGlg);

        foreach (var fd in _faces)
        {
            var cfg = fd.config;
            for (int r = 0; r < cfg.rowCount; r++)
            for (int c = 0; c < cfg.colCount; c++)
            {
                int idx = r * cfg.colCount + c;
                var go = new GameObject($"ST_f{_faces.IndexOf(fd)}_{idx}");
                go.layer = 5;
                go.transform.SetParent(sensorGridParent, false);
                var rt = go.AddComponent<RectTransform>();
                PositionSensorCell(rt, cfg, c, r);
                go.AddComponent<Image>();
                var cell = go.AddComponent<SensorTileCell>();
                cell.col   = c;
                cell.row   = r;
                cell.index = idx;
                fd.sensorTiles[idx] = cell;
            }
        }
    }

    IEnumerator BuildSensorGridAsync(float progressStart, float progressEnd)
    {
        if (sensorGridParent == null || _canvasConfig == null) yield break;

        for (int i = sensorGridParent.childCount - 1; i >= 0; i--)
            Destroy(sensorGridParent.GetChild(i).gameObject);

        var oldGlg = sensorGridParent.GetComponent<GridLayoutGroup>();
        if (oldGlg != null) Destroy(oldGlg);

        int totalCells = 0;
        foreach (var fd in _faces) totalCells += fd.config.colCount * fd.config.rowCount;
        if (totalCells == 0) yield break;

        int built = 0;
        const int batchSize = 300;

        for (int fi = 0; fi < _faces.Count; fi++)
        {
            var fd  = _faces[fi];
            var cfg = fd.config;
            for (int r = 0; r < cfg.rowCount; r++)
            for (int c = 0; c < cfg.colCount; c++)
            {
                int idx = r * cfg.colCount + c;
                var go = new GameObject($"ST_f{fi}_{idx}");
                go.layer = 5;
                go.transform.SetParent(sensorGridParent, false);
                var rt = go.AddComponent<RectTransform>();
                PositionSensorCell(rt, cfg, c, r);
                go.AddComponent<Image>();
                var cell = go.AddComponent<SensorTileCell>();
                cell.col   = c;
                cell.row   = r;
                cell.index = idx;
                fd.sensorTiles[idx] = cell;

                built++;
                if (built % batchSize == 0)
                {
                    SetLoadingProgress(Mathf.Lerp(progressStart, progressEnd, (float)built / totalCells));
                    yield return null;
                }
            }
        }
        SetLoadingProgress(progressEnd);
    }

    /// <summary>在 Canvas_Secondary（感應格）中以像素座標定位格子（1px = 1 canvas unit）。
    /// 永遠使用原始畫布座標；旋轉僅作用於 Display 1 控制格，不影響感應格 RT。</summary>
    static void PositionSensorCell(RectTransform rt, SensorFaceConfig cfg, int c, int r)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(
            cfg.originX + c * cfg.GizmoCellW,
          -(cfg.originY + r * cfg.GizmoCellH)
        );
        rt.sizeDelta = new Vector2(cfg.GizmoCellW, cfg.GizmoCellH);
    }

    // ── 控制格（Display 1）建立 ───────────────────────────────────────────────

    void BuildControlGrid()
    {
        if (controlGridParent == null || _canvasConfig == null) return;

        // 清除舊格子（保留 ConsoleGameOverlay 建立的 GameDisplayOverlay / GridLineOverlay）
        for (int i = controlGridParent.childCount - 1; i >= 0; i--)
        {
            var child = controlGridParent.GetChild(i);
            if (child.name == "GameDisplayOverlay" || child.name == "GridLineOverlay") continue;
            Destroy(child.gameObject);
        }

        // 移除 GridLayoutGroup（改用絕對定位）
        var oldGlg = controlGridParent.GetComponent<GridLayoutGroup>();
        if (oldGlg != null) Destroy(oldGlg);

        float w = controlGridParent.rect.width;
        float h = controlGridParent.rect.height;
        if (w <= 0 || h <= 0)
        {
            // rect 尚未佈局，等下一幀重建（理論上只有第一幀才會發生）
            StartCoroutine(RebuildControlGridNextFrame());
            return;
        }

        CalcControlScale(w, h);

        // 切換視圖前先清空所有面的 controlTile 參照，避免懸空引用
        foreach (var fd in _faces)
            for (int i = 0; i < fd.controlTiles.Length; i++)
                fd.controlTiles[i] = null;

        int faceStart = _viewFaceIdx >= 0 ? _viewFaceIdx : 0;
        int faceEnd   = _viewFaceIdx >= 0 ? _viewFaceIdx + 1 : _faces.Count;

        if (faceEnd - faceStart > 1)
            for (int fi = faceStart; fi < faceEnd; fi++)
                AddFaceBackground(_faces[fi].config);

        for (int fi = faceStart; fi < faceEnd; fi++)
        {
            var fd  = _faces[fi];
            var cfg = fd.config;
            int capturedFi = fi;

            for (int r = 0; r < cfg.rowCount; r++)
            for (int c = 0; c < cfg.colCount; c++)
            {
                int idx = r * cfg.colCount + c;
                var go = new GameObject($"CT_f{fi}_{idx}");
                go.layer = 5;
                go.transform.SetParent(controlGridParent, false);
                var rt = go.AddComponent<RectTransform>();
                PositionControlCell(rt, cfg, c, r, _controlGridScale, _controlGridOffsetX, _controlGridOffsetY,
                    _rotateDisplay90, _faceViewWidth, _faceViewOriginX, _faceViewOriginY);
                go.AddComponent<Image>();
                var cell = go.AddComponent<ControlTileCell>();
                cell.Init(c, r, cfg.colCount);

                int capturedIdx = idx;
                cell.onRightClick = (col2, row2) => ToggleLock(capturedFi, col2, row2);
                cell.onLeftClick  = (col2, row2) => OnLeftClick(capturedFi, row2 * cfg.colCount + col2);
                cell.onHover      = (col2, row2, i2, lk) => OnTileHover(capturedFi, col2, row2, i2, lk);
                fd.controlTiles[idx] = cell;
            }
        }
    }

    IEnumerator BuildControlGridAsync(float progressStart, float progressEnd)
    {
        if (controlGridParent == null || _canvasConfig == null) yield break;

        for (int i = controlGridParent.childCount - 1; i >= 0; i--)
        {
            var child = controlGridParent.GetChild(i);
            if (child.name == "GameDisplayOverlay" || child.name == "GridLineOverlay") continue;
            Destroy(child.gameObject);
        }

        var oldGlg = controlGridParent.GetComponent<GridLayoutGroup>();
        if (oldGlg != null) Destroy(oldGlg);

        float w = controlGridParent.rect.width;
        float h = controlGridParent.rect.height;
        if (w <= 0 || h <= 0) { yield return null; w = controlGridParent.rect.width; h = controlGridParent.rect.height; }
        if (w <= 0 || h <= 0) yield break;

        CalcControlScale(w, h);

        // 切換視圖前先清空所有面的 controlTile 參照，避免懸空引用
        foreach (var fd in _faces)
            for (int i = 0; i < fd.controlTiles.Length; i++)
                fd.controlTiles[i] = null;

        int faceStart = _viewFaceIdx >= 0 ? _viewFaceIdx : 0;
        int faceEnd   = _viewFaceIdx >= 0 ? _viewFaceIdx + 1 : _faces.Count;

        if (faceEnd - faceStart > 1)
            for (int fi = faceStart; fi < faceEnd; fi++)
                AddFaceBackground(_faces[fi].config);

        int totalCells = 0;
        for (int fi = faceStart; fi < faceEnd; fi++)
            totalCells += _faces[fi].config.colCount * _faces[fi].config.rowCount;
        if (totalCells == 0) yield break;

        int built = 0;
        const int batchSize = 300;

        for (int fi = faceStart; fi < faceEnd; fi++)
        {
            var fd  = _faces[fi];
            var cfg = fd.config;
            int capturedFi = fi;

            for (int r = 0; r < cfg.rowCount; r++)
            for (int c = 0; c < cfg.colCount; c++)
            {
                int idx = r * cfg.colCount + c;
                var go = new GameObject($"CT_f{fi}_{idx}");
                go.layer = 5;
                go.transform.SetParent(controlGridParent, false);
                var rt = go.AddComponent<RectTransform>();
                PositionControlCell(rt, cfg, c, r, _controlGridScale, _controlGridOffsetX, _controlGridOffsetY,
                    _rotateDisplay90, _faceViewWidth, _faceViewOriginX, _faceViewOriginY);
                go.AddComponent<Image>();
                var cell = go.AddComponent<ControlTileCell>();
                cell.Init(c, r, cfg.colCount);

                cell.onRightClick = (col2, row2) => ToggleLock(capturedFi, col2, row2);
                cell.onLeftClick  = (col2, row2) => OnLeftClick(capturedFi, row2 * cfg.colCount + col2);
                cell.onHover      = (col2, row2, i2, lk) => OnTileHover(capturedFi, col2, row2, i2, lk);
                fd.controlTiles[idx] = cell;

                built++;
                if (built % batchSize == 0)
                {
                    SetLoadingProgress(Mathf.Lerp(progressStart, progressEnd, (float)built / totalCells));
                    yield return null;
                }
            }
        }
        SetLoadingProgress(progressEnd);
    }

    void AddFaceBackground(SensorFaceConfig cfg)
    {
        var go = new GameObject($"FaceBG_{cfg.faceName}");
        go.layer = 5;
        go.transform.SetParent(controlGridParent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);

        float s  = _controlGridScale;
        float cx = cfg.originX - _faceViewOriginX;
        float cy = cfg.originY - _faceViewOriginY;

        if (_rotateDisplay90)
        {
            rt.anchoredPosition = new Vector2(
                 _controlGridOffsetX + cy * s,
                -(_controlGridOffsetY + (_faceViewWidth - cx - cfg.TotalPixelWidth) * s)
            );
            rt.sizeDelta = new Vector2(
                cfg.TotalPixelHeight * s,
                cfg.TotalPixelWidth  * s
            );
        }
        else
        {
            rt.anchoredPosition = new Vector2(
                 _controlGridOffsetX + cx * s,
                -(_controlGridOffsetY + cy * s)
            );
            rt.sizeDelta = new Vector2(
                cfg.TotalPixelWidth  * s,
                cfg.TotalPixelHeight * s
            );
        }

        var img = go.AddComponent<Image>();
        var col = cfg.gizmoColor;
        img.color = new Color(col.r, col.g, col.b, 0.12f);
        img.raycastTarget = false;

        var labelGO = new GameObject("Label");
        labelGO.layer = 5;
        labelGO.transform.SetParent(go.transform, false);
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var txt = labelGO.AddComponent<TextMeshProUGUI>();
        txt.text          = cfg.faceName;
        txt.font          = _uiFont;
        txt.fontSize      = 11;
        txt.alignment     = TextAlignmentOptions.Midline;
        txt.color         = new Color(col.r, col.g, col.b, 0.60f);
        txt.raycastTarget = false;
        txt.overflowMode  = TextOverflowModes.Overflow;

        go.transform.SetSiblingIndex(2);
    }

    IEnumerator RebuildControlGridNextFrame()
    {
        yield return null;
        BuildControlGrid();
    }

    /// <summary>在 controlGridParent 中以等比縮放定位格子（畫布像素 → 控制區像素）。
    /// rotated90=true 時，將格子座標旋轉 90° CCW 後再定位。
    /// faceOriginX/Y：逐面模式下從格子座標中扣除，使該面從 (0,0) 起算。</summary>
    static void PositionControlCell(RectTransform rt, SensorFaceConfig cfg,
        int c, int r, float scale, float offsetX, float offsetY,
        bool rotated90 = false, float rotRefWidth = 0f,
        float faceOriginX = 0f, float faceOriginY = 0f)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);

        float cx = cfg.originX + c * cfg.GizmoCellW - faceOriginX;
        float cy = cfg.originY + r * cfg.GizmoCellH - faceOriginY;

        if (rotated90)
        {
            rt.anchoredPosition = new Vector2(
                 offsetX + cy * scale,
                -(offsetY + (rotRefWidth - cx - cfg.GizmoCellW) * scale)
            );
            rt.sizeDelta = new Vector2(
                Mathf.Max(1f, cfg.GizmoCellH * scale),
                Mathf.Max(1f, cfg.GizmoCellW * scale)
            );
        }
        else
        {
            rt.anchoredPosition = new Vector2(
                 offsetX + cx * scale,
               -(offsetY + cy * scale)
            );
            rt.sizeDelta = new Vector2(
                Mathf.Max(1f, cfg.GizmoCellW * scale),
                Mathf.Max(1f, cfg.GizmoCellH * scale)
            );
        }
    }

    // ── 感應格 RT 捕捉 ────────────────────────────────────────────────────────

    void SetupSensorGridRT()
    {
        if (_canvasSecondary == null || _canvasConfig == null) return;

        var sensorCanvas = _canvasSecondary.GetComponent<Canvas>();
        if (sensorCanvas == null) return;

        // 感應格 RT 永遠使用原始畫布尺寸（旋轉只作用於 Display 1 控制格）
        int w = _canvasConfig.canvasWidth;
        int h = _canvasConfig.canvasHeight;

        var rt = SensorGridRT.GetOrCreate(w, h);

        // 清除舊畫面，避免 SensorGridCam 第一幀前 overlay 顯示殘影
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, new Color(0.04f, 0.04f, 0.05f));
        RenderTexture.active = prev;

        // 建立 SensorCam
        var camGO = new GameObject("SensorGridCam");
        camGO.layer = 5;
        camGO.transform.SetParent(transform, false);
        _sensorGridCam = camGO.AddComponent<Camera>();
        _sensorGridCam.clearFlags       = CameraClearFlags.SolidColor;
        _sensorGridCam.backgroundColor  = new Color(0.04f, 0.04f, 0.05f);
        _sensorGridCam.cullingMask      = 1 << 5;
        _sensorGridCam.orthographic     = true;
        _sensorGridCam.orthographicSize = h / 2f;
        _sensorGridCam.aspect           = (float)w / h;
        _sensorGridCam.nearClipPlane    = -100f;
        _sensorGridCam.farClipPlane     = 100f;
        _sensorGridCam.targetTexture    = rt;
        _sensorGridCam.targetDisplay    = 0;
        _sensorGridCam.depth            = -10;

        var scaler = _canvasSecondary.GetComponent<CanvasScaler>();
        if (scaler != null) scaler.enabled = false;

        sensorCanvas.renderMode  = RenderMode.ScreenSpaceCamera;
        sensorCanvas.worldCamera = _sensorGridCam;

        _display2MirrorCanvas = BuildDisplay2Mirror(rt);

        Debug.Log($"[SensorTest] SensorGridRT 捕捉設定完成（{w}×{h}）");
    }

    static GameObject BuildDisplay2Mirror(RenderTexture rt)
    {
        var go = new GameObject("Canvas_Display2RTMirror");
        go.layer = 5;
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = 1;
        canvas.sortingOrder  = -10;

        var imgGO = new GameObject("RTImage");
        imgGO.layer = 5;
        imgGO.transform.SetParent(go.transform, false);
        var imgRT = imgGO.AddComponent<RectTransform>();
        imgRT.anchorMin = Vector2.zero;
        imgRT.anchorMax = Vector2.one;
        imgRT.offsetMin = imgRT.offsetMax = Vector2.zero;
        var rawImg = imgGO.AddComponent<RawImage>();
        rawImg.texture       = rt;
        rawImg.raycastTarget = false;

        return go;
    }
}
