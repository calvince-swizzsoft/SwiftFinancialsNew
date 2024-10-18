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
        public JsonResult Add(BankDTO bank,string branchdetails)
        {

            foreach (var branch in bank.BankBranches)
            {
                bank.Description = branch.Description;
            }
            return Json(new { success = true, data = JournalVoucherEntryDTOs });

        }
        [HttpPost]
        public JsonResult Remove(Guid id, BankDTO bank)
        {
            foreach (var branch in bank.BankBranches)
            {
                bank.Description = branch.Description;

            }
            return Json(new { success = true, data = JournalVoucherEntryDTOs });

        }
        [HttpPost]
        public async Task<ActionResult> Create(BankDTO BankDTO, string branchDetails)
        {
            BankDTO.ValidateAll();
            if (!BankDTO.HasErrors)
            {
                var bankBranches = JsonConvert.DeserializeObject<List<BankBranchDTO>>(branchDetails);
                BankDTO.BankBranches = new ObservableCollection<BankBranchDTO>(bankBranches);

                var bankDTO = await _channelService.AddBankAsync(BankDTO, GetServiceHeader());
                await _channelService.UpdateBankBranchesByBankIdAsync(bankDTO.Id, BankDTO.BankBranches,GetServiceHeader());
                
                return RedirectToAction("Index");
            }
            return View(BankDTO);
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
