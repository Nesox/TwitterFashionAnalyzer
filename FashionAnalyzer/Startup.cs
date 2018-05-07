using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(FashionAnalyzer.Startup))]
namespace FashionAnalyzer
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Enables SignalR
            app.MapSignalR(
#if DEBUG                
                new HubConfiguration{
                    EnableDetailedErrors = true,

                }
#endif
        );

        ConfigureAuth(app);
        }
    }
}
