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

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindFixedDepositsByFilterInPageAsync(
                   jQueryDataTablesModel.sSearch,
                   jQueryDataTablesModel.iDisplayStart,
                   jQueryDataTablesModel.iDisplayLength,
                    true,
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




        public async Task<ActionResult> Search(Guid? id, FixedDepositDTO fixedDepositDTO)
        {
            await ServeNavigationMenus();

            // Retrieve fixedDepositDTO from TempData
            if (TempData["FixedDepositDTO"] != null)
            {
                fixedDepositDTO = TempData["FixedDepositDTO"] as FixedDepositDTO;
                // TempData is read-once, so we need to preserve the value
                TempData.Keep("FixedDepositDTO");
            }

            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);

            if (id.HasValue && id != Guid.Empty)
            {
                var parseId = id.Value;

                var customer = await _channelService.FindCustomerAccountAsync(
                    parseId,
                    includeBalances: true,
                    includeProductDescription: true,
                    includeInterestBalanceForLoanAccounts: true,
                    considerMaturityPeriodForInvestmentAccounts: true,
                    GetServiceHeader()
                );

                if (customer != null)
                {
                    fixedDepositDTO.CustomerAccountCustomerNonIndividualDescription = customer.FullAccountNumber;
                    fixedDepositDTO.CustomerAccountCustomerIndividualLastName = customer.StatusDescription;
                    fixedDepositDTO.AvailableBalance = customer.AvailableBalance;
                    fixedDepositDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerFullName;
                    fixedDepositDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                    fixedDepositDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIndividualIdentityCardNumber;
                    fixedDepositDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                    fixedDepositDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                    fixedDepositDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                    fixedDepositDTO.Category = fixedDepositDTO.Category;
                    fixedDepositDTO.BranchId = customer.BranchId;
                    fixedDepositDTO.CustomerAccountCustomerId = customer.CustomerId;
                    fixedDepositDTO.ProductChartOfAccountId = customer.CustomerAccountTypeTargetProductChartOfAccountId;
                    fixedDepositDTO.CustomerAccountId = customer.Id;
                    fixedDepositDTO.Category = fixedDepositDTO.Category;
                    fixedDepositDTO.Remarks = fixedDepositDTO.Remarks;
                    fixedDepositDTO.Value = fixedDepositDTO.Value;
                    fixedDepositDTO.Rate = fixedDepositDTO.Rate;
                    fixedDepositDTO.Term = fixedDepositDTO.Term;
                    fixedDepositDTO.MaturityAction = fixedDepositDTO.MaturityAction;
                    fixedDepositDTO.CustomerAccountBranchId = customer.BranchId;
                    fixedDepositDTO.CustomerAccountCustomerAccountTypeTargetProductId = customer.CustomerAccountTypeTargetProductId;

                    var loanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                        customer.CustomerId,
                        customer.CustomerAccountTypeTargetProductId,
                        includeBalances: true,
                        includeProductDescription: true,
                        includeInterestBalanceForLoanAccounts: true,
                        considerMaturityPeriodForInvestmentAccounts: true,
                        serviceHeader: GetServiceHeader()
                    );

                    if (loanAccounts != null)
                    {
                        var loanAccountsList = loanAccounts.Take(3).ToList();
                        ViewBag.LoanAccounts = loanAccountsList;
                    }


                    TempData["FixedDepositDTO"] = fixedDepositDTO;

                }
                else
                {
                    TempData["ErrorMessage"] = "Customer account details could not be found.";
                    return RedirectToAction("Index");
                }
            }

            return View("Create", fixedDepositDTO);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(FixedDepositDTO fixedDepositDTO)
        {
            await ServeNavigationMenus();

            if (!ModelState.IsValid)
            {
                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
                return View("Create", fixedDepositDTO);
            }

            try
            {
                var serviceHeader = GetServiceHeader(); 
                var result = await _channelService.InvokeFixedDepositAsync(fixedDepositDTO, serviceHeader);

                if (result != null)
                {
                    TempData["FixedDepositDTO"] = result;

                    TempData["SuccessMessage"] = "Fixed deposit created successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "The fixed deposit could not be processed. Please try again.";
                    return View("Create", fixedDepositDTO);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while processing your request: {ex.Message}";
                return View("Create", fixedDepositDTO);
            }
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



        
        public async Task<ActionResult> Verify(Guid? id)
        {
            // Ensure the navigation menus are served
            await ServeNavigationMenus();

            // Check if the ID is provided
            if (!id.HasValue || id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Fixed Deposit ID is not provided.";
                return RedirectToAction("Index");
            }

            var fixedDepositId = id.Value; 

            // Call the service to find the fixed deposit by ID
            var fixedDepositDTO = await _channelService.FindFixedDepositAsync(fixedDepositId, GetServiceHeader());

            if (fixedDepositDTO == null)
            {
                TempData["ErrorMessage"] = "Fixed Deposit details could not be found.";
                return RedirectToAction("Index");
            }

            // Populate necessary ViewBag data for dropdowns or other elements

            return View(fixedDepositDTO);
        }



        // POST: Verify - Submits the Fixed Deposit details for verification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(FixedDepositDTO fixedDepositDTO, string fixedDepositAuthOption)
        {
            // Ensure the navigation menus are served
            await ServeNavigationMenus();

            // Validate the model
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "There are validation errors. Please check the details and try again.";
                return View("Index"); 
            }

            // Validate that the authorization option is provided
            if (string.IsNullOrEmpty(fixedDepositAuthOption))
            {
                TempData["ErrorMessage"] = "Authorization option is required.";
                return View(fixedDepositDTO);
            }

            // Convert the string action to FixedDepositAuthOption enum
            if (!Enum.TryParse(fixedDepositAuthOption, out FixedDepositAuthOption authOption))
            {
                TempData["ErrorMessage"] = "Invalid authorization option selected.";
                return View(fixedDepositDTO);
            }

            try
            {
                // Set the module navigation item code (adjust as necessary)
                int moduleNavigationItemCode = 1234;

                // Get the service header
                var serviceHeader = GetServiceHeader();

                // Call the service to audit the fixed deposit
                var isAuditSuccessful = await _channelService.AuditFixedDepositAsync(fixedDepositDTO, (int)authOption, moduleNavigationItemCode, serviceHeader);

                if (isAuditSuccessful)
                {
                    TempData["SuccessMessage"] = "Fixed Deposit verified successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "An error occurred while verifying the Fixed Deposit. Please try again.";
                }
            }
            catch (Exception ex)
            {
                // Log the exception (ex) if necessary
                TempData["ErrorMessage"] = "An unexpected error occurred: " + ex.Message;
            }

            // If something went wrong, redisplay the form
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