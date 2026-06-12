using System.Net;

namespace WallProjector.Web;

/// <summary>
/// file 模式的播放/檢視頁生成：依副檔名分流——影片 &lt;video&gt;、圖片 &lt;img&gt;、
/// PDF 用 vendored pdf.js（assets/ 本地檔，**離線可用**，場館可能沒外網）。
///
/// 旋轉沿用 wrap 頁同一套 stage CSS（<see cref="RotationCss"/>）——這些頁面是
/// 我們自己的 origin，沒有 iframe 嵌入限制問題。檔案 bytes 走 /media/{face}/raw
/// （支援 Range——影片 seek 必需）。title 內嵌 [WP-slot-N] 供視窗對位。
/// </summary>
public static class MediaPageBuilder
{
    public enum MediaKind { Video, Image, Pdf, Unsupported }

    public static MediaKind KindOf(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".mp4" or ".webm" or ".mov" => MediaKind.Video,
            ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" => MediaKind.Image,
            ".pdf" => MediaKind.Pdf,
            _ => MediaKind.Unsupported,
        };

    /// <summary>raw 端點的 Content-Type（依副檔名）。</summary>
    public static string ContentTypeOf(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream",
        };

    /// <summary>支援的副檔名清單（管理頁提示 / 錯誤訊息用）。</summary>
    public const string SupportedExtensions = "mp4 / webm / mov / pdf / png / jpg / jpeg / webp / gif";

    // ── 頁面生成 ────────────────────────────────────────────────────────────

    public static string BuildVideoPage(int face, string faceName, int rotation,
                                        bool loop, bool muted, bool controls)
    {
        string attrs = "autoplay playsinline" +
                       (loop ? " loop" : "") + (muted ? " muted" : "") + (controls ? " controls" : "");
        return Shell(face, faceName, rotation, $$"""
            <video id="v" src="/media/{{face}}/raw" {{attrs}}
                   style="width:100%;height:100%;object-fit:contain;background:#000;display:block;"></video>
            <script>
              var v = document.getElementById('v');
              v.addEventListener('error', function () {
                document.getElementById('stage').innerHTML =
                  '<div class="mediaErr">⚠ 影片載入失敗——檔案可能已被移走、或編碼不被瀏覽器支援' +
                  '（mp4 建議 H.264 + AAC）</div>';
              });
            </script>
            """);
    }

    public static string BuildImagePage(int face, string faceName, int rotation) =>
        Shell(face, faceName, rotation, $"""
            <img id="im" src="/media/{face}/raw"
                 style="width:100%;height:100%;object-fit:contain;display:block;"
                 onerror="document.getElementById('stage').innerHTML='&lt;div class=&quot;mediaErr&quot;&gt;⚠ 圖片載入失敗——檔案可能已被移走&lt;/div&gt;'">
            """);

    /// <summary>
    /// PDF 檢視頁：pdf.js（/assets/ 本地 vendored）單頁滿版 + 大顆翻頁按鈕（觸控可按）。
    /// 按鈕在 stage 內——跟內容一起旋轉，人看永遠在左右兩側。
    /// </summary>
    public static string BuildPdfPage(int face, string faceName, int rotation) =>
        Shell(face, faceName, rotation, $$"""
            <canvas id="pdfCanvas"></canvas>
            <div id="pdfErr" class="mediaErr" style="display:none"></div>
            <button id="prev" class="pageBtn" style="left:12px">‹</button>
            <button id="next" class="pageBtn" style="right:12px">›</button>
            <div id="pageInfo"></div>
            <script type="module">
              const errBox = document.getElementById('pdfErr');
              function fail(msg) { errBox.textContent = '⚠ ' + msg; errBox.style.display = 'flex'; }
              try {
                const pdfjs = await import('/assets/pdf.min.mjs');
                pdfjs.GlobalWorkerOptions.workerSrc = '/assets/pdf.worker.min.mjs';
                const doc = await pdfjs.getDocument('/media/{{face}}/raw').promise;
                const canvas = document.getElementById('pdfCanvas');
                const ctx = canvas.getContext('2d');
                const stage = document.getElementById('stage');
                let pageNum = 1, rendering = false, pending = 0;

                async function render() {
                  if (rendering) { pending = pageNum; return; }
                  rendering = true;
                  try {
                    const page = await doc.getPage(pageNum);
                    const base = page.getViewport({ scale: 1 });
                    const scale = Math.min(stage.clientWidth / base.width,
                                           stage.clientHeight / base.height);
                    const dpr = window.devicePixelRatio || 1;
                    const vp = page.getViewport({ scale: scale * dpr });
                    canvas.width = vp.width; canvas.height = vp.height;
                    canvas.style.width = (vp.width / dpr) + 'px';
                    canvas.style.height = (vp.height / dpr) + 'px';
                    await page.render({ canvasContext: ctx, viewport: vp }).promise;
                    document.getElementById('pageInfo').textContent = pageNum + ' / ' + doc.numPages;
                    document.getElementById('prev').disabled = pageNum <= 1;
                    document.getElementById('next').disabled = pageNum >= doc.numPages;
                  } finally {
                    rendering = false;
                    if (pending && pending !== pageNum) { pageNum = pending; pending = 0; render(); }
                    else pending = 0;
                  }
                }
                document.getElementById('prev').addEventListener('click',
                  () => { if (pageNum > 1) { pageNum--; render(); } });
                document.getElementById('next').addEventListener('click',
                  () => { if (pageNum < doc.numPages) { pageNum++; render(); } });
                window.addEventListener('resize', render);
                render();
              } catch (e) {
                fail('PDF 載入失敗：' + (e && e.message ? e.message : e) +
                     '——檢查檔案是否完好、assets/pdf.min.mjs 與 pdf.worker.min.mjs 是否在 exe 旁');
              }
            </script>
            """);

    /// <summary>file 模式錯誤頁（檔案不存在 / 不支援的副檔名）——旋轉轉正，現場讀得到。</summary>
    public static string BuildErrorPage(int face, string faceName, int rotation,
                                        string title, string detail) =>
        Shell(face, faceName, rotation, $"""
            <div class="mediaErr">
              <h1>⚠ {WebUtility.HtmlEncode(title)}</h1>
              <p>{WebUtility.HtmlEncode(detail)}</p>
              <p class="hint">支援副檔名：{SupportedExtensions}；PowerPoint 請先另存 PDF（README 有 SOP）</p>
            </div>
            """);

    /// <summary>共用外殼：黑底 + 旋轉 stage（與 wrap 頁同一套 CSS 數學）+ slot 標記 title。</summary>
    static string Shell(int face, string faceName, int rotation, string stageInner)
    {
        string stageCss = RotationCss.StageCss(rotation);
        string nameText = WebUtility.HtmlEncode(faceName);
        return $$"""
            <!DOCTYPE html>
            <html lang="zh-Hant">
            <head>
            <meta charset="utf-8">
            <title>{{nameText}} — WallProjector {{WrapPageBuilder.TitleMarker(face)}}</title>
            <style>
              html,body{margin:0;padding:0;width:100vw;height:100vh;overflow:hidden;background:#000;}
              #stage{position:fixed;left:0;top:0;{{stageCss}}display:flex;align-items:center;justify-content:center;}
              #pdfCanvas{display:block;background:#fff;}
              .pageBtn{position:absolute;top:50%;transform:translateY(-50%);z-index:2;
                width:64px;height:64px;border-radius:50%;border:0;cursor:pointer;
                background:rgba(91,141,238,.75);color:#fff;font-size:34px;line-height:1;}
              .pageBtn:disabled{opacity:.25;cursor:default;}
              #pageInfo{position:absolute;bottom:14px;left:50%;transform:translateX(-50%);z-index:2;
                color:#d8dce6;background:rgba(20,22,28,.7);border-radius:12px;padding:3px 14px;
                font:13px "Consolas","Microsoft JhengHei",monospace;}
              .mediaErr{position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;
                justify-content:center;gap:8px;background:#14161c;color:#d8dce6;text-align:center;
                font:15px/1.7 "Consolas","Microsoft JhengHei",monospace;padding:24px;box-sizing:border-box;}
              .mediaErr h1{font-size:20px;color:#ff4d4d;margin:0;}
              .mediaErr .hint,.mediaErr p.hint{color:#79808f;font-size:13px;}
            </style>
            </head>
            <body>
            <div id="stage">
            {{stageInner}}
            </div>
            </body>
            </html>
            """;
    }
}
