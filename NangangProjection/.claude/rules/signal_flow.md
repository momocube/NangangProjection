# 訊號流程：UDP 輸入 → 視覺輸出

## 完整流程圖

```
[硬體感應器]
  UDP 封包：55 55 [frame:1] [dataSize H+L:2] [X_H X_L Y_H Y_L]×N [checksum H+L:2]
    │
    ▼
UdpSensorReceiver.ReceiveLoop()            ← 背景執行緒，禁止呼叫 Unity API
  │  協議解析：header 驗證、checksum、提取 (rawX, rawY)
  │  存入 Queue<(int rawX, int rawY)>（上限 maxQueueSize=64）
    │
    ▼ 每幀（主執行緒）
UdpSensorReceiver.Update()
  │  從佇列取出，最多 maxPointsPerFrame 個（0=不限）
  │  觸發 OnPacketReceived(rawX, rawY)
    │
    ▼
SensorTestManager.OnUdpPacket(rawX, rawY)
  │  迭代 VenuePresetConfig.faces[]
  │  SensorFaceConfig.ContainsRaw(rawX, rawY) → 找到負責的面
  │  SensorFaceConfig.RawToPixel()            → 畫布像素 (px, py)
  │  SensorFaceConfig.PixelToCell()           → 格子 (col, row)
  │  查鎖定快照 IsRawPixelLocked()            → 鎖定則跳過
  │  呼叫 ActivateCell(faceIdx, col, row, skipVisual: true)
    │
    ├──[GhostTouchMode != None]──────────────────────────────┐
    │   ProcessGhostTouch(faceIdx, cellIdx)                   │
    │   → 判斷是否孤立點                                      │
    │   → 孤立：進 pending 狀態，等待鄰居確認                  │
    │   → 有鄰居：解除 pending，繼續往下                       │
    └────────────────────────────────────────────────────────┘
    │
    ▼
UpdateSmartTouch(faceIdx, cellIdx)         ← SensorTestManager_SmartTouch.cs
  │  記錄每格接觸時間、位移累計
  │  若 100ms 內未再接收到同格訊號 → FlushExpiredSmartTouches()
    │
    ▼
FlushExpiredSmartTouches()
  │  根據接觸持續時間決定手勢種類：
  │  < tapBoundaryMs → FireTap()
  │  >= tapBoundaryMs，未移動 → FirePressEnd()
  │  有位移 → FireDrag()
    │
    ▼
FireTap() / FirePressStart() / FirePressEnd() / FireDrag()
  │  冷卻 debounce 檢查（_chipLastTouchTimes）→ 過冷卻期則攔截
  │  更新 TileGridTexture 視覺（SetActive / SetPressing）
  │
  ├──[VCell 模式 OFF]────────────────────────────────────────┐
  │   BroadcastCanvasCell(faceIdx, col, row, kind)           │
  │   SensorBus.Tap/PressStart/PressEnd/Drag(col, row)       │
  │   SensorBus.CanvasTap/CanvasPressStart/...(x, y, cw, ch) │
  │                                                           │
  └──[VCell 模式 ON]─────────────────────────────────────────┤
      VirtualCellManager.TryGetVCellCenter() → (cx, cy, cw, ch)
      SensorBus.CanvasTap/CanvasPressStart/...(cx, cy, cw, ch)
      VirtualCellBus.VirtualCellTouch/Release(cx, cy, cw, ch, sizePx)
                                                              │
SensorBus 訂閱者（並行觸發）◄────────────────────────────────┘
  ├── SensorClickSimulator     → OS 點擊（PostMessage / SendInput）
  ├── SensorBridgeToPassPosition → 水波效果座標傳遞
  ├── TouchDebugLogger         → IMGUI 觸碰日誌
  └── TileManager.OnSensorPoint → 舊格子系統（若場景有掛）
```

---

## 繞過條件對照表

| 步驟 | 繞過條件 | 結果 |
|------|----------|------|
| GhostTouch 過濾 | `ghostTouchMode == None` | 直接進 SmartTouch，不過濾孤點 |
| 格子觸發 | `IsRawPixelLocked()` 為 true | 整個 OnUdpPacket 跳過此格 |
| ActivateCell 視覺更新 | `skipVisual = true`（UDP 路徑） | 不呼叫 SetActive，但廣播仍執行 |
| 冷卻 debounce | `cooldownEnabled = false` | Fire* 方法不攔截，全部通過 |
| 晶片廣播（BroadcastCanvasCell）| VCell 模式 ON | 改用 VCell 中心座標廣播 |
| OS 點擊發送 | `ClickMode == Listen` | SensorClickSimulator 只走 SensorBus，不發 OS 事件 |
| OS 點擊發送 | `cooldownEnabled && 冷卻中` | CheckCooldown 阻擋，跳過此次點擊 |
| Display 1 Raycast | `ShouldSuppressDisplay1Pointer()` | hook 偵測感應點擊後壓制 D1 Unity 輸入事件 |
| VCell 觸碰廣播 | `IsVCellLocked()` 為 true | VirtualCellManager.HandleChipTouch 不廣播，不發 VirtualCellBus |

---

## 關鍵不變量（每次修改前確認）

1. **skipVisual=true 路徑不代表不廣播**
   廣播層邏輯（跳變模式、冷卻）必須放在 `if(!skipVisual)` 區塊**外**，
   否則 UDP 路徑（skipVisual=true）永遠繞過。
   → 詳見 `.claude/rules/activatecell_skipvisual.md`

2. **冷卻 debounce 的 gate 位置**
   物理滑鼠路徑：gate 在 `OnControlLeftDown` 最前面
   感應訊號路徑：gate 在 `FireTap()` 開頭
   → 詳見 `.claude/rules/sensor_debounce_coverage.md`

3. **VCell 模式不能完全抑制 Canvas 事件**
   FireTap/FirePressStart 必須改用 VCell 中心座標廣播，不能直接跳過。
   → 詳見 `.claude/rules/vcell_smarttouch_routing.md`

4. **Display 1 Suppression 必須主動設定**
   SendInput 前必須呼叫 `Display1GraphicRaycaster.NotifySensorClick()`，
   不能只靠 WH_MOUSE_LL hook callback（非同步）。
   → 詳見 `.claude/rules/display1_input_suppression.md`

---

## SmartTouch 狀態機（按格子）

```
[無接觸]
  → 收到 rawX/rawY → [接觸中 _touchStartTime 記錄]
  → 100ms 未再收到 → FlushExpiredSmartTouches()
      → 持續 < tapBoundaryMs → FireTap → [無接觸]
      → 持續 >= tapBoundaryMs → FirePressEnd（已廣播 PressStart）→ [無接觸]
      → 有位移且 >= tapBoundaryMs → FireDrag → [無接觸]
  → 持續接收同格 → 每次 UpdateSmartTouch 重設計時器
      → 超過 tapBoundaryMs → FirePressStart（一次）→ [按壓中]
```
