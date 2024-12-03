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
            ViewBag.CustomerAccountProductCodeSelectList = GetCustomerAccountProductCodeSelectList(string.Empty);
            ViewBag.CustomerFilterSelectList = GetCustomerFilterSelectList(string.Empty);
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
                MessageBox.Show(
                    "An unexpected error occurred. Please try again.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );

                return View("Error");
            }

            var branchId = accountClosureRequestDTO.BranchId;
            var customerAccountId = accountClosureRequestDTO.CustomerAccountId;

            accountClosureRequestDTO.ValidateAll();

            if (!accountClosureRequestDTO.HasErrors)
            {
                await _channelService.AddAccountClosureRequestAsync(accountClosureRequestDTO, GetServiceHeader());

                MessageBox.Show(
                    "Account closure request created successfully.",
                    "Operation Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );

                return RedirectToAction("Index");
            }
            else
            {
                foreach (var error in accountClosureRequestDTO.ErrorMessages)
                {
                    Debug.WriteLine($"- {error}");
                }

                MessageBox.Show(
                    "There were validation errors. Please review the form and try again.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );

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
                    return RedirectToAction("Index");
                }
            }
            else
            {
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

                if (updateSuccess)
                {
                    MessageBox.Show(
                        "Account closure request updated successfully.",
                        "Operation Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification
                    );

                    return RedirectToAction("Index");
                }
                else
                {
                    MessageBox.Show(
                        "Failed to update the account closure request.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );

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
                    return RedirectToAction("Index");
                }
            }
            else
            {
                return RedirectToAction("Index");
            }

            // Return the populated DTO to the view for editing
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

                    MessageBox.Show(
                             closureApproveAction == "Approve"
                                 ? "Account closure request approved successfully."
                                 : "Account closure request deferred successfully.",
                             "Success",
                             MessageBoxButtons.OK,
                             MessageBoxIcon.Information
                         );
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing account closure request: {ex.Message}");
                    MessageBox.Show(
                        "An error occurred while processing the account closure request. Please try again.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            else
            {
                MessageBox.Show(
                           "Failed to process the account closure request. Please try again.",
                           "Error",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Error
                       );
            }

            return View("Index");
        }






        public async Task<ActionResult> Verify(Guid id)
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
                        MessageBox.Show(
                             closureVerifyAction == "Verify"
                                 ? "Account closure request verified successfully."
                                 : "Account closure request deferred successfully.",
                             "Success",
                             MessageBoxButtons.OK,
                             MessageBoxIcon.Information
                         );
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        MessageBox.Show(
                           "Failed to process the account closure request. Please try again.",
                           "Error",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Error
                       );
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing account closure request: {ex.Message}");
                    MessageBox.Show(
                        "An error occurred while processing the account closure request. Please try again.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            else
            {
                MessageBox.Show(
                    "There were validation errors. Please review the form and try again.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }

            return View(accountClosureRequestDTO);
        }

        public async Task<ActionResult> Settle(Guid id)
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
                    return RedirectToAction("Index");
                }
            }
            else
            {
                return RedirectToAction("Index");
            }

            return View(accountClosureRequestDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Settle(Guid id, AccountClosureRequestDTO accountClosureRequestDTO, string closureSettleAction)
        {
            // Determine the action based on the value of closureVerifyAction
            int accountClosureSettlementOption = closureSettleAction == "Settle"
                ? (int)AccountClosureSettlementOption.Settle
                : (int)AccountClosureSettlementOption.Defer;

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _channelService.SettleAccountClosureRequestAsync(accountClosureRequestDTO, accountClosureSettlementOption, GetServiceHeader());

                    if (result)
                    {
                        MessageBox.Show(
                            closureSettleAction == "Settle"
                                ? "Account closure request Settled successfully."
                                : "Account closure request deferred successfully.",
                            "Success",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        return RedirectToAction("Index");
                    }
                    else
                    {
                        MessageBox.Show(
                            "Failed to process the account closure request. Please try again.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing account closure request: {ex.Message}");
                    MessageBox.Show(
                        "An error occurred while processing the account closure request. Please try again.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            else
            {
                MessageBox.Show(
                    "There were validation errors. Please review the form and try again.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }

            return View(accountClosureRequestDTO);
        }


        //public async Task<ActionResult> CustomerLookUp(Guid? id)
        //{
        //    await ServeNavigationMenus();

            
        //    Guid parseId;

        //    if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
        //    {
        //        return View();
        //    }

        //    var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
        //    if (customer != null)
        //    {
                

        //        var CustomerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(customer.Id, true, true, true, true, GetServiceHeader());

        //        var referees = await _channelService.FindRefereeCollectionByCustomerIdAsync(parseId, GetServiceHeader());
        //        //var managementHistory=await _channelService.

                

               

        //        // Loans Guaranteed
        //        var loansGuaranteed = await _channelService.FindLoanGuarantorsByCustomerIdAsync(parseId, GetServiceHeader());

        //        // Loan Guarantors
        //        List<AccountClosureRequestDTO> loaneeLoanGuarantors = await GetLoanGuarantorsAsync(parseId);
        //        for (int i = 0; i < loaneeLoanGuarantors.Count; i++)
        //        {
        //            var customerDetails = loaneeLoanGuarantors[i];
        //            var customername = await _channelService.FindCustomerAsync(customerDetails.CustomerId, GetServiceHeader());

        //            loaneeLoanGuarantors[i].CustomerIndividualFirstName = customername.IndividualFirstName;
        //            loaneeLoanGuarantors[i].CustomerIndividualLastName = customername.IndividualLastName;

        //            loaneeLoanGuarantors[i].CreatedDate = Convert.ToDateTime(loaneeLoanGuarantors[i].CreatedDate);
        //        }




        //        var loanAccount = CustomerAccounts.Where(w => w.CustomerAccountTypeProductCode == (int)ProductCode.Loan).Sum(e => e.BookBalance);
        //        var InvestmentAccount = CustomerAccounts.Where(w => w.CustomerAccountTypeProductCode == (int)ProductCode.Investment).Sum(e => e.BookBalance);

        //        var savingsCarryForward = CustomerAccounts.Where(w => w.CustomerAccountTypeProductCode == (int)ProductCode.Savings).Sum(e => e.AvailableBalance);
        //        var savingsBalance = (loanAccount + savingsCarryForward);


        //        // Electronic Funds Transfer
        //        // Method to find electronic funds transfer???



        //        List<Tuple<decimal, int>> investmentsBalance = new List<Tuple<decimal, int>>();

        //        var loanAccounts = CustomerAccounts.Where(w => w.CustomerAccountTypeProductCode == (int)ProductCode.Loan);

        //        foreach (var Ids in loanAccounts)
        //        {
        //            var xFactor = await _channelService.ComputeEligibleLoanAppraisalInvestmentsBalanceAsync(parseId, Ids.CustomerAccountTypeTargetProductId);

        //            investmentsBalance.Add(new Tuple<decimal, int>(xFactor, 0));
        //        }

        //        return Json(new
        //        {
        //            success = true,
        //            data = new
        //            {
                       
        //                // Tables Data
        //                CustomerAccounts = CustomerAccounts,
        //                Referees = referees,
        //                StandingOrders = allStandingOrders,
        //                Signatories = allSignatories,
        //                AlternateChannels = allAlternateChannels,
        //                UnclearedCheques = allExternalCheques,
        //                FixedDeposits = allfixedDeposits,
        //                LoanGuarantors = loaneeLoanGuarantors,
        //                LoansGuaranteed = loansGuaranteed,

                       
        //            }
        //        });
        //    }

        //    return Json(new { success = false, message = "Customer not found" });
        //}



         


    }
}