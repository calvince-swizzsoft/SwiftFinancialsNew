using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BatchAuthorization_RefundController : MasterController
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

            var pageCollectionInfo = await _channelService.FindOverDeductionBatchesByStatusAndFilterInPageAsync(
                (int)BatchStatus.Audited,
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
                items: new List<OverDeductionBatchDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }



        public async Task<ActionResult> Authorize(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

            var debitBatchDTO = await _channelService.FindOverDeductionBatchAsync(id, GetServiceHeader());
            var overdeductionEntries = await _channelService.FindOverDeductionBatchEntriesByOverDeductionBatchIdAsync(id, true, GetServiceHeader());
            ViewBag.OverDeductionBatchEntryDTOs = overdeductionEntries;

            return View(debitBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(Guid id, OverDeductionBatchDTO overDeductionBatchDTO)
        {
            var batchAuthOption = overDeductionBatchDTO.RefundAuthOption;
            overDeductionBatchDTO.ValidateAll();

            if (!overDeductionBatchDTO.HasErrors)
            {
                await _channelService.AuthorizeOverDeductionBatchAsync(overDeductionBatchDTO, batchAuthOption, 1, GetServiceHeader());

                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(overDeductionBatchDTO.RefundAuthOption.ToString());

                TempData["Success"] = "Operation Completed Successfully";
                return RedirectToAction("Index");
            }
            else
            {
                await ServeNavigationMenus();
                var errorMessages = overDeductionBatchDTO.ErrorMessages;
                string errorMessage = string.Join("\n", errorMessages.Where(msg => !string.IsNullOrWhiteSpace(msg)));

                TempData["Fail"] = $"Operation Failed\n{errorMessage}"; ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(overDeductionBatchDTO.RefundAuthOption.ToString());

                return View(overDeductionBatchDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetDebitBatchesAsync()
        {
            var debitBatchDTOs = await _channelService.FindDebitBatchesAsync(GetServiceHeader());

            return Json(debitBatchDTOs, JsonRequestBehavior.AllowGet);
        }
    }

}