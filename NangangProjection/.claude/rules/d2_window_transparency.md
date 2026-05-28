# D2 視窗透明度規則

## D2 Unity 視窗必須同時設 WS_EX_LAYERED + WS_EX_TRANSPARENT

`Display 2 (Canvas_Secondary)` 純用於顯示感應格，不需要接收任何 OS 點擊事件。
要讓透明度有效（OS 桌面透出）並保持背景程式互動：

```csharp
// ✅ 正確：兩個 flag 一起加
int newEx = ex | WS_EX_LAYERED | WS_EX_TRANSPARENT;
SetWindowLong(_display2Hwnd, GWL_EXSTYLE, newEx);
SetLayeredWindowAttributes(_display2Hwnd, 0, bAlpha, LWA_ALPHA);

// ❌ 錯誤：只加 WS_EX_LAYERED，視窗仍是 hit-target，
//    WindowFromPoint 返回 Unity HWND，PostMessage 打到自己
```

## 為什麼需要 WS_EX_TRANSPARENT

| 狀態 | WindowFromPoint(D2) | PostMessage 目標 |
|---|---|---|
| 無 WS_EX_LAYERED/TRANSPARENT | Unity HWND | Unity（無效）|
| 只有 WS_EX_LAYERED | Unity HWND（bAlpha>0 時）| Unity（無效）|
| WS_EX_LAYERED + WS_EX_TRANSPARENT | 背景程式 HWND | 背景程式 ✓ |

## IsValidDisplay2Hwnd 邏輯：拒絕 Unity HWND，允許其他

D2 Unity 視窗設 click-through 後，`WindowFromPoint(D2)` 返回背景程式 HWND。
`IsValidDisplay2Hwnd` 必須 **拒絕 Unity 自己**（不是要求 Unity）：

```csharp
// ✅ 正確：拒絕 Unity 自己的 HWND，允許背景程式
if (hwnd == _knownDisplay2Hwnd) return false;
return true;

// ❌ 錯誤：要求 hwnd 必須是 Unity
if (_knownDisplay2Hwnd != Zero && hwnd != _knownDisplay2Hwnd) return false;
```

## 同步點

`ApplyD2WindowAlpha()` 必須在以下時機呼叫：
1. `FindDisplay2WindowCoroutine()` 找到 HWND 後（初始化）
2. `SyncD2TileAlpha()` 每次 overlayAlpha 改變時

## SensorTex idle 透明度同步點

`SyncD2TileAlpha()` 移除了「需遊戲載入」的 guard，必須在以下時機呼叫：
- `Start()` 的 `NotifyOverlay()` 之後（初始化）
- `SwitchPresetCoroutine()` 的 `NotifyOverlay()` 之後（場域切換）

若遺漏這兩個呼叫，TileGridTexture.Initialize() 預設 _sensorIdleAlpha=1（不透明），
overlay alpha slider 不會套用到初始狀態。
