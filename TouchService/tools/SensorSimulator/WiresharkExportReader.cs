using System.Text.RegularExpressions;

namespace SensorSimulator;

/// <summary>從 Wireshark export 讀出的一個 UDP datagram（payload = 感應協議 bytes）。</summary>
public sealed class CapturedPacket
{
    /// <summary>capture 相對時間（秒）</summary>
    public double TimeSec { get; init; }
    /// <summary>來源 UDP port（模組識別；replay 時每個 port 一個 socket）</summary>
    public int SourcePort { get; init; }
    public byte[] Payload { get; init; } = Array.Empty<byte>();
}

/// <summary>
/// 解析 Wireshark「Export Packet Dissections as Plain Text」格式：
/// 每個封包一段：summary line（No. Time Source Destination Protocol Length Info）
/// + 數行 dissection 文字 + hex dump（offset 0000/0010/...，每行最多 16 bytes，
/// hex 區後面接 ASCII 欄）。hex dump 從 UDP payload（Data 層）開始。
/// </summary>
public static partial class WiresharkExportReader
{
    // summary line：No. Time Source Destination "UDP" Length Info（Info 內含來源 port「49153 → 20001」）
    [GeneratedRegex(@"^\s*(\d+)\s+([\d.]+)\s+(\S+)\s+(\S+)\s+UDP\s+\d+\s+(\d+)\s+")]
    private static partial Regex SummaryLine();

    // hex dump line：4 位 16 進位 offset + 兩空白 + hex 區
    [GeneratedRegex(@"^([0-9a-fA-F]{4})  ")]
    private static partial Regex HexLine();

    public static List<CapturedPacket> ReadFile(string path)
    {
        var packets = new List<CapturedPacket>();

        double curTime = 0;
        int curPort = 0;
        bool haveSummary = false;
        var curBytes = new List<byte>(32);

        void Flush()
        {
            if (haveSummary && curBytes.Count > 0)
            {
                packets.Add(new CapturedPacket
                {
                    TimeSec = curTime,
                    SourcePort = curPort,
                    Payload = curBytes.ToArray(),
                });
            }
            curBytes.Clear();
        }

        foreach (var line in File.ReadLines(path))
        {
            var summary = SummaryLine().Match(line);
            if (summary.Success)
            {
                Flush();
                curTime = double.Parse(summary.Groups[2].Value);
                curPort = int.Parse(summary.Groups[5].Value);
                haveSummary = true;
                continue;
            }

            var hex = HexLine().Match(line);
            if (hex.Success && haveSummary)
            {
                // offset 0000 = 新 hex dump 開始（保險：同一封包不該出現兩次 0000）
                if (hex.Groups[1].Value == "0000" && curBytes.Count > 0)
                    Flush();

                // hex 區固定欄位：offset(4) + 2 空白 + 16×3 字元；之後是 ASCII 欄（不可誤讀）
                int start = 6;
                int end = Math.Min(line.Length, start + 16 * 3 - 1);
                var hexArea = line[start..end];
                foreach (var tok in hexArea.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (tok.Length != 2) break;   // 碰到 ASCII 欄就停
                    curBytes.Add(Convert.ToByte(tok, 16));
                }
            }
        }

        Flush();
        return packets;
    }
}
