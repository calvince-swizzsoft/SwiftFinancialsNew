using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BudgetController : MasterController
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

            var pageCollectionInfo = await _channelService.FindBudgetsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(budgets => budgets.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<BudgetDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var BudgetDTO = await _channelService.FindBudgetAsync(id, GetServiceHeader());

            return View(BudgetDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(BudgetDTO budgetDTO, ObservableCollection<BudgetEntryDTO> budgetEntries)
        {
            budgetDTO.ValidateAll();

            if (!budgetDTO.ErrorMessages.Any())
            {

                var budget = await _channelService.AddBudgetAsync(budgetDTO.MapTo<BudgetDTO>(), GetServiceHeader());
                if (budget.ErrorMessageResult != null)
                {
                    TempData["ErrorMsg"] = budget.ErrorMessageResult;
                    await ServeNavigationMenus();
                    return View();
                }
                TempData["SuccessMessage"] = "Create successful.";
                if (budget != null)
                {
                    //Update BudgetEntries


                    foreach (var budgetEntryDTO in budgetDTO.BudgetEntries)
                    {
                        budgetEntryDTO.BudgetId = budget.Id;
                        //budgetEntryDTO.ChartOfAccountId = budget.BudgetEntries[0].ChartOfAccountId;
                        //budgetEntryDTO.LoanProductId = budget.BudgetEntries[4].LoanProductId;

                        budgetEntries.Add(budgetEntryDTO);
                    }

                    await _channelService.UpdateBudgetEntriesByBudgetIdAsync(budget.Id, budgetEntries, GetServiceHeader());
                } 
                ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(budgetDTO.BudgetEntries[0].Type.ToString());

                TempData["SuccessMessage"] = "Create successful.";
                return RedirectToAction("Index");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);

                TempData["Error"] = string.Join(",", allErrors);

                return View(budgetDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            bool includeBalances = false;
            var BudgetDTO = await _channelService.FindBudgetAsync(id, GetServiceHeader());
            await _channelService.FindBudgetEntriesByBudgetIdAsync(BudgetDTO.Id, includeBalances, GetServiceHeader());
            ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(string.Empty);

            return View(BudgetDTO);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, BudgetDTO BudgetBindingModel, ObservableCollection<BudgetEntryDTO> budgetEntries)
        {
            if (BudgetBindingModel != null)
            {
                await _channelService.UpdateBudgetAsync(BudgetBindingModel, GetServiceHeader());
                ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(BudgetBindingModel.BudgetEntries[0].Type.ToString());

                if (BudgetBindingModel != null)
                {
                    //Update BudgetEntries


                    foreach (var budgetEntryDTO in BudgetBindingModel.BudgetEntries)
                    {
                        budgetEntryDTO.BudgetId = BudgetBindingModel.Id;
                        budgetEntryDTO.ChartOfAccountId = BudgetBindingModel.BudgetEntries[0].ChartOfAccountId;
                        //budgetEntryDTO.LoanProductId = budget.BudgetEntries[4].LoanProductId;

                        budgetEntries.Add(budgetEntryDTO);
                    }
                    await _channelService.UpdateBudgetEntriesByBudgetIdAsync(BudgetBindingModel.Id, budgetEntries, GetServiceHeader());
                    TempData["SuccessMessage"] = "Edit successful.";
                    return RedirectToAction("Index");
                }
                else
                {
                    return View(BudgetBindingModel);
                }

               
            }
            return View(BudgetBindingModel);
        }

            [HttpGet]
            public async Task<JsonResult> GetBudgetsAsync()
            {
                var budgetsDTOs = await _channelService.FindBudgetsAsync(GetServiceHeader());

                return Json(budgetsDTOs, JsonRequestBehavior.AllowGet);
            }
        }
    }