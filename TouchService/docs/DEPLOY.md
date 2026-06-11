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
  "autoLock":        { "enabled": true,  "windowSeconds": 10, "minPackets": 10 },
  "touchMode":        "grid",
  "mouseMode":       { "face": 0,
                       "screen": { "x": 0, "y": 0, "width": 1920, "height": 1080 },
                       "smoothing": 0.35, "moveMinPx": 2 },
  "ui":              { "autoLaunch": true },
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
| `autoLock.minPackets` | 10 | 窗內封包數達此值才鎖。噪訊晶片 ~80ms 重送、10 秒約 125 包，門檻 10 遠低於噪訊、又高於短暫誤踩 |
| `touchMode` | grid | `grid`（純發布）/ `mouse`（額外掛 OS 滑鼠 sink——**發布照常**，監控頁/訂閱端不受影響）。可由設定頁熱切 |
| `mouseMode.face` | 0 | 滑鼠來源感應面 index（venue faces 順序）。**選牆面**——704 的地板是 face 2（Floor），拿地板當滑鼠會被腳步亂點 |
| `mouseMode.screen` | 0,0,1920×1080 | 映射目標螢幕矩形（virtual desktop 座標，非主螢幕可為負）。設定頁可從螢幕下拉直接帶入 |
| `mouseMode.smoothing` | 0.35 | 平滑強度：8ms 迴圈每 tick 以此比例指數逼近目標（0.01~1；1 = 不平滑直接到位） |
| `mouseMode.moveMinPx` | 2 | 平滑迴圈單步位移小於此 px 不注入（防 125Hz 微步轟炸；到位的最後一步除外） |
| `ui.autoLaunch` | true | Windows 啟動完成後自動開 UI 視窗（Edge `--app=` → Chrome `--app=` → 預設瀏覽器）。非 Windows / 找不到瀏覽器只 log 不報錯 |
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
TouchService.exe                  ← 使用同目錄 config.json
TouchService.exe --config D:\cybercube\config.json
```

> **重啟注意**：啟動後前 `autoLock.windowSeconds`（預設 10）秒是自檢窗——
> 這段期間**不發布觸控事件**，且場上有人站著會被當噪訊晶片誤鎖。
> 重啟時請淨空場地；誤鎖用 `unlock` 指令救（見下表），不必再重啟。

啟動後 console / stdin 指令：

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

找不到瀏覽器 / 非 Windows 只 log 不報錯。頁面三個 tab：

- **監控**：場域圖、即時觸點、活躍觸點側欄（id / face / cell / px / 存活秒，
  存活久的在上）、事件流、統計、鎖定格（暗紅 X）——即原測試頁
- **設定**：觸控模式（採格/滑鼠）、滑鼠參數、releaseTimeoutMs / matchRadiusPx、
  autoLock、鎖定清單（每列可解鎖 + 寫回 config）、需重啟欄位唯讀顯示
- **診斷**：疑似噪訊排行（每 5 秒刷新，每列「鎖定」按鈕一鍵鎖）+ 手動鎖定表單
  （face / col / row）。非本機開頁時鎖定按鈕鎖灰（僅限本機操作）。
  tab 切換不斷 WS 連線。

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

熱套用範圍：`touchMode`、`mouseMode.*`、`releaseTimeoutMs`、`matchRadiusPx`
（下個 touch 生效）；`autoLock` 欄位只寫回 config、**下次啟動生效**（UI 有標註）。
`listenPort` / 輸出 port 為需重啟欄位——改 config.json 後重啟。

## 滑鼠模式（摸牆操作這台機器）

`touchMode: "mouse"` 時額外掛一個 OS 滑鼠 sink：**mouseMode.face 那一面**的觸點
映射成這台 Windows 機器的真實游標（down→LEFTDOWN、move→平滑移動、up→LEFTUP），
讓使用者直接「摸牆操作網頁」。

### 設定流程

1. 開 UI（autoLaunch 自動開，或瀏覽器開 `http://127.0.0.1:20111/`）→ 切「設定」tab
2. 觸控模式選「滑鼠模式」
3. **來源牆面**下拉選一面牆（清單預設排除地板 Floor）
4. **目標螢幕**下拉選要操作的螢幕（自動帶入矩形；也可手動填 x/y/w/h）
5. 視需要調**平滑強度**（預設 0.35；越小越滑順但跟手越慢，1 = 直接到位）
6. 按 **Apply** → 出現綠色「已套用」
7. 摸牆驗證：游標應跳到對應位置並按下左鍵，滑動時游標平滑跟隨，離手放開

### 行為細節與注意事項

- **單點所有權**：該面第一個 down 的 touch 控制游標，它離手前其他觸點全部忽略
  （OS 游標只有一顆）。多人同時摸牆不會搶游標，但只有第一個人有效
- **發布端不受影響**：滑鼠是「額外的 sink」——WS/UDP 照常全量發布，
  監控頁與其他訂閱端在滑鼠模式下完全正常
- ⚠ **滑鼠模式下摸牆會搶這台機器的真實游標**——維運人員同時用實體滑鼠會互相干擾；
  要操作機器先在設定頁切回採格模式（按住中熱切會自動放開按鍵，不會卡死）
- 非目標面（含地板）觸發零注入；80ms 封包間隔由 release timeout 橋接，按住不會中斷
- Linux 上選 mouse 模式：log warn + no-op（事件照常發布，不 crash）

## 開機自啟（建議：工作排程器）

1. 工作排程器 → 建立工作
2. 觸發程序：「登入時」（投影主機通常自動登入展演帳號）
3. 動作：啟動程式 `D:\cybercube\TouchService\TouchService.exe`，
   **「開始位置」務必填 exe 所在目錄**（讓相對路徑的 config / venues / logs 正確）
4. 設定：勾「如果工作失敗，每 1 分鐘重新啟動」

> 也可用 NSSM 包成 Windows service；TouchService 是純 console 程式無特殊需求。
> 包成 service 後 stdin 指令不可用，統計改看 log 檔。

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

服務啟動後前 `windowSeconds`（預設 10 秒）是**自檢窗**：窗內被觸發 ≥ `minPackets`
（預設 10）包的格子自動鎖定。持續發訊的噪訊晶片重啟一次就會被抓到。

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
