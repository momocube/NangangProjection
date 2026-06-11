using System.Text;
using System.Text.Json;
using TouchService.Core;
using TouchService.Output;
using TouchService.Tracking;
using Xunit;

namespace TouchService.Tests;

/// <summary>對外協議 v1 schema 序列化測試（schema 定義見 docs/PROTOCOL.md）。</summary>
public class FrameSerializerTests
{
    static readonly VenueProfile Venue =
        VenueProfile.Load(Path.Combine(AppContext.BaseDirectory, "venues", "704.json"));

    [Fact]
    public void Frame_schema_v1_欄位完整()
    {
        var ser = new FrameSerializer(Venue);
        var events = new List<TouchEventData>
        {
            new()
            {
                Id = 42, Phase = TouchPhase.Down,
                Point = new RoutedPoint
                {
                    FaceIndex = 2, Col = 7, Row = 13,
                    PixelX = 869, PixelY = 850,
                    NormX = 869f / 2688f, NormY = 850f / 3840f,
                },
            },
        };
        var alive = new List<int> { 42, 43 };

        var bytes = ser.Serialize(seq: 9, unixMs: 1765432100000, events, alive);
        using var doc = JsonDocument.Parse(bytes);
        var root = doc.RootElement;

        Assert.Equal(1, root.GetProperty("v").GetInt32());
        Assert.Equal(9, root.GetProperty("seq").GetInt64());
        Assert.Equal(1765432100000, root.GetProperty("t").GetInt64());
        Assert.Equal("704", root.GetProperty("venue").GetString());
        Assert.Equal(2688, root.GetProperty("canvas").GetProperty("w").GetInt32());
        Assert.Equal(3840, root.GetProperty("canvas").GetProperty("h").GetInt32());

        var e = Assert.Single(root.GetProperty("events").EnumerateArray());
        Assert.Equal(42, e.GetProperty("id").GetInt32());
        Assert.Equal("down", e.GetProperty("phase").GetString());
        Assert.Equal(2, e.GetProperty("face").GetInt32());
        Assert.Equal(new[] { 7, 13 }, e.GetProperty("cell").EnumerateArray().Select(x => x.GetInt32()));
        Assert.Equal(new[] { 869, 850 }, e.GetProperty("px").EnumerateArray().Select(x => x.GetInt32()));
        Assert.Equal(0.32329f, e.GetProperty("norm")[0].GetSingle(), 4);
        Assert.Equal(0.22135f, e.GetProperty("norm")[1].GetSingle(), 4);

        Assert.Equal(new[] { 42, 43 },
            root.GetProperty("alive").EnumerateArray().Select(x => x.GetInt32()));
    }

    [Fact]
    public void 空事件snapshot_keepalive形狀正確()
    {
        var ser = new FrameSerializer(Venue);
        var bytes = ser.Serialize(1, 1765432100000, new List<TouchEventData>(), new List<int>());
        using var doc = JsonDocument.Parse(bytes);

        Assert.Equal(0, doc.RootElement.GetProperty("events").GetArrayLength());
        Assert.Equal(0, doc.RootElement.GetProperty("alive").GetArrayLength());
    }

    [Theory]
    [InlineData(TouchPhase.Down, "down")]
    [InlineData(TouchPhase.Move, "move")]
    [InlineData(TouchPhase.Up, "up")]
    public void Phase字串對應(TouchPhase phase, string expected)
    {
        var ser = new FrameSerializer(Venue);
        var ev = new List<TouchEventData>
        {
            new() { Id = 1, Phase = phase, Point = default },
        };
        var json = Encoding.UTF8.GetString(ser.Serialize(1, 0, ev, new List<int>()));
        Assert.Contains($"\"phase\":\"{expected}\"", json);
    }
}
