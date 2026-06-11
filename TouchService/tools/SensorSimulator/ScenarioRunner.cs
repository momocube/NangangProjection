using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using TouchService.Core;

namespace SensorSimulator;

/// <summary>一次實際送出（或注入丟包）的紀錄，E2E 測試用來斷言 down 延遲。</summary>
public readonly struct SendRecord
{
    public required int TouchIndex { get; init; }
    /// <summary>送出當下的 Stopwatch.GetTimestamp()（同進程內可直接與接收端比對）</summary>
    public required long Timestamp { get; init; }
    public required int RawX { get; init; }
    public required int RawY { get; init; }
    /// <summary>是否為該 touch 的首包</summary>
    public required bool First { get; init; }
    /// <summary>true = 被丟包注入吃掉（沒真的送出）</summary>
    public required bool Dropped { get; init; }
}

/// <summary>
/// scenario 模式執行器：照時間軸驅動 N 個觸碰點，按 resendIntervalMs 重送，
/// 同模組同 tick 合包（≤4 點/datagram，與實測硬體行為一致），
/// 每個模組一個獨立 UDP socket（不同 source port）。
/// 可注入 jitter 與丟包率（亂數種子可重現）。
/// </summary>
public sealed class ScenarioRunner : IDisposable
{
    const int MaxPointsPerDatagram = 4;

    readonly Scenario _scenario;
    readonly IPEndPoint _target;
    readonly Dictionary<int, Socket> _sockets = new();
    readonly Random _rand;

    public List<SendRecord> Records { get; } = new();

    public ScenarioRunner(Scenario scenario, IPEndPoint target)
    {
        _scenario = scenario;
        _target = target;
        _rand = new Random(scenario.Seed);

        foreach (var module in scenario.Touches.Select(t => t.Module).Distinct())
        {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Bind(new IPEndPoint(IPAddress.Loopback, 0));   // 每模組一個 source port
            _sockets[module] = s;
        }
    }

    sealed class TouchState
    {
        public required ScenarioTouch Touch;
        public required int Index;
        public long NextSendMs;
        public bool SentFirst;
        public bool Done;
    }

    /// <summary>跑完整個 scenario（阻塞直到所有 touch 過了 upMs）。</summary>
    public void Run()
    {
        var states = _scenario.Touches
            .Select((t, i) => new TouchState { Touch = t, Index = i, NextSendMs = t.DownMs })
            .ToList();

        var clock = Stopwatch.StartNew();
        var byModule = new Dictionary<int, List<TouchState>>();

        while (true)
        {
            long now = clock.ElapsedMilliseconds;

            // 收集所有到期的 touch，按模組分組
            byModule.Clear();
            bool anyAlive = false;
            foreach (var st in states)
            {
                if (st.Done) continue;
                if (now > st.Touch.UpMs && st.SentFirst) { st.Done = true; continue; }
                anyAlive = true;
                if (now < st.NextSendMs) continue;

                if (!byModule.TryGetValue(st.Touch.Module, out var list))
                    byModule[st.Touch.Module] = list = new List<TouchState>();
                list.Add(st);
            }

            if (!anyAlive) break;

            foreach (var (module, due) in byModule)
            {
                // 同模組合包：最多 4 點一個 datagram（與實測硬體一致）
                for (int chunk = 0; chunk < due.Count; chunk += MaxPointsPerDatagram)
                {
                    var slice = due.Skip(chunk).Take(MaxPointsPerDatagram).ToList();
                    SendChunk(module, slice, now);
                }
            }

            // 排程下一次重送
            foreach (var list in byModule.Values)
                foreach (var st in list)
                {
                    int jitter = _scenario.JitterMs > 0
                        ? _rand.Next(-_scenario.JitterMs, _scenario.JitterMs + 1)
                        : 0;
                    st.NextSendMs = now + _scenario.ResendIntervalMs + jitter;
                }

            Thread.Sleep(1);
        }
    }

    void SendChunk(int module, List<TouchState> slice, long nowMs)
    {
        var points = new List<RawPoint>(slice.Count);
        var sent = new List<TouchState>(slice.Count);

        foreach (var st in slice)
        {
            // 丟包注入：首包不丟（down 時刻必須確定），其餘每次獨立判定
            bool drop = st.SentFirst && _scenario.DropRate > 0 && _rand.NextDouble() < _scenario.DropRate;

            var (x, y) = PositionAt(st.Touch, nowMs);
            if (drop)
            {
                Records.Add(new SendRecord
                {
                    TouchIndex = st.Index, Timestamp = Stopwatch.GetTimestamp(),
                    RawX = x, RawY = y, First = false, Dropped = true,
                });
                continue;
            }

            points.Add(new RawPoint(x, y));
            sent.Add(st);
        }

        if (points.Count == 0) return;

        var packet = SensorPacketParser.Build(0, points);
        long ts = Stopwatch.GetTimestamp();
        _sockets[module].SendTo(packet, _target);

        for (int i = 0; i < sent.Count; i++)
        {
            Records.Add(new SendRecord
            {
                TouchIndex = sent[i].Index, Timestamp = ts,
                RawX = points[i].X, RawY = points[i].Y,
                First = !sent[i].SentFirst, Dropped = false,
            });
            sent[i].SentFirst = true;
        }
    }

    static (int X, int Y) PositionAt(ScenarioTouch t, long nowMs)
    {
        if (t.MoveToRawX is null && t.MoveToRawY is null) return (t.RawX, t.RawY);

        double dur = Math.Max(1, t.UpMs - t.DownMs);
        double k = Math.Clamp((nowMs - t.DownMs) / dur, 0, 1);
        int x = (int)Math.Round(t.RawX + ((t.MoveToRawX ?? t.RawX) - t.RawX) * k);
        int y = (int)Math.Round(t.RawY + ((t.MoveToRawY ?? t.RawY) - t.RawY) * k);
        return (x, y);
    }

    public void Dispose()
    {
        foreach (var s in _sockets.Values) s.Dispose();
    }
}
