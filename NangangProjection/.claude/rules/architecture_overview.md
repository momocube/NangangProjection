# NangangProjection 程式架構總覽

## 系統定位

雙螢幕互動地板投影裝置。硬體感應模組（UDP）→ Unity 接收 → 視覺回饋 + OS 點擊模擬。
兩個獨立場景分別對應兩套硬體環境，共用同一套腳本框架。

---

## 分層架構圖

```
┌─────────────────────────────────────────────────────────┐
│  Layer 0：硬體 / 外部                                   │
│  感應地板 UDP 封包（20001 port）                         │
└────────────────────────┬────────────────────────────────┘
                         │ rawX / rawY（格子中心像素座標）
┌────────────────────────▼────────────────────────────────┐
│  Layer 1：輸入接收層  Input/                             │
│  UdpSensorReceiver   ← 背景執行緒接收 + checksum 驗證   │
│  SensorFaceRouter    ← 面分類 + 座標轉換 + 鎖定過濾      │
└────────────────────────┬────────────────────────────────┘
                         │ faceIdx / cellIdx（格子索引）
┌────────────────────────▼────────────────────────────────┐
│  Layer 2：感應邏輯層  Sensor/                            │
│  SensorTestManager         — 主控：場域切換、格子管理    │
│  ├─ _UIBuilder             — Display 1 UI 骨架           │
│  ├─ _GridBuilder           — 格子建立、RT 尺寸           │
│  ├─ _SmartTouch            — Tap/Press/Drag 狀態機       │
│  ├─ _GhostTouch            — 孤點過濾 pending 狀態機     │
│  ├─ _Logger                — CSV 日誌記錄                │
│  └─ _ClickLog              — Display 1 即時事件面板      │
│  VirtualCellManager        — 多格合一 VCell 模式（選用） │
└───────────┬──────────────────────┬──────────────────────┘
            │ SensorBus            │ VirtualCellBus
┌───────────▼──────────────────────▼──────────────────────┐
│  Layer 3：事件 Bus 層  Core/                             │
│  SensorBus       — Touch/Tap/PressStart/PressEnd/Drag    │
│                    + Canvas 系列（含全畫布像素座標）      │
│  VirtualCellBus  — VirtualCellTouch/Release              │
└───────┬───────────────────────┬──────────────────────────┘
        │                       │
┌───────▼──────────┐   ┌────────▼────────────────────────┐
│  Layer 4a        │   │  Layer 4b                        │
│  視覺輸出層      │   │  點擊模擬層  Simulator/          │
│  Visual/         │   │  SensorClickSimulator            │
│  TileGridTexture │   │  ├─ Listen 模式（只走 Bus）      │
│  VirtualCellMgr  │   │  ├─ PostMessage 模式             │
│  ConsoleOverlay  │   │  └─ SendInput 模式               │
│  Display2Dim     │   │  Display1GraphicRaycaster        │
│  MiniWindow      │   │  └─ D1 誤觸壓制（WH_MOUSE_LL）  │
│  GameDisplayRT   │   │  ControlGridPointerHandler       │
│  GameSceneSetup  │   │  └─ 控制格滑鼠事件轉換           │
└──────────────────┘   └──────────────────────────────────┘
        │
┌───────▼──────────────────────────────────────────────────┐
│  Layer 5：遊戲整合層                                     │
│  SensorBridgeToPassPosition — SensorBus → 水波座標平滑  │
│  GameSceneSetup             — Mirror Camera RT 管理      │
│  GameDisplayRT              — 靜態 RT 管理               │
└──────────────────────────────────────────────────────────┘
```

---

## 資料定義層（Core/，各層共享，不含邏輯）

| ScriptableObject | 說明 |
|---|---|
| `SensorHardwareConst` | CellPixelWidth=16、CellPixelHeight=32、PollingIntervalMs=100（靜態常數） |
| `SensorFaceConfig` | 單面：originX/Y、colCount/rowCount、flip、rawMin/Max + ContainsRaw/RawToPixel/PixelToCell |
| `DisplayCanvasConfig` | 投影畫布解析度（canvasWidth × canvasHeight） |
| `VenuePresetConfig` | 場域：DisplayCanvasConfig + SensorFaceConfig[] + VCellMode |
| `SceneInitialSettingsProfile` | 場景初始化：UDP port、點擊模式、各功能開關 |

---

## 執行緒模型

```
背景執行緒（ReceiveLoop）
  └─ UDP socket recv → checksum 驗證 → Queue.Enqueue(rawX, rawY)
       │  若佇列滿 → 丟棄最舊封包（保留最新觸碰）

主執行緒（每幀 Update）
  └─ UdpSensorReceiver.Update()
       └─ Queue.Dequeue → OnPacketReceived(rawX, rawY) event
            └─ SensorFaceRouter.OnPacket()
                 └─ SensorTestManager.DispatchSensorCell()
                      └─ SmartTouch 狀態機
                           └─ SensorBus.Tap/PressStart/... (主執行緒同步廣播)
```

**執行緒安全規則**：
- `LockedSnapshot`：volatile + immutable，背景執行緒讀取無需 lock
- 一切 Unity API（GameObject、Transform、UI）只在主執行緒呼叫
- `OnPacketReceived` event 在主執行緒觸發，訂閱者不需加鎖

---

## SensorBus 事件語意

| 事件 | 何時觸發 | 座標型態 |
|---|---|---|
| `OnTouch` | 每 100ms 有格子被踩 | col / row（面局部）|
| `OnTap` | 離腳且持續 < tapBoundaryMs | col / row |
| `OnPressStart` | 持續 >= tapBoundaryMs 首次 | col / row |
| `OnPressEnd` | 曾 PressStart 後離腳 | col / row |
| `OnDrag` | 滑動至鄰格 | col / row |
| `OnCanvasTouch` | 同 OnTouch，但給多面場域使用 | 全畫布像素 x/y |
| `OnCanvasTap` / `OnCanvasPressStart` / ... | 同上類比 | 全畫布像素 x/y |

**VCell 模式下**：Canvas 系列事件改以 VCell 中心座標廣播（不抑制事件，只換座標）。

---

## 兩種場域的差異點

| 差異項目 | sensor_2048x768（舞台） | sensor_2688x3840（光域 704） |
|---|---|---|
| 感應面數量 | 1 個（Face_ALL） | 7 個（上/左/中/右大/右中/右小/下） |
| Canvas 解析度 | 2048×768 | 2688×3840 |
| Raycaster | 內建 GraphicRaycaster | Display1GraphicRaycaster（含誤觸壓制） |
| VCell 預設 | None | Size32 / Size64 |
| 截圖備援 GO | inactive | active（ConsoleGameOverlay fallback） |
| TileGridTexture 貼圖 | 128×48 | 168×240 |

---

## 效能關鍵節點

| 位置 | 限制條件 | 原因 |
|---|---|---|
| `TileGridTexture.Apply()` | 貼圖尺寸 = canvas/16（不得還原全畫布） | 40 MB/幀 CPU→GPU 上傳會造成明顯延遲 |
| `LateUpdate` 迭代 | 只迭代 `_activeKeys`（不全格掃描） | 704 空格 7136 格掃描造成 GC spike |
| Mirror Camera RT | 512×192，frame skip=2 | 全畫布 Mirror Camera 讓場景每幀渲染兩次 |
| VCell Overlay | 單 RawImage + 84×120 小 Texture2D | per-VCell GO 凍結主執行緒 5~30 秒 |
| 熱路徑 Debug.Log | 禁止無條件呼叫 | 每觸碰一次 string 分配 × 100Hz = GC spike |

---

## 新功能開發對應位置

| 需求 | 新增/修改位置 |
|---|---|
| 新遊戲效果（訂閱觸碰） | 訂閱 `SensorBus.OnCanvasTap` 等 Canvas 系列事件 |
| 新感應面（新場域） | 新增 `SensorFaceConfig.asset`，加入 `VenuePresetConfig.faces[]` |
| 新場域預設 | 新增 `VenuePresetConfig.asset`，加入 `SensorTestManager.presets[]` |
| 新 VCell 視覺效果 | `VirtualCellManager.VCellOverlayColor()`，不得用迴圈建 GO |
| 新 OS 點擊模擬模式 | `SensorClickSimulator`（ClickMode enum + 對應 `Send*` 方法） |
| 新除錯工具 | `Debug/` 目錄，整個 `[除錯工具]` GO 平時 inactive |
| 新感應資料來源（非 UDP） | `Input/` 目錄，對外觸發 `OnPacketReceived(rawX, rawY)` event |

---

## 依賴方向（禁止反向依賴）

```
Core（資料定義）
  ↑
Input（接收層）
  ↑
Sensor（邏輯層）
  ↑
Bus（事件廣播）
  ↑
Visual / Simulator / Game（消費層，彼此平行，不相互依賴）
```

- **Visual** 不可直接呼叫 **Simulator** 的方法
- **Simulator** 不可直接呼叫 **Visual** 的方法
- 若需跨層通訊，一律透過 **SensorBus / VirtualCellBus**
- `SensorTestManager` 例外：作為協調者可持有 `VirtualCellManager` 參考（同 GO 元件）

---

## 查閱索引補充

| 我想了解… | 讀哪個檔案 |
|---|---|
| 整體架構（本文件） | `.claude/rules/architecture_overview.md` |
| UDP 到視覺的完整訊號流程 | `.claude/rules/signal_flow.md` |
| 每個腳本的一句話職責 | `.claude/rules/script_responsibilities.md` |
| 新腳本應放哪個子目錄 | `.claude/rules/script_directory_convention.md` |
| 感應面 rawMin/Max 校準 | `.claude/rules/sensor_face_calibration.md` |
