using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TouchService.Core;
using TouchService.Logging;

namespace TouchService.Diagnostics;

public sealed class NoiseCaptureCell
{
    public int Face { get; set; }
    public int Col { get; set; }
    public int Row { get; set; }
    public long SignalCount { get; set; }
    public DateTimeOffset FirstSeenUtc { get; set; }
    public DateTimeOffset LastSeenUtc { get; set; }
}

public sealed record NoiseCaptureCellView(
    int Face, int Col, int Row, long SignalCount,
    DateTimeOffset FirstSeenUtc, DateTimeOffset LastSeenUtc,
    bool Locked, string? LockSource, bool LockedByDraft, bool Saved,
    long DroppedSinceLock, long? LastDropAgoMs);

public sealed record NoiseCaptureSnapshot(
    int Version, string Venue, string VenueSignature, string DraftId, string State,
    bool Compatible, long ElapsedMs, DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc, DateTimeOffset? LastNewCellUtc,
    DateTimeOffset? LastSavedUtc, string? SaveError,
    IReadOnlyList<NoiseCaptureCellView> Cells);

/// <summary>
/// Bounded per-cell清場掃描記錄，搭配節流的背景 JSON 保存。
/// Pipeline 可在每個已路由 raw 點呼叫 Observe；磁碟寫入永遠不在呼叫執行緒。
/// </summary>
public sealed class NoiseCaptureJournal : IDisposable
{
    const int CurrentVersion = 1;
    const int SaveIntervalMs = 1000;
    static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    sealed class Draft
    {
        public int Version { get; set; } = CurrentVersion;
        public string Venue { get; set; } = "";
        public string VenueSignature { get; set; } = "";
        public string DraftId { get; set; } = "";
        public string State { get; set; } = "stopped";
        public long ElapsedMs { get; set; }
        public DateTimeOffset CreatedUtc { get; set; }
        public DateTimeOffset UpdatedUtc { get; set; }
        public DateTimeOffset? LastNewCellUtc { get; set; }
        public List<NoiseCaptureCell> Cells { get; set; } = new();
    }

    readonly object _gate = new();
    readonly LockedCellFilter _locks;
    readonly FileLogger _log;
    readonly string _venue;
    readonly string _venueSignature;
    readonly string? _path;
    readonly Func<long> _clockMs;
    readonly Dictionary<int, NoiseCaptureCell> _cells = new();
    readonly Thread _writer;
    Draft? _draft;
    long _runStartMs;
    long _version;
    long _lastWriteTimestamp;
    DateTimeOffset? _lastSavedUtc;
    string? _saveError;
    bool _dirty;
    bool _forceWrite;
    bool _shutdown;

    public NoiseCaptureJournal(string venue, string venueSignature, LockedCellFilter locks,
        string? configPath, Func<long> clockMs, FileLogger log)
    {
        _venue = venue;
        _venueSignature = venueSignature;
        _locks = locks;
        _clockMs = clockMs;
        _log = log;
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            var fullConfigPath = Path.GetFullPath(configPath);
            string identity = Path.GetFullPath(fullConfigPath) + "|" + venueSignature;
            string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..24].ToLowerInvariant();
            _path = Path.Combine(Path.GetDirectoryName(fullConfigPath)!, "state", "noise-capture", key + ".json");
            LoadDraft();
        }

        _writer = new Thread(WriterLoop) { IsBackground = true, Name = "NoiseCaptureSave" };
        _writer.Start();
    }

    public bool IsRunning { get { lock (_gate) return _draft?.State == "running"; } }
    public bool HasDraft { get { lock (_gate) return _draft != null; } }
    public string? CurrentDraftId { get { lock (_gate) return _draft?.DraftId; } }

    public (bool Ok, string Message) ValidateStart(string mode)
    {
        lock (_gate)
        {
            if (_draft?.State == "running") return (true, "清場掃描已在進行");
            if (mode == "new") return (true, "");
            if (mode != "append") return (false, "mode 必須是 new 或 append");
            if (_draft == null) return (false, "沒有可接續的掃描草稿");
            if (!IsCompatibleLocked()) return (false, "草稿的場域校準已改變，不能接續或套用");
            return (true, "");
        }
    }

    public (bool Ok, string Message, bool AlreadyRunning) Start(string mode)
    {
        lock (_gate)
        {
            if (_draft?.State == "running") return (true, "清場掃描已在進行", true);
            var valid = ValidateStartLocked(mode);
            if (!valid.Ok) return (false, valid.Message, false);

            var now = DateTimeOffset.UtcNow;
            if (mode == "new")
            {
                _draft = new Draft
                {
                    Venue = _venue,
                    VenueSignature = _venueSignature,
                    DraftId = Guid.NewGuid().ToString("N"),
                    State = "running",
                    CreatedUtc = now,
                    UpdatedUtc = now,
                };
                _cells.Clear();
            }
            else
            {
                _draft!.State = "running";
                _draft.UpdatedUtc = now;
                _cells.Clear();
                foreach (var c in _draft.Cells) _cells[Pack(c.Face, c.Col, c.Row)] = c;
            }
            _runStartMs = _clockMs();
            MarkDirtyLocked(force: true);
            return (true, mode == "new" ? "清場掃描已開始" : "已接續清場掃描", false);
        }
    }

    public (bool Ok, string Message) Stop()
    {
        lock (_gate)
        {
            if (_draft == null) return (false, "目前沒有掃描草稿");
            if (_draft.State != "running") return (true, "清場掃描已停止");
            _draft.ElapsedMs = CurrentElapsedLocked();
            _draft.State = "stopped";
            _draft.UpdatedUtc = DateTimeOffset.UtcNow;
            MarkDirtyLocked(force: true);
        }

        return (true, "清場掃描已停止，互動已恢復");
    }

    public bool Observe(in RoutedPoint point)
    {
        lock (_gate)
        {
            if (_draft?.State != "running") return false;
            bool shouldLock = !_locks.IsLocked(point.FaceIndex, point.Col, point.Row);
            int key = Pack(point.FaceIndex, point.Col, point.Row);
            var now = DateTimeOffset.UtcNow;
            if (_cells.TryGetValue(key, out var cell))
            {
                cell.SignalCount++;
                cell.LastSeenUtc = now;
            }
            else
            {
                cell = new NoiseCaptureCell
                {
                    Face = point.FaceIndex,
                    Col = point.Col,
                    Row = point.Row,
                    SignalCount = 1,
                    FirstSeenUtc = now,
                    LastSeenUtc = now,
                };
                _cells.Add(key, cell);
                _draft.Cells.Add(cell);
                _draft.LastNewCellUtc = now;
                // 已存在的設定／手動鎖保留來源；只有未鎖格由本草稿即時臨時鎖定。
            }
            _draft.UpdatedUtc = now;
            MarkDirtyLocked();
            return shouldLock;
        }
    }

    public NoiseCaptureSnapshot? Snapshot()
    {
        lock (_gate)
        {
            if (_draft == null) return null;
            var cells = _draft.Cells
                .OrderBy(c => c.Face).ThenBy(c => c.Col).ThenBy(c => c.Row)
                .Select(c =>
                {
                    var entry = _locks.Find(c.Face, c.Col, c.Row);
                    long? lastAgo = entry is { LastDropMs: >= 0 }
                        ? Math.Max(0, _clockMs() - entry.LastDropMs)
                        : null;
                    return new NoiseCaptureCellView(c.Face, c.Col, c.Row, c.SignalCount,
                        c.FirstSeenUtc, c.LastSeenUtc, entry != null,
                        entry == null ? null : LockedCellFilter.SourceName(entry.Source),
                        entry?.CaptureId == _draft.DraftId,
                        entry?.Source == LockSource.Config,
                        entry?.DroppedSinceLock ?? 0, lastAgo);
                }).ToArray();
            return new NoiseCaptureSnapshot(CurrentVersion, _draft.Venue, _draft.VenueSignature,
                _draft.DraftId, _draft.State, IsCompatibleLocked(), CurrentElapsedLocked(),
                _draft.CreatedUtc, _draft.UpdatedUtc, _draft.LastNewCellUtc,
                _lastSavedUtc, _saveError ?? (_path == null ? "此服務未提供設定檔路徑，掃描草稿無法持久保存" : null), cells);
        }
    }

    public (bool Ok, string Message, int Changed) Apply(string draftId, IEnumerable<(int Face, int Col, int Row)> requested)
    {
        lock (_gate)
        {
            if (_draft?.DraftId != draftId) return (false, "草稿已變更，請重新載入", 0);
            if (_draft.State == "running") return (false, "掃描中不能重新套用草稿", 0);
            if (!IsCompatibleLocked()) return (false, "場域校準已改變，不能套用草稿", 0);
            int changed = 0;
            foreach (var pos in requested.Distinct())
            {
                if (!_cells.ContainsKey(Pack(pos.Face, pos.Col, pos.Row))) continue;
                if (!_locks.IsLocked(pos.Face, pos.Col, pos.Row) &&
                    _locks.TryAdd(pos.Face, pos.Col, pos.Row, LockSource.Manual,
                        nowMs: _clockMs(), captureId: _draft.DraftId)) changed++;
            }
            _draft.UpdatedUtc = DateTimeOffset.UtcNow;
            MarkDirtyLocked(force: true);
            return (true, $"已重新套用 {changed} 格臨時鎖", changed);
        }
    }

    public (bool Ok, string Message, int Changed) Undo(string draftId, IEnumerable<(int Face, int Col, int Row)> requested)
    {
        lock (_gate)
        {
            if (_draft?.DraftId != draftId) return (false, "草稿已變更，請重新載入", 0);
            if (_draft.State == "running") return (false, "掃描中不能解除本次鎖定", 0);
            int changed = 0;
            foreach (var pos in requested.Distinct())
            {
                var entry = _locks.Find(pos.Face, pos.Col, pos.Row);
                if (entry?.CaptureId == draftId && entry.Source == LockSource.Manual &&
                    _locks.TryRemove(pos.Face, pos.Col, pos.Row, out _)) changed++;
            }
            _draft.UpdatedUtc = DateTimeOffset.UtcNow;
            MarkDirtyLocked(force: true);
            return (true, $"已解除本次掃描的 {changed} 格臨時鎖", changed);
        }
    }

    public (bool Ok, string Message, int Changed) MarkSaved(string draftId, IEnumerable<(int Face, int Col, int Row)> requested)
    {
        lock (_gate)
        {
            if (_draft?.DraftId != draftId) return (false, "草稿已變更，請重新載入", 0);
            if (_draft.State == "running") return (false, "掃描中不能保存鎖定", 0);
            if (!IsCompatibleLocked()) return (false, "場域校準已改變，不能保存草稿鎖定", 0);
            int changed = 0;
            foreach (var pos in requested.Distinct())
            {
                if (!_cells.ContainsKey(Pack(pos.Face, pos.Col, pos.Row))) continue;
                var entry = _locks.Find(pos.Face, pos.Col, pos.Row);
                if (entry == null || entry.CaptureId != draftId || entry.Source == LockSource.Config) continue;
                if (_locks.Reclassify(pos.Face, pos.Col, pos.Row, LockSource.Config)) changed++;
            }
            _draft.UpdatedUtc = DateTimeOffset.UtcNow;
            MarkDirtyLocked(force: true);
            return (true, $"已標記 {changed} 格為已保存", changed);
        }
    }

    public (bool Ok, string Message) Clear(string draftId)
    {
        lock (_gate)
        {
            if (_draft?.DraftId != draftId) return (false, "草稿已變更，請重新載入");
            if (_draft.State == "running") return (false, "請先停止掃描再清除草稿");
            _draft = null;
            _cells.Clear();
            MarkDirtyLocked(force: true);
        }
        return FlushNow(TimeSpan.FromSeconds(5))
            ? (true, "掃描草稿已清除；現有鎖定不變")
            : (false, "草稿清除尚未保存到磁碟，請檢查狀態訊息");
    }

    public bool FlushNow(TimeSpan timeout)
    {
        long target;
        lock (_gate)
        {
            if (!_dirty) return true;
            target = _version;
            _forceWrite = true;
            Monitor.PulseAll(_gate);
            var until = Stopwatch.GetTimestamp() + (long)(timeout.TotalSeconds * Stopwatch.Frequency);
            while (_dirty && _version == target && !_shutdown)
            {
                long leftTicks = until - Stopwatch.GetTimestamp();
                if (leftTicks <= 0) return false;
                int leftMs = Math.Max(1, (int)(leftTicks * 1000 / Stopwatch.Frequency));
                Monitor.Wait(_gate, leftMs);
            }
            return !_dirty && _saveError == null;
        }
    }

    void LoadDraft()
    {
        if (_path == null) return;
        foreach (var path in new[] { _path, _path + ".bak" })
        {
            if (!File.Exists(path)) continue;
            try
            {
                var draft = JsonSerializer.Deserialize<Draft>(File.ReadAllText(path), JsonOptions);
                if (draft == null || draft.Version != CurrentVersion || draft.Cells == null) continue;
                _draft = draft;
                foreach (var c in draft.Cells)
                    if (c.Face >= 0 && c.Face < 2048 && c.Col >= 0 && c.Col < 1024 && c.Row >= 0 && c.Row < 1024)
                        _cells[Pack(c.Face, c.Col, c.Row)] = c;
                if (_draft.State == "running")
                {
                    _draft.State = "interrupted";
                    _draft.UpdatedUtc = DateTimeOffset.UtcNow;
                    MarkDirtyLocked(force: true);
                }
                _lastSavedUtc = File.GetLastWriteTimeUtc(path);
                if (path.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                    _log.Warn("[NoiseCapture] 主草稿無法讀取，已從備份載入");
                return;
            }
            catch (Exception ex)
            {
                _log.Warn($"[NoiseCapture] 草稿讀取失敗（{Path.GetFileName(path)}）：{ex.Message}");
            }
        }
    }

    void WriterLoop()
    {
        while (true)
        {
            Draft? snapshot;
            long version;
            lock (_gate)
            {
                while (!_shutdown && !_dirty) Monitor.Wait(_gate);
                if (_shutdown && !_dirty) return;

                if (!_shutdown && !_forceWrite)
                {
                    long elapsedMs = _lastWriteTimestamp == 0 ? SaveIntervalMs :
                        (Stopwatch.GetTimestamp() - _lastWriteTimestamp) * 1000 / Stopwatch.Frequency;
                    if (elapsedMs < SaveIntervalMs)
                    {
                        Monitor.Wait(_gate, SaveIntervalMs - (int)elapsedMs);
                        continue;
                    }
                }

                snapshot = CloneDraftLocked();
                version = _version;
                _forceWrite = false;
            }

            if (_path == null)
            {
                lock (_gate)
                {
                    _dirty = false;
                    _saveError = "此服務未提供設定檔路徑，掃描草稿無法持久保存";
                    Monitor.PulseAll(_gate);
                }
                continue;
            }

            string? error = null;
            try { WriteSnapshot(snapshot); }
            catch (Exception ex) { error = ex.Message; }

            lock (_gate)
            {
                _lastWriteTimestamp = Stopwatch.GetTimestamp();
                if (error == null)
                {
                    if (_version == version) _dirty = false;
                    _lastSavedUtc = DateTimeOffset.UtcNow;
                    _saveError = null;
                }
                else
                {
                    _saveError = error;
                    _log.Error($"[NoiseCapture] 草稿保存失敗：{error}");
                    // 保留 dirty，下一輪重試；不要阻塞 UDP 收包執行緒。
                }
                Monitor.PulseAll(_gate);
                if (_shutdown && error != null) return;
            }
        }
    }

    void WriteSnapshot(Draft? snapshot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path!)!);
        if (snapshot == null)
        {
            File.Delete(_path!);
            File.Delete(_path + ".bak");
            File.Delete(_path + ".tmp");
            return;
        }
        string temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(snapshot, JsonOptions), new UTF8Encoding(false));
        if (File.Exists(_path)) File.Copy(_path, _path + ".bak", overwrite: true);
        File.Move(temp, _path! , overwrite: true);
    }

    Draft? CloneDraftLocked()
    {
        if (_draft == null) return null;
        return new Draft
        {
            Version = _draft.Version,
            Venue = _draft.Venue,
            VenueSignature = _draft.VenueSignature,
            DraftId = _draft.DraftId,
            State = _draft.State,
            ElapsedMs = CurrentElapsedLocked(),
            CreatedUtc = _draft.CreatedUtc,
            UpdatedUtc = DateTimeOffset.UtcNow,
            LastNewCellUtc = _draft.LastNewCellUtc,
            Cells = _draft.Cells.Select(c => new NoiseCaptureCell
            {
                Face = c.Face, Col = c.Col, Row = c.Row, SignalCount = c.SignalCount,
                FirstSeenUtc = c.FirstSeenUtc, LastSeenUtc = c.LastSeenUtc,
            }).ToList(),
        };
    }

    (bool Ok, string Message) ValidateStartLocked(string mode)
    {
        if (mode == "new") return (true, "");
        if (mode != "append") return (false, "mode 必須是 new 或 append");
        if (_draft == null) return (false, "沒有可接續的掃描草稿");
        if (!IsCompatibleLocked()) return (false, "草稿的場域校準已改變，不能接續或套用");
        return (true, "");
    }

    bool IsCompatibleLocked() => _draft != null &&
        string.Equals(_draft.VenueSignature, _venueSignature, StringComparison.Ordinal);

    long CurrentElapsedLocked() => _draft == null ? 0 :
        _draft.ElapsedMs + (_draft.State == "running" ? Math.Max(0, _clockMs() - _runStartMs) : 0);

    void MarkDirtyLocked(bool force = false)
    {
        _dirty = true;
        _version++;
        _forceWrite |= force;
        Monitor.PulseAll(_gate);
    }

    static int Pack(int face, int col, int row) => (face << 20) | (col << 10) | row;

    public void Dispose()
    {
        lock (_gate)
        {
            if (_draft?.State == "running")
            {
                _draft.ElapsedMs = CurrentElapsedLocked();
                _draft.State = "interrupted";
                _draft.UpdatedUtc = DateTimeOffset.UtcNow;
                MarkDirtyLocked(force: true);
            }
            _shutdown = true;
            _forceWrite = true;
            Monitor.PulseAll(_gate);
        }
        if (!_writer.Join(TimeSpan.FromSeconds(5)))
            _log.Warn("[NoiseCapture] 草稿保存執行緒未能在關閉期限內完成");
    }
}
