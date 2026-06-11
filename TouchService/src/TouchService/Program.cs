using TouchService.Config;
using TouchService.Logging;
using TouchService.Tray;
using TouchService.Ui;

namespace TouchService;

/// <summary>
/// TouchService 進入點：console 程式 + 系統列托盤常駐（v6）。
/// 啟動讀 config → 自檢窗（autoLock）→ 收 UDP → 發布（raw UDP JSON + WebSocket）。
///
/// Windows + ui.trayEnabled（預設 true）：啟動後隱藏 console 視窗、掛托盤圖示
/// （雙擊開 UI；右鍵：開啟介面 / 顯示主控台 / 結束服務）。`--console` flag 保留 console（除錯）。
/// 非 Windows / trayEnabled=false：行為同 v5（console 照常）。
///
/// 指令（stdin 與 UI 終端的 POST /command 共用 CommandProcessor，輸出一致）：
/// stats / touches / history / locks / lock / unlock / savelocks / probe / quit。
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        // config 路徑：--config <path> 或第一個位置參數；預設執行目錄下 config.json
        // --console：托盤化下保留 console 視窗（除錯用）
        string configPath = "config.json";
        bool keepConsole = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length) configPath = args[i + 1];
            else if (args[i] == "--console") keepConsole = true;
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

        log.Info($"[TouchService] 啟動，config：{configPath}");

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
        host.ShutdownRequested += quit.Set;   // stdin/API quit、托盤「結束服務」同源
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; quit.Set(); };

        // 托盤化（Windows + ui.trayEnabled）：隱藏 console + 掛 NotifyIcon。
        // --console flag 或 trayEnabled=false 不隱藏（v5 行為）。Linux 全 no-op。
        bool trayMode = OperatingSystem.IsWindows() && config.Ui.TrayEnabled;
        if (trayMode && !keepConsole)
            ConsoleWindowManager.Hide(log);
        using var tray = TrayHostFactory.Create(config, log,
            openUi: () => UiLauncher.Open(config, log),
            toggleConsole: () => ConsoleWindowManager.Toggle(log),
            quit: host.RequestShutdown,
            isConsoleVisible: ConsoleWindowManager.IsVisible);
        tray.Start();

        // 啟動完成 → 自動開 UI（Windows + ui.autoLaunch；Edge app → Chrome app → 預設瀏覽器。
        // 失敗只 log 不擋服務；放在 Main 而非 host.Start 內——測試大量啟動 host，不該彈視窗）
        UiLauncher.TryLaunch(config, log);

        Console.WriteLine(Api.CommandProcessor.HelpText);

        // stdin 指令迴圈（與 POST /command 共用 CommandProcessor，輸出文字一致）。
        // console 不可用（隱藏後重導 / service 模式 / stdin 關閉）→ graceful no-op：
        // ReadLine 回 null 或丟例外都只結束這條 thread，服務照跑（UI 終端 / 托盤仍可操作）。
        var stdinThread = new Thread(() =>
        {
            try
            {
                string? line;
                while ((line = Console.ReadLine()) != null)
                {
                    var output = host.Commands.Execute(line);
                    if (output.Length > 0) Console.WriteLine(output);
                }
            }
            catch (Exception)
            {
                // stdin 不可用：靜默退出（quit 仍可由 Ctrl+C / 托盤 / UI 終端觸發）
            }
        }) { IsBackground = true, Name = "Stdin" };
        stdinThread.Start();

        quit.Wait();
        log.Info("[TouchService] 收到結束指令，關閉中…");
        return 0;
    }
}
