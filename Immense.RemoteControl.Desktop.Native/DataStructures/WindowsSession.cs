using System.Runtime.Serialization;

namespace Immense.RemoteControl.Desktop.Native.DataStructures;

[DataContract]
public enum SessionType
{
    Console = 0,
    RDP = 1
}

[DataContract]
public class WindowsSession
{
    [DataMember(Name = "ID")]
    public uint Id { get; set; }
    [DataMember(Name = "Name")]
    public string Name { get; set; } = string.Empty;
    [DataMember(Name = "Type")]
    public SessionType Type { get; set; }
    [DataMember(Name = "Username")]
    public string Username { get; set; } = string.Empty;
}
