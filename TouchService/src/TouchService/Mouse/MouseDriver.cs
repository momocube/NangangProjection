using TouchService.Config;
using TouchService.Core;
using TouchService.Logging;
using TouchService.Tracking;

namespace TouchService.Mouse;

/// <summary>
/// 滑鼠模式 sink：訂閱 TouchPipeline 的 typed 事件流（ITouchEventObserver），
/// 把 mouseMode.face 那一面的觸點轉成 OS 滑鼠操作。
/// **額外 sink、不是取代**——grid 的 WS/UDP 發布完全不受模式影響
/// （Unity 時代「滑鼠模擬取代發布」正是這個服務要逃離的架構）。
///
/// 行為：
///   - 只看 mouseMode.face 那一面的觸點；其他面（含地板）一律忽略
///   - 單點所有權：該面第一個 down 的 touch 成為 owner，它 up 前其他觸點忽略
///     （OS 游標只有一顆，多點搶游標會亂跳）
///   - down → 游標直接跳到映射位置（先到位）再 LEFTDOWN（順序不能反，否則點到舊位置）
///   - move → 只更新目標位置；實際移動由 8ms 平滑迴圈指數逼近（factor = smoothing）
///   - up → 只 LEFTUP，游標停在原地（目標凍結，平滑迴圈停止）
///   - 單步位移 &lt; moveMinPx 不注入（避免 125Hz 微步轟炸；收斂到位的最後一步除外）
///
/// 80ms 封包間隔不是問題：touch 生命週期已由 release timeout 橋接，owner 不會中途假死。
/// 執行緒模型：OnTouchEvents 在 pipeline 鎖內被呼叫（receive / timer 執行緒）、
/// TickSmoothing 在自己的 8ms 執行緒；內部以單一 _gate 序列化。注入呼叫都很快（SendInput）。
/// </summary>
public sealed class MouseDriver : ITouchEventObserver, IDisposable
{
    readonly object _gate = new();
    readonly IMouseInjector _injector;
    readonly IReadOnlyList<SensorFaceConfig> _faces;
    readonly FileLogger _log;
    readonly CancellationTokenSource _cts = new();

    MouseModeConfig _cfg;
    Thread? _smoothThread;

    int _ownerId = -1;        // 目前持有游標的 touch ID（-1 = 無）
    bool _buttonDown;
    double _curX, _curY;      // 平滑迴圈的內部游標位置（浮點累積）
    double _targetX, _targetY;
    int _lastInjX, _lastInjY; // 最後注入的整數座標（moveMinPx 門檻基準）

    public MouseDriver(IMouseInjector injector, IReadOnlyList<SensorFaceConfig> faces,
                       MouseModeConfig cfg, FileLogger log)
    {
        _injector = injector;
        _faces = faces;
        _cfg = cfg;
        _log = log;
    }

    /// <summary>啟動 8ms 平滑迴圈執行緒（測試不啟動，直接呼叫 TickSmoothing 確定性驅動）。</summary>
    public void Start()
    {
        if (_smoothThread != null) return;
        _smoothThread = new Thread(() =>
        {
            while (!_cts.IsCancellationRequested)
            {
                Thread.Sleep(8);
                try { TickSmoothing(); }
                catch (Exception ex) { _log.Error($"[Mouse] 平滑迴圈失敗：{ex.Message}"); }
            }
        }) { IsBackground = true, Name = "MouseSmooth" };
        _smoothThread.Start();
    }

    /// <summary>
    /// 熱套用滑鼠參數（POST /config）。face 變更時若 owner 還按著先強制放鍵——
    /// 否則 owner 的後續事件全變成「非目標面」被忽略，按鍵會卡死。
    /// </summary>
    public void UpdateConfig(MouseModeConfig cfg)
    {
        lock (_gate)
        {
            if (cfg.Face != _cfg.Face && _buttonDown) ReleaseLocked();
            _cfg = cfg;
        }
    }

    /// <summary>
    /// 強制釋放：mouse→grid 熱切 / Dispose 時呼叫。owner 還按著就 LEFTUP，不留卡死的按鍵。
    /// </summary>
    public void ForceRelease()
    {
        lock (_gate)
        {
            if (_buttonDown) ReleaseLocked();
            else _ownerId = -1;
        }
    }

    // ── ITouchEventObserver（pipeline 鎖內呼叫，不得保留 events 參考）────────

    public void OnTouchEvents(IReadOnlyList<TouchEventData> events)
    {
        lock (_gate)
        {
            var cfg = _cfg;
            if (cfg.Face < 0 || cfg.Face >= _faces.Count) return;   // 防呆（apply 已驗證）

            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e.Point.FaceIndex != cfg.Face) continue;   // 非目標面（含地板）一律忽略

                switch (e.Phase)
                {
                    case TouchPhase.Down:
                        if (_ownerId >= 0) break;              // 單點所有權：owner 在，其他觸點忽略
                        _ownerId = e.Id;
                        (_targetX, _targetY) = MapToScreen(e.Point, cfg);
                        _curX = _targetX;
                        _curY = _targetY;
                        _lastInjX = (int)Math.Round(_curX);
                        _lastInjY = (int)Math.Round(_curY);
                        _injector.Move(_lastInjX, _lastInjY);  // down 第一下：直接跳到位…
                        _injector.LeftDown();                  // …再按下（順序不能反）
                        _buttonDown = true;
                        break;

                    case TouchPhase.Move:
                        if (e.Id != _ownerId) break;
                        (_targetX, _targetY) = MapToScreen(e.Point, cfg);   // 平滑迴圈負責移過去
                        break;

                    case TouchPhase.Up:
                        if (e.Id != _ownerId) break;
                        ReleaseLocked();                       // 游標停在原地，只放開按鍵
                        break;
                }
            }
        }
    }

    /// <summary>
    /// 平滑迴圈單步（production 由 8ms 執行緒驅動；測試直接呼叫）。
    /// 指數逼近：cur += (target - cur) * smoothing；剩餘 &lt; 0.5px 直接收斂到位。
    /// 注入門檻：與上次注入位置差 &lt; moveMinPx 不注入（到位的最後一步除外，保證收斂精確）。
    /// </summary>
    public void TickSmoothing()
    {
        lock (_gate)
        {
            if (!_buttonDown) return;   // up 後游標停在原地

            double dx = _targetX - _curX;
            double dy = _targetY - _curY;
            if (dx == 0 && dy == 0) return;

            if (dx * dx + dy * dy < 0.25)   // 剩餘 < 0.5px：截斷指數尾巴，直接到位
            {
                _curX = _targetX;
                _curY = _targetY;
            }
            else
            {
                double s = _cfg.Smoothing;
                _curX += dx * s;
                _curY += dy * s;
            }

            int rx = (int)Math.Round(_curX);
            int ry = (int)Math.Round(_curY);
            int ix = rx - _lastInjX;
            int iy = ry - _lastInjY;
            if (ix == 0 && iy == 0) return;

            bool atTarget = _curX == _targetX && _curY == _targetY;
            int minPx = _cfg.MoveMinPx;
            if (!atTarget && ix * ix + iy * iy < minPx * minPx) return;   // 位移 < moveMinPx 不注入

            _lastInjX = rx;
            _lastInjY = ry;
            _injector.Move(rx, ry);
        }
    }

    /// <summary>面內座標（px - face origin → 0~1）→ mouseMode.screen 矩形內的螢幕座標。</summary>
    (double X, double Y) MapToScreen(in RoutedPoint p, MouseModeConfig cfg)
    {
        var face = _faces[cfg.Face];
        double nx = face.TotalPixelWidth  > 0 ? (p.PixelX - face.OriginX) / (double)face.TotalPixelWidth  : 0;
        double ny = face.TotalPixelHeight > 0 ? (p.PixelY - face.OriginY) / (double)face.TotalPixelHeight : 0;
        nx = Math.Clamp(nx, 0, 1);
        ny = Math.Clamp(ny, 0, 1);
        return (cfg.Screen.X + nx * cfg.Screen.Width, cfg.Screen.Y + ny * cfg.Screen.Height);
    }

    void ReleaseLocked()   // 持 _gate 呼叫
    {
        _injector.LeftUp();
        _buttonDown = false;
        _ownerId = -1;
        _targetX = _curX;   // 凍結目標：up 後平滑迴圈不再動游標
        _targetY = _curY;
    }

    public void Dispose()
    {
        _cts.Cancel();
        ForceRelease();   // 服務關閉不留按住的按鍵
        _smoothThread?.Join(200);
    }
}
