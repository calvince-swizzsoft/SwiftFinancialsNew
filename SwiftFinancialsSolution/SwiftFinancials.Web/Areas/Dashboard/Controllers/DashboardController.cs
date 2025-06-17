using Infrastructure.Crosscutting.Framework.Logging;
using LazyCache;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Mvc;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Identity;

namespace SwiftFinancials.Web.Areas.Dashboard.Controllers
{
    [Authorize]
    public class DashboardController : MasterController
    {
        private readonly IAppCache _appCache;

        public DashboardController(ILogger logger, IAppCache appCache)
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

        public async Task<JsonResult> Branches()
        {
            return Json(new { success = true, Branches = await _channelService.FindBranchesAsync(GetServiceHeader()) });
        }

        public async Task<JsonResult> TotalSavings()
        {
            return Json(new { success = true, Savings = await _channelService.FindSavingsProductsAsync(GetServiceHeader()) });
        }

        public async Task<JsonResult> LoanPortfolio()
        {
            return Json(new { success = true, Loans = await _channelService.FindLoanProductsAsync(GetServiceHeader()) });
        }


        public async Task<JsonResult> ShareCapital()
        {
            return Json(new { success = true, Shares = await _channelService.FindInvestmentProductsAsync(GetServiceHeader()) });
        }

        public async Task<JsonResult> LoanCases()
        {
            var loanCases = await _channelService.FindLoanCasesAsync(GetServiceHeader());
            return Json(new { success = true, LoanCases = loanCases }, JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> FindMonthlyLoanDisbursement()
        {
            var monthsCap = Convert.ToInt32(ConfigurationManager.AppSettings["MessagingSummaryMonthCap"]) * -1;
            var startDate = DateTime.Today.AddMonths(monthsCap);
            var endDate = DateTime.Now;

            var result = await _channelService.FindLoanDisbursementBatchesByStatusAndFilterInPageAsync(2, startDate, endDate,null,int.MaxValue,int.MaxValue, GetServiceHeader());
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion

        public async Task<ActionResult> CustomerSummary(Guid id)
        {
            await ServeNavigationMenus();

            var companyDTO = await _channelService.FindCompanyAsync(id, GetServiceHeader());
            var customerId = User.Identity.GetCustomerId();

            ViewBag.SaccoName = companyDTO?.Description;
            TempData["CompanyDTO"] = companyDTO;

            return View();
        }
    }
}
