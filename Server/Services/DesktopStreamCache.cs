using Immense.RemoteControl.Server.Models;
using Immense.RemoteControl.Shared;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Immense.RemoteControl.Server.Services;

public interface IDesktopStreamCache
{
    void AddOrUpdate(Guid streamId, StreamSignaler signaler, Func<Guid, StreamSignaler, StreamSignaler> updateFactory);

    StreamSignaler GetOrAdd(Guid streamId, Func<Guid, StreamSignaler> createFactory);

    bool TryGet(Guid streamId, out StreamSignaler signaler);
    void TryRemove(Guid streamId, out StreamSignaler signaler);
    Task<Result<StreamSignaler>> WaitForStreamSession(Guid streamId, TimeSpan timeout);
}

public class DesktopStreamCache : IDesktopStreamCache
{
    private static readonly ConcurrentDictionary<Guid, StreamSignaler> _streamingSessions = new();
    private readonly ILogger<DesktopStreamCache> _logger;

    public DesktopStreamCache(ILogger<DesktopStreamCache> logger)
    {
        _logger = logger;
    }

    public void AddOrUpdate(Guid streamId, StreamSignaler signaler, Func<Guid, StreamSignaler, StreamSignaler> updateFactory)
    {
        _streamingSessions.AddOrUpdate(streamId, signaler, updateFactory);
    }

    public StreamSignaler GetOrAdd(Guid streamId, Func<Guid, StreamSignaler> createFactory)
    {
        return _streamingSessions.GetOrAdd(streamId, createFactory);
    }

    public bool TryGet(Guid streamId, out StreamSignaler signaler)
    {
        if (_streamingSessions.TryGetValue(streamId, out var result))
        {
            signaler = result;
            return true;
        }
        signaler = StreamSignaler.Empty;
        return false;
    }
    public void TryRemove(Guid streamId, out StreamSignaler signaler)
    {
        _streamingSessions.TryRemove(streamId, out var result);
        signaler = result ?? StreamSignaler.Empty;
    }

    public async Task<Result<StreamSignaler>> WaitForStreamSession(Guid streamId, TimeSpan timeout)
    {
        var session = _streamingSessions.GetOrAdd(streamId, key => new StreamSignaler(streamId));
        var waitResult = await session.ReadySignal.WaitAsync(timeout);

        if (!waitResult)
        {
            _logger.LogError("Timed out while waiting for session.");
            return Result.Fail<StreamSignaler>("Timed out while waiting for session.");
        }

        if (session.Stream is null)
        {
            _logger.LogError("Stream failed to start.");
            return Result.Fail<StreamSignaler>("Stream failed to start.");
        }

        return Result.Ok(session);
    }
}
