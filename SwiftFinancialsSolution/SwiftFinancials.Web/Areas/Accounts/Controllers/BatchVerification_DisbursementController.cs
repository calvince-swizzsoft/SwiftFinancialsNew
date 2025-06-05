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
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BatchVerification_DisbursementController : MasterController
    {
        string connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public async Task<ActionResult> UpdateLoanCaseBatchNumber(int CaseNumber, int BatchNumber, int status = 48829)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                var updateQuery = "UPDATE swiftFin_LoanCases SET BatchNumber=@BatchNumber, Status=@status Where CaseNumber=@CaseNumber";

                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@BatchNumber", BatchNumber);
                    cmd.Parameters.AddWithValue("@CaseNumber", CaseNumber);
                    cmd.Parameters.AddWithValue("@status", status);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            return RedirectToAction("");
        }


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
            var loan = await _channelService.FindLoanDisbursementBatchEntriesByLoanDisbursementBatchIdAsync(loanDisbursementBatchDTO.Id, GetServiceHeader());
            if (loanDisbursementBatchDTO.Status == (int)BatchStatus.Audited)
            {
                TempData["Audited"] = "The selected Batch is already Verified";
                return RedirectToAction("Index");
            }
            List<LoanCaseDTO> loanCaseDTO = new List<LoanCaseDTO>();
            foreach(var loancasess in loan)
            {
                var verifiedLoanCases = await _channelService.FindLoanCaseAsync(loancasess.LoanCaseId, GetServiceHeader());
                loanCaseDTO.Add(verifiedLoanCases);

            }
      

            ViewBag.BatchEntries = loanCaseDTO;
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
                var loan = await _channelService.UpdateLoanDisbursementBatchAsync(loanDisbursementBatchDTO, GetServiceHeader());

                foreach (var loanCaseId in selectedBatchIds)
                {
                    loanDisbursementBatchEntryDTO.LoanCaseId = loanCaseId;
                    loanDisbursementBatchEntryDTO.LoanDisbursementBatchId = loanDisbursementBatchDTO.Id;
                    batchEntryDTO.Add(loanDisbursementBatchEntryDTO);

                    await _channelService.UpdateLoanDisbursementBatchEntriesAsync(loanDisbursementBatchDTO.Id,batchEntryDTO, GetServiceHeader());

                    // Calculate Batch Total
                    List<LoanCaseDTO> LCDTO = new List<LoanCaseDTO>();
                    decimal batchTotal = 0;

                    var loanCase = await _channelService.FindLoanCaseAsync(loanCaseId, GetServiceHeader());
                    await UpdateLoanCaseBatchNumber(loanCase.CaseNumber, loanDisbursementBatchDTO.BatchNumber);

                    if (loanCase != null)
                    {
                        LCDTO.Add(loanCase);
                        batchTotal += loanCase.ApprovedAmount;
                    }
                    //Update LoanDisbursement with the Batch Total
                    var mainBatchDetails = await _channelService.FindLoanDisbursementBatchAsync(loanDisbursementBatchDTO.Id, GetServiceHeader());
                    mainBatchDetails.BatchTotal = batchTotal;
                    await _channelService.UpdateLoanDisbursementBatchAsync(mainBatchDetails, GetServiceHeader());
                    await UpdateLoanCaseBatchNumber(loanCase.CaseNumber, mainBatchDetails.BatchNumber);

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
