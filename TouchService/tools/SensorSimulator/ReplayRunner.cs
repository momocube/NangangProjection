using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace SensorSimulator;

/// <summary>
/// replay 模式：重放 Wireshark export 的真實封包流，保持原時序（可調速）。
/// capture 內每個來源 port（= 一個硬體模組）對應一個獨立 UDP socket。
/// </summary>
public sealed class ReplayRunner : IDisposable
{
    readonly List<CapturedPacket> _packets;
    readonly IPEndPoint _target;
    readonly double _speed;
    readonly Dictionary<int, Socket> _sockets = new();

    public ReplayRunner(List<CapturedPacket> packets, IPEndPoint target, double speed)
    {
        if (speed <= 0) throw new ArgumentOutOfRangeException(nameof(speed));
        _packets = packets.OrderBy(p => p.TimeSec).ToList();
        _target = target;
        _speed = speed;

        foreach (var port in packets.Select(p => p.SourcePort).Distinct())
        {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _sockets[port] = s;
        }
    }

    public int SentCount { get; private set; }

    /// <summary>重放一輪（阻塞）。時間軸以第一個封包為 0。</summary>
    public void Run(Action<string>? progress = null)
    {
        if (_packets.Count == 0) return;

        double t0 = _packets[0].TimeSec;
        var clock = Stopwatch.StartNew();

        foreach (var pkt in _packets)
        {
            long dueMs = (long)((pkt.TimeSec - t0) * 1000.0 / _speed);
            long waitMs = dueMs - clock.ElapsedMilliseconds;
            if (waitMs > 1) Thread.Sleep((int)waitMs);

            _sockets[pkt.SourcePort].SendTo(pkt.Payload, _target);
            SentCount++;

            if (progress != null && SentCount % 50 == 0)
                progress($"  已送 {SentCount}/{_packets.Count} 封包（t={pkt.TimeSec - t0:F1}s）");
        }
    }

    public void Dispose()
    {
        foreach (var s in _sockets.Values) s.Dispose();
    }
}
