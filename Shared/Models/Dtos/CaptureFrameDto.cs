using System;
using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared.Models.Dtos
{
    [DataContract]
    public class CaptureFrameDto
    {

        [DataMember(Name = "EndOfFrame")]
        public bool EndOfFrame { get; init; }

        [DataMember(Name = "Height")]
        public int Height { get; init; }

        [DataMember(Name = "ImageBytes")]
        public byte[] ImageBytes { get; init; } = Array.Empty<byte>();

        [DataMember(Name = "Left")]
        public int Left { get; init; }
        [DataMember(Name = "Top")]
        public int Top { get; init; }
        [DataMember(Name = "Width")]
        public int Width { get; init; }

        [DataMember(Name = "Sequence")]
        public long Sequence { get; init; }
    }
}
