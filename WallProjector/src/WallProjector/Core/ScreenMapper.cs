namespace WallProjector.Core;

/// <summary>投影螢幕 rect（與 TouchService mouseMode.screen 同欄位、同座標系）。</summary>
public sealed class ScreenRectConfig
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
}

/// <summary>投影內容校正（與 TouchService mouseMode.content 同欄位）。預設 0/0/1/1 = 滿版。</summary>
public sealed class ContentRectConfig
{
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double ScaleX { get; set; } = 1.0;
    public double ScaleY { get; set; } = 1.0;
}

/// <summary>視窗擺位 rect（螢幕絕對座標，px）。</summary>
public readonly record struct WindowRect(int X, int Y, int Width, int Height);

/// <summary>
/// canvas → 投影螢幕的映射數學。
/// **公式與 TouchService `MouseDriver.MapCanvasToScreen` 逐行對拍**（獨立實作，
/// ScreenMapperTests 以 TouchService MouseDriverTests 的已知值釘住）——
/// 觸控落點（滑鼠模式）與投影視窗用同一套數學，內容跟觸控天然對齊。
/// 改這裡 = 改 TouchService 那邊也要同步，否則摸的位置跟看的內容會錯開。
/// </summary>
public static class ScreenMapper
{
    /// <summary>
    /// canvas norm（0~1）→ 螢幕座標：
    /// screen.x + content.offsetX + norm × screen.width × content.scaleX，
    /// 結果 clamp 回 screen rect（校正填錯也不會把視窗丟出目標螢幕）。
    /// </summary>
    public static (double X, double Y) MapCanvasToScreen(
        double normX, double normY, ScreenRectConfig screen, ContentRectConfig content)
    {
        double x = screen.X + content.OffsetX + normX * screen.Width * content.ScaleX;
        double y = screen.Y + content.OffsetY + normY * screen.Height * content.ScaleY;
        x = Math.Clamp(x, screen.X, screen.X + screen.Width);
        y = Math.Clamp(y, screen.Y, screen.Y + screen.Height);
        return (x, y);
    }

    /// <summary>
    /// 面在投影螢幕上的視窗 rect：面的 canvas 左上/右下角 → norm → MapCanvasToScreen。
    /// 兩角各自映射再相減（不是先算尺寸再縮放）——跟滑鼠落點走完全相同的路徑，
    /// 邊界 clamp 行為也一致。
    /// </summary>
    public static WindowRect FaceScreenRect(
        FaceInfo face, VenueInfo venue, ScreenRectConfig screen, ContentRectConfig content)
    {
        double nx1 = (double)face.OriginX / venue.Canvas.Width;
        double ny1 = (double)face.OriginY / venue.Canvas.Height;
        double nx2 = (double)(face.OriginX + face.PixelWidth) / venue.Canvas.Width;
        double ny2 = (double)(face.OriginY + face.PixelHeight) / venue.Canvas.Height;

        var (x1, y1) = MapCanvasToScreen(nx1, ny1, screen, content);
        var (x2, y2) = MapCanvasToScreen(nx2, ny2, screen, content);

        int xi = (int)Math.Round(x1, MidpointRounding.AwayFromZero);
        int yi = (int)Math.Round(y1, MidpointRounding.AwayFromZero);
        int wi = (int)Math.Round(x2, MidpointRounding.AwayFromZero) - xi;
        int hi = (int)Math.Round(y2, MidpointRounding.AwayFromZero) - yi;
        return new WindowRect(xi, yi, wi, hi);
    }
}
