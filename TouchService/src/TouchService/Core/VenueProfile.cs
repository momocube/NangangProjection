using System.Text.Json;
using System.Text.Json.Serialization;

namespace TouchService.Core;

public sealed class CanvasSize
{
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// 場域 profile：畫布尺寸 + 感應面清單。
/// 對應 Unity 的 VenuePresetConfig + DisplayCanvasConfig + SensorFaceConfig[] 三個 ScriptableObject，
/// 校正資料一次性轉換自 Assets/_Script/Core/Setting/2688x3840/（轉換流程見 docs/CALIBRATION.md）。
/// faces 順序保持 Unity Preset_2688x3840.asset 內的順序（線性分類先到先得）。
/// </summary>
public sealed class VenueProfile
{
    public string Venue { get; set; } = "";
    public CanvasSize Canvas { get; set; } = new();
    public List<SensorFaceConfig> Faces { get; set; } = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static VenueProfile Load(string path)
    {
        var json = File.ReadAllText(path);
        var profile = JsonSerializer.Deserialize<VenueProfile>(json, JsonOptions)
            ?? throw new InvalidDataException($"venue profile 解析失敗：{path}");

        if (profile.Canvas.Width <= 0 || profile.Canvas.Height <= 0)
            throw new InvalidDataException($"venue profile canvas 尺寸非法：{path}");
        if (profile.Faces.Count == 0)
            throw new InvalidDataException($"venue profile 沒有任何感應面：{path}");

        return profile;
    }
}
