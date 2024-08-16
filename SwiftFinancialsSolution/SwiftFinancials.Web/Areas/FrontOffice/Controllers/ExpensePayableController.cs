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


                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(expensePayable => expensePayable.CreatedDate).ToList();

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

            // Initialize ViewBag and TempData for the view
            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);

            var model = new ExpensePayableDTO
            {
                ExpensePayableEntries = new List<ExpensePayableEntryDTO>() // Initialize the list to avoid null reference
            };

            return View(model);
        }

        





        [HttpPost]
        public async Task<ActionResult> Add(Guid? id, ExpensePayableDTO expensePayableDTO)
        {
            await ServeNavigationMenus();

            ExpensePayableEntryDTOs = TempData["ExpensePayableEntryDTO"] as ObservableCollection<ExpensePayableEntryDTO>;

            if (ExpensePayableEntryDTOs == null)
                ExpensePayableEntryDTOs = new ObservableCollection<ExpensePayableEntryDTO>();

            Guid parseId;

            // Check if id is null or cannot be parsed
            if (!id.HasValue || id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return Json(new { success = false, message = "Invalid ID provided!" });
            }

            // Validation for ChartOfAccountId and other required fields
            if (expensePayableDTO.ChartOfAccountId == Guid.Empty || string.IsNullOrWhiteSpace(expensePayableDTO.ChartOfAccountName))
            {
                return Json(new { success = false, message = "The G/L Account identifier is invalid!" });
            }

            if (expensePayableDTO.BranchId == Guid.Empty || string.IsNullOrWhiteSpace(expensePayableDTO.BranchDescription))
            {
                return Json(new { success = false, message = "The Branch Name identifier is invalid!" });
            }

            foreach (var expensePayableEntryDTO in expensePayableDTO.ExpensePayableEntries)
            {
                // Assuming that these fields are mandatory, ensure proper assignment
                expensePayableEntryDTO.ChartOfAccountId = expensePayableDTO.ChartOfAccountId;
                expensePayableEntryDTO.ChartOfAccountAccountName = expensePayableEntryDTO.ChartOfAccountAccountName;
                expensePayableEntryDTO.BranchDescription = expensePayableEntryDTO.BranchDescription;
                expensePayableEntryDTO.ChartOfAccountAccountName = expensePayableEntryDTO.ChartOfAccountName;
                expensePayableEntryDTO.TotalValue = expensePayableEntryDTO.TotalValue;
                expensePayableEntryDTO.PrimaryDescription = expensePayableEntryDTO.PrimaryDescription;
                expensePayableEntryDTO.SecondaryDescription = expensePayableEntryDTO.SecondaryDescription;
                expensePayableEntryDTO.Reference = expensePayableEntryDTO.Reference;
                ExpensePayableEntryDTOs.Add(expensePayableEntryDTO);
            }

            TempData["ExpensePayableEntryDTO"] = ExpensePayableEntryDTOs;
            TempData["ExpensePayableDTO"] = expensePayableDTO;

            ViewBag.ExpensePayableEntryDTOs = ExpensePayableEntryDTOs;
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayableDTO.Type.ToString());
            ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(expensePayableDTO.Type.ToString());

            return View("Create", expensePayableDTO);
        }






        [HttpPost]
        public async Task<ActionResult> Create(ExpensePayableDTO expensePayableDTO)
        {
            if (expensePayableDTO == null)
            {
                TempData["ErrorMessage"] = "Failed to retrieve the expense payable data.";
                return RedirectToAction("Create");
            }

            // Validate the expensePayableDTO
            expensePayableDTO.ValidateAll();

            if (!expensePayableDTO.HasErrors)
            {
                try
                {
                    var expensePayable = await _channelService.AddExpensePayableAsync(expensePayableDTO, GetServiceHeader());

                    if (expensePayable != null)
                    {
                        ExpensePayableEntryDTOs = TempData["ExpensePayableEntryDTOs"] as ObservableCollection<ExpensePayableEntryDTO>;
                        var expensePayableEntries = new ObservableCollection<ExpensePayableEntryDTO>();

                        ExpensePayableEntryDTOs = expensePayableEntries;

                        if (expensePayableEntries.Any())
                        {
                            await _channelService.UpdateExpensePayableEntriesByExpensePayableIdAsync(expensePayable.Id, expensePayableEntries, GetServiceHeader());
                        }

                        // Set success message in TempData
                        TempData["SuccessMessage"] = "Expense payable created successfully!";
                        return RedirectToAction("Index"); // Redirect to Index view
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to create expense payable.";
                        return RedirectToAction("Create"); // Redirect back to Create view
                    }
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "An error occurred while processing your request.";
                    return RedirectToAction("Create"); // Redirect back to Create view
                }
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(", ", expensePayableDTO.ErrorMessages);
                return RedirectToAction("Create"); // Redirect back to Create view
            }
        }






        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var expensePayableDTO = await _channelService.FindExpensePayableAsync(id, GetServiceHeader());
            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            return View(expensePayableDTO);


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
                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(expensePayableDTO.Type.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(expensePayableDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(expensePayableDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(expensePayableDTO.Type.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(expensePayableDTO.Type.ToString());
                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayableDTO.Type.ToString());
                ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(expensePayableDTO.Type.ToString());
                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayableDTO.Type.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = expensePayableDTO.ErrorMessages;
                ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(expensePayableDTO.Type.ToString());
                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(expensePayableDTO.Type.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(expensePayableDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(expensePayableDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(expensePayableDTO.Type.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(expensePayableDTO.Type.ToString());
                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayableDTO.Type.ToString());
                ViewBag.JournalVoucherEntryTypeSelectList = GetJournalVoucherEntryTypeSelectList(expensePayableDTO.Type.ToString());
                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayableDTO.Type.ToString());
                return View(expensePayableDTO);
            }
        }


        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            var expensePayableDTO = await _channelService.FindExpensePayableAsync(id, GetServiceHeader());

            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);
            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);
            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);

            return View(expensePayableDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, ExpensePayableDTO expensePayableDTO)
        {
            expensePayableDTO.ValidateAll();

            var expensePayableAuthOption = expensePayableDTO.Type;

            if (!expensePayableDTO.HasErrors)
            {

                await _channelService.AuditExpensePayableAsync(expensePayableDTO, expensePayableAuthOption, GetServiceHeader());

                ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(expensePayableDTO.Type.ToString());
                ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(expensePayableDTO.Type.ToString());
                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(expensePayableDTO.Type.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(expensePayableDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetCreditBatchesAsync(expensePayableDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(expensePayableDTO.Type.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(expensePayableDTO.Type.ToString());
                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayableDTO.Type.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = expensePayableDTO.ErrorMessages;

                return View(expensePayableDTO);
            }
        }

        public async Task<ActionResult> Approve(Guid id)
        {
            await ServeNavigationMenus();

            var expensePayableDTO = await _channelService.FindExpensePayableAsync(id, GetServiceHeader());

            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);
            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            return View(expensePayableDTO);
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

                await _channelService.AuthorizeExpensePayableAsync(expensePayableDTO, expensePayableDTO.Type, moduleNavigationItemCode, GetServiceHeader());

                // Success message
                TempData["SuccessMessage"] = "Expense payable approved successfully.";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = expensePayableDTO.ErrorMessages;

                // Error message
                TempData["ErrorMessage"] = "There were errors during approval: " + string.Join(", ", errorMessages);

                ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(expensePayableDTO.Type.ToString());
                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(expensePayableDTO.Type.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(expensePayableDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(expensePayableDTO.Type.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(expensePayableDTO.Type.ToString());

                return View(expensePayableDTO);
            }
        }



        [HttpPost]
        public async Task<ActionResult> RemoveEntry(Guid id, ExpensePayableDTO expensePayableDTO)
        {
            await ServeNavigationMenus();
            // Retrieve the TempData
            var ExpensePayableEntryDTOs = TempData["ExpensePayableEntryDTO"] as ObservableCollection<ExpensePayableEntryDTO>;

            if (ExpensePayableEntryDTOs != null)
            {
                // Find the entry to remove
                var entryToRemove = ExpensePayableEntryDTOs.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    ExpensePayableEntryDTOs.Remove(entryToRemove);
                    // Update TempData
                    TempData["ExpensePayableEntryDTO"] = ExpensePayableEntryDTOs;
                    return Json(new { success = true });
                }
            }
            return Json(new { success = false });
        }


        // In your Controller
        [HttpPost]
        public JsonResult AddExpensePayableEntry(Guid id, ExpensePayableEntryDTO expensePayableEntryDTO)
        {
            // Your logic here
            return Json(new { success = true });
        }



        //[HttpPost]
        //[ValidateAntiForgeryToken]


        //[httpget]
        //public async task<jsonresult> getexpensepayablesasync()
        //{
        //    var expensepayabledtos = await _channelservice.findexpensepayablesasync(getserviceheader());

        //    return json(expensepayabledtos, jsonrequestbehavior.allowget);
        //}
    }
}