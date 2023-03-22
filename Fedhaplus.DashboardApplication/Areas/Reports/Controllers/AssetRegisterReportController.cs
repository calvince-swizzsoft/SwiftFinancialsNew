using Fedhaplus.DashboardApplication.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Areas.Reports.Controllers
{
    public class AssetRegisterReportController : MasterController
    {
        // GET: Reports/AssetRegisterReport
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> AssetRegisters()
        {
            await ServeNavigationMenus();

            return View();
        }
    }
}