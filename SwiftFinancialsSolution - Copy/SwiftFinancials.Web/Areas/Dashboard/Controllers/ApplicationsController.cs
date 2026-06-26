using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using OfficeOpenXml;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using SwiftFinancials.Web.PDF;

namespace SwiftFinancials.Web.Areas.Dashboard.Controllers
{
    public class ApplicationsController : MasterController
    {
        string connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public async Task<List<Guid>> GetLoans()
        {
            var loans = new List<Guid>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                var query = @"SELECT * FROM swiftFin_LoanDisbursementBatchEntries";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        loans.Add((Guid)reader["LoanCaseId"]);
                    }
                }
            }

            return loans;
        }


        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.LoanCaseFilterSelectList = GetLoanCaseFilterTypeSelectList(string.Empty);
            ViewBag.LoanCaseStatusSelectList = GetLoanCaseStatusSelectList(string.Empty);

            return View();
        }


        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int loanCaseStatus, string filterValue, int filterType)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = new PageCollectionInfo<LoanCaseDTO>();

            if (loanCaseStatus == 48829)
            {
                pageCollectionInfo = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync(
                    48829,
                    filterValue,
                    filterType,
                    0,
                    int.MaxValue,
                    includeBatchStatus: true,
                    GetServiceHeader()
                );
            }

            else
            {
                pageCollectionInfo = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync(
                    loanCaseStatus,
                    filterValue,
                    filterType,
                    0,
                    int.MaxValue,
                    includeBatchStatus: true,
                    GetServiceHeader()
                );
            }


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(loanCase => loanCase.CreatedDate)
                    .ToList();

                totalRecordCount = sortedData.Count;

                var paginatedData = sortedData
                    .Skip(jQueryDataTablesModel.iDisplayStart)
                    .Take(jQueryDataTablesModel.iDisplayLength)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? sortedData.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: paginatedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }

            return this.DataTablesJson(
                items: new List<LoanCaseDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }



        public async Task<ActionResult> ViewDetails(Guid id)
        {
            await ServeNavigationMenus();

            var LoanCase = new LoanCaseDTO();

            var loancaseDetails = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());

            if (loancaseDetails != null)
            {
                LoanCase = loancaseDetails as LoanCaseDTO;
                LoanCase.AccountStatus = loancaseDetails.CustomerTypeDescription;


                var loanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(id, GetServiceHeader());
                if (loanGuarantors != null)
                    ViewBag.LoanGuarantors = loanGuarantors;
                else
                    TempData["emptyLoanGuarantors"] = "No data available to be displayed in table";


                var collaterals = await _channelService.FindLoanCollateralsByLoanCaseIdAsync(id, GetServiceHeader());
                if (collaterals != null)
                    ViewBag.Collaterals = collaterals;
                else
                    TempData["emptyCollaterals"] = "No data available to be displayed in table";


                //// Standing Orders
                ObservableCollection<Guid> customerAccountId = new ObservableCollection<Guid>();
                var customerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(loancaseDetails.CustomerId, true, true, true, true, GetServiceHeader());

                foreach (var accounts in customerAccounts)
                {
                    customerAccountId.Add(accounts.Id);
                }

                List<StandingOrderDTO> allStandingOrders = new List<StandingOrderDTO>();

                foreach (var Ids in customerAccountId)
                {
                    var standingOrders = await _channelService.FindStandingOrdersByBeneficiaryCustomerAccountIdAsync(Ids, true, GetServiceHeader());
                    if (standingOrders != null && standingOrders.Any())
                    {
                        allStandingOrders.AddRange(standingOrders);
                    }
                }

            }

            return View(LoanCase);
        }
    }
}