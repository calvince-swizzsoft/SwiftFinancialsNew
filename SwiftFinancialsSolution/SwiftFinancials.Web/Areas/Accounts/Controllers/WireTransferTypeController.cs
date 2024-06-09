using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
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

            var wireTransferTypeDTO = await _channelService.FindCommissionsByWireTransferTypeIdAsync(id, GetServiceHeader());

            ViewBag.wireTransferTypeCommission = wireTransferTypeDTO;

            return View(wireTransferTypeDTO);
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.transactionOwnership = GetTransactionOwnershipSelectList(string.Empty);

            return View();
        }


        public async Task<ActionResult> WireTransferType(WireTransferTypeDTO wireTransferTypeDTO)
        {
            Session["Description"] = wireTransferTypeDTO.Description;
            Session["ChartOfAccountId"] = wireTransferTypeDTO.ChartOfAccountId;
            Session["ChartOfAccount"] = wireTransferTypeDTO.ChartOfAccountAccountName;
            Session["isLocked"] = wireTransferTypeDTO.IsLocked;
            Session["TransactionOwnership"] = wireTransferTypeDTO.TransactionOwnership;

            return View("Create", wireTransferTypeDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(WireTransferTypeDTO wireTransferTypeDTO, ObservableCollection<CommissionDTO> selectedRows)
        {
            wireTransferTypeDTO.Description = Session["Description"].ToString();
            wireTransferTypeDTO.ChartOfAccountId = (Guid)Session["ChartOfAccountId"];
            wireTransferTypeDTO.ChartOfAccountAccountName = Session["ChartOfAccount"].ToString();
            wireTransferTypeDTO.IsLocked = (bool)Session["isLocked"];
            wireTransferTypeDTO.TransactionOwnership = Convert.ToInt32(Session["TransactionOwnership"].ToString());

            wireTransferTypeDTO.ValidateAll();

            if (!wireTransferTypeDTO.HasErrors)
            {
                var result = await _channelService.AddWireTransferTypeAsync(wireTransferTypeDTO, GetServiceHeader());

                await _channelService.UpdateCommissionsByWireTransferTypeIdAsync(result.Id, selectedRows, GetServiceHeader());

                TempData["Create"] = "Successfully Created Wire Transfer Type";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = wireTransferTypeDTO.ErrorMessages;

                ViewBag.transactionOwnership = GetTransactionOwnershipSelectList(wireTransferTypeDTO.TransactionOwnershipDescription.ToString());

                TempData["CreateError"] = "Failed to Create Wire Transfer Type";

                return View(wireTransferTypeDTO);
            }
        }
    }

}
