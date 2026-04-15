using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 觸碰訊號 Debug Logger，帶 Collapse 效果與每秒統計。
///
/// 掛在 sensor_test 場景任意 GameObject（建議掛在 SensorTestManager 上）。
/// 輸出格式方便複製給 AI 分析：
///
///   [T=12.34] TOUCH (col=64, row=12)  ×3  | Δ=16ms
///   [T=12.35] --- 1秒統計: 20次觸碰, 1個座標, 最高20次/秒 ---
/// </summary>
public class TouchDebugLogger : MonoBehaviour
{
    [Header("開關")]
    public bool enableLog = true;

    [Header("Collapse 設定")]
    [Tooltip("相同座標在此時間內（秒）視為同一筆，只印一次並累計次數")]
    public float collapseWindow = 0.1f;

    [Header("每秒統計")]
    [Tooltip("關閉可減少 Console 輸出量")]
    public bool showPerSecondStat = true;

    // ── 內部狀態 ────────────────────────────────────────────────────────────
    private struct CollapseEntry
    {
        public int   col, row;
        public int   count;
        public float firstTime;
        public float lastTime;
    }

    private readonly Dictionary<(int, int), CollapseEntry> _window
        = new Dictionary<(int, int), CollapseEntry>();

    // 每秒統計
    private int   _totalThisSec;
    private int   _peakThisSec;
    private float _secTimer;
    private readonly HashSet<(int,int)> _uniqueThisSec = new HashSet<(int,int)>();

    private void OnEnable()  => SensorBus.OnTouch += OnTouch;
    private void OnDisable() => SensorBus.OnTouch -= OnTouch;

    private void OnTouch(int col, int row)
    {
        if (!enableLog) return;

        float now = Time.time;
        var key = (col, row);

        // ── Collapse 邏輯 ──────────────────────────────────────────────
        if (_window.TryGetValue(key, out var entry))
        {
            if (now - entry.lastTime <= collapseWindow)
            {
                // 合併：只更新計數與時間，不重複印
                entry.count++;
                entry.lastTime = now;
                _window[key] = entry;
                _totalThisSec++;
                _uniqueThisSec.Add(key);
                return;
            }
            else
            {
                // 上一個 window 已過期，先把它印出來
                FlushEntry(entry);
            }
        }

        // 新條目
        _window[key] = new CollapseEntry
        {
            col = col, row = row,
            count = 1,
            firstTime = now,
            lastTime  = now
        };
        _totalThisSec++;
        _uniqueThisSec.Add(key);
    }

    private void Update()
    {
        if (!enableLog) return;

        float now = Time.time;

        // 把超過 collapseWindow 的條目印出來（確保不會永遠等待）
        var expired = new List<(int,int)>();
        foreach (var kv in _window)
        {
            if (now - kv.Value.lastTime > collapseWindow)
            {
                FlushEntry(kv.Value);
                expired.Add(kv.Key);
            }
        }
        foreach (var k in expired) _window.Remove(k);

        // 每秒統計
        _secTimer += Time.deltaTime;
        if (_peakThisSec < _totalThisSec) _peakThisSec = _totalThisSec;

        if (_secTimer >= 1f)
        {
            if (showPerSecondStat && _totalThisSec > 0)
            {
                Debug.Log(
                    $"[TouchLog] ── 1秒統計 ── " +
                    $"總觸發:{_totalThisSec}次  " +
                    $"不同座標:{_uniqueThisSec.Count}個  " +
                    $"≈{_totalThisSec}/秒"
                );
            }
            _totalThisSec  = 0;
            _uniqueThisSec.Clear();
            _secTimer -= 1f;
        }
    }

    private void FlushEntry(CollapseEntry e)
    {
        float durationMs = (e.lastTime - e.firstTime) * 1000f;

        if (e.count == 1)
        {
            Debug.Log(
                $"[TouchLog] T={e.firstTime:F2}s  " +
                $"TOUCH (col={e.col,3}, row={e.row,2})"
            );
        }
        else
        {
            Debug.Log(
                $"[TouchLog] T={e.firstTime:F2}s  " +
                $"TOUCH (col={e.col,3}, row={e.row,2})  " +
                $"×{e.count}  持續={durationMs:F0}ms  " +
                $"≈{e.count / Mathf.Max(e.lastTime - e.firstTime, 0.001f):F0}次/秒"
            );
        }
    }

    private void OnDestroy()
    {
        // 程式結束前把還在 window 的條目印出來
        foreach (var kv in _window)
            FlushEntry(kv.Value);
        _window.Clear();
    }
}
