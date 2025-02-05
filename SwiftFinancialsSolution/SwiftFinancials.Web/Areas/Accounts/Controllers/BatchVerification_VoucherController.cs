using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Domain.MainBoundedContext.AccountsModule.Aggregates.JournalVoucherAgg;
using Domain.MainBoundedContext.ValueObjects;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BatchVerification_VoucherController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            ViewBag.BatchStatus = GetBatchStatusTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int status, DateTime startDate, DateTime endDate)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindJournalVouchersByStatusAndFilterInPageAsync(status, startDate, endDate, jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                /*pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(journalVoucher => journalVoucher.CreatedDate).ToList();*/

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<JournalVoucherDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var journalVoucherDTO = await _channelService.FindJournalVoucherAsync(id, GetServiceHeader());
            var voucherBatches = await _channelService.FindJournalVoucherEntriesByJournalVoucherIdAsync(id, GetServiceHeader());

            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);

            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(string.Empty);

            ViewBag.JournalVoucherEntryDTOs = voucherBatches;
            return View(journalVoucherDTO);
        }

        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);
            ViewBag.JournalVoucherAuthOptionSelectList = GetJournalVoucherAuthOptionSelectList(string.Empty);

            var journalVoucherDTO = await _channelService.FindJournalVoucherAsync(id, GetServiceHeader());

            var voucherBatches = await _channelService.FindJournalVoucherEntriesByJournalVoucherIdAsync(id, GetServiceHeader());

            ViewBag.JournalVoucherEntryDTOs = voucherBatches;

            return View(journalVoucherDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, JournalVoucherDTO journalVoucherDTO)
        {
            journalVoucherDTO.ValidateAll();
            int AuthOption = journalVoucherDTO.AuthOption;
            if (!journalVoucherDTO.HasErrors)
            {
                await _channelService.AuditJournalVoucherAsync(journalVoucherDTO, AuthOption, GetServiceHeader());
                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());

                ViewBag.JournalVoucherAuthOptionSelectList = GetJournalVoucherAuthOptionSelectList(journalVoucherDTO.AuthOption.ToString());
                TempData["verifySuccess"] = "Journal batches have been verified succesifully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = journalVoucherDTO.ErrorMessages;

                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());

                ViewBag.JournalVoucherAuthOptionSelectList = GetJournalVoucherAuthOptionSelectList(journalVoucherDTO.AuthOption.ToString());
                return View(journalVoucherDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetJournalVouchersAsync()
        {
            var journalVoucherDTOs = await _channelService.FindJournalVouchersAsync(GetServiceHeader());

            return Json(journalVoucherDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
