using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 橋接 SensorBus 的觸碰事件到 PassPositionToMaterial。
/// 掛在與 PassPositionToMaterial 相同的 GameObject（Touch 物件）。
///
/// 設計原則（對齊現場感應器行為）：
///   每個觸碰點完全獨立。晶片感應到 → 水波出現；感應消失 → 水波淡出。
///   不區分「按壓」/ 「拖曳」/ 「尾跡」，因為現場硬體根本沒有這些概念：
///   每個晶片只會持續發出 Touch 封包，沒有 press/drag 之分。
///
///   滑鼠測試行為對應：
///   - 按住不動：HoldRoutine 每 0.05s 對目前 hover 格發出 Touch → 水波持續
///   - 按住滑動：HoldRoutine 跟著 hover 走 → 舊格逾時淡出，新格水波出現（排水效果）
///   - 放開：停止 Touch → touchTimeout 後淡出 → releaseHoldSeconds 後消失
///   - 多點觸碰：每個晶片獨立 slot，同時多個水波
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
    [Tooltip("超過此秒數無更新，視為離開，開始淡出計時（秒）")]
    public float touchTimeout = 0.3f;
    [Tooltip("最近鄰配對半徑（格子單位）；防止鄰近晶片觸發重複 slot")]
    public float matchRadiusCells = 3f;
    [Tooltip("觸碰結束後水波淡出持續秒數（0.7s ≈ 水波自然收縮時間）")]
    public float releaseHoldSeconds = 0.7f;

    [Header("位置平滑")]
    [Tooltip("水波位置插值速度（per second）；越高越跟手，越低越飄柔。0 = 關閉平滑")]
    public float positionSmoothSpeed = 12f;

    // ── 內部狀態 ────────────────────────────────────────────────────────────
    private const int   MAX_SLOTS = 20;
    private const float PARK_X    = -9999f;
    private const float PARK_Y    = -9999f;

    private struct TouchSlot
    {
        public bool    active;
        public float   lastSeen;
        public int     col, row;
        public Vector2 screenPos;
    }

    private PassPositionToMaterial _pptm;
    private TouchSlot[]   _slots       = new TouchSlot[MAX_SLOTS];
    private bool[]        _fadingOut   = new bool[MAX_SLOTS];
    private float[]       _fadeEndTime = new float[MAX_SLOTS];
    private Vector2[]     _smoothedPos = new Vector2[MAX_SLOTS];
    private List<Vector2Int> _frameHits = new List<Vector2Int>(MAX_SLOTS);
    private int _lastCanvasEventFrame = -1;

    // ── Lifecycle ───────────────────────────────────────────────────────────

    private void Awake()
    {
        _pptm = GetComponent<PassPositionToMaterial>();
    }

    private void Start()
    {
        _pptm.inputPosDetect = true;
        _pptm.testByMouse    = false;

        // 預填 20 個停車格，永不 Add/Remove
        _pptm.inputPosition.Clear();
        for (int i = 0; i < MAX_SLOTS; i++)
            _pptm.inputPosition.Add(new Vector4(PARK_X, PARK_Y, 0f, 0f));

        SensorBus.OnTouch       += OnSensorTouch;
        SensorBus.OnCanvasTouch += OnSensorCanvasTouch;
    }

    private void OnDestroy()
    {
        SensorBus.OnTouch       -= OnSensorTouch;
        SensorBus.OnCanvasTouch -= OnSensorCanvasTouch;
    }

    // ── 事件接收 ────────────────────────────────────────────────────────────

    private void OnSensorTouch(int col, int row)
    {
        // Canvas 精確事件在同幀已處理，跳過低精度 col/row 版本避免重複計算
        if (Time.frameCount == _lastCanvasEventFrame) return;
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
        SmoothPositions();
        FlushToPPTM();
        _frameHits.Clear();
    }

    private void SmoothPositions()
    {
        if (positionSmoothSpeed <= 0f) return;
        float t = Time.deltaTime * positionSmoothSpeed;
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (_slots[i].active)
                _smoothedPos[i] = Vector2.Lerp(_smoothedPos[i], _slots[i].screenPos, t);
        }
    }

    private void MatchAndUpdate()
    {
        float now      = Time.time;
        int   hitCount = _frameHits.Count;
        bool[] hitUsed = new bool[hitCount];

        // Step 1：現有 active slot 找最近的 hit
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
                hitUsed[best]       = true;
                _slots[s].col       = _frameHits[best].x;
                _slots[s].row       = _frameHits[best].y;
                _slots[s].screenPos = ColRowToScreen(_slots[s].col, _slots[s].row);
                _slots[s].lastSeen  = now;
                _fadingOut[s]       = false;   // 取消淡出（手重新觸碰此區域）
            }
            else if (_fadingOut[s])
            {
                // 淡出中：等結束時間才真正停用
                if (now >= _fadeEndTime[s])
                {
                    _slots[s].active = false;
                    _fadingOut[s]    = false;
                }
            }
            else if (now - _slots[s].lastSeen > touchTimeout)
            {
                // 逾時：開始淡出
                _fadingOut[s]   = true;
                _fadeEndTime[s] = now + releaseHoldSeconds;
            }
        }

        // Step 2：剩餘未配對的 hit → 開新 slot
        for (int h = 0; h < hitCount; h++)
        {
            if (hitUsed[h]) continue;

            // 已在 active slot 配對半徑內 → 不重複建立
            bool covered = false;
            for (int s = 0; s < MAX_SLOTS && !covered; s++)
            {
                if (!_slots[s].active) continue;
                float dc = _frameHits[h].x - _slots[s].col;
                float dr = _frameHits[h].y - _slots[s].row;
                if (dc * dc + dr * dr < matchRadiusCells * matchRadiusCells)
                    covered = true;
            }
            if (covered) continue;

            int free = FindFreeSlot();
            if (free < 0) break;

            _slots[free].active    = true;
            _slots[free].lastSeen  = now;
            _slots[free].col       = _frameHits[h].x;
            _slots[free].row       = _frameHits[h].y;
            _slots[free].screenPos = ColRowToScreen(_frameHits[h].x, _frameHits[h].y);
            _smoothedPos[free]     = _slots[free].screenPos;   // 新 slot 直接跳到起始位置
            _fadingOut[free]       = false;
        }
    }

    private void FlushToPPTM()
    {
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            Vector2 pos = (positionSmoothSpeed > 0f) ? _smoothedPos[i] : _slots[i].screenPos;
            _pptm.inputPosition[i] = _slots[i].active
                ? new Vector4(pos.x, pos.y, 0f, 0f)
                : new Vector4(PARK_X, PARK_Y, 0f, 0f);
        }

        // active 且非淡出 → holdOpen=true → PPTM 一律 expand（不管位置是否移動）
        if (_pptm.slotPressHeld != null && _pptm.slotPressHeld.Length >= MAX_SLOTS)
            for (int i = 0; i < MAX_SLOTS; i++)
                _pptm.slotPressHeld[i] = _slots[i].active && !_fadingOut[i];

        // 淡出中 → PPTM 以 fadeOutSpeed 縮小 radius
        if (_pptm.slotFadingOut != null && _pptm.slotFadingOut.Length >= MAX_SLOTS)
            for (int i = 0; i < MAX_SLOTS; i++)
                _pptm.slotFadingOut[i] = _fadingOut[i] && _slots[i].active;
    }

    // ── 工具 ────────────────────────────────────────────────────────────────

    /// <summary>格子座標 → 螢幕像素座標，預補償 PPTM 的 Screen.height 翻轉。</summary>
    /// <remarks>
    /// PassPositionToMaterial.ConvertScreenToWorldPos() 內部做：
    ///     unity_y = Screen.height - our_sy
    /// 但 Screen.height 回傳 Display 0 高度（例如 1080），不是 Display 2 的 768。
    /// Camera.ScreenToWorldPoint 用 Camera.pixelHeight（768）定義座標範圍，
    /// 若用 Screen.height（1080）計算 targetUnityY，Y 值會超出螢幕上方。
    ///
    /// 反推正確的 our_sy：
    ///     目標 unity_y = (1 - rowRatio) * displayHeight   （row=0 → 頂部）
    ///     our_sy = Screen.height - 目標 unity_y
    /// </remarks>
    private Vector2 ColRowToScreen(int col, int row)
    {
        float sx = (col + 0.5f) / gridCols * displayWidth;

        float rowRatio     = (row + 0.5f) / gridRows;
        float targetUnityY = (1f - rowRatio) * displayHeight;
        float sy           = Screen.height - targetUnityY;

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
