using System.Diagnostics;
using TouchService.Config;
using TouchService.Core;
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
        // config 路徑：--config <path> 或第一個位置參數；預設 EXE 同目錄 config.json，雙擊啟動不依賴工作目錄
        // --console：托盤化下保留 console 視窗（除錯用）
        // --restart-delay-ms <n>:啟動前等 n ms(切 venue 重啟時舊 process 釋放 port 用)
        string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        bool keepConsole = false;
        int restartDelayMs = 0;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length) configPath = args[i + 1];
            else if (args[i] == "--console") keepConsole = true;
            else if (args[i] == "--restart-delay-ms" && i + 1 < args.Length && int.TryParse(args[i + 1], out var d)) restartDelayMs = d;
            else if (!args[i].StartsWith("--") && configPath == "config.json") configPath = args[i];
        }
        configPath = Path.GetFullPath(configPath);
        var baseDir = Path.GetDirectoryName(configPath)!;

        if (restartDelayMs > 0) Thread.Sleep(restartDelayMs);

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
        // UI 網頁「Venue 切換」按鈕 → POST /venue/switch → host raise VenueSwitchRequested
        host.VenueSwitchRequested += relPath => DoSwitchVenue(relPath, config, configPath, log, host);

        using var tray = TrayHostFactory.Create(config, log,
            openUi: () => UiLauncher.Open(config, log),
            toggleConsole: () => ConsoleWindowManager.Toggle(log),
            quit: host.RequestShutdown,
            isConsoleVisible: ConsoleWindowManager.IsVisible);
        tray.Start();

        // 啟動完成 → 自動開 UI（Windows + ui.autoLaunch；Edge app → Chrome app → 預設瀏覽器。
        // 失敗只 log 不擋服務；放在 Main 而非 host.Start 內——測試大量啟動 host，不該彈視窗）
        // Venue 切換重啟(restartDelayMs > 0)不再開新視窗:舊視窗還開著,WS 會自動重連套用新 venue
        if (restartDelayMs <= 0) UiLauncher.TryLaunch(config, log);
        else log.Info("[TouchService] venue 切換重啟,跳過自動開 UI(舊視窗會自動重連)");

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

    /// <summary>
    /// UI 網頁按下「切換 venue」按鈕觸發(POST /venue/switch → host.VenueSwitchRequested):
    /// 更新 config.venueProfile、寫回檔案、spawn 新 process(帶 --restart-delay-ms 500
    /// 讓舊 process 完全釋放 port),然後 RequestShutdown 結束當前 process。
    /// path 已由 ConfigApi 檢核在掃到的清單裡(擋 path traversal)。
    /// </summary>
    static void DoSwitchVenue(string relPath, ServiceConfig config, string configPath,
                              FileLogger log, TouchServiceHost host)
    {
        log.Info($"[TouchService] 切換 venue → {relPath},重啟中…");
        config.VenueProfile = relPath;
        try
        {
            ServiceConfig.Save(configPath, config);
        }
        catch (Exception ex)
        {
            log.Error($"[TouchService] 寫回 config 失敗:{ex.Message}");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath ?? throw new InvalidOperationException("Environment.ProcessPath 為 null"),
                UseShellExecute = false,
            };
            psi.ArgumentList.Add("--config");
            psi.ArgumentList.Add(configPath);
            psi.ArgumentList.Add("--restart-delay-ms");
            psi.ArgumentList.Add("500");
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            log.Error($"[TouchService] spawn 新 process 失敗:{ex.Message}");
            return;
        }

        host.RequestShutdown();
    }
}
