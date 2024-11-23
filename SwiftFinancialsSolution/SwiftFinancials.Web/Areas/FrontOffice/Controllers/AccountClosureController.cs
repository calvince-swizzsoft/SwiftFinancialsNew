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
using System.Windows.Forms;



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

            bool sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            var sortedColumns = jQueryDataTablesModel.GetSortedColumns().Select(s => s.PropertyName).ToList();


            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;


            var pageCollectionInfo = await _channelService.FindAccountClosureRequestsByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                0,
                pageIndex,
                pageSize,
                false,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null)
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;


                var sortedData = pageCollectionInfo.PageCollection
                    .OrderBy(item => sortedColumns.Contains("CreatedDate") ? item.CreatedDate : default(DateTime))
                    .ToList();


                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? sortedData.Count : totalRecordCount;

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
                    items: new List<AccountClosureRequestDTO>(),
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);

            if (accountClosureRequestDTO != null)
            {
                var customer = await _channelService.FindCustomerAccountAsync(
                    accountClosureRequestDTO.CustomerAccountId, 
                    includeBalances: false,
                    includeProductDescription: true,
                    includeInterestBalanceForLoanAccounts: true,
                    considerMaturityPeriodForInvestmentAccounts: false,
                    GetServiceHeader()
                );

                if (customer != null)
                {
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIdentificationNumber;
                    accountClosureRequestDTO.CustomerAccountRemarks = customer.Remarks;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerNonIndividualDescription = customer.CustomerTypeDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerNonIndividualRegistrationNumber = customer.RecordStatusDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                    accountClosureRequestDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                    accountClosureRequestDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerFullName;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerId = customer.CustomerId;
                    accountClosureRequestDTO.BranchId = customer.BranchId;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductChartOfAccountName = customer.StatusDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualLastName = customer.FullAccountNumber;

                    var loanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                        customer.CustomerId,
                        customer.CustomerAccountTypeTargetProductId,
                        includeBalances: true,
                        includeProductDescription: true,
                        includeInterestBalanceForLoanAccounts: true,
                        considerMaturityPeriodForInvestmentAccounts: true,
                        serviceHeader: GetServiceHeader()
                    );
                    decimal totalLoanBalance = loanAccounts.Sum(l => l.BookBalance + l.CarryForwardsBalance);
                    accountClosureRequestDTO.LoanBalance = totalLoanBalance;

                    var investmentProduct = await _channelService.FindInvestmentProductAsync(
                        customer.CustomerId,
                        GetServiceHeader()
                    );
                    decimal totalInvestmentBalance = await _channelService.ComputeEligibleLoanAppraisalInvestmentsBalanceAsync(
                    customer.CustomerId,
                    customer.CustomerAccountTypeTargetProductId,
                    GetServiceHeader()
                    );
                    accountClosureRequestDTO.InvestmentBalance = totalInvestmentBalance;


                    var loanGuarantors = await _channelService.FindLoanGuarantorsByCustomerIdAsync(
                        customer.CustomerId,
                        GetServiceHeader()
                    );

                    accountClosureRequestDTO.NetRefundable = totalInvestmentBalance - totalLoanBalance;

                    ViewBag.LoanAccounts = loanAccounts.Take(3).ToList();
                    ViewBag.InvestmentAccounts = investmentProduct;
                    ViewBag.LoanGuarantors = loanGuarantors.Take(3).ToList();
                }
                else
                {
                    TempData["ErrorMessage"] = "Customer account details could not be found.";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Account closure request could not be found.";
                return RedirectToAction("Index");
            }

            return View(accountClosureRequestDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);

            var accountClosureRequestDTO = new AccountClosureRequestDTO();

            if (id.HasValue && id != Guid.Empty)
            {
                var parseId = id.Value;

                var customer = await _channelService.FindCustomerAccountAsync(
                    parseId,
                    includeBalances: false,
                    includeProductDescription: true,
                    includeInterestBalanceForLoanAccounts: true,
                    considerMaturityPeriodForInvestmentAccounts: false,
                    GetServiceHeader()
                );

                if (customer != null)
                {
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIdentificationNumber;
                    accountClosureRequestDTO.CustomerAccountRemarks = customer.Remarks;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerNonIndividualDescription = customer.CustomerTypeDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerNonIndividualRegistrationNumber = customer.RecordStatusDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                    accountClosureRequestDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                    accountClosureRequestDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerFullName;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerId = customer.CustomerId;
                    accountClosureRequestDTO.BranchId = customer.BranchId;
                    accountClosureRequestDTO.CustomerAccountId = customer.Id;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductChartOfAccountName = customer.CustomerAccountTypeTargetProductChartOfAccountName;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualLastName = customer.FullAccountNumber;
                    accountClosureRequestDTO.CustomerAccountCustomerNonIndividualDescription = customer.CustomerTypeDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerAccountTypeTargetProductId = customer.CustomerAccountTypeTargetProductId;
                    accountClosureRequestDTO.CustomerAccountBranchId = customer.BranchId;
                    accountClosureRequestDTO.BranchDescription = customer.BranchDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerAccountTypeProductCode = customer.CustomerAccountTypeProductCode;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductChartOfAccountId = customer.CustomerAccountTypeTargetProductChartOfAccountId;
                    accountClosureRequestDTO.Status = customer.Status;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductChartOfAccountName = customer.StatusDescription;

                    var loanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                        customer.CustomerId,
                        customer.CustomerAccountTypeTargetProductId,
                        includeBalances: true,
                        includeProductDescription: true,
                        includeInterestBalanceForLoanAccounts: true,
                        considerMaturityPeriodForInvestmentAccounts: true,
                        serviceHeader: GetServiceHeader()
                    );
                    decimal totalLoanBalance = loanAccounts.Sum(l => l.BookBalance + l.CarryForwardsBalance);
                    accountClosureRequestDTO.LoanBalance = totalLoanBalance;

                    var investmentProduct = await _channelService.FindInvestmentProductAsync(
                        customer.CustomerId,
                        GetServiceHeader()
                    );
                    decimal totalInvestmentBalance = await _channelService.ComputeEligibleLoanAppraisalInvestmentsBalanceAsync(
                    customer.CustomerId,
                    customer.CustomerAccountTypeTargetProductId, 
                    GetServiceHeader()
                    );
                    accountClosureRequestDTO.InvestmentBalance = totalInvestmentBalance;


                    var loanGuarantors = await _channelService.FindLoanGuarantorsByCustomerIdAsync(
                        customer.CustomerId,
                        GetServiceHeader()
                    );

                    accountClosureRequestDTO.NetRefundable = totalInvestmentBalance - totalLoanBalance;

                    ViewBag.LoanAccounts = loanAccounts.Take(3).ToList();
                    ViewBag.InvestmentAccounts = investmentProduct;
                    ViewBag.LoanGuarantors = loanGuarantors.Take(3).ToList();
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
            if (accountClosureRequestDTO == null)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                return View("Error");
            }

            var branchId = accountClosureRequestDTO.BranchId;
            var customerAccountId = accountClosureRequestDTO.CustomerAccountId;

            accountClosureRequestDTO.ValidateAll();

            if (!accountClosureRequestDTO.HasErrors)
            {
                await _channelService.AddAccountClosureRequestAsync(accountClosureRequestDTO, GetServiceHeader());
                MessageBox.Show(
                                                              "Operation Success",
                                                              "Customer Receipts",
                                                              MessageBoxButtons.OK,
                                                              MessageBoxIcon.Information,
                                                              MessageBoxDefaultButton.Button1,
                                                              MessageBoxOptions.ServiceNotification
                                                          );


                TempData["SuccessMessage"] = "Account closure request successfully created.";
                return RedirectToAction("Index");
            }
            else
            {
                foreach (var error in accountClosureRequestDTO.ErrorMessages)
                {
                    Debug.WriteLine($"- {error}");
                }

                TempData["ErrorMessage"] = "There were errors in your submission. Please review the form and try again.";
                return View("Index");
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);

            if (accountClosureRequestDTO != null)
            {
                var customer = await _channelService.FindCustomerAccountAsync(
                    accountClosureRequestDTO.CustomerAccountId,  
                    includeBalances: false,
                    includeProductDescription: true,
                    includeInterestBalanceForLoanAccounts: true,
                    considerMaturityPeriodForInvestmentAccounts: false,
                    GetServiceHeader()
                );

                if (customer != null)
                {
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIdentificationNumber;
                    accountClosureRequestDTO.CustomerAccountRemarks = customer.Remarks;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerNonIndividualDescription = customer.CustomerTypeDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerNonIndividualRegistrationNumber = customer.RecordStatusDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                    accountClosureRequestDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                    accountClosureRequestDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerFullName;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerId = customer.CustomerId;
                    accountClosureRequestDTO.BranchId = customer.BranchId;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductChartOfAccountName = customer.StatusDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualLastName = customer.FullAccountNumber;

                    var loanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                        customer.CustomerId,
                        customer.CustomerAccountTypeTargetProductId,
                        includeBalances: true,
                        includeProductDescription: true,
                        includeInterestBalanceForLoanAccounts: true,
                        considerMaturityPeriodForInvestmentAccounts: true,
                        serviceHeader: GetServiceHeader()
                    );
                    decimal totalLoanBalance = loanAccounts.Sum(l => l.BookBalance + l.CarryForwardsBalance);
                    accountClosureRequestDTO.LoanBalance = totalLoanBalance;

                    var investmentProduct = await _channelService.FindInvestmentProductAsync(
                        customer.CustomerId,
                        GetServiceHeader()
                    );
                    decimal totalInvestmentBalance = await _channelService.ComputeEligibleLoanAppraisalInvestmentsBalanceAsync(
                    customer.CustomerId,
                    customer.CustomerAccountTypeTargetProductId,
                    GetServiceHeader()
                    );
                    accountClosureRequestDTO.InvestmentBalance = totalInvestmentBalance;


                    var loanGuarantors = await _channelService.FindLoanGuarantorsByCustomerIdAsync(
                        customer.CustomerId,
                        GetServiceHeader()
                    );

                    accountClosureRequestDTO.NetRefundable = totalInvestmentBalance - totalLoanBalance;

                    ViewBag.LoanAccounts = loanAccounts.Take(3).ToList();
                    ViewBag.InvestmentAccounts = investmentProduct;
                    ViewBag.LoanGuarantors = loanGuarantors.Take(3).ToList();
                }
                else
                {
                    TempData["ErrorMessage"] = "Customer account details could not be found.";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Account closure request could not be found.";
                return RedirectToAction("Index");
            }

            // Return the populated DTO to the view for editing
            return View(accountClosureRequestDTO);
        }

        public async Task<ActionResult> GetCustomerDetails(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);

            var accountClosureRequestDTO = new AccountClosureRequestDTO();

            if (id.HasValue && id != Guid.Empty)
            {
                var parseId = id.Value;

                var customer = await _channelService.FindCustomerAccountAsync(
                    parseId,
                    includeBalances: false,
                    includeProductDescription: true,
                    includeInterestBalanceForLoanAccounts: true,
                    considerMaturityPeriodForInvestmentAccounts: false,
                    GetServiceHeader()
                );

                if (customer != null)
                {
                    // Populate AccountClosureRequestDTO properties here...
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIdentificationNumber;
                    accountClosureRequestDTO.CustomerAccountRemarks = customer.Remarks;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerNonIndividualDescription = customer.CustomerTypeDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerNonIndividualRegistrationNumber = customer.RecordStatusDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                    accountClosureRequestDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                    accountClosureRequestDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerFullName;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerId = customer.CustomerId;
                    accountClosureRequestDTO.BranchId = customer.BranchId;
                    accountClosureRequestDTO.CustomerAccountId = customer.Id;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductChartOfAccountName = customer.CustomerAccountTypeTargetProductChartOfAccountName;
                    accountClosureRequestDTO.CustomerAccountCustomerIndividualLastName = customer.FullAccountNumber;
                    accountClosureRequestDTO.CustomerAccountCustomerNonIndividualDescription = customer.CustomerTypeDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerAccountTypeTargetProductId = customer.CustomerAccountTypeTargetProductId;
                    accountClosureRequestDTO.CustomerAccountBranchId = customer.BranchId;
                    accountClosureRequestDTO.BranchDescription = customer.BranchDescription;
                    accountClosureRequestDTO.CustomerAccountCustomerAccountTypeProductCode = customer.CustomerAccountTypeProductCode;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductChartOfAccountId = customer.CustomerAccountTypeTargetProductChartOfAccountId;
                    accountClosureRequestDTO.Status = customer.Status;
                    accountClosureRequestDTO.CustomerAccountTypeTargetProductChartOfAccountName = customer.StatusDescription;

                    // Retrieve loan accounts
                    var loanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                        customer.CustomerId,
                        customer.CustomerAccountTypeTargetProductId,
                        includeBalances: true,
                        includeProductDescription: true,
                        includeInterestBalanceForLoanAccounts: true,
                        considerMaturityPeriodForInvestmentAccounts: true,
                        serviceHeader: GetServiceHeader()
                    );
                    decimal totalLoanBalance = loanAccounts.Sum(l => l.InterestBalance);
                    accountClosureRequestDTO.LoanBalance = totalLoanBalance;

                    // Retrieve specific investment product related to the customer
                    var investmentProduct = await _channelService.FindInvestmentProductAsync(
                        customer.CustomerAccountTypeTargetProductId,
                        GetServiceHeader()
                    );
                    decimal totalInvestmentBalance = investmentProduct?.PoolAmount ?? 0;
                    accountClosureRequestDTO.InvestmentBalance = totalInvestmentBalance;

                    // Retrieve loan guarantors
                    var loanGuarantors = await _channelService.FindLoanGuarantorsByCustomerIdAsync(
                        customer.CustomerId,
                        GetServiceHeader()
                    );

                    // Calculate net refundable amount
                    accountClosureRequestDTO.NetRefundable = totalInvestmentBalance - totalLoanBalance;

                    // Pass top 3 records for Loan Accounts, Investment Accounts, and Loan Guarantors to the view
                    ViewBag.LoanAccounts = loanAccounts.Take(3).ToList();
                    ViewBag.InvestmentAccounts = investmentProduct;
                    ViewBag.LoanGuarantors = loanGuarantors.Take(3).ToList();
                }
                else
                {
                    TempData["ErrorMessage"] = "Customer account details could not be found.";
                    return View("Edit", accountClosureRequestDTO);
                }
            }

            return View("Edit", accountClosureRequestDTO); 
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, AccountClosureRequestDTO accountClosureRequestDTO)
        {

            try
            {
                bool updateSuccess = await _channelService.UpdateAccountClosureRequestAsync(accountClosureRequestDTO, GetServiceHeader());
                MessageBox.Show(
                                                              "Operation Success",
                                                              "Customer Receipts",
                                                              MessageBoxButtons.OK,
                                                              MessageBoxIcon.Information,
                                                              MessageBoxDefaultButton.Button1,
                                                              MessageBoxOptions.ServiceNotification
                                                          );

                if (updateSuccess)
                {
                    TempData["SuccessMessage"] = "Account closure request updated successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update the account closure request.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Index");
            }

            return View("Index");
        }







        private int GetAuditOption(AccountClosureRequestDTO dto)
        {
            return 0;
        }


        public async Task<ActionResult> Approve(Guid id)
        {
            await ServeNavigationMenus();

            // Retrieve account closure request details
            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);

            if (accountClosureRequestDTO == null)
            {
                TempData["ErrorMessage"] = "Account closure request not found.";
                return RedirectToAction("Index");
            }

            // Assuming the accountClosureRequestDTO contains the necessary customer ID
            var customerId = accountClosureRequestDTO.CustomerAccountCustomerId;

            // Fetch loan accounts
            var loanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                customerId,
                accountClosureRequestDTO.CustomerAccountCustomerAccountTypeTargetProductId,
                includeBalances: true,
                includeProductDescription: true,
                includeInterestBalanceForLoanAccounts: true,
                considerMaturityPeriodForInvestmentAccounts: true,
                serviceHeader: GetServiceHeader()
            );

            // Calculate total loan balance
            decimal totalLoanBalance = loanAccounts.Sum(l => l.InterestBalance);
            accountClosureRequestDTO.LoanBalance = totalLoanBalance;

            // Fetch investment products
            var investmentAccounts = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            decimal totalInvestmentBalance = investmentAccounts.Sum(i => i.PoolAmount);
            accountClosureRequestDTO.InvestmentBalance = totalInvestmentBalance;

            // Calculate net refundable
            accountClosureRequestDTO.NetRefundable = totalInvestmentBalance - totalLoanBalance;

            // Fetch loan guarantors
            var loanGuarantors = await _channelService.FindLoanGuarantorsByCustomerIdAsync(
                customerId,
                GetServiceHeader()
            );

            // Convert collections to list if needed
            var loanAccountsList = loanAccounts.Take(3).ToList();
            var investmentAccountsList = investmentAccounts.Take(3).ToList();
            var loanGuarantorsList = loanGuarantors.Take(3).ToList();

            // Add data to ViewBag
            ViewBag.LoanAccounts = loanAccountsList;
            ViewBag.InvestmentAccounts = investmentAccountsList;
            ViewBag.LoanGuarantors = loanGuarantorsList;

            return View(accountClosureRequestDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approve(Guid id, AccountClosureRequestDTO accountClosureRequestDTO, string closureApproveAction)
        {
            // Check the value of closureApproveAction to set the accountClosureApprovalOption
            int accountClosureApprovalOption = closureApproveAction == "Approve"
                ? (int)AccountClosureApprovalOption.Approve
                : (int)AccountClosureApprovalOption.Defer;

            if (ModelState.IsValid)
            {
                try
                {
                    await _channelService.ApproveAccountClosureRequestAsync(accountClosureRequestDTO, accountClosureApprovalOption, GetServiceHeader());

                    TempData["SuccessMessage"] = closureApproveAction == "Approve"
                        ? "Account closure request approved successfully."
                        : "Account closure deferred successfully.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error approving account closure request: {ex.Message}");

                    TempData["ErrorMessage"] = "An error occurred while approving the account closure request. Please try again.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "There were validation errors. Please review the form and try again.";
            }

            return View("Index");
        }






        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            // Retrieve account closure request details
            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);

            if (accountClosureRequestDTO == null)
            {
                TempData["ErrorMessage"] = "Account closure request not found.";
                return RedirectToAction("Index");
            }

            // Assuming the accountClosureRequestDTO contains the necessary customer ID
            var customerId = accountClosureRequestDTO.CustomerAccountCustomerId;

            // Fetch loan accounts
            var loanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                customerId,
                accountClosureRequestDTO.CustomerAccountCustomerAccountTypeTargetProductId,
                includeBalances: true,
                includeProductDescription: true,
                includeInterestBalanceForLoanAccounts: true,
                considerMaturityPeriodForInvestmentAccounts: true,
                serviceHeader: GetServiceHeader()
            );

            // Calculate total loan balance
            decimal totalLoanBalance = loanAccounts.Sum(l => l.InterestBalance);
            accountClosureRequestDTO.LoanBalance = totalLoanBalance;

            // Fetch investment products
            var investmentAccounts = await _channelService.FindInvestmentProductsAsync(GetServiceHeader());
            decimal totalInvestmentBalance = investmentAccounts.Sum(i => i.PoolAmount);
            accountClosureRequestDTO.InvestmentBalance = totalInvestmentBalance;

            // Calculate net refundable
            accountClosureRequestDTO.NetRefundable = totalInvestmentBalance - totalLoanBalance;

            // Fetch loan guarantors
            var loanGuarantors = await _channelService.FindLoanGuarantorsByCustomerIdAsync(
                customerId,
                GetServiceHeader()
            );

            // Convert collections to list if needed
            var loanAccountsList = loanAccounts.Take(3).ToList();
            var investmentAccountsList = investmentAccounts.Take(3).ToList();
            var loanGuarantorsList = loanGuarantors.Take(3).ToList();

            // Add data to ViewBag
            ViewBag.LoanAccounts = loanAccountsList;
            ViewBag.InvestmentAccounts = investmentAccountsList;
            ViewBag.LoanGuarantors = loanGuarantorsList;

            return View(accountClosureRequestDTO);
        }







        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(Guid id, AccountClosureRequestDTO accountClosureRequestDTO, string closureVerifyAction)
        {
            // Determine the action based on the value of closureVerifyAction
            int auditAccountClosureRequestOption = closureVerifyAction == "Verify"
                ? (int)AccountClosureAuditOption.Audit
                : (int)AccountClosureAuditOption.Defer;

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _channelService.AuditAccountClosureRequestAsync(accountClosureRequestDTO, auditAccountClosureRequestOption, GetServiceHeader());

                    if (result)
                    {
                        TempData["SuccessMessage"] = closureVerifyAction == "Verify"
                            ? "Account closure request verified successfully."
                            : "Account closure request deferred successfully.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to process the account closure request. Please try again.";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing account closure request: {ex.Message}");
                    TempData["ErrorMessage"] = "An error occurred while processing the account closure request. Please try again.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "There were validation errors. Please review the form and try again.";
            }

            return View(accountClosureRequestDTO);
        }






    }
}