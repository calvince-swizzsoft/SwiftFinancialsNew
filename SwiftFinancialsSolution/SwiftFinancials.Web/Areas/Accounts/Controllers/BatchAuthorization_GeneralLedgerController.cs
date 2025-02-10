using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BatchAuthorization_GeneralLedgerController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.BatchStatus = GetBatchStatusTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = await _channelService.FindGeneralLedgersByStatusAndFilterInPageAsync(
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
                items: new List<GeneralLedgerDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        public async Task<ActionResult> Authorize(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);


            var generalLedgerDTO = await _channelService.FindGeneralLedgerAsync(id, GetServiceHeader());
            var batchentries = await _channelService.FindGeneralLedgerEntriesByGeneralLedgerIdAsync(id, GetServiceHeader());
            ViewBag.GeneralLedgerEntries = batchentries;
            return View(generalLedgerDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authorize(Guid id, GeneralLedgerDTO generalLedgerDTO)
        {
            var batchAuthOption = generalLedgerDTO.GeneralLedgerAuthOption;
            generalLedgerDTO.ValidateAll();

            if (!generalLedgerDTO.HasErrors)
            {
                await _channelService.AuthorizeGeneralLedgerAsync(generalLedgerDTO, batchAuthOption, 1, GetServiceHeader());


                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(generalLedgerDTO.GeneralLedgerAuthOption.ToString());

                TempData["AuthorizationSuccess"] = "Authorization Successiful";
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = generalLedgerDTO.ErrorMessages;
                ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(generalLedgerDTO.GeneralLedgerAuthOption.ToString());
                TempData["AuthorizationFail"] = "Authorization Failed!. Review all conditions.";

                return View(generalLedgerDTO);
            }
        }

    }
}