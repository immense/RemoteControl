using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared.Models.Dtos
{
    [DataContract]
    public class ScreenDataDto : BaseDto
    {
        [DataMember(Name = "DisplayNames")]
        public IEnumerable<string> DisplayNames { get; init; } = Enumerable.Empty<string>();


        [DataMember(Name = "DtoType")]
        public override DtoType DtoType { get; init; } = DtoType.ScreenData;

        [DataMember(Name = "SelectedDisplay")]
        public string SelectedDisplay { get; init; } = string.Empty;

        [DataMember(Name = "MachineName")]
        public string MachineName { get; init; } = string.Empty;

        [DataMember(Name = "ScreenWidth")]
        public int ScreenWidth { get; init; }

        [DataMember(Name = "ScreenHeight")]
        public int ScreenHeight { get; init; }
    }
}
