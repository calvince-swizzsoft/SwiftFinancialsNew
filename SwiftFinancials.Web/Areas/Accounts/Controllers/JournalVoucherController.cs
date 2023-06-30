using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class JournalVoucherController : MasterController
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

            var pageCollectionInfo = await _channelService.FindJournalVouchersByFilterInPageAsync(jQueryDataTablesModel.sSearch, 1, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<JournalVoucherDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var journalVoucherDTO = await _channelService.FindJournalVoucherAsync(id, GetServiceHeader());

            return View(journalVoucherDTO);
        }
        public async Task<ActionResult> Create()
        {

            await ServeNavigationMenus();
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(JournalVoucherDTO journalVoucherDTO)
        {
            var valuedate = Request["valuedate"];
            journalVoucherDTO.ValueDate = DateTime.ParseExact((Request["valuedate"].ToString()), "dd/MM/yyyy", CultureInfo.InvariantCulture);
            journalVoucherDTO.ValidateAll();

            if (!journalVoucherDTO.HasErrors)
            {
                await _channelService.AddJournalVoucherAsync(journalVoucherDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = journalVoucherDTO.ErrorMessages;

                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(journalVoucherDTO.Type.ToString());
                ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(journalVoucherDTO.EntryType.ToString());
                return View(journalVoucherDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var journalVoucherDTO = await _channelService.FindJournalVoucherAsync(id, GetServiceHeader());

            return View(journalVoucherDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, JournalVoucherDTO journalVoucherDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateJournalVoucherAsync(journalVoucherDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(journalVoucherDTO);
            }
        }

        /*[HttpGet]
        public async Task<JsonResult> GetJournalVouchersAsync()
        {
            var journalVoucherDTOs = await _channelService.FindJournalVouchersAsync(GetServiceHeader());

            return Json(journalVoucherDTOs, JsonRequestBehavior.AllowGet);
        }*/
    }
}
