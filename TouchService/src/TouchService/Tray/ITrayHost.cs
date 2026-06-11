namespace TouchService.Tray;

/// <summary>
/// 系統列托盤抽象：Windows 實作 WindowsTrayHost（P/Invoke Shell_NotifyIcon），
/// 非 Windows / ui.trayEnabled=false 用 NullTrayHost（全 no-op）。
/// 三個動作回呼由建構（factory）注入：
///   openUi        — 雙擊圖示 / 右鍵「開啟介面」→ 開 UI 視窗（複用 UiLauncher）
///   toggleConsole — 右鍵「顯示/隱藏主控台」→ console 視窗 show/hide
///   quit          — 右鍵「結束服務」→ 優雅 shutdown（同 stdin quit）
/// </summary>
public interface ITrayHost : IDisposable
{
    /// <summary>掛上托盤圖示（Windows 起獨立 message loop thread；Null 實作 no-op）。</summary>
    void Start();
}

/// <summary>非 Windows / trayEnabled=false：完全 no-op（服務行為同 v5）。</summary>
public sealed class NullTrayHost : ITrayHost
{
    public void Start() { }
    public void Dispose() { }
}
