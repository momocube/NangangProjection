using TouchService.Config;
using TouchService.Logging;

namespace TouchService.Tray;

/// <summary>
/// 依平台 / config 決定托盤實作（決策邏輯抽出來可單元測試；
/// Windows 實機的圖示與選單行為屬現場驗證項）。
/// </summary>
public static class TrayHostFactory
{
    /// <summary>
    /// trayEnabled=false 或非 Windows → NullTrayHost（行為同 v5）。
    /// isConsoleVisible：右鍵選單「顯示/隱藏主控台」項的文字依當下狀態切換。
    /// </summary>
    public static ITrayHost Create(ServiceConfig config, FileLogger log,
                                   Action openUi, Action toggleConsole, Action quit,
                                   Func<bool>? isConsoleVisible = null)
    {
        if (!config.Ui.TrayEnabled)
        {
            log.Info("[Tray] ui.trayEnabled=false，不掛托盤（行為同 v5：console 視窗照常）");
            return new NullTrayHost();
        }
        if (!OperatingSystem.IsWindows())
        {
            log.Info("[Tray] 非 Windows 平台，托盤 no-op");
            return new NullTrayHost();
        }
        return new WindowsTrayHost(log, openUi, toggleConsole, quit,
                                   isConsoleVisible ?? (() => true));
    }
}
