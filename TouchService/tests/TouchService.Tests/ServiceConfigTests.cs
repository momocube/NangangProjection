using TouchService.Config;
using Xunit;

namespace TouchService.Tests;

public class ServiceConfigTests
{
    [Fact]
    public void 缺檔時生成範本_並回傳預設值()
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-cfg").FullName;
        var path = Path.Combine(dir, "config.json");

        var cfg = ServiceConfig.LoadOrCreate(path);

        Assert.True(File.Exists(path), "缺檔時必須生成範本");
        Assert.Equal(20001, cfg.ListenPort);
        Assert.Equal(170, cfg.ReleaseTimeoutMs);
        Assert.Equal(96f, cfg.MatchRadiusPx);
        Assert.Equal(128, cfg.MaxSlots);
        Assert.True(cfg.UdpOutput.Enabled);
        Assert.True(cfg.WebSocketOutput.Enabled);
        Assert.Equal(20111, cfg.WebSocketOutput.Port);
        Assert.False(cfg.TuioOutput.Enabled);   // TUIO v1 預留插槽，預設關閉

        // 範本本身可以再被讀回（round-trip）
        var again = ServiceConfig.LoadOrCreate(path);
        Assert.Equal(cfg.ReleaseTimeoutMs, again.ReleaseTimeoutMs);
    }

    [Fact]
    public void 部分欄位缺漏_套用預設值()
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-cfg2").FullName;
        var path = Path.Combine(dir, "config.json");
        File.WriteAllText(path, """{ "listenPort": 30001 }""");

        var cfg = ServiceConfig.LoadOrCreate(path);
        Assert.Equal(30001, cfg.ListenPort);
        Assert.Equal(170, cfg.ReleaseTimeoutMs);     // 未指定 → 預設
        Assert.Equal("venues/704.json", cfg.VenueProfile);
    }

    [Fact]
    public void 倉庫內的config範本_可正常解析()
    {
        // TouchService/config.json（部署用範本）必須永遠可被服務讀取
        var repoConfig = FindRepoFile("config.json");
        var cfg = ServiceConfig.LoadOrCreate(repoConfig);
        Assert.Equal(20001, cfg.ListenPort);
        Assert.Equal("venues/704.json", cfg.VenueProfile);
        Assert.Equal(170, cfg.ReleaseTimeoutMs);
    }

    [Fact]
    public void 觸控模式與UI欄位_預設值與null容錯()
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-cfg3").FullName;
        var path = Path.Combine(dir, "config.json");

        // 缺欄位 → 預設：grid / mouseMode 預設 / ui.autoLaunch=true
        File.WriteAllText(path, """{ "listenPort": 30001 }""");
        var cfg = ServiceConfig.LoadOrCreate(path);
        Assert.Equal("grid", cfg.TouchMode);
        Assert.Equal(0, cfg.MouseMode.Face);
        Assert.Equal(1920, cfg.MouseMode.Screen.Width);
        Assert.Equal(1080, cfg.MouseMode.Screen.Height);
        Assert.Equal(0.35, cfg.MouseMode.Smoothing, 3);
        Assert.Equal(2, cfg.MouseMode.MoveMinPx);
        Assert.True(cfg.Ui.AutoLaunch);

        // 顯式 null 容錯
        File.WriteAllText(path, """{ "touchMode": null, "mouseMode": null, "ui": null }""");
        cfg = ServiceConfig.LoadOrCreate(path);
        Assert.Equal("grid", cfg.TouchMode);
        Assert.NotNull(cfg.MouseMode);
        Assert.True(cfg.Ui.AutoLaunch);

        // 大小寫 / 空白正規化
        File.WriteAllText(path, """{ "touchMode": " Mouse " }""");
        Assert.Equal("mouse", ServiceConfig.LoadOrCreate(path).TouchMode);

        // 完整欄位 roundtrip
        File.WriteAllText(path, """
            { "touchMode": "mouse",
              "mouseMode": { "face": 4, "screen": { "x": -1920, "y": 0, "width": 2560, "height": 1440 },
                             "smoothing": 0.5, "moveMinPx": 4 },
              "ui": { "autoLaunch": false } }
            """);
        cfg = ServiceConfig.LoadOrCreate(path);
        Assert.Equal("mouse", cfg.TouchMode);
        Assert.Equal(4, cfg.MouseMode.Face);
        Assert.Equal(-1920, cfg.MouseMode.Screen.X);
        Assert.Equal(2560, cfg.MouseMode.Screen.Width);
        Assert.Equal(0.5, cfg.MouseMode.Smoothing, 3);
        Assert.Equal(4, cfg.MouseMode.MoveMinPx);
        Assert.False(cfg.Ui.AutoLaunch);
    }

    static string FindRepoFile(string name)
    {
        // 從測試輸出目錄往上找 TouchService 根（含 TouchService.sln 的目錄）
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TouchService.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, name);
    }
}
