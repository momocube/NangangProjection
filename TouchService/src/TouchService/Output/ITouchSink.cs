namespace TouchService.Output;

/// <summary>
/// 觸控 frame 輸出端介面。v1 實作：UdpJsonPublisher、WebSocketHub。
/// TUIO 為預留插槽（TuioPublisher），v1 不實作。
/// </summary>
public interface ITouchSink : IDisposable
{
    /// <summary>發布一個 frame（UTF-8 JSON）。實作必須非阻塞或快速返回（在 receive 熱路徑上被呼叫）。</summary>
    void Publish(byte[] utf8Json);
}
