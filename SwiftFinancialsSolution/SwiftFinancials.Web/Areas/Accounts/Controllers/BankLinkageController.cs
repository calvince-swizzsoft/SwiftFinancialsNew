

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
    public class BankLinkageController : MasterController
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

            var pageCollectionInfo = await _channelService.FindBankLinkagesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<BankLinkageDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var bankLinkageDTO = await _channelService.FindBankLinkageAsync(id, GetServiceHeader());

            return View(bankLinkageDTO);
        }
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(BankLinkageDTO bankLinkageDTO)
        {
            bankLinkageDTO.ValidateAll();

            if (!bankLinkageDTO.HasErrors)
            {
                await _channelService.AddBankLinkageAsync(bankLinkageDTO, GetServiceHeader());
                TempData["SuccessMessage"] = "Create successful.";
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = bankLinkageDTO.ErrorMessages;

                return View(bankLinkageDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var bankLinkageDTO = await _channelService.FindBankLinkageAsync(id, GetServiceHeader());

            return View(bankLinkageDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, BankLinkageDTO bankLinkageBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateBankLinkageAsync(bankLinkageBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(bankLinkageBindingModel);
            }
        }

        /*[HttpGet]
        public async Task<JsonResult> GetBankLinkagesAsync()
        {
            var bankLinkageDTOs = await _channelService.FindBankLinkagesAsync(GetServiceHeader());

            return Json(bankLinkageDTOs, JsonRequestBehavior.AllowGet);
        }*/
    }
}

