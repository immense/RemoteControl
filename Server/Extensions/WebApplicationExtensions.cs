using Immense.RemoteControl.Server.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Extensions
{
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// <para>
        ///     Maps Razor pages and SignalR hubs.  The remote control viewer page will be mapped
        ///     to path "/RemoteControl/Viewer", the desktop hub to "/hubs/desktop", and viewer hub
        ///     to "/hubs/viewer".
        /// </para>
        /// <para>
        ///     Important: This must be called after "app.UseRouting()".
        /// </para>
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static WebApplication UseRemoteControlServer(this WebApplication app)
        {
            app.MapRazorPages();

            app.UseEndpoints(config =>
            {
                config.MapHub<DesktopHub>("/hubs/desktop");
                config.MapHub<ViewerHub>("/hubs/viewer");
            });

            return app;
        }
    }
}
