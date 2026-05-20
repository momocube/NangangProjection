using UnityEngine;

[CreateAssetMenu(
    fileName = "SceneInitialSettingsProfile",
    menuName = "NangangProjection/Scene Initial Settings Profile")]
public class SceneInitialSettingsProfile : ScriptableObject
{
    [Header("Base Scene")]
    public string profileName;
    public string sensorScenePath;
    public string gameSceneName;
    public bool showPresetSelector;
    public bool gameTileVisible;
    public float activeDuration = 0.1f;
    public float overlayAlpha = 0.5f;

    [Header("Display 2 位置（Windows 桌面座標）")]
    [Tooltip("Display 2 螢幕左上角 X（通常 = Display 1 寬度）。同時套用到 SensorTestManager 和 SensorClickSimulator。")]
    public int display2X = 1920;
    [Tooltip("Display 2 螢幕左上角 Y（通常為 0）。")]
    public int display2Y = 0;

    [Header("Lower Performance Cost")]
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

    [Header("Sensor")]
    public int maxPointsPerFrame = 32;
    public GhostTouchMode ghostTouchMode = GhostTouchMode.None;
    public float tapBoundaryMs = 160f;
    public float touchEndTimeoutMs = 220f;

    [Header("Click")]
    public SensorClickSimulator.ClickMode clickMode = SensorClickSimulator.ClickMode.PostMessage;
    public float clickCooldown = 0.1f;
}
