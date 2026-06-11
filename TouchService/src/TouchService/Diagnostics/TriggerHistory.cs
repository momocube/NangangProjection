using TouchService.Core;
using TouchService.Tracking;

namespace TouchService.Diagnostics;

/// <summary>
/// 運行期觸發歷史（per-cell 聚合統計）+ 疑似噪訊評分。
///
/// 為什麼存在：場域有「間歇型」噪訊晶片（發作一陣停一陣）。啟動自檢窗（autoLock）只能抓
/// 窗內在叫的；stdin `touches` 只看得到此刻活躍的——觸發結束 170ms 後就消失，操作者手速
/// 追不上。歷史讓事後可查，評分讓可疑晶片自動浮上排行（GET /history、stdin `history`、
/// UI 診斷 tab）。
///
/// 記錄單位 = touch 生命週期（down→up），所屬 cell 取 down 時的 face/cell。
/// 自檢窗內被抑制的觸發不進 tracker、沒有生命週期——在同一攔截點以 RecordSuppressed
/// 餵入，用 releaseTimeout 同款 gap 邏輯重建 per-cell 偽生命週期（單位因此與正常路徑一致：
/// 持續叫 10 秒 = 1 次觸發 + 10 秒活躍時長，而不是 125 個封包）。
/// 被鎖定的 cell 的觸發在 LockedCellFilter 就被擋掉、不會進到這裡——鎖定後歷史自然停止
/// 累積（鎖定前的歷史保留顯示），這是刻意行為。
///
/// 評分（0~100 整數，三個可解釋因子加權，權重見常數；設計依據見 docs/ARCHITECTURE.md §3）：
///   頻率 40：最近 30 分鐘觸發次數 / 30 飽和——正常遊玩單格不會被反覆觸發太多次，
///            間歇噪訊每次發作 +1、抖動型（反覆 up/down 換 ID）會幾十上百次。
///   孤立 35：down 當下 tracker 內無鄰近活躍 touch 的比率——真人腳掌/移動幾乎總是
///            多格連動（另一腳、上一步的觸點還活著），孤立觸發可疑。
///   佔用 25：總活躍時長占服務運行時間比率——噪訊晶片常駐叫。
/// 分數只是排序用，不自動鎖——營運中有真人在踩，誤鎖比漏鎖傷。
///
/// 鄰近判定註記：規格草案寫「±2 格」，但 matchRadiusPx=96（≈3 格）保證任何 96px 內的
/// 新點會被合併進既有 touch（不會成為獨立鄰居），±2 格內永遠不可能有「另一個」活躍
/// touch——故鄰近半徑取 2×matchRadiusPx（預設 192px ≈ 6 格，涵蓋另一腳/走路下一步），
/// 物理依據（多格連動）不變。另有追溯撤銷：B 在 A 仍活躍時於鄰近落下，A 的孤立記號
/// 一併撤銷（真人第二腳常晚幾十 ms 落地）。
///
/// 記憶體有界：per-cell 聚合為固定大小（30 分鐘 rolling 用 30 個分鐘桶環形陣列，
/// 不存個別事件），cell 數上限 = 場域晶片數（704 = 14,272）；進行中 touch 表上限
/// = tracker maxSlots。無任何 per-event 無界累積。
///
/// 執行緒模型：Record 路徑由 TouchPipeline 在 pipeline 鎖內呼叫，Snapshot 由
/// HTTP / stdin 執行緒呼叫——內部以自己的鎖序列化（臨界區皆為 O(cells) 以下，非熱點）。
/// 時間由呼叫端注入（nowMs，單調時鐘），方便確定性測試。
/// </summary>
public sealed class TriggerHistory
{
    // ── 評分權重（合計 100；改動要同步 docs/ARCHITECTURE.md §3 表）─────────────
    public const double WeightFrequency = 40;
    public const double WeightIsolation = 35;
    public const double WeightOccupancy = 25;
    /// <summary>頻率因子飽和點：最近 30 分鐘 30 次觸發 = 滿分（正常遊玩單格遠低於此）。</summary>
    public const double FrequencySaturationCount = 30;

    const int BucketCount = 30;   // 30 分鐘 rolling，一分鐘一桶

    /// <summary>排行快照的一筆 cell 聚合（依 score 降冪）。</summary>
    public sealed record CellEntry(
        int Face, int Col, int Row, int Score,
        int Count30Min, long TotalTouches,
        double IsolationRatio, double OccupancyRatio,
        long LastTriggerAgoMs, long LastLifetimeMs, long TotalActiveMs, bool Active);

    /// <summary>per-cell 聚合（固定大小：欄位 + 兩個 30 元素分鐘桶陣列，無 per-event 成長）。</summary>
    sealed class CellStats
    {
        public int Key, Face, Col, Row, PixelX, PixelY;
        public long TotalTouches;
        public long IsolatedTouches;
        public long TotalActiveMs;
        public long LastTriggerMs;
        public long LastLifetimeMs;
        public readonly int[] BucketMinute = new int[BucketCount];   // 桶對應的絕對分鐘（-1 = 空桶）
        public readonly int[] BucketHits = new int[BucketCount];

        // 自檢窗偽生命週期（RecordSuppressed 用）
        public bool SuppressedOpen;
        public bool SuppressedIsolated;   // 目前 open 偽 touch 是否被記為孤立（供追溯撤銷）
        public long SuppressedLastMs;
        public long SuppressedStartMs;

        public CellStats() { Array.Fill(BucketMinute, -1); }
    }

    /// <summary>進行中 touch（id → down 資訊；上限 = tracker maxSlots）。</summary>
    sealed class ActiveTouch
    {
        public int Key, Px, Py;
        public long DownMs;
        public bool Isolated;   // down 當下被記為孤立（供追溯撤銷）
    }

    readonly object _gate = new();
    readonly Dictionary<int, CellStats> _cells = new();
    readonly Dictionary<int, ActiveTouch> _active = new();
    readonly HashSet<int> _suppressedOpen = new();   // 自檢窗 open 偽 touch 的 cell key
    readonly long _startMs;
    readonly int _releaseTimeoutMs;
    readonly float _neighborRadiusSq;

    public TriggerHistory(long startNowMs, int releaseTimeoutMs, float neighborRadiusPx)
    {
        _startMs = startNowMs;
        _releaseTimeoutMs = Math.Max(1, releaseTimeoutMs);
        _neighborRadiusSq = neighborRadiusPx * neighborRadiusPx;
    }

    /// <summary>服務啟動時刻（佔用比率分母 = nowMs - StartMs）。</summary>
    public long StartMs => _startMs;

    /// <summary>已聚合的不同 cell 數（記憶體有界驗證用：上限 = 場域晶片數）。</summary>
    public int TrackedCellCount { get { lock (_gate) return _cells.Count; } }

    /// <summary>進行中 touch 數（記憶體有界驗證用：上限 = tracker maxSlots）。</summary>
    public int ActiveTouchCount { get { lock (_gate) return _active.Count; } }

    /// <summary>
    /// 餵入一批 touch 事件（TouchPipeline 在發布同一時點呼叫；down/up 入帳，move 只更新位置）。
    /// 兩段式：先註冊本批全部 down 再判孤立——同一 datagram 兩格同時落下時互為鄰居。
    /// </summary>
    public void OnEvents(IReadOnlyList<TouchEventData> events, long nowMs)
    {
        lock (_gate)
        {
            // pass 1：down 註冊 / move 更新位置 / up 收斂
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                switch (e.Phase)
                {
                    case TouchPhase.Down:
                    {
                        var c = GetOrAddCell(e.Point);
                        c.TotalTouches++;
                        c.LastTriggerMs = nowMs;
                        BumpBucket(c, nowMs);
                        _active[e.Id] = new ActiveTouch
                        {
                            Key = c.Key, Px = e.Point.PixelX, Py = e.Point.PixelY, DownMs = nowMs,
                        };
                        break;
                    }
                    case TouchPhase.Move:
                        if (_active.TryGetValue(e.Id, out var moving))
                        {
                            moving.Px = e.Point.PixelX;
                            moving.Py = e.Point.PixelY;
                            if (_cells.TryGetValue(moving.Key, out var mc)) mc.LastTriggerMs = nowMs;
                        }
                        break;
                    case TouchPhase.Up:
                        if (_active.Remove(e.Id, out var ended) &&
                            _cells.TryGetValue(ended.Key, out var ec))
                        {
                            long life = Math.Max(0, nowMs - ended.DownMs);
                            ec.TotalActiveMs += life;
                            ec.LastLifetimeMs = life;
                            ec.LastTriggerMs = nowMs;
                        }
                        break;
                }
            }

            // pass 2：本批 down 的孤立判定（含追溯撤銷鄰居的孤立記號）
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Phase != TouchPhase.Down) continue;
                if (!_active.TryGetValue(events[i].Id, out var self)) continue;

                bool hasNeighbor = false;
                foreach (var (otherId, other) in _active)
                {
                    if (otherId == events[i].Id || !NearSq(self.Px, self.Py, other.Px, other.Py)) continue;
                    hasNeighbor = true;
                    if (other.Isolated && _cells.TryGetValue(other.Key, out var oc))
                    {
                        other.Isolated = false;   // 追溯撤銷：真人第二腳/下一步晚落地
                        oc.IsolatedTouches--;
                    }
                }
                if (!hasNeighbor)
                {
                    self.Isolated = true;
                    _cells[self.Key].IsolatedTouches++;
                }
            }
        }
    }

    /// <summary>
    /// 自檢窗內被抑制的觸發（最重要的噪訊證據）：以 releaseTimeout gap 邏輯重建
    /// per-cell 偽生命週期——gap ≤ timeout 視為同一次觸發延續（累積活躍時長），
    /// 否則開新偽 touch（次數 +1）。孤立判定對象 = 其他 open 偽 touch（窗內 tracker 必空）。
    /// </summary>
    public void RecordSuppressed(in RoutedPoint p, long nowMs)
    {
        lock (_gate)
        {
            var c = GetOrAddCell(in p);

            if (c.SuppressedOpen && nowMs - c.SuppressedLastMs <= _releaseTimeoutMs)
            {
                // 延續：把封包間隔累進活躍時長
                c.TotalActiveMs += nowMs - c.SuppressedLastMs;
                c.LastLifetimeMs = nowMs - c.SuppressedStartMs;
            }
            else
            {
                // 新偽 touch
                c.TotalTouches++;
                c.SuppressedStartMs = nowMs;
                c.LastLifetimeMs = 0;
                BumpBucket(c, nowMs);

                bool hasNeighbor = false;
                foreach (var otherKey in _suppressedOpen)
                {
                    if (otherKey == c.Key || !_cells.TryGetValue(otherKey, out var o)) continue;
                    if (nowMs - o.SuppressedLastMs > _releaseTimeoutMs) continue;   // 已過期（懶清）
                    if (!NearSq(c.PixelX, c.PixelY, o.PixelX, o.PixelY)) continue;
                    hasNeighbor = true;
                    if (o.SuppressedIsolated)
                    {
                        o.SuppressedIsolated = false;   // 追溯撤銷（同腳第二顆晶片晚回報）
                        o.IsolatedTouches--;
                    }
                }
                c.SuppressedIsolated = !hasNeighbor;
                if (!hasNeighbor) c.IsolatedTouches++;
                _suppressedOpen.Add(c.Key);
            }

            c.SuppressedOpen = true;
            c.SuppressedLastMs = nowMs;
            c.LastTriggerMs = nowMs;
        }
    }

    /// <summary>
    /// 排行快照：依 score 降冪（同分依 30 分鐘次數、face/col/row 穩定排序）取前 top 筆。
    /// 進行中的 touch / 偽 touch 即時計入佔用時長、最後觸發顯示為「現在」。
    /// </summary>
    public IReadOnlyList<CellEntry> Snapshot(long nowMs, int top)
    {
        lock (_gate)
        {
            long uptimeMs = Math.Max(1, nowMs - _startMs);

            // 進行中 touch 的當下時長（尚未 up，但佔用必須即時反映——噪訊常駐叫不會 up）
            Dictionary<int, long>? running = null;
            foreach (var t in _active.Values)
            {
                running ??= new Dictionary<int, long>();
                running[t.Key] = running.GetValueOrDefault(t.Key) + Math.Max(0, nowMs - t.DownMs);
            }

            var list = new List<CellEntry>(_cells.Count);
            foreach (var c in _cells.Values)
            {
                long extraMs = running?.GetValueOrDefault(c.Key) ?? 0;
                bool active = extraMs > 0 ||
                              (c.SuppressedOpen && nowMs - c.SuppressedLastMs <= _releaseTimeoutMs);

                int count30 = CountRecent(c, nowMs);
                double isolation = c.TotalTouches > 0
                    ? Math.Clamp((double)c.IsolatedTouches / c.TotalTouches, 0, 1) : 0;
                double occupancy = Math.Clamp((double)(c.TotalActiveMs + extraMs) / uptimeMs, 0, 1);

                list.Add(new CellEntry(
                    c.Face, c.Col, c.Row,
                    ScoreOf(count30, isolation, occupancy),
                    count30, c.TotalTouches, isolation, occupancy,
                    active ? 0 : Math.Max(0, nowMs - c.LastTriggerMs),
                    extraMs > 0 ? extraMs : c.LastLifetimeMs,
                    c.TotalActiveMs + extraMs, active));
            }

            list.Sort((a, b) =>
                a.Score != b.Score ? b.Score.CompareTo(a.Score) :
                a.Count30Min != b.Count30Min ? b.Count30Min.CompareTo(a.Count30Min) :
                a.Face != b.Face ? a.Face.CompareTo(b.Face) :
                a.Col != b.Col ? a.Col.CompareTo(b.Col) : a.Row.CompareTo(b.Row));

            if (top > 0 && list.Count > top) list.RemoveRange(top, list.Count - top);
            return list;
        }
    }

    /// <summary>疑似噪訊評分（0~100 整數）。因子定義與權重依據見類別註解 / ARCHITECTURE §3。</summary>
    public static int ScoreOf(int count30Min, double isolationRatio, double occupancyRatio)
    {
        double frequency = Math.Min(1.0, count30Min / FrequencySaturationCount);
        double score = WeightFrequency * frequency
                     + WeightIsolation * Math.Clamp(isolationRatio, 0, 1)
                     + WeightOccupancy * Math.Clamp(occupancyRatio, 0, 1);
        return (int)Math.Clamp(Math.Round(score), 0, 100);
    }

    // ── 內部 ────────────────────────────────────────────────────────────────

    CellStats GetOrAddCell(in RoutedPoint p)
    {
        int key = Pack(p.FaceIndex, p.Col, p.Row);
        if (!_cells.TryGetValue(key, out var c))
        {
            c = new CellStats
            {
                Key = key, Face = p.FaceIndex, Col = p.Col, Row = p.Row,
                PixelX = p.PixelX, PixelY = p.PixelY,
            };
            _cells[key] = c;
        }
        return c;
    }

    /// <summary>30 分鐘 rolling：分鐘桶環形陣列（桶記絕對分鐘，過期懶覆寫——固定大小）。</summary>
    void BumpBucket(CellStats c, long nowMs)
    {
        int minute = MinuteOf(nowMs);
        int idx = minute % BucketCount;
        if (c.BucketMinute[idx] != minute)
        {
            c.BucketMinute[idx] = minute;
            c.BucketHits[idx] = 0;
        }
        c.BucketHits[idx]++;
    }

    int CountRecent(CellStats c, long nowMs)
    {
        int minute = MinuteOf(nowMs);
        int sum = 0;
        for (int i = 0; i < BucketCount; i++)
        {
            int m = c.BucketMinute[i];
            if (m >= 0 && minute - m < BucketCount) sum += c.BucketHits[i];
        }
        return sum;
    }

    int MinuteOf(long nowMs) => (int)Math.Max(0, (nowMs - _startMs) / 60_000);

    bool NearSq(int ax, int ay, int bx, int by)
    {
        float dx = ax - bx, dy = ay - by;
        return dx * dx + dy * dy <= _neighborRadiusSq;
    }

    static int Pack(int face, int col, int row) => (face << 20) | (col << 10) | row;   // 同 LockedCellFilter
}
