using WallProjector.Engine;
using WallProjector.Web;
using Xunit;

namespace WallProjector.Tests;

/// <summary>
/// CDP 注入 script 生成：四種 rotation 的 JS/CSS 輸出 golden——
/// 宣告值必須跟 wrap 頁 stage CSS（RotationCss）逐字同源，旋轉數學只有一份。
/// </summary>
public class CdpInjectorTests
{
    [Fact]
    public void rotation90_寬高對調_origin_topleft_translateY()
    {
        var js = CdpInjector.BuildRotationScript(90);
        Assert.Contains("el.style.setProperty('width','100vh','important');", js);
        Assert.Contains("el.style.setProperty('height','100vw','important');", js);
        Assert.Contains("el.style.setProperty('transform-origin','top left','important');", js);
        Assert.Contains("el.style.setProperty('transform','rotate(90deg) translateY(-100vw)','important');", js);
        Assert.Contains("el.setAttribute('data-wp-rotation','90');", js);
    }

    [Fact]
    public void rotation180_原地翻轉_不對調寬高_無origin()
    {
        var js = CdpInjector.BuildRotationScript(180);
        Assert.Contains("el.style.setProperty('width','100vw','important');", js);
        Assert.Contains("el.style.setProperty('height','100vh','important');", js);
        Assert.Contains("el.style.removeProperty('transform-origin');", js);
        Assert.Contains("el.style.setProperty('transform','rotate(180deg)','important');", js);
        Assert.Contains("el.setAttribute('data-wp-rotation','180');", js);
    }

    [Fact]
    public void rotation270_寬高對調_translateX()
    {
        var js = CdpInjector.BuildRotationScript(270);
        Assert.Contains("el.style.setProperty('width','100vh','important');", js);
        Assert.Contains("el.style.setProperty('height','100vw','important');", js);
        Assert.Contains("el.style.setProperty('transform-origin','top left','important');", js);
        Assert.Contains("el.style.setProperty('transform','rotate(270deg) translateX(-100vh)','important');", js);
    }

    [Fact]
    public void rotation0_無transform_有removeProperty保險()
    {
        // rotation=0 引擎不會注入（spec：只開 URL）；但 script 本身也必須無害——
        // 寬高滿版 + 清掉 transform（防呆，例如手動呼叫）
        var js = CdpInjector.BuildRotationScript(0);
        Assert.Contains("el.style.setProperty('width','100vw','important');", js);
        Assert.Contains("el.style.setProperty('height','100vh','important');", js);
        Assert.Contains("el.style.removeProperty('transform');", js);
        Assert.DoesNotContain("rotate(", js);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public void script結構_documentElement防呆_DOMContentLoaded補打(int rotation)
    {
        var js = CdpInjector.BuildRotationScript(rotation);
        // addScriptToEvaluateOnNewDocument 跑在 document 起點，documentElement 可能還沒有
        Assert.Contains("if (!el) return;", js);
        Assert.Contains("document.addEventListener('DOMContentLoaded', apply);", js);
        Assert.Contains("document.readyState === 'loading'", js);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public void 注入宣告與wrap頁stageCss同源(int rotation)
    {
        // RotationCss 是唯一真相源：wrap 頁 stage 與 CDP 注入的宣告值必須一致
        var d = RotationCss.For(rotation);
        var js = CdpInjector.BuildRotationScript(rotation);
        var stage = RotationCss.StageCss(rotation);

        Assert.Contains($"'{d.Width}'", js);
        Assert.Contains($"width:{d.Width};", stage);
        if (d.Transform != null)
        {
            Assert.Contains($"'{d.Transform}'", js);
            Assert.Contains($"transform:{d.Transform};", stage);
        }
    }

    [Fact]
    public void stageCss_四種rotation_golden字串不變()
    {
        // V1 wrap 頁釘住的字串——抽到 RotationCss 後必須 byte-identical
        Assert.Equal("width:100vw;height:100vh;", RotationCss.StageCss(0));
        Assert.Equal("width:100vh;height:100vw;transform-origin:top left;" +
                     "transform:rotate(90deg) translateY(-100vw);", RotationCss.StageCss(90));
        Assert.Equal("width:100vw;height:100vh;transform:rotate(180deg);", RotationCss.StageCss(180));
        Assert.Equal("width:100vh;height:100vw;transform-origin:top left;" +
                     "transform:rotate(270deg) translateX(-100vh);", RotationCss.StageCss(270));
    }
}
