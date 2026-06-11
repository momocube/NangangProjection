using System.Text.Json;
using TouchService.Core;
using TouchService.Tracking;

namespace TouchService.Output;

/// <summary>
/// 對外協議 v1 frame 序列化。schema 詳見 docs/PROTOCOL.md：
/// {"v":1,"seq":N,"t":unix_ms,"venue":"704","canvas":{"w":2688,"h":3840},
///  "events":[{"id":42,"phase":"down","face":2,"cell":[col,row],"px":[x,y],"norm":[nx,ny]}],
///  "alive":[42,43]}
/// 每包帶 alive 全清單（TUIO 語意，up 丟包不殘留鬼點）。
/// </summary>
public sealed class FrameSerializer
{
    readonly string _venue;
    readonly int _canvasW;
    readonly int _canvasH;
    readonly MemoryStream _stream = new(4096);

    public FrameSerializer(VenueProfile profile)
    {
        _venue = profile.Venue;
        _canvasW = profile.Canvas.Width;
        _canvasH = profile.Canvas.Height;
    }

    static string PhaseName(TouchPhase p) => p switch
    {
        TouchPhase.Down => "down",
        TouchPhase.Move => "move",
        TouchPhase.Up   => "up",
        _ => "unknown",
    };

    /// <summary>序列化一個 frame。非執行緒安全（呼叫端持 pipeline 鎖）。</summary>
    public byte[] Serialize(long seq, long unixMs, List<TouchEventData> events, List<int> alive)
    {
        _stream.SetLength(0);
        using (var w = new Utf8JsonWriter(_stream))
        {
            w.WriteStartObject();
            w.WriteNumber("v", 1);
            w.WriteNumber("seq", seq);
            w.WriteNumber("t", unixMs);
            w.WriteString("venue", _venue);

            w.WriteStartObject("canvas");
            w.WriteNumber("w", _canvasW);
            w.WriteNumber("h", _canvasH);
            w.WriteEndObject();

            w.WriteStartArray("events");
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                w.WriteStartObject();
                w.WriteNumber("id", e.Id);
                w.WriteString("phase", PhaseName(e.Phase));
                w.WriteNumber("face", e.Point.FaceIndex);

                w.WriteStartArray("cell");
                w.WriteNumberValue(e.Point.Col);
                w.WriteNumberValue(e.Point.Row);
                w.WriteEndArray();

                w.WriteStartArray("px");
                w.WriteNumberValue(e.Point.PixelX);
                w.WriteNumberValue(e.Point.PixelY);
                w.WriteEndArray();

                w.WriteStartArray("norm");
                w.WriteNumberValue(MathF.Round(e.Point.NormX, 5));
                w.WriteNumberValue(MathF.Round(e.Point.NormY, 5));
                w.WriteEndArray();

                w.WriteEndObject();
            }
            w.WriteEndArray();

            w.WriteStartArray("alive");
            for (int i = 0; i < alive.Count; i++)
                w.WriteNumberValue(alive[i]);
            w.WriteEndArray();

            w.WriteEndObject();
        }
        return _stream.ToArray();
    }
}
