using Immense.RemoteControl.Server.Hubs;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Extensions
{
    public static class WebApplicationExtensions
    {
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
