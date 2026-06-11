namespace TouchService.Core;

/// <summary>
/// 啟動自檢窗（autoLock）：服務啟動後 windowSeconds 內，所有路由成功的觸發點
/// 只做 per-cell 封包計數 + episode 切分、不進 touch tracker、不發布任何事件
/// （keepalive 空 frame 照發）。窗結束時命中任一判準（OR）的格子視為噪訊晶片，
/// 加入 runtime 黑名單（LockSource.Auto）：
///
///   判準 A（packets）：窗內封包數 ≥ minPackets——快速重送型噪訊（~80ms 重送，10s 約 125 包）。
///   判準 B（episodes）：窗內分離 episode 數 ≥ minEpisodes（minEpisodes ≤ 0 = 停用）——
///     慢速頻閃型噪訊（2026-06-11 現場：face2 cell(37,14) 每次只發 1 包、間隔 0.5~1.5s，
///     連續 #1~#84 個 down/up 循環，10s 窗內湊不滿 minPackets 而躲過判準 A）。
///
/// episode 定義：同一 cell 的封包流中，與前一包間隔 &gt; releaseTimeout（取生效值）即切分為
/// 新 episode——與 TriggerHistory.RecordSuppressed 的偽生命週期重建同一條 gap 邏輯
/// （gap ≤ timeout = 同一次觸發延續），單位因此與正常路徑的 touch 生命週期一致。
///
/// 依據：開機重啟時場上理應無人——此時還在發訊號的格子幾乎必然是故障晶片。
/// episode 判準對開機場景是鐵的：真人誤踩是一次連續事件（1 episode），
/// 不會在同一格分離地觸發 3 次。
/// 反面風險：重啟時有人站在場上會被誤鎖（DEPLOY.md SOP 有提醒；誤鎖用 stdin unlock 救）。
///
/// 非執行緒安全：呼叫端（TouchPipeline）負責在 pipeline 鎖內呼叫。
/// 時間由呼叫端注入（nowMs，單調時鐘），方便確定性測試。
/// </summary>
public sealed class AutoLockSelfCheck
{
    /// <summary>
    /// 自檢窗結束時的單格結果（鎖定清單與 log 用）。
    /// HitPackets / HitEpisodes = 命中的判準（兩者皆 false = 未達門檻不鎖）。
    /// </summary>
    public sealed record Result(int Face, int Col, int Row, int Packets, int Episodes,
                                bool HitPackets, bool HitEpisodes)
    {
        /// <summary>命中原因標記（log 用）："packets" / "episodes" / "packets+episodes"。</summary>
        public string Reason =>
            HitPackets && HitEpisodes ? "packets+episodes" :
            HitPackets ? "packets" :
            HitEpisodes ? "episodes" : "none";
    }

    struct CellCount
    {
        public int Face, Col, Row, Count, Episodes;
        public long LastMs;
    }

    readonly long _deadlineMs;
    readonly int _minPackets;
    readonly int _minEpisodes;
    readonly int _releaseTimeoutMs;
    readonly Dictionary<int, CellCount> _counts = new();
    long _totalPackets;

    /// <summary>
    /// minEpisodes ≤ 0 = 停用 episode 判準（只剩 packets 判準，行為同 v6）。
    /// releaseTimeoutMs = episode 切分用的生效值（與 tracker 的 release timeout 同源）。
    /// </summary>
    public AutoLockSelfCheck(long startNowMs, int windowSeconds, int minPackets,
                             int minEpisodes, int releaseTimeoutMs)
    {
        if (windowSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(windowSeconds));
        _deadlineMs = startNowMs + windowSeconds * 1000L;
        _minPackets = Math.Max(1, minPackets);
        _minEpisodes = Math.Max(0, minEpisodes);
        _releaseTimeoutMs = Math.Max(1, releaseTimeoutMs);
        Active = true;
    }

    /// <summary>自檢窗是否進行中（Finish 後為 false，之後管線行為完全恢復正常）。</summary>
    public bool Active { get; private set; }

    /// <summary>判準 A 門檻（窗內封包數 ≥ 此值即鎖）。</summary>
    public int MinPackets => _minPackets;

    /// <summary>判準 B 門檻（窗內分離 episode 數 ≥ 此值即鎖；0 = 停用此判準）。</summary>
    public int MinEpisodes => _minEpisodes;

    /// <summary>窗內已觀察到的不同格子數（倒數 log 用）。</summary>
    public int ObservedCells => _counts.Count;

    /// <summary>窗內已記錄的總封包數（倒數 log 用）。</summary>
    public long TotalPackets => _totalPackets;

    /// <summary>窗是否已到期（到期後下一次 OnDatagram / Tick 會觸發 Finish）。</summary>
    public bool IsExpired(long nowMs) => nowMs >= _deadlineMs;

    /// <summary>剩餘秒數（無條件進位；窗結束 = 0）。hello 的 autoLockRemainingSeconds 與倒數 log 用。</summary>
    public int RemainingSeconds(long nowMs) =>
        (int)Math.Max(0, (_deadlineMs - nowMs + 999) / 1000);

    /// <summary>
    /// 記錄一個窗內觸發點（路由成功、未被既有鎖定過濾的點）。
    /// nowMs 用於 episode 切分：與同 cell 前一包間隔 &gt; releaseTimeout 即記為新 episode。
    /// </summary>
    public void Record(in RoutedPoint p, long nowMs)
    {
        int key = (p.FaceIndex << 20) | (p.Col << 10) | p.Row;
        if (_counts.TryGetValue(key, out var c))
        {
            c.Count++;
            if (nowMs - c.LastMs > _releaseTimeoutMs) c.Episodes++;   // gap > timeout = 新 episode
        }
        else
        {
            c = new CellCount { Face = p.FaceIndex, Col = p.Col, Row = p.Row, Count = 1, Episodes = 1 };
        }
        c.LastMs = nowMs;
        _counts[key] = c;
        _totalPackets++;
    }

    /// <summary>
    /// 結束自檢窗。回傳 (命中任一判準該鎖的格子, 未達門檻不鎖的格子)，皆依 face → col → row 排序。
    /// 呼叫後 Active = false，之後 Record / Finish 不應再被呼叫。
    /// </summary>
    public (List<Result> ToLock, List<Result> BelowThreshold) Finish()
    {
        Active = false;
        var toLock = new List<Result>();
        var below = new List<Result>();
        foreach (var c in _counts.Values)
        {
            bool hitPackets = c.Count >= _minPackets;
            bool hitEpisodes = _minEpisodes > 0 && c.Episodes >= _minEpisodes;
            var r = new Result(c.Face, c.Col, c.Row, c.Count, c.Episodes, hitPackets, hitEpisodes);
            if (hitPackets || hitEpisodes) toLock.Add(r);
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
