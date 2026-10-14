# NangangProjection — Claude 工作說明

## 專案概述
Unity 互動地板投影裝置。硬體感應模組透過 UDP 傳送觸碰座標，Unity 接收後在投影畫面上產生視覺反應。

## 重要路徑
- **主專案**：`C:\CyberCube\Unity\NangangProjection\NangangProjection\NangangProjection\`
- **腳本**：`Assets/_Script/`（子目錄：`Core/` `Input/` `Sensor/` `Visual/` `Simulator/` `Debug/` `Legacy/` `Editor/`）
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

## 場景說明
| 場景 | 說明 |
|---|---|
| `sensor_2048x768` | ⭐ 舞台場景（雙螢幕，SensorTestManager 架構） |
| `sensor_2688x3840` | 光域 704 場景 |

## 腳本職責 / 訊號流程
- **整體架構總覽（新人必讀）** → `.claude/rules/architecture_overview.md`
- VCell CanvasTouch 去重規則 → `.claude/rules/vcell_canvas_touch_dedup.md`
- 每個腳本一句話職責 → `.claude/rules/script_responsibilities.md`
- UDP 到視覺的完整流程 + 繞過條件 → `.claude/rules/signal_flow.md`
- 規則索引（何時讀哪個檔案）→ `.claude/rules/script_responsibilities.md` 末尾

## 光域 704：七個感應面

七個感應面，總外框 2688 × 3840 px：

| 面 | asset 檔名 | faceName | origin | 格數（欄×列） |
|---|---|---|---|---|
| 上（紫） | Face_Right.asset | 右側 | (640, 0) | 44×40 |
| 左（紅） | Face_Forward.asset | 前方 | (0, 640) | 20×128 |
| 中（黃綠，主） | Face_Center.asset | 中央 | (640, 640) | 44×128 |
| 右（藍大） | Face_Back_Right_Big.asset | 右大 | (2048, 640) | 20×80 |
| 右（藍中） | Face_Back_Right_Center.asset | 右中 | (2048, 1920) | 20×32 |
| 右（藍小） | Face_Back_Right_Small.asset | 右小 | (2048, 2432) | 20×16 |
| 下（綠） | Face_Left.asset | 左側 | (640, 2688) | 44×40 |

- 校準公式 → `.claude/rules/sensor_face_calibration.md`
- 待完成：光域 704 各面現場四角測試後更新各 Face_*.asset 的 rawMin/Max

## 舞台座標校準（2048×768，2026-04-08）
```
rawMinX = 1928  rawMaxX = 3960
rawMinY = 16    rawMaxY = 752
```
> 校準值已寫入 `Assets/_Script/Core/Setting/2048x768/Face_ALL.asset`

## SensorClickSimulator 三種模式
| 模式 | 按鈕顏色 | 說明 |
|---|---|---|
| 內部監聽 | 灰 | 不發 OS 事件，只走 SensorBus |
| PostMessage | 綠 | 直送 WM_LBUTTONDOWN/UP，游標不動（瀏覽器 / 一般程式） |
| 滑鼠點擊 | 藍 | SendInput 模擬硬體點擊，點擊後游標立即還原原位 |

## Display 2 視窗行為（獨立執行檔）
- 晶片示意 **ON**：Unity 視窗顯示在 Display 2
- 晶片示意 **OFF**：`ShowWindow(SW_HIDE)` 隱藏，後方程式（瀏覽器等）露出
- 點擊事件在視窗隱藏時仍觸發
- `display2WindowOffsetX`（預設 1920）：定位 Display 2 的 UnityWndClass HWND

## 常見陷阱
1. **Canvas LocalScale 歸零**：Play Mode 結束後 LocalScale 變 (0,0,0)。每次 Play 前 `File → Open Scene` 強制重載。
2. **場景 YAML GUIDs**：CanvasScaler `0cd44c1031e13a943bb63640046fad76`、GraphicRaycaster `dc42784cf147c0c48a680349fa168899`、GridLayoutGroup `8a8695521f0d02e499659fee002a26c2`、EventSystem `76c392e42b5098c458856cdf6ecaaaa1`
3. **Display 2 不顯示**：需 `Display.displays[1].Activate()` 且 Windows 設延伸顯示（非複製）
4. **TMP 已啟用**：UI 全面用 `TextMeshProUGUI`（textmeshpro 3.0.7）；InputField 例外保留 UnityEngine.UI.Text
5. **inactive hierarchy 下 AddComponent 的 Awake 延遲**：物件 SetActive 前 Awake 不執行，GetComponent 取不到
6. **遊戲場景需加入 Build Settings**：`gameSceneName` 的場景必須在 Build Settings 中

## Git 遠端
`https://github.com/bottledsnow/NangangProjection.git`（branch: main）

## 工作分工建議
- **新功能 / 新 session** → 開新對話，說「請先讀 CLAUDE.md」
- **Bug 修正** → 直接描述問題 + 貼錯誤訊息
- **場景 YAML 修改** → 說明要改哪個 fileID 或物件名稱
- **推 Git** → 只有使用者明確要求時才 commit／push；已取得明確授權時直接執行，不重複詢問。共用規則見 ../../AGENTS.md。
- **光域 704 各面校準** → 現場四角測試後更新各 `Face_*.asset` 的 rawMin/Max
- **不確定讀哪個腳本** → 先看 `.claude/rules/script_responsibilities.md`
