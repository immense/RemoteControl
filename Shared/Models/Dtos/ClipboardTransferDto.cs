using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared.Models.Dtos
{
    [DataContract]
    public class ClipboardTransferDto : BaseDto
    {

        [DataMember(Name = "Text")]
        public string Text { get; set; } = string.Empty;

        [DataMember(Name = "TypeText")]
        public bool TypeText { get; set; }


        [DataMember(Name = "DtoType")]
        public override DtoType DtoType { get; init; } = DtoType.ClipboardTransfer;
    }
}
