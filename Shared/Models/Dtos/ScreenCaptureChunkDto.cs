using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared.Models.Dtos;

[DataContract]
public class ScreenCaptureChunkDto
{

    [DataMember]
    public int Height { get; init; }

    [DataMember]
    public byte[] ImageChunk { get; init; } = Array.Empty<byte>();

    [DataMember]
    public bool IsFirstChunk { get; init; }

    [DataMember]
    public bool IsLastChunk { get; init; }

    [DataMember]
    public int Left { get; init; }

    [DataMember]
    public int Top { get; init; }

    [DataMember]
    public int Width { get; init; }
}
