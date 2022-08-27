using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared.Models.Dtos
{
    [DataContract]
    public class ScreenSizeDto : BaseDto
    {
        public ScreenSizeDto(int width, int height)
        {
            Width = width;
            Height = height;
        }

        [DataMember(Name = "Width")]
        public int Width { get; }

        [DataMember(Name = "Height")]
        public int Height { get; }

        [DataMember(Name = "DtoType")]
        public override DtoType DtoType { get; init; } = DtoType.ScreenSize;
    }
}
