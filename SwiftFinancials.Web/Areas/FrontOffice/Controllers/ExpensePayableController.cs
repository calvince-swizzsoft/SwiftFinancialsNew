using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;


namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class ExpensePayableController : MasterController
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

            var pageCollectionInfo = await _channelService.FindExpensePayablesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<ExpensePayableDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var expensePayableDTO = await _channelService.FindExpensePayableAsync(id, GetServiceHeader());

            return View(expensePayableDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            ViewBag.JournalVoucherTypeSelectList =GetJournalVoucherTypeSelectList(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.ExpensePayableEntries = null;

            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Add(ExpensePayableDTO expensePayable)
        {
            await ServeNavigationMenus();

            ExpensePayableEntries = TempData["ExpensePayableEntryDTO"] as ObservableCollection<ExpensePayableEntryDTO>;
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(expensePayable.Type.ToString());
            if (ExpensePayableEntries == null)
                ExpensePayableEntries = new ObservableCollection<ExpensePayableEntryDTO>();

            foreach (var ExpensePayableEntry in expensePayable.ExpensePayableEntries)
            {
                ExpensePayableEntry.PrimaryDescription = ExpensePayableEntry.PrimaryDescription;
                ExpensePayableEntry.BranchId = ExpensePayableEntry.BranchId;//Temporary 
                ExpensePayableEntry.CreatedBy = ExpensePayableEntry.CreatedBy;
                ExpensePayableEntries.Add(ExpensePayableEntry);
            };

            TempData["ExpensePayableEntries"] = ExpensePayableEntries;

            TempData["ExpensePayableDTO"] = expensePayable;

            ViewBag.ExpensePayableEntries = ExpensePayableEntries;

            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(expensePayable.Type.ToString());
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayable.Type.ToString());
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(expensePayable.Type.ToString());

            return View("Create", expensePayable);
        }


        [HttpPost]
        public async Task<ActionResult> Create(ExpensePayableDTO expensePayableDTO)
        {

            Guid id = expensePayableDTO.Id;

            expensePayableDTO.ValidateAll();

            if (!expensePayableDTO.HasErrors)
            {

                var journalVoucher = await _channelService.AddExpensePayableAsync(expensePayableDTO, GetServiceHeader());

                if (journalVoucher != null)
                {
                  var   ExpensePayableEntries = new ObservableCollection<ExpensePayableEntryDTO>();


                    foreach (var expensePayableEntryDTO in expensePayableDTO.ExpensePayableEntries)
                    {
                        expensePayableEntryDTO.BranchId = journalVoucher.BranchId;
                        expensePayableEntryDTO.BranchDescription = expensePayableEntryDTO.BranchDescription;
                        expensePayableEntryDTO.ChartOfAccountId = id;
                        expensePayableEntryDTO.PrimaryDescription = expensePayableEntryDTO.PrimaryDescription;
                        expensePayableEntryDTO.SecondaryDescription = expensePayableEntryDTO.SecondaryDescription;
                        expensePayableEntryDTO.Reference = expensePayableEntryDTO.Reference;
                        ExpensePayableEntries.Add(expensePayableEntryDTO);
                    };

                    if (ExpensePayableEntries.Any())
                        await _channelService.UpdateExpensePayableEntryCollectionAsync(expensePayableDTO.Id, ExpensePayableEntries, GetServiceHeader());
                } 
                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayableDTO.Type.ToString());
                ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(expensePayableDTO.Type.ToString());

                ViewBag.JournalVoucherEntries = await _channelService.FindJournalVoucherEntriesByJournalVoucherIdAsync(journalVoucher.Id, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = expensePayableDTO.ErrorMessages;

                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayableDTO.Type.ToString());
                ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(expensePayableDTO.Type.ToString());

                return View(expensePayableDTO);
            }
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var expensePayableDTO = await _channelService.FindExpensePayableAsync(id, GetServiceHeader());

            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, ExpensePayableDTO expensePayableDTO)
        {
            
            expensePayableDTO.ValidateAll();

            if (!expensePayableDTO.HasErrors)
            {
                await _channelService.UpdateExpensePayableAsync(expensePayableDTO, GetServiceHeader());

                ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(expensePayableDTO.Type.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = expensePayableDTO.ErrorMessages;               

                return View(expensePayableDTO);
            }
        }
                                                                /////////////verify expense payable/////    
        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);
            var expensePayableDTO = await _channelService.FindExpensePayableAsync(id, GetServiceHeader());

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, ExpensePayableDTO expensePayableDTO)
        {
            var expensePayableAuthOption = expensePayableDTO.ExpensePayableAuthOption;
       
           if (!expensePayableDTO.HasErrors)
            {
               
                await _channelService.AuditExpensePayableAsync(expensePayableDTO, expensePayableAuthOption, GetServiceHeader());

                ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(expensePayableDTO.Type.ToString());
                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayableDTO.Type.ToString());
                return RedirectToAction("Index");
    }
            else
            {
                var errorMessages = expensePayableDTO.ErrorMessages;
               
                return View(expensePayableDTO);
}
        }
                                                            /////////////Approve expense payable/////    
        public async Task<ActionResult> Approve(Guid id)
        {
            await ServeNavigationMenus();

            var expensePayableDTO = await _channelService.FindExpensePayableAsync(id, GetServiceHeader());

            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approve(Guid id, ExpensePayableDTO expensePayableDTO)
        {

            expensePayableDTO.ValidateAll();

            if (!expensePayableDTO.HasErrors)
            {
                var expensePayableAuthOption = expensePayableDTO.ExpensePayableAuthOption;

                var moduleNavigationItemCode = expensePayableDTO.ModuleNavigationItemCode;

                await _channelService.AuthorizeExpensePayableAsync(expensePayableDTO, expensePayableAuthOption, moduleNavigationItemCode, GetServiceHeader());

                await _channelService.UpdateExpensePayableAsync(expensePayableDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = expensePayableDTO.ErrorMessages;

                ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(expensePayableDTO.Type.ToString());

                return View();
            }
        }

                                                    /////////////Cancel expense payable/////    
        public async Task<ActionResult> Cancel(Guid id)
        {
            await ServeNavigationMenus();

            var expensePayableDTO = await _channelService.FindExpensePayableAsync(id, GetServiceHeader());

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Cancel(Guid id, ExpensePayableDTO expensePayableDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateExpensePayableAsync(expensePayableDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(expensePayableDTO);
            }
        }

        //[HttpGet]
        //public async Task<JsonResult> GetExpensePayablesAsync()
        //{
        //    var expensePayableDTOs = await _channelService.FindExpensePayablesAsync(GetServiceHeader());

        //    return Json(expensePayableDTOs, JsonRequestBehavior.AllowGet);
        //}
    }
}