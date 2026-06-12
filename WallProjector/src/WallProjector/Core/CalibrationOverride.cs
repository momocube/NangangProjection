using WallProjector.Config;

namespace WallProjector.Core;

/// <summary>
/// screenOverride / contentOverride 逃生口：TouchService 的 mouseMode.screen/content
/// 是**為滑鼠校的**——若該機器上滑鼠空間與視窗擺放空間不一致（例如歷史校正殘值、
/// 或滑鼠校正刻意偏移），現場可以只在 projections.json 覆寫投影用的值，
/// **不動 TouchService 的滑鼠校正**。null = 沿用 TouchService（現行為，預設）。
///
/// 包在 Resolve 外層（Program 組裝處呼叫）——TouchServiceClient 保持純粹，
/// live / cache / fallback 失效鏈完全不知道 override 的存在。
/// </summary>
public static class CalibrationOverride
{
    /// <summary>套用 config 的 override；兩者皆 null 時原樣回傳（零成本）。</summary>
    public static TouchServiceState Apply(TouchServiceState ts, ProjectorConfig config, Logger? log = null)
    {
        var s = config.ScreenOverride;
        var c = config.ContentOverride;
        if (s == null && c == null) return ts;

        // 覆寫是刻意的逃生口，但容易被忘在 config 裡——每次 resolve 都 WARN 提醒
        if (s != null)
            log?.Warn($"[Override] 投影 screen 使用 projections.json 的 screenOverride：" +
                      $"({s.X},{s.Y}) {s.Width}x{s.Height}——TouchService 的 mouseMode.screen 被忽略");
        if (c != null)
            log?.Warn($"[Override] 投影 content 使用 projections.json 的 contentOverride：" +
                      $"offset({c.OffsetX},{c.OffsetY}) scale({c.ScaleX},{c.ScaleY})" +
                      "——TouchService 的 mouseMode.content 被忽略");

        return new TouchServiceState
        {
            Venue = ts.Venue,
            Screen = s ?? ts.Screen,
            Content = c ?? ts.Content,
            Source = ts.Source,
            Reachable = ts.Reachable,
            Warning = ts.Warning,
            ScreenSource = s != null ? "override" : "touchservice",
            ContentSource = c != null ? "override" : "touchservice",
        };
    }
}
