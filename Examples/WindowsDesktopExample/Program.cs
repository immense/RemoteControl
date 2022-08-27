using Desktop.Windows;
using Microsoft.Win32;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    cts.Cancel();
};

Console.WriteLine("Press Ctrl + C to exit.");

return await Startup.Run(ex => 
    {
        Console.WriteLine($"Error: {ex.Message}");
    },
    cts.Token);
