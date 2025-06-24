using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class TransactionJournalsController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            var j = journalEntryFilterSelectList(string.Empty);
            ViewBag.Journalfielter = j;
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, DateTime? startDate, DateTime? endDate, string reference, int? filter)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;


            var pageCollectionInfo = await _channelService.FindGeneralLedgerTransactionsByDateRangeAndFilterInPageAsync(
                int.MaxValue,
                int.MaxValue,
                startDate ?? new DateTime(1998, 1, 1),
                endDate ?? DateTime.Today,
                string.IsNullOrWhiteSpace(reference) ? "0000097~00001142" : reference,
                (int)filter,
                GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(x => x.JournalCreatedDate)
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
                items: new List<GeneralLedgerTransaction>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var systemGeneralLedgerAccountMappingDTO = await _channelService.FindSystemGeneralLedgerAccountMappingAsync(id, GetServiceHeader());

            return View(systemGeneralLedgerAccountMappingDTO);
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(string.Empty);
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(SystemGeneralLedgerAccountMappingDTO systemGeneralLedgerAccountMappingDTO)
        {
            systemGeneralLedgerAccountMappingDTO.ValidateAll();

            if (!systemGeneralLedgerAccountMappingDTO.HasErrors)
            {
                var result = await _channelService.AddSystemGeneralLedgerAccountMappingAsync(systemGeneralLedgerAccountMappingDTO, GetServiceHeader());

                if (result.ErrorMessageResult != null)
                {
                    await ServeNavigationMenus();

                    ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(string.Empty);

                    TempData["ErrorMsg"] = result.ErrorMessageResult;

                    return View();
                }

                TempData["AlertMessage"] = "Successfully mapped G/L Account";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = systemGeneralLedgerAccountMappingDTO.ErrorMessages;
                ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(systemGeneralLedgerAccountMappingDTO.SystemGeneralLedgerAccountCode.ToString());

                TempData["CreateError"] = "Failed to Map G/L Account";

                return View(systemGeneralLedgerAccountMappingDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(string.Empty);

            var systemGeneralLedgerAccountMappingDTO = await _channelService.FindSystemGeneralLedgerAccountMappingAsync(id, GetServiceHeader());

            return View(systemGeneralLedgerAccountMappingDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, SystemGeneralLedgerAccountMappingDTO systemGeneralLedgerAccountMappingDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateSystemGeneralLedgerAccountMappingAsync(systemGeneralLedgerAccountMappingDTO, GetServiceHeader());

                TempData["EditMessage"] = "Successfully edited G/L Account Determination";

                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(systemGeneralLedgerAccountMappingDTO.SystemGeneralLedgerAccountCode.ToString());

                TempData["EditError"] = "Failed to edit G/L Account Determination";

                return View(systemGeneralLedgerAccountMappingDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSystemGeneralLedgerAccountMappingsAsync()
        {
            var systemGeneralLedgerAccountMappingDTOs = await _channelService.FindSystemGeneralLedgerAccountMappingsAsync(GetServiceHeader());

            return Json(systemGeneralLedgerAccountMappingDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
