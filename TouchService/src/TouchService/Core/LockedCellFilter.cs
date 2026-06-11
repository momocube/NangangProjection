using TouchService.Config;

namespace TouchService.Core;

/// <summary>
/// 鎖定晶片過濾（噪訊晶片）：config lockedCells → packed key HashSet。
/// 對應 Unity 專案 SensorTestManager 的 IsRawPixelLocked / locked[]，移植為 config 化版本——
/// 現場有晶片故障持續發訊號（同一格反覆 down/up、touch 換 ID 重生）時，
/// 把該格填進 config 即可在進 touch tracker 前丟棄。
///
/// 熱路徑 O(1)、零配置：HashSet&lt;int&gt;.Contains 不 alloc。
/// key packing：(face &lt;&lt; 20) | (col &lt;&lt; 10) | row——face ≤ 2047、col/row ≤ 1023，
/// 遠大於 704 場域上限（7 面、col ≤ 44、row ≤ 128）。
/// </summary>
public sealed class LockedCellFilter
{
    /// <summary>空過濾器（不鎖任何格）。</summary>
    public static readonly LockedCellFilter Empty = new(null);

    readonly HashSet<int> _keys = new();
    readonly List<LockedCellConfig> _valid = new();

    public LockedCellFilter(IEnumerable<LockedCellConfig>? entries)
    {
        if (entries == null) return;
        foreach (var e in entries)
        {
            if (e == null || !e.IsValid) { Skipped++; continue; }
            if (_keys.Add(Pack(e.Face, e.Cell![0], e.Cell[1])))
                _valid.Add(e);
        }
    }

    /// <summary>有效鎖定格數。</summary>
    public int Count => _keys.Count;

    /// <summary>config 裡格式不完整被跳過的項數（啟動 log 用）。</summary>
    public int Skipped { get; }

    /// <summary>有效鎖定格清單（啟動 log 用）。</summary>
    public IReadOnlyList<LockedCellConfig> Entries => _valid;

    /// <summary>此晶片格是否被鎖定。熱路徑：O(1)、零配置。</summary>
    public bool IsLocked(int face, int col, int row) =>
        _keys.Count > 0 && _keys.Contains(Pack(face, col, row));

    static int Pack(int face, int col, int row) => (face << 20) | (col << 10) | row;
}
