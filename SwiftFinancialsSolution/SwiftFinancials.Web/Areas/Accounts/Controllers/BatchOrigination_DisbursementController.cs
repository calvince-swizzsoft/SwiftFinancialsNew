using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BatchOrigination_DisbursementController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            ViewBag.BatchStatus = GetBatchStatusTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int Status, DateTime startDate, DateTime endDate, string filterValue)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = new PageCollectionInfo<LoanDisbursementBatchDTO>();

            if (filterValue == string.Empty || filterValue == "" && startDate != null && endDate != null)
                pageCollectionInfo = await _channelService.FindLoanDisbursementBatchesByStatusAndFilterInPageAsync(
                 Status,
                 startDate,
                 endDate,
                 jQueryDataTablesModel.sSearch,
                 0,
                 int.MaxValue,
                 GetServiceHeader()
                 );
            else if (startDate == null || endDate == null)
                pageCollectionInfo = await _channelService.FindLoanDisbursementBatchesByStatusAndFilterInPageAsync(
                 Status,
                 DateTime.Now.AddDays(-1000),
                 DateTime.Now,
                 jQueryDataTablesModel.sSearch,
                 0,
                 int.MaxValue,
                 GetServiceHeader()
                 );
            else if (filterValue != null | filterValue != string.Empty && startDate != null && endDate != null)
                pageCollectionInfo = await _channelService.FindLoanDisbursementBatchesByStatusAndFilterInPageAsync(
                Status,
                startDate,
                endDate,
                filterValue,
                0,
                int.MaxValue,
                GetServiceHeader()
                );
            else
                pageCollectionInfo = await _channelService.FindLoanDisbursementBatchesByStatusAndFilterInPageAsync(
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


            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(LoanDisbursementBatchDTO loanDisbursementBatchDTO, LoanDisbursementBatchEntryDTO loanDisbursementBatchEntryDTO)
        {
            loanDisbursementBatchDTO.ValidateAll();

            if (!loanDisbursementBatchDTO.HasErrors)
            {
                var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
                if (userDTO.BranchId != null)
                {
                    loanDisbursementBatchDTO.BranchId = (Guid)userDTO.BranchId;
                }

                var loanDisbursement = await _channelService.AddLoanDisbursementBatchAsync(loanDisbursementBatchDTO, GetServiceHeader());
                TempData["SuccessMessage"] = "Loan Disbursement Batch created Successful ";
                //if (loanDisbursement != null)
                //{
                //    var verifiedLoanCasesList = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync((int)LoanCaseStatus.Audited, string.Empty, (int)LoanCaseFilter.CaseNumber, 0, 200, false, GetServiceHeader());

                //    var verifiedLoanCases = verifiedLoanCasesList.PageCollection.Where(x => x.IsBatched == false);

                //    foreach (var loanCase in verifiedLoanCases)
                //    {
                //        loanDisbursementBatchEntryDTO.LoanCaseId = loanCase.Id;
                //        loanDisbursementBatchEntryDTO.LoanDisbursementBatchId = loanDisbursement.Id;

                //        await _channelService.AddLoanDisbursementBatchEntryAsync(loanDisbursementBatchEntryDTO, GetServiceHeader());
                //    }
                //}

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

        //public async Task<ActionResult> Verify(Guid id)
        //{
        //    await ServeNavigationMenus();

        //    ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
        //    ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(string.Empty);
        //    ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
        //    ViewBag.BatchStatuselectList = GetBatchStatusTypeSelectList(string.Empty);
        //    ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

        //    ViewBag.Priority = GetQueuePriorityAsync(string.Empty);


        //    var loanDisbursementBatchDTO = await _channelService.FindLoanDisbursementBatchAsync(id, GetServiceHeader());

        //    return View(loanDisbursementBatchDTO);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Verify(Guid id, LoanDisbursementBatchDTO loanDisbursementBatchDTO)
        //{
        //    loanDisbursementBatchDTO.ValidateAll();
        //    int batchAuthOption = loanDisbursementBatchDTO.Auth;
        //    if (!loanDisbursementBatchDTO.HasErrors)
        //    {
        //        await _channelService.AuditLoanDisbursementBatchAsync(loanDisbursementBatchDTO, batchAuthOption, GetServiceHeader());

        //        ViewBag.BatchType = GetWireTransferBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
        //        ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
        //        ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(loanDisbursementBatchDTO.Priority.ToString());

        //        ViewBag.Priority = GetQueuePriorityAsync(loanDisbursementBatchDTO.Priority.ToString());

        //        return RedirectToAction("Index");
        //    }
        //    else
        //    {
        //        var errorMessages = loanDisbursementBatchDTO.ErrorMessages;
        //        ViewBag.BatchStatuselectList = GetBatchStatusTypeSelectList(loanDisbursementBatchDTO.Status.ToString());
        //        ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(loanDisbursementBatchDTO.Type.ToString());

        //        ViewBag.BatchType = GetWireTransferBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
        //        ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
        //        ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(loanDisbursementBatchDTO.Priority.ToString());

        //        ViewBag.Priority = GetQueuePriorityAsync(loanDisbursementBatchDTO.Priority.ToString());

        //        TempData["Verification"] = "Verification successfull";
        //        return View(loanDisbursementBatchDTO);
        //    }
        //}

        //public async Task<ActionResult> Authorize(Guid id)
        //{
        //    await ServeNavigationMenus();

        //    ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
        //    ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(string.Empty);
        //    ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
        //    ViewBag.BatchStatuselectList = GetBatchStatusTypeSelectList(string.Empty);
        //    ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

        //    ViewBag.Priority = GetQueuePriorityAsync(string.Empty);

        //    var loanDisbursementBatchDTO = await _channelService.FindLoanDisbursementBatchAsync(id, GetServiceHeader());

        //    return View(loanDisbursementBatchDTO);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Authorize(Guid id, LoanDisbursementBatchDTO loanDisbursementBatchDTO)
        //{
        //    /*var batchAuthOption = wireTransferBatchDTO.batch*/
        //    int batchAuthOption = loanDisbursementBatchDTO.Auth;
        //    loanDisbursementBatchDTO.ValidateAll();



        //    if (!loanDisbursementBatchDTO.HasErrors)
        //    {
        //        await _channelService.AuthorizeLoanDisbursementBatchAsync(loanDisbursementBatchDTO,batchAuthOption, 1, GetServiceHeader());

        //        ViewBag.BatchType = GetWireTransferBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
        //        ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
        //        ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(loanDisbursementBatchDTO.Priority.ToString());
        //        ViewBag.BatchStatuselectList = GetBatchStatusTypeSelectList(loanDisbursementBatchDTO.Status.ToString());
        //        ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(loanDisbursementBatchDTO.Auth.ToString());

        //        ViewBag.Priority = GetQueuePriorityAsync(loanDisbursementBatchDTO.Priority.ToString());
        //        TempData["Authorization"] = "Authorization successfull";
        //        return RedirectToAction("Index");
        //    }
        //    else
        //    {
        //        var errorMessages = loanDisbursementBatchDTO.ErrorMessages;

        //        ViewBag.BatchType = GetWireTransferBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
        //        ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
        //        ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(loanDisbursementBatchDTO.Priority.ToString());

        //        ViewBag.Priority = GetQueuePriorityAsync(loanDisbursementBatchDTO.Priority.ToString());

        //        return View(loanDisbursementBatchDTO);
        //    }
        //}
    }

}
