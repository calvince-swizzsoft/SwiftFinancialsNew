using Infrastructure.Crosscutting.Framework.Logging;
using LazyCache;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Mvc;
using Fedhaplus.DashboardApplication.Controllers;
using Fedhaplus.DashboardApplication.Identity;

namespace Fedhaplus.DashboardApplication.Areas.Dashboard.Controllers
{
    [Authorize]
    public class DashboardController : MasterController
    {
        // private readonly ICustomerInterfaceService _customerInterfaceService;
        private readonly IAppCache _appCache;

        public DashboardController(ILogger logger,
            IAppCache appCache)
        {
            _appCache = appCache;
        }

        #region Metrics

        public async Task<JsonResult> Companies()
        {
            return Json(new { success = true, Companies = await _channelService.GetCompaniesCountAsync(GetServiceHeader()) });
        }

        public async Task<JsonResult> Users()
        {
            return Json(new { success = true, Users = await _channelService.GetApplicationUsersCountAsync(GetServiceHeader()) });
        }

        public async Task<JsonResult> Customers()
        {
            return Json(new { success = true, Customers = await _channelService.GetCustomersCountAsync(GetServiceHeader()) });
        }

        public JsonResult AssetRegisters()
        {
            return Json(new { success = true, AssetRegisters = _channelService.FindAssetRegistersAsync(GetServiceHeader()).Result.Count });
        }

        public async Task<JsonResult> FindMonthlyEmailAlertsStatistics()
        {
            var monthsCap = Convert.ToInt32(ConfigurationManager.AppSettings["MessagingSummaryMonthCap"]) * -1;

            var results = Json(await _channelService.FindEmailAlertsMonthlyStatisticsAsync(User.Identity.GetCompanyId(), DateTime.Today.AddMonths(monthsCap), DateTime.Now, GetServiceHeader()), JsonRequestBehavior.AllowGet);

            return results;
        }

        public async Task<JsonResult> FindMonthlyTextAlertsStatistics()
        {
            var monthsCap = Convert.ToInt32(ConfigurationManager.AppSettings["MessagingSummaryMonthCap"]) * -1;

            var results = Json(await _channelService.FindTextAlertsMonthlyStatisticsAsync(User.Identity.GetCompanyId(), DateTime.Today.AddMonths(monthsCap), DateTime.Now, GetServiceHeader()), JsonRequestBehavior.AllowGet);

            return results;
        }

        #endregion

        public async Task<ActionResult> CustomerSummary(Guid id)
        {
            await ServeNavigationMenus();

            var companyDTO = await _channelService.FindCompanyAsync(id, GetServiceHeader());

            var customerId = User.Identity.GetCustomerId();

            ViewBag.SaccoName = companyDTO?.Description;

            //Items are only removed from TempData at the end of a request if they have been tagged for removal.
            //Items are only tagged for removal when they are read.
            //Items may be untagged(for deletion) by calling TempData.Keep(key).
            //After reading a value call keep:
            TempData["CompanyDTO"] = companyDTO;

            return View();
        }
    }
}