using System.Net;

namespace WallProjector.Web;

/// <summary>
/// wrap 頁生成：把目標 URL iframe 進來，外層容器套 CSS rotate。
/// 旋轉數學集中在 <see cref="RotationCss"/>（wrap / media / CDP 三路共用）。
///
/// 觸控不需反算：瀏覽器 hit-testing 原生處理 CSS transform，TouchService 滑鼠模式
/// 點在螢幕座標上，瀏覽器自己把事件路由進旋轉後的 iframe。
///
/// title 內嵌 `[WP-slot-N]` 標記——Win32 EnumWindows 靠它找到該視窗做精確對位。
/// </summary>
public static class WrapPageBuilder
{
    /// <summary>視窗標題裡的 slot 標記（WindowPositioner 比對用）。</summary>
    public static string TitleMarker(int face) => $"[WP-slot-{face}]";

    public static string Build(int face, string faceName, string url, int rotation,
                               string? blockReason = null)
    {
        string stageCss = RotationCss.StageCss(rotation);

        string urlAttr = WebUtility.HtmlEncode(url);
        string nameText = WebUtility.HtmlEncode(faceName);

        string stageInner = blockReason == null
            ? $"""
               <iframe id="frame" src="{urlAttr}" allow="autoplay; fullscreen; geolocation"></iframe>
               <div id="overlay"><div class="msg">載入中…</div><div class="url">{urlAttr}</div></div>
               """
            : $"""
               <div id="blocked">
                 <h1>⚠ 目標網站拒絕被嵌入</h1>
                 <p class="url">{urlAttr}</p>
                 <p>{WebUtility.HtmlEncode(blockReason)}</p>
                 <p class="hint">該站以 X-Frame-Options / CSP frame-ancestors 禁止 iframe 嵌入。<br>
                    請到管理頁把此面的模式切成「網頁CDP」——CDP 模式直接開原站分頁（不經 iframe）
                    並注入旋轉 CSS，不受嵌入限制。詳見 WallProjector/README.md。</p>
               </div>
               """;

        return $$"""
            <!DOCTYPE html>
            <html lang="zh-Hant">
            <head>
            <meta charset="utf-8">
            <title>{{nameText}} — WallProjector {{TitleMarker(face)}}</title>
            <style>
              html,body{margin:0;padding:0;width:100vw;height:100vh;overflow:hidden;background:#000;}
              #stage{position:fixed;left:0;top:0;{{stageCss}}}
              #stage iframe{width:100%;height:100%;border:0;display:block;background:#000;}
              #overlay{position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;
                justify-content:center;gap:10px;background:#14161c;color:#79808f;
                font:16px/1.6 "Consolas","Microsoft JhengHei",monospace;}
              #overlay .url{font-size:12px;word-break:break-all;max-width:80%;}
              #blocked{position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;
                justify-content:center;gap:8px;background:#14161c;color:#d8dce6;text-align:center;
                font:15px/1.7 "Consolas","Microsoft JhengHei",monospace;padding:24px;box-sizing:border-box;}
              #blocked h1{font-size:20px;color:#ff4d4d;margin:0;}
              #blocked .url{color:#5b8dee;word-break:break-all;max-width:90%;}
              #blocked .hint{color:#79808f;font-size:13px;}
            </style>
            </head>
            <body>
            <div id="stage">
            {{stageInner}}
            </div>
            <script>
              var f = document.getElementById('frame'), o = document.getElementById('overlay');
              if (f) {
                f.addEventListener('load', function () { if (o) o.style.display = 'none'; });
                setTimeout(function () {
                  if (o && o.style.display !== 'none')
                    o.querySelector('.msg').textContent =
                      '頁面載入逾時——可能網路不通、站點過慢、或拒絕被嵌入（檢查 console）';
                }, 20000);
              }
            </script>
            </body>
            </html>
            """;
    }

    /// <summary>face 沒設定 URL 時的占位頁。</summary>
    public static string BuildNotConfigured(int face) => $$"""
        <!DOCTYPE html>
        <html lang="zh-Hant"><head><meta charset="utf-8">
        <title>未設定 — WallProjector {{TitleMarker(face)}}</title>
        <style>body{margin:0;height:100vh;display:flex;align-items:center;justify-content:center;
          background:#14161c;color:#79808f;font:16px "Consolas","Microsoft JhengHei",monospace;}</style>
        </head><body>face {{face}} 尚未設定投影 URL——請開管理頁設定</body></html>
        """;
}
