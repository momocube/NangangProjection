using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 以兩張 Texture2D 取代原先 3000+ 個 SensorTileCell / ControlTileCell GameObject：
///   SensorTex  → Display 2 感應格視覺（深色底 + 格子顏色）
///   ControlTex → Display 1 控制格視覺（透明底 + 格子顏色）
///
/// 貼圖解析度：每格 1 像素（以 CellPixelWidth=16 為單位），比全畫布解析度小約 250 倍；
/// RawImage + FilterMode.Point 放大後視覺完全相同。
/// 狀態變更只寫入 CPU 像素緩衝，LateUpdate 統一 Apply（每幀最多 2 次 GPU 上傳）。
/// </summary>
public class TileGridTexture : MonoBehaviour
{
    public enum CellState { Idle, Active, Locked, Pending, Pressing }

    // ── 顏色 ─────────────────────────────────────────────────────────────────

    // Sensor (D2)
    static readonly Color32 SId = new Color32( 15,  15,  20, 255); // idle fill
    static readonly Color32 SAc = new Color32(230,  26,  26, 255); // active
    static readonly Color32 SLk = new Color32(217, 184,   0, 255); // locked
    static readonly Color32 SPr = new Color32(255, 140,   0, 255); // pressing
    static readonly Color32 SPd = new Color32(140, 140, 153, 255); // pending

    // Control (D1) — fill only (idle = transparent)
    static readonly Color32 CAc = new Color32(230,  26,  26, 230); // active
    static readonly Color32 CLk = new Color32(217, 184,   0, 230); // locked
    static readonly Color32 CPr = new Color32(255, 140,   0, 230); // pressing
    static readonly Color32 CPd = new Color32(140, 140, 153, 128); // pending
    static readonly Color32 CHv = new Color32( 51,  56,  77, 102); // hover
    static readonly Color32 C0  = new Color32(  0,   0,   0,   0); // idle (transparent)

    // ── 執行期資料 ────────────────────────────────────────────────────────────

    class FaceRuntime
    {
        public SensorFaceConfig config;
        public CellState[]      states;
        public float[]          activeExpiry;  // Time.time 到期時回 Idle
        public bool[]           activeTracked;
        public Color32[]        sensorIdle;    // 每格的 sensor idle fill color（face tint）
    }

    readonly List<FaceRuntime> _faces = new List<FaceRuntime>();
    readonly List<int> _activeKeys = new List<int>();
    int     _texW, _texH;
    float   _activeDuration = 1.5f;
    float   _sensorIdleAlpha = 1f;

    int _hoverFace = -1, _hoverCell = -1;

    Color32[] _sensorPx;
    Color32[] _controlPx;
    bool      _sensorDirty;
    bool      _controlDirty;

    public Texture2D SensorTex  { get; private set; }
    public Texture2D ControlTex { get; private set; }

    // ── 初始化 ────────────────────────────────────────────────────────────────

    /// <param name="canvasW">畫布寬度（px）；貼圖內部縮小為 canvasW/16 × canvasH/16。</param>
    public void Initialize(int canvasW, int canvasH, IList<SensorFaceConfig> faceConfigs, float activeDuration)
    {
        _activeDuration = activeDuration;
        const int u = SensorHardwareConst.CellPixelWidth;   // 16
        int texW = canvasW / u;
        int texH = canvasH / u;
        _texW = texW;
        _texH = texH;

        if (SensorTex == null || SensorTex.width != texW || SensorTex.height != texH)
        {
            if (SensorTex != null) Destroy(SensorTex);
            SensorTex             = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            SensorTex.filterMode  = FilterMode.Point;
            SensorTex.wrapMode    = TextureWrapMode.Clamp;
        }
        if (ControlTex == null || ControlTex.width != texW || ControlTex.height != texH)
        {
            if (ControlTex != null) Destroy(ControlTex);
            ControlTex            = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            ControlTex.filterMode = FilterMode.Point;
            ControlTex.wrapMode   = TextureWrapMode.Clamp;
        }

        int pixelCount = texW * texH;
        if (_sensorPx  == null || _sensorPx.Length  != pixelCount) _sensorPx  = new Color32[pixelCount];
        if (_controlPx == null || _controlPx.Length != pixelCount) _controlPx = new Color32[pixelCount];

        _faces.Clear();
        _activeKeys.Clear();
        _hoverFace = _hoverCell = -1;

        // 先把 Control 全部清透明
        System.Array.Clear(_controlPx, 0, _controlPx.Length);

        foreach (var cfg in faceConfigs)
        {
            if (cfg == null) continue;
            int cellCount = cfg.colCount * cfg.rowCount;
            var fr = new FaceRuntime
            {
                config        = cfg,
                states        = new CellState[cellCount],
                activeExpiry  = new float[cellCount],
                activeTracked = new bool[cellCount],
                sensorIdle    = new Color32[cellCount],
            };
            // 計算每格 idle tint（與原 SensorTileCell.SetFaceColor 相同：Lerp(ColIdle, faceColor, 0.12)）
            Color tint = cfg.gizmoColor;
            for (int ci = 0; ci < cellCount; ci++)
            {
                Color blended = Color.Lerp(
                    new Color(SId.r / 255f, SId.g / 255f, SId.b / 255f),
                    tint, 0.12f);
                blended.a = _sensorIdleAlpha;
                fr.sensorIdle[ci] = ToColor32(blended);
            }
            _faces.Add(fr);
        }

        // 全部畫一次 idle
        PaintAllSensor();
        _sensorDirty  = true;
        _controlDirty = true;
    }

    /// <summary>切換場域時重建（尺寸可能改變）。</summary>
    public void Reinitialize(int canvasW, int canvasH, IList<SensorFaceConfig> faceConfigs)
    {
        _hoverFace = _hoverCell = -1;
        Initialize(canvasW, canvasH, faceConfigs, _activeDuration);
    }

    // ── 狀態設定 ─────────────────────────────────────────────────────────────

    /// <summary>觸發亮紅（Active → Idle 計時器）。鎖定時忽略。</summary>
    public void SetActive(int fi, int ci)
    {
        if (!Valid(fi, ci)) return;
        var fr = _faces[fi];
        if (fr.states[ci] == CellState.Locked) return;
        bool wasActive = fr.states[ci] == CellState.Active;
        fr.states[ci]       = CellState.Active;
        fr.activeExpiry[ci] = Time.time + _activeDuration;
        if (!fr.activeTracked[ci])
        {
            fr.activeTracked[ci] = true;
            _activeKeys.Add(MakeKey(fi, ci));
        }
        // 格子已是 Active 時只更新計時器，不重繪（避免 100Hz 感應每幀觸發全圖 Apply）
        if (!wasActive) PaintCellBoth(fi, ci, CellState.Active);
    }

    /// <summary>設定鎖定狀態。</summary>
    public void SetLocked(int fi, int ci, bool locked)
    {
        if (!Valid(fi, ci)) return;
        var fr     = _faces[fi];
        var newState = locked ? CellState.Locked : CellState.Idle;
        if (fr.states[ci] == newState) return;
        fr.states[ci] = newState;
        ClearActiveTracking(fr, ci);
        if (locked) fr.activeExpiry[ci] = 0f;
        PaintCellBoth(fi, ci, newState);
    }

    /// <summary>設定按壓橘燈。鎖定時不回 Idle。</summary>
    public void SetPressing(int fi, int ci, bool pressing)
    {
        if (!Valid(fi, ci)) return;
        var fr = _faces[fi];
        if (fr.states[ci] == CellState.Locked) return;
        fr.states[ci] = pressing ? CellState.Pressing : CellState.Idle;
        ClearActiveTracking(fr, ci);
        if (pressing) fr.activeExpiry[ci] = 0f;
        PaintCellBoth(fi, ci, fr.states[ci]);
    }

    /// <summary>設定孤點等待（灰白）狀態。鎖定時不改變。</summary>
    public void SetPending(int fi, int ci, bool pending)
    {
        if (!Valid(fi, ci)) return;
        var fr = _faces[fi];
        if (fr.states[ci] == CellState.Locked) return;
        fr.states[ci] = pending ? CellState.Pending : CellState.Idle;
        ClearActiveTracking(fr, ci);
        PaintCellBoth(fi, ci, fr.states[ci]);
    }

    /// <summary>D1 hover（只畫 ControlTex）。</summary>
    public void SetHovered(int fi, int ci)
    {
        if (_hoverFace == fi && _hoverCell == ci) return;
        int oldFace = _hoverFace, oldCell = _hoverCell;
        _hoverFace = fi;
        _hoverCell = ci;
        if (Valid(oldFace, oldCell)) PaintCellControl(oldFace, oldCell, _faces[oldFace].states[oldCell]);
        if (Valid(fi, ci))           PaintCellControl(fi,      ci,      _faces[fi].states[ci]);
        _controlDirty = true;
    }

    public void ClearHover()
    {
        if (_hoverFace < 0) return;
        int oldFace = _hoverFace, oldCell = _hoverCell;
        _hoverFace = _hoverCell = -1;
        if (Valid(oldFace, oldCell)) PaintCellControl(oldFace, oldCell, _faces[oldFace].states[oldCell]);
        _controlDirty = true;
    }

    /// <summary>遊戲載入時調整 sensor idle 格子透明度（alpha=1 → 不透明深色，alpha=0 → 全透明讓遊戲畫面透出）。</summary>
    public void SetSensorIdleAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        if (Mathf.Approximately(_sensorIdleAlpha, alpha)) return;
        _sensorIdleAlpha = alpha;

        // 更新所有面的 idle tint alpha
        foreach (var fr in _faces)
        {
            for (int ci = 0; ci < fr.sensorIdle.Length; ci++)
            {
                var c = fr.sensorIdle[ci];
                c.a = (byte)Mathf.RoundToInt(alpha * 255f);
                fr.sensorIdle[ci] = c;
            }
            // 只重刷 Idle 格子
            for (int ci = 0; ci < fr.states.Length; ci++)
                if (fr.states[ci] == CellState.Idle)
                    PaintCellSensor(_faces.IndexOf(fr), ci, CellState.Idle);
        }
        _sensorDirty = true;
    }

    // ── LateUpdate ────────────────────────────────────────────────────────────

    void LateUpdate()
    {
        float now = Time.time;
        for (int i = _activeKeys.Count - 1; i >= 0; i--)
        {
            int key = _activeKeys[i];
            int fi = key / 100000;
            int ci = key - fi * 100000;
            if (!Valid(fi, ci))
            {
                RemoveActiveKeyAt(i);
                continue;
            }

            var fr = _faces[fi];
            if (fr.states[ci] != CellState.Active)
            {
                FinishActiveTracking(fr, ci);
                RemoveActiveKeyAt(i);
                continue;
            }

            if (now >= fr.activeExpiry[ci])
            {
                fr.states[ci] = CellState.Idle;
                FinishActiveTracking(fr, ci);
                PaintCellBoth(fi, ci, CellState.Idle);
                RemoveActiveKeyAt(i);
            }
        }

        if (_sensorDirty)
        {
            SensorTex.SetPixels32(_sensorPx);
            SensorTex.Apply(false);
            _sensorDirty = false;
        }
        if (_controlDirty)
        {
            ControlTex.SetPixels32(_controlPx);
            ControlTex.Apply(false);
            _controlDirty = false;
        }
    }

    void OnDestroy()
    {
        if (SensorTex  != null) { Destroy(SensorTex);  SensorTex  = null; }
        if (ControlTex != null) { Destroy(ControlTex); ControlTex = null; }
    }

    // ── 繪圖 ─────────────────────────────────────────────────────────────────

    void PaintAllSensor()
    {
        for (int fi = 0; fi < _faces.Count; fi++)
        {
            var fr = _faces[fi];
            for (int ci = 0; ci < fr.states.Length; ci++)
                PaintCellSensor(fi, ci, fr.states[ci]);
        }
    }

    void PaintCellBoth(int fi, int ci, CellState state)
    {
        PaintCellSensor(fi, ci, state);
        PaintCellControl(fi, ci, state);
        _sensorDirty  = true;
        _controlDirty = true;
    }

    void PaintCellSensor(int fi, int ci, CellState state)
    {
        if (fi < 0 || fi >= _faces.Count) return;
        var  fr   = _faces[fi];
        var  cfg  = fr.config;
        const int u = SensorHardwareConst.CellPixelWidth;
        int  cellW = cfg.GizmoCellW / u;   // 1 (橫) or 2 (直)
        int  cellH = cfg.GizmoCellH / u;   // 2 (橫) or 1 (直)
        int  col   = ci % cfg.colCount;
        int  row   = ci / cfg.colCount;
        int  texX  = cfg.originX / u + col * cellW;
        int  texY  = cfg.originY / u + row * cellH;

        Color32 fillColor = state switch
        {
            CellState.Active   => SAc,
            CellState.Locked   => SLk,
            CellState.Pressing => SPr,
            CellState.Pending  => SPd,
            _                  => fr.sensorIdle[ci],
        };

        FillRect(_sensorPx, texX, texY, cellW, cellH, fillColor);
    }

    void PaintCellControl(int fi, int ci, CellState state)
    {
        if (fi < 0 || fi >= _faces.Count) return;
        var  fr   = _faces[fi];
        var  cfg  = fr.config;
        const int u = SensorHardwareConst.CellPixelWidth;
        int  cellW = cfg.GizmoCellW / u;
        int  cellH = cfg.GizmoCellH / u;
        int  col   = ci % cfg.colCount;
        int  row   = ci / cfg.colCount;
        int  texX  = cfg.originX / u + col * cellW;
        int  texY  = cfg.originY / u + row * cellH;

        bool hovered = (fi == _hoverFace && ci == _hoverCell);

        Color32 fillColor = state switch
        {
            CellState.Active   => CAc,
            CellState.Locked   => CLk,
            CellState.Pressing => CPr,
            CellState.Pending  => CPd,
            _ when hovered     => CHv,
            _                  => C0,
        };

        FillRect(_controlPx, texX, texY, cellW, cellH, fillColor);
    }

    /// <summary>填充矩形到 CPU 像素緩衝（Y 軸翻轉：canvas top → texture bottom）。</summary>
    void FillRect(Color32[] buf, int x, int y, int w, int h, Color32 color)
    {
        if (w <= 0 || h <= 0) return;
        int x1 = Mathf.Clamp(x,     0, _texW);
        int x2 = Mathf.Clamp(x + w, 0, _texW);
        int y1 = Mathf.Clamp(y,     0, _texH);
        int y2 = Mathf.Clamp(y + h, 0, _texH);

        for (int cy = y1; cy < y2; cy++)
        {
            int ty = _texH - 1 - cy;  // Y 翻轉
            int rowBase = ty * _texW;
            for (int cx = x1; cx < x2; cx++)
                buf[rowBase + cx] = color;
        }
    }

    // ── 工具 ─────────────────────────────────────────────────────────────────

    bool Valid(int fi, int ci)
        => fi >= 0 && fi < _faces.Count && ci >= 0 && ci < _faces[fi].states.Length;

    static int MakeKey(int fi, int ci) => fi * 100000 + ci;

    static void ClearActiveTracking(FaceRuntime fr, int ci)
    {
        fr.activeExpiry[ci] = 0f;
    }

    static void FinishActiveTracking(FaceRuntime fr, int ci)
    {
        fr.activeTracked[ci] = false;
        fr.activeExpiry[ci] = 0f;
    }

    void RemoveActiveKeyAt(int index)
    {
        int last = _activeKeys.Count - 1;
        _activeKeys[index] = _activeKeys[last];
        _activeKeys.RemoveAt(last);
    }

    static Color32 ToColor32(Color c)
        => new Color32(
            (byte)Mathf.RoundToInt(c.r * 255f),
            (byte)Mathf.RoundToInt(c.g * 255f),
            (byte)Mathf.RoundToInt(c.b * 255f),
            (byte)Mathf.RoundToInt(c.a * 255f));
}
