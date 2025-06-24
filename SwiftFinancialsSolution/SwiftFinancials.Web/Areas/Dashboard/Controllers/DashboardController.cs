using Infrastructure.Crosscutting.Framework.Logging;
using LazyCache;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Mvc;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Identity;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;

namespace SwiftFinancials.Web.Areas.Dashboard.Controllers
{
    [Authorize]
    public class DashboardController : MasterController
    {
        private readonly string _connectionString;

        public DashboardController()
        {
            // Get connection string from Web.config
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        }
        private readonly IAppCache _appCache;

        public DashboardController(ILogger logger, IAppCache appCache)
        {
            _appCache = appCache;
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        }

        #region Metrics

        public async Task<JsonResult> Companies()
        {
            return Json(new { success = true, Companies = await _channelService.GetCompaniesCountAsync(GetServiceHeader()) });
        }
        [HttpGet]
        public async Task<JsonResult> GetSavingsProductBalances(string id, DateTime endDate, string op, float opVal)
        {
            var result = new List<object>();
            var connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("sp_SavingsProductsBalances", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Ensure parameters are correctly passed
                    cmd.Parameters.AddWithValue("@ID", id); // Comma-separated GUIDs
                    cmd.Parameters.AddWithValue("@Enddate", endDate);
                    cmd.Parameters.AddWithValue("@Operator", op);
                    cmd.Parameters.AddWithValue("@OperatorValue", opVal);

                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new
                            {
                                FullAccount = reader["FullAccount"]?.ToString(),
                                FullName = reader["FullName"]?.ToString(),
                                Description = reader["Description"]?.ToString(),
                                Balance = reader["Balance"] != DBNull.Value ? Convert.ToDecimal(reader["Balance"]) : 0
                            });
                        }
                    }
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log the exception (you can replace this with your logging framework)
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public async Task<JsonResult> GetSavingsProducts()
        {
            var list = new List<object>();
            var connStr = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

            try
            {
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand("SELECT ID, Description FROM Swiftfin_SavingsProducts", conn))
                {
                    await conn.OpenAsync();
                    var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            ID = reader["ID"].ToString(),
                            Name = reader["Description"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }

            return Json(list, JsonRequestBehavior.AllowGet);
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

            var result = await _channelService.FindLoanDisbursementBatchesByStatusAndFilterInPageAsync(2, startDate, endDate, null, int.MaxValue, int.MaxValue, GetServiceHeader());
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
