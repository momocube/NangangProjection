using System.Text.Json;
using WallProjector.Core;

namespace WallProjector.Config;

/// <summary>
/// projections.json：投影槽清單 + 工具自身設定。
///
/// 容錯：
/// - 檔案不存在 → 全預設（首次存檔會寫出完整結構）
/// - 根節點是**陣列** → 視為 projections 清單、其餘全預設（spec 的最小寫法）
/// - 缺欄位 → 預設值；rotation 非 0/90/180/270 → 正規化（負值取模、非直角歸 0 並警告）
/// - 缺 mode（V1 檔案）→ "wrap"；非法 mode → "wrap" 並警告（**V1 config 不改即用**）
///
/// 牆面 rotation 放這裡而不是 TouchService venue JSON——V1 不動 TouchService 的決定
/// （見 TouchService/docs/ROADMAP-PROJECTION.md）；未來多場域才遷成 venue 的 viewRotation。
/// </summary>
public sealed class ProjectorConfig
{
    public int HttpPort { get; set; } = 20121;
    public string TouchServiceBaseUrl { get; set; } = "http://127.0.0.1:20111";
    public bool AutoRestore { get; set; } = true;
    public bool RestartOnCrash { get; set; } = true;
    public bool Topmost { get; set; } = true;

    /// <summary>每槽獨立 browser profile 根目錄（相對 config 所在目錄）。
    /// 獨立 user-data-dir 強制每槽一個獨立瀏覽器 process——
    /// 否則 Edge/Chrome 會把新視窗併進既有 process，watchdog 盯的 process 立刻退出（誤判死亡）。</summary>
    public string BrowserProfileDir { get; set; } = "profiles";

    /// <summary>CDP 模式 debug port 基準：槽未指定 cdpPort 時用 cdpBasePort + face
    /// （每槽必須獨立 port——每槽本來就是獨立瀏覽器 process）。</summary>
    public int CdpBasePort { get; set; } = 9333;

    public FallbackConfig Fallback { get; set; } = new();
    public List<SlotConfig> Projections { get; set; } = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
    };

    public static ProjectorConfig Load(string path, Logger? log = null)
    {
        if (!File.Exists(path))
        {
            log?.Warn($"[Config] {path} 不存在，使用預設值（首次寫入會建立檔案）");
            return new ProjectorConfig();
        }

        var json = File.ReadAllText(path);
        ProjectorConfig cfg;

        using (var doc = JsonDocument.Parse(json, new JsonDocumentOptions
               { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }))
        {
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                // spec 最小寫法：整個檔案就是槽陣列
                cfg = new ProjectorConfig
                {
                    Projections = JsonSerializer.Deserialize<List<SlotConfig>>(json, JsonOptions) ?? new(),
                };
            }
            else
            {
                cfg = JsonSerializer.Deserialize<ProjectorConfig>(json, JsonOptions) ?? new ProjectorConfig();
            }
        }

        // rotation / mode 正規化 + 去重（同 face 重複時保留最後一筆）
        var seen = new Dictionary<int, SlotConfig>();
        foreach (var slot in cfg.Projections)
        {
            int norm = SlotConfig.NormalizeRotation(slot.Rotation);
            if (norm != slot.Rotation)
            {
                log?.Warn($"[Config] face {slot.Face} rotation {slot.Rotation} 非 0/90/180/270，正規化為 {norm}");
                slot.Rotation = norm;
            }
            string mode = SlotConfig.NormalizeMode(slot.Mode);
            if (mode != slot.Mode)
            {
                if (!string.IsNullOrWhiteSpace(slot.Mode))
                    log?.Warn($"[Config] face {slot.Face} mode \"{slot.Mode}\" 非 wrap/cdp/file，視為 wrap");
                slot.Mode = mode;   // 缺欄位（V1 檔案）/ 非法值 → wrap，向後相容
            }
            if (seen.ContainsKey(slot.Face))
                log?.Warn($"[Config] face {slot.Face} 重複設定，保留最後一筆");
            seen[slot.Face] = slot;
        }
        cfg.Projections = seen.Values.ToList();
        return cfg;
    }

    public void Save(string path)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, JsonOptions));
        File.Move(tmp, path, overwrite: true);
    }

    public SlotConfig? GetSlot(int face) => Projections.FirstOrDefault(p => p.Face == face);

    /// <summary>取得或建立 face 的槽設定（管理頁寫入用）。</summary>
    public SlotConfig UpsertSlot(int face)
    {
        var slot = GetSlot(face);
        if (slot == null)
        {
            slot = new SlotConfig { Face = face };
            Projections.Add(slot);
            Projections.Sort((a, b) => a.Face.CompareTo(b.Face));
        }
        return slot;
    }
}

public sealed class FallbackConfig
{
    /// <summary>TouchService 不可達且無 cache 時的投影螢幕 rect 後備值。</summary>
    public ScreenRectConfig Screen { get; set; } = new();
    public ContentRectConfig Content { get; set; } = new();

    /// <summary>後備 venue JSON 路徑（如 TouchService 的 venues/704.json 複本）；null = 不用。</summary>
    public string? VenueFile { get; set; }
}

public sealed class SlotConfig
{
    public const string ModeWrap = "wrap";
    public const string ModeCdp = "cdp";
    public const string ModeFile = "file";

    /// <summary>face index（對照 GET /venue 的 faces 陣列順序）。</summary>
    public int Face { get; set; } = -1;

    /// <summary>wrap/cdp 模式：目標網址；file 模式：本地檔案路徑（絕對或相對 config 所在目錄）。</summary>
    public string Url { get; set; } = "";

    /// <summary>投影內容旋轉（CSS rotate 度數、順時針；0/90/180/270）。
    /// 左右牆臂在螢幕空間相對人視角轉了 90°，內容反向旋轉讓人看是正的。</summary>
    public int Rotation { get; set; }

    public bool Enabled { get; set; }

    /// <summary>"wrap"（iframe 包覆，V1 行為）/ "cdp"（直開原站 + DevTools 注入旋轉，
    /// 繞過 X-Frame-Options）/ "file"（本地影片/PDF/圖片）。缺欄位 = wrap（V1 相容）。</summary>
    public string Mode { get; set; } = ModeWrap;

    /// <summary>cdp 模式的 debug port；null = cdpBasePort + face 自動配發。</summary>
    public int? CdpPort { get; set; }

    /// <summary>file 模式（影片）：循環播放。</summary>
    public bool Loop { get; set; } = true;

    /// <summary>file 模式（影片）：靜音（牆面投影預設不出聲；autoplay 也最可靠）。</summary>
    public bool Muted { get; set; } = true;

    /// <summary>file 模式（影片）：顯示播放控制列（觸控可按）。</summary>
    public bool Controls { get; set; }

    /// <summary>負值取模到 0~359；非直角（非 90 倍數）歸 0。</summary>
    public static int NormalizeRotation(int rotation)
    {
        int r = ((rotation % 360) + 360) % 360;
        return r is 0 or 90 or 180 or 270 ? r : 0;
    }

    /// <summary>缺值 / 非法值 → wrap（V1 config 無 mode 欄位照常工作）。</summary>
    public static string NormalizeMode(string? mode)
    {
        var m = mode?.Trim().ToLowerInvariant();
        return m is ModeWrap or ModeCdp or ModeFile ? m : ModeWrap;
    }
}
