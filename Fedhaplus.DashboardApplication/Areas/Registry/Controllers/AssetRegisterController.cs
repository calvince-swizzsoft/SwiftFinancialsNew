using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.RegistryModule;
using Fedhaplus.DashboardApplication.Controllers;
using Fedhaplus.DashboardApplication.Helpers;
using Fedhaplus.Presentation.Infrastructure.Util;
using Infrastructure.Crosscutting.Framework.Adapter;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Areas.Registry.Controllers
{
    public class AssetRegisterController : MasterController
    {
        // GET: Registry/AssetRegister
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

            var pageCollectionInfo = await _channelService.FindAssetsRegisterByFilterInPageAsync(0, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<AssetRegisterDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var assetRegisterDTO = await _channelService.FindAssetRegisterAsync(id, GetServiceHeader());

            return View(assetRegisterDTO.ProjectedAs<AssetRegisterDTO>());
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(AssetRegisterBindingModel assetRegisterBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.AddAssetRegisterAsync(assetRegisterBindingModel.MapTo<AssetRegisterDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(assetRegisterBindingModel);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var assetRegisterDTO = await _channelService.FindAssetRegisterAsync(id, GetServiceHeader());

            return View(assetRegisterDTO.MapTo<AssetRegisterBindingModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, AssetRegisterBindingModel assetRegisterBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateAssetRegisterAsync(assetRegisterBindingModel.MapTo<AssetRegisterDTO>(), GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(assetRegisterBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetItemCategoriesAsync()
        {
            var itemCategoryDTOs = await _channelService.FindItemCategoriesAsync(GetServiceHeader());

            return Json(itemCategoryDTOs.Where(x => x.IsEnabledForAssetRegister == true), JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> GetItemSubCategoriesAsync(AssetRegisterBindingModel assetRegisterBindingModel)
        {
            var itemSubCategoryDTOs = await _channelService.FindItemSubCategoriesAsync(GetServiceHeader());

            //string categoryid = Guid.Empty.ToString();

            // categoryid = Request.Form["Category"];

            return Json(itemSubCategoryDTOs.Where(x => x.IsEnabledForAssetRegister = true), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetAssetRegistersAsync()
        {
            var assetRegisterDTOs = await _channelService.FindAssetRegistersAsync(GetServiceHeader());

            return Json(assetRegisterDTOs.Where(x => x.RecordStatus == (short)AssetRegisterStatus.Available && x.IsLocked == false), JsonRequestBehavior.AllowGet);
        }
    }
}