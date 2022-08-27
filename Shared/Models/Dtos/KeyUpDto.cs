using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared.Models.Dtos
{
    [DataContract]
    public class KeyUpDto : BaseDto
    {
        [DataMember(Name = "Key")]
        public string Key { get; set; } = string.Empty;

        [DataMember(Name = "DtoType")]
        public override DtoType DtoType { get; init; } = DtoType.KeyUp;
    }
}
