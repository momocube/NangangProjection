using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 訊號記錄功能（partial）。
/// 按下「訊號記錄」按鈕後，記錄 recordDuration 秒的 UDP 封包、觸碰格子、
/// 模擬點擊事件，完成後自動寫入 txt 檔。
/// </summary>
public partial class SensorTestManager
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("訊號記錄")]
    [Tooltip("取消勾選可隱藏「訊號記錄」按鈕（不影響其他功能）。")]
    public bool  showLogButton     = true;
    [Tooltip("按下記錄按鈕後，自動記錄幾秒。")]
    public float logRecordDuration = 5f;

    // ── 執行期狀態 ─────────────────────────────────────────────────────────────

    TextMeshProUGUI _loggerBtnText;
    Image           _loggerBtnImage;

    bool            _logRecording;
    float           _logStartTime;
    readonly List<string> _logEntries = new List<string>();
    int             _logUdpCount;
    int             _logTouchCount;
    int             _logSimCount;
    Coroutine       _logCoroutine;

    // ── 按鈕回呼 ──────────────────────────────────────────────────────────────

    void OnToggleLog()
    {
        if (_logRecording) StopLog();
        else               StartLog();
    }

    // ── 記錄流程 ──────────────────────────────────────────────────────────────

    void StartLog()
    {
        if (_logRecording) return;

        _logRecording = true;
        _logStartTime = Time.realtimeSinceStartup;
        _logEntries.Clear();
        _logUdpCount = _logTouchCount = _logSimCount = 0;

        _logEntries.Add("=== 感應訊號記錄 ===");
        _logEntries.Add($"時間：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _logEntries.Add($"記錄時長：{logRecordDuration:F1} 秒");
        _logEntries.Add($"點擊模式：{(_clickSim != null ? _clickSim.ModeLabel : "無")}");
        _logEntries.Add("================================");

        UdpSensorReceiver.OnPacketReceived  += OnLogUdp;
        SensorBus.OnTouch                   += OnLogTouch;
        SensorClickSimulator.OnSimEvent     += OnLogSimEvent;

        _logCoroutine = StartCoroutine(LogCountdownCoroutine());
        UpdateLoggerButton();
        Debug.Log("[SensorLogger] 開始記錄...");
    }

    void StopLog()
    {
        if (!_logRecording) return;

        _logRecording = false;

        if (_logCoroutine != null) { StopCoroutine(_logCoroutine); _logCoroutine = null; }

        UdpSensorReceiver.OnPacketReceived  -= OnLogUdp;
        SensorBus.OnTouch                   -= OnLogTouch;
        SensorClickSimulator.OnSimEvent     -= OnLogSimEvent;

        WriteLogFile();
        UpdateLoggerButton();
    }

    IEnumerator LogCountdownCoroutine()
    {
        while (_logRecording)
        {
            float elapsed   = Time.realtimeSinceStartup - _logStartTime;
            float remaining = logRecordDuration - elapsed;
            if (remaining <= 0f) break;

            if (_loggerBtnText != null)
                _loggerBtnText.text = $"記錄 {Mathf.CeilToInt(remaining)}s";

            yield return new WaitForSecondsRealtime(0.2f);
        }

        StopLog();
    }

    // ── 事件收集 ──────────────────────────────────────────────────────────────

    void OnLogUdp(int rawX, int rawY)
    {
        if (!_logRecording) return;
        float t = Time.realtimeSinceStartup - _logStartTime;
        _logEntries.Add($"[{t:F3}]  UDP    rawX={rawX,-6}  rawY={rawY}");
        _logUdpCount++;
    }

    void OnLogTouch(int col, int row)
    {
        if (!_logRecording) return;
        float t = Time.realtimeSinceStartup - _logStartTime;
        _logEntries.Add($"[{t:F3}]  觸發   col={col,-4}  row={row}");
        _logTouchCount++;
    }

    void OnLogSimEvent(string desc)
    {
        if (!_logRecording) return;
        float t = Time.realtimeSinceStartup - _logStartTime;
        _logEntries.Add($"[{t:F3}]  模擬   {desc}");
        _logSimCount++;
    }

    // ── 寫檔 ──────────────────────────────────────────────────────────────────

    void WriteLogFile()
    {
        float elapsed = Time.realtimeSinceStartup - _logStartTime;
        _logEntries.Add("================================");
        _logEntries.Add(
            $"統計：UDP 封包 {_logUdpCount} 筆  |  觸發格子 {_logTouchCount} 次  |  模擬事件 {_logSimCount} 次  |  實際記錄 {elapsed:F2} 秒");

        // 輸出資料夾旁的 Log/ 子資料夾；Editor 模式回退至 persistentDataPath
#if UNITY_EDITOR
        string dir = Application.persistentDataPath;
#else
        string dir = Path.Combine(
            Path.GetDirectoryName(Application.dataPath) ?? Application.persistentDataPath,
            "Log");
#endif
        Directory.CreateDirectory(dir);

        var    now  = DateTime.Now;
        string name = $"Log_{now:yyyy-MM-dd}-{now:HH}h-{now:mm}m-{now:ss}s.txt";
        string path = Path.Combine(dir, name);

        try
        {
            File.WriteAllLines(path, _logEntries, System.Text.Encoding.UTF8);
            Debug.Log($"[SensorLogger] 已儲存：{path}");
            if (_statusText != null)
                _statusText.text = $"記錄完成 → {path}";
        }
        catch (Exception e)
        {
            Debug.LogError($"[SensorLogger] 寫檔失敗：{e.Message}");
        }
    }

    // ── 按鈕外觀 ──────────────────────────────────────────────────────────────

    void UpdateLoggerButton()
    {
        if (_loggerBtnText != null)
            _loggerBtnText.text = _logRecording ? "記錄中..." : "訊號記錄";
        if (_loggerBtnImage != null)
            _loggerBtnImage.color = _logRecording
                ? new Color(0.50f, 0.20f, 0.12f)   // 橘紅：進行中
                : new Color(0.15f, 0.18f, 0.25f);  // 預設
    }
}
