using TouchService.Config;
using TouchService.Diagnostics;
using TouchService.Logging;

namespace TouchService;

/// <summary>
/// TouchService 進入點：headless console。
/// 啟動讀 config → 自檢窗（autoLock）→ 收 UDP → 發布（raw UDP JSON + WebSocket）。
/// stdin 指令：stats / touches / locks / lock / unlock / savelocks / probe / quit。
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

        using var host = new TouchServiceHost(config, baseDir, log, configPath);
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

        Console.WriteLine("指令：stats（統計）/ touches（活躍 touch）/ locks（鎖定清單）/ " +
                          "lock|unlock <face> <col> <row>（手動鎖/解鎖）/ savelocks（鎖定寫回 config）/ " +
                          "probe（dump latency CSV）/ quit（離開）");

        // stdin 指令迴圈（stdin 關閉時改等 Ctrl+C / SIGTERM）
        var stdinThread = new Thread(() =>
        {
            string? line;
            while ((line = Console.ReadLine()) != null)
            {
                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                switch (parts.Length == 0 ? "" : parts[0].ToLowerInvariant())
                {
                    case "stats":
                        Console.WriteLine(host.StatsSnapshot());
                        break;
                    case "touches":
                        Console.WriteLine(host.TouchesSnapshot());
                        break;
                    case "locks":
                        Console.WriteLine(host.LocksSnapshot());
                        break;
                    case "lock" or "unlock":
                        if (parts.Length != 4 ||
                            !int.TryParse(parts[1], out int face) ||
                            !int.TryParse(parts[2], out int col) ||
                            !int.TryParse(parts[3], out int row))
                        {
                            Console.WriteLine($"用法：{parts[0].ToLowerInvariant()} <face> <col> <row>（例：{parts[0].ToLowerInvariant()} 2 7 13）");
                            break;
                        }
                        Console.WriteLine(parts[0].ToLowerInvariant() == "lock"
                            ? host.LockCell(face, col, row)
                            : host.UnlockCell(face, col, row));
                        break;
                    case "savelocks":
                        Console.WriteLine(host.SaveLocks());
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
                        Console.WriteLine("未知指令。可用：stats / touches / locks / lock / unlock / savelocks / probe / quit");
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
