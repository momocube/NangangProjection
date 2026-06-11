using TouchService.Config;
using TouchService.Core;
using TouchService.Diagnostics;
using TouchService.Logging;
using TouchService.Output;
using TouchService.Tracking;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// 觸發歷史（TriggerHistory）：per-cell 聚合（down/up、30 分鐘 rolling、孤立、佔用）
/// + 疑似噪訊評分 + 自檢窗內被抑制觸發的計入 + 記憶體有界。
/// 時間全部注入（nowMs），無 sleep、確定性。
/// </summary>
public class TriggerHistoryTests
{
    const int Timeout = 170;
    const float NeighborRadius = 192f;   // = 2×matchRadiusPx 預設

    static TriggerHistory NewHistory(long startMs = 0) => new(startMs, Timeout, NeighborRadius);

    static RoutedPoint Pt(int face, int col, int row, int px, int py) => new()
    {
        FaceIndex = face, Col = col, Row = row, PixelX = px, PixelY = py, NormX = 0, NormY = 0,
    };

    static TouchEventData Ev(int id, TouchPhase phase, RoutedPoint p) =>
        new() { Id = id, Phase = phase, Point = p };

    static void One(TriggerHistory h, long nowMs, params TouchEventData[] events) =>
        h.OnEvents(events, nowMs);

    static TriggerHistory.CellEntry Find(TriggerHistory h, long nowMs, int face, int col, int row)
    {
        var e = h.Snapshot(nowMs, 0).FirstOrDefault(x => x.Face == face && x.Col == col && x.Row == row);
        Assert.NotNull(e);
        return e!;
    }

    // ── down / up 聚合 ───────────────────────────────────────────────────────

    [Fact]
    public void down與up_聚合到down時的cell_存活與佔用正確()
    {
        var h = NewHistory();
        var cell = Pt(2, 7, 13, 869, 850);

        One(h, 1000, Ev(1, TouchPhase.Down, cell));
        One(h, 1500, Ev(1, TouchPhase.Up, cell));

        var e = Find(h, 2000, 2, 7, 13);
        Assert.Equal(1, e.TotalTouches);
        Assert.Equal(1, e.Count30Min);
        Assert.Equal(500, e.LastLifetimeMs);
        Assert.Equal(500, e.TotalActiveMs);
        Assert.Equal(0.25, e.OccupancyRatio, 3);     // 500ms / 2000ms
        Assert.Equal(500, e.LastTriggerAgoMs);       // up 於 1500、查詢於 2000
        Assert.False(e.Active);
    }

    [Fact]
    public void 進行中touch_佔用即時累計_最後觸發顯示進行中()
    {
        var h = NewHistory();
        One(h, 0, Ev(1, TouchPhase.Down, Pt(2, 7, 13, 869, 850)));

        var e = Find(h, 10_000, 2, 7, 13);   // 沒 up：噪訊常駐叫的樣貌
        Assert.True(e.Active);
        Assert.Equal(0, e.LastTriggerAgoMs);
        Assert.Equal(10_000, e.TotalActiveMs);
        Assert.Equal(1.0, e.OccupancyRatio, 2);
        Assert.Equal(1, h.ActiveTouchCount);

        One(h, 12_000, Ev(1, TouchPhase.Up, Pt(2, 7, 13, 869, 850)));
        Assert.Equal(0, h.ActiveTouchCount);
        Assert.Equal(12_000, Find(h, 12_000, 2, 7, 13).LastLifetimeMs);
    }

    [Fact]
    public void move換cell_仍記在down時的cell()
    {
        var h = NewHistory();
        One(h, 0, Ev(1, TouchPhase.Down, Pt(2, 7, 13, 869, 850)));
        One(h, 100, Ev(1, TouchPhase.Move, Pt(2, 8, 13, 901, 850)));   // 滑到隔壁格
        One(h, 300, Ev(1, TouchPhase.Up, Pt(2, 8, 13, 901, 850)));

        var snap = h.Snapshot(1000, 0);
        var e = Assert.Single(snap);             // 只有 down 的 cell 入帳，move 不開新 cell
        Assert.Equal((2, 7, 13), (e.Face, e.Col, e.Row));
        Assert.Equal(300, e.LastLifetimeMs);
    }

    // ── 30 分鐘 rolling（分鐘桶環形陣列）────────────────────────────────────

    [Fact]
    public void rolling30分鐘_正確衰減_總次數不變()
    {
        var h = NewHistory();
        var cell = Pt(2, 7, 13, 869, 850);

        // 第 0~4 分鐘各一次 down/up（共 5 次）
        for (int m = 0; m < 5; m++)
        {
            One(h, m * 60_000L, Ev(m + 1, TouchPhase.Down, cell));
            One(h, m * 60_000L + 200, Ev(m + 1, TouchPhase.Up, cell));
        }

        Assert.Equal(5, Find(h, 10 * 60_000L, 2, 7, 13).Count30Min);    // 第 10 分鐘：全在窗內
        Assert.Equal(1, Find(h, 33 * 60_000L, 2, 7, 13).Count30Min);    // 第 33 分鐘：只剩第 4 分鐘那筆
        var e = Find(h, 34 * 60_000L, 2, 7, 13);                        // 第 34 分鐘：全部滾出窗
        Assert.Equal(0, e.Count30Min);
        Assert.Equal(5, e.TotalTouches);                                // 總次數不衰減
    }

    [Fact]
    public void rolling桶環形覆寫_超過30分鐘後舊桶被新分鐘重用()
    {
        var h = NewHistory();
        var cell = Pt(0, 1, 1, 100, 700);

        One(h, 0, Ev(1, TouchPhase.Down, cell));                  // 第 0 分鐘
        One(h, 200, Ev(1, TouchPhase.Up, cell));
        One(h, 30 * 60_000L, Ev(2, TouchPhase.Down, cell));       // 第 30 分鐘：同一個桶 index
        One(h, 30 * 60_000L + 200, Ev(2, TouchPhase.Up, cell));

        var e = Find(h, 30 * 60_000L + 1000, 0, 1, 1);
        Assert.Equal(1, e.Count30Min);    // 第 0 分鐘那筆已出窗，桶被第 30 分鐘覆寫
        Assert.Equal(2, e.TotalTouches);
    }

    // ── 孤立因子 ─────────────────────────────────────────────────────────────

    [Fact]
    public void 單獨down_無鄰近活躍touch_記為孤立()
    {
        var h = NewHistory();
        One(h, 0, Ev(1, TouchPhase.Down, Pt(2, 7, 13, 869, 850)));
        One(h, 300, Ev(1, TouchPhase.Up, Pt(2, 7, 13, 869, 850)));

        Assert.Equal(1.0, Find(h, 1000, 2, 7, 13).IsolationRatio, 3);
    }

    [Fact]
    public void 同批兩格鄰近down_互為鄰居_皆不孤立()
    {
        var h = NewHistory();
        // 同一 datagram 兩點（>96px 不合併、<192px 互為鄰居——真人雙腳）
        One(h, 0,
            Ev(1, TouchPhase.Down, Pt(2, 10, 20, 500, 500)),
            Ev(2, TouchPhase.Down, Pt(2, 14, 20, 628, 500)));
        One(h, 300,
            Ev(1, TouchPhase.Up, Pt(2, 10, 20, 500, 500)),
            Ev(2, TouchPhase.Up, Pt(2, 14, 20, 628, 500)));

        Assert.Equal(0.0, Find(h, 1000, 2, 10, 20).IsolationRatio, 3);
        Assert.Equal(0.0, Find(h, 1000, 2, 14, 20).IsolationRatio, 3);
    }

    [Fact]
    public void 追溯撤銷_第二腳晚落地_先落的孤立記號被撤銷()
    {
        var h = NewHistory();
        One(h, 0, Ev(1, TouchPhase.Down, Pt(2, 10, 20, 1000, 1000)));    // 第一腳：當下孤立
        One(h, 60, Ev(2, TouchPhase.Down, Pt(2, 13, 20, 1100, 1000)));   // 第二腳 60ms 後鄰近落地
        One(h, 500,
            Ev(1, TouchPhase.Up, Pt(2, 10, 20, 1000, 1000)),
            Ev(2, TouchPhase.Up, Pt(2, 13, 20, 1100, 1000)));

        Assert.Equal(0.0, Find(h, 1000, 2, 10, 20).IsolationRatio, 3);   // 已撤銷
        Assert.Equal(0.0, Find(h, 1000, 2, 13, 20).IsolationRatio, 3);
    }

    [Fact]
    public void 遠處touch不算鄰居_仍孤立()
    {
        var h = NewHistory();
        One(h, 0, Ev(1, TouchPhase.Down, Pt(2, 10, 20, 500, 500)));
        One(h, 50, Ev(2, TouchPhase.Down, Pt(2, 40, 20, 1500, 500)));    // 1000px 外：不相干
        One(h, 400,
            Ev(1, TouchPhase.Up, Pt(2, 10, 20, 500, 500)),
            Ev(2, TouchPhase.Up, Pt(2, 40, 20, 1500, 500)));

        Assert.Equal(1.0, Find(h, 1000, 2, 10, 20).IsolationRatio, 3);
        Assert.Equal(1.0, Find(h, 1000, 2, 40, 20).IsolationRatio, 3);
    }

    // ── 自檢窗內被抑制的觸發（RecordSuppressed）────────────────────────────

    [Fact]
    public void 自檢窗suppressed_gap邏輯重建偽生命週期_單位與正常路徑一致()
    {
        var h = NewHistory();
        var p = Pt(2, 7, 13, 869, 850);

        // 噪訊晶片 80ms 重送 ×11（0~800ms）：1 次觸發 + 800ms 活躍，不是 11 次
        for (int i = 0; i <= 10; i++) h.RecordSuppressed(in p, i * 80);

        var e = Find(h, 900, 2, 7, 13);
        Assert.Equal(1, e.TotalTouches);
        Assert.Equal(1, e.Count30Min);
        Assert.Equal(800, e.TotalActiveMs);
        Assert.Equal(800, e.LastLifetimeMs);
        Assert.True(e.Active);            // 最後一包在 timeout 內：進行中

        // gap > releaseTimeout → 第二次觸發（間歇型噪訊的「下一次發作」）
        h.RecordSuppressed(in p, 2000);
        Assert.Equal(2, Find(h, 2100, 2, 7, 13).TotalTouches);
    }

    [Fact]
    public void 自檢窗suppressed_孤立與鄰近連動判定()
    {
        var h = NewHistory();
        var lone = Pt(2, 7, 13, 869, 850);       // 孤立噪訊
        var footA = Pt(0, 5, 60, 160, 1600);     // 同一隻腳的兩顆鄰近晶片（32px 相距）
        var footB = Pt(0, 6, 60, 192, 1600);

        h.RecordSuppressed(in lone, 0);
        h.RecordSuppressed(in footA, 0);
        h.RecordSuppressed(in footB, 40);        // 晚 40ms 回報：追溯撤銷 footA 的孤立

        Assert.Equal(1.0, Find(h, 500, 2, 7, 13).IsolationRatio, 3);
        Assert.Equal(0.0, Find(h, 500, 0, 5, 60).IsolationRatio, 3);
        Assert.Equal(0.0, Find(h, 500, 0, 6, 60).IsolationRatio, 3);
    }

    // ── 評分 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void 評分_連續狂叫孤立cell顯著高於正常多格連動遊玩()
    {
        var h = NewHistory();
        var noisy = Pt(2, 7, 13, 869, 850);

        // 間歇噪訊晶片：每 30 秒發作一次、每次叫 10 秒，持續 30 分鐘（孤立、無鄰近連動）
        for (int k = 0; k < 60; k++)
        {
            long t = k * 30_000L;
            One(h, t, Ev(1000 + k, TouchPhase.Down, noisy));
            One(h, t + 10_000, Ev(1000 + k, TouchPhase.Up, noisy));
        }

        // 正常遊玩：雙腳連動（兩格同批 down）、每次踩 600ms、每 5 分鐘一輪
        var footA = Pt(2, 20, 40, 1300, 1280);
        var footB = Pt(2, 23, 40, 1396, 1280);
        for (int k = 0; k < 6; k++)
        {
            long t = k * 300_000L + 7_000;
            One(h, t,
                Ev(2000 + k * 2, TouchPhase.Down, footA),
                Ev(2001 + k * 2, TouchPhase.Down, footB));
            One(h, t + 600,
                Ev(2000 + k * 2, TouchPhase.Up, footA),
                Ev(2001 + k * 2, TouchPhase.Up, footB));
        }

        long now = 30 * 60_000L;
        var noisyEntry = Find(h, now, 2, 7, 13);
        var playEntry = Find(h, now, 2, 20, 40);

        Assert.True(noisyEntry.Score >= 80,
            $"噪訊 cell score {noisyEntry.Score} 應 ≥ 80（高度可疑門檻）");
        Assert.True(playEntry.Score <= 40,
            $"正常遊玩 cell score {playEntry.Score} 應 ≤ 40");
        Assert.True(noisyEntry.Score - playEntry.Score >= 40,
            $"噪訊 {noisyEntry.Score} 與正常 {playEntry.Score} 的差距應顯著（≥ 40）");

        // 排行：噪訊 cell 必須排第一
        Assert.Equal((2, 7, 13), (h.Snapshot(now, 1)[0].Face, h.Snapshot(now, 1)[0].Col, h.Snapshot(now, 1)[0].Row));
    }

    [Fact]
    public void 評分函式_三因子加權與飽和()
    {
        Assert.Equal(0, TriggerHistory.ScoreOf(0, 0, 0));
        Assert.Equal(100, TriggerHistory.ScoreOf(30, 1.0, 1.0));      // 全飽和
        Assert.Equal(100, TriggerHistory.ScoreOf(500, 1.0, 1.0));     // 頻率超過飽和點不再加分
        Assert.Equal(40, TriggerHistory.ScoreOf(30, 0, 0));           // 純頻率
        Assert.Equal(35, TriggerHistory.ScoreOf(0, 1.0, 0));          // 純孤立
        Assert.Equal(25, TriggerHistory.ScoreOf(0, 0, 1.0));          // 純佔用
        // 常駐叫但只算一次觸發的晶片（孤立 + 佔滿）：≥ 60「看一眼」門檻
        Assert.True(TriggerHistory.ScoreOf(1, 1.0, 1.0) >= 60);
    }

    // ── 記憶體有界 ───────────────────────────────────────────────────────────

    [Fact]
    public void 記憶體有界_聚合結構大小不隨事件數成長()
    {
        var h = NewHistory();
        var a = Pt(2, 7, 13, 869, 850);
        var b = Pt(2, 20, 40, 1300, 1280);
        var sup = Pt(0, 1, 1, 100, 700);

        // 10 萬個 down/up 生命週期打進 2 個 cell + 1 萬包 suppressed 打進 1 個 cell
        for (int i = 0; i < 50_000; i++)
        {
            long t = i * 1000L;
            One(h, t, Ev(i * 2 + 1, TouchPhase.Down, a), Ev(i * 2 + 2, TouchPhase.Down, b));
            One(h, t + 500, Ev(i * 2 + 1, TouchPhase.Up, a), Ev(i * 2 + 2, TouchPhase.Up, b));
        }
        for (int i = 0; i < 10_000; i++)
            h.RecordSuppressed(in sup, i * 80L);

        // 聚合只隨「不同 cell 數」成長（上限 = 場域晶片數），不隨事件數成長
        Assert.Equal(3, h.TrackedCellCount);
        Assert.Equal(0, h.ActiveTouchCount);   // 全部 up 收斂，無 in-flight 殘留

        var e = Find(h, 50_000_000L, 2, 7, 13);
        Assert.Equal(50_000, e.TotalTouches);
        Assert.True(e.Count30Min <= 30 * 60, "rolling 計數只涵蓋 30 分鐘窗");
    }

    // ── pipeline 整合（自檢窗計入 / 鎖定後停止累積）─────────────────────────

    sealed class Harness : IDisposable
    {
        public readonly TouchPipeline Pipeline;
        public readonly TriggerHistory History = new(0, Timeout, NeighborRadius);
        public readonly SensorFaceRouter Router;
        public readonly LockedCellFilter Locks = new(new List<LockedCellConfig>());
        readonly FileLogger _log;

        sealed class NullSink : ITouchSink
        {
            public void Publish(byte[] utf8Json) { }
            public void Dispose() { }
        }

        public Harness(AutoLockSelfCheck? autoLock)
        {
            var venue = VenueProfile.Load(Path.Combine(AppContext.BaseDirectory, "venues", "704.json"));
            Router = new SensorFaceRouter(venue);
            _log = new FileLogger(
                Directory.CreateTempSubdirectory("touchservice-history").FullName, LogLevel.Warn, console: false);
            Pipeline = new TouchPipeline(
                Router, new TouchTracker(128, 96f, Timeout), new FrameSerializer(venue),
                new List<ITouchSink> { new NullSink() }, new StatsCollector(), _log,
                keepaliveIntervalMs: 1_000_000, Locks, autoLock, History);
        }

        public void Send(RawPoint p, long nowMs) =>
            Pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { p }), nowMs);

        public (int Face, int Col, int Row) RouteOf(RawPoint p)
        {
            Assert.True(Router.TryRoute(p.X, p.Y, out var rp));
            return (rp.FaceIndex, rp.Col, rp.Row);
        }

        public void Dispose() => _log.Dispose();
    }

    static readonly RawPoint NoisyRaw = new(3264, 1664);
    static readonly RawPoint LegitRaw = new(2800, 2400);

    [Fact]
    public void pipeline_正常down與up進歷史()
    {
        using var h = new Harness(autoLock: null);
        var cell = h.RouteOf(LegitRaw);

        h.Send(LegitRaw, 0);
        h.Pipeline.Tick(180);   // > releaseTimeout → up

        var e = Find(h.History, 200, cell.Face, cell.Col, cell.Row);
        Assert.Equal(1, e.TotalTouches);
        Assert.Equal(180, e.LastLifetimeMs);
    }

    [Fact]
    public void pipeline_自檢窗內被抑制的觸發計入歷史_鎖定後停止累積但保留()
    {
        using var h = new Harness(new AutoLockSelfCheck(0, 10, 10, minEpisodes: 3, releaseTimeoutMs: 170));
        var noisy = h.RouteOf(NoisyRaw);
        var legit = h.RouteOf(LegitRaw);

        // 噪訊：窗內 15 包（達門檻會被鎖）；合法：窗內 2 包（誤踩，不鎖）
        for (int i = 0; i < 15; i++) h.Send(NoisyRaw, i * 80);
        h.Send(LegitRaw, 500);
        h.Send(LegitRaw, 580);

        // 窗內就已計入歷史（被抑制的觸發是最重要的噪訊證據）
        var during = Find(h.History, 1500, noisy.Face, noisy.Col, noisy.Row);
        Assert.Equal(1, during.TotalTouches);              // gap 邏輯：連續重送 = 1 次觸發
        Assert.Equal(15 * 80 - 80, during.TotalActiveMs);  // 0~1120ms 連續活躍
        Assert.Equal(1, Find(h.History, 1500, legit.Face, legit.Col, legit.Row).TotalTouches);

        // 窗結束 → 噪訊鎖定
        h.Pipeline.Tick(10_001);
        Assert.True(h.Locks.IsLocked(noisy.Face, noisy.Col, noisy.Row));

        // 鎖定後：觸發被 filter 擋掉、歷史不再累積（鎖定前的歷史保留）
        var before = Find(h.History, 11_000, noisy.Face, noisy.Col, noisy.Row);
        for (int i = 0; i < 5; i++) h.Send(NoisyRaw, 11_000 + i * 80);
        var after = Find(h.History, 12_000, noisy.Face, noisy.Col, noisy.Row);
        Assert.Equal(before.TotalTouches, after.TotalTouches);
        Assert.Equal(before.TotalActiveMs, after.TotalActiveMs);

        // 合法 cell 窗後照常累積（down 由 tracker 走正常生命週期）
        h.Send(LegitRaw, 12_000);
        Assert.Equal(2, Find(h.History, 12_100, legit.Face, legit.Col, legit.Row).TotalTouches);
    }
}
