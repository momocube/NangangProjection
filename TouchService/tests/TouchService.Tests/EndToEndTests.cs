using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using SensorSimulator;
using TouchService.Config;
using TouchService.Core;
using TouchService.Logging;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// 端到端（Linux loopback、真實 socket）：
/// SensorSimulator scenario（60 點併發）→ TouchService 完整 host → UDP JSON client。
/// 斷言：60 點全部出現、down 延遲 p95 &lt; 5ms 且 max &lt; 25ms、無假 up、alive 清單正確。
/// timing 斷言前先送一次 warm-up 觸發（不計入）暖 JIT / 首包路徑——
/// 開發機 load 高時首 touch 冷啟實測 7~13ms，會把逐點 &lt;5ms 斷言打成 flake。
/// </summary>
public class EndToEndTests
{
    sealed record Frame(long RecvTs, JsonDocument Doc);

    [Fact]
    public void 六十點併發_全鏈路驗證()
    {
        var tmpDir = Directory.CreateTempSubdirectory("touchservice-e2e").FullName;

        // ── 1. raw UDP 輸出的 test client ────────────────────────────────────
        using var client = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int clientPort = ((IPEndPoint)client.Client.LocalEndPoint!).Port;

        var frames = new ConcurrentQueue<Frame>();
        var recvDone = new ManualResetEventSlim(false);
        var recvThread = new Thread(() =>
        {
            var remote = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                while (true)
                {
                    var data = client.Receive(ref remote);
                    frames.Enqueue(new Frame(Stopwatch.GetTimestamp(), JsonDocument.Parse(data)));
                }
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            finally { recvDone.Set(); }
        }) { IsBackground = true };
        recvThread.Start();

        // ── 2. TouchService 完整 host（真實 UDP listen / timer / publisher）──
        var config = new ServiceConfig
        {
            ListenPort = 0,   // OS 配發，避免測試環境 port 衝突
            VenueProfile = Path.Combine(AppContext.BaseDirectory, "venues", "704.json"),
            ReleaseTimeoutMs = 170,
            MatchRadiusPx = 96,
            FlushIntervalMs = 5,
            KeepaliveIntervalMs = 200,
            AutoLock = new AutoLockConfig { Enabled = false },   // E2E 驗證正常發布路徑，不走自檢窗
            UdpOutput = new UdpOutputConfig
            {
                Enabled = true,
                Endpoints = new List<string> { $"127.0.0.1:{clientPort}" },
            },
            WebSocketOutput = new WebSocketOutputConfig { Enabled = false },
            Log = new LogConfig { Console = false, Dir = tmpDir, Level = "Warn" },
        };

        using var log = new FileLogger(tmpDir, LogLevel.Warn, console: false);
        using var host = new TouchServiceHost(config, AppContext.BaseDirectory, log);
        host.Start();

        // ── 3a. warm-up 觸發（不計入斷言）：暖 JIT / 首包全路徑（parse→route→track→
        //        serialize→publish→client 收包解析）。首 touch 冷啟在高 load 開發機
        //        實測 7~13ms，會把 timing 斷言打成 flake——暖完、等 up 收斂後清空 frames
        //        再跑正式 scenario。
        using (var warmupSock = new UdpClient())
        {
            var warmupPkt = SensorPacketParser.Build(0, new[] { new RawPoint(3264, 1664) });
            warmupSock.Send(warmupPkt, warmupPkt.Length,
                new IPEndPoint(IPAddress.Loopback, host.ActualListenPort));
        }
        Thread.Sleep(170 + 300);   // down + releaseTimeout up 全部走完
        while (frames.TryDequeue(out var warmFrame)) warmFrame.Doc.Dispose();

        // ── 3b. simulator scenario：60 點併發（15 模組 × 4 點、80ms 重送、jitter ±10ms）──
        var scenario = Scenario.Load(Path.Combine(AppContext.BaseDirectory, "scenarios", "stress-60.json"));
        Assert.Equal(60, scenario.Touches.Count);

        List<SendRecord> records;
        using (var runner = new ScenarioRunner(
                   scenario, new IPEndPoint(IPAddress.Loopback, host.ActualListenPort)))
        {
            runner.Run();   // 阻塞 ~1.7s（最後 upMs = 1500）
            records = runner.Records;
        }

        // 等所有 up（releaseTimeout 170ms）+ 至少一個 trailing keepalive snapshot
        Thread.Sleep(170 + 400);
        client.Close();
        recvDone.Wait(2000);

        // 前置守門：scenario 任一 touch 的實際送包 gap 必須 < releaseTimeout，
        // 否則「無假 up」斷言無意義（場景參數錯誤——dropRate > 0 加上 jitter
        // 可能讓單次丟包 gap 超過 timeout，up + 重新 down 反而是服務的正確行為）。
        double tickMsPre = 1000.0 / Stopwatch.Frequency;
        foreach (var g in records.Where(r => !r.Dropped).GroupBy(r => r.TouchIndex))
        {
            var ts = g.Select(r => r.Timestamp).OrderBy(x => x).ToArray();
            for (int i = 1; i < ts.Length; i++)
            {
                double gapMs = (ts[i] - ts[i - 1]) * tickMsPre;
                Assert.True(gapMs < config.ReleaseTimeoutMs,
                    $"scenario 參數錯誤：touch {g.Key} 實際送包 gap {gapMs:F0}ms ≥ timeout {config.ReleaseTimeoutMs}ms，" +
                    "請調整 stress-60.json 的 dropRate / jitterMs");
            }
        }

        // ── 4. 斷言 ──────────────────────────────────────────────────────────
        var all = frames.ToArray();
        Assert.NotEmpty(all);

        double tickMs = 1000.0 / Stopwatch.Frequency;

        // 預期畫布座標：與服務同一套 router 算出 touchIndex → px 對照
        var router = new SensorFaceRouter(VenueProfile.Load(config.VenueProfile));
        var pxToTouch = new Dictionary<(int, int), int>();
        for (int i = 0; i < scenario.Touches.Count; i++)
        {
            Assert.True(router.TryRoute(scenario.Touches[i].RawX, scenario.Touches[i].RawY, out var rp),
                $"scenario touch {i} 的 raw 座標必須可路由");
            pxToTouch[(rp.PixelX, rp.PixelY)] = i;
        }

        var downsByTouch = new Dictionary<int, (int Id, long RecvTs)>();
        var upsById = new Dictionary<int, long>();
        long lastSeq = 0;
        int maxAlive = 0;
        int totalDowns = 0, totalUps = 0;

        foreach (var f in all)
        {
            var root = f.Doc.RootElement;

            long seq = root.GetProperty("seq").GetInt64();
            Assert.True(seq > lastSeq, "seq 必須嚴格遞增");
            lastSeq = seq;

            maxAlive = Math.Max(maxAlive, root.GetProperty("alive").GetArrayLength());

            foreach (var e in root.GetProperty("events").EnumerateArray())
            {
                int id = e.GetProperty("id").GetInt32();
                string phase = e.GetProperty("phase").GetString()!;
                var px = (e.GetProperty("px")[0].GetInt32(), e.GetProperty("px")[1].GetInt32());

                if (phase == "down")
                {
                    totalDowns++;
                    Assert.True(pxToTouch.TryGetValue(px, out int touchIdx),
                        $"down px={px} 對不到任何 scenario touch");
                    Assert.False(downsByTouch.ContainsKey(touchIdx),
                        $"touch {touchIdx} 出現第二次 down（假離手後重新取得）");
                    downsByTouch[touchIdx] = (id, f.RecvTs);
                }
                else if (phase == "up")
                {
                    totalUps++;
                    Assert.False(upsById.ContainsKey(id), $"id {id} 出現第二次 up");
                    upsById[id] = f.RecvTs;
                }
            }
        }

        // (a) 60 點全部出現、無假 up 造成的重複 down
        Assert.Equal(60, downsByTouch.Count);
        Assert.Equal(60, totalDowns);
        Assert.Equal(60, totalUps);

        // (b) down 延遲：p95 < 5ms 且 max < 25ms。
        //     warm-up 已暖 JIT——逐點 <5ms 在高 load 開發機仍會被排程毛刺打成 flake，
        //     p95 保住「軟體段近零延遲」的回歸抓取力，max 擋住整體劣化。
        var firstSend = records.Where(r => r.First && !r.Dropped)
                               .GroupBy(r => r.TouchIndex)
                               .ToDictionary(g => g.Key, g => g.Min(r => r.Timestamp));
        var downLatencies = new List<double>();
        foreach (var (touchIdx, (_, recvTs)) in downsByTouch)
            downLatencies.Add((recvTs - firstSend[touchIdx]) * tickMs);
        downLatencies.Sort();
        double p95 = downLatencies[(int)Math.Ceiling(downLatencies.Count * 0.95) - 1];
        double maxLatency = downLatencies[^1];
        Assert.True(p95 < 5.0,
            $"down 延遲 p95 {p95:F2}ms ≥ 5ms（min={downLatencies[0]:F2} " +
            $"p50={downLatencies[downLatencies.Count / 2]:F2} max={maxLatency:F2}）");
        Assert.True(maxLatency < 25.0, $"down 延遲 max {maxLatency:F2}ms ≥ 25ms");

        // (c) 無假 up：每個 touch 的 up 必須在「最後實送封包 + releaseTimeout」之後（容差 25ms）
        var lastSend = records.Where(r => !r.Dropped)
                              .GroupBy(r => r.TouchIndex)
                              .ToDictionary(g => g.Key, g => g.Max(r => r.Timestamp));
        foreach (var (touchIdx, (id, _)) in downsByTouch)
        {
            Assert.True(upsById.ContainsKey(id), $"touch {touchIdx}（id {id}）沒有收到 up");
            double upAfterLastSendMs = (upsById[id] - lastSend[touchIdx]) * tickMs;
            Assert.True(upAfterLastSendMs >= 170 - 25,
                $"touch {touchIdx} 假離手：最後封包後 {upAfterLastSendMs:F0}ms 就發了 up（timeout 170ms）");
        }

        // (d) alive 清單正確：高峰 60、結尾 snapshot 清空
        Assert.Equal(60, maxAlive);
        var lastFrame = all[^1].Doc.RootElement;
        Assert.Equal(0, lastFrame.GetProperty("alive").GetArrayLength());

        // 殘留鬼點防護：所有 up 之後的 frame，alive 不得再包含已 up 的 id
        bool seenAllUps = false;
        var uppedIds = new HashSet<int>();
        foreach (var f in all)
        {
            var root = f.Doc.RootElement;
            foreach (var e in root.GetProperty("events").EnumerateArray())
                if (e.GetProperty("phase").GetString() == "up")
                    uppedIds.Add(e.GetProperty("id").GetInt32());

            if (uppedIds.Count == 60) seenAllUps = true;
            if (seenAllUps)
                foreach (var aliveId in root.GetProperty("alive").EnumerateArray())
                    Assert.DoesNotContain(aliveId.GetInt32(), uppedIds);
        }
        Assert.True(seenAllUps);

        // 輸出延遲摘要（dotnet test -v 不顯示，但失敗時有用）
        Console.WriteLine(
            $"[E2E] down latency: min={downLatencies[0]:F3}ms " +
            $"p50={downLatencies[downLatencies.Count / 2]:F3}ms p95={p95:F3}ms max={maxLatency:F3}ms");
    }

    [Fact]
    public void Replay真實capture_事件流合理()
    {
        var tmpDir = Directory.CreateTempSubdirectory("touchservice-replay").FullName;

        using var client = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int clientPort = ((IPEndPoint)client.Client.LocalEndPoint!).Port;

        int downs = 0, ups = 0, moves = 0;
        var recvThread = new Thread(() =>
        {
            var remote = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                while (true)
                {
                    var data = client.Receive(ref remote);
                    using var doc = JsonDocument.Parse(data);
                    foreach (var e in doc.RootElement.GetProperty("events").EnumerateArray())
                    {
                        switch (e.GetProperty("phase").GetString())
                        {
                            case "down": Interlocked.Increment(ref downs); break;
                            case "up": Interlocked.Increment(ref ups); break;
                            case "move": Interlocked.Increment(ref moves); break;
                        }
                    }
                }
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
        }) { IsBackground = true };
        recvThread.Start();

        var config = new ServiceConfig
        {
            ListenPort = 0,
            VenueProfile = Path.Combine(AppContext.BaseDirectory, "venues", "704.json"),
            AutoLock = new AutoLockConfig { Enabled = false },   // E2E 驗證正常發布路徑，不走自檢窗
            UdpOutput = new UdpOutputConfig
            {
                Enabled = true,
                Endpoints = new List<string> { $"127.0.0.1:{clientPort}" },
            },
            WebSocketOutput = new WebSocketOutputConfig { Enabled = false },
            Log = new LogConfig { Console = false, Dir = tmpDir, Level = "Warn" },
        };
        using var log = new FileLogger(tmpDir, LogLevel.Warn, console: false);
        using var host = new TouchServiceHost(config, AppContext.BaseDirectory, log);
        host.Start();

        // 多點 capture（15.5s）加速 8 倍重放（gap 同步縮短，touch 邏輯只驗事件守恆）
        var packets = WiresharkExportReader.ReadFile(
            Path.Combine(AppContext.BaseDirectory, "fixtures", "capture-multitouch-floor.txt"));
        using (var replay = new ReplayRunner(
                   packets, new IPEndPoint(IPAddress.Loopback, host.ActualListenPort), speed: 8.0))
        {
            replay.Run();
            Assert.Equal(335, replay.SentCount);
        }

        Thread.Sleep(170 + 300);
        client.Close();

        // 守恆：所有 down 最終都有 up；至少出現一次 down（真實腳踩資料）
        Assert.True(downs > 0, "重放真實 capture 應該至少產生一個 down");
        Assert.Equal(downs, ups);
        Console.WriteLine($"[E2E replay] downs={downs} moves={moves} ups={ups}");
    }
}
