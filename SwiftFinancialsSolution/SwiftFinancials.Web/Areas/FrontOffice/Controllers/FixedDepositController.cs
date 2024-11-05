using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Diagnostics;



namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class FixedDepositController : MasterController
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
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            bool includeProductDescription = true; 

            var pageCollectionInfo = await _channelService.FindFixedDepositsByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                pageIndex,
                jQueryDataTablesModel.iDisplayLength,
                includeProductDescription,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection
                    .OrderByDescending(postingPeriod => postingPeriod.CreatedDate)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: pageCollectionInfo.PageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<FixedDepositDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }


        [HttpPost]
        public async Task<ActionResult> FindExternalChequesByDate(JQueryDataTablesModel jQueryDataTablesModel, DateTime startDate, DateTime endDate)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;

            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            int status = 1; // Define the status as per your requirements
            bool includeProductDescription = true; // Set this based on your requirements

            // Call the service with adapted parameters
            var pageCollectionInfo = await _channelService.FindFixedDepositsByStatusAndFilterInPageAsync(
                status,
                jQueryDataTablesModel.sSearch,
                pageIndex,
                pageSize,
                includeProductDescription,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: pageCollectionInfo.PageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<FixedDepositDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var fixedDepositDTO = await _channelService.FindFixedDepositAsync(id, GetServiceHeader());

            return View(fixedDepositDTO);
        }




        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            // Populate ViewBag with Customer Type Select List
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);

            // Initialize a new FixedDepositDTO
            FixedDepositDTO fixedDepositDTO = new FixedDepositDTO();

            // If ID is provided, perform the lookup for the FixedDepositType
            if (id.HasValue && id != Guid.Empty)
            {
                var parseId = id.Value;

                var fixedDepositType = await _channelService.FindFixedDepositTypeAsync(parseId, GetServiceHeader());

                if (fixedDepositType != null)
                {
                    // Populate the DTO with FixedDepositType details
                    fixedDepositDTO.FixedDepositTypeId = fixedDepositType.Id;
                    fixedDepositDTO.FixedDepositTypeDescription = fixedDepositType.Description;
                   


                }
                else
                {
                    TempData["ErrorMessage"] = "Fixed deposit type details could not be found.";
                    return RedirectToAction("Index");
                }
            }

            return View(fixedDepositDTO);
        }




        public async Task<ActionResult> Search(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            var fixedDepositDTO = new FixedDepositDTO();

            if (id.HasValue && id != Guid.Empty)
            {
                try
                {
                    var customer = await _channelService.FindCustomerAccountAsync(
                        id.Value,
                        includeBalances: true,
                        includeProductDescription: true,
                        includeInterestBalanceForLoanAccounts: true,
                        considerMaturityPeriodForInvestmentAccounts: true,
                        GetServiceHeader()
                    );

                    if (customer != null)
                    {
                        // Populate DTO
                        PopulateFixedDepositDTO(fixedDepositDTO, customer);

                        var loanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                            customer.CustomerId,
                            customer.CustomerAccountTypeTargetProductId,
                            includeBalances: true,
                            includeProductDescription: true,
                            includeInterestBalanceForLoanAccounts: true,
                            considerMaturityPeriodForInvestmentAccounts: true,
                            serviceHeader: GetServiceHeader()
                        );


                        TempData["FixedDepositDTO"] = fixedDepositDTO;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Customer account details could not be found.";
                        return RedirectToAction("Index");
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception
                    TempData["ErrorMessage"] = "An error occurred while retrieving customer details.";
                    return RedirectToAction("Index");
                }
            }

            return View("Create", fixedDepositDTO);
        }

        // A helper method to populate the DTO from the customer data
        private void PopulateFixedDepositDTO(FixedDepositDTO dto, CustomerAccountDTO customer)
        {
            dto.CustomerAccountCustomerNonIndividualDescription = customer.FullAccountNumber;
            dto.CustomerAccountCustomerIndividualLastName = customer.StatusDescription;
            dto.AvailableBalance = customer.AvailableBalance;
            dto.CustomerAccountCustomerIndividualFirstName = customer.CustomerFullName;
            dto.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
            dto.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIndividualIdentityCardNumber;
            dto.CustomerAccountCustomerReference1 = customer.CustomerReference1;
            dto.CustomerAccountCustomerReference2 = customer.CustomerReference2;
            dto.CustomerAccountCustomerReference3 = customer.CustomerReference3;
            dto.BranchId = customer.BranchId;
            dto.BranchDescription = customer.BranchDescription;
            dto.CustomerAccountCustomerId = customer.CustomerId;
            dto.ProductChartOfAccountId = customer.CustomerAccountTypeTargetProductChartOfAccountId;
            dto.CustomerAccountId = customer.Id;
            dto.CustomerAccountBranchId = customer.BranchId;
            dto.CustomerAccountCustomerAccountTypeTargetProductId = customer.CustomerAccountTypeTargetProductId;
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(FixedDepositDTO fixedDepositDTO)
        {
            if (ModelState.IsValid) // Validate the model state
            {
                try
                {
                    // Create a service header (adjust as necessary)
                    ServiceHeader serviceHeader = GetServiceHeader();

                    // Invoke the service to create the fixed deposit
                    var result = await _channelService.InvokeFixedDepositAsync(fixedDepositDTO, serviceHeader);

                    if (result != null) // Check if the operation was successful
                    {
                        TempData["SuccessMessage"] = "Fixed deposit created successfully."; // Success message
                        return RedirectToAction("Index"); // Redirect to index or another appropriate action
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to create the fixed deposit."); // Handle failure
                    }
                }
                catch (Exception ex) // Catch any exceptions
                {
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}"); // Add error message
                }
            }

            // If model state is invalid or creation failed, return to the create view with the current model
            return View(fixedDepositDTO);
        }






        [HttpPost]
        public async Task<ActionResult> Add(FixedDepositPayableDTO fixedDepositPayableDTO, FormCollection form)
        {
            await ServeNavigationMenus();

            ObservableCollection<FixedDepositPayableDTO> fixedDepositPayableDTOs = TempData["FixedDepositPayableDTOs"] as ObservableCollection<FixedDepositPayableDTO>
                ?? new ObservableCollection<FixedDepositPayableDTO>();

            var selectedAccounts = form.GetValues("selectedAccounts");

            if (selectedAccounts != null)
            {
                foreach (var accountNumber in selectedAccounts)
                {
                    var accountDetails = form["accountDetails"].Split(',');

                    var newEntry = new FixedDepositPayableDTO
                    {
                        CustomerAccountCustomerNonIndividualRegistrationNumber = accountNumber,
                        CustomerAccountTypeTargetProductDescription = accountDetails[1],
                        BookBalance = Convert.ToDecimal(accountDetails[2]),
                        PrincipalBalance = Convert.ToDecimal(accountDetails[3]),
                        InterestBalance = Convert.ToDecimal(accountDetails[4])
                    };

                    if (string.IsNullOrWhiteSpace(newEntry.FullAccountNumber) ||
                        string.IsNullOrWhiteSpace(newEntry.CustomerAccountTypeTargetProductDescription) ||
                        newEntry.BookBalance <= 0 ||
                        newEntry.PrincipalBalance <= 0 ||
                        newEntry.InterestBalance <= 0)
                    {
                        TempData["ErrorMessage"] = "Could not add Fixed Deposit Payable Entry. Ensure all required fields are entered.";
                    }
                    else
                    {
                        fixedDepositPayableDTOs.Add(newEntry);
                    }
                }
                TempData["FixedDepositPayableDTOs"] = fixedDepositPayableDTOs;
                Session["FixedDepositPayableEntries"] = fixedDepositPayableDTOs;
                TempData["EntryAdded"] = "Successfully added entries.";
            }

            var fixedDepositDTO = new FixedDepositDTO
            {
            };

            ViewBag.FixedDepositPayableDTOs = fixedDepositPayableDTOs;

            return View("Create", fixedDepositDTO);
        }




        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            var fixedDepositDTO = await _channelService.FindFixedDepositAsync(id, GetServiceHeader());

            return View(fixedDepositDTO);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, FixedDepositDTO fixedDepositDTO, string fixedDepositAuth)
        {
            // Determine the action based on the value of fixedDepositAuth
            int fixedDepositAuthOption = fixedDepositAuth == "Post"
                ? (int)FixedDepositAuthOption.Post
                : (int)FixedDepositAuthOption.Reject;

            int moduleNavigationItemCode = 1; // Assign the appropriate value for moduleNavigationItemCode

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _channelService.AuditFixedDepositAsync(
                        fixedDepositDTO,
                        fixedDepositAuthOption,
                        moduleNavigationItemCode,
                        GetServiceHeader()
                    );

                    if (result)
                    {
                        TempData["SuccessMessage"] = fixedDepositAuth == "Post"
                            ? "Account Fixing Posted successfully."
                            : "Account Fixing Rejected successfully.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to process the account closure request. Please try again.";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing account fixing: {ex.Message}");
                    TempData["ErrorMessage"] = "An error occurred while processing the account fixed deposit. Please try again.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "There were validation errors. Please review the form and try again.";
            }

            return View(fixedDepositDTO);
        }








        public async Task<ActionResult> Terminate(Guid id, DateTime? startDate = null, DateTime? endDate = null, string searchText = null)
        {
            await ServeNavigationMenus();

            // Fetch the fixed deposit account
            var fixedDepositDTO = await _channelService.FindFixedDepositAsync(id, GetServiceHeader());

            if (fixedDepositDTO != null)
            {
                if (startDate.HasValue && endDate.HasValue)
                {
                    if (!(fixedDepositDTO.CreatedDate >= startDate.Value && fixedDepositDTO.MaturityDate <= endDate.Value))
                    {
                        fixedDepositDTO = null;
                    }
                }

                if (!string.IsNullOrEmpty(searchText) && !fixedDepositDTO.ProductDescription.Contains(searchText))
                {
                    fixedDepositDTO = null;
                }
            }

            if (Request.IsAjaxRequest())
            {
                return Json(new[] { fixedDepositDTO }, JsonRequestBehavior.AllowGet);
            }

            ViewBag.ModuleNavigationItemCode = 123;
            ViewBag.FixedDeposit = fixedDepositDTO;

            return View(fixedDepositDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Terminate(Guid[] selectedFixedDepositIds, int moduleNavigationItemCode)
        {
            if (selectedFixedDepositIds == null || selectedFixedDepositIds.Length == 0)
            {
                TempData["ErrorMessage"] = "No fixed deposit selected for termination.";
                return RedirectToAction("Terminate");
            }

            // Fetch the fixed deposits using the selected IDs
            var fixedDeposits = new List<FixedDepositDTO>();
            foreach (var id in selectedFixedDepositIds)
            {
                var fixedDepositDTO = await _channelService.FindFixedDepositAsync(id, GetServiceHeader());
                if (fixedDepositDTO != null)
                {
                    fixedDeposits.Add(fixedDepositDTO);
                }
            }

            if (!fixedDeposits.Any())
            {
                TempData["ErrorMessage"] = "The selected fixed deposit(s) could not be found.";
                return RedirectToAction("Terminate");
            }

            var selectedFixedDepositObservableCollection = new ObservableCollection<FixedDepositDTO>(fixedDeposits);

            bool result = await _channelService.RevokeFixedDepositsAsync(selectedFixedDepositObservableCollection, moduleNavigationItemCode, GetServiceHeader());

            if (result)
            {
                TempData["SuccessMessage"] = "The selected fixed deposit(s) were successfully terminated.";
                return RedirectToAction("Index", "FixedDeposit", new { Area = "FrontOffice" });
            }
            else
            {
                TempData["ErrorMessage"] = "An error occurred while terminating the selected fixed deposit(s).";
                return RedirectToAction("Terminate");
            }
        }




        public async Task<ActionResult> Liquidation(Guid id, DateTime? startDate = null, DateTime? endDate = null, string searchText = null)
        {
            await ServeNavigationMenus();

            // Fetch the fixed deposit account
            var fixedDepositDTO = await _channelService.FindFixedDepositAsync(id, GetServiceHeader());

            // Optionally filter based on the date range and search text
            if (fixedDepositDTO != null)
            {
                if (startDate.HasValue && endDate.HasValue)
                {
                    if (!(fixedDepositDTO.CreatedDate >= startDate.Value && fixedDepositDTO.MaturityDate <= endDate.Value))
                    {
                        fixedDepositDTO = null;
                    }
                }

                if (!string.IsNullOrEmpty(searchText) && !fixedDepositDTO.ProductDescription.Contains(searchText))
                {
                    fixedDepositDTO = null;
                }
            }

            if (Request.IsAjaxRequest())
            {
                return Json(new[] { fixedDepositDTO }, JsonRequestBehavior.AllowGet);
            }
            ViewBag.ModuleNavigationItemCode = 123;


            ViewBag.FixedDeposit = fixedDepositDTO;
            return View(fixedDepositDTO);
        }



        [HttpPost]
        public async Task<ActionResult> Liquidation(Guid[] selectedFixedDepositIds, int moduleNavigationItemCode)
        {
            if (selectedFixedDepositIds == null || selectedFixedDepositIds.Length == 0)
            {
                TempData["ErrorMessage"] = "No fixed deposit selected for liquidation.";
                return RedirectToAction("Liquidation");
            }

            // Fetch the fixed deposits using the selected IDs
            var fixedDeposits = new List<FixedDepositDTO>();
            foreach (var id in selectedFixedDepositIds)
            {
                var fixedDepositDTO = await _channelService.FindFixedDepositAsync(id, GetServiceHeader());
                if (fixedDepositDTO != null)
                {
                    if (fixedDepositDTO.MaturityDate > DateTime.Now)
                    {
                        TempData["ErrorMessage"] = $"The fixed deposit with account number {fixedDepositDTO.CustomerAccountFullAccountNumber} has not yet reached maturity and cannot be liquidated.";
                        return RedirectToAction("Liquidation");
                    }
                    fixedDeposits.Add(fixedDepositDTO);
                }
            }

            if (!fixedDeposits.Any())
            {
                TempData["ErrorMessage"] = "The selected fixed deposit(s) could not be found.";
                return RedirectToAction("Liquidation");
            }

            foreach (var fixedDepositDTO in fixedDeposits)
            {
                bool result = await _channelService.PayFixedDepositAsync(fixedDepositDTO, moduleNavigationItemCode, GetServiceHeader());

                if (!result)
                {
                    TempData["ErrorMessage"] = $"An error occurred while terminating the fixed deposit with ID {fixedDepositDTO.Id}.";
                    return RedirectToAction("Liquidation");
                }
            }

            TempData["SuccessMessage"] = "The selected fixed deposit(s) were successfully terminated.";
            return RedirectToAction("Index", "FixedDeposit", new { Area = "FrontOffice" });
        }



        public async Task<ActionResult> Catalogue()
        {
            await ServeNavigationMenus();
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Catalogue(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindFixedDepositsByFilterInPageAsync(
                   jQueryDataTablesModel.sSearch,
                   jQueryDataTablesModel.iDisplayStart,
                   jQueryDataTablesModel.iDisplayLength,
                   includeProductDescription: true,
                   GetServiceHeader()
               );
            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;


                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(expensePayable => expensePayable.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<FixedDepositDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }





    }
}