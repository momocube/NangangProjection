using System.Text.Json;
using System.Text.Json.Serialization;

namespace SensorSimulator;

/// <summary>
/// scenario JSON 內的一個觸碰點時間軸：downMs 開始、每 resendIntervalMs 重送、upMs 停止。
/// 有 moveToRawX/Y 時位置在 down→up 期間線性插值（模擬拖移）。
/// </summary>
public sealed class ScenarioTouch
{
    /// <summary>所屬模組編號。同模組同 tick 的點會合包成一個 datagram（最多 4 點），並共用同一個 UDP source socket。</summary>
    public int Module { get; set; }

    public int RawX { get; set; }
    public int RawY { get; set; }

    /// <summary>down 時刻（scenario 時間軸 ms）</summary>
    public int DownMs { get; set; }
    /// <summary>up 時刻（最後一次重送不晚於此時刻）</summary>
    public int UpMs { get; set; }

    /// <summary>選填：拖移終點（down→up 線性插值）</summary>
    public int? MoveToRawX { get; set; }
    public int? MoveToRawY { get; set; }
}

/// <summary>
/// scenario 模式腳本：N 個點的 down/hold/up 時間軸，按 resendIntervalMs 重送
/// + 可注入 jitter / 丟包率。用多個 UDP source port 模擬多模組。
/// </summary>
public sealed class Scenario
{
    public string Name { get; set; } = "scenario";

    /// <summary>同點重送週期（ms）。實測硬體主峰 80ms。</summary>
    public int ResendIntervalMs { get; set; } = 80;

    /// <summary>重送 jitter（ms，均勻分佈 ±jitterMs）</summary>
    public int JitterMs { get; set; } = 0;

    /// <summary>丟包率 0.0~1.0（每次重送獨立判定；down 首包不丟，確保 down 時刻確定）</summary>
    public double DropRate { get; set; } = 0.0;

    /// <summary>亂數種子（jitter / 丟包可重現）</summary>
    public int Seed { get; set; } = 1;

    public List<ScenarioTouch> Touches { get; set; } = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public static Scenario Load(string path) =>
        JsonSerializer.Deserialize<Scenario>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException($"scenario 解析失敗：{path}");
}
