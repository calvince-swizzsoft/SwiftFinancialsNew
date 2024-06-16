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

        public async Task<ActionResult> Details(Guid id, BudgetDTO budgetDTO)
        {
            await ServeNavigationMenus();

            var BudgetDTO = await _channelService.FindBudgetAsync(id, GetServiceHeader());
            var k = await _channelService.FindBudgetEntriesByBudgetIdAsync(BudgetDTO.Id, true, GetServiceHeader());
            ViewBag.budgetEntryDTOs = k;

            budgetEntryDTOs = TempData["BudgetEntryDTO"] as ObservableCollection<BudgetEntryDTO>;



            return View(BudgetDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(string.Empty);
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Add(BudgetDTO budgetDTO)
        {
            await ServeNavigationMenus();
            ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(budgetDTO.BudgetEntries[0].Type.ToString());

            budgetEntryDTOs = TempData["BudgetEntryDTO"] as ObservableCollection<BudgetEntryDTO>;

            if (budgetEntryDTOs == null)
                budgetEntryDTOs = new ObservableCollection<BudgetEntryDTO>();

            foreach (var chargeSplitDTO in budgetDTO.BudgetEntries)
            {
                chargeSplitDTO.Type = chargeSplitDTO.Type;
                chargeSplitDTO.ChartOfAccountId = chargeSplitDTO.ChartOfAccountId;
                chargeSplitDTO.Amount = chargeSplitDTO.Amount;

                chargeSplitDTO.Reference = chargeSplitDTO.Reference;
                chargeSplitDTO.CreatedBy = chargeSplitDTO.CreatedBy;

                budgetEntryDTOs.Add(chargeSplitDTO);
            };


            TempData["BudgetEntryDTO"] = budgetEntryDTOs;

            TempData["BudgetDTO"] = budgetDTO;

            ViewBag.budgetEntryDTOs = budgetEntryDTOs;

            return View("Create", budgetDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Edit2(BudgetDTO budgetDTO)
        {
            await ServeNavigationMenus();
            ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(budgetDTO.BudgetEntries[0].Type.ToString());

            budgetEntryDTOs = TempData["BudgetEntryDTO"] as ObservableCollection<BudgetEntryDTO>;

            if (budgetEntryDTOs == null)
                budgetEntryDTOs = new ObservableCollection<BudgetEntryDTO>();

            foreach (var chargeSplitDTO in budgetDTO.BudgetEntries)
            {
                chargeSplitDTO.Type = chargeSplitDTO.Type;
                chargeSplitDTO.ChartOfAccountId = chargeSplitDTO.ChartOfAccountId;
                chargeSplitDTO.Amount = chargeSplitDTO.Amount;

                chargeSplitDTO.Reference = chargeSplitDTO.Reference;
                chargeSplitDTO.CreatedBy = chargeSplitDTO.CreatedBy;

                budgetEntryDTOs.Add(chargeSplitDTO);
            };


            TempData["BudgetEntryDTO"] = budgetEntryDTOs;

            TempData["BudgetDTO"] = budgetDTO;

            ViewBag.budgetEntryDTOs = budgetEntryDTOs;

            return View("Edit", budgetDTO);
        }
        [HttpPost]
        public async Task<ActionResult> Remove(BudgetDTO budgetDTO)
        {
            await ServeNavigationMenus();
            budgetDTO = TempData["BudgetDTO"] as BudgetDTO;
            budgetEntryDTOs = TempData["BudgetEntryDTO"] as ObservableCollection<BudgetEntryDTO>;

            if (budgetEntryDTOs == null)
                budgetEntryDTOs = new ObservableCollection<BudgetEntryDTO>();

            foreach (var chargeSplitDTO in budgetDTO.BudgetEntries)
            {
                chargeSplitDTO.ChartOfAccountId = budgetDTO.Id;
                chargeSplitDTO.Type = chargeSplitDTO.Type;

                chargeSplitDTO.Amount = chargeSplitDTO.Amount;

                chargeSplitDTO.Reference = chargeSplitDTO.Reference;
                chargeSplitDTO.CreatedBy = chargeSplitDTO.CreatedBy;

                budgetEntryDTOs.Remove(chargeSplitDTO);
            };


            TempData["BudgetEntryDTO"] = budgetEntryDTOs;

            TempData["BudgetDTO"] = budgetDTO;

            ViewBag.budgetEntryDTOs = budgetEntryDTOs;

            return View("Create", budgetDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(BudgetDTO budgetDTO, ObservableCollection<BudgetEntryDTO> budgetEntries, BudgetEntryDTO budgetEntry)
        {

            ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(budgetDTO.ToString());


            budgetEntryDTOs = TempData["BudgetEntryDTO"] as ObservableCollection<BudgetEntryDTO>;
            budgetDTO = TempData["BudgetDTO"] as BudgetDTO;
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
                if (budgetEntryDTOs != null)
                {

                    //Update BudgetEntries
                    budgetEntries = budgetEntryDTOs;

                    foreach (var budgetEntryDTO in budgetDTO.BudgetEntries)
                    {
                        budgetEntryDTO.BudgetId = budget.Id;

                        //budgetEntryDTO.ChartOfAccountId = budget.BudgetEntries[0].ChartOfAccountId;
                        //budgetEntryDTO.LoanProductId = budget.BudgetEntries[4].LoanProductId;

                        budgetEntries.Add(budgetEntryDTO);
                    }

                    await _channelService.UpdateBudgetEntriesByBudgetIdAsync(budget.Id, budgetEntryDTOs, GetServiceHeader());
                }
                TempData["BudgetDTO"] = "";
                TempData["BudgetEntryDTO"] = "";
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
            ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(string.Empty);

            bool includeBalances = false;
            var BudgetDTO = await _channelService.FindBudgetAsync(id, GetServiceHeader());
            var k = await _channelService.FindBudgetEntriesByBudgetIdAsync(BudgetDTO.Id, includeBalances, GetServiceHeader());
            ViewBag.budgetEntryDTOs = k;

            budgetEntryDTOs = TempData["BudgetEntryDTO"] as ObservableCollection<BudgetEntryDTO>;
            TempData["BudgetDTO"] = BudgetDTO;
            ViewBag.john = BudgetDTO;
            return View(BudgetDTO.MapTo<BudgetDTO>());
        }

        [HttpPost]
        public async Task<ActionResult> Edit(Guid id, BudgetDTO budgetDTO)
        {
            budgetEntryDTOs = TempData["BudgetEntryDTO"] as ObservableCollection<BudgetEntryDTO>;
          
            ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(budgetDTO.ToString());
            
            var BudgetDTO = await _channelService.FindBudgetAsync(id, GetServiceHeader());
                   

            BudgetDTO.ValidateAll();
            if (!BudgetDTO.ErrorMessages.Any())
            {               

                var budget = await _channelService.UpdateBudgetAsync(BudgetDTO, GetServiceHeader());

                if (budgetEntryDTOs != null)
                {

                    //Update BudgetEntries
                    var entries = new ObservableCollection<BudgetEntryDTO>();

                    entries = budgetEntryDTOs;

                    await _channelService.UpdateBudgetEntriesByBudgetIdAsync(budgetDTO.Id, budgetEntryDTOs, GetServiceHeader());
                }
                TempData["BudgetDTO"] = "";
                TempData["BudgetEntryDTO"] = "";
                TempData["SuccessMessage"] = "Edit successful.";
                return RedirectToAction("");
            }
            else
            {
                return RedirectToAction("");
                
            }


        }




        [HttpGet]
        public async Task<JsonResult> GetBudgetsAsync()
        {
            var budgetsDTOs = await _channelService.FindBudgetsAsync(GetServiceHeader());

            return Json(budgetsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}