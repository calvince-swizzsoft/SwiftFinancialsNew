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
    public class ItemCategoryController : MasterController
    {
        // GET: Registry/Employer
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

            var pageCollectionInfo = await _channelService.FindItemCategoriesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<ItemCategoryDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var itemCategoryDTO = await _channelService.FindItemCategoryAsync(id, GetServiceHeader());

            return View(itemCategoryDTO.ProjectedAs<ItemCategoryDTO>());
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

          //  ViewBag.ItemCategoryTypesList = GetItemCategoryTypes(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(ItemCategoryBindingModel itemCategoryBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.AddItemCategoryAsync(itemCategoryBindingModel.MapTo<ItemCategoryDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

               // ViewBag.ItemCategoryTypesList = GetItemCategoryTypes(string.Empty);

                TempData["Error"] = string.Join(",", allErrors);

                return View(itemCategoryBindingModel);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var itemCategoryDTO = await _channelService.FindEmployerAsync(id, GetServiceHeader());

            return View(itemCategoryDTO.MapTo<ItemCategoryBindingModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, ItemCategoryBindingModel itemCategoryBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateItemCategoryAsync(itemCategoryBindingModel.MapTo<ItemCategoryDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(itemCategoryBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetItemCategoriesAsync()
        {
            var itemCategoryDTOs = await _channelService.FindItemCategoriesAsync(GetServiceHeader());

            return Json(itemCategoryDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
