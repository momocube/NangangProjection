using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 場景初始設定視窗。
/// 選單：NangangProjection → 場景初始設定
///
/// 一次集中顯示各元件需在 Play 前確認的欄位，
/// 直接修改即存，支援 Undo。
/// </summary>
public class NangangSetupWindow : EditorWindow
{
    [MenuItem("NangangProjection/場景初始設定", false, 100)]
    public static void ShowWindow()
    {
        var win = GetWindow<NangangSetupWindow>();
        win.titleContent = new GUIContent("初始設定");
        win.minSize = new Vector2(390, 520);
        win.Refresh();
    }

    // ── 元件 ─────────────────────────────────────────────────────────────────

    SensorTestManager         _stm;
    TouchDebugLogger          _logger;
    SensorClickSimulator      _clickSim;
    UdpSensorReceiver         _udp;
    ConsoleGameOverlay        _overlay;
    VirtualCellManager        _vcell;
    SensorBridgeToPassPosition _bridge;

    // ── Serialized Objects ────────────────────────────────────────────────────

    SerializedObject _stmSO, _loggerSO, _clickSimSO, _udpSO, _overlaySO, _vcellSO, _bridgeSO;

    readonly List<(VenuePresetConfig preset, SerializedObject so)> _presetSOs
        = new List<(VenuePresetConfig, SerializedObject)>();

    // ── 折疊狀態 ─────────────────────────────────────────────────────────────

    bool _foldUdp     = true;
    bool _foldVenue   = true;
    bool _foldScene   = true;
    bool _foldDisplay = true;
    bool _foldLog     = true;
    bool _foldVCell   = true;
    bool _foldBridge  = false;  // 較少改動，預設收起
    bool _foldClick   = false;

    Vector2 _scroll;
    double  _refreshTime = -999;

    // ── 生命週期 ──────────────────────────────────────────────────────────────

    void OnFocus()  => Refresh();
    void OnEnable() => EditorApplication.hierarchyChanged += OnHierarchyChanged;
    void OnDisable() => EditorApplication.hierarchyChanged -= OnHierarchyChanged;
    void OnHierarchyChanged() => Refresh();

    void Refresh()
    {
        _stm      = FindObjectOfType<SensorTestManager>(true);
        _logger   = FindObjectOfType<TouchDebugLogger>(true);
        _clickSim = FindObjectOfType<SensorClickSimulator>(true);
        _udp      = FindObjectOfType<UdpSensorReceiver>(true);
        _overlay  = FindObjectOfType<ConsoleGameOverlay>(true);
        _vcell    = FindObjectOfType<VirtualCellManager>(true);
        _bridge   = FindObjectOfType<SensorBridgeToPassPosition>(true);

        _stmSO      = _stm      != null ? new SerializedObject(_stm)      : null;
        _loggerSO   = _logger   != null ? new SerializedObject(_logger)   : null;
        _clickSimSO = _clickSim != null ? new SerializedObject(_clickSim) : null;
        _udpSO      = _udp      != null ? new SerializedObject(_udp)      : null;
        _overlaySO  = _overlay  != null ? new SerializedObject(_overlay)  : null;
        _vcellSO    = _vcell    != null ? new SerializedObject(_vcell)    : null;
        _bridgeSO   = _bridge   != null ? new SerializedObject(_bridge)   : null;

        _presetSOs.Clear();
        if (_stm != null && _stm.presets != null)
            foreach (var p in _stm.presets)
                if (p != null) _presetSOs.Add((p, new SerializedObject(p)));

        _refreshTime = EditorApplication.timeSinceStartup;
        Repaint();
    }

    // ── 主繪製 ────────────────────────────────────────────────────────────────

    void OnGUI()
    {
        // Toolbar
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("NangangProjection 初始設定", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("↺ 重新整理", EditorStyles.toolbarButton, GUILayout.Width(68)))
            Refresh();
        EditorGUILayout.EndHorizontal();

        if (_refreshTime > 0)
        {
            double age = EditorApplication.timeSinceStartup - _refreshTime;
            EditorGUILayout.LabelField($"上次掃描 {age:F0}s 前", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(2);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        // 更新全部 SO
        _stmSO?.Update(); _loggerSO?.Update(); _clickSimSO?.Update();
        _udpSO?.Update(); _overlaySO?.Update(); _vcellSO?.Update(); _bridgeSO?.Update();
        foreach (var (_, so) in _presetSOs) so.Update();

        DrawUdpSection();
        DrawVenueSection();
        DrawSceneSection();
        DrawDisplaySection();
        DrawLogSection();
        DrawVCellSection();
        DrawBridgeSection();
        DrawClickSection();

        EditorGUILayout.Space(8);
        EditorGUILayout.EndScrollView();

        // 套用所有修改
        _stmSO?.ApplyModifiedProperties(); _loggerSO?.ApplyModifiedProperties();
        _clickSimSO?.ApplyModifiedProperties(); _udpSO?.ApplyModifiedProperties();
        _overlaySO?.ApplyModifiedProperties(); _vcellSO?.ApplyModifiedProperties();
        _bridgeSO?.ApplyModifiedProperties();
        foreach (var (_, so) in _presetSOs) so.ApplyModifiedProperties();
    }

    // ── 各區段 ────────────────────────────────────────────────────────────────

    void DrawUdpSection()
    {
        if (!BeginSection("感應接收 / UDP", ref _foldUdp)) return;

        if (!HasComponent(_udp, "UdpSensorReceiver")) { EndSection(); return; }

        Prop(_udpSO, "listenPort", "監聽 Port（預設 20001）");
        Prop(_udpSO, "debugLog",   "封包 Debug Log（建議關閉，效能影響大）");

        EndSection();
    }

    void DrawVenueSection()
    {
        if (!BeginSection("場域預設", ref _foldVenue)) return;

        if (!HasComponent(_stm, "SensorTestManager")) { EndSection(); return; }

        Prop(_stmSO, "showPresetSelector", "顯示場域切換按鈕 ◀/▶（多場域時開啟）");

        EditorGUILayout.Space(4);

        if (_presetSOs.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "presets[] 為空！請在 SensorTestManager Inspector 設定至少一個 VenuePresetConfig。",
                MessageType.Error);
        }
        else
        {
            foreach (var (preset, so) in _presetSOs)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(preset.presetName, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                Prop(so, "virtualCellMode", "虛擬格模式");
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }

        EndSection();
    }

    void DrawSceneSection()
    {
        if (!BeginSection("遊戲場景", ref _foldScene)) return;

        if (!HasComponent(_stm, "SensorTestManager")) { EndSection(); return; }

        var nameProp = _stmSO.FindProperty("gameSceneName");
        EditorGUILayout.PropertyField(nameProp, new GUIContent("場景名稱"));

        string name = nameProp.stringValue;
        if (string.IsNullOrEmpty(name))
        {
            EditorGUILayout.HelpBox(
                "留空：不載入遊戲場景，以純感應測試模式執行。",
                MessageType.Info);
        }
        else
        {
            bool inBuild = IsSceneInBuild(name);
            EditorGUILayout.HelpBox(
                inBuild ? $"✓  '{name}' 已在 Build Settings 中。"
                        : $"⚠  '{name}' 未加入 Build Settings！執行時會跳過載入並印 Warning。",
                inBuild ? MessageType.Info : MessageType.Warning);

            if (!inBuild && GUILayout.Button("開啟 Build Settings"))
                EditorApplication.ExecuteMenuItem("File/Build Settings...");
        }

        EndSection();
    }

    void DrawDisplaySection()
    {
        if (!BeginSection("顯示設定", ref _foldDisplay)) return;

        if (!HasComponent(_stm, "SensorTestManager")) { EndSection(); return; }

        Prop(_stmSO, "_gameTileVisible",      "初始晶片示意（Display 2）開啟");
        Prop(_stmSO, "display2WindowOffsetX", "Display 2 螢幕 X 起始位置（px，預設 1920）");
        Prop(_stmSO, "activeDuration",         "晶片觸發亮紅持續時間（秒，預設 1.5）");

        if (_overlaySO != null)
        {
            EditorGUILayout.Space(2);
            using (new EditorGUI.DisabledScope(false))
                EditorGUILayout.LabelField("ConsoleGameOverlay", EditorStyles.miniLabel);
            Prop(_overlaySO, "overlayAlpha", "初始疊加不透明度（0=全透明，1=全顯示）");
        }

        EndSection();
    }

    void DrawLogSection()
    {
        if (!BeginSection("觸碰日誌（TouchDebugLogger）", ref _foldLog)) return;

        if (!HasComponent(_logger, "TouchDebugLogger")) { EndSection(); return; }

        Prop(_loggerSO, "showWindow",       "初始顯示 Log 視窗");
        Prop(_loggerSO, "toggleKey",        "切換視窗按鍵（預設 F9）");
        EditorGUILayout.Space(2);
        Prop(_loggerSO, "listenRawUdp",    "訂閱 UDP 原始座標（校準 rawMin/Max 用）");
        Prop(_loggerSO, "listenSensorBus", "訂閱 SensorBus 格子座標（正常使用時開啟）");

        EndSection();
    }

    void DrawVCellSection()
    {
        if (!BeginSection("虛擬格（VirtualCellManager）", ref _foldVCell)) return;

        if (_vcell == null)
        {
            EditorGUILayout.HelpBox(
                "VirtualCellManager 未掛載於場景中。\n" +
                "若需虛擬格功能，請在 SensorTestManager 的 GameObject 上加入此元件，\n" +
                "並在各 VenuePresetConfig 的「虛擬格模式」選擇 Size32 / Size64。",
                MessageType.Info);
            EndSection();
            return;
        }

        Prop(_vcellSO, "pollingIntervalMs",
            "硬體輪詢間隔（ms）\n超過 2× 此值無觸碰 = 離手（預設 100ms）");

        EndSection();
    }

    void DrawBridgeSection()
    {
        if (!BeginSection("感應橋接（SensorBridgeToPassPosition）", ref _foldBridge)) return;

        if (_bridge == null)
        {
            EditorGUILayout.HelpBox(
                "SensorBridgeToPassPosition 未在場景中找到。若遊戲場景使用水波 VFX，此元件需存在並正確設定。",
                MessageType.Info);
            EndSection();
            return;
        }

        EditorGUILayout.HelpBox("格子解析度需與 SensorTestManager 場域一致。", MessageType.None);
        Prop(_bridgeSO, "gridCols",     "格子欄數（colCount，預設 128）");
        Prop(_bridgeSO, "gridRows",     "格子列數（rowCount，預設 24）");
        Prop(_bridgeSO, "displayWidth", "Display 2 寬度（px，預設 2048）");
        Prop(_bridgeSO, "displayHeight","Display 2 高度（px，預設 768）");
        EditorGUILayout.Space(2);
        Prop(_bridgeSO, "touchTimeout",           "離手逾時（秒）");
        Prop(_bridgeSO, "matchRadiusCells",       "配對半徑（格子單位）");
        Prop(_bridgeSO, "pressReleaseHoldSeconds","按壓結束淡出（秒）");

        EndSection();
    }

    void DrawClickSection()
    {
        if (!BeginSection("點擊模擬（SensorClickSimulator）", ref _foldClick)) return;

        if (!HasComponent(_clickSim, "SensorClickSimulator")) { EndSection(); return; }

        Prop(_clickSimSO, "_mode",          "初始點擊模式（內部監聽 / PostMessage / 滑鼠點擊）");
        Prop(_clickSimSO, "cooldownEnabled","冷卻啟用");
        Prop(_clickSimSO, "clickCooldown",  "冷卻時間（秒，0.1 ～ 5.0）");

        EndSection();
    }

    // ── 共用工具 ──────────────────────────────────────────────────────────────

    /// <summary>折疊區段標頭。回傳 true 代表展開，須呼叫 EndSection()。</summary>
    static bool BeginSection(string label, ref bool foldout)
    {
        EditorGUILayout.Space(2);
        foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, label);
        if (!foldout) EditorGUILayout.EndFoldoutHeaderGroup();
        return foldout;
    }

    static void EndSection() => EditorGUILayout.EndFoldoutHeaderGroup();

    /// <summary>元件不存在時顯示警告並回傳 false。</summary>
    static bool HasComponent(Object obj, string typeName)
    {
        if (obj != null) return true;
        EditorGUILayout.HelpBox($"場景中找不到 {typeName}。", MessageType.Warning);
        return false;
    }

    /// <summary>顯示單一 SerializedProperty。找不到欄位時顯示警告。</summary>
    static void Prop(SerializedObject so, string propName, string label)
    {
        if (so == null) return;
        var prop = so.FindProperty(propName);
        if (prop != null)
            EditorGUILayout.PropertyField(prop, new GUIContent(label));
        else
            EditorGUILayout.HelpBox($"找不到序列化欄位：{propName}（可能已改名）", MessageType.Warning);
    }

    static bool IsSceneInBuild(string sceneName)
    {
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (Path.GetFileNameWithoutExtension(s.path) == sceneName)
                return true;
        }
        return false;
    }
}
