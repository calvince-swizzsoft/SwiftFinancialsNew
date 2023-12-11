using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SwiftFinancials.Web.Startup))]
namespace SwiftFinancials.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
