using UnityEngine;

/// <summary>
/// UDP 封包 → 面分類 → 座標轉換 → 鎖定過濾 → 路由到 SensorTestManager.DispatchSensorCell。
/// 掛在與 SensorTestManager 同一個 GameObject 上。
/// Inspector 停用此元件即停用感應路由（踩踏封包不再更新格子狀態）。
/// </summary>
public class SensorFaceRouter : MonoBehaviour
{
    [Header("UDP 封包 → 面分類 → 路由到 SensorTestManager")]
    [Header("停用此元件 → 踩踏封包不再更新格子 / 不廣播事件")]
    [Tooltip("開啟後每次路由結果都寫入 Console（熱路徑，演出時請關閉）")]
    public bool debugLog = false;

    SensorTestManager  _manager;
    SensorFaceConfig[] _faceConfigs;

    void Awake()
    {
        _manager = GetComponent<SensorTestManager>();
        if (_manager == null)
            Debug.LogError("[SensorFaceRouter] 找不到同 GO 上的 SensorTestManager");
    }

    void OnEnable()  { UdpSensorReceiver.OnPacketReceived += OnPacket; }
    void OnDisable() { UdpSensorReceiver.OnPacketReceived -= OnPacket; }

    /// <summary>場域切換時由 SensorTestManager 呼叫，更新目前有效的面設定陣列。</summary>
    public void SetFaceConfigs(SensorFaceConfig[] configs)
    {
        _faceConfigs = configs;
    }

    /// <summary>CoordTestPanel / TriggerRawCoord 呼叫入口，路徑與 UDP 封包相同。</summary>
    public void RoutePacket(int rawX, int rawY) => OnPacket(rawX, rawY);

    void OnPacket(int rawX, int rawY)
    {
        if (_manager == null || _faceConfigs == null) return;

        // 面分類 + 座標轉換
        for (int fi = 0; fi < _faceConfigs.Length; fi++)
        {
            var cfg = _faceConfigs[fi];
            if (cfg == null || !cfg.ContainsRaw(rawX, rawY)) continue;

            var (px, py)   = cfg.RawToPixel(rawX, rawY);
            var (col, row) = cfg.PixelToCell(px, py);
            int cellIdx    = row * cfg.colCount + col;

            // 鎖定過濾：背景執行緒已用 snapshot 過濾，此處在主執行緒再確認（捕捉 snapshot 延遲窗口）
            if (_manager.IsRawPixelLocked(rawX, rawY)) return;

            if (debugLog)
                Debug.Log($"[SensorFaceRouter] raw({rawX},{rawY}) → face{fi} cell{cellIdx} ({col},{row})");

            _manager.DispatchSensorCell(rawX, rawY, fi, cellIdx);
            return;
        }

        _manager.OnRawOutOfRange(rawX, rawY);
    }
}
