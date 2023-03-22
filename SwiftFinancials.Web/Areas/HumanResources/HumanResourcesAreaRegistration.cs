using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.HumanResources
{
    public class HumanResourcesAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "HumanResources";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "HumanResources_default",
                "HumanResources/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}