using Application.MainBoundedContext.DTO;
using EasyBim.Presentation.Infrastructure.DataTables;
using Infrastructure.Crosscutting.Framework.Adapter;
using Infrastructure.Crosscutting.Framework.Logging;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace SwiftFinancials.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            BundleTable.EnableOptimizations = true;

            ConfigureFactories();

            //Whenever JQueryDataTablesModel is a parameter in an action, the JQueryDataTablesModelBinder should be used when binding the model.
            ModelBinders.Binders.Add(typeof(JQueryDataTablesModel), new JQueryDataTablesModelBinder());
        }

        protected void Application_Error()
        {
            ILogger logger = new SerilogLogger();

            var ex = Server.GetLastError();
            //log the error!
            logger.LogError("SwiftFinancials.Web...", ex);
        }

        public static void ConfigureFactories()
        {
            var typeAdapterFactory = DependencyResolver.Current.GetService<ITypeAdapterFactory>();

            Infrastructure.Crosscutting.Framework.Adapter.TypeAdapterFactory.SetCurrent(typeAdapterFactory);
        }
    }
}