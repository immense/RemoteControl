using Immense.RemoteControl.Server.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Extensions
{
    public interface IRemoteControlServerBuilder
    {
        void AddHubEventHandler<T>()
            where T : class, IHubEventHandler;

        void AddServiceHubSessionCache<T>()
            where T : class, IServiceHubSessionCache;

        void AddViewerAuthorizer<T>()
            where T : class, IViewerAuthorizer;

        void AddViewerHubDataProvider<T>()
            where T : class, IViewerHubDataProvider;

        void AddViewerPageDataProvider<T>()
            where T : class, IViewerPageDataProvider;
    }

    internal class RemoteControlServerBuilder : IRemoteControlServerBuilder
    {
        private readonly IServiceCollection _services;

        public RemoteControlServerBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public void AddHubEventHandler<T>() 
            where T : class, IHubEventHandler
        {
            _services.AddScoped<IHubEventHandler, T>();
        }

        public void AddServiceHubSessionCache<T>() 
            where T : class, IServiceHubSessionCache
        {
            _services.AddSingleton<IServiceHubSessionCache, T>();
        }

        public void AddViewerAuthorizer<T>() 
            where T : class, IViewerAuthorizer
        {
            _services.AddSingleton<IViewerAuthorizer, T>();
        }

        public void AddViewerHubDataProvider<T>() 
            where T : class, IViewerHubDataProvider
        {
            _services.AddScoped<IViewerHubDataProvider, T>();
        }

        public void AddViewerPageDataProvider<T>() 
            where T : class, IViewerPageDataProvider
        {
            _services.AddScoped<IViewerPageDataProvider, T>();
        }

        internal void Validate()
        {
            var serviceTypes = new[]
            {
                typeof(IHubEventHandler),
                typeof(IServiceHubSessionCache),
                typeof(IViewerAuthorizer),
                typeof(IViewerHubDataProvider),
                typeof(IViewerPageDataProvider)
            };

            foreach (var type in serviceTypes)
            {
                if (!_services.Any(x => x.ServiceType == type))
                {
                    throw new Exception($"Missing service registration for type {type.Name}.");
                }
            }
        }
    }
}
