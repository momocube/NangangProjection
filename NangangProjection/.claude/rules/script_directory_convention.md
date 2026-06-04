# 腳本目錄規範（Assets/_Script/ 子目錄）

## 子目錄一覽

| 目錄 | 放置規則 |
|---|---|
| `Core/` | 靜態 C# 事件 Bus、ScriptableObject 定義（SensorFaceConfig 等）、硬體常數、SceneInitialSettingsProfile。Setting/ 資產資料夾也在此。 |
| `Input/` | UDP / 硬體通訊接收層：只負責「從外部取得原始資料」。 |
| `Sensor/` | SensorTestManager partial 系列 + 任何訂閱 SensorBus 並橋接到遊戲邏輯的腳本。 |
| `Visual/` | 視覺輸出：TileGridTexture、Overlay 系列、RenderTexture 管理、Mirror Camera、視窗控制。 |
| `Simulator/` | OS 點擊模擬、Display 1 Raycaster 覆寫、控制格指標事件轉換。 |
| `Debug/` | 開發除錯工具（IMGUI 視窗、Debug.Log 轉導、座標測試）。正式演出不需要時整個 GameObject SetActive(false)。 |
| `Legacy/` | 舊 TileManager 系統，待現場確認場景是否掛載後決定保留或刪除。 |
| `Editor/` | Unity 保留名稱，只放 Editor 工具（不進 Build），**不更名**。 |

## 新增腳本時的對應規則

1. 新增純事件定義（Action / UnityEvent 型靜態類別）→ `Core/`
2. 新增感應資料接收（UDP / Serial / Bluetooth）→ `Input/`
3. 新增感應狀態機或橋接器（訂閱 SensorBus 後做邏輯）→ `Sensor/`
4. 新增顯示相關（RawImage / RenderTexture / Window 操作）→ `Visual/`
5. 新增 OS 事件模擬 / Raycaster 覆寫 → `Simulator/`
6. 新增只在開發期需要的工具（F 鍵開關、Log 視窗）→ `Debug/`
7. 確定是廢棄舊系統 → `Legacy/`（等待刪除，不繼續開發）

## 禁止行為

- **不在 `Assets/_Script/` 根目錄直接放新腳本**（除非要立即分類到子目錄）
- **不建立 `SensorTest/` 或 `MultiSensor/` 目錄**（已廢棄，內容已分類）
- **不更名 `Editor/`**（Unity 編輯器識別靠此名稱）
