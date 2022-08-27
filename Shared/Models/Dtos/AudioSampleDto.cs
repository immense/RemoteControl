using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared.Models.Dtos
{
    [DataContract]
    public class AudioSampleDto : BaseDto
    {
        public AudioSampleDto(byte[] buffer)
        {
            Buffer = buffer;
        }

        [DataMember(Name = "Buffer")]
        public byte[] Buffer { get; }


        [DataMember(Name = "DtoType")]
        public override DtoType DtoType { get; init; } = DtoType.AudioSample;

    }
}
