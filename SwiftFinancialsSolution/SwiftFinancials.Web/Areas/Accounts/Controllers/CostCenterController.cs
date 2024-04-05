using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class CostCenterController : MasterController
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

            var pageCollectionInfo = await _channelService.FindCostCentersByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(costCenter => costCenter.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CostCenterDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var costCenterDTO = await _channelService.FindCostCenterAsync(id, GetServiceHeader());

            return View(costCenterDTO);
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(CostCenterDTO costCenterDTO)
        {
            costCenterDTO.CreatedDate = DateTime.Today;

            costCenterDTO.ValidateAll();

            if (!costCenterDTO.HasErrors)
            {
                await _channelService.AddCostCenterAsync(costCenterDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Cost Centre created successfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = costCenterDTO.ErrorMessages;

                TempData["Error"] = "Failed to create Cost Centre";

                return View(costCenterDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var costCenterDTO = await _channelService.FindCostCenterAsync(id, GetServiceHeader());

            return View(costCenterDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CostCenterDTO costCenterDTO)
        {
            costCenterDTO.CreatedDate = DateTime.Today;

            if (ModelState.IsValid)
            {
                await _channelService.UpdateCostCenterAsync(costCenterDTO, GetServiceHeader());

                TempData["Edit"] = "Edited Cost Center successfully";

                return RedirectToAction("Index");
            }
            else
            {
                return View(costCenterDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCostCentersAsync()
        {
            var costCenterDTOs = await _channelService.FindCostCentersAsync(GetServiceHeader());

            return Json(costCenterDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
