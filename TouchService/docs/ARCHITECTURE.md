# TouchService 架構與設計記憶文件

> **這份文件的讀者是未來接手修改的 AI / 工程師。**
> 目的：讓你不需要重新考古就能理解這個系統為什麼長這樣、改哪裡、什麼不能動。
> 配套文件：`PROTOCOL.md`（對外協議）、`DEPLOY.md`（部署/config）、
> `CALIBRATION.md`（新場域校正）、`MIGRATION-ART-APP.md`（訂閱端接入）、`HANDOVER.md`（總覽）。

## 1. 這是什麼、為什麼存在

CyberCube 光域場域（南港 704 等）的**獨立觸控服務**：收地板/牆面感應晶片的 UDP 廣播，
轉成標準觸控事件（down/move/up + touch ID），透過 WebSocket / raw UDP JSON 發布給
任意數量的訂閱端（遊戲、美術互動程式、監控頁）。

**它從南港 Unity 專案移植而來**（2026-06-11，整段歷史見 §7）。拆出來的原因：

1. 原架構讓 Unity 程式模擬 OS 滑鼠點擊餵給下游 → 單點限制、全場 1 秒冷卻、延遲 300ms+
2. 客戶需求 15 人 × 4 觸發點 = **60 點併發**，OS 滑鼠模擬結構上不可能
3. Unity 端 Display2ScreenCapture 在主執行緒拷 31MB/幀拖垮幀率，觸控跟渲染綁死
4. 未來要複製到其他場域（佈局不同），需要 config 化的場域描述

實測成果：踩下→訂閱端收到 down < 5ms（軟體段 ~30µs）；現場踩下→投影變色約 0.1s
（剩餘延遲為硬體偵測 40–80ms + 顯示鏈，軟體已不是瓶頸）。

## 2. 訊號流程（一條直線）

```
硬體模組 ×N（每模組 8 磚 × 8 晶片，獨立 UDP 廣播 → port 20001）
  │ 封包：55 55 [frame:1] [dataSize:2BE] ([X:2BE Y:2BE]×N) [SUM:2BE]
  ▼
SensorPacketParser（Core/SensorPacketParser.cs）— header/長度/checksum 驗證，壞包丟棄
  ▼
SensorFaceRouter（Core/SensorFaceRouter.cs）— raw 座標 → 7 面線性分類
  ▼
SensorFaceConfig（Core/SensorFaceConfig.cs）— raw → canvas px / cell（校正數學，逐 bit 對等 Unity）
  ▼
LockedCellFilter（Core/LockedCellFilter.cs）— 噪訊晶片黑名單（config lockedCells + runtime
  │   auto/manual 鎖定共用，來源以 LockSource 區分；熱路徑 O(1) volatile snapshot HashSet）
  ▼
AutoLockSelfCheck（Core/AutoLockSelfCheck.cs）— 啟動自檢窗（預設 10s）：窗內的點只做
  │   per-cell 計數、不進 tracker、不發布（keepalive 照發）；窗結束時 ≥ minPackets 的
  │   格子加入上面的 filter（auto 來源）。窗結束後此節點變 no-op，行為完全恢復正常
  ▼
TouchTracker（Tracking/TouchTracker.cs）— 最近鄰貪婪配對 → touch ID 生命週期（down/move/up）
  ▼
TouchPipeline（TouchPipeline.cs）— 串起以上全部；flush timer（5ms）驅動 release timeout
  ▼
FrameSerializer（Output/FrameSerializer.cs）— 統一 JSON frame（events + alive + seq）
  ├─ UdpJsonPublisher（Output/UdpJsonPublisher.cs）→ config endpoints（預設 127.0.0.1:20110）
  ├─ WebSocketHub（Output/WebSocketHub.cs）→ ws://:20111/ + HTTP GET /venue + testpage
  └─ TuioPublisher（Output/TuioPublisher.cs）→ **未實作的插槽**（啟用會明確報錯）
```

執行緒模型：**receive thread 直通**（收包當下同步走完 parse→route→map→track→publish，
無 queue、無幀循環——這正是 Unity 版延遲的反面教材）；另有 flush timer thread（5ms）
處理 release timeout 與 keepalive。兩者以 `TouchPipeline` 內單一 lock 序列化。

入口：`Program.cs` → `TouchServiceHost.cs`（config 載入、stdin 指令 stats/touches/probe/quit、
啟動 log）。

## 3. 不能隨便動的設計決策（含依據）

| 決策 | 值/做法 | 依據 | 改之前要知道 |
|---|---|---|---|
| release timeout | **170ms**（config 可調） | 2026-06-10 現場 Wireshark：按住期間重送 gap 主峰 80ms、長尾 120–160ms 共 53/506 筆 | 低於 140ms 會假離手（觸點閃爍）；это物理極限——硬體沒有離手訊號，只有重送停止 |
| down 即時發 | 首包零等待 | 延遲需求的核心；tap/press 語意判定刻意**不做**（Unity 版 touchEndTimeoutMs 等待就是 300ms 的元兇之一） | 不要把任何手勢判定加回服務端；那是訂閱端的事 |
| alive 全清單 | 每 frame 都帶 | UDP 丟包時 up 事件可能遺失；TUIO 同款設計 | 拿掉它鬼點就回來了 |
| touch ID 配對 | 全域最近 pair 貪婪（非 slot-major） | slot-major 在事件驅動下兩點交錯會 ID 互換（單元測試 TouchIdContinuityTests 釘死） | 改配對演算法必須過 ID 連續性測試 |
| matchRadiusPx | 96px（= 3 格） | 對等 Unity matchRadiusCells=3；一腳踩多晶片合併為一 touch | 60 人密度下若出現「相鄰玩家觸點併吞」調小；單腳被拆兩 touch 調大 |
| 座標映射 quirk | (N-1) 段拉伸到 N 格、高列號 round 進下一格 | **刻意逐 bit 保留 Unity 行為**（FaceMappingTests 全晶片 14,272 顆對拍） | 「修正」它會讓格子判定跟 Unity 投影端不一致——除非兩邊同步改 |
| WS server 自寫 RFC6455 | 不用 HttpListener/Kestrel | Windows 部署免 netsh urlacl、單 port 兼營 /venue 與 testpage、零依賴 | 不支援壓縮擴充；場館內網用，勿直接暴露公網 |
| checksum 不含 frame byte | 對等 Unity 解析 | 實測 448 真實封包全數通過 | frame byte 目前沒人用，可未來做硬體丟包偵測 |
| 鎖晶片 | config lockedCells（face+cell），路由後、追蹤前丟棄 | 現場有噪訊晶片持續發訊（Unity 版有同功能但 runtime 手動、無持久化） | 這是遮瑕不是修復；座標要回報硬體商 |
| autoLock 自檢窗 | 啟動後 10s 內被觸發 ≥10 包的格子自動鎖（enabled 預設 true） | **開機重啟時場上理應無人**——此時還在發訊的格子幾乎必然是故障晶片；噪訊 ~80ms 重送 10s 約 125 包，門檻 10 遠低於噪訊、高於誤踩 | 窗期間不發布任何事件（keepalive 照發）；重啟時有人站場上會誤鎖（stdin `unlock` 救，DEPLOY.md 有 SOP）；`savelocks` 才會持久化到 config |

## 4. 場域可攜性（venue profile）

- `venues/704.json` = 7 個面的 rawMin/Max、翻轉、orientation、在展開畫布上的 origin/尺寸
  ＋畫布總尺寸。**由 Unity 的 Face_*.asset ×7 一次性轉出**（2026-05 現場校正值）。
- 新場域 = 寫一份新 JSON + 跑 `CALIBRATION.md` 的校正流程，**code 一行不動**。
- 立體場域（地板+牆）展開成 2D 平面的思路：每面是畫布上的一塊矩形，face config 負責
  raw→面內→畫布的轉換。訂閱端拿到的 px/norm 都是畫布座標。

## 5. 測試結構（120 個，必須全綠才能交付）

| 測試檔 | 守什麼 |
|---|---|
| PacketParserTests | 真實 capture 的 golden hex（1~4 點合包）+ 5 種拒收 |
| FaceMappingTests | 七面邊界 21 組 golden + **14,272 顆全晶片**對拍 Unity 參考實作 |
| TouchTrackerTests | 80ms 重送不假離手、160ms gap@170 不假離手、>170 up、丟包注入 |
| TouchIdContinuityTests | 移動不換 ID、兩點交錯/交叉不互換 |
| LockedCellsTests | 鎖定格不產生事件、相鄰格不受影響、config 容錯 |
| AutoLockTests | 自檢窗：達門檻鎖/未達不鎖、窗內零發布、keepalive 照發、停用=現狀、lock/unlock/locks/savelocks 指令、savelocks roundtrip、WS hello lockedCells + locks push（時間注入，無 sleep） |
| FrameSerializerTests | schema 欄位、alive、seq |
| WebSocketHubTests | RFC6455 對打（.NET ClientWebSocket）、三種 frame 長度編碼 |
| WiresharkReaderTests | capture 解析（630 點全 checksum 通過全可路由） |
| EndToEndTests | 真 socket 全鏈路：60 點併發、down<5ms、無假 up（timing 斷言偶發 flake，重跑即可） |
| ServiceConfigTests | config 預設值/缺欄位容錯 |

fixtures：`tests/fixtures/capture-*.txt` = 2026-06-10 南港現場真實 Wireshark export，
**所有 timeout/封包格式的爭論先重放它再說**。

開發環境：Linux/.NET 8 可直接 `dotnet test`；發布 Windows 版**必須**
`-p:SelfContained=true -p:PublishSingleFile=true`（csproj 只有 RuntimeIdentifiers，
漏 SelfContained 會變 framework-dependent ~9MB，現場沒裝 runtime 跑不起來；正確 ~35MB）。

## 6. 已知限制與未來工作

1. **up 延遲 ~170–250ms 是物理極限**（80ms 重送週期 + timeout）。要更快需硬體提速重送
   或加離手封包——跟硬體商談的事，軟體端到頂了。
2. **TUIO 輸出未實作**（TouchDesigner 原生支援，要接 TD 時做）：實作要點註記在
   `Output/TuioPublisher.cs`，alive/set/fseq bundle、norm 座標直接可用。
3. **硬體行為依據單日實測**（閥值 700/1）：AutoLED 檔位會改變重送週期（曾實測 500/3
   → 240ms）。現場改閥值設定後要重驗 releaseTimeoutMs。
4. 踩兩磚之間時 px 在磚界抖動是物理現象（兩晶片輪流回報）——服務端不平滑（保真實），
   視覺端用黏滯判定處理（見 `testpage/colorpad.html` 的 HYST 實作）。
5. 噪訊晶片以**啟動自檢窗（autoLock）自動鎖定為主**、stdin `lock` 手動補充；
   `savelocks` 寫回 config 持久化。自檢的盲點：間歇性發作的晶片若窗內剛好沒發訊
   就抓不到（用 `touches` 找、`lock` 補）；反向風險：重啟時有人站場上會被誤鎖
   （`unlock` 救）。`stats` 的 dropped by lock / auto-locked cells 可驗證過濾生效。

## 7. 歷史脈絡（考古入口）

- 2026-06-09~10：南港現場排查 300ms 延遲。Wireshark 抓包、Unity 端 LatencyProbe/
  RuntimeToggle（repo 內 `NangangProjection/Assets/_Script/Debug/`）narrow 出
  幀率崩（Display2ScreenCapture）+ Tap 等待 + 全域 cooldown 三因。
- 2026-06-11：決定拆獨立服務，一日完成移植（核心 ~70% 搬自 Unity code）、
  現場驗證 0.1s。延遲根因完整分析見 repo `docs/LATENCY_HOTSPOTS.md`、
  硬體行為見 `docs/SENSOR_FIELD_REPORT.md`。
- Unity 端對應實作（移植母本）：`Assets/_Script/Input/UdpSensorReceiver.cs`、
  `Core/SensorFaceConfig.cs`、`Sensor/SensorBridgeToPassPosition.cs`（slot 配對前身）、
  `Sensor/SensorTestManager_SmartTouch.cs`（生命週期前身，其 tap/press 語意已捨棄）。

## 8. 接手修改 SOP

1. 讀這份 + `PROTOCOL.md`，動協議欄位前先看 §3 的表
2. 改 code → `dotnet test` 103 全綠（timing flake 重跑一次）
3. 協議有變 → 同步改 `PROTOCOL.md` + `MIGRATION-ART-APP.md`，新增欄位 OK、
   改名/移除 = 升 v2
4. 發布：見 §5 結尾的 publish 參數紅字
5. 交付前用 `tools/SensorSimulator` 跑 `scenarios/stress-60.json` 過一輪
6. commit 風格：中文、`feat:/fix:/docs:/test:` 前綴，看 `git log` 仿照
