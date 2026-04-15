/// <summary>
/// 感應事件中轉站（靜態 Event Bus）。
///
/// 統一「左鍵點擊控制格」與「真實 UDP 感應器」兩條路，
/// 讓任何遊戲場景只需訂閱這一個事件即可接收觸碰座標。
///
/// 廣播格子座標（col / row），而非原始感應器座標，
/// 座標已由 SensorTestManager 完成校正與格子對齊。
/// </summary>
public static class SensorBus
{
    /// <summary>
    /// 有格子被觸碰時廣播（已去除鎖定格子）。
    /// col: 0 ~ 127（欄），row: 0 ~ 23（列）
    /// </summary>
    public static event System.Action<int, int> OnTouch;

    /// <summary>由 SensorTestManager 呼叫，不要在其他地方呼叫。</summary>
    public static void Touch(int col, int row) => OnTouch?.Invoke(col, row);
}
