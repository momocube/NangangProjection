using TouchService.Core;

namespace TouchService.Tracking;

public enum TouchPhase { Down, Move, Up }

/// <summary>一筆 touch 生命週期事件（對外 schema 的 events[] 元素）。</summary>
public readonly struct TouchEventData
{
    public required int Id { get; init; }
    public required TouchPhase Phase { get; init; }
    public required RoutedPoint Point { get; init; }
}

/// <summary>stdin `touches` 指令用的活躍 touch 快照（現場找噪訊晶片：看誰存活異常久且不動）。</summary>
public readonly struct ActiveTouchSnapshot
{
    public required int Id { get; init; }
    public required RoutedPoint Point { get; init; }
    /// <summary>down 至今的存活時間（ms）。</summary>
    public required long AgeMs { get; init; }
}

/// <summary>
/// Touch ID 追蹤器：slot 最近鄰配對。
/// 核心配對演算法移植自 Unity 專案 Assets/_Script/Sensor/SensorBridgeToPassPosition.cs:133-212
/// （MatchAndUpdate，零 GC 設計），改動：
///   - 丟掉 PPTM / 位置平滑 / Screen.height 補償 / 淡出（fade）段
///   - 配對單位從「每幀 hits」改為「每 datagram points」（事件驅動，無幀概念）
///   - 配對策略從 slot-major 貪婪改為「全域最近 pair 優先」貪婪：
///     Unity 每幀的 batch 必含所有活躍點，slot-major 夠用；事件驅動下一個 datagram
///     常只有 1 點，slot-major 會變成「陣列順序優先」——兩點交錯靠近時前面的 slot
///     會偷走別人的點導致 ID 互換。改成把 (slot, point) 依距離排序後貪婪配對，
///     單點 batch 也能配到「最近的 slot」而非「最前面的 slot」
///   - 配對半徑從格子單位改為畫布像素（config matchRadiusPx，預設 96 ≈ 704 場域 3 格）
///   - MAX_SLOTS 從 const 20 改為 config（預設 128）
///   - 逾時直接發 Up（release timeout 由 config 指定，預設 170ms；rationale 見 PROTOCOL.md）
///
/// 生命週期語意：
///   down = 該點第一個封包當下立即發（零等待）
///   move = 同 touch ID 座標變化
///   up   = 最後封包後 releaseTimeoutMs 無新封包
///
/// 非執行緒安全：呼叫端（TouchPipeline）負責用同一把鎖序列化 ProcessBatch / Flush。
/// 時間由呼叫端注入（nowMs，單調時鐘），方便確定性測試。
/// </summary>
public sealed class TouchTracker
{
    struct TouchSlot
    {
        public bool        Active;
        public int         Id;
        public long        FirstSeenMs;
        public long        LastSeenMs;
        public RoutedPoint Last;
    }

    struct PairEntry
    {
        public float DistSq;
        public int Slot;
        public int Point;
    }

    sealed class PairComparer : IComparer<PairEntry>
    {
        public static readonly PairComparer Instance = new();
        public int Compare(PairEntry a, PairEntry b) => a.DistSq.CompareTo(b.DistSq);
    }

    readonly TouchSlot[] _slots;
    readonly bool[] _slotMatched;
    readonly float _matchRadiusSq;
    readonly int   _releaseTimeoutMs;
    readonly int   _moveMinDistancePx;

    // 預分配，避免熱路徑 alloc（對齊 Unity _hitUsed 的零 GC 設計）
    bool[] _pointUsed = new bool[64];
    PairEntry[] _pairBuf = new PairEntry[256];
    int _nextId = 1;
    int _activeCount;

    public TouchTracker(int maxSlots, float matchRadiusPx, int releaseTimeoutMs, int moveMinDistancePx = 0)
    {
        if (maxSlots <= 0) throw new ArgumentOutOfRangeException(nameof(maxSlots));
        _slots = new TouchSlot[maxSlots];
        _slotMatched = new bool[maxSlots];
        _matchRadiusSq = matchRadiusPx * matchRadiusPx;
        _releaseTimeoutMs = releaseTimeoutMs;
        _moveMinDistancePx = moveMinDistancePx;
    }

    public int ActiveCount => _activeCount;
    public int ReleaseTimeoutMs => _releaseTimeoutMs;

    /// <summary>
    /// 處理同一個 datagram 解析出的一批點（同模組合包，物理上同時發生）。
    /// 產生的 Down / Move 事件 append 到 eventsOut（不清空，呼叫端負責）。
    /// </summary>
    public void ProcessBatch(ReadOnlySpan<RoutedPoint> points, long nowMs, List<TouchEventData> eventsOut)
    {
        int n = points.Length;
        if (n == 0) return;

        if (n > _pointUsed.Length) _pointUsed = new bool[n + 16];
        Array.Clear(_pointUsed, 0, n);
        Array.Clear(_slotMatched, 0, _slotMatched.Length);

        // Step 1：收集所有半徑內的 (slot, point) 候選 pair，依距離排序後貪婪配對。
        // 全域最近 pair 優先：兩點交錯靠近時各自配回最近的 slot，ID 不互換。
        int pairCount = 0;
        for (int s = 0; s < _slots.Length; s++)
        {
            if (!_slots[s].Active) continue;
            for (int p = 0; p < n; p++)
            {
                float dx = points[p].PixelX - _slots[s].Last.PixelX;
                float dy = points[p].PixelY - _slots[s].Last.PixelY;
                float d  = dx * dx + dy * dy;
                if (d >= _matchRadiusSq) continue;

                if (pairCount == _pairBuf.Length)
                    Array.Resize(ref _pairBuf, _pairBuf.Length * 2);
                _pairBuf[pairCount++] = new PairEntry { DistSq = d, Slot = s, Point = p };
            }
        }

        if (pairCount > 1)
            Array.Sort(_pairBuf, 0, pairCount, PairComparer.Instance);

        for (int i = 0; i < pairCount; i++)
        {
            int s = _pairBuf[i].Slot;
            int p = _pairBuf[i].Point;
            if (_slotMatched[s] || _pointUsed[p]) continue;

            _slotMatched[s] = true;
            _pointUsed[p] = true;

            var prev = _slots[s].Last;
            _slots[s].Last       = points[p];
            _slots[s].LastSeenMs = nowMs;

            int mdx = points[p].PixelX - prev.PixelX;
            int mdy = points[p].PixelY - prev.PixelY;
            bool moved = (mdx != 0 || mdy != 0) &&
                         (mdx * mdx + mdy * mdy >= _moveMinDistancePx * _moveMinDistancePx);
            if (moved)
            {
                eventsOut.Add(new TouchEventData
                {
                    Id = _slots[s].Id,
                    Phase = TouchPhase.Move,
                    Point = points[p],
                });
            }
        }

        // Step 2：剩餘未配對的點 → 已被某 active slot 半徑涵蓋則丟棄（同足多晶片去重），
        //          否則開新 slot 並立即發 Down（零等待）
        for (int p = 0; p < n; p++)
        {
            if (_pointUsed[p]) continue;

            bool covered = false;
            for (int s = 0; s < _slots.Length && !covered; s++)
            {
                if (!_slots[s].Active) continue;
                float dx = points[p].PixelX - _slots[s].Last.PixelX;
                float dy = points[p].PixelY - _slots[s].Last.PixelY;
                if (dx * dx + dy * dy < _matchRadiusSq) covered = true;
            }
            if (covered) continue;

            int free = FindFreeSlot();
            if (free < 0) break;   // slot 滿（maxSlots），丟棄新點

            _slots[free].Active      = true;
            _slots[free].Id          = _nextId++;
            _slots[free].FirstSeenMs = nowMs;
            _slots[free].LastSeenMs  = nowMs;
            _slots[free].Last        = points[p];
            _activeCount++;

            eventsOut.Add(new TouchEventData
            {
                Id = _slots[free].Id,
                Phase = TouchPhase.Down,
                Point = points[p],
            });
        }
    }

    /// <summary>掃描逾時 slot，發 Up 並停用。由 timer 執行緒定期呼叫。</summary>
    public void Flush(long nowMs, List<TouchEventData> eventsOut)
    {
        for (int s = 0; s < _slots.Length; s++)
        {
            if (!_slots[s].Active) continue;
            if (nowMs - _slots[s].LastSeenMs <= _releaseTimeoutMs) continue;

            _slots[s].Active = false;
            _activeCount--;

            eventsOut.Add(new TouchEventData
            {
                Id = _slots[s].Id,
                Phase = TouchPhase.Up,
                Point = _slots[s].Last,
            });
        }
    }

    /// <summary>收集目前所有活躍 touch ID（schema 的 alive 清單）。</summary>
    public void CollectAlive(List<int> idsOut)
    {
        idsOut.Clear();
        for (int s = 0; s < _slots.Length; s++)
            if (_slots[s].Active) idsOut.Add(_slots[s].Id);
    }

    /// <summary>收集所有活躍 touch 細節（id / 座標 / 存活時間）。stdin `touches` 指令用，非熱路徑。</summary>
    public void CollectAliveDetails(long nowMs, List<ActiveTouchSnapshot> touchesOut)
    {
        touchesOut.Clear();
        for (int s = 0; s < _slots.Length; s++)
        {
            if (!_slots[s].Active) continue;
            touchesOut.Add(new ActiveTouchSnapshot
            {
                Id = _slots[s].Id,
                Point = _slots[s].Last,
                AgeMs = nowMs - _slots[s].FirstSeenMs,
            });
        }
    }

    int FindFreeSlot()
    {
        for (int i = 0; i < _slots.Length; i++)
            if (!_slots[i].Active) return i;
        return -1;
    }
}
