using System.Diagnostics;
using System.Text;

namespace TouchService.Diagnostics;

/// <summary>
/// 延遲量測探針：UDP 收包 → 發布 的完整路徑時間戳。
/// 核心移植自 Unity 專案 Assets/_Script/Debug/LatencyProbe.cs:125-139
/// （Stopwatch.GetTimestamp + Interlocked 環形 buffer，零 GC alloc、跨執行緒安全、無 lock）。
/// 改動：去掉 MonoBehaviour / F8 dump，改為 stdin `probe` 指令 dump CSV。
/// </summary>
public static class LatencyProbe
{
    public enum EventType : byte
    {
        UdpRecv    = 1,   // receive 執行緒收到 datagram（extra1=來源 port, extra2=bytes）
        ParseDone  = 2,   // 解析完成（extra1=點數, extra2=parse error code）
        Published  = 3,   // frame 發布完成（extra1=事件數, extra2=該 datagram 處理耗時 us）
        FlushTick  = 4,   // timer flush（extra1=up 事件數）
    }

    struct ProbeEvent
    {
        public long      Timestamp;
        public EventType Evt;
        public int       Extra1;
        public int       Extra2;
    }

    const int BufferSize = 16384;

    static readonly ProbeEvent[] _buffer = new ProbeEvent[BufferSize];
    static readonly long _t0 = Stopwatch.GetTimestamp();
    static readonly double _tickToUs = 1_000_000.0 / Stopwatch.Frequency;
    static int _writeIdx;

    /// <summary>在任何執行緒、任何路徑上打點。零 GC alloc，約 50-200ns。</summary>
    public static void Mark(EventType evt, int extra1 = 0, int extra2 = 0)
    {
        long t   = Stopwatch.GetTimestamp() - _t0;
        int  idx = Interlocked.Increment(ref _writeIdx) - 1;
        int  i   = ((idx % BufferSize) + BufferSize) % BufferSize;

        _buffer[i].Timestamp = t;
        _buffer[i].Evt       = evt;
        _buffer[i].Extra1    = extra1;
        _buffer[i].Extra2    = extra2;
    }

    /// <summary>dump 環形 buffer 為 CSV，回傳寫出路徑。</summary>
    public static string DumpCsv(string dir)
    {
        Directory.CreateDirectory(dir);
        int total    = Volatile.Read(ref _writeIdx);
        int count    = Math.Min(total, BufferSize);
        int startIdx = total >= BufferSize
            ? ((total % BufferSize) + BufferSize) % BufferSize
            : 0;

        var path = Path.Combine(dir, $"latency-probe-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        var sb = new StringBuilder(count * 48);
        sb.AppendLine("seq,t_us,event,extra1,extra2");

        for (int n = 0; n < count; n++)
        {
            int i = (startIdx + n) % BufferSize;
            ref var e = ref _buffer[i];
            double us = e.Timestamp * _tickToUs;
            sb.Append(n).Append(',')
              .Append(us.ToString("F1")).Append(',')
              .Append(e.Evt).Append(',')
              .Append(e.Extra1).Append(',')
              .Append(e.Extra2).AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
        return path;
    }
}
