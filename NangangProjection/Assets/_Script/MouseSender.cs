using UnityEngine;
using OscJack;

public class MouseSender : MonoBehaviour
{
    [Header("OSC Settings")]
    public string ipAddress = "127.0.0.1";
    public int port = 9000;

    [Header("OSC Addresses")]
    public string posAddress = "/unity/mouse";      // 傳送位置用
    public string clickAddress = "/unity/rightclick"; // 傳送右鍵用

    OscClient _client;

    void Start()
    {
        _client = new OscClient(ipAddress, port);
    }

    void Update()
    {
        // --- 處理座標 (解決極限不為 0 的問題) ---
        Vector3 mousePos = Input.mousePosition;

        // 使用 Mathf.Clamp01 強制將數值鎖定在 0.0 到 1.0 之間
        // 這樣就不會出現 -0.01 或 1.02 這種奇怪的數值
        float x = Mathf.Clamp01(mousePos.x / Screen.width);
        float y = Mathf.Clamp01(mousePos.y / Screen.height);

        // --- 處理右鍵 (新增功能) ---
        // GetMouseButton(1) 代表右鍵 (0是左鍵, 2是中鍵)
        // 如果按住，回傳 1；沒按，回傳 0
        int rightClick = Input.GetMouseButton(1) ? 1 : 0;

        // --- 發送訊號 ---

        // 1. 發送座標 (位置: /unity/mouse, 數值: float x, float y)
        _client.Send(posAddress, x, y);

        // 2. 發送點擊 (位置: /unity/rightclick, 數值: int 0 或 1)
        _client.Send(clickAddress, rightClick);
    }

    void OnDestroy()
    {
        if (_client != null) _client.Dispose();
    }
}