using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
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

            int status = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindJournalReversalBatchesByStatusAndFilterInPageAsync(status, startDate, endDate, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(debitBatch => debitBatch.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<JournalReversalBatchDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
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

        //public async Task<ActionResult> Edit(Guid id)
        //{
        //    await ServeNavigationMenus();

        //    ViewBag.BatchType = GetWireTransferBatchTypeSelectList(string.Empty);
        //    ViewBag.DisbursementType = GetLoanDisbursementTypeBatchTypeSelectList(string.Empty);
        //    ViewBag.Category = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);


        //    var debitBatchDTO = await _channelService.FindDebitBatchAsync(id, GetServiceHeader());

        //    return View(debitBatchDTO);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Edit(Guid id, WireTransferBatchDTO wireTransferBatchDTO)
        //{
        //    wireTransferBatchDTO.ValidateAll();

        //    if (!wireTransferBatchDTO.HasErrors)
        //    {
        //        await _channelService.UpdateWireTransferBatchAsync(wireTransferBatchDTO, GetServiceHeader());

        //        return RedirectToAction("Index");
        //    }
        //    else
        //    {
        //        var errorMessages = wireTransferBatchDTO.ErrorMessages;

        //        ViewBag.BatchType = GetWireTransferBatchTypeSelectList(wireTransferBatchDTO.Priority.ToString());
        //        ViewBag.Priority = GetQueuePriorityAsync(wireTransferBatchDTO.Priority.ToString());

        //        return View(wireTransferBatchDTO);
        //    }
        //}

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
