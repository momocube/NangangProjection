# 場域校正手冊（venue profile）

## venue JSON 結構

`venues/704.json`（光域 704，2026-05 已現場校正）：

```json
{
  "venue": "704",
  "canvas": { "width": 2688, "height": 3840 },
  "faces": [
    {
      "name": "Floor",
      "originX": 640, "originY": 640,
      "orientation": "vertical",
      "colCount": 44, "rowCount": 128,
      "rawMinX": 2576, "rawMaxX": 3952,
      "rawMinY": 648,  "rawMaxY": 2680,
      "flipX": false, "flipY": false
    }
  ]
}
```

| 欄位 | 說明 |
|---|---|
| `canvas` | 場域合併畫布尺寸（px），投影輸出解析度 |
| `faces[]` | 感應面清單。**順序即 face index**（線性分類先到先得），也是輸出 schema 的 `face` 值 |
| `name` | 面名稱（log / testpage 顯示用） |
| `originX/Y` | 此面左上角在合併畫布的位置（px） |
| `orientation` | 晶片安裝方向：`horizontal` 每格 16×32 px、`vertical` 每格 32×16 px（704 全部 vertical） |
| `colCount/rowCount` | 畫面 X / Y 方向格數（與安裝方向無關） |
| `rawMinX/Y rawMaxX/Y` | 感應原始值校準範圍 = **第一顆 / 最後一顆晶片的中心 raw 值** |
| `flipX/flipY` | 感應器物理方向與畫布方向相反時翻轉 |

來源對照（一次性轉換自 Unity assets，`Assets/_Script/Core/Setting/2688x3840/`）：

| venue JSON | Unity asset |
|---|---|
| canvas | `DisplayCanvasConfig.asset`（canvasWidth/Height） |
| faces 順序 | `Preset_2688x3840.asset` 的 faces GUID 順序 |
| 各 face 欄位 | `Face_*.asset`（faceName→name、orientation 0=horizontal/1=vertical、其餘同名） |

## rawMin/Max 計算公式（格子中心像素）

感應模組輸出的 raw 座標系帶有 **X +1920 偏移**（歷史因素：模組校準以部署機
Windows 延伸桌面座標為基準，Display 2 從 X=1920 起）。公式：

```
直（vertical，704 全部）：cellW=32、cellH=16、halfCellW=16、halfCellH=8
橫（horizontal）：        cellW=16、cellH=32、halfCellW=8、 halfCellH=16

rawMinX = originX + halfCellW + 1920
rawMaxX = originX + (colCount-1)×cellW + halfCellW + 1920
rawMinY = originY + halfCellH
rawMaxY = originY + (rowCount-1)×cellH + halfCellH
```

## 光域 704 七面對照（2026-05 已校正、與 venues/704.json 一致）

| face idx | name | origin | 格數 | rawMinX | rawMaxX | rawMinY | rawMaxY |
|---|---|---|---|---|---|---|---|
| 0 | Wall Left | 0,640 | 20×128 | 1936 | 2544 | 648 | 2680 |
| 1 | Wall Button | 640,2688 | 44×40 | 2576 | 3952 | 2696 | 3320 |
| 2 | Floor | 640,640 | 44×128 | 2576 | 3952 | 648 | 2680 |
| 3 | Wall Top | 640,0 | 44×40 | 2576 | 3952 | 8 | 632 |
| 4 | Wall Right Big | 2048,640 | 20×80 | 3984 | 4592 | 648 | 1912 |
| 5 | Entrance | 2048,1920 | 20×32 | 3984 | 4592 | 1928 | 2424 |
| 6 | Wall Right little | 2048,2432 | 20×16 | 3984 | 4592 | 2440 | 2680 |

> 7 面共 14,272 顆晶片（20×128 + 44×40 + 44×128 + 44×40 + 20×80 + 20×32 + 20×16）。

## flip 設定原則

- flip 描述感應器物理方向是否與畫布方向相反
- 用公式算完先設 `flipX=false, flipY=false`，現場四角測試後若方向反轉再切換
- 2026-05 校正時 704 所有面均以 flip=false 通過驗證

## 新場域上線流程

1. **拿到圖面**：各面在合併畫布的 origin、格數（colCount × rowCount）、安裝方向
2. **建 venue JSON**：照上面公式算 rawMin/Max（先假設 +1920 偏移與 704 相同；
   若新場域模組校準基準不同，第 4 步的實測值會直接暴露偏移量）
3. **config.json** 把 `venueProfile` 指到新檔，啟動服務
4. **四角測試**（每面）：
   - 開 testpage（`http://<host>:20111/`）連線
   - 實際踩該面「左上、右上、左下、右下」四個角落格
   - 看 testpage 觸點是否出現在對應角落
   - 全部偏移一截 → rawMin/Max 平移量算錯，從 service log（Debug 級）或
     Wireshark 讀實際 raw 值回推
   - 左右顛倒 → `flipX: true`；上下顛倒 → `flipY: true`
   - 完全沒反應 + `stats` 的 unrouted 上升 → raw 值落在所有面範圍外，
     先用 Wireshark 確認模組輸出範圍
5. **走格驗證**：沿著面走一直線，testpage 觸點軌跡應連續、不跳面、ID 不變
6. 把實測修正寫回 venue JSON、進版控

## 常見錯誤（從 Unity 端校正經驗繼承）

- rawMax 用格子右下邊緣像素（而非**中心**）→ 偏高約 halfCell
- 用物理測試值取代公式值 → 精度受感應器噪訊影響；以公式為準、實測只驗方向與偏移
- 改錯檔案 → 服務啟動 log 會印出實際載入的 venue 路徑與面數，先核對
- 面與面之間的 raw 縫隙（如 704 的 2545~2575）不屬於任何面，踩到縫隙的點被丟棄
  並計入 unrouted——這是正常的，縫隙對應實體面交界

## 與 Unity 端公式的對等性

本服務的 `ContainsRaw / RawToPixel / PixelToCell` 與 Unity `SensorFaceConfig.cs`
逐行對拍（含 float 正規化與 round-half-to-even），由全晶片掃描測試
（`FaceMappingTests.全晶片掃描_與Unity參考實作完全一致`，14,272 顆全蓋）釘死。
已知的 Unity 拉伸 quirk（高列號晶片映射進下一格）**刻意保留**，詳見 PROTOCOL.md。
改公式前先想清楚：投影端（Unity）與觸控端（本服務）的格子判定必須一致。
