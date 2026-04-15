using UnityEngine;

/// <summary>
/// 感應格預覽畫面共用 RenderTexture（2048×768）。
///
/// 由 SensorTestManager.SetupSensorGridRT() 建立：
///   將 Canvas_Secondary 切換至 ScreenSpaceCamera 模式，
///   SensorCam 渲染感應格到此 RT。
///
/// ConsoleGameOverlay 讀取此 RT，在 Display 1 顯示感應格背景預覽。
/// 當遊戲場景已載入（GameDisplayRT.Current != null）時，ConsoleGameOverlay 優先使用遊戲 RT。
/// </summary>
public static class SensorGridRT
{
    /// <summary>目前的感應格 RenderTexture。由 SensorTestManager 建立，場景結束時釋放。</summary>
    public static RenderTexture Current { get; private set; }

    /// <summary>建立（或取得）RenderTexture，由 SensorTestManager 呼叫。</summary>
    public static RenderTexture GetOrCreate(int width = 2048, int height = 768)
    {
        if (Current != null && Current.IsCreated() &&
            Current.width == width && Current.height == height)
            return Current;

        Release();
        Current = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        Current.name = "RT_SensorGrid";
        Current.Create();
        Debug.Log($"[SensorGridRT] 建立 {width}×{height} RenderTexture");
        return Current;
    }

    /// <summary>釋放 RenderTexture，由 SensorTestManager.OnDestroy 呼叫。</summary>
    public static void Release()
    {
        if (Current == null) return;
        Current.Release();
        Object.Destroy(Current);
        Current = null;
        Debug.Log("[SensorGridRT] 釋放 RenderTexture");
    }
}
