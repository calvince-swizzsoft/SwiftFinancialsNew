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
                0,
                int.MaxValue,
                false,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(accountClosureRequestDTO => accountClosureRequestDTO.CreatedDate)
                    .ToList();

                totalRecordCount = sortedData.Count;

                var paginatedData = sortedData
                    .Skip(jQueryDataTablesModel.iDisplayStart)
                    .Take(jQueryDataTablesModel.iDisplayLength)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? sortedData.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: paginatedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }

            return this.DataTablesJson(
                items: new List<AccountClosureRequestDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
        );
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
            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.CustomerFilterSelectList = GetCustomerFilterSelectList(string.Empty);

            
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

                    accountClosureRequestDTO.NetRefundable = Math.Abs(totalInvestmentBalance) - Math.Abs(totalLoanBalance);

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
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "An unexpected error occurred. Please try again.";
                return RedirectToAction("Error");
            }

            var branchId = accountClosureRequestDTO.BranchId;
            var customerAccountId = accountClosureRequestDTO.CustomerAccountId;

            accountClosureRequestDTO.ValidateAll();

            if (!accountClosureRequestDTO.HasErrors)
            {
                await _channelService.AddAccountClosureRequestAsync(accountClosureRequestDTO, GetServiceHeader());

                TempData["AlertType"] = "success";
                TempData["AlertMessage"] = "Account closure request created successfully.";
                return RedirectToAction("Index");
            }
            else
            {
                foreach (var error in accountClosureRequestDTO.ErrorMessages)
                {
                    Debug.WriteLine($"- {error}");
                }

                TempData["AlertType"] = "warning";
                TempData["AlertMessage"] = "There were validation errors. Please review the form and try again.";
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
                    TempData["AlertType"] = "success";
                    TempData["AlertMessage"] = "Account closure request updated successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["AlertType"] = "error";
                    TempData["AlertMessage"] = "Failed to update the account closure request.";
                }
            }
            catch (Exception ex)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = $"An error occurred: {ex.Message}";
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

            return View(accountClosureRequestDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approve(Guid id, AccountClosureRequestDTO accountClosureRequestDTO, string closureApproveAction)
        {
            int accountClosureApprovalOption = closureApproveAction == "Approve"
                ? (int)AccountClosureApprovalOption.Approve
                : (int)AccountClosureApprovalOption.Defer;

            if (ModelState.IsValid)
            {
                try
                {
                    await _channelService.ApproveAccountClosureRequestAsync(accountClosureRequestDTO, accountClosureApprovalOption, GetServiceHeader());

                    TempData["AlertType"] = "success";
                    TempData["AlertMessage"] = closureApproveAction == "Approve"
                        ? "Account closure request approved successfully."
                        : "Account closure request deferred successfully.";

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing account closure request: {ex.Message}");

                    TempData["AlertType"] = "error";
                    TempData["AlertMessage"] = "An error occurred while processing the account closure request. Please try again.";
                }
            }
            else
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Failed to process the account closure request. Please try again.";
            }

            return RedirectToAction("Index");
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
                        TempData["AlertType"] = "success";
                        TempData["AlertMessage"] = closureVerifyAction == "Verify"
                            ? "Account closure request verified successfully."
                            : "Account closure request deferred successfully.";

                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["AlertType"] = "error";
                        TempData["AlertMessage"] = "Failed to process the account closure request. Please try again.";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing account closure request: {ex.Message}");

                    TempData["AlertType"] = "error";
                    TempData["AlertMessage"] = "An error occurred while processing the account closure request. Please try again.";
                }
            }
            else
            {
                TempData["AlertType"] = "warning";
                TempData["AlertMessage"] = "There were validation errors. Please review the form and try again.";
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
            // Determine the action based on the value of closureSettleAction
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
                        TempData["AlertType"] = "success";
                        TempData["AlertMessage"] = closureSettleAction == "Settle"
                            ? "Account closure request settled successfully."
                            : "Account closure request deferred successfully.";

                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["AlertType"] = "error";
                        TempData["AlertMessage"] = "Failed to process the account closure request. Please try again.";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing account closure request: {ex.Message}");
                    TempData["AlertType"] = "error";
                    TempData["AlertMessage"] = "An error occurred while processing the account closure request. Please try again.";
                }
            }
            else
            {
                TempData["AlertType"] = "warning";
                TempData["AlertMessage"] = "There were validation errors. Please review the form and try again.";
            }

            return View(accountClosureRequestDTO);
        }











    }
}