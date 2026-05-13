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
    [Tooltip("拖曳時舊位置的水波殘影持續秒數（製造物理尾跡效果；越大尾跡越長）")]
    public float wakeHoldSeconds = 0.3f;

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
        public int     col, row;       // 格子座標
        public Vector2 screenPos;      // 螢幕像素座標
    }

    private PassPositionToMaterial _pptm;
    private TouchSlot[]   _slots        = new TouchSlot[MAX_SLOTS];
    private bool[]        _pressActive  = new bool[MAX_SLOTS];   // 此 slot 是否處於按壓狀態
    private bool[]        _fadingOut    = new bool[MAX_SLOTS];   // 此 slot 是否正在淡出
    private float[]       _fadeEndTime  = new float[MAX_SLOTS];  // 淡出結束時間
    private bool[]        _isWake       = new bool[MAX_SLOTS];   // 因拖曳產生的尾跡，命中時不還原按壓狀態
    private Vector2[]     _smoothedPos  = new Vector2[MAX_SLOTS]; // 平滑插值位置（解決離散跳格移動感）
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
        float now       = Time.time;
        int   hitCount  = _frameHits.Count;
        bool[] hitUsed  = new bool[hitCount];

        // Step 1：現有 active slot 找最近的未使用 hit
        // wake slot（尾跡殘影）不參與配對，避免消耗新 pressActive slot 應得到的 hit
        for (int s = 0; s < MAX_SLOTS; s++)
        {
            if (!_slots[s].active) continue;

            int   best     = -1;
            float bestDist = matchRadiusCells * matchRadiusCells;

            if (!_isWake[s])
            {
                for (int h = 0; h < hitCount; h++)
                {
                    if (hitUsed[h]) continue;
                    float dc = _frameHits[h].x - _slots[s].col;
                    float dr = _frameHits[h].y - _slots[s].row;
                    float d  = dc * dc + dr * dr;
                    if (d < bestDist) { bestDist = d; best = h; }
                }
            }

            if (best >= 0)
            {
                hitUsed[best]      = true;
                _slots[s].col      = _frameHits[best].x;
                _slots[s].row      = _frameHits[best].y;
                _slots[s].screenPos = ColRowToScreen(_slots[s].col, _slots[s].row);
                _slots[s].lastSeen  = now;
                // 短暫中斷後恢復觸碰：只還原「非尾跡」的淡出 slot（尾跡殘影應繼續消散）
                if (_fadingOut[s] && !_isWake[s])
                {
                    _fadingOut[s]   = false;
                    _pressActive[s] = true;
                }
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
                // 逾時：進入淡出狀態，在 pressReleaseHoldSeconds 後才真正停用，避免水波瞬間消失
                _fadingOut[s]   = true;
                _fadeEndTime[s] = now + pressReleaseHoldSeconds;
            }
        }

        // Step 2：剩餘未配對的 hit → 開新 slot（已被現有 slot 涵蓋的 hit 不重複建立）
        for (int h = 0; h < hitCount; h++)
        {
            if (hitUsed[h]) continue;

            // 若此 hit 在任何 active 非尾跡 slot（含本輪新建）的配對半徑內，視為同一觸點，不開新 slot
            // wake slot 不計入 covered，否則拖曳回近原位時無法建立新 pressActive slot
            bool covered = false;
            for (int s = 0; s < MAX_SLOTS && !covered; s++)
            {
                if (!_slots[s].active || _isWake[s]) continue;
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
            _smoothedPos[free]     = _slots[free].screenPos;  // 新 slot 直接跳到起始位置，不從舊位置漸進
            _isWake[free]          = false;

            // 快速滑動跳格補救：若整場只有一個按壓 slot，無論距離多遠都轉移按壓狀態，
            // 確保同一時間只有一個主水波源，避免放手時出現多道水岔開
            int pressCount = 0, pressSlot = -1;
            for (int s2 = 0; s2 < MAX_SLOTS; s2++)
            {
                if (s2 != free && _pressActive[s2] && _slots[s2].active)
                { pressCount++; pressSlot = s2; }
            }
            if (pressCount == 1)
            {
                _pressActive[pressSlot] = false;
                _fadingOut[pressSlot]   = true;
                _fadeEndTime[pressSlot] = now + wakeHoldSeconds;
                _isWake[pressSlot]      = true;
                _pressActive[free]      = true;
            }
        }
    }

    private void FlushToPPTM()
    {
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            // positionSmoothSpeed > 0 時使用平滑位置，否則直接使用原始格子座標
            Vector2 pos = (positionSmoothSpeed > 0f) ? _smoothedPos[i] : _slots[i].screenPos;
            _pptm.inputPosition[i] = _slots[i].active
                ? new Vector4(pos.x, pos.y, 0f, 0f)
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
                _isWake[i]      = false;  // 此 slot 現在是正式按壓，清除尾跡旗標
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
        _smoothedPos[free]     = _slots[free].screenPos;
        _pressActive[free]     = true;
        _isWake[free]          = false;
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
            _isWake[i]       = false;  // PressEnd 淡出可因觸碰恢復而取消（與尾跡行為相反）
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

    /// <summary>
    /// 拖曳：舊位置化為短暫尾跡殘影，新位置建立主按壓點。
    /// 效果：手滑過時形成物理感尾跡曲線，放手只消散最後一點，不岔開。
    /// </summary>
    private void OnSensorDrag(int col, int row)
    {
        if (!_processingCanvasEvent && Time.frameCount == _lastCanvasEventFrame) return;
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (!_pressActive[i] || !_slots[i].active) continue;

            // 舊位置 → 短暫尾跡（殘影），製造物理尾跡效果
            _pressActive[i] = false;
            _fadingOut[i]   = true;
            _fadeEndTime[i] = Time.time + wakeHoldSeconds;
            _isWake[i]      = true;

            // 新位置 → 建立主按壓點
            int next = FindFreeSlot();
            if (next >= 0)
            {
                _slots[next].active    = true;
                _slots[next].lastSeen  = Time.time;
                _slots[next].col       = col;
                _slots[next].row       = row;
                _slots[next].screenPos = ColRowToScreen(col, row);
                _smoothedPos[next]     = _slots[next].screenPos;  // 拖曳新位置直接跳到，不從舊位置飄入
                _pressActive[next]     = true;
                _isWake[next]          = false;
            }
            return;
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

        float rowRatio = (row + 0.5f) / gridRows;
        float targetUnityY = (1f - rowRatio) * displayHeight;   // row=0 → displayHeight（頂部）
        float sy = Screen.height - targetUnityY;                 // 補償 PPTM 的 Screen.height flip

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
