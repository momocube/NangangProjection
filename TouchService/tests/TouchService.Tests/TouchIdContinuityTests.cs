using TouchService.Core;
using TouchService.Tracking;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// touch ID 連續性：鄰近點移動不換 ID、兩點交錯不互換。
/// </summary>
public class TouchIdContinuityTests
{
    static TouchTracker NewTracker() => new(128, 96f, 170);

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

    [Fact]
    public void 鄰近點連續移動_全程同ID()
    {
        var t = NewTracker();
        int id = Batch(t, 0, P(640, 1000))[0].Id;

        // 每 80ms 移一格（32px），走 30 步（共 960px）
        for (int step = 1; step <= 30; step++)
        {
            var ev = Batch(t, step * 80, P(640 + step * 32, 1000));
            var e = Assert.Single(ev);
            Assert.Equal(TouchPhase.Move, e.Phase);
            Assert.Equal(id, e.Id);
        }
        Assert.Equal(1, t.ActiveCount);
    }

    [Fact]
    public void 兩點交錯封包_ID不互換()
    {
        // 兩個 touch 來自不同模組（datagram 交錯到達），位置固定
        var t = NewTracker();
        int idA = Batch(t, 0, P(800, 1000))[0].Id;
        int idB = Batch(t, 40, P(1600, 2000))[0].Id;
        Assert.NotEqual(idA, idB);

        // 交錯重送 20 輪：A 在 t=80n、B 在 t=80n+40
        for (int n = 1; n <= 20; n++)
        {
            Assert.Empty(Batch(t, n * 80, P(800, 1000)));
            Assert.Empty(Batch(t, n * 80 + 40, P(1600, 2000)));
        }

        var alive = new List<int>();
        t.CollectAlive(alive);
        Assert.Equal(2, alive.Count);
        Assert.Contains(idA, alive);
        Assert.Contains(idB, alive);
    }

    [Fact]
    public void 兩點交叉走過_ID不互換()
    {
        // A 從左往右、B 從右往左，縱向相距 64px（< 96 配對半徑，會互相競爭），
        // 每 80ms 各走 40px，中途 X 交會。
        var t = NewTracker();

        int xa = 800,  ya = 1000;
        int xb = 1600, yb = 1064;

        int idA = Batch(t, 0,  P(xa, ya))[0].Id;
        int idB = Batch(t, 40, P(xb, yb))[0].Id;

        long now = 40;
        var lastA = (X: xa, Y: ya);
        var lastB = (X: xb, Y: yb);

        for (int step = 1; step <= 20; step++)
        {
            xa += 40; xb -= 40;

            now += 40;
            foreach (var e in Batch(t, now, P(xa, ya)))
            {
                Assert.Equal(idA, e.Id);     // A 的封包永遠更新 A
                lastA = (e.Point.PixelX, e.Point.PixelY);
            }

            now += 40;
            foreach (var e in Batch(t, now, P(xb, yb)))
            {
                Assert.Equal(idB, e.Id);     // B 的封包永遠更新 B
                lastB = (e.Point.PixelX, e.Point.PixelY);
            }
        }

        // 走完後 A 在右、B 在左（交叉完成、ID 沒換）
        Assert.True(lastA.X > lastB.X, $"A({lastA.X}) 應在 B({lastB.X}) 右側");
        Assert.Equal(1000, lastA.Y);
        Assert.Equal(1064, lastB.Y);
        Assert.Equal(2, t.ActiveCount);
    }

    [Fact]
    public void 一點離手另一點存活_ID互不干擾()
    {
        var t = NewTracker();
        int idA = Batch(t, 0, P(800, 1000))[0].Id;
        int idB = Batch(t, 0, P(2000, 2000))[0].Id;

        // 只有 B 持續重送
        for (int n = 1; n <= 5; n++)
            Batch(t, n * 80, P(2000, 2000));

        var ev = new List<TouchEventData>();
        t.Flush(400, ev);

        var up = Assert.Single(ev);
        Assert.Equal(idA, up.Id);

        var alive = new List<int>();
        t.CollectAlive(alive);
        Assert.Equal(new[] { idB }, alive);
    }
}
