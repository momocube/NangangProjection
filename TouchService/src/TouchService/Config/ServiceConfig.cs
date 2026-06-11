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

/// <summary>啟動 UI（瀏覽器 app 視窗自動開啟監控/設定頁）。僅 Windows 生效，非 Windows 只 log。</summary>
public sealed class UiConfig
{
    public bool AutoLaunch { get; set; } = true;

    /// <summary>
    /// 系統列托盤常駐（僅 Windows）：啟動時隱藏 console、掛 NotifyIcon
    /// （雙擊開 UI / 右鍵選單）。false = 行為同 v5（console 視窗照常顯示、無托盤）。
    /// `--console` CLI flag 可在 trayEnabled=true 下保留 console（除錯用）。
    /// </summary>
    public bool TrayEnabled { get; set; } = true;
}

/// <summary>滑鼠模式的目標螢幕矩形（virtual desktop 座標，非主螢幕可為負）。</summary>
public sealed class ScreenRectConfig
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
}

/// <summary>
/// 投影內容在螢幕內的校正（letterbox / 投影沒滿版時用）：
/// 有效落點 = screen rect 套用 offset/scale。預設 0/0/1/1 = 滿版（不校正）。
/// screenX = screen.x + offsetX + norm.x × screen.width × scaleX。
/// </summary>
public sealed class ContentRectConfig
{
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double ScaleX { get; set; } = 1.0;
    public double ScaleY { get; set; } = 1.0;
}

/// <summary>
/// 滑鼠模式（touchMode == "mouse" 時生效）：啟用面（faces）上的觸點以
/// **canvas 十字展開圖座標**映射為 OS 滑鼠操作（down→LEFTDOWN / move→平滑移動 / up→LEFTUP）。
/// 投影螢幕顯示的是整個場域 canvas 的十字展開圖——映射是 canvas norm（px / canvas 尺寸）
/// → 目標螢幕 rect 等比落點，不是單一面拉伸到全螢幕（v4 的 bug）。
/// 單點所有權跨所有啟用面：任一啟用面第一個 down 的 touch 是 owner，up 前其他觸點忽略。
/// 詳見 docs/DEPLOY.md「滑鼠模式」。
/// </summary>
public sealed class MouseModeConfig
{
    /// <summary>
    /// 啟用滑鼠觸控的面清單（venue faces index）。
    /// 缺欄位/null = 預設：全部面但排除名稱含 Floor 的面（704 = 地板關、六個牆面開）。
    /// 空陣列 = 全部停用（滑鼠 sink 不動作）。
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<int>? Faces { get; set; }

    /// <summary>
    /// 舊版（v4/v5）單數欄位：讀到時自動轉成 Faces=[Face] 並 log 提示（向後相容，僅供反序列化）。
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Face { get; set; }

    /// <summary>映射目標螢幕矩形（canvas norm 0~1 → 此矩形內的螢幕座標）。</summary>
    public ScreenRectConfig Screen { get; set; } = new();

    /// <summary>投影內容校正（letterbox/沒滿版時用）。缺欄位/null = 滿版（0/0/1/1）。</summary>
    public ContentRectConfig Content { get; set; } = new();

    /// <summary>平滑強度：8ms 迴圈每 tick 以此比例指數逼近目標（0 &lt; s ≤ 1；1 = 不平滑直接到位）。</summary>
    public double Smoothing { get; set; } = 0.35;

    /// <summary>平滑迴圈單步位移小於此值（px）不注入，避免 125Hz 微步轟炸（到位的最後一步除外）。</summary>
    public int MoveMinPx { get; set; } = 2;
}

/// <summary>
/// 啟動自檢窗（自動鎖噪訊晶片）：服務啟動後 windowSeconds 內命中任一判準（OR）的格子
/// 自動加入 runtime 黑名單（依據：開機時場上理應無人）。窗期間不發布任何觸控事件。
///   判準 A：窗內封包數 ≥ minPackets（快速重送型噪訊）
///   判準 B：窗內分離 episode 數 ≥ minEpisodes（慢速頻閃型噪訊：單包間隔秒級）
/// 全欄位有預設值、缺欄位 / null 容錯。詳見 docs/DEPLOY.md。
/// </summary>
public sealed class AutoLockConfig
{
    public bool Enabled { get; set; } = true;

    /// <summary>自檢窗長度（秒）。≤ 0 視為停用。</summary>
    public int WindowSeconds { get; set; } = 10;

    /// <summary>窗內封包數達此值即鎖（噪訊晶片 ~80ms 重送，10 秒約 125 包；判準 A）。</summary>
    public int MinPackets { get; set; } = 10;

    /// <summary>
    /// 窗內分離 episode 數達此值即鎖（判準 B，與判準 A 為 OR）。0 = 停用此判準。
    /// episode = 同 cell 封包流以「間隔 &gt; releaseTimeout」切分的一次連續觸發。
    /// 依據：開機時場上沒人，真人誤踩是一次連續事件，不會在同一格分離地觸發 3 次；
    /// 慢速頻閃型噪訊（單包間隔秒級、湊不滿 minPackets）靠這個抓。
    /// </summary>
    public int MinEpisodes { get; set; } = 3;
}

/// <summary>
/// 鎖定晶片格（噪訊晶片過濾）：face = 面 index、cell = [col, row]（晶片格座標）。
/// 對應 Unity 專案 SensorTestManager 的 locked[]，config 化版本。
/// </summary>
public sealed class LockedCellConfig
{
    public int Face { get; set; } = -1;
    public int[]? Cell { get; set; }

    /// <summary>欄位齊全且在 packing 範圍內才有效（壞項跳過、不擋啟動）。</summary>
    [JsonIgnore]
    public bool IsValid => Face is >= 0 and <= 2047 &&
                           Cell is { Length: 2 } &&
                           Cell[0] is >= 0 and <= 1023 &&
                           Cell[1] is >= 0 and <= 1023;
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

    /// <summary>
    /// 鎖定晶片清單（噪訊晶片過濾）：命中的點在進 touch tracker 前丟棄。
    /// 缺欄位 / null = 空 = 不過濾。現場鎖定 SOP 見 docs/DEPLOY.md。
    /// </summary>
    public List<LockedCellConfig> LockedCells { get; set; } = new();

    /// <summary>啟動自檢窗（自動鎖噪訊晶片）。缺欄位 / null = 預設啟用（10s、≥10 包）。</summary>
    public AutoLockConfig AutoLock { get; set; } = new();

    /// <summary>觸控模式："grid"（預設，純發布）/ "mouse"（額外掛 OS 滑鼠 sink，發布照常）。</summary>
    public string TouchMode { get; set; } = "grid";

    /// <summary>滑鼠模式參數（touchMode == "mouse" 時生效）。缺欄位 / null = 預設值。</summary>
    public MouseModeConfig MouseMode { get; set; } = new();

    /// <summary>
    /// 舊 config 的 mouseMode.face（單數）被轉成 faces:[face] 時為 true——
    /// host 啟動時據此 log 提示（LoadOrCreate 無 logger，旗標帶出去）。
    /// </summary>
    [JsonIgnore]
    public bool MouseModeLegacyFaceConverted { get; private set; }

    /// <summary>啟動 UI（Windows 自動開瀏覽器 app 視窗）。缺欄位 / null = 預設開啟。</summary>
    public UiConfig Ui { get; set; } = new();

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
        var cfg = JsonSerializer.Deserialize<ServiceConfig>(json, JsonOptions) ?? new ServiceConfig();
        cfg.LockedCells ??= new List<LockedCellConfig>();   // "lockedCells": null 容錯
        cfg.AutoLock ??= new AutoLockConfig();              // "autoLock": null 容錯
        cfg.TouchMode = string.IsNullOrWhiteSpace(cfg.TouchMode) ? "grid" : cfg.TouchMode.Trim().ToLowerInvariant();
        cfg.MouseMode ??= new MouseModeConfig();            // "mouseMode": null 容錯
        cfg.MouseMode.Screen ??= new ScreenRectConfig();
        cfg.MouseMode.Content ??= new ContentRectConfig();  // "content": null 容錯（滿版）
        // 向後相容：舊 config 的 mouseMode.face（單數）→ faces:[face]（host 啟動時 log 提示）
        if (cfg.MouseMode.Faces == null && cfg.MouseMode.Face is int legacyFace)
        {
            cfg.MouseMode.Faces = new List<int> { legacyFace };
            cfg.MouseModeLegacyFaceConverted = true;
        }
        cfg.MouseMode.Face = null;                          // 單數欄位僅供讀入，不再往下傳
        cfg.Ui ??= new UiConfig();                          // "ui": null 容錯
        return cfg;
    }
}
