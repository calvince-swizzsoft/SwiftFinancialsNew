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
            var expensePayableEntries = await _channelService.FindExpensePayableEntriesByExpensePayableIdAsync(id, GetServiceHeader());

            ViewBag.ExpensePaybleTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);



            ViewBag.ExpensePayableEntryDTOs = expensePayableEntries;
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
                ExpensePayableEntries = new List<ExpensePayableEntryDTO>()
            };

            return View(model);
        }


        [HttpPost]
        public async Task<ActionResult> Create(ExpensePayableDTO expensePayableDTO, ObservableCollection<ExpensePayableEntryDTO> expensePayableEntries)
        {
            // Retrieve ExpensePayableDTO and ExpensePayableEntryDTOs from TempData
            expensePayableDTO = TempData["ExpensePayableDTO"] as ExpensePayableDTO ?? expensePayableDTO;

            // Check if individual ExpensePayableEntryDTO is present in TempData and assign it to ExpensePayableEntries
            if (TempData["ExpensePayableEntryDTOs"] != null)
            {
                // expensePayableDTO.ExpensePayableEntries = TempData["ExpensePayableEntryDTO"] as ObservableCollection<ExpensePayableEntryDTO>;
                ExpensePayableEntryDTOs = TempData["ExpensePayableEntryDTOs"] as ObservableCollection<ExpensePayableEntryDTO>;

            }

            // Validate the ExpensePayableDTO
            expensePayableDTO.ValidateAll();

            if (!expensePayableDTO.HasErrors)
            {
                //var expensePayableEntries = new ObservableCollection<ExpensePayableEntryDTO>();

                //// Iterate through each entry and assign properties accordingly
                //foreach (var entryDTO in expensePayableDTO.ExpensePayableEntries)
                //{
                //    entryDTO.BranchDescription = expensePayableDTO.BranchDescription;
                //    entryDTO.ChartOfAccountId = expensePayableDTO.ChartOfAccountId;
                //    entryDTO.ChartOfAccountCostCenterDescription = expensePayableDTO.ChartOfAccountCostCenterDescription;
                //    entryDTO.ChartOfAccountAccountName = expensePayableDTO.ChartOfAccountAccountName;
                //    entryDTO.Value = expensePayableDTO.TotalValue;

                //    expensePayableEntries.Add(entryDTO);
                //}

                // Call the service to add the expense payable
                var expensePayable = await _channelService.AddExpensePayableAsync(expensePayableDTO, GetServiceHeader());

                if (expensePayable.ErrorMessageResult != null)
                {
                    await ServeNavigationMenus();

                    TempData["ErrorMessage"] = expensePayable.ErrorMessageResult;
                    return View();
                }

                TempData["SuccessMessage"] = "Expense payable created successfully!";
                TempData["ExpensePayableDTO"] = "";

                //if (expensePayableEntries.Any())
                //{
                await _channelService.UpdateExpensePayableEntriesByExpensePayableIdAsync(expensePayable.Id, ExpensePayableEntryDTOs, GetServiceHeader());
                //}

                TempData["ExpensePayableEntryDTO"] = "";
                TempData["Success"] = "Expense payable and entries have been created successfully";

                return RedirectToAction("Index");
            }

            // If validation fails, reload data from TempData and redirect back to the view
            //expensePayableDTO = TempData["ExpensePayableDTO"] as ExpensePayableDTO;
            //expensePayableEntryDTOs = TempData["ExpensePayableEntryDTOs"] as ObservableCollection<ExpensePayableEntryDTO>;

            return RedirectToAction("Create");
        }

        


        [HttpPost]
        public async Task<ActionResult> Add(ExpensePayableDTO expensePayableDTO)
        {
            // Ensure that navigation menus are loaded
            await ServeNavigationMenus();

            // Retrieve the current list of entries from TempData or initialize a new list
            ObservableCollection<ExpensePayableEntryDTO> expensePayableEntryDTOs = TempData["ExpensePayableEntryDTO"] as ObservableCollection<ExpensePayableEntryDTO>;
            if (expensePayableEntryDTOs == null)
                expensePayableEntryDTOs = new ObservableCollection<ExpensePayableEntryDTO>();

            // Add the new entry to the list
            var newEntry = expensePayableDTO.ExpensePayableEntry;
            if (newEntry != null)
            {
                // Assign values to newEntry from the provided expensePayableDTO
                newEntry.BranchDescription = expensePayableDTO.BranchDescription;
                newEntry.ChartOfAccountAccountName = expensePayableDTO.ChartOfAccountAccountName;
                newEntry.ChartOfAccountId = expensePayableDTO.ChartOfAccountId;
                newEntry.Reference = expensePayableDTO.ExpensePayableEntry.Reference;
                newEntry.SecondaryDescription = expensePayableDTO.ExpensePayableEntry.SecondaryDescription;
                newEntry.PrimaryDescription = expensePayableDTO.ExpensePayableEntry.PrimaryDescription;
                newEntry.Value = expensePayableDTO.ExpensePayableEntry.Value;

                // Validate the entry and add it to the list if valid
                if (string.IsNullOrWhiteSpace(newEntry.PrimaryDescription) ||
                    string.IsNullOrWhiteSpace(newEntry.SecondaryDescription) ||
                    newEntry.Value <= 0)
                {
                    TempData["tPercentage"] = "Could not add Expense Payable Entry. Ensure all required fields are entered.";
                }
                else
                {
                    // Add the entry to the collection
                    expensePayableEntryDTOs.Add(newEntry);

                    // Calculate the total value of the entries
                    decimal sumValue = expensePayableEntryDTOs.Sum(e => e.Value);

                    // Check if the total value exceeds the allowed amount
                    if (sumValue > expensePayableDTO.TotalValue)
                    {
                        TempData["tPercentage"] = "Failed to add Expense Payable Entry. Total value exceeded allowed amount.";
                        expensePayableEntryDTOs.Remove(newEntry);
                    }
                    else
                    {
                        // Save the updated entries back to TempData and Session
                        TempData["ExpensePayableEntryDTO"] = expensePayableEntryDTOs;
                        Session["ExpensePayableEntries"] = expensePayableEntryDTOs;
                        TempData["ExpensePayableDTO"] = expensePayableDTO;

                        TempData["EntryAdded"] = "Successfully added an entry.";
                    }
                }
            }

            // Pass the updated list back to the view
            ViewBag.ExpensePayableEntryDTOs = expensePayableEntryDTOs;

            // Return to the Create/Edit view
            return View("Create");
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
                try
                {
                    await _channelService.AuditExpensePayableAsync(expensePayableDTO, expensePayableAuthOption, GetServiceHeader());

                    // Set success message in TempData
                    TempData["SuccessMessage"] = "Expense payable has been successfully verified.";

                    // Prepare view bags for the view
                    ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(expensePayableDTO.Type.ToString());
                    ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(expensePayableDTO.Type.ToString());
                    ViewBag.MonthsSelectList = GetMonthsAsync(expensePayableDTO.Type.ToString());
                    ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(expensePayableDTO.Type.ToString());
                    ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(expensePayableDTO.Type.ToString());
                    ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayableDTO.Type.ToString());

                    // Redirect to Index action with a success message
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Set error message in TempData
                    TempData["ErrorMessage"] = "An error occurred while verifying the expense payable: " + ex.Message;

                    // Prepare view bags for the view
                    ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(expensePayableDTO.Type.ToString());
                    ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(expensePayableDTO.Type.ToString());
                    ViewBag.MonthsSelectList = GetMonthsAsync(expensePayableDTO.Type.ToString());
                    ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(expensePayableDTO.Type.ToString());
                    ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(expensePayableDTO.Type.ToString());
                    ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayableDTO.Type.ToString());

                    return View(expensePayableDTO);
                }
            }
            else
            {
                // Set error message in TempData
                TempData["ErrorMessage"] = "Validation failed. Please correct the errors and try again.";

                // Prepare view bags for the view
                ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(expensePayableDTO.Type.ToString());
                ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(expensePayableDTO.Type.ToString());
                ViewBag.MonthsSelectList = GetMonthsAsync(expensePayableDTO.Type.ToString());
                ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(expensePayableDTO.Type.ToString());
                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(expensePayableDTO.Type.ToString());
                ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(expensePayableDTO.Type.ToString());

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
            var expensePayableEntryDTOs = TempData["ExpensePayableEntryDTO"] as ObservableCollection<ExpensePayableEntryDTO>;

            if (expensePayableEntryDTOs != null)
            {
                // Find the entry to remove
                var entryToRemove = expensePayableEntryDTOs.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    expensePayableEntryDTOs.Remove(entryToRemove);

                    // Update TempData
                    TempData["ExpensePayableEntryDTO"] = expensePayableEntryDTOs;

                    return Json(new { success = true });
                }
            }

            return Json(new { success = false, message = "Entry not found." });
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