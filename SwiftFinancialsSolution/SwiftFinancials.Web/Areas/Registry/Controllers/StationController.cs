using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class StationController : MasterController
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

            var pageCollectionInfo = await _channelService.FindStationsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<StationDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var StationDTO = await _channelService.FindStationAsync(id, GetServiceHeader());

            return View(StationDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        //[HttpPost]
        //public async Task<ActionResult> Create(StationBindingModel stationBindingModel)
        //{
        //    stationBindingModel.ValidateAll();

        //    if (!stationBindingModel.HasErrors)
        //    {
        //        await _channelService.AddStationAsync(stationBindingModel, GetServiceHeader());

        //        return RedirectToAction("Index");
        //    }
        //    else
        //    {
        //        var errorMessages = stationBindingModel.ErrorMessages;

        //        return View(stationBindingModel);
        //    }
        //}

        //public async Task<ActionResult> Edit(Guid id)
        //{
        //    await ServeNavigationMenus();

        //    var stationDTO = await _channelService.FindStationAsync(id, GetServiceHeader());

        //    return View(stationDTO);
        //}

        /*
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, StationBindingModel stationBindingModel)
        {
            stationBindingModel.ValidateAll();

            if (!stationBindingModel.HasErrors)
            {
                await _channelService.UpdateStationAsync(stationBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(stationBindingModel);
            }
        }*/

        [HttpGet]
        public async Task<JsonResult> GetStationsAsync()
          {
            var stationsDTOs = await _channelService.FindStationsAsync(GetServiceHeader());

            return Json(stationsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}