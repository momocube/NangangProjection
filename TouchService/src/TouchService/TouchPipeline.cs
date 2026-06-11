using System.Diagnostics;
using TouchService.Core;
using TouchService.Diagnostics;
using TouchService.Logging;
using TouchService.Output;
using TouchService.Tracking;

namespace TouchService;

/// <summary>
/// typed touch 事件觀察者：在 frame 發布的同一時點收到 List&lt;TouchEventData&gt;
/// （滑鼠模式的 MouseDriver 用——額外 sink，不影響 WS/UDP 發布）。
/// 在 pipeline 鎖內、熱路徑上被呼叫：實作必須快速返回、不得保留 events 參考
/// （底層是重用 buffer）。
/// </summary>
public interface ITouchEventObserver
{
    void OnTouchEvents(IReadOnlyList<TouchEventData> events);
}

/// <summary>
/// 處理管線：datagram → parse → route → map → track → publish。
/// 事件驅動——receive 執行緒直接走完整條（取代 Unity 的 queue + 每幀派送），
/// down 事件在首包當下即發布（零等待）。
/// timer 執行緒走 Tick()：release timeout 掃描（up）+ keepalive snapshot。
/// 兩條路徑用同一把鎖序列化。
///
/// 啟動自檢窗（autoLock 非 null 時）：窗內路由成功的點只做 per-cell 計數、
/// 不進 tracker、不發事件（keepalive 照發）；窗結束時達門檻的格子加入 _lockedCells
/// （LockSource.Auto），之後行為完全恢復正常。
/// </summary>
public sealed class TouchPipeline : IDisposable
{
    readonly object _gate = new();
    readonly SensorFaceRouter _router;
    readonly TouchTracker _tracker;
    readonly FrameSerializer _serializer;
    readonly LockedCellFilter _lockedCells;
    readonly List<ITouchSink> _sinks;
    readonly StatsCollector _stats;
    readonly FileLogger _log;
    readonly int _keepaliveIntervalMs;
    readonly AutoLockSelfCheck? _autoLock;
    int _autoLockLastLoggedSec = int.MaxValue;
    ITouchEventObserver? _observer;   // 滑鼠模式 sink（null = grid，零開銷）；受 _gate 保護

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
        int keepaliveIntervalMs,
        LockedCellFilter? lockedCells = null,
        AutoLockSelfCheck? autoLock = null)
    {
        _router = router;
        _tracker = tracker;
        _serializer = serializer;
        _lockedCells = lockedCells ?? LockedCellFilter.Empty;
        _sinks = sinks;
        _stats = stats;
        _log = log;
        _keepaliveIntervalMs = keepaliveIntervalMs;
        _autoLock = autoLock;
    }

    /// <summary>自檢窗結束時觸發（參數 = 本次自動鎖定的格子，可為空）。host 用來推 WS locks 訊息。</summary>
    public event Action<IReadOnlyList<AutoLockSelfCheck.Result>>? AutoLockCompleted;

    /// <summary>
    /// 掛/卸 typed 事件觀察者（滑鼠模式熱切）。null = 卸下（grid 模式，零開銷）。
    /// 發布端（WS/UDP sink）完全不受影響——觀察者是「額外的」，不是「取代」。
    /// </summary>
    public void SetEventObserver(ITouchEventObserver? observer)
    {
        lock (_gate) _observer = observer;
    }

    /// <summary>
    /// 熱套用追蹤參數（POST /config：matchRadiusPx / releaseTimeoutMs），下個 touch / flush 生效。
    /// </summary>
    public void UpdateTuning(float matchRadiusPx, int releaseTimeoutMs)
    {
        lock (_gate) _tracker.UpdateTuning(matchRadiusPx, releaseTimeoutMs);
    }

    public int ActiveTouches { get { lock (_gate) return _tracker.ActiveCount; } }

    /// <summary>自檢窗狀態（hello 的 autoLockActive / autoLockRemainingSeconds 用）。</summary>
    public (bool Active, int RemainingSeconds) GetAutoLockStatus(long nowMs)
    {
        lock (_gate)
        {
            return _autoLock is { Active: true }
                ? (true, _autoLock.RemainingSeconds(nowMs))
                : (false, 0);
        }
    }

    /// <summary>stdin `touches` 指令：複製當下活躍 touch 清單（現場找噪訊晶片用，非熱路徑）。</summary>
    public void SnapshotTouches(long nowMs, List<ActiveTouchSnapshot> touchesOut)
    {
        lock (_gate) _tracker.CollectAliveDetails(nowMs, touchesOut);
    }

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
                {
                    // 鎖定晶片（噪訊）過濾：進 tracker 前丟棄（config lockedCells）
                    if (_lockedCells.IsLocked(p.FaceIndex, p.Col, p.Row))
                    {
                        _stats.OnDroppedByLock();
                        continue;
                    }
                    _routedBuf[routed++] = p;
                }
                else
                    _stats.OnUnrouted();
            }
            // 啟動自檢窗：窗內只計數不發布；窗剛過先收斂結果再正常處理本包
            if (_autoLock is { Active: true })
            {
                if (!_autoLock.IsExpired(nowMs))
                {
                    for (int i = 0; i < routed; i++)
                        _autoLock.Record(in _routedBuf[i]);
                    return;
                }
                FinishAutoLockLocked();
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

    /// <summary>timer 執行緒入口：release timeout 掃描 + keepalive snapshot + 自檢窗到期收斂。</summary>
    public void Tick(long nowMs)
    {
        lock (_gate)
        {
            if (_autoLock is { Active: true })
            {
                if (_autoLock.IsExpired(nowMs)) FinishAutoLockLocked();
                else
                {
                    // 倒數提示：每秒一行（5ms tick 下用秒數變化去抖，不洗版）
                    int rem = _autoLock.RemainingSeconds(nowMs);
                    if (rem != _autoLockLastLoggedSec)
                    {
                        _autoLockLastLoggedSec = rem;
                        _log.Info($"[AutoLock] 自檢中…剩 {rem}s（已觀察 {_autoLock.ObservedCells} 格 / {_autoLock.TotalPackets} 包）");
                    }
                }
            }

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

    /// <summary>
    /// 自檢窗收斂（持 _gate 呼叫）：達門檻的格子加入 runtime 黑名單（auto 來源）+ 醒目 log。
    /// AutoLockCompleted 在鎖內觸發——與 PublishLocked 在鎖內做 sink IO 的既有模式一致，
    /// 且一個 process 生命週期只發生一次。
    /// </summary>
    void FinishAutoLockLocked()
    {
        var (toLock, below) = _autoLock!.Finish();

        foreach (var r in toLock)
            _lockedCells.TryAdd(r.Face, r.Col, r.Row, LockSource.Auto, r.Packets);

        if (toLock.Count == 0)
        {
            _log.Info("[AutoLock] ══════════ 自檢通過 ══════════");
            _log.Info($"[AutoLock] 自檢窗結束：沒有格子達到鎖定門檻（≥{_autoLock.MinPackets} 包），未自動鎖定任何晶片。");
        }
        else
        {
            _log.Warn("[AutoLock] ══════════ 自檢結果：發現噪訊晶片 ══════════");
            _log.Warn($"[AutoLock] 自動鎖定 {toLock.Count} 顆（窗內 ≥{_autoLock.MinPackets} 包；與 config lockedCells 同一過濾生效）：");
            foreach (var r in toLock)
                _log.Warn($"[AutoLock]   face={r.Face} cell=({r.Col},{r.Row}) 封包 {r.Packets}");
            _log.Warn("[AutoLock] 誤鎖（重啟時有人站在場上）用 stdin `unlock <face> <col> <row>` 解；`savelocks` 可寫回 config。");
        }
        if (below.Count > 0)
        {
            var list = string.Join("、", below.Select(r => $"face={r.Face} cell=({r.Col},{r.Row}) {r.Packets} 包"));
            _log.Info($"[AutoLock] 另觀察到 {below.Count} 格未達門檻、不鎖：{list}");
        }
        _log.Info("[AutoLock] 自檢窗結束，觸控事件發布恢復正常。");

        AutoLockCompleted?.Invoke(toLock);
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

        // 滑鼠模式觀察者（額外 sink）：keepalive 空 frame 不需要通知
        if (_events.Count > 0)
            _observer?.OnTouchEvents(_events);

        _stats.OnFramePublished();
        _lastPublishMs = nowMs;
    }

    public void Dispose()
    {
        foreach (var s in _sinks) s.Dispose();
    }
}
