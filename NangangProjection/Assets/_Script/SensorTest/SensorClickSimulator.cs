using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 將硬體感應器的 UDP 座標，轉換為 Display 2 上的 Windows OS 等級滑鼠點擊。
/// 掛在 sensor_test 場景的任意 GameObject 上即可運作（不依賴 SensorTestManager）。
///
/// 工作流程：
///   UDP 封包 → rawX/rawY → 換算格子 col/row → 格子中心像素 →
///   加上 Display 2 在 Windows 桌面的偏移 → SetCursorPos + mouse_event（真實 OS 點擊）
///
/// 適用場景：投影地板後方開啟瀏覽器、網頁或任何 Windows 程式，
/// 踩踏感應區域即可觸發對應程式的點擊事件。
/// </summary>
public class SensorClickSimulator : MonoBehaviour
{
    [Header("座標校準（與 SensorTestManager 一致）")]
    public float rawMinX = 1928f;
    public float rawMaxX = 3960f;
    public float rawMinY = 16f;
    public float rawMaxY = 752f;

    [Header("Display 2 在 Windows 桌面的起始位置")]
    [Tooltip("Display 2 左上角的 Windows 絕對座標。\n通常 X = Display 1 的寬度（預設 1920），Y = 0。\n可在 Windows 顯示設定 → 識別中確認。")]
    public int display2OffsetX = 1920;
    public int display2OffsetY = 0;

    [Header("點擊冷卻（秒）")]
    [Tooltip("兩次點擊之間的最短間隔，防止同一區域連續觸發。")]
    public float clickCooldown = 1f;

    // 格子規格（固定，與硬體一致）
    const int Cols  = 128;
    const int Rows  = 24;
    const int CellW = 16;   // 每格 16 px（橫）
    const int CellH = 32;   // 每格 32 px（縱）

    float _lastClickTime = float.NegativeInfinity;

    // ── Windows API ──────────────────────────────────────────────────────────

    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP   = 0x0004;

    // ── 生命週期 ──────────────────────────────────────────────────────────────

    void OnEnable()  => UdpSensorReceiver.OnPacketReceived += OnUdpPacket;
    void OnDisable() => UdpSensorReceiver.OnPacketReceived -= OnUdpPacket;

    // ── 感應處理 ──────────────────────────────────────────────────────────────

    void OnUdpPacket(int rawX, int rawY)
    {
        // 冷卻檢查
        if (Time.time - _lastClickTime < clickCooldown) return;

        // 正規化（0~1）
        float nx = Mathf.InverseLerp(rawMinX, rawMaxX, rawX);
        float ny = Mathf.InverseLerp(rawMinY, rawMaxY, rawY);

        // 過濾明顯超出範圍的雜訊
        if (nx < -0.1f || nx > 1.1f || ny < -0.1f || ny > 1.1f) return;

        // 換算格子座標
        int col = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(nx) * Cols), 0, Cols - 1);
        int row = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(ny) * Rows), 0, Rows - 1);

        // 格子中心的 Display 2 本地像素座標
        int localX = col * CellW + CellW / 2;   // 例：col=0 → 8
        int localY = row * CellH + CellH / 2;   // 例：row=0 → 16

        // 加上 Windows 桌面偏移 → 絕對座標
        int absX = display2OffsetX + localX;
        int absY = display2OffsetY + localY;

        _lastClickTime = Time.time;

        // 移動滑鼠並模擬點擊
        SetCursorPos(absX, absY);
        mouse_event(MOUSEEVENTF_LEFTDOWN, absX, absY, 0, 0);
        mouse_event(MOUSEEVENTF_LEFTUP,   absX, absY, 0, 0);

        Debug.Log($"[SensorClick] raw({rawX},{rawY}) → 格({col},{row}) → 螢幕({absX},{absY})");
    }
}
