using System.Text.Json;
using TouchService.Core;
using TouchService.Diagnostics;
using TouchService.Logging;
using TouchService.Output;
using TouchService.Tracking;
using Xunit;

namespace TouchService.Tests;

public class InteractionToggleTests
{
    sealed class CaptureSink : ITouchSink
    {
        public readonly List<byte[]> Frames = new();
        public void Publish(byte[] utf8Json) => Frames.Add(utf8Json.ToArray());
        public void Dispose() { }
    }

    [Fact]
    public void 關閉時送離開清空alive_後續不發frame_重新開啟只處理新封包()
    {
        var venue = VenueProfile.Load(Path.Combine(AppContext.BaseDirectory, "venues", "704.json"));
        var sink = new CaptureSink();
        var logDir = Directory.CreateTempSubdirectory("touchservice-interaction").FullName;
        using var log = new FileLogger(logDir, LogLevel.Warn, console: false);
        using var pipeline = new TouchPipeline(
            new SensorFaceRouter(venue), new TouchTracker(128, 96f, 170), new FrameSerializer(venue),
            new List<ITouchSink> { sink }, new StatsCollector(), log,
            keepaliveIntervalMs: 100, interactionEnabled: true);

        var first = new RawPoint(3264, 1664);
        var afterResume = new RawPoint(2800, 2400);
        pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { first }), 0);
        Assert.Equal(1, pipeline.ActiveTouches);
        Assert.Equal("down", Events(sink.Frames[^1]).Single().Phase);

        pipeline.SetInteractionEnabled(false, 10);
        Assert.False(pipeline.InteractionEnabled);
        Assert.Equal(0, pipeline.ActiveTouches);
        using (var released = JsonDocument.Parse(sink.Frames[^1]))
        {
            Assert.Equal("up", Events(sink.Frames[^1]).Single().Phase);
            Assert.Empty(released.RootElement.GetProperty("alive").EnumerateArray());
        }

        int framesAfterRelease = sink.Frames.Count;
        pipeline.SetInteractionEnabled(false, 20);  // 重複關閉不重複發 frame
        pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { afterResume }), 30);
        pipeline.Tick(1_000);                       // 停用時連 keepalive 也不送
        Assert.Equal(framesAfterRelease, sink.Frames.Count);
        Assert.Equal(0, pipeline.ActiveTouches);

        pipeline.SetInteractionEnabled(true, 1_010);
        Assert.Equal(framesAfterRelease, sink.Frames.Count);  // 不補播停用期間觸點
        pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { afterResume }), 1_020);
        Assert.Equal(1, pipeline.ActiveTouches);
        Assert.Equal("down", Events(sink.Frames[^1]).Single().Phase);
    }

    static IEnumerable<(int Id, string Phase)> Events(byte[] frame)
    {
        using var doc = JsonDocument.Parse(frame);
        foreach (var e in doc.RootElement.GetProperty("events").EnumerateArray())
            yield return (e.GetProperty("id").GetInt32(), e.GetProperty("phase").GetString()!);
    }
}
