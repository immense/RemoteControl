using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared.Models.Dtos
{
    [DataContract]
    public class MouseMoveDto : BaseDto
    {

        [DataMember(Name = "DtoType")]
        public override DtoType DtoType { get; init; } = DtoType.MouseMove;

        [DataMember(Name = "PercentX")]
        public double PercentX { get; set; }

        [DataMember(Name = "PercentY")]
        public double PercentY { get; set; }
    }
}
