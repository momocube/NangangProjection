using WallProjector.Web;
using Xunit;

namespace WallProjector.Tests;

/// <summary>
/// wrap 頁生成：四種 rotation 的 transform / 寬高對調、URL escape、
/// slot 標記（視窗對位依據）、封鎖錯誤頁。
/// </summary>
public class WrapPageBuilderTests
{
    [Fact]
    public void rotation0_無transform_容器滿版()
    {
        var html = WrapPageBuilder.Build(2, "Floor", "https://example.com/", 0);
        Assert.Contains("width:100vw;height:100vh;", html);
        Assert.DoesNotContain("transform:rotate", html);
        Assert.Contains("""<iframe id="frame" src="https://example.com/" """, html);
    }

    [Fact]
    public void rotation90_寬高對調_順時針轉並平移回視窗()
    {
        var html = WrapPageBuilder.Build(4, "Wall Right Big", "https://example.com/", 90);
        // 寬高對調：容器 100vh×100vw
        Assert.Contains("width:100vh;height:100vw;", html);
        // transform-origin 必須是 top left，先 translateY(-100vw) 再 rotate(90deg)
        Assert.Contains("transform-origin:top left;", html);
        Assert.Contains("transform:rotate(90deg) translateY(-100vw);", html);
    }

    [Fact]
    public void rotation180_原地翻轉_不對調寬高()
    {
        var html = WrapPageBuilder.Build(1, "Wall Button", "https://example.com/", 180);
        Assert.Contains("width:100vw;height:100vh;transform:rotate(180deg);", html);
        Assert.DoesNotContain("100vh;height:100vw", html);
    }

    [Fact]
    public void rotation270_寬高對調_逆時針轉()
    {
        var html = WrapPageBuilder.Build(0, "Wall Left", "https://example.com/", 270);
        Assert.Contains("width:100vh;height:100vw;", html);
        Assert.Contains("transform:rotate(270deg) translateX(-100vh);", html);
    }

    [Fact]
    public void title內嵌slot標記_供EnumWindows比對()
    {
        var html = WrapPageBuilder.Build(4, "Wall Right Big", "https://example.com/", 90);
        Assert.Contains("[WP-slot-4]", html);
        Assert.Equal("[WP-slot-4]", WrapPageBuilder.TitleMarker(4));
    }

    [Fact]
    public void url經HTML_escape_不能注入()
    {
        var html = WrapPageBuilder.Build(0, "Wall Left",
            "https://example.com/?a=1&b=\"><script>alert(1)</script>", 0);
        Assert.DoesNotContain("<script>alert", html);
        Assert.Contains("&amp;b=&quot;&gt;&lt;script&gt;", html);
    }

    [Fact]
    public void 封鎖時_顯示明確錯誤頁_無iframe_保留旋轉()
    {
        var html = WrapPageBuilder.Build(0, "Wall Left", "https://blocked.example/", 270,
            blockReason: "X-Frame-Options: DENY（一律拒絕嵌入）");
        Assert.DoesNotContain("<iframe", html);
        Assert.Contains("目標網站拒絕被嵌入", html);
        Assert.Contains("X-Frame-Options: DENY", html);
        Assert.Contains("CDP", html);   // v2 路線提示
        Assert.Contains("transform:rotate(270deg)", html);   // 錯誤訊息也轉正，現場直接讀得到
    }

    [Fact]
    public void 未設定槽_占位頁也帶slot標記()
    {
        var html = WrapPageBuilder.BuildNotConfigured(3);
        Assert.Contains("[WP-slot-3]", html);
        Assert.Contains("尚未設定", html);
    }
}
