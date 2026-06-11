using System.Text;

namespace TouchService.Logging;

public enum LogLevel { Debug = 0, Info = 1, Warn = 2, Error = 3 }

/// <summary>
/// 極簡檔案 logger：每日輪替（檔名帶日期）+ console 精簡輸出。
/// 執行緒安全（單一 lock；服務 log 量低，不需要非同步 queue）。
/// </summary>
public sealed class FileLogger : IDisposable
{
    readonly object _gate = new();
    readonly string _dir;
    readonly bool _console;
    readonly LogLevel _minLevel;

    StreamWriter? _writer;
    DateTime _writerDate;

    public FileLogger(string dir, LogLevel minLevel, bool console)
    {
        _dir = dir;
        _minLevel = minLevel;
        _console = console;
        Directory.CreateDirectory(dir);
    }

    public static LogLevel ParseLevel(string s) => s.Trim().ToLowerInvariant() switch
    {
        "debug" => LogLevel.Debug,
        "info" => LogLevel.Info,
        "warn" or "warning" => LogLevel.Warn,
        "error" => LogLevel.Error,
        _ => LogLevel.Info,
    };

    public void Debug(string msg) => Write(LogLevel.Debug, msg);
    public void Info(string msg)  => Write(LogLevel.Info, msg);
    public void Warn(string msg)  => Write(LogLevel.Warn, msg);
    public void Error(string msg) => Write(LogLevel.Error, msg);

    void Write(LogLevel level, string msg)
    {
        if (level < _minLevel) return;

        var now = DateTime.Now;
        var line = $"{now:yyyy-MM-dd HH:mm:ss.fff} [{level,-5}] {msg}";

        lock (_gate)
        {
            EnsureWriter(now.Date);
            _writer!.WriteLine(line);
            _writer.Flush();

            if (_console)
                Console.WriteLine(line);
        }
    }

    void EnsureWriter(DateTime date)
    {
        if (_writer != null && _writerDate == date) return;

        _writer?.Dispose();
        _writerDate = date;
        var path = Path.Combine(_dir, $"touchservice-{date:yyyyMMdd}.log");
        _writer = new StreamWriter(path, append: true, Encoding.UTF8);
    }

    public void Dispose()
    {
        lock (_gate) { _writer?.Dispose(); _writer = null; }
    }
}
