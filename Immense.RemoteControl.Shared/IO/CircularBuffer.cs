using Immense.RemoteControl.Shared.Helpers;
using System.Collections.Concurrent;

namespace Immense.RemoteControl.Shared.IO;

/// <summary>
/// A circular buffer implementation that allows flow control based
/// on buffer capacity, total data size, and oldest item age.
/// </summary>
/// <typeparam name="T"></typeparam>
public class CircularBuffer<T> : IDisposable
{
    private readonly T[] _buffer;
    private readonly ConcurrentQueue<DateTimeOffset> _writeTimes = new();
    private readonly Func<T, int> _itemDataSizeFunc;
    private readonly int _maxDataSize;
    private readonly SemaphoreSlim _readLock = new(1, 1);
    private readonly TimeSpan _readTimeout;
    private readonly TimeSpan _maxItemAge;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly TimeSpan _writeTimeout;
    private volatile int _dataSize;
    private int _readIndex;
    private int _writeIndex;
    private bool disposedValue;

    /// <summary>
    /// See full constructor for details.
    /// </summary>
    public CircularBuffer(
        int bufferCapacity,
        int maxDataSize,
        Func<T, int> itemDataSizeFunc,
        TimeSpan readWriteTimeout)
        : this(bufferCapacity, maxDataSize, itemDataSizeFunc, readWriteTimeout, TimeSpan.MaxValue)
    {
    }

    /// <summary>
    /// See full constructor for details.
    /// </summary>
    public CircularBuffer(
       int bufferCapacity,
       int maxDataSize,
       Func<T, int> itemDataSizeFunc,
       TimeSpan readWriteTimeout,
       TimeSpan maxItemAge)
       : this(bufferCapacity, maxDataSize, itemDataSizeFunc, readWriteTimeout, readWriteTimeout, maxItemAge)
    {
    }

    /// <summary>
    /// A circular buffer implementation that allows flow control based
    /// on buffer capacity, total data size, and oldest item age.
    /// </summary>
    /// <param name="bufferCapacity">
    ///     The total number of items that can be stored in the buffer at a given time.
    /// </param>
    /// <param name="maxDataSize">
    ///     The maximum data size of all unread items in the buffer. If the
    ///     data size is exceeded, writes will be halted until the buffer is read.
    /// </param>
    /// <param name="itemDataSizeFunc">
    ///     A function used to get the data size of an item.
    /// </param>
    /// <param name="writeTimeout">
    ///     The maximum time to wait for the buffer to be writable.
    /// </param>
    /// <param name="readTimeout">
    ///     The maximum time to wait for the buffer to be readable.
    /// </param>
    /// <param name="maxItemAge">
    ///     The maximum age of an item in the buffer.  If the maximum age 
    ///     is exceeded, writes will be halted until the buffer is read.
    /// </param>
    public CircularBuffer(
        int bufferCapacity,
        int maxDataSize,
        Func<T, int> itemDataSizeFunc,
        TimeSpan writeTimeout, 
        TimeSpan readTimeout,
        TimeSpan maxItemAge)
    {
        _buffer = new T[bufferCapacity];
        _maxDataSize = maxDataSize;
        _itemDataSizeFunc = itemDataSizeFunc;
        _writeTimeout = writeTimeout;
        _readTimeout = readTimeout;
        _maxItemAge = maxItemAge;
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async Task<Result<T>> TryRead()
    {
        await _readLock.WaitAsync();
        try
        {
            var waitResult = await WaitHelper.WaitForAsync(
                condition: () => _readIndex != _writeIndex,
                timeout: _readTimeout,
                pollingMs: 1);

            if (!waitResult)
            {
                return Result.Fail<T>("Timed out while waiting for data.");
            }

            var item = _buffer[_readIndex];
            Array.Clear(_buffer, _readIndex, 1);
            _writeTimes.TryDequeue(out _);
            var itemSize = _itemDataSizeFunc(item);
            Interlocked.Exchange(ref _dataSize, _dataSize - itemSize);

            _readIndex = GetNextIndex(_readIndex);
            return Result.Ok(item);
        }
        finally
        {
            _readLock.Release();
        }
    }

    public async Task<Result> TryWrite(T item)
    {
        await _writeLock.WaitAsync();
        try
        {
            var next = GetNextIndex(_writeIndex);

            var waitResult = await WaitHelper.WaitForAsync(
                condition: () =>
                {
                    if (_dataSize >= _maxDataSize ||
                        next == _readIndex)
                    {
                        return false;
                    }

                    if (_writeTimes.TryPeek(out var oldest))
                    {
                        return DateTimeOffset.Now - oldest < _maxItemAge;
                    }

                    return true;
                },
                timeout: _writeTimeout,
                pollingMs: 1);

            if (!waitResult)
            {
                var message = "Timed out while trying to write.";
                if (next == _readIndex)
                {
                    return Result.Fail($"{message}  Buffer is full.");
                }

                if (_dataSize >= _maxDataSize)
                {
                    return Result.Fail($"{message}  Max data size exceeded.");
                }

                if (_writeTimes.TryPeek(out var oldest) &&
                    DateTimeOffset.Now - oldest < _maxItemAge)
                {
                    return Result.Fail($"{message}  Max item age exceeded.");
                }
            }

            var itemSize = _itemDataSizeFunc(item);
            Interlocked.Exchange(ref _dataSize, _dataSize + itemSize);

            _buffer[_writeIndex] = item;
            _writeIndex = next;
            _writeTimes.Enqueue(DateTimeOffset.Now);

            return Result.Ok();
        }
        finally
        {
            _writeLock.Release();
        }
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _readLock.Dispose();
                _writeLock.Dispose();
            }

            Array.Clear(_buffer);
            disposedValue = true;
        }
    }

    private int GetNextIndex(int currentIndex)
    {
        var next = currentIndex + 1;
        if (next >= _buffer.Length)
        {
            next = 0;
        }
        return next;
    }
}
