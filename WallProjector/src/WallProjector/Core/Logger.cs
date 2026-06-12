namespace WallProjector.Core;

/// <summary>
/// 極簡 console logger（時間戳 + level）。WallProjector 是顯示層工具，
/// log 需求遠小於 TouchService（無熱路徑）——不引檔案 logger，console 即真相。
/// </summary>
public sealed class Logger
{
    public enum Level { Debug, Info, Warn, Error }

    public Level MinLevel { get; set; } = Level.Info;

    readonly object _gate = new();

    public void Debug(string msg) => Write(Level.Debug, msg);
    public void Info(string msg) => Write(Level.Info, msg);
    public void Warn(string msg) => Write(Level.Warn, msg);
    public void Error(string msg) => Write(Level.Error, msg);

    void Write(Level level, string msg)
    {
        if (level < MinLevel) return;
        var line = $"{DateTime.Now:HH:mm:ss.fff} [{level,-5}] {msg}";
        lock (_gate)
        {
            if (level >= Level.Warn) Console.Error.WriteLine(line);
            else Console.WriteLine(line);
        }
    }
}
