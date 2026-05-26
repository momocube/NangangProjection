# Mirror Camera 效能規則

## Mirror Camera 必須縮小 RT 並加 frame skip

任何 `CopyFrom(mainCamera) + targetTexture = RT` 的 Mirror Camera，不得以主相機全解析度渲染 RT。

**規定設定（GameSceneSetup）：**
```csharp
public int rtWidth  = 512;   // 不要 2048
public int rtHeight = 192;   // 不要 768
public int mirrorFrameSkip = 2; // 每隔一幀才啟用

// LateUpdate：
_mirrorCamera.enabled = (++_frameCounter % mirrorFrameSkip == 0);
```

**為什麼：** 2048×768 @ 60fps Mirror Camera 讓遊戲場景每幀渲染兩次，粒子/Shader 重度場景直接卡頓。

## Display.Activate() 不傳 RT 解析度

```csharp
// ❌ 錯誤：把 RT 解析度當螢幕解析度
Display.displays[i].Activate(rtWidth, rtHeight, 60);

// ✅ 正確：讓 Windows 決定
Display.displays[i].Activate();
```
