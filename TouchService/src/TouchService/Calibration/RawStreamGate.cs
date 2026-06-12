namespace TouchService.Calibration;

/// <summary>
/// raw 檢視串流的節流 + 穩定計數（純邏輯、時間注入、可單元測試）。
///
/// 穩定計數：per-source（ip:port = 一個感應模組）追蹤「同座標連續重送」次數 n
/// ——精靈的穩定偵測（n ≥ 3 自動取用）以此為準。per-source 隔離讓別的模組
/// 在叫的噪訊晶片不會打斷正在踩的角落晶片的連續性。
///
/// 節流（兩層，噪訊晶片在叫也不能洗版）：
///   1. 同座標連續重送去重：前 3 包（n = 1,2,3，讓 UI 看到穩定進度）照推，
///      之後同座標重送壓到每秒最多 1 包（心跳，UI 知道訊號還在）；座標一變立即推
///   2. 全域上限 ~30 msg/s（固定 1 秒窗計數，超過直接丟）
/// </summary>
public sealed class RawStreamGate
{
    /// <summary>穩定門檻：同座標連續 ≥ 3 包 = 穩定（UI 自動取用的依據）。</summary>
    public const int StableCount = 3;

    /// <summary>全域推送上限（msg/s）。</summary>
    public const int MaxPerSecond = 30;

    /// <summary>同座標重送的心跳間隔（ms）：去重後仍每秒推一包讓 UI 知道訊號還在。</summary>
    public const int HeartbeatMs = 1000;

    /// <summary>source 狀態上限（防禦：理論上 source 數 = 模組數，幾十個）。</summary>
    const int MaxSources = 512;

    sealed class SourceState
    {
        public int LastX, LastY;
        public int Count;                 // 同座標連續次數（穩定偵測）
        public bool HasEmitted;
        public int LastEmitX, LastEmitY;
        public long LastEmitMs;
    }

    readonly Dictionary<(string Addr, int Port), SourceState> _sources = new();
    long _windowStartMs = long.MinValue;
    int _windowCount;

    /// <summary>清空全部狀態（校正工作階段開始時呼叫）。</summary>
    public void Reset()
    {
        _sources.Clear();
        _windowStartMs = long.MinValue;
        _windowCount = 0;
    }

    /// <summary>
    /// 一個 raw 點進來：更新穩定計數並判定要不要推給 client。
    /// 回傳 true = 推（n = 此座標在該 source 的連續次數）。
    /// </summary>
    public bool Offer(string srcAddr, int srcPort, int x, int y, long nowMs, out int n)
    {
        if (_sources.Count >= MaxSources) _sources.Clear();   // 防禦：不該發生（source = 模組數）

        var key = (srcAddr, srcPort);
        if (!_sources.TryGetValue(key, out var st))
            _sources[key] = st = new SourceState();

        // 穩定計數：同座標連續 +1、座標一變歸 1
        if (st.Count > 0 && st.LastX == x && st.LastY == y) st.Count++;
        else { st.Count = 1; st.LastX = x; st.LastY = y; }
        n = st.Count;

        // 去重：座標變了 / 穩定進度（前 3 包）/ 心跳到期 才推
        bool want = !st.HasEmitted || st.LastEmitX != x || st.LastEmitY != y
                    || st.Count <= StableCount
                    || nowMs - st.LastEmitMs >= HeartbeatMs;
        if (!want) return false;

        // 全域上限：固定 1 秒窗（_windowStartMs 哨兵值要先判掉——
        // nowMs - long.MinValue 會 overflow 成負數，窗永遠不重置）
        if (_windowStartMs == long.MinValue || nowMs - _windowStartMs >= 1000)
        {
            _windowStartMs = nowMs;
            _windowCount = 0;
        }
        if (_windowCount >= MaxPerSecond) return false;
        _windowCount++;

        st.HasEmitted = true;
        st.LastEmitX = x;
        st.LastEmitY = y;
        st.LastEmitMs = nowMs;
        return true;
    }
}
