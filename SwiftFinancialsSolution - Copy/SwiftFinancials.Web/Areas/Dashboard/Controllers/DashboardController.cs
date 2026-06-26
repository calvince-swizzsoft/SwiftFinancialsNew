using Infrastructure.Crosscutting.Framework.Logging;
using LazyCache;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Mvc;
using SwiftFinancials.Web.Identity;
using SwiftFinancials.Web.Controllers;

namespace SwiftFinancials.Web.Areas.Dashboard.Controllers
{
    [Authorize]
    public class DashboardController : MasterController
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;
        private readonly IAppCache _appCache;

        public DashboardController(ILogger logger, IAppCache appCache)
        {
            _logger = logger;
            _appCache = appCache;
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        #region KPI Actions

        public async Task<JsonResult> Customers()
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM swiftFin_Customers;", conn))
            {
                await conn.OpenAsync();
                int count = (int)await cmd.ExecuteScalarAsync();
                return Json(new { success = true, Customers = count }, JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<JsonResult> TotalsavingsAccounts()
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
        SELECT SUM(j.Amount) AS TotalSavingsAmount
        FROM swiftFin_CustomerAccounts ca
        INNER JOIN swiftfin_journalentries j ON j.CustomerAccountId = ca.Id
        WHERE ca.CustomerAccountType_TargetProductCode = 1
          AND j.Amount > 0", conn))
            {
                await conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();

                // Handle null result by defaulting to 0m
                decimal totalSavingsAmount = 0m;
                if (result != DBNull.Value && result != null)
                {
                    totalSavingsAmount = Convert.ToDecimal(result);
                }

                return Json(new { success = true, TotalSavingsAmount = totalSavingsAmount }, JsonRequestBehavior.AllowGet);
            }
        }


        public async Task<JsonResult> Users()
        {
            var users = new List<object>();
            const string sql = "SELECT * FROM swiftFin_AspNetUsers";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(new
                        {
                            Id = reader["Id"],
                            UserName = reader["UserName"],
                            Email = reader["Email"],
                            PhoneNumber = reader["PhoneNumber"],
                        });
                    }
                }
            }

            return Json(new { success = true, Users = users }, JsonRequestBehavior.AllowGet);
        }


        public async Task<JsonResult> Companies()
        {
            var result = new List<object>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("SELECT * FROM swiftFin_Companies", conn))
                {
                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new
                            {
                                Id = reader["Id"]?.ToString(),
                                CompanyCode = reader["CompanyCode"]?.ToString(),
                                CompanyName = reader["CompanyName"]?.ToString(),
                                Status = reader["Status"]?.ToString(),
                                CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : (DateTime?)null
                                // Add more fields based on your actual table columns
                            });
                        }
                    }
                }

                return Json(new { success = true, Companies = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching companies: " + ex.Message);
                return Json(new { success = false, message = "Failed to retrieve companies." }, JsonRequestBehavior.AllowGet);
            }
        }


        public async Task<JsonResult> TotalSavings()
        {
            const string sql = @"
        SELECT 
            SUM(j.Amount) AS TotalJournalAmount
        FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_CustomerAccounts] ca
        INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftfin_journalentries] j
            ON j.CustomerAccountId = ca.Id
        WHERE ca.CustomerAccountType_TargetProductCode = 1
          AND j.Amount > 0;
    ";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    await conn.OpenAsync();
                    var result = await cmd.ExecuteScalarAsync();

                    decimal totalJournalAmount = 0m;
                    if (result != DBNull.Value && result != null)
                    {
                        totalJournalAmount = Convert.ToDecimal(result);
                    }

                    return Json(new { success = true, TotalJournalAmount = totalJournalAmount });
                }
            }
            catch (Exception ex)
            {
                // Log error...
                return Json(new { success = false, message = ex.Message });
            }
        }





        public async Task<JsonResult> LoanPortfolio()
        {
            try
            {
                decimal totalLoans = 0;

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("select count (*)from SwiftFinancialsDB_Live.dbo.Swiftfin_loancases where status=48829", conn))
                {
                    await conn.OpenAsync();
                    var result = await cmd.ExecuteScalarAsync();
                    totalLoans = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                }

                return Json(new
                {
                    success = true,
                    Loans = new
                    {
                        TotalAmount = totalLoans
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching loan portfolio: " + ex.Message);
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public async Task<JsonResult> ShareCapital()
        {
            try
            {
                // Call the existing service
                var result = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());

                decimal? maximumBalance = null;

                // SQL query to get MaximumBalance
                const string sql = @"SELECT TOP 1000 MaximumBalance FROM swiftFin_InvestmentProducts";

                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(sql, connection))
                {
                    await connection.OpenAsync();

                    var value = await command.ExecuteScalarAsync();
                    if (value != DBNull.Value && value != null)
                    {
                        maximumBalance = Convert.ToDecimal(value);
                    }
                }

                // Return combined data
                return Json(new
                {
                    success = true,
                    Shares = result,
                    MaximumBalance = maximumBalance
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public async Task<JsonResult> LoanCases()
        {
            var result = new List<object>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("SELECT * FROM swiftFin_LoanCases", conn))
                {
                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new
                            {
                                Id = reader["Id"]?.ToString(),
                                MemberId = reader["MemberId"]?.ToString(),
                                ProductId = reader["ProductId"]?.ToString(),
                                AmountRequested = reader["AmountRequested"] != DBNull.Value ? Convert.ToDecimal(reader["AmountRequested"]) : 0,
                                Status = reader["Status"]?.ToString(),
                                CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : (DateTime?)null,
                                // Add more fields as needed based on your table structure
                            });
                        }
                    }
                }

                return Json(new { success = true, LoanCases = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching loan cases: " + ex.Message);
                return Json(new { success = false, message = "Failed to retrieve loan cases." }, JsonRequestBehavior.AllowGet);
            }
        }


        public async Task<JsonResult> FindMonthlyLoanDisbursement()
        {
            int monthsCap = Convert.ToInt32(ConfigurationManager.AppSettings["MessagingSummaryMonthCap"]) * -1;
            DateTime startDate = DateTime.Today.AddMonths(monthsCap);
            DateTime endDate = DateTime.Now;

            var result = await _channelService.FindLoanDisbursementBatchesByStatusAndFilterInPageAsync(
                2, startDate, endDate, null, int.MaxValue, int.MaxValue, GetServiceHeader());

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Custom SQL Methods

        public async Task<JsonResult> GetSavingsProducts()
        {
            var result = new List<object>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("SELECT * FROM swiftFin_SavingsProducts", conn))
                {
                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new
                            {
                                ID = reader["ID"].ToString(),
                                Name = reader["Description"].ToString()
                            });
                        }
                    }
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Optional: log error here
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        

        public async Task<JsonResult> GetSavingsProductBalances(string id, DateTime endDate, string op, float opVal)
        {
            var result = new List<object>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("sp_SavingsProductsBalances", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", id);
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
                _logger.LogError("Error fetching savings balances: " + ex.Message);
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<JsonResult> BranchPerformance()
        {
            var result = new List<object>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    string query = @"
                SELECT 
                    b.Id AS BranchId,
                    b.Description AS Branch,
                    COUNT(j.Id) AS Count,
                    ISNULL(SUM(j.TotalValue), 0) AS TotalValue
                FROM swiftFin_Journals j
                INNER JOIN swiftFin_Branches b ON j.BranchId = b.Id
                GROUP BY b.Id, b.Description";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        await conn.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new
                                {
                                    BranchId = reader["BranchId"]?.ToString(),
                                    Branch = reader["Branch"]?.ToString(),
                                    Count = reader["Count"] != DBNull.Value ? Convert.ToInt32(reader["Count"]) : 0,
                                    TotalValue = reader["TotalValue"] != DBNull.Value ? Convert.ToDecimal(reader["TotalValue"]) : 0
                                });
                            }
                        }
                    }
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching branch performance: " + ex.Message);
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        #endregion

        public async Task<ActionResult> CustomerSummary(Guid id)
        {
            await ServeNavigationMenus();
            var companyDTO = await _channelService.FindCompanyAsync(id, GetServiceHeader());
            ViewBag.SaccoName = companyDTO?.Description;
            TempData["CompanyDTO"] = companyDTO;
            return View();
        }
    }
}
