using WallProjector.Config;
using WallProjector.Core;
using WallProjector.Engine;
using WallProjector.Web;

namespace WallProjector;

/// <summary>
/// WallProjector 入口——CyberCube 光域牆面投影管理工具 V1。
/// 顯示層（視窗擺位 + 旋轉 wrap），輸入層是 TouchService（滑鼠模式）——完全解耦，
/// 本工具不碰任何輸入。
/// </summary>
public static class Program
{
    const string Version = "2.0.0";

    public static int Main(string[] args)
    {
        string configPath = "projections.json";
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help" or "-h":
                    PrintHelp();
                    return 0;
                case "--config" when i + 1 < args.Length:
                    configPath = args[++i];
                    break;
                default:
                    if (!args[i].StartsWith("--") && configPath == "projections.json")
                        configPath = args[i];
                    break;
            }
        }

        // 預設 config 在 exe 旁（cwd 沒有時）——場館機捷徑啟動 cwd 常是 system32
        if (configPath == "projections.json" && !File.Exists(configPath))
        {
            var beside = Path.Combine(AppContext.BaseDirectory, "projections.json");
            if (File.Exists(beside)) configPath = beside;
        }
        configPath = Path.GetFullPath(configPath);

        var log = new Logger();
        log.Info($"WallProjector v{Version} — 牆面投影管理工具（顯示層；輸入層 = TouchService）");
        log.Info($"[Config] {configPath}");

        var config = ProjectorConfig.Load(configPath, log);

        var cachePath = Path.Combine(Path.GetDirectoryName(configPath)!, "touchservice-cache.json");
        var tsClient = new TouchServiceClient(config.TouchServiceBaseUrl, cachePath, config.Fallback, log);
        var ts = tsClient.Resolve();

        using var engine = new ProjectionEngine(config, configPath, ts, log);
        engine.SetReconnect(tsClient.Resolve);

        // vendored 資源（pdf.js）：config 旁優先（repo 開發），再 exe 旁（dist 部署）
        var assetsDirs = new[]
        {
            Path.Combine(Path.GetDirectoryName(configPath)!, "assets"),
            Path.Combine(AppContext.BaseDirectory, "assets"),
        };
        var api = new AdminApi(engine, assetsDirs);
        using var http = new HttpServer(config.HttpPort, api.Handle, log);
        try
        {
            http.Start();
        }
        catch (Exception ex)
        {
            log.Error($"[Http] port {config.HttpPort} 啟動失敗：{ex.Message}（已有另一個 WallProjector？）");
            return 1;
        }
        engine.HttpPort = http.Port;

        if (config.AutoRestore)
        {
            log.Info("[Engine] autoRestore=true，還原所有 enabled 槽…");
            foreach (var line in engine.RestoreAll().Split('\n'))
                log.Info($"[Engine] {line}");
        }

        log.Info($"管理頁：http://127.0.0.1:{http.Port}/   stdin 指令：status / restore / start <face> / stop <face> / quit");

        // Ctrl+C 優雅關閉（殺掉所有投影瀏覽器）
        var quit = new ManualResetEventSlim(false);
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; quit.Set(); };

        // stdin 迴圈（console 不可用時 graceful 等待 Ctrl+C / kill）
        var stdinThread = new Thread(() =>
        {
            while (!quit.IsSet)
            {
                string? line;
                try { line = Console.ReadLine(); }
                catch { break; }
                if (line == null) { quit.Set(); break; }   // EOF（Ctrl+D / 重導結束）

                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;
                switch (parts[0].ToLowerInvariant())
                {
                    case "status":
                        Console.WriteLine(engine.StatusText());
                        break;
                    case "restore":
                        Console.WriteLine(engine.Reconnect());
                        Console.WriteLine(engine.RestoreAll());
                        break;
                    case "start" when parts.Length > 1 && int.TryParse(parts[1], out int sf):
                        Console.WriteLine(engine.Start(sf).Message);
                        break;
                    case "stop" when parts.Length > 1 && int.TryParse(parts[1], out int tf):
                        Console.WriteLine(engine.Stop(tf).Message);
                        break;
                    case "quit" or "exit":
                        quit.Set();
                        break;
                    case "help":
                        Console.WriteLine("指令：status / restore / start <face> / stop <face> / quit");
                        break;
                    default:
                        Console.WriteLine($"未知指令：{parts[0]}（help 看清單）");
                        break;
                }
            }
        }) { IsBackground = true, Name = "Stdin" };
        stdinThread.Start();

        quit.Wait();
        log.Info("關閉中——停止所有投影瀏覽器…");
        return 0;
    }

    static void PrintHelp() => Console.WriteLine($"""
        WallProjector v{Version} — CyberCube 光域牆面投影管理工具

        用法：WallProjector [--config <path>] [--help]
          --config <path>   指定 projections.json（預設：cwd 或 exe 旁）

        做什麼：
          把內容投到場域特定牆面——從 TouchService 拿十字佈局（GET /venue）與投影螢幕
          校正（GET /config 的 mouseMode.screen/content），算出牆面在投影螢幕上的 rect，
          用 Edge/Chrome --app 無邊框視窗擺上去；左右牆臂內容 CSS rotate 轉正。
          三模式（每槽 mode 欄位）：wrap=網頁 iframe 包覆（預設）、cdp=直開原站 +
          DevTools 注入旋轉（X-Frame-Options 擋不到）、file=本地影片/PDF/圖片。
          觸控由 TouchService 滑鼠模式提供，本工具不碰輸入。

        管理頁：http://127.0.0.1:20121/（port 由 projections.json httpPort 設定）
        stdin 指令：status / restore / start <face> / stop <face> / quit
        文件：WallProjector/README.md
        """);
}
