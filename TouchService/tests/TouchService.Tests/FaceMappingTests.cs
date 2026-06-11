using TouchService.Core;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// 座標映射對拍 Unity 端公式（SensorFaceConfig.ContainsRaw/RawToPixel/PixelToCell）。
/// 用 venues/704.json 七個面各取邊界值；golden 值以 round-half-to-even（Mathf.RoundToInt 語意）
/// 獨立重算後寫死於此。
/// </summary>
public class FaceMappingTests
{
    static readonly VenueProfile Venue = VenueProfile.Load(Path.Combine(AppContext.BaseDirectory, "venues", "704.json"));
    static readonly SensorFaceRouter Router = new(Venue);

    static SensorFaceConfig Face(string name) => Venue.Faces.First(f => f.Name == name);

    [Fact]
    public void Venue載入_7面_全部直向_有校準()
    {
        Assert.Equal("704", Venue.Venue);
        Assert.Equal(2688, Venue.Canvas.Width);
        Assert.Equal(3840, Venue.Canvas.Height);
        Assert.Equal(7, Venue.Faces.Count);
        Assert.All(Venue.Faces, f =>
        {
            Assert.Equal(ChipOrientation.Vertical, f.Orientation);
            Assert.True(f.HasRawCalibration);
            Assert.False(f.FlipX);
            Assert.False(f.FlipY);
            // 直向：每格 32×16 px
            Assert.Equal(32, f.ScreenCellW);
            Assert.Equal(16, f.ScreenCellH);
        });
    }

    /// <summary>
    /// 七個面的邊界值 golden 對拍：
    /// rawMin 角 → px = origin、cell (0,0)
    /// rawMax 角 → px = origin + TotalPixel（落在面外一格的邊界，Unity 同此行為）、cell clamp 到最後一格
    /// 中間晶片 k=(7,13)（raw = min + k×32 / min + k×16）→ 獨立重算的 px/cell
    /// </summary>
    [Theory]
    //          face                rawX  rawY   pxX   pxY   col row
    [InlineData("Wall Left",        1936,  648,    0,  640,   0,   0)]
    [InlineData("Wall Left",        2544, 2680,  640, 2688,  19, 127)]
    [InlineData("Wall Left",        2160,  856,  236,  850,   7,  13)]
    [InlineData("Wall Button",      2576, 2696,  640, 2688,   0,   0)]
    [InlineData("Wall Button",      3952, 3320, 2048, 3328,  43,  39)]
    [InlineData("Wall Button",      2800, 2904,  869, 2901,   7,  13)]
    [InlineData("Floor",            2576,  648,  640,  640,   0,   0)]
    [InlineData("Floor",            3952, 2680, 2048, 2688,  43, 127)]
    [InlineData("Floor",            2800,  856,  869,  850,   7,  13)]
    [InlineData("Wall Top",         2576,    8,  640,    0,   0,   0)]
    [InlineData("Wall Top",         3952,  632, 2048,  640,  43,  39)]
    [InlineData("Wall Top",         2800,  216,  869,  213,   7,  13)]
    [InlineData("Wall Right Big",   3984,  648, 2048,  640,   0,   0)]
    [InlineData("Wall Right Big",   4592, 1912, 2688, 1920,  19,  79)]
    [InlineData("Wall Right Big",   4208,  856, 2284,  851,   7,  13)]
    [InlineData("Entrance",         3984, 1928, 2048, 1920,   0,   0)]
    [InlineData("Entrance",         4592, 2424, 2688, 2432,  19,  31)]
    [InlineData("Entrance",         4208, 2136, 2284, 2135,   7,  13)]
    [InlineData("Wall Right little",3984, 2440, 2048, 2432,   0,   0)]
    [InlineData("Wall Right little",4592, 2680, 2688, 2688,  19,  15)]
    [InlineData("Wall Right little",4208, 2648, 2284, 2654,   7,  13)]
    public void 七面邊界值golden對拍(string name, int rawX, int rawY, int pxX, int pxY, int col, int row)
    {
        var f = Face(name);
        Assert.True(f.ContainsRaw(rawX, rawY), $"{name} 應包含 raw({rawX},{rawY})");

        var (gx, gy) = f.RawToPixel(rawX, rawY);
        Assert.Equal((pxX, pxY), (gx, gy));

        var (c, r) = f.PixelToCell(gx, gy);
        Assert.Equal((col, row), (c, r));
    }

    /// <summary>
    /// 已知 Unity 端公式 quirk（移植時必須保留）：
    /// RawToPixel 把 (N-1) 段的 raw 範圍拉伸到 N 格的像素範圍，
    /// 高列號晶片中心會被 round 進下一格（如 Floor 列 124 → cell row 125）。
    /// 此測試把 quirk 釘死，防止「好心修正」造成與 Unity 行為分歧。
    /// </summary>
    [Theory]
    [InlineData("Floor", 124, 125)]   // raw Y = 648+124*16 = 2632
    [InlineData("Floor", 126, 127)]
    [InlineData("Wall Button", 38, 39)]
    [InlineData("Wall Right Big", 77, 78)]
    public void Unity拉伸quirk_高列號晶片映射到下一格(string name, int chipRow, int expectedCellRow)
    {
        var f = Face(name);
        int rawX = f.RawMinX;                      // col 0
        int rawY = f.RawMinY + chipRow * 16;       // 直向：raw Y 步距 16
        var (px, py) = f.RawToPixel(rawX, rawY);
        var (_, row) = f.PixelToCell(px, py);
        Assert.Equal(expectedCellRow, row);
    }

    // ── ContainsRaw 邊界（含面與面之間的縫隙）─────────────────────────────────

    [Theory]
    [InlineData(1935, 648)]    // Wall Left rawMinX - 1
    [InlineData(2545, 1000)]   // Wall Left 與 Floor 之間的縫隙（2545..2575）
    [InlineData(3953, 1000)]   // Floor 與 Wall Right Big 之間的縫隙（3953..3983）
    [InlineData(4593, 1000)]   // Wall Right Big rawMaxX + 1
    [InlineData(3000, 7)]      // Wall Top rawMinY - 1
    [InlineData(3000, 3321)]   // Wall Button rawMaxY + 1
    [InlineData(3000, 2683)]   // Floor 下緣與 Wall Button 上緣之間（2681..2695）
    [InlineData(0, 0)]
    [InlineData(65535, 65535)]
    public void 縫隙與範圍外的raw點_不路由到任何面(int rawX, int rawY)
    {
        Assert.False(Router.TryRoute(rawX, rawY, out _));
    }

    // ── Router：7 面線性分類 ────────────────────────────────────────────────

    [Theory]
    [InlineData(2160,  856, "Wall Left")]
    [InlineData(2800, 2904, "Wall Button")]
    [InlineData(2800,  856, "Floor")]
    [InlineData(2800,  216, "Wall Top")]
    [InlineData(4208,  856, "Wall Right Big")]
    [InlineData(4208, 2136, "Entrance")]
    [InlineData(4208, 2648, "Wall Right little")]
    public void Router分類到正確的面(int rawX, int rawY, string expectedFace)
    {
        Assert.True(Router.TryRoute(rawX, rawY, out var p));
        Assert.Equal(expectedFace, Venue.Faces[p.FaceIndex].Name);
    }

    [Fact]
    public void Router輸出norm為畫布正規化座標()
    {
        Assert.True(Router.TryRoute(2800, 856, out var p));   // Floor k=(7,13) → px(869,850)
        Assert.Equal(869, p.PixelX);
        Assert.Equal(850, p.PixelY);
        Assert.Equal(869f / 2688f, p.NormX, 5);
        Assert.Equal(850f / 3840f, p.NormY, 5);
    }

    /// <summary>
    /// 全晶片掃描對拍：對 7 面每一顆晶片中心（raw = min + k×步距）跑一次
    /// RawToPixel + PixelToCell，與測試內「Unity 原始碼逐字轉寫參考實作」比對。
    /// 守住 float 正規化 / round-half-to-even / clamp 的移植正確性（14,272 顆全蓋）。
    /// </summary>
    [Fact]
    public void 全晶片掃描_與Unity參考實作完全一致()
    {
        foreach (var f in Venue.Faces)
        {
            for (int kx = 0; kx < f.ColCount; kx++)
            for (int ky = 0; ky < f.RowCount; ky++)
            {
                int rawX = f.RawMinX + kx * 32;
                int rawY = f.RawMinY + ky * 16;

                var (px, py) = f.RawToPixel(rawX, rawY);
                var (ux, uy) = UnityReferenceRawToPixel(f, rawX, rawY);
                Assert.True(px == ux && py == uy,
                    $"{f.Name} chip({kx},{ky}) raw({rawX},{rawY})：service=({px},{py}) unity=({ux},{uy})");

                var (col, row) = f.PixelToCell(px, py);
                var (ucol, urow) = UnityReferencePixelToCell(f, px, py);
                Assert.True(col == ucol && row == urow,
                    $"{f.Name} chip({kx},{ky})：cell=({col},{row}) unity=({ucol},{urow})");
            }
        }
    }

    // Unity SensorFaceConfig.cs:84-98 逐字轉寫（float + Mathf 語意），作為對拍基準
    static (int, int) UnityReferenceRawToPixel(SensorFaceConfig f, int rawX, int rawY)
    {
        float nx = Math.Clamp((float)(rawX - f.RawMinX) / (f.RawMaxX - f.RawMinX), 0f, 1f);
        float ny = Math.Clamp((float)(rawY - f.RawMinY) / (f.RawMaxY - f.RawMinY), 0f, 1f);
        if (f.FlipX) nx = 1f - nx;
        if (f.FlipY) ny = 1f - ny;
        int px = f.OriginX + (int)MathF.Round(nx * (f.ColCount * 32));   // 直向 cellW=32
        int py = f.OriginY + (int)MathF.Round(ny * (f.RowCount * 16));   // 直向 cellH=16
        return (px, py);
    }

    // Unity SensorFaceConfig.cs:112-117 逐字轉寫
    static (int, int) UnityReferencePixelToCell(SensorFaceConfig f, int pixelX, int pixelY)
    {
        int col = Math.Clamp((pixelX - f.OriginX) / 32, 0, f.ColCount - 1);
        int row = Math.Clamp((pixelY - f.OriginY) / 16, 0, f.RowCount - 1);
        return (col, row);
    }

    [Fact]
    public void CellToPixel為格子左上角()
    {
        var f = Face("Floor");
        Assert.Equal((640, 640), f.CellToPixel(0, 0));
        Assert.Equal((640 + 7 * 32, 640 + 13 * 16), f.CellToPixel(7, 13));
    }
}
