using System.Text;
using UnityEngine;

/// <summary>
/// 執行時切換各功能元件的開關，用於現場 narrow frame drop 兇手。
/// 配合 LatencyProbe 使用：切換配置後按 F8 dump、把不同配置的 CSV 給 Luna 比對。
///
/// 鍵位：
///   F1  → ConsoleGameOverlay        切換（Display 1 控制台後方預覽）
///   F2  → Display2DimOverlay        切換（地板亮度疊加）
///   F3  → Display2ScreenCapture     切換整個 GameObject（GDI 截圖、最重嫌疑）
///   F4  → MiniWindowController      切換
///   F5  → VirtualCellManager        切換 component enabled
///   F6  → 一鍵全關（baseline 對照）
///   F7  → 一鍵全開（回復原狀）
///   F8  → LatencyProbe dump（由 LatencyProbe 自己處理、這裡不重複）
///   F12 → 顯示 / 隱藏狀態 OnGUI
///
/// 使用流程：
///   1. F7 全開（baseline）→ 跑 10 秒 → F8 dump → CSV 命名 baseline
///   2. F6 全關 → 跑 10 秒 → F8 dump → CSV 命名 all-off
///   3. F7 全開 → F1 關 ConsoleGameOverlay → 跑 10 秒 → F8 dump → 命名 no-overlay
///   4. F1 開回、F2 關 → 跑 10 秒 → F8 dump → 命名 no-dim
///   ... 依序測完每一個
///   5. 把所有 CSV 給 Luna 比對 deltaTime
/// </summary>
public class RuntimeToggle : MonoBehaviour
{
    [Header("執行時切換 panel")]
    [Tooltip("關閉時所有按鍵不生效")]
    public bool toggleEnabled = true;

    [Tooltip("啟動時顯示 OnGUI 狀態 panel")]
    public bool showStatusOnStart = true;

    bool _showStatus;

    void Awake()
    {
        _showStatus = showStatusOnStart;
        Debug.Log("[RuntimeToggle] init. F1-F7 切換、F12 顯示狀態、F8 LatencyProbe dump");
    }

    void Update()
    {
        if (!toggleEnabled) return;

        if (Input.GetKeyDown(KeyCode.F1)) ToggleComponent<ConsoleGameOverlay>("ConsoleGameOverlay");
        if (Input.GetKeyDown(KeyCode.F2)) ToggleComponent<Display2DimOverlay>("Display2DimOverlay");
        if (Input.GetKeyDown(KeyCode.F3)) ToggleGameObject<Display2ScreenCapture>("Display2ScreenCapture");
        if (Input.GetKeyDown(KeyCode.F4)) ToggleComponent<MiniWindowController>("MiniWindowController");
        if (Input.GetKeyDown(KeyCode.F5)) ToggleComponent<VirtualCellManager>("VirtualCellManager");
        if (Input.GetKeyDown(KeyCode.F6)) AllOff();
        if (Input.GetKeyDown(KeyCode.F7)) AllOn();
        if (Input.GetKeyDown(KeyCode.F12)) _showStatus = !_showStatus;
    }

    // ── 切換工具 ──────────────────────────────────────────────────────────────

    static T FindAny<T>() where T : Component
    {
        // 用 Resources.FindObjectsOfTypeAll 也找得到 inactive GO 上的 component
        var all = Resources.FindObjectsOfTypeAll<T>();
        foreach (var x in all)
            if (x.gameObject.scene.IsValid())  // 過濾 prefab asset
                return x;
        return null;
    }

    void ToggleComponent<T>(string name) where T : Behaviour
    {
        var c = FindAny<T>();
        if (c == null) { Debug.LogWarning($"[RuntimeToggle] {name} 在場景中找不到"); return; }
        c.enabled = !c.enabled;
        Debug.Log($"[RuntimeToggle] {name}.enabled = {c.enabled}");
    }

    void ToggleGameObject<T>(string name) where T : Component
    {
        var c = FindAny<T>();
        if (c == null) { Debug.LogWarning($"[RuntimeToggle] {name} 在場景中找不到"); return; }
        var go = c.gameObject;
        go.SetActive(!go.activeSelf);
        Debug.Log($"[RuntimeToggle] {name} GameObject.activeSelf = {go.activeSelf}");
    }

    void AllOff()
    {
        SetComponent<ConsoleGameOverlay>(false);
        SetComponent<Display2DimOverlay>(false);
        SetGameObject<Display2ScreenCapture>(false);
        SetComponent<MiniWindowController>(false);
        SetComponent<VirtualCellManager>(false);
        Debug.Log("[RuntimeToggle] ALL OFF");
    }

    void AllOn()
    {
        SetComponent<ConsoleGameOverlay>(true);
        SetComponent<Display2DimOverlay>(true);
        SetGameObject<Display2ScreenCapture>(true);
        SetComponent<MiniWindowController>(true);
        SetComponent<VirtualCellManager>(true);
        Debug.Log("[RuntimeToggle] ALL ON");
    }

    void SetComponent<T>(bool on) where T : Behaviour
    {
        var c = FindAny<T>();
        if (c != null) c.enabled = on;
    }

    void SetGameObject<T>(bool on) where T : Component
    {
        var c = FindAny<T>();
        if (c != null) c.gameObject.SetActive(on);
    }

    // ── OnGUI 狀態 panel ──────────────────────────────────────────────────────

    GUIStyle _labelStyle;

    void OnGUI()
    {
        if (!_showStatus) return;

        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 14;
            _labelStyle.normal.textColor = Color.white;
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== RuntimeToggle (F12 hide) ===");
        sb.AppendLine($"F1 ConsoleGameOverlay     : {StatusComp<ConsoleGameOverlay>()}");
        sb.AppendLine($"F2 Display2DimOverlay     : {StatusComp<Display2DimOverlay>()}");
        sb.AppendLine($"F3 Display2ScreenCapture  : {StatusGO<Display2ScreenCapture>()}");
        sb.AppendLine($"F4 MiniWindowController   : {StatusComp<MiniWindowController>()}");
        sb.AppendLine($"F5 VirtualCellManager     : {StatusComp<VirtualCellManager>()}");
        sb.AppendLine("F6 ALL OFF   F7 ALL ON   F8 dump CSV");
        sb.AppendLine($"FPS: {(1f / Mathf.Max(0.001f, Time.smoothDeltaTime)):F0}  deltaTime: {Time.deltaTime * 1000f:F1}ms");

        // 黑底
        var bg = new Rect(10, Screen.height - 200, 500, 180);
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(bg, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(20, Screen.height - 195, 480, 170), sb.ToString(), _labelStyle);
    }

    static string StatusComp<T>() where T : Behaviour
    {
        var c = FindAny<T>();
        if (c == null) return "<not found>";
        return c.enabled ? "ON" : "OFF";
    }

    static string StatusGO<T>() where T : Component
    {
        var c = FindAny<T>();
        if (c == null) return "<not found>";
        return c.gameObject.activeSelf ? "GO ON" : "GO OFF";
    }
}
