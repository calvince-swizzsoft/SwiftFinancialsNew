using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    // Next is Loan Authorization

    public class LoanCancellationController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanCasesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iColumns, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());

            return View(loanCaseDTO);
        }

        public async Task<ActionResult> Cancel(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());
            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
            ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(string.Empty);
            ViewBag.LoanCancellationOptionSelectList = GetLoanCancellationOptionSelectList(string.Empty);

            return View(loanCaseDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Cancel(LoanCaseDTO loanCaseDTO)
        {
            var loanCancellationOption = loanCaseDTO.LoanCancellationOption;
            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {
                await _channelService.AuditLoanCaseAsync(loanCaseDTO, loanCancellationOption, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanCaseDTO.ErrorMessages;
                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());
                ViewBag.LoanCancellationOptionSelectList = GetLoanCancellationOptionSelectList(loanCaseDTO.LoanCancellationOption.ToString());
                return View(loanCaseDTO);
            }
        }
    }
}