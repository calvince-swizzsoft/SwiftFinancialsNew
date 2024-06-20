

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
    public class BankReconciliation_PeriodsController : MasterController
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

            var pageCollectionInfo = await _channelService.FindBankReconciliationPeriodsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(bankLinkage => bankLinkage.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<BankReconciliationPeriodDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var bankReconciliationPeriodDTO = await _channelService.FindBankReconciliationPeriodAsync(id, GetServiceHeader());

            return View(bankReconciliationPeriodDTO);
        }


        public async Task<ActionResult> Create(Guid? id, BankReconciliationPeriodDTO bankReconciliationPeriodDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            return View(bankReconciliationPeriodDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(BankReconciliationPeriodDTO bankReconciliationPeriodDTO)
        {
            bankReconciliationPeriodDTO.ValidateAll();

            if (!bankReconciliationPeriodDTO.HasErrors)
            {
                await _channelService.AddBankReconciliationPeriodAsync(bankReconciliationPeriodDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Bank Reconciliation Period Created Successfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = bankReconciliationPeriodDTO.ErrorMessages;

                TempData["Error"] = "Failed to create Bank Linkage";

                return View(bankReconciliationPeriodDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var bankReconciliationPeriodDTO = await _channelService.FindBankReconciliationPeriodAsync(id, GetServiceHeader());

            return View(bankReconciliationPeriodDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid? id, BankReconciliationPeriodDTO bankReconciliationPeriodDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateBankReconciliationPeriodAsync(bankReconciliationPeriodDTO, GetServiceHeader());

                TempData["Edit"] = "Successfully edited Bank Reconciliation Period";

                return RedirectToAction("Index");
            }
            else
            {
                return View(bankReconciliationPeriodDTO);
            }
        }
    }
}

