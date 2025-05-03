using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SwiftFinancials.Apis.Startup))]
namespace SwiftFinancials.Apis
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            app.MapSignalR();
        }
    }
}