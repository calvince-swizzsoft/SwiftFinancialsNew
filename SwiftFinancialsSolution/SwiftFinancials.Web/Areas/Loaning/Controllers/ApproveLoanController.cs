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
    public class ApproveLoanController : MasterController
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

            var pageCollectionInfo = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync((int)LoanCaseStatus.Appraised, jQueryDataTablesModel.sSearch, (int)LoanCaseFilter.CaseNumber, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id, JQueryDataTablesModel jQueryDataTablesModel)
        {
            await ServeNavigationMenus();

            int status = 1, loanCaseFilter = 0;

            string text = "";

            var loanCaseDTO = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync(status, text, loanCaseFilter, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader());

            return View(loanCaseDTO);
        }




        public async Task<ActionResult> Approve(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());

            ViewBag.LoanApprovalOptionSelectList = GetLoanApprovalOptionSelectList(string.Empty);

            return View(loanCaseDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approve(LoanCaseDTO loanCaseDTO)
        {
            var loanApprovalOption = loanCaseDTO.LoanApprovalOption;

            var loanDTO = await _channelService.FindLoanCaseAsync(loanCaseDTO.Id, GetServiceHeader());

            loanDTO.ValidateAll();

            if (!loanDTO.HasErrors)
            {
                await _channelService.ApproveLoanCaseAsync(loanDTO, loanApprovalOption, GetServiceHeader());

                TempData["approve"] = "Loan Approval Successful";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanDTO.ErrorMessages;
                ViewBag.LoanApprovalOptionSelectList = GetLoanApprovalOptionSelectList(loanCaseDTO.LoanApprovalOption.ToString());

                TempData["approveError"] = "Loan Approval Unsuccessful";

                return View(loanCaseDTO);
            }
        }
    }
}