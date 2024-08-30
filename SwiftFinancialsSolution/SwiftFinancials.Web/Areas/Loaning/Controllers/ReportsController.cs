using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using SwiftFinancials.Web.PDF;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class ReportsController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }


        [HttpPost]
        public async Task<JsonResult> GetRegisteredLoans()
        {
            DateTime startDate = new DateTime(2024, 01, 01);
            DateTime endDate = new DateTime(2024, 12, 31);
            int statusCode = 48829; // Example status code

            // Call the stored procedure and get the list of LoanCaseDTO
            var loanCases = await GetLoansByStatusAsync(startDate, endDate, statusCode);

            // Debugging: Check if loanCases is populated
            if (loanCases == null || loanCases.Count == 0)
            {
                // Log or breakpoint here
                Console.WriteLine("No loan cases retrieved.");
                return Json(new { success = false, message = "No data to generate PDF." });
            }

            // Call createpdf class
            var createpdf = new RegisteredLoans();
            string fpath = Request.ServerVariables["REMOTE_ADDR"];
            var address = Path.Combine(Server.MapPath("~/Files/"));

            // Debugging: Check if address is correct
            Console.WriteLine($"File path: {address}");

            bool pdfCreated = createpdf.WritePdf(fpath, address, loanCases);

            // Debugging: Check if WritePdf was successful
            if (!pdfCreated)
            {
                // Log or breakpoint here
                Console.WriteLine("Failed to create PDF.");
                return Json(new { success = false, message = "Failed to create PDF." });
            }

            return Json(loanCases);
        }

        private async Task<List<LoanCaseDTO>> GetLoansByStatusAsync(DateTime startDate, DateTime endDate, int statusCode)
        {
            var loanCases = new List<LoanCaseDTO>();

            string connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"]?.ConnectionString;

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'SwiftFin_Dev' is not configured properly.");
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_LoansByStatus", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Set the parameters
                        command.Parameters.AddWithValue("@StartDate", startDate);
                        command.Parameters.AddWithValue("@EndDate", endDate);
                        command.Parameters.AddWithValue("@Status", statusCode);

                        await connection.OpenAsync();

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var loanCase = new LoanCaseDTO
                                {
                                    CaseNumber = reader.IsDBNull(reader.GetOrdinal("CaseNumber")) ? 0 : reader.GetInt32(reader.GetOrdinal("CaseNumber")),
                                    CustomerIndividualFirstName = reader.IsDBNull(reader.GetOrdinal("FullName")) ? string.Empty : reader.GetString(reader.GetOrdinal("FullName")),
                                    AmountApplied = reader.IsDBNull(reader.GetOrdinal("AmountApplied")) ? 0 : reader.GetDecimal(reader.GetOrdinal("AmountApplied")),
                                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? string.Empty : reader.GetString(reader.GetOrdinal("CreatedBy")),
                                    CreatedDate = reader.IsDBNull(reader.GetOrdinal("CreatedDate")) ? (DateTime)(DateTime?)null : reader.GetDateTime(reader.GetOrdinal("CreatedDate"))

                                    // Map other properties as necessary
                                };

                                loanCases.Add(loanCase);
                            }
                        }

                        return loanCases;
                    }
                }
            }
            catch (SqlException ex)
            {
                // Log the exception or handle it as needed
                // Example: log error (assuming you have a logging framework)
                // _logger.LogError(ex, "An error occurred while retrieving loan cases.");
                throw; // Rethrow the exception if you want it to propagate
            }
            catch (Exception ex)
            {
                // Handle other potential exceptions
                // Example: log error
                // _logger.LogError(ex, "An unexpected error occurred.");
                throw; // Rethrow the exception if you want it to propagate
            }

            return loanCases;
        }



        [HttpPost]
        public async Task<JsonResult> GetAppraisedLoans(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            double positiveInfinity = double.PositiveInfinity;
            int positiveInfinityAsInt = positiveInfinity > int.MaxValue ? int.MaxValue : (int)positiveInfinity;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanCasesByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)LoanCaseStatus.Appraised, 0, positiveInfinityAsInt, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        [HttpPost]
        public async Task<JsonResult> GetApprovedLoans(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            double positiveInfinity = double.PositiveInfinity;
            int positiveInfinityAsInt = positiveInfinity > int.MaxValue ? int.MaxValue : (int)positiveInfinity;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanCasesByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)LoanCaseStatus.Approved, 0, positiveInfinityAsInt, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        [HttpPost]
        public async Task<JsonResult> GetRejectedLoans(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            double positiveInfinity = double.PositiveInfinity;
            int positiveInfinityAsInt = positiveInfinity > int.MaxValue ? int.MaxValue : (int)positiveInfinity;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanCasesByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)LoanCaseStatus.Rejected, 0, positiveInfinityAsInt, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        [HttpPost]
        public async Task<JsonResult> GetVerifiedLoans(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            double positiveInfinity = double.PositiveInfinity;
            int positiveInfinityAsInt = positiveInfinity > int.MaxValue ? int.MaxValue : (int)positiveInfinity;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanCasesByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)LoanCaseStatus.Audited, 0, positiveInfinityAsInt, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        [HttpPost]
        public async Task<JsonResult> GetDisbursedLoans(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            double positiveInfinity = double.PositiveInfinity;
            int positiveInfinityAsInt = positiveInfinity > int.MaxValue ? int.MaxValue : (int)positiveInfinity;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanCasesByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)LoanCaseStatus.Disbursed, 0, positiveInfinityAsInt, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        [HttpPost]
        public async Task<JsonResult> GetDeferredLoans(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            double positiveInfinity = double.PositiveInfinity;
            int positiveInfinityAsInt = positiveInfinity > int.MaxValue ? int.MaxValue : (int)positiveInfinity;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanCasesByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)LoanCaseStatus.Deferred, 0, positiveInfinityAsInt, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        [HttpPost]
        public async Task<JsonResult> GetRestructuredLoans(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            double positiveInfinity = double.PositiveInfinity;
            int positiveInfinityAsInt = positiveInfinity > int.MaxValue ? int.MaxValue : (int)positiveInfinity;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanCasesByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)LoanCaseStatus.Restructured, 0, positiveInfinityAsInt, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }
    }
}