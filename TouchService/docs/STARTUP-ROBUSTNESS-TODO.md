# TouchService 啟動健壯性改善任務單

> 2026-06-17 起草。針對「程式啟動後閃退、無錯誤提示」的根因改善。
> 給承接維護的 AI / 工程師：每項都有 P 等級、檔案路徑、現狀引用、修改方向、驗收條件。

## 背景：一次真實事故（2026-06-17 南港 704 現場）

啟動時 console log：
```
[Info ] [Mouse] 採格模式（grid）：純發布，無滑鼠注入
[Error] [TouchService] 啟動失敗：嘗試存取通訊端被拒絕，因為存取權限不足。
[Info ] [Host] 已停止
```

實際 root cause：另一個 process（`CyberCube.exe`，PID 26456）正在 listen UDP 20001，TouchService 想 bind 同一 port → `SocketException(WSAEACCES, 10013)`。

但目前的錯誤訊息對使用者一點幫助都沒有：
- 看不出是 **哪個** port（log 沒帶 port 號）
- 看不出 **誰** 佔用（沒列 PID/process name）
- 不知道 **怎麼修**（沒給 taskkill 提示）
- **托盤模式下 console 被隱藏，連這行錯誤都看不到** → 表象就是「點兩下沒反應、閃退」

以下任務按優先序排，每項可獨立完成、獨立 PR。

---

## P0-1：Bind 失敗友善訊息 + 佔用者識別

### 現狀
- `src/TouchService/TouchServiceHost.cs:173` — `_listenSocket.Bind(new IPEndPoint(IPAddress.Any, _config.ListenPort))` 無 try-catch
- `src/TouchService/Output/WebSocketHub.cs:94` — `_listener.Start()` 無 try-catch
- 例外冒泡到 `Program.cs:51` 才被 catch、用 `log.Error(ex.Message)` 寫出
- 訊息只是 .NET 翻譯後的 "嘗試存取通訊端被拒絕，因為存取權限不足"，使用者無法 actionable

### 修改方向
1. 在三個 bind 點（20001 listen / 20111 TCP listen / 未來新增的 bind 點）外圍包 try-catch，捕捉 `SocketException`
2. 區分 `SocketError`：
   - `AddressAlreadyInUse` (10048) → 「port X 已被別的 process 佔用」
   - `AccessDenied` (10013) → 同上（Windows 上同一個 port 衝突常呈現成這個）
   - 其他 → 原訊息 + error code
3. 拋出自訂例外 `PortBindException(int port, string protocol, string occupantHint)`，攜帶：
   - port 號
   - protocol（UDP / TCP）
   - 佔用者提示（見下方識別邏輯）
4. 在 `Program.cs:51` catch 端對 `PortBindException` 特化處理 → 寫多行 friendly log
5. 識別佔用者：用 `System.Diagnostics.Process` + `netstat` 解析、或呼叫 `iphlpapi.dll` 的 `GetExtendedUdpTable` / `GetExtendedTcpTable`（不要依賴外部 process）
   - 找到 owning PID → `Process.GetProcessById(pid).ProcessName`

### 驗收
- 故意先開一個別的 process 佔用 20001（例如 `python -c "import socket;s=socket.socket(socket.AF_INET,socket.SOCK_DGRAM);s.bind(('0.0.0.0',20001));input()"`）
- 啟動 TouchService → log 應顯示類似：
  ```
  [Error] [TouchService] 無法綁定 UDP port 20001：已被 process "python" (PID 12345) 佔用
  [Error] [TouchService] 處理建議：
  [Error] [TouchService]   1) 確認 python.exe 是否應該執行；若否，請結束該 process
  [Error] [TouchService]      cmd: taskkill /F /PID 12345
  [Error] [TouchService]   2) 或修改 config.json 的 ListenPort 改用其他 port
  ```

---

## P0-2：Port-in-use 預檢（啟動前主動探測）

### 現狀
- 啟動鏈直接 bind，失敗才知道
- 沒有 pre-flight check

### 修改方向
1. 在 `TouchServiceHost.Start()` 開頭、`_listenSocket.Bind` 之前，加 `PortPreflightCheck` 步驟
2. 對每一個將要 bind 的 port，用 `IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners()` / `GetActiveTcpListeners()` 確認是否已被 listen
3. 若已被佔用，**不要直接拋**——先 log 警告 + 識別佔用者 + 等 N 秒重試（讓使用者有機會 kill 衝突 process）
4. 加 config：
   ```json
   "startup": {
     "portCheckTimeoutSec": 10,
     "portCheckRetryIntervalSec": 1
   }
   ```
5. 重試逾時仍佔用 → 拋 `PortBindException`（接 P0-1）

### 驗收
- 開衝突 process → 啟動 TouchService
- 看到「等待 port 20001 釋放… (剩餘 9s)」rolling log
- 在等待視窗內 kill 衝突 process → TouchService 繼續啟動成功
- 不 kill → 10s 後印 P0-1 的 friendly error 退出

---

## P0-3：托盤/GUI 模式啟動失敗時必彈 popup

### 現狀
- `src/TouchService/Program.cs:64-65` — 托盤模式下 `ConsoleWindowManager.Hide(log)` 隱藏 console
- `Program.cs:51-55` catch 後只 `log.Error()` + `return 1`
- 使用者看不到任何錯誤、表象就是「點兩下閃退」

### 修改方向
1. 在 `Program.cs` 的 outermost catch 內判斷是否為托盤模式（`trayMode == true && !keepConsole`）
2. 若是 → 呼叫 `System.Windows.Forms.MessageBox.Show` 或 P/Invoke `user32.dll!MessageBoxW`
   - 標題：「TouchService 啟動失敗」
   - 內文：error message + log 檔路徑（`logs\touchservice-YYYYMMDD.log`）
   - icon：MB_ICONERROR
3. 內文末段加：「詳細日誌請查看 logs 資料夾下的最新 .log 檔」
4. 即使 console 被隱藏、log 仍有寫檔（FileLogger 行為已正確），所以提示路徑即可

### 驗收
- 雙擊 TouchService.exe（托盤模式預設）
- 故意製造 bind 衝突
- 應跳 MessageBox，內文含「port 20001」「PID xxx」「請查看 logs\touchservice-20260617.log」
- 按確定後 process 才結束

---

## P0-4：全局 Unhandled Exception Handler

### 現狀
- `Program.cs:21` Main 進入點未註冊 `AppDomain.CurrentDomain.UnhandledException`
- 未註冊 `TaskScheduler.UnobservedTaskException`
- 背景 thread（ReceiveLoop / TimerLoop / stdin / 托盤 STA thread）各自有 try-catch，但 catch 不到全部情境
- 非預期崩潰 → process 直接死、無 log

### 修改方向
1. `Program.Main` 最開頭（在 logger 初始化後）註冊：
   ```csharp
   AppDomain.CurrentDomain.UnhandledException += (s, e) => {
       var ex = e.ExceptionObject as Exception;
       log.Error($"[FATAL] Unhandled exception (IsTerminating={e.IsTerminating}): {ex}");
       // 視情況寫 crash dump：MiniDumpWriteDump 或先簡化只寫 log
   };
   TaskScheduler.UnobservedTaskException += (s, e) => {
       log.Error($"[FATAL] Unobserved task exception: {e.Exception}");
       e.SetObserved();
   };
   ```
2. 確保 logger 在 handler 觸發前已初始化（必要時用靜態 fallback：寫到 `%TEMP%\touchservice-crash-{timestamp}.log`）
3. 同時觸發 P0-3 的 MessageBox（托盤模式下）

### 驗收
- 在某個背景 thread 故意 throw `new InvalidOperationException("test")`
- 應該在 log 看到 `[FATAL] Unhandled exception...` 完整 stack trace
- 托盤模式下還要彈 MessageBox

---

## P1-1：Single-Instance Mutex

### 現狀
- 無防止重複啟動機制
- 若使用者誤點兩次 TouchService.exe，第二個會搶 20001 → bind 失敗 → 但這時 P0-1 friendly error 會顯示「被 process TouchService.exe (PID xxx) 佔用」，使用者可能誤殺第一個

### 修改方向
1. `Program.Main` 開頭建立 named mutex：
   ```csharp
   var mutex = new System.Threading.Mutex(true, "Global\\TouchService_NangangProjection", out bool createdNew);
   if (!createdNew) {
       log.Error("TouchService 已經在執行中（single-instance mutex）");
       // 托盤模式彈 MessageBox：「TouchService 已在背景執行，請從工作列圖示開啟設定」
       return 2;
   }
   ```
2. mutex 在 process 結束時自動釋放（finally + Dispose）
3. P0-1 的佔用者識別若發現是自己（process name 為 "TouchService"），直接走 single-instance 路徑、不要拋 PortBindException

### 驗收
- 雙擊 TouchService.exe 兩次
- 第二次應彈 MessageBox「已經在執行中」、不會殺第一個

---

## P1-2：CyberCube.exe 衝突主動處理（場域特化）

### 現狀
- `CyberCube.exe` 是前身/廠商版本，與 TouchService 都會 listen UDP 20001
- 現場 PC（DESKTOP-51K94CL）開機自動啟動 CyberCube.exe，OS 啟動 TouchService 必衝突

### 修改方向（兩種選一）

**方案 A：TouchService 啟動腳本層處理（推薦）**
- 寫 `scripts/start-touchservice.bat`：
  ```bat
  @echo off
  taskkill /F /IM CyberCube.exe 2>nul
  start "" "%~dp0TouchService.exe"
  ```
- 開機自啟改成跑這個 bat 而不是直接跑 exe
- 優點：TouchService 程式碼不需特化、外部腳本可讀可改

**方案 B：TouchService 內建 conflicting-process 黑名單**
- config.json 加：
  ```json
  "startup": {
    "conflictingProcesses": ["CyberCube"]
  }
  ```
- 啟動前掃 process 列表、發現黑名單 process 時 log 警告 + 提示使用者 kill（不要自動殺，避免誤殺）
- 優點：行為可審計、不依賴外部腳本

### 驗收
- 開機 → CyberCube.exe 啟動（無法阻止，廠商機制）→ TouchService 啟動
- 方案 A：CyberCube.exe 被殺、TouchService 起來
- 方案 B：使用者看到「偵測到 CyberCube.exe (PID xxx) 與本服務衝突，建議結束該 process」warning + 5 秒倒數 + 如未解決則拋 P0-1 friendly error

---

## P2-1：Crash Dump 寫入（選做）

### 現狀
- P0-4 只寫 log、無 minidump
- 非例外型崩潰（例如 stack overflow、AccessViolation）log 也可能來不及寫

### 修改方向
- 在 P0-4 UnhandledException handler 內呼叫 `MiniDumpWriteDump`（dbghelp.dll）
- 寫到 `logs\crashdumps\touchservice-{timestamp}.dmp`
- 保留最近 5 份，自動清舊

### 驗收
- 觸發 unhandled crash → `logs\crashdumps\` 有 .dmp 檔
- WinDbg 開檔可看 stack

---

## 共通：error code 對照表 doc

請新增 `docs/ERROR-CODES.md`，列出所有 PortBindException、SocketError code（10013/10048/...）、`return code`（0/1/2/...）的對照與處理建議。

---

## 給承接 AI 的小提醒

- TouchService 是 .NET 8 console + Windows-only P/Invoke（托盤、Console hide）的混合體。Linux 端可跑 dotnet test 驗證大部分邏輯，但托盤/MessageBox 等改動必須在 Windows 實機驗證
- 修改後務必：(1) `dotnet test` 全綠 (2) Windows publish (3) 現場驗證閃退情境是否變成 friendly error
- 不要刪 `Program.cs:51` 那個 outermost catch；P0-1 ~ P0-4 都是「在它前面/裡面加東西」而不是替換它
- 改完後更新 `docs/HANDOVER.md` 對應段落、結案報告未解決事項
- commit 風格參考最近的 8f74290 / 765ddc3（中文 conventional + 簡要原因 + 測試數）
