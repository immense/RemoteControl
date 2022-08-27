using Microsoft.Win32;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    cts.Cancel();
};

SystemEvents.SessionSwitch += (s, e) =>
{
    Console.WriteLine($"Session changed.  Reason: {e.Reason}");
};

SystemEvents.SessionEnding += (s, e) =>
{
    Console.WriteLine($"Session ending.  Reason: {e.Reason}");
};

Console.WriteLine("Press Ctrl + C to quit.");
await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);
