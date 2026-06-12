using System.Text;
using System.Text.Json;
using WallProjector.Config;
using WallProjector.Core;
using WallProjector.Engine;
using WallProjector.Web;
using Xunit;

namespace WallProjector.Tests;

/// <summary>
/// V3（DPI 修正 + 自我診斷）的純函式部分：
/// - screenOverride / contentOverride 解析與套用（CalibrationOverride）
/// - rect 比對容差（WindowPositioner.RectsMatch）
/// - CDP title 標記注入 script 組字（CdpInjector.BuildTitleMarkerScript）
/// - StateJson 的 monitors / screenSource / actualRect / rectMismatch 形狀
/// Win32 行為（DPI 宣告生效、GetWindowRect 讀回、螢幕列舉）屬 Windows 現場驗證清單。
/// </summary>
public class V3DiagnosticsTests : IDisposable
{
    readonly string _dir = Directory.CreateTempSubdirectory("wp-v3-tests").FullName;
    readonly Logger _log = new() { MinLevel = Logger.Level.Error };

    string Write(string name, string json)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, json);
        return path;
    }

    // ── A/D：config override 解析 ───────────────────────────────────────────

    [Fact]
    public void config_缺override欄位_為null_沿用TouchService()
    {
        // V1/V2 的 config 沒有這兩個欄位——必須是 null（= 現行為，不改即用）
        var path = Write("no-override.json", """{ "httpPort": 20121 }""");
        var cfg = ProjectorConfig.Load(path);
        Assert.Null(cfg.ScreenOverride);
        Assert.Null(cfg.ContentOverride);
    }

    [Fact]
    public void config_override欄位_解析正確()
    {
        var path = Write("override.json", """
            {
              "screenOverride":  { "x": 100, "y": 50, "width": 3840, "height": 2160 },
              "contentOverride": { "offsetX": 5, "offsetY": -3, "scaleX": 0.98, "scaleY": 1.02 }
            }
            """);
        var cfg = ProjectorConfig.Load(path);
        Assert.NotNull(cfg.ScreenOverride);
        Assert.Equal(100, cfg.ScreenOverride!.X);
        Assert.Equal(2160, cfg.ScreenOverride.Height);
        Assert.NotNull(cfg.ContentOverride);
        Assert.Equal(-3, cfg.ContentOverride!.OffsetY);
        Assert.Equal(0.98, cfg.ContentOverride.ScaleX);
    }

    [Fact]
    public void calibrationOverride_兩者皆null_原樣回傳_source為touchservice()
    {
        var ts = new TouchServiceState
        {
            Screen = new ScreenRectConfig { X = 0, Y = 0, Width = 1920, Height = 1080 },
            Content = new ContentRectConfig(),
            Source = "live", Reachable = true,
        };
        var applied = CalibrationOverride.Apply(ts, new ProjectorConfig(), _log);
        Assert.Same(ts, applied);   // 零成本：不建新物件
        Assert.Equal("touchservice", applied.ScreenSource);
        Assert.Equal("touchservice", applied.ContentSource);
    }

    [Fact]
    public void calibrationOverride_screenOverride_只換screen_content沿用()
    {
        var ts = new TouchServiceState
        {
            Screen = new ScreenRectConfig { X = 0, Y = 0, Width = 1920, Height = 1080 },
            Content = new ContentRectConfig { OffsetX = 7 },
            Source = "live", Reachable = true, Warning = null,
        };
        var cfg = new ProjectorConfig
        {
            ScreenOverride = new ScreenRectConfig { X = 100, Y = 50, Width = 3840, Height = 2160 },
        };
        var applied = CalibrationOverride.Apply(ts, cfg, _log);

        Assert.Equal(3840, applied.Screen.Width);
        Assert.Equal("override", applied.ScreenSource);
        // content 沒覆寫：沿用 TouchService 的值與來源
        Assert.Equal(7, applied.Content.OffsetX);
        Assert.Equal("touchservice", applied.ContentSource);
        // 失效鏈資訊原樣帶過（override 不影響 live/cache/fallback 判斷）
        Assert.Equal("live", applied.Source);
        Assert.True(applied.Reachable);
    }

    [Fact]
    public void calibrationOverride_contentOverride_只換content()
    {
        var ts = new TouchServiceState
        {
            Screen = new ScreenRectConfig { Width = 1920, Height = 1080 },
            Content = new ContentRectConfig(),
            Source = "cache", Reachable = false, Warning = "離線",
        };
        var cfg = new ProjectorConfig
        {
            ContentOverride = new ContentRectConfig { OffsetX = 12, ScaleY = 0.95 },
        };
        var applied = CalibrationOverride.Apply(ts, cfg, _log);

        Assert.Equal(12, applied.Content.OffsetX);
        Assert.Equal("override", applied.ContentSource);
        Assert.Equal(1920, applied.Screen.Width);
        Assert.Equal("touchservice", applied.ScreenSource);
        Assert.Equal("離線", applied.Warning);
    }

    // ── B：rect 比對容差 ────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0, 0, 0, true)]      // 全等
    [InlineData(2, -2, 2, -2, true)]    // 四分量都在 ±2 邊界上
    [InlineData(3, 0, 0, 0, false)]     // x 超容差
    [InlineData(0, 0, -3, 0, false)]    // width 超容差
    [InlineData(0, 0, 0, 3, false)]     // height 超容差
    public void rectsMatch_預設容差2px(int dx, int dy, int dw, int dh, bool expect)
    {
        var target = new WindowRect(100, 200, 800, 600);
        var actual = new WindowRect(100 + dx, 200 + dy, 800 + dw, 600 + dh);
        Assert.Equal(expect, WindowPositioner.RectsMatch(target, actual));
    }

    [Fact]
    public void rectsMatch_DPI縮放150百分比的典型錯位_必被抓出()
    {
        // 704 症狀的數據樣貌：縮放 150% 時 SetWindowPos 被虛擬化，落點/尺寸 ×1.5
        var target = new WindowRect(0, 540, 960, 540);
        var scaled = new WindowRect(0, 810, 1440, 810);
        Assert.False(WindowPositioner.RectsMatch(target, scaled));
    }

    [SkippableFact]
    public void tryPosition_非Windows_回未對位不炸()
    {
        Skip.If(OperatingSystem.IsWindows(), "本測試驗 Linux no-op 路徑");
        var r = WindowPositioner.TryPosition("[WP-slot-0]", new WindowRect(0, 0, 100, 100),
                                             topmost: false, _log, timeoutMs: 100);
        Assert.False(r.Positioned);
        Assert.Null(r.ActualRect);
        Assert.False(r.RectMismatch);
    }

    // ── E：CDP title 標記 script 組字 ───────────────────────────────────────

    [Fact]
    public void titleMarkerScript_含標記_可重入_導航與改title都防到()
    {
        var js = CdpInjector.BuildTitleMarkerScript(WrapPageBuilder.TitleMarker(3));
        Assert.Contains("var m = '[WP-slot-3]';", js);
        // 可重入：已含標記就不重複 append（addScript + evaluate 都會跑、interval 反覆跑）
        Assert.Contains("document.title.indexOf(m) === -1", js);
        Assert.Contains("document.title = document.title + ' ' + m;", js);
        // <title> 解析 / 站方 JS 重設 title 都會洗掉標記——DOMContentLoaded + interval 補回
        Assert.Contains("document.addEventListener('DOMContentLoaded', apply);", js);
        Assert.Contains("setInterval(apply, 1000);", js);
    }

    [Fact]
    public void titleMarkerScript_marker特殊字元_跳脫不壞JS()
    {
        var js = CdpInjector.BuildTitleMarkerScript("a'b\\c");
        Assert.Contains(@"var m = 'a\'b\\c';", js);
    }

    // ── C/B：StateJson 形狀（monitors / screenSource / actualRect）──────────

    ProjectionEngine MakeEngine(TouchServiceState ts,
                                IReadOnlyList<MonitorEnumerator.MonitorInfo>? monitors = null)
    {
        var configPath = Path.Combine(_dir, $"projections-{Guid.NewGuid():N}.json");
        return new ProjectionEngine(new ProjectorConfig(), configPath, ts, _log,
                                    frameChecker: _ => null,
                                    monitorProvider: () => monitors ?? new List<MonitorEnumerator.MonitorInfo>());
    }

    static TouchServiceState Ts() => new()
    {
        Venue = VenueInfo.Parse(File.ReadAllText("venues/704.json")),
        Screen = new ScreenRectConfig { Width = 1920, Height = 1080 },
        Content = new ContentRectConfig(),
        Source = "live", Reachable = true,
    };

    [Fact]
    public void state_monitors陣列_rect與縮放百分比形狀()
    {
        var monitors = new List<MonitorEnumerator.MonitorInfo>
        {
            new(0, @"\\.\DISPLAY1", 0, 0, 3840, 2160, Primary: true, Dpi: 144, ScalePercent: 150),
            new(1, @"\\.\DISPLAY2", 3840, 0, 1920, 1080, Primary: false, Dpi: 96, ScalePercent: 100),
        };
        using var engine = MakeEngine(Ts(), monitors);
        var root = JsonDocument.Parse(engine.StateJson()).RootElement;

        var ms = root.GetProperty("monitors");
        Assert.Equal(2, ms.GetArrayLength());
        Assert.Equal(@"\\.\DISPLAY1", ms[0].GetProperty("name").GetString());
        Assert.Equal(3840, ms[0].GetProperty("width").GetInt32());
        Assert.Equal(150, ms[0].GetProperty("scalePercent").GetInt32());
        Assert.True(ms[0].GetProperty("primary").GetBoolean());
        Assert.Equal(3840, ms[1].GetProperty("x").GetInt32());
        Assert.Equal(100, ms[1].GetProperty("scalePercent").GetInt32());
        Assert.False(ms[1].GetProperty("primary").GetBoolean());
    }

    [Fact]
    public void state_screenSource_預設touchservice_override時標示()
    {
        using var engine = MakeEngine(Ts());
        var ts1 = JsonDocument.Parse(engine.StateJson()).RootElement.GetProperty("touchService");
        Assert.Equal("touchservice", ts1.GetProperty("screenSource").GetString());
        Assert.Equal("touchservice", ts1.GetProperty("contentSource").GetString());

        // override 經 CalibrationOverride 套用後，state 反映來源
        var overridden = CalibrationOverride.Apply(Ts(), new ProjectorConfig
        {
            ScreenOverride = new ScreenRectConfig { Width = 3840, Height = 2160 },
        }, _log);
        using var engine2 = MakeEngine(overridden);
        var ts2 = JsonDocument.Parse(engine2.StateJson()).RootElement.GetProperty("touchService");
        Assert.Equal("override", ts2.GetProperty("screenSource").GetString());
        Assert.Equal("touchservice", ts2.GetProperty("contentSource").GetString());
        Assert.Equal(3840, ts2.GetProperty("screen").GetProperty("width").GetInt32());
    }

    [Fact]
    public void state_face含actualRect與rectMismatch_未對位時null與false()
    {
        using var engine = MakeEngine(Ts());
        var face = JsonDocument.Parse(engine.StateJson()).RootElement.GetProperty("faces")[0];
        Assert.Equal(JsonValueKind.Null, face.GetProperty("actualRect").ValueKind);
        Assert.False(face.GetProperty("rectMismatch").GetBoolean());
    }

    [Fact]
    public void 管理頁內嵌HTML_含V3診斷區塊()
    {
        // 內嵌資源跟著 assembly 走——確認 V3 UI（螢幕清單 / 實際 rect / override 標示）有進去
        using var s = typeof(AdminApi).Assembly
            .GetManifestResourceStream("WallProjector.Web.adminpage.html")!;
        var html = new StreamReader(s, Encoding.UTF8).ReadToEnd();
        Assert.Contains("monitorInfo", html);
        Assert.Contains("scalePercent", html);
        Assert.Contains("actualRect", html);
        Assert.Contains("rectMismatch", html);
        Assert.Contains("screenSource", html);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
