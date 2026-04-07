using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 在 Windows 執行檔啟動時自動彈出一個命令列視窗，
/// 將所有 Debug.Log / Warning / Error 輸出到其中。
/// Editor 裡不作用（自動跳過）。
/// </summary>
public class GameConsole : MonoBehaviour
{
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
    [DllImport("kernel32.dll")] static extern bool AllocConsole();
    [DllImport("kernel32.dll")] static extern bool SetConsoleTitle(string title);
#endif

    [Tooltip("視窗標題")]
    public string windowTitle = "UDP Debug Console";

    void Awake()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        AllocConsole();
        SetConsoleTitle(windowTitle);

        // AllocConsole 後必須重新綁定 stdout，否則畫面空白
        var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        Console.SetOut(writer);

        // 把 Unity 所有 log 轉到 Console 視窗
        Application.logMessageReceived += OnLog;

        Console.WriteLine("=== UDP Debug Console 已啟動 ===");
        Console.WriteLine($"監聽 UDP port：20001");
        Console.WriteLine("================================\n");
#endif
    }

    void OnDestroy()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        Application.logMessageReceived -= OnLog;
#endif
    }

    static void OnLog(string message, string stackTrace, LogType type)
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        string prefix = type switch
        {
            LogType.Error   => "[ERROR] ",
            LogType.Warning => "[WARN]  ",
            _               => "[LOG]   "
        };

        Console.ForegroundColor = type switch
        {
            LogType.Error   => ConsoleColor.Red,
            LogType.Warning => ConsoleColor.Yellow,
            _               => ConsoleColor.Cyan
        };

        Console.WriteLine(prefix + message);
        Console.ResetColor();
#endif
    }
}
