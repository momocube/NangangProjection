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
  │   per-cell 計數 + episode 切分（間隔 > releaseTimeout = 新段）、不進 tracker、不發布
  │   （keepalive 照發）；窗結束時命中任一判準（≥ minPackets 包 OR ≥ minEpisodes 段）的
  │   格子加入上面的 filter（auto 來源）。窗結束後此節點變 no-op，行為完全恢復正常
  ▼
TouchTracker（Tracking/TouchTracker.cs）— 最近鄰貪婪配對 → touch ID 生命週期（down/move/up）
  │   └─ TriggerHistory（Diagnostics/TriggerHistory.cs）— 觸發歷史 + 疑似噪訊評分：
  │      down/up 事件與自檢窗內被抑制的觸發都餵進 per-cell 聚合（固定大小、30 分鐘
  │      rolling 用分鐘桶環形陣列、runtime-only 重啟歸零）。出口：GET /history、
  │      stdin `history`、UI 診斷 tab。被鎖定 cell 的觸發在 filter 就被擋掉——
  │      鎖定後歷史自然停止累積（鎖定前的保留顯示）
  ▼
TouchPipeline（TouchPipeline.cs）— 串起以上全部；flush timer（5ms）驅動 release timeout
  ▼
FrameSerializer（Output/FrameSerializer.cs）— 統一 JSON frame（events + alive + seq）
  ├─ UdpJsonPublisher（Output/UdpJsonPublisher.cs）→ config endpoints（預設 127.0.0.1:20110）
  ├─ WebSocketHub（Output/WebSocketHub.cs）→ ws://:20111/ + HTTP GET /venue + testpage
  │     └─ 同 port 本機管理 API（Api/ConfigApi.cs）：GET/POST /config、/monitors、
  │        /history（疑似噪訊排行，唯讀不限 loopback）、/locks/*（lock/unlock/save）
  │        ——寫入類僅 loopback；給內建設定/診斷頁用，不屬於對外協議
  └─ TuioPublisher（Output/TuioPublisher.cs）→ **未實作的插槽**（啟用會明確報錯）

TouchPipeline 另一條支線（touchMode=mouse 時；typed 事件、不經序列化）：
  └─ MouseDriver（Mouse/MouseDriver.cs）— ITouchEventObserver，**canvas 十字映射**
       （canvas norm → 螢幕 rect + content 校正）、啟用面過濾（mouseMode.faces，
       預設排除 Floor）、跨面單點所有權 + 8ms 平滑迴圈
       └─ IMouseInjector：Win32MouseInjector（SendInput ABSOLUTE|VIRTUALDESK）
          / NullMouseInjector（非 Windows no-op）/ FakeInjector（測試）

旁路元件（v6）：
  ├─ CommandProcessor（Api/CommandProcessor.cs）— 指令單一入口：stdin 迴圈與
  │    POST /command 走同一個，輸出文字一致（stats/touches/history/locks/lock/
  │    unlock/savelocks/probe/quit；quit 經 host.ShutdownRequested 優雅關閉）
  ├─ LogStreamer（Logging/LogStreamer.cs）— FileLogger.EntryWritten → ring 300 行
  │    + 背景執行緒推 {"type":"log",...} 給 **loopback WS client**（連上先補 backlog）
  └─ ITrayHost（Tray/）— Windows 托盤常駐：WindowsTrayHost（P/Invoke Shell_NotifyIcon，
       獨立 STA thread + hidden window message loop；雙擊開 UI / 右鍵選單 / TaskbarCreated
       重掛）/ NullTrayHost（非 Windows、trayEnabled=false 全 no-op）。
       ConsoleWindowManager（GetConsoleWindow+ShowWindow）負責 console 隱藏/叫回，
       `--console` flag 跳過啟動隱藏
```

執行緒模型：**receive thread 直通**（收包當下同步走完 parse→route→map→track→publish，
無 queue、無幀循環——這正是 Unity 版延遲的反面教材）；另有 flush timer thread（5ms）
處理 release timeout 與 keepalive。兩者以 `TouchPipeline` 內單一 lock 序列化。
滑鼠模式再多一條 8ms 平滑迴圈 thread（MouseDriver 內部，自有 lock）。

入口：`Program.cs` → `TouchServiceHost.cs`（config 載入、CommandProcessor、啟動 log）
→ 啟動完成後托盤化（Windows + ui.trayEnabled：ConsoleWindowManager.Hide + TrayHostFactory，
`--console` flag 不隱藏）→ `Ui/UiLauncher.cs` 自動開 UI（Windows + ui.autoLaunch：
Edge `--app=` → Chrome `--app=` → 預設瀏覽器；UiLauncher 在 Program.Main 呼叫而非
host.Start 內——測試大量啟動 host，不該彈視窗）。stdin 迴圈把每行丟給
host.Commands.Execute；console 不可用（隱藏後重導 / service 模式）時 graceful no-op。

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
| autoLock 自檢窗 | 啟動後 10s 內命中**任一判準（OR）**的格子自動鎖（enabled 預設 true）：**判準 A** 封包 ≥ minPackets（預設 10）；**判準 B** 分離 episode ≥ minEpisodes（預設 3、0 = 停用）。episode = 同格封包流以「間隔 > releaseTimeout」切段——與 TriggerHistory 偽生命週期同一條 gap 邏輯，單位與正常 touch 生命週期一致 | **開機重啟時場上理應無人**——此時還在發訊的格子幾乎必然是故障晶片。判準 A 抓快速重送型（~80ms 重送 10s 約 125 包，門檻 10 遠低於噪訊、高於誤踩）；判準 B 抓**慢速頻閃型**（2026-06-11 現場 face2 cell(37,14)：每次只發 1 包、間隔 0.5~1.5s 反覆 down/up，包數永遠湊不滿判準 A）。**episode 判準對開機場景是鐵的：場上沒人時真人誤踩是一次連續事件（1 段），不會在同一格分離地觸發 3 次**——分離觸發 ≥3 段只剩故障晶片一種解釋 | 窗期間不發布任何事件（keepalive 照發）；重啟時有人站場上會誤鎖（stdin `unlock` 救，DEPLOY.md 有 SOP）；`savelocks` 才會持久化到 config；鎖定 log 標明每顆命中原因（packets / episodes / packets+episodes）。minEpisodes 別降到 2——「踩了又回頭再踩一次」就是 2 段 |
| touchMode 滑鼠 = 額外 sink | mouse 模式只在 pipeline 掛 typed 事件觀察者（MouseDriver），WS/UDP 發布照常全量 | Unity 時代「滑鼠模擬**取代**發布」正是這個服務要逃離的架構（單點限制、全場冷卻的根源） | 把滑鼠模式做成取代發布會回到老路；監控頁/訂閱端必須在任何模式下照常收事件 |
| 滑鼠單點所有權 | **跨所有啟用面**：任一啟用面（mouseMode.faces）第一個 down 的 touch 是 owner，它 up 前其他觸點（含其他啟用面）全忽略；未勾選面（預設地板）滑鼠 sink 完全無視 | OS 游標只有一顆——多點搶游標會抖動亂跳；地板不過濾的話腳步會亂點 | 要多點手勢去訂閱端做；owner 生命週期由 release timeout 橋接，80ms 重送不會中斷。模式/faces 熱切時若 owner 還按著會先 LEFTUP（不留卡死按鍵） |
| canvas 十字映射 | 滑鼠落點 = canvas norm（px ÷ canvas 尺寸）→ 螢幕 rect 等比 + content offset/scale 校正，**不是單一面拉伸到全螢幕** | 投影螢幕顯示的是整個場域 canvas 的十字展開圖——v4 面內拉伸映射讓「摸右牆游標跑到螢幕中央地板區」（2026-06 現場 bug）；canvas 座標系才跟投影畫面一一對應 | content 預設 0/0/1/1 = 滿版；投影 letterbox 時用 content 校正（DEPLOY.md 有流程）。落點 clamp 回螢幕 rect，校正填錯不會飛出目標螢幕。改映射公式同步改 `MouseDriver.MapCanvasToScreen` 與其測試 |
| 托盤 P/Invoke 不用 WinForms | Shell_NotifyIcon 手寫 P/Invoke（Tray/WindowsTrayHost.cs，~300 行），不是 net8.0-windows + WinForms NotifyIcon | WinForms 多目標要 `EnableWindowsTargeting=true` 才能在 Linux build、TFM 分岔（net8.0 + net8.0-windows）、publish 變胖 ~15MB+；P/Invoke 保持單一 TFM net8.0 / console OutputType / exe ~35MB 不變 | 托盤行為（圖示、雙擊、選單）Linux 測不到——純對映層有單元測試（TrayMenu），實機行為屬現場驗證。若未來要氣球通知 / 動態圖示再評估 WinForms 路線 |
| log push 僅 loopback | type:"log" 訊息只送 loopback WS client（per-client IsLoopback 旗標）；ring buffer 300 行連線即補發 | LAN 訂閱端本來就忽略帶 type 的訊息（協議規則），但 log **內容**不該外流內網（路徑、鎖定座標、內部狀態）；與寫入類 API 的 loopback 限制同一條安全線 | LogStreamer 是 enqueue + 背景推送執行緒——log 熱路徑零阻塞、推送產生的 log 不遞迴。改 ring 上限同步改 `LogStreamer.Capacity` 與測試 |
| 滑鼠平滑迴圈 | 8ms timer 指數逼近（factor=mouseMode.smoothing），單步 < moveMinPx 不注入；down 第一下例外：直接跳到位**再** LEFTDOWN | 80ms 封包間隔直接設游標會肉眼跳格；位移門檻防 125Hz 微步轟炸；先到位再按下否則 click 落在舊位置 | 平滑只存在滑鼠 sink——發布座標仍是原始值（服務端不平滑、保真實，§6-4 同一原則）；收斂到位的最後一步豁免 moveMinPx（保證精確到點） |
| 疑似噪訊評分 | 三因子加權（0~100）：**頻率 40**（30 分鐘觸發次數 / 30 飽和）＋**孤立 35**（down 當下 tracker 內無鄰近活躍 touch 的比率）＋**佔用 25**（總活躍時長占運行時間） | 為什麼這三個因子：間歇噪訊 = 同格反覆觸發（正常遊玩單格不會幾十上百次）、無鄰近連動（真人腳掌/移動幾乎總是多格連動——另一腳、上一步觸點還活著）、常駐叫（佔用比真人高一個量級）。三者皆可解釋、可單獨驗證。鄰近半徑 = 2×matchRadiusPx（≈192px）：matchRadius 內的點必被合併進同一 touch，更小的半徑（如 ±2 格）不可能有「另一個」活躍 touch | **分數只是排序用，不自動鎖——營運中有真人在踩，誤鎖比漏鎖傷**（鎖定一律人工確認：UI 診斷 tab 按鈕 / stdin lock）。改權重同步改 `TriggerHistory.cs` 常數與本表；歷史 runtime-only 重啟歸零（見 §6-8） |

## 4. 場域可攜性（venue profile）

- `venues/704.json` = 7 個面的 rawMin/Max、翻轉、orientation、在展開畫布上的 origin/尺寸
  ＋畫布總尺寸。**由 Unity 的 Face_*.asset ×7 一次性轉出**（2026-05 現場校正值）。
- 新場域 = 寫一份新 JSON + 跑 `CALIBRATION.md` 的校正流程，**code 一行不動**。
- 立體場域（地板+牆）展開成 2D 平面的思路：每面是畫布上的一塊矩形，face config 負責
  raw→面內→畫布的轉換。訂閱端拿到的 px/norm 都是畫布座標。

## 5. 測試結構（219 個，必須全綠才能交付）

| 測試檔 | 守什麼 |
|---|---|
| PacketParserTests | 真實 capture 的 golden hex（1~4 點合包）+ 5 種拒收 |
| FaceMappingTests | 七面邊界 21 組 golden + **14,272 顆全晶片**對拍 Unity 參考實作 |
| TouchTrackerTests | 80ms 重送不假離手、160ms gap@170 不假離手、>170 up、丟包注入 |
| TouchIdContinuityTests | 移動不換 ID、兩點交錯/交叉不互換 |
| LockedCellsTests | 鎖定格不產生事件、相鄰格不受影響、config 容錯 |
| AutoLockTests | 自檢窗：達門檻鎖/未達不鎖、窗內零發布、keepalive 照發、停用=現狀、**episode 判準**（慢速頻閃 3 段×1 包鎖 / 長串 1 段走 packets 不退化 / 真人連續 1 段不鎖 / 2 段不鎖 + gap==timeout 算同段 / minEpisodes=0 停用 / 鎖定 log 命中原因三態）、minEpisodes config 容錯、lock/unlock/locks/savelocks 指令、savelocks roundtrip、WS hello lockedCells + locks push（時間注入，無 sleep） |
| FrameSerializerTests | schema 欄位、alive、seq |
| WebSocketHubTests | RFC6455 對打（.NET ClientWebSocket）、三種 frame 長度編碼 |
| WiresharkReaderTests | capture 解析（630 點全 checksum 通過全可路由） |
| EndToEndTests | 真 socket 全鏈路：60 點併發、down 延遲 **p95<5ms 且 max<25ms**、無假 up。timing 斷言前先送 warm-up 觸發（不計入）暖 JIT——首 touch 冷啟在高 load 機實測 7~13ms，逐點 <5ms 斷言曾因此 flake，v7 起改 p95/max 後連跑穩定 |
| ServiceConfigTests | config 預設值/缺欄位容錯（含 touchMode/mouseMode/ui.trayEnabled）、舊 config `face` 單數 → `faces` 相容轉換 + 旗標 |
| MouseDriverTests | down 先到位再 LEFTDOWN、平滑收斂、**canvas 十字映射**（純函式對拍：content offset/scale、負座標 rect、clamp）、faces 過濾（勾選面注入 / 地板與空清單零注入）、跨面單點所有權、熱切強制放鍵、SendInput 正規化、UpdateTuning 熱套用 |
| ConfigApiTests | GET/POST /config roundtrip（faces 陣列 + content）+ 寫回保留其他欄位、舊欄位 `face` API 相容、非法值 400 全拒（不留半套）、非 loopback 403、/monitors 非 Windows 空陣列、locks API、按住中 mouse→grid 熱切自動 LEFTUP |
| CommandProcessorTests | stdin 與 POST /command 同指令同輸出、用法錯誤/未知指令訊息、quit 觸發 ShutdownRequested、/command 非 loopback 403、壞 body 400 |
| LogStreamTests | ring buffer 上限 300、log 訊息 schema、loopback 連上 hello→backlog→即時推、**非 loopback client 不收 log type**（frame 照常收；既有 WS harness 從 LAN IP 連入驗證） |
| TrayHostTests | factory 決策（trayEnabled=false / 非 Windows → NullTrayHost）、NullTrayHost no-op、托盤訊息/選單 → 動作純對映（雙擊=開 UI、選單三指令）。Windows 實機行為屬現場驗證 |
| TriggerHistoryTests | 觸發歷史聚合（down/up、30 分鐘 rolling 衰減、move 不換 cell）、孤立判定（同批/追溯撤銷/遠處不算）、自檢窗 suppressed gap 邏輯、評分（噪訊 ≥80 vs 正常遊玩 ≤40 對比）、記憶體有界（聚合不隨事件數成長）、pipeline 整合（窗內計入 / 鎖定後停止累積）。時間注入，無 sleep |
| HistoryApiTests | GET /history 排序/欄位/top/locked 旗標、空歷史、POST /locks/lock 語意同 stdin lock + 非 loopback 403（/history 唯讀可看）、stdin `history` 排行格式與 top 筆數 |

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
5. 噪訊晶片三層工作流（完整 SOP 見 DEPLOY.md）：**開機自檢自動鎖（autoLock）→
   營運中看診斷排行一鍵鎖（UI 診斷 tab / GET /history）→ savelocks 持久化**；
   console 指令（touches/history/lock）為備援。自檢兩判準（OR）：包數抓快速重送型、
   分離 episode 數抓慢速頻閃型（v7 起，見 §3 表）。剩餘盲點：間歇性發作的晶片若窗內
   剛好沒發訊（或發訊 <3 段）就抓不到——觸發歷史 + 評分就是為此而生
   （窗內被抑制的觸發也計入歷史）。
   反向風險：重啟時有人站場上會被誤鎖（`unlock` 救）。`stats` 的 dropped by lock /
   auto-locked cells 可驗證過濾生效。
6. **滑鼠模式是單點**（OS 游標只有一顆，結構限制不是 bug）——v6 起所有權跨所有
   啟用面（任一啟用面第一個 down 持有游標）；觸控板手勢 / 多點要走訂閱端。
   滑鼠模式下摸牆會搶機器的真實游標——維運先切回 grid（設定頁熱切，
   按住中切換會自動 LEFTUP）。
7. config API 的熱套用範圍只到 touchMode / mouseMode（faces/screen/content/smoothing/
   moveMinPx）/ releaseTimeoutMs / matchRadiusPx；autoLock 與 ui.trayEnabled 寫回
   config 下次啟動生效；listenPort / 輸出 port 需重啟（UI 唯讀顯示）。
   `maxSlots` / `flushIntervalMs` 等冷門欄位刻意不進 API——改 config.json。
8. **觸發歷史是 runtime-only，重啟歸零——刻意的**：噪訊晶片（尤其間歇型）會很快重新
   冒上排行，持久化歷史反而把「上次營運的舊帳」跟「現在的狀態」混在一起；要持久的是
   鎖定結果（savelocks），不是統計。另兩個已知近似：孤立因子的鄰近判定用 down 時座標
   （touch 後續移動不更新鄰近關係）、touch 存活時長含 releaseTimeout 尾巴（~170ms）
   ——對排序用途無影響。歷史的 releaseTimeout / 鄰近半徑取啟動值，不跟 config 熱套用。
9. **托盤 / console 隱藏是 Windows-only 行為**：Linux CI 只能測抽象層（factory 決策、
   訊息對映、NullTrayHost）；圖示顯示、雙擊、右鍵選單、console show/hide、
   TaskbarCreated 重掛屬**現場驗證項**（流程見 DEPLOY.md「托盤常駐」）。
   UI 視窗（Edge --app）關閉只是關瀏覽器、服務照跑——托盤雙擊隨時叫回。

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
2. 改 code → `dotnet test` 219 全綠（E2E timing 斷言 v7 起為 p95/max + warm-up，連跑穩定；若仍偶發 flake 重跑一次）
3. 協議有變 → 同步改 `PROTOCOL.md` + `MIGRATION-ART-APP.md`，新增欄位 OK、
   改名/移除 = 升 v2
4. 發布：見 §5 結尾的 publish 參數紅字
5. 交付前用 `tools/SensorSimulator` 跑 `scenarios/stress-60.json` 過一輪
6. commit 風格：中文、`feat:/fix:/docs:/test:` 前綴，看 `git log` 仿照
