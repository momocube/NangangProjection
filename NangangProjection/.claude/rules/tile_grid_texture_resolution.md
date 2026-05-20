# TileGridTexture 貼圖解析度規則

## 禁止以全畫布像素尺寸建立 TileGridTexture

TileGridTexture 的內部貼圖**必須**以 `canvasW/CellPixelWidth × canvasH/CellPixelWidth`（= canvasW/16 × canvasH/16）為尺寸，
**不得**恢復成 `canvasW × canvasH`。

| 場域 | 錯誤尺寸 | 正確尺寸 | 每幀 Apply 成本 |
|---|---|---|---|
| 舞台 | 2048×768 = 6 MB | 128×48 = 24 KB | 250× 縮小 |
| 704 | 2688×3840 = 40 MB | 168×240 = 157 KB | 250× 縮小 |

**為什麼：** LateUpdate 每幀最多兩次 `Texture2D.Apply()`；全畫布尺寸在 704 場域造成 40 MB/幀 CPU→GPU 上傳，
是觸碰明顯延遲的根本原因。RawImage + FilterMode.Point 放大後視覺完全相同。

## 正確的 PaintCell 座標計算

```csharp
const int u = SensorHardwareConst.CellPixelWidth;   // 16
int cellW = cfg.GizmoCellW / u;   // 橫=1, 直=2
int cellH = cfg.GizmoCellH / u;   // 橫=2, 直=1
int texX  = cfg.originX / u + col * cellW;
int texY  = cfg.originY / u + row * cellH;
FillRect(buf, texX, texY, cellW, cellH, fillColor);
```

不需要畫邊框（1×2 或 2×1 像素沒有空間做邊框縮排）。

## 哪些貼圖不受此規則約束

- `ConsoleGameOverlay._gridLineTex`：全畫布尺寸，但只在場域重建時 Apply 一次
- `VirtualCellManager._sensorLineTex`：同上
- `Display2ScreenCapture.CaptureTexture`：螢幕截圖，只在截圖時 Apply 一次

只有**每幀都可能 dirty 的貼圖**（即 TileGridTexture）需要嚴格縮小。
