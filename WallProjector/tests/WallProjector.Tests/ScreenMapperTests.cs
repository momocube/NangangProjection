using WallProjector.Core;
using Xunit;

namespace WallProjector.Tests;

/// <summary>
/// canvas → 螢幕映射數學：**與 TouchService `MouseDriver.MapCanvasToScreen` 對拍**。
/// 前半的期望值逐字取自 TouchService tests/MouseDriverTests.cs 的 canvas 映射測試——
/// 兩邊公式必須永遠一致（觸控落點 = 投影內容位置），改公式必須兩邊同步改。
/// </summary>
public class ScreenMapperTests
{
    // ── 對拍 TouchService MouseDriverTests（期望值逐字複製）────────────────────

    [Fact]
    public void canvas映射_預設content滿版_等比落點()
    {
        var screen = new ScreenRectConfig { X = 0, Y = 0, Width = 1920, Height = 1080 };
        var full = new ContentRectConfig();

        Assert.Equal((0d, 0d), ScreenMapper.MapCanvasToScreen(0, 0, screen, full));
        Assert.Equal((960d, 540d), ScreenMapper.MapCanvasToScreen(0.5, 0.5, screen, full));
        Assert.Equal((1920d, 1080d), ScreenMapper.MapCanvasToScreen(1, 1, screen, full));
        Assert.Equal((480d, 270d), ScreenMapper.MapCanvasToScreen(0.25, 0.25, screen, full));
    }

    [Fact]
    public void canvas映射_content_offset與scale校正()
    {
        // 投影沒滿版：內容右移 100px、下移 50px、縮 0.8×0.9
        var screen = new ScreenRectConfig { X = 0, Y = 0, Width = 1920, Height = 1080 };
        var content = new ContentRectConfig { OffsetX = 100, OffsetY = 50, ScaleX = 0.8, ScaleY = 0.9 };

        Assert.Equal((100d, 50d), ScreenMapper.MapCanvasToScreen(0, 0, screen, content));
        // 0.5：x = 100 + 0.5×1920×0.8 = 868；y = 50 + 0.5×1080×0.9 = 536
        Assert.Equal((868d, 536d), ScreenMapper.MapCanvasToScreen(0.5, 0.5, screen, content));
        // 1.0：x = 100 + 1536 = 1636；y = 50 + 972 = 1022（letterbox 內，不到螢幕右下）
        Assert.Equal((1636d, 1022d), ScreenMapper.MapCanvasToScreen(1, 1, screen, content));
    }

    [Fact]
    public void canvas映射_非主螢幕負座標rect()
    {
        // 第二螢幕在主螢幕左邊：rect (-1920, 120, 1280×720)
        var screen = new ScreenRectConfig { X = -1920, Y = 120, Width = 1280, Height = 720 };
        var full = new ContentRectConfig();

        Assert.Equal((-1920d, 120d), ScreenMapper.MapCanvasToScreen(0, 0, screen, full));
        Assert.Equal((-1600d, 300d), ScreenMapper.MapCanvasToScreen(0.25, 0.25, screen, full));
        Assert.Equal((-640d, 840d), ScreenMapper.MapCanvasToScreen(1, 1, screen, full));

        // 負座標 rect + content 校正
        var content = new ContentRectConfig { OffsetX = 10, OffsetY = 20, ScaleX = 0.5, ScaleY = 0.5 };
        Assert.Equal((-1920d + 10 + 640, 120d + 20 + 360),
            ScreenMapper.MapCanvasToScreen(1, 1, screen, content));
    }

    [Fact]
    public void canvas映射_content校正超界_clamp回螢幕rect()
    {
        var screen = new ScreenRectConfig { X = 0, Y = 0, Width = 1920, Height = 1080 };
        var over = new ContentRectConfig { ScaleX = 1.2, ScaleY = 1.2 };

        // norm 1.0 × scale 1.2 = 2304 → clamp 1920（填錯校正不會把視窗丟出目標螢幕）
        Assert.Equal((1920d, 1080d), ScreenMapper.MapCanvasToScreen(1, 1, screen, over));

        var negOff = new ContentRectConfig { OffsetX = -500, OffsetY = -500 };
        Assert.Equal((0d, 0d), ScreenMapper.MapCanvasToScreen(0, 0, screen, negOff));
    }

    // ── 面 rect 計算（真實 704 venue）──────────────────────────────────────────

    static VenueInfo Venue704() => VenueInfo.Parse(File.ReadAllText("venues/704.json"));

    [Fact]
    public void faceRect_704_WallLeft_滿版1080p()
    {
        var venue = Venue704();
        var screen = new ScreenRectConfig { X = 0, Y = 0, Width = 1920, Height = 1080 };
        var rect = ScreenMapper.FaceScreenRect(venue.Faces[0], venue, screen, new ContentRectConfig());

        // Wall Left：canvas (0,640) 640×2048、canvas 2688×3840
        // x1 = 0；y1 = 640/3840×1080 = 180
        // x2 = 640/2688×1920 = 457.14… → 457；y2 = 2688/3840×1080 = 756
        Assert.Equal(new WindowRect(0, 180, 457, 576), rect);
    }

    [Fact]
    public void faceRect_704_Floor與WallRightBig()
    {
        var venue = Venue704();
        var screen = new ScreenRectConfig { X = 0, Y = 0, Width = 1920, Height = 1080 };
        var full = new ContentRectConfig();

        // Floor（index 2）：canvas (640,640) 1408×2048
        // x1 = 640/2688×1920 = 457.14→457；x2 = 2048/2688×1920 = 1462.86→1463
        var floor = ScreenMapper.FaceScreenRect(venue.Faces[2], venue, screen, full);
        Assert.Equal(new WindowRect(457, 180, 1006, 576), floor);

        // Wall Right Big（index 4）：canvas (2048,640) 640×1280
        // x1 = 1463；x2 = 1920；y2 = 1920/3840×1080 = 540
        var right = ScreenMapper.FaceScreenRect(venue.Faces[4], venue, screen, full);
        Assert.Equal(new WindowRect(1463, 180, 457, 360), right);
    }

    [Fact]
    public void faceRect_相鄰面在螢幕上無縫拼接()
    {
        // 兩角各自過同一映射 → 相鄰面的邊界像素一致（左牆右緣 = 地板左緣）
        var venue = Venue704();
        var screen = new ScreenRectConfig { X = 0, Y = 0, Width = 1920, Height = 1080 };
        var full = new ContentRectConfig();

        var left = ScreenMapper.FaceScreenRect(venue.Faces[0], venue, screen, full);
        var floor = ScreenMapper.FaceScreenRect(venue.Faces[2], venue, screen, full);
        Assert.Equal(left.X + left.Width, floor.X);
    }

    [Fact]
    public void faceRect_content校正_offset與scale套用()
    {
        var venue = Venue704();
        var screen = new ScreenRectConfig { X = 0, Y = 0, Width = 1920, Height = 1080 };
        var content = new ContentRectConfig { OffsetX = 100, OffsetY = 50, ScaleX = 0.8, ScaleY = 0.9 };

        // Wall Left：x1 = 100 + 0 = 100；y1 = 50 + (640/3840)×1080×0.9 = 50+162 = 212
        // x2 = 100 + (640/2688)×1920×0.8 = 100 + 365.71 = 465.71 → 466
        // y2 = 50 + (2688/3840)×1080×0.9 = 50 + 680.4 = 730.4 → 730
        var rect = ScreenMapper.FaceScreenRect(venue.Faces[0], venue, screen, content);
        Assert.Equal(new WindowRect(100, 212, 366, 518), rect);
    }

    [Fact]
    public void faceRect_負座標第二螢幕()
    {
        var venue = Venue704();
        var screen = new ScreenRectConfig { X = -1920, Y = 0, Width = 1920, Height = 1080 };
        var rect = ScreenMapper.FaceScreenRect(venue.Faces[0], venue, screen, new ContentRectConfig());

        // 同滿版 1080p 案例整體平移 -1920
        Assert.Equal(new WindowRect(-1920, 180, 457, 576), rect);
    }
}
