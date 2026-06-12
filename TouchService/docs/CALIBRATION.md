# 場域校正手冊（venue profile）

> **v8 起新場域建置走 UI 校正精靈（建議路徑）**——非工程師十分鐘可完成；
> 本文後半的公式與手動流程是校正語意的真相源 + 精靈不夠用時（特殊佈局、
> 遠端除錯、驗算）的備援。

> **⚠️ 驗證狀態（2026-06-12 交接時點）**：校正精靈的閉環只在開發機以
> SensorSimulator + 自動化 E2E（`CalibrationWizardE2ETests`）驗過，
> **尚未在實機硬體上走過完整流程**——704 交接前沒有排到實機驗證時段
> （營運場地不可承受設定毀損風險）。第一次實機使用請遵守下方安全守則。
>
> **安全守則（防呆 + 還原路線）**：
> 1. **704 營運機上不要拿正式設定做實驗**。精靈只寫兩個檔：
>    `venues/<場域名>.json`（**同名會覆寫**——場域名填 `704` 就會蓋掉正式校正檔）
>    和 `config.json` 的 `venueProfile` 指標（其餘欄位原樣保留）。程式本體零修改。
> 2. **動精靈前先備份**（一個資料夾搞定）：
>    把 exe 旁的 `config.json` + 整個 `venues\` 資料夾複製一份（如 `backup-YYYYMMDD\`）。
> 3. **還原** = 把備份的兩個東西複製回原位 → 重啟 TouchService。
>    精靈寫入要**重啟才生效**，所以還原前就算誤存，跑著的服務也還是舊設定——
>    發現存錯時別重啟，先還原檔案即可。
> 4. 想練習流程：場域名填 `test`（產生 `venues/test.json`，不碰 704），
>    驗完刪檔 + 把 config 的 `venueProfile` 改回 `venues/704.json` 即可；
>    或在任何開發機用 SensorSimulator 走全流程（見下）。

## 校正精靈（UI，建議路徑）

在**服務所在機器**開 UI（autoLaunch 自動開、或瀏覽器開 `http://127.0.0.1:20111/`）
→「校正」tab（非本機開頁會顯示「校正僅限本機」——raw 訊號與寫檔只開放 loopback）：

1. **開始校正**：開校正工作階段（`POST /calibration/start`）。服務把每個進來的
   raw 點即時推到精靈頁（同座標去重 + ~30 msg/s 節流；15 分鐘無動作自動結束，
   重按可延長）。**校正期間服務照常發布**，不影響營運。
2. **填場域與面**：場域名稱（= 檔名 `venues/<名>.json`）、面名稱、格數
   cols × rows、晶片方向 vertical / horizontal；畫布尺寸自動計算。
3. **踩左上角**：依引導踩該面「左上角第一顆晶片」踩住不動——raw 值大字顯示，
   **同值連續 ≥ 3 包自動鎖定**（訊號跳動時可按「手動取用目前值」）。
4. **踩右下角**：同上，踩「右下角最後一顆晶片」。
5. 精靈自動算出 **rawMin/Max + flipX/flipY**（語意見下方〈校正語意〉），
   面預覽圖即時把踩的點標出來——兩角取得後再踩任意位置會映射成綠點，
   方向 / flip 對不對當場看得到。
6. **加入此面** → 多面場域改面名與 origin（手填，px）後重複 3–5 逐面累積；
   單面場域 origin 維持 0,0。
7. **儲存並套用**：`POST /calibration/save` 寫入 `venues/<名>.json` 並把
   config.json 的 `venueProfile` 指過去——**重啟服務後生效**
   （托盤右鍵「結束服務」後重開 TouchService.exe）。
8. 重啟後用監控 tab 做四角 + 走格驗證（見下方手動流程第 4–5 步）。

無實機練習：`SensorSimulator` 對 listenPort 發任意 raw 座標即可全流程走一遍
（`tests/CalibrationWizardE2ETests.cs` 就是這條閉環的自動化版本）。

## 校正語意（rawMin/Max 與 flip 的定義——精靈與手動共同的真相源）

- `rawMinX/Y`、`rawMaxX/Y` = **第一顆晶片（左上）與最後一顆晶片（右下）的中心
  raw 值**——不是面邊緣、不是格子邊緣。
- `flipX/flipY` = 兩角 raw 的大小關係**反向**（左上 raw > 右下 raw）時為 true
  ——感應器物理方向與畫布方向相反。精靈據此自動判定；rawMin/Max 永遠存
  小值/大值（min/max），方向資訊只在 flip 欄位。
- 兩角 raw 相同（同一軸）= 無法定義範圍，save 會拒收——表示兩角踩到同一顆
  晶片或同一行/列。

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
- **校正精靈自動判定**（兩角 raw 大小關係反向 = flip，見上方〈校正語意〉）
- 手動建檔時：用公式算完先設 `flipX=false, flipY=false`，現場四角測試後若方向反轉再切換
- 2026-05 校正時 704 所有面均以 flip=false 通過驗證

## 新場域上線流程（手動備援——一般情況用上方校正精靈）

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
