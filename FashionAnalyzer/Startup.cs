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
            app.MapSignalR();

            ConfigureAuth(app);
        }
    }
}
