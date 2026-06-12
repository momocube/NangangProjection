using WallProjector.Config;
using Xunit;

namespace WallProjector.Tests;

/// <summary>projections.json：roundtrip、缺欄位容錯、根陣列最小寫法、rotation 正規化。</summary>
public class ProjectorConfigTests : IDisposable
{
    readonly string _dir = Directory.CreateTempSubdirectory("wp-config-tests").FullName;

    string Write(string name, string json)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void roundtrip_save後load_內容一致()
    {
        var cfg = new ProjectorConfig
        {
            HttpPort = 30000,
            TouchServiceBaseUrl = "http://127.0.0.1:9999",
            AutoRestore = false,
            RestartOnCrash = false,
            Topmost = false,
            Projections =
            {
                new SlotConfig { Face = 4, Url = "https://example.com/", Rotation = 90, Enabled = true },
                new SlotConfig { Face = 0, Url = "https://other.example/", Rotation = 270, Enabled = false },
            },
        };
        var path = Path.Combine(_dir, "roundtrip.json");
        cfg.Save(path);

        var loaded = ProjectorConfig.Load(path);
        Assert.Equal(30000, loaded.HttpPort);
        Assert.Equal("http://127.0.0.1:9999", loaded.TouchServiceBaseUrl);
        Assert.False(loaded.AutoRestore);
        Assert.False(loaded.RestartOnCrash);
        Assert.False(loaded.Topmost);
        Assert.Equal(2, loaded.Projections.Count);

        var slot4 = loaded.GetSlot(4)!;
        Assert.Equal("https://example.com/", slot4.Url);
        Assert.Equal(90, slot4.Rotation);
        Assert.True(slot4.Enabled);
        Assert.Equal(270, loaded.GetSlot(0)!.Rotation);
    }

    [Fact]
    public void 根陣列最小寫法_直接當projections清單()
    {
        // spec 的最小寫法：整個檔案就是槽陣列
        var path = Write("array.json",
            """[{ "face": 4, "url": "https://example.com/", "rotation": 90, "enabled": true }]""");
        var cfg = ProjectorConfig.Load(path);

        Assert.Single(cfg.Projections);
        Assert.Equal(4, cfg.Projections[0].Face);
        Assert.Equal(90, cfg.Projections[0].Rotation);
        Assert.True(cfg.Projections[0].Enabled);
        // 其餘吃預設
        Assert.Equal(20121, cfg.HttpPort);
        Assert.Equal("http://127.0.0.1:20111", cfg.TouchServiceBaseUrl);
        Assert.True(cfg.AutoRestore);
        Assert.True(cfg.RestartOnCrash);
    }

    [Fact]
    public void 缺欄位容錯_全吃預設()
    {
        var path = Write("partial.json", """{ "projections": [{ "face": 1 }] }""");
        var cfg = ProjectorConfig.Load(path);

        Assert.Equal(20121, cfg.HttpPort);
        Assert.True(cfg.Topmost);
        Assert.Equal(1920, cfg.Fallback.Screen.Width);
        Assert.Equal(1.0, cfg.Fallback.Content.ScaleX);

        var slot = cfg.GetSlot(1)!;
        Assert.Equal("", slot.Url);
        Assert.Equal(0, slot.Rotation);
        Assert.False(slot.Enabled);
    }

    [Fact]
    public void 檔案不存在_回預設不丟錯()
    {
        var cfg = ProjectorConfig.Load(Path.Combine(_dir, "missing.json"));
        Assert.Empty(cfg.Projections);
        Assert.Equal(20121, cfg.HttpPort);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 90)]
    [InlineData(180, 180)]
    [InlineData(270, 270)]
    [InlineData(360, 0)]
    [InlineData(-90, 270)]     // 負角度取模
    [InlineData(450, 90)]
    [InlineData(45, 0)]        // 非直角歸 0
    public void rotation正規化(int input, int expected)
    {
        Assert.Equal(expected, SlotConfig.NormalizeRotation(input));
    }

    [Fact]
    public void load時rotation非法值被正規化()
    {
        var path = Write("badrot.json",
            """{ "projections": [{ "face": 0, "rotation": -90 }, { "face": 1, "rotation": 45 }] }""");
        var cfg = ProjectorConfig.Load(path);
        Assert.Equal(270, cfg.GetSlot(0)!.Rotation);
        Assert.Equal(0, cfg.GetSlot(1)!.Rotation);
    }

    [Fact]
    public void 同face重複_保留最後一筆()
    {
        var path = Write("dup.json", """
            { "projections": [
              { "face": 2, "url": "https://old.example/" },
              { "face": 2, "url": "https://new.example/" }
            ] }
            """);
        var cfg = ProjectorConfig.Load(path);
        Assert.Single(cfg.Projections);
        Assert.Equal("https://new.example/", cfg.GetSlot(2)!.Url);
    }

    // ── V2：mode 欄位（向後相容是硬需求——V1 的 projections.json 不改即用）──

    [Fact]
    public void V1檔案無mode欄位_預設wrap_照常工作()
    {
        // V1 發布的範本原樣（無 mode / loop / muted / controls / cdpBasePort）
        var path = Write("v1.json", """
            {
              "httpPort": 20121,
              "topmost": true,
              "projections": [
                { "face": 4, "url": "https://example.com/", "rotation": 90, "enabled": true }
              ]
            }
            """);
        var cfg = ProjectorConfig.Load(path);

        var slot = cfg.GetSlot(4)!;
        Assert.Equal(SlotConfig.ModeWrap, slot.Mode);
        Assert.Equal("https://example.com/", slot.Url);
        Assert.Equal(90, slot.Rotation);
        Assert.True(slot.Enabled);
        // file 模式選項預設
        Assert.True(slot.Loop);
        Assert.True(slot.Muted);
        Assert.False(slot.Controls);
        Assert.Null(slot.CdpPort);
        Assert.Equal(9333, cfg.CdpBasePort);
    }

    [Fact]
    public void mode非法值_視為wrap並照常載入()
    {
        var path = Write("badmode.json",
            """{ "projections": [{ "face": 0, "url": "https://x.example/", "mode": "kiosk" }] }""");
        var cfg = ProjectorConfig.Load(path);
        Assert.Equal(SlotConfig.ModeWrap, cfg.GetSlot(0)!.Mode);
    }

    [Theory]
    [InlineData(null, "wrap")]
    [InlineData("", "wrap")]
    [InlineData("wrap", "wrap")]
    [InlineData("CDP", "cdp")]
    [InlineData(" file ", "file")]
    [InlineData("bogus", "wrap")]
    public void mode正規化(string? input, string expected) =>
        Assert.Equal(expected, SlotConfig.NormalizeMode(input));

    [Fact]
    public void roundtrip_V2欄位全保留()
    {
        var cfg = new ProjectorConfig
        {
            CdpBasePort = 9500,
            Projections =
            {
                new SlotConfig { Face = 1, Url = "https://yt.example/", Rotation = 90,
                                 Mode = SlotConfig.ModeCdp, CdpPort = 9999, Enabled = true },
                new SlotConfig { Face = 2, Url = @"D:\media\demo.mp4", Rotation = 270,
                                 Mode = SlotConfig.ModeFile, Loop = false, Muted = false, Controls = true },
            },
        };
        var path = Path.Combine(_dir, "v2-roundtrip.json");
        cfg.Save(path);

        var loaded = ProjectorConfig.Load(path);
        Assert.Equal(9500, loaded.CdpBasePort);

        var cdp = loaded.GetSlot(1)!;
        Assert.Equal(SlotConfig.ModeCdp, cdp.Mode);
        Assert.Equal(9999, cdp.CdpPort);

        var file = loaded.GetSlot(2)!;
        Assert.Equal(SlotConfig.ModeFile, file.Mode);
        Assert.Equal(@"D:\media\demo.mp4", file.Url);
        Assert.False(file.Loop);
        Assert.False(file.Muted);
        Assert.True(file.Controls);
    }

    [Fact]
    public void upsert_新face建立並排序()
    {
        var cfg = new ProjectorConfig();
        cfg.UpsertSlot(4).Url = "https://a.example/";
        cfg.UpsertSlot(1).Url = "https://b.example/";
        var again = cfg.UpsertSlot(4);

        Assert.Equal(2, cfg.Projections.Count);
        Assert.Equal("https://a.example/", again.Url);   // 不重複建
        Assert.Equal(1, cfg.Projections[0].Face);        // 排序
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
