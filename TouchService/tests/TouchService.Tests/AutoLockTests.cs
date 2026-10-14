using System.Net.WebSockets;
using System.Text;
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
/// 啟動自檢窗（autoLock）：服務啟動後 windowSeconds 內被觸發的格子只計數不發布，
/// 窗結束時命中任一判準（packets ≥ minPackets OR 分離 episodes ≥ minEpisodes）的格子
/// 自動加入 runtime 黑名單（LockSource.Auto）。episode = 同 cell 間隔 &gt; releaseTimeout 切段。
/// 加 runtime 鎖定管理（lock / unlock / locks / savelocks）與 WS hello / locks push。
/// 時間全部注入（nowMs），無 sleep、確定性。
/// </summary>
public class AutoLockTests
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
        public readonly LockedCellFilter Locks;
        public readonly string LogDir;
        readonly FileLogger _log;

        public Harness(AutoLockSelfCheck? autoLock, LockedCellFilter? locks = null,
                       int keepaliveIntervalMs = 1_000_000)
        {
            var venue = VenueProfile.Load(Path.Combine(AppContext.BaseDirectory, "venues", "704.json"));
            Router = new SensorFaceRouter(venue);
            Locks = locks ?? new LockedCellFilter(new List<LockedCellConfig>());
            var tracker = new TouchTracker(128, 96f, 170);
            LogDir = Directory.CreateTempSubdirectory("touchservice-autolock").FullName;
            _log = new FileLogger(LogDir, LogLevel.Warn, console: false);
            Pipeline = new TouchPipeline(
                Router, tracker, new FrameSerializer(venue), new List<ITouchSink> { Sink },
                Stats, _log, keepaliveIntervalMs, Locks, autoLock);
        }

        /// <summary>讀出 harness log 全文（FileLogger 每筆 flush，Finish 後即可讀）。</summary>
        public string ReadLog()
        {
            _log.Dispose();   // 關閉檔案控制代碼後再讀，避免 IO 競態
            var files = Directory.GetFiles(LogDir, "touchservice-*.log");
            var sb = new StringBuilder();
            foreach (var file in files)
            {
                sb.Append(File.ReadAllText(file));
            }
            return sb.ToString();
        }

        public void Send(RawPoint p, long nowMs) =>
            Pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { p }), nowMs);

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

    // Floor 面（face 2）中央附近兩個不同格的 raw 點
    static readonly RawPoint NoisyRaw = new(3264, 1664);
    static readonly RawPoint LegitRaw = new(2800, 2400);

    // 預設值對齊 config 預設：minPackets=10、minEpisodes=3、releaseTimeout=170
    static AutoLockSelfCheck Window10s(int minPackets = 10, int minEpisodes = 3, int releaseTimeoutMs = 170)
        => new(0, 10, minPackets, minEpisodes, releaseTimeoutMs);

    (int Face, int Col, int Row) RouteOf(Harness h, RawPoint p)
    {
        Assert.True(h.Router.TryRoute(p.X, p.Y, out var rp));
        return (rp.FaceIndex, rp.Col, rp.Row);
    }

    // ── 自檢窗核心行為 ───────────────────────────────────────────────────

    [Fact]
    public void 窗內達門檻_自動鎖_窗後永久沉默()
    {
        using var h = new Harness(Window10s(minPackets: 10));
        var noisy = RouteOf(h, NoisyRaw);

        // 噪訊晶片：窗內每 80ms 一包，共 12 包（≥ 門檻 10）
        for (int i = 0; i < 12; i++)
            h.Send(NoisyRaw, i * 80);

        // 窗內：不進 tracker、不發布任何 frame
        Assert.Equal(0, h.Pipeline.ActiveTouches);
        Assert.Empty(h.Sink.Frames);

        // 窗結束（timer tick 跨過 10s）→ 收斂
        h.Pipeline.Tick(10_001);
        Assert.True(h.Locks.IsLocked(noisy.Face, noisy.Col, noisy.Row));
        var entry = h.Locks.Find(noisy.Face, noisy.Col, noisy.Row)!;
        Assert.Equal(LockSource.Auto, entry.Source);
        Assert.Equal(12, entry.Packets);

        // 窗後該格永久沉默：持續發包也不產生任何事件
        for (int i = 0; i < 5; i++)
            h.Send(NoisyRaw, 10_100 + i * 80);
        Assert.Equal(0, h.Pipeline.ActiveTouches);
        Assert.Equal(0, h.CountEvents("down"));
        Assert.Contains("dropped by lock  : 5", h.Stats.Snapshot(0, 0));
    }

    [Fact]
    public void 窗內未達門檻_不鎖_窗後正常down()
    {
        using var h = new Harness(Window10s(minPackets: 10));
        var cell = RouteOf(h, NoisyRaw);

        // 只有 3 包（< 門檻 10）——像有人短暫踩過
        for (int i = 0; i < 3; i++)
            h.Send(NoisyRaw, i * 80);
        Assert.Empty(h.Sink.Frames);

        h.Pipeline.Tick(10_001);
        Assert.False(h.Locks.IsLocked(cell.Face, cell.Col, cell.Row));
        Assert.Equal(0, h.Locks.Count);

        // 窗後正常 down
        h.Send(NoisyRaw, 10_100);
        Assert.Equal(1, h.Pipeline.ActiveTouches);
        Assert.Equal(1, h.CountEvents("down"));
    }

    // ── episode 判準（v7：慢速頻閃型噪訊）─────────────────────────────────
    // 現場實證 2026-06-11：face2 cell(37,14) 每次只發 1 包、間隔 0.5~1.5s，
    // 連續 #1~#84 個 down/up 循環——10s 窗內湊不滿 minPackets=10，躲過 packets 判準。
    // 鐵判準：開機時場上沒人，真人誤踩是一次連續事件，不會在同一格分離地觸發 3 次。

    [Fact]
    public void 慢速頻閃_窗內單包x3段_未達包數門檻也鎖()
    {
        using var h = new Harness(Window10s(minPackets: 10, minEpisodes: 3));
        var noisy = RouteOf(h, NoisyRaw);

        // 每次 1 包、間隔 1s（> releaseTimeout 170ms → 各自成段）
        h.Send(NoisyRaw, 0);
        h.Send(NoisyRaw, 1000);
        h.Send(NoisyRaw, 2000);

        IReadOnlyList<AutoLockSelfCheck.Result>? completed = null;
        h.Pipeline.AutoLockCompleted += r => completed = r;
        h.Pipeline.Tick(10_001);

        Assert.True(h.Locks.IsLocked(noisy.Face, noisy.Col, noisy.Row));
        var entry = h.Locks.Find(noisy.Face, noisy.Col, noisy.Row)!;
        Assert.Equal(LockSource.Auto, entry.Source);
        Assert.Equal(3, entry.Packets);   // packets=3 < minPackets=10 仍鎖（episodes 命中）

        var r = Assert.Single(completed!);
        Assert.Equal(3, r.Episodes);
        Assert.True(r.HitEpisodes);
        Assert.False(r.HitPackets);
        Assert.Equal("episodes", r.Reason);

        // 窗後永久沉默
        h.Send(NoisyRaw, 11_000);
        Assert.Equal(0, h.Pipeline.ActiveTouches);
        Assert.Equal(0, h.CountEvents("down"));
    }

    [Fact]
    public void 連續一長串1段_達包數門檻照鎖_既有路徑不退化()
    {
        using var h = new Harness(Window10s(minPackets: 10, minEpisodes: 3));
        var noisy = RouteOf(h, NoisyRaw);

        // 80ms 重送 12 包 = 1 episode（< minEpisodes），但 packets ≥ 10 → 照鎖
        for (int i = 0; i < 12; i++)
            h.Send(NoisyRaw, i * 80);

        IReadOnlyList<AutoLockSelfCheck.Result>? completed = null;
        h.Pipeline.AutoLockCompleted += r => completed = r;
        h.Pipeline.Tick(10_001);

        Assert.True(h.Locks.IsLocked(noisy.Face, noisy.Col, noisy.Row));
        var r = Assert.Single(completed!);
        Assert.Equal(1, r.Episodes);
        Assert.True(r.HitPackets);
        Assert.False(r.HitEpisodes);
        Assert.Equal("packets", r.Reason);
    }

    [Fact]
    public void 真人模擬_一次連續5包1段_不鎖()
    {
        using var h = new Harness(Window10s(minPackets: 10, minEpisodes: 3));
        var cell = RouteOf(h, LegitRaw);

        // 真人誤踩：一次連續事件（80ms 重送 5 包 = 1 episode、packets < 10）
        for (int i = 0; i < 5; i++)
            h.Send(LegitRaw, i * 80);

        h.Pipeline.Tick(10_001);
        Assert.False(h.Locks.IsLocked(cell.Face, cell.Col, cell.Row));
        Assert.Equal(0, h.Locks.Count);

        // 窗後正常 down
        h.Send(LegitRaw, 10_100);
        Assert.Equal(1, h.CountEvents("down"));
    }

    [Fact]
    public void 窗內2段_未達段數門檻_不鎖_且gap等於timeout視為同段()
    {
        using var h = new Harness(Window10s(minPackets: 10, minEpisodes: 3));
        var cell = RouteOf(h, NoisyRaw);

        // 第 1 段：0ms、170ms（gap == releaseTimeout → 延續，不切段）
        h.Send(NoisyRaw, 0);
        h.Send(NoisyRaw, 170);
        // 第 2 段：1000ms（gap 830 > 170 → 新段）
        h.Send(NoisyRaw, 1000);

        IReadOnlyList<AutoLockSelfCheck.Result>? completed = null;
        h.Pipeline.AutoLockCompleted += r => completed = r;
        h.Pipeline.Tick(10_001);

        Assert.False(h.Locks.IsLocked(cell.Face, cell.Col, cell.Row));
        Assert.NotNull(completed);
        Assert.Empty(completed!);   // 2 段 < 3、3 包 < 10：兩判準皆未命中
    }

    [Fact]
    public void minEpisodes為0_episode判準停用()
    {
        using var h = new Harness(Window10s(minPackets: 10, minEpisodes: 0));
        var cell = RouteOf(h, NoisyRaw);

        // 5 段分離單包（minEpisodes=3 時必鎖）——判準停用後不鎖（packets 5 < 10）
        for (int i = 0; i < 5; i++)
            h.Send(NoisyRaw, i * 1000);

        h.Pipeline.Tick(10_001);
        Assert.False(h.Locks.IsLocked(cell.Face, cell.Col, cell.Row));
        Assert.Equal(0, h.Locks.Count);
    }

    [Fact]
    public void 鎖定log標明每顆命中原因()
    {
        using var h = new Harness(Window10s(minPackets: 10, minEpisodes: 3));
        var slowRaw = NoisyRaw;                  // 慢速頻閃：episodes 命中
        var fastRaw = LegitRaw;                  // 快速重送：packets 命中
        var bothRaw = new RawPoint(3600, 1000);  // 兩者皆命中（Floor 面第三格）
        var slow = RouteOf(h, slowRaw);
        var fast = RouteOf(h, fastRaw);
        var both = RouteOf(h, bothRaw);

        // slow：3 段 × 1 包；fast：1 段 × 12 包；both：4 段 × 5 包 = 20 包
        for (int i = 0; i < 3; i++) h.Send(slowRaw, i * 1000);
        for (int i = 0; i < 12; i++) h.Send(fastRaw, i * 80);
        for (int ep = 0; ep < 4; ep++)
            for (int i = 0; i < 5; i++)
                h.Send(bothRaw, ep * 2000 + i * 80);

        h.Pipeline.Tick(10_001);
        Assert.True(h.Locks.IsLocked(slow.Face, slow.Col, slow.Row));
        Assert.True(h.Locks.IsLocked(fast.Face, fast.Col, fast.Row));
        Assert.True(h.Locks.IsLocked(both.Face, both.Col, both.Row));

        var log = h.ReadLog();
        Assert.Contains($"face={slow.Face} cell=({slow.Col},{slow.Row}) 封包 3 / 3 段 — 命中 episodes", log);
        Assert.Contains($"face={fast.Face} cell=({fast.Col},{fast.Row}) 封包 12 / 1 段 — 命中 packets", log);
        Assert.Contains($"face={both.Face} cell=({both.Col},{both.Row}) 封包 20 / 4 段 — 命中 packets+episodes", log);
    }

    [Fact]
    public void 窗內合法cell也不發布_窗後恢復正常()
    {
        using var h = new Harness(Window10s(minPackets: 10));
        var legit = RouteOf(h, LegitRaw);

        // 窗內合法 cell 觸發 2 包：不發 down、不發任何 frame
        h.Send(LegitRaw, 1000);
        h.Send(LegitRaw, 1080);
        Assert.Equal(0, h.Pipeline.ActiveTouches);
        Assert.Empty(h.Sink.Frames);
        Assert.Equal(0, h.CountEvents("down"));

        h.Pipeline.Tick(10_001);
        Assert.False(h.Locks.IsLocked(legit.Face, legit.Col, legit.Row));

        // 窗後同一格正常 down / up
        h.Send(LegitRaw, 10_100);
        Assert.Equal(1, h.CountEvents("down"));
        h.Pipeline.Tick(10_100 + 171);
        Assert.Equal(1, h.CountEvents("up"));
    }

    [Fact]
    public void 噪訊與合法並存_只鎖達門檻的()
    {
        using var h = new Harness(Window10s(minPackets: 10));
        var noisy = RouteOf(h, NoisyRaw);
        var legit = RouteOf(h, LegitRaw);

        for (int i = 0; i < 100; i++) h.Send(NoisyRaw, i * 80);   // 噪訊：100 包
        h.Send(LegitRaw, 500);                                     // 合法：1 包

        IReadOnlyList<AutoLockSelfCheck.Result>? completed = null;
        h.Pipeline.AutoLockCompleted += r => completed = r;
        h.Pipeline.Tick(10_001);

        Assert.True(h.Locks.IsLocked(noisy.Face, noisy.Col, noisy.Row));
        Assert.False(h.Locks.IsLocked(legit.Face, legit.Col, legit.Row));

        // 事件帶正確結果（face / cell / 封包數）
        Assert.NotNull(completed);
        var r = Assert.Single(completed!);
        Assert.Equal((noisy.Face, noisy.Col, noisy.Row, 100), (r.Face, r.Col, r.Row, r.Packets));

        // 窗後：噪訊沉默、合法正常
        h.Send(NoisyRaw, 10_100);
        h.Send(LegitRaw, 10_100);
        Assert.Equal(1, h.Pipeline.ActiveTouches);
        Assert.Equal(1, h.CountEvents("down"));
    }

    [Fact]
    public void 窗內無任何觸發_自檢通過_不鎖任何格()
    {
        using var h = new Harness(Window10s());
        IReadOnlyList<AutoLockSelfCheck.Result>? completed = null;
        h.Pipeline.AutoLockCompleted += r => completed = r;

        h.Pipeline.Tick(10_001);

        Assert.NotNull(completed);   // 一顆都沒鎖也要收斂（log「自檢通過」）
        Assert.Empty(completed!);
        Assert.Equal(0, h.Locks.Count);

        h.Send(LegitRaw, 10_100);
        Assert.Equal(1, h.CountEvents("down"));
    }

    [Fact]
    public void 窗期間keepalive空frame照發()
    {
        using var h = new Harness(Window10s(), keepaliveIntervalMs: 100);

        h.Send(NoisyRaw, 50);          // 窗內觸發（不發布）
        h.Pipeline.Tick(150);          // keepalive 週期到 → 空 frame
        h.Pipeline.Tick(300);

        Assert.Equal(2, h.Sink.Frames.Count);
        foreach (var f in h.Sink.Frames)
        {
            using var doc = JsonDocument.Parse(f);
            Assert.Equal(0, doc.RootElement.GetProperty("events").GetArrayLength());
            Assert.Equal(0, doc.RootElement.GetProperty("alive").GetArrayLength());
        }
    }

    [Fact]
    public void 停用時_行為與現狀完全相同()
    {
        using var h = new Harness(autoLock: null);

        h.Send(NoisyRaw, 0);   // 啟動瞬間第一包就正常 down
        Assert.Equal(1, h.Pipeline.ActiveTouches);
        Assert.Equal(1, h.CountEvents("down"));
        Assert.Equal(0, h.Locks.Count);
    }

    [Fact]
    public void 窗到期後第一個datagram也會觸發收斂()
    {
        // timer 沒先跨過去、由 OnDatagram 收斂：窗後第一包要先收斂噪訊再正常處理
        using var h = new Harness(Window10s(minPackets: 10));
        var noisy = RouteOf(h, NoisyRaw);

        for (int i = 0; i < 15; i++) h.Send(NoisyRaw, i * 80);

        // 不 Tick，直接送窗後封包（合法 cell）
        h.Send(LegitRaw, 10_500);

        Assert.True(h.Locks.IsLocked(noisy.Face, noisy.Col, noisy.Row));
        Assert.Equal(1, h.CountEvents("down"));   // 窗後合法包正常 down
    }

    [Fact]
    public void 自檢狀態查詢_窗內active_窗後inactive()
    {
        using var h = new Harness(Window10s());

        var (active, remaining) = h.Pipeline.GetAutoLockStatus(2_500);
        Assert.True(active);
        Assert.Equal(8, remaining);   // (10000-2500) / 1000 無條件進位 = 8

        h.Pipeline.Tick(10_001);
        (active, remaining) = h.Pipeline.GetAutoLockStatus(10_100);
        Assert.False(active);
        Assert.Equal(0, remaining);
    }

    // ── config 解析 ──────────────────────────────────────────────────────

    [Fact]
    public void config缺autoLock欄位_預設啟用()
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-al-cfg").FullName;
        var path = Path.Combine(dir, "config.json");
        File.WriteAllText(path, """{ "listenPort": 30001 }""");

        var cfg = ServiceConfig.LoadOrCreate(path);
        Assert.True(cfg.AutoLock.Enabled);
        Assert.Equal(10, cfg.AutoLock.WindowSeconds);
        Assert.Equal(10, cfg.AutoLock.MinPackets);
        Assert.Equal(3, cfg.AutoLock.MinEpisodes);

        // 顯式 null 也容錯
        File.WriteAllText(path, """{ "autoLock": null }""");
        Assert.True(ServiceConfig.LoadOrCreate(path).AutoLock.Enabled);

        // 部分欄位（v6 以前的 config 沒有 minEpisodes → 預設 3）
        File.WriteAllText(path, """{ "autoLock": { "windowSeconds": 5 } }""");
        var partial = ServiceConfig.LoadOrCreate(path).AutoLock;
        Assert.True(partial.Enabled);
        Assert.Equal(5, partial.WindowSeconds);
        Assert.Equal(10, partial.MinPackets);
        Assert.Equal(3, partial.MinEpisodes);

        // minEpisodes 顯式 0 = 停用 episode 判準
        File.WriteAllText(path, """{ "autoLock": { "minEpisodes": 0 } }""");
        Assert.Equal(0, ServiceConfig.LoadOrCreate(path).AutoLock.MinEpisodes);

        // 顯式停用
        File.WriteAllText(path, """{ "autoLock": { "enabled": false } }""");
        Assert.False(ServiceConfig.LoadOrCreate(path).AutoLock.Enabled);
    }

    // ── runtime 鎖定管理（LockedCellFilter 可變層）──────────────────────

    [Fact]
    public void 過濾器_TryAdd與TryRemove_來源可區分()
    {
        var filter = new LockedCellFilter(new[]
        {
            new LockedCellConfig { Face = 0, Cell = new[] { 1, 2 } },
        });

        Assert.True(filter.TryAdd(2, 7, 13, LockSource.Auto, packets: 99));
        Assert.True(filter.TryAdd(1, 3, 4, LockSource.Manual));
        Assert.False(filter.TryAdd(2, 7, 13, LockSource.Manual));   // 重複加鎖 → false
        Assert.Equal(3, filter.Count);
        Assert.Equal(1, filter.AutoCount);

        var snap = filter.Snapshot();
        Assert.Equal(3, snap.Count);
        Assert.Equal(LockSource.Config, snap.First(e => e.Face == 0).Source);
        Assert.Equal(LockSource.Manual, snap.First(e => e.Face == 1).Source);
        var auto = snap.First(e => e.Face == 2);
        Assert.Equal(LockSource.Auto, auto.Source);
        Assert.Equal(99, auto.Packets);

        // 任何來源都可解（含 config 來源——只影響 runtime）
        Assert.True(filter.TryRemove(0, 1, 2, out var removed));
        Assert.Equal(LockSource.Config, removed!.Source);
        Assert.False(filter.IsLocked(0, 1, 2));
        Assert.False(filter.TryRemove(0, 1, 2, out _));   // 已不存在 → false
        Assert.Equal(2, filter.Count);
    }

    // ── host 指令（lock / unlock / locks / savelocks）────────────────────

    static (TouchServiceHost Host, string ConfigPath, FileLogger Log) NewHost(
        string configJson, bool wsEnabled = false)
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-al-host").FullName;
        var configPath = Path.Combine(dir, "config.json");
        File.WriteAllText(configPath, configJson);
        var cfg = ServiceConfig.LoadOrCreate(configPath);
        cfg.ListenPort = 0;
        cfg.VenueProfile = Path.Combine(AppContext.BaseDirectory, "venues", "704.json");
        cfg.UdpOutput = new UdpOutputConfig { Enabled = false };
        cfg.WebSocketOutput = new WebSocketOutputConfig { Enabled = wsEnabled, Port = 0 };
        cfg.Log = new LogConfig { Console = false, Dir = dir, Level = "Warn" };
        var log = new FileLogger(dir, LogLevel.Warn, console: false);
        var host = new TouchServiceHost(cfg, AppContext.BaseDirectory, log, configPath);
        host.Start();
        return (host, configPath, log);
    }

    [Fact]
    public void lock與unlock指令_runtime生效()
    {
        var (host, _, log) = NewHost("""{ "autoLock": { "enabled": false } }""");
        using (host)
        using (log)
        {
            var msg = host.LockCell(2, 7, 13);
            Assert.Contains("已鎖定", msg);
            Assert.True(host.Locks.IsLocked(2, 7, 13));
            Assert.Equal(LockSource.Manual, host.Locks.Find(2, 7, 13)!.Source);

            // 重複加鎖
            Assert.Contains("已鎖定（來源 manual），未重複加鎖", host.LockCell(2, 7, 13));

            // 超出範圍防呆
            Assert.Contains("超出場域面數", host.LockCell(99, 0, 0));
            Assert.Contains("超出", host.LockCell(2, 9999, 0));

            // 解鎖
            Assert.Contains("已解鎖", host.UnlockCell(2, 7, 13));
            Assert.False(host.Locks.IsLocked(2, 7, 13));
            Assert.Contains("本來就沒有鎖定", host.UnlockCell(2, 7, 13));
        }
    }

    [Fact]
    public void unlock解config來源_只影響runtime不改檔()
    {
        var configJson = """
            { "autoLock": { "enabled": false },
              "lockedCells": [ { "face": 2, "cell": [7, 13] } ] }
            """;
        var (host, configPath, log) = NewHost(configJson);
        using (host)
        using (log)
        {
            Assert.True(host.Locks.IsLocked(2, 7, 13));
            var msg = host.UnlockCell(2, 7, 13);
            Assert.Contains("config 來源", msg);
            Assert.False(host.Locks.IsLocked(2, 7, 13));

            // 檔案沒被動
            Assert.Contains("\"cell\": [7, 13]", File.ReadAllText(configPath));
        }
    }

    [Fact]
    public void locks指令_列出全部來源與封包數()
    {
        var configJson = """
            { "autoLock": { "enabled": false },
              "lockedCells": [ { "face": 0, "cell": [1, 2] } ] }
            """;
        var (host, _, log) = NewHost(configJson);
        using (host)
        using (log)
        {
            host.Locks.TryAdd(2, 7, 13, LockSource.Auto, packets: 125);
            host.LockCell(1, 3, 4);

            var text = host.LocksSnapshot();
            Assert.Contains("3 顆（config 1 / auto 1 / manual 1）", text);
            Assert.Contains("(1,2)", text);
            Assert.Contains("(7,13)", text);
            Assert.Contains("(3,4)", text);
            Assert.Contains("config", text);
            Assert.Contains("auto", text);
            Assert.Contains("manual", text);
            Assert.Contains("125", text);          // auto 封包數
            Assert.Contains("2 Floor", text);      // face 名稱
        }
    }

    [Fact]
    public void savelocks_合併寫回config_可roundtrip且保留其他欄位()
    {
        var configJson = """
            {
              "listenPort": 30001,
              "releaseTimeoutMs": 190,
              "customFutureField": { "keep": true },
              "autoLock": { "enabled": false },
              "lockedCells": [ { "face": 0, "cell": [1, 2] } ]
            }
            """;
        var (host, configPath, log) = NewHost(configJson);
        using (host)
        using (log)
        {
            host.Locks.TryAdd(2, 7, 13, LockSource.Auto, packets: 125);   // 模擬自檢結果
            host.LockCell(1, 3, 4);                                        // manual
            host.LockCell(1, 3, 4);                                        // 重複——去重

            var msg = host.SaveLocks();
            Assert.Contains("已寫回 3 顆", msg);

            // roundtrip：重新載入 config → 3 顆全部以 config 來源生效
            var reloaded = ServiceConfig.LoadOrCreate(configPath);
            Assert.Equal(3, reloaded.LockedCells.Count);
            var filter = new LockedCellFilter(reloaded.LockedCells);
            Assert.True(filter.IsLocked(0, 1, 2));
            Assert.True(filter.IsLocked(1, 3, 4));
            Assert.True(filter.IsLocked(2, 7, 13));

            // 其他欄位原樣保留（含未知欄位），縮排 2 空格
            var raw = File.ReadAllText(configPath);
            Assert.Equal(30001, reloaded.ListenPort);
            Assert.Equal(190, reloaded.ReleaseTimeoutMs);
            Assert.Contains("customFutureField", raw);
            Assert.Contains("\n  \"lockedCells\"", raw);
        }
    }

    // ── WS hello / locks push ────────────────────────────────────────────

    static async Task<JsonDocument> ReceiveJson(ClientWebSocket ws)
    {
        var buf = new byte[1 << 16];
        int off = 0;
        while (true)
        {
            var r = await ws.ReceiveAsync(buf.AsMemory(off), CancellationToken.None)
                .AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            off += r.Count;
            if (r.EndOfMessage) return JsonDocument.Parse(Encoding.UTF8.GetString(buf, 0, off));
        }
    }

    [Fact]
    public async Task hello含lockedCells與source_鎖定變動推locks訊息()
    {
        var configJson = """
            { "autoLock": { "enabled": false },
              "lockedCells": [ { "face": 2, "cell": [7, 13] } ] }
            """;
        var (host, _, log) = NewHost(configJson, wsEnabled: true);
        using (host)
        using (log)
        {
            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri($"ws://127.0.0.1:{host.WsHub!.Port}/"), CancellationToken.None);

            // hello：帶 config 鎖定 + 自檢狀態（停用 → inactive）
            using (var hello = await ReceiveJson(ws))
            {
                var root = hello.RootElement;
                Assert.Equal("hello", root.GetProperty("type").GetString());
                Assert.False(root.GetProperty("autoLockActive").GetBoolean());
                Assert.Equal(0, root.GetProperty("autoLockRemainingSeconds").GetInt32());

                var cells = root.GetProperty("lockedCells");
                Assert.Equal(1, cells.GetArrayLength());
                var c = cells[0];
                Assert.Equal(2, c.GetProperty("face").GetInt32());
                Assert.Equal(7, c.GetProperty("cell")[0].GetInt32());
                Assert.Equal(13, c.GetProperty("cell")[1].GetInt32());
                Assert.Equal("config", c.GetProperty("source").GetString());
            }

            // lock 指令 → 所有 WS client 收到 locks push
            host.LockCell(1, 3, 4);
            using (var push = await ReceiveJson(ws))
            {
                var root = push.RootElement;
                Assert.Equal("locks", root.GetProperty("type").GetString());
                var cells = root.GetProperty("lockedCells");
                Assert.Equal(2, cells.GetArrayLength());
                Assert.Contains(cells.EnumerateArray(),
                    c => c.GetProperty("source").GetString() == "manual" &&
                         c.GetProperty("face").GetInt32() == 1);
            }

            // unlock → 再推一包
            host.UnlockCell(2, 7, 13);
            using (var push = await ReceiveJson(ws))
            {
                Assert.Equal("locks", push.RootElement.GetProperty("type").GetString());
                Assert.Equal(1, push.RootElement.GetProperty("lockedCells").GetArrayLength());
            }
        }
    }

    [Fact]
    public async Task 自檢進行中_hello帶active與剩餘秒數()
    {
        var (host, _, log) = NewHost(
            """{ "autoLock": { "enabled": true, "windowSeconds": 3600, "minPackets": 10 } }""",
            wsEnabled: true);
        using (host)
        using (log)
        {
            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri($"ws://127.0.0.1:{host.WsHub!.Port}/"), CancellationToken.None);

            using var hello = await ReceiveJson(ws);
            var root = hello.RootElement;
            Assert.True(root.GetProperty("autoLockActive").GetBoolean());
            Assert.InRange(root.GetProperty("autoLockRemainingSeconds").GetInt32(), 3595, 3600);
        }
    }
}
