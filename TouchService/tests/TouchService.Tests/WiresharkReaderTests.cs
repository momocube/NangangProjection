using SensorSimulator;
using TouchService.Core;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// Wireshark export 解析 + 真實 capture 整檔驗證
/// （fixtures 為 2026-06-10 南港現場 Wireshark「Export Packet Dissections as Plain Text」）。
/// </summary>
public class WiresharkReaderTests
{
    static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "fixtures", name);

    static readonly VenueProfile Venue =
        VenueProfile.Load(Path.Combine(AppContext.BaseDirectory, "venues", "704.json"));

    [Fact]
    public void 單腳capture_113封包127點_全數checksum正確()
    {
        var packets = WiresharkExportReader.ReadFile(Fixture("capture-stomp-floor.txt"));
        Assert.Equal(113, packets.Count);

        var pts = new List<RawPoint>();
        var byN = new Dictionary<int, int>();
        foreach (var p in packets)
        {
            int before = pts.Count;
            Assert.Equal(PacketError.None, SensorPacketParser.TryParse(p.Payload, pts, out _));
            int n = pts.Count - before;
            byN[n] = byN.GetValueOrDefault(n) + 1;
        }

        Assert.Equal(127, pts.Count);
        Assert.Equal(102, byN[1]);   // 1 點 ×102
        Assert.Equal(8,   byN[2]);   // 2 點 ×8
        Assert.Equal(3,   byN[3]);   // 3 點 ×3
        Assert.All(packets, p => Assert.Equal(49153, p.SourcePort));

        // 原時序保留：capture 內相對時間跨度 ~89.25s
        double span = packets[^1].TimeSec - packets[0].TimeSec;
        Assert.InRange(span, 89.0, 90.0);
    }

    [Fact]
    public void 多點capture_335封包503點_含4點合包()
    {
        var packets = WiresharkExportReader.ReadFile(Fixture("capture-multitouch-floor.txt"));
        Assert.Equal(335, packets.Count);

        var pts = new List<RawPoint>();
        var byN = new Dictionary<int, int>();
        foreach (var p in packets)
        {
            int before = pts.Count;
            Assert.Equal(PacketError.None, SensorPacketParser.TryParse(p.Payload, pts, out _));
            int n = pts.Count - before;
            byN[n] = byN.GetValueOrDefault(n) + 1;
        }

        Assert.Equal(503, pts.Count);
        Assert.Equal(205, byN[1]);
        Assert.Equal(100, byN[2]);
        Assert.Equal(22,  byN[3]);
        Assert.Equal(8,   byN[4]);   // 4 點合包（實測上限）
    }

    [Fact]
    public void 兩份capture所有點_都路由到合法的面()
    {
        var router = new SensorFaceRouter(Venue);
        int total = 0, routed = 0;
        var faceHit = new HashSet<string>();

        foreach (var file in new[] { "capture-stomp-floor.txt", "capture-multitouch-floor.txt" })
        {
            var pts = new List<RawPoint>();
            foreach (var p in WiresharkExportReader.ReadFile(Fixture(file)))
                SensorPacketParser.TryParse(p.Payload, pts, out _);

            foreach (var pt in pts)
            {
                total++;
                if (router.TryRoute(pt.X, pt.Y, out var rp))
                {
                    routed++;
                    faceHit.Add(Venue.Faces[rp.FaceIndex].Name);
                }
            }
        }

        Assert.Equal(630, total);         // 127 + 503
        Assert.Equal(total, routed);      // 真實資料零 unrouted
        Assert.Contains("Floor", faceHit);
        Assert.Contains("Entrance", faceHit);   // 單腳 capture 有踩到 Entrance 面
    }
}
