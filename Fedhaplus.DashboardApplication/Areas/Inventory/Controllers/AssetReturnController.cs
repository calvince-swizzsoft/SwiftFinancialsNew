using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.TransactionsModule;
using Fedhaplus.DashboardApplication.Controllers;
using Fedhaplus.DashboardApplication.Helpers;
using Fedhaplus.Presentation.Infrastructure.Util;
using Infrastructure.Crosscutting.Framework.Adapter;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fedhaplus.DashboardApplication.Areas.Inventory.Controllers
{
    public class AssetReturnController : MasterController
    {
        // GET: Inventory/AssetReturn
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

            var pageCollectionInfo = await _channelService.FindAssetAllocationsByRecordStatusAndFilterInPageAsync((int)AssetAllocationStatus.Authorized, DateTime.Today.AddDays(-60), DateTime.Today, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<AssetAllocationDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var assetReturnDTO = await _channelService.FindAssetReturnAsync(id, GetServiceHeader());

            return View(assetReturnDTO.ProjectedAs<AssetReturnDTO>());
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(AssetReturnBindingModel assetReturnBindingModel)
        {
            if (ModelState.IsValid)
            {
                var assetReturnDTO = await _channelService.AddNewAssetReturnAsync(assetReturnBindingModel.MapTo<AssetReturnDTO>(), GetServiceHeader());

                var assetAllocationDTO = await _channelService.FindAssetAllocationAsync(assetReturnBindingModel.AssetAllocationId, GetServiceHeader());

                assetAllocationDTO.RecordStatus = (short)AssetRegisterStatus.Returned;

                await _channelService.UpdateAssetAllocationAsync(assetAllocationDTO, GetServiceHeader());

                var assetRegisterDTO = await _channelService.FindAssetRegisterAsync(assetAllocationDTO.AssetRegisterId, GetServiceHeader());

                assetRegisterDTO.RecordStatus = (short)AssetRegisterStatus.Available;

                await _channelService.UpdateAssetRegisterAsync(assetRegisterDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(assetReturnBindingModel);
            }
        }

        public async Task<ActionResult> Return(Guid id)
        {
            await ServeNavigationMenus();

            var assetAllocationDTO = await _channelService.FindAssetAllocationAsync(id, GetServiceHeader());

            return View(assetAllocationDTO.MapTo<AssetAllocationBindingModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Return(Guid id, AssetAllocationBindingModel assetAllocationBindingModel)
        {
            if (ModelState.IsValid)
            {
                assetAllocationBindingModel.RecordStatus = (int)AssetAllocationStatus.Returned;
                await _channelService.UpdateAssetAllocationAsync(assetAllocationBindingModel.MapTo<AssetAllocationDTO>(), GetServiceHeader());

                var assetReturnBindingModel = new AssetAllocationBindingModel();
                assetReturnBindingModel.RecordStatus = (int)AssetReturnStatus.Completed;
                assetReturnBindingModel.AssetRegisterId = assetAllocationBindingModel.AssetRegisterId;
                assetReturnBindingModel.CustomerId = assetAllocationBindingModel.CustomerId;
                assetReturnBindingModel.Station = assetAllocationBindingModel.Station;
                assetReturnBindingModel.Remarks = assetAllocationBindingModel.Remarks;
                assetReturnBindingModel.AuthorizationDate = DateTime.UtcNow;
                assetReturnBindingModel.AuthorizationRemarks = assetAllocationBindingModel.AuthorizationRemarks;
                await _channelService.AddNewAssetReturnAsync(assetReturnBindingModel.MapTo<AssetReturnDTO>(), GetServiceHeader());

                var assetRegisterDTO = await _channelService.FindAssetRegisterAsync(assetAllocationBindingModel.AssetRegisterId, GetServiceHeader());
                assetRegisterDTO.RecordStatus = (short)AssetRegisterStatus.Available;
                await _channelService.UpdateAssetRegisterAsync(assetRegisterDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(assetAllocationBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetAssetReturnsAsync()
        {
            var assetReturnDTOs = await _channelService.FindAssetReturnsAsync(GetServiceHeader());

            return Json(assetReturnDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}