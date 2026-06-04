using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 遊戲場景相機設定。
/// 掛在遊戲場景的主相機（例如 Map_CybercubeLogo_J_2048x768 的 Main Camera）。
///
/// 功能：
///   1. 確保主相機輸出到 Display 2（targetDisplay = 1，投影地板）
///   2. 建立 Mirror Camera → 寫入 GameDisplayRT，讓主控台可以預覽遊戲畫面
///   3. 場景卸載時自動釋放 RenderTexture
///
/// 使用方式：
///   將此腳本掛到遊戲場景的主相機即可，不需額外設定。
/// </summary>
[RequireComponent(typeof(Camera))]
public class GameSceneSetup : MonoBehaviour
{
    [Header("Display 設定")]
    [Tooltip("遊戲畫面輸出到哪個 Display（0-based，Display 2 = 1）")]
    public int gameDisplayIndex = 1;

    [Header("RenderTexture 解析度")]
    [Tooltip("Mirror Camera RT 寬度（D1 預覽用，無需全解析度；建議 512～1024）")]
    public int rtWidth  = 512;
    [Tooltip("Mirror Camera RT 高度（配合 rtWidth 等比縮放，2048:768 ≈ 8:3）")]
    public int rtHeight = 192;

    [Header("Mirror Camera 效能")]
    [Range(1, 8)]
    [Tooltip("Mirror Camera 每幾幀才更新一次 RT（1=每幀；2=30fps；建議 2）")]
    public int mirrorFrameSkip = 2;

    private Camera _mainCamera;
    private Camera _mirrorCamera;
    private int    _frameCounter;

    private void Awake()
    {
        _mainCamera = GetComponent<Camera>();

        // 確保遊戲畫面輸出到投影地板（Display 2）
        _mainCamera.targetDisplay = gameDisplayIndex;

        // 確保 Display 2 已啟用（不指定解析度，由 Windows/系統決定）
        if (Display.displays.Length > gameDisplayIndex)
            Display.displays[gameDisplayIndex].Activate();
    }

    private void Start()
    {
        SetupMirrorCamera();
    }

    private void LateUpdate()
    {
        if (_mirrorCamera == null) return;
        _frameCounter++;
        _mirrorCamera.enabled = (_frameCounter % mirrorFrameSkip == 0);
    }

    private void OnDestroy()
    {
        // 遊戲場景卸載時釋放 RT
        if (_mirrorCamera != null)
            Destroy(_mirrorCamera.gameObject);
        GameDisplayRT.Release();
    }

    /// <summary>
    /// 建立 Mirror Camera：與主相機完全相同的設定，
    /// 但 targetTexture = GameDisplayRT（供主控台疊加預覽）。
    /// </summary>
    private void SetupMirrorCamera()
    {
        RenderTexture rt = GameDisplayRT.GetOrCreate(rtWidth, rtHeight);

        // 建立 Mirror Camera GameObject（子物件，跟隨主相機移動）
        var go = new GameObject("MirrorCamera_RT");
        go.transform.SetParent(transform, false);

        _mirrorCamera = go.AddComponent<Camera>();

        // 複製主相機所有設定
        _mirrorCamera.CopyFrom(_mainCamera);

        // 覆寫：輸出到 RenderTexture 而非螢幕
        _mirrorCamera.targetTexture = rt;
        _mirrorCamera.targetDisplay = 0; // 有 targetTexture 時這個值無效，但設 0 避免佔用

        // 比主相機晚渲染（深度較高），確保主相機先初始化
        _mirrorCamera.depth = _mainCamera.depth + 1;

        // URP：Camera 元件不會自動建立 UniversalAdditionalCameraData，
        // 缺少此元件 URP 會完全跳過這個相機導致 RT 永遠黑色。
        // GetUniversalAdditionalCameraData() 在缺少時會自動 AddComponent。
        var srcUrp = _mainCamera.GetUniversalAdditionalCameraData();
        var dstUrp = _mirrorCamera.GetUniversalAdditionalCameraData();
        dstUrp.renderType       = CameraRenderType.Base;
        dstUrp.renderShadows    = srcUrp.renderShadows;
        dstUrp.requiresColorTexture = false;
        dstUrp.requiresDepthTexture = false;

        Debug.Log($"[GameSceneSetup] Mirror Camera 建立完成，RT = {rt.width}×{rt.height}");
    }
}
