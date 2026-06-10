# VCell 模式 CanvasTouch 廣播規則

## 禁止在 HandleChipTouch 內對每顆晶片呼叫 SensorBus.CanvasTouch

VCell 模式下，一個 VCell 包含 N 顆晶片。
若每顆晶片都廣播 `SensorBus.CanvasTouch`，下游訂閱者（SensorClickSimulator、SensorBridgeToPassPosition）
一個 VCell 踩踏就會收到 N 個相同座標的事件，造成重複互動。

```csharp
// ❌ 錯誤：在 HandleChipTouch 每次呼叫都廣播
public void HandleChipTouch(int faceIdx, int chipIdx)
{
    // ...
    SensorBus.CanvasTouch(center.x, center.y, _canvasW, _canvasH); // ← 3 晶片 = 3 次廣播
}

// ✅ 正確：在 Update() 對每個活躍 VCell 廣播一次
void Update()
{
    // ...
    if (anyWarm)
        SensorBus.CanvasTouch(c.x, c.y, _canvasW, _canvasH); // ← 1 VCell = 1 次廣播
}
```

## 廣播職責分工

| 事件 | 觸發位置 | 頻率 | 說明 |
|---|---|---|---|
| `SensorBus.CanvasTouch` | `VirtualCellManager.Update()` | 每幀（活躍 VCell） | 連續觸碰信號，供 Drag / 水波用 |
| `VirtualCellBus.VirtualCellTouch` | `HandleChipTouch` → `BroadcastTouch()` | 一次（VCell 啟動時） | 離散事件，供網頁互動邏輯用 |
| `VirtualCellBus.VirtualCellRelease` | `Update()` → `BroadcastRelease()` | 一次（VCell 釋放時） | 離散事件 |
| `SensorBus.CanvasTap` | SmartTouch `FireTap()`（VCellHasSibling 去重） | 一次（手勢解算時） | 輕觸語意，已 per-VCell 去重 |

## 為什麼 CanvasTouch 不能只在 VCell 啟動時發一次

SensorClickSimulator 的 Drag 模式和 SensorBridgeToPassPosition 依賴持續的 CanvasTouch
來判斷「是否還在按壓 / 拖曳中」。只發一次會讓拖曳效果在第一幀後就停止。
在 Update() 每幀發一次，語意與原本 per-chip 100Hz 廣播等效，且不再 × N。
