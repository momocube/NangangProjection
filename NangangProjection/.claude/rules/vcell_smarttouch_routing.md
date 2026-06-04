---
name: vcell-smarttouch-routing
description: VCell 模式下 SmartTouch 事件路由規則
metadata:
  type: feedback
---

# VCell 模式 SmartTouch 事件路由規則

## 規定：VCell 模式不能完全抑制 SmartTouch Canvas 事件

`FireTap / FirePressStart / FirePressEnd / FireDrag` 在 VCell 模式下，
必須改以 **VCell 中心座標** 廣播 Canvas 事件，不能直接跳過整個 else。

```csharp
// ❌ 錯誤：VCell 模式完全抑制
if (_vcellManager == null || !_vcellManager.Enabled)
{
    BroadcastCanvasCell(fd, col, row, TouchKind.Tap);
    SensorBus.Tap(col, row);
}
// VCell 模式：什麼都不做 → SensorClickSimulator / 遊戲效果收不到訊號

// ✅ 正確：VCell 模式改用 VCell 中心座標廣播
if (_vcellManager != null && _vcellManager.Enabled)
{
    if (_vcellManager.TryGetVCellCenter(faceIdx, cellIdx, out float cx, out float cy, out float cw, out float ch))
        SensorBus.CanvasTap(cx, cy, cw, ch);
}
else
{
    BroadcastCanvasCell(fd, col, row, TouchKind.Tap);
    SensorBus.Tap(col, row);
}
```

## 為什麼不能抑制

| 下游訂閱者 | 依賴的事件 | 抑制後影響 |
|---|---|---|
| `SensorClickSimulator.OnSmartTap` | `OnCanvasTap` | Tap 模式不發 OS 點擊 |
| `SensorClickSimulator.OnSmartPressStart` | `OnCanvasPressStart` | Press 模式不發 MouseDown |
| 遊戲效果（水波等） | `OnCanvasTap / OnCanvasPressStart` | 踩踏無反應 |

`HandleChipTouch` 的 `CanvasTouch`（每 100ms）只代表「持續按壓中」，
**不能**替代 Tap（離腳瞬間）、PressStart（長壓開始）等離散狀態事件。

## HandleChipTouch 個別晶片視覺規定

`HandleChipTouch` 必須呼叫 `_tileGrid?.SetActive(faceIdx, chipIdx)`，
讓使用者在落腳期間看到個別晶片亮紅，而非只有 VCell amber 色塊。

TileGridTexture.SetActive 內建防重繪（已是 Active 時只更新 timer），
100Hz 呼叫安全，不會造成每幀 Apply。
