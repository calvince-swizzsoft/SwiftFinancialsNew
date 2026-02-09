//using Application.MainBoundedContext.DTO.AccountsModule;
//using Application.MainBoundedContext.DTO.AdministrationModule;
//using Application.MainBoundedContext.DTO.BackOfficeModule;
//using Infrastructure.Crosscutting.Framework.Utils;
//using SwiftFinancials.Presentation.Infrastructure.Models;
//using SwiftFinancials.Presentation.Infrastructure.Util;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Data;
//using System.Data.SqlClient;
//using System.Diagnostics.Metrics;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Web.Http;
//using TestApis.Controllers;
//using TestApis.Models;

//namespace TestApis.Helpers
//{
//    public class LoanOffsetting
//    {        private readonly MasterController master;

//        public static async Task<bool> Create([FromBody] LoanCaseDTO2 loanCaseDTO)
//        {
//            LoanCaseDTO parentLoan = null;
//            decimal outstandingBalance = 0m;
//            master = new MasterController();

//            if (loanCaseDTO.ParentId != Guid.Empty)
//            {
//                parentLoan = await master._channelService.FindLoanCaseAsync(loanCaseDTO.ParentId, serviceHeader);

//                if (parentLoan == null)
//                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Parent loan not found for top-up."));


//                outstandingBalance = parentLoan.TotalLoansBalance = (Math.Max(0, parentLoan.LoanProductLoanBalance));

//                if (loanCaseDTO.AmountApplied <= outstandingBalance)
//                    return Ok(ApiResponse<string>.Fail(
//                        "Error posting this loan.",
//                        $"Applied amount must exceed outstanding balance of {outstandingBalance:N2}."
//                    ));


//                loanCaseDTO.AuditTopUpAmount = outstandingBalance;
//                loanCaseDTO.ParentId = parentLoan.Id;
//                var savingsAccounts = await master._channelService.FindCustomerAccountsByCustomerIdAsync(loanCaseDTO.CustomerId, true, true, true, true, serviceHeader);

//                var memberSavings = savingsAccounts.FirstOrDefault(c => c.CustomerAccountTypeTargetProductDescription == "Member Deposits");
//                CustomerAccountDTO SavingsAccount = new CustomerAccountDTO();
//                BranchDTO branchDTO = new BranchDTO();
//                branchDTO = branches[0];
//                if (memberSavings == null)
//                {

//                    var savingsProductDTO = await master._channelService.FindDefaultSavingsProductAsync(serviceHeader);
//                    SavingsAccount.CustomerId = loanCaseDTO.CustomerId;
//                    SavingsAccount.CustomerAccountTypeProductCode = (int)ProductCode.Savings;
//                    SavingsAccount.CustomerAccountTypeTargetProductId = savingsProductDTO.Id;
//                    SavingsAccount.CustomerAccountTypeTargetProductCode = (int)ProductCode.Savings;
//                    SavingsAccount.Status = (int)CustomerAccountStatus.Normal;
//                    SavingsAccount.CustomerAccountTypeTargetProductIsDefault = true;
//                    SavingsAccount.RecordStatus = (int)RecordStatus.Approved;
//                    SavingsAccount.BranchId = branchDTO.Id;

//                }
//                CustomerAccountDTO customerLoanAccountDTO = null;

//                var customerLoanAccounts = await master._channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(loanCaseDTO.CustomerId, loanCaseDTO.LoanProductId, false, false, false, false, serviceHeader);

//                if (customerLoanAccounts != null && customerLoanAccounts.Any())
//                    customerLoanAccountDTO = customerLoanAccounts.First();
//                else
//                {
//                    var customerAccountDTO = new CustomerAccountDTO
//                    {
//                        BranchId = branch.Id,
//                        CustomerId = loanCaseDTO.CustomerId,
//                        CustomerAccountTypeProductCode = (int)ProductCode.Loan,
//                        CustomerAccountTypeTargetProductId = loanCaseDTO.LoanProductId,
//                        CustomerAccountTypeTargetProductCode = parentLoan.LoanProductCode,
//                        Status = (int)CustomerAccountStatus.Normal,
//                        RecordStatus = (int)RecordStatus.Approved,
//                    };
//                    customerAccountDTO = await master._channelService.AddCustomerAccountAsync(customerAccountDTO, serviceHeader);
//                    //customerAccountDTO = _customerAccountAppService.AddNewCustomerAccount(customerAccountDTO, serviceHeader);
//                    if (customerAccountDTO != null)
//                        customerLoanAccountDTO = customerAccountDTO;
//                }


//                var bankAccount = await master._channelService.FindBankLinkagesAsync();
//                if (bankAccount == null)
//                    return Ok(new { success = false, message = "Bank account not found" });

//                // 7. Post transaction to journal
//                var transactionModel = new CustomerTransactionModel
//                {
//                    BranchId = branch.Id,
//                    TotalValue = outstandingBalance,
//                    SecondaryDescription = "LOAN OFF-SETTING",
//                    PrimaryDescription = "LOAN OFFSETTED FOR LOAN " + loanProduct.Description,
//                    Reference = "LOAN OFFSETTED FOR LOAN " + loanProduct.Description,
//                    DebitCustomerAccount = customerLoanAccountDTO,
//                    DebitCustomerAccountId = customerLoanAccountDTO.Id,
//                    CreditCustomerAccount = SavingsAccount,
//                    CreditCustomerAccountId = SavingsAccount.Id,
//                    DebitChartOfAccountId = bankAccount[0].ChartOfAccountId,
//                    CreditChartOfAccountId = loanProduct.ChartOfAccountId,
//                };

//                var journal = await master._channelService.AddJournalWithCustomerAccountAsync(transactionModel, serviceHeader);

//                if (parentLoan != null)
//                {
//                    ObservableCollection<AttachedLoanDTO> attachedLoanDTO = new ObservableCollection<AttachedLoanDTO>();
//                    var parentAttachedLoans = await master._channelService
//                        .FindAttachedLoansByLoanCaseIdAsync(parentLoan.Id, serviceHeader);

//                    foreach (var al in parentAttachedLoans)
//                    {
//                        al.PrincipalBalance = 0;
//                        al.InterestBalance = 0;
//                        al.CarryForwardsBalance = 0;
//                        attachedLoanDTO.Add(al);
//                    }

//                    await master._channelService.UpdateAttachedLoansByLoanCaseIdAsync(createResult.Id, attachedLoanDTO, serviceHeader);

//                    parentLoan.TotalLoansBalance = 0;
//                    parentLoan.LoanProductLoanBalance = 0;
//                    parentLoan.Status = (int)LoanCaseStatus.Restructured;

//                    await master._channelService.UpdateLoanCaseAsync(parentLoan, serviceHeader);
//                }
//            }
//            return true;

//        }
//    }
//}
