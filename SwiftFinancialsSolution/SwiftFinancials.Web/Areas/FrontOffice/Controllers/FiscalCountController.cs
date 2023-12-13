using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class FiscalCountController : MasterController
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

            var pageCollectionInfo = await _channelService.FindTreasuriesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<FiscalCountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var fiscalCountDTO = await _channelService.FindFiscalCountAsync(id, GetServiceHeader());

            return View(fiscalCountDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(FiscalCountDTO fiscalCountDTO)
        {
            fiscalCountDTO.ValidateAll();

            if (!fiscalCountDTO.HasErrors)
            {
                await _channelService.AddFiscalCountAsync(fiscalCountDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = fiscalCountDTO.ErrorMessages;
                ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(fiscalCountDTO.TransactionType.ToString());

                return View(fiscalCountDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var fiscalCountDTO = await _channelService.FindFiscalCountAsync(id, GetServiceHeader());

            return View(fiscalCountDTO);
        }

        /*[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, FiscalCountDTO fiscalCountDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateFiscalCountAsync(fiscalCountDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(fiscalCountDTO);
            }
        }*/

        /*[HttpGet]
        public async Task<JsonResult> GetTreasuriesAsync()
        {
            var treasuriesDTOs = await _channelService.FindTreasuriesAsync(GetServiceHeader());

            return Json(treasuriesDTOs, JsonRequestBehavior.AllowGet);
        }*/
    }
}