using System.Net;
using System.Text;
using System.Text.Json;
using TouchService.Config;
using TouchService.Core;
using TouchService.Logging;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// v9 臨時鎖（staging）機制：鎖後活動統計（CheckAndCount / DroppedSinceLock /
/// LastDropMs）、全解鎖（Clear / unlockall）、來源重分類（轉正式 promote /
/// savelocks 後標 config）、noiseCapture config 容錯。
/// 臨時鎖語意 = 立即生效過濾但不持久化（重啟即消失）；已保存 = config 來源。
/// </summary>
public class LockStagingTests
{
    // ── LockedCellFilter 單元 ────────────────────────────────────────────────

    [Fact]
    public void CheckAndCount_未鎖回false不計_鎖定累計鎖後活動()
    {
        var f = new LockedCellFilter(null);
        Assert.False(f.CheckAndCount(2, 7, 13, 100));   // 沒鎖：放行

        Assert.True(f.TryAdd(2, 7, 13, LockSource.Manual, nowMs: 1000));
        Assert.True(f.CheckAndCount(2, 7, 13, 1080));
        Assert.True(f.CheckAndCount(2, 7, 13, 1160));
        Assert.False(f.CheckAndCount(2, 7, 14, 1160));  // 相鄰格不受影響

        var e = Assert.Single(f.Snapshot());
        Assert.Equal(1000, e.LockedAtMs);
        Assert.Equal(2, e.DroppedSinceLock);
        Assert.Equal(1160, e.LastDropMs);

        // 剛鎖、還沒發過 → LastDropMs = -1（UI 顯示「鎖後安靜」）
        Assert.True(f.TryAdd(0, 1, 2, LockSource.Manual, nowMs: 2000));
        var quiet = f.Snapshot().Single(x => x.Face == 0);
        Assert.Equal(0, quiet.DroppedSinceLock);
        Assert.Equal(-1, quiet.LastDropMs);
    }

    [Fact]
    public void Snapshot_回傳副本_統計時點一致()
    {
        var f = new LockedCellFilter(null);
        f.TryAdd(2, 7, 13, LockSource.Manual, nowMs: 0);
        f.CheckAndCount(2, 7, 13, 80);

        var snap = Assert.Single(f.Snapshot());
        f.CheckAndCount(2, 7, 13, 160);   // 快照後續寫

        Assert.Equal(1, snap.DroppedSinceLock);             // 副本不被後續寫入改動
        Assert.Equal(2, f.Snapshot()[0].DroppedSinceLock);  // 新快照看到新值
    }

    [Fact]
    public void Clear_全解鎖_含config來源_回傳格數()
    {
        var f = new LockedCellFilter(new[]
        {
            new LockedCellConfig { Face = 2, Cell = new[] { 7, 13 } },
        });
        f.TryAdd(0, 1, 2, LockSource.Auto, packets: 50);
        f.TryAdd(1, 3, 4, LockSource.Manual);
        Assert.Equal(3, f.Count);

        Assert.Equal(3, f.Clear());
        Assert.Equal(0, f.Count);
        Assert.False(f.IsLocked(2, 7, 13));
        Assert.False(f.IsLocked(0, 1, 2));

        Assert.Equal(0, f.Clear());   // 冪等：空清單回 0
    }

    [Fact]
    public void Reclassify_轉正式_保留鎖後活動統計()
    {
        var f = new LockedCellFilter(null);
        f.TryAdd(2, 7, 13, LockSource.Manual, nowMs: 500);
        f.CheckAndCount(2, 7, 13, 600);

        Assert.True(f.Reclassify(2, 7, 13, LockSource.Config));
        var e = Assert.Single(f.Snapshot());
        Assert.Equal(LockSource.Config, e.Source);
        Assert.Equal(500, e.LockedAtMs);          // 鎖定時刻不重置
        Assert.Equal(1, e.DroppedSinceLock);      // 鎖後活動不重置
        Assert.True(f.IsLocked(2, 7, 13));        // 過濾照常生效

        Assert.False(f.Reclassify(9, 9, 9, LockSource.Config));   // 沒鎖的格

        f.TryAdd(0, 1, 2, LockSource.Auto);
        Assert.Equal(1, f.ReclassifyAll(LockSource.Config));      // 只剩 auto 那格要改
        Assert.All(f.Snapshot(), x => Assert.Equal(LockSource.Config, x.Source));
    }

    // ── host / API 層 ────────────────────────────────────────────────────────

    static (TouchServiceHost Host, string ConfigPath, FileLogger Log) NewHost(string configJson)
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-staging").FullName;
        var configPath = Path.Combine(dir, "config.json");
        File.WriteAllText(configPath, configJson);
        var cfg = ServiceConfig.LoadOrCreate(configPath);
        cfg.ListenPort = 0;
        cfg.VenueProfile = Path.Combine(AppContext.BaseDirectory, "venues", "704.json");
        cfg.UdpOutput = new UdpOutputConfig { Enabled = false };
        cfg.WebSocketOutput = new WebSocketOutputConfig { Enabled = true, Port = 0 };
        cfg.Log = new LogConfig { Console = false, Dir = dir, Level = "Warn" };
        var log = new FileLogger(dir, LogLevel.Warn, console: false);
        var host = new TouchServiceHost(cfg, AppContext.BaseDirectory, log, configPath);
        host.Start();
        return (host, configPath, log);
    }

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

    static async Task<(HttpStatusCode Status, string Body)> Post(TouchServiceHost host, string path, string json = "")
    {
        var resp = await Http.PostAsync($"http://127.0.0.1:{host.WsHub!.Port}{path}",
            new StringContent(json, Encoding.UTF8, "application/json"));
        return (resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    static async Task<(HttpStatusCode Status, string Body)> Get(TouchServiceHost host, string path)
    {
        var resp = await Http.GetAsync($"http://127.0.0.1:{host.WsHub!.Port}{path}");
        return (resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnlockAll_API與stdin同語意_只動runtime不動config()
    {
        var (host, configPath, log) = NewHost("""
            { "autoLock": { "enabled": false },
              "lockedCells": [ { "face": 2, "cell": [7, 13] } ] }
            """);
        using (host)
        using (log)
        {
            host.LockCell(0, 1, 2);   // runtime manual
            Assert.Equal(2, host.Locks.Count);

            var (status, body) = await Post(host, "/locks/unlockall");
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Contains("\"ok\":true", body);
            Assert.Equal(0, host.Locks.Count);

            // config.json 不動：lockedCells 還在（重啟會鎖回）
            using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
            Assert.Equal(1, doc.RootElement.GetProperty("lockedCells").GetArrayLength());

            // 冪等：再解一次回「無可解除」
            var output = host.Commands.Execute("unlockall");
            Assert.Contains("無可解除", output);
        }
    }

    [Fact]
    public async Task Promote_單格轉正式_寫config並改標已保存()
    {
        var (host, configPath, log) = NewHost("""{ "autoLock": { "enabled": false } }""");
        using (host)
        using (log)
        {
            host.LockCell(2, 7, 13);
            host.LockCell(0, 1, 2);

            var (status, body) = await Post(host, "/locks/promote", """{ "face": 2, "col": 7, "row": 13 }""");
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Contains("\"ok\":true", body);

            // 只有 promote 的那格進 config；另一格仍是臨時
            using (var doc = JsonDocument.Parse(File.ReadAllText(configPath)))
            {
                var cells = doc.RootElement.GetProperty("lockedCells");
                var one = Assert.Single(cells.EnumerateArray());
                Assert.Equal(2, one.GetProperty("face").GetInt32());
                Assert.Equal(7, one.GetProperty("cell")[0].GetInt32());
            }
            Assert.Equal(LockSource.Config, host.Locks.Find(2, 7, 13)!.Source);
            Assert.Equal(LockSource.Manual, host.Locks.Find(0, 1, 2)!.Source);

            // 冪等：已保存的格再 promote 回 ok（不重複 append）
            (_, body) = await Post(host, "/locks/promote", """{ "face": 2, "col": 7, "row": 13 }""");
            Assert.Contains("\"ok\":true", body);
            using (var doc = JsonDocument.Parse(File.ReadAllText(configPath)))
                Assert.Equal(1, doc.RootElement.GetProperty("lockedCells").GetArrayLength());

            // 沒鎖的格：ok:false
            (_, body) = await Post(host, "/locks/promote", """{ "face": 5, "col": 0, "row": 0 }""");
            Assert.Contains("\"ok\":false", body);
        }
    }

    [Fact]
    public async Task Savelocks_全部轉正式_寫回後來源全標config()
    {
        var (host, configPath, log) = NewHost("""{ "autoLock": { "enabled": false } }""");
        using (host)
        using (log)
        {
            host.LockCell(2, 7, 13);
            host.LockCell(0, 1, 2);

            var (status, body) = await Post(host, "/locks/save");
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Contains("\"ok\":true", body);

            using (var doc = JsonDocument.Parse(File.ReadAllText(configPath)))
                Assert.Equal(2, doc.RootElement.GetProperty("lockedCells").GetArrayLength());

            // 寫回後全部標 config（已保存）——UI 臨時鎖區清空
            Assert.All(host.Locks.Snapshot(), e => Assert.Equal(LockSource.Config, e.Source));

            var (_, locksBody) = await Get(host, "/locks");
            using (var doc = JsonDocument.Parse(locksBody))
                Assert.All(doc.RootElement.GetProperty("cells").EnumerateArray(),
                    c => Assert.True(c.GetProperty("saved").GetBoolean()));
        }
    }

    [Fact]
    public void NoiseCapture_config預設與容錯()
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-nc").FullName;
        var path = Path.Combine(dir, "config.json");

        // 缺欄位 → 預設 5 / 10
        File.WriteAllText(path, """{ "listenPort": 30001 }""");
        var cfg = ServiceConfig.LoadOrCreate(path);
        Assert.Equal(5, cfg.NoiseCapture.LookbackSeconds);
        Assert.Equal(10, cfg.NoiseCapture.CaptureSeconds);

        // null 容錯
        File.WriteAllText(path, """{ "noiseCapture": null }""");
        cfg = ServiceConfig.LoadOrCreate(path);
        Assert.Equal(5, cfg.NoiseCapture.LookbackSeconds);

        // 自訂值
        File.WriteAllText(path, """{ "noiseCapture": { "lookbackSeconds": 8, "captureSeconds": 20 } }""");
        cfg = ServiceConfig.LoadOrCreate(path);
        Assert.Equal((8, 20), (cfg.NoiseCapture.LookbackSeconds, cfg.NoiseCapture.CaptureSeconds));

        // 超出範圍（填錯）→ 回預設，不壞 UI
        File.WriteAllText(path, """{ "noiseCapture": { "lookbackSeconds": -1, "captureSeconds": 0 } }""");
        cfg = ServiceConfig.LoadOrCreate(path);
        Assert.Equal((5, 10), (cfg.NoiseCapture.LookbackSeconds, cfg.NoiseCapture.CaptureSeconds));
    }
}
