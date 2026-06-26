using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.DataImport
{
    public class DataImportAreaRegistration : AreaRegistration
    {

        public override string AreaName
        {
            get
            {
                return "DataImport";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
               "DataImport_default",
               "DataImport/{controller}/{action}/{id}",
               new { action = "Index", id = UrlParameter.Optional }
           );
        }
    }
}