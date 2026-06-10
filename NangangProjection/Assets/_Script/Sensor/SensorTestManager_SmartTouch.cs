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
    public float tapBoundaryMs = 120f;

    [Tooltip("超過此值（ms）未收到新封包視為手指離開（硬體 80ms 輪詢，建議 120ms = 1.5× 輪詢間隔）。")]
    public float touchEndTimeoutMs = 120f;

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
    readonly List<int> _expiredBuffer = new List<int>();
    readonly List<int> _dragKeyBuffer = new List<int>();

    // ── 核心邏輯（由 SensorTestManager.DispatchSensorCell 呼叫）──────────────

    /// <summary>每次 UDP 封包觸發一次，追蹤觸碰持續時間。</summary>
    void UpdateSmartTouch(int faceIdx, int cellIdx)
    {
        float now       = Time.time;
        float boundaryS = tapBoundaryMs / 1000f;
        int   key       = faceIdx * 100000 + cellIdx;

        if (!_smartTouches.ContainsKey(key))
        {
            // 按壓中移動：若鄰近（±1格）有按壓中的格子，把按壓狀態轉移到新格子，
            // 讓手指在同一晶片範圍內滑動時不中斷按壓（水流跟著移動）。
            if (TryTransferPressToCell(faceIdx, cellIdx))
                return;

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

            // 第一次踩到：立刻亮燈，不等手勢解算
            _tileGrid?.SetActive(faceIdx, cellIdx);
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

    /// <summary>
    /// 若 cellIdx 的鄰近（±1格）存在「按壓中」的格子，把按壓狀態轉移到 cellIdx。
    /// 效果：手指在晶片邊界滑動時，按壓不中斷，水流跟著移動。
    /// 回傳 true 代表已轉移（呼叫端直接 return）。
    /// </summary>
    bool TryTransferPressToCell(int faceIdx, int cellIdx)
    {
        int colCount = _faces[faceIdx].config.colCount;
        int col = cellIdx % colCount;
        int row = cellIdx / colCount;

        int             fromKey   = -1;
        SmartTouchState fromState = default;

        foreach (var kv in _smartTouches)
        {
            var st = kv.Value;
            if (st.faceIdx != faceIdx || !st.pressStarted) continue;
            int pCol = st.cellIdx % colCount;
            int pRow = st.cellIdx / colCount;
            if (Mathf.Abs(col - pCol) > 1 || Mathf.Abs(row - pRow) > 1) continue;
            fromKey   = kv.Key;
            fromState = st;
            break;
        }

        if (fromKey < 0)
        {
            // 記錄：有新格子但找不到鄰近按壓格，說明 TryTransfer 沒生效
            if (_logRecording && _smartTouches.Count > 0)
                LogDiagEvent($"TransferPress 找不到鄰近按壓格  新格=({col},{row})  目前SmartTouches={_smartTouches.Count}個");
            return false;
        }

        if (_logRecording)
            LogDiagEvent($"TransferPress 成功  ({fromState.cellIdx % colCount},{fromState.cellIdx / colCount})→({col},{row})  按壓已持續={Time.time - fromState.startTime:F2}s");

        // 清除舊格子的橘燈，防止移動後殘影
        _tileGrid?.SetPressing(faceIdx, fromState.cellIdx, false);

        // 從舊 key 遷移到新 key，保留 pressStarted / startTime，更新 cellIdx 與 lastTouchTime
        int oldCellIdx = fromState.cellIdx;   // 保存舊 cellIdx，供 VCell 跨格判斷使用
        _smartTouches.Remove(fromKey);
        fromState.cellIdx       = cellIdx;
        fromState.lastTouchTime = Time.time;
        _smartTouches[faceIdx * 100000 + cellIdx] = fromState;

        // 廣播拖曳，讓 SensorBridgeToPassPosition 把水流位置更新到新格子
        if (_vcellManager != null && _vcellManager.Enabled)
        {
            int fromVCell = _vcellManager.GetVCellIdx(faceIdx, oldCellIdx);
            int toVCell   = _vcellManager.GetVCellIdx(faceIdx, cellIdx);
            if (fromVCell != toVCell &&
                _vcellManager.TryGetVCellCenter(faceIdx, cellIdx, out float cx, out float cy, out float cw, out float ch))
                SensorBus.CanvasDrag(cx, cy, cw, ch);
        }
        else
        {
            BroadcastCanvasCell(_faces[faceIdx], col, row, TouchKind.Drag);
            SensorBus.Drag(col, row);
        }
        return true;
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

        _expiredBuffer.Clear();
        foreach (var kv in _smartTouches)
            if (now - kv.Value.lastTouchTime > timeoutS)
                _expiredBuffer.Add(kv.Key);

        foreach (int key in _expiredBuffer)
        {
            var st = _smartTouches[key];
            _smartTouches.Remove(key);
            _jumpModeOccupied.Remove(key);   // 玩家抬腳，解鎖跳變

            if (st.faceIdx >= _faces.Count) continue;

            int expCol = st.cellIdx % _faces[st.faceIdx].config.colCount;
            int expRow = st.cellIdx / _faces[st.faceIdx].config.colCount;
            float silentMs = (now - st.lastTouchTime) * 1000f;
            float durationMs = (now - st.startTime) * 1000f;

            if (st.isDrag)
            {
                LogDiagEvent($"Drag 逾時結束  ({expCol},{expRow})  無封包={silentMs:F0}ms");
                _tileGrid?.SetPressing(st.faceIdx, st.cellIdx, false);
            }
            else if (st.pressStarted)
            {
                LogDiagEvent($"★Press 超時結束  ({expCol},{expRow})  無封包={silentMs:F0}ms  按壓持續={durationMs:F0}ms");
                FirePressEnd(st.faceIdx, st.cellIdx);
            }
            else
            {
                LogDiagEvent($"Tap 解算  ({expCol},{expRow})  持續={durationMs:F0}ms");
                FireTap(st.faceIdx, st.cellIdx);
            }
        }
    }

    // ── 事件觸發 ──────────────────────────────────────────────────────────────

    void FireTap(int faceIdx, int cellIdx)
    {
        var fd = _faces[faceIdx];
        if (fd.locked[cellIdx]) return;

        // 感應冷卻 debounce：與物理滑鼠路徑共用同一個 _chipLastTouchTimes 字典
        bool cooldownOn = _clickSim != null && _clickSim.cooldownEnabled;
        float debounceS = cooldownOn ? _clickSim.clickCooldown : 0f;
        if (debounceS > 0f)
        {
            int debounceKey = faceIdx * 100000 + cellIdx;
            float nowR = Time.realtimeSinceStartup;
            if (_chipLastTouchTimes.TryGetValue(debounceKey, out float lastT) && nowR - lastT <= debounceS)
                return;
            _chipLastTouchTimes[debounceKey] = nowR;
        }

        // 輕觸：解算時閃一次紅燈（視覺永遠執行）
        _tileGrid?.SetActive(faceIdx, cellIdx);

        int col = cellIdx % fd.config.colCount;
        int row = cellIdx / fd.config.colCount;
        if (_vcellManager != null && _vcellManager.Enabled)
        {
            // VCell 去重：同 VCell 還有其他晶片在追蹤 → 跳過，等最後一顆廣播
            if (VCellHasSiblingTouch(faceIdx, cellIdx)) return;
            if (_vcellManager.TryGetVCellCenter(faceIdx, cellIdx, out float cx, out float cy, out float cw, out float ch))
                SensorBus.CanvasTap(cx, cy, cw, ch);
        }
        else
        {
            BroadcastCanvasCell(fd, col, row, TouchKind.Tap);
            SensorBus.Tap(col, row);
        }
    }

    void FirePressStart(int faceIdx, int cellIdx)
    {
        var fd = _faces[faceIdx];
        if (fd.locked[cellIdx]) return;

        // 按壓開始：橘燈亮（常亮）（視覺永遠執行）
        _tileGrid?.SetPressing(faceIdx, cellIdx, true);

        int col = cellIdx % fd.config.colCount;
        int row = cellIdx / fd.config.colCount;
        if (_vcellManager != null && _vcellManager.Enabled)
        {
            // VCell 去重：同 VCell 已有其他晶片 pressStarted → 跳過，只讓第一顆廣播
            if (VCellHasSiblingPress(faceIdx, cellIdx)) return;
            if (_vcellManager.TryGetVCellCenter(faceIdx, cellIdx, out float cx, out float cy, out float cw, out float ch))
                SensorBus.CanvasPressStart(cx, cy, cw, ch);
        }
        else
        {
            BroadcastCanvasCell(fd, col, row, TouchKind.PressStart);
            SensorBus.PressStart(col, row);
        }
    }

    void FirePressEnd(int faceIdx, int cellIdx)
    {
        var fd = _faces[faceIdx];

        // 按壓結束：熄滅橘燈（視覺永遠執行）
        _tileGrid?.SetPressing(faceIdx, cellIdx, false);

        int col = cellIdx % fd.config.colCount;
        int row = cellIdx / fd.config.colCount;
        if (_vcellManager != null && _vcellManager.Enabled)
        {
            // VCell 去重：同 VCell 還有其他晶片未離手 → 跳過，等最後一顆廣播 PressEnd
            if (VCellHasSiblingTouch(faceIdx, cellIdx)) return;
            if (_vcellManager.TryGetVCellCenter(faceIdx, cellIdx, out float cx, out float cy, out float cw, out float ch))
                SensorBus.CanvasPressEnd(cx, cy, cw, ch);
        }
        else
        {
            BroadcastCanvasCell(fd, col, row, TouchKind.PressEnd);
            SensorBus.PressEnd(col, row);
        }

        // 清除同面所有殘留的拖曳格，防止按壓結束後橘燈永不熄滅
        ClearDragCells(faceIdx);
    }

    void ClearDragCells(int faceIdx)
    {
        _dragKeyBuffer.Clear();
        foreach (var kv in _smartTouches)
            if (kv.Value.isDrag && kv.Value.faceIdx == faceIdx)
                _dragKeyBuffer.Add(kv.Key);

        foreach (int k in _dragKeyBuffer)
        {
            var st = _smartTouches[k];
            _smartTouches.Remove(k);
            if (st.faceIdx >= _faces.Count) continue;
            _tileGrid?.SetPressing(st.faceIdx, st.cellIdx, false);
        }
    }

    void FireDrag(int faceIdx, int cellIdx)
    {
        var fd = _faces[faceIdx];
        if (fd.locked[cellIdx]) return;

        // 拖曳格：橘燈亮起（視覺永遠執行）
        _tileGrid?.SetPressing(faceIdx, cellIdx, true);

        int col = cellIdx % fd.config.colCount;
        int row = cellIdx / fd.config.colCount;
        if (_vcellManager != null && _vcellManager.Enabled)
        {
            // VCell 去重：同 VCell 已有錨點晶片 → 跳過（足部內部移動不重複廣播）
            // 若跨 VCell（錨點在不同 VCell），兄弟檢查 = false，正常廣播新 VCell 位置
            if (VCellHasSiblingTouch(faceIdx, cellIdx)) return;
            if (_vcellManager.TryGetVCellCenter(faceIdx, cellIdx, out float cx, out float cy, out float cw, out float ch))
                SensorBus.CanvasDrag(cx, cy, cw, ch);
        }
        else
        {
            BroadcastCanvasCell(fd, col, row, TouchKind.Drag);
            SensorBus.Drag(col, row);
        }
    }

    // ── VCell 廣播去重 helper ─────────────────────────────────────────────────

    /// <summary>
    /// VCell 模式：_smartTouches 中是否還有同 VCell 的「其他」晶片。
    /// 用於 FireTap / FirePressEnd / FireDrag 的廣播去重——只讓最後一顆晶片廣播。
    /// 注意：呼叫前當前晶片已從 _smartTouches 移除（FlushExpiredSmartTouches 路徑）。
    /// FireDrag 路徑中當前晶片仍在 _smartTouches，故用 cellIdx == 自己 過濾。
    /// </summary>
    bool VCellHasSiblingTouch(int faceIdx, int cellIdx)
    {
        if (_vcellManager == null || !_vcellManager.Enabled) return false;
        int myVCell = _vcellManager.GetVCellIdx(faceIdx, cellIdx);
        if (myVCell < 0) return false;
        foreach (var kv in _smartTouches)
        {
            var st = kv.Value;
            if (st.faceIdx != faceIdx || st.cellIdx == cellIdx) continue;
            if (_vcellManager.GetVCellIdx(faceIdx, st.cellIdx) == myVCell) return true;
        }
        return false;
    }

    /// <summary>
    /// VCell 模式：_smartTouches 中是否有同 VCell 的「其他」晶片已 pressStarted=true。
    /// 用於 FirePressStart 去重——只讓第一顆達到 Press 門檻的晶片廣播。
    /// </summary>
    bool VCellHasSiblingPress(int faceIdx, int cellIdx)
    {
        if (_vcellManager == null || !_vcellManager.Enabled) return false;
        int myVCell = _vcellManager.GetVCellIdx(faceIdx, cellIdx);
        if (myVCell < 0) return false;
        foreach (var kv in _smartTouches)
        {
            var st = kv.Value;
            if (st.faceIdx != faceIdx || st.cellIdx == cellIdx || !st.pressStarted) continue;
            if (_vcellManager.GetVCellIdx(faceIdx, st.cellIdx) == myVCell) return true;
        }
        return false;
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
