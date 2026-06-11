using System.Diagnostics;
using System.Text;

namespace TouchService.Diagnostics;

/// <summary>
/// 服務統計：封包 / 點 / 事件 / 發布 計數 + 處理延遲。
/// 計數用 Interlocked，stdin `stats` 指令吐 snapshot。
/// </summary>
public sealed class StatsCollector
{
    readonly Stopwatch _uptime = Stopwatch.StartNew();

    long _datagrams;
    long _parseErrors;
    long _points;
    long _pointsUnrouted;
    long _droppedByLock;
    long _downs;
    long _moves;
    long _ups;
    long _framesPublished;
    long _processUsTotal;
    long _processUsMax;

    // pkt rate：最近一秒視窗
    long _lastWindowDatagrams;
    long _windowStartMs;
    double _pktRate;

    public void OnDatagram()      => Interlocked.Increment(ref _datagrams);
    public void OnParseError()    => Interlocked.Increment(ref _parseErrors);
    public void OnPoints(int n)   => Interlocked.Add(ref _points, n);
    public void OnUnrouted()      => Interlocked.Increment(ref _pointsUnrouted);
    public void OnDroppedByLock() => Interlocked.Increment(ref _droppedByLock);
    public void OnDown()          => Interlocked.Increment(ref _downs);
    public void OnMove()          => Interlocked.Increment(ref _moves);
    public void OnUp()            => Interlocked.Increment(ref _ups);
    public void OnFramePublished()=> Interlocked.Increment(ref _framesPublished);

    public void OnProcessed(long elapsedUs)
    {
        Interlocked.Add(ref _processUsTotal, elapsedUs);
        long max;
        while (elapsedUs > (max = Interlocked.Read(ref _processUsMax)))
            if (Interlocked.CompareExchange(ref _processUsMax, elapsedUs, max) == max) break;
    }

    /// <summary>由 timer 執行緒每次 tick 呼叫，更新 1s 視窗 pkt rate。</summary>
    public void Tick(long nowMs)
    {
        if (nowMs - _windowStartMs < 1000) return;
        long d = Interlocked.Read(ref _datagrams);
        long elapsed = nowMs - _windowStartMs;
        if (_windowStartMs > 0 && elapsed > 0)
            _pktRate = (d - _lastWindowDatagrams) * 1000.0 / elapsed;
        _lastWindowDatagrams = d;
        _windowStartMs = nowMs;
    }

    public string Snapshot(int activeTouches, int wsClients, int autoLockedCells = 0)
    {
        long datagrams = Interlocked.Read(ref _datagrams);
        long processed = Interlocked.Read(ref _processUsTotal);
        double avgUs = datagrams > 0 ? (double)processed / datagrams : 0;

        var sb = new StringBuilder();
        sb.AppendLine("── TouchService stats ─────────────────────────");
        sb.AppendLine($"uptime           : {_uptime.Elapsed:hh\\:mm\\:ss}");
        sb.AppendLine($"datagrams        : {datagrams}  ({_pktRate:F1} pkt/s)");
        sb.AppendLine($"parse errors     : {Interlocked.Read(ref _parseErrors)}");
        sb.AppendLine($"points           : {Interlocked.Read(ref _points)}  (unrouted {Interlocked.Read(ref _pointsUnrouted)})");
        sb.AppendLine($"dropped by lock  : {Interlocked.Read(ref _droppedByLock)}  (lockedCells 噪訊晶片過濾)");
        sb.AppendLine($"auto-locked cells: {autoLockedCells}  (啟動自檢窗自動鎖定，詳見 locks 指令)");
        sb.AppendLine($"touch down/move/up: {Interlocked.Read(ref _downs)} / {Interlocked.Read(ref _moves)} / {Interlocked.Read(ref _ups)}");
        sb.AppendLine($"active touches   : {activeTouches}");
        sb.AppendLine($"frames published : {Interlocked.Read(ref _framesPublished)}");
        sb.AppendLine($"process latency  : avg {avgUs:F1} us / max {Interlocked.Read(ref _processUsMax)} us（recv→publish）");
        sb.AppendLine($"ws clients       : {wsClients}");
        sb.Append("───────────────────────────────────────────────");
        return sb.ToString();
    }
}
