using WallProjector.Web;

namespace WallProjector.Engine;

/// <summary>
/// CDP 模式的旋轉注入：直接開目標站分頁（不經 wrap 頁 iframe，繞過
/// X-Frame-Options / CSP frame-ancestors），把旋轉 CSS 打在 `html` 元素上。
///
/// 數學與 wrap 頁同一套（<see cref="RotationCss"/>）：90/270 寬高對調 +
/// transform-origin top left + translate；180 原地翻轉。差別只在套用對象——
/// wrap 頁套在 #stage 容器、這裡直接套 documentElement（頁面就是目標站本人）。
///
/// 導航存活：`Page.addScriptToEvaluateOnNewDocument` 讓 SPA 跳轉 / 連結點擊後
/// 的**每個新 document** 都在建立時重打一次樣式；當前已載入的頁面另以
/// `Runtime.evaluate` 立即補打。rotation=0 不注入（呼叫端直接跳過）。
/// </summary>
public static class CdpInjector
{
    /// <summary>
    /// 生成注入 JS（純函式，golden 測試對象）。
    /// - setProperty 帶 'important'：壓過目標站自己的 html 樣式
    /// - apply() 防 documentElement 尚未建立（addScriptToEvaluateOnNewDocument
    ///   跑在 document 起點）——loading 中掛 DOMContentLoaded 再補打一次
    /// - data-wp-rotation 標記：可重入（evaluate 與 addScript 重複打也無害），
    ///   也供 Runtime.evaluate 驗證注入已生效
    /// </summary>
    public static string BuildRotationScript(int rotation)
    {
        var d = RotationCss.For(rotation);
        string origin = d.Origin != null
            ? $"el.style.setProperty('transform-origin','{d.Origin}','important');"
            : "el.style.removeProperty('transform-origin');";
        string transform = d.Transform != null
            ? $"el.style.setProperty('transform','{d.Transform}','important');"
            : "el.style.removeProperty('transform');";

        return $$"""
            (function () {
              var apply = function () {
                var el = document.documentElement;
                if (!el) return;
                el.style.setProperty('width','{{d.Width}}','important');
                el.style.setProperty('height','{{d.Height}}','important');
                {{origin}}
                {{transform}}
                el.setAttribute('data-wp-rotation','{{rotation}}');
              };
              apply();
              if (document.readyState === 'loading')
                document.addEventListener('DOMContentLoaded', apply);
            })();
            """;
    }

    /// <summary>
    /// 對已連線的 page target 注入旋轉（rotation=0 為 no-op）。
    /// 任何一步失敗都往外丟——呼叫端轉成槽的明確錯誤狀態，不靜默。
    /// </summary>
    public static async Task InjectRotationAsync(CdpClient cdp, int rotation,
                                                 CancellationToken ct = default)
    {
        if (rotation == 0) return;
        var script = BuildRotationScript(rotation);
        await cdp.SendAsync("Page.enable", null, ct);
        await cdp.SendAsync("Page.addScriptToEvaluateOnNewDocument", new { source = script }, ct);
        await cdp.SendAsync("Runtime.evaluate", new { expression = script }, ct);
    }

    /// <summary>
    /// 生成 title 標記注入 JS（純函式，golden 測試對象）。
    ///
    /// 為什麼需要：CDP 模式視窗 title 是目標站自己的（沒有 [WP-slot-N]），對位本來
    /// 只剩 pid 比對——但 Edge 可能有 launcher 行為（視窗實際屬 child process），
    /// pid 比對失敗時視窗就停在 --window-position 的 DIP 粗對位上。把標記注進
    /// document.title 後，EnumWindows 的 title 比對對 CDP 模式也有效（視窗無邊框，
    /// title 使用者看不到）。
    ///
    /// 為什麼不能只 append 一次：目標站（YouTube 等 SPA）載入完與站內導航都會
    /// **重設 document.title**，一次性 append 馬上被洗掉——所以掛 1s 的守護
    /// interval 補回（檢查不在才補，可重入、無限重打也無害）。
    /// </summary>
    public static string BuildTitleMarkerScript(string marker)
    {
        var js = JsEscape(marker);
        return $$"""
            (function () {
              var m = '{{js}}';
              var apply = function () {
                try {
                  if (document.title.indexOf(m) === -1) document.title = document.title + ' ' + m;
                } catch (e) {}
              };
              apply();
              if (document.readyState === 'loading')
                document.addEventListener('DOMContentLoaded', apply);
              setInterval(apply, 1000);
            })();
            """;
    }

    /// <summary>
    /// 對已連線的 page target 注入 title 標記：addScriptToEvaluateOnNewDocument
    /// 讓導航後的新 document 也帶標記 + Runtime.evaluate 立即打在當前頁。
    /// 失敗往外丟——呼叫端決定嚴重性（對位還有 pid 備援，通常只 WARN 不致命）。
    /// </summary>
    public static async Task InjectTitleMarkerAsync(CdpClient cdp, string marker,
                                                    CancellationToken ct = default)
    {
        var script = BuildTitleMarkerScript(marker);
        await cdp.SendAsync("Page.enable", null, ct);
        await cdp.SendAsync("Page.addScriptToEvaluateOnNewDocument", new { source = script }, ct);
        await cdp.SendAsync("Runtime.evaluate", new { expression = script }, ct);
    }

    /// <summary>單引號 JS 字串的最小跳脫（marker 目前是 [WP-slot-N]，防未來變動）。</summary>
    static string JsEscape(string s) =>
        s.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
}
