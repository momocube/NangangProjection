using UnityEngine;
using UnityEditor;

/// <summary>
/// Scene 視窗中顯示所有 SensorFaceConfig 的晶片格 Gizmo。
/// 自動掃描 Project 中所有 SensorFaceConfig，不需要掛在 GameObject 上。
/// 每面第一格（col=0, row=0）顯示紅色；其餘格子顯示各面自訂顏色。
/// 座標系：1 unit = 1 px，X 向右，Y 向上（像素 Y 向下，因此取負號）。
/// </summary>
[InitializeOnLoad]
public static class FaceVisualizer
{
    static bool _enabled = true;
    const string MENU_ITEM = "NangangProjection/顯示晶片格 Gizmo";

    static FaceVisualizer()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        _enabled = EditorPrefs.GetBool(MENU_ITEM, true);
    }

    [MenuItem(MENU_ITEM)]
    static void ToggleGizmo()
    {
        _enabled = !_enabled;
        EditorPrefs.SetBool(MENU_ITEM, _enabled);
        SceneView.RepaintAll();
    }

    [MenuItem(MENU_ITEM, true)]
    static bool ToggleGizmoValidate()
    {
        Menu.SetChecked(MENU_ITEM, _enabled);
        return true;
    }

    static void OnSceneGUI(SceneView view)
    {
        if (!_enabled) return;

        DrawCanvasBoundary();

        string[] guids = AssetDatabase.FindAssets("t:SensorFaceConfig");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var face = AssetDatabase.LoadAssetAtPath<SensorFaceConfig>(path);
            if (face == null || !face.showInVisualizer) continue;
            DrawFace(face);
        }
    }

    // ------ 畫布邊界（白色粗框）------
    static void DrawCanvasBoundary()
    {
        // 讀取 DisplayCanvasConfig，找不到就用預設值
        int cw = 2048, ch = 768;
        string[] guids = AssetDatabase.FindAssets("t:DisplayCanvasConfig");
        if (guids.Length > 0)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<DisplayCanvasConfig>(
                          AssetDatabase.GUIDToAssetPath(guids[0]));
            if (cfg != null) { cw = cfg.canvasWidth; ch = cfg.canvasHeight; }
        }

        float w = cw;
        float h = ch;

        // 外框
        Handles.color = Color.white;
        Handles.DrawAAPolyLine(4f,
            new Vector3(0,  0,  0),
            new Vector3(w,  0,  0),
            new Vector3(w, -h,  0),
            new Vector3(0, -h,  0),
            new Vector3(0,  0,  0)
        );

        // 標籤：左上角
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            normal    = { textColor = Color.white },
            fontStyle = FontStyle.Bold,
            fontSize  = 16,
        };
        Handles.Label(new Vector3(0, 32, 0), $"Display 2  {cw} × {ch} px", style);
    }

    // ------ 單個感應面 ------
    static void DrawFace(SensorFaceConfig face)
    {
        float x0 = face.originX;
        float y0 = -face.originY;
        float tw = face.TotalPixelWidth;
        float th = face.TotalPixelHeight;

        // colCount/rowCount 永遠是畫面 X/Y 格數，cellW/H 依安裝方向決定
        bool isVertical = face.orientation == SensorFaceConfig.ChipOrientation.直;
        float cellW = face.GizmoCellW;
        float cellH = face.GizmoCellH;
        int screenCols = face.colCount;
        int screenRows = face.rowCount;

        // ------ 格線 ------
        Handles.color = face.gizmoColor;

        int lineCount = (screenRows + 1) + (screenCols + 1);
        var pts = new Vector3[lineCount * 2];
        int idx = 0;

        for (int r = 0; r <= screenRows; r++)
        {
            float y = y0 - r * cellH;
            pts[idx++] = new Vector3(x0,      y, 0f);
            pts[idx++] = new Vector3(x0 + tw, y, 0f);
        }
        for (int c = 0; c <= screenCols; c++)
        {
            float x = x0 + c * cellW;
            pts[idx++] = new Vector3(x, y0,      0f);
            pts[idx++] = new Vector3(x, y0 - th, 0f);
        }
        Handles.DrawLines(pts);

        // ------ 面外框（粗線）------
        Color outlineColor = face.gizmoColor;
        outlineColor.a = 1f;
        Handles.color = outlineColor;
        Handles.DrawAAPolyLine(3f,
            new Vector3(x0,      y0,      0f),
            new Vector3(x0 + tw, y0,      0f),
            new Vector3(x0 + tw, y0 - th, 0f),
            new Vector3(x0,      y0 - th, 0f),
            new Vector3(x0,      y0,      0f)
        );

        // ------ 標籤：面名稱 + 方向 + 像素尺寸 ------
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            normal    = { textColor = face.gizmoColor },
            fontStyle = FontStyle.Bold,
            fontSize  = 14,
        };
        string orientLabel = isVertical ? "  [直]" : "  [橫]";
        string label = $"{face.faceName}{orientLabel}\n{tw}×{th} px  ({screenCols}×{screenRows} 格)";
        Handles.Label(new Vector3(x0, y0 + cellH * 2, 0f), label, style);
    }
}
