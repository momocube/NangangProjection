using UnityEngine;

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
}
