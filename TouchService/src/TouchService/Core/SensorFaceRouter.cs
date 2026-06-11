namespace TouchService.Core;

/// <summary>路由結果：raw 點分類到面、映射為畫布像素 / 格子 / 正規化座標。</summary>
public readonly struct RoutedPoint
{
    public required int FaceIndex { get; init; }
    public required int Col { get; init; }
    public required int Row { get; init; }
    public required int PixelX { get; init; }
    public required int PixelY { get; init; }
    public required float NormX { get; init; }
    public required float NormY { get; init; }
}

/// <summary>
/// raw 點 → 7 面線性分類 → 座標映射。
/// 移植自 Unity 專案 Assets/_Script/Input/SensorFaceRouter.cs:37-62（OnPacket），
/// 去掉 Unity 的鎖定過濾與 manager 派送，改為純函式回傳 RoutedPoint。
/// </summary>
public sealed class SensorFaceRouter
{
    readonly SensorFaceConfig[] _faces;
    readonly float _canvasW;
    readonly float _canvasH;

    public SensorFaceRouter(VenueProfile profile)
    {
        _faces = profile.Faces.ToArray();
        _canvasW = profile.Canvas.Width;
        _canvasH = profile.Canvas.Height;
    }

    public IReadOnlyList<SensorFaceConfig> Faces => _faces;

    /// <summary>
    /// 線性掃描各面（先到先得，與 Unity 行為一致）。
    /// 回傳 false 代表 raw 點不屬於任何面（縫隙 / 雜訊）。
    /// </summary>
    public bool TryRoute(int rawX, int rawY, out RoutedPoint point)
    {
        for (int fi = 0; fi < _faces.Length; fi++)
        {
            var cfg = _faces[fi];
            if (cfg == null || !cfg.ContainsRaw(rawX, rawY)) continue;

            var (px, py)   = cfg.RawToPixel(rawX, rawY);
            var (col, row) = cfg.PixelToCell(px, py);

            point = new RoutedPoint
            {
                FaceIndex = fi,
                Col = col,
                Row = row,
                PixelX = px,
                PixelY = py,
                NormX = px / _canvasW,
                NormY = py / _canvasH,
            };
            return true;
        }

        point = default;
        return false;
    }
}
