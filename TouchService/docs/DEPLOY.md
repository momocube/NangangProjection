# TouchService 部署手冊

## 系統需求

- 目標機：Windows 10/11 x64（場館投影主機）
- 發布為 self-contained 單檔執行檔，**目標機不需要安裝 .NET**
- 開發 / 測試：任何有 .NET 8 SDK 的機器（Linux / Windows 皆可）

## 發布

```bash
# 在開發機（repo 的 TouchService/ 目錄下）
dotnet publish src/TouchService/TouchService.csproj  -c Release -r win-x64 -p:SelfContained=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish/win-x64
dotnet publish tools/SensorSimulator/SensorSimulator.csproj -c Release -r win-x64 -p:SelfContained=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish/win-x64-sim
```

> **SelfContained 必須顯式給**（csproj 只有 RuntimeIdentifiers）：漏掉會變
> framework-dependent ~9MB，現場沒裝 runtime 跑不起來。正確產物 ~35MB（含壓縮）。

拷貝到目標機的最小部署包：

```
TouchService/
├── TouchService.exe      ← publish 產物（~35MB，含 runtime、單檔壓縮）
├── config.json           ← 服務設定（缺檔時首次啟動自動生成範本）
├── venues/
│   └── 704.json          ← 場域校正 profile
└── testpage/
    └── index.html        ← 監控/設定頁（選配；服務會在 GET / 提供，啟動 UI 開的就是它）
```

> 注意：`config.json` 的相對路徑（venueProfile、log dir）以 **config.json 所在目錄**為基準，
> 不是 exe 位置。`TouchService.exe --config D:\path\config.json` 可指定路徑。

## 新場域開箱流程（v8 校正精靈——非工程師可完成）

新場域（另一台電腦 + 不同牆面佈局）從零到可用，**不需要手算任何校正值**：

1. **拷部署包**：上方最小部署包整包拷到新機（venues/ 裡的 704.json 留著沒關係，
   等下精靈會寫新檔並把 config 指過去）
2. **開防火牆**：UDP `listenPort`（預設 20001）inbound 允許 TouchService.exe
   （見下方「防火牆 / 網路」；主機網卡須與感應模組同一 L2 網段）
3. **啟動 `TouchService.exe`**：UI 自動開（沒開就托盤雙擊）。此時觸點還不會正確
   ——場域 profile 還是舊的，正常
4. **UI →「校正」tab**：照精靈引導走——填場域名/面格數 → 開始校正 →
   踩面的「左上角第一顆晶片」與「右下角最後一顆晶片」（raw 值大字顯示、
   同值連續 ≥3 包自動取值）→ 預覽確認 → 儲存。多面場域逐面累積
   （origin 手填，進階見 `CALIBRATION.md`）
5. **重啟服務**（托盤右鍵「結束服務」→ 重開 exe）：載入新場域 profile
6. **驗證**：監控 tab 踩四角 + 走格（觸點位置正確、軌跡連續）；
   `stats` 的 `unrouted` 不增加
7. 視場地狀況跑噪訊晶片 SOP（下方三層工作流）、設定開機自啟

> 沒有實機要先演練：`SensorSimulator.exe` 對 listenPort 發任意座標即可把
> 精靈全流程走一遍（步驟見「離場驗證」）。

## config.json 全欄位

```json
{
  "listenPort": 20001,
  "venueProfile": "venues/704.json",
  "releaseTimeoutMs": 170,
  "matchRadiusPx": 96,
  "maxSlots": 128,
  "flushIntervalMs": 5,
  "keepaliveIntervalMs": 1000,
  "moveMinDistancePx": 0,
  "lockedCells":      [],
  "autoLock":        { "enabled": true,  "windowSeconds": 10, "minPackets": 10, "minEpisodes": 3 },
  "touchMode":        "grid",
  "mouseMode":       { "faces": [0, 1, 3, 4, 5, 6],
                       "screen": { "x": 0, "y": 0, "width": 1920, "height": 1080 },
                       "content": { "offsetX": 0, "offsetY": 0, "scaleX": 1.0, "scaleY": 1.0 },
                       "smoothing": 0.35, "moveMinPx": 2 },
  "ui":              { "autoLaunch": true, "trayEnabled": true },
  "udpOutput":       { "enabled": true,  "endpoints": [ "127.0.0.1:20110" ] },
  "webSocketOutput": { "enabled": true,  "port": 20111 },
  "tuioOutput":      { "enabled": false, "endpoint": "127.0.0.1:3333" },
  "log":             { "level": "Info",  "dir": "logs", "console": true }
}
```

| 欄位 | 預設 | 說明 |
|---|---|---|
| `listenPort` | 20001 | 感應模組廣播的 UDP port |
| `venueProfile` | venues/704.json | 場域校正 profile 路徑（欄位說明見 CALIBRATION.md） |
| `releaseTimeoutMs` | 170 | 最後封包後多久視為離手（取值依據見 PROTOCOL.md，**不建議 <140**） |
| `matchRadiusPx` | 96 | touch ID 最近鄰配對半徑（畫布 px；96 ≈ 704 場域 3 格） |
| `maxSlots` | 128 | 同時追蹤 touch 上限，超過時丟棄新點 |
| `flushIntervalMs` | 5 | 離手掃描週期，決定 up 事件時間精度 |
| `keepaliveIntervalMs` | 1000 | 無事件時的全狀態 snapshot 週期 |
| `moveMinDistancePx` | 0 | move 事件最小位移門檻（0 = 任何座標變化都發） |
| `lockedCells` | `[]` | 鎖定晶片清單（噪訊晶片過濾）：`[{"face":2,"cell":[7,13]}]`，face = 面 index、cell = [col,row]（晶片格）。命中的點在進 touch tracker 前丟棄。鎖定 SOP 見下方「現場發現噪訊晶片怎麼辦」 |
| `autoLock.enabled` | true | 啟動自檢窗（自動鎖噪訊晶片）。**開機時場上理應無人**——窗內還在發訊號的格子幾乎必然是故障晶片 |
| `autoLock.windowSeconds` | 10 | 自檢窗長度（秒）。窗期間**不發布任何觸控事件**（keepalive 照發）；≤ 0 視為停用 |
| `autoLock.minPackets` | 10 | 窗內封包數達此值即鎖（判準 A，與 minEpisodes 為 **OR**）。快速重送型噪訊 ~80ms 重送、10 秒約 125 包，門檻 10 遠低於噪訊、又高於短暫誤踩 |
| `autoLock.minEpisodes` | 3 | 窗內**分離觸發段數**達此值即鎖（判準 B，與 minPackets 為 **OR**；0 = 停用此判準）。一段（episode）= 同格封包流以「間隔 > releaseTimeoutMs」切分的一次連續觸發。抓**慢速頻閃型**噪訊：2026-06-11 現場 face2 cell(37,14) 每次只發 1 包、間隔 0.5~1.5 秒反覆 down/up，10 秒窗內湊不滿 minPackets 而躲過判準 A。依據：開機時場上沒人，真人誤踩是**一次連續事件（1 段）**，不會在同一格分離地觸發 3 次 |
| `touchMode` | grid | `grid`（純發布）/ `mouse`（額外掛 OS 滑鼠 sink——**發布照常**，監控頁/訂閱端不受影響）。可由設定頁熱切 |
| `mouseMode.faces` | 缺欄位 = 排除 Floor | **啟用滑鼠觸控的面清單**（venue faces index 陣列）。缺欄位/null = 預設「全部面但排除名稱含 Floor 的面」（704 = 地板關、六個牆面開）；空陣列 = 全部停用。**舊 config 的 `face`（單數 int）讀到時自動轉成 `faces:[該值]` 並在 log 提示**，設定頁套用後以新 schema 寫回 |
| `mouseMode.screen` | 0,0,1920×1080 | 映射目標螢幕矩形（virtual desktop 座標，非主螢幕可為負）。設定頁可從螢幕下拉直接帶入 |
| `mouseMode.content` | 0/0/1/1 | 投影內容在螢幕內的校正（letterbox/沒滿版時用）：落點 = screen 原點 + offset + norm × screen 寬高 × scale。預設 0/0/1/1 = 滿版。校正流程見下方「滑鼠模式」 |
| `mouseMode.smoothing` | 0.35 | 平滑強度：8ms 迴圈每 tick 以此比例指數逼近目標（0.01~1；1 = 不平滑直接到位） |
| `mouseMode.moveMinPx` | 2 | 平滑迴圈單步位移小於此 px 不注入（防 125Hz 微步轟炸；到位的最後一步除外） |
| `ui.autoLaunch` | true | Windows 啟動完成後自動開 UI 視窗（Edge `--app=` → Chrome `--app=` → 預設瀏覽器）。非 Windows / 找不到瀏覽器只 log 不報錯 |
| `ui.trayEnabled` | true | Windows 系統列托盤常駐：啟動時**隱藏 console 視窗**、掛托盤圖示（雙擊開 UI / 右鍵選單）。false = 行為同 v5（console 照常、無托盤）。非 Windows 無作用。除錯要看 console：`TouchService.exe --console` |
| `udpOutput.endpoints` | 127.0.0.1:20110 | raw UDP JSON 收端清單（可多個，`host:port`） |
| `webSocketOutput.port` | 20111 | WS + HTTP（/venue、testpage）監聽 port |
| `tuioOutput` | 停用 | **v1 未實作**，enabled=true 會啟動失敗（防呆） |
| `log.level` | Info | Debug / Info / Warn / Error |
| `log.dir` | logs | log 目錄，每日輪替（`touchservice-YYYYMMDD.log`） |
| `log.console` | true | console 同步輸出 |

所有欄位都有預設值；config.json 不存在時自動生成範本後以預設值啟動。

## 防火牆 / 網路

| 方向 | 協議 / Port | 用途 |
|---|---|---|
| inbound | UDP 20001 | 感應模組廣播（來源為模組網段，255.255.255.255 廣播） |
| inbound | TCP 20111 | WebSocket 訂閱 + testpage / venue HTTP |
| outbound | UDP → endpoints | raw JSON 推送（本機 127.0.0.1 不需開） |

- Windows Defender 防火牆：允許 `TouchService.exe` 的 UDP 20001 inbound
  （私人網路 profile）。模組是**廣播**封包——主機網卡必須與模組在同一 L2 網段。
- **與 Unity 投影程式共存注意**：Unity 端 `UdpSensorReceiver` 也綁 20001 且
  **未設 SO_REUSEADDR**。本服務有設 ReuseAddress，但若 Unity 先佔用，廣播封包
  的分流行為依 Windows socket 規則不可靠——**正式上線時兩者擇一收包**
  （長期規劃：Unity 改訂 TouchService 的輸出）。

## 啟動與日常維運

```
TouchService.exe                  ← 使用同目錄 config.json；Windows 預設托盤化（console 隱藏）
TouchService.exe --console        ← 托盤化但保留 console 視窗（除錯用）
TouchService.exe --config D:\cybercube\config.json
```

### 托盤常駐（v6 起，黑視窗從此不需要）

Windows + `ui.trayEnabled: true`（預設）時，服務啟動後：

1. **console 視窗自動隱藏**（背景照跑），系統列出現 TouchService 托盤圖示
2. **雙擊圖示** → 開 UI 視窗（監控/設定/診斷/終端）
3. **右鍵圖示** → 選單：**開啟介面** / **顯示主控台**（再點一次隱藏）/ **結束服務**（優雅關閉）

**心智模型**：服務本體常駐在托盤——**UI 視窗（Edge --app）關掉只是關瀏覽器，服務照跑**，
托盤雙擊隨時叫回；要真正停止服務用右鍵「結束服務」（或 console `quit`）。
維運不再需要面對黑視窗：log 與指令都在 UI 的「終端」tab（見下）。

- 除錯要看原生 console：`--console` flag 啟動（不隱藏），或托盤右鍵「顯示主控台」
- 回到 v5 行為（無托盤、console 常駐）：config `ui.trayEnabled: false`
- explorer 重啟（TaskbarCreated）托盤圖示會自動重掛，不會消失
- Linux / 包成 service（無 console）：托盤與隱藏全部自動 no-op，不影響服務

> **重啟注意**：啟動後前 `autoLock.windowSeconds`（預設 10）秒是自檢窗——
> 這段期間**不發布觸控事件**，且場上有人站著會被當噪訊晶片誤鎖。
> 重啟時請淨空場地；誤鎖用 `unlock` 指令救（見下表），不必再重啟。

啟動後 console / stdin 指令（**同一組指令在 UI「終端」tab 也能打**，輸出一字不差——
托盤化後不需要 console 也能維運）：

| 指令 | 作用 |
|---|---|
| `stats` | 吐統計（pkt rate、parse errors、down/move/up、dropped by lock、auto-locked 計數、處理延遲、WS client 數） |
| `touches` | 列出當下所有活躍 touch（id / face / cell / px / 存活秒數），找「此刻在叫」的噪訊晶片用 |
| `history [n]` | 疑似噪訊排行（預設 top 20）：score / 30 分鐘次數 / 總次 / 孤立% / 佔用% / 最後觸發 / 鎖定標記。**間歇型噪訊（發作一陣停一陣，`touches` 追不到）靠這個**；自檢窗內被抑制的觸發也計入。score ≥ 60 看一眼、≥ 80 高度可疑；只排序不自動鎖 |
| `locks` | 列出目前所有鎖定（face / cell / 來源 config\|auto\|manual / auto 鎖定的封包數） |
| `lock <face> <col> <row>` | 手動加鎖（runtime 即時生效，來源 manual；例 `lock 2 7 13`） |
| `unlock <face> <col> <row>` | 解鎖（任何來源都可解；解 config 來源只影響本次執行、不改檔，重啟會鎖回） |
| `savelocks` | 把目前 runtime 全部鎖定（config+auto+manual 合併去重）寫回 config.json 的 lockedCells，其他欄位原樣保留 |
| `probe` | dump latency CSV 到 log 目錄（環形 buffer 最近 ~16k 筆事件） |
| `quit` | 優雅關閉 |

log 每日輪替於 `log.dir`，無自動清理——場館機建議每季手動清舊檔。

## 啟動 UI（監控 + 設定頁）

Windows 且 `ui.autoLaunch: true`（預設）時，服務啟動完成後自動開 UI 視窗到
`http://127.0.0.1:<wsPort>/`，依序嘗試：

1. Edge app 視窗（`msedge --app=`，無網址列、像原生視窗）——標準安裝路徑 + PATH
2. Chrome `--app=`
3. fallback：預設瀏覽器開該網址

找不到瀏覽器 / 非 Windows 只 log 不報錯。頁面四個 tab：

- **監控**：場域圖、即時觸點、活躍觸點側欄（id / face / cell / px / 存活秒，
  存活久的在上）、事件流、統計、鎖定格（暗紅 X）——即原測試頁
- **設定**：觸控模式（採格/滑鼠）、**啟用觸控範圍勾選清單**（7 面 checkbox，地板預設不勾）、
  目標螢幕、content 校正、releaseTimeoutMs / matchRadiusPx、
  autoLock、鎖定清單（每列可解鎖 + 寫回 config）、需重啟欄位唯讀顯示
- **診斷**：疑似噪訊排行（每 5 秒刷新，每列「鎖定」按鈕一鍵鎖）+ 手動鎖定表單
  （face / col / row）。非本機開頁時鎖定按鈕鎖灰（僅限本機操作）。
  tab 切換不斷 WS 連線。
- **校正**（v8 新增）：新場域校正精靈——開校正工作階段看 raw 訊號串流、
  踩兩角自動算 rawMin/Max + flip、面預覽圖即時標點、一鍵寫 venue JSON +
  config（重啟生效）。**僅限本機開頁**（非本機顯示「校正僅限本機」）。
  完整流程見 `CALIBRATION.md` 與上方「新場域開箱流程」
- **終端**（v6 新增）：黑視窗的完整替代——
  - **log 滾動視窗**：服務 log 即時串流（WS push，等寬字體、自動捲到底、可暫停捲動）；
    連上時先補發最近 300 行 ring buffer
  - **指令輸入框**：Enter 送出、↑↓ 鍵翻歷史；與 stdin 走同一個處理器
    （`POST /command`），輸出文字跟 console 一字不差
  - **快速按鈕列**：stats / locks / history / touches 一鍵執行
  - **僅限本機**：log 只推給 loopback 連線（內網其他機器訂閱 WS 收不到 log 內容）；
    非本機開頁終端 tab 顯示「終端僅限本機」、log 區塊隱藏

設定頁背後是本機管理 HTTP API（與 WS 同 port）：

| 端點 | 用途 | 來源限制 |
|---|---|---|
| `GET /config` | 目前生效設定（去敏感，不含 log 設定） | 無 |
| `POST /config` | 套用設定（驗證全過才套用；熱套用 + 寫回 config.json，其他欄位保留） | **僅 loopback**，否則 403 |
| `GET /monitors` | 螢幕清單（Windows EnumDisplayMonitors；非 Windows 空陣列） | **僅 loopback** |
| `GET /history` | 疑似噪訊排行（`?top=N`，預設 50；score 降冪，欄位含 30 分鐘次數 / 孤立% / 佔用% / locked） | 無（唯讀，內網監看 OK） |
| `POST /locks/lock` | 手動鎖定一格（語意同 stdin `lock`，來源 manual；body `{"face":2,"col":7,"row":13}`） | **僅 loopback** |
| `POST /locks/unlock` | 解鎖一格（語意同 stdin `unlock`） | **僅 loopback** |
| `POST /locks/save` | 鎖定寫回 config（語意同 stdin `savelocks`） | **僅 loopback** |
| `POST /command` | UI 終端指令（body `{"cmd":"history 20"}` → `{"output":"..."}`；與 stdin 共用處理器，輸出一致） | **僅 loopback** |
| `POST /calibration/start` | 開校正工作階段：raw 檢視串流推給 loopback WS client（15 分鐘無 stop 自動結束；重打 = 延長；服務照常發布） | **僅 loopback** |
| `POST /calibration/stop` | 結束校正工作階段 | **僅 loopback** |
| `GET /calibration/status` | `{active, remainingSeconds}` | **僅 loopback** |
| `POST /calibration/save` | 兩角校正 → 寫 `venues/<名>.json` + config `venueProfile`（驗證全過才動檔案；**重啟生效**） | **僅 loopback** |

熱套用範圍：`touchMode`、`mouseMode.*`、`releaseTimeoutMs`、`matchRadiusPx`
（下個 touch 生效）；`autoLock` 欄位只寫回 config、**下次啟動生效**（UI 有標註）。
`listenPort` / 輸出 port 為需重啟欄位——改 config.json 後重啟。

## 滑鼠模式（摸牆操作這台機器）

`touchMode: "mouse"` 時額外掛一個 OS 滑鼠 sink：**啟用面（mouseMode.faces）**上的觸點
映射成這台 Windows 機器的真實游標（down→LEFTDOWN、move→平滑移動、up→LEFTUP），
讓使用者直接「摸牆操作網頁」。

### canvas 十字映射（v6 重寫——為什麼游標位置對得上）

投影螢幕顯示的是**整個場域 canvas 的十字展開圖**（中央地板、四周牆臂，704 = 2688×3840），
不是單一牆面拉滿。所以映射是：

```
觸點 canvas norm（px ÷ canvas 尺寸）→ 目標螢幕 rect 等比落點
screenX = rect.x + content.offsetX + norm.x × rect.width  × content.scaleX
screenY = rect.y + content.offsetY + norm.y × rect.height × content.scaleY
```

摸 Right Big Wall，游標就落在螢幕上十字圖的右臂——跟投影畫面一一對應。
十字外的盲區自然無事（沒有感應面就沒有觸點）。
> v4 的 bug：把「單一面的面內座標」拉伸到整個螢幕——摸右牆游標跑到螢幕中央（地板區）。
> v6 起以 canvas 為座標系，已修正。

### 設定流程

1. 開 UI（autoLaunch 自動開、或托盤雙擊）→ 切「設定」tab
2. 觸控模式選「滑鼠模式」
3. **啟用觸控範圍**：7 個面的勾選清單，勾選的面才驅動游標。
   預設 = 六個牆面開、**地板（Floor）不勾**（腳步會亂點游標）
4. **目標螢幕**下拉選投影十字圖所在的螢幕（自動帶入矩形；也可手動填 x/y/w/h）
5. **內容校正**（投影沒滿版才需要，滿版維持 0/0/1/1）：見下方校正流程
6. 視需要調**平滑強度**（預設 0.35；越小越滑順但跟手越慢，1 = 直接到位）
7. 按 **Apply** → 出現綠色「已套用」
8. 摸牆驗證：游標應跳到螢幕十字圖的**對應面對應位置**並按下左鍵，
   滑動時游標平滑跟隨，離手放開

### content 校正流程（投影沒滿版 / letterbox 時）

投影內容若沒鋪滿整顆螢幕（上下黑邊、置中縮放等），游標落點會整體偏移——用
content 四個欄位把「canvas 十字圖實際顯示的範圍」校進來：

1. 先量（或從投影軟體讀）內容區在螢幕內的位置：左上角偏移 `(offsetX, offsetY)` px、
   內容寬高相對螢幕的比例 `(scaleX, scaleY)`
   - 例：1920×1080 螢幕、內容置中顯示 1536×864 → offsetX=192、offsetY=108、
     scaleX=0.8、scaleY=0.8
2. 設定 tab 填入四欄 → Apply
3. 驗證：摸 canvas 四個角落的感應面邊緣，游標應落在投影內容的對應角落；
   有系統性偏移就調 offset、比例不對就調 scale
4. 落點計算後一律 clamp 回螢幕 rect——校正填錯游標也不會飛到別顆螢幕

### 行為細節與注意事項

- **單點所有權（跨所有啟用面）**：任一啟用面第一個 down 的 touch 控制游標，
  它離手前其他觸點（含其他啟用面）全部忽略（OS 游標只有一顆）。
  多人同時摸牆不會搶游標，但只有第一個人有效
- **發布端不受影響**：滑鼠是「額外的 sink」——WS/UDP 照常全量發布，
  監控頁與其他訂閱端在滑鼠模式下完全正常；未勾選面（地板）的觸發也照常全量發布
- ⚠ **滑鼠模式下摸牆會搶這台機器的真實游標**——維運人員同時用實體滑鼠會互相干擾；
  要操作機器先在設定頁切回採格模式（按住中熱切會自動放開按鍵，不會卡死）
- 未勾選面（預設含地板）觸發零注入；80ms 封包間隔由 release timeout 橋接，按住不會中斷
- Linux 上選 mouse 模式：log warn + no-op（事件照常發布，不 crash）

### 舊 config 相容（v4/v5 → v6）

- 舊欄位 `mouseMode.face`（單數 int）讀到時自動轉成 `faces:[該值]`，log 會提示；
  下次從設定頁 Apply 後以新 schema（faces 陣列 + content）寫回 config.json
- `faces` 缺欄位 = 預設「全部面排除名稱含 Floor 的面」；`content` 缺欄位 = 滿版 0/0/1/1
- **注意**：舊 config 轉換後啟用面只有原本那一面——要六面全開請在設定頁勾齊再 Apply

## 開機自啟（建議：工作排程器）

1. 工作排程器 → 建立工作
2. 觸發程序：「登入時」（投影主機通常自動登入展演帳號）
3. 動作：啟動程式 `D:\cybercube\TouchService\TouchService.exe`，
   **「開始位置」務必填 exe 所在目錄**（讓相對路徑的 config / venues / logs 正確）
4. 設定：勾「如果工作失敗，每 1 分鐘重新啟動」

> 也可用 NSSM 包成 Windows service；TouchService 是純 console 程式無特殊需求。
> 包成 service 後 stdin 指令不可用（stdin loop 自動 no-op、不影響服務）、托盤也不會出現
> （service session 沒有桌面）——指令與 log 改用 UI「終端」tab（本機開
> `http://127.0.0.1:20111/`），或看 log 檔。

## 離場驗證（沒有硬體時）

```bash
# 終端 A：服務
TouchService.exe

# 終端 B：模擬 60 點併發（或 replay 真實 capture）
SensorSimulator.exe scenario scenarios/stress-60.json --target 127.0.0.1:20001
SensorSimulator.exe replay capture-multitouch-floor.txt --target 127.0.0.1:20001 --speed 1.0

# 瀏覽器：開 http://127.0.0.1:20111/ → 連線 → 看觸點與事件流
```

## 噪訊晶片處理 SOP（三層工作流，完全不需要碰 console）

症狀：沒人踩卻一直有觸點、或同一格觸點反覆 up / down 換 ID 重生——通常是該格晶片
故障持續發訊號。麻煩的是**間歇型**：發作一陣停一陣，發作結束 ~170ms 後觸點就消失，
肉眼/手速追不上——所以工作流的核心是「歷史 + 排行」，不是「當場抓」。

```
第 1 層（開機）   autoLock 啟動自檢窗 → 持續叫的晶片自動鎖
第 2 層（營運中） UI 診斷 tab 疑似噪訊排行 → 人工確認 → 一鍵「鎖定」
第 3 層（收尾）   設定 tab「鎖定寫回 config（savelocks）」→ 持久化
```

### 第 1 層：開機自檢自動鎖（autoLock，預設開）

服務啟動後前 `windowSeconds`（預設 10 秒）是**自檢窗**：窗內命中**任一判準（OR）**
的格子自動鎖定，重啟一次就會被抓到——

- **判準 A（包數）**：窗內被觸發 ≥ `minPackets`（預設 10）包。抓**快速重送型**
  （~80ms 持續重送，10 秒約 125 包）。
- **判準 B（段數）**：窗內**分離觸發** ≥ `minEpisodes`（預設 3）段
  （間隔 > `releaseTimeoutMs` 即切段；0 = 停用）。抓**慢速頻閃型**：每次只發
  1~2 包、間隔秒級反覆 down/up（現場實例 face2 cell(37,14)，間隔 0.5~1.5 秒），
  包數湊不滿判準 A。開機時場上沒人，真人誤踩是一次連續事件（1 段），
  不會在同一格分離地觸發 3 次——對開機場景這個判準是鐵的。

鎖定 log 會標明每顆命中哪個判準（`命中 packets / episodes / packets+episodes`）。

1. **確認場上沒人**（重要！窗內站在場上的人會被當成噪訊晶片誤鎖）
2. 重啟 TouchService，等倒數結束（UI 監控 tab 有「自檢中 N 秒」、console 也有倒數）
3. 自動鎖定結果會推到 UI 設定 tab 的鎖定清單（auto 來源、帶封包數）；
   誤鎖就在清單按「解鎖」

> 自檢窗內被抑制的觸發**也會計入觸發歷史**——窗內叫過但沒達鎖定門檻的格子，
> 會直接出現在診斷排行上，不會消失。

### 第 2 層：營運中看診斷排行、一鍵鎖定（間歇型噪訊主力）

開 UI（autoLaunch 自動開，或本機瀏覽器開 `http://127.0.0.1:20111/`）→「診斷」tab：

1. **疑似噪訊排行**每 5 秒自動刷新。score 0–100 由三個可解釋因子加權：
   30 分鐘觸發頻率（40）＋孤立觸發比率（35）＋活躍時長佔比（25）
2. 判讀：**score ≥ 60 看一眼、≥ 80 高度可疑**。典型噪訊樣貌：同格 30 分鐘幾十上百次、
   孤立 % 接近 100（觸發時周圍沒有別的觸點——真人腳掌/移動幾乎總是多格連動）、
   佔用 % 明顯高於其他格
3. 確認不是真人（看監控 tab 該位置有沒有人、或對照場上狀況）後，
   按該列**「鎖定」**——立即生效，不用重啟；該列標記「已鎖定」、
   設定 tab 鎖定清單同步出現（manual 來源）
4. 也可用排行下方的**手動鎖定表單**直接填 face / col / row
5. 驗證：監控 tab 該格變暗紅 X、排行該列「已鎖定」、該格不再產生觸點

> **分數只是排序用，服務永遠不會自動鎖**——營運中有真人在踩，誤鎖比漏鎖傷。
> 排行是「嫌疑清單」，鎖定永遠由人按下去。
> 觸發歷史是運行期統計、**重啟歸零（刻意設計）**：可疑晶片會很快重新冒上排行，
> 不會跟上次營運的舊帳混在一起。
> 非本機開頁（場館其他機器看排行）時「鎖定」按鈕鎖灰——寫入操作僅限本機。

### 第 3 層：savelocks 持久化

對鎖定結果滿意後，到**設定 tab 按「鎖定寫回 config（savelocks）」**：
目前 runtime 全部鎖定（config + auto + manual 合併去重）寫回 config.json 的
`lockedCells`——下次啟動即使自檢沒抓到（晶片間歇性發作）也會從 config 鎖住。

### 備援：console 指令（UI 不可用時）

UI 壞了或遠端 ssh 進來時，stdin 指令可做完全相同的事：

| 對應 UI 操作 | console 指令 |
|---|---|
| 診斷排行 | `history [n]`（預設 top 20） |
| 此刻活躍觸點 | `touches`（存活久的排最上面：存活持續累加 + cell 固定不動 = 可疑） |
| 一鍵鎖定 | `lock <face> <col> <row>` |
| 解鎖 | `unlock <face> <col> <row>` |
| 鎖定清單 | `locks` |
| 寫回 config | `savelocks` |

> **誤鎖救援**：重啟時有人站在場上被誤鎖 → 設定 tab 解鎖（或 `unlock`），不用再重啟。
> 不想要自檢（例如除錯硬體時要看原始觸發）→ config `autoLock.enabled: false`。
> 填錯座標不會壞事：config 裡超出場域範圍的項啟動時會在 log 印警告（永遠不會命中）；
> 格式不完整的項自動跳過、不擋啟動；`lock` 指令/按鈕則直接拒絕超範圍座標。
> 鎖定只影響那一格（16×32cm 地磚的一塊晶片），相鄰格踩踏不受影響。
> 鎖定是遮瑕不是修復——座標（face / cell）記得回報硬體商。

## 健康檢查清單（進場後）

1. 重啟後等自檢窗結束（預設 10 秒，console 有倒數），看自檢結果 log——
   有自動鎖定的格子先確認不是有人站在場上造成的誤鎖（`unlock` 可救）
2. `stats` → `datagrams` 持續上升、`parse errors = 0`、`unrouted = 0`
3. testpage 踩一腳：down 紅點即時出現（體感無延遲）、放開 ~170ms 後淡出；
   鎖定格顯示為暗紅 X，圖例有「鎖定晶片」
4. `unrouted > 0` 持續增加 → 校正範圍沒蓋到實際 raw 值 → 走 CALIBRATION.md 四角測試
5. 觸點閃爍（按住卻 up/down 交替）→ 重送 gap 超過 timeout → `releaseTimeoutMs` 調高 20ms 再觀察
6. 沒人踩卻有觸點 / 同格反覆重生 → 噪訊晶片 → 走上方「噪訊晶片處理 SOP（三層工作流）」；
   營運一陣子後順手開「診斷」tab 掃一眼排行，score ≥ 80 的格子排查
