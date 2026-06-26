using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.MicroCredit
{
    public class MicroCreditAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "MicroCredit";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
    "MicroCredit_default",
    "MicroCredit/{controller}/{action}/{id}",
    new { action = "Index", id = UrlParameter.Optional }
);

        }
    }
}