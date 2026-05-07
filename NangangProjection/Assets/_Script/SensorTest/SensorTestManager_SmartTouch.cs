using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 輕觸 / 按壓辨識（partial）。
/// 每次 UDP 封包觸發 UpdateSmartTouch，Update 掃描逾時觸碰後解算為 Tap 或 Press。
/// 不再每次 UDP 閃紅格子；改為 Tap 解算時閃紅、Press 開始時橘燈亮。
/// </summary>
public partial class SensorTestManager
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("輕觸 / 按壓辨識")]
    [Tooltip("持續觸碰超過此值（ms）視為按壓，否則視為輕觸。")]
    public float tapBoundaryMs = 200f;

    [Tooltip("超過此值（ms）未收到新封包視為手指離開（硬體 100ms 輪詢，建議 140~160ms）。")]
    public float touchEndTimeoutMs = 150f;

    // ── UI 狀態 ───────────────────────────────────────────────────────────────

    TextMeshProUGUI _tapToggleBtnText;
    Image           _tapToggleBtnImage;
    TextMeshProUGUI _pressToggleBtnText;
    Image           _pressToggleBtnImage;
    TextMeshProUGUI _dragToggleBtnText;
    Image           _dragToggleBtnImage;
    TextMeshProUGUI _tapBoundaryText;

    // ── 觸碰狀態 ──────────────────────────────────────────────────────────────

    struct SmartTouchState
    {
        public float startTime, lastTouchTime;
        public bool  pressStarted;
        public bool  isDrag;
        public int   faceIdx, cellIdx;
    }

    readonly Dictionary<int, SmartTouchState> _smartTouches = new Dictionary<int, SmartTouchState>();

    // ── 核心邏輯（由 OnUdpPacket 呼叫）───────────────────────────────────────

    /// <summary>每次 UDP 封包觸發一次，追蹤觸碰持續時間。</summary>
    void UpdateSmartTouch(int faceIdx, int cellIdx)
    {
        float now       = Time.time;
        float boundaryS = tapBoundaryMs / 1000f;
        int   key       = faceIdx * 100000 + cellIdx;

        if (!_smartTouches.ContainsKey(key))
        {
            if (IsAdjacentToAnyTouch(faceIdx, cellIdx))
            {
                _smartTouches[key] = new SmartTouchState
                {
                    startTime     = now,
                    lastTouchTime = now,
                    isDrag        = true,
                    faceIdx       = faceIdx,
                    cellIdx       = cellIdx
                };
                FireDrag(faceIdx, cellIdx);
                return;
            }

            _smartTouches[key] = new SmartTouchState
            {
                startTime     = now,
                lastTouchTime = now,
                pressStarted  = false,
                faceIdx       = faceIdx,
                cellIdx       = cellIdx
            };
            return;
        }

        var state = _smartTouches[key];
        state.lastTouchTime = now;

        if (!state.pressStarted && !state.isDrag && (now - state.startTime) >= boundaryS)
        {
            state.pressStarted = true;
            _smartTouches[key] = state;
            FirePressStart(faceIdx, cellIdx);
            return;
        }

        _smartTouches[key] = state;
    }

    bool IsAdjacentToAnyTouch(int faceIdx, int cellIdx)
    {
        int colCount = _faces[faceIdx].config.colCount;
        int col = cellIdx % colCount;
        int row = cellIdx / colCount;

        foreach (var kv in _smartTouches)
        {
            var st = kv.Value;
            if (st.faceIdx != faceIdx) continue;
            // 純輕觸（尚未升為 press 也非 drag）不作為拖曳錨點
            if (!st.pressStarted && !st.isDrag) continue;
            int pCol = st.cellIdx % colCount;
            int pRow = st.cellIdx / colCount;
            if (Mathf.Abs(col - pCol) <= 2 && Mathf.Abs(row - pRow) <= 1)
                return true;
        }
        return false;
    }

    /// <summary>每幀掃描逾時觸碰，解算為 Tap 或 PressEnd。</summary>
    void FlushExpiredSmartTouches()
    {
        if (_smartTouches.Count == 0) return;

        float now      = Time.time;
        float timeoutS = touchEndTimeoutMs / 1000f;

        var expired = new List<int>();
        foreach (var kv in _smartTouches)
            if (now - kv.Value.lastTouchTime > timeoutS)
                expired.Add(kv.Key);

        foreach (int key in expired)
        {
            var st = _smartTouches[key];
            _smartTouches.Remove(key);

            if (st.faceIdx >= _faces.Count) continue;

            if (st.isDrag)
            {
                // 拖曳格逾時：熄滅橘燈
                var fd2 = _faces[st.faceIdx];
                fd2.controlTiles[st.cellIdx]?.SetPressState(false);
                if (_gameTileVisible)
                    fd2.sensorTiles[st.cellIdx]?.SetPressState(false);
            }
            else if (st.pressStarted)
                FirePressEnd(st.faceIdx, st.cellIdx);
            else
                FireTap(st.faceIdx, st.cellIdx);
        }
    }

    // ── 事件觸發 ──────────────────────────────────────────────────────────────

    void FireTap(int faceIdx, int cellIdx)
    {
        var fd = _faces[faceIdx];
        if (fd.locked[cellIdx]) return;

        // 輕觸：解算時閃一次紅燈
        fd.controlTiles[cellIdx]?.Activate();
        if (_gameTileVisible)
            fd.sensorTiles[cellIdx]?.Activate();

        int col = cellIdx % fd.config.colCount;
        int row = cellIdx / fd.config.colCount;
        BroadcastCanvasCell(fd, col, row, TouchKind.Tap);
        SensorBus.Tap(col, row);
    }

    void FirePressStart(int faceIdx, int cellIdx)
    {
        var fd = _faces[faceIdx];
        if (fd.locked[cellIdx]) return;

        // 按壓開始：橘燈亮（常亮）
        fd.controlTiles[cellIdx]?.SetPressState(true);
        if (_gameTileVisible)
            fd.sensorTiles[cellIdx]?.SetPressState(true);

        int col = cellIdx % fd.config.colCount;
        int row = cellIdx / fd.config.colCount;
        BroadcastCanvasCell(fd, col, row, TouchKind.PressStart);
        SensorBus.PressStart(col, row);
    }

    void FirePressEnd(int faceIdx, int cellIdx)
    {
        var fd = _faces[faceIdx];

        // 按壓結束：熄滅橘燈
        fd.controlTiles[cellIdx]?.SetPressState(false);
        if (_gameTileVisible)
            fd.sensorTiles[cellIdx]?.SetPressState(false);

        int col = cellIdx % fd.config.colCount;
        int row = cellIdx / fd.config.colCount;
        BroadcastCanvasCell(fd, col, row, TouchKind.PressEnd);
        SensorBus.PressEnd(col, row);

        // 清除同面所有殘留的拖曳格，防止按壓結束後橘燈永不熄滅
        ClearDragCells(faceIdx);
    }

    void ClearDragCells(int faceIdx)
    {
        var dragKeys = new System.Collections.Generic.List<int>();
        foreach (var kv in _smartTouches)
            if (kv.Value.isDrag && kv.Value.faceIdx == faceIdx)
                dragKeys.Add(kv.Key);

        foreach (int k in dragKeys)
        {
            var st = _smartTouches[k];
            _smartTouches.Remove(k);
            if (st.faceIdx >= _faces.Count) continue;
            var fd2 = _faces[st.faceIdx];
            fd2.controlTiles[st.cellIdx]?.SetPressState(false);
            if (_gameTileVisible)
                fd2.sensorTiles[st.cellIdx]?.SetPressState(false);
        }
    }

    void FireDrag(int faceIdx, int cellIdx)
    {
        var fd = _faces[faceIdx];
        if (fd.locked[cellIdx]) return;

        // 拖曳格：橘燈亮起（與按壓視覺相同，表示手滑過）
        fd.controlTiles[cellIdx]?.SetPressState(true);
        if (_gameTileVisible)
            fd.sensorTiles[cellIdx]?.SetPressState(true);

        int col = cellIdx % fd.config.colCount;
        int row = cellIdx / fd.config.colCount;
        BroadcastCanvasCell(fd, col, row, TouchKind.Drag);
        SensorBus.Drag(col, row);
    }

    // ── UI 建立 ───────────────────────────────────────────────────────────────

    void BuildGestureBar(RectTransform parent, TMP_FontAsset font)
    {
        // ── Section 1：點擊勾選 (0 → 0.16) ────────────────────────────────────
        _tapToggleBtnText =
            CreateButton(parent, "BtnTapToggle", "點擊 ON", OnToggleTap, 0.000f, 0.160f, font);
        _tapToggleBtnImage = _tapToggleBtnText.transform.parent.GetComponent<Image>();

        // ── Section 2：按壓勾選 (0.16 → 0.32) ─────────────────────────────────
        _pressToggleBtnText =
            CreateButton(parent, "BtnPressToggle", "按壓 ON", OnTogglePress, 0.160f, 0.320f, font);
        _pressToggleBtnImage = _pressToggleBtnText.transform.parent.GetComponent<Image>();

        // ── Section 3：拖曳勾選 (0.32 → 0.46) ────────────────────────────────
        _dragToggleBtnText =
            CreateButton(parent, "BtnDragToggle", "拖曳 ON", OnToggleDrag, 0.320f, 0.460f, font);
        _dragToggleBtnImage = _dragToggleBtnText.transform.parent.GetComponent<Image>();

        // ── 分隔線 ─────────────────────────────────────────────────────────────
        var div = CreatePanel(parent, "GestureDiv", new Color(0.30f, 0.32f, 0.40f),
            new Vector2(0.462f, 0.1f), new Vector2(0.468f, 0.9f), Vector2.zero, Vector2.zero);
        div.GetComponent<Image>().raycastTarget = false;

        // ── Section 4：按壓判定 ms (0.47 → 0.74) ──────────────────────────────
        var boundaryLabel = CreatePanel(parent, "TapBoundaryLabel", Color.clear,
            new Vector2(0.470f, 0f), new Vector2(0.560f, 1f), Vector2.zero, Vector2.zero);
        boundaryLabel.GetComponent<Image>().raycastTarget = false;
        AddText(boundaryLabel.transform, "LabelText", "按壓判定",
            13, TextAlignmentOptions.MidlineRight, new Color(0.60f, 0.65f, 0.75f), font);

        CreateButton(parent, "BtnTapBoundaryDec", "-", OnDecreaseTapBoundary, 0.560f, 0.605f, font);

        var boundaryValPanel = CreatePanel(parent, "TapBoundaryValue", new Color(0.10f, 0.12f, 0.16f),
            new Vector2(0.605f, 0.1f), new Vector2(0.700f, 0.9f), Vector2.zero, Vector2.zero);
        boundaryValPanel.GetComponent<Image>().raycastTarget = false;
        _tapBoundaryText = AddText(boundaryValPanel.transform, "ValueText",
            $"{tapBoundaryMs:F0}ms",
            13, TextAlignmentOptions.Midline, new Color(0.95f, 0.95f, 1.00f), font);

        CreateButton(parent, "BtnTapBoundaryInc", "+", OnIncreaseTapBoundary, 0.700f, 0.740f, font);

        // ── Section 5：孤點偵測模式 + 忽略次數 (0.75 → 1.0) ──────────────────
        var ghostDiv = CreatePanel(parent, "GhostDiv", new Color(0.30f, 0.32f, 0.40f),
            new Vector2(0.742f, 0.1f), new Vector2(0.748f, 0.9f), Vector2.zero, Vector2.zero);
        ghostDiv.GetComponent<Image>().raycastTarget = false;

        _ghostModeBtnText  = CreateButton(parent, "BtnGhostMode", GhostModeLabel, CycleGhostMode, 0.750f, 0.860f, font);
        _ghostModeBtnImage = _ghostModeBtnText.transform.parent.GetComponent<Image>();

        CreateButton(parent, "BtnGhostCountDec", "-", OnDecreaseGhostCount, 0.862f, 0.895f, font);

        var gcValPanel = CreatePanel(parent, "GhostCountValue", new Color(0.10f, 0.12f, 0.16f),
            new Vector2(0.895f, 0.1f), new Vector2(0.960f, 0.9f), Vector2.zero, Vector2.zero);
        gcValPanel.GetComponent<Image>().raycastTarget = false;
        _ghostCountText = AddText(gcValPanel.transform, "GCVal", $"{ghostConfirmCount}次",
            13, TextAlignmentOptions.Midline, new Color(0.95f, 0.95f, 1.00f), font);

        CreateButton(parent, "BtnGhostCountInc", "+", OnIncreaseGhostCount, 0.960f, 1.000f, font);

        UpdateGestureBar();
        UpdateTapBoundaryText();
        UpdateGhostModeButton();
        UpdateGhostCountText();
    }

    // ── 按鈕回呼 ──────────────────────────────────────────────────────────────

    void OnToggleTap()
    {
        if (_clickSim == null) return;
        _clickSim.tapClickEnabled = !_clickSim.tapClickEnabled;
        UpdateGestureBar();
    }

    void OnTogglePress()
    {
        if (_clickSim == null) return;
        _clickSim.pressClickEnabled = !_clickSim.pressClickEnabled;
        UpdateGestureBar();
    }

    void OnToggleDrag()
    {
        if (_clickSim == null) return;
        _clickSim.dragClickEnabled = !_clickSim.dragClickEnabled;
        UpdateGestureBar();
    }

    void OnDecreaseTapBoundary()
    {
        tapBoundaryMs = Mathf.Max(50f, tapBoundaryMs - 10f);
        UpdateTapBoundaryText();
    }

    void OnIncreaseTapBoundary()
    {
        tapBoundaryMs = Mathf.Min(1000f, tapBoundaryMs + 10f);
        UpdateTapBoundaryText();
    }

    // ── 更新顯示 ──────────────────────────────────────────────────────────────

    public void UpdateGestureBar()
    {
        if (_tapToggleBtnText != null)
        {
            bool on = _clickSim == null || _clickSim.tapClickEnabled;
            _tapToggleBtnText.text = on ? "點擊 ON" : "點擊";
            if (_tapToggleBtnImage != null)
                _tapToggleBtnImage.color = on
                    ? new Color(0.15f, 0.35f, 0.20f)
                    : new Color(0.15f, 0.18f, 0.25f);
        }

        if (_pressToggleBtnText != null)
        {
            bool on = _clickSim == null || _clickSim.pressClickEnabled;
            _pressToggleBtnText.text = on ? "按壓 ON" : "按壓";
            if (_pressToggleBtnImage != null)
                _pressToggleBtnImage.color = on
                    ? new Color(0.15f, 0.35f, 0.20f)
                    : new Color(0.15f, 0.18f, 0.25f);
        }

        if (_dragToggleBtnText != null)
        {
            bool on = _clickSim == null || _clickSim.dragClickEnabled;
            _dragToggleBtnText.text = on ? "拖曳 ON" : "拖曳";
            if (_dragToggleBtnImage != null)
                _dragToggleBtnImage.color = on
                    ? new Color(0.15f, 0.35f, 0.20f)
                    : new Color(0.15f, 0.18f, 0.25f);
        }
    }

    void UpdateTapBoundaryText()
    {
        if (_tapBoundaryText != null)
            _tapBoundaryText.text = $"{tapBoundaryMs:F0}ms";
    }
}
