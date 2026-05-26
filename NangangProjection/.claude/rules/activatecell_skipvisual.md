# ActivateCell skipVisual 路徑規則

## skipVisual=true 呼叫方會繞過 if(!skipVisual) 區塊

`ActivateCell` 有兩條路徑：

| 呼叫方 | skipVisual | 說明 |
|---|---|---|
| `OnUdpPacket` (line ~457) | `true` | UDP 感應封包，每 100ms 大量呼叫 |
| `OnHoldTick` (line ~604) | `true` | 按壓持續 tick |
| `OnLeftClick` (line ~603) | `false`（預設） | 滑鼠點擊控制格 |
| 直接呼叫 (line ~797) | `false`（預設） | 其他一次性觸發 |

## 規定：凡是必須對「所有路徑」生效的邏輯，不得放在 if(!skipVisual) 區塊內

```csharp
// ❌ 錯誤：放在 if(!skipVisual) 內，UDP 路徑（skipVisual=true）完全繞過
if (!skipVisual)
{
    if (_jumpModeEnabled) { ... }  // UDP 看不到這裡
    if (!shouldActivate) return;
    SetActive();
}
BroadcastCanvasCell(...);

// ✅ 正確：跳變模式等「廣播前攔截邏輯」放在區塊外、BroadcastCanvasCell 之前
if (!skipVisual)
{
    // 視覺 debounce 只影響視覺，返回後廣播不執行（return 在區塊內）
    if (!shouldActivate) return;
    SetActive();
}
// 這裡才是所有路徑共用的攔截點
if (_jumpModeEnabled) { ... }
BroadcastCanvasCell(...);
```

## 為什麼 skipVisual=true 仍需要廣播攔截

UDP 路徑跳過視覺更新（SetActive）是為了效能考量（不重複閃燈），
但廣播事件（BroadcastCanvasCell / SensorBus.Touch）仍需被狀態邏輯管控。
跳變模式、未來任何「每落腳只觸發一次」的語意，都屬於廣播層邏輯，
必須放在 skipVisual 區塊之外。

## 已知 skipVisual=true 用途總結

- **視覺 debounce 不重複計時**：OnUdpPacket 不應該每 100ms 更新 `_chipLastTouchTimes`
- **SetActive 不重複執行**：避免 UDP 高頻閃燈
- **不代表「不廣播」**：廣播由跳變/冷卻等廣播層邏輯自行決定
