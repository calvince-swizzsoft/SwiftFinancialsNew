using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class LoanInterestCapitalizationController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();


            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            RecurringBatchDTO recurringBatchDTO = new RecurringBatchDTO();
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(recurringBatchDTO.Month.ToString());
            ViewBag.MonthsSelectList = GetMonthsAsync(recurringBatchDTO.Type.ToString());
            ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(recurringBatchDTO.Type.ToString());
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(recurringBatchDTO.Priority.ToString());
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(recurringBatchDTO.Type.ToString());
            int totalRecordCount = 0;
            int searchRecordCount = 0;
            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortColumnIndex = jQueryDataTablesModel.iSortCol_.FirstOrDefault();
            var sortColumnDirection = jQueryDataTablesModel.sSortDir_.FirstOrDefault();

            //var sortPropertyName = ""; // Define the property name for sorting

            //if (sortColumnIndex >= 0 && sortColumnIndex < jQueryDataTablesModel.iColumns.Count)
            //{
            //    sortPropertyName = jQueryDataTablesModel.GetSortedColumns()[sortColumnIndex].PropertyName;
            //}

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, jQueryDataTablesModel.iColumns, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<RecurringBatchDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loanProductDTO = await _channelService.FindLoanProductAsync(id, GetServiceHeader());

            return View(loanProductDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();


            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);


            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var loanProduct = await _channelService.FindChartOfAccountAsync(parseId, GetServiceHeader());

            LoanProductDTO loanProductDTO = new LoanProductDTO();

            if (loanProduct != null)
            {
                loanProductDTO.ChartOfAccountId = loanProduct.Id;
                loanProductDTO.InterestReceivedChartOfAccountId = loanProduct.Id;
                loanProductDTO.InterestReceivableChartOfAccountId = loanProduct.Id;
                loanProductDTO.InterestChargedChartOfAccountId = loanProduct.Id;
                loanProductDTO.ChartOfAccountName = loanProduct.AccountName;
                //loanProductDTO.InterestReceivedChartOfAccountAccountName = loanProduct.AccountName;
            }
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(RecurringBatchDTO recurringBatchDTO, List<LoanProductDTO> selectedRows)
        {
            recurringBatchDTO.ValidateAll();
            int Priority = recurringBatchDTO.Priority;
            if (!recurringBatchDTO.HasErrors)
            {
                foreach (var selectedRow in selectedRows)
                {
                    var savingsProductDTO = await _channelService.FindSavingsProductAsync(selectedRow.Id, GetServiceHeader());
                    
                    await _channelService.UpdateSavingsProductAsync(savingsProductDTO, GetServiceHeader());

                }

                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(recurringBatchDTO.Month.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(recurringBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(recurringBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(recurringBatchDTO.Priority.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(recurringBatchDTO.Type.ToString());
                return RedirectToAction("Create");
            }

            else
            {
                var errorMessages = recurringBatchDTO.ErrorMessages;
                return View(recurringBatchDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var loanProductDTO = await _channelService.FindLoanProductAsync(id, GetServiceHeader());

            return View(loanProductDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, LoanProductDTO loanProductBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateLoanProductAsync(loanProductBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(loanProductBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetLoanProductsAsync()
        {
            var loanProductsDTOs = await _channelService.FindLoanProductsAsync(GetServiceHeader());

            return Json(loanProductsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}