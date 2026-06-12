namespace WallProjector.Web;

/// <summary>
/// 旋轉 CSS 數學的單一真相源——wrap 頁（iframe 容器）、media 頁（影片/PDF/圖片）、
/// CDP 注入（html 元素）三條路徑共用同一組宣告，保證旋轉行為一致。
///
/// 數學（rotation = 順時針度數；視窗 W×H）：
/// - 0：100vw×100vh，無 transform。
/// - 90：**100vh×100vw（寬高對調）**、origin top left、
///   `rotate(90deg) translateY(-100vw)`——先在局部座標上移 100vw 再順時針轉 90°，
///   旋轉後 bounding box 恰好等於視窗（推導見 README；單元測試釘住字串）。
/// - 180：100vw×100vh、`rotate(180deg)`（預設 origin center，原地翻轉）。
/// - 270：100vh×100vw、origin top left、`rotate(270deg) translateX(-100vh)`。
/// </summary>
public static class RotationCss
{
    /// <summary>單一 rotation 的 CSS 宣告組（Origin / Transform 為 null = 不需要該宣告）。</summary>
    public readonly record struct Decl(string Width, string Height, string? Origin, string? Transform);

    public static Decl For(int rotation) => rotation switch
    {
        90 => new("100vh", "100vw", "top left", "rotate(90deg) translateY(-100vw)"),
        180 => new("100vw", "100vh", null, "rotate(180deg)"),
        270 => new("100vh", "100vw", "top left", "rotate(270deg) translateX(-100vh)"),
        _ => new("100vw", "100vh", null, null),
    };

    /// <summary>stage 容器的 inline CSS（wrap 頁 / media 頁共用；字串被單元測試釘住）。</summary>
    public static string StageCss(int rotation)
    {
        var d = For(rotation);
        string css = $"width:{d.Width};height:{d.Height};";
        if (d.Origin != null) css += $"transform-origin:{d.Origin};";
        if (d.Transform != null) css += $"transform:{d.Transform};";
        return css;
    }
}
