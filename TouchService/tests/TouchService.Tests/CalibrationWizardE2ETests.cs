using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SensorSimulator;
using TouchService.Config;
using TouchService.Logging;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// 校正精靈端到端（真實 socket、模擬器當感應硬體，無實機）：
///
///   calibration start → 模擬器重送「左上角」raw → 從 WS raw 串流取得穩定值（n ≥ 3）
///   → 模擬器發「右下角」→ 同樣取值 → POST /calibration/save → 關掉服務 →
///   以同一份 config.json 重啟（LoadOrCreate 走真實載入路徑）→ 模擬器再發兩角與中央
///   → 驗證映射 cell：左上 = (0,0)、右下 = (cols-1, rows-1)、中央 = 中央格。
///
/// 跑單面正向與雙反向（flip）兩個案例——新場域「踩兩角就建好環境」的完整閉環。
/// 可重複執行：dotnet test --filter CalibrationWizardE2E
/// </summary>
public class CalibrationWizardE2ETests
{
    // 單面牆：8×4 格 vertical（cellW 32 / cellH 16）→ 畫布 256×64
    const int Cols = 8, Rows = 4;

    sealed record Captured(int X, int Y);

    static (TouchServiceHost Host, FileLogger Log) StartHost(string dir, string configPath)
    {
        var cfg = ServiceConfig.LoadOrCreate(configPath);   // 重啟語意：每次都走真實 config 載入
        var log = new FileLogger(dir, LogLevel.Warn, console: false);
        var host = new TouchServiceHost(cfg, dir, log, configPath);
        host.Start();
        return (host, log);
    }

    static async Task<(ClientWebSocket Ws, ConcurrentQueue<string> Sink)> ConnectWs(TouchServiceHost host)
    {
        var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri($"ws://127.0.0.1:{host.WsHub!.Port}/"), CancellationToken.None);
        var sink = new ConcurrentQueue<string>();
        _ = Task.Run(async () =>
        {
            var buf = new byte[1 << 20];
            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    int off = 0;
                    WebSocketReceiveResult r;
                    do
                    {
                        r = await ws.ReceiveAsync(new ArraySegment<byte>(buf, off, buf.Length - off),
                                                  CancellationToken.None);
                        off += r.Count;
                    } while (!r.EndOfMessage);
                    if (r.MessageType == WebSocketMessageType.Close) return;
                    sink.Enqueue(Encoding.UTF8.GetString(buf, 0, off));
                }
            }
            catch { /* host dispose 斷線 */ }
        });
        return (ws, sink);
    }

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

    static async Task<string> Post(TouchServiceHost host, string path, string json = "")
    {
        var resp = await Http.PostAsync($"http://127.0.0.1:{host.WsHub!.Port}{path}",
            new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.True(resp.IsSuccessStatusCode, $"{path} → {(int)resp.StatusCode}");
        return await resp.Content.ReadAsStringAsync();
    }

    /// <summary>模擬器：一隻腳踩在 (rawX, rawY) 上 holdMs（80ms 重送，與實機一致）。</summary>
    static void Stomp(int listenPort, int rawX, int rawY, int holdMs = 400)
    {
        var scenario = new Scenario
        {
            Name = "wizard-corner",
            ResendIntervalMs = 80,
            Touches = new List<ScenarioTouch>
            {
                new() { Module = 0, RawX = rawX, RawY = rawY, DownMs = 0, UpMs = holdMs },
            },
        };
        using var runner = new ScenarioRunner(scenario, new IPEndPoint(IPAddress.Loopback, listenPort));
        runner.Run();
    }

    /// <summary>精靈取值：等 raw 串流出現「同值連續 ≥ 3（n ≥ 3）」的穩定座標。</summary>
    static async Task<Captured> CaptureStable(ConcurrentQueue<string> sink, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            while (sink.TryDequeue(out var msg))
            {
                using var doc = JsonDocument.Parse(msg);
                var root = doc.RootElement;
                if (root.TryGetProperty("type", out var t) && t.GetString() == "raw" &&
                    root.GetProperty("n").GetInt32() >= 3)
                    return new Captured(root.GetProperty("x").GetInt32(), root.GetProperty("y").GetInt32());
            }
            await Task.Delay(20);
        }
        throw new TimeoutException("等不到穩定 raw 值（n ≥ 3）");
    }

    /// <summary>送一點並從 frame 串流讀回 down 事件的 (face, col, row)；等 up 收斂再返回。</summary>
    static async Task<(int Face, int Col, int Row)> StompAndReadCell(
        TouchServiceHost host, ConcurrentQueue<string> sink, int rawX, int rawY)
    {
        while (sink.TryDequeue(out _)) { }   // 清掉舊訊息
        Stomp(host.ActualListenPort, rawX, rawY, holdMs: 200);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        (int, int, int)? hit = null;
        while (DateTime.UtcNow < deadline && hit == null)
        {
            while (sink.TryDequeue(out var msg))
            {
                using var doc = JsonDocument.Parse(msg);
                var root = doc.RootElement;
                if (root.TryGetProperty("type", out _)) continue;   // hello / locks / raw / log
                foreach (var e in root.GetProperty("events").EnumerateArray())
                {
                    if (e.GetProperty("phase").GetString() != "down") continue;
                    var cell = e.GetProperty("cell");
                    hit = (e.GetProperty("face").GetInt32(),
                           cell[0].GetInt32(), cell[1].GetInt32());
                }
            }
            if (hit == null) await Task.Delay(20);
        }
        Assert.True(hit != null, $"raw ({rawX},{rawY}) 沒有產生 down 事件（沒路由到新場域？）");

        await Task.Delay(170 + 150);   // releaseTimeout：等 up 收斂，下一點是乾淨的新 touch
        return hit!.Value;
    }

    [Theory]
    [InlineData("e2e-wall", 5000, 300, 5448, 348)]   // 正向：左上 raw < 右下 raw（無 flip）
    [InlineData("e2e-flip", 5448, 348, 5000, 300)]   // 雙反向：左上 raw > 右下 raw（flipX+flipY）
    public async Task 精靈全流程_兩角校正_重啟_映射驗證(string venueName, int tlX, int tlY, int brX, int brY)
    {
        // ── 0. 新場域部署模擬：乾淨目錄 + config + 既有 704 venue（精靈跑之前的舊環境）──
        var dir = Directory.CreateTempSubdirectory("calib-wizard-e2e").FullName;
        var configPath = Path.Combine(dir, "config.json");
        File.WriteAllText(configPath, $$"""
            {
              "listenPort": 0,
              "venueProfile": "venues/704.json",
              "releaseTimeoutMs": 170,
              "autoLock": { "enabled": false },
              "udpOutput": { "enabled": false },
              "webSocketOutput": { "enabled": true, "port": 0 },
              "log": { "console": false, "dir": "{{dir.Replace("\\", "/")}}", "level": "Warn" }
            }
            """);
        Directory.CreateDirectory(Path.Combine(dir, "venues"));
        File.Copy(Path.Combine(AppContext.BaseDirectory, "venues", "704.json"),
                  Path.Combine(dir, "venues", "704.json"));

        // ── 1. 啟動服務 + 開校正工作階段 ────────────────────────────────────
        Captured tl, br;
        var (host1, log1) = StartHost(dir, configPath);
        using (log1)
        using (host1)
        {
            var (ws, sink) = await ConnectWs(host1);
            using var _ = ws;
            await Post(host1, "/calibration/start");

            // ── 2. 模擬器踩「左上角第一顆晶片」（80ms 重送）→ 精靈取得穩定值 ──
            var stompTl = Task.Run(() => Stomp(host1.ActualListenPort, tlX, tlY));
            tl = await CaptureStable(sink);
            await stompTl;
            Assert.Equal((tlX, tlY), (tl.X, tl.Y));

            // ── 3. 踩「右下角最後一顆晶片」→ 取值 ────────────────────────────
            var stompBr = Task.Run(() => Stomp(host1.ActualListenPort, brX, brY));
            br = await CaptureStable(sink);
            await stompBr;
            Assert.Equal((brX, brY), (br.X, br.Y));

            // ── 4. save：兩角 → venue JSON + config venueProfile ─────────────
            var saveBody = JsonSerializer.Serialize(new
            {
                venue = venueName,
                faces = new[]
                {
                    new
                    {
                        name = "Wall", cols = Cols, rows = Rows, orientation = "vertical",
                        originX = 0, originY = 0,
                        topLeftRaw = new[] { tl.X, tl.Y },
                        bottomRightRaw = new[] { br.X, br.Y },
                    },
                },
            });
            var saveResp = await Post(host1, "/calibration/save", saveBody);
            Assert.Contains("\"ok\":true", saveResp);
        }   // ── 5. 關閉服務（精靈提示「需重啟生效」）───────────────────────────

        Assert.True(File.Exists(Path.Combine(dir, "venues", venueName + ".json")));

        // ── 6. 重啟：同一份 config.json（venueProfile 已指到新場域）──────────
        var (host2, log2) = StartHost(dir, configPath);
        using (log2)
        using (host2)
        {
            Assert.Equal(venueName, host2.Venue.Venue);
            Assert.Equal((256, 64), (host2.Venue.Canvas.Width, host2.Venue.Canvas.Height));

            var (ws, sink) = await ConnectWs(host2);
            using var _ = ws;

            // ── 7. 模擬器再踩兩角與中央：映射 cell 必須正確 ──────────────────
            Assert.Equal((0, 0, 0),
                await StompAndReadCell(host2, sink, tlX, tlY));               // 左上 → (0,0)
            Assert.Equal((0, Cols - 1, Rows - 1),
                await StompAndReadCell(host2, sink, brX, brY));               // 右下 → (7,3)
            Assert.Equal((0, Cols / 2, Rows / 2),
                await StompAndReadCell(host2, sink, 5224, 324));              // 中央 → (4,2)
        }
    }
}
