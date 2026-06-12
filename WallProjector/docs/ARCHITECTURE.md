# WallProjector 架構與程式流向（維護者文件）

> 2026-06-12 交接版。給接手改程式的人（或 AI）：整體資料流、模組職責、
> 不能隨便動的設計決策、交接時點已知問題與排查入口。
> 使用面（部署 / 管理頁 / 旋轉校正 / 現場驗證清單）在 `../README.md`。

## 定位（一句話）

**顯示層工具**：把網頁 / 本地影片 / PDF / 圖片，用無邊框瀏覽器殼擺到投影螢幕上
「某面牆」的對應區域，並把側牆 90° 旋轉轉正。**輸入完全不碰**——觸控由
TouchService 滑鼠模式提供，本工具只是它 `/venue` + `/config` 的普通 HTTP client。

## 整體資料流

```
TouchService (20111)                         WallProjector
┌──────────────────┐   GET /venue            ┌─────────────────────────────┐
│ venue 佈局（faces）│ ──────────────────────→ │ TouchServiceClient.Resolve() │
│ mouseMode.screen/ │   GET /config           │  live → cache → fallback     │
│ content（滑鼠校正）│ ──────────────────────→ │  ↓ CalibrationOverride.Apply │
└──────────────────┘                         │  （screenOverride 逃生口）     │
                                             └──────────┬──────────────────┘
                                                        ↓ TouchServiceState
   每面牆 face ──→ ScreenMapper.FaceScreenRect ──→ 螢幕物理 rect
                  （與 TouchService MouseDriver 同一套數學，獨立實作對拍釘住）
                                                        ↓
                  ProjectionEngine.StartLocked（每槽一個瀏覽器 process）
                  ├─ wrap：開 http://127.0.0.1:20121/wrap/{face}（iframe + CSS 旋轉）
                  ├─ cdp ：直開目標站 + CDP 注入旋轉 CSS（繞 X-Frame-Options）
                  └─ file：開 /media/{face}（影片/圖片/PDF，bytes 走 /media/{face}/raw）
                                                        ↓
                  BrowserLauncher（--app kiosk + 每槽獨立 user-data-dir）
                  WindowPositioner（EnumWindows 找窗 → 去邊框 → SetWindowPos 精確對位
                                    → GetWindowRect 讀回驗證）
                  watchdog（2s tick：死亡偵測 → 指數退避重啟 1/2/4/8/16s ×5）
```

## 模組職責表

| 檔案 | 職責 | 動它之前要知道 |
|---|---|---|
| `Program.cs` | 入口組裝；**Main 第一行宣告 DPI awareness** | 順序不可換：DPI → config → resolve → engine → http |
| `Core/DpiAwareness.cs` | PMv2 宣告（三層 fallback） | V3 等比例錯位修正的地基；拿掉就回到 DPI 虛擬化 |
| `Core/ScreenMapper.cs` | canvas norm → 螢幕 rect 數學 | **與 TouchService MouseDriver 逐行對拍**，改一邊必須同步另一邊，否則摸的位置跟看的內容錯開 |
| `Core/TouchServiceClient.cs` | /venue + /config 抓取；live→cache→fallback 失效鏈 | 非 live 一律帶 Warning 給管理頁，別吞掉 |
| `Core/CalibrationOverride.cs` | screenOverride/contentOverride 套用 | 包在 Resolve 外層，client 保持純粹；每次 resolve 都 WARN 提醒（防 override 被遺忘） |
| `Core/VenueInfo.cs` | /venue JSON 最小解析 + 面像素尺寸公式 | 公式與 TouchService SensorFaceConfig 對拍（h:16×32 / v:32×16） |
| `Core/MonitorEnumerator.cs` | 螢幕清單 + DPI/縮放%（診斷） | 自帶一份不跨專案引用（兩 exe 獨立部署） |
| `Config/ProjectorConfig.cs` | projections.json 讀寫 + 容錯正規化 | V1 檔案不改即用（缺 mode = wrap）；rotation 只收 0/90/180/270 |
| `Engine/ProjectionEngine.cs` | 槽生命週期 + watchdog；所有狀態單一 `_gate` lock | 跑長操作（對位輪詢/CDP 連線）一律背景 Task，不准佔 `_gate` |
| `Engine/BrowserLauncher.cs` | 找 Edge/Chrome + 組 kiosk 參數 | **每槽獨立 user-data-dir 是必須**——否則新視窗併進既有 process，watchdog 誤判死亡；`--window-position/size` 單位是 DIP，只當粗對位 |
| `Engine/WindowPositioner.cs` | EnumWindows（title marker 或 pid）→ 去邊框 → SetWindowPos → 讀回驗證 | 失敗會 log 可見視窗清單（≤20）幫現場定位 |
| `Engine/CdpClient.cs` | 極簡 CDP client（ClientWebSocket、JSON-RPC） | 單執行緒序列使用；失敗丟例外不靜默 |
| `Engine/CdpInjector.cs` | CDP 注入旋轉 CSS + title 標記 | addScriptToEvaluateOnNewDocument 保 SPA 導航存活；title 標記含 1s 守護 interval（站方 JS 會重設 title） |
| `Web/HttpServer.cs` | 自寫最小 HTTP（免 netsh urlacl、零依賴） | 大檔走 FileStream Range 串流，不進記憶體 |
| `Web/AdminApi.cs` | 管理頁 + API 路由 | **寫入端點僅 loopback**（403），唯讀 /api/state 內網可看 |
| `Web/WrapPageBuilder.cs` | wrap 頁生成 + `[WP-slot-N]` title 標記 | 觸控不需反算：瀏覽器 hit-testing 原生處理 CSS transform |
| `Web/MediaPageBuilder.cs` | file 模式頁面（video/img/pdf.js 分流） | pdf.js 是 vendored 本地檔——場館可能沒外網 |
| `Web/RotationCss.cs` | **旋轉數學單一真相源**（三模式共用） | 90/270 寬高對調 + origin top left + translate；改這裡三條路一起變 |
| `Web/FrameBlockChecker.cs` | XFO/CSP 嵌入封鎖預檢（server 端 header 檢查） | client 端偵測不可靠（被擋也 fire load）——header 預檢是唯一確定性判斷 |

## 不能隨便動的設計決策

| 決定 | 理由 |
|---|---|
| 獨立 exe，不併入 TouchService | 顯示層崩潰不能拖垮觸控基礎設施；TouchService 程式碼從 V1 到 V3 零修改 |
| rect 兩角各自過 MapCanvasToScreen 再相減 | 與滑鼠落點走完全相同路徑（含 clamp），內容與觸控天然對齊 |
| 每槽獨立瀏覽器 process + profile | watchdog 的 process 監看前提；殺也要 `entireProcessTree` |
| DPI 宣告在 Main 第一行 | process 級一次性；任何視窗/螢幕 API 之後才宣告 = 無效 |
| CDP 注入失敗 → 槽標 Failed + 收瀏覽器 | 寧可黑屏 + 管理頁紅字，不讓內容歪著掛在牆上沒人發現 |
| rotation 放 projections.json 不放 venue JSON | V1「不動 TouchService」的約定；多場域時才遷成 venue 的 viewRotation（BACKLOG #6） |

## 交接時點已知問題（2026-06-12，未解決，轉交接手者）

> 現場（704）回報投影**尚不可用**，兩個問題。v3 已內建診斷工具但交接前
> 未完成現場驗證循環——接手者從這裡開始。

### 問題 1：視窗位置不對（等比例錯位——牆面/地板各佔一半）

- **已做的分析**：三個疊加嫌疑——(a) v2 以前 process 是 DPI-unaware，縮放 ≠ 100% 時
  SetWindowPos 座標被虛擬化；(b) Chromium `--window-position/size` 單位是 DIP，
  被縮放倍數放大；(c) CDP 模式 pid 認窗可能失敗 → 精確對位沒跑到、停在 (b) 的錯誤粗對位。
  同機滑鼠模式準（SendInput 正規化不經 DPI）+ 視窗歪 = DPI 問題指紋。
- **v3 已修**：PMv2 宣告（全程物理像素）+ CDP title 標記注入（認窗備援）+
  對位讀回驗證 + 管理頁顯示螢幕縮放% 與預期/實際 rect + `screenOverride` 逃生口。
- **接手者排查步驟**：裝 v3 → 管理頁看螢幕清單縮放%（≠100% 黃字）→ 啟動槽看
  「實際 (x,y) w×h」紅綠 → 還是紅就看啟動 log 的 `[DPI]` 行與 `[Window]` WARN
  （含當下所有可見視窗 title+pid）→ 最後手段用 `screenOverride` 直接指定物理 rect。
  詳見 README「V3：DPI 修正與自我診斷」段。

### 問題 2：YouTube 無法播放（cdp 模式）

- **現象**：cdp 模式直開 YouTube（iframe 封鎖已繞過）仍無法播放。**根因未定位**
  ——交接前沒有機會在場館機上看實際卡在哪個畫面。
- **排查方向（按可能性）**：
  1. 獨立 profile 無登入/同意狀態——YouTube 對全新 profile 可能出 cookie 同意頁
     或「確認你不是機器人」卡住。驗法：場館機手動開同一個 profile
     （`profiles\slot-N`）看畫面停在哪。
  2. 自動播放限制——已帶 `--autoplay-policy=no-user-gesture-required`，但
     YouTube 有自己的策略（非 muted 自動播放常被站方擋）。
  3. DRM/codec（Edge 一般沒問題）或場館網路防火牆。
- **現成替代路線（建議直接用）**：要在牆上播影片，**file 模式才是穩的**——
  影片抓成 mp4 放本機，槽切 `mode: file` 指向檔案路徑，循環/靜音/控制列都有
  選項，離線可用、無站方限制。YouTube 直投本質是跟站方反自動化機制對抗，
  投入產出比低，除非需求非 YT 不可，否則建議列為不修。

## 測試地圖（167 個，`dotnet test`）

- `ScreenMapperTests` — rect 數學，用 TouchService MouseDriverTests 已知值釘住（對拍）
- `RotationCssTests` / `WrapPageBuilderTests` / `MediaPageBuilderTests` — 旋轉宣告與頁面生成 golden
- `ProjectorConfigTests` — config 容錯（V1 相容、正規化、override 欄位）
- `TouchServiceClientTests` / `CalibrationOverrideTests` — 失效鏈與逃生口
- `CdpInjectorTests` — 注入 JS 組字 golden
- `CdpRealChromiumTests` — **真 chromium headless E2E**：CDP 連線 → 注入 →
  getComputedStyle 矩陣驗證 + 導航存活 + title 標記（站方重設後 1s 補回）
- `ProjectionEngineTests` — 槽生命週期 / watchdog / 模式分流
- `HttpRangeTests` / `HttpServerTests` — Range 解析與路由

Windows-only 的部分（DPI 實際生效、SetWindowPos、螢幕列舉）Linux 測不到——
現場驗證清單在 README 最後一段。

## 建置與發布

```bash
dotnet build && dotnet test
# 發布（參數逐一明寫——漏掉會產出 framework-dependent 壞檔，正確 exe ~35MB）
dotnet publish src/WallProjector -c Release -r win-x64 \
  -p:SelfContained=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

zip 結構：`WallProjector.exe` + `projections.json` 範本 + `assets/`（pdf.js×2）+ `README.md`。
