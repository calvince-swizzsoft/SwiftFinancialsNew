using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Fedhaplus.DashboardApplication.Startup))]
namespace Fedhaplus.DashboardApplication
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
