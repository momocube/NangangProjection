using TouchService.Config;
using TouchService.Logging;
using TouchService.Tray;
using Xunit;

namespace TouchService.Tests;

/// <summary>
/// 托盤抽象層（平台無關的部分）：factory 決策（trayEnabled / 非 Windows → NullTrayHost）、
/// NullTrayHost no-op、訊息/選單 → 動作的純對映（「呼叫時機」由這層釘住）。
/// Windows 實機的圖示顯示 / 雙擊 / 右鍵選單行為屬**現場驗證項**（Linux CI 跑不到 message loop）。
/// </summary>
public class TrayHostTests : IDisposable
{
    readonly FileLogger _log = new(
        Directory.CreateTempSubdirectory("touchservice-tray").FullName, LogLevel.Warn, console: false);

    public void Dispose() => _log.Dispose();

    static ServiceConfig Cfg(bool trayEnabled) =>
        new() { Ui = new UiConfig { TrayEnabled = trayEnabled } };

    [Fact]
    public void trayEnabled_false_回NullTrayHost_行為同v5()
    {
        using var tray = TrayHostFactory.Create(Cfg(trayEnabled: false), _log,
            openUi: () => { }, toggleConsole: () => { }, quit: () => { });
        Assert.IsType<NullTrayHost>(tray);
    }

    [Fact]
    public void 非Windows平台_一律NullTrayHost()
    {
        if (OperatingSystem.IsWindows()) return;   // 此測試只驗非 Windows 路徑

        using var tray = TrayHostFactory.Create(Cfg(trayEnabled: true), _log,
            openUi: () => { }, toggleConsole: () => { }, quit: () => { });
        Assert.IsType<NullTrayHost>(tray);
    }

    [Fact]
    public void NullTrayHost_全程noop不crash()
    {
        var tray = new NullTrayHost();
        tray.Start();
        tray.Start();     // 重複呼叫
        tray.Dispose();
        tray.Dispose();   // 重複 dispose
    }

    [Fact]
    public void 非Windows_ConsoleWindowManager全noop()
    {
        if (OperatingSystem.IsWindows()) return;

        Assert.False(ConsoleWindowManager.IsVisible());
        ConsoleWindowManager.Hide(_log);            // 不丟例外
        Assert.False(ConsoleWindowManager.Toggle(_log));
    }

    // ── 托盤互動 → 動作（純對映；WindowsTrayHost message loop 呼叫這層）──────

    [Fact]
    public void 雙擊圖示_開UI_其他滑鼠訊息不動作()
    {
        Assert.Equal(TrayAction.OpenUi, TrayMenu.MapTrayMouseMessage(TrayMenu.WM_LBUTTONDBLCLK));
        Assert.Equal(TrayAction.None, TrayMenu.MapTrayMouseMessage(0x0201));   // WM_LBUTTONDOWN：單擊不動作
        Assert.Equal(TrayAction.None, TrayMenu.MapTrayMouseMessage(0x0200));   // WM_MOUSEMOVE
    }

    [Fact]
    public void 右鍵_觸發context_menu()
    {
        Assert.True(TrayMenu.IsContextMenuTrigger(TrayMenu.WM_RBUTTONUP));
        Assert.True(TrayMenu.IsContextMenuTrigger(TrayMenu.WM_CONTEXTMENU));
        Assert.False(TrayMenu.IsContextMenuTrigger(TrayMenu.WM_LBUTTONDBLCLK));
    }

    [Fact]
    public void 選單指令_對應三動作_取消不動作()
    {
        Assert.Equal(TrayAction.OpenUi, TrayMenu.MapMenuCommand(TrayMenu.CMD_OPEN_UI));
        Assert.Equal(TrayAction.ToggleConsole, TrayMenu.MapMenuCommand(TrayMenu.CMD_TOGGLE_CONSOLE));
        Assert.Equal(TrayAction.Quit, TrayMenu.MapMenuCommand(TrayMenu.CMD_QUIT));
        Assert.Equal(TrayAction.None, TrayMenu.MapMenuCommand(0));     // TrackPopupMenu 取消
        Assert.Equal(TrayAction.None, TrayMenu.MapMenuCommand(999));
    }
}
