using Immense.RemoteControl.Server.Abstractions;
using Immense.RemoteControl.Server.Models;
using Immense.RemoteControl.Shared.Helpers;
using Microsoft.AspNetCore.SignalR;

namespace Immense.RemoteControl.Examples.ServerExample.Services;

// This is just a demo implementation.
public class SessionRecordingSink : ISessionRecordingSink
{
    private readonly IWebHostEnvironment _hostingEnv;

    public SessionRecordingSink(
        IWebHostEnvironment hostingEnv)
    {
        _hostingEnv = hostingEnv;
    }

    public async Task SinkWebmStream(
        IAsyncEnumerable<byte[]> webmStream, 
        RemoteControlSession session)
    {
        try
        {
            var appData = _hostingEnv.IsDevelopment() ?
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data") :
                Path.Combine(_hostingEnv.ContentRootPath, "App_Data");

            var recordingDir = Path.Combine(
                appData,
                "Recordings",
                $"{DateTimeOffset.Now:yyyy-MM-dd}");

            _ = Directory.CreateDirectory(recordingDir);

            var filePrefix = !string.IsNullOrWhiteSpace(session.RequesterName) ?
                $"{session.RequesterName}" :
                "UnknownUser";
            var fileName = 
                $"{filePrefix}_" +
                $"{DateTimeOffset.Now:yyyyMMdd_HHmmssfff}.webm";

            using var fs = new FileStream(Path.Combine(recordingDir, fileName), FileMode.Create);

            await foreach (var chunk in webmStream)
            {
                await fs.WriteAsync(chunk);
            }
        }
        catch (OperationCanceledException)
        {
            // Log info.
        }
        catch (Exception)
        {
            // Log error.
        }
    }
}
