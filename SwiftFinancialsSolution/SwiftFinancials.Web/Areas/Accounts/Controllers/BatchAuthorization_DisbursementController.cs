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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BatchAuthorization_DisbursementController : MasterController
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


        public async Task<ActionResult> Authorize(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

            var loanDisbursementBatchDTO = await _channelService.FindLoanDisbursementBatchAsync(id, GetServiceHeader());
            if (loanDisbursementBatchDTO.Status == (int)BatchStatus.Posted)
            {
                TempData["Authorized"] = "The selected Batch is already Authorized";
                return RedirectToAction("Index");
            }

            var loanDisbursementBatchEntries = await _channelService.FindLoanDisbursementBatchEntriesByLoanDisbursementBatchIdAsync(id, GetServiceHeader());
            ViewBag.BatchEntries = loanDisbursementBatchEntries;
            return View(loanDisbursementBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(LoanDisbursementBatchDTO loanDisbursementBatchDTO)
        {
            int batchAuthOption = loanDisbursementBatchDTO.Auth;
            loanDisbursementBatchDTO.ValidateAll();

            if (!loanDisbursementBatchDTO.HasErrors)
            {
                var findLoanDisbursementBatch = await _channelService.FindLoanDisbursementBatchAsync(loanDisbursementBatchDTO.Id, GetServiceHeader());

                var loanDisbursementBatchEntryDTO = await _channelService.FindLoanDisbursementBatchEntriesByLoanDisbursementBatchIdAsync(
                    findLoanDisbursementBatch.Id,
                    GetServiceHeader());

                foreach (var entries in loanDisbursementBatchEntryDTO)
                {
                    var findLoanCase = await _channelService.FindLoanCaseAsync(entries.LoanCaseId, GetServiceHeader());
                    var customer = await _channelService.FindCustomerAsync(findLoanCase.CustomerId, GetServiceHeader());

                    #region Send Text Notification
                    if (loanDisbursementBatchDTO != null)
                    {
                        var smsBody = new StringBuilder();
                        smsBody.AppendFormat("Dear {0}. Your loan application of Kshs. {1} for product {2} has been processed on {3} and will be deposited to your bank account.",
                            customer.FullName,
                            findLoanCase.ApprovedAmount,
                            findLoanCase.LoanProductDescription,
                            DateTime.Now.ToString("MMMM dd, yyyy"));

                        var textAlertDTO = new TextAlertDTO
                        {
                            BranchId = loanDisbursementBatchDTO.BranchId,
                            TextMessageOrigin = (int)MessageOrigin.Within,
                            TextMessageRecipient = customer.AddressMobileLine,
                            TextMessageBody = smsBody.ToString(),
                            MessageCategory = (int)MessageCategory.SMSAlert,
                            AppendSignature = false,
                            TextMessagePriority = (int)QueuePriority.Highest,
                        };

                        var textAlertDTOs = new ObservableCollection<TextAlertDTO> { textAlertDTO };
                        await _channelService.AddTextAlertsAsync(textAlertDTOs, GetServiceHeader());
                    }
                    #endregion
                    var loanCaseDTO = findLoanCase as LoanCaseDTO;
                    loanCaseDTO.Status = (int)LoanCaseStatus.Disbursed;

                }
                await _channelService.AuthorizeLoanDisbursementBatchAsync(loanDisbursementBatchDTO, batchAuthOption, 1, GetServiceHeader());

                ViewBag.BatchType = GetWireTransferBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(loanDisbursementBatchDTO.Priority.ToString());
                ViewBag.BatchStatuselectList = GetBatchStatusTypeSelectList(loanDisbursementBatchDTO.Status.ToString());
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(loanDisbursementBatchDTO.Auth.ToString());

                ViewBag.Priority = GetQueuePriorityAsync(loanDisbursementBatchDTO.Priority.ToString());
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
