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
    public class AppraiseLoanController : MasterController
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

            var pageCollectionInfo = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync((int)LoanCaseStatus.Registered,jQueryDataTablesModel.sSearch,(int) LoanCaseFilter.CaseNumber, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader());

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


        public async Task<ActionResult> Appraise(Guid Id)

        {
            await ServeNavigationMenus();
            int caseNumber = 0;
            await _channelService.FindLoanCaseAsync(Id, GetServiceHeader());

            //ViewBag.LoanAppraisalOptionSelectList = GetLoanAppraisalOptionSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Appraise(LoanCaseDTO loanCaseDTO)
        {
            var loanAppraisalOption = loanCaseDTO.LoanAppraisalOption;

            loanCaseDTO.ValidateAll();
            if (!loanCaseDTO.HasErrors)
            {
                await _channelService.AppraiseLoanCaseAsync(loanCaseDTO, loanAppraisalOption, 1, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanCaseDTO.ErrorMessages;
                ViewBag.LoanAppraisalOptionSelectList = GetLoanAppraisalOptionSelectList(loanCaseDTO.LoanApprovalOption.ToString());
                return View(loanCaseDTO);
            }
        }
    }
}