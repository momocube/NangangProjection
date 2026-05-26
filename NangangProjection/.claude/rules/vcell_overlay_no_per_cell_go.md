# VirtualCellManager Overlay 規則：禁止 per-VCell Image GameObject

## 禁止在 BuildControlLockOverlays / BuildSensorVCellOverlay 內用迴圈建立 Image GO

每個 VCell 建立一個 Image GameObject 在 2688×3840 場域會產生 7136×2 = 14,272 個 GO，
全部在 `Start()` coroutine 的同步迴圈內建立，導致主執行緒凍結 5~30 秒。
`OnUdpPacket` 訂閱在凍結期間永遠不執行，感應互動因此永久失效。

## 正確做法：單一 RawImage ＋ 小 Texture2D

```
_vcellTexW = canvasW / _cellSizePx   // 2688/32 = 84 px
_vcellTexH = canvasH / _cellSizePx   // 3840/32 = 120 px
```

- 每個顯示器（D1 control, D2 sensor）建立 **1 個 RawImage GO**
- 共用同一個 `_vcellOverlayTex`（84×120，TextureFormat.RGBA32，FilterMode.Point）
- 狀態變更時只 paint 1~N 個像素到 `_vcellPixelBuf` 再 `Apply()`

## 為什麼不受 tile_grid_texture_resolution 規則約束

`_vcellOverlayTex` 與 `TileGridTexture` 不同，不是每幀都可能 dirty；
只在 VCell 鎖定/觸碰狀態改變時才 Apply（事件驅動，非每幀）。
且 84×120 × 4 bytes = ~40 KB，成本極低。

## 例外：感應格 VCell 格線貼圖（`_sensorLineTex`）

`_sensorLineTex` 是全畫布尺寸（2688×3840 = 40 MB），但只在場域重建時 Apply 一次，
屬於 `tile_grid_texture_resolution` 規則中明定的例外，允許保留。

## 新增 VCell 視覺效果時的開發指引

1. 不要用 `for vcrow ... for vcol ... new GameObject(...)` 迴圈
2. 在 `VCellOverlayColor()` 中加新的顏色條件即可
3. `RefreshAllLockOverlays()` 或 `RefreshLockOverlaysForVCell()` 會自動反映到貼圖
