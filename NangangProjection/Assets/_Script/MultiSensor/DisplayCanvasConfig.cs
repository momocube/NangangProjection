using UnityEngine;

/// <summary>
/// 投影畫布的解析度設定（對應 Display 2 實際像素大小）。
/// 建立方式：Project 視窗右鍵 → Create → NangangProjection → DisplayCanvasConfig
/// 整個專案只需要一個，FaceVisualizer 會自動讀取。
/// </summary>
[CreateAssetMenu(fileName = "DisplayCanvasConfig", menuName = "NangangProjection/DisplayCanvasConfig")]
public class DisplayCanvasConfig : ScriptableObject
{
    [Header("Display 2 畫布解析度（px）")]
    public int canvasWidth  = 2048;
    public int canvasHeight = 768;
}
