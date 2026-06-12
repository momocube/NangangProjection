using System.Collections.Concurrent;
using System.Text.Json;
using TouchService.Logging;
using TouchService.Output;

namespace TouchService.Calibration;

/// <summary>
/// 校正工作階段 + raw 檢視串流（校正精靈的地基）：
///
///   POST /calibration/start → Active = true（預設 15 分鐘逾時自動 stop）
///   active 期間 pipeline 對每個進來的 raw 點（路由成功與否都要）呼叫 OnRawPoint，
///   經 RawStreamGate 節流後推 {"type":"raw","x","y","src","port","face","n"} 給
///   **僅 loopback** 的 WS client（與 log push 同一條安全線：內部訊號不外流內網）。
///
/// 非校正階段零成本：OnRawPoint 第一行 volatile bool 檢查就返回。
/// 校正階段服務照常運作——raw 推送是「額外的 tap」，發布 / 追蹤 / 滑鼠完全不受影響。
///
/// 執行緒模型（仿 LogStreamer）：OnRawPoint 在 pipeline 鎖內被呼叫，只做
/// gate 判定 + enqueue + signal（≤ 30 msg/s，無 IO）；網路推送在自己的背景執行緒。
/// </summary>
public sealed class CalibrationStreamer : IDisposable
{
    /// <summary>工作階段預設逾時（分鐘）：現場人員忘了按停止也不會永遠開著。</summary>
    public const int DefaultTimeoutMinutes = 15;

    readonly object _session = new();
    readonly RawStreamGate _gate = new();
    readonly ConcurrentQueue<byte[]> _pending = new();
    readonly AutoResetEvent _signal = new(false);
    readonly CancellationTokenSource _cts = new();
    readonly FileLogger _log;
    Thread? _thread;
    volatile WebSocketHub? _hub;

    volatile bool _active;
    long _expiresAtMs;

    public CalibrationStreamer(FileLogger log) => _log = log;

    /// <summary>校正工作階段是否進行中（pipeline 熱路徑的 gating 用，volatile）。</summary>
    public bool Active => _active;

    /// <summary>
    /// 開始（或重新開始）校正工作階段：重置節流狀態、重算逾時。
    /// 重複 start = 延長逾時（UI keep-alive 可重打）。回傳逾時秒數。
    /// </summary>
    public int Start(long nowMs, int timeoutMinutes = DefaultTimeoutMinutes)
    {
        lock (_session)
        {
            _gate.Reset();
            _expiresAtMs = nowMs + timeoutMinutes * 60_000L;
            _active = true;
        }
        _log.Info($"[Calibration] 校正工作階段開始（raw 檢視串流 → loopback WS；{timeoutMinutes} 分鐘無 stop 自動結束）");
        PushStatus();
        return timeoutMinutes * 60;
    }

    /// <summary>結束校正工作階段（API stop / 逾時 / host 關閉）。已停止時為 no-op。</summary>
    public void Stop(string reason)
    {
        lock (_session)
        {
            if (!_active) return;
            _active = false;
        }
        _log.Info($"[Calibration] 校正工作階段結束（{reason}）");
        PushStatus();
    }

    /// <summary>逾時檢查（pipeline Tick 呼叫；非 active 時零成本）。</summary>
    public void CheckExpiry(long nowMs)
    {
        if (!_active) return;
        bool expired;
        lock (_session) expired = _active && nowMs >= _expiresAtMs;
        if (expired) Stop($"逾時 {DefaultTimeoutMinutes} 分鐘自動結束");
    }

    /// <summary>工作階段狀態（GET /calibration/status / start 回應用）。</summary>
    public (bool Active, int RemainingSeconds) Status(long nowMs)
    {
        lock (_session)
        {
            if (!_active) return (false, 0);
            return (true, (int)Math.Max(0, (_expiresAtMs - nowMs + 999) / 1000));
        }
    }

    /// <summary>
    /// pipeline 對每個進來的 raw 點呼叫（路由成功 face = 面 index、失敗 = null；
    /// 在 pipeline 鎖內、熱路徑上——非 active 時第一行就返回）。
    /// </summary>
    public void OnRawPoint(int x, int y, string? srcAddr, int srcPort, int? face, long nowMs)
    {
        if (!_active || _hub == null) return;

        bool emit;
        int n;
        lock (_session)
        {
            if (!_active) return;
            emit = _gate.Offer(srcAddr ?? "", srcPort, x, y, nowMs, out n);
        }
        if (!emit) return;

        _pending.Enqueue(Serialize(x, y, srcAddr ?? "", srcPort, face, n));
        _signal.Set();
    }

    /// <summary>{"type":"raw","x":..,"y":..,"src":"ip","port":..,"face":idx|null,"n":連續次數}（純函式，測試對拍）。</summary>
    public static byte[] Serialize(int x, int y, string src, int port, int? face, int n) =>
        JsonSerializer.SerializeToUtf8Bytes(new { type = "raw", x, y, src, port, face, n });

    /// <summary>WS hub 起來後掛上（host.Start 呼叫），並啟動推送執行緒。</summary>
    public void Attach(WebSocketHub hub)
    {
        _hub = hub;
        if (_thread != null) return;
        _thread = new Thread(PushLoop) { IsBackground = true, Name = "CalibStream" };
        _thread.Start();
    }

    /// <summary>start / stop / 逾時 → 推 {"type":"calibration","active":...} 通知 UI（僅 loopback）。</summary>
    void PushStatus()
    {
        if (_hub == null) return;
        _pending.Enqueue(JsonSerializer.SerializeToUtf8Bytes(new { type = "calibration", active = _active }));
        _signal.Set();
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
