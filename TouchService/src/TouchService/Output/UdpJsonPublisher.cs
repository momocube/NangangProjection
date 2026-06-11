using System.Net;
using System.Net.Sockets;
using TouchService.Logging;

namespace TouchService.Output;

/// <summary>
/// raw UDP JSON 輸出：把每個 frame 原封不動送往 config 指定的 endpoint 清單。
/// 訂閱端範例見 docs/HANDOVER.md。
/// </summary>
public sealed class UdpJsonPublisher : ITouchSink
{
    readonly Socket _socket;
    readonly IPEndPoint[] _endpoints;
    readonly FileLogger _log;

    public UdpJsonPublisher(IEnumerable<string> endpoints, FileLogger log)
    {
        _log = log;
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _endpoints = endpoints.Select(ParseEndpoint).ToArray();
    }

    public static IPEndPoint ParseEndpoint(string s)
    {
        int idx = s.LastIndexOf(':');
        if (idx <= 0) throw new FormatException($"endpoint 格式錯誤（需 host:port）：{s}");
        var host = s[..idx];
        var port = int.Parse(s[(idx + 1)..]);
        if (!IPAddress.TryParse(host, out var ip))
            ip = Dns.GetHostAddresses(host).First(a => a.AddressFamily == AddressFamily.InterNetwork);
        return new IPEndPoint(ip, port);
    }

    public int EndpointCount => _endpoints.Length;

    public void Publish(byte[] utf8Json)
    {
        for (int i = 0; i < _endpoints.Length; i++)
        {
            try
            {
                _socket.SendTo(utf8Json, _endpoints[i]);
            }
            catch (SocketException se)
            {
                // UDP 送失敗（如 ICMP port unreachable 回壓）不致命，記 debug 級即可
                _log.Debug($"[UdpJsonPublisher] SendTo {_endpoints[i]} 失敗：{se.SocketErrorCode}");
            }
        }
    }

    public void Dispose() => _socket.Dispose();
}
