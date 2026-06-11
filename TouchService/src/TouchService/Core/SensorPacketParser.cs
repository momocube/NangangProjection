namespace TouchService.Core;

/// <summary>感應封包中的一個原始觸碰點（raw sensor 座標）。</summary>
public readonly record struct RawPoint(int X, int Y);

/// <summary>封包解析失敗原因。</summary>
public enum PacketError
{
    None = 0,
    TooShort,       // 不足最小封包長度 7 bytes
    BadHeader,      // 開頭不是 55 55
    BadDataSize,    // dataSize 非 4 的倍數
    Truncated,      // 實際長度 < 5 + dataSize + 2
    BadChecksum,    // checksum 不符
}

/// <summary>
/// CyberCube 光域感應模組 UDP 封包解析。
/// 移植自 Unity 專案 Assets/_Script/Input/UdpSensorReceiver.cs:134-188（ParsePacket）。
///
/// 協議格式（已用真實 Wireshark 資料驗證）：
///   55 55 [frame:1] [dataSize_H:1][dataSize_L:1]
///   ([X_H X_L Y_H Y_L] × N 點)
///   [SUM_H:1][SUM_L:1]
///
/// big-endian；checksum = data 區（點座標 bytes）累加取低 16 bit。
/// 一個 datagram 可帶 1~4+ 點（同模組內合包）。
/// </summary>
public static class SensorPacketParser
{
    public const byte Header = 0x55;
    public const int MinPacketLength = 7;

    /// <summary>
    /// 解析一個 UDP datagram。成功時把所有點 append 到 <paramref name="pointsOut"/>（不清空，呼叫端負責）。
    /// 回傳 PacketError.None 代表成功。
    /// </summary>
    public static PacketError TryParse(ReadOnlySpan<byte> data, List<RawPoint> pointsOut, out byte frame)
    {
        frame = 0;

        if (data.Length < MinPacketLength)
            return PacketError.TooShort;

        if (data[0] != Header || data[1] != Header)
            return PacketError.BadHeader;

        frame = data[2];
        int dataSize = (data[3] << 8) | data[4];
        if (dataSize % 4 != 0)
            return PacketError.BadDataSize;

        int expectedLen = 5 + dataSize + 2;
        if (data.Length < expectedLen)
            return PacketError.Truncated;

        // 驗證 checksum：data 區 byte 累加取低 16 bit（frame byte 不計入，與 Unity 端一致）
        int sum = 0;
        for (int i = 5; i < 5 + dataSize; i++) sum += data[i];
        int packetSum = (data[5 + dataSize] << 8) | data[5 + dataSize + 1];
        if ((sum & 0xFFFF) != packetSum)
            return PacketError.BadChecksum;

        int count = dataSize / 4;
        for (int i = 0; i < count; i++)
        {
            int offset = 5 + i * 4;
            int x = (data[offset]     << 8) | data[offset + 1];
            int y = (data[offset + 2] << 8) | data[offset + 3];
            pointsOut.Add(new RawPoint(x, y));
        }

        return PacketError.None;
    }

    /// <summary>組裝一個合法封包（simulator / 測試用）。</summary>
    public static byte[] Build(byte frame, IReadOnlyList<RawPoint> points)
    {
        int dataSize = points.Count * 4;
        var buf = new byte[5 + dataSize + 2];
        buf[0] = Header;
        buf[1] = Header;
        buf[2] = frame;
        buf[3] = (byte)(dataSize >> 8);
        buf[4] = (byte)(dataSize & 0xFF);

        int sum = 0;
        for (int i = 0; i < points.Count; i++)
        {
            int o = 5 + i * 4;
            buf[o]     = (byte)(points[i].X >> 8);
            buf[o + 1] = (byte)(points[i].X & 0xFF);
            buf[o + 2] = (byte)(points[i].Y >> 8);
            buf[o + 3] = (byte)(points[i].Y & 0xFF);
            sum += buf[o] + buf[o + 1] + buf[o + 2] + buf[o + 3];
        }

        buf[5 + dataSize]     = (byte)((sum & 0xFFFF) >> 8);
        buf[5 + dataSize + 1] = (byte)(sum & 0xFF);
        return buf;
    }
}
