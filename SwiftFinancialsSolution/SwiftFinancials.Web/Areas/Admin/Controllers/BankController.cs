using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AdministrationModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Admin.Controllers
{
    public class BankController : MasterController
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

            List<string> sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindBanksByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<BankDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var BankDTO = await _channelService.FindBankAsync(id, GetServiceHeader());

            return View(BankDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(BankDTO BankDTO)
        {
            BankDTO.ValidateAll();

            if (!BankDTO.HasErrors)
            {
                await _channelService.AddBankAsync(BankDTO, GetServiceHeader());
                TempData["SuccessMessage"] = "Bank Created successfully";
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = BankDTO.ErrorMessages;

                return View(BankDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var BankDTO = await _channelService.FindBankAsync(id, GetServiceHeader());

            return View(BankDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, BankDTO BankBindingModel)
        {
            BankBindingModel.ValidateAll();

            if (!BankBindingModel.HasErrors)
            {
                await _channelService.UpdateBankAsync(BankBindingModel, GetServiceHeader());
                TempData["SuccessMessage"] = "Bank Edited successfully";
                return RedirectToAction("Index");
            }
            else
            {
                return View(BankBindingModel);
            }
        }

        /* [HttpGet]
         public async Task<JsonResult> GetBanksAsync()
         {
             var banksDTOs = await _channelService.FindBanksAsync(GetServiceHeader());

             return Json(banksDTOs, JsonRequestBehavior.AllowGet);
         }*/
    }
}
