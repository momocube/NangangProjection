using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 觸碰訊號 Debug Logger。
/// - listenRawUdp = true：直接顯示 UDP 原始像素座標，用於校準 raw 範圍
/// - listenSensorBus = true：顯示已映射的格子座標（需 face 校準完成後才有資料）
/// 按 F9 切換視窗，可拖曳、可拉動大小。
/// 新條目往下往右填，左欄填滿再填右欄，視窗拉多大即有多少格。
/// </summary>
public class TouchDebugLogger : MonoBehaviour
{
    [Header("觸碰紀錄視窗（F9 開關）")]
    [Header("停用此元件 → 記錄視窗無法開啟")]
    [Header("訂閱來源")]
    [Tooltip("直接訂閱 UDP raw 座標，校準用")]
    public bool listenRawUdp    = true;
    [Tooltip("訂閱 SensorBus 格子座標，校準完成後開啟")]
    public bool listenSensorBus = false;

    [Header("IMGUI 視窗")]
    public bool    showWindow = true;
    public KeyCode toggleKey  = KeyCode.F9;

    // ── 常數 ──────────────────────────────────────────────────────────────────
    private const int   RowH    = 26;   // 每列高度（px）
    private const int   ColMinW = 260;  // 每欄最小寬度（px）
    private const int   CopyBtnW = 24;  // 複製按鈕寬度
    private const int   HeaderH = 48;   // 工具列高度
    private const int   GripSz  = 16;   // resize grip 尺寸
    private const int   AbsMax  = 400;  // 絕對上限（防無限增長）

    // ── 內部資料 ──────────────────────────────────────────────────────────────
    private enum EntryKind { Raw, Cell }

    private struct LogEntry
    {
        public EntryKind kind;
        public int  a, b;   // Raw: (rawX, rawY)  |  Cell: (col, row)
        public int  count;
        public float time;
    }

    private readonly List<LogEntry> _entries = new List<LogEntry>();
    private bool                _paused;
    private float               _lastSensorTime = -1f;
    private const float         SuppressAfterSensorSec = 0.15f;
    private UdpSensorReceiver   _udpReceiver;

    private int    _hoveredIdx  = -1;
    private string _toastMsg    = "";
    private float  _toastExpiry = -1f;

    // 快取 GUIStyle，OnGUI 第一幀初始化後重用，避免每幀 new 觸發 GC
    private GUIStyle _styleLabelRich;
    private GUIStyle _styleToast;
    private GUIStyle _styleHover;

    private Rect    _winRect = new Rect(20, 20, 340, 460);
    private int     _winId;
    private enum ResizeMode { None, Right, Bottom, Corner }
    private ResizeMode _resizeMode;
    private Vector2    _resizeStart;
    private Vector2    _sizeStart;

    // ── 生命週期 ──────────────────────────────────────────────────────────────
    private void Awake()
    {
        _winId       = GetInstanceID();
        _udpReceiver = FindObjectOfType<UdpSensorReceiver>();
    }

    private void OnEnable()
    {
        UdpSensorReceiver.OnPacketReceived += OnSensorTick;   // 永遠訂閱，用於屏蔽假點擊
        if (listenRawUdp)    UdpSensorReceiver.OnPacketReceived += OnRawUdp;
        if (listenSensorBus) SensorBus.OnTouch                 += OnCell;
    }

    private void OnDisable()
    {
        UdpSensorReceiver.OnPacketReceived -= OnSensorTick;
        UdpSensorReceiver.OnPacketReceived -= OnRawUdp;
        SensorBus.OnTouch                 -= OnCell;
    }

    private void OnSensorTick(int x, int y) => _lastSensorTime = Time.unscaledTime;

    // ── 事件處理 ──────────────────────────────────────────────────────────────
    private void OnRawUdp(int rawX, int rawY) => Record(EntryKind.Raw,  rawX, rawY);
    private void OnCell(int col, int row)     => Record(EntryKind.Cell, col,  row);

    private void Record(EntryKind kind, int a, int b)
    {
        if (_paused) return;
        // 已存在 → 原位更新
        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            if (e.kind == kind && e.a == a && e.b == b)
            {
                e.count++;
                e.time = Time.time;
                _entries[i] = e;
                return;
            }
        }

        // 新座標 → 加到最後
        if (_entries.Count >= AbsMax) return;
        _entries.Add(new LogEntry { kind = kind, a = a, b = b, count = 1, time = Time.time });
    }

    // ── Update ────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))   showWindow = !showWindow;
        if (Input.GetKeyDown(KeyCode.F10)) _entries.Clear();
        if (Input.GetKeyDown(KeyCode.F11)) _paused = !_paused;
        if (Input.GetKeyDown(KeyCode.F8) && _udpReceiver != null)
            _udpReceiver.enabled = !_udpReceiver.enabled;
        if (Input.GetKeyDown(KeyCode.F12) && _hoveredIdx >= 0 && _hoveredIdx < _entries.Count)
        {
            var e = _entries[_hoveredIdx];
            string coords = $"({e.a}, {e.b})";
            GUIUtility.systemCopyBuffer = coords;
            _toastMsg    = $"成功複製 {coords}";
            _toastExpiry = Time.unscaledTime + 2f;
        }
    }

    // ── IMGUI ─────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        if (!showWindow) return;

        OnGUI_ResizeHandle();
        _winRect = GUI.Window(_winId, _winRect, DrawWindow, "Touch Log  [F9]");


        // ── Resize 邊緣（Window 外疊加，永遠可互動）────────────────────────
        const int EdgeW = 6;    // 邊緣感應帶寬度
        float wx = _winRect.x, wy = _winRect.y, ww = _winRect.width, wh = _winRect.height;

        var rightEdge  = new Rect(wx + ww - EdgeW,          wy + 20,           EdgeW,      wh - 20 - GripSz);
        var bottomEdge = new Rect(wx + 20,                  wy + wh - EdgeW,   ww - 20 - GripSz, EdgeW);
        var corner     = new Rect(wx + ww - GripSz,         wy + wh - GripSz,  GripSz,    GripSz);

        // 滑鼠樣式提示（可選）
        EditorCursorHint(rightEdge, bottomEdge, corner);

        GUI.Label(corner, "◢");

        if (Event.current.type == EventType.MouseDown)
        {
            var mp = Event.current.mousePosition;
            ResizeMode hit = ResizeMode.None;
            if      (corner.Contains(mp))     hit = ResizeMode.Corner;
            else if (rightEdge.Contains(mp))  hit = ResizeMode.Right;
            else if (bottomEdge.Contains(mp)) hit = ResizeMode.Bottom;

            if (hit != ResizeMode.None)
            {
                _resizeMode  = hit;
                _resizeStart = mp;
                _sizeStart   = new Vector2(ww, wh);
                Event.current.Use();
            }
        }
    }

    private void EnsureStyles()
    {
        if (_styleLabelRich != null) return;
        _styleLabelRich = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 };
        _styleToast     = new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(0.4f, 1f, 0.4f) } };
        _styleHover     = new GUIStyle(_styleLabelRich) { normal = { textColor = Color.white } };
    }

    private void DrawWindow(int id)
    {
        EnsureStyles();
        if (Event.current.type == EventType.Repaint)
            _hoveredIdx = -1;
        // ── 工具列 ───────────────────────────────────────────────────────────
        bool sensorRecent  = Time.unscaledTime - _lastSensorTime < SuppressAfterSensorSec;
        bool udpConnected  = _udpReceiver != null && _udpReceiver.enabled;
        // Toast 訊息
        if (Time.unscaledTime < _toastExpiry)
        {
            GUILayout.Label(_toastMsg, _styleToast);
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label($"{_entries.Count} 筆", GUILayout.Width(50));
        var connColor = GUI.backgroundColor;
        GUI.backgroundColor = udpConnected ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.6f, 0.6f, 0.6f);
        if (GUILayout.Button(udpConnected ? "● UDP" : "○ UDP", GUILayout.Width(60)))
        {
            if (_udpReceiver != null) _udpReceiver.enabled = !_udpReceiver.enabled;
        }
        GUI.backgroundColor = connColor;
        // Clear / 暫停：感應器注入期間視覺灰掉且不響應
        var oldBg = GUI.backgroundColor;
        GUI.backgroundColor = sensorRecent ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
        if (GUILayout.Button("Clear", GUILayout.Width(55)) && !sensorRecent)
            _entries.Clear();
        GUI.backgroundColor = sensorRecent ? new Color(0.5f, 0.5f, 0.5f)
                            : (_paused     ? new Color(1f, 0.4f, 0.4f)
                                           : new Color(0.4f, 0.9f, 0.4f));
        if (GUILayout.Button(_paused ? "▶ 繼續" : "⏸ 暫停", GUILayout.Width(65)) && !sensorRecent)
            _paused = !_paused;
        GUI.backgroundColor = oldBg;
        GUILayout.EndHorizontal();

        // ── 版面計算 ─────────────────────────────────────────────────────────
        int cols   = Mathf.Max(1, Mathf.FloorToInt(_winRect.width / ColMinW));
        int availH = Mathf.Max(RowH, (int)_winRect.height - HeaderH - GripSz);
        int rows   = availH / RowH;
        // 固定欄寬：ColMinW 扣掉複製鈕，不隨視窗撐大
        float colW = ColMinW - CopyBtnW - 6;

        // ── 欄位並排（左對齊，固定寬度）────────────────────────────────────
        GUILayout.BeginHorizontal();
        for (int c = 0; c < cols; c++)
        {
            GUILayout.BeginVertical(GUILayout.Width(ColMinW));
            for (int r = 0; r < rows; r++)
            {
                int idx = c * rows + r;   // 左欄先填滿
                if (idx < _entries.Count)
                    DrawEntry(idx, _entries[idx], colW);
                else
                    GUILayout.Space(RowH);
            }
            GUILayout.EndVertical();
        }
        GUILayout.FlexibleSpace();   // 多餘空間推到右邊，欄位不被撐開
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0, 0, _winRect.width - GripSz, 20));
    }

    private void DrawEntry(int idx, LogEntry e, float labelW)
    {
        string rich = e.kind == EntryKind.Raw
            ? $"<b>[{e.time:F2}s]</b>  raw ({e.a}, {e.b}){(e.count > 1 ? $" <color=yellow>×{e.count}</color>" : "")}"
            : $"<b>[{e.time:F2}s]</b>  <color=#88ff88>col={e.a,3} row={e.b,2}</color>{(e.count > 1 ? $" <color=yellow>×{e.count}</color>" : "")}";

        GUIStyle style = _hoveredIdx == idx ? _styleHover : _styleLabelRich;

        GUILayout.BeginHorizontal();
        GUILayout.Label(rich, style, GUILayout.Width(labelW), GUILayout.Height(RowH));

        // 繪製後偵測滑鼠是否在這一行
        if (Event.current.type == EventType.Repaint)
        {
            var rowRect = GUILayoutUtility.GetLastRect();
            if (rowRect.Contains(Event.current.mousePosition))
                _hoveredIdx = idx;
        }

        GUILayout.EndHorizontal();
    }

    private void OnGUI_ResizeHandle()
    {
        if (_resizeMode == ResizeMode.None) return;
        if (Event.current.type == EventType.MouseDrag)
        {
            var delta = Event.current.mousePosition - _resizeStart;
            if (_resizeMode == ResizeMode.Right  || _resizeMode == ResizeMode.Corner)
                _winRect.width  = Mathf.Max(ColMinW + 10, _sizeStart.x + delta.x);
            if (_resizeMode == ResizeMode.Bottom || _resizeMode == ResizeMode.Corner)
                _winRect.height = Mathf.Max(120,           _sizeStart.y + delta.y);
            Event.current.Use();
        }
        else if (Event.current.type == EventType.MouseUp)
        {
            _resizeMode = ResizeMode.None;
            Event.current.Use();
        }
    }

    // 在 Runtime 中不做任何事（僅佔位，避免 Editor-only API 報錯）
    private static void EditorCursorHint(Rect r, Rect b, Rect c) { }

    private void OnDestroy() { }
}
