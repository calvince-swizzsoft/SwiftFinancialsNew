using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using AutoMapper.Execution;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BatchAuthorization_CreditController : MasterController
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

            var pageCollectionInfo = await _channelService.FindCreditBatchesByStatusAndFilterInPageAsync(
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
                items: new List<CreditBatchDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }


        public async Task<ActionResult> Authorize(Guid id)
        {
            await ServeNavigationMenus();


            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

            var creditBatchDTO = await _channelService.FindCreditBatchAsync(id, GetServiceHeader());

            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);

            var creditBatches = await _channelService.FindCreditBatchEntriesByCreditBatchIdAsync(id, true, GetServiceHeader());

            ViewBag.CrdeitBatchEntryDTOs = creditBatches;

            return View(creditBatchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(Guid id, CreditBatchDTO creditBatchDTO)
        {
            creditBatchDTO.ValidateAll();
            var batchAuthOption = creditBatchDTO.CreditAuthOption;
            if (!creditBatchDTO.HasErrors)
            {
                await _channelService.AuthorizeCreditBatchAsync(creditBatchDTO, batchAuthOption, 1, GetServiceHeader());
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(creditBatchDTO.Type.ToString());
                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(creditBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(creditBatchDTO.Priority.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(creditBatchDTO.ConcessionType.ToString());

                TempData["AuthorizeSuccess"] = "Operation Completed Successifully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = creditBatchDTO.ErrorMessages;
                string errorMessage = string.Join("\n", errorMessages.Where(msg => !string.IsNullOrWhiteSpace(msg)));

                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(creditBatchDTO.Type.ToString());
                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(creditBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(creditBatchDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(creditBatchDTO.Priority.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(creditBatchDTO.ConcessionType.ToString());

                TempData["Fail"] = "Operation Failed";
                return View(creditBatchDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCreditBatchesAsync()
        {
            var creditBatchDTOs = await _channelService.FindCreditBatchesAsync(GetServiceHeader());

            return Json(creditBatchDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
