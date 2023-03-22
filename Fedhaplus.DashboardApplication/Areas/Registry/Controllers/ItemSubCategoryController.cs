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
    public class ItemSubCategoryController : MasterController
    {
        // GET: Registry/ItemSubCategory
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

            var pageCollectionInfo = await _channelService.FindItemSubCategoriesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<ItemSubCategoryDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var itemSubCategoryDTO = await _channelService.FindItemSubCategoryAsync(id, GetServiceHeader());

            return View(itemSubCategoryDTO.ProjectedAs<ItemSubCategoryDTO>());
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(ItemSubCategoryBindingModel itemSubCategoryBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.AddItemSubCategoryAsync(itemSubCategoryBindingModel.MapTo<ItemSubCategoryDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(itemSubCategoryBindingModel);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var itemSubCategoryDTO = await _channelService.FindItemSubCategoryAsync(id, GetServiceHeader());

            return View(itemSubCategoryDTO.MapTo<ItemSubCategoryBindingModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, ItemSubCategoryBindingModel itemSubCategoryBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateItemSubCategoryAsync(itemSubCategoryBindingModel.MapTo<ItemSubCategoryDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(itemSubCategoryBindingModel);
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
