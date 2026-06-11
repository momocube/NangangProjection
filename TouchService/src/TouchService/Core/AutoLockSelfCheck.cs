namespace TouchService.Core;

/// <summary>
/// 啟動自檢窗（autoLock）：服務啟動後 windowSeconds 內，所有路由成功的觸發點
/// 只做 per-cell 封包計數、不進 touch tracker、不發布任何事件（keepalive 空 frame 照發）。
/// 窗結束時計數 ≥ minPackets 的格子視為噪訊晶片，加入 runtime 黑名單（LockSource.Auto）。
///
/// 依據：開機重啟時場上理應無人——此時還在發訊號的格子幾乎必然是故障晶片。
/// 反面風險：重啟時有人站在場上會被誤鎖（DEPLOY.md SOP 有提醒；誤鎖用 stdin unlock 救）。
///
/// 非執行緒安全：呼叫端（TouchPipeline）負責在 pipeline 鎖內呼叫。
/// 時間由呼叫端注入（nowMs，單調時鐘），方便確定性測試。
/// </summary>
public sealed class AutoLockSelfCheck
{
    /// <summary>自檢窗結束時的單格結果（鎖定清單與 log 用）。</summary>
    public sealed record Result(int Face, int Col, int Row, int Packets);

    struct CellCount
    {
        public int Face, Col, Row, Count;
    }

    readonly long _deadlineMs;
    readonly int _minPackets;
    readonly Dictionary<int, CellCount> _counts = new();
    long _totalPackets;

    public AutoLockSelfCheck(long startNowMs, int windowSeconds, int minPackets)
    {
        if (windowSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(windowSeconds));
        _deadlineMs = startNowMs + windowSeconds * 1000L;
        _minPackets = Math.Max(1, minPackets);
        Active = true;
    }

    /// <summary>自檢窗是否進行中（Finish 後為 false，之後管線行為完全恢復正常）。</summary>
    public bool Active { get; private set; }

    /// <summary>鎖定門檻（窗內封包數 ≥ 此值才鎖）。</summary>
    public int MinPackets => _minPackets;

    /// <summary>窗內已觀察到的不同格子數（倒數 log 用）。</summary>
    public int ObservedCells => _counts.Count;

    /// <summary>窗內已記錄的總封包數（倒數 log 用）。</summary>
    public long TotalPackets => _totalPackets;

    /// <summary>窗是否已到期（到期後下一次 OnDatagram / Tick 會觸發 Finish）。</summary>
    public bool IsExpired(long nowMs) => nowMs >= _deadlineMs;

    /// <summary>剩餘秒數（無條件進位；窗結束 = 0）。hello 的 autoLockRemainingSeconds 與倒數 log 用。</summary>
    public int RemainingSeconds(long nowMs) =>
        (int)Math.Max(0, (_deadlineMs - nowMs + 999) / 1000);

    /// <summary>記錄一個窗內觸發點（路由成功、未被既有鎖定過濾的點）。</summary>
    public void Record(in RoutedPoint p)
    {
        int key = (p.FaceIndex << 20) | (p.Col << 10) | p.Row;
        if (_counts.TryGetValue(key, out var c)) c.Count++;
        else c = new CellCount { Face = p.FaceIndex, Col = p.Col, Row = p.Row, Count = 1 };
        _counts[key] = c;
        _totalPackets++;
    }

    /// <summary>
    /// 結束自檢窗。回傳 (達門檻該鎖的格子, 未達門檻不鎖的格子)，皆依 face → col → row 排序。
    /// 呼叫後 Active = false，之後 Record / Finish 不應再被呼叫。
    /// </summary>
    public (List<Result> ToLock, List<Result> BelowThreshold) Finish()
    {
        Active = false;
        var toLock = new List<Result>();
        var below = new List<Result>();
        foreach (var c in _counts.Values)
        {
            var r = new Result(c.Face, c.Col, c.Row, c.Count);
            if (c.Count >= _minPackets) toLock.Add(r);
            else below.Add(r);
        }
        Comparison<Result> cmp = (a, b) =>
            a.Face != b.Face ? a.Face.CompareTo(b.Face) :
            a.Col != b.Col ? a.Col.CompareTo(b.Col) : a.Row.CompareTo(b.Row);
        toLock.Sort(cmp);
        below.Sort(cmp);
        return (toLock, below);
    }
}
