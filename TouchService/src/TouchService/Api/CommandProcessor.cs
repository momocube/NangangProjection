using TouchService.Diagnostics;

namespace TouchService.Api;

/// <summary>
/// 服務指令的單一處理入口：stdin 迴圈與 POST /command **走同一個**，輸出文字一致
/// （UI 終端看到的跟黑視窗一字不差）。指令：stats / touches / history [n] / locks /
/// lock / unlock / savelocks / probe / quit。
/// quit 透過 requestShutdown 回呼觸發優雅關閉（Program 的 quit event / 托盤「結束服務」同源）。
/// </summary>
public sealed class CommandProcessor
{
    public const string HelpText =
        "指令：stats（統計）/ touches（活躍 touch）/ history [n]（疑似噪訊排行，預設 top 20）/ " +
        "locks（鎖定清單）/ lock|unlock <face> <col> <row>（手動鎖/解鎖）/ " +
        "savelocks（鎖定寫回 config）/ probe（dump latency CSV）/ quit（離開）";

    const string UnknownText = "未知指令。可用：stats / touches / history [n] / locks / lock / unlock / savelocks / probe / quit";

    readonly TouchServiceHost _host;
    readonly string _logDir;
    readonly Action _requestShutdown;

    public CommandProcessor(TouchServiceHost host, string logDir, Action requestShutdown)
    {
        _host = host;
        _logDir = logDir;
        _requestShutdown = requestShutdown;
    }

    /// <summary>
    /// 執行一行指令，回傳輸出文字（空字串 = 無輸出）。
    /// quit/exit/q 會觸發 requestShutdown（呼叫端不需要特判）。
    /// </summary>
    public string Execute(string line)
    {
        var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        switch (parts.Length == 0 ? "" : parts[0].ToLowerInvariant())
        {
            case "stats":
                return _host.StatsSnapshot();

            case "touches":
                return _host.TouchesSnapshot();

            case "history":
                int top = 20;
                if (parts.Length >= 2 && (!int.TryParse(parts[1], out top) || top <= 0))
                    return "用法：history [n]（n 為正整數，預設 20；例：history 50）";
                return _host.HistorySnapshot(top);

            case "locks":
                return _host.LocksSnapshot();

            case "lock" or "unlock":
                if (parts.Length != 4 ||
                    !int.TryParse(parts[1], out int face) ||
                    !int.TryParse(parts[2], out int col) ||
                    !int.TryParse(parts[3], out int row))
                {
                    var cmd = parts[0].ToLowerInvariant();
                    return $"用法：{cmd} <face> <col> <row>（例：{cmd} 2 7 13）";
                }
                return parts[0].ToLowerInvariant() == "lock"
                    ? _host.LockCell(face, col, row)
                    : _host.UnlockCell(face, col, row);

            case "savelocks":
                return _host.SaveLocks();

            case "probe":
                var path = LatencyProbe.DumpCsv(_logDir);
                return $"latency CSV → {path}";

            case "quit" or "exit" or "q":
                _requestShutdown();
                return "正在關閉服務…";

            case "":
                return "";

            case "help" or "?":
                return HelpText;

            default:
                return UnknownText;
        }
    }
}
