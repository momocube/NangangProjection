# 腳本職責一覽

每個腳本「只做一件事」的說明。Claude 看到某個任務時，對照此表決定需要讀哪些檔案。

---

## 事件中樞（不含邏輯，不動）

| 腳本 | 職責 |
|------|------|
| `SensorBus.cs` | 靜態 C# 事件廣播中樞：Touch / Tap / PressStart / PressEnd / Drag / Canvas* 系列 |
| `VirtualCellBus.cs` | 靜態 C# 事件廣播中樞：VirtualCellTouch / VirtualCellRelease |

---

## 資料定義（ScriptableObject / 靜態常數，不動）

| 腳本 | 職責 |
|------|------|
| `MultiSensor/SensorHardwareConst.cs` | 硬體物理常數：CellPixelWidth=16、CellPixelHeight=32、PollingIntervalMs=100 |
| `MultiSensor/SensorFaceConfig.cs` | 單一感應面配置＋座標轉換：ContainsRaw / RawToPixel / PixelToCell / CellToPixel |
| `MultiSensor/VenuePresetConfig.cs` | 場域預設：場域名稱 + DisplayCanvasConfig + SensorFaceConfig[] + VCell 模式 |
| `MultiSensor/DisplayCanvasConfig.cs` | 投影畫布解析度（canvasWidth × canvasHeight）|
| `SceneInitialSettingsProfile.cs` | 場景初始設定 Profile（UDP port、格子解析度、點擊模式、各功能開關） |

---

## 感應輸入層

| 腳本 | 職責 |
|------|------|
| `UdpSensorReceiver.cs` | UDP 背景執行緒接收 + 協議解析（checksum）+ 主執行緒佇列派送，觸發 OnPacketReceived(rawX, rawY) |

---

## 主控管理（SensorTestManager partial 系列）

> 這七個檔案共同組成一個 partial class `SensorTestManager`，掛在 sensor 場景的 SensorManager GameObject。

| 腳本 | 職責 |
|------|------|
| `SensorTest/SensorTestManager.cs` | 主調度：初始化、場域切換、ActivateCell、鎖定管理、Display 2 Window 控制 |
| `SensorTest/SensorTestManager_UIBuilder.cs` | Display 1 UI 骨架建立：標題列、按鈕列、感應冷卻列、疊加透明列 |
| `SensorTest/SensorTestManager_GridBuilder.cs` | 格子建立與縮放：Texture2D 格子、D2 顯示、控制格 Pointer 事件注入 |
| `SensorTest/SensorTestManager_SmartTouch.cs` | 智慧觸碰辨識：Tap / PressStart / PressEnd / Drag 狀態機，含 VCell 路由 |
| `SensorTest/SensorTestManager_GhostTouch.cs` | 孤點過濾：pending 狀態機，需鄰居確認才放行 |
| `SensorTest/SensorTestManager_Logger.cs` | 訊號記錄：訂閱所有 Bus 事件，寫入 .csv 日誌檔案 |
| `SensorTest/SensorTestManager_ClickLog.cs` | 互動記錄面板：Display 1 上方即時顯示事件記錄（HPS 統計） |

---

## 視覺呈現層

| 腳本 | 職責 |
|------|------|
| `SensorTest/TileGridTexture.cs` | 所有格子的 Texture2D 視覺管理：SetActive / SetLocked / SetPressing / SetPending / LateUpdate 批量 Apply |
| `ConsoleGameOverlay.cs` | Display 1 控制格後方疊加：GameDisplayRT（遊戲）或 TileGridTexture.SensorTex（感應）預覽 |
| `Display2DimOverlay.cs` | Display 2 上方可調暗化覆蓋層（0%～100%） |
| `VirtualCellManager.cs` | VCell 管理：格子→VCell 映射、鎖定狀態、觸碰追蹤、覆蓋 Texture 繪製 |

---

## 點擊模擬層

| 腳本 | 職責 |
|------|------|
| `SensorTest/SensorClickSimulator.cs` | 感應觸碰 → OS 點擊事件轉換：Listen / PostMessage / SendInput 三種模式 |
| `SensorTest/Display1GraphicRaycaster.cs` | Display 1 Raycaster 覆寫：WH_MOUSE_LL hook 偵測感應點擊，壓制 D1 的感應誤觸 |
| `SensorTest/ControlGridPointerHandler.cs` | Display 1 控制格滑鼠事件（Down / Up / Hover / Hold）→ callback 轉換 |

---

## 遊戲整合層

| 腳本 | 職責 |
|------|------|
| `GameSceneSetup.cs` | 遊戲場景相機設定 + Mirror Camera RT（512×192，frame skip=2） |
| `GameDisplayRT.cs` | 靜態遊戲畫面 RenderTexture 管理（GetOrCreate / Release） |
| `SensorBridgeToPassPosition.cs` | SensorBus 觸碰事件 → 水波效果座標轉換與平滑（訂閱 OnTouch + OnCanvasTouch） |
| `GameConsole.cs` | 獨立執行檔彈出 Windows Console 視窗，轉導 Unity Debug.Log |
| `MiniWindowController.cs` | Display 1 主視窗縮小成右下角無邊框迷你面板，支援拖移 |

---

## 除錯工具（可 Disable 降低效能）

| 腳本 | 職責 |
|------|------|
| `TouchDebugLogger.cs` | IMGUI 觸碰日誌視窗（F9 開關）：訂閱 UDP 原始座標或 SensorBus 格子事件 |
| `UdpDebugOverlay.cs` | 右上角 UDP 接收狀態覆蓋：封包數、最後座標、接收時間 |
| `SensorTest/CoordTestPanel.cs` | Display 0 右上角座標測試面板：rawX/rawY 輸入 → 顯示命中面與格子 |
| `Display2ScreenCapture.cs` | GDI BitBlt 截取 Display 2 整個螢幕（30fps），用於 ConsoleGameOverlay 備援 |

---

## Editor 工具（不進 Build）

| 腳本 | 職責 |
|------|------|
| `Editor/FaceVisualizer.cs` | Scene 視窗 Gizmo：自動掃描 SensorFaceConfig，繪製格線 + 白框 + 標籤 |
| `Editor/NangangSetupWindow.cs` | NangangProjection → 場景初始設定視窗：一鍵套用 Profile、驗證 Build Settings |

---

## 待確認舊系統（確認後決定保留或刪除）

> 這三個腳本是比 SensorTestManager 更早的格子系統。
> 請在 sensor_2048x768 場景 Inspector 確認是否仍有掛載 TileManager。

| 腳本 | 功能說明 |
|------|---------|
| `TileManager.cs` | 舊版格子管理：32×12 小格 + 大格覆蓋，訂閱 UdpSensorReceiver.OnPacketReceived，有自己的 round timer。**若場景有掛，感應訊號會同時流向 TileManager 和 SensorTestManager。** |
| `SensorTile.cs` | TileManager 用的單格 UI 元件（TMP 標籤顯示 ID、時間、點擊數），實作 IPointerEnterHandler |
| `LargeTileOverlay.cs` | TileManager 用的多格合一邊框覆蓋（純邊框，無內容） |

---

## 查閱索引

| 我想了解… | 讀哪個檔案 |
|-----------|----------|
| UDP 到視覺的完整流程 | `.claude/rules/signal_flow.md` |
| 繞過條件（鎖定、skipVisual、冷卻） | `.claude/rules/signal_flow.md` + `.claude/rules/activatecell_skipvisual.md` |
| 冷卻 debounce 的 gate 位置 | `.claude/rules/sensor_debounce_coverage.md` |
| VCell 事件路由規則 | `.claude/rules/vcell_smarttouch_routing.md` |
| VCell 禁止建立 per-cell GO | `.claude/rules/vcell_overlay_no_per_cell_go.md` |
| D2 視窗透明度設定 | `.claude/rules/d2_window_transparency.md` |
| Display 1 Suppression | `.claude/rules/display1_input_suppression.md` |
| Mirror Camera 效能 | `.claude/rules/mirror_camera_performance.md` |
| TileGridTexture 貼圖尺寸 | `.claude/rules/tile_grid_texture_resolution.md` |
| 感應面 rawMin/Max 校準公式 | `.claude/rules/sensor_face_calibration.md` |
| 熱路徑 Debug.Log 禁用 | `.claude/rules/unity_hot_path.md` |
