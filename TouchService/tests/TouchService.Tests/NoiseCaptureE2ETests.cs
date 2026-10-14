using System.Net;
using System.Text;
using System.Text.Json;
using SensorSimulator;
using TouchService.Config;
using TouchService.Core;
using TouchService.Logging;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// v9「捕捉噪訊 → 臨時鎖 → 觀察鎖後活動 → 轉正式」端到端（真實 socket、模擬器當噪訊晶片）。
///
/// 監控頁捕捉流程的前端選型是**純輪詢既有 API**（不加後端 session 端點），
/// 所以這裡驗證的就是前端賴以運作的 API 閉環：
///
///   GET /config 取 noiseCapture 窗參數
///   → baseline GET /history（回頭窗：lastTriggerAgoMs ≤ lookback 的格子入清單）
///   → 噪訊晶片發包 → 再 GET /history：cell 出現、total 累進（前向窗 delta 計數的根據）
///   → POST /locks/lock 臨時鎖 → 該格退出 /history 排行
///   → 鎖定中晶片繼續發包 → GET /locks 的 droppedSinceLock / lastDropAgoMs 反映鎖後活動
///   → POST /locks/promote 轉正式 → config.json lockedCells 落盤、saved=true
///   → 重啟（同 config）→ 該格以 config 來源載入、過濾照常生效。
///
/// 可重複執行：dotnet test --filter NoiseCaptureE2E
/// </summary>
public class NoiseCaptureE2ETests
{
    static (TouchServiceHost Host, FileLogger Log) StartHost(string dir, string configPath)
    {
        var cfg = ServiceConfig.LoadOrCreate(configPath);   // 重啟語意：每次都走真實 config 載入
        var log = new FileLogger(dir, LogLevel.Warn, console: false);
        var host = new TouchServiceHost(cfg, AppContext.BaseDirectory, log, configPath);
        host.Start();
        return (host, log);
    }

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

    static async Task<JsonDocument> GetJson(TouchServiceHost host, string path)
    {
        var resp = await Http.GetAsync($"http://127.0.0.1:{host.WsHub!.Port}{path}");
        Assert.True(resp.IsSuccessStatusCode, $"{path} → {(int)resp.StatusCode}");
        return JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
    }

    static async Task<string> Post(TouchServiceHost host, string path, string json = "")
    {
        var resp = await Http.PostAsync($"http://127.0.0.1:{host.WsHub!.Port}{path}",
            new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.True(resp.IsSuccessStatusCode, $"{path} → {(int)resp.StatusCode}");
        return await resp.Content.ReadAsStringAsync();
    }

    /// <summary>模擬噪訊晶片：固定 raw 點重送 holdMs（80ms 週期，與實機一致）。</summary>
    static void Stomp(int listenPort, int rawX, int rawY, int holdMs = 400)
    {
        var scenario = new Scenario
        {
            Name = "noise-chip",
            ResendIntervalMs = 80,
            Touches = new List<ScenarioTouch>
            {
                new() { Module = 0, RawX = rawX, RawY = rawY, DownMs = 0, UpMs = holdMs },
            },
        };
        using var runner = new ScenarioRunner(scenario, new IPEndPoint(IPAddress.Loopback, listenPort));
        runner.Run();
    }

    /// <summary>從 GET /history 的 cells 找指定格（找不到回 null）。</summary>
    static JsonElement? FindCell(JsonDocument history, int face, int col, int row)
    {
        foreach (var c in history.RootElement.GetProperty("cells").EnumerateArray())
        {
            if (c.GetProperty("face").GetInt32() == face &&
                c.GetProperty("cell")[0].GetInt32() == col &&
                c.GetProperty("cell")[1].GetInt32() == row)
                return c;
        }
        return null;
    }

    // Floor 面（face 2）中央附近的 raw 點 → cell 由服務路由決定（704 venue）
    const int NoisyRawX = 3264, NoisyRawY = 1664;

    [Fact]
    public async Task 清場掃描_間歇格只發一包也會立即臨時鎖定_觸摸互動停用時仍有效()
    {
        var dir = Directory.CreateTempSubdirectory("noise-capture-single-hit").FullName;
        var configPath = Path.Combine(dir, "config.json");
        File.WriteAllText(configPath, $$"""
            {
              "listenPort": 0,
              "venueProfile": "{{Path.Combine(AppContext.BaseDirectory, "venues", "704.json").Replace("\\", "/")}}",
              "interactionEnabled": false,
              "autoLock": { "enabled": false },
              "udpOutput": { "enabled": false },
              "webSocketOutput": { "enabled": true, "port": 0 },
              "log": { "console": false, "dir": "{{dir.Replace("\\", "/")}}", "level": "Warn" }
            }
            """);

        var (host, log) = StartHost(dir, configPath);
        using (log)
        using (host)
        {
            var body = await Post(host, "/noise-capture/start", """{ "mode": "new" }""");
            Assert.Contains("\"ok\":true", body);

            var raw = new RawPoint(NoisyRawX, NoisyRawY);
            var router = new SensorFaceRouter(host.Venue);
            Assert.True(router.TryRoute(raw.X, raw.Y, out var routed));

            // 單次孤立有效訊號：不夠舊版 autoLock 門檻，但清場掃描仍即時鎖定。
            host.Pipeline.OnDatagram(SensorPacketParser.Build(0, new[] { raw }), 100);

            Assert.True(host.Locks.IsLocked(routed.FaceIndex, routed.Col, routed.Row));
            Assert.Equal(0, host.Pipeline.ActiveTouches);
            var snapshot = Assert.IsType<TouchService.Diagnostics.NoiseCaptureSnapshot>(host.NoiseCapture.Snapshot());
            var cell = Assert.Single(snapshot.Cells);
            Assert.Equal((routed.FaceIndex, routed.Col, routed.Row), (cell.Face, cell.Col, cell.Row));
            Assert.Equal(1, cell.SignalCount);
            Assert.True(cell.LockedByDraft);
            Assert.Equal("running", snapshot.State);
        }
    }

    [Fact]
    public async Task 捕捉到臨時鎖到轉正式_全流程API閉環()
    {
        var dir = Directory.CreateTempSubdirectory("noise-capture-e2e").FullName;
        var configPath = Path.Combine(dir, "config.json");
        File.WriteAllText(configPath, $$"""
            {
              "listenPort": 0,
              "venueProfile": "{{Path.Combine(AppContext.BaseDirectory, "venues", "704.json").Replace("\\", "/")}}",
              "releaseTimeoutMs": 170,
              "autoLock": { "enabled": false },
              "udpOutput": { "enabled": false },
              "webSocketOutput": { "enabled": true, "port": 0 },
              "log": { "console": false, "dir": "{{dir.Replace("\\", "/")}}", "level": "Warn" }
            }
            """);

        int face, col, row;
        var (host1, log1) = StartHost(dir, configPath);
        using (log1)
        using (host1)
        {
            // ── 0. UI 讀窗參數：GET /config 帶 noiseCapture 預設（回頭 5s / 前向 10s）──
            using (var cfg = await GetJson(host1, "/config"))
            {
                var nc = cfg.RootElement.GetProperty("noiseCapture");
                Assert.Equal(5, nc.GetProperty("lookbackSeconds").GetInt32());
                Assert.Equal(10, nc.GetProperty("captureSeconds").GetInt32());
            }

            // ── 1. baseline：尚無觸發 ────────────────────────────────────────
            using (var baseline = await GetJson(host1, "/history?top=500"))
                Assert.Equal(0, baseline.RootElement.GetProperty("cells").GetArrayLength());

            // ── 2. 噪訊晶片發作（捕捉窗內）→ /history 出現、total 累進 ────────
            Stomp(host1.ActualListenPort, NoisyRawX, NoisyRawY, holdMs: 400);
            await Task.Delay(400);   // 等 releaseTimeout 收斂 up

            JsonElement hit;
            using (var during = await GetJson(host1, "/history?top=500"))
            {
                var cells = during.RootElement.GetProperty("cells");
                Assert.Equal(1, cells.GetArrayLength());
                hit = cells[0].Clone();
            }
            face = hit.GetProperty("face").GetInt32();
            col = hit.GetProperty("cell")[0].GetInt32();
            row = hit.GetProperty("cell")[1].GetInt32();
            Assert.Equal(2, face);   // Floor
            Assert.True(hit.GetProperty("total").GetInt64() >= 1);   // 前端 delta 計數的根據
            // 回頭窗判定的根據：剛觸發過的格 lastTriggerAgoMs 小
            Assert.True(hit.GetProperty("lastTriggerAgoMs").GetInt64() < 5000);

            // ── 3. 臨時鎖（= 監控頁清單按「臨時鎖定」）→ 退出排行 ─────────────
            Assert.Contains("\"ok\":true",
                await Post(host1, "/locks/lock", $$"""{ "face": {{face}}, "col": {{col}}, "row": {{row}} }"""));
            using (var after = await GetJson(host1, "/history?top=500"))
                Assert.Null(FindCell(after, face, col, row));

            // ── 4. 鎖定中晶片繼續發包 → 鎖後活動（鐵證噪訊）────────────────────
            Stomp(host1.ActualListenPort, NoisyRawX, NoisyRawY, holdMs: 300);
            using (var locks = await GetJson(host1, "/locks"))
            {
                var cell = Assert.Single(locks.RootElement.GetProperty("cells").EnumerateArray());
                Assert.Equal("manual", cell.GetProperty("source").GetString());
                Assert.False(cell.GetProperty("saved").GetBoolean());                 // 臨時（未保存）
                Assert.True(cell.GetProperty("droppedSinceLock").GetInt64() >= 3);    // 鎖後仍在發
                Assert.True(cell.GetProperty("lastDropAgoMs").GetInt64() < 5000);
            }

            // 觸控事件已被過濾：鎖定中發包不產生 touch
            Assert.Equal(0, host1.Pipeline.ActiveTouches);

            // ── 5. 轉正式並保存（單格 promote）→ config 落盤、標已保存 ─────────
            Assert.Contains("\"ok\":true",
                await Post(host1, "/locks/promote", $$"""{ "face": {{face}}, "col": {{col}}, "row": {{row}} }"""));
            using (var locks = await GetJson(host1, "/locks"))
            {
                var cell = Assert.Single(locks.RootElement.GetProperty("cells").EnumerateArray());
                Assert.True(cell.GetProperty("saved").GetBoolean());
                Assert.Equal("config", cell.GetProperty("source").GetString());
            }
        }   // ── 6. 關閉服務 ──────────────────────────────────────────────────

        // ── 7. 重啟（同 config）：轉正式的格以 config 來源載入、過濾生效 ──────
        var (host2, log2) = StartHost(dir, configPath);
        using (log2)
        using (host2)
        {
            Assert.True(host2.Locks.IsLocked(face, col, row));
            using (var locks = await GetJson(host2, "/locks"))
            {
                var cell = Assert.Single(locks.RootElement.GetProperty("cells").EnumerateArray());
                Assert.Equal("config", cell.GetProperty("source").GetString());
                Assert.True(cell.GetProperty("saved").GetBoolean());
            }

            // 噪訊再發作：被過濾、不產生觸控事件、鎖後活動繼續累計
            Stomp(host2.ActualListenPort, NoisyRawX, NoisyRawY, holdMs: 300);
            Assert.Equal(0, host2.Pipeline.ActiveTouches);
            using (var locks = await GetJson(host2, "/locks"))
            {
                var cell = Assert.Single(locks.RootElement.GetProperty("cells").EnumerateArray());
                Assert.True(cell.GetProperty("droppedSinceLock").GetInt64() >= 3);
            }

            // ── 8. 設定頁「解鎖全部」：runtime 清空、config 不動（重啟會鎖回）──
            Assert.Contains("\"ok\":true", await Post(host2, "/locks/unlockall"));
            Assert.Equal(0, host2.Locks.Count);
            using (var doc = JsonDocument.Parse(File.ReadAllText(configPath)))
                Assert.Equal(1, doc.RootElement.GetProperty("lockedCells").GetArrayLength());
        }
    }
}
