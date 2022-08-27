using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared.Models.Dtos
{
    [DataContract]
    public class TapDto : BaseDto
    {

        [DataMember(Name = "DtoType")]
        public override DtoType DtoType { get; init; } = DtoType.Tap;

        [DataMember(Name = "PercentX")]
        public double PercentX { get; set; }

        [DataMember(Name = "PercentY")]
        public double PercentY { get; set; }
    }
}
