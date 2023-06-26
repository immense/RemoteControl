using Castle.Core.Logging;
using Immense.RemoteControl.Server.Abstractions;
using Immense.RemoteControl.Server.Models;
using Immense.RemoteControl.Server.Services;
using Immense.RemoteControl.Shared.Helpers;
using Immense.RemoteControl.Shared.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Immense.RemoteControl.Server.Tests;

[TestClass]
public class RemoteControlSessionCacheTests
{
#nullable disable
    private SystemTime _systemTime;
    private Mock<IHubEventHandler> _hubEventHandler;
    private Mock<ILogger<RemoteControlSessionCache>> _logger;
    private RemoteControlSessionCache _sessionCache;
#nullable enable

    [TestInitialize]
    public void Init()
    {
        _systemTime = new SystemTime();
        _hubEventHandler = new Mock<IHubEventHandler>();
        _logger = new Mock<ILogger<RemoteControlSessionCache>>();

        _sessionCache = new RemoteControlSessionCache(_systemTime, _hubEventHandler.Object, _logger.Object);
    }

    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 0)]
    [DataRow(1, 1)]
    [DataRow(5, 3)]
    [DataRow(10, 10)]
    [DataRow(20, 10)]
    public async Task AddAndRemove_GivenMultipleConcurrentOperations_OK(int sessionsToAdd, int sessionsToRemove)
    {
        var startSignal = new ManualResetEvent(false);

        var addOrUpdateTask = Task.Run(() =>
        {
            startSignal.WaitOne();
            for (var i = 0; i < sessionsToAdd; i++)
            {
                var session = new RemoteControlSession();
                _sessionCache.AddOrUpdate($"{i}", session);
            }
        });

        var getOrAddTask = Task.Run(() =>
        {
            startSignal.WaitOne();
            for (var i = 0; i < sessionsToAdd; i++)
            {
                var session = new RemoteControlSession();
                _sessionCache.GetOrAdd($"{i}", key => session);
            }
        });

        var tryAddTask = Task.Run(() =>
        {
            startSignal.WaitOne();
            for (var i = 0; i < sessionsToAdd; i++)
            {
                var session = new RemoteControlSession();
                _sessionCache.TryAdd($"{i}", session);
            }
        });


        startSignal.Set();

        await Task.WhenAll(addOrUpdateTask, tryAddTask, getOrAddTask);

        _hubEventHandler.Verify(
            x => x.NotifyDesktopSessionAdded(It.IsAny<RemoteControlSession>()),
            Times.Exactly(sessionsToAdd));

        Assert.AreEqual(sessionsToAdd, _sessionCache.Sessions.Count());

        for (var i = 0; i < sessionsToRemove; i++)
        {
            _sessionCache.TryRemove($"{i}", out _);
        }

        Assert.AreEqual(sessionsToAdd - sessionsToRemove, _sessionCache.Sessions.Count());
       
        _hubEventHandler.Verify(
            x => x.NotifyDesktopSessionRemoved(It.IsAny<RemoteControlSession>()),
            Times.Exactly(sessionsToRemove));
    }
}
