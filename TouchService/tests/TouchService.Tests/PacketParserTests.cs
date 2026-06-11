using TouchService.Core;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// 封包解析 golden tests。
/// golden hex 全部取自真實 Wireshark capture（tests/fixtures/，2026-06-10 南港現場）。
/// </summary>
public class PacketParserTests
{
    static byte[] Hex(string s) =>
        s.Split(' ', StringSplitOptions.RemoveEmptyEntries)
         .Select(t => Convert.ToByte(t, 16)).ToArray();

    static (PacketError Err, List<RawPoint> Points, byte Frame) Parse(byte[] data)
    {
        var pts = new List<RawPoint>();
        var err = SensorPacketParser.TryParse(data, pts, out var frame);
        return (err, pts, frame);
    }

    // ── golden：真實 capture 封包 ────────────────────────────────────────────

    [Fact]
    public void 真實封包_1點()
    {
        // capture-stomp-floor.txt frame 2
        var (err, pts, frame) = Parse(Hex("55 55 00 00 04 0e b0 03 68 01 29"));
        Assert.Equal(PacketError.None, err);
        Assert.Equal(0, frame);
        Assert.Equal(new[] { new RawPoint(3760, 872) }, pts);
    }

    [Fact]
    public void 真實封包_2點_同模組合包()
    {
        // capture-stomp-floor.txt frame 4
        var (err, pts, _) = Parse(Hex("55 55 00 00 08 0e d0 03 28 0e d0 03 38 02 22"));
        Assert.Equal(PacketError.None, err);
        Assert.Equal(new[] { new RawPoint(3792, 808), new RawPoint(3792, 824) }, pts);
    }

    [Fact]
    public void 真實封包_3點()
    {
        // capture-multitouch-floor.txt（hex dump 跨 0000/0010 兩行）
        var (err, pts, _) = Parse(Hex("55 55 00 00 0c 0e f0 08 88 0e d0 08 78 0f 50 08 48 03 9b"));
        Assert.Equal(PacketError.None, err);
        Assert.Equal(new[]
        {
            new RawPoint(3824, 2184),
            new RawPoint(3792, 2168),
            new RawPoint(3920, 2120),
        }, pts);
    }

    [Fact]
    public void 真實封包_4點()
    {
        // capture-multitouch-floor.txt（一個 datagram 帶 4 點 = 實測合包上限）
        var (err, pts, _) = Parse(Hex(
            "55 55 00 00 10 0e 50 08 68 0e 70 08 78 0e b0 08 58 0e 90 08 68 03 f8"));
        Assert.Equal(PacketError.None, err);
        Assert.Equal(new[]
        {
            new RawPoint(3664, 2152),
            new RawPoint(3696, 2168),
            new RawPoint(3760, 2136),
            new RawPoint(3728, 2152),
        }, pts);
    }

    // ── 錯誤案例 ────────────────────────────────────────────────────────────

    [Fact]
    public void Checksum錯誤_拒收()
    {
        // 真實 1 點封包把 Y_L 0x68 改成 0x69，checksum 不再相符
        var (err, pts, _) = Parse(Hex("55 55 00 00 04 0e b0 03 69 01 29"));
        Assert.Equal(PacketError.BadChecksum, err);
        Assert.Empty(pts);
    }

    [Fact]
    public void Header不符_拒收()
    {
        var (err, _, _) = Parse(Hex("55 54 00 00 04 0e b0 03 68 01 29"));
        Assert.Equal(PacketError.BadHeader, err);
    }

    [Fact]
    public void DataSize非4倍數_拒收()
    {
        var (err, _, _) = Parse(Hex("55 55 00 00 03 0e b0 03 68 01 29"));
        Assert.Equal(PacketError.BadDataSize, err);
    }

    [Fact]
    public void 封包長度不足_拒收()
    {
        // dataSize 宣告 8（2 點）但實際只有 1 點的資料
        var (err, _, _) = Parse(Hex("55 55 00 00 08 0e b0 03 68 01 29"));
        Assert.Equal(PacketError.Truncated, err);
    }

    [Fact]
    public void 不足最小長度_拒收()
    {
        var (err, _, _) = Parse(Hex("55 55 00 00 04 0e"));
        Assert.Equal(PacketError.TooShort, err);
    }

    [Fact]
    public void 空封包_拒收()
    {
        var (err, _, _) = Parse(Array.Empty<byte>());
        Assert.Equal(PacketError.TooShort, err);
    }

    // ── Build / Parse 互逆 ──────────────────────────────────────────────────

    [Fact]
    public void Build後Parse互逆()
    {
        var src = new List<RawPoint> { new(2576, 648), new(3952, 2680), new(4592, 2440) };
        var packet = SensorPacketParser.Build(7, src);

        var (err, pts, frame) = Parse(packet);
        Assert.Equal(PacketError.None, err);
        Assert.Equal(7, frame);
        Assert.Equal(src, pts);
    }

    [Fact]
    public void Build產生的bytes與真實封包一致()
    {
        var packet = SensorPacketParser.Build(0, new List<RawPoint> { new(3760, 872) });
        Assert.Equal(Hex("55 55 00 00 04 0e b0 03 68 01 29"), packet);
    }
}
