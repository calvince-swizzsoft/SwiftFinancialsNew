using DistributedServices.MainBoundedContext;
using Infrastructure.Crosscutting.Framework.Logging;
using System.Configuration;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace SwiftFinancials.Apis
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            LoggerFactory.SetCurrent(new EntLibLogFactory());

            System.Net.ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) =>
            {
                return true;
            };

            var configCode = default(int);

            if (int.TryParse(ConfigurationManager.AppSettings["ConfigCode"], out configCode))
            {
                if (configCode == 3040)
                {
                    SecurityConfig.ProtectConfiguration();
                }
                else if (configCode == 4030)
                {
                    SecurityConfig.UnProtectConfiguration();
                }
            }
        }
    }
}