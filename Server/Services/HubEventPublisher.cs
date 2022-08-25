using Immense.RemoteControl.Server.Models;
using Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Services
{
    public interface IHubEventPublisher
    {
        IDisposable OnRestartScreenCasterRequired(Func<RestartScreenCasterRequiredModel, Task> callback);
        IDisposable OnUnattendedSessionReady(Func<UnattendedSessionReadyModel, Task> callback);
        internal Task InvokeUnattendedSessionReady(UnattendedSessionReadyModel sessionModel);
        internal Task InvokeRestartScreenCasterRequired(RestartScreenCasterRequiredModel restartScreenCasterRequiredModel);
    }

    public interface IHubEvent
    {

    }
    internal class HubEventPublisher : IHubEventPublisher
    {
        private readonly ConcurrentList<Func<UnattendedSessionReadyModel, Task>> _unattendedSessionReadyCallbacks = new();
        private readonly ConcurrentList<Func<RestartScreenCasterRequiredModel, Task>> _restartScreenCasterCallbacks = new();

     
        public IDisposable OnRestartScreenCasterRequired(Func<RestartScreenCasterRequiredModel, Task> callback)
        {
            _restartScreenCasterCallbacks.Add(callback);

            return new CallbackDisposable(() =>
            {
                _restartScreenCasterCallbacks.Remove(callback);
            });
        }

        public IDisposable OnUnattendedSessionReady(Func<UnattendedSessionReadyModel, Task> callback)
        {
            _unattendedSessionReadyCallbacks.Add(callback);

            return new CallbackDisposable(() =>
            {
                _unattendedSessionReadyCallbacks.Remove(callback);
            });
        }

        async Task IHubEventPublisher.InvokeUnattendedSessionReady(UnattendedSessionReadyModel sessionModel)
        {
            foreach (var callback in _unattendedSessionReadyCallbacks)
            {
                await callback.Invoke(sessionModel);
            }
        }

        async Task IHubEventPublisher.InvokeRestartScreenCasterRequired(RestartScreenCasterRequiredModel restartModel)
        {
            foreach (var callback in _restartScreenCasterCallbacks)
            {
                await callback.Invoke(restartModel);
            }
        }

    }
}
