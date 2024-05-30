﻿using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared.Models.Dtos;

[DataContract]
public class TextTransferDto
{

    [DataMember(Name = "Text")]
    public string Text { get; set; } = string.Empty;

    [DataMember(Name = "TypeText")]
    public bool TypeText { get; set; }
}
