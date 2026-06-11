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
/// down 先到位再按下、平滑收斂、單點所有權、非目標面/grid 零注入、
/// 面內座標 → screen rect 映射（含偏移與負座標）、熱切強制放鍵、SendInput 正規化。
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

    // face 0 = Wall Left：origin (0,640)、640×2048 px。face 2 = Floor（地板）。
    const int WallFace = 0;
    const int FloorFace = 2;

    static MouseModeConfig Cfg(int face = WallFace, int sx = 0, int sy = 0, int sw = 1920, int sh = 1080,
                               double smoothing = 0.5, int moveMinPx = 2) => new()
    {
        Face = face,
        Screen = new ScreenRectConfig { X = sx, Y = sy, Width = sw, Height = sh },
        Smoothing = smoothing,
        MoveMinPx = moveMinPx,
    };

    (MouseDriver Driver, FakeInjector Injector) NewDriver(MouseModeConfig? cfg = null)
    {
        var injector = new FakeInjector();
        return (new MouseDriver(injector, _venue.Faces, cfg ?? Cfg(), _log), injector);
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

    // ── down / move / up 基本序列 ─────────────────────────────────────────────

    [Fact]
    public void down事件_先Move到位再LeftDown()
    {
        var (d, inj) = NewDriver();
        using (d)
        {
            // Wall Left 面中心：px (320, 640+1024) → 面內 (0.5, 0.5) → 螢幕 (960, 540)
            Send(d, Ev(1, TouchPhase.Down, WallFace, 320, 640 + 1024));
            Assert.Equal(new[] { "move(960,540)", "down" }, inj.Calls);
        }
    }

    [Fact]
    public void move事件_平滑tick漸進收斂到目標()
    {
        var (d, inj) = NewDriver(Cfg(smoothing: 0.5, moveMinPx: 2));
        using (d)
        {
            // down 在面左上角 → 螢幕 (0,0)；move 目標面右下角 → 螢幕 (1920,1080)
            Send(d, Ev(1, TouchPhase.Down, WallFace, 0, 640));
            Send(d, Ev(1, TouchPhase.Move, WallFace, 640, 640 + 2048));
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
            Send(d, Ev(1, TouchPhase.Down, WallFace, 0, 640));
            Send(d, Ev(1, TouchPhase.Move, WallFace, 640, 640 + 2048));
            d.TickSmoothing();   // 滑到一半
            d.TickSmoothing();
            int movesBefore = inj.Calls.Count;

            Send(d, Ev(1, TouchPhase.Up, WallFace, 640, 640 + 2048));
            Assert.Equal("up", inj.Calls[^1]);

            // up 後游標凍結：繼續 tick 不再有任何 Move
            for (int i = 0; i < 20; i++) d.TickSmoothing();
            Assert.Equal(movesBefore + 1, inj.Calls.Count);   // 只多了那個 "up"
        }
    }

    // ── 單點所有權 ───────────────────────────────────────────────────────────

    [Fact]
    public void 單點所有權_owner存活時第二觸點完全忽略()
    {
        var (d, inj) = NewDriver();
        using (d)
        {
            Send(d, Ev(1, TouchPhase.Down, WallFace, 320, 640 + 1024));
            int n = inj.Calls.Count;

            // 第二觸點的 down / move / up 全部不產生注入
            Send(d, Ev(2, TouchPhase.Down, WallFace, 100, 700));
            Send(d, Ev(2, TouchPhase.Move, WallFace, 120, 720));
            Send(d, Ev(2, TouchPhase.Up, WallFace, 120, 720));
            for (int i = 0; i < 10; i++) d.TickSmoothing();
            Assert.Equal(n, inj.Calls.Count);

            // owner 還在：它的 up 正常放鍵
            Send(d, Ev(1, TouchPhase.Up, WallFace, 320, 640 + 1024));
            Assert.Equal("up", inj.Calls[^1]);
        }
    }

    [Fact]
    public void owner離手後_新觸點可接手()
    {
        var (d, inj) = NewDriver();
        using (d)
        {
            Send(d, Ev(1, TouchPhase.Down, WallFace, 0, 640));
            Send(d, Ev(1, TouchPhase.Up, WallFace, 0, 640));

            Send(d, Ev(3, TouchPhase.Down, WallFace, 320, 640 + 1024));
            Assert.Equal("move(960,540)", inj.Calls[^2]);
            Assert.Equal("down", inj.Calls[^1]);

            Send(d, Ev(3, TouchPhase.Up, WallFace, 320, 640 + 1024));
            Assert.Equal("up", inj.Calls[^1]);
        }
    }

    // ── 非目標面 / 映射 ──────────────────────────────────────────────────────

    [Fact]
    public void 非目標面_地板觸點零注入()
    {
        var (d, inj) = NewDriver(Cfg(face: WallFace));
        using (d)
        {
            // Floor（face 2）的 down/move/up 全程忽略
            Send(d, Ev(1, TouchPhase.Down, FloorFace, 800, 800));
            Send(d, Ev(1, TouchPhase.Move, FloorFace, 850, 850));
            Send(d, Ev(1, TouchPhase.Up, FloorFace, 850, 850));
            for (int i = 0; i < 10; i++) d.TickSmoothing();
            Assert.Empty(inj.Calls);
        }
    }

    [Fact]
    public void 面內座標映射_含rect偏移與非主螢幕負座標()
    {
        // 第二螢幕在主螢幕左邊：rect (-1920, 120, 1280×720)
        var (d, inj) = NewDriver(Cfg(sx: -1920, sy: 120, sw: 1280, sh: 720));
        using (d)
        {
            // 面內 (0.25, 0.25)：px (160, 640+512) → (-1920 + 320, 120 + 180) = (-1600, 300)
            Send(d, Ev(1, TouchPhase.Down, WallFace, 160, 640 + 512));
            Assert.Equal("move(-1600,300)", inj.Calls[0]);

            Send(d, Ev(1, TouchPhase.Up, WallFace, 160, 640 + 512));

            // 面右下角 = rect 右下角；面外座標 clamp 回 rect 邊界
            Send(d, Ev(2, TouchPhase.Down, WallFace, 640, 640 + 2048));
            Assert.Equal($"move({-1920 + 1280},{120 + 720})", inj.Calls[^2]);
        }
    }

    // ── 熱切 / 設定變更 ──────────────────────────────────────────────────────

    [Fact]
    public void 熱切到grid_按住中ForceRelease自動LeftUp()
    {
        var (d, inj) = NewDriver();
        using (d)
        {
            Send(d, Ev(1, TouchPhase.Down, WallFace, 320, 640 + 1024));
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
    public void 來源面熱切_按住中換面_先放開不留卡死按鍵()
    {
        var (d, inj) = NewDriver(Cfg(face: WallFace));
        using (d)
        {
            Send(d, Ev(1, TouchPhase.Down, WallFace, 320, 640 + 1024));
            d.UpdateConfig(Cfg(face: 4));   // 換到 Wall Right Big
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
        using var driver = new MouseDriver(injector, _venue.Faces, Cfg(face: WallFace), _log);
        pipeline.SetEventObserver(driver);

        // 目標面觸點：滑鼠注入 + WS/UDP 發布兩者都發生
        pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { WallRaw }), 0);
        Assert.Equal("down", injector.Calls[^1]);
        Assert.Single(sink.Frames);

        // 地板觸點：被 driver 忽略（owner 是牆面觸點），發布照常
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
