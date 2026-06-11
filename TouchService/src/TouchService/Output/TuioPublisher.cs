using TouchService.Logging;

namespace TouchService.Output;

/// <summary>
/// TUIO 1.1（/tuio/2Dcur，OSC over UDP）輸出插槽。
/// v1 規格保留介面但不實作——config tuioOutput.enabled = true 時啟動會直接報錯，
/// 防止以為有 TUIO 輸出實際上沒有。
/// 未來實作要點（給接手者）：
///   - OSC bundle：alive（全 ID 清單）+ set（per-touch s/x/y/X/Y/m）+ fseq
///   - x/y 直接用 TouchEventData.Point.NormX/NormY（已是 0-1 正規化）
///   - session id 直接用 touch id（單調遞增，符合 TUIO session 語意）
/// </summary>
public sealed class TuioPublisher : ITouchSink
{
    public TuioPublisher(string endpoint, FileLogger log)
    {
        throw new NotImplementedException(
            "TUIO 輸出 v1 尚未實作（規格預留插槽）。請把 config.json 的 tuioOutput.enabled 設為 false。");
    }

    public void Publish(byte[] utf8Json) => throw new NotImplementedException();
    public void Dispose() { }
}
