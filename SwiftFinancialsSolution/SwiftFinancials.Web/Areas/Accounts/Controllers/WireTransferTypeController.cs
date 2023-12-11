
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class WireTransferTypeController : MasterController
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

            var pageCollectionInfo = await _channelService.FindWireTransferTypesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<WireTransferTypeDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        //public async Task<ActionResult> Details(Guid id)
        //{
        //    await ServeNavigationMenus();

        //    var wireTransferTypeDTO = await _channelService.FindWireTransferTypesAsync(id, GetServiceHeader());

        //    return View(wireTransferTypeDTO);
        //}
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(WireTransferTypeDTO wireTransferTypeDTO)
        {
            wireTransferTypeDTO.ValidateAll();

            if (!wireTransferTypeDTO.HasErrors)
            {
                await _channelService.AddWireTransferTypeAsync(wireTransferTypeDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = wireTransferTypeDTO.ErrorMessages;

                return View(wireTransferTypeDTO);
            }
        }

        //public async Task<ActionResult> Edit(Guid id)
        //{
        //    await ServeNavigationMenus();

        //    var wireTransferTypeDTO = await _channelService.FindWireTransferTypesAsync(id, GetServiceHeader());

        //    return View(wireTransferTypeDTO);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, WireTransferTypeDTO wireTransferTypeBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateWireTransferTypeAsync(wireTransferTypeBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(wireTransferTypeBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetWireTransferTypesAsync()
        {
            var wireTransferTypeDTOs = await _channelService.FindWireTransferTypesAsync(GetServiceHeader());

            return Json(wireTransferTypeDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}
