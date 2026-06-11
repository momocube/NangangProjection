using System.Collections.Concurrent;
using System.Text.Json;
using TouchService.Output;

namespace TouchService.Logging;

/// <summary>
/// UI 終端的 log 串流：訂閱 FileLogger.EntryWritten →
///   1. ring buffer 保留最近 300 行（loopback client 連上時補發）
///   2. 背景執行緒推 {"type":"log","ts":...,"level":"...","line":"..."} 給
///      **僅 loopback** 的 WS client（LAN 訂閱端不收 log——內容不該外流；
///      它們本來就會忽略帶 type 的訊息，這裡是內容面的防線）
///
/// 執行緒模型：OnLog 只 enqueue + signal（log 熱路徑零阻塞）；序列化與網路 IO
/// 都在自己的背景執行緒。推送本身產生的 log（例如 client 離線）也走同一條 queue，
/// 不會遞迴爆炸。
/// </summary>
public sealed class LogStreamer : IDisposable
{
    public const int Capacity = 300;

    readonly object _gate = new();
    readonly Queue<byte[]> _ring = new(Capacity);           // 已序列化的 log 訊息
    readonly ConcurrentQueue<byte[]> _pending = new();      // 待推送
    readonly AutoResetEvent _signal = new(false);
    readonly CancellationTokenSource _cts = new();
    Thread? _thread;
    volatile WebSocketHub? _hub;

    /// <summary>FileLogger.EntryWritten 掛這裡（任何執行緒；只 enqueue，不做 IO）。</summary>
    public void OnLog(DateTime ts, LogLevel level, string line)
    {
        var bytes = Serialize(new DateTimeOffset(ts).ToUnixTimeMilliseconds(), level, line);
        lock (_gate)
        {
            _ring.Enqueue(bytes);
            while (_ring.Count > Capacity) _ring.Dequeue();
        }
        // 未 Attach（webSocketOutput 停用）不進 pending——否則佇列無人消化會無限長大
        if (_hub != null)
        {
            _pending.Enqueue(bytes);
            _signal.Set();
        }
    }

    /// <summary>{"type":"log","ts":...,"level":"Info","line":"..."}（純函式，測試對拍）。</summary>
    public static byte[] Serialize(long tsMs, LogLevel level, string line) =>
        JsonSerializer.SerializeToUtf8Bytes(new { type = "log", ts = tsMs, level = level.ToString(), line });

    /// <summary>ring buffer 快照（loopback client 連上時 hub 補發；最舊在前）。</summary>
    public IReadOnlyList<byte[]> Backlog()
    {
        lock (_gate) return _ring.ToArray();
    }

    /// <summary>WS hub 起來後掛上（host.Start 呼叫），並啟動推送執行緒。</summary>
    public void Attach(WebSocketHub hub)
    {
        _hub = hub;
        if (_thread != null) return;
        _thread = new Thread(PushLoop) { IsBackground = true, Name = "LogStream" };
        _thread.Start();
    }

    void PushLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            _signal.WaitOne(500);
            var hub = _hub;
            while (hub != null && _pending.TryDequeue(out var bytes))
                hub.PublishToLoopback(bytes);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _signal.Set();
        _thread?.Join(1000);
        _signal.Dispose();
    }
}
