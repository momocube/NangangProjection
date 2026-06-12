# 投影程式設計備忘（2026-06-11 深夜記錄）

> Luke 提出、待 v7 現場驗證後一起細化。**獨立的新 exe**，不是 TouchService 的功能。
>
> **✅ V1 已實作（2026-06-12）：`WallProjector/`（repo 根目錄，獨立程式）**
> - 下方「v1 功能草圖」全項落地＋牆面旋轉（wrap 頁 CSS rotate）＋管理頁（port 20121）
>   ＋watchdog＋autoRestore；64 個測試（rect 對拍 MapCanvasToScreen）；
>   `_dist/WallProjector-win64-v1.zip`。文件：`WallProjector/README.md`
> - **決定：rotation 放 WallProjector 自己的 projections.json，不動 TouchService**
>   ——本文件下方「venue JSON 加欄位 viewRotation」維持未來式（多場域時才遷；
>   屆時 WallProjector 改為優先讀 /venue 的 viewRotation、config 值作 override）
> - TouchService 程式碼零修改（V1 約定守住）；WallProjector 只是 /venue + /config
>   的普通 HTTP client，不可達時 cache/fallback 降級
> - 視窗對位/watchdog 等 Windows 行為的現場驗證清單在 WallProjector/README.md
>
> **✅ V2 已實作（2026-06-12）：CDP 模式 + 本地檔案模式（V1 現場回報的兩個缺口）**
> - 每槽 `mode: wrap | cdp | file`（缺欄位 = wrap，V1 config 向後相容）
> - **cdp**：解 X-Frame-Options/CSP 擋 iframe（YouTube 等）——直開原站
>   `--remote-debugging-port=<每槽獨立>`，ClientWebSocket 連 CDP 注入旋轉 CSS 到
>   html 元素（`Page.addScriptToEvaluateOnNewDocument` + `Runtime.evaluate`，
>   SPA 導航後旋轉存活）；連線失敗 → 槽標失敗紅字 + 收瀏覽器，不靜默；
>   wrap 模式預檢偵測被擋 → 管理頁「切到 CDP」一鍵切換
> - **file**：影片 mp4/webm/mov（`<video>` loop/muted/controls 槽選項）、PDF
>   （vendored pdf.js 離線可用、觸控翻頁）、圖片——`/media/<face>` 端點 +
>   Range request（seek 必需）+ 路徑白名單（只伺服 config 明示檔案）；
>   PPT 投影 SOP = 另存 PDF（README）
> - 旋轉數學三模式同源（`Web/RotationCss.cs` 單一真相源）；148 個測試
>   （含**真 chromium headless CDP E2E**：注入後 getComputedStyle 矩陣驗證 +
>   導航存活）；`_dist/WallProjector-win64-v2.zip`；TouchService 仍零修改
> - 下方「V2（任意視窗）Windows.Graphics.Capture 合成」**改編號為 V3**（仍未做）
>
> **✅ V3 已實作（2026-06-12）：DPI 修正 + 自我診斷（704 現場「視窗等比例錯位」回報）**
> - 症狀：投影視窗等比例錯位（貼滿牆 → 牆一半地板一半），滑鼠模式卻準 →
>   根因：WallProjector 是 DPI-unaware process，投影螢幕縮放 ≠ 100% 時 SetWindowPos
>   座標被 DPI 虛擬化（×縮放倍數）；疊加 Chromium `--window-position` 單位是 DIP、
>   CDP 模式 pid 比對可能失敗（Edge launcher）→ 精確對位沒跑到停在粗對位
> - 修法：入口宣告 Per-Monitor V2 DPI awareness（三層 fallback）＋對位後 GetWindowRect
>   讀回驗證（±2px，管理頁紅字）＋螢幕清單診斷（rect + 縮放%）＋ CDP title 標記注入
>   （pid 比對備援）＋ screenOverride/contentOverride 逃生口；167 個測試；
>   `_dist/WallProjector-win64-v3.zip`；TouchService 仍零修改
> - 註：本段佔用 V3 編號——「任意視窗 Windows.Graphics.Capture 合成」再順延為 V4（仍未做）

## 需求（Luke 原述整理）

- 在牆面區域的螢幕範圍上，做「虛擬螢幕投影」：把**特定視窗**投影到**特定牆面**的位置
- 不只顯示，要能**觸控**（使用者直接摸牆操作網頁）
- 目的：人員要把網頁放上牆面時，選一選就好，不用自己手動拖視窗對位
- 觸控模式（TouchService）獨立運作；投影模式額外啟用
- 未來可能投影很多東西（多視窗、多牆面）

## 架構決定（已討論定案）

1. **獨立 exe**（投影管理程式），不進 TouchService：
   - TouchService = 輸入層（感應→觸控事件），投影程式 = 顯示層（視窗擺位）
   - 投影程式是普通訂閱端：WS 拿觸控、`GET /venue` 拿十字佈局與牆面螢幕座標
   - 故障隔離：投影崩潰不影響觸控基礎設施
2. **v1 輸入走 TouchService 滑鼠模式**：投影程式只管擺視窗，「摸=點」由滑鼠模式提供

## v1 功能草圖

- 列出目前系統視窗（EnumWindows 過濾可見頂層視窗）
- 選視窗 + 選牆面（從 /venue 的 faces 算出該面在投影螢幕上的 rect——
  即 mouseMode 的 canvas→screen 同一套映射數學，可共用公式）
- SetWindowPos 移動/縮放到該 rect；選項：去邊框（kiosk 樣式）、置頂
- 配置持久化（JSON）：開機自動還原各視窗到各牆面
- 視窗死掉的偵測與重新喚起（可選：監看 process、自動重啟瀏覽器 kiosk）

## 已知的未來坑（設計時留口）

- **單游標限制**：滑鼠模式全機一顆游標——兩面牆兩個人同時摸會打架。
  升級路：**Windows Touch Injection API**（InitializeTouchInjection / InjectTouchInput）
  注入真正多點觸控，前景視窗原生收 touch 事件；TouchService 的多點資料現成。
  → 投影程式的「輸入策略」要做成可抽換（v1=依賴全域滑鼠模式；v2=touch injection）
- 瀏覽器專用替代路：CDP（DevTools protocol）可對特定分頁注入 touch，不動全域輸入——
  若投影對象永遠是瀏覽器，這條比 touch injection 更可控
- 多螢幕：投影螢幕 rect 來源與 mouseMode.screen 一致，留意 content 校正值共用

## 待 Luke 確認的開放問題（細化時討論）

1. 投影對象以瀏覽器為主？還是任意視窗（簡報、影片播放器）？→ 影響輸入策略選型
2. 一面牆一個視窗，還是一面牆可以切多區？
3. 操作介面要做在哪：獨立小視窗？還是併進 TouchService UI 加一個 tab（跨 exe 用 API 溝通）？
4. 人員權限：誰可以改投影配置（同 config API 的 loopback-only 原則？）

## 追加（2026-06-11 23:15，Luke 提出）

### 第三模式：多點觸控（v8 候選）
- touchMode 加 `multitouch`：Windows Touch Injection API（InitializeTouchInjection /
  InjectTouchInput）把 TouchService 的多點資料注入成真實 Windows 觸控
- 落點下的視窗原生收 touch 事件（瀏覽器 touchstart 直接動），多人多牆同時操作成立
- 映射與面勾選沿用滑鼠模式那套（canvas→screen）；注意注入生命週期：觸點需持續
  餵更新（Windows 對 stale contact 會自動 cancel），由 flush timer 以固定頻率重注入
- 解掉滑鼠模式的單游標限制 → 投影程式 v1 直接受益
- 只能現場 Windows 實機驗證

### 新場域複製（已支援，記流程）
- 硬體協議不變；寫新 `venues/XXX.json`（畫布 + 各面 rawMin/Max/翻轉/origin）；
  config 指向；重啟——程式零修改，下游訂閱端經 hello 自動適應
- 要重驗：該場域重送週期（閥值不同會變 → releaseTimeoutMs 重調）、硬體同款確認
- 未來甜頭：**校正精靈**——UI 引導踩指定角落自動算 rawMin/Max 寫 JSON，
  校正從工程師半天變現場人員十分鐘

## 追加（2026-06-12，v6+v7 現場驗收全過後）

### 牆面 90° 旋轉問題（Luke 現場發現，投影工具核心需求）
- 十字展開的左右臂在螢幕空間相對「人在場內看牆」轉了 90°——滑鼠無所謂，
  投網頁上去人看是橫的
- **解法分層**：
  - V1（網頁）：投影工具自管瀏覽器殼（無邊框 kiosk）擺到牆面 rect，內容包
    CSS rotate 容器（寬高對調）。瀏覽器 hit-testing 原生支援 transform——
    觸控/滑鼠座標自動反算，零額外處理。✅ 已實作
  - V2（擋 iframe 的外站 + 本地檔案）：CDP 注入旋轉 CSS（直開原站）＋
    /media 本地檔案投影（影片/PDF/圖片）。✅ 已實作（2026-06-12，見頂部）
  - V3（任意視窗）：Windows.Graphics.Capture 擷取 + 旋轉合成 + 輸入反向轉發（重，後排，未做）
- **venue JSON 加欄位**：每面 `viewRotation`（0/90/180/270，對人的正向），
  投影工具從 /venue 讀，多場域沿用——旋轉是場域屬性不是工具設定
- TouchService 不動：它的座標是物理位置真相，旋轉純屬顯示層

### 驗收記錄
- 2026-06-12 上午：v6（托盤/滑鼠十字對位/面勾選/終端 tab）+ v7（episode 自動鎖）
  現場全項通過
