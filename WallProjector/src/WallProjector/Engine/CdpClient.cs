using System.Net.WebSockets;
using System.Text.Json;
using WallProjector.Core;

namespace WallProjector.Engine;

/// <summary>
/// 極簡 Chrome DevTools Protocol client（.NET 內建 ClientWebSocket，零依賴）。
///
/// 連線流程：
/// 1. 輪詢 `http://127.0.0.1:&lt;port&gt;/json`（瀏覽器開 debug port 有延遲）直到列出
///    type=="page" 的 target——瀏覽器以 `--remote-debugging-port` 啟動時才有
/// 2. WS 連上該 target 的 webSocketDebuggerUrl
/// 3. SendAsync = JSON-RPC：送 {id, method, params}，讀 frame 直到等到同 id 回應
///    （中途的 CDP event / 其他 id 一律跳過——本 client 單執行緒序列使用）
///
/// 失敗不靜默：超時/連不上 → TimeoutException、CDP 回 error → InvalidOperationException，
/// 由呼叫端（ProjectionEngine）轉成槽的明確錯誤狀態。
/// </summary>
public sealed class CdpClient : IAsyncDisposable
{
    readonly ClientWebSocket _ws;
    int _nextId;

    CdpClient(ClientWebSocket ws) => _ws = ws;

    /// <summary>
    /// 輪詢 /json 找 page target 並建立 WS 連線。port 沒起來 / timeout 內沒有
    /// page target → TimeoutException（帶最後一次錯誤訊息）。
    /// </summary>
    public static async Task<CdpClient> ConnectAsync(int port, TimeSpan timeout,
                                                     CancellationToken ct = default)
    {
        var wsUrl = await FindPageTargetAsync(port, timeout, ct);
        var ws = new ClientWebSocket();
        try
        {
            await ws.ConnectAsync(new Uri(wsUrl), ct);
        }
        catch
        {
            ws.Dispose();
            throw;
        }
        return new CdpClient(ws);
    }

    /// <summary>輪詢 http://127.0.0.1:port/json 直到出現 page target，回 webSocketDebuggerUrl。</summary>
    public static async Task<string> FindPageTargetAsync(int port, TimeSpan timeout,
                                                         CancellationToken ct = default)
    {
        long deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        string? lastError = null;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var json = await http.GetStringAsync($"http://127.0.0.1:{port}/json", ct);
                using var doc = JsonDocument.Parse(json);
                foreach (var t in doc.RootElement.EnumerateArray())
                {
                    if (t.TryGetProperty("type", out var type) && type.GetString() == "page" &&
                        t.TryGetProperty("webSocketDebuggerUrl", out var wsu) &&
                        wsu.GetString() is { Length: > 0 } url)
                        return url;
                }
                lastError = "/json 已可達但還沒有 page target";
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                lastError = "/json 請求逾時";
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }

            if (Environment.TickCount64 >= deadline)
                throw new TimeoutException(
                    $"CDP port {port} 在 {timeout.TotalSeconds:0}s 內沒有可用的 page target" +
                    (lastError != null ? $"（最後錯誤：{lastError}）" : ""));
            await Task.Delay(250, ct);
        }
    }

    /// <summary>送一個 CDP method 並等回應；CDP 回 error → InvalidOperationException。</summary>
    public async Task<JsonElement> SendAsync(string method, object? @params = null,
                                             CancellationToken ct = default)
    {
        int id = Interlocked.Increment(ref _nextId);
        var payload = JsonSerializer.SerializeToUtf8Bytes(new { id, method, @params = @params ?? new { } });
        await _ws.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, ct);

        var buf = new byte[64 * 1024];
        using var ms = new MemoryStream();
        while (true)
        {
            ms.SetLength(0);
            WebSocketReceiveResult r;
            do
            {
                r = await _ws.ReceiveAsync(buf, ct);
                if (r.MessageType == WebSocketMessageType.Close)
                    throw new InvalidOperationException($"CDP 連線被關閉（{method} 等回應中）");
                ms.Write(buf, 0, r.Count);
            } while (!r.EndOfMessage);

            using var doc = JsonDocument.Parse(ms.ToArray());
            var root = doc.RootElement;
            if (!root.TryGetProperty("id", out var idEl) || idEl.GetInt32() != id)
                continue;   // CDP event 或其他 id：跳過

            if (root.TryGetProperty("error", out var err))
                throw new InvalidOperationException($"CDP {method} 失敗：{err.GetRawText()}");
            return root.TryGetProperty("result", out var result)
                ? result.Clone()
                : default;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_ws.State == WebSocketState.Open)
            {
                using var cts = new CancellationTokenSource(1000);
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cts.Token);
            }
        }
        catch { /* 關閉失敗無所謂 */ }
        _ws.Dispose();
    }
}
