# 外部觸碰事件對接文件

南港投影裝置（NangangProjection）可在「外部監聽」模式下，將地板感應觸碰座標以 UDP 封包即時傳送給外部軟體。

---

## 啟用方式

1. 在 Unity 場景 `sensor_base` 的 `SensorClickSimulator` 元件上，確認 **externalIP** 與 **externalPort** 填入目標地址。
2. 執行後，點擊控制台右下角的模式按鈕，循環切換至「**外部監聽**」（紫色按鈕）。

> 模式順序：內部監聽 → PostMessage → 滑鼠點擊 → **外部監聽** → …

---

## 封包規格

| 項目 | 說明 |
|---|---|
| 傳輸協定 | **UDP** |
| 預設目標 | `127.0.0.1 : 20002` |
| 字元編碼 | UTF-8 |
| 格式 | JSON（無換行，無 BOM） |

### 封包內容

```json
{"col": 3, "row": 5}
```

| 欄位 | 型別 | 說明 |
|---|---|---|
| `col` | int | 觸碰格欄（X 方向），從 0 起算 |
| `row` | int | 觸碰格列（Y 方向），從 0 起算 |

> 座標上限由當下載入的 **VenuePresetConfig** 決定（每個場域的欄列數不同），接收端不應假設固定範圍。

> **每次感應觸發只送一個封包。** 若啟用「感應冷卻」，同一格在冷卻時間內的重複觸碰不會觸發封包。

---

## 格子座標對應實際像素

每格固定為 **16 × 32 px**（寬 × 高）：

```
像素 X = col × 16 + 8   ← 格子中心
像素 Y = row × 32 + 16
```

實際畫布解析度與格子數量依場域而異，可參考各 `VenuePresetConfig` 的 `canvasWidth / canvasHeight`。

---

## 接收範例

### Python

```python
import socket, json

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.bind(("0.0.0.0", 20002))

while True:
    data, addr = sock.recvfrom(256)
    touch = json.loads(data.decode("utf-8"))
    print(f"觸碰：欄={touch['col']}  列={touch['row']}")
```

### Node.js

```js
const dgram = require("dgram");
const sock = dgram.createSocket("udp4");

sock.on("message", (msg) => {
  const touch = JSON.parse(msg.toString("utf8"));
  console.log(`觸碰：欄=${touch.col}  列=${touch.row}`);
});

sock.bind(20002);
```

### C# (.NET / Unity)

```csharp
using System.Net;
using System.Net.Sockets;
using System.Text;

var client = new UdpClient(20002);
while (true)
{
    var ep   = new IPEndPoint(IPAddress.Any, 0);
    var data = client.Receive(ref ep);
    var json = Encoding.UTF8.GetString(data);
    // 解析：System.Text.Json 或 Newtonsoft.Json
    // e.g. { "col": 3, "row": 5 }
}
```

---

## 注意事項

- `externalIP` 可填區網內其他電腦 IP，實現跨機接收。
- 接收端需在 Unity 啟動**前**開好 socket，避免漏掉最初幾個封包。
- 冷卻設定（`clickCooldown`）對外部監聽同樣有效，預設 1 秒。
- 此模式**不模擬滑鼠點擊**，不影響 Display 2 上的任何視窗。
