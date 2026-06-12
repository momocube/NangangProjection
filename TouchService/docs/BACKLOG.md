# TouchService / WallProjector 待辦累積（Backlog）

> 2026-06-12 整理。已討論、有設計方向、尚未實作的改進項——接手者照優先序取用。
> 每項背後的依據與設計細節見 ARCHITECTURE.md §3/§6/§9 與 ROADMAP-PROJECTION.md。

## 優先序高（已有完整設計依據）

1. **自適應 release timeout**（離手體感 -50ms）
   - per-touch 觀察重送 gap 穩定度：訊號穩（80±3ms）的 touch 用 ~120ms timeout、
     出現 jitter 自動放寬到 170+；滑鼠/多點模式固定 170（按住中斷代價高）
   - 依據：2026-06-12 per-chip 分析——健康晶片實壓 gap max 103ms（ARCHITECTURE §9）
2. **多點觸控模式（touchMode: multitouch）**
   - Windows Touch Injection API（InitializeTouchInjection / InjectTouchInput）
   - 解掉滑鼠模式單游標限制；多人多牆同時操作；瀏覽器原生收 touchstart
   - 注意注入生命週期：stale contact 會被 Windows 自動 cancel，flush timer 固定頻率重注入
   - WallProjector 直接受益（多人同時觸控不同牆面的網頁）
3. **校正精靈**（新場域複製的關鍵體驗）
   - UI 引導現場人員依序踩指定角落 → 自動算 rawMin/Max → 寫 venue JSON
   - 把校正從「工程師半天」變「現場人員十分鐘」；流程基礎見 CALIBRATION.md

## 優先序中

4. **WallProjector v2 輸入/相容強化**（視 v1 現場回饋）
   - 外站擋 iframe → CDP 注入旋轉 CSS；任意視窗（簡報/影片）→ capture 合成
5. **TUIO 輸出實作**（要接 TouchDesigner 時）——插槽已留，要點註記在 TuioPublisher.cs
6. **viewRotation 遷入 venue JSON**（多場域時）——v1 暫放 WallProjector config

## 優先序低 / 看時機

7. **frame byte 丟包偵測**——硬體封包的模組流水號目前沒人用（ARCHITECTURE §3）
8. **硬體商談判項**（軟體到頂之後真正的延遲底）：
   - 重送週期 80→40ms（多格同步已證明硬體做得到）→ 離手可壓到 ~90ms
   - 離手封包 → 離手延遲趨近 0
   - 已鎖噪訊晶片座標清單回報（`locks` 指令可隨時匯出）——鎖定是遮瑕不是修復

## 不要做的（決策記錄，防止接手者走回頭路）

- 服務端手勢判定（tap/press）——300ms 延遲的元兇之一，語意留給訂閱端
- 滑鼠模式取代發布——必須維持「額外 sink」，發布永遠全量
- 自動鎖在營運中觸發——誤鎖比漏鎖傷，營運中只排行不自動鎖
