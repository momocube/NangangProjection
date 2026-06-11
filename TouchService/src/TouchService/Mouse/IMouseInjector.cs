namespace TouchService.Mouse;

/// <summary>
/// OS 滑鼠注入抽象：滑鼠模式（MouseDriver）的唯一出口。
/// 實作：Win32MouseInjector（SendInput）、NullMouseInjector（非 Windows no-op）、
/// 測試用 FakeInjector（tests 內，記錄呼叫序列）。
/// </summary>
public interface IMouseInjector
{
    /// <summary>游標移到螢幕絕對座標（virtual desktop 座標系，非主螢幕可為負值）。</summary>
    void Move(int x, int y);

    void LeftDown();
    void LeftUp();
}

/// <summary>
/// 非 Windows 平台的 no-op 注入器：服務在 Linux 上選 mouse 模式不 crash、
/// 事件照常發布，只是沒有游標可動（host 啟動時會 log warn）。
/// </summary>
public sealed class NullMouseInjector : IMouseInjector
{
    public void Move(int x, int y) { }
    public void LeftDown() { }
    public void LeftUp() { }
}
