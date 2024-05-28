
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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



        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var wireTransferTypeDTO = await _channelService.FindWireTransferTypesAsync(GetServiceHeader());

            return View(wireTransferTypeDTO);
        }


        public async Task<ActionResult> WireTransferType(WireTransferTypeDTO wireTransferTypeDTO)
        {
            Session["ChartOfAccountId"] = wireTransferTypeDTO.ChartOfAccountId;
            Session["ChartOfAccountName"] = wireTransferTypeDTO.ChartOfAccountAccountName;
            Session["Description"] = wireTransferTypeDTO.Description;
            Session["TransactionOwnership"] = wireTransferTypeDTO.TransactionOwnership;
            Session["IsLocked"] = wireTransferTypeDTO.IsLocked;

            return View("Create", wireTransferTypeDTO);
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.TransactionOwnership = GetTransactionOwnershipSelectList(string.Empty);

            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(WireTransferTypeDTO wireTransferTypeDTO, ObservableCollection<CommissionDTO> selectedRows)
        {
            wireTransferTypeDTO.ValidateAll();

            if (!wireTransferTypeDTO.HasErrors)
            {
                await _channelService.AddWireTransferTypeAsync(wireTransferTypeDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Successfully Created Wire Transfer Type";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = wireTransferTypeDTO.ErrorMessages;

                ViewBag.TransactionOwnership = GetTransactionOwnershipSelectList(wireTransferTypeDTO.TransactionOwnershipDescription);

                TempData["CreateError"] = "Failed to Create Wire Transfer Type";

                return View(wireTransferTypeDTO);
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, WireTransferTypeDTO wireTransferTypeBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateWireTransferTypeAsync(wireTransferTypeBindingModel, GetServiceHeader());

                TempData["Edit"] = "Successfully Edited Wire Transfer Type";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["EditError"] = "Failed to Edit Wire Transfer Type";

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
