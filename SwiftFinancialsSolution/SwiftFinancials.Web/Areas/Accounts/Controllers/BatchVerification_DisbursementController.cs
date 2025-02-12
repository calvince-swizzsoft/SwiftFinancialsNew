using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.MessagingModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BatchVerification_DisbursementController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            ViewBag.BatchStatus = GetBatchStatusTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = await _channelService.FindLoanDisbursementBatchesByStatusAndFilterInPageAsync(
                    1,
                    DateTime.Now.AddDays(-1000),
                    DateTime.Now,
                    jQueryDataTablesModel.sSearch,
                    0,
                    int.MaxValue,
                    GetServiceHeader()
                    );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(k => k.CreatedDate)
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
                items: new List<LoanDisbursementBatchDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }


        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

            var loanDisbursementBatchDTO = await _channelService.FindLoanDisbursementBatchAsync(id, GetServiceHeader());
            if (loanDisbursementBatchDTO.Status == (int)BatchStatus.Audited)
            {
                TempData["Audited"] = "The selected Batch is already Verified";
                return RedirectToAction("Index");
            }

            var verifiedLoanCasesList = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync((int)LoanCaseStatus.Audited,
                string.Empty, (int)LoanCaseFilter.CaseNumber, 0, 200, false, GetServiceHeader());
            var verifiedLoanCases = verifiedLoanCasesList.PageCollection.Where(x => x.IsBatched == false);

            ViewBag.BatchEntries = verifiedLoanCases;
            return View(loanDisbursementBatchDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(string selectedIds, LoanDisbursementBatchDTO loanDisbursementBatchDTO, LoanDisbursementBatchEntryDTO loanDisbursementBatchEntryDTO)
        {
            if (string.IsNullOrWhiteSpace(selectedIds))
            {
                await ServeNavigationMenus();
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

                TempData["required"] = "Loan Disbursement Batch Entries are required!";
                return View(loanDisbursementBatchDTO);
            }

            int batchAuthOption = loanDisbursementBatchDTO.Auth;
            loanDisbursementBatchDTO.ValidateAll();

            if (!loanDisbursementBatchDTO.HasErrors)
            {
                List<Guid> selectedBatchIds = selectedIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => Guid.Parse(id.Trim())).ToList();

                List<LoanDisbursementBatchEntryDTO> batchEntryDTO = new List<LoanDisbursementBatchEntryDTO>();

                foreach (var loanCaseId in selectedBatchIds)
                {
                    loanDisbursementBatchEntryDTO.LoanCaseId = loanCaseId;
                    loanDisbursementBatchEntryDTO.LoanDisbursementBatchId = loanDisbursementBatchDTO.Id;

                    await _channelService.AddLoanDisbursementBatchEntryAsync(loanDisbursementBatchEntryDTO, GetServiceHeader());

                    // Calculate Batch Total
                    List<LoanCaseDTO> LCDTO = new List<LoanCaseDTO>();
                    decimal batchTotal = 0;
                    var loanCase = await _channelService.FindLoanCaseAsync(loanCaseId, GetServiceHeader());
                    if (loanCase != null)
                    {
                        LCDTO.Add(loanCase);
                        batchTotal += loanCase.ApprovedAmount;
                    }
                    //Update LoanDisbursement with the Batch Total
                    var mainBatchDetails = await _channelService.FindLoanDisbursementBatchAsync(loanDisbursementBatchDTO.Id, GetServiceHeader());
                    mainBatchDetails.BatchTotal = batchTotal;
                    await _channelService.UpdateLoanDisbursementBatchAsync(mainBatchDetails, GetServiceHeader());
                }

                var submit = await _channelService.AuditLoanDisbursementBatchAsync(loanDisbursementBatchDTO, batchAuthOption, GetServiceHeader());

                TempData["Success"] = "Operation Completed Successfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanDisbursementBatchDTO.ErrorMessages;
                string errorMessage = string.Join("\n", errorMessages.Where(msg => !string.IsNullOrWhiteSpace(msg)));
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(loanDisbursementBatchDTO.AuthDescriptiom.ToString());
                TempData["Fail"] = $"Operation Failed!\n{errorMessage}";
                return View(loanDisbursementBatchDTO);
            }
        }
    }
}
