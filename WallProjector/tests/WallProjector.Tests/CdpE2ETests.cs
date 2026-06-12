using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using WallProjector.Engine;
using Xunit;

namespace WallProjector.Tests;

/// <summary>
/// CDP client 端到端：**真 chromium**（headless）開 --remote-debugging-port →
/// CdpClient 連線 → 注入旋轉 → Runtime.evaluate 查 getComputedStyle(html).transform
/// 驗證矩陣值 → Page.navigate 後驗證旋轉存活（addScriptToEvaluateOnNewDocument）。
///
/// 找不到瀏覽器的環境（純 CI 等）自動 Skip（訊息可見）——開發機跑是真 E2E。
///
/// 矩陣推導（CSS transform list 右乘；y 軸向下；W/H = 100vw/100vh 的 px 值）：
///   rotate(90deg) translateY(-100vw)  → matrix(0, 1, -1, 0, W, 0)
///   rotate(180deg)                    → matrix(-1, 0, 0, -1, 0, 0)
///   rotate(270deg) translateX(-100vh) → matrix(0, -1, 1, 0, 0, H)
/// 100vw/100vh 用 probe 元素實測（不用 innerWidth——classic scrollbar 出現時兩者會差
/// 15px，vw 規格上假設 root scrollbar 不存在）。
/// </summary>
public class CdpE2ETests : IDisposable
{
    static readonly string[] BrowserCandidates =
    {
        "/usr/bin/google-chrome", "/usr/bin/chromium", "/usr/bin/chromium-browser",
        "/usr/bin/microsoft-edge", "/snap/bin/chromium",
    };

    readonly string _profileDir = Directory.CreateTempSubdirectory("wp-cdp-e2e").FullName;
    Process? _browser;

    static string? FindBrowser() => BrowserCandidates.FirstOrDefault(File.Exists);

    static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    int LaunchHeadless(string browserExe, string url)
    {
        int port = FreePort();
        var psi = new ProcessStartInfo(browserExe,
            $"--headless=new --no-sandbox --disable-gpu --remote-debugging-port={port} " +
            $"--user-data-dir=\"{_profileDir}\" --no-first-run --no-default-browser-check " +
            $"--window-size=800,600 \"{url}\"")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        _browser = Process.Start(psi)!;
        return port;
    }

    sealed record Probe(string Transform, double VwPx, double VhPx, string Marker);

    static async Task<Probe> QueryRotation(CdpClient cdp)
    {
        var result = await cdp.SendAsync("Runtime.evaluate", new
        {
            expression = """
                (() => {
                  const p = document.createElement('div');
                  p.style.cssText = 'position:fixed;left:0;top:0;width:100vw;height:100vh;visibility:hidden;pointer-events:none;';
                  (document.documentElement || document).appendChild(p);
                  const r = p.getBoundingClientRect();
                  p.remove();
                  return JSON.stringify({
                    t: getComputedStyle(document.documentElement).transform,
                    vw: r.width, vh: r.height,
                    m: document.documentElement.getAttribute('data-wp-rotation') || ''
                  });
                })()
                """,
            returnByValue = true,
        });
        using var doc = JsonDocument.Parse(result.GetProperty("result").GetProperty("value").GetString()!);
        return new Probe(doc.RootElement.GetProperty("t").GetString()!,
                         doc.RootElement.GetProperty("vw").GetDouble(),
                         doc.RootElement.GetProperty("vh").GetDouble(),
                         doc.RootElement.GetProperty("m").GetString()!);
    }

    /// <summary>"matrix(a, b, c, d, e, f)" → 6 doubles。</summary>
    static double[] ParseMatrix(string transform)
    {
        Assert.StartsWith("matrix(", transform);
        return transform["matrix(".Length..^1]
            .Split(',', StringSplitOptions.TrimEntries)
            .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
            .ToArray();
    }

    static void AssertMatrix(int rotation, Probe p)
    {
        // probe 的 getBoundingClientRect 在 html 已被 transform 時量到的是轉換後座標——
        // 但 100vw/100vh 是 viewport 單位、與元素 transform 無關；rect 寬高在 90/270 時
        // 仍是局部尺寸經旋轉的 bounding box（寬高互換）。取兩者較大/較小值穩定對應。
        double vw = Math.Max(p.VwPx, p.VhPx);   // viewport 較長邊 = 100vw（800×600 視窗）
        double vh = Math.Min(p.VwPx, p.VhPx);
        var m = ParseMatrix(p.Transform);
        const double tol = 0.5;
        switch (rotation)
        {
            case 90:
                Assert.Equal(new[] { 0.0, 1.0, -1.0, 0.0 }, m[..4].Select(v => Math.Round(v, 3)));
                Assert.Equal(vw, m[4], tol);
                Assert.Equal(0.0, m[5], tol);
                break;
            case 180:
                Assert.Equal(new[] { -1.0, 0.0, 0.0, -1.0 }, m[..4].Select(v => Math.Round(v, 3)));
                Assert.Equal(0.0, m[4], tol);
                Assert.Equal(0.0, m[5], tol);
                break;
            case 270:
                Assert.Equal(new[] { 0.0, -1.0, 1.0, 0.0 }, m[..4].Select(v => Math.Round(v, 3)));
                Assert.Equal(0.0, m[4], tol);
                Assert.Equal(vh, m[5], tol);
                break;
        }
    }

    [SkippableTheory]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public async Task 真chromium_注入旋轉_computedStyle矩陣正確(int rotation)
    {
        var browserExe = FindBrowser();
        Skip.If(browserExe == null, "此環境沒有 chromium/chrome——CDP E2E 需要真瀏覽器");

        int port = LaunchHeadless(browserExe!, "about:blank");
        await using var cdp = await CdpClient.ConnectAsync(port, TimeSpan.FromSeconds(20));
        await CdpInjector.InjectRotationAsync(cdp, rotation);

        var probe = await QueryRotation(cdp);
        Assert.Equal(rotation.ToString(), probe.Marker);
        AssertMatrix(rotation, probe);
    }

    [SkippableFact]
    public async Task 真chromium_導航後旋轉存活_addScriptToEvaluateOnNewDocument()
    {
        var browserExe = FindBrowser();
        Skip.If(browserExe == null, "此環境沒有 chromium/chrome——CDP E2E 需要真瀏覽器");

        int port = LaunchHeadless(browserExe!, "about:blank");
        await using var cdp = await CdpClient.ConnectAsync(port, TimeSpan.FromSeconds(20));
        await CdpInjector.InjectRotationAsync(cdp, 90);

        // SPA 跳轉/連結點擊的等價物：導去全新 document（data: URL，不需網路）
        await cdp.SendAsync("Page.navigate", new { url = "data:text/html,<h1>after-nav</h1>" });

        // 新 document 的旋轉由 addScriptToEvaluateOnNewDocument 負責——輪詢等生效
        Probe? probe = null;
        for (int i = 0; i < 25; i++)
        {
            await Task.Delay(200);
            var q = await QueryRotation(cdp);
            if (q.Marker == "90") { probe = q; break; }
        }
        Assert.NotNull(probe);
        AssertMatrix(90, probe!);
    }

    [SkippableFact]
    public async Task 真chromium_rotation0不注入_transform為none()
    {
        var browserExe = FindBrowser();
        Skip.If(browserExe == null, "此環境沒有 chromium/chrome——CDP E2E 需要真瀏覽器");

        int port = LaunchHeadless(browserExe!, "about:blank");
        await using var cdp = await CdpClient.ConnectAsync(port, TimeSpan.FromSeconds(20));
        await CdpInjector.InjectRotationAsync(cdp, 0);   // spec：rotation=0 不注入（no-op）

        var probe = await QueryRotation(cdp);
        Assert.Equal("none", probe.Transform);
        Assert.Equal("", probe.Marker);
    }

    [Fact]
    public async Task port沒起來_明確TimeoutException不靜默()
    {
        int deadPort = FreePort();   // 沒有任何東西聽這個 port
        var ex = await Assert.ThrowsAsync<TimeoutException>(
            () => CdpClient.ConnectAsync(deadPort, TimeSpan.FromSeconds(2)));
        Assert.Contains(deadPort.ToString(), ex.Message);
        Assert.Contains("page target", ex.Message);
    }

    public void Dispose()
    {
        try
        {
            if (_browser is { HasExited: false }) _browser.Kill(entireProcessTree: true);
            _browser?.Dispose();
        }
        catch { /* ignore */ }
        try { Directory.Delete(_profileDir, recursive: true); } catch { /* ignore */ }
    }
}
