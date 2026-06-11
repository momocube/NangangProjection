using TouchService.Core;
using TouchService.Tracking;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// touch 生命週期測試：時間全部由測試注入（確定性），對齊實測重送行為：
/// p50=81.7ms、主峰 80ms、gap 長尾 120-160ms（release timeout 預設 170ms 的依據）。
/// </summary>
public class TouchTrackerTests
{
    const int Timeout = 170;

    static TouchTracker NewTracker(int maxSlots = 128, float radius = 96f, int timeout = Timeout)
        => new(maxSlots, radius, timeout);

    static RoutedPoint P(int px, int py) => new()
    {
        FaceIndex = 0, Col = px / 32, Row = py / 16,
        PixelX = px, PixelY = py,
        NormX = px / 2688f, NormY = py / 3840f,
    };

    static List<TouchEventData> Batch(TouchTracker t, long nowMs, params RoutedPoint[] pts)
    {
        var ev = new List<TouchEventData>();
        t.ProcessBatch(pts, nowMs, ev);
        return ev;
    }

    static List<TouchEventData> Flush(TouchTracker t, long nowMs)
    {
        var ev = new List<TouchEventData>();
        t.Flush(nowMs, ev);
        return ev;
    }

    // ── down：首包立即發 ────────────────────────────────────────────────────

    [Fact]
    public void 首包立即發Down_零等待()
    {
        var t = NewTracker();
        var ev = Batch(t, 0, P(1000, 1000));

        var e = Assert.Single(ev);
        Assert.Equal(TouchPhase.Down, e.Phase);
        Assert.Equal(1, e.Id);
        Assert.Equal(1, t.ActiveCount);
    }

    // ── 80ms 重送：不假離手 ─────────────────────────────────────────────────

    [Fact]
    public void 重送80ms_持續按住_不假離手()
    {
        var t = NewTracker();
        Batch(t, 0, P(1000, 1000));

        // 80ms 重送 × 10 輪，每輪之間以 5ms 解析度掃 timeout
        for (long now = 80; now <= 800; now += 80)
        {
            for (long f = now - 80 + 5; f <= now; f += 5)
                Assert.Empty(Flush(t, f));
            Batch(t, now, P(1000, 1000));
        }
        Assert.Equal(1, t.ActiveCount);
    }

    [Fact]
    public void 實測gap長尾160ms_timeout170_不假離手()
    {
        var t = NewTracker();
        Batch(t, 0, P(1000, 1000));

        // 實測長尾：gap 160ms（@170ms timeout 不可假離手）
        Assert.Empty(Flush(t, 160));
        var ev = Batch(t, 160, P(1000, 1000));
        Assert.Empty(ev);   // 同位置、無 move、無 down
        Assert.Equal(1, t.ActiveCount);
    }

    [Fact]
    public void 超過timeout_發Up()
    {
        var t = NewTracker();
        Batch(t, 0, P(1000, 1000));

        Assert.Empty(Flush(t, 170));        // 恰好 170：不離手（> 判定）
        var ev = Flush(t, 171);
        var e = Assert.Single(ev);
        Assert.Equal(TouchPhase.Up, e.Phase);
        Assert.Equal(1, e.Id);
        Assert.Equal(1000, e.Point.PixelX); // up 帶最後位置
        Assert.Equal(0, t.ActiveCount);
    }

    [Fact]
    public void 丟包注入_單次丟包gap160_不假離手()
    {
        var t = NewTracker();
        Batch(t, 0, P(1000, 1000));
        Batch(t, 80, P(1000, 1000));
        // t=160 的重送丟包 → 下一包 t=240，gap=160 < 170
        for (long f = 85; f < 240; f += 5)
            Assert.Empty(Flush(t, f));
        var ev = Batch(t, 240, P(1000, 1000));
        Assert.Empty(ev);
        Assert.Equal(1, t.ActiveCount);     // 同一 touch 存活、ID 不變
    }

    [Fact]
    public void 丟包注入_連續兩包丟失gap240_視為離手後重新Down()
    {
        var t = NewTracker();
        var down1 = Batch(t, 0, P(1000, 1000));
        int firstId = down1[0].Id;

        // t=80、t=160 都丟 → gap 240 > 170 → up
        var ups = Flush(t, 241);
        Assert.Single(ups);
        Assert.Equal(TouchPhase.Up, ups[0].Phase);

        // t=240 才到的包 → 新 touch、新 ID
        var down2 = Batch(t, 242, P(1000, 1000));
        var e = Assert.Single(down2);
        Assert.Equal(TouchPhase.Down, e.Phase);
        Assert.NotEqual(firstId, e.Id);
    }

    // ── move ───────────────────────────────────────────────────────────────

    [Fact]
    public void 座標變化_發Move_同ID()
    {
        var t = NewTracker();
        int id = Batch(t, 0, P(1000, 1000))[0].Id;

        var ev = Batch(t, 80, P(1032, 1000));   // 移一格（32px < 96 半徑）
        var e = Assert.Single(ev);
        Assert.Equal(TouchPhase.Move, e.Phase);
        Assert.Equal(id, e.Id);
        Assert.Equal(1032, e.Point.PixelX);
    }

    [Fact]
    public void 同位置重送_不發Move()
    {
        var t = NewTracker();
        Batch(t, 0, P(1000, 1000));
        Assert.Empty(Batch(t, 80, P(1000, 1000)));
    }

    // ── 同足多晶片（合包）去重 ───────────────────────────────────────────────

    [Fact]
    public void 同datagram多點_半徑內只算一個touch()
    {
        // 真實 3 點合包 case（capture-multitouch-floor 55 55 00 00 0c 0e 70 ...）：
        // raw (3696,2168)(3760,2136)(3728,2152) → 相鄰晶片、px 跨距 ~65px < 96 半徑
        var t = NewTracker();
        var ev = Batch(t, 0, P(1784, 2164), P(1849, 2132), P(1817, 2148));

        Assert.Single(ev.Where(e => e.Phase == TouchPhase.Down));
        Assert.Equal(1, t.ActiveCount);
    }

    [Fact]
    public void 同datagram跨距達3格的合包_拆成兩個touch_Unity對等行為()
    {
        // 真實 4 點合包 case（55 55 00 00 10 0e 50 ...）：px 跨距 ~98px，
        // 第 3 點離第 1 點恰好 ≥ 96 半徑（嚴格 < 判定）→ 拆 2 個 touch。
        // Unity SensorBridgeToPassPosition（matchRadiusCells=3、嚴格 <）對同批 hits 行為相同，
        // 此測試把對等行為釘死。
        var t = NewTracker();
        var ev = Batch(t, 0, P(1751, 2148), P(1784, 2164), P(1849, 2132), P(1817, 2148));

        Assert.Equal(2, ev.Count(e => e.Phase == TouchPhase.Down));
        Assert.Equal(2, t.ActiveCount);
    }

    [Fact]
    public void 同datagram兩點_超出半徑_兩個touch()
    {
        var t = NewTracker();
        var ev = Batch(t, 0, P(1000, 1000), P(1200, 1000));   // 200px > 96 半徑

        Assert.Equal(2, ev.Count(e => e.Phase == TouchPhase.Down));
        Assert.Equal(2, t.ActiveCount);
    }

    [Fact]
    public void 跨模組同足_第二個datagram被既有slot吸收()
    {
        // 腳跨模組邊界：兩個 datagram 相隔 40ms（實測），位置相鄰
        var t = NewTracker();
        Batch(t, 0, P(1000, 1000));
        var ev = Batch(t, 40, P(1032, 1016));   // 另一模組的相鄰晶片

        Assert.DoesNotContain(ev, e => e.Phase == TouchPhase.Down);
        Assert.Equal(1, t.ActiveCount);
    }

    // ── alive / slot 上限 ───────────────────────────────────────────────────

    [Fact]
    public void Alive清單正確反映活躍touch()
    {
        var t = NewTracker();
        Batch(t, 0, P(500, 500), P(1500, 1500));
        Batch(t, 10, P(2500, 2500));

        var alive = new List<int>();
        t.CollectAlive(alive);
        Assert.Equal(new[] { 1, 2, 3 }, alive.OrderBy(x => x));

        Flush(t, 500);   // 全部逾時
        t.CollectAlive(alive);
        Assert.Empty(alive);
    }

    [Fact]
    public void 超過maxSlots_丟棄新點不崩潰()
    {
        var t = NewTracker(maxSlots: 2);
        var ev = Batch(t, 0, P(500, 500), P(1500, 1500), P(2500, 2500));

        Assert.Equal(2, ev.Count(e => e.Phase == TouchPhase.Down));
        Assert.Equal(2, t.ActiveCount);
    }

    [Fact]
    public void Up後slot可重用_ID不重複()
    {
        var t = NewTracker(maxSlots: 1);
        int id1 = Batch(t, 0, P(500, 500))[0].Id;
        Flush(t, 200);

        int id2 = Batch(t, 210, P(500, 500))[0].Id;
        Assert.NotEqual(id1, id2);
        Assert.True(id2 > id1);
    }
}
