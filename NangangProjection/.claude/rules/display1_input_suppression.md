# Display 1 Input Suppression 規則

## WH_MOUSE_LL hook callback 是非同步的，不能單獨依賴

`SetWindowsHookEx(WH_MOUSE_LL, ...)` 的 callback 透過向安裝執行緒 **post message** 觸發，
不是在 `SendInput()` 呼叫中同步執行。這代表：
- Hook 可能在 WM_LBUTTONDOWN 被 Unity 讀進 Input 系統之後才設定 suppression
- 若安全軟體阻擋 hook 安裝，`_sensorSuppressUntil` 永遠為 0

## 規定：所有感應器發出的 SendInput button 事件必須在呼叫前主動設定 suppression

```csharp
// ❌ 錯誤：只靠 hook callback 設定 suppression
SendInput(2, inputs, ...);

// ✅ 正確：先主動設定，再呼叫 SendInput
Display1GraphicRaycaster.NotifySensorClick();
SendInput(2, inputs, ...);
```

**需要呼叫的方法**：
- `SendMouseDown()` — LEFTDOWN 事件
- `SendInputClick()` — LEFTDOWN + LEFTUP 事件

**不需要呼叫的方法**：
- `SendMouseUp()` — suppression 由 SendMouseDown 的呼叫已涵蓋
- `SendMouseMove()` — MOVE 事件不觸發 Button click

## NotifySensorClick 對真實滑鼠無副作用

`ShouldSuppressDisplay1Pointer()` = `!_realLeftDown && time < _sensorSuppressUntil`

若使用者正按住真實左鍵（`_realLeftDown = true`），suppression 不生效，
真實操作照常。`_realLeftDown` 只由 WH_MOUSE_LL hook callback 管理。

## 已知尚未根治的情況（2026-05）

- 效果有改善（主動式 suppression），但仍有回報偶發誤觸
- 可能殘留原因待查：Listen 模式下 SensorBus 事件的下游訂閱者、
  Unity 多幀事件緩衝行為、或與系統 DPI 縮放有關的座標偏移
