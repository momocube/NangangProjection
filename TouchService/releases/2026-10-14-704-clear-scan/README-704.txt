TouchService 704 清場掃描版
版本：aab26b9

此資料夾含 Windows x64 執行檔、704 場域檔與監控網頁。
第一次啟動時若沒有 config.json，服務會建立預設設定；若已有現場設定，請保留原檔。
設定中的 venueProfile 預設為 venues/704.json，因此請保留 venues 資料夾與其路徑。

建議使用 start-704.cmd 啟動。正式佈署更新時，先停止服務並備份，再更新 TouchService.exe、venues/704.json 與 testpage；保留現場 config.json 和 state 資料夾。
