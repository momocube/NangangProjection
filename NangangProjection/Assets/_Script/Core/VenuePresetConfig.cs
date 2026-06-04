using UnityEngine;

/// <summary>虛擬格模式：None = 維持原始晶片粒度，Size32 = 32×32 px，Size64 = 64×64 px。</summary>
public enum VirtualCellMode { None, Size32, Size64 }

/// <summary>
/// 場域預設：畫布設定 + 一或多個感應面。
/// 建立方式：Project 視窗右鍵 → Create → NangangProjection → VenuePresetConfig
/// </summary>
[CreateAssetMenu(fileName = "VenuePresetConfig", menuName = "NangangProjection/VenuePresetConfig")]
public class VenuePresetConfig : ScriptableObject
{
    [Header("場域名稱（顯示在下拉選單）")]
    public string presetName = "場域";

    [Header("畫布設定")]
    public DisplayCanvasConfig canvasConfig;

    [Header("感應面列表")]
    public SensorFaceConfig[] faces;

    [Header("控制格顯示旋轉")]
    [Tooltip("開啟時：控制格顯示旋轉 90° CCW（直向畫布建議開啟，讓畫面在橫向螢幕填滿更多空間）。")]
    public bool rotateDisplay90 = false;

    [Header("感應座標校準（整體畫布四角測試）")]
    [Tooltip("在整個畫布的四個角踩出的原始感應值。程式會依各面的 originX/Y 與格數自動算出每個面的有效範圍。留 0 表示未設定，回退至各面 SensorFaceConfig 的 rawMin/Max。")]
    public float canvasRawMinX;
    public float canvasRawMaxX;
    public float canvasRawMinY;
    public float canvasRawMaxY;

    [Tooltip("感應實際涵蓋的像素範圍（四角測試踩得到的區域）。若小於畫布尺寸（例如 3328 < 3840），填入此值讓比例計算正確。留 0 表示與畫布同尺寸。")]
    public int sensorAreaWidth;
    public int sensorAreaHeight;

    [Header("虛擬格模式")]
    [Tooltip("None = 晶片模式（原始行為）；Size32 = 32×32 px 虛擬格（橫式 2 顆，直式 2 顆）；Size64 = 64×64 px 虛擬格（橫式 8 顆，直式 8 顆）。")]
    public VirtualCellMode virtualCellMode = VirtualCellMode.None;
}
