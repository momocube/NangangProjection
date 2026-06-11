using System.Diagnostics;
using TouchService.Config;
using TouchService.Logging;

namespace TouchService.Ui;

/// <summary>
/// 啟動 UI：服務啟動完成後（Windows 且 ui.autoLaunch=true）自動開瀏覽器視窗到
/// http://127.0.0.1:&lt;wsPort&gt;/（監控 + 設定頁）。
/// 依序嘗試：(1) Edge app 視窗 --app=（無網址列、像原生視窗）(2) Chrome --app=
/// (3) fallback 預設瀏覽器開該網址。
/// 非 Windows / 找不到瀏覽器 / 任何錯誤：只 log，不影響服務（Program.Main 在 host.Start 後呼叫，
/// 不放在 host.Start 內——測試大量啟動 host，不該彈視窗）。
/// </summary>
public static class UiLauncher
{
    public static void TryLaunch(ServiceConfig config, FileLogger log)
    {
        try { LaunchInner(config, log); }
        catch (Exception ex)
        {
            log.Warn($"[UI] 自動開啟 UI 失敗（不影響服務）：{ex.Message}");
        }
    }

    static void LaunchInner(ServiceConfig config, FileLogger log)
    {
        if (!config.Ui.AutoLaunch)
        {
            log.Info("[UI] ui.autoLaunch=false，不自動開啟 UI");
            return;
        }
        if (!OperatingSystem.IsWindows())
        {
            log.Info("[UI] 非 Windows，不自動開啟 UI（瀏覽器自行開 http://127.0.0.1:" +
                     $"{config.WebSocketOutput.Port}/）");
            return;
        }
        if (!config.WebSocketOutput.Enabled)
        {
            log.Warn("[UI] webSocketOutput 停用，沒有 UI 可開（ui.autoLaunch 略過）");
            return;
        }

        var url = $"http://127.0.0.1:{config.WebSocketOutput.Port}/";

        // (1) Edge app 視窗
        var edge = FindBrowser("msedge.exe", new[]
        {
            @"%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe",
            @"%ProgramFiles%\Microsoft\Edge\Application\msedge.exe",
            @"%LocalAppData%\Microsoft\Edge\Application\msedge.exe",
        });
        if (edge != null && TryStart(edge, $"--app={url}", log, "Edge app 視窗")) return;

        // (2) Chrome app 視窗
        var chrome = FindBrowser("chrome.exe", new[]
        {
            @"%ProgramFiles%\Google\Chrome\Application\chrome.exe",
            @"%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe",
            @"%LocalAppData%\Google\Chrome\Application\chrome.exe",
        });
        if (chrome != null && TryStart(chrome, $"--app={url}", log, "Chrome app 視窗")) return;

        // (3) fallback：預設瀏覽器
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            log.Info($"[UI] 已用預設瀏覽器開啟 {url}");
        }
        catch (Exception ex)
        {
            log.Warn($"[UI] 找不到可用瀏覽器（Edge/Chrome/預設都失敗）：{ex.Message}——" +
                     $"請手動開 {url}");
        }
    }

    /// <summary>標準安裝路徑（環境變數展開）找不到再掃 PATH。</summary>
    static string? FindBrowser(string exeName, string[] standardPaths)
    {
        foreach (var p in standardPaths)
        {
            var expanded = Environment.ExpandEnvironmentVariables(p);
            if (!expanded.Contains('%') && File.Exists(expanded)) return expanded;
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), exeName);
                if (File.Exists(candidate)) return candidate;
            }
            catch { /* 壞的 PATH 片段：跳過 */ }
        }
        return null;
    }

    static bool TryStart(string exePath, string args, FileLogger log, string label)
    {
        try
        {
            Process.Start(new ProcessStartInfo(exePath, args) { UseShellExecute = false });
            log.Info($"[UI] 已開啟 {label}：{exePath} {args}");
            return true;
        }
        catch (Exception ex)
        {
            log.Warn($"[UI] {label} 啟動失敗（{exePath}）：{ex.Message}，嘗試下一個");
            return false;
        }
    }
}
