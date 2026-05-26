using UnityEngine;

[CreateAssetMenu(
    fileName = "SceneInitialSettingsProfile",
    menuName = "NangangProjection/Scene Initial Settings Profile")]
public class SceneInitialSettingsProfile : ScriptableObject
{
    // ── 場景專屬設定 ──────────────────────────────────────────────────────────

    [Header("場景專屬 / 基本")]
    public string profileName;
    public string sensorScenePath;
    public string gameSceneName;
    public bool showPresetSelector;
    public bool gameTileVisible;
    public float activeDuration = 0.1f;
    public float overlayAlpha = 0.5f;

    [Header("場景專屬 / Display 2 位置（Windows 桌面座標）")]
    [Tooltip("Display 2 螢幕左上角 X（通常 = Display 1 寬度）。同時套用到 SensorTestManager 和 SensorClickSimulator。")]
    public int display2X = 1920;
    [Tooltip("Display 2 螢幕左上角 Y（通常為 0）。")]
    public int display2Y = 0;

    [Header("場景專屬 / 降低效能負擔")]
    public bool lowerPerformanceCost = true;
    public bool disableDisplay2ScreenCapture = true;
    public bool disableRuntimeLogObject = true;
    public bool disableTouchDebugLogger = true;
    public bool disableCoordTestPanel = true;
    public bool disableGameConsole = true;
    public bool hideClickLogPanel = true;
    public bool hideDiagnosticLogButton = true;
    public bool disableUdpDebugLog = true;
    public bool disableClickDebugLog = true;
    public bool disableGhostTouchFilter = true;
    public bool disableClickCooldown = true;

    [Header("場景專屬 / 感應器")]
    public int maxPointsPerFrame = 32;
    public GhostTouchMode ghostTouchMode = GhostTouchMode.None;
    public float tapBoundaryMs = 160f;
    public float touchEndTimeoutMs = 180f;

    [Header("場景專屬 / 點擊")]
    public SensorClickSimulator.ClickMode clickMode = SensorClickSimulator.ClickMode.PostMessage;
    public float clickCooldown = 0.1f;

    // ── 場域通用設定 ──────────────────────────────────────────────────────────

    [Header("場域通用 / 感應接收（UDP）")]
    [Tooltip("UDP 監聽 Port（通常固定 20001，不同場域也相同）。")]
    public int listenPort = 20001;
    [Tooltip("背景接收佇列上限，超過此數量的封包會被丟棄（預設 256）。")]
    public int maxQueueSize = 256;

    [Header("場域通用 / 感應橋接（SensorBridgeToPassPosition）")]
    [Tooltip("格子欄數，需與 SensorTestManager 場域 colCount 一致。")]
    public int bridgeGridCols = 128;
    [Tooltip("格子列數，需與 SensorTestManager 場域 rowCount 一致。")]
    public int bridgeGridRows = 24;
    [Tooltip("Display 2 畫布寬度（px）。")]
    public int bridgeDisplayWidth = 2048;
    [Tooltip("Display 2 畫布高度（px）。")]
    public int bridgeDisplayHeight = 768;
    [Tooltip("超過此秒數無更新視為離手，開始淡出計時（秒）。")]
    public float bridgeTouchTimeout = 0.3f;
    [Tooltip("最近鄰配對半徑（格子單位）。")]
    public float bridgeMatchRadiusCells = 3f;
    [Tooltip("觸碰結束後水波淡出持續秒數。")]
    public float bridgeReleaseHoldSeconds = 0.7f;
    [Tooltip("水波位置插值速度；0 = 關閉平滑。")]
    public float bridgePositionSmoothSpeed = 12f;

    [Header("場域通用 / 觸碰日誌（TouchDebugLogger）")]
    [Tooltip("遊戲啟動時是否顯示 Log 視窗。")]
    public bool logShowWindow = false;
    [Tooltip("訂閱 UDP 原始座標（校準 rawMin/Max 用）。")]
    public bool logListenRawUdp = true;
    [Tooltip("訂閱 SensorBus 格子座標（正常使用時開啟）。")]
    public bool logListenSensorBus = false;
    [Tooltip("切換 Log 視窗的按鍵（預設 F9）。")]
    public KeyCode logToggleKey = KeyCode.F9;

    [Header("場域通用 / 虛擬格（VirtualCellManager）")]
    [Tooltip("硬體輪詢間隔（ms）；超過 2× 此值無觸碰視為離手（預設 100）。")]
    public int pollingIntervalMs = 100;

    [Header("場域通用 / 點擊模擬進階（SensorClickSimulator）")]
    [Tooltip("滑鼠模式：按壓按住秒數（預設 0.3）。")]
    public float clickPressHoldDuration = 0.3f;
    [Tooltip("滑鼠模式：拖曳放開延遲秒數（預設 0.5）。")]
    public float clickDragReleaseDelay = 0.5f;
}
