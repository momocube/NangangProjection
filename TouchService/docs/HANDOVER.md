# TouchService 交接總覽

> 給接手同仁：這支服務把 CyberCube 光域場域的觸控訊號從 Unity 專案獨立出來，
> 任何程式（Unity / TouchDesigner / 瀏覽器 / Python）都能用標準協議訂閱觸控事件。

## 這是什麼

- **獨立的觸控服務**（.NET 8 console、Windows 單檔 exe、headless）
- 收感應模組的 UDP 廣播（port 20001）→ 解析 / 7 面分類 / 座標映射 / touch ID 追蹤
  → 以 JSON 廣播 touch down/move/up 事件
- 觸控邏輯**移植自** Unity 專案 `NangangProjection`（封包解析、座標公式、
  slot 配對都與 Unity 端對拍），但去掉視覺 / 手勢 / OS 點擊模擬，只留純觸控語意
- 一句話：**「14,272 顆晶片 → 乾淨的 multi-touch 事件流」**

## 目錄結構

```
TouchService/
├── TouchService.sln
├── config.json                  ← 服務設定範本（欄位見 docs/DEPLOY.md）
├── venues/704.json              ← 光域 704 校正 profile（見 docs/CALIBRATION.md）
├── src/TouchService/            ← 服務本體
│   ├── Core/                    ← 封包解析、面分類、座標數學（Unity 對拍）
│   ├── Tracking/TouchTracker.cs ← touch ID 生命週期（down/move/up）
│   ├── Output/                  ← UDP JSON / WebSocket(RFC6455 自寫) / TUIO 插槽
│   ├── Diagnostics/             ← LatencyProbe（環形 buffer）+ stats
│   └── TouchServiceHost.cs      ← 組裝：UDP listen / timer / stdin 指令
├── tools/SensorSimulator/       ← 離場驗證核心工具（replay + scenario 兩模式）
│   └── scenarios/               ← single-stomp / two-feet-cross / stress-60
├── tools/TestClient/            ← raw UDP 訂閱範例 / 端到端驗證 client
├── tests/                       ← 212 個測試（xunit）+ 真實 Wireshark capture fixtures
├── testpage/index.html          ← 瀏覽器測試/校調頁（零依賴單檔）
└── docs/                        ← PROTOCOL / DEPLOY / CALIBRATION / HANDOVER
```

## 快速上手（開發機）

```bash
dotnet build TouchService.sln          # 建置
dotnet test  TouchService.sln          # 跑 212 個測試（含 60 點併發端到端）

# 跑起來看看（三個終端）
dotnet run --project src/TouchService                       # 服務
dotnet run --project tools/TestClient                       # 看 raw UDP 輸出
dotnet run --project tools/SensorSimulator -- scenario tools/SensorSimulator/scenarios/stress-60.json
# 瀏覽器開 http://127.0.0.1:20111/ → 連線 → 看觸點
```

發布到場館機：見 `docs/DEPLOY.md`。

## 三種訂閱方式

### 1. raw UDP JSON（最低延遲，給 Unity / TouchDesigner / 任何後端）

config 的 `udpOutput.endpoints` 加上你的 `host:port`，然後收 UDP 即可。

**Python：**
```python
import socket, json

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.bind(("0.0.0.0", 20110))
while True:
    frame = json.loads(sock.recv(65535))
    for e in frame["events"]:
        print(f'#{e["id"]} {e["phase"]} face={e["face"]} px={e["px"]} norm={e["norm"]}')
    alive = set(frame["alive"])   # 對帳：不在 alive 的 ID 一律視為已離手
```

**C#（Unity 也適用，開背景執行緒收）：**
```csharp
using var udp = new UdpClient(20110);
var remote = new IPEndPoint(IPAddress.Any, 0);
while (true)
{
    var json = Encoding.UTF8.GetString(udp.Receive(ref remote));
    var frame = JsonSerializer.Deserialize<JsonElement>(json);
    foreach (var e in frame.GetProperty("events").EnumerateArray())
        Console.WriteLine($"#{e.GetProperty("id")} {e.GetProperty("phase")} " +
                          $"px=({e.GetProperty("px")[0]},{e.GetProperty("px")[1]})");
}
```

### 2. WebSocket（給瀏覽器 / 跨網段工具，連上先收場域輪廓）

```javascript
const ws = new WebSocket("ws://192.168.x.x:20111/");
ws.onmessage = ev => {
  const msg = JSON.parse(ev.data);
  if (msg.type === "hello") {        // 連上第一包：場域輪廓（faces 幾何）
    drawVenue(msg.canvas, msg.faces);
    return;
  }
  for (const e of msg.events)        // 之後每包：touch 事件 frame
    handleTouch(e.id, e.phase, e.px, e.norm);
  reconcile(new Set(msg.alive));     // 用 alive 全清單清鬼點
};
```

### 3. HTTP GET /venue（場域校正資料，一次性拉取）

```bash
curl http://192.168.x.x:20111/venue   # 回完整 venue JSON（含 raw 校準值，CORS *）
```

> **TUIO**：v1 預留插槽未實作（config `tuioOutput` + `TuioPublisher.cs` 介面位）。
> 需要接 TUIO 生態（如 TouchDesigner 原生 TUIO In）時照檔內註記實作即可。

schema 完整定義 / touch 生命週期 / timeout 取值依據 → `docs/PROTOCOL.md`。

## 關鍵設計決定（為什麼長這樣）

| 決定 | 理由 |
|---|---|
| down 首包立即發（無 debounce） | 互動展演對延遲極敏感；Unity 端 320ms 延遲教訓 |
| up 用 170ms timeout | 實測重送 gap 長尾 120-160ms，100ms 會假離手（histogram 見 PROTOCOL.md） |
| 每包帶 alive 全清單 | up 丟包不殘留鬼點（TUIO 語意） |
| WS 自寫 RFC6455 | HttpListener 在 Windows 需 netsh urlacl 註冊，場館部署多一道坑；自寫零依賴 |
| 座標公式逐 bit 對拍 Unity | 投影（Unity）與觸控（本服務）的格子判定必須一致，quirk 也保留 |
| simulator 用真實 capture | 離場驗證的基準是現場 Wireshark 資料，不是想像的理想封包 |

## 維運 FAQ

- **觸點閃爍**（按住卻 up/down 交替）→ `releaseTimeoutMs` +20ms 觀察；
  根因通常是模組重送變慢（檢查感應閥值設定）
- **某一面整面沒反應** → `stats` 看 unrouted；raw 範圍沒蓋到 → CALIBRATION.md 四角測試
- **一隻腳變兩個點** → 晶片觸發跨距 ≥3 格；`matchRadiusPx` 調大（如 112）
- **延遲高** → `probe` dump CSV 看 recv→publish（正常 <100µs）；高的話查訂閱端
- **與 Unity 同時收不到包** → 兩程式搶 UDP 20001，見 DEPLOY.md 共存注意
