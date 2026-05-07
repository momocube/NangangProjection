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
    [Tooltip("按壓結束後淡出持續秒數（須 ≥ radius / 0.8 ≈ 0.5s）")]
    public float pressReleaseHoldSeconds = 0.7f;

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
    private TouchSlot[]   _slots        = new TouchSlot[MAX_SLOTS];
    private bool[]        _pressActive  = new bool[MAX_SLOTS];   // 此 slot 是否處於按壓狀態
    private bool[]        _fadingOut    = new bool[MAX_SLOTS];   // 此 slot 是否正在淡出
    private float[]       _fadeEndTime  = new float[MAX_SLOTS];  // 淡出結束時間
    private List<Vector2Int> _frameHits = new List<Vector2Int>(MAX_SLOTS);
    private int _lastCanvasEventFrame = -1;
    private bool _processingCanvasEvent;

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

        SensorBus.OnTouch      += OnSensorTouch;
        SensorBus.OnPressStart += OnSensorPressStart;
        SensorBus.OnPressEnd   += OnSensorPressEnd;
        SensorBus.OnDrag       += OnSensorDrag;
        SensorBus.OnCanvasTouch      += OnSensorCanvasTouch;
        SensorBus.OnCanvasPressStart += OnSensorCanvasPressStart;
        SensorBus.OnCanvasPressEnd   += OnSensorCanvasPressEnd;
        SensorBus.OnCanvasDrag       += OnSensorCanvasDrag;
    }

    private void OnDestroy()
    {
        SensorBus.OnTouch      -= OnSensorTouch;
        SensorBus.OnPressStart -= OnSensorPressStart;
        SensorBus.OnPressEnd   -= OnSensorPressEnd;
        SensorBus.OnDrag       -= OnSensorDrag;
        SensorBus.OnCanvasTouch      -= OnSensorCanvasTouch;
        SensorBus.OnCanvasPressStart -= OnSensorCanvasPressStart;
        SensorBus.OnCanvasPressEnd   -= OnSensorCanvasPressEnd;
        SensorBus.OnCanvasDrag       -= OnSensorCanvasDrag;
    }

    // ── 事件接收 ────────────────────────────────────────────────────────────

    private void OnSensorTouch(int col, int row)
    {
        if (!_processingCanvasEvent && Time.frameCount == _lastCanvasEventFrame) return;
        _frameHits.Add(new Vector2Int(col, row));
    }

    private void OnSensorCanvasTouch(float x, float y, float canvasW, float canvasH)
    {
        _lastCanvasEventFrame = Time.frameCount;
        _frameHits.Add(CanvasToGridHit(x, y, canvasW, canvasH));
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
            else if (_fadingOut[s])
            {
                // 淡出中：等淡出時間結束才停用
                if (now >= _fadeEndTime[s])
                {
                    _slots[s].active = false;
                    _fadingOut[s]    = false;
                }
            }
            else if (!_pressActive[s] && now - _slots[s].lastSeen > touchTimeout)
            {
                // 按壓中的 slot 不逾時，等 PressEnd 才清除
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

        // 同步按壓 / 淡出狀態給 PPTM
        if (_pptm.slotPressHeld != null && _pptm.slotPressHeld.Length >= MAX_SLOTS)
            for (int i = 0; i < MAX_SLOTS; i++)
                _pptm.slotPressHeld[i] = _pressActive[i] && _slots[i].active;

        if (_pptm.slotFadingOut != null && _pptm.slotFadingOut.Length >= MAX_SLOTS)
            for (int i = 0; i < MAX_SLOTS; i++)
                _pptm.slotFadingOut[i] = _fadingOut[i] && _slots[i].active;
    }

    // ── 按壓 / 拖曳事件 ─────────────────────────────────────────────────────

    /// <summary>按壓開始：找到對應 slot 並標記為 pressActive，防止逾時失效。</summary>
    private void OnSensorPressStart(int col, int row)
    {
        if (!_processingCanvasEvent && Time.frameCount == _lastCanvasEventFrame) return;
        // 優先找已存在的 slot
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (_slots[i].active && _slots[i].col == col && _slots[i].row == row)
            {
                _pressActive[i] = true;
                return;
            }
        }
        // OnTouch 應該已建立 slot；若未建立（感應器只送一次封包），手動補建
        int free = FindFreeSlot();
        if (free < 0) return;
        _slots[free].active    = true;
        _slots[free].lastSeen  = Time.time;
        _slots[free].col       = col;
        _slots[free].row       = row;
        _slots[free].screenPos = ColRowToScreen(col, row);
        _pressActive[free]     = true;
    }

    private void OnSensorCanvasPressStart(float x, float y, float canvasW, float canvasH)
    {
        var hit = CanvasToGridHit(x, y, canvasW, canvasH);
        _processingCanvasEvent = true;
        OnSensorPressStart(hit.x, hit.y);
        _processingCanvasEvent = false;
        _lastCanvasEventFrame = Time.frameCount;
    }

    /// <summary>按壓結束：進入淡出狀態，slot 在 pressReleaseHoldSeconds 後停用。</summary>
    private void OnSensorPressEnd(int col, int row)
    {
        if (!_processingCanvasEvent && Time.frameCount == _lastCanvasEventFrame) return;
        float endTime = Time.time + pressReleaseHoldSeconds;
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (!_pressActive[i]) continue;
            _pressActive[i]  = false;
            _fadingOut[i]    = true;
            _fadeEndTime[i]  = endTime;
        }
    }

    private void OnSensorCanvasPressEnd(float x, float y, float canvasW, float canvasH)
    {
        var hit = CanvasToGridHit(x, y, canvasW, canvasH);
        _processingCanvasEvent = true;
        OnSensorPressEnd(hit.x, hit.y);
        _processingCanvasEvent = false;
        _lastCanvasEventFrame = Time.frameCount;
    }

    /// <summary>拖曳：將按壓 slot 的位置更新到新格子（暴度已開時效果跟著手移動）。</summary>
    private void OnSensorDrag(int col, int row)
    {
        if (!_processingCanvasEvent && Time.frameCount == _lastCanvasEventFrame) return;
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (!_pressActive[i] || !_slots[i].active) continue;
            _slots[i].col       = col;
            _slots[i].row       = row;
            _slots[i].screenPos = ColRowToScreen(col, row);
            _slots[i].lastSeen  = Time.time;
            return;  // 只更新第一個按壓 slot（單點觸碰）
        }
    }

    private void OnSensorCanvasDrag(float x, float y, float canvasW, float canvasH)
    {
        var hit = CanvasToGridHit(x, y, canvasW, canvasH);
        _processingCanvasEvent = true;
        OnSensorDrag(hit.x, hit.y);
        _processingCanvasEvent = false;
        _lastCanvasEventFrame = Time.frameCount;
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
        float screenW = Screen.width  > 0 ? Screen.width  : displayWidth;
        float screenH = Screen.height > 0 ? Screen.height : displayHeight;
        float sx = (col + 0.5f) / gridCols * screenW;

        float rowRatio = (row + 0.5f) / gridRows;
        float targetUnityY = (1f - rowRatio) * screenH;  // row=0 -> top
        float sy = screenH - targetUnityY;               // compensate PPTM's Screen.height flip

        return new Vector2(sx, sy);
    }

    private Vector2Int CanvasToGridHit(float x, float y, float canvasW, float canvasH)
    {
        if (canvasW <= 0f) canvasW = displayWidth;
        if (canvasH <= 0f) canvasH = displayHeight;

        int col = Mathf.Clamp(Mathf.FloorToInt((x / canvasW) * gridCols), 0, gridCols - 1);
        int row = Mathf.Clamp(Mathf.FloorToInt((y / canvasH) * gridRows), 0, gridRows - 1);
        return new Vector2Int(col, row);
    }

    private int FindFreeSlot()
    {
        for (int i = 0; i < MAX_SLOTS; i++)
            if (!_slots[i].active) return i;
        return -1;
    }
}
