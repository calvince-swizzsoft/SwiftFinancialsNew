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
    public class SupplierController : MasterController
    {
        // GET: Registry/Supplier
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

            var pageCollectionInfo = await _channelService.FindSuppliersByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<SupplierDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var supplierDTO = await _channelService.FindSupplierAsync(id, GetServiceHeader());

            return View(supplierDTO.ProjectedAs<SupplierDTO>());
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(SupplierBindingModel supplierBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.AddSupplierAsync(supplierBindingModel.MapTo<SupplierDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(supplierBindingModel);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var supplierDTO = await _channelService.FindSupplierAsync(id, GetServiceHeader());

            return View(supplierDTO.MapTo<SupplierBindingModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, SupplierBindingModel supplierBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateSupplierAsync(supplierBindingModel.MapTo<SupplierDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(supplierBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSuppliersAsync()
        {
            var supplierDTOs = await _channelService.FindSuppliersAsync(GetServiceHeader());

            return Json(supplierDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}