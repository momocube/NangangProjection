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
| Display 2 | 玩家畫面（投影地板） | 2048×768 (8:3) | 1 |

## 感應硬體規格
- 全地板解析度：2048×768 px
- 感應晶片格子：128 欄 × 24 列 = 3072 個
- 每格像素：16×32 px（橫×縱）
- UDP Port：20001
- 協議：55 55 [frame] [dataSize H+L] [X_H X_L Y_H Y_L]×N [checksum H+L]

## 座標校準（2026-04-08 四角測試）
```
rawMinX = 1928  rawMaxX = 3960
rawMinY = 16    rawMaxY = 752
```
換算公式：
- 欄 = (rawX - 1928) / 2032 × 128
- 列 = (rawY - 16)   / 736  × 24

## 場景說明
| 場景 | 說明 |
|---|---|
| `stage_test` | 舊測試場景（TileManager 架構） |
| `sensor_test` | ⭐ 主要開發場景（雙螢幕，SensorTestManager 架構） |

## 主要腳本
| 腳本 | 位置 | 說明 |
|---|---|---|
| `UdpSensorReceiver` | `_Script/` | UDP 接收，解析協議，觸發 `OnPacketReceived` 事件 |
| `GameConsole` | `_Script/` | 獨立執行檔彈出 Windows 命令列視窗 |
| `SensorTestManager` | `_Script/SensorTest/` | 建立雙螢幕格子、UDP 座標映射、鎖定邏輯 |
| `SensorTileCell` | `_Script/SensorTest/` | Display 2 感應格（觸摸→亮紅 1.5s） |
| `ControlTileCell` | `_Script/SensorTest/` | Display 1 控制格（右鍵鎖定→黃色） |

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

## Git 遠端
`https://github.com/bottledsnow/NangangProjection.git`（branch: main）

## 工作分工建議（降低 Token）
- **新功能** → 開新對話，說「我要做 XXX，請先讀 CLAUDE.md」
- **Bug 修正** → 直接描述問題 + 貼錯誤訊息
- **場景 YAML 修改** → 說明要改哪個 fileID 或哪個物件
- **推 Git** → 每個功能完成後立即 commit + push，避免跨對話追蹤未提交的內容
