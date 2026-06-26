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
                var customerLoanAccounts = await master._channelService
                    .FindCustomerAccountsByCustomerIdAsync(
                        loanCaseDTO.CustomerId, true, true, true, true, serviceHeader);

                var customerLoanAccountDTO = customerLoanAccounts?
                    .FirstOrDefault(a =>
                        a.CustomerAccountTypeTargetProductId == loanCaseDTO.LoanProductId &&
                        a.CustomerAccountTypeProductCode == 2)
                    ?? null;

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

                    customerLoanAccounts = await master._channelService
                        .FindCustomerAccountsByCustomerIdAsync(
                            loanCaseDTO.CustomerId, true, true, true, true, serviceHeader);

                    customerLoanAccountDTO = customerLoanAccounts?
                        .FirstOrDefault(a =>
                            a.CustomerAccountTypeTargetProductId == loanCaseDTO.LoanProductId)
                        ?? null;

                    if (customerLoanAccountDTO == null)
                        return Ok(new { success = false, message = "Customer loan account still not found after creation" });
                }

                // ================== BRANCH ==================
                var branches = await master._channelService.FindBranchesAsync(serviceHeader);
                var branchDTO = branches?
                    .FirstOrDefault(b =>
                        b.Description.StartsWith("Rubani", StringComparison.OrdinalIgnoreCase))
                    ?? branches?.FirstOrDefault();

                if (branchDTO == null)
                    return Ok(new { success = false, message = "Branch not found" });

                // ================== CALCULATE AMOUNTS ==================
                decimal principal = loanCaseDTO.AmountApplied;
                decimal interest = 0;
                string reference = loanCaseDTO.CaseNumber.ToString();

                // ================== BOOSTED FLOW ==================
                if (string.Equals(loanCaseDTO.Remarks?.Trim(), "Boosted", StringComparison.OrdinalIgnoreCase))
                {
                    if (!decimal.TryParse(loanCaseDTO.Reference, out decimal boostPrincipal))
                        return Ok(new { success = false, message = "Invalid boost amount reference" });

                    decimal boostInterest = Math.Round(boostPrincipal * 0.05m, 2);
                    decimal bankDisbursement = principal - (boostPrincipal + boostInterest);

                    if (bankDisbursement < 0)
                        return Ok(new { success = false, message = "Boost exceeds loan amount" });

                    principal = bankDisbursement;
                    interest = boostInterest;
                    reference = loanCaseDTO.Reference;

                    // Store settlement record for boosted disbursement
                    await InsertSettlementRecordAsync(
                        customerAccountId: customerLoanAccountDTO.Id,
                        principal: principal,
                        interest: interest,
                        carryForwards: boostPrincipal,   // boost principal stored as carry forward
                        reference: reference,
                        createdBy: loanDisbursementRequest.DisbursedBy
                    );

                    // Mark loan disbursed
                    var batchEntry = new LoanDisbursementBatchEntryDTO
                    {
                        LoanCaseId = loanCaseDTO.Id,
                        LoanCaseApprovedAmount = bankDisbursement,
                        LoanCaseAuditTopUpAmount = 0,
                        LoanCaseMonthlyPaybackAmount = 0,
                        LoanDisbursementBatchReference = "Successfully disbursed"
                    };

                    loanCaseDTO.Status = (int)LoanCaseStatus.Disbursed;
                    await master._channelService.MarkLoanCaseDisbursed(batchEntry, serviceHeader);

                    // SMS
                    await SmsHelper.SendMessageAsync(
                        loanCaseDTO.Customer.AddressMobileLine,
                        $"Dear {loanCaseDTO.Customer.IndividualFirstName} {loanCaseDTO.Customer.IndividualLastName}, " +
                        $"your loan of KES {loanCaseDTO.AmountApplied:N0} has been successfully disbursed.");

                    return Ok(new { success = true, message = "Boosted loan disbursed successfully" });
                }

                // ================== REFINANCE / TOP-UP FLOW ==================
                else if (loanCaseDTO.ParentId.HasValue && loanCaseDTO.ParentId.Value != Guid.Empty)
                {
                    var parentLoan = await master._channelService
                        .FindLoanCaseAsync(loanCaseDTO.ParentId.Value, serviceHeader);

                    if (parentLoan == null)
                        return Ok(new { success = false, message = "Parent loan not found" });

                    var parentLoanAccounts = await master._channelService
                        .FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                            parentLoan.CustomerId, parentLoan.LoanProductId,
                            true, true, true, true, serviceHeader);

                    var parentLoanAccount = parentLoanAccounts?.FirstOrDefault();
                    if (parentLoanAccount == null)
                        return Ok(new { success = false, message = "Parent loan account not found" });

                    if (parentLoanAccount.CustomerId != customerLoanAccountDTO.CustomerId)
                        return Ok(new { success = false, message = "Parent loan does not belong to same member" });

                    decimal parentOutstanding = parentLoanAccount.BookBalance;
                    decimal newLoanAmount = loanCaseDTO.AmountApplied;
                    decimal topUpAmount = Math.Max(newLoanAmount - parentOutstanding, 0);
                    decimal monthlyRate = (decimal)parentLoan.LoanInterestAnnualPercentageRate / 100m / 12m;
                    decimal interestPortion = Math.Round(
                                                    parentOutstanding * monthlyRate, 2,
                                                    MidpointRounding.AwayFromZero);

                    // Store settlement record for refinance
                    // principal     = top-up cash out to member
                    // interest      = interest on parent loan
                    // carryForwards = parent outstanding being settled
                    await InsertSettlementRecordAsync(
                        customerAccountId: customerLoanAccountDTO.Id,
                        principal: topUpAmount,
                        interest: interestPortion,
                        carryForwards: parentOutstanding,
                        reference: parentLoan.CaseNumber.ToString(),
                        createdBy: loanDisbursementRequest.DisbursedBy
                    );

                    // Mark new loan disbursed
                    var batchEntry = new LoanDisbursementBatchEntryDTO
                    {
                        LoanCaseId = loanCaseDTO.Id,
                        LoanCaseApprovedAmount = newLoanAmount,
                        LoanCaseMonthlyPaybackAmount = loanCaseDTO.MonthlyPaybackAmount,
                        LoanDisbursementBatchReference = "Successfully disbursed"
                    };

                    loanCaseDTO.Status = (int)LoanCaseStatus.Disbursed;
                    await master._channelService.MarkLoanCaseDisbursed(batchEntry, serviceHeader);

                    // SMS
                    await SmsHelper.SendMessageAsync(
                        loanCaseDTO.Customer.AddressMobileLine,
                        $"Dear {loanCaseDTO.Customer.IndividualFirstName} {loanCaseDTO.Customer.IndividualLastName}, " +
                        $"your loan of KES {loanCaseDTO.AmountApplied:N0} has been successfully disbursed.");

                    // Update parent loan status
                    await UpdateLoanCaseStatusAsync(parentLoan.Id, (int)LoanCaseStatus.Restructured);

                    return Ok(new { success = true, message = "Refinance loan disbursed successfully" });
                }

                // ================== NORMAL DISBURSEMENT ==================
                else
                {
                    // Store settlement record for normal disbursement
                    // principal     = full loan amount
                    // interest      = 0 (no interest at disbursement stage)
                    // carryForwards = 0
                    await InsertSettlementRecordAsync(
                        customerAccountId: customerLoanAccountDTO.Id,
                        principal: principal,
                        interest: 0,
                        carryForwards: 0,
                        reference: reference,
                        createdBy: loanDisbursementRequest.DisbursedBy
                    );

                    // Mark loan disbursed
                    var batchEntry = new LoanDisbursementBatchEntryDTO
                    {
                        LoanCaseId = loanCaseDTO.Id,
                        LoanCaseApprovedAmount = loanCaseDTO.ApprovedAmount,
                        LoanCaseAuditTopUpAmount = 0,
                        LoanCaseMonthlyPaybackAmount = 0,
                        LoanDisbursementBatchReference = "Successfully disbursed"
                    };

                    loanCaseDTO.Status = (int)LoanCaseStatus.Disbursed;
                    await master._channelService.MarkLoanCaseDisbursed(batchEntry, serviceHeader);

                    // SMS
                    await SmsHelper.SendMessageAsync(
                        loanCaseDTO.Customer.AddressMobileLine,
                        $"Dear {loanCaseDTO.Customer.IndividualFirstName} {loanCaseDTO.Customer.IndividualLastName}, " +
                        $"your loan of KES {loanCaseDTO.AmountApplied:N0} has been successfully disbursed.");

                    return Ok(new { success = true, message = "Loan disbursed successfully" });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/GetLoanSettlements")]
        public async Task<IHttpActionResult> GetSettlements(
    [FromUri] bool processed = false,
    [FromUri] int page = 1,
    [FromUri] int pageSize = 20)
        {
            try
            {
                var settlements = new List<object>();
                int totalCount = 0;
                int offset = (page - 1) * pageSize;

                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // ================== TOTAL COUNT ==================
                    using (var countCmd = new SqlCommand(@"
                SELECT COUNT(1)
                FROM [dbo].[swiftFin_MembershipWithdrawalSettlements] s
                INNER JOIN [dbo].[swiftFin_CustomerAccounts] ca ON ca.[Id] = s.[CustomerAccountId]
                INNER JOIN [dbo].[swiftFin_Customers] c         ON c.[Id]  = ca.[CustomerId]
                WHERE s.[WithdrawalNotificationId] IS NULL
                  AND ISNUMERIC(s.[Reference]) = 1
                  AND (
                        (@Processed = 1 AND s.[ProcessedDate] IS NOT NULL)
                     OR (@Processed = 0 AND s.[ProcessedDate] IS NULL)
                  )", conn))
                    {
                        countCmd.Parameters.Add("@Processed", SqlDbType.Bit).Value = processed;
                        totalCount = (int)await countCmd.ExecuteScalarAsync();
                    }

                    // ================== FETCH LOAN SETTLEMENTS ONLY ==================
                    using (var cmd = new SqlCommand(@"
                SELECT
                    s.[Id]                AS SettlementId,
                    s.[CustomerAccountId],
                    s.[Principal],
                    s.[Interest],
                    s.[CarryForwards],
                    s.[Reference],
                    s.[CreatedBy],
                    s.[CreatedDate],
                    s.[ProcessedDate],
                    s.[ProcessedBy],
                    ca.[CustomerId],
                    c.[Individual_FirstName],
                    c.[Individual_LastName],
                    c.[Address_MobileLine],
                    c.[Reference2]        AS MemberNumber,
                    CASE
                        WHEN s.[CarryForwards] > 0 AND s.[Interest] > 0 THEN 'Refinance'
                        WHEN s.[CarryForwards] > 0 AND s.[Interest] = 0 THEN 'Boosted'
                        ELSE 'Normal'
                    END                   AS DisbursementType,
                    (s.[Principal] + s.[Interest] + s.[CarryForwards]) AS TotalAmount
                FROM [dbo].[swiftFin_MembershipWithdrawalSettlements] s
                INNER JOIN [dbo].[swiftFin_CustomerAccounts] ca ON ca.[Id] = s.[CustomerAccountId]
                INNER JOIN [dbo].[swiftFin_Customers] c         ON c.[Id]  = ca.[CustomerId]
                WHERE s.[WithdrawalNotificationId] IS NULL
                  AND ISNUMERIC(s.[Reference]) = 1
                  AND (
                        (@Processed = 1 AND s.[ProcessedDate] IS NOT NULL)
                     OR (@Processed = 0 AND s.[ProcessedDate] IS NULL)
                  )
                ORDER BY s.[CreatedDate] DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", conn))
                    {
                        cmd.Parameters.Add("@Processed", SqlDbType.Bit).Value = processed;
                        cmd.Parameters.Add("@Offset", SqlDbType.Int).Value = offset;
                        cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                settlements.Add(new
                                {
                                    settlementId = new Guid(Convert.ToString(reader["SettlementId"])),
                                    customerAccountId = new Guid(Convert.ToString(reader["CustomerAccountId"])),
                                    customerId = new Guid(Convert.ToString(reader["CustomerId"])),
                                    customerName = $"{Convert.ToString(reader["Individual_FirstName"])} {Convert.ToString(reader["Individual_LastName"])}".Trim(),
                                    memberNumber = Convert.ToString(reader["MemberNumber"]),
                                    mobile = Convert.ToString(reader["Address_MobileLine"]),
                                    principal = Convert.ToDecimal(reader["Principal"] == DBNull.Value ? 0 : reader["Principal"]),
                                    interest = Convert.ToDecimal(reader["Interest"] == DBNull.Value ? 0 : reader["Interest"]),
                                    carryForwards = Convert.ToDecimal(reader["CarryForwards"] == DBNull.Value ? 0 : reader["CarryForwards"]),
                                    totalAmount = Convert.ToDecimal(reader["TotalAmount"] == DBNull.Value ? 0 : reader["TotalAmount"]),
                                    reference = Convert.ToString(reader["Reference"]),
                                    disbursementType = Convert.ToString(reader["DisbursementType"]),
                                    createdBy = Convert.ToString(reader["CreatedBy"]),
                                    createdDate = Convert.ToDateTime(reader["CreatedDate"]),
                                    processedDate = reader["ProcessedDate"] != DBNull.Value
                                                            ? (DateTime?)Convert.ToDateTime(reader["ProcessedDate"])
                                                            : null,
                                    processedBy = reader["ProcessedBy"] != DBNull.Value
                                                            ? Convert.ToString(reader["ProcessedBy"])
                                                            : null,
                                    isProcessed = reader["ProcessedDate"] != DBNull.Value
                                });
                            }
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = $"Found {totalCount} {(processed ? "processed" : "pending")} loan settlements",
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    data = settlements
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = $"Failed to fetch loan settlements: {ex.Message} | Inner: {ex.InnerException?.Message}"
                });
            }
        }




        [HttpPost]
        [Route("api/ProcessDisbursement")]
        public async Task<IHttpActionResult> ProcessDisbursement([FromBody] ProcessDisbursementRequest request)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();
                serviceHeader.ApplicationUserName = request.ProcessedBy;

                // ================== LOAD SETTLEMENT RECORD ==================
                SettlementRecord settlement = null;

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(@"
            SELECT 
                s.[Id],
                s.[WithdrawalNotificationId],
                s.[CustomerAccountId],
                s.[Principal],
                s.[Interest],
                s.[CarryForwards],
                s.[Reference],
                s.[CreatedBy],
                s.[CreatedDate]
            FROM [dbo].[swiftFin_MembershipWithdrawalSettlements] s
            WHERE s.[Id] = @SettlementId", conn))
                {
                    cmd.Parameters.Add("@SettlementId", SqlDbType.UniqueIdentifier).Value = request.SettlementId;

                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!reader.Read())
                            return Ok(new { success = false, message = "Settlement record not found" });

                        settlement = new SettlementRecord
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("Id")),
                            WithdrawalNotificationId = reader["WithdrawalNotificationId"] != DBNull.Value
                                                        ? (Guid?)reader.GetGuid(reader.GetOrdinal("WithdrawalNotificationId"))
                                                        : null,
                            CustomerAccountId = reader.GetGuid(reader.GetOrdinal("CustomerAccountId")),
                            Principal = reader.GetDecimal(reader.GetOrdinal("Principal")),
                            Interest = reader.GetDecimal(reader.GetOrdinal("Interest")),
                            CarryForwards = reader.GetDecimal(reader.GetOrdinal("CarryForwards")),
                            Reference = reader["Reference"] as string,
                            CreatedBy = reader["CreatedBy"] as string,
                            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"))
                        };
                    }
                }

                // ================== CHECK NOT ALREADY PROCESSED ==================
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(@"
            SELECT COUNT(1)
            FROM [dbo].[swiftFin_MembershipWithdrawalSettlements]
            WHERE [Id]            = @SettlementId
              AND [ProcessedDate] IS NOT NULL", conn))
                {
                    cmd.Parameters.Add("@SettlementId", SqlDbType.UniqueIdentifier).Value = request.SettlementId;
                    await conn.OpenAsync();
                    var count = (int)await cmd.ExecuteScalarAsync();
                    if (count > 0)
                        return Ok(new { success = false, message = "Settlement has already been processed" });
                }

                // ================== LOAD SUPPORTING DATA ==================
                var postingPeriod = await master._channelService.FindCurrentPostingPeriodAsync(serviceHeader);
                if (postingPeriod == null)
                    return Ok(new { success = false, message = "Posting period not found" });

                var bankAccount = await master._channelService
                    .FindBankLinkageAsync(request.BankAccountId, serviceHeader);
                if (bankAccount == null || bankAccount.ChartOfAccountId == null)
                    return Ok(new { success = false, message = "Bank GL not configured" });

                var branches = await master._channelService.FindBranchesAsync(serviceHeader);
                var branchDTO = branches?
                    .FirstOrDefault(b =>
                        b.Description.StartsWith("Rubani", StringComparison.OrdinalIgnoreCase))
                    ?? branches?.FirstOrDefault();

                if (branchDTO == null)
                    return Ok(new { success = false, message = "Branch not found" });

                // ================== LOAD CUSTOMER LOAN ACCOUNT ==================
                CustomerAccountDTO customerLoanAccountDTO = null;

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(@"
    SELECT 
        ca.[Id],
        ca.[CustomerId],
        ca.[CustomerAccountType_TargetProductId] AS TargetProductId,
        lp.[ChartOfAccountId],
        lp.[InterestReceivedChartOfAccountId]
    FROM [dbo].[swiftFin_CustomerAccounts] ca
    LEFT JOIN [dbo].[swiftFin_LoanProducts] lp ON lp.[Id] = ca.[CustomerAccountType_TargetProductId]
    WHERE ca.[Id] = @CustomerAccountId", conn))
                {
                    cmd.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value
                        = settlement.CustomerAccountId;

                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!reader.Read())
                            return Ok(new { success = false, message = "Customer loan account not found" });

                        customerLoanAccountDTO = new CustomerAccountDTO
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("Id")),
                            CustomerId = reader.GetGuid(reader.GetOrdinal("CustomerId")),

                            CustomerAccountTypeTargetProductId =
                                reader["TargetProductId"] != DBNull.Value
                                    ? reader.GetGuid(reader.GetOrdinal("TargetProductId"))
                                    : Guid.Empty,

                            CustomerAccountTypeTargetProductChartOfAccountId =
                                reader["ChartOfAccountId"] != DBNull.Value
                                    ? reader.GetGuid(reader.GetOrdinal("ChartOfAccountId"))
                                    : Guid.Empty,

                            CustomerAccountTypeTargetProductInterestReceivedChartOfAccountId =
                                reader["InterestReceivedChartOfAccountId"] != DBNull.Value
                                    ? (Guid)reader["InterestReceivedChartOfAccountId"]
                                    : Guid.Empty
                        };
                    }
                }

                // ================== DETERMINE DISBURSEMENT TYPE ==================
                // CarryForwards > 0 && Interest = 0  → Boosted
                // CarryForwards > 0 && Interest > 0  → Refinance
                // CarryForwards = 0 && Interest = 0  → Normal
                bool isRefinance = settlement.CarryForwards > 0 && settlement.Interest > 0;
                bool isBoosted = settlement.CarryForwards > 0 && settlement.Interest == 0;
                bool isNormal = settlement.CarryForwards == 0 && settlement.Interest == 0;

                var journalErrors = new List<string>();

                // ================== NORMAL DISBURSEMENT ==================
                if (isNormal)
                {
                    var disburseTxn = new CustomerTransactionModel
                    {
                        BranchId = branchDTO.Id,
                        PostingPeriodId = postingPeriod.Id,
                        ValueDate = DateTime.UtcNow,
                        TotalValue = settlement.Principal,
                        PrimaryDescription = "Loan Disbursement",
                        SecondaryDescription = $"BC {bankAccount.BankName}",
                        Reference = settlement.Reference,

                        DebitCustomerAccount = customerLoanAccountDTO,
                        DebitChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId,
                        CreditCustomerAccount = customerLoanAccountDTO,
                        CreditChartOfAccountId = bankAccount.ChartOfAccountId
                    };

                    var result = await master._channelService
                        .AddJournalWithCustomerAccountAsync(disburseTxn, serviceHeader);

                    if (result.HasErrors)
                        journalErrors.Add("Normal disbursement posting failed");
                }

                // ================== BOOSTED DISBURSEMENT ==================
                else if (isBoosted)
                {
                    var savingsProduct = await master._channelService
                        .FindDefaultSavingsProductAsync(serviceHeader);
                    if (savingsProduct == null)
                        return Ok(new { success = false, message = "Default savings product not configured" });

                    var savingsAccounts = await master._channelService
                        .FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(
                            customerLoanAccountDTO.CustomerId, savingsProduct.Id,
                            true, true, true, true, serviceHeader);

                    var savingsAccount = savingsAccounts?.FirstOrDefault();
                    if (savingsAccount == null)
                        return Ok(new { success = false, message = "Member savings account not found" });

                    var chartOfAccounts = await master._channelService.FindChartOfAccountsAsync(serviceHeader);
                    var interestExpenseGl = chartOfAccounts.FirstOrDefault(a => a.AccountCode == 11100005);
                    if (interestExpenseGl == null)
                        return Ok(new { success = false, message = "Interest expense GL not configured" });

                    var savingsControlGl = savingsAccount.CustomerAccountTypeTargetProductChartOfAccountId;
                    if (savingsControlGl == null)
                        return Ok(new { success = false, message = "Savings control GL not configured" });

                    // 1) Boost Principal: Loan -> Savings
                    var boostPrincipalTxn = new CustomerTransactionModel
                    {
                        BranchId = branchDTO.Id,
                        PostingPeriodId = postingPeriod.Id,
                        ValueDate = DateTime.UtcNow,
                        TotalValue = settlement.CarryForwards,
                        PrimaryDescription = "Savings Boost - Member Portion",
                        SecondaryDescription = "Deposited to member savings account",
                        Reference = settlement.Reference,

                        DebitCustomerAccount = customerLoanAccountDTO,
                        CreditCustomerAccount = savingsAccount,
                        DebitChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId,
                        CreditChartOfAccountId = savingsControlGl
                    };

                    var r1 = await master._channelService
                        .AddJournalWithCustomerAccountAsync(boostPrincipalTxn, serviceHeader);
                    if (r1.HasErrors)
                        journalErrors.Add("Boost principal posting failed");

                    // 2) Boost Interest: Interest Expense GL -> Savings
                    if (settlement.Interest > 0 && !journalErrors.Any())
                    {
                        var interestTxn = new CustomerTransactionModel
                        {
                            BranchId = branchDTO.Id,
                            PostingPeriodId = postingPeriod.Id,
                            ValueDate = DateTime.UtcNow,
                            TotalValue = settlement.Interest,
                            PrimaryDescription = "Savings Boost - Interest Incentive",
                            Reference = settlement.Reference,

                            DebitCustomerAccount = customerLoanAccountDTO,
                            CreditCustomerAccount = customerLoanAccountDTO,
                            DebitChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId,
                            CreditChartOfAccountId = interestExpenseGl.Id
                        };

                        var r2 = await master._channelService
                            .AddJournalWithCustomerAccountAsync(interestTxn, serviceHeader);
                        if (r2.HasErrors)
                            journalErrors.Add("Boost interest posting failed");
                    }

                    // 3) Bank Disbursement: Loan -> Bank
                    if (settlement.Principal > 0 && !journalErrors.Any())
                    {
                        var bankTxn = new CustomerTransactionModel
                        {
                            BranchId = branchDTO.Id,
                            PostingPeriodId = postingPeriod.Id,
                            ValueDate = DateTime.UtcNow,
                            TotalValue = settlement.Principal,
                            PrimaryDescription = "Loan Disbursement - Bank",
                            SecondaryDescription = $"BC {bankAccount.BankName}",
                            Reference = settlement.Reference,

                            DebitCustomerAccount = customerLoanAccountDTO,
                            DebitChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId,
                            CreditCustomerAccount = customerLoanAccountDTO,
                            CreditChartOfAccountId = bankAccount.ChartOfAccountId
                        };

                        var r3 = await master._channelService
                            .AddJournalWithCustomerAccountAsync(bankTxn, serviceHeader);
                        if (r3.HasErrors)
                            journalErrors.Add("Bank disbursement posting failed");
                    }
                }

                // ================== REFINANCE DISBURSEMENT ==================
                else if (isRefinance)
                {
                    // 1) Post Interest on parent loan
                    if (settlement.Interest > 0)
                    {
                        var interestTxn = new CustomerTransactionModel
                        {
                            BranchId = branchDTO.Id,
                            PostingPeriodId = postingPeriod.Id,
                            ValueDate = DateTime.UtcNow,
                            TotalValue = settlement.Interest,
                            PrimaryDescription = "Loan Interest Payment",
                            SecondaryDescription = $"Interest on Parent Loan Ref: {settlement.Reference}",
                            Reference = settlement.Reference,

                            DebitCustomerAccount = customerLoanAccountDTO,
                            CreditCustomerAccount = customerLoanAccountDTO,
                            DebitChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId,
                            CreditChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductInterestReceivedChartOfAccountId
                        };

                        var r1 = await master._channelService
                            .AddJournalWithCustomerAccountAsync(interestTxn, serviceHeader);
                        if (r1.HasErrors)
                            journalErrors.Add("Interest posting failed");
                    }

                    // 2) Settle parent loan
                    if (!journalErrors.Any())
                    {
                        var settleTxn = new CustomerTransactionModel
                        {
                            BranchId = branchDTO.Id,
                            PostingPeriodId = postingPeriod.Id,
                            ValueDate = DateTime.UtcNow,
                            TotalValue = settlement.CarryForwards,
                            PrimaryDescription = "Loan Refinance - Parent Loan Settlement",
                            SecondaryDescription = $"Settlement Ref: {settlement.Reference}",
                            Reference = settlement.Reference,

                            DebitCustomerAccount = customerLoanAccountDTO,
                            CreditCustomerAccount = customerLoanAccountDTO,
                            DebitChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId,
                            CreditChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId
                        };

                        var r2 = await master._channelService
                            .AddJournalWithCustomerAccountAsync(settleTxn, serviceHeader);
                        if (r2.HasErrors)
                            journalErrors.Add("Parent loan settlement posting failed");
                    }

                    // 3) Top-up to bank
                    if (settlement.Principal > 0 && !journalErrors.Any())
                    {
                        var disburseTxn = new CustomerTransactionModel
                        {
                            BranchId = branchDTO.Id,
                            PostingPeriodId = postingPeriod.Id,
                            ValueDate = DateTime.UtcNow,
                            TotalValue = settlement.Principal,
                            PrimaryDescription = "Loan Disbursement - Top Up",
                            SecondaryDescription = $"BC {bankAccount.BankName}",
                            Reference = settlement.Reference,

                            DebitCustomerAccount = customerLoanAccountDTO,
                            DebitChartOfAccountId = customerLoanAccountDTO.CustomerAccountTypeTargetProductChartOfAccountId,
                            CreditCustomerAccount = customerLoanAccountDTO,
                            CreditChartOfAccountId = bankAccount.ChartOfAccountId
                        };

                        var r3 = await master._channelService
                            .AddJournalWithCustomerAccountAsync(disburseTxn, serviceHeader);
                        if (r3.HasErrors)
                            journalErrors.Add("Top-up disbursement posting failed");
                    }
                }

                // ================== HANDLE JOURNAL ERRORS ==================
                if (journalErrors.Any())
                    return Ok(new
                    {
                        success = false,
                        message = "Journal posting failed",
                        errors = journalErrors
                    });

                // ================== MARK SETTLEMENT AS PROCESSED ==================
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(@"
            UPDATE [dbo].[swiftFin_MembershipWithdrawalSettlements]
            SET [ProcessedDate] = GETUTCDATE(),
                [ProcessedBy]   = @ProcessedBy
            WHERE [Id] = @SettlementId", conn))
                {
                    cmd.Parameters.Add("@SettlementId", SqlDbType.UniqueIdentifier).Value = request.SettlementId;
                    cmd.Parameters.Add("@ProcessedBy", SqlDbType.NVarChar, 200).Value = request.ProcessedBy;

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }

                return Ok(new { success = true, message = "Disbursement processed and journals posted successfully" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("api/GetBankLinkages")]
        public async Task<IHttpActionResult> GetBankLinkages()
        {
            try
            {
                var bankLinkages = new List<object>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (var cmd = new SqlCommand(@"
                SELECT
                    bl.[Id],
                    bl.[BranchId],
                    bl.[ChartOfAccountId],
                    bl.[BankName],
                    bl.[BankBranchName],
                    bl.[BankAccountNumber],
                    bl.[Remarks],
                    bl.[IsLocked],
                    bl.[BankId],
                    coa.[AccountCode],
                    coa.[AccountName]
                FROM [dbo].[swiftFin_BankLinkages] bl
                LEFT JOIN [dbo].[swiftFin_ChartOfAccounts] coa ON coa.[Id] = bl.[ChartOfAccountId]
                WHERE bl.[IsLocked] = 0
                ORDER BY bl.[BankName] ASC", conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                bankLinkages.Add(new
                                {
                                    id = new Guid(Convert.ToString(reader["Id"])),
                                    branchId = reader["BranchId"] != DBNull.Value ? new Guid(Convert.ToString(reader["BranchId"])) : (Guid?)null,
                                    chartOfAccountId = reader["ChartOfAccountId"] != DBNull.Value ? new Guid(Convert.ToString(reader["ChartOfAccountId"])) : (Guid?)null,
                                    bankName = Convert.ToString(reader["BankName"]),
                                    bankBranchName = Convert.ToString(reader["BankBranchName"]),
                                    bankAccountNumber = Convert.ToString(reader["BankAccountNumber"]),
                                    remarks = Convert.ToString(reader["Remarks"]),
                                    isLocked = Convert.ToBoolean(reader["IsLocked"] == DBNull.Value ? false : reader["IsLocked"]),
                                    bankId = reader["BankId"] != DBNull.Value ? new Guid(Convert.ToString(reader["BankId"])) : (Guid?)null,
                                    chartOfAccountCode = Convert.ToString(reader["AccountCode"]),
                                    chartOfAccountName = Convert.ToString(reader["AccountName"]),
                                    displayName = $"{Convert.ToString(reader["BankName"])} - {Convert.ToString(reader["BankAccountNumber"])} ({Convert.ToString(reader["AccountName"])})"
                                });
                            }
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = $"Found {bankLinkages.Count} bank linkages",
                    data = bankLinkages
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = $"Failed to fetch bank linkages: {ex.Message} | Inner: {ex.InnerException?.Message}"
                });
            }
        }


        // ================== REQUEST MODEL ==================
        public class ProcessDisbursementRequest
        {
            public Guid SettlementId { get; set; }
            public Guid BankAccountId { get; set; }
            public string ProcessedBy { get; set; }
        }


        // ================== SETTLEMENT RECORD MODEL ==================
        public class SettlementRecord
        {
            public Guid Id { get; set; }
            public Guid? WithdrawalNotificationId { get; set; }
            public Guid CustomerAccountId { get; set; }
            public decimal Principal { get; set; }
            public decimal Interest { get; set; }
            public decimal CarryForwards { get; set; }
            public string Reference { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedDate { get; set; }
        }


        // ================== HELPER: INSERT SETTLEMENT RECORD ==================
        private async Task InsertSettlementRecordAsync(
            Guid customerAccountId,
            decimal principal,
            decimal interest,
            decimal carryForwards,
            string reference,
            string createdBy)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
        INSERT INTO [dbo].[swiftFin_MembershipWithdrawalSettlements]
            ([Id],
             [CustomerAccountId],
             [Principal],
             [Interest],
             [CarryForwards],
             [Reference],
             [CreatedBy],
             [CreatedDate])
        VALUES
            (NEWID(),
             @CustomerAccountId,
             @Principal,
             @Interest,
             @CarryForwards,
             @Reference,
             @CreatedBy,
             GETUTCDATE())", conn))
            {
                cmd.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = customerAccountId;
                cmd.Parameters.Add("@Principal", SqlDbType.Decimal).Value = principal;
                cmd.Parameters.Add("@Interest", SqlDbType.Decimal).Value = interest;
                cmd.Parameters.Add("@CarryForwards", SqlDbType.Decimal).Value = carryForwards;
                cmd.Parameters.Add("@Reference", SqlDbType.NVarChar, 200).Value =
                    string.IsNullOrEmpty(reference) ? (object)DBNull.Value : reference;
                cmd.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 200).Value =
                    string.IsNullOrEmpty(createdBy) ? (object)DBNull.Value : createdBy;

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
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