using System.Text.Json;
using System.Text.Json.Serialization;

namespace TouchService.Config;

public sealed class UdpOutputConfig
{
    public bool Enabled { get; set; } = true;
    public List<string> Endpoints { get; set; } = new() { "127.0.0.1:20110" };
}

public sealed class WebSocketOutputConfig
{
    public bool Enabled { get; set; } = true;
    public int Port { get; set; } = 20111;
}

/// <summary>TUIO 輸出插槽：v1 只保留介面與設定欄位，不實作（見 Output/TuioPublisher.cs）。</summary>
public sealed class TuioOutputConfig
{
    public bool Enabled { get; set; } = false;
    public string Endpoint { get; set; } = "127.0.0.1:3333";
}

public sealed class LogConfig
{
    public string Level { get; set; } = "Info";
    public string Dir { get; set; } = "logs";
    public bool Console { get; set; } = true;
}

/// <summary>
/// 服務設定（config.json）。全部欄位有預設值；缺檔時自動生成範本。
/// </summary>
public sealed class ServiceConfig
{
    /// <summary>感應模組廣播的 UDP port</summary>
    public int ListenPort { get; set; } = 20001;

    /// <summary>場域 profile 路徑（相對於 config.json 所在目錄）</summary>
    public string VenueProfile { get; set; } = "venues/704.json";

    /// <summary>
    /// 最後封包後多久無新封包視為離手（ms）。
    /// 預設 170：實測重送 gap 長尾 120-160ms，100ms 會假離手。詳見 docs/PROTOCOL.md。
    /// </summary>
    public int ReleaseTimeoutMs { get; set; } = 170;

    /// <summary>touch ID 最近鄰配對半徑（畫布 px；96 ≈ 704 場域 3 格）</summary>
    public float MatchRadiusPx { get; set; } = 96f;

    /// <summary>同時追蹤的 touch 上限</summary>
    public int MaxSlots { get; set; } = 128;

    /// <summary>release timeout 掃描週期（ms），決定 up 事件的時間精度</summary>
    public int FlushIntervalMs { get; set; } = 5;

    /// <summary>全狀態 snapshot keepalive 週期（ms）</summary>
    public int KeepaliveIntervalMs { get; set; } = 1000;

    /// <summary>move 事件最小位移門檻（px，0 = 任何座標變化都發）</summary>
    public int MoveMinDistancePx { get; set; } = 0;

    public UdpOutputConfig UdpOutput { get; set; } = new();
    public WebSocketOutputConfig WebSocketOutput { get; set; } = new();
    public TuioOutputConfig TuioOutput { get; set; } = new();
    public LogConfig Log { get; set; } = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>讀取設定；檔案不存在時寫出預設範本並回傳預設值。</summary>
    public static ServiceConfig LoadOrCreate(string path)
    {
        if (!File.Exists(path))
        {
            var def = new ServiceConfig();
            File.WriteAllText(path, JsonSerializer.Serialize(def, JsonOptions));
            return def;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ServiceConfig>(json, JsonOptions) ?? new ServiceConfig();
    }
}
