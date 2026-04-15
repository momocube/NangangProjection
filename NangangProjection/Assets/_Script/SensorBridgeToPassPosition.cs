using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 橋接 SensorBus 的觸碰事件到 PassPositionToMaterial。
/// 掛在與 PassPositionToMaterial 相同的 GameObject（Touch 物件）。
///
/// 資料流：
///   SensorBus.OnTouch(col, row)
///       → 換算成螢幕像素座標
///       → 固定 20 格 slot 管理（永不 Add/Remove，避免 index drift）
///       → 寫入 pptm.inputPosition[0..19]
///       → PassPositionToMaterial → Shader → VFX 水波
///
/// Camera.main 問題的解法：
///   PassPositionToMaterial.ConvertScreenToWorldPos() 內部用 Camera.main。
///   為確保它取到本場景的相機（而非 sensor_test 的相機），
///   sensor_test 的 Main Camera tag 已改為 Untagged，
///   只保留遊戲場景相機的 MainCamera tag。
/// </summary>
[RequireComponent(typeof(PassPositionToMaterial))]
public class SensorBridgeToPassPosition : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("格子解析度（需與 SensorTestManager 一致）")]
    public int gridCols = 128;
    public int gridRows = 24;

    [Header("Display 解析度（Display 2，投影地板）")]
    public int displayWidth  = 2048;
    public int displayHeight = 768;

    [Header("觸碰追蹤")]
    [Tooltip("超過此秒數無更新，視為離開（秒）")]
    public float touchTimeout = 0.3f;
    [Tooltip("最近鄰配對半徑（格子單位）")]
    public float matchRadiusCells = 3f;

    // ── 內部狀態 ────────────────────────────────────────────────────────────
    private const int   MAX_SLOTS = 20;
    private const float PARK_X    = -9999f;
    private const float PARK_Y    = -9999f;

    private struct TouchSlot
    {
        public bool    active;
        public float   lastSeen;
        public int     col, row;       // 格子座標
        public Vector2 screenPos;      // 螢幕像素座標
    }

    private PassPositionToMaterial _pptm;
    private TouchSlot[]   _slots       = new TouchSlot[MAX_SLOTS];
    private List<Vector2Int> _frameHits = new List<Vector2Int>(MAX_SLOTS);

    // ── Lifecycle ───────────────────────────────────────────────────────────

    private void Awake()
    {
        _pptm = GetComponent<PassPositionToMaterial>();
    }

    private void Start()
    {
        _pptm.inputPosDetect = true;
        _pptm.testByMouse    = false;

        // 預填 20 個停車格（w=0, xy=PARK），之後只更新 xy，永不 Add/Remove
        _pptm.inputPosition.Clear();
        for (int i = 0; i < MAX_SLOTS; i++)
            _pptm.inputPosition.Add(new Vector4(PARK_X, PARK_Y, 0f, 0f));

        SensorBus.OnTouch += OnSensorTouch;
    }

    private void OnDestroy()
    {
        SensorBus.OnTouch -= OnSensorTouch;
    }

    // ── 事件接收 ────────────────────────────────────────────────────────────

    private void OnSensorTouch(int col, int row)
    {
        _frameHits.Add(new Vector2Int(col, row));
    }

    // ── 每幀更新 ────────────────────────────────────────────────────────────

    private void Update()
    {
        MatchAndUpdate();
        FlushToPPTM();
        _frameHits.Clear();
    }

    private void MatchAndUpdate()
    {
        float now       = Time.time;
        int   hitCount  = _frameHits.Count;
        bool[] hitUsed  = new bool[hitCount];

        // Step 1：現有 active slot 找最近的未使用 hit
        for (int s = 0; s < MAX_SLOTS; s++)
        {
            if (!_slots[s].active) continue;

            int   best     = -1;
            float bestDist = matchRadiusCells * matchRadiusCells;

            for (int h = 0; h < hitCount; h++)
            {
                if (hitUsed[h]) continue;
                float dc = _frameHits[h].x - _slots[s].col;
                float dr = _frameHits[h].y - _slots[s].row;
                float d  = dc * dc + dr * dr;
                if (d < bestDist) { bestDist = d; best = h; }
            }

            if (best >= 0)
            {
                hitUsed[best]      = true;
                _slots[s].col      = _frameHits[best].x;
                _slots[s].row      = _frameHits[best].y;
                _slots[s].screenPos = ColRowToScreen(_slots[s].col, _slots[s].row);
                _slots[s].lastSeen  = now;
            }
            else if (now - _slots[s].lastSeen > touchTimeout)
            {
                _slots[s].active = false;
            }
        }

        // Step 2：剩餘未配對的 hit → 開新 slot
        for (int h = 0; h < hitCount; h++)
        {
            if (hitUsed[h]) continue;
            int free = FindFreeSlot();
            if (free < 0) break;

            _slots[free].active   = true;
            _slots[free].lastSeen = now;
            _slots[free].col      = _frameHits[h].x;
            _slots[free].row      = _frameHits[h].y;
            _slots[free].screenPos = ColRowToScreen(_frameHits[h].x, _frameHits[h].y);
        }
    }

    private void FlushToPPTM()
    {
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            _pptm.inputPosition[i] = _slots[i].active
                ? new Vector4(_slots[i].screenPos.x, _slots[i].screenPos.y, 0f, 0f)
                : new Vector4(PARK_X, PARK_Y, 0f, 0f);
        }
    }

    // ── 工具 ────────────────────────────────────────────────────────────────

    /// <summary>格子座標 → 螢幕像素座標，預補償 PPTM 的 Screen.height 翻轉。</summary>
    /// <remarks>
    /// PassPositionToMaterial.ConvertScreenToWorldPos() 內部做：
    ///     unity_y = Screen.height - our_sy
    /// 但 Screen.height 回傳 Display 0 高度（例如 1080），不是 Display 1 的 768。
    /// 若直接傳 sy=0..768，換算後只有上半部有效果。
    ///
    /// 反推正確的 our_sy：
    ///     目標 unity_y = (1 - rowRatio) * displayHeight   （row=0 → 頂部）
    ///     our_sy = Screen.height - 目標 unity_y
    /// </remarks>
    private Vector2 ColRowToScreen(int col, int row)
    {
        float sx = (col + 0.5f) / gridCols * displayWidth;

        float rowRatio = (row + 0.5f) / gridRows;
        float targetUnityY = (1f - rowRatio) * displayHeight;   // row=0 → displayHeight（頂部）
        float sy = Screen.height - targetUnityY;                 // 補償 PPTM 的 Screen.height flip

        return new Vector2(sx, sy);
    }

    private int FindFreeSlot()
    {
        for (int i = 0; i < MAX_SLOTS; i++)
            if (!_slots[i].active) return i;
        return -1;
    }
}
