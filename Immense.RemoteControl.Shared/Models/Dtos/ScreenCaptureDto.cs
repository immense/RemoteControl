using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared.Models.Dtos;

[DataContract]
public class ScreenCaptureDto
{

    [DataMember]
    public int Height { get; init; }

    [DataMember]
    public byte[] ImageBytes { get; init; } = Array.Empty<byte>();

    [DataMember]
    public Guid InstanceId { get; init; }

    [DataMember]
    public bool IsLastChunk { get; init; }

    [DataMember]
    public int Left { get; init; }

    [DataMember]
    public int Top { get; init; }

    [DataMember]
    public int Width { get; init; }
}
