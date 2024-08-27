using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class AccountClosureController : MasterController
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

            var pageCollectionInfo = await _channelService.FindAccountClosureRequestsByFilterInPageAsync(
                            jQueryDataTablesModel.sSearch, 2, jQueryDataTablesModel.iDisplayStart,
                            jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader()
                        );
            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;


                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(expensePayable => expensePayable.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<AccountClosureRequestDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);
            return View(accountClosureRequestDTO);
        }

        // GET: FrontOffice/AccountClosureRequest/Create
        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);

            var accountClosureRequestDTO = new AccountClosureRequestDTO();

            if (id.HasValue && id != Guid.Empty)
            {
                var parseId = id.Value;

                // Retrieve customer account details
                var customer = await _channelService.FindCustomerAccountAsync(
                    parseId,
                    includeBalances: false,
                    includeProductDescription: false,
                    includeInterestBalanceForLoanAccounts: false,
                    considerMaturityPeriodForInvestmentAccounts: false,
                    GetServiceHeader()
                );

                if (customer != null)
                {
                    // Populate the DTO with customer details
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIdentificationNumber;
                    accountClosureRequestDTO.CustomerAccountRemarks = customer.Remarks;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerNonIndividualDescription = customer.TypeDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerNonIndividualRegistrationNumber = customer.RecordStatusDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                    accountClosureRequestDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                    accountClosureRequestDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerFullName;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerId = customer.CustomerId;
                    accountClosureRequestDTO.BranchId = customer.BranchId;
                    accountClosureRequestDTO.CustomerAccountId = customer.Id;

                    
                    // Fetch loan accounts for the customer based on customer ID and product type ID
                    var loanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                        customer.CustomerId, 
                        customer.CustomerAccountTypeTargetProductId, 
                        includeBalances: true, 
                        includeProductDescription: true,
                        includeInterestBalanceForLoanAccounts: true,
                        considerMaturityPeriodForInvestmentAccounts: true,
                        serviceHeader: GetServiceHeader() 
                    );

                    var investmentAccounts = await _channelService.FindCustomerAccountsByProductCodeAndFilterInPageAsync(
                        productCode: 12345, // Example product code for Investment Accounts
                        text: string.Empty,
                        customerFilter: 1 /* Customer Filter */,
                        pageIndex: 0,
                        pageSize: 100, // Adjust page size as needed
                        includeBalances: true,
                        includeProductDescription: true,
                        includeInterestBalanceForLoanAccounts: false,
                        considerMaturityPeriodForInvestmentAccounts: true,
                        serviceHeader: GetServiceHeader()
                    );

                    var loansGuaranteed = await _channelService.FindCustomerAccountsByFilterInPageAsync(
                        text: "Guaranteed", // Filter text to match guaranteed loans
                        customerFilter: 2, // Example value for guaranteed loans
                        pageIndex: 0,
                        pageSize: 10, // Adjust page size as needed
                        includeBalances: true,
                        includeProductDescription: true,
                        includeInterestBalanceForLoanAccounts: false,
                        considerMaturityPeriodForInvestmentAccounts: false,
                        serviceHeader: GetServiceHeader()
                    );


                    // Check for null and set ViewBag.LoanAccounts
                    var loanAccountsList = loanAccounts.Take(3).ToList();
                    var investmentAccountsList = investmentAccounts;
                    var loansGuaranteedList = loansGuaranteed;

                    // Pass the limited list to the view
                    ViewBag.LoanAccounts = loanAccountsList;
                    ViewBag.InvestmentAccounts = investmentAccountsList;
                    ViewBag.LoansGuaranteed = loansGuaranteedList;
                }
                else
                {
                    TempData["ErrorMessage"] = "Customer account details could not be found.";
                    return RedirectToAction("Index");
                }
            }

            return View(accountClosureRequestDTO);
        }



        [HttpPost]
        public async Task<ActionResult> Create(AccountClosureRequestDTO accountClosureRequestDTO)
        {
            // Handle unexpected null DTO
            if (accountClosureRequestDTO == null)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                return View("Error");
            }

            // Access the hidden fields
            var branchId = accountClosureRequestDTO.BranchId;
            var customerAccountId = accountClosureRequestDTO.CustomerAccountId;

            // Validate the DTO
            accountClosureRequestDTO.ValidateAll();

            if (!accountClosureRequestDTO.HasErrors)
            {
                // Process the account closure request
                await _channelService.AddAccountClosureRequestAsync(accountClosureRequestDTO, GetServiceHeader());

                TempData["SuccessMessage"] = "Account closure request successfully created.";
                return RedirectToAction("Index");
            }
            else
            {
                // Log errors and return view with validation messages
                foreach (var error in accountClosureRequestDTO.ErrorMessages)
                {
                    Debug.WriteLine($"- {error}");
                }

                TempData["ErrorMessage"] = "There were errors in your submission. Please review the form and try again.";
                return View(accountClosureRequestDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);
            PopulateViewBags(accountClosureRequestDTO);
            return View(accountClosureRequestDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, AccountClosureRequestDTO accountClosureRequestDTO)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Assuming you need to determine the audit option here. Replace with actual logic as needed.
                    int accountClosureAuditOption = GetAuditOption(accountClosureRequestDTO);

                    // Call the AuditAccountClosureRequestAsync method
                    bool auditResult = await _channelService.AuditAccountClosureRequestAsync(accountClosureRequestDTO, accountClosureAuditOption, GetServiceHeader());

                    if (auditResult)
                    {
                        // Optionally, you might want to update the account closure request status or details here
                        // await _channelService.UpdateAccountClosureRequestAsync(accountClosureRequestDTO, GetServiceHeader());

                        return RedirectToAction("Index");
                    }
                    else
                    {
                        // Handle the case where the audit operation failed
                        ModelState.AddModelError("", "Failed to audit account closure request.");
                    }
                }
                catch (Exception)
                {
                    // Log the exception (ex) if necessary
                    ModelState.AddModelError("", "An error occurred while processing your request.");
                }
            }

            PopulateViewBags(accountClosureRequestDTO);
            return View(accountClosureRequestDTO);
        }


        // Method to determine the audit option (provide your implementation)
        private int GetAuditOption(AccountClosureRequestDTO dto)
        {
            // Your logic to determine the audit option
            return 0; // Example placeholder value
        }


        public async Task<ActionResult> Approval(Guid id)
        {
            await ServeNavigationMenus();
            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);
            PopulateViewBags(accountClosureRequestDTO);
            return View(accountClosureRequestDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approval(Guid id, AccountClosureRequestDTO accountClosureRequestDTO)
        {
            int accountClosureApprovalOption = 0;

            if (ModelState.IsValid)
            {
                try
                {
                    await _channelService.ApproveAccountClosureRequestAsync(accountClosureRequestDTO, accountClosureApprovalOption, GetServiceHeader());

                    // Set a success message in TempData
                    TempData["SuccessMessage"] = "Account closure request approved successfully.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Log the exception if needed
                    Debug.WriteLine($"Error approving account closure request: {ex.Message}");

                    // Set an error message in TempData
                    TempData["ErrorMessage"] = "An error occurred while approving the account closure request. Please try again.";
                }
            }
            else
            {
                // Set an error message if the model state is not valid
                TempData["ErrorMessage"] = "There were validation errors. Please review the form and try again.";
            }

            // Repopulate the view bags and return the view with the model
            PopulateViewBags(accountClosureRequestDTO);
            return View(accountClosureRequestDTO);
        }

        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();
            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);
            PopulateViewBags(accountClosureRequestDTO);


            return View(accountClosureRequestDTO);

      
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, AccountClosureRequestDTO accountClosureRequestDTO)
        {
            int auditAccountClosureRequestAsync = 1;

            if (!accountClosureRequestDTO.HasErrors)
            {
                try
                {
                    await _channelService.AuditAccountClosureRequestAsync(accountClosureRequestDTO, auditAccountClosureRequestAsync, GetServiceHeader());

                    // Set a success message in TempData
                    TempData["SuccessMessage"] = "Account closure request verified successfully.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Log the exception if needed
                    Debug.WriteLine($"Error verifying account closure request: {ex.Message}");

                    // Set an error message in TempData
                    TempData["ErrorMessage"] = "An error occurred while verifying the account closure request. Please try again.";
                }
            }
            else
            {
                // Set an error message if there are validation errors
                TempData["ErrorMessage"] = "There were validation errors. Please review the form and try again.";
            }

            // Repopulate the view bags and return the view with the model
            PopulateViewBags(accountClosureRequestDTO);
            return View(accountClosureRequestDTO);
        }

        // Action to handle Customer Look Up
        public async Task<JsonResult> CustomerLookUp(Guid customerId)
        {
            if (customerId == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid customer ID" }, JsonRequestBehavior.AllowGet);
            }

            var CustomerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(customerId, true, true, true, true, GetServiceHeader());

            if (CustomerAccounts != null)
            {
                ViewBag.CustomerAccounts = CustomerAccounts;
                return Json(new { success = true, accounts = CustomerAccounts }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = false, message = "The selected customer has no associated/attached accounts" }, JsonRequestBehavior.AllowGet);
            }
        }

        private void PopulateViewBags(AccountClosureRequestDTO accountClosureRequestDTO)
        {
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(accountClosureRequestDTO.CustomerAccountCustomerType.ToString());
        }

        private AccountClosureRequestDTO PopulateAccountClosureDTO(AccountClosureRequestDTO dto, CustomerAccountDTO customerAccount)
        {
            dto.CustomerAccountCustomerId = customerAccount.Id;
            dto.CustomerAccountId = customerAccount.Id;
            dto.CustomerAccountCustomerIndividualFirstName = customerAccount.FullAccountNumber;
            dto.CustomerAccountCustomerIndividualPayrollNumbers = customerAccount.CustomerIndividualPayrollNumbers;
            dto.CustomerAccountCustomerSerialNumber = customerAccount.CustomerSerialNumber;
            dto.CustomerAccountCustomerIndividualIdentityCardNumber = customerAccount.CustomerIndividualIdentityCardNumber;
            dto.CustomerAccountCustomerIndividualLastName = customerAccount.CustomerIndividualFirstName + " " + customerAccount.CustomerIndividualLastName;
            return dto;
        }
    }
}
