using TouchService.Config;

namespace TouchService.Core;

/// <summary>鎖定來源：config = config.json lockedCells、auto = 啟動自檢窗、manual = stdin lock 指令。</summary>
public enum LockSource { Config, Auto, Manual }

/// <summary>
/// 一筆 runtime 鎖定（含來源標記；auto 來源帶自檢窗內的封包數）。
/// v9 起帶「鎖後活動」統計：鎖定那一刻之後該格又發了幾包、最後一包何時——
/// 鎖後仍持續發包 = 鐵證噪訊；鎖後安靜 = 可能誤鎖（UI 臨時鎖區據此判讀）。
/// </summary>
public sealed class LockedCellEntry
{
    public required int Face { get; init; }
    public required int Col { get; init; }
    public required int Row { get; init; }
    public required LockSource Source { get; init; }
    /// <summary>建立此臨時鎖的清場草稿；只供安全解除該次掃描的鎖使用。</summary>
    public string? CaptureId { get; internal set; }
    /// <summary>auto 鎖定時自檢窗內收到的封包數（config / manual 為 0）。</summary>
    public int Packets { get; init; }
    /// <summary>鎖定時刻（pipeline 單調時鐘 ms；config 來源 = 啟動時 ≈ 0）。</summary>
    public long LockedAtMs { get; init; }
    /// <summary>鎖定後被過濾丟棄的封包點數（鎖後活動）。寫入受 _mutGate 保護。</summary>
    public long DroppedSinceLock { get; internal set; }
    /// <summary>最後一次被丟棄的時刻（-1 = 鎖定後沒再發過）。寫入受 _mutGate 保護。</summary>
    public long LastDropMs { get; internal set; } = -1;
}

/// <summary>
/// 鎖定晶片過濾（噪訊晶片）：config lockedCells + runtime 鎖定（auto 自檢 / manual 指令）
/// 共用同一個 packed key HashSet 生效，來源以 LockSource 區分。
/// 對應 Unity 專案 SensorTestManager 的 IsRawPixelLocked / locked[]，移植為 config 化版本——
/// 現場有晶片故障持續發訊號（同一格反覆 down/up、touch 換 ID 重生）時，
/// 把該格填進 config、等啟動自檢自動鎖、或下 stdin `lock` 指令，皆在進 touch tracker 前丟棄。
///
/// 執行緒模型：IsLocked / CheckAndCount 為熱路徑（receive 執行緒），讀 volatile snapshot
/// HashSet，O(1)、零配置、無鎖（CheckAndCount 僅在「命中鎖定」時才取 _mutGate 記鎖後活動
/// ——只發生在被鎖噪訊晶片的封包上，冷量）。變更（TryAdd / TryRemove / Clear / Reclassify）
/// 為冷路徑（stdin / API / 自檢結束），在 _mutGate 下重建 HashSet 後整顆換掉（copy-on-write）。
/// key packing：(face &lt;&lt; 20) | (col &lt;&lt; 10) | row——face ≤ 2047、col/row ≤ 1023，
/// 遠大於 704 場域上限（7 面、col ≤ 44、row ≤ 128）。
/// </summary>
public sealed class LockedCellFilter
{
    /// <summary>空過濾器（不鎖任何格）。</summary>
    public static readonly LockedCellFilter Empty = new(null);

    readonly object _mutGate = new();
    readonly Dictionary<int, LockedCellEntry> _entries = new();
    volatile HashSet<int> _keys = new();

    public LockedCellFilter(IEnumerable<LockedCellConfig>? entries)
    {
        if (entries == null) return;
        foreach (var e in entries)
        {
            if (e == null || !e.IsValid) { Skipped++; continue; }
            TryAdd(e.Face, e.Cell![0], e.Cell[1], LockSource.Config);
        }
    }

    /// <summary>有效鎖定格數（全部來源）。</summary>
    public int Count => _keys.Count;

    /// <summary>auto 來源（啟動自檢窗）鎖定格數，stats 輸出用。</summary>
    public int AutoCount
    {
        get { lock (_mutGate) return _entries.Values.Count(e => e.Source == LockSource.Auto); }
    }

    /// <summary>config 裡格式不完整被跳過的項數（啟動 log 用）。</summary>
    public int Skipped { get; }

    /// <summary>此晶片格是否被鎖定。熱路徑：O(1)、零配置、無鎖（讀 volatile snapshot）。</summary>
    public bool IsLocked(int face, int col, int row)
    {
        var keys = _keys;
        return keys.Count > 0 && keys.Contains(Pack(face, col, row));
    }

    /// <summary>
    /// 鎖定檢查 + 鎖後活動入帳（pipeline 過濾點用）：未鎖定 = 與 IsLocked 同成本
    /// （O(1) 無鎖）直接回 false；鎖定 = 在 _mutGate 下累計 DroppedSinceLock / LastDropMs
    /// 後回 true。取鎖只發生在「被鎖噪訊晶片的封包」上（每顆 ~12.5 pkt/s 冷量），
    /// 正常觸發熱路徑零影響。
    /// </summary>
    public bool CheckAndCount(int face, int col, int row, long nowMs)
    {
        var keys = _keys;
        int key = Pack(face, col, row);
        if (keys.Count == 0 || !keys.Contains(key)) return false;

        lock (_mutGate)
        {
            // 檢查與取鎖之間被 unlock 的窄窗：entry 已不在就當沒鎖（本包放行下一包重判）
            if (!_entries.TryGetValue(key, out var e)) return false;
            e.DroppedSinceLock++;
            e.LastDropMs = nowMs;
            return true;
        }
    }

    /// <summary>
    /// 新增鎖定。回傳 false = 該格已鎖定（任何來源）或座標超出 packing 範圍。
    /// 冷路徑（自檢結束 / stdin lock）。nowMs = 鎖定時刻（鎖後活動的基準點）。
    /// </summary>
    public bool TryAdd(int face, int col, int row, LockSource source, int packets = 0, long nowMs = 0,
                       string? captureId = null)
    {
        if (face is < 0 or > 2047 || col is < 0 or > 1023 || row is < 0 or > 1023) return false;
        lock (_mutGate)
        {
            int key = Pack(face, col, row);
            if (_entries.ContainsKey(key)) return false;
            _entries[key] = new LockedCellEntry
            {
                Face = face, Col = col, Row = row, Source = source, Packets = packets,
                LockedAtMs = nowMs, CaptureId = captureId,
            };
            RebuildKeysLocked();
            return true;
        }
    }

    /// <summary>批次加入清場鎖，一次重建熱路徑 snapshot，避免逐格重建整份 HashSet。</summary>
    public int TryAddCaptureBatch(IEnumerable<(int Face, int Col, int Row)> cells, string captureId, long nowMs)
    {
        lock (_mutGate)
        {
            int added = 0;
            foreach (var (face, col, row) in cells.Distinct())
            {
                if (face is < 0 or > 2047 || col is < 0 or > 1023 || row is < 0 or > 1023) continue;
                int key = Pack(face, col, row);
                if (_entries.ContainsKey(key)) continue;
                _entries[key] = new LockedCellEntry
                {
                    Face = face, Col = col, Row = row, Source = LockSource.Manual,
                    LockedAtMs = nowMs, CaptureId = captureId,
                };
                added++;
            }
            if (added > 0) RebuildKeysLocked();
            return added;
        }
    }

    /// <summary>解除鎖定（任何來源都可解）。回傳 false = 該格本來就沒鎖。冷路徑（stdin unlock）。</summary>
    public bool TryRemove(int face, int col, int row, out LockedCellEntry? removed)
    {
        lock (_mutGate)
        {
            int key = Pack(face, col, row);
            if (!_entries.Remove(key, out removed)) return false;
            RebuildKeysLocked();
            return true;
        }
    }

    /// <summary>
    /// 全部解除（v9「解鎖全部」：含 config 來源）。只動 runtime——config.json 不變、
    /// 重啟會回到 config 的鎖定清單；要永久生效再走 savelocks。回傳解除的格數。
    /// </summary>
    public int Clear()
    {
        lock (_mutGate)
        {
            int n = _entries.Count;
            if (n == 0) return 0;
            _entries.Clear();
            RebuildKeysLocked();
            return n;
        }
    }

    /// <summary>
    /// 單格來源重分類（v9「轉正式」：寫進 config 後 runtime 來源改標 config，
    /// UI 的「已保存 / 未保存」分類才會跟上）。鎖定時刻與鎖後活動統計保留不重置。
    /// 回傳 false = 該格沒鎖。
    /// </summary>
    public bool Reclassify(int face, int col, int row, LockSource source)
    {
        lock (_mutGate)
        {
            int key = Pack(face, col, row);
            if (!_entries.TryGetValue(key, out var e)) return false;
            if (e.Source != source) _entries[key] = CopyWithSource(e, source);
            return true;
        }
    }

    /// <summary>全部來源重分類（savelocks 寫回 config 後全部改標 config）。回傳改動的格數。</summary>
    public int ReclassifyAll(LockSource source)
    {
        lock (_mutGate)
        {
            int changed = 0;
            foreach (var (key, e) in _entries.ToList())
            {
                if (e.Source == source) continue;
                _entries[key] = CopyWithSource(e, source);
                changed++;
            }
            return changed;
        }
    }

    /// <summary>正式保存某份清場草稿後，只將該草稿擁有的 runtime 鎖標成已保存。</summary>
    public int ReclassifyAllForCapture(string captureId, LockSource source)
    {
        lock (_mutGate)
        {
            int changed = 0;
            foreach (var (key, e) in _entries.ToList())
            {
                if (e.CaptureId != captureId || e.Source == source) continue;
                _entries[key] = CopyWithSource(e, source);
                changed++;
            }
            return changed;
        }
    }

    static LockedCellEntry CopyWithSource(LockedCellEntry e, LockSource source) => new()
    {
        Face = e.Face, Col = e.Col, Row = e.Row, Source = source, Packets = e.Packets,
        LockedAtMs = e.LockedAtMs, DroppedSinceLock = e.DroppedSinceLock, LastDropMs = e.LastDropMs,
        CaptureId = e.CaptureId,
    };

    /// <summary>查詢單格鎖定資訊（locks / lock 指令用）。</summary>
    public LockedCellEntry? Find(int face, int col, int row)
    {
        lock (_mutGate) return _entries.GetValueOrDefault(Pack(face, col, row));
    }

    /// <summary>
    /// 當下所有鎖定的穩定排序快照（face → col → row），
    /// 供 locks 指令、hello / locks push、GET /locks、savelocks 使用。
    /// 回傳副本——鎖後活動統計欄位讀到的是一致時點的值，不會跟熱路徑寫入互踩。
    /// </summary>
    public IReadOnlyList<LockedCellEntry> Snapshot()
    {
        lock (_mutGate)
        {
            return _entries.Values
                .OrderBy(e => e.Face).ThenBy(e => e.Col).ThenBy(e => e.Row)
                .Select(e => CopyWithSource(e, e.Source))
                .ToList();
        }
    }

    /// <summary>來源標記的對外字串（hello / locks push 的 source 欄位）。</summary>
    public static string SourceName(LockSource s) => s switch
    {
        LockSource.Config => "config",
        LockSource.Auto   => "auto",
        LockSource.Manual => "manual",
        _ => "unknown",
    };

    void RebuildKeysLocked()
    {
        var keys = new HashSet<int>(_entries.Count);
        foreach (var k in _entries.Keys) keys.Add(k);
        _keys = keys;   // 整顆換掉：熱路徑讀到的永遠是不再變動的 set
    }

    static int Pack(int face, int col, int row) => (face << 20) | (col << 10) | row;
}
