using Immense.RemoteControl.Server.Filters;
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
        public static IServiceCollection AddRemoteControlServer(
            this IServiceCollection services, 
            Action<IRemoteControlServerBuilder> configure)
        {
            var builder = new RemoteControlServerBuilder(services);
            configure(builder);
            builder.Validate();

            services.AddRazorPages();
            services.AddSingleton<IDesktopHubSessionCache, DesktopHubSessionCache>();
            services.AddScoped<ViewerAuthorizationFilter>();

            return services;
        }
    }
}
