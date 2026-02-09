//using Application.MainBoundedContext.AccountsModule.Services;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Presentation.Infrastructure.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.UI.WebControls;
using TestApis.Helpers;
using static TestApis.Controllers.MemberPortalController;

namespace TestApis.Controllers
{
    public class LoanDisbursementController : ApiController
    {
        private readonly MasterController master;

        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public LoanDisbursementController()
        {
            master = new MasterController();
        }

        [HttpGet]
        [Route("api/LoanDisbursement/Preview/")]
        public async Task<IHttpActionResult> GetLoanDisbursementPreview([FromUri]Guid loanCaseID)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                // ================== LOAD LOAN CASE ==================
                var loanCaseDTOdata = await master._channelService
                    .FindLoanCaseAsync(loanCaseID, serviceHeader);

                if (loanCaseDTOdata == null)
                    return Ok(new { success = false, message = "Loan case not found" });

                var loanCaseDTO = loanCaseDTOdata.MapTo<LoanCaseDTO>();

                // ================== BANK GL ==================
                var bankAccount = await master._channelService
                    .FindBankLinkagesAsync(serviceHeader);
                var bankAccounts = bankAccount?.FirstOrDefault(b => b.BranchDescription.StartsWith("Rubani", StringComparison.OrdinalIgnoreCase));
                // ================== CUSTOMER LOAN ACCOUNT ==================
                var customerLoanAccounts = await master._channelService.FindCustomerAccountsByCustomerIdAsync(
                    loanCaseDTO.CustomerId, true, true, true, true, serviceHeader);

                var customerLoanAccountDTO = customerLoanAccounts?
                    .FirstOrDefault(a => a.CustomerAccountTypeTargetProductId == loanCaseDTO.LoanProductId)
                    ?? null;

                // ================== SAVINGS ACCOUNT ==================
                var savingsProduct = await master._channelService.FindDefaultSavingsProductAsync(serviceHeader);
                var savingsAccounts = await master._channelService
                    .FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                        loanCaseDTO.CustomerId, savingsProduct?.Id ?? Guid.Empty, true, true, true, true, serviceHeader);

                var savingsAccount = savingsAccounts?.FirstOrDefault();

                // ================== CHART OF ACCOUNTS ==================
                var chartOfAccounts = await master._channelService.FindChartOfAccountsAsync(serviceHeader);
                var interestExpenseGl = chartOfAccounts.FirstOrDefault(a => a.AccountCode == 11100005);

                // ================== CALCULATION ==================
                string disbursementType = "Normal Disbursement";
                decimal bankDisbursement = loanCaseDTO.AmountApplied;
                decimal boostPrincipal = 0m;
                decimal boostInterest = 0m;
                decimal topUpAmount = 0m;
                decimal settlementAmount = 0m;

                // Refinance / Parent Loan
                if (loanCaseDTO.ParentId.HasValue && loanCaseDTO.ParentId.Value != Guid.Empty)
                {
                    disbursementType = "Refinance";
                    var parentLoan = await master._channelService.FindLoanCaseAsync(loanCaseDTO.ParentId.Value, serviceHeader);
                    var parentLoanAccounts = await master._channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                        parentLoan.CustomerId, parentLoan.LoanProductId, true, true, true, true, serviceHeader);

                    var parentLoanAccount = parentLoanAccounts?.FirstOrDefault();

                    decimal parentOutstanding = parentLoanAccount.NewAvailableBalance != 0m
                        ? parentLoanAccount.NewAvailableBalance
                        : parentLoanAccount.BookBalance != 0m
                            ? parentLoanAccount.BookBalance
                            : parentLoanAccount.InterestBalance;

                    settlementAmount = Math.Min(loanCaseDTO.AmountApplied, parentOutstanding);

                    topUpAmount = loanCaseDTO.AmountApplied - settlementAmount;
                    bankDisbursement= loanCaseDTO.AmountApplied - settlementAmount;
                }

                // Boosted Flow
                else if (string.Equals(loanCaseDTO.Remarks?.Trim(), "Boosted", StringComparison.OrdinalIgnoreCase))
                {
                    disbursementType = "Boosted Disbursement";
                    if (!decimal.TryParse(loanCaseDTO.Reference, out boostPrincipal))
                        boostPrincipal = 0m;

                    boostInterest = Math.Round(boostPrincipal * 0.05m, 2);
                    bankDisbursement = loanCaseDTO.AmountApplied - (boostPrincipal + boostInterest);
                }

                // ================== RESULT ==================
                var result = new
                {
                    LoanCase = loanCaseDTO.LoanProductDescription,
                    Memberfullname = loanCaseDTO.CustomerIndividualFirstName+ " " +loanCaseDTO.CustomerIndividualLastName,
                    LoanAmountApplied = loanCaseDTO.AmountApplied,
                    DisbursementType = disbursementType,
                    BankDisbursement = bankDisbursement,
                    BoostPrincipal = boostPrincipal,
                    BoostInterest = boostInterest,
                    SettlementAmount = settlementAmount,
                    TopUpAmount = topUpAmount,
                    AffectedGLs = new
                    {
                        LoanAccountGL = customerLoanAccountDTO?.CustomerAccountTypeTargetProductChartOfAccountName,
                        BankGL = bankAccounts?.BankName,
                        SavingsGL = savingsAccount?.CustomerAccountTypeTargetProductChartOfAccountName,
                        InterestExpenseGL = interestExpenseGl?.AccountName
                    }
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpPost]
        [Route("api/LoanDisbursement")]
        public async Task<IHttpActionResult> PostLoanHybrid(LoanDisbursementRequest loanDisbursementRequest)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();
                serviceHeader.ApplicationUserName = loanDisbursementRequest.DisbursedBy;

                // ================== LOAD LOAN CASE ==================
                var loanCaseDTOdata = await master._channelService
                    .FindLoanCaseAsync(loanDisbursementRequest.loanCaseID, serviceHeader);

                if (loanCaseDTOdata == null)
                    return Ok(new { success = false, message = "Loan case not found" });

                var loanproduct = await master._channelService
                    .FindLoanProductAsync(loanCaseDTOdata.LoanProductId, serviceHeader);

                if (loanproduct == null)
                    return Ok(new { success = false, message = "Loan product not found" });

                var loanCaseDTO = loanCaseDTOdata.MapTo<LoanCaseDTO>();
                loanCaseDTO.DisbursedDate = loanDisbursementRequest.DisbursmentDate;

                // ================== POSTING PERIOD ==================
                var postingPeriod = await master._channelService.FindCurrentPostingPeriodAsync(serviceHeader);
                if (postingPeriod == null)
                    return Ok(new { success = false, message = "Posting period not found" });

                // ================== BANK GL ==================
                var bankAccount = await master._channelService
                    .FindBankLinkageAsync(loanDisbursementRequest.BankAccountId, serviceHeader);

                if (bankAccount == null || bankAccount.ChartOfAccountId == null)
                    return Ok(new { success = false, message = "Bank GL not configured" });

                // ================== CUSTOMER LOAN ACCOUNT ==================
                var customerLoanAccounts = await master._channelService.FindCustomerAccountsByCustomerIdAsync(loanCaseDTO.CustomerId, true, true, true, true, serviceHeader);

                var customerLoanAccountDTO = customerLoanAccounts?.FirstOrDefault(a => a.CustomerAccountTypeTargetProductId == loanCaseDTO.LoanProductId && a.CustomerAccountTypeProductCode == 2) ?? null;

                // ================== AUTO-PROVISION IF MISSING ==================
                if (customerLoanAccountDTO == null)
                {
                    var createAccountRequest = new CustomerAccountDTO
                    {
                        CustomerId = loanCaseDTO.CustomerId,
                        BranchId = loanCaseDTO.BranchId,
                        Status = (int)CustomerAccountStatus.Normal,
                        RecordStatus = (int)RecordStatus.Approved,
                        CustomerAccountTypeProductCode = (int)ProductCode.Loan,
                        CustomerAccountTypeTargetProductId = loanCaseDTO.LoanProductId,
                        Remarks = "Auto-created during loan disbursement",
                    };

                    var createAccountResult = await master._channelService
                        .AddCustomerAccountAsync(createAccountRequest, serviceHeader);

                    if (createAccountResult == null)
                        return Ok(new { success = false, message = "Failed to auto-create customer loan account" });

                    // Re-fetch after creation to ensure full hydrated DTO
                    customerLoanAccounts = await master._channelService
                        .FindCustomerAccountsByCustomerIdAsync(
                            loanCaseDTO.CustomerId, true, true, true, true, serviceHeader);

                    customerLoanAccountDTO =
                        customerLoanAccounts?.FirstOrDefault(a =>
                            a.CustomerAccountTypeTargetProductId == loanCaseDTO.LoanProductId)
                        ?? null;

                    if (customerLoanAccountDTO == null)
                        return Ok(new { success = false, message = "Customer loan account still not found after creation" });
                }

                // ================== BRANCH ==================
                var branches = await master._channelService.FindBranchesAsync(serviceHeader);
                var branchDTO = branches?
                    .FirstOrDefault(b => b.Description.StartsWith("Rubani", StringComparison.OrdinalIgnoreCase))
                    ?? branches?.FirstOrDefault();

                if (branchDTO == null)
                    return Ok(new { success = false, message = "Branch not found" });

                // ================== SAVINGS ACCOUNT ==================
                var savingsProduct = await master._channelService.FindDefaultSavingsProductAsync(serviceHeader);

                if (savingsProduct == null)
                    return Ok(new { success = false, message = "Default savings product not configured" });

                var savingsAccounts = await master._channelService
                    .FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                        loanCaseDTO.CustomerId, savingsProduct.Id, true, true, true, true, serviceHeader);

                var savingsAccount = savingsAccounts?.FirstOrDefault();

                if (savingsAccount == null)
                    return Ok(new { success = false, message = "Member savings account not found" });

                // ================== BOOSTED FLOW ==================
                if (string.Equals(loanCaseDTO.Remarks?.Trim(), "Boosted", StringComparison.OrdinalIgnoreCase))
                {
                    // ===== GLs =====
                    var chartOfAccounts = await master._channelService.FindChartOfAccountsAsync(serviceHeader);

                    var interestExpenseGl = chartOfAccounts.FirstOrDefault(a => a.AccountCode == 11100005);
                    if (interestExpenseGl == null)
                        return Ok(new { success = false, message = "Interest expense GL not configured" });

                    var savingsControlGl = savingsAccount.CustomerAccountTypeTargetProductChartOfAccountId;
                    if (savingsControlGl == null)
                        return Ok(new { success = false, message = "Savings control GL not configured" });

                    // ===== Amount Split =====
                    decimal totalLoan = loanCaseDTO.AmountApplied;

                    if (!decimal.TryParse(loanCaseDTO.Reference, out decimal boostPrincipal))
                        return Ok(new { success = false, message = "Invalid boost amount reference" });

                    decimal boostInterest = Math.Round(boostPrincipal * 0.05m, 2);
                    decimal bankDisbursement = totalLoan - (boostPrincipal + boostInterest);

                    if (bankDisbursement < 0)
                        return Ok(new { success = false, message = "Boost exceeds loan amount" });

                    // ===== 1) Boost Principal: Loan -> Savings =====
                    var boostPrincipalTxn = new CustomerTransactionModel
                    {
                        BranchId = branchDTO.Id,
                        PostingPeriodId = postingPeriod.Id,
                        ValueDate = DateTime.UtcNow,
                        TotalValue = boostPrincipal,
                        PrimaryDescription = "Savings Boost - Member Portion",
                        SecondaryDescription = "Deposited to member savings account",
                        Reference = loanCaseDTO.Reference,

                        DebitCustomerAccount = customerLoanAccountDTO,
                        CreditCustomerAccount = savingsAccount,

                        DebitChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId,
                        CreditChartOfAccountId = savingsControlGl
                    };

                    var r1 = await master._channelService.AddJournalWithCustomerAccountAsync(boostPrincipalTxn, serviceHeader);
                    if (r1.HasErrors)
                        return Ok(new { success = false, message = "Boost principal posting failed" });

                    // ===== 2) Boost Interest: Interest Expense GL -> Savings =====
                    if (boostInterest > 0)
                    {
                        var interestTxn = new CustomerTransactionModel
                        {
                            BranchId = branchDTO.Id,
                            PostingPeriodId = postingPeriod.Id,
                            ValueDate = DateTime.UtcNow,
                            TotalValue = boostInterest,
                            PrimaryDescription = "Savings Boost - Interest Incentive",
                            Reference = loanCaseDTO.Reference,

                            DebitCustomerAccount = customerLoanAccountDTO,
                            CreditCustomerAccount = customerLoanAccountDTO,

                            DebitChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId,
                            CreditChartOfAccountId = interestExpenseGl.Id
                        };

                        var r2 = await master._channelService.AddJournalWithCustomerAccountAsync(interestTxn, serviceHeader);
                        if (r2.HasErrors)
                            return Ok(new { success = false, message = "Boost interest posting failed" });
                    }

                    // ===== 3) Bank Disbursement: Loan -> Bank =====
                    if (bankDisbursement > 0)
                    {
                        var bankTxn = new CustomerTransactionModel
                        {
                            BranchId = branchDTO.Id,
                            PostingPeriodId = postingPeriod.Id,
                            ValueDate = DateTime.UtcNow,
                            TotalValue = bankDisbursement,
                            PrimaryDescription = "Loan Disbursement - Bank",
                            SecondaryDescription = $"BC {bankAccount.BankName}",
                            Reference = customerLoanAccountDTO.CustomerReference1,

                            DebitCustomerAccount = customerLoanAccountDTO,
                            DebitChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId,
                            CreditCustomerAccount = customerLoanAccountDTO,
                            CreditChartOfAccountId = bankAccount.ChartOfAccountId
                        };

                        var r3 = await master._channelService.AddJournalWithCustomerAccountAsync(bankTxn, serviceHeader);
                        if (r3.HasErrors)
                            return Ok(new { success = false, message = "Bank disbursement posting failed" });
                    }

                    var loanDisbursementBatchEntry = new LoanDisbursementBatchEntryDTO
                    {
                        LoanCaseId = loanCaseDTO.Id,
                        LoanCaseApprovedAmount = bankDisbursement,
                        LoanCaseAuditTopUpAmount = 0,
                        LoanCaseMonthlyPaybackAmount = 0,
                        LoanDisbursementBatchReference = "Successfully disbursed"
                    };

                    loanCaseDTO.Status = (int)LoanCaseStatus.Disbursed;
                    await master._channelService.MarkLoanCaseDisbursed(loanDisbursementBatchEntry, serviceHeader);

                    // ================== SMS ==================
                    string message =
                        $"Dear {loanCaseDTO.Customer.IndividualFirstName} {loanCaseDTO.Customer.IndividualLastName}, " +
                        $"your loan of KES {loanCaseDTO.AmountApplied:N0} has been successfully disbursed.";

                    await SmsHelper.SendMessageAsync(loanCaseDTO.Customer.AddressMobileLine, message);

                }

                else if (loanCaseDTO.ParentId.HasValue && loanCaseDTO.ParentId.Value != Guid.Empty)
                {
                    // ===== FETCH PARENT LOAN =====
                    var parentLoan = await master._channelService.FindLoanCaseAsync(loanCaseDTO.ParentId.Value, serviceHeader);
                    if (parentLoan == null)
                        return Ok(new { success = false, message = "Parent loan not found" });

                    var parentLoanAccounts = await master._channelService
                        .FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                            parentLoan.CustomerId, parentLoan.LoanProductId, true, true, true, true, serviceHeader);

                    var parentLoanAccount = parentLoanAccounts?.FirstOrDefault();
                    if (parentLoanAccount == null)
                        return Ok(new { success = false, message = "Parent loan account not found" });

                    if (parentLoanAccount.CustomerId != customerLoanAccountDTO.CustomerId)
                        return Ok(new { success = false, message = "Parent loan does not belong to same member" });

                    // ===== CALCULATE AMOUNTS =====
                    decimal parentOutstanding = parentLoanAccount.BookBalance;
                    decimal newLoanAmount = loanCaseDTO.AmountApplied;

                    // 1️⃣ Settle parent loan fully
                    decimal settlementAmount = parentOutstanding;

                    // 2️⃣ Top-up for customer (cannot be negative)
                    decimal topUpAmount = Math.Max(newLoanAmount - parentOutstanding, 0);

                    // 3️⃣ Monthly interest for parent loan
                    decimal monthlyRate = (decimal)parentLoan.LoanInterestAnnualPercentageRate / 100m / 12m;
                    decimal interestPortion = Math.Round(parentOutstanding * monthlyRate, 2, MidpointRounding.AwayFromZero);

                    // ===== 1) POST INTEREST =====
                    var interestTxn = new CustomerTransactionModel
                    {
                        BranchId = branchDTO.Id,
                        PostingPeriodId = postingPeriod.Id,
                        ValueDate = DateTime.UtcNow,
                        TotalValue = interestPortion,
                        PrimaryDescription = "Loan Interest Payment",
                        SecondaryDescription = $"Interest on Parent Loan {parentLoan.CaseNumber}",
                        Reference = parentLoan.CaseNumber.ToString(),

                        DebitCustomerAccount = customerLoanAccountDTO, // new loan
                        CreditCustomerAccount = parentLoanAccount,

                        DebitChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId,
                        CreditChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductInterestReceivedChartOfAccountId
                    };

                    var interestPostingResult = await master._channelService.AddJournalWithCustomerAccountAsync(interestTxn, serviceHeader);
                    if (interestPostingResult.HasErrors)
                        return Ok(new { success = false, message = "Interest posting failed" });

                    // ===== 2) SETTLE PARENT LOAN =====
                    var settleTxn = new CustomerTransactionModel
                    {
                        BranchId = branchDTO.Id,
                        PostingPeriodId = postingPeriod.Id,
                        ValueDate = DateTime.UtcNow,
                        TotalValue = settlementAmount,
                        PrimaryDescription = "Loan Refinance - Parent Loan Settlement",
                        SecondaryDescription = $"parent loancase{parentLoan.Id}",
                        Reference = parentLoan.CaseNumber.ToString(),

                        DebitCustomerAccount = customerLoanAccountDTO, // new loan
                        CreditCustomerAccount = parentLoanAccount,     // old loan

                        DebitChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId,
                        CreditChartOfAccountId = parentLoanAccount.CustomerAccountTypeTargetProductChartOfAccountId
                    };

                    var settlePostingResult = await master._channelService.AddJournalWithCustomerAccountAsync(settleTxn, serviceHeader);
                    if (settlePostingResult.HasErrors)
                        return Ok(new { success = false, message = "Parent loan settlement posting failed" });

                    // ===== 3) TOP-UP DISBURSEMENT (IF ANY) =====
                    if (topUpAmount > 0)
                    {
                        var disburseTxn = new CustomerTransactionModel
                        {
                            BranchId = branchDTO.Id,
                            PostingPeriodId = postingPeriod.Id,
                            ValueDate = DateTime.UtcNow,
                            TotalValue = topUpAmount,
                            PrimaryDescription = "Loan Disbursement",
                            SecondaryDescription = $"BC {bankAccount.BankName}",
                            Reference = loanCaseDTO.CaseNumber.ToString(),

                            DebitCustomerAccount = customerLoanAccountDTO,
                            DebitChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId,

                            CreditCustomerAccount = customerLoanAccountDTO,
                            CreditChartOfAccountId = bankAccount.ChartOfAccountId
                        };

                        var disbursementResult = await master._channelService.AddJournalWithCustomerAccountAsync(disburseTxn, serviceHeader);
                        if (disbursementResult.HasErrors)
                            return Ok(new { success = false, message = "Top-up disbursement posting failed" });
                    }

                    // ===== 4) MARK NEW LOAN DISBURSED =====
                    var loanDisbursementBatchEntry = new LoanDisbursementBatchEntryDTO
                    {
                        LoanCaseId = loanCaseDTO.Id,
                        LoanCaseApprovedAmount = newLoanAmount,
                        LoanCaseMonthlyPaybackAmount = loanCaseDTO.MonthlyPaybackAmount,
                        LoanDisbursementBatchReference = "Successfully disbursed"
                    };

                    loanCaseDTO.Status = (int)LoanCaseStatus.Disbursed;
                    await master._channelService.MarkLoanCaseDisbursed(loanDisbursementBatchEntry, serviceHeader);

                    // ===== 5) SEND SMS =====
                    string message =
                        $"Dear {loanCaseDTO.Customer.IndividualFirstName} {loanCaseDTO.Customer.IndividualLastName}, " +
                        $"your loan of KES {loanCaseDTO.AmountApplied:N0} has been successfully disbursed.";
                    await SmsHelper.SendMessageAsync(loanCaseDTO.Customer.AddressMobileLine, message);

                    // ===== 6) UPDATE PARENT LOAN STATUS =====
                    parentLoan.Status = (int)LoanCaseStatus.Restructured;
                    await UpdateLoanCaseStatusAsync(parentLoan.Id, parentLoan.Status);
                }

                else
                {
                    // ================== NORMAL DISBURSEMENT ==================
                    var disbursementTxn = new CustomerTransactionModel
                    {
                        BranchId = branchDTO.Id,
                        PostingPeriodId = postingPeriod.Id,
                        ValueDate = DateTime.UtcNow,
                        TotalValue = loanCaseDTO.AmountApplied,
                        PrimaryDescription = "Loan Disbursement",
                        SecondaryDescription = $"BC {bankAccount.BankName}",
                        Reference = loanCaseDTO.CaseNumber.ToString(),

                        DebitCustomerAccount = customerLoanAccountDTO,
                        DebitChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId,
                        CreditCustomerAccount = customerLoanAccountDTO,
                        CreditChartOfAccountId = bankAccount.ChartOfAccountId,
                    };

                    var r = await master._channelService.AddJournalWithCustomerAccountAsync(disbursementTxn, serviceHeader);
                    if (r.HasErrors)
                        return Ok(new { success = false, message = "Loan disbursement failed" });

                }

                // ================== MARK DISBURSED ==================
                var loanDisbursementBatchEntryDTO = new LoanDisbursementBatchEntryDTO
                {
                    LoanCaseId = loanCaseDTO.Id,
                    LoanCaseApprovedAmount = loanCaseDTO.ApprovedAmount,
                    LoanCaseAuditTopUpAmount = 0,
                    LoanCaseMonthlyPaybackAmount = 0,
                    LoanDisbursementBatchReference = "Successfully disbursed"
                };

                loanCaseDTO.Status = (int)LoanCaseStatus.Disbursed;
                await master._channelService.MarkLoanCaseDisbursed(loanDisbursementBatchEntryDTO, serviceHeader);

                // ================== SMS ==================
                string messageText =
                    $"Dear {loanCaseDTO.Customer.IndividualFirstName} {loanCaseDTO.Customer.IndividualLastName}, " +
                    $"your loan of KES {loanCaseDTO.AmountApplied:N0} has been successfully disbursed.";

                await SmsHelper.SendMessageAsync(loanCaseDTO.Customer.AddressMobileLine, messageText);


                return Ok(new { success = true, message = "Loan disbursed successfully" });
                // await UpdateLoanCaseSnapshotAsync(loanCaseDTO);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public class LoanDisbursementRequest
        {
            public int caseNumber { get; set; }
            public Guid loanCaseID { get; set; }
            public string DisbursedBy { get; set; }
            public DateTime DisbursmentDate { get; set; }

            public Guid BankAccountId { get; set; }
        }
        private async Task UpdateLoanCaseStatusAsync(Guid loanCaseId, int status)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (var tx = conn.BeginTransaction())
                using (var cmd = new SqlCommand(@"
UPDATE swiftFin_LoanCases
SET Status = @Status
WHERE Id = @Id
", conn, tx))
                {
                    cmd.Parameters.Add("@Status", SqlDbType.Int).Value = status;
                    cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = loanCaseId;

                    var rows = await cmd.ExecuteNonQueryAsync();
                    if (rows != 1)
                        throw new Exception("Status update failed — row count mismatch.");

                    tx.Commit();
                }
            }
        }


        private async Task UpdateLoanCaseSnapshotAsync(LoanCaseDTO loanCaseDTO)
        {
            using (var conn = new SqlConnection())
            {
                await conn.OpenAsync();

                using (var tx = conn.BeginTransaction(_connectionString))
                using (SqlCommand updateCmd = new SqlCommand(@"
UPDATE swiftFin_LoanCases
SET
    ApprovedAmount            = @ApprovedAmount,
    ApprovedInterestPayment   = @ApprovedInterestPayment,
    TotalPaybackAmount        = @TotalPaybackAmount,
    TotalLoansBalance         = @TotalLoansBalance,
    MonthlyPaybackAmount      = @MonthlyPaybackAmount,
    AppraisedAmount           = @AppraisedAmount,
    LoanProductLoanBalance    = @LoanProductLoanBalance,
    Status                    = @Status
WHERE Id = @Id
", conn, tx))
                {
                    updateCmd.Parameters.Add("@ApprovedAmount", SqlDbType.Decimal).Value = loanCaseDTO.ApprovedAmount;
                    updateCmd.Parameters.Add("@ApprovedInterestPayment", SqlDbType.Decimal).Value = loanCaseDTO.ApprovedInterestPayment;
                    updateCmd.Parameters.Add("@TotalPaybackAmount", SqlDbType.Decimal).Value = loanCaseDTO.TotalPaybackAmount;
                    updateCmd.Parameters.Add("@TotalLoansBalance", SqlDbType.Decimal).Value = loanCaseDTO.TotalLoansBalance;
                    updateCmd.Parameters.Add("@MonthlyPaybackAmount", SqlDbType.Decimal).Value = loanCaseDTO.MonthlyPaybackAmount;
                    updateCmd.Parameters.Add("@AppraisedAmount", SqlDbType.Decimal).Value = loanCaseDTO.AppraisedAmount;
                    updateCmd.Parameters.Add("@LoanProductLoanBalance", SqlDbType.Decimal).Value = loanCaseDTO.LoanProductLoanBalance;
                    updateCmd.Parameters.Add("@Status", SqlDbType.Int).Value = loanCaseDTO.Status;
                    updateCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = loanCaseDTO.Id;

                    var rows = await updateCmd.ExecuteNonQueryAsync();
                    if (rows != 1)
                        throw new Exception("Snapshot update failed — row count mismatch.");

                    tx.Commit();
                }
            }
        }

    }
}