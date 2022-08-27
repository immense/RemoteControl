using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared.Models.Dtos
{
    [DataContract]
    public class WindowsSessionsDto : BaseDto
    {
        public WindowsSessionsDto(List<WindowsSession> windowsSessions)
        {
            WindowsSessions = windowsSessions;
        }


        [DataMember(Name = "WindowsSessions")]
        public List<WindowsSession> WindowsSessions { get; set; }


        [DataMember(Name = "DtoType")]
        public override DtoType DtoType { get; init; } = DtoType.WindowsSessions;
    }
}
