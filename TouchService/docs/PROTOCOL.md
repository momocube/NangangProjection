# TouchService 對外協議規格（v1）

## 資料流總覽

```
[CyberCube 感應模組 ×N]                    （7 感應面共 14,272 顆晶片）
   │  UDP 廣播 → port 20001
   │  55 55 [frame] [dataSize H L] ([X_H X_L Y_H Y_L]×N) [SUM_H SUM_L]
   ▼
TouchService（本服務）
   │  解析 → 7 面分類 → 座標映射 → touch ID 追蹤（down/move/up）
   ├──▶ raw UDP JSON  → config 指定的 endpoint 清單（預設 127.0.0.1:20110）
   ├──▶ WebSocket JSON → ws://<host>:20111/（同 schema；連上先收 hello）
   ├──▶ HTTP GET /venue → 場域 profile JSON（與 WS 同 port）
   └──▶ TUIO（v1 預留插槽，未實作；見文末）
```

## 輸入：感應模組封包（參考）

| 欄位 | 長度 | 說明 |
|---|---|---|
| header | 2 | `55 55` |
| frame | 1 | 模組流水號（不參與 checksum） |
| dataSize | 2 | big-endian；必為 4 的倍數 |
| 點資料 ×N | 4×N | `X_H X_L Y_H Y_L`（big-endian raw 座標） |
| checksum | 2 | 點資料區 byte 累加取低 16 bit |

- 一個 datagram 可帶 1~4+ 點（同模組內合包；實測上限 4 點）
- 每個硬體模組（8 地磚 × 8 晶片 = 64 顆）獨立廣播，來源 port 各自不同
- 解析失敗（header / dataSize / checksum / 長度）整包丟棄，計入 `stats` 的 parse errors

## 輸出 schema（v1）

每個 frame 是一個 UTF-8 JSON 物件，raw UDP 與 WebSocket 內容完全相同：

```json
{
  "v": 1,
  "seq": 1024,
  "t": 1765432100123,
  "venue": "704",
  "canvas": { "w": 2688, "h": 3840 },
  "events": [
    { "id": 42, "phase": "down", "face": 2,
      "cell": [7, 13], "px": [869, 850], "norm": [0.32329, 0.22135] }
  ],
  "alive": [42, 43]
}
```

| 欄位 | 型別 | 說明 |
|---|---|---|
| `v` | int | schema 版本，目前固定 `1` |
| `seq` | int64 | 服務啟動後嚴格遞增的 frame 流水號（可偵測丟包） |
| `t` | int64 | 發布當下 unix epoch 毫秒（UTC） |
| `venue` | string | 場域 profile 名稱 |
| `canvas` | object | 場域合併畫布尺寸（px） |
| `events[]` | array | 本 frame 的觸控事件（可為空，見 keepalive） |
| `events[].id` | int | touch ID，服務啟動後單調遞增、**永不重用** |
| `events[].phase` | string | `down` / `move` / `up` |
| `events[].face` | int | 感應面 index（venue JSON faces 陣列順序，704 場域 0-6） |
| `events[].cell` | [col, row] | 該面內的格子座標 |
| `events[].px` | [x, y] | 合併畫布絕對像素座標 |
| `events[].norm` | [nx, ny] | px / canvas 正規化座標（0~1，5 位小數） |
| `alive` | int[] | **當下所有活躍 touch ID 全清單**（TUIO 語意） |

### alive 全清單語意（防鬼點）

每包都帶 `alive`。訂閱端應以 `alive` 為唯一真相源對帳：
**任何不在 `alive` 裡的 ID 視為已離手**——即使 `up` 事件的那包 UDP 丟了，
下一包的 `alive` 也會把鬼點清掉。這是 TUIO alive 訊息的同等設計。

### keepalive snapshot

無事件期間每 `keepaliveIntervalMs`（預設 1000ms）發一包 `events:[]` 的全狀態
snapshot，讓訂閱端確認連線存活並對帳 `alive`。

## Touch ID 生命週期

```
down ── 該點「第一個封包」當下立即發布（零等待，無 debounce / 無手勢解算）
move ── 同 touch ID 座標變化（座標不變的重送不發事件）
up  ── 最後封包後 releaseTimeoutMs（預設 170ms）無新封包
```

- ID 配對：**最近鄰 slot 配對**（畫布像素距離、`matchRadiusPx` 預設 96px ≈ 704 場域 3 格）。
  同一 datagram 的多點批次處理：(slot, point) 所有半徑內候選配對依距離排序後貪婪指派，
  剩餘未配對的點若落在既有 touch 半徑內則视为同足多晶片丟棄，否則開新 touch。
- 一隻腳踩到多顆鄰近晶片（合包 / 跨模組 40ms 內兩個 datagram）→ 合併為一個 touch。
- 兩點交錯（交叉走過）：全域最近優先配對保證 ID 不互換（有單元測試釘住）。

## Release timeout 取值依據（為什麼是 170ms）

2026-06-10 南港現場 Wireshark 實測（感應閥值 700/1），「同一點持續按住」的重送間隔分佈：

| gap 區間 | 次數 | 備註 |
|---|---|---|
| 60–80ms | 38 | |
| 80–100ms | **351** | 主峰（p50 = 81.7ms） |
| 100–120ms | 3 | |
| 120–140ms | **49** | 長尾主要成分 |
| 140–160ms | 4 | |
| 160ms+ | 零星 | 多為真實離手 / 重踩混入 |

- 按住期間 **>10% 的間隔超過 100ms**（117/506 = 23% 超過 100ms、其中 120-160ms 共 53 筆）。
- 若 timeout 設 100ms：120-160ms 長尾全部變成「假離手」→ up + 立刻重新 down，
  下游會看到觸點閃爍（Unity 端早期 touchEndTimeoutMs=120ms 也踩過同一問題）。
- **170ms = 涵蓋 160ms 實測長尾 + 10ms margin**，同時是 2× 主峰週期（80ms）再加緩衝；
  延遲代價是離手判定慢 170ms，對展演互動可接受。
- 此值在 `config.json` 的 `releaseTimeoutMs` 可調：現場若觀察到閃爍（假離手）調高、
  若需要更俐落的離手反應且硬體重送穩定可調低（不建議低於 140）。
- 對拍資料源：`tests/fixtures/capture-*.txt`（真實 capture，測試直接重算上表驗證）。

## 已知特性（與 Unity 端對等，刻意保留）

1. **高列號晶片映射偏移**：Unity `RawToPixel` 把 (N-1) 段的 raw 校準範圍拉伸到
   N 格像素範圍，靠近 rawMax 的晶片中心會被 round 進下一格（例：Floor 面第 124 列
   晶片 → cell row 125）。本服務逐 bit 保留此行為（全晶片掃描測試釘死），
   確保與 Unity 投影端的格子判定一致。
2. **rawMax 邊界 px**：`rawMax` 角落映射出的 px 恰好等於 `origin + 面尺寸`
   （面外一 px 的邊界值），cell 會 clamp 回最後一格。下游用 px 做精確判定時注意。
3. **跨距 ≥3 格的合包拆 touch**：同一 datagram 內兩點距離 ≥ matchRadius（96px）
   會被視為兩個 touch（Unity SensorBridge 同樣是嚴格 `<` 半徑判定）。
   實測有單腳觸發 4 晶片跨 3 格的 case 會拆成 2 個 touch；如不希望，調大 `matchRadiusPx`。

## WebSocket 細節

- 端點：`ws://<host>:20111/`（port 由 config `webSocketOutput.port` 指定）
- 連上後服務先推一包 **hello**（識別：有 `"type":"hello"` 欄位；一般 frame 沒有 `type`）：

```json
{
  "type": "hello", "v": 1, "venue": "704",
  "canvas": { "w": 2688, "h": 3840 },
  "releaseTimeoutMs": 170,
  "faces": [
    { "index": 0, "name": "Wall Left", "originX": 0, "originY": 640,
      "width": 640, "height": 2048, "cols": 20, "rows": 128, "cellW": 32, "cellH": 16 }
  ],
  "lockedCells": [
    { "face": 2, "cell": [7, 13], "source": "auto", "packets": 125 }
  ],
  "autoLockActive": false,
  "autoLockRemainingSeconds": 0
}
```

hello 的鎖定相關欄位（v1 相容性新增，2026-06）：

| 欄位 | 說明 |
|---|---|
| `lockedCells[]` | 當下所有被鎖定（過濾中）的晶片格。鎖定格不會產生任何觸控事件 |
| `lockedCells[].source` | 鎖定來源：`config`（config.json）/ `auto`（啟動自檢窗）/ `manual`（stdin lock 指令） |
| `lockedCells[].packets` | `auto` 來源 = 自檢窗內收到的封包數；其他來源固定 0 |
| `autoLockActive` | 啟動自檢窗是否進行中（true 期間服務**不發布任何觸控事件**，keepalive 照發） |
| `autoLockRemainingSeconds` | 自檢窗剩餘秒數（無條件進位；非自檢中 = 0） |

- 之後每包都是 frame（schema 同上），**除了** `type:"locks"` 推播（見下）

### locks 推播（鎖定變動通知）

鎖定清單變動時（啟動自檢窗結束、stdin `lock` / `unlock` 指令），服務對**所有**
WS client 廣播一包 locks 訊息（識別：`"type":"locks"`）：

```json
{
  "type": "locks",
  "lockedCells": [
    { "face": 2, "cell": [7, 13], "source": "auto", "packets": 125 }
  ],
  "autoLockActive": false,
  "autoLockRemainingSeconds": 0
}
```

- `lockedCells` 為**全量清單**（不是 delta），欄位同 hello——收到直接整包替換即可
- 一般訂閱端（遊戲 / 美術程式）**可以無視**這個訊息：鎖定格本來就不會有事件。
  它存在的目的是讓監控頁（testpage）即時顯示鎖定狀態
- 訂閱端 parser 規則：**帶 `type` 欄位的訊息都不是觸控 frame**（目前有 hello / locks /
  log / raw / calibration，未來可能新增），未知 type 一律忽略

### log 推播（僅 loopback；UI 終端用，2026-06 v6 新增）

服務 log 即時推給 **loopback 來源的 WS client**（識別：`"type":"log"`）：

```json
{ "type": "log", "ts": 1765432100123, "level": "Info", "line": "[Host] 啟動完成…" }
```

| 欄位 | 說明 |
|---|---|
| `ts` | log 時刻 unix epoch 毫秒 |
| `level` | `Debug` / `Info` / `Warn` / `Error` |
| `line` | log 內文（不含時間戳前綴——`ts`/`level` 已是獨立欄位） |

- **只推給 loopback（127.0.0.1）連線**：LAN 訂閱端收不到（它們本來就會忽略帶
  `type` 的訊息，但 log 內容也不該外流到內網）
- loopback client 連上時先補發最近 **300 行** ring buffer，之後即時推
- 這是 UI「終端」tab 的資料源，**不屬於對外協議**——LAN 訂閱端永遠看不到此 type

### raw 推播（僅 loopback；校正精靈用，2026-06 v8 新增）

校正工作階段進行中（`POST /calibration/start` 後、stop / 15 分鐘逾時前），
服務把每個解析成功的 raw 點（**路由成功與否都推**）發給 **loopback WS client**：

```json
{ "type": "raw", "x": 5448, "y": 348, "src": "192.168.1.31", "port": 49152, "face": 2, "n": 3 }
```

| 欄位 | 說明 |
|---|---|
| `x` `y` | 感應模組封包內的原始 raw 座標（未經校正映射） |
| `src` `port` | 來源模組的 IP / port（一個模組一個來源） |
| `face` | 以**目前**場域 profile 路由到的面 index；路由不到（新場域校正中常態）= `null` |
| `n` | 該來源**同座標連續重送**次數——精靈的穩定偵測依據（n ≥ 3 = 穩定，自動取值） |

- 節流：同座標前 3 包照推（讓 UI 看到穩定進度 n=1,2,3）、之後壓到每秒 1 包心跳；
  座標一變立即推；全域上限 ~30 msg/s（per-source 隔離——別的模組在叫的噪訊
  晶片不會打斷正在踩的角落晶片的連續計數）
- 工作階段狀態變化（start / stop / 逾時）另推 `{"type":"calibration","active":bool}`
- 非校正階段零訊息、零成本；**校正期間觸控發布完全照常**（raw 推播是額外 tap）
- 與 log 同一條安全線：**僅 loopback**，內部訊號不外流內網；不屬於對外協議

- 同 port 的 HTTP：`GET /venue` 回完整場域 profile（含 raw 校準值，CORS `*`）、
  `GET /` 回內建 testpage
- 實作為最小 RFC 6455（text frame、close、ping/pong），不支援壓縮擴充；
  場館內網使用，不對公網開放

## 觸控模式與協議（2026-06 補充）

- **touch frame 協議不因 touchMode 而變**：`touchMode: "mouse"`（滑鼠模式）只是服務
  本機額外掛一個 OS 游標 sink，WS / raw UDP 的全量發布、schema、時序完全照常——
  訂閱端不需要也無法感知目前模式。
- **config API 是本機管理介面、不屬於對外協議**：與 WS 同 port 另有
  `GET /config`、`POST /config`、`GET /monitors`、`GET /history`、
  `POST /locks/lock`、`POST /locks/unlock`、`POST /locks/save`、`POST /command`、
  `POST /calibration/start`、`POST /calibration/stop`、`GET /calibration/status`、
  `POST /calibration/save` 等 HTTP 端點（給內建設定/診斷/終端/校正頁用）。
  **寫入類端點、/monitors、/command 與 /calibration/\* 僅接受 loopback 來源**
  （非 loopback 一律 403）；`GET /history`（疑似噪訊排行，`?top=N` 預設 50）
  與 `GET /config` 為唯讀、不限 loopback——場館內網監看 OK。
  皆不在 v1 承諾範圍內、欄位可能隨版本調整，訂閱端不要依賴。
  端點細節見 `DEPLOY.md`「啟動 UI」。
- **`/calibration/*`（v8）**：校正精靈的工作階段控制與兩角校正寫檔——
  start/stop 開關 raw 推播（見上方 raw 推播段；15 分鐘無 stop 自動結束）、
  save 寫 `venues/<名>.json` + 更新 config `venueProfile`（**重啟生效**，
  驗證全過才動檔案）。流程見 `CALIBRATION.md`。
- **`POST /command`（v6）**：UI 終端的指令通道——body `{"cmd":"history 20"}` →
  回 `{"output":"..."}`。與 stdin 走同一個 CommandProcessor，輸出文字一致；
  搭配上方 log 推播（type:"log"，僅 loopback），UI 終端 = console 的完整替代。

## 版本策略

- `v` 欄位代表 schema 主版本。**v1 承諾：既有欄位不改名、不改型別、不移除**；
  新增欄位視為相容變更（訂閱端必須忽略未知欄位）。
- 破壞性變更才會升 `v2`，並保留 v1 輸出開關過渡。
- TUIO 1.1（`/tuio/2Dcur`，OSC over UDP）為 v1 的預留插槽：config 已有 `tuioOutput`
  欄位、程式內有 `TuioPublisher` 介面位，啟用會明確報錯未實作。實作要點已註記於
  `src/TouchService/Output/TuioPublisher.cs`（alive/set/fseq bundle、norm 座標直接可用）。
