using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Procurement
{
    public class ProcurementAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Control";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Control_default",
                "Control/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}