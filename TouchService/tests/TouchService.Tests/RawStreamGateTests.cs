using TouchService.Calibration;
using TouchService.Logging;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// raw 檢視串流的節流 + 穩定計數（時間注入、無 sleep）：
/// 同值連續計數（穩定偵測 ≥3 的依據）、同座標去重（第 4 包起壓到心跳）、
/// 座標變化立即推、全域 ~30 msg/s 上限、多 source 獨立。
/// </summary>
public class RawStreamGateTests
{
    [Fact]
    public void 連續同值_n遞增_前三包都推()
    {
        var gate = new RawStreamGate();
        for (int i = 1; i <= RawStreamGate.StableCount; i++)
        {
            Assert.True(gate.Offer("10.0.0.1", 100, 1000, 500, i * 80, out int n));
            Assert.Equal(i, n);   // 穩定偵測：同值連續次數（UI 在 n ≥ 3 自動取用）
        }
    }

    [Fact]
    public void 第四包起去重_心跳每秒最多一包()
    {
        var gate = new RawStreamGate();
        long t = 0;
        int emitted = 0;
        // 同座標 80ms 重送 1 秒（13 包）：只推前 3 包
        for (int i = 0; i < 13; i++, t += 80)
            if (gate.Offer("10.0.0.1", 100, 1000, 500, t, out _)) emitted++;
        Assert.Equal(RawStreamGate.StableCount, emitted);

        // 持續重送到下一秒：心跳放行一包（UI 知道訊號還在）
        bool heartbeat = false;
        for (int i = 0; i < 13; i++, t += 80)
            if (gate.Offer("10.0.0.1", 100, 1000, 500, t, out int n))
            {
                heartbeat = true;
                Assert.True(n > RawStreamGate.StableCount);   // 計數持續累加、沒被去重打斷
                break;
            }
        Assert.True(heartbeat);
    }

    [Fact]
    public void 座標變化_立即推_計數歸1()
    {
        var gate = new RawStreamGate();
        for (int i = 0; i < 5; i++) gate.Offer("10.0.0.1", 100, 1000, 500, i * 10, out _);

        Assert.True(gate.Offer("10.0.0.1", 100, 1032, 500, 60, out int n));
        Assert.Equal(1, n);
    }

    [Fact]
    public void 全域上限_每秒30包()
    {
        var gate = new RawStreamGate();
        int emitted = 0;
        // 同一毫秒 100 個不同座標（噪訊大爆發）：只放行 30
        for (int i = 0; i < 100; i++)
            if (gate.Offer("10.0.0.1", 100, 1000 + i, 500, 0, out _)) emitted++;
        Assert.Equal(RawStreamGate.MaxPerSecond, emitted);

        // 下一個 1 秒窗：重新放行
        Assert.True(gate.Offer("10.0.0.1", 100, 9000, 500, 1000, out _));
    }

    [Fact]
    public void 多source獨立_別模組的噪訊不打斷穩定計數()
    {
        var gate = new RawStreamGate();
        // source A 的角落晶片穩定重送，source B 的噪訊晶片交錯亂叫
        gate.Offer("10.0.0.1", 100, 1000, 500, 0, out _);
        gate.Offer("10.0.0.2", 200, 7777, 999, 10, out _);
        gate.Offer("10.0.0.1", 100, 1000, 500, 80, out int n2);
        gate.Offer("10.0.0.2", 200, 8888, 111, 90, out _);
        gate.Offer("10.0.0.1", 100, 1000, 500, 160, out int n3);

        Assert.Equal(2, n2);
        Assert.Equal(3, n3);   // 達穩定門檻——噪訊在別的 source，不影響
    }

    [Fact]
    public void Reset_清空狀態()
    {
        var gate = new RawStreamGate();
        gate.Offer("10.0.0.1", 100, 1000, 500, 0, out _);
        gate.Offer("10.0.0.1", 100, 1000, 500, 80, out _);
        gate.Reset();
        Assert.True(gate.Offer("10.0.0.1", 100, 1000, 500, 160, out int n));
        Assert.Equal(1, n);
    }

    [Fact]
    public void 逾時_自動stop()
    {
        using var log = new FileLogger(
            Directory.CreateTempSubdirectory("calib-timeout").FullName,
            LogLevel.Warn, console: false);
        using var streamer = new CalibrationStreamer(log);

        streamer.Start(nowMs: 0);
        Assert.True(streamer.Active);
        Assert.Equal((true, CalibrationStreamer.DefaultTimeoutMinutes * 60), streamer.Status(0));

        // 14 分 59 秒：還活著
        streamer.CheckExpiry(15 * 60_000 - 1000);
        Assert.True(streamer.Active);

        // 15 分鐘整：自動結束
        streamer.CheckExpiry(15 * 60_000);
        Assert.False(streamer.Active);
        Assert.Equal((false, 0), streamer.Status(15 * 60_000));
    }

    [Fact]
    public void 重複start_重置逾時與節流()
    {
        using var log = new FileLogger(
            Directory.CreateTempSubdirectory("calib-restart").FullName,
            LogLevel.Warn, console: false);
        using var streamer = new CalibrationStreamer(log);

        streamer.Start(nowMs: 0);
        streamer.Start(nowMs: 10 * 60_000);            // keep-alive 重打 = 延長
        streamer.CheckExpiry(15 * 60_000);             // 原本的到期點：已被延長，不停止
        Assert.True(streamer.Active);
        streamer.CheckExpiry(25 * 60_000);
        Assert.False(streamer.Active);
    }
}
