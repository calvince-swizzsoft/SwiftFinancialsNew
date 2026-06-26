using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BudgetEntryController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel,BudgetEntryDTO budgetEntryDTO)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindBudgetsByFilterInPageAsync(jQueryDataTablesModel.sSearch,jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<BudgetEntryDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var BudgetEntryDTO = await _channelService.FindBudgetAsync(id, GetServiceHeader());

            return View(BudgetEntryDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(BudgetEntryDTO budgetEntryDTO)
        {
            budgetEntryDTO.ValidateAll();

            if (!budgetEntryDTO.HasErrors)
            {
                await _channelService.AddBudgetEntryAsync(budgetEntryDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = budgetEntryDTO.ErrorMessages;

                return View(budgetEntryDTO);
            }
        }

       /* public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var budgetEntryDTO = await _channelService.FindBudgetEntryAsync(id, GetServiceHeader());

            return View(budgetEntryDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, BudgetEntryDTO budgetEntryBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateBudgetEntryAsync(budgetEntryBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(budgetEntryBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetBudgetEntrysAsync()
        {
            var budgetEntrysDTOs = await _channelService.FindBudgetEntriesAsync(GetServiceHeader());

            return Json(budgetEntrysDTOs, JsonRequestBehavior.AllowGet);
        }*/
    }
}