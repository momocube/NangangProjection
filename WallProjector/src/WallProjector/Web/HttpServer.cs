using System.Net;
using System.Net.Sockets;
using System.Text;
using WallProjector.Core;

namespace WallProjector.Web;

/// <summary>
/// 自寫最小 HTTP server（thread-per-connection、Connection: close）。
/// 寫法承襲 TouchService WebSocketHub 的 HTTP 部分（同 repo 同哲學：
/// 免 netsh urlacl、零依賴、行為自控），但這裡**只需要 HTTP**——沒有 WS upgrade。
/// 場館內網用，不對公網開放；寫入類端點由 handler 以 IsLoopback 限制。
/// </summary>
public sealed class HttpServer : IDisposable
{
    public sealed record Request(string Method, string Path, string Query, string Body, bool IsLoopback)
    {
        /// <summary>request headers（key 不分大小寫；/media raw 的 Range 等用）。</summary>
        public IReadOnlyDictionary<string, string> Headers { get; init; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string? Header(string name) =>
            Headers.TryGetValue(name, out var v) ? v : null;
    }

    public sealed record Response(string Status, string ContentType, byte[] Body)
    {
        /// <summary>非 null = body 改從檔案串流（影片等大檔不進記憶體）：路徑 + 起點 + 長度。</summary>
        public (string Path, long Offset, long Length)? FileBody { get; init; }

        /// <summary>額外 response headers（Accept-Ranges / Content-Range 等）。</summary>
        public IReadOnlyList<(string Name, string Value)> ExtraHeaders { get; init; } =
            Array.Empty<(string, string)>();

        public static Response Json(string status, string json) =>
            new(status, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(json));
        public static Response Html(string status, string html) =>
            new(status, "text/html; charset=utf-8", Encoding.UTF8.GetBytes(html));
        public static Response Text(string status, string text) =>
            new(status, "text/plain; charset=utf-8", Encoding.UTF8.GetBytes(text));
        public static Response NotFound() => Text("404 Not Found", "not found");

        public static Response File(string status, string contentType, string path,
                                    long offset, long length,
                                    params (string Name, string Value)[] extraHeaders) =>
            new(status, contentType, Array.Empty<byte>())
            { FileBody = (path, offset, length), ExtraHeaders = extraHeaders };
    }

    readonly TcpListener _listener;
    readonly Func<Request, Response> _handler;
    readonly Logger _log;
    readonly CancellationTokenSource _cts = new();
    Thread? _acceptThread;

    /// <summary>實際綁定的 port（建構傳 0 時由 OS 配發——測試用）。</summary>
    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public HttpServer(int port, Func<Request, Response> handler, Logger log)
    {
        _handler = handler;
        _log = log;
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public void Start()
    {
        _listener.Start();
        _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "HttpAccept" };
        _acceptThread.Start();
        _log.Info($"[Http] 監聽 port {Port}（管理頁 http://127.0.0.1:{Port}/）");
    }

    void AcceptLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient tcp;
            try { tcp = _listener.AcceptTcpClient(); }
            catch (SocketException) { break; }
            catch (ObjectDisposedException) { break; }

            var t = new Thread(() => HandleClient(tcp)) { IsBackground = true, Name = "HttpClient" };
            t.Start();
        }
    }

    void HandleClient(TcpClient tcp)
    {
        try
        {
            tcp.NoDelay = true;
            var stream = tcp.GetStream();
            stream.ReadTimeout = 5000;

            var request = ReadHttpRequest(stream);
            if (request == null) { tcp.Close(); return; }

            var (method, rawPath, body, headers) = request.Value;
            string path = StripQuery(rawPath);
            string query = QueryOf(rawPath);

            Response resp;
            if (method == "OPTIONS")
            {
                resp = new Response("204 No Content", "text/plain; charset=utf-8", Array.Empty<byte>());
            }
            else
            {
                try
                {
                    resp = _handler(new Request(method, path, query, body, IsLoopback(tcp)) { Headers = headers });
                }
                catch (Exception ex)
                {
                    _log.Error($"[Http] handler 例外（{method} {path}）：{ex}");
                    resp = Response.Text("500 Internal Server Error", "internal error");
                }
            }

            WriteHttpResponse(stream, resp);
            tcp.Close();
        }
        catch (Exception ex)
        {
            _log.Debug($"[Http] client 處理失敗：{ex.Message}");
            try { tcp.Close(); } catch { /* ignore */ }
        }
    }

    static string StripQuery(string path)
    {
        int q = path.IndexOf('?');
        return q < 0 ? path : path[..q];
    }

    static string QueryOf(string path)
    {
        int q = path.IndexOf('?');
        return q < 0 ? "" : path[(q + 1)..];
    }

    /// <summary>連線來源是否 loopback（寫入類端點的來源限制依據；同 TouchService 慣例）。</summary>
    static bool IsLoopback(TcpClient tcp)
    {
        if (tcp.Client.RemoteEndPoint is not IPEndPoint ep) return false;
        var addr = ep.Address;
        if (addr.IsIPv4MappedToIPv6) addr = addr.MapToIPv4();
        return IPAddress.IsLoopback(addr);
    }

    static (string Method, string Path, string Body, Dictionary<string, string> Headers)?
        ReadHttpRequest(NetworkStream stream)
    {
        var buf = new byte[64 * 1024];   // headers + body 上限 64KB（slot patch 遠小於此）
        int len = 0;
        int headerEnd = -1;
        while (len < buf.Length)
        {
            int n = stream.Read(buf, len, buf.Length - len);
            if (n <= 0) return null;
            len += n;
            headerEnd = IndexOfCrlfCrlf(buf, len);
            if (headerEnd >= 0) break;
        }
        if (headerEnd < 0) return null;

        var text = Encoding.ASCII.GetString(buf, 0, headerEnd);
        var lines = text.Split("\r\n");
        var reqParts = lines[0].Split(' ');
        if (reqParts.Length < 3) return null;

        int contentLength = 0;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < lines.Length; i++)
        {
            int idx = lines[i].IndexOf(':');
            if (idx <= 0) continue;
            string name = lines[i][..idx].Trim();
            string value = lines[i][(idx + 1)..].Trim();
            headers[name] = value;   // 重複 header 取最後一個（本工具用到的都單值）
            if (name.Equals("content-length", StringComparison.OrdinalIgnoreCase) &&
                (!int.TryParse(value, out contentLength) ||
                 contentLength < 0 || headerEnd + 4 + contentLength > buf.Length))
                return null;
        }

        int bodyStart = headerEnd + 4;
        while (len < bodyStart + contentLength)
        {
            int n = stream.Read(buf, len, buf.Length - len);
            if (n <= 0) return null;
            len += n;
        }
        var body = contentLength > 0 ? Encoding.UTF8.GetString(buf, bodyStart, contentLength) : "";
        return (reqParts[0], reqParts[1], body, headers);
    }

    static int IndexOfCrlfCrlf(byte[] buf, int len)
    {
        for (int i = 0; i + 3 < len; i++)
            if (buf[i] == '\r' && buf[i + 1] == '\n' && buf[i + 2] == '\r' && buf[i + 3] == '\n')
                return i;
        return -1;
    }

    static void WriteHttpResponse(NetworkStream stream, Response resp)
    {
        long contentLength = resp.FileBody?.Length ?? resp.Body.Length;
        var sb = new StringBuilder()
            .Append($"HTTP/1.1 {resp.Status}\r\n")
            .Append($"Content-Type: {resp.ContentType}\r\n")
            .Append($"Content-Length: {contentLength}\r\n");
        foreach (var (name, value) in resp.ExtraHeaders)
            sb.Append($"{name}: {value}\r\n");
        sb.Append("Access-Control-Allow-Origin: *\r\n" +
                  "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
                  "Access-Control-Allow-Headers: Content-Type, Range\r\n" +
                  "Connection: close\r\n\r\n");
        stream.Write(Encoding.ASCII.GetBytes(sb.ToString()));

        if (resp.FileBody is { } fb)
        {
            // 大檔（影片）分塊串流，不整檔進記憶體；client 斷線（seek 換 range）丟例外由外層收掉
            using var fs = new FileStream(fb.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            fs.Seek(fb.Offset, SeekOrigin.Begin);
            var chunk = new byte[64 * 1024];
            long remaining = fb.Length;
            while (remaining > 0)
            {
                int n = fs.Read(chunk, 0, (int)Math.Min(chunk.Length, remaining));
                if (n <= 0) break;
                stream.Write(chunk, 0, n);
                remaining -= n;
            }
        }
        else
        {
            stream.Write(resp.Body);
        }
        stream.Flush();
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _listener.Stop(); } catch { /* ignore */ }
        _acceptThread?.Join(500);
    }
}
