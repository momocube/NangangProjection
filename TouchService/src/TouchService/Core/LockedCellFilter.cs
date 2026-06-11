using TouchService.Config;

namespace TouchService.Core;

/// <summary>鎖定來源：config = config.json lockedCells、auto = 啟動自檢窗、manual = stdin lock 指令。</summary>
public enum LockSource { Config, Auto, Manual }

/// <summary>一筆 runtime 鎖定（含來源標記；auto 來源帶自檢窗內的封包數）。</summary>
public sealed class LockedCellEntry
{
    public required int Face { get; init; }
    public required int Col { get; init; }
    public required int Row { get; init; }
    public required LockSource Source { get; init; }
    /// <summary>auto 鎖定時自檢窗內收到的封包數（config / manual 為 0）。</summary>
    public int Packets { get; init; }
}

/// <summary>
/// 鎖定晶片過濾（噪訊晶片）：config lockedCells + runtime 鎖定（auto 自檢 / manual 指令）
/// 共用同一個 packed key HashSet 生效，來源以 LockSource 區分。
/// 對應 Unity 專案 SensorTestManager 的 IsRawPixelLocked / locked[]，移植為 config 化版本——
/// 現場有晶片故障持續發訊號（同一格反覆 down/up、touch 換 ID 重生）時，
/// 把該格填進 config、等啟動自檢自動鎖、或下 stdin `lock` 指令，皆在進 touch tracker 前丟棄。
///
/// 執行緒模型：IsLocked 為熱路徑（receive 執行緒），讀 volatile snapshot HashSet，
/// O(1)、零配置、無鎖。變更（TryAdd / TryRemove）為冷路徑（stdin / 自檢結束），
/// 在 _mutGate 下重建 HashSet 後整顆換掉（copy-on-write）。
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
    /// 新增鎖定。回傳 false = 該格已鎖定（任何來源）或座標超出 packing 範圍。
    /// 冷路徑（自檢結束 / stdin lock）。
    /// </summary>
    public bool TryAdd(int face, int col, int row, LockSource source, int packets = 0)
    {
        if (face is < 0 or > 2047 || col is < 0 or > 1023 || row is < 0 or > 1023) return false;
        lock (_mutGate)
        {
            int key = Pack(face, col, row);
            if (_entries.ContainsKey(key)) return false;
            _entries[key] = new LockedCellEntry
            {
                Face = face, Col = col, Row = row, Source = source, Packets = packets,
            };
            RebuildKeysLocked();
            return true;
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

    /// <summary>查詢單格鎖定資訊（locks / lock 指令用）。</summary>
    public LockedCellEntry? Find(int face, int col, int row)
    {
        lock (_mutGate) return _entries.GetValueOrDefault(Pack(face, col, row));
    }

    /// <summary>
    /// 當下所有鎖定的穩定排序快照（face → col → row），
    /// 供 locks 指令、hello / locks push、savelocks 使用。
    /// </summary>
    public IReadOnlyList<LockedCellEntry> Snapshot()
    {
        lock (_mutGate)
        {
            return _entries.Values
                .OrderBy(e => e.Face).ThenBy(e => e.Col).ThenBy(e => e.Row)
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
