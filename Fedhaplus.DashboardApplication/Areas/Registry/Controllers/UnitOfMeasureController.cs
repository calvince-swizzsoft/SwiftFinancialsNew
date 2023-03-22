using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.RegistryModule;
using Fedhaplus.DashboardApplication.Controllers;
using Fedhaplus.DashboardApplication.Helpers;
using Fedhaplus.Presentation.Infrastructure.Util;
using Infrastructure.Crosscutting.Framework.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Areas.Registry.Controllers
{
    public class UnitOfMeasureController : MasterController
    {
        // GET: Registry/UnitOfMeasure
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

            var pageCollectionInfo = await _channelService.FindUnitsOfMeasureByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<UnitOfMeasureDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var unitOfMeasureDTO = await _channelService.FindUnitOfMeasureAsync(id, GetServiceHeader());

            return View(unitOfMeasureDTO.ProjectedAs<UnitOfMeasureDTO>());
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(UnitOfMeasureBindingModel unitOfMeasureBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.AddUnitOfMeasureAsync(unitOfMeasureBindingModel.MapTo<UnitOfMeasureDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(unitOfMeasureBindingModel);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var unitOfMeasureDTO = await _channelService.FindUnitOfMeasureAsync(id, GetServiceHeader());

            return View(unitOfMeasureDTO.MapTo<UnitOfMeasureBindingModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, UnitOfMeasureBindingModel unitOfMeasureBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateUnitOfMeasureAsync(unitOfMeasureBindingModel.MapTo<UnitOfMeasureDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(unitOfMeasureBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetUnitOfMeasuresAsync()
        {
            var unitOfMeasureDTOs = await _channelService.FindUnitOfMeasuresAsync(GetServiceHeader());

            return Json(unitOfMeasureDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}