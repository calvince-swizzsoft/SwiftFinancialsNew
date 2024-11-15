using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync(loanCaseStatus, filterValue, filterType, pageIndex, jQueryDataTablesModel.iDisplayLength, true, GetServiceHeader());

            var page = pageCollectionInfo.PageCollection;

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
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