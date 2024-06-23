using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
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
        public async Task<ActionResult> Add(Guid? id, BudgetDTO budgetDTO)
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

                Session["chargeSplit"] = budgetDTO.BudgetEntries;


                if (chargeSplitDTO.Amount > budgetDTO.TotalValue)
                {
                    TempData["tPercentage"] = "Amount cannot exceed the total value. The last added Entry has been removed.";

                    budgetEntryDTOs.Remove(chargeSplitDTO);

                    Session["chargeSplit"] = budgetEntryDTOs;
                }
                else if (chargeSplitDTO.Amount <= budgetDTO.TotalValue)
                {
                    TempData["tPercentage"] = "Amount must be Equal to Total Amount.";
                    budgetEntryDTOs.Add(chargeSplitDTO);
                }

            };


            TempData["BudgetEntryDTO"] = budgetEntryDTOs;

            TempData["BudgetDTO"] = budgetDTO;

            ViewBag.budgetEntryDTOs = budgetEntryDTOs;

            return View("Create", budgetDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Remove(Guid id, BudgetDTO budgetDTO)
        {
            await ServeNavigationMenus();
            await _channelService.FindBudgetAsync(id, GetServiceHeader());

            budgetDTO = TempData["BudgetDTO"] as BudgetDTO;

            budgetEntryDTOs = TempData["BudgetEntryDTO2"] as ObservableCollection<BudgetEntryDTO>;

            if (budgetEntryDTOs == null)

                budgetEntryDTOs = new ObservableCollection<BudgetEntryDTO>();

            foreach (var chargeSplitDTO in budgetEntryDTOs)
            {
                chargeSplitDTO.ChartOfAccountId = budgetDTO.Id;
                chargeSplitDTO.Type = chargeSplitDTO.Type;

                chargeSplitDTO.Amount = chargeSplitDTO.Amount;

                chargeSplitDTO.Reference = chargeSplitDTO.Reference;
                chargeSplitDTO.CreatedBy = chargeSplitDTO.CreatedBy;

                budgetEntryDTOs.Remove(chargeSplitDTO);
            };
            await _channelService.RemoveBudgetEntriesAsync(budgetEntryDTOs, GetServiceHeader());

            TempData["BudgetEntryDTO"] = budgetEntryDTOs;

            TempData["BudgetDTO"] = budgetDTO;

            ViewBag.budgetEntryDTOs = budgetEntryDTOs;

            return View("Create", budgetDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Remove2(Guid id, BudgetDTO budgetDTO)
        {

            await ServeNavigationMenus();
            var chargeId = (Guid)Session["Id"];
            budgetDTO = TempData["BudgetDTO"] as BudgetDTO;


            budgetEntryDTOs = TempData["BudgetEntryDTO2"] as ObservableCollection<BudgetEntryDTO>;

            if (budgetEntryDTOs == null)
                budgetEntryDTOs = new ObservableCollection<BudgetEntryDTO>();

            foreach (var chargeSplitDTO in budgetEntryDTOs)
            {
                chargeSplitDTO.ChartOfAccountId = budgetDTO.Id;
                chargeSplitDTO.Type = chargeSplitDTO.Type;

                chargeSplitDTO.Amount = chargeSplitDTO.Amount;

                chargeSplitDTO.Reference = chargeSplitDTO.Reference;
                chargeSplitDTO.CreatedBy = chargeSplitDTO.CreatedBy;

                budgetEntryDTOs.Remove(chargeSplitDTO);
            };
            await _channelService.RemoveBudgetEntriesAsync(budgetEntryDTOs, GetServiceHeader());

            TempData["BudgetEntryDTO"] = budgetEntryDTOs;

            TempData["BudgetDTO"] = budgetDTO;

            ViewBag.budgetEntryDTOs = budgetEntryDTOs;

            return View("Edit", budgetDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(BudgetDTO budgetDTO, ObservableCollection<BudgetEntryDTO> budgetEntries, BudgetEntryDTO budgetEntry)
        {

            ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(budgetDTO.ToString());


            budgetEntryDTOs = TempData["BudgetEntryDTO"] as ObservableCollection<BudgetEntryDTO>;
            budgetDTO = TempData["BudgetDTO"] as BudgetDTO;
            Guid ChartOfAccountId = budgetDTO.Id;
            if (!budgetDTO.HasErrors)
            {

                var budget = await _channelService.AddBudgetAsync(budgetDTO, GetServiceHeader());

                if (budget.ErrorMessageResult != null)
                {
                    TempData["ErrorMsg"] = budget.ErrorMessageResult;
                    await ServeNavigationMenus();
                    return View();
                }
                TempData["SuccessMessage"] = "Edit successful.";
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
                TempData["Failed"] = "Failed to Create .";
                return View(budgetDTO);
            }
        }
        public async Task<ActionResult> Edit(Guid id)
        {
            Session["Id"] = id;
            await ServeNavigationMenus();
            ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(string.Empty);

            bool includeBalances = true;
            var budgetDTO = await _channelService.FindBudgetAsync(id, GetServiceHeader());
            Session["budgetDTO"] = budgetDTO;


            var chargeId = (Guid)Session["Id"];
            await ServeNavigationMenus();
            var chargesplits = await _channelService.FindBudgetEntriesByBudgetIdAsync(chargeId, includeBalances, GetServiceHeader());
            ViewBag.chargesplits = chargesplits;
            TempData["BudgetDTO2"] = budgetDTO;
            ViewBag.budgetDTO2 = budgetDTO;
            TempData["BudgetEntryDTO2"] = chargesplits;

            return View(budgetDTO);
        }


        public async Task<ActionResult> Edit3(Guid? id, BudgetEntryDTO budgetDTO, BudgetDTO budgetDTO1)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var chargeId = (Guid)Session["Id"];
            await ServeNavigationMenus();
            ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(string.Empty);

            bool includeBalances = false;

            await ServeNavigationMenus();

            var BudgetDTO = await _channelService.FindBudgetAsync(chargeId, GetServiceHeader());
            var chargesplits = await _channelService.FindBudgetEntriesByBudgetIdAsync(BudgetDTO.Id, includeBalances, GetServiceHeader());

            ViewBag.chargesplits = chargesplits;
            TempData["BudgetEntryDTO2"] = chargesplits;

            // Loop through the collection to find the item with the given id
            var matchingEntry = chargesplits.FirstOrDefault(cs => cs.Id == id);

            if (matchingEntry == null)
            {
                return HttpNotFound();
            }
            else if (matchingEntry != null)
            {

                var viewModel = new BudgetEntryDTO()
                {
                    Id = matchingEntry.Id,
                    Type = matchingEntry.Type,
                    ChartOfAccountId = matchingEntry.ChartOfAccountId,
                    ChartOfAccountAccountName = matchingEntry.ChartOfAccountAccountName,
                    Amount = matchingEntry.Amount,
                    Reference = matchingEntry.Reference
                };
            }


            

            return View(matchingEntry);


        }




        [HttpPost]
        public async Task<ActionResult> Edit2(Guid? id, BudgetDTO budgetDTO)
        {
            var chargeId = (Guid)Session["Id"];
            var k = Session["budgetDTO"].ToString();


            await ServeNavigationMenus();

            bool includeBalances = true;

            var chargesplits = await _channelService.FindBudgetEntriesByBudgetIdAsync(chargeId, includeBalances, GetServiceHeader());

            ViewBag.chargesplits = chargesplits;


            chargesplits = TempData["BudgetEntryDTO"] as ObservableCollection<BudgetEntryDTO>;

            if (chargesplits == null)
                chargesplits = new ObservableCollection<BudgetEntryDTO>();

            foreach (var chargeSplitDTO in chargesplits)
            {
                chargeSplitDTO.Type = chargeSplitDTO.Type;
                chargeSplitDTO.ChartOfAccountId = chargeSplitDTO.ChartOfAccountId;
                chargeSplitDTO.Amount = chargeSplitDTO.Amount;

                chargeSplitDTO.Reference = chargeSplitDTO.Reference;
                chargeSplitDTO.CreatedBy = chargeSplitDTO.CreatedBy;




                if (chargeSplitDTO.Amount > budgetDTO.TotalValue)
                {
                    TempData["tPercentage"] = "Amount cannot exceed the total value. The last added Entry has been removed.";

                    budgetEntryDTOs.Remove(chargeSplitDTO);

                    Session["chargeSplit"] = chargesplits;
                }
                else if (chargeSplitDTO.Amount <= budgetDTO.TotalValue)
                {
                    TempData["tPercentage"] = "Amount must be Equal to Total Amount.";

                    chargesplits.Add(chargeSplitDTO);

                    ViewBag.chargesplits = chargesplits;

                }

            };

            TempData["BudgetEntryDTO2"] = budgetEntryDTOs;

            TempData["BudgetDTO2"] = budgetDTO;

            ViewBag.budgetEntryDTOs = budgetEntryDTOs;

            return View("Edit", budgetDTO);
        }







        [HttpPost]
        public async Task<ActionResult> Edit(BudgetDTO budgetDTO, ObservableCollection<BudgetEntryDTO> budgetEntries)
        {


            budgetEntryDTOs = TempData["BudgetEntryDTO2"] as ObservableCollection<BudgetEntryDTO>;

            ViewBag.BudgetEntryTypeSelectList = GetBudgetEntryTypeSelectList(budgetDTO.ToString());


            if (!budgetDTO.HasErrors)
            {

                await _channelService.UpdateBudgetAsync(budgetDTO, GetServiceHeader());


                TempData["SuccessMessage"] = "Edit successful.";


                if (budgetDTO.BudgetEntries != null)
                {

                    //Update BudgetEntries

                    budgetEntries = budgetDTO.BudgetEntries;

                    //foreach (var budgetEntryDTO in budgetDTO.BudgetEntries)
                    //{
                    //    budgetEntryDTO.BudgetId = budgetDTO.Id;

                    //    //budgetEntryDTO.ChartOfAccountId = budget.BudgetEntries[0].ChartOfAccountId;
                    //    //budgetEntryDTO.LoanProductId = budget.BudgetEntries[4].LoanProductId;

                    //    budgetEntries.Add(budgetEntryDTO);
                    //}

                    await _channelService.UpdateBudgetEntriesByBudgetIdAsync(budgetDTO.Id, budgetEntries, GetServiceHeader());
                }
                TempData["BudgetDTO"] = "";
                TempData["BudgetEntryDTO"] = "";
                TempData["SuccessMessage"] = "Edit successful.";
                return RedirectToAction("Index");
            }
            else
            {

                TempData["ErrorMsg"] = "Failed to Create .";
                return View(budgetDTO);
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