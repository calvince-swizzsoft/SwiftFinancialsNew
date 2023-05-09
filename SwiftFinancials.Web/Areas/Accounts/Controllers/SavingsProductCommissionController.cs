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
  /*  public class SavingsProductCommissionController : MasterController
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

            var pageCollectionInfo = await _channelService.FindSavingsProductCommissionsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<SavingsProductCommissionDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var savingsProductCommissionDTO = await _channelService.FindSavingsProductCommissionAsync(id, GetServiceHeader());

            return View(SavingsProductCommissionDTO);
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(SavingsProductCommissionDTO savingsProductCommissionDTO)
        {
            savingsProductCommissionDTO.ValidateAll();

            if (!savingsProductCommissionDTO.HasErrors)
            {
                await _channelService.AddSavingsProductCommissionAsync(savingsProductCommissionDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = savingsProductCommissionDTO.ErrorMessages;

                return View(savingsProductCommissionDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var savingsProductCommissionDTO = await _channelService.FindSavingsProductCommissionAsync(id, GetServiceHeader());

            return View(savingsProductCommissionDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, SavingsProductCommissionDTO savingsProductCommissionBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateSavingsProductCommissionAsync(savingsProductCommissionBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(savingsProductCommissionBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSavingsProductCommissionsAsync()
        {
            var savingsProductCommissionDTOs = await _channelService.FindSavingsProductCommissionsAsync(GetServiceHeader());

            return Json(savingsProductCommissionDTOs, JsonRequestBehavior.AllowGet);
        }
    }*/
}
