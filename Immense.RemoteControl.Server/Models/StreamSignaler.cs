using Immense.RemoteControl.Shared;
using Immense.RemoteControl.Shared.Helpers;
using Immense.RemoteControl.Shared.IO;

namespace Immense.RemoteControl.Server.Models;

public class StreamSignaler : IDisposable
{
    private readonly CircularBuffer<byte[]> _buffer = new(
        bufferCapacity: 20,
        maxDataSize: 500_000,
        itemDataSizeFunc: arr => arr.Length,
        readWriteTimeout: TimeSpan.FromSeconds(15),
        maxItemAge: TimeSpan.FromSeconds(2));

    private bool _disposedValue;
    private bool _streamEnded;

    public StreamSignaler(Guid streamId)
    {
        StreamId = streamId;
    }

    public string DesktopConnectionId { get; internal set; } = string.Empty;
    public SemaphoreSlim ReadySignal { get; } = new(0, 1);
    public bool StreamEnded => _streamEnded;
    public Guid StreamId { get; init; }
    public string ViewerConnectionId { get; internal set; } = string.Empty;

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void EndStream()
    {
        _streamEnded = true;
    }

    public TimeSpan GetViewerLag()
    {
        return _buffer.GetReadLag();
    }

    public async Task<Result<byte[]>> TryReadFromStream()
    {
        return await _buffer.TryRead();
    }

    public async Task<Result> WriteToStream(byte[] chunk)
    {
        return await _buffer.TryWrite(chunk);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _streamEnded = true;
                ReadySignal.Dispose();
                _buffer.Dispose();
            }
            _disposedValue = true;
        }
    }
}
