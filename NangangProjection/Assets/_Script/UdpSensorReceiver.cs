using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>
/// 監聽 UDP port 20001，解析互動通信協議，
/// 將觸發像素座標傳給 TileManager.OnSensorPoint。
///
/// 協議格式（PDF）：
///   55 55 [frame:1] [dataSize_H:1][dataSize_L:1]
///   [X_H:1][X_L:1][Y_H:1][Y_L:1] × N點
///   [SUM_H:1][SUM_L:1]
/// </summary>
public class UdpSensorReceiver : MonoBehaviour
{
    [Header("▶ 地板感應接收 ─ 接收地板感應器的觸碰訊號。停用此元件 → 踩踏完全無反應")]
    [Header("網路設定")]
    [Tooltip("監聽 Port，預設 20001")]
    public int listenPort = 20001;

    [Header("偵錯")]
    [Tooltip("在 Console 顯示收到的封包與換算結果")]
    public bool debugLog = false;

    [Header("效能")]
    [Tooltip("每幀最多消化的觸碰點數，0 = 不限制（每幀清空佇列，最低延遲）。預設 0")]
    public int maxPointsPerFrame = 0;

    [Tooltip("背景接收佇列上限。超過時丟棄最舊的點，保留最新觸碰（預設 64）")]
    public int maxQueueSize = 64;

    /// <summary>每次解析到一個有效座標點時觸發（主執行緒）</summary>
    public static event Action<int, int> OnPacketReceived;

    public static UdpSensorReceiver Instance { get; private set; }

    private UdpClient     _udpClient;
    private Thread        _receiveThread;
    private volatile bool _running;

    // 執行緒安全佇列（儲存像素座標，由主執行緒每幀消化）
    private readonly Queue<Vector2Int> _pending     = new Queue<Vector2Int>();
    private readonly object            _lock        = new object();
    private readonly List<Vector2Int>  _batchBuffer = new List<Vector2Int>(64);

    private const byte HEADER      = 0x55;
    private const int  MIN_PKT_LEN = 7;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()  => StartListening();
    void OnDestroy()         => StopListening();
    void OnApplicationQuit() => StopListening();

    void Update()
    {
        lock (_lock)
        {
            if (_pending.Count == 0) return;
            int take = (maxPointsPerFrame <= 0)
                ? _pending.Count
                : Mathf.Min(_pending.Count, maxPointsPerFrame);
            for (int i = 0; i < take; i++)
                _batchBuffer.Add(_pending.Dequeue());
        }

        foreach (var pt in _batchBuffer)
        {
            if (debugLog)
                Debug.Log($"[UDP] pixel ({pt.x}, {pt.y})");

            TileManager.Instance?.OnSensorPoint(pt.x, pt.y);
            OnPacketReceived?.Invoke(pt.x, pt.y);
        }
        _batchBuffer.Clear();
    }

    public void StartListening()
    {
        if (_running) return;
        try
        {
            _udpClient     = new UdpClient(listenPort);
            _running       = true;
            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "UdpSensor" };
            _receiveThread.Start();
            Debug.Log($"[UdpSensorReceiver] 監聽 port {listenPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UdpSensorReceiver] 無法開啟 port {listenPort}：{e.Message}");
        }
    }

    public void StopListening()
    {
        _running = false;
        _udpClient?.Close();
        _udpClient = null;
        _receiveThread?.Join(500);
        _receiveThread = null;
    }

    private void ReceiveLoop()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        while (_running)
        {
            try
            {
                byte[] data = _udpClient.Receive(ref ep);
                ParsePacket(data);
            }
            catch (SocketException se)
            {
                if (_running) Debug.LogWarning($"[UdpSensorReceiver] Socket: {se.Message}");
            }
            catch (Exception e)
            {
                if (_running) Debug.LogError($"[UdpSensorReceiver] 錯誤：{e.Message}");
            }
        }
    }

    private void ParsePacket(byte[] data)
    {
        if (data == null || data.Length < MIN_PKT_LEN) return;
        if (data[0] != HEADER || data[1] != HEADER)
        {
            if (debugLog) Debug.LogWarning("[UDP] header 不符");
            return;
        }

        int dataSize    = (data[3] << 8) | data[4];
        if (dataSize % 4 != 0)
        {
            if (debugLog) Debug.LogWarning($"[UDP] dataSize={dataSize} 非 4 的倍數");
            return;
        }

        int expectedLen = 5 + dataSize + 2;
        if (data.Length < expectedLen)
        {
            if (debugLog) Debug.LogWarning("[UDP] 封包長度不足");
            return;
        }

        // 驗證 checksum
        int sum = 0;
        for (int i = 5; i < 5 + dataSize; i++) sum += data[i];
        int packetSum = (data[5 + dataSize] << 8) | data[5 + dataSize + 1];
        if ((sum & 0xFFFF) != packetSum)
        {
            if (debugLog) Debug.LogWarning($"[UDP] checksum 不符 計算={sum & 0xFFFF:X4} 封包={packetSum:X4}");
            return;
        }

        // 解析每個點，以像素座標形式存入佇列
        int count = dataSize / 4;
        var mgr = SensorTestManager.Instance;
        lock (_lock)
        {
            for (int i = 0; i < count; i++)
            {
                int offset = 5 + i * 4;
                int x = (data[offset]     << 8) | data[offset + 1];
                int y = (data[offset + 2] << 8) | data[offset + 3];
                // 鎖定格子的訊號在背景執行緒就丟棄，不進佇列、不消耗配額
                if (mgr != null && mgr.IsRawPixelLocked(x, y)) continue;

                // 佇列上限：丟棄最舊的點，保留最新觸碰（觸碰輸入永遠以最新狀態為準）
                while (_pending.Count >= maxQueueSize) _pending.Dequeue();
                _pending.Enqueue(new Vector2Int(x, y));

                if (debugLog) Debug.Log($"[UDP] 點[{i}] X={x} Y={y}");
            }
        }
    }
}
