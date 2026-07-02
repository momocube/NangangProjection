using System.Text.Json;
using System.Text.Json.Serialization;

namespace TouchService.Core;

public sealed class CanvasSize
{
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>掃 venues/ 資料夾時,每個可用 profile 的輕量描述:相對路徑 + 顯示名稱。</summary>
public sealed record VenueEntry(string RelativePath, string VenueName);

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

    /// <summary>
    /// 掃 venuesDir 內所有 *.json,回傳可用 venue 清單。壞檔靜默跳過(不擋整體)。
    /// RelativePath 統一用 "venues/&lt;name&gt;.json" 格式(跟 ServiceConfig 預設值一致)。
    /// venuesDir 不存在或空時回空清單。
    /// </summary>
    public static IReadOnlyList<VenueEntry> DiscoverAll(string venuesDir)
    {
        if (!Directory.Exists(venuesDir)) return Array.Empty<VenueEntry>();

        var list = new List<VenueEntry>();
        foreach (var file in Directory.EnumerateFiles(venuesDir, "*.json").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var p = Load(file);
                var name = string.IsNullOrWhiteSpace(p.Venue) ? Path.GetFileNameWithoutExtension(file) : p.Venue;
                var rel = "venues/" + Path.GetFileName(file);
                list.Add(new VenueEntry(rel, name));
            }
            catch
            {
                // 壞 JSON / 缺欄位:跳過該檔,不擋其他項目
            }
        }
        return list;
    }
}
