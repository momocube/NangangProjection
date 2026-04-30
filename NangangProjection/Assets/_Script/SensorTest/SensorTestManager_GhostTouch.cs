using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum GhostTouchMode { None, Auto, Strict }

public partial class SensorTestManager
{
    [Header("孤點偵測（Ghost Touch）")]
    public GhostTouchMode ghostTouchMode     = GhostTouchMode.None;
    public float          ghostTouchWindowMs = 300f;  // x：每格判定窗口（ms）
    public int            ghostNeighborCols  = 3;
    public int            ghostNeighborRows  = 2;
    public int            ghostConfirmCount  = 2;     // y：灰白格後需再觸發幾次才確認

    struct PendingCellState
    {
        public float firstSeenTime;
        public int   hitCount;     // 起始為 1（灰白格本身）
        public int   faceIdx;
        public int   cellIdx;
    }

    readonly Dictionary<int, PendingCellState> _pendingCells = new Dictionary<int, PendingCellState>();

    TextMeshProUGUI _ghostModeBtnText;
    Image           _ghostModeBtnImage;
    TextMeshProUGUI _ghostCountText;

    string GhostModeLabel => ghostTouchMode switch
    {
        GhostTouchMode.Auto   => "孤點偵測 自動",
        GhostTouchMode.Strict => "孤點偵測 嚴格",
        _                     => "孤點偵測 OFF",
    };

    Color GhostModeColor => ghostTouchMode switch
    {
        GhostTouchMode.Auto   => new Color(0.15f, 0.40f, 0.35f),
        GhostTouchMode.Strict => new Color(0.40f, 0.25f, 0.10f),
        _                     => new Color(0.15f, 0.18f, 0.25f),
    };

    // ── 核心邏輯 ──────────────────────────────────────────────────────────────

    // 回傳 true = 允許觸發（確認為真實觸碰，或模式為 None）
    bool ProcessGhostTouch(int faceIdx, int cellIdx)
    {
        if (ghostTouchMode == GhostTouchMode.None) return true;

        int   key    = faceIdx * 100000 + cellIdx;
        float now    = Time.realtimeSinceStartup;
        float expiry = ghostTouchWindowMs * ghostConfirmCount / 1000f;

        if (_pendingCells.TryGetValue(key, out var state))
        {
            if (now - state.firstSeenTime <= expiry)
            {
                state.hitCount++;
                _pendingCells[key] = state;

                bool confirmed = ghostTouchMode == GhostTouchMode.Auto
                    ? state.hitCount > ghostConfirmCount
                    : state.hitCount > ghostConfirmCount && HasPendingNeighbor(faceIdx, cellIdx);

                if (confirmed)
                {
                    SetCellPendingVisual(faceIdx, cellIdx, false);
                    _pendingCells.Remove(key);
                    return true;
                }
                return false;
            }
            else
            {
                // 窗口過期：重設為灰白格起點
                state.firstSeenTime = now;
                state.hitCount      = 1;
                _pendingCells[key]  = state;
                SetCellPendingVisual(faceIdx, cellIdx, true);
                return false;
            }
        }
        else
        {
            _pendingCells[key] = new PendingCellState
            {
                firstSeenTime = now,
                hitCount      = 1,
                faceIdx       = faceIdx,
                cellIdx       = cellIdx,
            };
            SetCellPendingVisual(faceIdx, cellIdx, true);
            return false;
        }
    }

    bool HasPendingNeighbor(int faceIdx, int cellIdx)
    {
        var fd     = _faces[faceIdx];
        int stride = fd.config.colCount;
        int col    = cellIdx % stride;
        int row    = cellIdx / stride;

        for (int dc = -ghostNeighborCols; dc <= ghostNeighborCols; dc++)
        for (int dr = -ghostNeighborRows; dr <= ghostNeighborRows; dr++)
        {
            if (dc == 0 && dr == 0) continue;
            int nc = col + dc, nr = row + dr;
            if (nc < 0 || nc >= stride || nr < 0 || nr >= fd.config.rowCount) continue;
            if (_pendingCells.ContainsKey(faceIdx * 100000 + nr * stride + nc)) return true;
        }
        return false;
    }

    void SetCellPendingVisual(int faceIdx, int cellIdx, bool pending)
    {
        var fd = _faces[faceIdx];
        fd.sensorTiles[cellIdx]?.SetPendingState(pending);
        fd.controlTiles[cellIdx]?.SetPendingState(pending);
    }

    void CleanExpiredPendingCells()
    {
        if (ghostTouchMode == GhostTouchMode.None || _pendingCells.Count == 0) return;
        float now    = Time.realtimeSinceStartup;
        float expiry = ghostTouchWindowMs * ghostConfirmCount / 1000f;
        var   remove = new List<int>();
        foreach (var kv in _pendingCells)
            if (now - kv.Value.firstSeenTime > expiry) remove.Add(kv.Key);
        foreach (var k in remove)
        {
            var s = _pendingCells[k];
            SetCellPendingVisual(s.faceIdx, s.cellIdx, false);
            _pendingCells.Remove(k);
        }
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    void CycleGhostMode()
    {
        ghostTouchMode = ghostTouchMode switch
        {
            GhostTouchMode.None   => GhostTouchMode.Auto,
            GhostTouchMode.Auto   => GhostTouchMode.Strict,
            GhostTouchMode.Strict => GhostTouchMode.None,
            _                     => GhostTouchMode.None,
        };
        _pendingCells.Clear();
        UpdateGhostModeButton();
    }

    void OnDecreaseGhostCount()
    {
        ghostConfirmCount = Mathf.Max(1, ghostConfirmCount - 1);
        _pendingCells.Clear();
        UpdateGhostCountText();
    }

    void OnIncreaseGhostCount()
    {
        ghostConfirmCount = Mathf.Min(10, ghostConfirmCount + 1);
        _pendingCells.Clear();
        UpdateGhostCountText();
    }

    void UpdateGhostModeButton()
    {
        if (_ghostModeBtnText  != null) _ghostModeBtnText.text   = GhostModeLabel;
        if (_ghostModeBtnImage != null) _ghostModeBtnImage.color = GhostModeColor;
    }

    void UpdateGhostCountText()
    {
        if (_ghostCountText != null) _ghostCountText.text = $"{ghostConfirmCount}次";
    }
}
