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
    public class StationLinkageController : MasterController
    {


        [RoleBasedAccessControl]
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            Guid Stationid = new Guid("5168F019-A5C7-E811-835A-08D40C76F50B");
            var customerslinkages = await _channelService.FindCustomersByStationIdAndFilterInPageAsync(Stationid, "", 1, 0, int.MaxValue, GetServiceHeader());
            if (customerslinkages != null && customerslinkages.PageCollection.Any())
            {

                var sortedData = customerslinkages.PageCollection
                    .OrderByDescending(i => i.CreatedDate)
                    .ToList();
                ViewBag.customerlinkage = sortedData;

            }
            return View();
        }

        [HttpPost]
        [RoleBasedAccessControl]
        public async Task<ActionResult> Create(StationDTO stationDTO)
        {
            stationDTO.ValidateAll();

            if (!stationDTO.ErrorMessages.Any())
            {
                var zone = await _channelService.AddZoneAsync(stationDTO.MapTo<ZoneDTO>(), GetServiceHeader());


                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(stationDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var stationDTO = await _channelService.FindZoneAsync(id, GetServiceHeader());

            return View(stationDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, StationDTO zoneBindingModel)
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