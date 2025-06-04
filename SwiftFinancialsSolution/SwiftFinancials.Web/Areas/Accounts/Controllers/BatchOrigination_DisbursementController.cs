using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BatchOrigination_DisbursementController : MasterController
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

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var LDB = await _channelService.FindLoanDisbursementBatchAsync(id, GetServiceHeader());
            var loanDisbursementBatchEntries = await _channelService.FindLoanDisbursementBatchEntriesByLoanDisbursementBatchIdAsync(id, GetServiceHeader());
            ViewBag.BatchEntries = loanDisbursementBatchEntries;
            return View(LDB);
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.Priority = GetQueuePriorityAsync(string.Empty);
            ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(string.Empty);
            ViewBag.BatchStatuselectList = GetBatchStatusTypeSelectList(string.Empty);
            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);


            ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);

            var verifiedLoanCasesList = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync((int)LoanCaseStatus.Audited, string.Empty, (int)LoanCaseFilter.CaseNumber, 0, 200, false, GetServiceHeader());
            var verifiedLoanCases = verifiedLoanCasesList.PageCollection.Where(x => x.IsBatched == false);

            ViewBag.BatchEntries = verifiedLoanCases;
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(LoanDisbursementBatchDTO loanDisbursementBatchDTO, string[] BatchEntries)
        {
          
            LoanCaseDTO loanCaseDTO = new LoanCaseDTO();
            LoanDisbursementBatchEntryDTO loanDisbursementBatchEntryDTO = new LoanDisbursementBatchEntryDTO();

            loanDisbursementBatchDTO.ValidateAll();



            int batchAuthOption = loanDisbursementBatchDTO.Auth;
            if (BatchEntries != null && BatchEntries.Any())
            {
                var selectedBatchIds = BatchEntries.Select(Guid.Parse).ToList();

            
            loanDisbursementBatchDTO.ValidateAll();

                if (!loanDisbursementBatchDTO.HasErrors)
                {

              var k=  await _channelService.AddLoanDisbursementBatchAsync(loanDisbursementBatchDTO, GetServiceHeader());
                    List<LoanDisbursementBatchEntryDTO> batchEntryDTO = new List<LoanDisbursementBatchEntryDTO>();

                    foreach (var loanCaseId in selectedBatchIds)
                    {
                        loanDisbursementBatchEntryDTO.LoanCaseId = loanCaseId;
                        loanDisbursementBatchEntryDTO.LoanDisbursementBatchId = k.Id;

                        await _channelService.AddLoanDisbursementBatchEntryAsync(loanDisbursementBatchEntryDTO, GetServiceHeader());

                        // Calculate Batch Total
                        List<LoanCaseDTO> LCDTO = new List<LoanCaseDTO>();
                        decimal batchTotal = 0;

                        var loanCase = await _channelService.FindLoanCaseAsync(loanCaseId, GetServiceHeader());
                        await UpdateLoanCaseBatchNumber(loanCase.CaseNumber, k.BatchNumber);

                        if (loanCase != null)
                        {
                            LCDTO.Add(loanCase);
                            batchTotal += loanCase.ApprovedAmount;
                        }
                        //Update LoanDisbursement with the Batch Total
                        var mainBatchDetails = await _channelService.FindLoanDisbursementBatchAsync(k.Id, GetServiceHeader());
                        mainBatchDetails.BatchTotal = batchTotal;
                        await _channelService.UpdateLoanDisbursementBatchAsync(mainBatchDetails, GetServiceHeader());
                        await UpdateLoanCaseBatchNumber(loanCase.CaseNumber, mainBatchDetails.BatchNumber);

                    }
                }
                var submit = await _channelService.AuditLoanDisbursementBatchAsync(loanDisbursementBatchDTO, batchAuthOption, GetServiceHeader());

                TempData["Success"] = "Operation Completed Successfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanDisbursementBatchDTO.ErrorMessages;
                ViewBag.BatchStatuselectList = GetBatchStatusTypeSelectList(loanDisbursementBatchDTO.Status.ToString());
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(loanDisbursementBatchDTO.Type.ToString());
                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(loanDisbursementBatchDTO.Priority.ToString());
                ViewBag.Priority = GetQueuePriorityAsync(loanDisbursementBatchDTO.Priority.ToString());

                return View(loanDisbursementBatchDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.Priority = GetQueuePriorityAsync(string.Empty);
            ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(string.Empty);
            ViewBag.BatchStatuselectList = GetBatchStatusTypeSelectList(string.Empty);
            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);
            ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);

            var LDB = await _channelService.FindLoanDisbursementBatchAsync(id, GetServiceHeader());

            return View(LDB);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(LoanDisbursementBatchDTO loanDisbursementBatchDTO)
        {
            loanDisbursementBatchDTO.ValidateAll();

            if (!loanDisbursementBatchDTO.HasErrors)
            {
                await _channelService.UpdateLoanDisbursementBatchAsync(loanDisbursementBatchDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanDisbursementBatchDTO.ErrorMessages;

                ViewBag.BatchStatuselectList = GetBatchStatusTypeSelectList(loanDisbursementBatchDTO.Status.ToString());
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(loanDisbursementBatchDTO.Type.ToString());
                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(loanDisbursementBatchDTO.Priority.ToString());
                ViewBag.Priority = GetQueuePriorityAsync(loanDisbursementBatchDTO.Priority.ToString());

                return View(loanDisbursementBatchDTO);
            }
        }
    }
}
