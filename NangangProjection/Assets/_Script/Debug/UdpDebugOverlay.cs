using UnityEngine;
using TMPro;

/// <summary>
/// 在 Game 畫面右上角即時顯示 UDP 接收狀態，
/// 不需要切 Console tab 即可確認訊號是否進來。
/// </summary>
public class UdpDebugOverlay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;

    private int   _packetCount;
    private float _lastReceiveTime = -1f;
    private string _lastCoords = "--";

    void OnEnable()
    {
        UdpSensorReceiver.OnPacketReceived += HandlePacket;
    }

    void OnDisable()
    {
        UdpSensorReceiver.OnPacketReceived -= HandlePacket;
    }

    private void HandlePacket(int x, int y)
    {
        _packetCount++;
        _lastReceiveTime = Time.realtimeSinceStartup;
        _lastCoords = $"X={x}  Y={y}";
    }

    void Update()
    {
        if (statusText == null) return;

        float elapsed = _lastReceiveTime < 0
            ? -1f
            : Time.realtimeSinceStartup - _lastReceiveTime;

        string sinceStr = _lastReceiveTime < 0
            ? "尚未收到"
            : $"{elapsed:0.0}s 前";

        statusText.text =
            $"<b>UDP port 20001</b>\n" +
            $"封包數：{_packetCount}\n" +
            $"最後座標：{_lastCoords}\n" +
            $"最後收到：{sinceStr}";

        // 超過 1 秒沒收到 → 紅字；收到中 → 綠字
        statusText.color = (elapsed >= 0 && elapsed < 1f)
            ? new Color(0.2f, 1f, 0.4f)
            : new Color(1f, 0.4f, 0.4f);
    }
}
