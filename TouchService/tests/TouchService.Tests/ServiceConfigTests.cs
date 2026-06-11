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

        // 缺欄位 → 預設：grid / mouseMode 預設（faces=null = 排除 Floor 的預設規則，由 host 解析）
        // / content 滿版 / ui.autoLaunch=true / ui.trayEnabled=true
        File.WriteAllText(path, """{ "listenPort": 30001 }""");
        var cfg = ServiceConfig.LoadOrCreate(path);
        Assert.Equal("grid", cfg.TouchMode);
        Assert.Null(cfg.MouseMode.Faces);
        Assert.False(cfg.MouseModeLegacyFaceConverted);
        Assert.Equal(1920, cfg.MouseMode.Screen.Width);
        Assert.Equal(1080, cfg.MouseMode.Screen.Height);
        Assert.Equal(0.0, cfg.MouseMode.Content.OffsetX, 3);
        Assert.Equal(1.0, cfg.MouseMode.Content.ScaleX, 3);
        Assert.Equal(0.35, cfg.MouseMode.Smoothing, 3);
        Assert.Equal(2, cfg.MouseMode.MoveMinPx);
        Assert.True(cfg.Ui.AutoLaunch);
        Assert.True(cfg.Ui.TrayEnabled);

        // 顯式 null 容錯
        File.WriteAllText(path, """{ "touchMode": null, "mouseMode": null, "ui": null }""");
        cfg = ServiceConfig.LoadOrCreate(path);
        Assert.Equal("grid", cfg.TouchMode);
        Assert.NotNull(cfg.MouseMode);
        Assert.NotNull(cfg.MouseMode.Content);
        Assert.True(cfg.Ui.AutoLaunch);
        Assert.True(cfg.Ui.TrayEnabled);

        // 大小寫 / 空白正規化
        File.WriteAllText(path, """{ "touchMode": " Mouse " }""");
        Assert.Equal("mouse", ServiceConfig.LoadOrCreate(path).TouchMode);

        // 完整欄位 roundtrip（v6 schema：faces 陣列 + content）
        File.WriteAllText(path, """
            { "touchMode": "mouse",
              "mouseMode": { "faces": [0, 4], "screen": { "x": -1920, "y": 0, "width": 2560, "height": 1440 },
                             "content": { "offsetX": 16, "offsetY": -9, "scaleX": 0.8, "scaleY": 0.9 },
                             "smoothing": 0.5, "moveMinPx": 4 },
              "ui": { "autoLaunch": false, "trayEnabled": false } }
            """);
        cfg = ServiceConfig.LoadOrCreate(path);
        Assert.Equal("mouse", cfg.TouchMode);
        Assert.Equal(new List<int> { 0, 4 }, cfg.MouseMode.Faces);
        Assert.False(cfg.MouseModeLegacyFaceConverted);
        Assert.Equal(-1920, cfg.MouseMode.Screen.X);
        Assert.Equal(2560, cfg.MouseMode.Screen.Width);
        Assert.Equal(16, cfg.MouseMode.Content.OffsetX, 3);
        Assert.Equal(0.8, cfg.MouseMode.Content.ScaleX, 3);
        Assert.Equal(0.5, cfg.MouseMode.Smoothing, 3);
        Assert.Equal(4, cfg.MouseMode.MoveMinPx);
        Assert.False(cfg.Ui.AutoLaunch);
        Assert.False(cfg.Ui.TrayEnabled);
    }

    [Fact]
    public void 舊config的face單數欄位_相容轉成faces並標旗()
    {
        var dir = Directory.CreateTempSubdirectory("touchservice-cfg4").FullName;
        var path = Path.Combine(dir, "config.json");

        // v4/v5 的舊 schema：face 單數 int → faces:[face] + 轉換旗標（host 啟動 log 提示用）
        File.WriteAllText(path, """
            { "touchMode": "mouse",
              "mouseMode": { "face": 4, "screen": { "x": 0, "y": 0, "width": 1920, "height": 1080 },
                             "smoothing": 0.35, "moveMinPx": 2 } }
            """);
        var cfg = ServiceConfig.LoadOrCreate(path);
        Assert.Equal(new List<int> { 4 }, cfg.MouseMode.Faces);
        Assert.True(cfg.MouseModeLegacyFaceConverted);
        Assert.Null(cfg.MouseMode.Face);   // 單數欄位讀完即清，不往下傳

        // face 與 faces 同時存在（不該發生，但容錯）：faces 優先、不標旗
        File.WriteAllText(path, """{ "mouseMode": { "face": 1, "faces": [0, 3] } }""");
        cfg = ServiceConfig.LoadOrCreate(path);
        Assert.Equal(new List<int> { 0, 3 }, cfg.MouseMode.Faces);
        Assert.False(cfg.MouseModeLegacyFaceConverted);
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
