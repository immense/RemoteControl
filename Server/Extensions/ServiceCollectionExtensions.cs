using Immense.RemoteControl.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddRemoteControlServer(this IServiceCollection services)
        {
            var pub = new HubEventPublisher();
            pub.OnEvent<Models.RestartScreenCasterRequiredModel>(model =>
            {

                return Task.CompletedTask;
            });
            services.AddSingleton<IHubEventPublisher, HubEventPublisher>();
        }
    }
}
