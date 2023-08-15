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
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.ExpensePayableEntries = null;


            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Add(ExpensePayableDTO expensePayableDTO)
        {
            await ServeNavigationMenus();

            ExpensePayableEntryDTOs = TempData["ExpensePayableEntryDTO"] as ObservableCollection<ExpensePayableEntryDTO>;
          
            if (ExpensePayableEntryDTOs == null)
                ExpensePayableEntryDTOs = new ObservableCollection<ExpensePayableEntryDTO>();

            foreach (var expensePayableEntryDTO in expensePayableDTO.ExpensePayableEntries)
            {

                expensePayableEntryDTO.ChartOfAccountId = expensePayableDTO.Id;
                expensePayableEntryDTO.BranchId = expensePayableDTO.BranchId;
                expensePayableEntryDTO.TotalValue = expensePayableEntryDTO.TotalValue;
                expensePayableEntryDTO.PrimaryDescription = expensePayableEntryDTO.PrimaryDescription;
                expensePayableEntryDTO.SecondaryDescription = expensePayableEntryDTO.SecondaryDescription;
                expensePayableEntryDTO.Reference = expensePayableEntryDTO.Reference;
                ExpensePayableEntryDTOs.Add(expensePayableEntryDTO);
            };

            TempData["ExpensePayableEntryDTO"] = ExpensePayableEntryDTOs;

            TempData["ExpensePayableDTO"] = expensePayableDTO;

            ViewBag.ExpensePayableEntryDTOs = ExpensePayableEntryDTOs;

            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayableDTO.Type.ToString());
            return View("Create", expensePayableDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(ExpensePayableDTO expensePayableDTO)
        {

            Guid expensePayableEntryChartOfAccountId = expensePayableDTO.Id;
            Guid expensePayableEntryBranchId = expensePayableDTO.Id;

            expensePayableDTO.ValidateAll();

            if (!expensePayableDTO.HasErrors)
            {

                var expensePayable = await _channelService.AddExpensePayableAsync(expensePayableDTO, GetServiceHeader());

                if (expensePayable != null)
                {
                    var expensePayableEntries = new ObservableCollection<ExpensePayableEntryDTO>();


                    foreach (var expensePayableEntryDTO in expensePayableDTO.ExpensePayableEntries)
                    {
                        expensePayableEntryDTO.ExpensePayableId = expensePayable.Id;
                        expensePayableEntryDTO.ChartOfAccountId = expensePayableEntryChartOfAccountId;
                        expensePayableEntryDTO.BranchId = expensePayableEntryBranchId;
                        expensePayableEntryDTO.TotalValue = expensePayableEntryDTO.TotalValue;
                        expensePayableEntryDTO.PrimaryDescription = expensePayableEntryDTO.PrimaryDescription;
                        expensePayableEntryDTO.SecondaryDescription = expensePayableEntryDTO.SecondaryDescription;
                        expensePayableEntryDTO.Reference = expensePayableEntryDTO.Reference;

                        expensePayableEntries.Add(expensePayableEntryDTO);
                    };

                    if (expensePayableEntries.Any())
                        await _channelService.UpdateExpensePayableEntriesByExpensePayableIdAsync(expensePayable.Id, expensePayableEntries, GetServiceHeader());
                }
                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayableDTO.Type.ToString());
                ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(expensePayableDTO.Type.ToString());

                ViewBag.ExpensePayableEntries = await _channelService.FindExpensePayableEntriesByExpensePayableIdAsync(expensePayable.Id, GetServiceHeader());

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