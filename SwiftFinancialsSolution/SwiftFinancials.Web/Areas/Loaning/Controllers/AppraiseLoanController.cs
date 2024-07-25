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

            var pageCollectionInfo = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync((int)LoanCaseStatus.Registered, jQueryDataTablesModel.sSearch, (int)LoanCaseFilter.CaseNumber, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Appraise(Guid Id, Guid? id)

        {
            await ServeNavigationMenus();
            int caseNumber = 0;

            ViewBag.LoanAppraisalOptionSelectList = GetLoanAppraisalOptionSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            var loanBalance = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());

            var loaneeCustomer = await _channelService.FindLoanCaseAsync(Id, GetServiceHeader());


            LoanCaseDTO loanCaseDTO = new LoanCaseDTO();


            if (loaneeCustomer != null)
            {
                loanCaseDTO.CaseNumber = loaneeCustomer.CaseNumber;
                loanCaseDTO.CustomerId = loaneeCustomer.CustomerId;
                loanCaseDTO.CustomerIndividualFirstName = loaneeCustomer.CustomerIndividualFirstName;
                loanCaseDTO.CustomerReference2 = loaneeCustomer.CustomerReference2;
                loanCaseDTO.CustomerReference1 = loaneeCustomer.CustomerReference1;
                loanCaseDTO.LoanProductDescription = loaneeCustomer.LoanProductDescription;
                loanCaseDTO.CustomerReference3 = loaneeCustomer.CustomerReference3;
                loanCaseDTO.LoanPurposeDescription = loaneeCustomer.LoanPurposeDescription;
                loanCaseDTO.LoanRegistrationMaximumAmount = loaneeCustomer.LoanRegistrationMaximumAmount;
                loanCaseDTO.MaximumAmountPercentage = loaneeCustomer.MaximumAmountPercentage;
                loanCaseDTO.AmountApplied = loaneeCustomer.AmountApplied;
                loanCaseDTO.LoanRegistrationTermInMonths = loaneeCustomer.LoanRegistrationTermInMonths;
                loanCaseDTO.BranchDescription = loaneeCustomer.BranchDescription;

            }

            return View(loanCaseDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Appraise(LoanCaseDTO loanCaseDTO)
        {
            var loanDTO = await _channelService.FindLoanCaseAsync(loanCaseDTO.Id, GetServiceHeader());

            loanDTO.AppraisedAmount = loanCaseDTO.AppraisedAmount;
            loanDTO.SystemAppraisalRemarks = loanCaseDTO.SystemAppraisalRemarks;
            loanDTO.AppraisalRemarks = loanCaseDTO.AppraisalRemarks;

            loanDTO.ValidateAll();

            if (loanDTO.AppraisedAmount == 0 || loanDTO.SystemAppraisalRemarks == string.Empty || loanDTO.AppraisalRemarks == string.Empty)
            {
                TempData["AppraisedAmount"] = "Appraised amount, System Appraisal Remarks and Appraisal Remarks required to appraise loan.";

                return View();
            }

            if (!loanDTO.HasErrors)
            {
                await _channelService.AppraiseLoanCaseAsync(loanDTO, loanCaseDTO.LoanAppraisalOption, 1, GetServiceHeader());

                TempData["approve"] = "Loan Appraisal Successful";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanDTO.ErrorMessages.ToString();

                TempData["BugdetBalance"] = errorMessages;

                ViewBag.LoanAppraisalOptionSelectList = GetLoanAppraisalOptionSelectList(loanCaseDTO.LoanAppraisalOption.ToString());

                TempData["approveError"] = "Loan Appraisal Unsuccessful";

                return View(loanCaseDTO);
            }
        }
    }
}