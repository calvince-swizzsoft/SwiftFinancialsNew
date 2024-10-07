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
    public class BranchController : MasterController
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

            var pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var pageCollectionInfo = await _channelService.FindBranchesByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<BranchDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var branchDTO = await _channelService.FindBranchAsync(id, GetServiceHeader());

            return View(branchDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(BranchDTO branchDTO)
        {
            branchDTO.ValidateAll();

            if (!branchDTO.HasErrors)
            {
                var companyDTO = await _channelService.FindCompanyAsync(branchDTO.CompanyId, GetServiceHeader());
                  branchDTO.companyDTO=companyDTO;
                var h = await _channelService.AddBranchAsync(branchDTO, GetServiceHeader());
                TempData["SuccessMessage"] = "Branch " + h.Description + " created successfully";
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = branchDTO.ErrorMessages;

                return View(branchDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var branchDTO = await _channelService.FindBranchAsync(id, GetServiceHeader());

            return View(branchDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, BranchDTO branchBindingModel)
        {
            branchBindingModel.ValidateAll();

            if (!branchBindingModel.HasErrors)
            {
                await _channelService.UpdateBranchAsync(branchBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(branchBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetBranchesAsync()
        {
            var branchesDTOs = await _channelService.FindBranchesAsync(GetServiceHeader());

            return Json(branchesDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}