using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.EnterpriseServices;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.RegistryModule;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Attributes;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    [RoleBasedAccessControl]
    public class ZoneController : MasterController
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

            var pageCollectionInfo = await _channelService.FindZonesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<ZoneDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var zoneDTO = await _channelService.FindZoneAsync(id, GetServiceHeader());

            return View(zoneDTO);
        }
        [RoleBasedAccessControl]
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        [RoleBasedAccessControl]
        public async Task<ActionResult> Create(ZoneBindingModel zoneBindingModel)
        {
            zoneBindingModel.ValidateAll();

            if (!zoneBindingModel.ErrorMessages.Any())
            {
                var zone = await _channelService.AddZoneAsync(zoneBindingModel.MapTo<ZoneDTO>(), GetServiceHeader());

                if (zone != null)
                {
                    //Update Stations

                    var stations = new ObservableCollection<StationDTO>();

                    foreach (var stationDTO in zoneBindingModel.Stations)
                    {
                        stationDTO.ZoneId = zone.Id;

                        stations.Add(stationDTO);
                    }

                    await _channelService.UpdateStationsByZoneIdAsync(zone.Id, stations, GetServiceHeader());
                }

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(zoneBindingModel);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var zoneDTO = await _channelService.FindZoneAsync(id, GetServiceHeader());

            return View(zoneDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, ZoneBindingModel zoneBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateZoneAsync(zoneBindingModel.MapTo<ZoneDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(zoneBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetZonesAsync()
        {
            var zoneDTOs = await _channelService.FindZonesAsync(GetServiceHeader());

            return Json(zoneDTOs, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetStationsAsync()
        {
            var stationsDTOs = await _channelService.FindStationsAsync(GetServiceHeader());

            return Json(stationsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}