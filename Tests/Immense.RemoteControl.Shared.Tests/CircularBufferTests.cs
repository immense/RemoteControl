using Immense.RemoteControl.Shared.IO;

namespace Immense.RemoteControl.Shared.Tests;

[TestClass]
public class CircularBufferTests
{
    [TestMethod]
    public async Task ReadWrite_GivenExpectedConditions_OK()
    {
        var buffer = new CircularBuffer<int>(3, 10, x => x, TimeSpan.FromSeconds(1));

        var startSignal = new ManualResetEventSlim(false);

        var writeTask = Task.Run(async () =>
        {
            startSignal.Wait();
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    var result = await buffer.TryWrite(1);
                    if (!result.IsSuccess)
                    {
                        return result;
                    }

                    result = await buffer.TryWrite(2);
                    if (!result.IsSuccess)
                    {
                        return result;
                    }

                    result = await buffer.TryWrite(3);
                    if (!result.IsSuccess)
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    return Result.Fail(ex);
                }
            }
            return Result.Ok();
        });

        var readTask = Task.Run(async () =>
        {
            var readResult = true;
            startSignal.Wait();
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    var result = await buffer.TryRead();
                    Assert.IsTrue(result.IsSuccess);
                    Assert.AreEqual(1, result.Value);

                    result = await buffer.TryRead();
                    Assert.IsTrue(result.IsSuccess);
                    Assert.AreEqual(2, result.Value);

                    result = await buffer.TryRead();
                    Assert.IsTrue(result.IsSuccess);
                    Assert.AreEqual(3, result.Value);
                }
                catch
                {
                    return false;
                }
            }
            return readResult;
        });

        await Task.Delay(10);

        startSignal.Set();

        await Task.WhenAll(writeTask, readTask);

        Assert.IsTrue(writeTask.Result.IsSuccess);
        Assert.IsTrue(readTask.Result);
    }

    [TestMethod]
    public async Task ReadWrite_GivenReaderStops_WriterTimesOut()
    {
        var buffer = new CircularBuffer<int>(3, 10, x => x, TimeSpan.FromSeconds(1));

        var startSignal = new ManualResetEventSlim(false);

        var writeTask = Task.Run(async () =>
        {
            startSignal.Wait();
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    var result = await buffer.TryWrite(1);
                    if (!result.IsSuccess)
                    {
                        return result;
                    }

                    result = await buffer.TryWrite(2);
                    if (!result.IsSuccess)
                    {
                        return result;
                    }

                    result = await buffer.TryWrite(3);
                    if (!result.IsSuccess)
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    return Result.Fail(ex);
                }
            }
            return Result.Ok();
        });

        var readTask = Task.Run(async () =>
        {
            var readResult = true;
            startSignal.Wait();
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    var result = await buffer.TryRead();
                    Assert.IsTrue(result.IsSuccess);
                    Assert.AreEqual(1, result.Value);

                    result = await buffer.TryRead();
                    Assert.IsTrue(result.IsSuccess);
                    Assert.AreEqual(2, result.Value);

                    result = await buffer.TryRead();
                    Assert.IsTrue(result.IsSuccess);
                    Assert.AreEqual(3, result.Value);
                }
                catch
                {
                    return false;
                }
            }
            return readResult;
        });

        await Task.Delay(10);

        startSignal.Set();

        await Task.WhenAll(writeTask, readTask);

        Assert.IsFalse(writeTask.Result.IsSuccess);
        Assert.IsTrue(writeTask.Result.Reason.Contains("Buffer is full."));
        Assert.IsTrue(readTask.Result);
    }
}