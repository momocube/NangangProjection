using WallProjector.Core;

namespace WallProjector.Web;

/// <summary>
/// iframe 嵌入封鎖預檢：wrap 頁 render 前對目標 URL 做一次 GET（只讀 headers），
/// 看 X-Frame-Options / CSP frame-ancestors 是否擋我們嵌入。
///
/// 為什麼 server 端檢查：iframe 被 XFO 擋下時瀏覽器照樣 fire load 事件、
/// cross-origin 下 JS 無法區分「載入成功」與「被擋」——client 端偵測不可靠，
/// header 預檢是 V1 唯一確定性的判斷。檢查本身 best-effort：逾時/連線失敗回 null
/// （照常 render iframe，讓瀏覽器自己呈現網路錯誤）。
/// </summary>
public static class FrameBlockChecker
{
    /// <summary>回 null = 沒發現封鎖；非 null = 封鎖原因（人話）。</summary>
    public static string? Check(string url, string ourOrigin, TimeSpan timeout, Logger? log = null)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
                return null;   // 非 http(s)（如 file:）不檢查

            using var http = new HttpClient { Timeout = timeout };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("WallProjector/1.0");
            using var resp = http.Send(
                new HttpRequestMessage(HttpMethod.Get, uri),
                HttpCompletionOption.ResponseHeadersRead);

            string? xfo = resp.Headers.TryGetValues("X-Frame-Options", out var xv)
                ? xv.FirstOrDefault() : null;
            string? csp = resp.Content.Headers.TryGetValues("Content-Security-Policy", out var cv1)
                ? cv1.FirstOrDefault()
                : resp.Headers.TryGetValues("Content-Security-Policy", out var cv2)
                    ? cv2.FirstOrDefault() : null;

            return EvaluateHeaders(xfo, csp, $"{uri.Scheme}://{uri.Authority}", ourOrigin);
        }
        catch (Exception ex)
        {
            log?.Debug($"[FrameCheck] 預檢失敗（{url}）：{ex.Message}——照常 render iframe");
            return null;   // 預檢失敗不擋 render
        }
    }

    /// <summary>
    /// header 判斷邏輯（純函式，單元測試對象）。
    /// ourOrigin = wrap 頁 origin（http://127.0.0.1:port）；targetOrigin = 目標站 origin。
    /// </summary>
    public static string? EvaluateHeaders(string? xfo, string? csp, string targetOrigin, string ourOrigin)
    {
        // CSP frame-ancestors 優先（規格上它覆蓋 XFO）
        if (csp != null)
        {
            foreach (var directive in csp.Split(';'))
            {
                var d = directive.Trim();
                if (!d.StartsWith("frame-ancestors", StringComparison.OrdinalIgnoreCase)) continue;

                var sources = d["frame-ancestors".Length..]
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var s in sources)
                {
                    var src = s.Trim('\'', '"');
                    if (src == "*") return null;
                    if (src.Equals(ourOrigin, StringComparison.OrdinalIgnoreCase)) return null;
                    // scheme 萬用（http: / https:）視為允許
                    if ((src is "http:" or "https:") &&
                        ourOrigin.StartsWith(src, StringComparison.OrdinalIgnoreCase)) return null;
                }
                return $"CSP frame-ancestors 未允許 {ourOrigin}（值：{string.Join(' ', sources)}）";
            }
        }

        if (xfo != null)
        {
            var v = xfo.Trim();
            if (v.Equals("DENY", StringComparison.OrdinalIgnoreCase))
                return "X-Frame-Options: DENY（一律拒絕嵌入）";
            if (v.StartsWith("SAMEORIGIN", StringComparison.OrdinalIgnoreCase) &&
                !targetOrigin.Equals(ourOrigin, StringComparison.OrdinalIgnoreCase))
                return $"X-Frame-Options: SAMEORIGIN（僅允許 {targetOrigin} 自己嵌入）";
        }

        return null;
    }
}
