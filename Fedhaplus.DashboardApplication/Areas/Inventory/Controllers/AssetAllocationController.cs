using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.TransactionsModule;
using Domain.MainBoundedContext.RegistryModule.Aggregates.AssetRegisterAgg;
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
    public class AssetAllocationController : MasterController
    {
        // GET: Inventory/AssetAllocation
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

            var pageCollectionInfo = await _channelService.FindAssetAllocationsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<AssetAllocationDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Authorize()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Authorize(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindAssetAllocationsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.Where(x => x.RecordStatus == (short)AssetAllocationStatus.NotAuthorized).ToList();

                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<AssetAllocationDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var assetAllocationDTO = await _channelService.FindAssetAllocationAsync(id, GetServiceHeader());

            return View(assetAllocationDTO.ProjectedAs<AssetAllocationDTO>());
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(AssetAllocationBindingModel assetAllocationBindingModel)
        {
            if (ModelState.IsValid)
            {
                assetAllocationBindingModel.RecordStatus = (short)AssetAllocationStatus.NotAuthorized;

                await _channelService.AddNewAssetAllocationAsync(assetAllocationBindingModel.MapTo<AssetAllocationDTO>(), GetServiceHeader());

                var assetRegisterDTO = await _channelService.FindAssetRegisterAsync(assetAllocationBindingModel.AssetRegisterId, GetServiceHeader());

                assetRegisterDTO.RecordStatus = (short)AssetRegisterStatus.Booked;

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

        public async Task<ActionResult> Approve(Guid id)
        {
            await ServeNavigationMenus();

            var assetAllocationDTO = await _channelService.FindAssetAllocationAsync(id, GetServiceHeader());

            return View(assetAllocationDTO.MapTo<AssetAllocationBindingModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approve(Guid id, AssetAllocationBindingModel assetAllocationBindingModel)
        {
            if (ModelState.IsValid)
            {
                assetAllocationBindingModel.AuthorizationDate = DateTime.UtcNow;
                assetAllocationBindingModel.AuthorizedBy = GetServiceHeader().ApplicationUserName;
                assetAllocationBindingModel.RecordStatus = (short)AssetAllocationStatus.Authorized;

                await _channelService.UpdateAssetAllocationAsync(assetAllocationBindingModel.MapTo<AssetAllocationDTO>(), GetServiceHeader());

                var assetRegisterDTO = await _channelService.FindAssetRegisterAsync(assetAllocationBindingModel.AssetRegisterId, GetServiceHeader());

                assetRegisterDTO.RecordStatus = (short)AssetRegisterStatus.Issued;

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
                var assetReturnBindingModel = new AssetReturnBindingModel();
                assetReturnBindingModel.AssetAllocationId = assetAllocationBindingModel.Id;
                assetReturnBindingModel.RecordStatus = (int)AssetReturnStatus.Completed;
                assetReturnBindingModel.Remarks = assetAllocationBindingModel.Remarks;
                await _channelService.AddNewAssetReturnAsync(assetReturnBindingModel.MapTo<AssetReturnDTO>(), GetServiceHeader());

                assetAllocationBindingModel.RecordStatus = (int)AssetAllocationStatus.Returned;
                await _channelService.UpdateAssetAllocationAsync(assetAllocationBindingModel.MapTo<AssetAllocationDTO>(), GetServiceHeader());

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
        public async Task<JsonResult> GetAssetAllocationsAsync()
        {
            var assetAllocationDTOs = await _channelService.FindAssetAllocationsAsync(GetServiceHeader());

            return Json(assetAllocationDTOs.Where(x => x.RecordStatus == (short)AssetAllocationStatus.NotAuthorized), JsonRequestBehavior.AllowGet);
        }
    }
}