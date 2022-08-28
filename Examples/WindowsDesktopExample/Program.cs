using Immense.RemoteControl.Desktop.Windows.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    cts.Cancel();
};


var services = new ServiceCollection();

await services.AddRemoteControlClient(
    args,
    //"https://localhost:7024",
    "",
    cts.Token);

var provider = services.BuildServiceProvider();

// Do other app startup stuff.

Console.WriteLine("Press Ctrl + C to exit.");
await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);