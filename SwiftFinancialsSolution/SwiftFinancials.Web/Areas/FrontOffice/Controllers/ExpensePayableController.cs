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
using System.Windows.Forms;


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

            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            try
            {
                var pageCollectionInfo = await _channelService.FindExpensePayablesByFilterInPageAsync(
                    jQueryDataTablesModel.sSearch,
                    pageIndex,
                    pageSize,
                    GetServiceHeader()
                );

                if (pageCollectionInfo != null)
                {
                    totalRecordCount = pageCollectionInfo.ItemsCount;

                    var sortedData = sortAscending
                        ? pageCollectionInfo.PageCollection.OrderBy(item => item.CreatedDate).ToList()
                        : pageCollectionInfo.PageCollection.OrderByDescending(item => item.CreatedDate).ToList();

                    searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                        ? sortedData.Count
                        : totalRecordCount;

                    return this.DataTablesJson(
                        items: sortedData,
                        totalRecords: totalRecordCount,
                        totalDisplayRecords: searchRecordCount,
                        sEcho: jQueryDataTablesModel.sEcho
                    );
                }
                else
                {
                    return this.DataTablesJson(
                        items: new List<ExpensePayableDTO>(),
                        totalRecords: totalRecordCount,
                        totalDisplayRecords: searchRecordCount,
                        sEcho: jQueryDataTablesModel.sEcho
                    );
                }
            }
            catch (Exception)
            {
                return this.DataTablesJson(
                    items: new List<ExpensePayableDTO>(),
                    totalRecords: 0,
                    totalDisplayRecords: 0,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
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
        public async Task<ActionResult> Create(ExpensePayableDTO expensePayableDTO)
        {
            var expensePayableEntries = TempData["ExpensePayableEntryDTOs"] as ObservableCollection<ExpensePayableEntryDTO>;

            TempData.Keep("ExpensePayableEntryDTOs");

            if (expensePayableDTO == null || expensePayableEntries == null)
            {
                TempData["ErrorMessage"] = "Required data is missing.";
                return RedirectToAction("Create");
            }

            // Validate the main ExpensePayableDTO
            expensePayableDTO.ValidateAll();
            if (expensePayableDTO.HasErrors)
            {
                ModelState.AddModelError(string.Empty, "Validation errors occurred.");
                TempData["ExpensePayableDTO"] = expensePayableDTO;
                return RedirectToAction("Create");
            }

            // Add the main Expense Payable
            var resultDTO = await _channelService.AddExpensePayableAsync(expensePayableDTO, GetServiceHeader());
            MessageBox.Show(
                                                              "Operation Success",
                                                              "Customer Receipts",
                                                              MessageBoxButtons.OK,
                                                              MessageBoxIcon.Information,
                                                              MessageBoxDefaultButton.Button1,
                                                              MessageBoxOptions.ServiceNotification
                                                          );

            if (!string.IsNullOrEmpty(resultDTO.ErrorMessageResult))
            {
                ModelState.AddModelError(string.Empty, resultDTO.ErrorMessageResult);
                TempData["ErrorMessage"] = resultDTO.ErrorMessageResult;
                return View(expensePayableDTO);
            }




            // Add each entry individually
            foreach (var entry in expensePayableEntries)
            {
                entry.ExpensePayableId = resultDTO.Id;
                var entryResult = await _channelService.AddExpensePayableEntryAsync(entry, GetServiceHeader());
                MessageBox.Show(
                                                              "Operation Success",
                                                              "Customer Receipts",
                                                              MessageBoxButtons.OK,
                                                              MessageBoxIcon.Information,
                                                              MessageBoxDefaultButton.Button1,
                                                              MessageBoxOptions.ServiceNotification
                                                          );



                // Check if any error messages are returned
                if (entryResult.ErrorMessages != null && entryResult.ErrorMessages.Any())
                {
                    string errorMessage = string.Join("; ", entryResult.ErrorMessages);
                    ModelState.AddModelError(string.Empty, $"Error adding entry: {errorMessage}");
                    TempData["ErrorMessage"] = errorMessage;
                    return View(expensePayableDTO);
                }

                if (entryResult.ErrorMessages != null && entryResult.ErrorMessages.Any())
                {
                    string errorMessage = string.Join("; ", entryResult.ErrorMessages);
                    ModelState.AddModelError(string.Empty, $"Error adding entry: {errorMessage}");
                    TempData["ErrorMessage"] = errorMessage;
                    return View("Index");
                }
            }

            // Success message and redirect to Index
            TempData["SuccessMessage"] = "Expense payable and its entries created successfully.";
            TempData["SuccessMessage"] = "Expense payable and its entries created successfully.";
            return RedirectToAction("Index");
        }





        [HttpPost]
        public ActionResult AddEntry(ExpensePayableDTO expensePayableDTO, ExpensePayableEntryDTO entry)
        {
            TempData["ExpensePayableDTO"] = expensePayableDTO;

            var expensePayableEntries = TempData["ExpensePayableEntryDTOs"] as ObservableCollection<ExpensePayableEntryDTO>
                                        ?? new ObservableCollection<ExpensePayableEntryDTO>();

            expensePayableEntries.Add(entry);

            TempData["ExpensePayableEntryDTOs"] = expensePayableEntries;

            return Json(new { success = true });
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
            var expensePayableEntries = await _channelService.FindExpensePayableEntriesByExpensePayableIdAsync(id, GetServiceHeader());


            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);
            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);
            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);
            ViewBag.ExpensePayableEntryDTOs = expensePayableEntries;


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

                    return View("Index");
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

                return View("Index");
            }
        }


        public async Task<ActionResult> Approve(Guid id)
        {
            await ServeNavigationMenus();

            var expensePayableDTO = await _channelService.FindExpensePayableAsync(id, GetServiceHeader());

            var expensePayableEntries = await _channelService.FindExpensePayableEntriesByExpensePayableIdAsync(id, GetServiceHeader());


            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);
            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);
            ViewBag.ExpensePayableAuthOptionTypeSelectList = GetExpensePayableAuthOptionSelectList(string.Empty);
            ViewBag.CreditBatchTypeTypeSelectList = GetCreditBatchesAsync(string.Empty);
            ViewBag.QueuePriorityTypeSelectList = GetQueuePriorityAsync(string.Empty);
            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.JournalVoucherTypeSelectList = GetJournalVoucherTypeSelectList(string.Empty);
            ViewBag.ExpensePayableEntryDTOs = expensePayableEntries;
            return View(expensePayableDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approve(Guid id, ExpensePayableDTO expensePayableDTO)
        {
            // Validate all properties of the DTO
            expensePayableDTO.ValidateAll();

            // Check if there are any errors after validation
            if (!expensePayableDTO.HasErrors)
            {
                // Extract necessary data from DTO
                var expensePayableAuthOption = expensePayableDTO.ExpensePayableAuthOption;
                var moduleNavigationItemCode = expensePayableDTO.ModuleNavigationItemCode;

                // Authorize the expense payable
                var isAuthorized = await _channelService.AuthorizeExpensePayableAsync(expensePayableDTO, expensePayableDTO.Type, moduleNavigationItemCode, GetServiceHeader());

                if (!isAuthorized)
                {
                    // Set error message in TempData
                    TempData["errorMessage"] = "Sorry, but requisite minimum requirements have not been satisfied viz. (batch total/posting period/journal voucher control account)";

                    // Return the Create view with the current model to allow user corrections
                    return View("Create", expensePayableDTO);
                }

                // Set success message in TempData
                TempData["SuccessMessage"] = "Expense payable approved successfully.";

                // Redirect to Index on successful approval
                return RedirectToAction("Index");

            }
            else
            {
                // Get the error messages
                var errorMessages = expensePayableDTO.ErrorMessages;

                // Set error message in TempData
                TempData["ErrorMessage"] = "There were errors during approval: " + string.Join(", ", errorMessages);


                ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(expensePayableDTO.Type.ToString());

                // Return the view with the DTO containing the errors
                return View("Index");
            }
        }



        [HttpPost]
        public ActionResult RemoveEntry(Guid entryId)
        {
            // Retrieve the collection of entries from TempData
            var expensePayableEntries = TempData["ExpensePayableEntryDTOs"] as ObservableCollection<ExpensePayableEntryDTO>
                                        ?? new ObservableCollection<ExpensePayableEntryDTO>();

            // Find the entry with the specified ID
            var entryToRemove = expensePayableEntries.FirstOrDefault(e => e.Id == entryId);
            if (entryToRemove != null)
            {
                // Remove the entry from the collection
                expensePayableEntries.Remove(entryToRemove);

                // Save the updated collection back to TempData
                TempData["ExpensePayableEntryDTOs"] = expensePayableEntries;

                // Return a success response
                return Json(new { success = true });
            }

            // Return a failure response if entry was not found
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