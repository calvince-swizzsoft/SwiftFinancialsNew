using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
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
    public class BatchOrigination_ReversalController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, DateTime startDate, DateTime endDate)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = await _channelService.FindJournalReversalBatchesByStatusAndFilterInPageAsync(
                (int)BatchStatus.Pending, 
                startDate, 
                endDate, 
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
                items: new List<JournalReversalBatchDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var reversalBatch = await _channelService.FindJournalReversalBatchByIdAsync(id, GetServiceHeader());

            return View(reversalBatch);
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            //ViewBag.Priority = GetQueuePriorityAsync(string.Empty);
            //ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            //ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(string.Empty);
            //ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);


            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(JournalReversalBatchDTO journalReversalBatchDTO)
        {
            journalReversalBatchDTO.ValidateAll();

            if (!journalReversalBatchDTO.HasErrors)
            {
                await _channelService.AddJournalReversalBatchAsync(journalReversalBatchDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = journalReversalBatchDTO.ErrorMessages;

                //ViewBag.BatchType = GetWireTransferBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                //ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                //ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(loanDisbursementBatchDTO.Priority.ToString());
                //ViewBag.Priority = GetQueuePriorityAsync(loanDisbursementBatchDTO.Priority.ToString());

                return View(journalReversalBatchDTO);
            }
        }

       
        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            //ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            //ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(string.Empty);
            //ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);

            //ViewBag.Priority = GetQueuePriorityAsync(string.Empty);


            var journalReversalBatchDTO = await _channelService.FindJournalReversalBatchByIdAsync(id, GetServiceHeader());

            return View(journalReversalBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, JournalReversalBatchDTO journalReversalBatchDTO)
        {
            journalReversalBatchDTO.ValidateAll();

            if (!journalReversalBatchDTO.HasErrors)
            {
                await _channelService.AuditJournalReversalBatchAsync(journalReversalBatchDTO, 1, GetServiceHeader());

                //ViewBag.BatchType = GetWireTransferBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                //ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                //ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(loanDisbursementBatchDTO.Priority.ToString());

                //ViewBag.Priority = GetQueuePriorityAsync(loanDisbursementBatchDTO.Priority.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = journalReversalBatchDTO.ErrorMessages;

                //ViewBag.BatchType = GetWireTransferBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                //ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                //ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(loanDisbursementBatchDTO.Priority.ToString());

                //ViewBag.Priority = GetQueuePriorityAsync(loanDisbursementBatchDTO.Priority.ToString());

                return View(journalReversalBatchDTO);
            }
        }

        public async Task<ActionResult> Authorize(Guid id)
        {
            await ServeNavigationMenus();

            //ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
            //ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(string.Empty);
            //ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);

            //ViewBag.Priority = GetQueuePriorityAsync(string.Empty);

            var journalReversalBatchDTO = await _channelService.FindJournalReversalBatchByIdAsync(id, GetServiceHeader());

            return View(journalReversalBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(Guid id, JournalReversalBatchDTO journalReversalBatchDTO)
        {
            /*var batchAuthOption = wireTransferBatchDTO.batch*/

            journalReversalBatchDTO.ValidateAll();



            if (!journalReversalBatchDTO.HasErrors)
            {
                await _channelService.AuthorizeJournalReversalBatchAsync(journalReversalBatchDTO, 1, 1, GetServiceHeader());

                //ViewBag.BatchType = GetWireTransferBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                //ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                //ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(loanDisbursementBatchDTO.Priority.ToString());

                //ViewBag.Priority = GetQueuePriorityAsync(loanDisbursementBatchDTO.Priority.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = journalReversalBatchDTO.ErrorMessages;

                //ViewBag.BatchType = GetWireTransferBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                //ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(loanDisbursementBatchDTO.Priority.ToString());
                //ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(loanDisbursementBatchDTO.Priority.ToString());

                //ViewBag.Priority = GetQueuePriorityAsync(loanDisbursementBatchDTO.Priority.ToString());

                return View(journalReversalBatchDTO);
            }
        }



        //[HttpGet]
        //public async Task<JsonResult> GetDebitBatchesAsync()
        //{
        //    var debitBatchDTOs = await _channelService.FindDebitBatchesAsync(GetServiceHeader());

        //    return Json(debitBatchDTOs, JsonRequestBehavior.AllowGet);
        //}
    }

}
