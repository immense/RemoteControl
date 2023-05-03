
namespace Immense.RemoteControl.Server.Models;

public class StreamSignaler : IDisposable
{
    private bool _disposedValue;

    public StreamSignaler(Guid streamId)
    {
        StreamId = streamId;
    }

    public SemaphoreSlim EndSignal { get; } = new(0, 1);
    public SemaphoreSlim ReadySignal { get; } = new(0, 1);
    public IAsyncEnumerable<byte[]>? Stream { get; set; }
    public Guid StreamId { get; init; }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                EndSignal.Dispose();
                ReadySignal.Dispose();
            }

            _disposedValue = true;
        }
    }
}
