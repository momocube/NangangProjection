# WallProjector — 牆面投影管理工具（V1）

> 給現場人員：要把網頁投到某面牆上，開管理頁**選牆面、貼網址、按啟動**就完成。
> 不用再手動拖視窗對位；左右牆臂的 90° 旋轉由工具處理，人看是正的。

## 這是什麼

CyberCube 光域場域的投影螢幕顯示整個場域 canvas（2688×3840）的**十字展開圖**
（中央地板、四周牆臂）。WallProjector 把網頁用無邊框瀏覽器視窗精確擺到指定牆面的
螢幕區域上，並依牆面方向旋轉內容。

- **獨立 exe（顯示層）**，與 TouchService（輸入層）完全解耦——故障互不影響
- 觸控由 TouchService 滑鼠模式提供（摸牆 = 點擊），本工具**不碰任何輸入**
- V1 不改 TouchService 任何程式碼（旋轉值放本工具 config，見下）

## 跟 TouchService 的關係

```
感應模組 ──UDP──▶ TouchService（輸入層）──滑鼠模式──▶ OS 游標（摸牆=點）
                       │ GET /venue   （十字佈局：faces 的 canvas 幾何）
                       │ GET /config  （mouseMode.screen + content：投影螢幕 rect 與校正）
                       ▼
                 WallProjector（顯示層）
                       │ 牆面 canvas rect → norm → 螢幕 rect（公式與滑鼠模式同一套，
                       │ 單元測試對拍——觸控落點跟投影內容天然對齊）
                       ├─ HTTP :20121 ── 管理頁 + /wrap/<face>（旋轉 wrap 頁）
                       └─ Edge --app=http://127.0.0.1:20121/wrap/<face>
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
├── src/WallProjector/
│   ├── Core/                    ← venue 解析、canvas→螢幕映射（對拍 TouchService）、TS client
│   ├── Engine/                  ← 投影引擎：瀏覽器殼、Win32 視窗對位、watchdog
│   ├── Web/                     ← 自寫 HTTP server、wrap 頁生成、管理 API、管理頁（內嵌）
│   └── Program.cs               ← 入口 + stdin 指令
└── tests/WallProjector.Tests/   ← 64 個測試（xunit；rect 對拍、wrap 旋轉、config、API）
```

## 快速上手（開發機，Linux 可建可測）

```bash
dotnet build WallProjector.sln
dotnet test  WallProjector.sln          # 64 個測試
dotnet run --project src/WallProjector  # 跑起來（無 TouchService 時走 fallback 並警示）
# 瀏覽器開 http://127.0.0.1:20121/
```

## 部署（場館機）

1. 與 TouchService **同一台機器**（管理 API 都只准 loopback 寫入；wrap 頁 origin 也是 127.0.0.1）
2. 解壓 `WallProjector-win64-v1.zip` 到任意資料夾（如 `C:\WallProjector\`）
3. **開機順序：TouchService 先、WallProjector 後**——WallProjector 啟動時抓
   /venue + /config；先後順序錯了會落到 cache/fallback（功能照常，但佈局若有變動不會反映，
   開管理頁按「重新連線 TouchService」即可）
4. 雙擊 `WallProjector.exe`，開 `http://127.0.0.1:20121/`
5. port 衝突時改 `projections.json` 的 `httpPort`

| port | 用途 |
|---|---|
| 20111 | TouchService（WS + /venue + /config）——既有 |
| 20121 | WallProjector 管理頁 + wrap 頁（本工具） |

## projections.json

```jsonc
{
  "httpPort": 20121,
  "touchServiceBaseUrl": "http://127.0.0.1:20111",
  "autoRestore": true,          // 啟動時自動還原所有 enabled 槽
  "restartOnCrash": true,       // 瀏覽器死亡自動重拉（指數退避 1/2/4/8/16s，最多 5 次）
  "topmost": true,              // 投影視窗 HWND_TOPMOST 置頂
  "browserProfileDir": "profiles",   // 每槽獨立 browser profile（相對 config 所在目錄）
  "fallback": {                 // TouchService 不可達且無 cache 時的後備值
    "screen":  { "x": 0, "y": 0, "width": 1920, "height": 1080 },
    "content": { "offsetX": 0, "offsetY": 0, "scaleX": 1.0, "scaleY": 1.0 },
    "venueFile": null           // 可指向 venue JSON 複本（如 TouchService 的 venues/704.json）
  },
  "projections": [
    { "face": 4, "url": "https://example.com/", "rotation": 90, "enabled": true }
  ]
}
```

- `face`：GET /venue 的 faces 陣列 index（704 場域 0~6；管理頁直接列名稱）
- `rotation`：0/90/180/270（CSS 順時針度數）
- 檔案根節點直接寫**槽陣列**也接受（其餘吃預設）
- 管理頁的變更**即時生效並寫回本檔**；手改檔案要重啟才生效

## 管理頁用法（http://127.0.0.1:20121/）

- 列出 venue 所有面（含地板，標註「通常投牆面」）：每面一列——
  URL 輸入框、rotation 下拉（0/90/180/270）、啟動/停止、狀態
- 狀態：`運行中` / `已停` / `重啟中`（瀏覽器死亡退避中）/ `失敗`（連死 5 次放棄，手動啟動重試）
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

### 旋轉的實作（wrap 頁）

每個投影槽的瀏覽器載入的是本工具的 wrap 頁（`/wrap/<face>`），目標網址以 iframe
嵌入、外層容器套 CSS transform：

| rotation | 容器尺寸 | transform |
|---|---|---|
| 0 | 100vw × 100vh | （無） |
| 90 | **100vh × 100vw（寬高對調）** | `rotate(90deg) translateY(-100vw)`，origin top left |
| 180 | 100vw × 100vh | `rotate(180deg)` |
| 270 | **100vh × 100vw** | `rotate(270deg) translateX(-100vh)`，origin top left |

90/270 的推導：容器在自身座標先平移再旋轉，旋轉後 bounding box 恰好等於視窗
（單元測試釘住字串）。**觸控不用反算**——瀏覽器 hit-testing 原生處理 CSS transform，
TouchService 滑鼠模式點在螢幕座標上，瀏覽器自己把事件路由進旋轉後的 iframe。

## iframe 限制與 v2 路線

目標站若以 `X-Frame-Options: DENY/SAMEORIGIN` 或 CSP `frame-ancestors` 拒絕嵌入，
**V1 無法投影該站**。wrap 頁 render 前會對目標 URL 做 header 預檢，被擋時投影視窗
直接顯示明確錯誤訊息（也會旋轉轉正，現場讀得到）。

v2 路線（依重量排序）：
1. **CDP 注入**：用 DevTools protocol 直接開原站分頁（不經 iframe），注入旋轉 CSS
   到頁面 root——繞過嵌入限制，亦可對單一分頁注入 touch 事件（不動全域輸入）
2. **Windows.Graphics.Capture 合成**：擷取任意視窗 + 旋轉合成 + 輸入反向轉發
   ——支援非瀏覽器視窗（簡報、播放器），重，後排
3. **Touch injection**（TouchService roadmap 的多點觸控模式）：解掉滑鼠模式單游標
   限制，多人多牆同時操作

## Watchdog 行為

- 每 2s 檢查各槽瀏覽器 process；死亡 → 指數退避重啟（1/2/4/8/16s），連死 5 次標
  `失敗` 放棄（管理頁手動啟動重試；存活超過 60s 退避歸零）
- **手動關掉投影視窗也算「死亡」會被重拉**——要停止請用管理頁/stdin 的停止
  （會把 enabled 關掉並寫回 config）
- 每槽用獨立 `--user-data-dir`（`profiles/slot-N/`）：強制獨立瀏覽器 process，
  否則 Edge/Chrome 會合併進既有 process、watchdog 監看與 kill 都失準

## Windows 現場驗證清單（Linux 測不到的部分）

> 程式邏輯（rect 數學、wrap 旋轉、config、API、fallback 鏈）已由 64 個單元/整合
> 測試 + Linux smoke test 覆蓋；以下 process/視窗行為**只能 Windows 實機驗**：

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

## 發布

```bash
dotnet publish src/WallProjector/WallProjector.csproj -c Release -r win-x64 \
  -p:SelfContained=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true \
  -o publish/win-x64
```

產物：單檔 `WallProjector.exe`（~35MB，含 runtime）。`_dist/WallProjector-win64-v1.zip`
= exe + projections.json 範本 + 本 README。
