using System.Diagnostics;
using TouchService.Core;
using TouchService.Diagnostics;
using TouchService.Logging;
using TouchService.Output;
using TouchService.Tracking;

namespace TouchService;

/// <summary>
/// 處理管線：datagram → parse → route → map → track → publish。
/// 事件驅動——receive 執行緒直接走完整條（取代 Unity 的 queue + 每幀派送），
/// down 事件在首包當下即發布（零等待）。
/// timer 執行緒走 Tick()：release timeout 掃描（up）+ keepalive snapshot。
/// 兩條路徑用同一把鎖序列化。
/// </summary>
public sealed class TouchPipeline : IDisposable
{
    readonly object _gate = new();
    readonly SensorFaceRouter _router;
    readonly TouchTracker _tracker;
    readonly FrameSerializer _serializer;
    readonly List<ITouchSink> _sinks;
    readonly StatsCollector _stats;
    readonly FileLogger _log;
    readonly int _keepaliveIntervalMs;

    // 熱路徑預分配 buffer（receive 執行緒 + timer 執行緒共用，受 _gate 保護）
    readonly List<RawPoint> _rawPoints = new(16);
    readonly List<TouchEventData> _events = new(32);
    readonly List<int> _alive = new(128);
    RoutedPoint[] _routedBuf = new RoutedPoint[64];

    long _seq;
    long _lastPublishMs;

    public TouchPipeline(
        SensorFaceRouter router,
        TouchTracker tracker,
        FrameSerializer serializer,
        List<ITouchSink> sinks,
        StatsCollector stats,
        FileLogger log,
        int keepaliveIntervalMs)
    {
        _router = router;
        _tracker = tracker;
        _serializer = serializer;
        _sinks = sinks;
        _stats = stats;
        _log = log;
        _keepaliveIntervalMs = keepaliveIntervalMs;
    }

    public int ActiveTouches { get { lock (_gate) return _tracker.ActiveCount; } }

    /// <summary>receive 執行緒入口：一個 UDP datagram 全程處理。</summary>
    public void OnDatagram(ReadOnlySpan<byte> data, long nowMs, int sourcePort = 0)
    {
        long t0 = Stopwatch.GetTimestamp();
        LatencyProbe.Mark(LatencyProbe.EventType.UdpRecv, sourcePort, data.Length);
        _stats.OnDatagram();

        lock (_gate)
        {
            _rawPoints.Clear();
            var err = SensorPacketParser.TryParse(data, _rawPoints, out _);
            LatencyProbe.Mark(LatencyProbe.EventType.ParseDone, _rawPoints.Count, (int)err);

            if (err != PacketError.None)
            {
                _stats.OnParseError();
                _log.Debug($"[Pipeline] 封包解析失敗：{err}（len={data.Length} src={sourcePort}）");
                return;
            }

            _stats.OnPoints(_rawPoints.Count);

            // 面分類 + 座標映射
            if (_routedBuf.Length < _rawPoints.Count)
                _routedBuf = new RoutedPoint[_rawPoints.Count + 16];
            int routed = 0;
            for (int i = 0; i < _rawPoints.Count; i++)
            {
                if (_router.TryRoute(_rawPoints[i].X, _rawPoints[i].Y, out var p))
                    _routedBuf[routed++] = p;
                else
                    _stats.OnUnrouted();
            }
            if (routed == 0) return;

            // touch ID 追蹤（down / move）
            _events.Clear();
            _tracker.ProcessBatch(_routedBuf.AsSpan(0, routed), nowMs, _events);

            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].Phase == TouchPhase.Down) _stats.OnDown();
                else if (_events[i].Phase == TouchPhase.Move) _stats.OnMove();
            }

            if (_events.Count > 0)
                PublishLocked(nowMs);

            long elapsedUs = (Stopwatch.GetTimestamp() - t0) * 1_000_000 / Stopwatch.Frequency;
            _stats.OnProcessed(elapsedUs);
            LatencyProbe.Mark(LatencyProbe.EventType.Published, _events.Count, (int)elapsedUs);
        }
    }

    /// <summary>timer 執行緒入口：release timeout 掃描 + keepalive snapshot。</summary>
    public void Tick(long nowMs)
    {
        lock (_gate)
        {
            _events.Clear();
            _tracker.Flush(nowMs, _events);

            if (_events.Count > 0)
            {
                for (int i = 0; i < _events.Count; i++) _stats.OnUp();
                LatencyProbe.Mark(LatencyProbe.EventType.FlushTick, _events.Count);
                PublishLocked(nowMs);
            }
            else if (nowMs - _lastPublishMs >= _keepaliveIntervalMs)
            {
                // 每 1s 發全狀態 snapshot keepalive（events 空、alive 全清單）
                PublishLocked(nowMs);
            }
        }

        _stats.Tick(nowMs);
    }

    void PublishLocked(long nowMs)
    {
        _tracker.CollectAlive(_alive);
        var bytes = _serializer.Serialize(
            ++_seq,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            _events,
            _alive);

        for (int i = 0; i < _sinks.Count; i++)
            _sinks[i].Publish(bytes);

        _stats.OnFramePublished();
        _lastPublishMs = nowMs;
    }

    public void Dispose()
    {
        foreach (var s in _sinks) s.Dispose();
    }
}
