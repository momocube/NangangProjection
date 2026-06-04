using UnityEngine;

/// <summary>
/// 遊戲畫面共用 RenderTexture（2048×768）。
///
/// 由遊戲場景的 GameSceneSetup 建立（Additive 加載時），
/// 主控台的 ConsoleGameOverlay 讀取並顯示為透明疊加。
///
/// 使用靜態屬性讓跨場景存取零耦合：不需 Inspector 引用。
/// </summary>
public static class GameDisplayRT
{
    /// <summary>
    /// 目前的遊戲畫面 RenderTexture。
    /// 由 GameSceneSetup 在遊戲場景載入時設定，卸載時清除。
    /// </summary>
    public static RenderTexture Current { get; private set; }

    /// <summary>建立（或取得）RenderTexture，由 GameSceneSetup 呼叫。</summary>
    public static RenderTexture GetOrCreate(int width = 2048, int height = 768)
    {
        if (Current != null && Current.IsCreated() &&
            Current.width == width && Current.height == height)
            return Current;

        Release();
        Current = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Current.name = "RT_GameDisplay";
        Current.Create();
        Debug.Log($"[GameDisplayRT] 建立 {width}×{height} RenderTexture");
        return Current;
    }

    /// <summary>釋放 RenderTexture，由 GameSceneSetup 在場景卸載時呼叫。</summary>
    public static void Release()
    {
        if (Current == null) return;
        Current.Release();
        Object.Destroy(Current);
        Current = null;
        Debug.Log("[GameDisplayRT] 釋放 RenderTexture");
    }
}
