using Immense.RemoteControl.Desktop.Shared.Abstractions;

namespace Immense.RemoteControl.Desktop.Linux.Services;

public class AudioCapturerLinux : IAudioCapturer
{
    public event EventHandler<byte[]>? AudioSampleReady;

    public void ToggleAudio(bool toggleOn)
    {
        // Not implemented.
    }
}
