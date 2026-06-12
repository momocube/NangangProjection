using System.Diagnostics;
using WallProjector.Core;

namespace WallProjector.Engine;

/// <summary>
/// 瀏覽器殼啟動：Edge --app=（無網址列 kiosk 樣式）優先，fallback Chrome。
/// 尋找順序與 TouchService UiLauncher 同款（標準安裝路徑 → PATH），
/// Linux 開發機額外掃 chromium/google-chrome（可建可測）。
///
/// **每槽獨立 --user-data-dir**：否則 Edge/Chrome 會把新視窗合併進既有瀏覽器
/// process（我們 launch 的 process 立刻退出）——watchdog 的 process 監看與
/// kill 都會失準。獨立 profile 強制獨立 process tree。
/// </summary>
public static class BrowserLauncher
{
    public static string? FindBrowser(Logger log)
    {
        var candidates = OperatingSystem.IsWindows()
            ? new[]
            {
                // Edge 優先（場館機標配）
                @"%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe",
                @"%ProgramFiles%\Microsoft\Edge\Application\msedge.exe",
                @"%LocalAppData%\Microsoft\Edge\Application\msedge.exe",
                // fallback Chrome
                @"%ProgramFiles%\Google\Chrome\Application\chrome.exe",
                @"%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe",
                @"%LocalAppData%\Google\Chrome\Application\chrome.exe",
            }
            : new[]
            {
                "/usr/bin/microsoft-edge", "/usr/bin/google-chrome",
                "/usr/bin/chromium", "/usr/bin/chromium-browser", "/snap/bin/chromium",
            };

        foreach (var c in candidates)
        {
            var expanded = Environment.ExpandEnvironmentVariables(c);
            if (!expanded.Contains('%') && File.Exists(expanded)) return expanded;
        }

        // PATH 掃描
        var exeNames = OperatingSystem.IsWindows()
            ? new[] { "msedge.exe", "chrome.exe" }
            : new[] { "microsoft-edge", "google-chrome", "chromium", "chromium-browser" };
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var exe in exeNames)
            foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var candidate = Path.Combine(dir.Trim(), exe);
                    if (File.Exists(candidate)) return candidate;
                }
                catch { /* 壞的 PATH 片段：跳過 */ }
            }

        log.Warn("[Browser] 找不到 Edge / Chrome（標準路徑 + PATH 都沒有）");
        return null;
    }

    /// <summary>組出 --app kiosk 啟動參數（公開純函式，方便檢視/測試）。</summary>
    public static string BuildArgs(string url, string profileDir, WindowRect rect) =>
        $"--app={url} " +
        $"--user-data-dir=\"{profileDir}\" " +
        "--no-first-run --no-default-browser-check --disable-session-crashed-bubble " +
        "--disable-features=msEdgeSidebarV2,TranslateUI " +
        "--autoplay-policy=no-user-gesture-required " +
        $"--window-position={rect.X},{rect.Y} --window-size={rect.Width},{rect.Height}";

    public static Process? Launch(string browserExe, string url, string profileDir, WindowRect rect, Logger log)
    {
        try
        {
            Directory.CreateDirectory(profileDir);
            var psi = new ProcessStartInfo(browserExe, BuildArgs(url, profileDir, rect))
            {
                UseShellExecute = false,
            };
            var proc = Process.Start(psi);
            if (proc == null)
            {
                log.Warn($"[Browser] Process.Start 回 null（{browserExe}）");
                return null;
            }
            log.Info($"[Browser] 已啟動 pid={proc.Id}：{Path.GetFileName(browserExe)} → {url} " +
                     $"@ ({rect.X},{rect.Y}) {rect.Width}x{rect.Height}");
            return proc;
        }
        catch (Exception ex)
        {
            log.Warn($"[Browser] 啟動失敗（{browserExe}）：{ex.Message}");
            return null;
        }
    }
}
