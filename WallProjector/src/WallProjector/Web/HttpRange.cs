namespace WallProjector.Web;

/// <summary>
/// HTTP Range header 解析（影片 seek 必需——瀏覽器對 &lt;video&gt; 用 Range 抓分段）。
///
/// 支援單一 range 的三種寫法（RFC 9110 §14）：
///   bytes=a-b（含端點）/ bytes=a-（到檔尾）/ bytes=-n（檔尾倒數 n bytes）
///
/// 寬鬆面（server MAY ignore Range → 200 全檔，永遠合法）：
/// - 沒有 Range header / 非 bytes 單位 / 多段 range（瀏覽器播影片不會送）→ Full
/// 嚴格面：
/// - 語法是單段 bytes 但不可滿足（起點 ≥ 檔長、a&gt;b、空 spec）→ Unsatisfiable（416）
/// </summary>
public static class HttpRange
{
    public enum Kind { Full, Partial, Unsatisfiable }

    public readonly record struct Result(Kind Kind, long Offset, long Length);

    public static Result Parse(string? header, long fileLength)
    {
        if (string.IsNullOrWhiteSpace(header))
            return new(Kind.Full, 0, fileLength);

        var h = header.Trim();
        if (!h.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            return new(Kind.Full, 0, fileLength);   // 非 bytes 單位：忽略 Range

        var spec = h["bytes=".Length..].Trim();
        if (spec.Contains(','))
            return new(Kind.Full, 0, fileLength);   // 多段 range：忽略（回 200 全檔合法）

        int dash = spec.IndexOf('-');
        if (dash < 0) return new(Kind.Unsatisfiable, 0, 0);

        string fromStr = spec[..dash].Trim();
        string toStr = spec[(dash + 1)..].Trim();

        if (fromStr.Length == 0)
        {
            // bytes=-n：檔尾倒數 n bytes
            if (!long.TryParse(toStr, out long suffix) || suffix <= 0)
                return new(Kind.Unsatisfiable, 0, 0);
            if (fileLength == 0) return new(Kind.Unsatisfiable, 0, 0);
            long len = Math.Min(suffix, fileLength);
            return new(Kind.Partial, fileLength - len, len);
        }

        if (!long.TryParse(fromStr, out long from) || from < 0)
            return new(Kind.Unsatisfiable, 0, 0);
        if (from >= fileLength)
            return new(Kind.Unsatisfiable, 0, 0);

        if (toStr.Length == 0)
            return new(Kind.Partial, from, fileLength - from);   // bytes=a-

        if (!long.TryParse(toStr, out long to) || to < from)
            return new(Kind.Unsatisfiable, 0, 0);
        long end = Math.Min(to, fileLength - 1);
        return new(Kind.Partial, from, end - from + 1);
    }
}
