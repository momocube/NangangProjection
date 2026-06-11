using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using TouchService.Logging;

namespace TouchService.Output;

/// <summary>
/// 最小 WebSocket server（RFC 6455）+ HTTP GET /venue 端點，raw TCP 自寫實作。
///
/// 為什麼不用 HttpListener.AcceptWebSocketAsync：
/// 已在 Linux .NET 8 實測（2026-06-11）——AcceptWebSocketAsync 其實「可用」。
/// 仍選自寫 raw TCP 版的理由：
///   1. 部署目標是 Windows：HttpListener 走 http.sys，非 localhost prefix 需要
///      管理員 netsh http add urlacl 註冊，場館部署多一道易踩的坑
///   2. 一個 TCP port 同時服務 WS + GET /venue + testpage，無平台差異
///   3. 行為完全自控（~250 LOC），不依賴 http.sys / managed HttpListener 的實作差異
///
/// 支援範圍（夠用就好）：
///   - handshake：Sec-WebSocket-Key + SHA1 + base64 → Sec-WebSocket-Accept
///   - server→client：未分片 text frame（廣播 JSON）
///   - client→server：close（回 close 後斷線）、ping（回 pong）、text/binary（讀掉丟棄）
///   - HTTP GET /venue → 200 JSON（含 CORS header，給 file:// 開啟的 testpage 用）
///   - HTTP GET / 或 /index.html → 回 testpage（檔案存在時）
/// 不支援：分片訊息、permessage-deflate、TLS。場館內網用，不對公網開放。
/// </summary>
public sealed class WebSocketHub : ITouchSink
{
    const string WsMagicGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    /// <summary>本機管理 API 的請求（HandleClient → apiHandler）。IsLoopback 供寫入類端點做來源限制。</summary>
    public readonly record struct ApiRequest(string Method, string Path, string Body, bool IsLoopback);

    /// <summary>apiHandler 的回應；回 null = 此 path 不歸 API 管（落到 404）。</summary>
    public sealed record ApiResponse(string Status, string ContentType, byte[] Body)
    {
        public static ApiResponse Json(string status, string json) =>
            new(status, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(json));
    }

    sealed class Client
    {
        public required TcpClient Tcp;
        public required NetworkStream Stream;
        public readonly object SendGate = new();
        public volatile bool Dead;
    }

    readonly TcpListener _listener;
    readonly FileLogger _log;
    readonly byte[] _venueJson;
    readonly Func<byte[]> _helloProvider;
    readonly string? _testPagePath;
    readonly Func<ApiRequest, ApiResponse?>? _apiHandler;
    readonly List<Client> _clients = new();
    readonly object _clientsGate = new();
    readonly CancellationTokenSource _cts = new();
    Thread? _acceptThread;

    /// <summary>
    /// helloProvider：每個 client 連上時呼叫一次，產生當下的 hello（鎖定清單 / 自檢狀態是動態的）。
    /// apiHandler：本機管理 HTTP API（/config、/monitors、/locks/*；ConfigApi），null = 停用。
    /// </summary>
    public WebSocketHub(int port, byte[] venueJson, Func<byte[]> helloProvider, string? testPagePath,
                        FileLogger log, Func<ApiRequest, ApiResponse?>? apiHandler = null)
    {
        _log = log;
        _venueJson = venueJson;
        _helloProvider = helloProvider;
        _testPagePath = testPagePath;
        _apiHandler = apiHandler;
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public int ClientCount
    {
        get { lock (_clientsGate) return _clients.Count; }
    }

    public void Start()
    {
        _listener.Start();
        _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "WsAccept" };
        _acceptThread.Start();
        _log.Info($"[WebSocketHub] 監聽 port {Port}（ws://0.0.0.0:{Port}/ + GET /venue）");
    }

    void AcceptLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient tcp;
            try { tcp = _listener.AcceptTcpClient(); }
            catch (SocketException) { break; }
            catch (ObjectDisposedException) { break; }

            var t = new Thread(() => HandleClient(tcp)) { IsBackground = true, Name = "WsClient" };
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

            var (method, path, headers, body) = request.Value;

            if (headers.TryGetValue("upgrade", out var upgrade) &&
                upgrade.Contains("websocket", StringComparison.OrdinalIgnoreCase) &&
                headers.TryGetValue("sec-websocket-key", out var wsKey))
            {
                DoWebSocket(tcp, stream, wsKey);
                return;
            }

            if (method == "OPTIONS")
            {
                // CORS preflight（file:// 開啟 testpage 對 API 發 POST 時瀏覽器會先打這個）
                WriteHttpResponse(stream, "204 No Content", "text/plain; charset=utf-8", Array.Empty<byte>());
            }
            else if (method == "GET" && (path == "/venue" || path.StartsWith("/venue?")))
            {
                WriteHttpResponse(stream, "200 OK", "application/json; charset=utf-8", _venueJson);
            }
            else if (method == "GET" && (path == "/" || path == "/index.html") &&
                     _testPagePath != null && File.Exists(_testPagePath))
            {
                WriteHttpResponse(stream, "200 OK", "text/html; charset=utf-8", File.ReadAllBytes(_testPagePath));
            }
            else if (_apiHandler != null &&
                     _apiHandler(new ApiRequest(method, StripQuery(path), body, IsLoopback(tcp))) is { } apiResp)
            {
                WriteHttpResponse(stream, apiResp.Status, apiResp.ContentType, apiResp.Body);
            }
            else
            {
                WriteHttpResponse(stream, "404 Not Found", "text/plain; charset=utf-8",
                    Encoding.UTF8.GetBytes("not found"));
            }
            tcp.Close();
        }
        catch (Exception ex)
        {
            _log.Debug($"[WebSocketHub] client 處理失敗：{ex.Message}");
            try { tcp.Close(); } catch { /* ignore */ }
        }
    }

    // ── HTTP ────────────────────────────────────────────────────────────────

    static string StripQuery(string path)
    {
        int q = path.IndexOf('?');
        return q < 0 ? path : path[..q];
    }

    /// <summary>連線來源是否 loopback（寫入類 API / monitors 的來源限制依據）。</summary>
    static bool IsLoopback(TcpClient tcp)
    {
        if (tcp.Client.RemoteEndPoint is not IPEndPoint ep) return false;
        var addr = ep.Address;
        if (addr.IsIPv4MappedToIPv6) addr = addr.MapToIPv4();
        return IPAddress.IsLoopback(addr);
    }

    static (string Method, string Path, Dictionary<string, string> Headers, string Body)? ReadHttpRequest(
        NetworkStream stream)
    {
        var buf = new byte[64 * 1024];   // headers + body 上限 64KB（config patch 遠小於此）
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
        if (headerEnd < 0) return null;   // headers 超過 buffer：拒收

        var text = Encoding.ASCII.GetString(buf, 0, headerEnd);
        var lines = text.Split("\r\n");
        var reqParts = lines[0].Split(' ');
        if (reqParts.Length < 3) return null;

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < lines.Length; i++)
        {
            int idx = lines[i].IndexOf(':');
            if (idx <= 0) continue;
            headers[lines[i][..idx].Trim().ToLowerInvariant()] = lines[i][(idx + 1)..].Trim();
        }

        // body（Content-Length；WS upgrade / GET 沒有 body）
        int bodyStart = headerEnd + 4;
        int contentLength = 0;
        if (headers.TryGetValue("content-length", out var clStr) &&
            (!int.TryParse(clStr, out contentLength) || contentLength < 0 ||
             bodyStart + contentLength > buf.Length))
            return null;   // 格式錯 / 超過上限：拒收

        while (len < bodyStart + contentLength)
        {
            int n = stream.Read(buf, len, buf.Length - len);
            if (n <= 0) return null;
            len += n;
        }
        var body = contentLength > 0 ? Encoding.UTF8.GetString(buf, bodyStart, contentLength) : "";

        return (reqParts[0], reqParts[1], headers, body);
    }

    static int IndexOfCrlfCrlf(byte[] buf, int len)
    {
        for (int i = 0; i + 3 < len; i++)
            if (buf[i] == '\r' && buf[i + 1] == '\n' && buf[i + 2] == '\r' && buf[i + 3] == '\n')
                return i;
        return -1;
    }

    static void WriteHttpResponse(NetworkStream stream, string status, string contentType, byte[] body)
    {
        var header = $"HTTP/1.1 {status}\r\n" +
                     $"Content-Type: {contentType}\r\n" +
                     $"Content-Length: {body.Length}\r\n" +
                     "Access-Control-Allow-Origin: *\r\n" +
                     "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
                     "Access-Control-Allow-Headers: Content-Type\r\n" +
                     "Connection: close\r\n\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(header);
        stream.Write(headerBytes);
        stream.Write(body);
        stream.Flush();
    }

    // ── WebSocket ───────────────────────────────────────────────────────────

    void DoWebSocket(TcpClient tcp, NetworkStream stream, string wsKey)
    {
        // handshake：SHA1(key + magic GUID) → base64
        var acceptKey = Convert.ToBase64String(
            SHA1.HashData(Encoding.ASCII.GetBytes(wsKey + WsMagicGuid)));
        var response = "HTTP/1.1 101 Switching Protocols\r\n" +
                       "Upgrade: websocket\r\n" +
                       "Connection: Upgrade\r\n" +
                       $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n";
        stream.Write(Encoding.ASCII.GetBytes(response));
        stream.Flush();
        stream.ReadTimeout = Timeout.Infinite;

        var client = new Client { Tcp = tcp, Stream = stream };
        lock (_clientsGate) _clients.Add(client);
        _log.Info($"[WebSocketHub] client 連入（{tcp.Client.RemoteEndPoint}），目前 {ClientCount} 個");

        // hello：venue 輪廓 + 當下鎖定清單 / 自檢狀態下發（testpage 啟動繪製用）
        SendFrame(client, 0x1, _helloProvider());

        try { ClientReadLoop(client); }
        catch { /* 斷線 */ }
        finally { DropClient(client); }
    }

    void ClientReadLoop(Client client)
    {
        var stream = client.Stream;
        var header = new byte[8];

        while (!_cts.IsCancellationRequested && !client.Dead)
        {
            if (!ReadExact(stream, header, 2)) return;

            int opcode = header[0] & 0x0F;
            bool masked = (header[1] & 0x80) != 0;
            long payloadLen = header[1] & 0x7F;

            if (payloadLen == 126)
            {
                if (!ReadExact(stream, header, 2)) return;
                payloadLen = (header[0] << 8) | header[1];
            }
            else if (payloadLen == 127)
            {
                if (!ReadExact(stream, header, 8)) return;
                payloadLen = 0;
                for (int i = 0; i < 8; i++) payloadLen = (payloadLen << 8) | header[i];
            }

            if (payloadLen > 1 << 20) return;   // 防護：client 不該送 >1MB

            var maskKey = new byte[4];
            if (masked && !ReadExact(stream, maskKey, 4)) return;

            var payload = new byte[payloadLen];
            if (payloadLen > 0 && !ReadExact(stream, payload, (int)payloadLen)) return;
            if (masked)
                for (int i = 0; i < payload.Length; i++)
                    payload[i] ^= maskKey[i & 3];

            switch (opcode)
            {
                case 0x8:   // close：回 close 後斷線
                    SendFrame(client, 0x8, ReadOnlySpan<byte>.Empty);
                    return;
                case 0x9:   // ping → pong（echo payload）
                    SendFrame(client, 0xA, payload);
                    break;
                default:    // text / binary / continuation / pong：讀掉丟棄
                    break;
            }
        }
    }

    static bool ReadExact(NetworkStream stream, byte[] buf, int count)
    {
        int off = 0;
        while (off < count)
        {
            int n = stream.Read(buf, off, count - off);
            if (n <= 0) return false;
            off += n;
        }
        return true;
    }

    void SendFrame(Client client, byte opcode, ReadOnlySpan<byte> payload)
    {
        if (client.Dead) return;

        Span<byte> header = stackalloc byte[10];
        int headerLen;
        header[0] = (byte)(0x80 | opcode);   // FIN + opcode（server→client 不遮罩）
        if (payload.Length < 126)
        {
            header[1] = (byte)payload.Length;
            headerLen = 2;
        }
        else if (payload.Length < 65536)
        {
            header[1] = 126;
            header[2] = (byte)(payload.Length >> 8);
            header[3] = (byte)(payload.Length & 0xFF);
            headerLen = 4;
        }
        else
        {
            header[1] = 127;
            long len = payload.Length;
            for (int i = 0; i < 8; i++) header[2 + i] = (byte)(len >> (8 * (7 - i)));
            headerLen = 10;
        }

        try
        {
            lock (client.SendGate)
            {
                client.Stream.Write(header[..headerLen]);
                if (payload.Length > 0) client.Stream.Write(payload);
                client.Stream.Flush();
            }
        }
        catch
        {
            client.Dead = true;
        }
    }

    void DropClient(Client client)
    {
        client.Dead = true;
        lock (_clientsGate) _clients.Remove(client);
        try { client.Tcp.Close(); } catch { /* ignore */ }
        _log.Info($"[WebSocketHub] client 離線，目前 {ClientCount} 個");
    }

    // ── ITouchSink ──────────────────────────────────────────────────────────

    public void Publish(byte[] utf8Json)
    {
        Client[] snapshot;
        lock (_clientsGate) snapshot = _clients.ToArray();

        foreach (var c in snapshot)
        {
            SendFrame(c, 0x1, utf8Json);
            if (c.Dead) DropClient(c);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _listener.Stop(); } catch { /* ignore */ }
        lock (_clientsGate)
        {
            foreach (var c in _clients) { try { c.Tcp.Close(); } catch { /* ignore */ } }
            _clients.Clear();
        }
    }
}
