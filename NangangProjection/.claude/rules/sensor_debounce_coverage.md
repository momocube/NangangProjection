# 感應冷卻 Debounce 覆蓋規則

## 觸發路徑必須全部通過同一個 debounce 字典

`_chipLastTouchTimes` 是物理滑鼠與感應訊號共用的 debounce 字典，key = `faceIdx * 100000 + cellIdx`。

## 兩條路徑的 gate 位置

| 路徑 | Gate 位置 | 備註 |
|---|---|---|
| 物理滑鼠點擊 | `OnControlLeftDown`（GridBuilder）最前面 | 同時攔 OnLeftClick + OnMousePressStart |
| 感應訊號 Tap | `FireTap()`（SmartTouch）開頭 | FlushExpiredSmartTouches 觸發的 Tap 解算 |

## 為什麼不能只靠 ActivateCell 的 debounce

`ActivateCell` 的 debounce 在 `if (!skipVisual)` 區塊內：
- 感應訊號呼叫 `ActivateCell(skipVisual:true)` → 整個 debounce 區塊被跳過
- 視覺實際來自 `FireTap()`，而非 `ActivateCell`

## 為什麼不能只靠 ActivateCell（物理滑鼠路徑）

`OnControlLeftDown` 同時呼叫：
1. `OnLeftClick` → `ActivateCell` (有 debounce)
2. `OnMousePressStart` (無 debounce)

若只靠 `ActivateCell` 的 debounce，`OnMousePressStart` 仍會在第二次點擊時廣播 SensorBus.PressStart 與 SetPressing，讓遊戲仍然響應。

## 新增事件路徑時的檢查清單

新增任何會呼叫 `_tileGrid?.SetActive` / `SensorBus.*` 的路徑時，需確認：
- 路徑是否在 `OnControlLeftDown` 之下（物理滑鼠）→ 由 gate 保護 ✓
- 路徑是否由 `FlushExpiredSmartTouches` 觸發（感應 Tap）→ 在該方法開頭加 debounce ✓
- 其他新路徑 → 自行在最上層加 `_chipLastTouchTimes` 檢查
