

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
    public class UnPayReasonController : MasterController
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

            var pageCollectionInfo = await _channelService.FindUnPayReasonsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<UnPayReasonDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var unPayReasonDTO = await _channelService.FindUnPayReasonAsync(id, GetServiceHeader());

            return View(unPayReasonDTO);
        }


        public async Task<ActionResult> UnpayReason(UnPayReasonDTO unpayReasonDTO)
        {
            Session["Description"] = unpayReasonDTO.Description;
            Session["Code"] = unpayReasonDTO.Code;
            Session["isLocked"] = unpayReasonDTO.IsLocked;

            return View("Create", unpayReasonDTO);
        }



        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }




        [HttpPost]
        public async Task<ActionResult> Create(UnPayReasonDTO unPayReasonDTO, ObservableCollection<CommissionDTO> selectedRows)
        {
            unPayReasonDTO.Description = Session["Description"].ToString();
            unPayReasonDTO.Code = Convert.ToInt32(Session["Code"].ToString());
            unPayReasonDTO.IsLocked = (bool)Session["isLocked"];

            unPayReasonDTO.ValidateAll();
            
            if (!unPayReasonDTO.HasErrors)
            {
                var result = await _channelService.AddUnPayReasonAsync(unPayReasonDTO, GetServiceHeader());

                await _channelService.UpdateCommissionsByUnPayReasonIdAsync(result.Id, selectedRows, GetServiceHeader());

                TempData["Create"] = "Unpay Reason Created Successfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = unPayReasonDTO.ErrorMessages;

                TempData["CreateError"] = "Failed to Create Unpay Reason";

                return View(unPayReasonDTO);
            }
        }



        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var unPayReasonDTO = await _channelService.FindUnPayReasonAsync(id, GetServiceHeader());

            return View(unPayReasonDTO);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, UnPayReasonDTO unPayReasonBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateUnPayReasonAsync(unPayReasonBindingModel, GetServiceHeader());

                TempData["Edit"] = "Unpay Reason Edited Successfully";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["EditError"] = "Failed to Edit Unpay Reason";

                return View(unPayReasonBindingModel);
            }
        }

      
    }
}

