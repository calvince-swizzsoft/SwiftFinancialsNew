using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Domain.MainBoundedContext.AdministrationModule.Aggregates.BankBranchAgg;
using Newtonsoft.Json;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            var pageCollectionInfo = await _channelService.FindBanksByFilterInPageAsync
                (jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(b => b.CreatedDate)
                    .ToList();

                totalRecordCount = sortedData.Count;

                var paginatedData = sortedData
                    .Skip(jQueryDataTablesModel.iDisplayStart)
                    .Take(jQueryDataTablesModel.iDisplayLength)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? sortedData.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: paginatedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }

            return this.DataTablesJson(
                items: new List<BankDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }



        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var BankDTO = await _channelService.FindBankAsync(id, GetServiceHeader());
            await _channelService.FindBankBranchesByBankIdAsync(BankDTO.Id, GetServiceHeader());

            return View(BankDTO);
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Create(BankDTO bank)
        {
            bank.ValidateAll();

            if (!bank.HasErrors)
            {
                var bankDTO = await _channelService.AddBankAsync(bank, GetServiceHeader());
                BankBranchDTO j = new BankBranchDTO();
                j.Description = bank.Description;
                bankBranches.Add(j);
                await _channelService.UpdateBankBranchesByBankIdAsync(bankDTO.Id, bankBranches, GetServiceHeader());

                return RedirectToAction("Index");
            }
            return View(bank);
        }



        [HttpPost]
        public JsonResult Remove(Guid id, BankDTO bank)
        {
            foreach (var branch in bank.BankBranche)
            {
                bank.Description = branch.Description;

            }
            return Json(new { success = true, data = JournalVoucherEntryDTOs });

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
    }
}
