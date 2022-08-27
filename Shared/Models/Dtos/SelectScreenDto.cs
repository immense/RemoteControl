using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared.Models.Dtos
{
    [DataContract]
    public class SelectScreenDto : BaseDto
    {
        [DataMember(Name = "DisplayName")]
        public string DisplayName { get; set; } = string.Empty;

        [DataMember(Name = "DtoType")]
        public override DtoType DtoType { get; init; } = DtoType.SelectScreen;
    }
}
