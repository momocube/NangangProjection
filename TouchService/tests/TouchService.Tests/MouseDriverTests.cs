using TouchService.Config;
using TouchService.Core;
using TouchService.Diagnostics;
using TouchService.Logging;
using TouchService.Mouse;
using TouchService.Output;
using TouchService.Tracking;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// 滑鼠模式 MouseDriver（FakeInjector 記錄呼叫序列，時間/平滑 tick 全部手動驅動，確定性）：
/// **canvas 十字映射**（v6：canvas norm → 螢幕 rect 等比落點，含 content 校正與負座標 rect）、
/// down 先到位再按下、平滑收斂、跨面單點所有權、未啟用面（地板）零注入、
/// 熱切強制放鍵、SendInput 正規化。
/// </summary>
public class MouseDriverTests : IDisposable
{
    sealed class FakeInjector : IMouseInjector
    {
        public readonly List<string> Calls = new();
        public void Move(int x, int y) => Calls.Add($"move({x},{y})");
        public void LeftDown() => Calls.Add("down");
        public void LeftUp() => Calls.Add("up");
    }

    readonly FileLogger _log;
    readonly VenueProfile _venue;

    public MouseDriverTests()
    {
        _log = new FileLogger(
            Directory.CreateTempSubdirectory("touchservice-mouse").FullName, LogLevel.Warn, console: false);
        _venue = VenueProfile.Load(Path.Combine(AppContext.BaseDirectory, "venues", "704.json"));
    }

    public void Dispose() => _log.Dispose();

    // 704 canvas = 2688×3840。face 0 = Wall Left（origin (0,640) 640×2048）、
    // face 2 = Floor（地板，預設不啟用）、face 4 = Wall Right Big。
    const int WallLeft = 0;
    const int Floor = 2;
    const int WallRightBig = 4;

    /// <summary>預設啟用面 = 排除地板（704：0,1,3,4,5,6）。</summary>
    static readonly int[] DefaultFaces = { 0, 1, 3, 4, 5, 6 };

    static MouseModeConfig Cfg(int sx = 0, int sy = 0, int sw = 1920, int sh = 1080,
                               double smoothing = 0.5, int moveMinPx = 2,
                               ContentRectConfig? content = null) => new()
    {
        Screen = new ScreenRectConfig { X = sx, Y = sy, Width = sw, Height = sh },
        Content = content ?? new ContentRectConfig(),
        Smoothing = smoothing,
        MoveMinPx = moveMinPx,
    };

    (MouseDriver Driver, FakeInjector Injector) NewDriver(MouseModeConfig? cfg = null, int[]? faces = null)
    {
        var injector = new FakeInjector();
        return (new MouseDriver(injector, _venue, cfg ?? Cfg(), faces ?? DefaultFaces, _log), injector);
    }

    static TouchEventData Ev(int id, TouchPhase phase, int face, int px, int py) => new()
    {
        Id = id,
        Phase = phase,
        Point = new RoutedPoint
        {
            FaceIndex = face, Col = 0, Row = 0, PixelX = px, PixelY = py, NormX = 0, NormY = 0,
        },
    };

    static void Send(MouseDriver d, params TouchEventData[] events) =>
        d.OnTouchEvents(events);

    static (int X, int Y) ParseMove(string call)
    {
        var inner = call["move(".Length..^1].Split(',');
        return (int.Parse(inner[0]), int.Parse(inner[1]));
    }

    // ── canvas → 螢幕映射數學（純函式對拍）──────────────────────────────────

    [Fact]
    public void canvas映射_預設content滿版_等比落點()
    {
        var screen = new ScreenRectConfig { X = 0, Y = 0, Width = 1920, Height = 1080 };
        var full = new ContentRectConfig();

        Assert.Equal((0d, 0d), MouseDriver.MapCanvasToScreen(0, 0, screen, full));
        Assert.Equal((960d, 540d), MouseDriver.MapCanvasToScreen(0.5, 0.5, screen, full));
        Assert.Equal((1920d, 1080d), MouseDriver.MapCanvasToScreen(1, 1, screen, full));
        Assert.Equal((480d, 270d), MouseDriver.MapCanvasToScreen(0.25, 0.25, screen, full));
    }

    [Fact]
    public void canvas映射_content_offset與scale校正()
    {
        // 投影沒滿版：內容右移 100px、下移 50px、縮 0.8×0.9
        var screen = new ScreenRectConfig { X = 0, Y = 0, Width = 1920, Height = 1080 };
        var content = new ContentRectConfig { OffsetX = 100, OffsetY = 50, ScaleX = 0.8, ScaleY = 0.9 };

        Assert.Equal((100d, 50d), MouseDriver.MapCanvasToScreen(0, 0, screen, content));
        // 0.5：x = 100 + 0.5×1920×0.8 = 868；y = 50 + 0.5×1080×0.9 = 536
        Assert.Equal((868d, 536d), MouseDriver.MapCanvasToScreen(0.5, 0.5, screen, content));
        // 1.0：x = 100 + 1536 = 1636；y = 50 + 972 = 1022（letterbox 內，不到螢幕右下）
        Assert.Equal((1636d, 1022d), MouseDriver.MapCanvasToScreen(1, 1, screen, content));
    }

    [Fact]
    public void canvas映射_非主螢幕負座標rect()
    {
        // 第二螢幕在主螢幕左邊：rect (-1920, 120, 1280×720)
        var screen = new ScreenRectConfig { X = -1920, Y = 120, Width = 1280, Height = 720 };
        var full = new ContentRectConfig();

        Assert.Equal((-1920d, 120d), MouseDriver.MapCanvasToScreen(0, 0, screen, full));
        Assert.Equal((-1600d, 300d), MouseDriver.MapCanvasToScreen(0.25, 0.25, screen, full));
        Assert.Equal((-640d, 840d), MouseDriver.MapCanvasToScreen(1, 1, screen, full));

        // 負座標 rect + content 校正
        var content = new ContentRectConfig { OffsetX = 10, OffsetY = 20, ScaleX = 0.5, ScaleY = 0.5 };
        Assert.Equal((-1920d + 10 + 640, 120d + 20 + 360),
            MouseDriver.MapCanvasToScreen(1, 1, screen, content));
    }

    [Fact]
    public void canvas映射_content校正超界_clamp回螢幕rect()
    {
        var screen = new ScreenRectConfig { X = 0, Y = 0, Width = 1920, Height = 1080 };
        var over = new ContentRectConfig { ScaleX = 1.2, ScaleY = 1.2 };

        // norm 1.0 × scale 1.2 = 2304 → clamp 1920（填錯校正不會把游標丟出目標螢幕）
        Assert.Equal((1920d, 1080d), MouseDriver.MapCanvasToScreen(1, 1, screen, over));

        var negOff = new ContentRectConfig { OffsetX = -500, OffsetY = -500 };
        Assert.Equal((0d, 0d), MouseDriver.MapCanvasToScreen(0, 0, screen, negOff));
    }

    // ── down / move / up 基本序列（canvas 座標）─────────────────────────────

    [Fact]
    public void down事件_先Move到位再LeftDown_canvas等比落點()
    {
        var (d, inj) = NewDriver();
        using (d)
        {
            // Wall Left 上的點 canvas px (336, 1920)：norm (0.125, 0.5) → 螢幕 (240, 540)
            // （v4 bug 對照：舊版面內拉伸會把這點丟到螢幕中央偏左上——現在是 canvas 十字位置）
            Send(d, Ev(1, TouchPhase.Down, WallLeft, 336, 1920));
            Assert.Equal(new[] { "move(240,540)", "down" }, inj.Calls);
        }
    }

    [Fact]
    public void move事件_平滑tick漸進收斂到目標()
    {
        var (d, inj) = NewDriver(Cfg(smoothing: 0.5, moveMinPx: 2));
        using (d)
        {
            // down 在 canvas 左上角 → 螢幕 (0,0)；move 到 canvas 右下角 → 螢幕 (1920,1080)
            Send(d, Ev(1, TouchPhase.Down, WallLeft, 0, 0));
            Send(d, Ev(1, TouchPhase.Move, WallLeft, 2688, 3840));
            Assert.Equal(2, inj.Calls.Count);   // move 只更新目標，不立刻注入

            for (int i = 0; i < 80; i++) d.TickSmoothing();

            var moves = inj.Calls.Skip(2).Select(ParseMove).ToList();
            Assert.True(moves.Count >= 3, $"指數逼近應產生多步 Move（實際 {moves.Count} 步）——不是瞬移");
            Assert.NotEqual((1920, 1080), moves[0]);              // 第一步不是直接到位
            Assert.Equal((1920, 1080), moves[^1]);                // 最終收斂到目標

            // 漸進：每一步單調逼近目標
            var prev = (X: 0, Y: 0);
            foreach (var m in moves)
            {
                Assert.True(m.X >= prev.X && m.Y >= prev.Y, $"步進必須單調逼近：{prev} → {m}");
                prev = m;
            }

            // 收斂後繼續 tick：不再注入
            int n = inj.Calls.Count;
            for (int i = 0; i < 10; i++) d.TickSmoothing();
            Assert.Equal(n, inj.Calls.Count);
        }
    }

    [Fact]
    public void up事件_只放開按鍵_游標停在原地()
    {
        var (d, inj) = NewDriver(Cfg(smoothing: 0.5));
        using (d)
        {
            Send(d, Ev(1, TouchPhase.Down, WallLeft, 0, 640));
            Send(d, Ev(1, TouchPhase.Move, WallLeft, 640, 2688));
            d.TickSmoothing();   // 滑到一半
            d.TickSmoothing();
            int movesBefore = inj.Calls.Count;

            Send(d, Ev(1, TouchPhase.Up, WallLeft, 640, 2688));
            Assert.Equal("up", inj.Calls[^1]);

            // up 後游標凍結：繼續 tick 不再有任何 Move
            for (int i = 0; i < 20; i++) d.TickSmoothing();
            Assert.Equal(movesBefore + 1, inj.Calls.Count);   // 只多了那個 "up"
        }
    }

    // ── 單點所有權（跨所有啟用面）────────────────────────────────────────────

    [Fact]
    public void 單點所有權_owner存活時第二觸點完全忽略()
    {
        var (d, inj) = NewDriver();
        using (d)
        {
            Send(d, Ev(1, TouchPhase.Down, WallLeft, 336, 1920));
            int n = inj.Calls.Count;

            // 第二觸點（同面）的 down / move / up 全部不產生注入
            Send(d, Ev(2, TouchPhase.Down, WallLeft, 100, 700));
            Send(d, Ev(2, TouchPhase.Move, WallLeft, 120, 720));
            Send(d, Ev(2, TouchPhase.Up, WallLeft, 120, 720));
            for (int i = 0; i < 10; i++) d.TickSmoothing();
            Assert.Equal(n, inj.Calls.Count);

            // owner 還在：它的 up 正常放鍵
            Send(d, Ev(1, TouchPhase.Up, WallLeft, 336, 1920));
            Assert.Equal("up", inj.Calls[^1]);
        }
    }

    [Fact]
    public void 跨面單點所有權_他面啟用觸點也被owner擋住_owner離手後可接手()
    {
        var (d, inj) = NewDriver();
        using (d)
        {
            // owner 在 Wall Left
            Send(d, Ev(1, TouchPhase.Down, WallLeft, 336, 1920));
            int n = inj.Calls.Count;

            // Wall Right Big（另一個啟用面）的觸點：owner 在，全部忽略
            Send(d, Ev(2, TouchPhase.Down, WallRightBig, 2400, 1000));
            Send(d, Ev(2, TouchPhase.Move, WallRightBig, 2420, 1020));
            Assert.Equal(n, inj.Calls.Count);

            // owner up → 釋放；Wall Right Big 新 down 接手游標
            Send(d, Ev(1, TouchPhase.Up, WallLeft, 336, 1920));
            Assert.Equal("up", inj.Calls[^1]);

            // canvas px (2688, 1920)：norm (1, 0.5) → 螢幕 (1920, 540)
            Send(d, Ev(3, TouchPhase.Down, WallRightBig, 2688, 1920));
            Assert.Equal("move(1920,540)", inj.Calls[^2]);
            Assert.Equal("down", inj.Calls[^1]);
        }
    }

    // ── faces 過濾 ───────────────────────────────────────────────────────────

    [Fact]
    public void 未啟用面_地板觸點零注入()
    {
        var (d, inj) = NewDriver();   // 預設啟用面排除地板
        using (d)
        {
            // Floor（face 2）的 down/move/up 全程忽略
            Send(d, Ev(1, TouchPhase.Down, Floor, 800, 800));
            Send(d, Ev(1, TouchPhase.Move, Floor, 850, 850));
            Send(d, Ev(1, TouchPhase.Up, Floor, 850, 850));
            for (int i = 0; i < 10; i++) d.TickSmoothing();
            Assert.Empty(inj.Calls);
        }
    }

    [Fact]
    public void faces清單_只勾選的面觸發注入()
    {
        var (d, inj) = NewDriver(faces: new[] { WallRightBig });   // 只勾 face 4
        using (d)
        {
            Send(d, Ev(1, TouchPhase.Down, WallLeft, 336, 1920));   // face 0 沒勾：忽略
            Assert.Empty(inj.Calls);

            Send(d, Ev(2, TouchPhase.Down, WallRightBig, 2688, 1920));
            Assert.Equal(new[] { "move(1920,540)", "down" }, inj.Calls);
        }
    }

    [Fact]
    public void faces空清單_全部停用_零注入()
    {
        var (d, inj) = NewDriver(faces: Array.Empty<int>());
        using (d)
        {
            Send(d, Ev(1, TouchPhase.Down, WallLeft, 336, 1920));
            Send(d, Ev(2, TouchPhase.Down, Floor, 800, 800));
            Assert.Empty(inj.Calls);
        }
    }

    // ── 熱切 / 設定變更 ──────────────────────────────────────────────────────

    [Fact]
    public void 熱切到grid_按住中ForceRelease自動LeftUp()
    {
        var (d, inj) = NewDriver();
        using (d)
        {
            Send(d, Ev(1, TouchPhase.Down, WallLeft, 336, 1920));
            d.ForceRelease();   // host 在 mouse→grid 熱切時呼叫
            Assert.Equal("up", inj.Calls[^1]);

            // 已放開：重複 ForceRelease 不再注入
            int n = inj.Calls.Count;
            d.ForceRelease();
            for (int i = 0; i < 5; i++) d.TickSmoothing();
            Assert.Equal(n, inj.Calls.Count);
        }
    }

    [Fact]
    public void 啟用面熱切_按住中換面清單_先放開不留卡死按鍵()
    {
        var (d, inj) = NewDriver(faces: new[] { WallLeft });
        using (d)
        {
            Send(d, Ev(1, TouchPhase.Down, WallLeft, 336, 1920));
            d.UpdateConfig(Cfg(), new[] { WallRightBig });   // 換成只勾 face 4
            Assert.Equal("up", inj.Calls[^1]);
        }
    }

    // ── SendInput 正規化（純函式對拍）────────────────────────────────────────

    [Fact]
    public void SendInput正規化_virtual_desktop含負原點()
    {
        // 雙螢幕：virtual desktop 原點 (-1920, 0)、3840×1080
        Assert.Equal((16384, 32768),
            Win32MouseInjector.NormalizeToVirtualDesk(-960, 540, -1920, 0, 3840, 1080));
        // 原點本身 → (0,0)
        Assert.Equal((0, 0),
            Win32MouseInjector.NormalizeToVirtualDesk(-1920, 0, -1920, 0, 3840, 1080));
        // 超出邊界 clamp 0~65535
        Assert.Equal((65535, 65535),
            Win32MouseInjector.NormalizeToVirtualDesk(5000, 5000, 0, 0, 1920, 1080));
        Assert.Equal((0, 0),
            Win32MouseInjector.NormalizeToVirtualDesk(-100, -100, 0, 0, 1920, 1080));
        // 退化輸入（metrics 失敗回 0）不除零
        Assert.Equal((0, 0),
            Win32MouseInjector.NormalizeToVirtualDesk(100, 100, 0, 0, 0, 0));
    }

    // ── pipeline 整合：grid 零注入 / mouse 模式發布照常 ──────────────────────

    sealed class CaptureSink : ITouchSink
    {
        public readonly List<byte[]> Frames = new();
        public void Publish(byte[] utf8Json) => Frames.Add(utf8Json);
        public void Dispose() { }
    }

    (TouchPipeline Pipeline, CaptureSink Sink) NewPipeline()
    {
        var sink = new CaptureSink();
        var pipeline = new TouchPipeline(
            new SensorFaceRouter(_venue),
            new TouchTracker(128, 96f, 170),
            new FrameSerializer(_venue),
            new List<ITouchSink> { sink },
            new StatsCollector(), _log, keepaliveIntervalMs: 1_000_000,
            new LockedCellFilter(new List<LockedCellConfig>()));
        return (pipeline, sink);
    }

    // Wall Left（face 0）raw 範圍內的點：rawX 1936~2544、rawY 648~2680
    static readonly RawPoint WallRaw = new(2200, 1600);
    // Floor（face 2）raw 範圍內的點
    static readonly RawPoint FloorRaw = new(3264, 1664);

    [Fact]
    public void grid模式_無觀察者_觸控照常發布且零注入()
    {
        var (pipeline, sink) = NewPipeline();
        var injector = new FakeInjector();   // 沒掛任何觀察者 = grid

        pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { WallRaw }), 0);
        pipeline.Tick(171);   // up

        Assert.True(sink.Frames.Count >= 2);   // down frame + up frame 照常發布
        Assert.Empty(injector.Calls);
        pipeline.Dispose();
    }

    [Fact]
    public void mouse模式_注入與發布並行_發布端不受模式影響()
    {
        var (pipeline, sink) = NewPipeline();
        var injector = new FakeInjector();
        using var driver = new MouseDriver(injector, _venue, Cfg(), DefaultFaces, _log);
        pipeline.SetEventObserver(driver);

        // 啟用面（牆面）觸點：滑鼠注入 + WS/UDP 發布兩者都發生
        pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { WallRaw }), 0);
        Assert.Equal("down", injector.Calls[^1]);
        Assert.Single(sink.Frames);

        // 地板觸點：被 driver 忽略（未啟用面），發布照常
        pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { FloorRaw }), 50);
        int callsAfterFloor = injector.Calls.Count;
        Assert.Equal(2, sink.Frames.Count);

        pipeline.Tick(300);   // 兩點都 up（同一個 flush frame）
        Assert.Equal(3, sink.Frames.Count);
        Assert.Equal("up", injector.Calls[^1]);
        Assert.Equal(callsAfterFloor + 1, injector.Calls.Count);   // 地板 up 不注入、只有 owner up

        // 卸下觀察者（grid 熱切）：後續觸點零注入、發布照常
        pipeline.SetEventObserver(null);
        int n = injector.Calls.Count;
        pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { WallRaw }), 400);
        Assert.Equal(n, injector.Calls.Count);
        Assert.Equal(4, sink.Frames.Count);
        pipeline.Dispose();
    }

    // ── runtime tuning 熱套用（POST /config 的 releaseTimeoutMs / matchRadiusPx）──

    [Fact]
    public void UpdateTuning_releaseTimeout熱套用_下個flush生效()
    {
        var (pipeline, sink) = NewPipeline();
        pipeline.UpdateTuning(96f, 50);   // 170 → 50ms

        pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { WallRaw }), 0);
        pipeline.Tick(40);    // 未逾時
        Assert.Single(sink.Frames);
        pipeline.Tick(60);    // 50ms 逾時 → up（原 170ms 還早）
        Assert.Equal(2, sink.Frames.Count);
        Assert.Contains("\"phase\":\"up\"", System.Text.Encoding.UTF8.GetString(sink.Frames[^1]));
        pipeline.Dispose();
    }
}
