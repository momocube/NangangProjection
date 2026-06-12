using TouchService.Calibration;
using TouchService.Core;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// 校正精靈純數學：兩角 raw → rawMin/Max + flip（四種組合）、venue JSON 生成
/// golden（單面 vertical / horizontal）、驗證規則（名稱 / cols/rows / 兩角相同）、
/// 寫檔 roundtrip → 兩角映射回 (0,0) 與 (cols-1,rows-1)。
/// </summary>
public class CalibrationMathTests
{
    // ── TryComputeAxis ───────────────────────────────────────────────────────

    [Fact]
    public void 兩角正向_不翻轉()
    {
        Assert.True(CalibrationMath.TryComputeAxis(1000, 1300, out var min, out var max, out var flip));
        Assert.Equal(1000, min);
        Assert.Equal(1300, max);
        Assert.False(flip);
    }

    [Fact]
    public void 兩角反向_翻轉()
    {
        Assert.True(CalibrationMath.TryComputeAxis(1300, 1000, out var min, out var max, out var flip));
        Assert.Equal(1000, min);
        Assert.Equal(1300, max);
        Assert.True(flip);
    }

    [Fact]
    public void 兩角相同_失敗()
    {
        Assert.False(CalibrationMath.TryComputeAxis(1000, 1000, out _, out _, out _));
    }

    // ── BuildVenue：四種 flip 組合 ───────────────────────────────────────────

    static CalibrationSaveRequest Request(int[] tl, int[] br, string orientation = "vertical",
                                          int cols = 4, int rows = 2, string venue = "demo")
        => new()
        {
            Venue = venue,
            Faces = new List<CalibrationFaceRequest>
            {
                new()
                {
                    Name = "Wall", Cols = cols, Rows = rows, Orientation = orientation,
                    OriginX = 0, OriginY = 0, TopLeftRaw = tl, BottomRightRaw = br,
                },
            },
        };

    [Theory]
    [InlineData(1000, 500, 1300, 530, false, false)]   // 同向
    [InlineData(1300, 500, 1000, 530, true,  false)]   // X 反向
    [InlineData(1000, 530, 1300, 500, false, true)]    // Y 反向
    [InlineData(1300, 530, 1000, 500, true,  true)]    // 雙反向
    public void 四種flip組合(int tlX, int tlY, int brX, int brY, bool expFlipX, bool expFlipY)
    {
        var (profile, errors) = CalibrationMath.BuildVenue(
            Request(new[] { tlX, tlY }, new[] { brX, brY }));
        Assert.Empty(errors);
        var f = profile!.Faces[0];
        Assert.Equal(1000, f.RawMinX);
        Assert.Equal(1300, f.RawMaxX);
        Assert.Equal(500, f.RawMinY);
        Assert.Equal(530, f.RawMaxY);
        Assert.Equal(expFlipX, f.FlipX);
        Assert.Equal(expFlipY, f.FlipY);
    }

    // ── venue JSON golden（單面 vertical / horizontal）──────────────────────

    [Fact]
    public void Golden_單面vertical()
    {
        // vertical：每格 32×16 → 4×2 格 = 128×32 畫布（單面 origin 0,0 = 面即畫布）
        var (profile, errors) = CalibrationMath.BuildVenue(
            Request(new[] { 1000, 500 }, new[] { 1300, 530 }));
        Assert.Empty(errors);

        var json = CalibrationMath.SerializeVenueJson(profile!).Replace("\r\n", "\n");
        Assert.Equal("""
            {
              "venue": "demo",
              "canvas": {
                "width": 128,
                "height": 32
              },
              "faces": [
                {
                  "name": "Wall",
                  "originX": 0,
                  "originY": 0,
                  "orientation": "vertical",
                  "colCount": 4,
                  "rowCount": 2,
                  "rawMinX": 1000,
                  "rawMaxX": 1300,
                  "rawMinY": 500,
                  "rawMaxY": 530,
                  "flipX": false,
                  "flipY": false
                }
              ]
            }
            """.Replace("\r\n", "\n"), json);
    }

    [Fact]
    public void Golden_單面horizontal()
    {
        // horizontal：每格 16×32 → 4×2 格 = 64×64 畫布；X 反向 → flipX
        var (profile, errors) = CalibrationMath.BuildVenue(
            Request(new[] { 1300, 500 }, new[] { 1000, 530 }, orientation: "horizontal", venue: "demo-h"));
        Assert.Empty(errors);

        var json = CalibrationMath.SerializeVenueJson(profile!).Replace("\r\n", "\n");
        Assert.Equal("""
            {
              "venue": "demo-h",
              "canvas": {
                "width": 64,
                "height": 64
              },
              "faces": [
                {
                  "name": "Wall",
                  "originX": 0,
                  "originY": 0,
                  "orientation": "horizontal",
                  "colCount": 4,
                  "rowCount": 2,
                  "rawMinX": 1000,
                  "rawMaxX": 1300,
                  "rawMinY": 500,
                  "rawMaxY": 530,
                  "flipX": true,
                  "flipY": false
                }
              ]
            }
            """.Replace("\r\n", "\n"), json);
    }

    // ── 驗證規則 ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("..")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("a:b")]
    [InlineData("a?b")]
    public void 場域名稱非法_拒收(string name)
    {
        var req = Request(new[] { 1000, 500 }, new[] { 1300, 530 }, venue: name);
        var (profile, errors) = CalibrationMath.BuildVenue(req);
        Assert.Null(profile);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void 場域名稱過長_拒收()
    {
        var req = Request(new[] { 1000, 500 }, new[] { 1300, 530 }, venue: new string('x', 65));
        Assert.Null(CalibrationMath.BuildVenue(req).Profile);
    }

    [Theory]
    [InlineData("光域704")]
    [InlineData("demo-wall_2")]
    public void 場域名稱合法_含中文與連字號(string name)
    {
        var (profile, errors) = CalibrationMath.BuildVenue(
            Request(new[] { 1000, 500 }, new[] { 1300, 530 }, venue: name));
        Assert.Empty(errors);
        Assert.Equal(name, profile!.Venue);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(4, 0)]
    [InlineData(1025, 2)]
    [InlineData(4, -1)]
    public void colsRows超範圍_拒收(int cols, int rows)
    {
        var req = Request(new[] { 1000, 500 }, new[] { 1300, 530 }, cols: cols, rows: rows);
        var (profile, errors) = CalibrationMath.BuildVenue(req);
        Assert.Null(profile);
        Assert.Contains(errors, e => e.Contains("cols") || e.Contains("rows"));
    }

    [Fact]
    public void 兩角rawX相同_拒收()
    {
        var (profile, errors) = CalibrationMath.BuildVenue(
            Request(new[] { 1000, 500 }, new[] { 1000, 530 }));
        Assert.Null(profile);
        Assert.Contains(errors, e => e.Contains("rawMinX 必須 ≠ rawMaxX"));
    }

    [Fact]
    public void 兩角rawY相同_拒收()
    {
        var (profile, errors) = CalibrationMath.BuildVenue(
            Request(new[] { 1000, 500 }, new[] { 1300, 500 }));
        Assert.Null(profile);
        Assert.Contains(errors, e => e.Contains("rawMinY 必須 ≠ rawMaxY"));
    }

    [Fact]
    public void orientation非法_拒收()
    {
        var req = Request(new[] { 1000, 500 }, new[] { 1300, 530 }, orientation: "diagonal");
        Assert.Null(CalibrationMath.BuildVenue(req).Profile);
    }

    [Fact]
    public void 無face_拒收()
    {
        var (profile, errors) = CalibrationMath.BuildVenue(
            new CalibrationSaveRequest { Venue = "demo" });
        Assert.Null(profile);
        Assert.Contains(errors, e => e.Contains("至少要有一面"));
    }

    [Fact]
    public void 角落缺欄位_拒收()
    {
        var req = Request(new[] { 1000, 500 }, new[] { 1300, 530 });
        req.Faces[0].BottomRightRaw = new[] { 1300 };   // 長度不是 2
        Assert.Null(CalibrationMath.BuildVenue(req).Profile);
    }

    // ── canvas：缺省 bounding box / 過小拒收 ─────────────────────────────────

    [Fact]
    public void 多面_canvas缺省取boundingBox()
    {
        var req = Request(new[] { 1000, 500 }, new[] { 1300, 530 });
        req.Faces.Add(new CalibrationFaceRequest
        {
            Name = "Wall2", Cols = 4, Rows = 2, Orientation = "vertical",
            OriginX = 128, OriginY = 0,
            TopLeftRaw = new[] { 2000, 500 }, BottomRightRaw = new[] { 2300, 530 },
        });
        var (profile, errors) = CalibrationMath.BuildVenue(req);
        Assert.Empty(errors);
        Assert.Equal(256, profile!.Canvas.Width);    // 128 + 4×32
        Assert.Equal(32, profile.Canvas.Height);
    }

    [Fact]
    public void canvas過小_拒收()
    {
        var req = Request(new[] { 1000, 500 }, new[] { 1300, 530 });
        req.Canvas = new CalibrationCanvasRequest { Width = 100, Height = 32 };   // 面要 128 寬
        var (profile, errors) = CalibrationMath.BuildVenue(req);
        Assert.Null(profile);
        Assert.Contains(errors, e => e.Contains("bounding box"));
    }

    [Fact]
    public void canvas顯式給且夠大_採用()
    {
        var req = Request(new[] { 1000, 500 }, new[] { 1300, 530 });
        req.Canvas = new CalibrationCanvasRequest { Width = 500, Height = 600 };
        var (profile, errors) = CalibrationMath.BuildVenue(req);
        Assert.Empty(errors);
        Assert.Equal(500, profile!.Canvas.Width);
        Assert.Equal(600, profile.Canvas.Height);
    }

    // ── roundtrip：寫檔 → VenueProfile.Load → 兩角映射正確（四種 flip）──────

    [Theory]
    [InlineData(1000, 500, 1300, 530)]   // 同向
    [InlineData(1300, 500, 1000, 530)]   // X 反向
    [InlineData(1000, 530, 1300, 500)]   // Y 反向
    [InlineData(1300, 530, 1000, 500)]   // 雙反向
    public void Roundtrip_寫檔載入_兩角映射到頭尾格(int tlX, int tlY, int brX, int brY)
    {
        const int cols = 4, rows = 2;
        var (profile, errors) = CalibrationMath.BuildVenue(
            Request(new[] { tlX, tlY }, new[] { brX, brY }, cols: cols, rows: rows));
        Assert.Empty(errors);

        var path = Path.Combine(
            Directory.CreateTempSubdirectory("calib-roundtrip").FullName, "demo.json");
        File.WriteAllText(path, CalibrationMath.SerializeVenueJson(profile!));

        var loaded = VenueProfile.Load(path);
        var router = new SensorFaceRouter(loaded);

        // 左上角 raw → (0,0)
        Assert.True(router.TryRoute(tlX, tlY, out var tl));
        Assert.Equal((0, 0), (tl.Col, tl.Row));

        // 右下角 raw → (cols-1, rows-1)（rawMax 邊界 px 落在面外一 px、clamp 回最後一格——
        // 與 Unity 對等的已知特性，見 PROTOCOL.md）
        Assert.True(router.TryRoute(brX, brY, out var br));
        Assert.Equal((cols - 1, rows - 1), (br.Col, br.Row));

        // 中央點 → 中央格
        Assert.True(router.TryRoute(1150, 515, out var mid));
        Assert.Equal((2, 1), (mid.Col, mid.Row));
    }
}
