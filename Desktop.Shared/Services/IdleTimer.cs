using Microsoft.Extensions.Logging;
using System.Timers;

namespace Immense.RemoteControl.Desktop.Shared.Services;

public interface IIdleTimer
{
    DateTimeOffset ViewersLastSeen { get; }

    void Start();
    void Stop();
}

public class IdleTimer : IIdleTimer
{
    private readonly IAppState _appState;
    private readonly ILogger<IdleTimer> _logger;
    private System.Timers.Timer? _timer;

    public IdleTimer(IAppState appState, ILogger<IdleTimer> logger)
    {
        _appState = appState;
        _logger = logger;
    }


    public DateTimeOffset ViewersLastSeen { get; private set; } = DateTimeOffset.Now;


    public void Start()
    {
        _timer?.Dispose();
        _timer = new System.Timers.Timer(100);
        _timer.Elapsed += Timer_Elapsed;
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_appState.Viewers.IsEmpty)
        {
            ViewersLastSeen = DateTimeOffset.Now;
        }
        else if (DateTimeOffset.Now - ViewersLastSeen > TimeSpan.FromSeconds(30))
        {
            _logger.LogWarning("No viewers connected after 30 seconds.  Shutting down.");
            Environment.Exit(0);
        }
    }
}
