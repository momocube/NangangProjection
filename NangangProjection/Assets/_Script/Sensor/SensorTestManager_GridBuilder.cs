using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 格子建立、縮放計算（partial）。
/// 舊版的 SensorTileCell / ControlTileCell GameObjects 已改為
/// TileGridTexture（兩張 Texture2D + 一個 LateUpdate flush）。
/// Display 2 與 Display 1 各一個 RawImage；SensorGridCam / SensorGridRT 已移除。
/// </summary>
public partial class SensorTestManager
{
    // ── 縮放計算 ──────────────────────────────────────────────────────────────

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
        var _activePreset = (presets != null && _presetIdx < presets.Length) ? presets[_presetIdx] : null;
        _rotateDisplay90    = _activePreset != null ? _activePreset.rotateDisplay90 : sRotated > sNormal;
        float displayW      = _rotateDisplay90 ? cH : cW;
        float displayH      = _rotateDisplay90 ? cW : cH;
        _controlGridScale   = _rotateDisplay90 ? sRotated : sNormal;
        _controlGridOffsetX = (w - displayW * _controlGridScale) * 0.5f;
        _controlGridOffsetY = (h - displayH * _controlGridScale) * 0.5f;
    }

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

        SensorFaceConfig[] visibleFaces;
        if (_viewFaceIdx >= 0 && _viewFaceIdx < _faces.Count)
            visibleFaces = new[] { _faces[_viewFaceIdx].config };
        else
        {
            visibleFaces = new SensorFaceConfig[_faces.Count];
            for (int i = 0; i < _faces.Count; i++) visibleFaces[i] = _faces[i].config;
        }

        int vcellSizePx = 0;
        if (_vcellManager != null && _vcellManager.Enabled)
        {
            var vm = presets[_presetIdx].virtualCellMode;
            vcellSizePx = vm == VirtualCellMode.Size32 ? 32 : vm == VirtualCellMode.Size64 ? 64 : 0;
        }

        _overlay.UpdateGridLines(
            _controlGridOffsetX, _controlGridOffsetY,
            displayW, displayH,
            _rotateDisplay90, cW, cH, _controlGridScale,
            _faceViewOriginX, _faceViewOriginY,
            (int)_faceViewWidth, (int)_faceViewHeight,
            visibleFaces, vcellSizePx);

        UpdateControlTexOverlayTransform();

        if (_vcellManager != null)
        {
            _vcellManager.BuildControlLockOverlays(
                controlGridParent, _viewFaceIdx,
                _controlGridScale, _controlGridOffsetX, _controlGridOffsetY,
                _rotateDisplay90, _faceViewWidth,
                _faceViewOriginX, _faceViewOriginY);
        }
    }

    void RebuildSensorVCellOverlay()
    {
        if (_vcellManager == null || sensorGridParent == null || _canvasConfig == null) return;
        var allFaces = new SensorFaceConfig[_faces.Count];
        for (int i = 0; i < _faces.Count; i++) allFaces[i] = _faces[i].config;
        _vcellManager.BuildSensorVCellOverlay(
            sensorGridParent,
            _canvasConfig.canvasWidth, _canvasConfig.canvasHeight,
            allFaces);
    }

    // ── 格子建立（Texture2D 版）────────────────────────────────────────────────

    /// <summary>
    /// 建立或重建 TileGridTexture，並設定 D2 / D1 的 RawImage 顯示。
    /// 替代舊版的 BuildSensorGrid + BuildControlGrid + SetupSensorGridRT。
    /// </summary>
    void BuildGridTextures()
    {
        if (_canvasConfig == null) return;

        // 取得或建立 TileGridTexture component
        if (_tileGrid == null) _tileGrid = GetComponent<TileGridTexture>();
        if (_tileGrid == null) _tileGrid = gameObject.AddComponent<TileGridTexture>();

        var faceConfigs = new List<SensorFaceConfig>(_faces.Count);
        foreach (var fd in _faces) faceConfigs.Add(fd.config);

        _tileGrid.Initialize(_canvasConfig.canvasWidth, _canvasConfig.canvasHeight,
                             faceConfigs, activeDuration);

        // 截圖尺寸跟著畫布（場域切換時同步）
        Display2ScreenCapture.Instance?.Resize(_canvasConfig.canvasWidth, _canvasConfig.canvasHeight);

        BuildD2Display();
        BuildControlDisplay();
    }

    // ── Display 2 RawImage ────────────────────────────────────────────────────

    void BuildD2Display()
    {
        if (sensorGridParent == null || _tileGrid == null) return;

        // 清除舊子物件（保留 VCell overlay）
        for (int i = sensorGridParent.childCount - 1; i >= 0; i--)
        {
            var child = sensorGridParent.GetChild(i);
            if (child.name == "SensorTexDisplay") { Destroy(child.gameObject); break; }
            // 不清 VCell overlay，它由 RebuildSensorVCellOverlay 管理
        }
        var go = new GameObject("SensorTexDisplay");
        go.layer = 5;
        go.transform.SetParent(sensorGridParent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<RawImage>();
        img.texture       = _tileGrid.SensorTex;
        img.raycastTarget = false;
        go.transform.SetSiblingIndex(0);  // 最底層
        _d2SensorRawImage = go;
    }

    // ── Display 1 RawImage + ControlGridPointerHandler ────────────────────────

    void BuildControlDisplay()
    {
        if (controlGridParent == null || _canvasConfig == null || _tileGrid == null) return;

        // 清除舊格子（保留 GameDisplayOverlay / GridLineOverlay / ControlTexOverlay）
        for (int i = controlGridParent.childCount - 1; i >= 0; i--)
        {
            var child = controlGridParent.GetChild(i);
            if (child.name == "GameDisplayOverlay" ||
                child.name == "GridLineOverlay"    ||
                child.name == "ControlTexOverlay") continue;
            Destroy(child.gameObject);
        }

        // Nested Canvas + Display1GraphicRaycaster
        if (controlGridParent.GetComponent<Canvas>() == null)
        {
            controlGridParent.gameObject.AddComponent<Canvas>();
            controlGridParent.gameObject.AddComponent<Display1GraphicRaycaster>();
        }

        float w = controlGridParent.rect.width;
        float h = controlGridParent.rect.height;
        if (w <= 0 || h <= 0)
        {
            StartCoroutine(RebuildControlDisplayNextFrame());
            return;
        }

        CalcControlScale(w, h);

        // 多面場域：面背景標籤
        int faceStart = _viewFaceIdx >= 0 ? _viewFaceIdx : 0;
        int faceEnd   = _viewFaceIdx >= 0 ? _viewFaceIdx + 1 : _faces.Count;
        if (faceEnd - faceStart > 1)
            for (int fi = faceStart; fi < faceEnd; fi++)
                AddFaceBackground(_faces[fi].config);

        // ControlTexOverlay RawImage（若已存在則更新 texture，否則新建）
        var existingTr = controlGridParent.Find("ControlTexOverlay");
        if (existingTr != null)
        {
            _controlTexRawImage = existingTr.gameObject;
            var img = _controlTexRawImage.GetComponent<RawImage>();
            if (img != null) img.texture = _tileGrid.ControlTex;
        }
        else
        {
            var go = new GameObject("ControlTexOverlay");
            go.layer = 5;
            go.transform.SetParent(controlGridParent, false);
            var overlayRT = go.AddComponent<RectTransform>();
            overlayRT.anchorMin = new Vector2(0f, 1f);
            overlayRT.anchorMax = new Vector2(0f, 1f);
            overlayRT.pivot     = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<RawImage>();
            img.texture       = _tileGrid.ControlTex;
            img.raycastTarget = true;

            var handler = go.AddComponent<ControlGridPointerHandler>();
            handler.controlGridParent = controlGridParent;
            handler.hitTest           = HitTestControlGrid;
            handler.onLeftDown        = (fi, ci) => OnControlLeftDown(fi, ci);
            handler.onLeftUp          = (fi, ci) => OnControlLeftUp(fi, ci);
            handler.onHoldTick        = (fi, ci) => OnHoldTick(fi, ci);
            handler.onRightDown       = (fi, ci, ctrl) =>
            {
                if (ctrl || _vcellManager == null || !_vcellManager.Enabled)
                    ToggleLock(fi, ci % _faces[fi].config.colCount, ci / _faces[fi].config.colCount);
                else
                    _vcellManager.ToggleVCellLock(fi, ci);
            };
            handler.onHoverChanged = OnControlHoverChanged;

            _controlTexRawImage = go;
            go.transform.SetSiblingIndex(2); // 0=GameDisplayOverlay, 1=GridLineOverlay, 2=ControlTexOverlay
        }

        UpdateControlTexOverlayTransform();
    }

    void UpdateControlTexOverlayTransform()
    {
        if (_controlTexRawImage == null || _canvasConfig == null || _controlGridScale <= 0f) return;
        int   cW       = _canvasConfig.canvasWidth;
        int   cH       = _canvasConfig.canvasHeight;
        float displayW = (_rotateDisplay90 ? cH : cW) * _controlGridScale;
        float displayH = (_rotateDisplay90 ? cW : cH) * _controlGridScale;

        var rt  = _controlTexRawImage.GetComponent<RectTransform>();
        var img = _controlTexRawImage.GetComponent<RawImage>();
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);

        if (!_rotateDisplay90)
        {
            rt.localEulerAngles = Vector3.zero;
            rt.sizeDelta        = new Vector2(displayW, displayH);
            if (img != null) img.uvRect = new Rect(0f, 0f, 1f, 1f);
        }
        else
        {
            rt.localEulerAngles = new Vector3(0f, 0f, -90f);
            rt.sizeDelta        = new Vector2(cW * _controlGridScale, cH * _controlGridScale);
            if (img != null) img.uvRect = new Rect(1f, 1f, -1f, -1f);
        }
        rt.anchoredPosition = new Vector2(
             _controlGridOffsetX + displayW * 0.5f,
           -(_controlGridOffsetY + displayH * 0.5f)
        );
    }

    IEnumerator RebuildControlDisplayNextFrame()
    {
        yield return null;
        BuildControlDisplay();
    }

    // ── 拆除（替代舊版 TeardownSensorGridRT）────────────────────────────────────

    void TeardownGridDisplay()
    {
        if (_d2SensorRawImage    != null) { Destroy(_d2SensorRawImage);    _d2SensorRawImage    = null; }
        if (_controlTexRawImage  != null) { Destroy(_controlTexRawImage);  _controlTexRawImage  = null; }
        // Canvas_Secondary 保持 ScreenSpaceOverlay，不需要額外還原
    }

    // ── HitTest：螢幕座標 → (faceIdx, cellIdx) ────────────────────────────────

    (int fi, int ci) HitTestControlGrid(Vector2 localPos)
    {
        // localPos 是 controlGridParent 的 local 座標
        // 轉換為「從 controlGridParent 左上角起算的相對座標」（pivot-independent）
        if (_controlGridScale <= 0f || controlGridParent == null) return (-1, -1);
        var rect = controlGridParent.rect;
        float relX = localPos.x - rect.x;
        float relY = (rect.y + rect.height) - localPos.y;

        float cpxF, cpyF;
        if (!_rotateDisplay90)
        {
            float gx = relX - _controlGridOffsetX;
            float gy = relY - _controlGridOffsetY;
            cpxF = gx / _controlGridScale + _faceViewOriginX;
            cpyF = gy / _controlGridScale + _faceViewOriginY;
        }
        else
        {
            // 90° CW 旋轉：canvas Y → screen X，canvas X → screen Y（反轉）
            float gx = relX - _controlGridOffsetX;
            float gy = relY - _controlGridOffsetY;
            cpxF = _faceViewWidth - gy / _controlGridScale + _faceViewOriginX;
            cpyF = gx / _controlGridScale + _faceViewOriginY;
        }

        int cpx = (int)cpxF;
        int cpy = (int)cpyF;

        int faceStart = _viewFaceIdx >= 0 ? _viewFaceIdx : 0;
        int faceEnd   = _viewFaceIdx >= 0 ? _viewFaceIdx + 1 : _faces.Count;
        for (int fi = faceStart; fi < faceEnd; fi++)
        {
            var cfg = _faces[fi].config;
            if (!cfg.ContainsPixel(cpx, cpy)) continue;
            var (col, row) = cfg.PixelToCell(cpx, cpy);
            return (fi, row * cfg.colCount + col);
        }
        return (-1, -1);
    }

    // ── ControlGrid 滑鼠事件處理 ─────────────────────────────────────────────

    void OnControlLeftDown(int fi, int ci)
    {
        // 感應冷卻 debounce：gate 整個 down 事件（Touch + PressStart 都一起攔）
        bool cooldownOn = _clickSim != null && _clickSim.cooldownEnabled;
        float debounceS = cooldownOn ? _clickSim.clickCooldown : 0f;
        if (debounceS > 0f)
        {
            int key = fi * 100000 + ci;
            float now = Time.realtimeSinceStartup;
            if (_chipLastTouchTimes.TryGetValue(key, out float lastT) && now - lastT <= debounceS)
                return;
            // 不在這裡更新時間戳——由 ActivateCell 的 if(!skipVisual) 統一負責
        }
        OnLeftClick(fi, ci);
        OnMousePressStart(fi, ci);
    }

    void OnControlLeftUp(int fi, int ci)
    {
        OnMousePressEnd(fi, ci);
    }

    void OnControlHoverChanged(int fi, int ci)
    {
        // 清除舊 hover
        if (_hoverFace >= 0 && _hoverCell >= 0)
            _tileGrid?.ClearHover();

        _hoverFace = fi;
        _hoverCell = ci;

        if (fi >= 0 && ci >= 0 && fi < _faces.Count)
        {
            _tileGrid?.SetHovered(fi, ci);
            var cfg    = _faces[fi].config;
            int col    = ci % cfg.colCount;
            int row    = ci / cfg.colCount;
            int stride = cfg.colCount;
            bool lk    = _faces[fi].locked[ci];
            OnTileHover(fi, col, row, ci, lk);
        }
        else
        {
            SetStatusIdle();
        }
    }

    // ── 面背景標籤 ────────────────────────────────────────────────────────────

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
            rt.sizeDelta = new Vector2(cfg.TotalPixelHeight * s, cfg.TotalPixelWidth * s);
        }
        else
        {
            rt.anchoredPosition = new Vector2(
                 _controlGridOffsetX + cx * s,
                -(_controlGridOffsetY + cy * s)
            );
            rt.sizeDelta = new Vector2(cfg.TotalPixelWidth * s, cfg.TotalPixelHeight * s);
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

    // ── 定位靜態方法（供 VirtualCellManager 使用）────────────────────────────

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
}
