using System.Net;
using TouchService.Output;

namespace SensorSimulator;

/// <summary>
/// 感應模組模擬器：離場驗證 TouchService 的核心工具。
///
///   replay   重放 Wireshark export 真實封包流（保持原時序，可調速）
///   scenario 照 JSON 腳本驅動 N 個點的 down/hold/up 時間軸（80ms 重送 + jitter / 丟包注入）
///
/// 用法：
///   SensorSimulator replay   capture.txt   [--target 127.0.0.1:20001] [--speed 1.0] [--loop]
///   SensorSimulator scenario scenario.json [--target 127.0.0.1:20001]
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            PrintUsage();
            return 1;
        }

        var mode = args[0].ToLowerInvariant();
        var file = args[1];
        var target = UdpJsonPublisher.ParseEndpoint(GetOption(args, "--target") ?? "127.0.0.1:20001");
        double speed = double.Parse(GetOption(args, "--speed") ?? "1.0");
        bool loop = args.Contains("--loop");

        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"檔案不存在：{file}");
            return 1;
        }

        switch (mode)
        {
            case "replay":
            {
                var packets = WiresharkExportReader.ReadFile(file);
                double duration = packets.Count > 0 ? packets[^1].TimeSec - packets[0].TimeSec : 0;
                var modules = packets.Select(p => p.SourcePort).Distinct().Count();
                Console.WriteLine($"[replay] {file}");
                Console.WriteLine($"[replay] {packets.Count} 封包 / {modules} 個模組 source port / 原始時長 {duration:F1}s / 速度 ×{speed} → {target}");

                do
                {
                    using var runner = new ReplayRunner(packets, target, speed);
                    runner.Run(Console.WriteLine);
                    Console.WriteLine($"[replay] 完成，共送出 {runner.SentCount} 封包");
                } while (loop);
                return 0;
            }

            case "scenario":
            {
                var scenario = Scenario.Load(file);
                Console.WriteLine($"[scenario] {scenario.Name}");
                Console.WriteLine($"[scenario] {scenario.Touches.Count} 個觸碰點 / 重送 {scenario.ResendIntervalMs}ms / jitter ±{scenario.JitterMs}ms / 丟包率 {scenario.DropRate:P0} → {target}");

                using var runner = new ScenarioRunner(scenario, target);
                runner.Run();

                int sent = runner.Records.Count(r => !r.Dropped);
                int dropped = runner.Records.Count(r => r.Dropped);
                Console.WriteLine($"[scenario] 完成：實送 {sent} 點（含合包）、注入丟包 {dropped} 點");
                return 0;
            }

            default:
                PrintUsage();
                return 1;
        }
    }

    static string? GetOption(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }

    static void PrintUsage()
    {
        Console.WriteLine("用法：");
        Console.WriteLine("  SensorSimulator replay   <capture.txt>   [--target 127.0.0.1:20001] [--speed 1.0] [--loop]");
        Console.WriteLine("  SensorSimulator scenario <scenario.json> [--target 127.0.0.1:20001]");
    }
}
