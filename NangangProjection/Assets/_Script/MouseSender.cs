using UnityEngine;
using OscJack; // 引用 OscJack

public class MouseSender : MonoBehaviour
{
    // 設定 TD 的 IP (本機就用 127.0.0.1) 和 Port
    public string ipAddress = "127.0.0.1";
    public int port = 9000;

    // OSC 地址名稱
    public string oscAddress = "/unity/mouse";

    OscClient _client;

    void Start()
    {
        // 建立連線
        _client = new OscClient(ipAddress, port);
    }

    void Update()
    {
        // 1. 獲取滑鼠位置 (像素座標)
        Vector3 mousePos = Input.mousePosition;

        // 2. 轉換為 0~1 的比例座標 (Viewport Coordinates)
        // 這樣 TD 即使解析度改變，數值也是 0 (左下) 到 1 (右上)
        // 如果想要 -1 到 1，也可以在這裡做數學運算
        float x = mousePos.x / Screen.width;
        float y = mousePos.y / Screen.height;

        // 3. 發送數據 (X 和 Y 一起發)
        _client.Send(oscAddress, x, y);
    }

    void OnDestroy()
    {
        // 關閉時釋放連線
        if (_client != null) _client.Dispose();
    }
}