# 場景 GameObject 組織規則

## 兩個場景共同結構

```
Main Camera          ← URP 主相機，輸出到 Display 1
Camera_Display2      ← URP 副相機，輸出到 Display 2
Canvas_Primary       ← Display 1 UI（操作者控制台）
  ├─ CanvasScaler
  └─ [Raycaster，見下方差異]
Canvas_Secondary     ← Display 2 UI（投影地板感應格）
  └─ CanvasScaler
EventSystem          ← Unity 輸入事件系統（不動）
[感應系統]           ← 所有感應相關功能（原名 SensorTestManager）
[除錯工具]           ← 所有除錯工具，平時 inactive（原名 Log）
[截圖備援]           ← Display 2 截圖（原名 Display2ScreenCapture）
```

---

## [感應系統] GO 元件清單與順序

> 元件順序代表功能優先級：核心功能在前，可選功能在後。

### 核心（不能停用）
1. `SensorTestManager` — 感應系統主控、場域切換、格子管理
2. `UdpSensorReceiver` — 接收地板感應器訊號

### 功能擴充（停用後只影響對應功能）
3. `SensorClickSimulator` — 踩踏轉換為電腦滑鼠點擊
4. `VirtualCellManager` — 虛擬大格模式（多格合一）
5. `ConsoleGameOverlay` — 控制台格子後方的畫面預覽
6. `Display2DimOverlay` — 地板畫面亮度調暗控制
7. `MiniWindowController` — 控制台縮小成右下角迷你視窗

---

## [除錯工具] GO 元件清單（平時 inactive）

> 整個 GO 平時停用。需要除錯時才 SetActive(true)。

1. `TouchDebugLogger` — F9 開啟踩踏紀錄視窗
2. `CoordTestPanel` — 座標校準輸入測試面板
3. `GameConsole` — 外部黑色命令列視窗

---

## 兩個場景的差異

| 項目 | sensor_2048x768（舞台） | sensor_2688x3840（光域 704） |
|------|----------------------|--------------------------|
| `Canvas_Primary` Raycaster | 內建 `GraphicRaycaster` | 自訂 `Display1GraphicRaycaster` |
| `[截圖備援]` GO 狀態 | **inactive**（不啟用） | **active**（啟用，704 使用截圖做備援預覽） |
| VirtualCellManager 預設模式 | None（不使用虛擬大格） | Size32 或 Size64 |

### 為什麼 704 用 Display1GraphicRaycaster？
704 場景有 `SensorClickSimulator` 的 PostMessage 模式，需要防止感應點擊事件誤觸 Display 1 的控制台按鈕。
`Display1GraphicRaycaster` 覆寫 Unity 的 Raycast，在偵測到感應器注入的點擊時主動壓制，保護操作者介面。
舞台場景不用此保護（或暫時用內建版本）。

### 為什麼 704 啟用截圖備援？
704 場景的 `ConsoleGameOverlay` 在沒有遊戲場景 RT 時，fallback 到 `Display2ScreenCapture` 的 GDI 截圖。
舞台場景不需此備援，故停用節省資源。

---

## 各元件停用後的影響

| 元件 | 停用影響 | 可安全停用？ |
|------|---------|------------|
| `SensorTestManager` | ❌ 所有感應功能失效 | 否 |
| `UdpSensorReceiver` | ❌ 地板踩踏完全無反應 | 否 |
| `SensorClickSimulator` | 踩踏不再觸發外部程式點擊 | ✅ 是（純感應測試時） |
| `VirtualCellManager` | 虛擬大格模式停用，回到小晶片格 | ✅ 是 |
| `ConsoleGameOverlay` | 控制台格子後方的預覽畫面消失 | ✅ 是 |
| `Display2DimOverlay` | 無法調整地板亮度 | ✅ 是 |
| `MiniWindowController` | 縮小按鈕消失 | ✅ 是 |
| `Display1GraphicRaycaster` | ⚠️ 感應點擊可能誤觸控制台按鈕 | 謹慎 |
| `Display2ScreenCapture` | 截圖預覽備援失效 | ✅ 是（704 場景） |
| `[除錯工具]` 整個 GO | 所有除錯工具停用 | ✅ 是（正式演出時） |

---

## 快速操作指引

### 正式演出模式（最精簡）
- `[除錯工具]` GO → SetActive(false)（已預設）
- `SensorClickSimulator` → 依需求選模式
- `Display2DimOverlay.dimAlpha` → 調整至需要的亮度

### 除錯感應問題
- `[除錯工具]` GO → SetActive(true)
- `TouchDebugLogger.listenRawUdp` → true（看原始 UDP 座標）
- `TouchDebugLogger.listenSensorBus` → true（看格子事件）

### 關閉外部點擊模擬（只測感應視覺）
- `SensorClickSimulator` → 停用此元件（Inspector 左上角打勾取消）

---

## GO 命名規則

- `[功能群組]` 格式：用方括號標示這是功能群組，不是單一物件
- 純容器 GO（無腳本）：使用描述性名稱，如 `ControlGridParent`、`SensorGridParent`
- 系統 GO：使用 Unity 慣例，如 `Main Camera`、`EventSystem`
