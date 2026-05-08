# Unity 熱路徑規則

## 禁止在熱路徑使用 Debug.Log

熱路徑定義：每幀呼叫、每次觸碰呼叫、或 Coroutine 每次 yield 前後。

**違規範例（每次觸碰結束都呼叫）**：
```csharp
// MatchAndUpdate() 內 slot 逾時
Debug.Log($"[PPTM] slot{s} 逾時停用...");
```

**合規做法**：
- 開發期日誌加 `if (debugLog)` guard（Inspector 勾選才開）
- 非熱路徑（一次性初始化、使用者手動操作）允許無條件 Debug.Log

## 禁止在熱路徑每幀分配 managed heap

- 每幀 `new List<T>()` / `new StringBuilder()` 觸發 GC spike
- 改用 class-level 預分配欄位 + 每幀 `.Clear()` 重用

## SensorFaceConfig.gizmoColor 只供 Editor Gizmo 使用

`gizmoColor` 的值不應直接作為 Runtime UI 顏色。
`SensorTileCell.SetFaceColor` 使用 `Lerp(ColIdle, faceColor, 0.12f)` 混入，
係數控制在 0.12 以下，防止高飽和顏色壓過深色底盤。
新增 face asset 時：gizmoColor 設鮮豔色用於辨識，Runtime 顯示效果由係數保護。
