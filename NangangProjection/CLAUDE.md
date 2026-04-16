# NangangProjection — Claude 工作說明

## 專案概述
Unity 互動地板投影裝置。硬體感應模組透過 UDP 傳送觸碰座標，Unity 接收後在投影畫面上產生視覺反應。

## 重要路徑
- **主專案**：`C:\CyberCube\Unity\NangangProjection\NangangProjection\NangangProjection\`
- **腳本**：`Assets/_Script/`
- **場景**：`Assets/Scenes/`
- ⚠️ 若 Claude 在 worktree 環境，編輯檔案時務必使用**主專案路徑**，不要編輯 worktree 副本

## 螢幕配置
| 顯示器 | 用途 | 解析度 | Unity targetDisplay |
|---|---|---|---|
| Display 1 | 操作者控制台（控制格＋鎖定） | 1920×1080 (16:9) | 0 |
| Display 2 | 玩家畫面（投影地板） | 由 VenuePresetConfig 決定 | 1 |

## 感應硬體規格
- 每格像素：16×32 px（橫×縱，`SensorHardwareConst`）
- UDP Port：20001
- 協議：55 55 [frame] [dataSize H+L] [X_H X_L Y_H Y_L]×N [checksum H+L]

## 座標校準（舞台 2048×768，2026-04-08 四角測試）
```
rawMinX = 1928  rawMaxX = 3960
rawMinY = 16    rawMaxY = 752
```
> 校準值已寫入 `Assets/_Script/MultiSensor/Setting/2048x768/Face_ALL.asset`

## 場景說明
| 場景 | 說明 |
|---|---|
| `sensor_base` | ⭐ 主要開發場景（雙螢幕，SensorTestManager 架構） |

## 主要腳本
| 腳本 | 位置 | 說明 |
|---|---|---|
| `UdpSensorReceiver` | `_Script/` | UDP 接收，解析協議，主執行緒佇列派送，觸發 `OnPacketReceived` |
| `GameConsole` | `_Script/` | 獨立執行檔彈出 Windows 命令列視窗 |
| `SensorBus` | `_Script/` | 靜態事件中轉站，`OnTouch(col,row)`，已通過鎖定檢查才廣播 |
| `SensorTestManager` | `_Script/SensorTest/` | 多場域主控：讀 VenuePresetConfig[]，建格子、UDP 映射、鎖定、ShowWindow |
| `SensorTileCell` | `_Script/SensorTest/` | Display 2 感應格（觸摸→亮紅 1.5s，鎖定→黃色） |
| `ControlTileCell` | `_Script/SensorTest/` | Display 1 控制格（idle 透明，右鍵鎖定→黃色，觸摸→紅色） |
| `SensorClickSimulator` | `_Script/SensorTest/` | 訂閱 SensorBus，三模式循環：內部監聽 / PostMessage / SendInput |
| `TouchDebugLogger` | `_Script/` | 訂閱 SensorBus，Console 印出觸碰紀錄（含 Collapse 與每秒統計） |
| `ConsoleGameOverlay` | `_Script/` | Display 1 格子範圍內顯示 Display 2 畫面（RT 疊加，透明度可調） |
| `GameDisplayRT` | `_Script/` | 遊戲畫面共用 RenderTexture，跨場景靜態存取 |
| `SensorGridRT` | `_Script/` | 感應格畫面 RenderTexture（尺寸由 VenuePresetConfig 決定），由 SensorTestManager 建立 |
| `GameSceneSetup` | `_Script/` | 遊戲場景相機設定，建立 Mirror Camera 寫入 GameDisplayRT |
| `VenuePresetConfig` | `_Script/MultiSensor/` | ScriptableObject：場域名稱 + DisplayCanvasConfig + SensorFaceConfig[] |

## Display 1 控制台版面

### 標題列（頂部 58px）
- 左側：「感應測試控制台」標題
- 右側：**[◀] 場域名稱 [▶]** — 切換 VenuePresetConfig（重建格子 + RT）

### 按鈕列（第一排）
| 按鈕 | 功能 |
|---|---|
| 離開遊戲 | `Application.Quit()` |
| 智慧鎖格 | 觸碰的格子自動鎖定 |
| 解除鎖格 | 解除全部鎖定 |
| 晶片示意 | 切換 Display 2 感應格顯示（同步 ShowWindow 隱藏/顯示 Unity 視窗） |
| 內部監聽 / PostMessage / 滑鼠點擊 | 循環切換 SensorClickSimulator 點擊模式 |

### 感應冷卻 ＋ 疊加透明度列（第二排，左右各半）
| 區域 | 元件 | 說明 |
|---|---|---|
| 左：感應冷卻 | 冷卻 ON／OFF | 切換 `SensorClickSimulator.cooldownEnabled`，對三種模式均有效 |
| 左：感應冷卻 | − / + | 調整 `clickCooldown`（0.1s ～ 5.0s，每步 0.1s） |
| 左：感應冷卻 | 數值顯示 | 目前冷卻秒數；冷卻關閉時變暗 |
| 右：疊加透明 | − / + | 調整 `ConsoleGameOverlay.overlayAlpha`（0% ～ 100%，每步 10%） |
| 右：疊加透明 | 數值顯示 | 目前不透明度百分比 |

- **冷卻 ON（綠）**：每幀只允許一個點擊，模擬真實單點滑鼠
- **冷卻 OFF（橘紅）**：所有同幀觸碰均觸發，原生多點觸控模式

## Display 1 格子透視疊加說明
控制格（ControlTileCell）的 idle 背景已改為全透明，在格子範圍內顯示 Display 2 的畫面：
- **遊戲已載入**：疊加顯示 `GameDisplayRT`（遊戲場景 Mirror Camera RT）
- **僅感應測試**：疊加顯示 `SensorGridRT`（SensorCam 捕捉 Canvas_Secondary）
- 觸發（紅）/ 鎖定（黃）格子仍完整顯示於疊加畫面之上
- `ConsoleGameOverlay.overlayAlpha`：Inspector 可設定初始透明度（預設 1.0），執行中可用第二排 −/+ 調整

### RT 捕捉機制
- `SensorTestManager.SetupSensorGridRT()`：Start 的 yield 後執行
  - 建立 `SensorCam`（Camera → `targetTexture = SensorGridRT`，UI layer only）
  - 切換 `Canvas_Secondary` 至 `ScreenSpaceCamera` 模式（`worldCamera = SensorCam`）
  - 在 Display 2 建立 `Canvas_Display2RTMirror`（ScreenSpaceOverlay RawImage），維持 Display 2 畫面
- `SetGameTileVisible(false)` 同時隱藏 Mirror Canvas，避免遊戲相機畫面被 ScreenSpaceOverlay 覆蓋

## SensorClickSimulator 三種模式
| 模式 | 按鈕顏色 | 說明 |
|---|---|---|
| 內部監聽 | 灰 | 不發 OS 事件，只走 SensorBus（自製 Unity 程式訂閱 SensorBus） |
| PostMessage | 綠 | 直送 WM_LBUTTONDOWN/UP 給目標視窗，**游標不動**（瀏覽器 / 一般程式） |
| 滑鼠點擊 | 藍 | SendInput 模擬硬體點擊，點擊後游標立即還原原位 |

> ⚠️ SensorClickSimulator 必須手動掛到 sensor_base 場景的 GameObject。
> ⚠️ SensorClickSimulator 訂閱 `SensorBus.OnTouch`，鎖定的格子自動不觸發點擊。
> ⚠️ `OnEnable` 使用 `-=` 再 `+=` 防止重複訂閱（場景重載時的常見 bug）。

## Display 2 視窗行為（獨立執行檔）
- 晶片示意 **ON**：Unity 視窗顯示在 Display 2
- 晶片示意 **OFF**：`ShowWindow(SW_HIDE)` 隱藏 Unity 視窗，後方程式（瀏覽器等）露出
- SensorClickSimulator 點擊事件在視窗隱藏時依然觸發
- `display2WindowOffsetX`（預設 1920）：用 `GetWindowRect` 定位 Display 2 的 UnityWndClass HWND

## 常見陷阱
1. **Canvas LocalScale 歸零**：Unity 在 Play Mode 結束後會把 Canvas LocalScale 寫成 (0,0,0)。每次 Play 前先 `File → Open Scene` 強制重載。
2. **場景 YAML 的 GUIDs**（常用）：
   - CanvasScaler：`0cd44c1031e13a943bb63640046fad76`
   - GraphicRaycaster：`dc42784cf147c0c48a680349fa168899`
   - GridLayoutGroup：`8a8695521f0d02e499659fee002a26c2`
   - EventSystem：`76c392e42b5098c458856cdf6ecaaaa1`
   - StandaloneInputModule：`4f231c4fb786f3946a6b90b886c48677`
3. **Display 2 不顯示**：需要 `Display.displays[1].Activate()` 且 Windows 設為延伸顯示（非複製）
4. **TMP 未安裝**：此專案不使用 TextMeshPro，用 `UnityEngine.UI.Text` 代替
5. **SensorTileCell inactive 時不能 StartCoroutine**：晶片示意關閉後 Canvas_Secondary 變 inactive，`Activate()` 有 `activeInHierarchy` 守衛；`ActivateCell` 也會跳過 inactive 的感應格
6. **遊戲場景需加入 Build Settings**：`gameSceneName` 填寫的場景必須在 `File → Build Settings` 中，否則會跳過並印 Warning

## 新案場：光域 704（多面場域擴充）

參考圖：`C:\CyberCube\二代設備\光域 704 作圖-標線-實際像素大小.pdf`

六個感應面，總外框 2688 × 3840 px：

| 面 | 約略尺寸 |
|---|---|
| 上（紫） | 1408 × 640 |
| 左（紅） | 640 × 2048 |
| 中（黃綠，主） | 1408 × 2048 |
| 右（藍） | 640 × 1280 |
| 下（綠） | ~1280 × ~1019 |
| 不確定面（藍框） | ~512，方向待確認 |

### Phase 1：Editor 工具（已完成）

| 腳本 | 位置 | 說明 |
|---|---|---|
| `SensorHardwareConst` | `_Script/MultiSensor/` | 硬體物理常數：`CellPixelWidth=16`、`CellPixelHeight=32` |
| `SensorFaceConfig` | `_Script/MultiSensor/` | ScriptableObject，每面一個 |
| `DisplayCanvasConfig` | `_Script/MultiSensor/` | ScriptableObject，設定畫布解析度（預設 2048×768），FaceVisualizer 讀取 |
| `FaceVisualizer` | `_Script/Editor/` | Editor-only，`[InitializeOnLoad]`，自動掃描所有 SensorFaceConfig，Scene 中畫格線與畫布白框 |

#### SensorFaceConfig 欄位說明

| 欄位 | 說明 |
|---|---|
| `faceName` | 面名稱，顯示在 Scene Gizmo 標籤 |
| `gizmoColor` | 格線顏色 |
| `showInVisualizer` | 是否在 Scene 顯示 |
| `originX / originY` | 此面左上角在合併畫布的像素位置 |
| `orientation` | `橫`：rawX→畫面X、rawY→畫面Y；`直`：rawX→畫面Y、rawY→畫面X |
| `colCount / rowCount` | **永遠代表畫面 X/Y 格數**，與安裝方向無關 |
| `rawMinX/MaxX/Y` | 感應原始值範圍（四角測試校準） |
| `flipX / flipY` | 感應方向裝反時勾選 |

> ⚠️ `colCount/rowCount` 是**畫面格數**，不是晶片排列數。切換 orientation 只改 rawXY 對應軸，格子數量不變。

#### 原始舞台（2048×768）SensorFaceConfig 參考值
```
originX=0, originY=0, orientation=橫
colCount=128, rowCount=24
rawMinX=1928, rawMaxX=3960, rawMinY=16, rawMaxY=752
```

#### FaceVisualizer 使用
- 不需掛 GameObject，編譯後自動啟動
- Scene 視窗需開啟 **Gizmos** 開關
- 選單 **NangangProjection → 顯示晶片格 Gizmo** 切換（有打勾 = 開啟）
- 白色粗框 = 畫布邊界（讀 DisplayCanvasConfig，找不到則 2048×768）
- 各面標籤格式：`面名稱  [橫/直]\n寬×高 px  (欄×列 格)`

### Phase 2：Runtime 整合（已完成）

`SensorTestManager` 已全面重構為多場域架構：

#### 新增腳本 / 資料
| 項目 | 說明 |
|---|---|
| `VenuePresetConfig` | ScriptableObject：場域名稱 + `DisplayCanvasConfig` + `SensorFaceConfig[]` |
| `Setting/2048x768/Preset_2048x768.asset` | 舞台場域（128×24，校準值已填入） |
| `Setting/2688x3840/Preset_2688x3840.asset` | 光域 704（七個面，待各面校準） |

#### 架構變更重點
- 格子由 `GridLayoutGroup` 改為**絕對 RectTransform 定位**，每面依 `originX/Y` 對齊正確像素
- `SensorTestManager.presets[]` 在 Inspector 指定；場景 YAML 已內含兩個 preset 引用
- 標題列右側 **[◀][▶]** 切換場域，切換時自動重建格子 + RT
- `OnUdpPacket` 對每個面做 rawMin/Max 範圍過濾，避免跨面誤觸發
- `ControlTileCell.Init(c, r, stride)` 增加 `stride` 參數支援非 128 欄的面
- `CoordTestPanel` 改用 `SensorTestManager.GetCellForRaw()` API

#### 待完成
- 光域 704 各面的 rawMin/Max 校準（需現場四角測試）
- `SensorBus.OnTouch` 加 `faceId` 參數（破壞性改動，需同步改 ControlTileCell / SensorTileCell / SensorClickSimulator / TouchDebugLogger）

## Git 遠端
`https://github.com/bottledsnow/NangangProjection.git`（branch: main）

## 工作分工建議（降低 Token）
- **新功能** → 開新對話，說「我要做 XXX，請先讀 CLAUDE.md」
- **Bug 修正** → 直接描述問題 + 貼錯誤訊息
- **場景 YAML 修改** → 說明要改哪個 fileID 或哪個物件
- **推 Git** → 每個功能完成後立即 commit + push，避免跨對話追蹤未提交的內容
- **光域 704 各面校準** → 現場四角測試後更新各 `Face_*.asset` 的 rawMin/Max
- **SensorBus faceId** → 新對話說「加 SensorBus faceId 參數，請先讀 CLAUDE.md」
