using TouchService.Config;
using TouchService.Diagnostics;
using TouchService.Logging;

namespace TouchService;

/// <summary>
/// TouchService 進入點：headless console。
/// 啟動讀 config → 收 UDP → 發布（raw UDP JSON + WebSocket）。
/// stdin 指令：stats（吐統計）、probe（dump latency CSV）、quit。
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        // config 路徑：--config <path> 或第一個位置參數；預設執行目錄下 config.json
        string configPath = "config.json";
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length) configPath = args[i + 1];
            else if (!args[i].StartsWith("--") && configPath == "config.json") configPath = args[i];
        }
        configPath = Path.GetFullPath(configPath);
        var baseDir = Path.GetDirectoryName(configPath)!;

        bool created = !File.Exists(configPath);
        var config = ServiceConfig.LoadOrCreate(configPath);
        if (created)
            Console.WriteLine($"[TouchService] config 不存在，已生成範本：{configPath}");

        var logDir = Path.IsPathRooted(config.Log.Dir) ? config.Log.Dir : Path.Combine(baseDir, config.Log.Dir);
        using var log = new FileLogger(logDir, FileLogger.ParseLevel(config.Log.Level), config.Log.Console);

        log.Info($"[TouchService] v1 啟動，config：{configPath}");

        using var host = new TouchServiceHost(config, baseDir, log);
        try
        {
            host.Start();
        }
        catch (Exception ex)
        {
            log.Error($"[TouchService] 啟動失敗：{ex.Message}");
            return 1;
        }

        var quit = new ManualResetEventSlim(false);
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; quit.Set(); };

        Console.WriteLine("指令：stats（統計）/ probe（dump latency CSV）/ quit（離開）");

        // stdin 指令迴圈（stdin 關閉時改等 Ctrl+C / SIGTERM）
        var stdinThread = new Thread(() =>
        {
            string? line;
            while ((line = Console.ReadLine()) != null)
            {
                switch (line.Trim().ToLowerInvariant())
                {
                    case "stats":
                        Console.WriteLine(host.StatsSnapshot());
                        break;
                    case "probe":
                        var path = LatencyProbe.DumpCsv(logDir);
                        Console.WriteLine($"latency CSV → {path}");
                        break;
                    case "quit" or "exit" or "q":
                        quit.Set();
                        return;
                    case "":
                        break;
                    default:
                        Console.WriteLine("未知指令。可用：stats / probe / quit");
                        break;
                }
            }
        }) { IsBackground = true, Name = "Stdin" };
        stdinThread.Start();

        quit.Wait();
        log.Info("[TouchService] 收到結束指令，關閉中…");
        return 0;
    }
}
