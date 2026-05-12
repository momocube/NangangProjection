# 感應面校準公式

## rawMin/Max 計算公式（格子中心像素）

```
rawX = cellTopLeftX + halfCellW + 1920
rawY = cellTopLeftY + halfCellH
```

- 直（orientation=1）：halfCellW = 16，halfCellH = 8（cellW=32，cellH=16）
- 橫（orientation=0）：halfCellW =  8，halfCellH = 16

對每個面：
```
rawMinX = originX + halfCellW + 1920
rawMaxX = originX + (colCount-1)*cellW + halfCellW + 1920
rawMinY = originY + halfCellH
rawMaxY = originY + (rowCount-1)*cellH + halfCellH
```

## 光域 704 面對照（2026-05 已校正）

| 新檔名 | originX,Y | colCount × rowCount | rawMinX | rawMaxX | rawMinY | rawMaxY |
|---|---|---|---|---|---|---|
| Face_Wall Top | 640,0 | 44×40 | 2576 | 3952 | 8 | 632 |
| Face_Wall Left | 0,640 | 20×128 | 1936 | 2544 | 648 | 2680 |
| Face_Floor | 640,640 | 44×128 | 2576 | 3952 | 648 | 2680 |
| Face_Wall Right Big | 2048,640 | 20×80 | 3984 | 4592 | 648 | 1912 |
| Face_Entrance | 2048,1920 | 20×32 | 3984 | 4592 | 1928 | 2424 |
| Face_Wall Right little | 2048,2432 | 20×16 | 3984 | 4592 | 2440 | 2680 |
| Face_Wall Button | 640,2688 | 44×40 | 2576 | 3952 | 2696 | 3320 |

## flip 設定原則

- flip 描述感應器物理方向是否與畫布方向相反
- 用公式計算後，先設 flipX=0 flipY=0，現場四角測試後若方向反轉再切換
- 2026-05 校準時所有面均以 flipX=0 flipY=0 通過驗證

## 常見錯誤

- rawMaxY 使用格子右下邊緣像素（而非中心）→ 偏高約 halfCellH
- 用物理測試值而非公式值 → 精度受感應器噪訊影響
- 編輯舊 asset 路徑（面已在 Unity 中重命名）→ 編輯無效
