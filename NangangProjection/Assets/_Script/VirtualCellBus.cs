/// <summary>
/// 虛擬格事件中轉站（靜態 Event Bus）。
///
/// 遊戲場景訂閱此 Bus 取得虛擬格觸發 / 釋放事件，
/// 座標為畫布絕對像素（與 SensorBus.OnCanvasTouch 相同座標系）。
///
/// 事件簽名：(canvasX, canvasY, canvasW, canvasH, cellSizePx)
///   canvasX/Y = 虛擬格中心點像素座標（原點左上）
///   canvasW/H = 畫布總尺寸（由 DisplayCanvasConfig 決定）
///   cellSizePx = 32 或 64
/// </summary>
public static class VirtualCellBus
{
    /// <summary>虛擬格從靜止變為觸發時廣播（VCell 鎖定時不廣播）。</summary>
    public static event System.Action<float, float, float, float, int> OnVirtualCellTouch;

    /// <summary>虛擬格內所有晶片超過 2×PollingInterval 無觸碰後廣播。</summary>
    public static event System.Action<float, float, float, float, int> OnVirtualCellRelease;

    public static void VirtualCellTouch(float cx, float cy, float cw, float ch, int sizePx)
        => OnVirtualCellTouch?.Invoke(cx, cy, cw, ch, sizePx);

    public static void VirtualCellRelease(float cx, float cy, float cw, float ch, int sizePx)
        => OnVirtualCellRelease?.Invoke(cx, cy, cw, ch, sizePx);
}
