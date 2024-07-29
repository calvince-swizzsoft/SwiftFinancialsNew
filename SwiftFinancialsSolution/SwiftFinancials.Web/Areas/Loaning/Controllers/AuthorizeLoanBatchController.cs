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
    public class AuthorizeLoanBatchController : MasterController
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

            var pageCollectionInfo = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync((int)LoanCaseStatus.Audited, jQueryDataTablesModel.sSearch, (int)LoanCaseFilter.CaseNumber, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader());

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


        public async Task<ActionResult> Authorize(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchStatusOptionSelectList = GetBatchStatusTypeSelectList(string.Empty);
            ViewBag.LoanProductCategory = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);

            return View();
        }


        public async Task<ActionResult> BranchesLookup(Guid? id, LoanDisbursementBatchDTO loanDisbursementBatchDTO)
        {
            await ServeNavigationMenus();

            ViewBag.BatchStatusOptionSelectList = GetBatchStatusTypeSelectList(string.Empty);
            ViewBag.LoanProductCategory = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Authorize");
            }

            if (Session["DataAttachmentPeriodId"]!=null)
            {
                loanDisbursementBatchDTO.DataAttachmentPeriodId = (Guid)Session["DataAttachmentPeriodId"];
                loanDisbursementBatchDTO.DataAttachmentPeriodPostingPeriodDescription = Session["DataAttachmentPeriodPostingPeriodDescription"].ToString();
            }


            var branch = await _channelService.FindBranchAsync(parseId, GetServiceHeader());
            if (branch != null)
            {
                loanDisbursementBatchDTO.BranchId = branch.Id;
                loanDisbursementBatchDTO.BranchDescription = branch.Description;

                Session["BranchId"] = loanDisbursementBatchDTO.BranchId;
                Session["BranchDescription"] = loanDisbursementBatchDTO.BranchDescription;
            }

            return View("Authorize", loanDisbursementBatchDTO);
        } 
        
        
        public async Task<ActionResult> PostingPeriodLookUp(Guid? id, LoanDisbursementBatchDTO loanDisbursementBatchDTO)
        {
            await ServeNavigationMenus();

            ViewBag.BatchStatusOptionSelectList = GetBatchStatusTypeSelectList(string.Empty);
            ViewBag.LoanProductCategory = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("Authorize");
            }


            if (Session["BranchId"]!=null)
            {
                loanDisbursementBatchDTO.BranchId = (Guid)Session["BranchId"];
                loanDisbursementBatchDTO.BranchDescription = Session["BranchDescription"].ToString();
            }


            var postingPeriod = await _channelService.FindPostingPeriodAsync(parseId, GetServiceHeader());
            if (postingPeriod != null)
            {
                loanDisbursementBatchDTO.DataAttachmentPeriodId = postingPeriod.Id;
                loanDisbursementBatchDTO.DataAttachmentPeriodPostingPeriodDescription = postingPeriod.Description;

                Session["DataAttachmentPeriodId"] = loanDisbursementBatchDTO.DataAttachmentPeriodId;
                Session["DataAttachmentPeriodPostingPeriodDescription"] = loanDisbursementBatchDTO.DataAttachmentPeriodPostingPeriodDescription;
            }

            return View("Authorize", loanDisbursementBatchDTO);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(LoanDisbursementBatchDTO loanDisbursementBatchDTO)
        {
            loanDisbursementBatchDTO.ValidateAll();

            if (!loanDisbursementBatchDTO.HasErrors)
            {
                await _channelService.AuthorizeLoanDisbursementBatchAsync(loanDisbursementBatchDTO, 1, 1, GetServiceHeader());

                TempData["approve"] = "Loan Authorization Successful";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanDisbursementBatchDTO.ErrorMessages;
                ViewBag.BatchStatusOptionSelectList = GetBatchStatusTypeSelectList(loanDisbursementBatchDTO.Type.ToString());
                ViewBag.LoanProductCategory = GetLoanRegistrationLoanProductCategorySelectList(loanDisbursementBatchDTO.LoanProductCategory.ToString());

                TempData["approveError"] = "Loan Authorization Unsuccessful";

                return View(loanDisbursementBatchDTO);
            }
        }
    }
}