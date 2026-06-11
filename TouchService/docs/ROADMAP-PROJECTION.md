# 投影程式設計備忘（未動工，2026-06-11 深夜記錄）

> Luke 提出、待 v7 現場驗證後一起細化。**獨立的新 exe**，不是 TouchService 的功能。

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
