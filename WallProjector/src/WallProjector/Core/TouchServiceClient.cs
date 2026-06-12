using System.Text.Json;
using WallProjector.Config;

namespace WallProjector.Core;

/// <summary>解析後的 TouchService 狀態（venue 佈局 + 投影螢幕 rect/校正）。</summary>
public sealed class TouchServiceState
{
    public VenueInfo? Venue { get; init; }
    public ScreenRectConfig Screen { get; init; } = new();
    public ContentRectConfig Content { get; init; } = new();

    /// <summary>"live"（TouchService 即時）/ "cache"（上次成功的本地快取）/ "fallback"（config 後備值）</summary>
    public string Source { get; init; } = "fallback";
    public bool Reachable { get; init; }
    public string? Warning { get; init; }
}

/// <summary>
/// TouchService 的普通 HTTP client：GET /venue 拿 faces 佈局、GET /config 拿
/// mouseMode.screen + content（與滑鼠模式同一座標系——觸控落點跟投影內容天然對齊）。
///
/// 失效鏈：live → cache（上次成功抓的整包，存 config 旁 touchservice-cache.json）
/// → fallback（projections.json 的 fallback.screen/content + 可選 venueFile）。
/// 非 live 一律帶 Warning，管理頁顯眼警示。
/// </summary>
public sealed class TouchServiceClient
{
    readonly string _baseUrl;
    readonly string _cachePath;
    readonly FallbackConfig _fallback;
    readonly Logger _log;
    readonly TimeSpan _timeout;

    public TouchServiceClient(string baseUrl, string cachePath, FallbackConfig fallback, Logger log,
                              TimeSpan? timeout = null)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _cachePath = cachePath;
        _fallback = fallback;
        _log = log;
        _timeout = timeout ?? TimeSpan.FromSeconds(3);
    }

    public TouchServiceState Resolve()
    {
        // 1) live
        try
        {
            using var http = new HttpClient { Timeout = _timeout };
            string venueJson = http.GetStringAsync($"{_baseUrl}/venue").GetAwaiter().GetResult();
            string configJson = http.GetStringAsync($"{_baseUrl}/config").GetAwaiter().GetResult();

            var venue = VenueInfo.Parse(venueJson);
            var (screen, content) = ParseScreenContent(configJson);

            WriteCache(venueJson, screen, content);
            _log.Info($"[TouchService] 已連線 {_baseUrl}（venue={venue.Venue}、faces={venue.Faces.Count}、" +
                      $"screen={screen.X},{screen.Y} {screen.Width}x{screen.Height}）");
            return new TouchServiceState
            {
                Venue = venue, Screen = screen, Content = content,
                Source = "live", Reachable = true,
            };
        }
        catch (Exception ex)
        {
            _log.Warn($"[TouchService] 連線失敗（{_baseUrl}）：{ex.Message}——改用本地 cache / fallback");
        }

        // 2) cache
        try
        {
            if (File.Exists(_cachePath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(_cachePath));
                var root = doc.RootElement;
                var venue = VenueInfo.Parse(root.GetProperty("venue").GetRawText());
                var screen = JsonSerializer.Deserialize<ScreenRectConfig>(
                    root.GetProperty("screen").GetRawText(), VenueInfo.JsonOptions) ?? new();
                var content = JsonSerializer.Deserialize<ContentRectConfig>(
                    root.GetProperty("content").GetRawText(), VenueInfo.JsonOptions) ?? new();
                string fetchedAt = root.TryGetProperty("fetchedAtUtc", out var t) ? t.GetString() ?? "?" : "?";

                var warn = $"TouchService 不可達——使用本地 cache（{fetchedAt} 抓取）。佈局/校正若有變動不會反映，請確認 TouchService 已啟動後按「重新連線」。";
                _log.Warn($"[TouchService] {warn}");
                return new TouchServiceState
                {
                    Venue = venue, Screen = screen, Content = content,
                    Source = "cache", Reachable = false, Warning = warn,
                };
            }
        }
        catch (Exception ex)
        {
            _log.Warn($"[TouchService] cache 讀取失敗（{_cachePath}）：{ex.Message}");
        }

        // 3) fallback（config 後備值；venue 只有指定 venueFile 才有）
        VenueInfo? fbVenue = null;
        if (!string.IsNullOrWhiteSpace(_fallback.VenueFile))
        {
            try { fbVenue = VenueInfo.Parse(File.ReadAllText(_fallback.VenueFile)); }
            catch (Exception ex)
            {
                _log.Warn($"[TouchService] fallback venueFile 讀取失敗（{_fallback.VenueFile}）：{ex.Message}");
            }
        }

        var warning = fbVenue == null
            ? "TouchService 不可達、無 cache、無 fallback venueFile——**無法計算牆面 rect，投影槽無法啟動**。請先啟動 TouchService。"
            : "TouchService 不可達——使用 projections.json 的 fallback 值（screen/content/venueFile）。請確認 TouchService 已啟動後按「重新連線」。";
        _log.Warn($"[TouchService] {warning}");
        return new TouchServiceState
        {
            Venue = fbVenue, Screen = _fallback.Screen, Content = _fallback.Content,
            Source = "fallback", Reachable = false, Warning = warning,
        };
    }

    /// <summary>GET /config 回應 → mouseMode.screen + mouseMode.content（缺欄位用預設）。</summary>
    public static (ScreenRectConfig Screen, ContentRectConfig Content) ParseScreenContent(string configJson)
    {
        using var doc = JsonDocument.Parse(configJson);
        var screen = new ScreenRectConfig();
        var content = new ContentRectConfig();

        if (doc.RootElement.TryGetProperty("mouseMode", out var mm))
        {
            if (mm.TryGetProperty("screen", out var s))
                screen = JsonSerializer.Deserialize<ScreenRectConfig>(s.GetRawText(), VenueInfo.JsonOptions) ?? screen;
            if (mm.TryGetProperty("content", out var c))
                content = JsonSerializer.Deserialize<ContentRectConfig>(c.GetRawText(), VenueInfo.JsonOptions) ?? content;
        }
        return (screen, content);
    }

    void WriteCache(string venueJson, ScreenRectConfig screen, ContentRectConfig content)
    {
        try
        {
            using var venueDoc = JsonDocument.Parse(venueJson);
            var cache = new
            {
                fetchedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                venue = venueDoc.RootElement,
                screen = new { x = screen.X, y = screen.Y, width = screen.Width, height = screen.Height },
                content = new
                {
                    offsetX = content.OffsetX, offsetY = content.OffsetY,
                    scaleX = content.ScaleX, scaleY = content.ScaleY,
                },
            };
            File.WriteAllText(_cachePath,
                JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            _log.Warn($"[TouchService] cache 寫入失敗（{_cachePath}）：{ex.Message}（不影響運作）");
        }
    }
}
