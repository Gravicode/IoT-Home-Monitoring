using Microsoft.Owin;
using Owin;

namespace HomeMonitoring.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
