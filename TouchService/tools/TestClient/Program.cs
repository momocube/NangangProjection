using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace TestClient;

/// <summary>
/// 端到端驗證用的小型 raw UDP JSON 訂閱 client：
/// 監聽 TouchService 的 UDP 輸出 port，即時印出事件流，Ctrl+C 結束時印統計摘要。
///
/// 用法：TestClient [port]（預設 20110）
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        int port = args.Length > 0 ? int.Parse(args[0]) : 20110;

        using var udp = new UdpClient(port);
        Console.WriteLine($"[TestClient] 監聽 UDP {port}，等待 TouchService 輸出…（Ctrl+C 結束）");

        long frames = 0, downs = 0, moves = 0, ups = 0;
        var seenIds = new HashSet<int>();
        long lastSeq = -1, seqGaps = 0;
        int maxAlive = 0;

        var done = new ManualResetEventSlim(false);
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; done.Set(); udp.Close(); };

        var remote = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            while (!done.IsSet)
            {
                byte[] data;
                try { data = udp.Receive(ref remote); }
                catch (SocketException) { break; }
                catch (ObjectDisposedException) { break; }
                if (data.Length == 0) continue;   // socket 關閉時可能吐空 datagram

                frames++;
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;

                long seq = root.GetProperty("seq").GetInt64();
                if (lastSeq >= 0 && seq != lastSeq + 1) seqGaps++;
                lastSeq = seq;

                int alive = root.GetProperty("alive").GetArrayLength();
                maxAlive = Math.Max(maxAlive, alive);

                foreach (var e in root.GetProperty("events").EnumerateArray())
                {
                    int id = e.GetProperty("id").GetInt32();
                    var phase = e.GetProperty("phase").GetString();
                    seenIds.Add(id);
                    switch (phase)
                    {
                        case "down": downs++; break;
                        case "move": moves++; break;
                        case "up": ups++; break;
                    }

                    var px = e.GetProperty("px");
                    Console.WriteLine(
                        $"seq={seq,-6} id={id,-4} {phase,-5} face={e.GetProperty("face").GetInt32()} " +
                        $"px=({px[0].GetInt32()},{px[1].GetInt32()}) alive={alive}");
                }
            }
        }
        finally
        {
            var sb = new StringBuilder();
            sb.AppendLine("── TestClient 統計 ───────────────────────");
            sb.AppendLine($"frames     : {frames}（seq 跳號 {seqGaps} 次）");
            sb.AppendLine($"touch IDs  : {seenIds.Count} 個");
            sb.AppendLine($"down/move/up: {downs} / {moves} / {ups}");
            sb.AppendLine($"max alive  : {maxAlive}");
            sb.Append("──────────────────────────────────────────");
            Console.WriteLine(sb.ToString());
        }
        return 0;
    }
}
