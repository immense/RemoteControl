using Immense.RemoteControl.Desktop.Shared.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Desktop.Shared.Extensions
{
    public interface IRemoteControlClientBuilder
    {
        void AddBrandingProvider<T>()
            where T : class, IBrandingProvider;
    }

    internal class RemoteControlClientBuilder : IRemoteControlClientBuilder
    {
        private readonly IServiceCollection _services;

        public RemoteControlClientBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public void AddBrandingProvider<T>()
            where T : class, IBrandingProvider
        {
            _services.AddScoped<IBrandingProvider, T>();
        }

        internal void Validate()
        {
            var serviceTypes = new[]
            {
                typeof(IBrandingProvider)
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
