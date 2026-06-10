# LatencyProbe 延遲量測探針

**建立日期**：2026-06-10
**Branch**：`debug/latency-probe`
**起因**：南港旗艦店現場實測踩磚延遲 ~320ms，遠超過理論值（硬體 80ms + 軟體 ~20ms + LED ~30ms ≈ 130ms），約 190ms gap 在軟體層、需要實測 narrow root cause

---

## 背景脈絡

### 三層延遲拆解（Luke 確認）

1. **硬體鏈路**：晶片紅外偵測 → daisy-chain → 接收卡 → Mooncell → UDP 進電腦 = 80ms cycle（同磚連續觸發 UDP 間隔觀測值）
2. **軟體層**（Unity）：理論 ~20ms（UDP queue 等幀 + SetActive + Apply + vSync）
3. **LED 螢幕 input lag**：< 30ms（HDMI → LED 牆典型值）

理論最佳 ~130ms、實測 ~320ms、**gap 190ms 在軟體層、來源不明**。

### 嫌疑列表（按可能性排）

1. **SensorBus listener chain 同步阻塞**——`SensorBus.Invoke()` 順序呼叫所有訂閱者，整條鏈跑完才 return
   - `SensorClickSimulator` PostMessage 模式同步呼叫 `WindowFromPoint()` + `PostMessage()`
   - `SensorBridgeToPassPosition` 算水波座標
   - `SensorTestManager_Logger` 寫 CSV（同步 I/O）
2. **VCellManager.Update** 每幀掃所有 face × VCell × chip + 每個活躍 VCell 廣播 CanvasTouch
3. **Display2ScreenCapture** GDI 截圖 + RGB→BGR loop + Apply（30fps 觸發、執行那幀超預算）
4. **連續 frame drop 累積**——一幀爆 16.67ms → 33ms → UDP 在 queue 等更久

---

## 量測點（10 個 EventType）

| EventType | 位置 | 意義 |
|---|---|---|
| `UdpRecv` | `UdpSensorReceiver.ParsePacket` Enqueue 後（背景執行緒）| UDP 進 queue |
| `UdpDequeue` | `UdpSensorReceiver.Update` foreach 內 | 主執行緒拿到該點 |
| `SetActive` | `UpdateSmartTouch` 第一次踩到、SetActive 之前 | pixel buffer 改之前 |
| `ApplyStart` / `ApplyEnd` | `TileGridTexture.LateUpdate` 兩個 Apply 包夾 | 推 GPU 耗時（extra1=0 sensor, =1 control）|
| `FlushStart` / `FlushEnd` | `SensorTestManager.Update` FlushExpiredSmartTouches 包夾 | 手勢解算耗時 |
| `BusInvokeStart` / `BusInvokeEnd` | `FireTap` / `FirePressStart` / `FirePressEnd` / `FireDrag` 內 `SensorBus.*` 廣播包夾 | **listener chain 整條耗時** |
| `VCellUpdateStart` / `VCellUpdateEnd` | `VirtualCellManager.Update` 整段 | VCell 每幀掃描 + 廣播耗時 |
| `FrameTick` | `LatencyProbe.Update` 每幀 | frame number + deltaTime（看 frame drop）|

---

## 技術實作

### 環形 buffer + lock-free
- `ProbeEvent[]` 預分配 8192 筆
- `Interlocked.Increment` 寫索引（跨執行緒安全）
- 滿了覆寫舊資料

### 時間戳
- `System.Diagnostics.Stopwatch.GetTimestamp()`
- ns 精度、跨執行緒可比
- Awake 時 `_t0` baseline、之後都是相對時間

### 熱路徑零負擔
- `Mark()` 停用時 earliest return < 5ns
- 啟用時 ~50-200ns / 次
- 零 GC alloc（pre-allocated struct array）
- 符合 `.claude/rules/unity_hot_path.md`

### Dump
- F8 鍵盤觸發
- 寫到 `Application.persistentDataPath/latency-probe-YYYYMMDD-HHMMSS.csv`
- Console 印路徑（log）
- CSV 格式：`seq, t_us, event, extra1, extra2`

---

## 啟用 / 停用機制（兩層）

### 第 1 層：編譯時開關（`LATENCY_PROBE` define）

`LatencyProbe.Mark()` 用 `[Conditional("LATENCY_PROBE")]` 修飾。
**沒有定義 `LATENCY_PROBE` 時、所有 caller 的 `Mark()` call 編譯期被砍光、binary 完全乾淨、零 runtime cost、零 binary footprint。**

設定位置：
```
Edit → Project Settings → Player → Other Settings → Scripting Define Symbols
```

- **啟用偵錯（這個 branch）**：加上 `LATENCY_PROBE`
- **正式 release**：移除 `LATENCY_PROBE`、重 build → 所有 hook 點完全消失

啟動時 Console 會印出 compile-time 狀態確認：
```
[LatencyProbe] compile=ENABLED ...   ← define 有設
[LatencyProbe] compile=DISABLED (LATENCY_PROBE not defined — all Mark() calls compiled out) ...   ← 沒設
```

### 第 2 層：執行時開關（Inspector `probeEnabled`）

即使編譯有進去、Inspector 把 `probeEnabled = false` 也會讓 `Mark()` 第一行 early return。
適用於：build 進去了、但暫時關掉、或現場想切換不重 build。

執行時關掉的 cost：~5ns / 次 Mark、每秒 < 1us、可忽略。

### 對 production 的最佳實踐

正式發布 SOP：
1. 在 release build 前移除 `LATENCY_PROBE` define
2. 重 build → binary 完全沒有 LatencyProbe 相關 code path
3. LatencyProbe.cs 本身可以保留在專案、未來需要 debug 時加回 define 即可

## 使用方式（現場操作）

### 安裝
1. `git pull origin debug/latency-probe`
2. Unity Editor 開啟、確認 `LatencyProbe.cs` 編譯通過
3. **Edit → Project Settings → Player → Scripting Define Symbols 加上 `LATENCY_PROBE`**
4. 在場景內任意 GameObject 掛 `LatencyProbe` 元件
   - 推薦：放在 `[除錯工具]` GameObject 下、跟 `TouchDebugLogger` 同層
5. Inspector 確認 `probeEnabled = true`、`dumpKey = F8`
6. Build → 部署到現場機器

### 量測
1. 跑遊戲、等系統穩定
2. 踩磚塊 10-20 次（連續或間斷都可）
3. 按 **F8**
4. Console 出現：`[LatencyProbe] dumped N events → /path/to/csv`

### CSV 檔案位置（Windows）
```
%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\latency-probe-YYYYMMDD-HHMMSS.csv
```

---

## 分析方法

### 1. UDP → 螢幕的單次 e2e 延遲

挑同一個 (x, y) 的 `UdpRecv` → 追蹤到對應 `SetActive` → `ApplyEnd`：

```
t_us       event       extra1  extra2
1000.0     UdpRecv     3760    872
1008.5     UdpDequeue  3760    872        ← +8.5us, queue 等下一幀
1009.2     SetActive   0       123        ← faceIdx=0, cellIdx=123
1015.0     ApplyStart  0       0          ← 等本幀 LateUpdate
1018.3     ApplyEnd    0       0          ← +3.3us upload GPU

→ UDP→Apply 總耗時 = 18.3us（< 1 frame，很正常）
```

### 2. Listener chain 耗時（**最重要**）

每個 Tap/PressStart 都有 BusInvokeStart→End 一對：

```
t_us       event              extra1  extra2
5000.0     BusInvokeStart     0       123
5045.2     BusInvokeEnd       0       123

→ listener chain 耗時 = 45.2us
```

**判讀標準**：
- < 1ms：正常
- 1-5ms：偏重、可接受
- 5-20ms：明顯阻塞、值得 narrow listener
- > 20ms：**主嫌疑、必修**

### 3. VCellManager.Update 耗時

每幀都有一對：

```
VCellUpdateStart → VCellUpdateEnd
```

**判讀標準**：
- < 0.5ms：正常
- 0.5-3ms：704 場域多面正常範圍
- > 5ms：每幀都吃 30% budget、累積會 frame drop

### 4. Frame drop 偵測

`FrameTick` extra2 = deltaTime（us）：

```
event=FrameTick frame=12345 dt_us=16670   ← 60Hz 正常
event=FrameTick frame=12346 dt_us=33330   ← drop 到 30Hz
event=FrameTick frame=12347 dt_us=50000   ← drop 到 20Hz
```

連續 dt_us > 20000 = 連續 frame drop、累積延遲明顯。

### 5. 哪一幀爆預算？

把 FrameTick 之間的事件分組、加總每幀的 BusInvoke + VCellUpdate + Flush + Apply 耗時：

```
某一幀內:
  VCellUpdate: 3.2ms
  Flush:       0.5ms
  BusInvoke:   18ms  ← 異常
  Apply:       2.1ms
  總計:        23.8ms ← 超 16.67ms budget
→ 該幀 frame drop、下一個 UDP 累積等更久
```

---

## 預期結果（假設驗證）

### 如果 listener chain 是主因
- `BusInvokeStart → End` 平均 20-50ms
- 主嫌：`SensorClickSimulator.OnSmartTap`（PostMessage 同步 Win32 call）

### 如果 VCellManager 是主因
- `VCellUpdateStart → End` 平均 5-20ms
- 704 場域多面（14 個 face）、加上每活躍 VCell 廣播 chain 累積

### 如果 Display2ScreenCapture 是主因
- Frame 之間出現 ~33ms dt_us spike
- 規律性出現（每 1/captureFPS 秒一次）

### 如果硬體 Mooncell 才是主因
- Software-side 一切正常（每段 < 5ms）
- UDP 間 80ms cycle 正常
- 320ms 應該大部分在 Mooncell + LED、軟體無能為力

---

## 下一步

1. **同仁 build + 部署現場**
2. **現場跑、踩磚、F8 dump**
3. **CSV 檔回傳給 Luna（透過 Discord / 雲端 / git）**
4. **Luna 分析、定位 root cause**

---

## 改動檔案清單

| 檔案 | 改動 |
|---|---|
| `Assets/_Script/Debug/LatencyProbe.cs` | **新增**（單一 MonoBehaviour）|
| `Assets/_Script/Input/UdpSensorReceiver.cs` | +2 處 Mark（UdpRecv, UdpDequeue）|
| `Assets/_Script/Sensor/SensorTestManager_SmartTouch.cs` | +1 處 SetActive + 4 處 Bus 包夾（FireTap/PressStart/PressEnd/Drag）|
| `Assets/_Script/Sensor/SensorTestManager.cs` | Flush 包夾 |
| `Assets/_Script/Visual/TileGridTexture.cs` | Apply 包夾 ×2 |
| `Assets/_Script/Visual/VirtualCellManager.cs` | Update 整段包夾 |

---

## 注意事項

- **熱路徑符合 unity_hot_path.md 規則**：零 alloc、零 Debug.Log（停用模式）
- **不影響量測準度**：每次 Mark 50-200ns、相對於 ms 級延遲可忽略
- **probeEnabled 預設 true**：未來 release build 應改 false（或移除掛載）
- **F8 鍵衝突檢查**：現場若有其他工具用 F8、可改 dumpKey
- **CSV 檔不會自動清**：累積後手動清 `Application.persistentDataPath`
