using Infrastructure.Crosscutting.Framework.Logging;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Attributes
{
    public class CustomErrorHandlingAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext exceptionContext)
        {
            if (!exceptionContext.ExceptionHandled)
            {
                string controllerName = (string)exceptionContext.RouteData.Values["controller"];
                string actionName = (string)exceptionContext.RouteData.Values["action"];

                ILogger logger = new SerilogLogger();

                logger.LogError(string.Format("Fedhapluss.DashboardApplication...{0} in {1}", exceptionContext.Exception, controllerName));

                var model = new HandleErrorInfo(exceptionContext.Exception, controllerName, actionName);

                exceptionContext.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/Error.cshtml",
                    ViewData = new ViewDataDictionary<HandleErrorInfo>(model),
                    TempData = exceptionContext.Controller.TempData
                };

                exceptionContext.ExceptionHandled = true;
            }
        }
    }
}