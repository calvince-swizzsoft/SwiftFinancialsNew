using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Registry.Controllers
{
    public class AdministrativeDivisionController : MasterController
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

            var pageCollectionInfo = await _channelService.FindAdministrativeDivisionsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<AdministrativeDivisionDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var administrativeDivisionDTO = await _channelService.FindAdministrativeDivisionAsync(id, GetServiceHeader());

            return View(administrativeDivisionDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(AdministrativeDivisionBindingModel administrativeDivisionBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.AddAdministrativeDivisionAsync(administrativeDivisionBindingModel.MapTo<AdministrativeDivisionDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(administrativeDivisionBindingModel);
            }
        }
        

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var administrativeDivisionDTO = await _channelService.FindAdministrativeDivisionAsync(id, GetServiceHeader());

            return View(administrativeDivisionDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, AdministrativeDivisionBindingModel administrativeDivisionBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateAdministrativeDivisionAsync(administrativeDivisionBindingModel.MapTo<AdministrativeDivisionDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(administrativeDivisionBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetAdministrativeDivisionsAsync()
        {
            var administrativeDivisionDTOs = await _channelService.FindAdministrativeDivisionsAsync(false,true,GetServiceHeader());

            return Json(administrativeDivisionDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}