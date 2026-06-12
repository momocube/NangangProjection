using WallProjector.Core;
using Xunit;

namespace WallProjector.Tests;

/// <summary>
/// /venue JSON 解析 + 面像素幾何（公式對拍 TouchService SensorFaceConfig：
/// vertical 每格 32×16、horizontal 16×32）+ 旋轉預設猜測。
/// </summary>
public class VenueInfoTests
{
    static VenueInfo Venue704() => VenueInfo.Parse(File.ReadAllText("venues/704.json"));

    [Fact]
    public void parse_704_canvas與7面()
    {
        var v = Venue704();
        Assert.Equal("704", v.Venue);
        Assert.Equal(2688, v.Canvas.Width);
        Assert.Equal(3840, v.Canvas.Height);
        Assert.Equal(7, v.Faces.Count);
    }

    [Fact]
    public void 面像素尺寸_vertical格子32x16_對拍hello已知值()
    {
        var v = Venue704();

        // Wall Left：20 col × 128 row、vertical → 640×2048（TouchService hello 的 width/height）
        Assert.Equal(640, v.Faces[0].PixelWidth);
        Assert.Equal(2048, v.Faces[0].PixelHeight);
        Assert.Equal(32, v.Faces[0].CellW);
        Assert.Equal(16, v.Faces[0].CellH);

        // Floor：44 col × 128 row → 1408×2048
        Assert.Equal(1408, v.Faces[2].PixelWidth);
        Assert.Equal(2048, v.Faces[2].PixelHeight);
    }

    [Fact]
    public void 面像素尺寸_horizontal格子16x32()
    {
        var v = VenueInfo.Parse("""
            {
              "venue": "t", "canvas": { "width": 1000, "height": 1000 },
              "faces": [{ "name": "H", "originX": 0, "originY": 0,
                          "orientation": "horizontal", "colCount": 10, "rowCount": 4 }]
            }
            """);
        Assert.Equal(160, v.Faces[0].PixelWidth);    // 10 × 16
        Assert.Equal(128, v.Faces[0].PixelHeight);   // 4 × 32
    }

    [Fact]
    public void 地板面判定_名稱含Floor()
    {
        var v = Venue704();
        Assert.Equal(2, v.FloorIndex());
        Assert.True(v.Faces[2].IsFloor);
        Assert.False(v.Faces[0].IsFloor);
    }

    [Fact]
    public void 旋轉猜測_704七面_左右臂正負90_上下臂0與180()
    {
        var v = Venue704();

        Assert.Equal(270, v.GuessRotation(0));   // Wall Left（左臂）
        Assert.Equal(180, v.GuessRotation(1));   // Wall Button（下臂）
        Assert.Equal(0, v.GuessRotation(2));     // Floor（地板）
        Assert.Equal(0, v.GuessRotation(3));     // Wall Top（上臂）
        Assert.Equal(90, v.GuessRotation(4));    // Wall Right Big（右臂）
        Assert.Equal(90, v.GuessRotation(5));    // Entrance（右臂下段）
        Assert.Equal(90, v.GuessRotation(6));    // Wall Right little（右臂最下段）
    }

    [Fact]
    public void 旋轉猜測_超界index回0()
    {
        var v = Venue704();
        Assert.Equal(0, v.GuessRotation(-1));
        Assert.Equal(0, v.GuessRotation(99));
    }

    [Fact]
    public void parse_壞資料_明確丟錯()
    {
        Assert.ThrowsAny<Exception>(() => VenueInfo.Parse("{}"));
        Assert.ThrowsAny<Exception>(() => VenueInfo.Parse(
            """{ "venue": "x", "canvas": { "width": 0, "height": 10 }, "faces": [{}] }"""));
    }
}
