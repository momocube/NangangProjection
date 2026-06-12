# AutoLED 畫布資料流分析（免編譯部署可行性）

> 2026-06-12 read-only 分析。結論：**免編譯部署可行，新架構已完成 80%**，
> 缺口 = AutoLED 畫布檔 → venue JSON 轉換器（blocked on 格式樣本，見文末問題清單）。

## 1. Setting 資料夾盤點

路徑：`NangangProjection/Assets/_Script/Core/Setting/`。全部 14 個 `.asset` 都是
Unity YAML ScriptableObject，**無一是 AutoLED 直接輸出**（高信心：rawMin/Max 完全符合
工程師公式 `rawMinX = originX + halfCellW + 1920`（`.claude/rules/sensor_face_calibration.md`），
七面值逐一驗算吻合；repo 找不到任何 AutoLED 匯出檔或 importer）。

| 檔案 | 類型 | 內容 |
|---|---|---|
| `2688x3840/Preset_2688x3840.asset` | VenuePresetConfig | 704 場域預設：canvasConfig + faces×7 + rotateDisplay90 + virtualCellMode |
| `2688x3840/DisplayCanvasConfig.asset` | DisplayCanvasConfig | 2688×3840 |
| `2688x3840/Face_*.asset` ×7 | SensorFaceConfig | faceName / origin / orientation / cols,rows / rawMin,Max / flip |
| `2048x768/*`（3 檔） | 同上 | 舞台場域（單面 128×24、用整體校正 canvasRaw*） |
| `SceneInitialProfiles/*.asset` ×2 | SceneInitialSettingsProfile | 場景啟動參數（port、tap/timeout、debug 開關）——**只被 Editor 工具蓋章用**（NangangSetupWindow.cs:588），不進 runtime |

引用鏈：`sensor_2688x3840.unity:401` → Preset →（canvasConfig + faces[0..6]，順序即
face index，與 `venues/704.json` 完全一致）。數值閉環：Unity asset = `.claude/rules` 校正表 =
CALIBRATION.md 對照表 = venues/704.json，四處逐欄一致。

## 2. AutoLED 的角色（grep 全 repo）

只出現 5 處、全部關於**感應參數**（閥值/連續觸發次數→影響重送週期 80~240ms，
SENSOR_FIELD_REPORT.md:60-126），**零處關於畫布檔案**。間接證據（推測級）：raw 座標的
+1920 偏移「以部署機延伸桌面座標為基準」→ 模組端存在「晶片→座標」的校準程序，
極可能就是 AutoLED 的「畫布」＝晶片→raw 座標對應表（燒進控制器）。**需同仁確認**。

## 3. 為什麼 Unity 端要重建（已證實）

- Preset/Face/Canvas 是 scene 序列化引用（`SensorTestManager.cs:28 presets[]` Inspector 指定），烘進 build
- Unity runtime 零 `Resources.Load` / StreamingAssets / JSON 讀檔
- → 任何畫布變更 = 開 Unity 改 asset → rebuild → 重新部署（同仁回報屬實、架構性）

## 4. 新架構的免編譯現況

| 環節 | 狀態 |
|---|---|
| TouchService | ✅ venue JSON runtime 載入（VenueProfile.cs:33），丟檔即生效 |
| WallProjector | ✅ GET /venue + projections.json，純 client |
| 美術程式 | ✅ 遷移後收 WS 事件，與畫布解耦 |
| **Unity 投影端** | ❌ 唯一殘留；DEPLOY.md:104 註明長期規劃「Unity 改訂 TouchService 輸出」 |

## 5. 設計選項（按工程量）

**a. AutoLED 檔 → venue JSON 轉換器（推薦；blocked on 格式樣本）**
離線 CLI，解析 → faces 拓樸 → 套既有公式 → 吐 JSON；14,272 顆全晶片對拍測試直接當驗收。
風險：AutoLED 檔可能不含「各面在展開畫布上的 origin 擺位」（那是設計決策）——不含的話
轉換器產 faces 骨架、origin 由人填或匯入 UI 拖拉。穩定後可包成 TouchService 匯入 API。

**b. Unity 改 runtime 載入**（僅 Unity 投影短期無法退場時的過渡）：中工程量，
要排雷 scene 內對畫布尺寸的編輯期假設（TileGridTexture 不變量等）。

**c. Unity 退出部署迴路（戰略方向，repo 已在走）**：三件套（TouchService + WallProjector +
美術程式）全 runtime 資料驅動；美術接手視覺後，客戶體驗 = 「畫布檔 → 轉 JSON → 改 config → 重啟」。

**結論：c 為方向、a 補缺口、校正精靈做最後一哩；b 只做過渡。**

## 6. 與校正精靈的分工

| 欄位 | 來源 | 理由 |
|---|---|---|
| faces 拓樸（name/cols/rows/orientation/origin） | AutoLED 匯入 | 結構性事實，踩不出來 |
| rawMin/Max、flipX/Y | 踩角精靈 | 直接量測，吃掉 +1920 類場域偏移未知數 |
| 驗證 | 精靈走格驗證 | 匯入理論值 vs 踩角實測值偏差過大 = 接線/方向錯誤即時告警 |

## 7. 待同仁確認的問題清單

1. **AutoLED 匯出的 704 畫布檔原檔一份**（專案檔/匯出檔都要）——選項 a 唯一 blocker
2. 「畫布套用進 Unity」的具體操作步驟（手動填 Inspector？有無 importer？）
3. 畫布檔是否包含「各面在合併畫布上的 origin 擺位」？
4. **重生畫布會不會改變模組發出的 raw 座標**？（會 → 轉換器從方便升級為必要）
5. Unity 投影端是否仍為 704 營運必要？退場時程？
6. （既有問題）AutoLED 閥值/連續觸發次數對照表、+1920 偏移是設定項還是裝機手調
