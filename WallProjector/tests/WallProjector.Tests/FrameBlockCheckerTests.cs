using WallProjector.Web;
using Xunit;

namespace WallProjector.Tests;

/// <summary>iframe 封鎖預檢的 header 判斷（純函式；網路部分 best-effort 不在單元測試範圍）。</summary>
public class FrameBlockCheckerTests
{
    const string Us = "http://127.0.0.1:20121";

    [Fact]
    public void 無header_不封鎖()
    {
        Assert.Null(FrameBlockChecker.EvaluateHeaders(null, null, "https://example.com", Us));
    }

    [Fact]
    public void XFO_DENY_封鎖()
    {
        var r = FrameBlockChecker.EvaluateHeaders("DENY", null, "https://example.com", Us);
        Assert.NotNull(r);
        Assert.Contains("DENY", r);
    }

    [Fact]
    public void XFO_SAMEORIGIN_跨origin封鎖_同origin放行()
    {
        Assert.NotNull(FrameBlockChecker.EvaluateHeaders("SAMEORIGIN", null, "https://example.com", Us));
        // 目標就是我們自己（同 origin）→ 放行
        Assert.Null(FrameBlockChecker.EvaluateHeaders("SAMEORIGIN", null, Us, Us));
    }

    [Fact]
    public void CSP_frame_ancestors_none_封鎖()
    {
        var r = FrameBlockChecker.EvaluateHeaders(null,
            "default-src 'self'; frame-ancestors 'none'", "https://example.com", Us);
        Assert.NotNull(r);
        Assert.Contains("frame-ancestors", r);
    }

    [Fact]
    public void CSP_frame_ancestors_星號或我們的origin_放行()
    {
        Assert.Null(FrameBlockChecker.EvaluateHeaders(null,
            "frame-ancestors *", "https://example.com", Us));
        Assert.Null(FrameBlockChecker.EvaluateHeaders(null,
            $"frame-ancestors 'self' {Us}", "https://example.com", Us));
        Assert.Null(FrameBlockChecker.EvaluateHeaders(null,
            "frame-ancestors http:", "https://example.com", Us));   // scheme 萬用
    }

    [Fact]
    public void CSP優先於XFO()
    {
        // CSP 允許時 XFO 不再看（規格：frame-ancestors 存在則覆蓋 XFO）
        Assert.Null(FrameBlockChecker.EvaluateHeaders("DENY",
            "frame-ancestors *", "https://example.com", Us));
    }

    [Fact]
    public void 非http_scheme_不檢查不丟錯()
    {
        Assert.Null(FrameBlockChecker.Check("file:///tmp/x.html", Us, TimeSpan.FromSeconds(1)));
        Assert.Null(FrameBlockChecker.Check("not a url", Us, TimeSpan.FromSeconds(1)));
    }
}
