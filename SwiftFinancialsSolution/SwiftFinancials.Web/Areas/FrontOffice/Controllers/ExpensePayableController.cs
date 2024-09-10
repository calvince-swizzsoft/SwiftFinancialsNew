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
        public async Task<ActionResult> Create(ExpensePayableDTO expensePayableDTO)
        {
            // Retrieve the entries from TempData
            var expensePayableEntries = TempData["ExpensePayableEntryDTOs"] as ObservableCollection<ExpensePayableEntryDTO>;

            // Ensure TempData is retained for subsequent calls
            TempData.Keep("ExpensePayableEntryDTOs");

            // Ensure expensePayableDTO and expensePayableEntries are not null
            if (expensePayableDTO == null || expensePayableEntries == null)
            {
                TempData["ErrorMessage"] = "Required data is missing.";
                return RedirectToAction("Create");
            }

            expensePayableDTO.ValidateAll();

            if (expensePayableDTO.HasErrors)
            {
                ModelState.AddModelError(string.Empty, "Validation errors occurred.");

                TempData["ExpensePayableDTO"] = expensePayableDTO;

                return RedirectToAction("Create");
            }

            var resultDTO = await _channelService.AddExpensePayableAsync(expensePayableDTO, GetServiceHeader());

            if (!string.IsNullOrEmpty(resultDTO.ErrorMessageResult))
            {
                ModelState.AddModelError(string.Empty, resultDTO.ErrorMessageResult);

                TempData["ErrorMessage"] = resultDTO.ErrorMessageResult;

                return View(expensePayableDTO);
            }

            await _channelService.UpdateExpensePayableEntriesByExpensePayableIdAsync(resultDTO.Id, expensePayableEntries, GetServiceHeader());

            // Success message and clear TempData
            TempData["SuccessMessage"] = "Expense payable created successfully";
            TempData["ExpensePayableDTO"] = "";
            TempData["ExpensePayableEntryDTOs"] = "";

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
                return View(expensePayableDTO);
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