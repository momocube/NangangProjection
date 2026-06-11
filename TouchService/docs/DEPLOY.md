# TouchService 部署手冊

## 系統需求

- 目標機：Windows 10/11 x64（場館投影主機）
- 發布為 self-contained 單檔執行檔，**目標機不需要安裝 .NET**
- 開發 / 測試：任何有 .NET 8 SDK 的機器（Linux / Windows 皆可）

## 發布

```bash
# 在開發機（repo 的 TouchService/ 目錄下）
dotnet publish src/TouchService/TouchService.csproj  -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64
dotnet publish tools/SensorSimulator/SensorSimulator.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64-sim
```

拷貝到目標機的最小部署包：

```
TouchService/
├── TouchService.exe      ← publish 產物（~65MB，含 runtime）
├── config.json           ← 服務設定（缺檔時首次啟動自動生成範本）
├── venues/
│   └── 704.json          ← 場域校正 profile
└── testpage/
    └── index.html        ← 測試/校調頁（選配；服務會在 GET / 提供）
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

啟動後 console / stdin 指令：

| 指令 | 作用 |
|---|---|
| `stats` | 吐統計（pkt rate、parse errors、down/move/up、處理延遲、WS client 數） |
| `probe` | dump latency CSV 到 log 目錄（環形 buffer 最近 ~16k 筆事件） |
| `quit` | 優雅關閉 |

log 每日輪替於 `log.dir`，無自動清理——場館機建議每季手動清舊檔。

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

## 健康檢查清單（進場後）

1. `stats` → `datagrams` 持續上升、`parse errors = 0`、`unrouted = 0`
2. testpage 踩一腳：down 紅點即時出現（體感無延遲）、放開 ~170ms 後淡出
3. `unrouted > 0` 持續增加 → 校正範圍沒蓋到實際 raw 值 → 走 CALIBRATION.md 四角測試
4. 觸點閃爍（按住卻 up/down 交替）→ 重送 gap 超過 timeout → `releaseTimeoutMs` 調高 20ms 再觀察
