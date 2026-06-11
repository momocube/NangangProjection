using System.Text.Json;
using TouchService.Config;
using TouchService.Core;
using TouchService.Diagnostics;
using TouchService.Logging;
using TouchService.Output;
using TouchService.Tracking;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// 鎖定晶片過濾（config lockedCells）：噪訊晶片的點在進 touch tracker 前丟棄。
/// 對應 Unity SensorTestManager 的 IsRawPixelLocked / locked[] 移植。
/// </summary>
public class LockedCellsTests
{
    sealed class CaptureSink : ITouchSink
    {
        public readonly List<byte[]> Frames = new();
        public void Publish(byte[] utf8Json) => Frames.Add(utf8Json);
        public void Dispose() { }
    }

    sealed class Harness : IDisposable
    {
        public readonly TouchPipeline Pipeline;
        public readonly CaptureSink Sink = new();
        public readonly StatsCollector Stats = new();
        public readonly SensorFaceRouter Router;
        readonly FileLogger _log;

        public Harness(LockedCellFilter? filter)
        {
            var venue = VenueProfile.Load(Path.Combine(AppContext.BaseDirectory, "venues", "704.json"));
            Router = new SensorFaceRouter(venue);
            var tracker = new TouchTracker(128, 96f, 170);
            _log = new FileLogger(
                Directory.CreateTempSubdirectory("touchservice-locked").FullName, LogLevel.Warn, console: false);
            Pipeline = new TouchPipeline(
                Router, tracker, new FrameSerializer(venue), new List<ITouchSink> { Sink },
                Stats, _log, keepaliveIntervalMs: 1_000_000, filter);
        }

        /// <summary>送一個單點封包進管線（合法 wire format）。</summary>
        public void Send(RawPoint p, long nowMs) =>
            Pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { p }), nowMs);

        /// <summary>已發布 frame 中指定 phase 的事件數。</summary>
        public int CountEvents(string phase)
        {
            int n = 0;
            foreach (var f in Sink.Frames)
            {
                using var doc = JsonDocument.Parse(f);
                foreach (var e in doc.RootElement.GetProperty("events").EnumerateArray())
                    if (e.GetProperty("phase").GetString() == phase) n++;
            }
            return n;
        }

        public void Dispose() => _log.Dispose();
    }

    // Floor 面（face 2）中央附近的 raw 點（範圍 2576-3952 / 648-2680）
    static readonly RawPoint FloorRaw = new(3264, 1664);

    static LockedCellFilter FilterFor(params (int Face, int Col, int Row)[] cells) =>
        new(cells.Select(c => new LockedCellConfig { Face = c.Face, Cell = new[] { c.Col, c.Row } }));

    // ── 過濾行為（管線層）─────────────────────────────────────────────────

    [Fact]
    public void 鎖定格的點_不產生down()
    {
        // 先用同一套 router 算出 raw 點所屬的 face/cell，再鎖那一格
        var venue = VenueProfile.Load(Path.Combine(AppContext.BaseDirectory, "venues", "704.json"));
        Assert.True(new SensorFaceRouter(venue).TryRoute(FloorRaw.X, FloorRaw.Y, out var rp));

        using var h = new Harness(FilterFor((rp.FaceIndex, rp.Col, rp.Row)));

        // 噪訊晶片行為：同一格持續發包
        for (int i = 0; i < 10; i++)
            h.Send(FloorRaw, i * 80);

        Assert.Equal(0, h.Pipeline.ActiveTouches);
        Assert.Empty(h.Sink.Frames);   // 沒有任何事件 → 沒有任何 frame
        Assert.Contains("dropped by lock  : 10", h.Stats.Snapshot(0, 0));
    }

    [Fact]
    public void 相鄰格不受影響()
    {
        Assert.True(new Harness(null).Router.TryRoute(FloorRaw.X, FloorRaw.Y, out var locked));

        using var h = new Harness(FilterFor((locked.FaceIndex, locked.Col, locked.Row)));

        // 往 +X 掃出「同列下一格」的 raw 點（同一面、col+1）
        RawPoint adjacent = default;
        bool found = false;
        for (int dx = 1; dx <= 200 && !found; dx++)
        {
            if (h.Router.TryRoute(FloorRaw.X + dx, FloorRaw.Y, out var rp) &&
                rp.FaceIndex == locked.FaceIndex && rp.Col == locked.Col + 1 && rp.Row == locked.Row)
            {
                adjacent = new RawPoint(FloorRaw.X + dx, FloorRaw.Y);
                found = true;
            }
        }
        Assert.True(found, "找不到相鄰格的 raw 點（場域校正參數變了？）");

        h.Send(adjacent, 0);

        Assert.Equal(1, h.Pipeline.ActiveTouches);
        Assert.Equal(1, h.CountEvents("down"));
        Assert.Contains("dropped by lock  : 0", h.Stats.Snapshot(1, 0));
    }

    [Fact]
    public void 空lockedCells_行為不變()
    {
        using var h = new Harness(new LockedCellFilter(new List<LockedCellConfig>()));

        h.Send(FloorRaw, 0);

        Assert.Equal(1, h.Pipeline.ActiveTouches);
        Assert.Equal(1, h.CountEvents("down"));
        Assert.Contains("dropped by lock  : 0", h.Stats.Snapshot(1, 0));
    }

    // ── config 解析容錯 ──────────────────────────────────────────────────

    [Fact]
    public void config缺lockedCells欄位_為空()
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-lock-cfg").FullName;
        var path = Path.Combine(dir, "config.json");
        File.WriteAllText(path, """{ "listenPort": 30001 }""");

        var cfg = ServiceConfig.LoadOrCreate(path);
        Assert.NotNull(cfg.LockedCells);
        Assert.Empty(cfg.LockedCells);

        // 顯式 null 也容錯
        File.WriteAllText(path, """{ "lockedCells": null }""");
        Assert.Empty(ServiceConfig.LoadOrCreate(path).LockedCells);
    }

    [Fact]
    public void config含lockedCells_正確解析與命中()
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-lock-cfg2").FullName;
        var path = Path.Combine(dir, "config.json");
        File.WriteAllText(path, """
            { "lockedCells": [ { "face": 2, "cell": [7, 13] }, { "face": 0, "cell": [0, 0] } ] }
            """);

        var cfg = ServiceConfig.LoadOrCreate(path);
        Assert.Equal(2, cfg.LockedCells.Count);

        var filter = new LockedCellFilter(cfg.LockedCells);
        Assert.Equal(2, filter.Count);
        Assert.Equal(0, filter.Skipped);
        Assert.True(filter.IsLocked(2, 7, 13));
        Assert.True(filter.IsLocked(0, 0, 0));
        Assert.False(filter.IsLocked(2, 8, 13));   // 相鄰 col
        Assert.False(filter.IsLocked(2, 7, 14));   // 相鄰 row
        Assert.False(filter.IsLocked(1, 7, 13));   // 不同面、相同 cell（packing 不可碰撞）
    }

    [Fact]
    public void 格式不完整的項_跳過不擋啟動()
    {
        var entries = new List<LockedCellConfig>
        {
            new() { Face = 2 },                            // 缺 cell
            new() { Face = -1, Cell = new[] { 1, 2 } },    // face 負值
            new() { Face = 0, Cell = new[] { 1 } },        // cell 長度錯
            new() { Face = 0, Cell = new[] { 3, 4 } },     // 唯一有效項
        };

        var filter = new LockedCellFilter(entries);
        Assert.Equal(1, filter.Count);
        Assert.Equal(3, filter.Skipped);
        Assert.True(filter.IsLocked(0, 3, 4));
    }

    // ── touches 指令輸出 ─────────────────────────────────────────────────

    [Fact]
    public void touches指令_輸出格式()
    {
        var venue = VenueProfile.Load(Path.Combine(AppContext.BaseDirectory, "venues", "704.json"));

        var touches = new List<ActiveTouchSnapshot>
        {
            new()
            {
                Id = 15,
                Point = new RoutedPoint
                {
                    FaceIndex = 0, Col = 3, Row = 40,
                    PixelX = 96, PixelY = 1290, NormX = 0f, NormY = 0f,
                },
                AgeMs = 2100,
            },
            new()
            {
                Id = 12,
                Point = new RoutedPoint
                {
                    FaceIndex = 2, Col = 7, Row = 13,
                    PixelX = 1043, PixelY = 512, NormX = 0f, NormY = 0f,
                },
                AgeMs = 34_200,
            },
        };

        var text = TouchServiceHost.FormatTouches(touches, venue.Faces);

        Assert.Contains("活躍 touch：2 筆", text);
        Assert.Contains("12", text);
        Assert.Contains("2 Floor", text);          // face index + 面名稱
        Assert.Contains("(7,13)", text);           // cell
        Assert.Contains("(1043,512)", text);       // px
        Assert.Contains("34.2s", text);            // 存活秒數
        Assert.Contains("0 Wall Left", text);
        Assert.Contains("2.1s", text);
        Assert.Contains("lockedCells", text);      // 現場 SOP 提示

        // 存活久的排前面（噪訊晶片浮到最上面）
        Assert.True(text.IndexOf("34.2s") < text.IndexOf("2.1s"), "應依存活時間降冪排序");
    }

    [Fact]
    public void touches指令_無活躍touch()
    {
        var venue = VenueProfile.Load(Path.Combine(AppContext.BaseDirectory, "venues", "704.json"));
        var text = TouchServiceHost.FormatTouches(new List<ActiveTouchSnapshot>(), venue.Faces);

        Assert.Contains("活躍 touch：0 筆", text);
        Assert.Contains("目前沒有活躍 touch", text);
    }

    [Fact]
    public void touches快照_存活時間正確()
    {
        using var h = new Harness(null);
        h.Send(FloorRaw, 0);

        var touches = new List<ActiveTouchSnapshot>();
        h.Pipeline.SnapshotTouches(5000, touches);

        var t = Assert.Single(touches);
        Assert.Equal(5000, t.AgeMs);
        Assert.Equal(2, t.Point.FaceIndex);   // Floor
    }
}
