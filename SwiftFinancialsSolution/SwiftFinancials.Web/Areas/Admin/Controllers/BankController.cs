using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Domain.MainBoundedContext.AdministrationModule.Aggregates.BankBranchAgg;
using Microsoft.AspNet.Identity;
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
            var BankBranches = await _channelService.FindBankBranchesByBankIdAsync(BankDTO.Id, GetServiceHeader());
            ViewBag.BankBranches = BankBranches;

            return View(BankDTO);
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            return View();
        }


        [HttpPost]
        public async Task<JsonResult> Add(BankDTO bankDTO)
        {
            await ServeNavigationMenus();

            var branches = Session["bankBranches"] as ObservableCollection<BankBranchDTO>;

            if (branches == null)
            {
                branches = new ObservableCollection<BankBranchDTO>();
            }

            foreach (var branch in bankDTO.BankBranchesDTO)
            {
                var existingEntry = branches.FirstOrDefault(e => e.Description == branch.Description);

                if (existingEntry != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "The Selected Branch has already been added to the Branches List."
                    });
                }

                branches.Add(branch);
            }

            Session["bankBranches"] = branches;

            return Json(new { success = true, entries = branches });
        }


        [HttpPost]
        public async Task<JsonResult> Remove(Guid id)
        {
            await ServeNavigationMenus();

            var branches = Session["bankBranches"] as ObservableCollection<BankBranchDTO>;

            if (branches != null)
            {
                var entryToRemove = branches.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    branches.Remove(entryToRemove);

                    Session["bankBranches"] = branches;
                }
            }

            return Json(new { success = true, data = branches });
        }


        [HttpPost]
        public async Task<ActionResult> Create(BankDTO bank)
        {
            if(Session["bankBranches"] == null)
            {
                await ServeNavigationMenus();
                TempData["EBB"] = "Empty Bank Branches";
                return View(bank);
            }

            bank.ValidateAll();

            if (!bank.HasErrors)
            {
                var bankDTO = await _channelService.AddBankAsync(bank, GetServiceHeader());

                var bankBranches = Session["bankBranches"] as ObservableCollection<BankBranchDTO>;

                if (bankBranches != null)
                    await _channelService.UpdateBankBranchesByBankIdAsync(bankDTO.Id, bankBranches, GetServiceHeader());

                TempData["Success"] = "Ok";

                return RedirectToAction("Index");
            }

            TempData["Failed"] = "Fail!";
            return View(bank);
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
