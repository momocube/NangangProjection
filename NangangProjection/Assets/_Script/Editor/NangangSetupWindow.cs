using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    SensorTestManager          _stm;
    TouchDebugLogger           _logger;
    SensorClickSimulator       _clickSim;
    UdpSensorReceiver          _udp;
    ConsoleGameOverlay         _overlay;
    VirtualCellManager         _vcell;
    SensorBridgeToPassPosition _bridge;
    Display2ScreenCapture      _display2Capture;
    CoordTestPanel             _coordTestPanel;
    GameConsole                _gameConsole;

    // ── Serialized Objects ────────────────────────────────────────────────────

    SerializedObject _stmSO, _loggerSO, _clickSimSO, _udpSO, _overlaySO, _vcellSO, _bridgeSO;
    SerializedObject _profileSO;

    readonly List<(VenuePresetConfig preset, SerializedObject so)> _presetSOs
        = new List<(VenuePresetConfig, SerializedObject)>();

    // ── 折疊狀態 ─────────────────────────────────────────────────────────────

    bool _foldFeature = true;
    bool _foldProfile = true;
    bool _foldVenue   = true;
    bool _foldScene   = true;

    // 場景設定檔內部子折疊
    bool _foldPerfSub      = true;
    bool _foldCommonUdp    = false;
    bool _foldCommonBridge = false;
    bool _foldCommonLog    = false;
    bool _foldCommonVCell  = false;
    bool _foldCommonClick  = false;

    SceneInitialSettingsProfile _profile;

    Vector2 _scroll;
    double  _refreshTime = -999;

    static GUIStyle _applyButtonStyle;
    static GUIStyle ApplyButtonStyle
        => _applyButtonStyle ??= new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };

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
        _display2Capture = FindObjectOfType<Display2ScreenCapture>(true);
        _coordTestPanel  = FindObjectOfType<CoordTestPanel>(true);
        _gameConsole     = FindObjectOfType<GameConsole>(true);

        _stmSO      = _stm      != null ? new SerializedObject(_stm)      : null;
        _loggerSO   = _logger   != null ? new SerializedObject(_logger)   : null;
        _clickSimSO = _clickSim != null ? new SerializedObject(_clickSim) : null;
        _udpSO      = _udp      != null ? new SerializedObject(_udp)      : null;
        _overlaySO  = _overlay  != null ? new SerializedObject(_overlay)  : null;
        _vcellSO    = _vcell    != null ? new SerializedObject(_vcell)    : null;
        _bridgeSO   = _bridge   != null ? new SerializedObject(_bridge)   : null;
        _profileSO  = _profile  != null ? new SerializedObject(_profile)  : null;

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

        _stmSO?.Update(); _loggerSO?.Update(); _clickSimSO?.Update();
        _udpSO?.Update(); _overlaySO?.Update(); _vcellSO?.Update(); _bridgeSO?.Update();
        _profileSO?.Update();
        foreach (var (_, so) in _presetSOs) so.Update();

        DrawFeatureToggleSection();
        DrawProfileSection();
        DrawVenueSection();
        DrawSceneSection();

        EditorGUILayout.Space(8);
        EditorGUILayout.EndScrollView();

        _stmSO?.ApplyModifiedProperties(); _loggerSO?.ApplyModifiedProperties();
        _clickSimSO?.ApplyModifiedProperties(); _udpSO?.ApplyModifiedProperties();
        _overlaySO?.ApplyModifiedProperties(); _vcellSO?.ApplyModifiedProperties();
        _bridgeSO?.ApplyModifiedProperties();
        _profileSO?.ApplyModifiedProperties();
        foreach (var (_, so) in _presetSOs) so.ApplyModifiedProperties();
    }

    // ── 功能開關（即時套用）──────────────────────────────────────────────────

    static GUIContent _warningIcon;
    static GUIContent WarningIcon => _warningIcon ??= EditorGUIUtility.IconContent("console.warnicon.sml");

    void DrawFeatureToggleSection()
    {
        if (!BeginSection("功能開關（即時套用到場景）", ref _foldFeature)) return;

        EditorGUILayout.LabelField("▸ 高耗能功能", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        ToggleHighPower(
            label:       "Display2 螢幕擷取",
            tooltip:     "每幀用 GDI BitBlt 截取 Display 2，每幀 CPU→GPU 上傳 ~6MB。建議正式執行時關閉。",
            getEnabled:  () => _display2Capture != null && _display2Capture.gameObject.activeSelf,
            setEnabled:  v =>
            {
                if (_display2Capture == null) return;
                SetObjectActive(_display2Capture.gameObject, v);
                MarkSceneDirty();
            },
            warningText: "開啟 Display2 螢幕擷取後，每幀會在主執行緒執行 GDI BitBlt 截圖並上傳 ~6MB 到 GPU，長時間運行可能導致速度下降。\n\n確定開啟？"
        );

        ToggleHighPower(
            label:       "ConsoleGameOverlay 疊加顯示",
            tooltip:     "Display 1 格子背後顯示 Display 2 畫面（RT 混合，額外 draw call）。",
            getEnabled:  () => _overlay != null && _overlay.enabled,
            setEnabled:  v =>
            {
                if (_overlay == null) return;
                Undo.RecordObject(_overlay, "Toggle ConsoleGameOverlay");
                _overlay.enabled = v;
                EditorUtility.SetDirty(_overlay);
                MarkSceneDirty();
            },
            warningText: "開啟疊加顯示後，每幀會額外執行 RT 混合（draw call 增加）。\n\n確定開啟？"
        );

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("▸ 除錯輔助功能", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        ToggleNormal(
            label:      "TouchDebugLogger（IMGUI 觸碰紀錄）",
            tooltip:    "每幀渲染 IMGUI 視窗，有 GUIStyle GC 壓力。校準期使用，正式執行建議停用。",
            getEnabled: () => _logger != null && _logger.enabled,
            setEnabled: v =>
            {
                if (_logger == null) return;
                Undo.RecordObject(_logger, "Toggle TouchDebugLogger");
                _logger.enabled = v;
                if (!v) _logger.showWindow = false;
                EditorUtility.SetDirty(_logger);
                MarkSceneDirty();
            }
        );

        ToggleNormal(
            label:      "UDP 封包 Debug Log",
            tooltip:    "每次 UDP 封包都寫 Console，感應器 100Hz 時每秒 100+ 行，嚴重影響效能。",
            getEnabled: () => _udp != null && _udp.debugLog,
            setEnabled: v =>
            {
                if (_udp == null) return;
                Undo.RecordObject(_udp, "Toggle UDP debugLog");
                _udp.debugLog = v;
                EditorUtility.SetDirty(_udp);
                MarkSceneDirty();
            }
        );

        ToggleNormal(
            label:      "座標測試面板（CoordTestPanel）",
            tooltip:    "顯示即時座標偵測面板，校準用，正式執行可停用。",
            getEnabled: () => _coordTestPanel != null && _coordTestPanel.enabled,
            setEnabled: v =>
            {
                if (_coordTestPanel == null) return;
                Undo.RecordObject(_coordTestPanel, "Toggle CoordTestPanel");
                _coordTestPanel.enabled = v;
                EditorUtility.SetDirty(_coordTestPanel);
                MarkSceneDirty();
            }
        );

        ToggleNormal(
            label:      "外部 Console 視窗（GameConsole）",
            tooltip:    "彈出 Windows 命令列視窗，正式展演建議停用。",
            getEnabled: () => _gameConsole != null && _gameConsole.enabled,
            setEnabled: v =>
            {
                if (_gameConsole == null) return;
                Undo.RecordObject(_gameConsole, "Toggle GameConsole");
                _gameConsole.enabled = v;
                EditorUtility.SetDirty(_gameConsole);
                MarkSceneDirty();
            }
        );

        if (_stm != null)
        {
            ToggleNormal(
                label:      "訊號記錄按鈕",
                tooltip:    "隱藏控制台的「訊號記錄」按鈕，不影響其他功能。",
                getEnabled: () => _stm.showLogButton,
                setEnabled: v =>
                {
                    Undo.RecordObject(_stm, "Toggle showLogButton");
                    _stm.showLogButton = v;
                    EditorUtility.SetDirty(_stm);
                    MarkSceneDirty();
                }
            );
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(4);

        int highPowerOn = 0;
        if (_display2Capture != null && _display2Capture.gameObject.activeSelf) highPowerOn++;
        if (_overlay != null && _overlay.enabled) highPowerOn++;
        int mediumOn = 0;
        if (_logger != null && _logger.enabled) mediumOn++;
        if (_udp != null && _udp.debugLog) mediumOn++;

        string summary = $"高耗能開啟：{highPowerOn}/2　　除錯開啟：{mediumOn}/2";
        var msgType = highPowerOn > 0 ? MessageType.Warning : (mediumOn > 0 ? MessageType.Info : MessageType.None);
        EditorGUILayout.HelpBox(summary, msgType);

        EndSection();
    }

    static void ToggleHighPower(string label, string tooltip, System.Func<bool> getEnabled,
        System.Action<bool> setEnabled, string warningText)
    {
        bool current = getEnabled();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(WarningIcon, GUILayout.Width(18), GUILayout.Height(18));
        EditorGUI.BeginChangeCheck();
        bool next = EditorGUILayout.ToggleLeft(
            new GUIContent(label, tooltip), current,
            current ? EditorStyles.boldLabel : EditorStyles.label);
        if (EditorGUI.EndChangeCheck())
        {
            if (next && !current)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "⚠ 高耗能功能確認", warningText, "確定開啟", "取消");
                if (!confirmed) next = false;
            }
            if (next != current) setEnabled(next);
        }
        EditorGUILayout.EndHorizontal();
    }

    static void ToggleNormal(string label, string tooltip,
        System.Func<bool> getEnabled, System.Action<bool> setEnabled)
    {
        bool current = getEnabled();
        EditorGUI.BeginChangeCheck();
        bool next = EditorGUILayout.ToggleLeft(new GUIContent(label, tooltip), current);
        if (EditorGUI.EndChangeCheck() && next != current)
            setEnabled(next);
    }

    static void MarkSceneDirty()
        => EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

    // ── 場景設定檔（含全部子分類）────────────────────────────────────────────

    void DrawProfileSection()
    {
        if (!BeginSection("場景設定檔", ref _foldProfile)) return;

        var next = (SceneInitialSettingsProfile)EditorGUILayout.ObjectField(
            "設定檔", _profile, typeof(SceneInitialSettingsProfile), false);
        if (next != _profile)
        {
            _profile = next;
            _profileSO = _profile != null ? new SerializedObject(_profile) : null;
        }

        if (_profileSO == null)
        {
            EditorGUILayout.HelpBox(
                "請指定一個 SceneInitialSettingsProfile。預設檔在：\n" +
                "Assets/_Script/MultiSensor/Setting/SceneInitialProfiles/",
                MessageType.Info);
            EndSection();
            return;
        }

        _profileSO.Update();

        // ── 場景專屬：基本 ────────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("▸ 場景專屬設定", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        Prop(_profileSO, "profileName",        "名稱");
        Prop(_profileSO, "sensorScenePath",    "感應場景路徑");
        Prop(_profileSO, "gameSceneName",      "遊戲場景名稱");
        Prop(_profileSO, "showPresetSelector", "顯示場域切換");
        Prop(_profileSO, "gameTileVisible",    "初始晶片示意");
        Prop(_profileSO, "activeDuration",     "晶片亮起時間");
        Prop(_profileSO, "overlayAlpha",       "主畫面疊加不透明度");
        EditorGUILayout.Space(2);
        Prop(_profileSO, "display2X", "Display 2 X（px，通常等於 Display 1 寬度 1920）");
        Prop(_profileSO, "display2Y", "Display 2 Y（px，通常為 0）");
        EditorGUILayout.HelpBox(
            "display2X/Y 套用後同時更新：SensorTestManager.display2WindowOffsetX\n" +
            "以及 SensorClickSimulator.display2OffsetX / display2OffsetY",
            MessageType.None);
        EditorGUI.indentLevel--;

        // ── 感應器 & 點擊 ─────────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("▸ 感應器 & 點擊", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        Prop(_profileSO, "maxPointsPerFrame",  "每幀最多 UDP 點數");
        Prop(_profileSO, "ghostTouchMode",     "Ghost Touch 模式");
        Prop(_profileSO, "tapBoundaryMs",      "Tap / Press 分界 ms");
        Prop(_profileSO, "touchEndTimeoutMs",  "Touch 結束 timeout ms");
        Prop(_profileSO, "clickMode",          "點擊模式");
        Prop(_profileSO, "clickCooldown",      "點擊冷卻秒數");
        EditorGUILayout.HelpBox(
            "點擊模式在此統一設定，套用後寫入 SensorClickSimulator。",
            MessageType.None);
        EditorGUI.indentLevel--;

        // ── 降低效能負擔（子折疊）────────────────────────────────────────────
        EditorGUILayout.Space(4);
        _foldPerfSub = EditorGUILayout.Foldout(_foldPerfSub, "▸ 降低效能負擔", true);
        if (_foldPerfSub)
        {
            EditorGUI.indentLevel++;
            Prop(_profileSO, "lowerPerformanceCost", "啟用降低效能負擔");
            bool lpc = _profile != null && _profile.lowerPerformanceCost;
            using (new EditorGUI.DisabledScope(!lpc))
            {
                Prop(_profileSO, "disableDisplay2ScreenCapture", "停用 Display 2 擷取");
                Prop(_profileSO, "disableRuntimeLogObject",      "停用 Log 物件");
                Prop(_profileSO, "disableTouchDebugLogger",      "停用 TouchDebugLogger");
                Prop(_profileSO, "disableCoordTestPanel",        "停用座標測試面板");
                Prop(_profileSO, "disableGameConsole",           "停用外部 Console");
                Prop(_profileSO, "hideClickLogPanel",            "隱藏點擊 Log 面板");
                Prop(_profileSO, "hideDiagnosticLogButton",      "隱藏診斷 Log 按鈕");
                Prop(_profileSO, "disableUdpDebugLog",           "關閉 UDP Debug Log");
                Prop(_profileSO, "disableClickDebugLog",         "關閉點擊 Debug Log");
                Prop(_profileSO, "disableGhostTouchFilter",      "停用 Ghost Touch 過濾");
                Prop(_profileSO, "disableClickCooldown",         "停用點擊冷卻");
            }
            EditorGUI.indentLevel--;
        }

        // ── 場域通用 ──────────────────────────────────────────────────────────
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("▸ 場域通用設定", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "以下設定不因場域（舞台/光域）而異，所有 profile 通常填相同值。",
            MessageType.None);

        // 感應接收（UDP）
        EditorGUILayout.Space(2);
        _foldCommonUdp = EditorGUILayout.Foldout(_foldCommonUdp, "  感應接收（UDP）", true);
        if (_foldCommonUdp)
        {
            EditorGUI.indentLevel++;
            Prop(_profileSO, "listenPort",   "監聽 Port（預設 20001）");
            Prop(_profileSO, "maxQueueSize", "背景佇列上限（預設 256，超過丟棄）");
            EditorGUI.indentLevel--;
        }

        // 感應橋接
        _foldCommonBridge = EditorGUILayout.Foldout(_foldCommonBridge, "  感應橋接（SensorBridgeToPassPosition）", true);
        if (_foldCommonBridge)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("格子欄列數需與 SensorTestManager 場域 colCount/rowCount 一致。", MessageType.None);
            Prop(_profileSO, "bridgeGridCols",          "格子欄數（colCount，預設 128）");
            Prop(_profileSO, "bridgeGridRows",          "格子列數（rowCount，預設 24）");
            Prop(_profileSO, "bridgeDisplayWidth",      "Display 2 寬度（px，預設 2048）");
            Prop(_profileSO, "bridgeDisplayHeight",     "Display 2 高度（px，預設 768）");
            EditorGUILayout.Space(2);
            Prop(_profileSO, "bridgeTouchTimeout",      "離手逾時（秒）");
            Prop(_profileSO, "bridgeMatchRadiusCells",  "配對半徑（格子單位）");
            Prop(_profileSO, "bridgeReleaseHoldSeconds","按壓結束淡出（秒）");
            Prop(_profileSO, "bridgePositionSmoothSpeed","水波位置平滑速度（0=關閉）");
            EditorGUI.indentLevel--;
        }

        // 觸碰日誌
        _foldCommonLog = EditorGUILayout.Foldout(_foldCommonLog, "  觸碰日誌（TouchDebugLogger）", true);
        if (_foldCommonLog)
        {
            EditorGUI.indentLevel++;
            Prop(_profileSO, "logShowWindow",    "初始顯示 Log 視窗");
            Prop(_profileSO, "logListenRawUdp",  "訂閱 UDP 原始座標（校準用）");
            Prop(_profileSO, "logListenSensorBus","訂閱 SensorBus 格子座標");
            Prop(_profileSO, "logToggleKey",     "切換視窗按鍵（預設 F9）");
            EditorGUI.indentLevel--;
        }

        // 虛擬格
        _foldCommonVCell = EditorGUILayout.Foldout(_foldCommonVCell, "  虛擬格（VirtualCellManager）", true);
        if (_foldCommonVCell)
        {
            EditorGUI.indentLevel++;
            Prop(_profileSO, "pollingIntervalMs",
                "硬體輪詢間隔（ms）；超過 2× 此值無觸碰視為離手（預設 100）");
            EditorGUI.indentLevel--;
        }

        // 點擊模擬進階
        _foldCommonClick = EditorGUILayout.Foldout(_foldCommonClick, "  點擊模擬進階（SensorClickSimulator）", true);
        if (_foldCommonClick)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("外送 UDP", EditorStyles.boldLabel);
            Prop(_profileSO, "clickExternalEnabled", "同時轉發 UDP 座標給外部程式");
            Prop(_profileSO, "clickExternalIP",      "外部程式 IP（預設 127.0.0.1）");
            Prop(_profileSO, "clickExternalPort",    "外部程式 Port（預設 20002）");
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("滑鼠模式進階（ClickMode = Mouse 時生效）", EditorStyles.boldLabel);
            Prop(_profileSO, "clickPressHoldDuration", "按壓按住秒數（預設 0.3）");
            Prop(_profileSO, "clickDragReleaseDelay",  "拖曳放開延遲（預設 0.5）");
            EditorGUI.indentLevel--;
        }

        // ── 套用按鈕 ─────────────────────────────────────────────────────────
        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("▶ 套用到目前場景", ApplyButtonStyle, GUILayout.Height(28)))
            ApplyProfileToCurrentScene(_profile);
        if (GUILayout.Button("設為 Build 啟用場景"))
            ApplyBuildSceneProfile(_profile);
        EditorGUILayout.EndHorizontal();

        EndSection();
    }

    // ── 場域預設 ─────────────────────────────────────────────────────────────

    void DrawVenueSection()
    {
        if (!BeginSection("場域預設（VenuePresetConfig）", ref _foldVenue)) return;

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

    // ── 遊戲場景（Build Settings 檢查）──────────────────────────────────────

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

    // ── 共用工具 ──────────────────────────────────────────────────────────────

    static bool BeginSection(string label, ref bool foldout)
    {
        EditorGUILayout.Space(2);
        foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, label);
        if (!foldout) EditorGUILayout.EndFoldoutHeaderGroup();
        return foldout;
    }

    static void EndSection() => EditorGUILayout.EndFoldoutHeaderGroup();

    static bool HasComponent(Object obj, string typeName)
    {
        if (obj != null) return true;
        EditorGUILayout.HelpBox($"場景中找不到 {typeName}。", MessageType.Warning);
        return false;
    }

    static void Prop(SerializedObject so, string propName, string label)
    {
        if (so == null) return;
        var prop = so.FindProperty(propName);
        if (prop != null)
            EditorGUILayout.PropertyField(prop, new GUIContent(label));
        else
            EditorGUILayout.HelpBox($"找不到序列化欄位：{propName}（可能已改名）", MessageType.Warning);
    }

    // ── 套用設定檔到場景 ──────────────────────────────────────────────────────

    void ApplyProfileToCurrentScene(SceneInitialSettingsProfile profile)
    {
        if (profile == null) return;
        bool reduceCost = profile.lowerPerformanceCost;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Apply Nangang scene initial settings");
        int undoGroup = Undo.GetCurrentGroup();

        // SensorTestManager
        if (_stm != null)
        {
            Undo.RecordObject(_stm, "Apply scene profile");
            _stm.gameSceneName         = profile.gameSceneName;
            _stm.showPresetSelector    = profile.showPresetSelector;
            _stm.activeDuration        = profile.activeDuration;
            _stm.display2WindowOffsetX = profile.display2X;
            SetSerializedField(_stm, "_gameTileVisible",  profile.gameTileVisible);
            SetSerializedField(_stm, "showClickLogPanel", !(reduceCost && profile.hideClickLogPanel));
            _stm.showLogButton     = !(reduceCost && profile.hideDiagnosticLogButton);
            _stm.tapBoundaryMs     = profile.tapBoundaryMs;
            _stm.touchEndTimeoutMs = profile.touchEndTimeoutMs;
            _stm.ghostTouchMode    = reduceCost && profile.disableGhostTouchFilter
                ? GhostTouchMode.None
                : profile.ghostTouchMode;
            EditorUtility.SetDirty(_stm);
        }

        // UdpSensorReceiver
        if (_udp != null)
        {
            Undo.RecordObject(_udp, "Apply scene profile");
            _udp.debugLog          = !(reduceCost && profile.disableUdpDebugLog);
            _udp.maxPointsPerFrame = profile.maxPointsPerFrame;
            _udp.listenPort        = profile.listenPort;
            _udp.maxQueueSize      = profile.maxQueueSize;
            EditorUtility.SetDirty(_udp);
        }

        // SensorClickSimulator
        if (_clickSim != null)
        {
            Undo.RecordObject(_clickSim, "Apply scene profile");
            _clickSim.debugLog        = !(reduceCost && profile.disableClickDebugLog);
            _clickSim.cooldownEnabled = !(reduceCost && profile.disableClickCooldown);
            _clickSim.clickCooldown   = profile.clickCooldown;
            EditorUtility.SetDirty(_clickSim);
            // Private / serialized-only fields
            SetSerializedField(_clickSim, "_mode",              (int)profile.clickMode);
            SetSerializedField(_clickSim, "display2OffsetX",    profile.display2X);
            SetSerializedField(_clickSim, "display2OffsetY",    profile.display2Y);
            SetSerializedField(_clickSim, "externalEnabled",    profile.clickExternalEnabled);
            SetSerializedField(_clickSim, "externalIP",         profile.clickExternalIP);
            SetSerializedField(_clickSim, "externalPort",       profile.clickExternalPort);
            SetSerializedField(_clickSim, "pressHoldDuration",  profile.clickPressHoldDuration);
            SetSerializedField(_clickSim, "dragReleaseDelay",   profile.clickDragReleaseDelay);
        }

        // ConsoleGameOverlay
        if (_overlay != null)
        {
            Undo.RecordObject(_overlay, "Apply scene profile");
            _overlay.overlayAlpha = profile.overlayAlpha;
            EditorUtility.SetDirty(_overlay);
        }

        // Display2ScreenCapture
        if (_display2Capture != null)
            SetObjectActive(_display2Capture.gameObject,
                !(reduceCost && profile.disableDisplay2ScreenCapture));

        // TouchDebugLogger
        if (_logger != null)
        {
            Undo.RecordObject(_logger, "Apply scene profile");
            bool logEnabled      = !(reduceCost && profile.disableTouchDebugLogger);
            _logger.enabled      = logEnabled;
            _logger.showWindow   = logEnabled && profile.logShowWindow;
            _logger.listenRawUdp  = logEnabled && profile.logListenRawUdp;
            _logger.listenSensorBus = logEnabled && profile.logListenSensorBus;
            _logger.toggleKey    = profile.logToggleKey;
            EditorUtility.SetDirty(_logger);
        }

        // CoordTestPanel
        if (_coordTestPanel != null)
        {
            Undo.RecordObject(_coordTestPanel, "Apply scene profile");
            _coordTestPanel.enabled = !(reduceCost && profile.disableCoordTestPanel);
            EditorUtility.SetDirty(_coordTestPanel);
        }

        // GameConsole
        if (_gameConsole != null)
        {
            Undo.RecordObject(_gameConsole, "Apply scene profile");
            _gameConsole.enabled = !(reduceCost && profile.disableGameConsole);
            EditorUtility.SetDirty(_gameConsole);
        }

        // Log 物件整體停用
        if (reduceCost && profile.disableRuntimeLogObject)
        {
            if (_logger != null)         SetObjectActive(_logger.gameObject, false);
            else if (_coordTestPanel != null) SetObjectActive(_coordTestPanel.gameObject, false);
            else if (_gameConsole != null)    SetObjectActive(_gameConsole.gameObject, false);
        }

        // SensorBridgeToPassPosition
        if (_bridge != null)
        {
            Undo.RecordObject(_bridge, "Apply scene profile");
            _bridge.gridCols             = profile.bridgeGridCols;
            _bridge.gridRows             = profile.bridgeGridRows;
            _bridge.displayWidth         = profile.bridgeDisplayWidth;
            _bridge.displayHeight        = profile.bridgeDisplayHeight;
            _bridge.touchTimeout         = profile.bridgeTouchTimeout;
            _bridge.matchRadiusCells     = profile.bridgeMatchRadiusCells;
            _bridge.releaseHoldSeconds   = profile.bridgeReleaseHoldSeconds;
            _bridge.positionSmoothSpeed  = profile.bridgePositionSmoothSpeed;
            EditorUtility.SetDirty(_bridge);
        }

        // VirtualCellManager
        if (_vcell != null)
        {
            Undo.RecordObject(_vcell, "Apply scene profile");
            SetSerializedField(_vcell, "pollingIntervalMs", profile.pollingIntervalMs);
            EditorUtility.SetDirty(_vcell);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);
        Refresh();
    }

    void ApplyBuildSceneProfile(SceneInitialSettingsProfile profile)
    {
        if (profile == null || string.IsNullOrEmpty(profile.sensorScenePath)) return;

        var scenes = new List<EditorBuildSettingsScene>();
        bool foundSensor = false;
        bool foundGame = string.IsNullOrEmpty(profile.gameSceneName);

        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (!string.IsNullOrEmpty(profile.gameSceneName) &&
                Path.GetFileNameWithoutExtension(scene.path) == profile.gameSceneName)
            {
                foundGame = true;
                scenes.Add(new EditorBuildSettingsScene(scene.path, true));
                continue;
            }
            bool enabled = scene.path == profile.sensorScenePath;
            if (enabled) foundSensor = true;
            scenes.Add(new EditorBuildSettingsScene(scene.path, enabled));
        }

        if (!foundSensor)
            scenes.Insert(0, new EditorBuildSettingsScene(profile.sensorScenePath, true));

        if (!foundGame)
        {
            string gameScenePath = FindScenePathByName(profile.gameSceneName);
            if (!string.IsNullOrEmpty(gameScenePath))
                scenes.Add(new EditorBuildSettingsScene(gameScenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ── SetSerializedField 多型 ────────────────────────────────────────────────

    static void SetObjectActive(GameObject go, bool active)
    {
        if (go == null || go.activeSelf == active) return;
        Undo.RecordObject(go, "Apply scene profile");
        go.SetActive(active);
        EditorUtility.SetDirty(go);
    }

    static void SetSerializedField(Object target, string propName, bool value)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        var prop = so.FindProperty(propName);
        if (prop == null || prop.propertyType != SerializedPropertyType.Boolean) return;
        prop.boolValue = value;
        so.ApplyModifiedProperties();
    }

    static void SetSerializedField(Object target, string propName, int value)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        var prop = so.FindProperty(propName);
        if (prop == null) return;
        if (prop.propertyType == SerializedPropertyType.Enum)
            prop.enumValueIndex = value;
        else
            prop.intValue = value;
        so.ApplyModifiedProperties();
    }

    static void SetSerializedField(Object target, string propName, float value)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        var prop = so.FindProperty(propName);
        if (prop == null || prop.propertyType != SerializedPropertyType.Float) return;
        prop.floatValue = value;
        so.ApplyModifiedProperties();
    }

    static void SetSerializedField(Object target, string propName, string value)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        var prop = so.FindProperty(propName);
        if (prop == null || prop.propertyType != SerializedPropertyType.String) return;
        prop.stringValue = value;
        so.ApplyModifiedProperties();
    }

    static string FindScenePathByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        string[] guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == sceneName)
                return path;
        }
        return null;
    }

    static bool IsSceneInBuild(string sceneName)
    {
        foreach (var s in EditorBuildSettings.scenes)
            if (Path.GetFileNameWithoutExtension(s.path) == sceneName) return true;
        return false;
    }
}
