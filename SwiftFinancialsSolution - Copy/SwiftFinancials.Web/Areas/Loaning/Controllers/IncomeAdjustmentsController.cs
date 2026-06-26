using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class IncomeAdjustmentsController : MasterController
    {
        // GET: Loaning/LoanRequest
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

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "desc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindIncomeAdjustmentsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.Where(item => !item.IsLocked).ToList();

                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(r => r.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else
            {
                return this.DataTablesJson(items: new List<IncomeAdjustmentDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var IncomeAdjustmentDTO = await _channelService.FindIncomeAdjustmentAsync(id, GetServiceHeader());

            return View(IncomeAdjustmentDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.IncomeAdjustmentTypeSelectList = GetIncomeAdjustmentTypeSelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(IncomeAdjustmentDTO incomeAdjustment)
        {
            incomeAdjustment.ValidateAll();

            if (!incomeAdjustment.HasErrors)
            {
                var adjustments= await _channelService.AddIncomeAdjustmentAsync(incomeAdjustment, GetServiceHeader());

                ViewBag.IncomeAdjustmentTypeSelectList = GetIncomeAdjustmentTypeSelectList(incomeAdjustment.Type.ToString());

                if (adjustments.ErrorMessageResult != null)
                {
                    await ServeNavigationMenus();

                    TempData["ErrorMsg"] = adjustments.ErrorMessageResult;

                    return View();
                }

                TempData["create"] = "Successfully created Income Adjustment";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = incomeAdjustment.ErrorMessages;

                TempData["createError"] = "Failed to create Income Adjustment";

                return View("index");
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.IncomeAdjustmentTypeSelectList = GetIncomeAdjustmentTypeSelectList(string.Empty);
            var loanRequestDTO = await _channelService.FindIncomeAdjustmentAsync(id, GetServiceHeader());

            return View(loanRequestDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, IncomeAdjustmentDTO incomeAdjustmentDTO)
        {
            if (ModelState.IsValid)
            {
                ViewBag.IncomeAdjustmentTypeSelectList = GetIncomeAdjustmentTypeSelectList(incomeAdjustmentDTO.Type.ToString());

                await _channelService.UpdateIncomeAdjustmentAsync(incomeAdjustmentDTO, GetServiceHeader());

                TempData["edit"] = "Successfully edited Income Adjustment";

                return RedirectToAction("Index");
            }
            else
            {
                TempData["editError"] = "Failed to edit Income Adjustment";

                return View(incomeAdjustmentDTO);
            }
        }
    }
}

