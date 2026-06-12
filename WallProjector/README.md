# WallProjector — 牆面投影管理工具（V2）

> 給現場人員：要把內容投到某面牆上，開管理頁**選牆面、選模式、貼網址或檔案路徑、按啟動**就完成。
> 不用再手動拖視窗對位；左右牆臂的 90° 旋轉由工具處理，人看是正的。
>
> V2 新增：**CDP 模式**（YouTube 等擋 iframe 的網站也能投）、**本地檔案模式**
> （影片 / PDF / 圖片，PowerPoint 另存 PDF 即可投簡報）。

## 這是什麼

CyberCube 光域場域的投影螢幕顯示整個場域 canvas（2688×3840）的**十字展開圖**
（中央地板、四周牆臂）。WallProjector 把內容用無邊框瀏覽器視窗精確擺到指定牆面的
螢幕區域上，並依牆面方向旋轉內容。

- **獨立 exe（顯示層）**，與 TouchService（輸入層）完全解耦——故障互不影響
- 觸控由 TouchService 滑鼠模式提供（摸牆 = 點擊），本工具**不碰任何輸入**
- 不改 TouchService 任何程式碼（旋轉值放本工具 config，見下）

## 三種投影模式（V2）

每個投影槽有 `mode` 欄位，依內容選：

| 模式 | 管理頁名稱 | 用途 | 原理 |
|---|---|---|---|
| `wrap`（預設） | 網頁嵌入 | **一般網頁**（自家頁面、儀表板、不擋 iframe 的站） | 瀏覽器載入本工具 wrap 頁，目標站 iframe 嵌入、外層 CSS rotate |
| `cdp` | 網頁CDP | **擋 iframe 的網站**（YouTube、Google、多數大站） | 瀏覽器直接開目標站（不經 iframe）+ DevTools protocol 注入旋轉 CSS 到頁面 root |
| `file` | 本地檔案 | **影片 / PDF / 圖片**（簡報先另存 PDF） | 本工具 HTTP server 的 /media 端點伺服檔案，旋轉播放/檢視頁 |

**怎麼選**：
1. 先用 wrap（最不打擾目標頁）。貼上網址後若管理頁出現
   「⚠ 此站擋嵌入」提示 → 按**「切到 CDP 模式」**一鍵切換即可。
2. 投影片、影片、文件 → 本地檔案模式（檔案放到場館機，填路徑）。

### wrap 與 cdp 的差異（檯面上講清楚）

- wrap 的旋轉套在我們自己的容器上，**不碰目標頁**——最安全，但被
  `X-Frame-Options` / CSP `frame-ancestors` 擋的站無法用。
- cdp 直接開目標站原頁（嵌入限制完全無關），旋轉 CSS 打在該頁的 `html` 元素上
  （`transform` + 寬高對調 + `!important`）。**個別網站自己的 CSS / JS 佈局邏輯
  可能與旋轉 transform 衝突**（例如自己量 viewport 寬高來排版的站、自訂滾動容器），
  多數內容站沒問題；遇到顯示異常請回報該網址，個案處理。
- cdp 的旋轉在頁面導航後（SPA 跳轉、點連結）由
  `Page.addScriptToEvaluateOnNewDocument` 自動補打——換頁不會「轉回去」。
- cdp 模式 rotation=0 時只開原站、完全不注入。
- CDP 連線失敗（debug port 沒起來 / timeout）→ 槽標**失敗**紅字並收掉瀏覽器，
  **不會**讓內容歪著掛在牆上沒人發現。

### 本地檔案模式支援的格式

| 類型 | 副檔名 | 呈現 |
|---|---|---|
| 影片 | mp4 / webm / mov | `<video>` 自動播放；槽選項：**循環**（預設開）、**靜音**（預設開）、**控制列**（預設關，開了觸控可按） |
| PDF | pdf | 單頁滿版 + 左右大顆翻頁按鈕（觸控可按；vendored pdf.js，**離線可用**） |
| 圖片 | png / jpg / jpeg / webp / gif | 滿版顯示（contain，不裁切） |

- 路徑填**絕對路徑**（`C:\media\demo.mp4`）或**相對 projections.json 的路徑**
- mp4 建議 H.264 + AAC（Edge/Chrome 原生支援最穩）
- **路徑安全**：HTTP 只伺服 config 裡明示的那一個檔案；URL 帶不進任何路徑成分，
  目錄遍歷結構上不可能（有測試釘住）

### PowerPoint 投影 SOP（簡報 → PDF）

1. PowerPoint 開啟簡報 → **檔案 → 另存新檔 → 檔案類型選 PDF**
   （或「匯出 → 建立 PDF」；動畫會攤平成靜態頁——投牆場景本來就建議無動畫）
2. PDF 放到場館機（例 `C:\media\簡報.pdf`）
3. 管理頁該面選**本地檔案**、貼路徑、按啟動
4. 翻頁：摸牆面左右兩側的 ‹ › 大按鈕（TouchService 滑鼠模式）

## 跟 TouchService 的關係

```
感應模組 ──UDP──▶ TouchService（輸入層）──滑鼠模式──▶ OS 游標（摸牆=點）
                       │ GET /venue   （十字佈局：faces 的 canvas 幾何）
                       │ GET /config  （mouseMode.screen + content：投影螢幕 rect 與校正）
                       ▼
                 WallProjector（顯示層）
                       │ 牆面 canvas rect → norm → 螢幕 rect（公式與滑鼠模式同一套，
                       │ 單元測試對拍——觸控落點跟投影內容天然對齊）
                       ├─ HTTP :20121 ── 管理頁 + /wrap/<face> + /media/<face> + /assets
                       ├─ wrap/file：Edge --app=http://127.0.0.1:20121/...
                       └─ cdp：Edge --app=<目標站> --remote-debugging-port=<每槽獨立>
                              └─ CDP WS（127.0.0.1）注入旋轉 CSS
                            （每槽獨立 process，Win32 去邊框 + SetWindowPos 精確對位）
```

WallProjector 是 TouchService 的**普通 HTTP client**。TouchService 不可達時依序
fallback：上次成功的本地 cache（`touchservice-cache.json`，自動生成）→
`projections.json` 的 `fallback`（screen/content + 可選 venueFile），並在管理頁警示。

## 目錄結構

```
WallProjector/
├── WallProjector.sln
├── projections.json             ← 設定範本（欄位見下）
├── assets/                      ← vendored pdf.js（pdf.min.mjs + pdf.worker.min.mjs，離線可用）
├── src/WallProjector/
│   ├── Core/                    ← venue 解析、canvas→螢幕映射（對拍 TouchService）、TS client
│   ├── Engine/                  ← 投影引擎：瀏覽器殼、Win32 視窗對位、watchdog、
│   │                               CdpClient/CdpInjector（CDP 連線與旋轉注入）
│   ├── Web/                     ← 自寫 HTTP server（含 Range）、wrap/media 頁生成、
│   │                               RotationCss（旋轉數學唯一真相源）、管理 API、管理頁（內嵌）
│   └── Program.cs               ← 入口 + stdin 指令
└── tests/WallProjector.Tests/   ← 148 個測試（xunit；含真 chromium CDP E2E）
```

## 快速上手（開發機，Linux 可建可測）

```bash
dotnet build WallProjector.sln
dotnet test  WallProjector.sln          # 148 個測試（CDP E2E 需要 chromium/chrome，沒有會 Skip）
dotnet run --project src/WallProjector  # 跑起來（無 TouchService 時走 fallback 並警示）
# 瀏覽器開 http://127.0.0.1:20121/
```

## 部署（場館機）

1. 與 TouchService **同一台機器**（管理 API 都只准 loopback 寫入；wrap/media 頁 origin 也是 127.0.0.1）
2. 解壓 `WallProjector-win64-v2.zip` 到任意資料夾（如 `C:\WallProjector\`）——
   **assets 資料夾要跟 exe 放一起**（PDF 投影需要）
3. **開機順序：TouchService 先、WallProjector 後**——WallProjector 啟動時抓
   /venue + /config；先後順序錯了會落到 cache/fallback（功能照常，但佈局若有變動不會反映，
   開管理頁按「重新連線 TouchService」即可）
4. 雙擊 `WallProjector.exe`，開 `http://127.0.0.1:20121/`
5. port 衝突時改 `projections.json` 的 `httpPort` / `cdpBasePort`

| port | 用途 |
|---|---|
| 20111 | TouchService（WS + /venue + /config）——既有 |
| 20121 | WallProjector 管理頁 + wrap/media 頁（本工具） |
| 9333+face | CDP 模式各槽的 debug port（僅 127.0.0.1；`cdpBasePort` 可改、槽可指定 `cdpPort`） |

## projections.json

```jsonc
{
  "httpPort": 20121,
  "touchServiceBaseUrl": "http://127.0.0.1:20111",
  "autoRestore": true,          // 啟動時自動還原所有 enabled 槽
  "restartOnCrash": true,       // 瀏覽器死亡自動重拉（指數退避 1/2/4/8/16s，最多 5 次）
  "topmost": true,              // 投影視窗 HWND_TOPMOST 置頂
  "browserProfileDir": "profiles",   // 每槽獨立 browser profile（相對 config 所在目錄）
  "cdpBasePort": 9333,          // cdp 模式 debug port 基準（槽 port = base + face，槽可用 cdpPort 覆寫）
  "fallback": {                 // TouchService 不可達且無 cache 時的後備值
    "screen":  { "x": 0, "y": 0, "width": 1920, "height": 1080 },
    "content": { "offsetX": 0, "offsetY": 0, "scaleX": 1.0, "scaleY": 1.0 },
    "venueFile": null           // 可指向 venue JSON 複本（如 TouchService 的 venues/704.json）
  },
  "projections": [
    // wrap：一般網頁（V1 行為；mode 缺欄位 = wrap，V1 的 config 不改即用）
    { "face": 4, "url": "https://example.com/", "rotation": 90, "mode": "wrap", "enabled": true },
    // cdp：擋 iframe 的網站（YouTube 等）；cdpPort 可省略（= cdpBasePort + face）
    { "face": 0, "url": "https://www.youtube.com/watch?v=xxxx", "rotation": 270, "mode": "cdp", "enabled": true },
    // file：本地檔案（影片選項 loop/muted/controls 只對影片生效）
    { "face": 1, "url": "C:\\media\\demo.mp4", "rotation": 0, "mode": "file",
      "loop": true, "muted": true, "controls": false, "enabled": true }
  ]
}
```

- `face`：GET /venue 的 faces 陣列 index（704 場域 0~6；管理頁直接列名稱）
- `rotation`：0/90/180/270（CSS 順時針度數）
- `mode`：`wrap` / `cdp` / `file`（缺欄位 = wrap——**V1 的 projections.json 向後相容**）
- 檔案根節點直接寫**槽陣列**也接受（其餘吃預設）
- 管理頁的變更**即時生效並寫回本檔**；手改檔案要重啟才生效

## 管理頁用法（http://127.0.0.1:20121/）

- 列出 venue 所有面（含地板，標註「通常投牆面」）：每面一列——
  **模式下拉（網頁嵌入/網頁CDP/本地檔案）**、URL/檔案路徑輸入框（提示隨模式切換）、
  rotation 下拉（0/90/180/270）、啟動/停止、狀態
- 狀態：`運行中` / `已停` / `重啟中`（瀏覽器死亡退避中）/ `失敗`（連死 5 次放棄 /
  CDP 注入失敗 / 檔案不存在，紅字一眼可辨）+ **目前模式 tag**
- wrap 模式偵測到目標站擋嵌入 → 該列出現「⚠ 此站擋嵌入 →〔切到 CDP 模式〕」一鍵切換
- file 模式檔案不存在 / 副檔名不支援 → 該列紅字直接顯示（不用等啟動失敗）
- **寫入操作僅接受本機（loopback）**，內網其他機器開管理頁只能看
- 「還原全部 enabled」= 啟動所有 enabled 槽；「重新連線 TouchService」= 重抓佈局/校正
  （跑著的槽 rect 不會自動變——重啟該槽才套用）

### stdin 指令（console）

`status` / `restore` / `start <face>` / `stop <face>` / `quit`（`--help` 看啟動參數）

## 旋轉校正流程（現場看著轉）

十字展開的左右牆臂在螢幕空間相對「人在場內看牆」轉了 90°——投影內容要反向旋轉。

1. 啟動該牆面的投影槽（隨便先選一個 rotation）
2. **人站場內看牆**：內容是橫的/倒的 → 管理頁該面的 rotation 下拉換一檔（變更即時重啟套用）
3. 看正了就完成——值已自動寫回 projections.json，下次開機自動還原

管理頁的預設值是依十字展開方位的合理猜測（左右臂 ±90、上下臂 0/180、地板 0），
**以現場實看為準**。

> 為什麼旋轉值放這裡不放 TouchService：V1 決定不動 TouchService（它的座標是物理
> 位置真相，旋轉純屬顯示層）。未來多場域時才把 `viewRotation` 遷成 venue JSON 欄位
> ——TouchService/docs/ROADMAP-PROJECTION.md 有記。

### 旋轉的實作（三模式同一套數學）

旋轉 CSS 集中在 `Web/RotationCss.cs`（單一真相源，單元測試釘住字串）：

| rotation | 容器尺寸 | transform |
|---|---|---|
| 0 | 100vw × 100vh | （無） |
| 90 | **100vh × 100vw（寬高對調）** | `rotate(90deg) translateY(-100vw)`，origin top left |
| 180 | 100vw × 100vh | `rotate(180deg)` |
| 270 | **100vh × 100vw** | `rotate(270deg) translateX(-100vh)`，origin top left |

90/270 的推導：容器在自身座標先平移再旋轉，旋轉後 bounding box 恰好等於視窗。
套用對象依模式：wrap/media 頁套在 `#stage` 容器、cdp 直接打在目標頁 `html` 元素
（同樣宣告值 + `!important`，CDP E2E 以 `getComputedStyle` 驗證過矩陣）。
**觸控不用反算**——瀏覽器 hit-testing 原生處理 CSS transform，TouchService 滑鼠模式
點在螢幕座標上，瀏覽器自己把事件路由進旋轉後的內容。

## Watchdog 行為

- 每 2s 檢查各槽瀏覽器 process；死亡 → 指數退避重啟（1/2/4/8/16s），連死 5 次標
  `失敗` 放棄（管理頁手動啟動重試；存活超過 60s 退避歸零）
- **手動關掉投影視窗也算「死亡」會被重拉**——要停止請用管理頁/stdin 的停止
  （會把 enabled 關掉並寫回 config）
- 每槽用獨立 `--user-data-dir`（`profiles/slot-N/`）：強制獨立瀏覽器 process，
  否則 Edge/Chrome 會合併進既有 process、watchdog 監看與 kill 都失準
  （cdp 模式還有第二個理由：Chromium 136+ 預設 profile 不給開 remote debugging port）
- cdp 模式重啟後會**重新注入旋轉**（注入跟著 StartLocked 走）

## Windows 現場驗證清單（Linux 測不到的部分）

> 程式邏輯（rect 數學、三模式頁面生成、CDP 注入腳本與真 chromium E2E、Range、
> config、API、fallback 鏈）已由 148 個單元/整合測試 + Linux smoke test 覆蓋；
> 以下 process/視窗行為**只能 Windows 實機驗**：

**V1 既有（V2 重驗一輪）**
- [ ] Edge 標準路徑找到並以 `--app=` 啟動（無 Edge 時 fallback Chrome）
- [ ] 視窗對位：EnumWindows 以 title 標記 `[WP-slot-N]` 找到視窗、去
      WS_CAPTION/WS_THICKFRAME 後 SetWindowPos 與管理頁顯示的 rect 一致（無邊框、無縫）
- [ ] `topmost: true` 時視窗蓋過工作列與其他視窗；`false` 時正常 z-order
- [ ] 投影內容與 TouchService 滑鼠模式落點對齊（摸牆面四角，游標在內容四角）
- [ ] 左/右牆臂 rotation 90/270 現場實看方向正確（不對就下拉換檔）
- [ ] watchdog：工作管理員殺掉投影瀏覽器 → 1s 後自動重拉、連殺 5 次後標失敗
- [ ] 關 WallProjector（quit / Ctrl+C / 關 console）→ 所有投影瀏覽器一起關閉
- [ ] 多槽同開（如左牆 + 右牆 + 下牆）：各自獨立 process、互不影響
- [ ] 開機順序測試：先開 WallProjector 再開 TouchService → cache 警示 → 「重新連線」恢復
- [ ] DPI 縮放 ≠ 100% 的機器：rect 是否仍精確（瀏覽器 --window-position 受 DPI 影響時
      以 SetWindowPos 結果為準）

**V2 新增**
- [ ] **CDP 模式（Edge）**：YouTube 網址 + rotation 90 → 直開原站、內容轉正、
      影片可播；管理頁「CDP 注入中…」短暫出現後消失
- [ ] CDP 模式視窗對位：title 是目標站的（無 slot 標記）→ **pid 比對**仍正確去邊框對位
- [ ] CDP 模式站內導航（點 YouTube 其他影片）→ 旋轉存活不轉回
- [ ] CDP 注入失敗情境（手動把該槽 cdpPort 設成被占用的 port）→ 槽標失敗紅字、瀏覽器被收掉
- [ ] wrap 模式貼 YouTube → 管理頁出現「此站擋嵌入」提示 → 一鍵切 CDP 成功
- [ ] **file 模式影片**：mp4 自動播放、循環、靜音；controls 開啟時觸控可按、可 seek
      （seek = Range request 路徑）
- [ ] file 模式 PDF：單頁滿版、‹ › 觸控翻頁、**離線**（拔網路線）仍可載入（vendored pdf.js）
- [ ] file 模式圖片：滿版 contain 顯示
- [ ] file 模式檔案路徑含中文 / 空白 → 正常伺服
- [ ] 檔案被移走後：管理頁紅字「檔案不存在」、啟動失敗訊息明確
- [ ] V1 的 projections.json（無 mode 欄位）原檔直接用 → 全部當 wrap 正常跑

## 發布

```bash
dotnet publish src/WallProjector/WallProjector.csproj -c Release -r win-x64 \
  -p:SelfContained=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true \
  -o publish/win-x64
```

產物：單檔 `WallProjector.exe`（含 runtime）。`_dist/WallProjector-win64-v2.zip`
= exe + projections.json 範本 + **assets/（pdf.js 兩檔）** + 本 README。v1 zip 保留。
