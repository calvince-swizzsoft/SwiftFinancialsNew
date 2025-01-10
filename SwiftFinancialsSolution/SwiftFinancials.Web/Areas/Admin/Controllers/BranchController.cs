
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

            var pageCollectionInfo = await _channelService.FindBranchesByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(B => B.CreatedDate)
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
                items: new List<BranchDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
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
                branchDTO.companyDTO = companyDTO;
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
        public async Task<ActionResult> Edit(Guid id, BranchDTO branchBindingModel)
        {
            branchBindingModel.ValidateAll();

            if (!branchBindingModel.HasErrors)
            {
                await _channelService.UpdateBranchAsync(branchBindingModel, GetServiceHeader());
                TempData["SuccessMessage"] = "Branch " + branchBindingModel.Description + " edited successfully";
                await ServeNavigationMenus();
                return RedirectToAction("Index"); // Redirect to avoid reposting on refresh
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