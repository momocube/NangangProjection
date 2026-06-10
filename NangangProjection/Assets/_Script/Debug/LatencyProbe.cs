using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// 延遲量測探針：在 UDP→畫面 的完整路徑上插入時間戳。
/// 環形 buffer 記錄，F8 dump 成 CSV 供離線分析。
///
/// 兩層啟停機制：
///   1. 編譯時：Mark() 用 [Conditional("LATENCY_PROBE")] 修飾。
///      Player Settings → Scripting Define Symbols 含 LATENCY_PROBE 才會編譯進去。
///      未定義時所有 caller 的 Mark() call 編譯期被砍光，零 runtime cost、零 binary footprint。
///   2. 執行時：Inspector probeEnabled bool。即使編譯有進去、停用時 Mark() earliest return < 5ns。
///
/// 正式 release build SOP：
///   - Edit → Project Settings → Player → Other Settings → Scripting Define Symbols
///   - 移除 LATENCY_PROBE
///   - 重新 Build → 所有 hook 完全消失、無任何效能影響
///
/// 使用：
///   1. Player Settings → Scripting Define Symbols 加上 `LATENCY_PROBE`
///   2. 場景內任意 GameObject 掛 LatencyProbe 元件（或建一個專屬 GO）
///   3. Inspector 確認 probeEnabled = true
///   4. 跑遊戲、踩磚數次、F8 dump
///   5. CSV 路徑會印到 Console（Application.persistentDataPath）
///
/// 量測點：
///   UdpRecv          背景執行緒收到 UDP、進 _pending queue
///   UdpDequeue       主執行緒 Update() dequeue 該點
///   SetActive        UpdateSmartTouch 呼叫 SetActive 之前
///   ApplyStart/End   LateUpdate Texture2D.Apply 包夾
///   FlushStart/End   FlushExpiredSmartTouches 包夾
///   BusInvokeStart/End  SensorBus 廣播包夾（量 listener chain 耗時）
///   VCellUpdateStart/End VirtualCellManager.Update 整段
///   FrameTick        每幀記 deltaTime（看 frame drop）
///
/// 設計：
///   - 環形 buffer、預分配、零 GC alloc
///   - Stopwatch.GetTimestamp() 跨執行緒安全、ns 精度
///   - Interlocked.Increment 索引、無 lock
///   - 熱路徑 Mark() 約 50-200ns；停用時 earliest return < 5ns
/// </summary>
public class LatencyProbe : MonoBehaviour
{
    public enum EventType : byte
    {
        UdpRecv          = 1,
        UdpDequeue       = 2,
        SetActive        = 3,
        ApplyStart       = 4,
        ApplyEnd         = 5,
        FrameTick        = 6,
        BusInvokeStart   = 7,
        BusInvokeEnd     = 8,
        VCellUpdateStart = 9,
        VCellUpdateEnd   = 10,
        FlushStart       = 11,
        FlushEnd         = 12,
    }

    struct ProbeEvent
    {
        public long      timestamp;
        public EventType evt;
        public int       extra1;
        public int       extra2;
    }

    [Header("延遲量測探針")]
    [Tooltip("關閉時所有 Mark() 立刻 return，零負擔")]
    public bool probeEnabled = true;

    [Tooltip("Buffer 大小（環形、覆寫舊資料）。8192 約涵蓋 ~10-30 秒的事件")]
    public int bufferSize = 8192;

    [Tooltip("按下此鍵 dump 到 CSV")]
    public KeyCode dumpKey = KeyCode.F8;

    [Tooltip("dump 後在 Console 顯示路徑（方便現場操作）")]
    public bool logDumpPath = true;

    static ProbeEvent[] _buffer;
    static int          _writeIdx;
    static long         _t0;
    static double       _tickToUs;
    static bool         _enabled;

    public static bool Enabled => _enabled;

    void Awake()
    {
        _buffer    = new ProbeEvent[Mathf.Max(64, bufferSize)];
        _t0        = Stopwatch.GetTimestamp();
        _tickToUs  = 1_000_000.0 / Stopwatch.Frequency;
        _enabled   = probeEnabled;

        string compileStatus =
#if LATENCY_PROBE
            "ENABLED";
#else
            "DISABLED (LATENCY_PROBE not defined — all Mark() calls compiled out)";
#endif
        UnityEngine.Debug.Log($"[LatencyProbe] compile={compileStatus} buffer={_buffer.Length} freq={Stopwatch.Frequency} dumpKey={dumpKey}");
    }

    void Update()
    {
        _enabled = probeEnabled;

        Mark(EventType.FrameTick,
             Time.frameCount,
             (int)(Time.deltaTime * 1_000_000));

        if (Input.GetKeyDown(dumpKey)) Dump();
    }

    /// <summary>
    /// 在任何執行緒、任何路徑上打點。零 GC alloc。
    /// Conditional: Player Settings 沒定義 LATENCY_PROBE 時，所有 caller 的 Mark() call 編譯期被砍光。
    /// Runtime stop: probeEnabled = false 時 earliest return < 5ns。
    /// </summary>
    [System.Diagnostics.Conditional("LATENCY_PROBE")]
    public static void Mark(EventType evt, int extra1 = 0, int extra2 = 0)
    {
        if (!_enabled || _buffer == null) return;

        long t   = Stopwatch.GetTimestamp() - _t0;
        int  idx = Interlocked.Increment(ref _writeIdx) - 1;
        int  len = _buffer.Length;
        int  i   = ((idx % len) + len) % len;

        _buffer[i].timestamp = t;
        _buffer[i].evt       = evt;
        _buffer[i].extra1    = extra1;
        _buffer[i].extra2    = extra2;
    }

    void Dump()
    {
        if (_buffer == null) return;

        int total    = _writeIdx;
        int count    = Mathf.Min(total, _buffer.Length);
        int startIdx = total >= _buffer.Length
            ? ((total % _buffer.Length) + _buffer.Length) % _buffer.Length
            : 0;

        string path = Path.Combine(
            Application.persistentDataPath,
            $"latency-probe-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

        try
        {
            var sb = new StringBuilder(count * 48);
            sb.AppendLine("seq,t_us,event,extra1,extra2");

            for (int n = 0; n < count; n++)
            {
                int i = (startIdx + n) % _buffer.Length;
                ref var e = ref _buffer[i];
                double us = e.timestamp * _tickToUs;
                sb.Append(n);                  sb.Append(',');
                sb.Append(us.ToString("F1"));  sb.Append(',');
                sb.Append(e.evt);              sb.Append(',');
                sb.Append(e.extra1);           sb.Append(',');
                sb.AppendLine(e.extra2.ToString());
            }

            File.WriteAllText(path, sb.ToString());
            if (logDumpPath)
                UnityEngine.Debug.Log($"[LatencyProbe] dumped {count} events → {path}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[LatencyProbe] dump failed: {ex.Message}");
        }
    }
}
