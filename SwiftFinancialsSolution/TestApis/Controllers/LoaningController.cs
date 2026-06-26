using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Presentation.Infrastructure.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.UI.WebControls;
using TestApis.Helpers;
using TestApis.Models;
using Image = iTextSharp.text.Image;

namespace TestApis.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [AllowAnonymous]
    [RoutePrefix("api/Loaning")]
    public class LoaningController : ApiController
    {
        private readonly MasterController master;

        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        private readonly HttpClient _httpClient;

        public LoaningController()
        {
            master = new MasterController();
            _httpClient = new HttpClient(); // FIXED

        }



        [HttpGet]
        [Route("GetLoansBy")]
        public async Task<IHttpActionResult> GetLoansBy(
     string status = "Registered",
     string filterValue = "",
     int filterType = 0,
     int pageIndex = 0,
     int pageSize = 50)
        {
            var serviceHeader = master.GetServiceHeader();

            var resolvedStatus = ResolveLoanStatus(status);
            if (resolvedStatus == null)
                return BadRequest($"Invalid loan status '{status}'.");

            var resolvedFilter = ResolveLoanFilter(filterType);
            if (resolvedFilter == null)
                return BadRequest($"Invalid filterType '{filterType}'.");

            var pageInfo = await master._channelService
                .FindLoanCasesByStatusAndFilterInPageAsync(
                    (int)resolvedStatus.Value,
                    filterValue,
                    filterType,
                    pageIndex,
                    pageSize,
                    includeBatchStatus: true,
                    serviceHeader);

            if (pageInfo?.PageCollection == null || !pageInfo.PageCollection.Any())
            {
                return Ok(new
                {
                    items = Array.Empty<LoanCaseDTO>(),
                    pageIndex,
                    pageSize
                });
            }

            // HARD GUARANTEE: guarantors are fully packed before response
            foreach (var loanCase in pageInfo.PageCollection)
            {
                var guarantors =
                    await master._channelService
                        .FindLoanGuarantorsByLoanCaseIdAsync(loanCase.Id, serviceHeader);

                loanCase.Guarantors = guarantors?.ToList()
                    ?? new List<LoanGuarantorDTO>();
            }

            return Ok(new
            {
                items = pageInfo.PageCollection,
                pageIndex,
                pageSize
            });

        }
        [HttpPost]
        [Route("Topup")]
        public async Task<IHttpActionResult> TopUpLoanAsync([FromBody] LoanTopUpRequest request)
        {
            if (request.TopUpAmount <= 0)
                return Ok(new { success = false, message = "Top-up amount must be greater than zero" });

            // 1. Load original loan case
            var originalLoanCase = await master._channelService.FindLoanCaseAsync(request.OriginalLoanCaseId, request.ServiceHeader);
            if (originalLoanCase == null)
                return Ok(new { success = false, message = "Original loan case not found" });

            // 2. Check eligibility
            var remainingEligibility = originalLoanCase.LoanRegistrationMaximumAmount - originalLoanCase.TotalLoansBalance;
            if (request.TopUpAmount > remainingEligibility)
                return Ok(new { success = false, message = "Top-up exceeds maximum entitlement" });

            // 3. Calculate interest & repayment
            decimal interestRate = (decimal)originalLoanCase.LoanInterestAnnualPercentageRate / 100m;
            int termMonths = originalLoanCase.LoanRegistrationTermInMonths;

            decimal totalPayback = request.TopUpAmount * (1 + interestRate); // simple interest
            decimal monthlyPayback = totalPayback / termMonths;

            // 4. Create top-up loan case
            var topUpLoanCase = new LoanCaseDTO
            {
                ParentId = originalLoanCase.Id,
                BranchId = originalLoanCase.BranchId,
                CustomerId = originalLoanCase.CustomerId,
                LoanProductId = originalLoanCase.LoanProductId,
                LoanProductCode = originalLoanCase.LoanProductCode,
                AmountApplied = request.TopUpAmount,
                AppraisedAmount = request.TopUpAmount,
                ApprovedAmount = request.TopUpAmount,
                AppraisedDate = DateTime.Now,
                MonthlyPaybackAmount = monthlyPayback,
                TotalPaybackAmount = totalPayback,
                Status = (int)LoanCaseStatus.Approved,
                CreatedDate = DateTime.Now
            };

            topUpLoanCase = await master._channelService.AddLoanCaseAsync(topUpLoanCase, request.ServiceHeader);
            if (topUpLoanCase == null)
                return Ok(new { success = false, message = "Failed to create top-up loan case" });

            // 5. Load or create customer loan account
            var customerLoanAccount = await master._channelService.FindCustomerAccountAsync(originalLoanCase.CustomerAccountId, true, true, true, false, request.ServiceHeader);
            if (customerLoanAccount == null)
            {
                var customerAccountDTO = new CustomerAccountDTO
                {
                    BranchId = topUpLoanCase.BranchId,
                    CustomerId = topUpLoanCase.CustomerId,
                    CustomerAccountTypeProductCode = (int)ProductCode.Loan,
                    CustomerAccountTypeTargetProductId = topUpLoanCase.LoanProductId,
                    CustomerAccountTypeTargetProductCode = topUpLoanCase.LoanProductCode,
                    Status = (int)CustomerAccountStatus.Normal,
                    RecordStatus = (int)RecordStatus.Approved
                };
                customerLoanAccount = await master._channelService.AddCustomerAccountAsync(customerAccountDTO, request.ServiceHeader);
            }

            // 6. Load bank account
            var bankAccount = await master._channelService.FindBankLinkagesAsync();
            if (bankAccount == null)
                return Ok(new { success = false, message = "Bank account not found" });

            // 7. Post transaction to journal
            var transactionModel = new CustomerTransactionModel
            {
                BranchId = topUpLoanCase.BranchId,
                TotalValue = request.TopUpAmount,
                DebitCustomerAccountId = customerLoanAccount.Id,
                CreditCustomerAccountId = customerLoanAccount.Id,
                DebitCustomerAccount = customerLoanAccount,
                CreditCustomerAccount = customerLoanAccount,
                DebitChartOfAccountId = customerLoanAccount.CustomerAccountTypeTargetProductChartOfAccountId,
            };

            var postingPeriod = transactionModel.PostingPeriodId != Guid.Empty
                ? await master._channelService.FindPostingPeriodAsync(transactionModel.PostingPeriodId, request.ServiceHeader)
                : await master._channelService.FindCurrentPostingPeriodAsync(request.ServiceHeader);

            if (postingPeriod == null)
                return BadRequest("Posting period not found");

            var journal = await master._channelService.AddJournalWithCustomerAccountAsync(transactionModel, request.ServiceHeader);
            if (journal == null)
                return Ok(new { success = false, message = "Failed to post journal entry" });

            // 8. Update original loan balances
            originalLoanCase.TotalLoansBalance += request.TopUpAmount;
            originalLoanCase.LoanProductLoanBalance += request.TopUpAmount;
            await master._channelService.UpdateLoanCaseAsync(originalLoanCase, request.ServiceHeader);

            // 9. Add entry to attached loans table
            var attachedLoan = new AttachedLoanDTO
            {
                LoanCaseId = topUpLoanCase.Id,
                CustomerAccountId = customerLoanAccount.Id,
                PrincipalBalance = request.TopUpAmount,
                InterestBalance = totalPayback - request.TopUpAmount,
                CarryForwardsBalance = 0,
                ClearanceCharges = 0,
                CreatedDate = DateTime.Now
            };
            ObservableCollection<AttachedLoanDTO> attachedLoanDTOs = new ObservableCollection<AttachedLoanDTO>();

            var attachedLoanList = new List<AttachedLoanDTO> { attachedLoan };
            await master._channelService.UpdateAttachedLoansByLoanCaseIdAsync(request.OriginalLoanCaseId, attachedLoanDTOs, request.ServiceHeader);

            return Ok(new
            {
                success = true,
                message = "Loan top-up processed successfully",
                TopUpLoanCaseId = topUpLoanCase.Id,
                UpdatedTotalBalance = originalLoanCase.TotalLoansBalance,
                MonthlyPaybackAmount = monthlyPayback,
                TotalPaybackAmount = totalPayback
            });
        }




        [HttpGet]
        [Route("GetLoansByFilters")]
        public async Task<IHttpActionResult> GetLoans(int status = (int)LoanCaseStatus.Registered, string filterValue = "", int filterType = 0, int pageIndex = 0, int pageSize = 50)
        {
            var serviceHeader = master.GetServiceHeader();

            var pageInfo = await master._channelService.FindLoanCasesByStatusAndFilterInPageAsync(status, filterValue, filterType, pageIndex, pageSize, includeBatchStatus: true, serviceHeader);

            if (pageInfo == null || pageInfo.PageCollection == null)
            {
                return Ok(new { items = new List<LoanCaseDTO>(), pageIndex = pageIndex, pageSize = pageSize });
            }

            return Ok(new { items = pageInfo.PageCollection, pageIndex = pageIndex, pageSize = pageSize });
        }


        [HttpGet]
        [Route("GetAllLoans")]
        public async Task<IHttpActionResult> GetAllLoans(
    [FromUri] string search = "",
    [FromUri] int page = 1,
    [FromUri] int pageSize = 20,
    [FromUri] string status = "all")
        {
            try
            {
                var loans = new List<object>();
                int totalCount = 0;
                int offset = (page - 1) * pageSize;

                // ── Status filter ─────────────────────────────────────────────
                string statusFilter = "";

                switch (status.ToLower())
                {
                    case "registered": statusFilter = "AND lc.Status = 48826"; break;
                    case "appraised": statusFilter = "AND lc.Status = 48827"; break;
                    case "approved": statusFilter = "AND lc.Status = 48828"; break;
                    case "disbursed": statusFilter = "AND lc.Status = 48829"; break;
                    case "rejected": statusFilter = "AND lc.Status = 48830"; break;
                    case "deferred": statusFilter = "AND lc.Status = 48831"; break;
                    case "audited":
                    case "verified": statusFilter = "AND lc.Status = 48832"; break;
                    case "restructured": statusFilter = "AND lc.Status = "; break;
                    default: statusFilter = ""; break;
                }

                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    Guid SafeGuid(SqlDataReader r, string col)
                    {
                        int ord = r.GetOrdinal(col);
                        return r.IsDBNull(ord) ? Guid.Empty : r.GetGuid(ord);
                    }

                    // ── Total count ───────────────────────────────────────────
                    using (var countCmd = new SqlCommand($@"
                SELECT COUNT(DISTINCT lc.Id)
                FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanCases] lc
                INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Customers] c
                    ON lc.CustomerId = c.Id
                INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanProducts] lp
                    ON lc.LoanProductId = lp.Id
                WHERE 1 = 1
                  {statusFilter}
                  AND (@Search = ''
                       OR c.Individual_FirstName      LIKE '%' + @Search + '%'
                       OR c.Individual_LastName       LIKE '%' + @Search + '%'
                       OR c.Individual_PayrollNumbers LIKE '%' + @Search + '%'
                       OR c.Reference2               LIKE '%' + @Search + '%'
                       OR c.Reference3               LIKE '%' + @Search + '%'
                       OR lc.Reference               LIKE '%' + @Search + '%'
                       OR lp.Description             LIKE '%' + @Search + '%'
                       OR CAST(lc.CaseNumber AS NVARCHAR) LIKE '%' + @Search + '%')", conn))
                    {
                        countCmd.Parameters.Add("@Search", SqlDbType.NVarChar, 200).Value = search ?? "";
                        totalCount = (int)await countCmd.ExecuteScalarAsync();
                    }

                    // ── Fetch loans ───────────────────────────────────────────
                    var loanIds = new List<Guid>();

                    using (var cmd = new SqlCommand($@"
                SELECT
                    lc.Id                                           AS LoanCaseId,
                    lc.CaseNumber,
                    lc.Reference                                    AS LoanReference,
                    lc.AmountApplied,
                    ISNULL(lc.ApprovedAmount,           0)          AS ApprovedAmount,
                    ISNULL(lc.DisbursedAmount,          0)          AS DisbursedAmount,
                    ISNULL(lc.MonthlyPaybackAmount,     0)          AS MonthlyPaybackAmount,
                    ISNULL(lc.TotalPaybackAmount,       0)          AS TotalPaybackAmount,
                    ISNULL(lc.TotalLoansBalance,        0)          AS TotalLoansBalance,
                    ISNULL(lc.ApprovedInterestPayment,  0)          AS TotalInterest,
                    ISNULL(lc.LoanInterest_AnnualPercentageRate, 0) AS APR,
                    ISNULL(lc.LoanRegistration_TermInMonths,     0) AS TermMonths,
                    lc.Status                                       AS LoanStatus,
                    lc.ReceivedDate                                 AS ApplicationDate,
                    lc.ApprovedDate,
                    lc.DisbursedDate,
                    lc.AuditedDate,
                    lc.Remarks,
                    lc.CreatedDate,
                    lc.CreatedBy,

                    CASE lc.Status
                        WHEN 48826 THEN 'Registered'
                        WHEN 48827 THEN 'Appraised'
                        WHEN 48828 THEN 'Approved'
                        WHEN 48829 THEN 'Disbursed'
                        WHEN 48830 THEN 'Rejected'
                        WHEN 48831 THEN 'Deferred'
                        WHEN 48832 THEN 'Audited/Verified'
                        WHEN 48833 THEN 'Restructured'
                        ELSE 'Unknown (' + CAST(lc.Status AS NVARCHAR) + ')'
                    END AS StatusDescription,

                    lp.Id                                           AS LoanProductId,
                    lp.Description                                  AS LoanProductName,
                    ISNULL(lp.Code, 0)                              AS LoanProductCode,

                    c.Id                                            AS MemberId,
                    c.Individual_FirstName                          AS MemberFirstName,
                    c.Individual_LastName                           AS MemberLastName,
                    c.Individual_PayrollNumbers                     AS MemberPayrollNumber,
                    c.Reference2                                    AS MemberNumber,
                    c.Reference3                                    AS MemberPFNumber,
                    c.Address_MobileLine                            AS MemberMobile,
                    c.Address_Email                                 AS MemberEmail,
                    c.Individual_IdentityCardNumber                 AS MemberIDNumber,
                    c.RegistrationDate                              AS MemberRegistrationDate

                FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanCases] lc
                INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Customers] c
                    ON lc.CustomerId = c.Id
                INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanProducts] lp
                    ON lc.LoanProductId = lp.Id
                WHERE 1 = 1
                  {statusFilter}
                  AND (@Search = ''
                       OR c.Individual_FirstName      LIKE '%' + @Search + '%'
                       OR c.Individual_LastName       LIKE '%' + @Search + '%'
                       OR c.Individual_PayrollNumbers LIKE '%' + @Search + '%'
                       OR c.Reference2               LIKE '%' + @Search + '%'
                       OR c.Reference3               LIKE '%' + @Search + '%'
                       OR lc.Reference               LIKE '%' + @Search + '%'
                       OR lp.Description             LIKE '%' + @Search + '%'
                       OR CAST(lc.CaseNumber AS NVARCHAR) LIKE '%' + @Search + '%')
                ORDER BY lc.CreatedDate DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", conn))
                    {
                        cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 200).Value = search ?? "";
                        cmd.Parameters.Add("@Offset", SqlDbType.Int).Value = offset;
                        cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Guid loanCaseId = SafeGuid(reader, "LoanCaseId");
                                Guid loanProductId = SafeGuid(reader, "LoanProductId");
                                Guid memberId = SafeGuid(reader, "MemberId");

                                loanIds.Add(loanCaseId);

                                string firstName = reader["MemberFirstName"]?.ToString() ?? "";
                                string lastName = reader["MemberLastName"]?.ToString() ?? "";

                                loans.Add(new
                                {
                                    loanCaseId = loanCaseId,
                                    caseNumber = reader["CaseNumber"] != DBNull.Value ? Convert.ToInt32(reader["CaseNumber"]) : 0,
                                    loanReference = reader["LoanReference"]?.ToString(),
                                    loanProductId = loanProductId,
                                    loanProductName = reader["LoanProductName"]?.ToString(),
                                    loanProductCode = reader["LoanProductCode"] != DBNull.Value ? Convert.ToInt32(reader["LoanProductCode"]) : 0,
                                    loanStatus = reader["LoanStatus"] != DBNull.Value ? Convert.ToInt32(reader["LoanStatus"]) : 0,
                                    statusDescription = reader["StatusDescription"]?.ToString(),
                                    applicationDate = reader["ApplicationDate"] != DBNull.Value
                            ? (DateTime?)Convert.ToDateTime(reader["ApplicationDate"]) : null,
                                    approvedDate = reader["ApprovedDate"] != DBNull.Value
                            ? (DateTime?)Convert.ToDateTime(reader["ApprovedDate"]) : null,
                                    auditedDate = reader["AuditedDate"] != DBNull.Value
                            ? (DateTime?)Convert.ToDateTime(reader["AuditedDate"]) : null,
                                    disbursedDate = reader["DisbursedDate"] != DBNull.Value
                            ? (DateTime?)Convert.ToDateTime(reader["DisbursedDate"]) : null,
                                    createdDate = reader["CreatedDate"] != DBNull.Value
                            ? Convert.ToDateTime(reader["CreatedDate"]) : DateTime.MinValue,
                                    createdBy = reader["CreatedBy"]?.ToString(),
                                    remarks = reader["Remarks"]?.ToString(),
                                    amountApplied = reader["AmountApplied"] != DBNull.Value ? Convert.ToDecimal(reader["AmountApplied"]) : 0m,
                                    approvedAmount = reader["ApprovedAmount"] != DBNull.Value ? Convert.ToDecimal(reader["ApprovedAmount"]) : 0m,
                                    disbursedAmount = reader["DisbursedAmount"] != DBNull.Value ? Convert.ToDecimal(reader["DisbursedAmount"]) : 0m,
                                    monthlyPayback = reader["MonthlyPaybackAmount"] != DBNull.Value ? Convert.ToDecimal(reader["MonthlyPaybackAmount"]) : 0m,
                                    totalPayback = reader["TotalPaybackAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalPaybackAmount"]) : 0m,
                                    totalLoansBalance = reader["TotalLoansBalance"] != DBNull.Value ? Convert.ToDecimal(reader["TotalLoansBalance"]) : 0m,
                                    totalInterest = reader["TotalInterest"] != DBNull.Value ? Convert.ToDecimal(reader["TotalInterest"]) : 0m,
                                    apr = reader["APR"] != DBNull.Value ? Convert.ToDouble(reader["APR"]) : 0d,
                                    termMonths = reader["TermMonths"] != DBNull.Value ? Convert.ToInt32(reader["TermMonths"]) : 0,

                                    member = new
                                    {
                                        memberId = memberId,
                                        fullName = $"{firstName} {lastName}".Trim(),
                                        firstName,
                                        lastName,
                                        memberNumber = reader["MemberNumber"]?.ToString(),
                                        pfNumber = reader["MemberPFNumber"]?.ToString(),
                                        payrollNumber = reader["MemberPayrollNumber"]?.ToString(),
                                        mobile = reader["MemberMobile"]?.ToString(),
                                        email = reader["MemberEmail"]?.ToString(),
                                        idNumber = reader["MemberIDNumber"]?.ToString(),
                                        registrationDate = reader["MemberRegistrationDate"] != DBNull.Value
                               ? (DateTime?)Convert.ToDateTime(reader["MemberRegistrationDate"]) : null
                                    },

                                    guarantors = new List<object>(),
                                    guarantorCount = 0,
                                    totalGuaranteed = 0m
                                });
                            }
                        }
                    }

                    // ── Load guarantors for all returned loans ────────────────
                    if (loanIds.Any())
                    {
                        var paramNames = loanIds.Select((_, i) => $"@LId{i}").ToList();
                        var inClause = string.Join(",", paramNames);

                        var guarantorMap = new Dictionary<Guid, List<object>>();

                        using (var gCmd = new SqlCommand($@"
                    SELECT
                        g.Id                                AS GuarantorId,
                        g.LoanCaseId,
                        g.CustomerId                        AS GuarantorCustomerId,
                        ISNULL(g.AmountGuaranteed, 0)       AS AmountGuaranteed,
                        ISNULL(g.AmountPledged,    0)       AS AmountPledged,
                        ISNULL(g.Status,           0)       AS GuarantorStatus,
                        g.CreatedDate                       AS GuarantorCreatedDate,
                        gc.Individual_FirstName             AS GuarantorFirstName,
                        gc.Individual_LastName              AS GuarantorLastName,
                        gc.Reference2                       AS GuarantorMemberNumber,
                        gc.Reference3                       AS GuarantorPFNumber,
                        gc.Address_MobileLine               AS GuarantorMobile,
                        gc.Individual_IdentityCardNumber    AS GuarantorIDNumber,
                        CASE ISNULL(g.Status, 0)
                            WHEN 0 THEN 'Pending'
                            WHEN 1 THEN 'Approved'
                            WHEN 2 THEN 'Rejected'
                            ELSE 'Unknown'
                        END AS GuarantorStatusDescription
                    FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanGuarantors] g
                    INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Customers] gc
                        ON g.CustomerId = gc.Id
                    WHERE g.LoanCaseId IN ({inClause})
                    ORDER BY g.LoanCaseId, g.CreatedDate ASC", conn))
                        {
                            for (int i = 0; i < loanIds.Count; i++)
                                gCmd.Parameters.Add(paramNames[i], SqlDbType.UniqueIdentifier).Value = loanIds[i];

                            using (var gReader = await gCmd.ExecuteReaderAsync())
                            {
                                while (await gReader.ReadAsync())
                                {
                                    Guid loanId = SafeGuid(gReader, "LoanCaseId");
                                    Guid guarantorCustomerId = SafeGuid(gReader, "GuarantorCustomerId");

                                    if (loanId == Guid.Empty) continue;

                                    if (!guarantorMap.ContainsKey(loanId))
                                        guarantorMap[loanId] = new List<object>();

                                    string gFirst = gReader["GuarantorFirstName"]?.ToString() ?? "";
                                    string gLast = gReader["GuarantorLastName"]?.ToString() ?? "";

                                    guarantorMap[loanId].Add(new
                                    {
                                        guarantorId = SafeGuid(gReader, "GuarantorId"),
                                        guarantorCustomerId = guarantorCustomerId,
                                        fullName = $"{gFirst} {gLast}".Trim(),
                                        firstName = gFirst,
                                        lastName = gLast,
                                        memberNumber = gReader["GuarantorMemberNumber"]?.ToString(),
                                        pfNumber = gReader["GuarantorPFNumber"]?.ToString(),
                                        mobile = gReader["GuarantorMobile"]?.ToString(),
                                        idNumber = gReader["GuarantorIDNumber"]?.ToString(),
                                        amountGuaranteed = gReader["AmountGuaranteed"] != DBNull.Value ? Convert.ToDecimal(gReader["AmountGuaranteed"]) : 0m,
                                        amountPledged = gReader["AmountPledged"] != DBNull.Value ? Convert.ToDecimal(gReader["AmountPledged"]) : 0m,
                                        status = gReader["GuarantorStatus"] != DBNull.Value ? Convert.ToInt32(gReader["GuarantorStatus"]) : 0,
                                        statusDescription = gReader["GuarantorStatusDescription"]?.ToString(),
                                        createdDate = gReader["GuarantorCreatedDate"] != DBNull.Value
                              ? Convert.ToDateTime(gReader["GuarantorCreatedDate"])
                              : DateTime.MinValue
                                    });
                                }
                            }
                        }

                        // ── Assemble guarantors onto each loan ────────────────
                        loans = loans.Select(l =>
                        {
                            var type = l.GetType();
                            Guid loanId = (Guid)type.GetProperty("loanCaseId").GetValue(l);

                            var gList = guarantorMap.ContainsKey(loanId)
                                            ? guarantorMap[loanId]
                                            : new List<object>();

                            return (object)new
                            {
                                loanCaseId = loanId,
                                caseNumber = type.GetProperty("caseNumber").GetValue(l),
                                loanReference = type.GetProperty("loanReference").GetValue(l),
                                loanProductId = type.GetProperty("loanProductId").GetValue(l),
                                loanProductName = type.GetProperty("loanProductName").GetValue(l),
                                loanProductCode = type.GetProperty("loanProductCode").GetValue(l),
                                loanStatus = type.GetProperty("loanStatus").GetValue(l),
                                statusDescription = type.GetProperty("statusDescription").GetValue(l),
                                applicationDate = type.GetProperty("applicationDate").GetValue(l),
                                approvedDate = type.GetProperty("approvedDate").GetValue(l),
                                auditedDate = type.GetProperty("auditedDate").GetValue(l),
                                disbursedDate = type.GetProperty("disbursedDate").GetValue(l),
                                createdDate = type.GetProperty("createdDate").GetValue(l),
                                createdBy = type.GetProperty("createdBy").GetValue(l),
                                remarks = type.GetProperty("remarks").GetValue(l),
                                amountApplied = type.GetProperty("amountApplied").GetValue(l),
                                approvedAmount = type.GetProperty("approvedAmount").GetValue(l),
                                disbursedAmount = type.GetProperty("disbursedAmount").GetValue(l),
                                monthlyPayback = type.GetProperty("monthlyPayback").GetValue(l),
                                totalPayback = type.GetProperty("totalPayback").GetValue(l),
                                totalLoansBalance = type.GetProperty("totalLoansBalance").GetValue(l),
                                totalInterest = type.GetProperty("totalInterest").GetValue(l),
                                apr = type.GetProperty("apr").GetValue(l),
                                termMonths = type.GetProperty("termMonths").GetValue(l),
                                member = type.GetProperty("member").GetValue(l),
                                guarantors = gList,
                                guarantorCount = gList.Count,
                                totalGuaranteed = gList.Sum(g =>
                                    Convert.ToDecimal(g.GetType().GetProperty("amountGuaranteed").GetValue(g)))
                            };
                        }).ToList();
                    }
                }

                int totalPages = totalCount > 0
                    ? (int)Math.Ceiling((double)totalCount / pageSize)
                    : 0;

                return Ok(new
                {
                    success = true,
                    message = $"Found {totalCount} loan(s)",
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = totalPages,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1,
                    loans = loans
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(
                    new Exception($"Failed to fetch loans: {ex.Message}", ex));
            }
        }

        [HttpGet]
        [Route("printshedule")]
        public async Task<IHttpActionResult> printshedule()
        {
            var serviceHeader = master.GetServiceHeader();
            var pageInfo = await master._channelService.FindLoanCasesAsync(serviceHeader);
            var loanCaseDTO = pageInfo?.FirstOrDefault(c => c.CustomerReference2 == "0004");
            var doublee = await master._channelService.FVAsync(loanCaseDTO.LoanRegistrationTermInMonths, loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear, loanCaseDTO.LoanInterestAnnualPercentageRate, (double)loanCaseDTO.MonthlyPaybackAmount, (double)loanCaseDTO.ApprovedAmount, loanCaseDTO.LoanRegistrationPaymentDueDate, serviceHeader);
            var repayment = await master._channelService.PrintLoanRepaymentScheduleAsync(loanCaseDTO, serviceHeader);

            return Ok(repayment);
        }



        [HttpGet]
        [Route("GetAllLoanByMemberNo")]
        public async Task<IHttpActionResult> GetAllLoanByMemberNo(string memberNo)
        {
            var serviceHeader = master.GetServiceHeader();

            var pageInfo = await master._channelService.FindLoanCasesAsync(serviceHeader);

            if (pageInfo == null || !pageInfo.Any())
                return BadRequest("No loans found.");

            var memberLoans = pageInfo
                .Where(c => c.CustomerReference2 == memberNo)
                .ToList();

            if (!memberLoans.Any())
                return BadRequest("No loans found for the specified member.");

            return Ok(memberLoans);
        }



        [HttpGet]
        [Route("GetPostingPeriods")]
        public async Task<IHttpActionResult> GetPostingPeriods()
        {
            var serviceHeader = master.GetServiceHeader();

            var pageInfo = await master._channelService.FindPostingPeriodsAsync(serviceHeader);
            if (pageInfo == null)
                return BadRequest("Posting Periods Not Found.");

            return Ok(pageInfo);
        }

        #region Loan Application Original

        //        [HttpPost]
        //        [Route("LoanApplication")]
        //        public async Task<IHttpActionResult> Create([FromBody] LoanCaseDTO2 loanCaseDTO)
        //        {

        //            try
        //            {
        //                var serviceHeader = master.GetServiceHeader();

        //                if (loanCaseDTO == null)
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Invalid request payload."));
        //                // 1. Validate member
        //                var customer = await master._channelService
        //                    .FindCustomerAsync(loanCaseDTO.CustomerId, serviceHeader);

        //                if (customer == null)
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Member not found."));


        //                // 2. Validate loan product
        //                var loanProduct = await master._channelService.FindLoanProductAsync(loanCaseDTO.LoanProductId, serviceHeader);

        //                if (loanProduct == null)
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Invalid loan product."));

        //                var products = await master._channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(loanCaseDTO.CustomerId, new[] { (int)ProductCode.Savings, (int)ProductCode.Loan, (int)ProductCode.Investment }, true, true, true, true, serviceHeader);



        //                if (loanCaseDTO.AmountApplied == 0)
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Amount Cannot be zero."));

        //                var multiplier = loanProduct.LoanRegistrationInvestmentsMultiplier;


        //                loanCaseDTO.ReceivedDate = DateTime.UtcNow;
        //                //decimal rate = Convert.ToDecimal( loanProduct.LoanInterestAnnualPercentageRate / 100); // if stored as 12 not 0.12
        //                //decimal principal = loanCaseDTO.AmountApplied;
        //                //decimal termInYears = loanProduct.LoanRegistrationTermInMonths / 12m;

        //                //loanCaseDTO.ApprovedInterestPayment = principal * rate * termInYears;
        //                //loanCaseDTO.TotalPaybackAmount = loanCaseDTO.AmountApplied + loanCaseDTO.ApprovedInterestPayment;



        //                //var productName = loanProduct.Description?.Trim();

        //                //// 3. Enforce Savings Boost ? Development Loan dependency
        //                //if (string.Equals(productName, "Savings Boost", StringComparison.OrdinalIgnoreCase))
        //                //{
        //                //    var memberLoans = await master._channelService
        //                //        .FindLoanCasesByCustomerIdInProcessAsync(loanCaseDTO.CustomerId, serviceHeader);

        //                //}

        //                // 4. Branch resolution (isolated, explicit)
        //                var branches = await master._channelService.FindBranchesAsync(serviceHeader);
        //                var branch = branches?.FirstOrDefault(b =>
        //                    b.Description != null &&
        //                    b.Description.StartsWith("Rubani", StringComparison.OrdinalIgnoreCase));

        //                if (branch != null)
        //                    loanCaseDTO.BranchId = branch.Id;

        //                // 5. Membership duration validation
        //                var membershipMonths = ((DateTime.UtcNow.Year - customer.CreatedDate.Year) * 12) + (DateTime.UtcNow.Month - customer.CreatedDate.Month);

        //                if (membershipMonths < loanProduct.LoanRegistrationMinimumMembershipPeriod)

        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Member does not meet minimum membership period."));

        //                // 6. Guarantor validation
        //                var guarantors = loanCaseDTO.Guarantors ?? new List<LoanGuarantorDTO>();

        //                if (!guarantors.Any())
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "At least one guarantor is required."));

        //                if (guarantors.Count < loanProduct.LoanRegistrationMinimumGuarantors)
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", $"Minimum {loanProduct.LoanRegistrationMinimumGuarantors} guarantors required."));

        //                //if (guarantors.Select(g => g.CustomerReference2).Distinct().Count() != guarantors.Count)
        //                //    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Duplicate guarantors detected."));

        //                if (guarantors.Sum(g => g.AmountGuaranteed) < loanCaseDTO.AmountApplied)
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Loan is not fully secured by guarantors."));

        //                // 7. Collaterals
        //                var collateralDocuments = new List<CustomerDocumentDTO>();

        //                if (!string.IsNullOrWhiteSpace(loanCaseDTO.collateralIds))
        //                {
        //                    var collateralIds = loanCaseDTO.collateralIds
        //                        .Split(',')
        //                        .Where(x => Guid.TryParse(x, out _))
        //                        .Select(Guid.Parse);

        //                    foreach (var id in collateralIds)
        //                    {
        //                        var doc = await master._channelService.FindCustomerDocumentAsync(id, serviceHeader);

        //                        if (doc != null)
        //                            collateralDocuments.Add(doc);
        //                    }
        //                }

        //                // 8. Apply loan product rules
        //                MapLoanProductAttributes(loanCaseDTO, loanProduct);
        //                var investmentProducts = products.Where(p => p.CustomerAccountTypeProductCode == (int)ProductCode.Savings).ToList();

        //                List<decimal> iBalance = new List<decimal>();

        //                foreach (var investmentsBalances in investmentProducts)
        //                {
        //                    iBalance.Add(investmentsBalances.BookBalance);
        //                }
        //                var investmentsBalance = iBalance.Sum();
        //                if (loanCaseDTO.Remarks == "Boosted")
        //                {
        //                    var decimalreferenceamount = Convert.ToDecimal(loanCaseDTO.Reference);
        //                    investmentsBalance += decimalreferenceamount;
        //                }

        //                decimal loanLimit = investmentsBalance * Convert.ToDecimal(multiplier);

        //                if (loanCaseDTO.AmountApplied > loanLimit)
        //                {
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", $"Amount applied exceeds your available loan limit of {loanLimit:N2}."));
        //                }

        //                // 9. Persist loan
        //                loanCaseDTO.CreatedBy = User.Identity.Name;
        //                loanCaseDTO.Status = 48826;
        //                loanCaseDTO.LoanStatus = "48826";
        //                loanCaseDTO.ReceivedDate = DateTime.UtcNow;

        //                var createResult = await master._channelService
        //                    .AddLoanCaseAsync(
        //                        loanCaseDTO.MapTo<LoanCaseDTO>(),
        //                        serviceHeader);



        //                if (!string.IsNullOrWhiteSpace(createResult.ErrorMessageResult))
        //                {
        //                    return Ok(ApiResponse<string>.Fail(
        //                        "Error posting this loan.",
        //                        createResult.ErrorMessageResult
        //                    ));
        //                }

        //                // 10. Sector classification
        //                using (var conn = new SqlConnection(_connectionString))
        //                using (var cmd = new SqlCommand(@"
        //            INSERT INTO LoanCaseSectorClassification
        //                (LoanCaseId, SectorCode, SubSectorCode)
        //            VALUES
        //                (@LoanCaseId, @SectorCode, @SubSectorCode)", conn))
        //                {
        //                    cmd.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = createResult.Id;
        //                    cmd.Parameters.Add("@SectorCode", SqlDbType.VarChar, 20).Value = loanCaseDTO.SectorCode;
        //                    cmd.Parameters.Add("@SubSectorCode", SqlDbType.VarChar, 30).Value = loanCaseDTO.SubSectorCode;

        //                    await conn.OpenAsync();
        //                    await cmd.ExecuteNonQueryAsync();
        //                }

        //                // 11. Attach collaterals
        //                if (collateralDocuments.Any())
        //                {
        //                    await master._channelService
        //                        .UpdateLoanCollateralsByLoanCaseIdAsync(
        //                            createResult.Id,
        //                            new ObservableCollection<CustomerDocumentDTO>(collateralDocuments),
        //                            serviceHeader);
        //                }

        //                // 12. Attach guarantors
        //                await master._channelService
        //                    .UpdateLoanGuarantorsByLoanCaseIdAsync(
        //                        createResult.Id,
        //                        new ObservableCollection<LoanGuarantorDTO>(guarantors),
        //                        serviceHeader);

        //                // 13. Notify member
        //                //var sms =
        //                //    $"Dear {customer.IndividualFirstName} {customer.IndividualLastName}, " +
        //                //    $"your loan application of KES {loanCaseDTO.AmountApplied:N0} has been successfully registered and is under review.";

        //                //await SmsHelper.SendMessageAsync(customer.AddressMobileLine, sms);

        //                var fullName = $"{customer.IndividualFirstName} {customer.IndividualLastName}";
        //                var loanName = loanProduct.Description;
        //                var reference = loanCaseDTO.Reference;


        //                decimal principal = loanCaseDTO.AmountApplied;

        //                decimal annualRatePercent = (decimal)loanProduct.LoanInterestAnnualPercentageRate; // e.g. 12
        //                decimal monthlyRate = (annualRatePercent / 100m) / 12m;

        //                int months = loanProduct.LoanRegistrationTermInMonths;

        //                // EMI (annuity)
        //                decimal pow = (decimal)Math.Pow((double)(1 + monthlyRate), months);

        //                decimal rawEmi = principal * (monthlyRate * pow) / (pow - 1);

        //                // ROUND EMI FIRST (bank-grade behavior)
        //                decimal monthlyPayback = Math.Round(rawEmi, 2);

        //                // Totals derived from rounded EMI
        //                decimal totalPayback = Math.Round(monthlyPayback * months, 2);
        //                decimal totalInterest = Math.Round(totalPayback - principal, 2);

        //                // Assign snapshot
        //                loanCaseDTO.MonthlyPaybackAmount = monthlyPayback;
        //                loanCaseDTO.TotalPaybackAmount = totalPayback;
        //                loanCaseDTO.ApprovedInterestPayment = totalInterest;
        //                loanCaseDTO.AppraisedAmount = principal;
        //                loanCaseDTO.ApprovedAmount = principal;

        //                // Opening balances
        //                loanCaseDTO.TotalLoansBalance = totalPayback;          // total obligation
        //                loanCaseDTO.LoanProductLoanBalance = principal;        // outstanding principal at disbursement


        //                try
        //                {
        //                    using (SqlConnection conn = new SqlConnection(_connectionString))
        //                    {
        //                        conn.Open();

        //                        using (SqlTransaction tx = conn.BeginTransaction())
        //                        {
        //                            // 1. VALIDATE EXISTENCE
        //                            using (SqlCommand checkCmd = new SqlCommand(
        //                                "SELECT COUNT(1) FROM swiftFin_LoanCases WHERE Id = @Id", conn, tx))
        //                            {
        //                                checkCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
        //                                    .Value = createResult.Id;

        //                                if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
        //                                    throw new Exception("Validation failed: Loan case not found for update.");
        //                            }

        //                            // 2. UPDATE FINANCIAL & APPRAISAL SNAPSHOT
        //                            using (SqlCommand updateCmd = new SqlCommand(@"
        //UPDATE swiftFin_LoanCases
        //SET
        //    ApprovedAmount            = @ApprovedAmount,
        //    ApprovedInterestPayment   = @ApprovedInterestPayment,
        //    TotalPaybackAmount        = @TotalPaybackAmount,
        //    TotalLoansBalance         = @TotalLoansBalance,
        //    MonthlyPaybackAmount      = @MonthlyPaybackAmount,
        //    AppraisedAmount           = @AppraisedAmount,
        //    LoanProductLoanBalance    = @LoanProductLoanBalance
        //WHERE Id = @Id
        //", conn, tx))
        //                            {
        //                                var pApproved = updateCmd.Parameters.Add("@ApprovedAmount", SqlDbType.Decimal);
        //                                pApproved.Precision = 18; pApproved.Scale = 2;
        //                                pApproved.Value = loanCaseDTO.ApprovedAmount = loanCaseDTO.AmountApplied;

        //                                var pInterest = updateCmd.Parameters.Add("@ApprovedInterestPayment", SqlDbType.Decimal);
        //                                pInterest.Precision = 18; pInterest.Scale = 2;
        //                                pInterest.Value = loanCaseDTO.ApprovedInterestPayment;

        //                                var pTotal = updateCmd.Parameters.Add("@TotalPaybackAmount", SqlDbType.Decimal);
        //                                pTotal.Precision = 18; pTotal.Scale = 2;
        //                                pTotal.Value = loanCaseDTO.TotalPaybackAmount;

        //                                var pBalance = updateCmd.Parameters.Add("@TotalLoansBalance", SqlDbType.Decimal);
        //                                pBalance.Precision = 18; pBalance.Scale = 2;
        //                                pBalance.Value = loanCaseDTO.TotalLoansBalance = loanCaseDTO.ApprovedAmount + loanCaseDTO.ApprovedInterestPayment;

        //                                var pMonthly = updateCmd.Parameters.Add("@MonthlyPaybackAmount", SqlDbType.Decimal);
        //                                pMonthly.Precision = 18; pMonthly.Scale = 2;
        //                                pMonthly.Value = loanCaseDTO.MonthlyPaybackAmount;

        //                                var pAppraised = updateCmd.Parameters.Add("@AppraisedAmount", SqlDbType.Decimal);
        //                                pAppraised.Precision = 18; pAppraised.Scale = 2;
        //                                pAppraised.Value = loanCaseDTO.AppraisedAmount;

        //                                var pProductBalance = updateCmd.Parameters.Add("@LoanProductLoanBalance", SqlDbType.Decimal);
        //                                pProductBalance.Precision = 18; pProductBalance.Scale = 2;
        //                                pProductBalance.Value = loanCaseDTO.LoanProductLoanBalance;

        //                                updateCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
        //                                    .Value = createResult.Id;

        //                                if (updateCmd.ExecuteNonQuery() != 1)
        //                                    throw new Exception("Update failed: unexpected number of rows affected.");
        //                            }

        //                            tx.Commit();
        //                        }
        //                    }
        //                }
        //                catch
        //                {
        //                    throw; // bubble up to API/logging layer
        //                }

        //                return Ok(ApiResponse<string>.Ok(
        //                    "Loan created successfully .",
        //                    $"{loanName} loan for {fullName} has been registered successfully. witha a boost of ksh {reference}"
        //                ));

        //            }
        //            catch (Exception ex)
        //            {
        //                return Ok(ApiResponse<string>.Fail(
        //                    "System error occurred.",
        //                    ex.Message
        //                ));
        //            }
        //        }

        #endregion
        [HttpPost]
        [Route("LoanApplication")]
        public async Task<IHttpActionResult> Create([FromBody] LoanCaseDTO2 loanCaseDTO)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                if (loanCaseDTO == null)
                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Invalid request payload."));

                // 1. MEMBER
                var customer = await master._channelService.FindCustomerAsync(loanCaseDTO.CustomerId, serviceHeader);
                if (customer == null)
                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Member not found."));

                // 2. PRODUCT
                var loanProduct = await master._channelService
                    .FindLoanProductAsync(loanCaseDTO.LoanProductId, serviceHeader);
                if (loanProduct == null)
                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Invalid loan product."));

                if (loanCaseDTO.AmountApplied <= 0)
                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Amount must be greater than zero."));

                var branches = await master._channelService.FindBranchesAsync(serviceHeader);
                var branch = branches?.FirstOrDefault(b =>
                    b.Description != null &&
                    b.Description.StartsWith("Rubani", StringComparison.OrdinalIgnoreCase));
                if (branch != null)
                    loanCaseDTO.BranchId = branch.Id;

                // ================== VALIDATIONS ==================

                // ---- Amount vs product limits ----
                decimal minAmount = loanProduct.LoanRegistrationMinimumAmount;
                decimal maxAmount = loanProduct.LoanRegistrationMaximumAmount;

                if (minAmount > 0 && loanCaseDTO.AmountApplied < minAmount)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        $"Minimum loan amount is {minAmount:N2}."));

                if (maxAmount > 0 && loanCaseDTO.AmountApplied > maxAmount)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        $"Maximum loan amount is {maxAmount:N2}."));

                // ---- Membership period ----
                int membershipMonths = -1;
                if (customer.RegistrationDate != null &&
                    customer.RegistrationDate.Value <= DefaultSettings.Instance.ServerDate)
                {
                    membershipMonths = UberUtil.GetPeriod(
                        DefaultSettings.Instance.ServerDate,
                        customer.RegistrationDate.Value);
                }

                int requiredMembershipPeriod = loanProduct.LoanRegistrationMinimumMembershipPeriod;
                if (requiredMembershipPeriod > 0 && membershipMonths < requiredMembershipPeriod)
                {
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "Member does not meet minimum membership period."));
                }

                // ---- Interest sanity ----
                double apr = loanProduct.LoanInterestAnnualPercentageRate;
                if (apr <= 0)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "Invalid interest configuration for selected product."));

                // ================== CHECK FOR EXISTING ACTIVE LOAN (SQL) ==================
                using (var connCheck = new SqlConnection(_connectionString))
                {
                    var cmdText = @"
                SELECT TOP 1 [Id], [Reference], [TotalLoansBalance]
                FROM [dbo].[swiftFin_LoanCases]
                WHERE [CustomerId]       = @CustomerId
                  AND [LoanProductId]    = @LoanProductId
                  AND [TotalLoansBalance] > 0";

                    using (var cmdCheck = new SqlCommand(cmdText, connCheck))
                    {
                        cmdCheck.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = loanCaseDTO.CustomerId;
                        cmdCheck.Parameters.Add("@LoanProductId", SqlDbType.UniqueIdentifier).Value = loanCaseDTO.LoanProductId;

                        await connCheck.OpenAsync();
                        using (var reader = await cmdCheck.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                var existingRef = reader["Reference"] as string;
                                var existingId = reader["Id"].ToString();
                                var existingBalance = Convert.ToDecimal(reader["TotalLoansBalance"]);

                                return Ok(ApiResponse<string>.Fail(
                                    "Error posting this loan.",
                                    $"You already have an active {loanProduct.Description} loan that is not yet completed. " +
                                    $"Reference: {(string.IsNullOrEmpty(existingRef) ? existingId : existingRef)}, " +
                                    $"Outstanding Balance: {existingBalance:N2}. " +
                                    $"Please complete the existing loan before applying for a new one."
                                ));
                            }
                        }
                    }
                }
                // ================== END ACTIVE LOAN CHECK ==================

                // ---- Guarantors ----
                var guarantors = loanCaseDTO.Guarantors ?? new List<LoanGuarantorDTO>();
                int minGuarantors = loanProduct.LoanRegistrationMinimumGuarantors;
                int maxGuarantors = loanProduct.LoanRegistrationMaximumGuarantees;
                bool allowSelfGuarantee = loanProduct.LoanRegistrationAllowSelfGuarantee;

                if (minGuarantors > 0 && !guarantors.Any())
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "At least one guarantor is required."));

                if (guarantors.Count < minGuarantors)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        $"Minimum {minGuarantors} guarantors required."));

                if (maxGuarantors > 0 && guarantors.Count > maxGuarantors)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        $"Maximum {maxGuarantors} guarantors allowed."));

                if (guarantors.Any(g => g.AmountGuaranteed <= 0))
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "Guarantor amounts must be greater than zero."));

                if (guarantors.Select(g => g.CustomerId).Distinct().Count() != guarantors.Count)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "Duplicate guarantors detected."));

                if (!allowSelfGuarantee &&
                    guarantors.Any(g => g.CustomerId == loanCaseDTO.CustomerId))
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "Self-guarantee is not allowed for this product."));

                if (guarantors.Any() && guarantors.Sum(g => g.AmountGuaranteed) < loanCaseDTO.AmountApplied)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "Loan is not fully secured by guarantors."));

                // ---- Term consistency ----
                int productTerm = loanProduct.LoanRegistrationTermInMonths;
                if (productTerm > 0 &&
                    loanCaseDTO.LoanRegistrationTermInMonths > 0 &&
                    loanCaseDTO.LoanRegistrationTermInMonths != productTerm)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "Invalid loan term for selected product."));

                // ================== END VALIDATIONS ==================

                // 6. COLLATERALS
                var collateralDocuments = new List<CustomerDocumentDTO>();
                if (!string.IsNullOrWhiteSpace(loanCaseDTO.collateralIds))
                {
                    var collateralIds = loanCaseDTO.collateralIds
                        .Split(',')
                        .Where(x => Guid.TryParse(x, out _))
                        .Select(Guid.Parse);

                    foreach (var id in collateralIds)
                    {
                        var doc = await master._channelService.FindCustomerDocumentAsync(id, serviceHeader);
                        if (doc != null) collateralDocuments.Add(doc);
                    }
                }

                // 7. PRODUCT RULES
                MapLoanProductAttributes(loanCaseDTO, loanProduct);

                // 8. EMI + BALANCES
                decimal principal = loanCaseDTO.AmountApplied;
                decimal netDisbursement = principal;
                decimal annualRatePercent = (decimal)apr;
                decimal monthlyRate = (annualRatePercent / 100m) / 12m;

                int months = productTerm > 0
                    ? productTerm
                    : (loanCaseDTO.LoanRegistrationTermInMonths > 0
                        ? loanCaseDTO.LoanRegistrationTermInMonths
                        : 12);

                decimal pow = (decimal)Math.Pow((double)(1 + monthlyRate), months);
                decimal rawEmi = monthlyRate > 0
                                    ? principal * (monthlyRate * pow) / (pow - 1)
                                    : principal / months;

                decimal monthlyPayback = Math.Round(rawEmi, 2);
                decimal totalPayback = Math.Round(monthlyPayback * months, 2);
                decimal totalInterest = Math.Round(totalPayback - principal, 2);

                loanCaseDTO.MonthlyPaybackAmount = monthlyPayback;
                loanCaseDTO.TotalPaybackAmount = totalPayback;
                loanCaseDTO.ApprovedInterestPayment = totalInterest;
                loanCaseDTO.AppraisedAmount = principal;
                loanCaseDTO.ApprovedAmount = principal;
                loanCaseDTO.TotalLoansBalance = totalPayback;
                loanCaseDTO.LoanProductLoanBalance = principal;

                // 9. CREATE LOAN
                loanCaseDTO.CreatedBy = User.Identity.Name;
                loanCaseDTO.Status = 48826;
                loanCaseDTO.ReceivedDate = DateTime.UtcNow;

                if (loanCaseDTO.BatchNumber == 0)
                    loanCaseDTO.BatchNumber = int.Parse(DateTime.UtcNow.ToString("HHmmss"));

                var createResult = await master._channelService
                    .AddLoanCaseAsync(loanCaseDTO.MapTo<LoanCaseDTO>(), serviceHeader);

                if (!string.IsNullOrWhiteSpace(createResult.ErrorMessageResult))
                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", createResult.ErrorMessageResult));

                // 10. SECTOR
                #region Sector Classification
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(@"
            INSERT INTO LoanCaseSectorClassification (LoanCaseId, SectorCode, SubSectorCode)
            VALUES (@LoanCaseId, @SectorCode, @SubSectorCode)", conn))
                {
                    cmd.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = createResult.Id;

                    cmd.Parameters.Add("@SectorCode", SqlDbType.VarChar, 20).Value =
                        string.IsNullOrEmpty(loanCaseDTO.SectorCode)
                            ? (object)DBNull.Value
                            : loanCaseDTO.SectorCode;

                    cmd.Parameters.Add("@SubSectorCode", SqlDbType.VarChar, 30).Value =
                        string.IsNullOrEmpty(loanCaseDTO.SubSectorCode)
                            ? (object)DBNull.Value
                            : loanCaseDTO.SubSectorCode;

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
                #endregion

                // 11. COLLATERALS
                if (collateralDocuments.Any())
                {
                    await master._channelService.UpdateLoanCollateralsByLoanCaseIdAsync(
                        createResult.Id,
                        new ObservableCollection<CustomerDocumentDTO>(collateralDocuments),
                        serviceHeader);
                }

                // 12. GUARANTORS
                await master._channelService.UpdateLoanGuarantorsByLoanCaseIdAsync(
                    createResult.Id,
                    new ObservableCollection<LoanGuarantorDTO>(guarantors),
                    serviceHeader);

                // ================== SMS TO GUARANTORS ==================
                try
                {
                    var loaneeFullName = $"{customer.IndividualFirstName} {customer.IndividualLastName}";

                    foreach (var guarantor in guarantors)
                    {
                        if (guarantor.CustomerId == Guid.Empty) continue;

                        var guarantorCustomer = await master._channelService
                            .FindCustomerAsync(guarantor.CustomerId, serviceHeader);

                        if (guarantorCustomer == null) continue;

                        var guarantorMobile = guarantorCustomer.AddressMobileLine;
                        if (string.IsNullOrWhiteSpace(guarantorMobile)) continue;

                        var guarantorFullName = $"{guarantorCustomer.IndividualFirstName} {guarantorCustomer.IndividualLastName}";

                        string guarantorMessage =
                            $"Dear {guarantorFullName}, " +
                            $"you have been listed as a guarantor for a {loanProduct.Description} loan of " +
                            $"KES {guarantor.AmountGuaranteed:N0} for {loaneeFullName}. " +
                            $"The loan application is pending approval.";

                        await SmsHelper.SendMessageAsync(guarantorMobile, guarantorMessage);
                    }
                }
                catch (Exception ex)
                {
                    // SMS failure should not fail loan creation
                    System.Diagnostics.Debug.WriteLine($"Guarantor SMS FAILED: {ex.Message}");
                }

                var fullName = $"{customer.IndividualFirstName} {customer.IndividualLastName}";
                var loanName = loanProduct.Description;

                return Ok(ApiResponse<string>.Ok(
                    "Loan created successfully.",
                    $"{loanName} loan for {fullName} registered successfully. Net disbursement: {netDisbursement:N2}"
                ));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<string>.Fail("System error occurred.", ex.Message));
            }
        }


        [HttpPost]
        [Route("EditLoanApplication")]
        public async Task<IHttpActionResult> EditLoanApplication([FromBody] LoanCaseDTO2 loanCaseDTO)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                if (loanCaseDTO == null || loanCaseDTO.Id == Guid.Empty)
                    return Ok(ApiResponse<string>.Fail("Error updating this loan.", "Invalid request payload."));

                // ================== LOAD EXISTING LOAN (MUST BE AT REGISTERED STATE) ==================
                int existingStatus = 0;

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(@"
            SELECT [Status]
            FROM [dbo].[swiftFin_LoanCases]
            WHERE [Id] = @Id", conn))
                {
                    cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = loanCaseDTO.Id;

                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!reader.Read())
                            return Ok(ApiResponse<string>.Fail("Error updating this loan.", "Loan case not found."));

                        existingStatus = Convert.ToInt32(reader["Status"] == DBNull.Value ? 0 : reader["Status"]);
                    }
                }

                const int REGISTERED_STATUS = 48826;
                if (existingStatus != REGISTERED_STATUS)
                    return Ok(ApiResponse<string>.Fail(
                        "Error updating this loan.",
                        $"Loan can only be edited while at registered state. Current status: {existingStatus}."));

                // 1. MEMBER
                var customer = await master._channelService.FindCustomerAsync(loanCaseDTO.CustomerId, serviceHeader);
                if (customer == null)
                    return Ok(ApiResponse<string>.Fail("Error updating this loan.", "Member not found."));

                // 2. PRODUCT
                var loanProduct = await master._channelService
                    .FindLoanProductAsync(loanCaseDTO.LoanProductId, serviceHeader);
                if (loanProduct == null)
                    return Ok(ApiResponse<string>.Fail("Error updating this loan.", "Invalid loan product."));

                if (loanCaseDTO.AmountApplied <= 0)
                    return Ok(ApiResponse<string>.Fail("Error updating this loan.", "Amount must be greater than zero."));

                // ================== VALIDATIONS ==================

                decimal minAmount = loanProduct.LoanRegistrationMinimumAmount;
                decimal maxAmount = loanProduct.LoanRegistrationMaximumAmount;

                if (minAmount > 0 && loanCaseDTO.AmountApplied < minAmount)
                    return Ok(ApiResponse<string>.Fail(
                        "Error updating this loan.",
                        $"Minimum loan amount is {minAmount:N2}."));

                if (maxAmount > 0 && loanCaseDTO.AmountApplied > maxAmount)
                    return Ok(ApiResponse<string>.Fail(
                        "Error updating this loan.",
                        $"Maximum loan amount is {maxAmount:N2}."));

                int membershipMonths = -1;
                if (customer.RegistrationDate != null &&
                    customer.RegistrationDate.Value <= DefaultSettings.Instance.ServerDate)
                {
                    membershipMonths = UberUtil.GetPeriod(
                        DefaultSettings.Instance.ServerDate,
                        customer.RegistrationDate.Value);
                }

                int requiredMembershipPeriod = loanProduct.LoanRegistrationMinimumMembershipPeriod;
                if (requiredMembershipPeriod > 0 && membershipMonths < requiredMembershipPeriod)
                {
                    return Ok(ApiResponse<string>.Fail(
                        "Error updating this loan.",
                        "Member does not meet minimum membership period."));
                }

                double apr = loanProduct.LoanInterestAnnualPercentageRate;
                if (apr <= 0)
                    return Ok(ApiResponse<string>.Fail(
                        "Error updating this loan.",
                        "Invalid interest configuration for selected product."));

                // ================== CHECK FOR OTHER ACTIVE LOANS (excluding this one) ==================
                using (var connCheck = new SqlConnection(_connectionString))
                {
                    var cmdText = @"
                SELECT TOP 1 [Id], [Reference], [TotalLoansBalance]
                FROM [dbo].[swiftFin_LoanCases]
                WHERE [CustomerId]       = @CustomerId
                  AND [LoanProductId]    = @LoanProductId
                  AND [Id]               != @CurrentLoanId
                  AND [TotalLoansBalance] > 0";

                    using (var cmdCheck = new SqlCommand(cmdText, connCheck))
                    {
                        cmdCheck.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = loanCaseDTO.CustomerId;
                        cmdCheck.Parameters.Add("@LoanProductId", SqlDbType.UniqueIdentifier).Value = loanCaseDTO.LoanProductId;
                        cmdCheck.Parameters.Add("@CurrentLoanId", SqlDbType.UniqueIdentifier).Value = loanCaseDTO.Id;

                        await connCheck.OpenAsync();
                        using (var reader = await cmdCheck.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                var existingRef = reader["Reference"] as string;
                                var existingId = reader["Id"].ToString();
                                var existingBalance = Convert.ToDecimal(reader["TotalLoansBalance"]);

                                return Ok(ApiResponse<string>.Fail(
                                    "Error updating this loan.",
                                    $"You already have an active {loanProduct.Description} loan that is not yet completed. " +
                                    $"Reference: {(string.IsNullOrEmpty(existingRef) ? existingId : existingRef)}, " +
                                    $"Outstanding Balance: {existingBalance:N2}. " +
                                    $"Please complete the existing loan before editing this one to use the same product."
                                ));
                            }
                        }
                    }
                }

                // ---- Guarantors ----
                var guarantors = loanCaseDTO.Guarantors ?? new List<LoanGuarantorDTO>();
                int minGuarantors = loanProduct.LoanRegistrationMinimumGuarantors;
                int maxGuarantors = loanProduct.LoanRegistrationMaximumGuarantees;
                bool allowSelfGuarantee = loanProduct.LoanRegistrationAllowSelfGuarantee;

                if (minGuarantors > 0 && !guarantors.Any())
                    return Ok(ApiResponse<string>.Fail(
                        "Error updating this loan.",
                        "At least one guarantor is required."));

                if (guarantors.Count < minGuarantors)
                    return Ok(ApiResponse<string>.Fail(
                        "Error updating this loan.",
                        $"Minimum {minGuarantors} guarantors required."));

                if (maxGuarantors > 0 && guarantors.Count > maxGuarantors)
                    return Ok(ApiResponse<string>.Fail(
                        "Error updating this loan.",
                        $"Maximum {maxGuarantors} guarantors allowed."));

                if (guarantors.Any(g => g.AmountGuaranteed <= 0))
                    return Ok(ApiResponse<string>.Fail(
                        "Error updating this loan.",
                        "Guarantor amounts must be greater than zero."));

                if (guarantors.Select(g => g.CustomerId).Distinct().Count() != guarantors.Count)
                    return Ok(ApiResponse<string>.Fail(
                        "Error updating this loan.",
                        "Duplicate guarantors detected."));

                if (!allowSelfGuarantee &&
                    guarantors.Any(g => g.CustomerId == loanCaseDTO.CustomerId))
                    return Ok(ApiResponse<string>.Fail(
                        "Error updating this loan.",
                        "Self-guarantee is not allowed for this product."));

                if (guarantors.Any() && guarantors.Sum(g => g.AmountGuaranteed) < loanCaseDTO.AmountApplied)
                    return Ok(ApiResponse<string>.Fail(
                        "Error updating this loan.",
                        "Loan is not fully secured by guarantors."));

                // ---- Term consistency ----
                int productTerm = loanProduct.LoanRegistrationTermInMonths;
                if (productTerm > 0 &&
                    loanCaseDTO.LoanRegistrationTermInMonths > 0 &&
                    loanCaseDTO.LoanRegistrationTermInMonths != productTerm)
                    return Ok(ApiResponse<string>.Fail(
                        "Error updating this loan.",
                        "Invalid loan term for selected product."));

                // ================== END VALIDATIONS ==================

                // 6. EMI + BALANCES
                decimal principal = loanCaseDTO.AmountApplied;
                decimal annualRatePercent = (decimal)apr;
                decimal monthlyRate = (annualRatePercent / 100m) / 12m;

                int months = productTerm > 0
                    ? productTerm
                    : (loanCaseDTO.LoanRegistrationTermInMonths > 0
                        ? loanCaseDTO.LoanRegistrationTermInMonths
                        : 12);

                decimal pow = (decimal)Math.Pow((double)(1 + monthlyRate), months);
                decimal rawEmi = monthlyRate > 0
                                    ? principal * (monthlyRate * pow) / (pow - 1)
                                    : principal / months;

                decimal monthlyPayback = Math.Round(rawEmi, 2);
                decimal totalPayback = Math.Round(monthlyPayback * months, 2);
                decimal totalInterest = Math.Round(totalPayback - principal, 2);

                // ================== UPDATE VIA SQL (using product's raw values directly) ==================
                try
                {
                    using (var conn = new SqlConnection(_connectionString))
                    {
                        await conn.OpenAsync();

                        using (var tx = conn.BeginTransaction())
                        {
                            // 1. VALIDATE EXISTENCE
                            using (var checkCmd = new SqlCommand(
                                "SELECT COUNT(1) FROM swiftFin_LoanCases WHERE Id = @Id", conn, tx))
                            {
                                checkCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = loanCaseDTO.Id;

                                if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) == 0)
                                    throw new Exception("Loan case not found for update.");
                            }

                            // 2. UPDATE EDITABLE FIELDS + RECALCULATED SNAPSHOT
                            // Snapshot fields pulled from a fully-populated row matching this LoanProductId
                            // (re-uses the same approach as the SQL UPDATE for BOSA migration rows earlier)
                            using (var updateCmd = new SqlCommand(@"
                        UPDATE lc
                        SET
                            lc.[AmountApplied]            = @AmountApplied,
                            lc.[LoanProductId]            = @LoanProductId,
                            lc.[LoanPurposeId]            = @LoanPurposeId,
                            lc.[SavingsProductId]         = @SavingsProductId,
                            lc.[BranchId]                 = @BranchId,
                            lc.[Remarks]                  = @Remarks,
                            lc.[Reference]                = @Reference,

                            lc.[MonthlyPaybackAmount]     = @MonthlyPaybackAmount,
                            lc.[TotalPaybackAmount]       = @TotalPaybackAmount,
                            lc.[ApprovedInterestPayment]  = @ApprovedInterestPayment,
                            lc.[AppraisedAmount]          = @AppraisedAmount,
                            lc.[ApprovedAmount]           = @ApprovedAmount,
                            lc.[TotalLoansBalance]        = @TotalLoansBalance,
                            lc.[LoanProductLoanBalance]   = @LoanProductLoanBalance,

                            -- snapshot fields copied from a reference row that matches the (new) product
                            lc.[LoanInterest_AnnualPercentageRate]   = ref.[LoanInterest_AnnualPercentageRate],
                            lc.[LoanInterest_ChargeMode]             = ref.[LoanInterest_ChargeMode],
                            lc.[LoanInterest_RecoveryMode]           = ref.[LoanInterest_RecoveryMode],
                            lc.[LoanInterest_CalculationMode]        = ref.[LoanInterest_CalculationMode],
                            lc.[LoanRegistration_TermInMonths]       = ref.[LoanRegistration_TermInMonths],
                            lc.[LoanRegistration_MinimumAmount]      = ref.[LoanRegistration_MinimumAmount],
                            lc.[LoanRegistration_MaximumAmount]      = ref.[LoanRegistration_MaximumAmount],
                            lc.[LoanRegistration_MinimumInterestAmount] = ref.[LoanRegistration_MinimumInterestAmount],
                            lc.[LoanRegistration_LoanProductSection] = ref.[LoanRegistration_LoanProductSection],
                            lc.[LoanRegistration_LoanProductCategory] = ref.[LoanRegistration_LoanProductCategory],
                            lc.[LoanRegistration_ConsecutiveIncome]  = ref.[LoanRegistration_ConsecutiveIncome],
                            lc.[LoanRegistration_InvestmentsMultiplier] = ref.[LoanRegistration_InvestmentsMultiplier],
                            lc.[LoanRegistration_MinimumGuarantors]  = ref.[LoanRegistration_MinimumGuarantors],
                            lc.[LoanRegistration_MaximumGuarantees]  = ref.[LoanRegistration_MaximumGuarantees],
                            lc.[LoanRegistration_RejectIfMemberHasBalance] = ref.[LoanRegistration_RejectIfMemberHasBalance],
                            lc.[LoanRegistration_SecurityRequired]   = ref.[LoanRegistration_SecurityRequired],
                            lc.[LoanRegistration_AllowSelfGuarantee] = ref.[LoanRegistration_AllowSelfGuarantee],
                            lc.[LoanRegistration_GracePeriod]        = ref.[LoanRegistration_GracePeriod],
                            lc.[LoanRegistration_MinimumMembershipPeriod] = ref.[LoanRegistration_MinimumMembershipPeriod],
                            lc.[LoanRegistration_PaymentFrequencyPerYear] = ref.[LoanRegistration_PaymentFrequencyPerYear],
                            lc.[LoanRegistration_PaymentDueDate]     = ref.[LoanRegistration_PaymentDueDate],
                            lc.[LoanRegistration_PayoutRecoveryMode] = ref.[LoanRegistration_PayoutRecoveryMode],
                            lc.[LoanRegistration_PayoutRecoveryPercentage] = ref.[LoanRegistration_PayoutRecoveryPercentage],
                            lc.[LoanRegistration_AggregateCheckOffRecoveryMode] = ref.[LoanRegistration_AggregateCheckOffRecoveryMode],
                            lc.[LoanRegistration_ChargeClearanceFee] = ref.[LoanRegistration_ChargeClearanceFee],
                            lc.[LoanRegistration_Microcredit]        = ref.[LoanRegistration_Microcredit],
                            lc.[LoanRegistration_StandingOrderTrigger] = ref.[LoanRegistration_StandingOrderTrigger],
                            lc.[LoanRegistration_TrackArrears]       = ref.[LoanRegistration_TrackArrears],
                            lc.[LoanRegistration_ChargeArrearsFee]   = ref.[LoanRegistration_ChargeArrearsFee],
                            lc.[LoanRegistration_EnforceSystemAppraisalRecommendation] = ref.[LoanRegistration_EnforceSystemAppraisalRecommendation],
                            lc.[LoanRegistration_BypassAudit]        = ref.[LoanRegistration_BypassAudit],
                            lc.[LoanRegistration_MaximumSelfGuaranteeEligiblePercentage] = ref.[LoanRegistration_MaximumSelfGuaranteeEligiblePercentage],
                            lc.[LoanRegistration_GuarantorSecurityMode] = ref.[LoanRegistration_GuarantorSecurityMode],
                            lc.[LoanRegistration_RoundingType]       = ref.[LoanRegistration_RoundingType],
                            lc.[LoanRegistration_DisburseMicroLoanLessDeductions] = ref.[LoanRegistration_DisburseMicroLoanLessDeductions],
                            lc.[LoanRegistration_ExcludeOutstandingLoansOnMaximumEntitlement] = ref.[LoanRegistration_ExcludeOutstandingLoansOnMaximumEntitlement],
                            lc.[LoanRegistration_ConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal] = ref.[LoanRegistration_ConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal],
                            lc.[LoanRegistration_ThrottleScheduledArrearsRecovery] = ref.[LoanRegistration_ThrottleScheduledArrearsRecovery],
                            lc.[LoanRegistration_CreateStandingOrderOnLoanAudit] = ref.[LoanRegistration_CreateStandingOrderOnLoanAudit],
                            lc.[MaximumAmountPercentage]             = ref.[MaximumAmountPercentage],
                            lc.[TakeHome_Type]                       = ref.[TakeHome_Type],
                            lc.[TakeHome_Percentage]                 = ref.[TakeHome_Percentage],
                            lc.[TakeHome_FixedAmount]                = ref.[TakeHome_FixedAmount]

                        FROM [dbo].[swiftFin_LoanCases] lc
                        CROSS JOIN [dbo].[swiftFin_LoanProducts] ref
                        WHERE lc.[Id] = @Id
                          AND ref.[Id] = @LoanProductId", conn, tx))
                            {
                                updateCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = loanCaseDTO.Id;
                                updateCmd.Parameters.Add("@LoanProductId", SqlDbType.UniqueIdentifier).Value = loanCaseDTO.LoanProductId;

                                updateCmd.Parameters.Add("@LoanPurposeId", SqlDbType.UniqueIdentifier).Value =
                                    loanCaseDTO.LoanPurposeId == Guid.Empty ? (object)DBNull.Value : loanCaseDTO.LoanPurposeId;

                                updateCmd.Parameters.Add("@SavingsProductId", SqlDbType.UniqueIdentifier).Value =
                                    loanCaseDTO.SavingsProductId == Guid.Empty ? (object)DBNull.Value : loanCaseDTO.SavingsProductId;

                                updateCmd.Parameters.Add("@BranchId", SqlDbType.UniqueIdentifier).Value = loanCaseDTO.BranchId;
                                updateCmd.Parameters.Add("@Remarks", SqlDbType.NVarChar, -1).Value = (object)loanCaseDTO.Remarks ?? DBNull.Value;
                                updateCmd.Parameters.Add("@Reference", SqlDbType.NVarChar, 200).Value = (object)loanCaseDTO.Reference ?? DBNull.Value;

                                AddDecimalParam(updateCmd, "@AmountApplied", principal);
                                AddDecimalParam(updateCmd, "@MonthlyPaybackAmount", monthlyPayback);
                                AddDecimalParam(updateCmd, "@TotalPaybackAmount", totalPayback);
                                AddDecimalParam(updateCmd, "@ApprovedInterestPayment", totalInterest);
                                AddDecimalParam(updateCmd, "@AppraisedAmount", principal);
                                AddDecimalParam(updateCmd, "@ApprovedAmount", principal);
                                AddDecimalParam(updateCmd, "@TotalLoansBalance", totalPayback);
                                AddDecimalParam(updateCmd, "@LoanProductLoanBalance", principal);

                                if (await updateCmd.ExecuteNonQueryAsync() != 1)
                                    throw new Exception("Update failed: unexpected rows affected (loan or product not found).");
                            }

                            tx.Commit();
                        }
                    }
                }
                catch (Exception ex)
                {
                    return Ok(ApiResponse<string>.Fail("Error updating this loan.", ex.Message));
                }

                // ================== UPDATE GUARANTORS ==================
                await master._channelService.UpdateLoanGuarantorsByLoanCaseIdAsync(
                    loanCaseDTO.Id,
                    new ObservableCollection<LoanGuarantorDTO>(guarantors),
                    serviceHeader);

                var fullName = $"{customer.IndividualFirstName} {customer.IndividualLastName}";

                return Ok(ApiResponse<string>.Ok(
                    "Loan updated successfully.",
                    $"{loanProduct.Description} loan for {fullName} updated successfully. New monthly payback: {monthlyPayback:N2}"
                ));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<string>.Fail("System error occurred.", ex.Message));
            }
        }

        // ================== HELPER ==================
        private static void AddDecimalParam(SqlCommand cmd, string name, decimal value)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Decimal);
            p.Precision = 18;
            p.Scale = 2;
            p.Value = value;
        }


        [HttpPut]
        [Route("UpdateLoanCase")]
        public async Task<IHttpActionResult> UpdateLoanCase(LoanCaseDTO loanCaseDTO)
        {
            try
            {
                // Validate input
                if (loanCaseDTO == null || loanCaseDTO.Id == Guid.Empty)
                {
                    return Ok(ApiResponse<string>.Fail(
                        "Invalid request. Loan case data is required and ID must be provided."));
                }

                var serviceHeader = master.GetServiceHeader();

                // Optional: Add authorization check (implement this method if needed)
                // if (!UserHasPermission(serviceHeader, "UpdateLoanCase"))
                // {
                //     return Unauthorized();
                // }

                var success = await master._channelService.UpdateLoanCaseAsync(loanCaseDTO, serviceHeader);

                if (success)
                {
                    return Ok(ApiResponse<bool>.Ok(true, "Loan case updated successfully."));
                }
                else
                {
                    return Ok(ApiResponse<bool>.Fail(
                        "Failed to update loan case. The loan case could not be updated."));
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                // _logger.LogError(ex, "Error updating loan case with ID: {LoanCaseId}", loanCaseDTO?.Id);

                return InternalServerError(new Exception("An error occurred while updating the loan case.", ex));
            }
        }


        [HttpPost]
        [Route("loan/appraisal")]
        public async Task<IHttpActionResult> LoanAppraisal([FromBody] LoanCaseDTO request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var serviceHeader = master.GetServiceHeader();

            var loanCase = await master._channelService.FindLoanCaseAsync(request.Id, serviceHeader);
            if (request.IsBatched == true)
            {
                decimal Intrest = request.InterestBalance;
                decimal totalIncome = request.LoanRegistrationTotalIncome;
                decimal existingDeductions = request.LoanRegistrationTotalDeduction;

                decimal newLoanRepayment = request.LoanRegistrationAbilityToPay;

                decimal maxAllowedDeductions = totalIncome * 2m / 3m;
                decimal projectedTotalDeductions = existingDeductions + newLoanRepayment;

                if (projectedTotalDeductions > maxAllowedDeductions)
                {
                    return BadRequest("2/3 rule violation. Total deductions exceed allowable limit.");

                }
            }
            //loanCase.AppraisalRemarks = request.AppraisalRemarks;

            ////loanCase.AppraisedAmount = request.AppraisedAmount;
            ////loanCase.AppraisedAmountRemarks = request.AppraisedAmountRemarks;
            ////loanCase.AppraisedNetIncome = request.AppraisedNetIncome;
            ////loanCase.AppraisedAbility = request.AppraisedAbility;
            ////loanCase.ApprovedAmount = request.ApprovedAmount;

            //loanCase.LoanRegistrationMaximumEntitled = request.LoanRegistrationMaximumEntitled;
            //loanCase.LoanRegistrationNetIncome = request.LoanRegistrationNetIncome;
            //loanCase.LoanRegistrationTotalAllowance = request.LoanRegistrationTotalAllowance;
            //loanCase.LoanRegistrationTotalDeduction = request.LoanRegistrationTotalDeduction;
            //loanCase.LoanRegistrationTotalIncome = request.LoanRegistrationTotalIncome;
            //loanCase.LoanRegistrationAbilityToPay = request.LoanRegistrationAbilityToPay;
            //loanCase.LoanRegistrationAbilityToPayOverLoanTerm = request.LoanRegistrationAbilityToPayOverLoanTerm;
            //loanCase.LoanRegistrationLoanPlusInterest = request.LoanRegistrationLoanPlusInterest;

            loanCase.TotalLoansBalance = request.TotalLoansBalance;
            //loanCase.LoanProductInvestmentsBalance = request.LoanProductInvestmentsBalance;
            //loanCase.LoanProductTotalSharesInvestmentsBalance = request.LoanProductTotalSharesInvestmentsBalance;
            //loanCase.Status = (int)LoanCaseStatus.Registered;


            if (loanCase == null)
                return NotFoundResponse("Loan case not found.");

            //loanCase.ValidateAll();
            //if (loanCase.HasErrors)
            //    return ValidationErrorResponse(loanCase.ErrorMessages);

            // Phase 1: Appraisal

            var appraised = await master._channelService.AppraiseLoanCaseAsync(loanCase, (int)LoanAppraisalOption.Appraise, 1, serviceHeader);

            if (!appraised)
                return FailureResponse("Loan appraisal failed.");


            // Refresh state only after successful appraisal
            //  await master._channelService.UpdateLoanCaseAsync(loanCase, serviceHeader);
            loanCase = await master._channelService.FindLoanCaseAsync(loanCase.Id, serviceHeader);

            loanCase.Status = (int)LoanCaseStatus.Approved;

            // Phase 2: Audit
            var audited = await master._channelService.AuditLoanCaseAsync(loanCase, (int)LoanAuditOption.Audit, serviceHeader);

            if (!audited)
            {
                string message1 =
                    $"Dear {loanCase.Customer.IndividualFirstName} {loanCase.Customer.IndividualLastName}, " +
                    $"loan application of KES {loanCase.AmountApplied:N0} did not meet the appraisal requirements at this time. " +
                    $"Please contact us for further clarification or future consideration.";
                await SmsHelper.SendMessageAsync(loanCase.Customer.AddressMobileLine, message1);

                return FailureResponse("Loan audit failed.");
            }
            //        string message = $"Dear {loanCase.Customer.IndividualFirstName} {loanCase.Customer.IndividualLastName}, " +
            //$"your loan application of KES {loanCase.AmountApplied:N0} has successfully passed appraisal and is awaiting final approval. " +
            //$"We will keep you informed of the next steps.";
            //        await SmsHelper.SendMessageAsync(loanCase.Customer.AddressMobileLine, message);


            //decimal rate = Convert.ToDecimal(loanCase.LoanInterestAnnualPercentageRate / 100); // if stored as 12 not 0.12
            //decimal termInYears = loanCase.LoanRegistrationTermInMonths / 12m;

            //loanCase.ApprovedInterestPayment = principal * rate * termInYears;
            //loanCase.TotalPaybackAmount = loanCase.AmountApplied + loanCase.ApprovedInterestPayment;
            //loanCase.AppraisedAmount = loanCase.AmountApplied;


            decimal principal = loanCase.AmountApplied;

            decimal annualRatePercent = (decimal)loanCase.LoanInterestAnnualPercentageRate; // e.g. 12
            decimal monthlyRate = (annualRatePercent / 100m) / 12m;

            int months = loanCase.LoanRegistrationTermInMonths;

            // EMI (annuity)
            decimal pow = (decimal)Math.Pow((double)(1 + monthlyRate), months);

            decimal rawEmi = principal * (monthlyRate * pow) / (pow - 1);

            // ROUND EMI FIRST (bank-grade behavior)
            decimal monthlyPayback = Math.Round(rawEmi, 2);

            // Totals derived from rounded EMI
            decimal totalPayback = Math.Round(monthlyPayback * months, 2);
            decimal totalInterest = Math.Round(totalPayback - principal, 2);

            // Assign snapshot
            loanCase.MonthlyPaybackAmount = monthlyPayback;
            loanCase.TotalPaybackAmount = totalPayback;
            loanCase.ApprovedInterestPayment = totalInterest;
            loanCase.AppraisedAmount = principal;
            loanCase.ApprovedAmount = principal;

            // Opening balances
            loanCase.TotalLoansBalance = totalPayback;          // total obligation
            loanCase.LoanProductLoanBalance = principal;        // outstanding principal at disbursement

            #region
            //            try
            //            {
            //                using (SqlConnection conn = new SqlConnection(_connectionString))
            //                {
            //                    conn.Open();

            //                    using (SqlTransaction tx = conn.BeginTransaction())
            //                    {
            //                        // 1. VALIDATE EXISTENCE
            //                        using (SqlCommand checkCmd = new SqlCommand(
            //                            "SELECT COUNT(1) FROM swiftFin_LoanCases WHERE Id = @Id", conn, tx))
            //                        {
            //                            checkCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
            //                                .Value = loanCase.Id;

            //                            if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
            //                                throw new Exception("Validation failed: Loan case not found for update.");
            //                        }

            //                        // 2. UPDATE FINANCIAL & APPRAISAL SNAPSHOT
            //                        using (SqlCommand updateCmd = new SqlCommand(@"
            //UPDATE swiftFin_LoanCases
            //SET
            //    ApprovedAmount            = @ApprovedAmount,
            //    ApprovedInterestPayment   = @ApprovedInterestPayment,
            //    TotalPaybackAmount        = @TotalPaybackAmount,
            //    TotalLoansBalance         = @TotalLoansBalance,
            //    MonthlyPaybackAmount      = @MonthlyPaybackAmount,
            //    AppraisedAmount           = @AppraisedAmount,
            //    LoanProductLoanBalance    = @LoanProductLoanBalance
            //WHERE Id = @Id
            //", conn, tx))
            //                        {
            //                            var pApproved = updateCmd.Parameters.Add("@ApprovedAmount", SqlDbType.Decimal);
            //                            pApproved.Precision = 18; pApproved.Scale = 2;
            //                            pApproved.Value = loanCase.ApprovedAmount = loanCase.AmountApplied;

            //                            var pInterest = updateCmd.Parameters.Add("@ApprovedInterestPayment", SqlDbType.Decimal);
            //                            pInterest.Precision = 18; pInterest.Scale = 2;
            //                            pInterest.Value = loanCase.ApprovedInterestPayment;

            //                            var pTotal = updateCmd.Parameters.Add("@TotalPaybackAmount", SqlDbType.Decimal);
            //                            pTotal.Precision = 18; pTotal.Scale = 2;
            //                            pTotal.Value = loanCase.TotalPaybackAmount;

            //                            var pBalance = updateCmd.Parameters.Add("@TotalLoansBalance", SqlDbType.Decimal);
            //                            pBalance.Precision = 18; pBalance.Scale = 2;
            //                            pBalance.Value = loanCase.TotalLoansBalance = loanCase.ApprovedAmount + loanCase.ApprovedInterestPayment;

            //                            var pMonthly = updateCmd.Parameters.Add("@MonthlyPaybackAmount", SqlDbType.Decimal);
            //                            pMonthly.Precision = 18; pMonthly.Scale = 2;
            //                            pMonthly.Value = loanCase.MonthlyPaybackAmount;

            //                            var pAppraised = updateCmd.Parameters.Add("@AppraisedAmount", SqlDbType.Decimal);
            //                            pAppraised.Precision = 18; pAppraised.Scale = 2;
            //                            pAppraised.Value = loanCase.AppraisedAmount;

            //                            var pProductBalance = updateCmd.Parameters.Add("@LoanProductLoanBalance", SqlDbType.Decimal);
            //                            pProductBalance.Precision = 18; pProductBalance.Scale = 2;
            //                            pProductBalance.Value = loanCase.LoanProductLoanBalance;

            //                            updateCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
            //                                .Value = loanCase.Id;

            //                            if (updateCmd.ExecuteNonQuery() != 1)
            //                                throw new Exception("Update failed: unexpected number of rows affected.");
            //                        }

            //                        tx.Commit();
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                throw; // bubble up to API/logging layer
            //            }
            #endregion

            return Ok(SuccessResponse("Loan appraised and audited successfully."));
        }

        [HttpPost]
        [Route("Approve")]
        public async Task<IHttpActionResult> ApproveLoan([FromBody] LoanCaseDTO request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var serviceHeader = master.GetServiceHeader();

            // ================== LOAD LOAN CASE VIA SQL ==================
            LoanCaseDTO loanCaseDTO = null;

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1
                    lc.[Id],
                    lc.[CustomerId],
                    lc.[LoanProductId],
                    lc.[BranchId],
                    lc.[AmountApplied],
                    lc.[MonthlyPaybackAmount],
                    lc.[TotalPaybackAmount],
                    lc.[ApprovedAmount],
                    lc.[ApprovedInterestPayment],
                    lc.[AppraisedAmount],
                    lc.[TotalLoansBalance],
                    lc.[LoanProductLoanBalance],
                    lc.[Status],
                    lc.[CaseNumber],
                    lc.[Reference],
                    lc.[Remarks],
                    lc.[LoanInterest_AnnualPercentageRate],
                    lc.[LoanRegistration_TermInMonths],
                    c.[Individual_FirstName],
                    c.[Individual_LastName],
                    c.[Address_MobileLine]
                FROM [dbo].[swiftFin_LoanCases] lc
                INNER JOIN [dbo].[swiftFin_Customers] c ON c.[Id] = lc.[CustomerId]
                WHERE lc.[Id] = @Id", conn))
                    {
                        cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = request.Id;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.Read())
                                return Content(HttpStatusCode.NotFound, new ApiResponse<object>
                                {
                                    Success = false,
                                    Message = "Loan case not found."
                                });

                            loanCaseDTO = new LoanCaseDTO
                            {
                                Id = new Guid(Convert.ToString(reader["Id"])),
                                CustomerId = new Guid(Convert.ToString(reader["CustomerId"])),
                                LoanProductId = new Guid(Convert.ToString(reader["LoanProductId"])),
                                BranchId = reader["BranchId"] != DBNull.Value
                                                    ? new Guid(Convert.ToString(reader["BranchId"]))
                                                    : Guid.Empty,

                                AmountApplied = Convert.ToDecimal(reader["AmountApplied"] == DBNull.Value ? 0 : reader["AmountApplied"]),
                                MonthlyPaybackAmount = Convert.ToDecimal(reader["MonthlyPaybackAmount"] == DBNull.Value ? 0 : reader["MonthlyPaybackAmount"]),
                                TotalPaybackAmount = Convert.ToDecimal(reader["TotalPaybackAmount"] == DBNull.Value ? 0 : reader["TotalPaybackAmount"]),
                                ApprovedAmount = Convert.ToDecimal(reader["ApprovedAmount"] == DBNull.Value ? 0 : reader["ApprovedAmount"]),
                                ApprovedInterestPayment = Convert.ToDecimal(reader["ApprovedInterestPayment"] == DBNull.Value ? 0 : reader["ApprovedInterestPayment"]),
                                AppraisedAmount = Convert.ToDecimal(reader["AppraisedAmount"] == DBNull.Value ? 0 : reader["AppraisedAmount"]),
                                TotalLoansBalance = Convert.ToDecimal(reader["TotalLoansBalance"] == DBNull.Value ? 0 : reader["TotalLoansBalance"]),
                                LoanProductLoanBalance = Convert.ToDecimal(reader["LoanProductLoanBalance"] == DBNull.Value ? 0 : reader["LoanProductLoanBalance"]),
                                Status = Convert.ToInt32(reader["Status"] == DBNull.Value ? 0 : reader["Status"]),
                                CaseNumber = Convert.ToInt32(reader["CaseNumber"] == DBNull.Value ? 0 : reader["CaseNumber"]),
                                Reference = reader["Reference"] as string,
                                Remarks = reader["Remarks"] as string,
                                LoanInterestAnnualPercentageRate = Convert.ToDouble(reader["LoanInterest_AnnualPercentageRate"] == DBNull.Value ? 0 : reader["LoanInterest_AnnualPercentageRate"]),
                                LoanRegistrationTermInMonths = Convert.ToInt32(reader["LoanRegistration_TermInMonths"] == DBNull.Value ? 0 : reader["LoanRegistration_TermInMonths"]),

                                Customer = new CustomerDTO
                                {
                                    IndividualFirstName = reader["Individual_FirstName"] as string,
                                    IndividualLastName = reader["Individual_LastName"] as string,
                                    AddressMobileLine = reader["Address_MobileLine"] as string
                                }
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"STEP 1 FAILED - Load loan case: {ex.Message} | Inner: {ex.InnerException?.Message}"
                });
            }

            // ================== CALCULATE EMI BEFORE APPROVE ==================
            try
            {
                decimal principal = loanCaseDTO.AmountApplied;
                decimal annualRatePercent = (decimal)loanCaseDTO.LoanInterestAnnualPercentageRate;
                decimal monthlyRate = (annualRatePercent / 100m) / 12m;
                int months = loanCaseDTO.LoanRegistrationTermInMonths;

                decimal pow = (decimal)Math.Pow((double)(1 + monthlyRate), months);
                decimal rawEmi = monthlyRate > 0
                                    ? principal * (monthlyRate * pow) / (pow - 1)
                                    : principal / months;

                decimal monthlyPayback = Math.Round(rawEmi, 2);
                decimal totalPayback = Math.Round(monthlyPayback * months, 2);
                decimal totalInterest = Math.Round(totalPayback - principal, 2);

                loanCaseDTO.ApprovedAmount = principal;
                loanCaseDTO.AppraisedAmount = principal;
                loanCaseDTO.MonthlyPaybackAmount = monthlyPayback;
                loanCaseDTO.TotalPaybackAmount = totalPayback;
                loanCaseDTO.ApprovedInterestPayment = totalInterest;
                loanCaseDTO.TotalLoansBalance = principal;
                loanCaseDTO.LoanProductLoanBalance = principal;
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"STEP 2 FAILED - EMI calculation: {ex.Message} | Principal: {loanCaseDTO?.AmountApplied} | Rate: {loanCaseDTO?.LoanInterestAnnualPercentageRate} | Months: {loanCaseDTO?.LoanRegistrationTermInMonths}"
                });
            }

            // ================== APPROVE LOAN ==================
            try
            {
                var auditResult = await master._channelService
                    .ApproveLoanCaseAsync(loanCaseDTO, (int)LoanApprovalOption.Approve, serviceHeader);

                if (!auditResult)
                    return Content(HttpStatusCode.InternalServerError, new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"STEP 3 FAILED - ApproveLoanCaseAsync returned false. LoanId: {loanCaseDTO.Id}, Status: {loanCaseDTO.Status}, ApprovedAmount: {loanCaseDTO.ApprovedAmount}"
                    });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"STEP 3 FAILED - ApproveLoanCaseAsync exception: {ex.Message} | Inner: {ex.InnerException?.Message}"
                });
            }

            // ================== LOAD LOAN PRODUCT ==================
            LoanProductDTO loanProductDTO = null;
            try
            {
                loanProductDTO = await master._channelService
                    .FindLoanProductAsync(loanCaseDTO.LoanProductId, serviceHeader);

                if (loanProductDTO == null)
                    return Ok(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"STEP 4 FAILED - Loan product not found for Id: {loanCaseDTO.LoanProductId}"
                    });
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"STEP 4 FAILED - FindLoanProductAsync: {ex.Message} | Inner: {ex.InnerException?.Message}"
                });
            }

            // ================== SET FINAL STATUS ==================
            loanCaseDTO.Status = (int)LoanCaseStatus.Approved;

            // ================== UPDATE VIA SQL ==================
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (var tx = conn.BeginTransaction())
                    {
                        // 1. VALIDATE EXISTENCE
                        using (var checkCmd = new SqlCommand(
                            "SELECT COUNT(1) FROM swiftFin_LoanCases WHERE Id = @Id", conn, tx))
                        {
                            checkCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = loanCaseDTO.Id;

                            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) == 0)
                                throw new Exception("Loan case not found for update.");
                        }

                        // 2. UPDATE FINANCIAL SNAPSHOT + STATUS
                        using (var updateCmd = new SqlCommand(@"
                    UPDATE swiftFin_LoanCases
                    SET
                        ApprovedAmount          = @ApprovedAmount,
                        ApprovedInterestPayment = @ApprovedInterestPayment,
                        TotalPaybackAmount      = @TotalPaybackAmount,
                        TotalLoansBalance       = @TotalLoansBalance,
                        MonthlyPaybackAmount    = @MonthlyPaybackAmount,
                        AppraisedAmount         = @AppraisedAmount,
                        LoanProductLoanBalance  = @LoanProductLoanBalance,
                        Status                  = @Status
                    WHERE Id = @Id", conn, tx))
                        {
                            var pApproved = updateCmd.Parameters.Add("@ApprovedAmount", SqlDbType.Decimal);
                            pApproved.Precision = 18; pApproved.Scale = 2;
                            pApproved.Value = loanCaseDTO.ApprovedAmount;

                            var pInterest = updateCmd.Parameters.Add("@ApprovedInterestPayment", SqlDbType.Decimal);
                            pInterest.Precision = 18; pInterest.Scale = 2;
                            pInterest.Value = loanCaseDTO.ApprovedInterestPayment;

                            var pTotal = updateCmd.Parameters.Add("@TotalPaybackAmount", SqlDbType.Decimal);
                            pTotal.Precision = 18; pTotal.Scale = 2;
                            pTotal.Value = loanCaseDTO.TotalPaybackAmount;

                            var pBalance = updateCmd.Parameters.Add("@TotalLoansBalance", SqlDbType.Decimal);
                            pBalance.Precision = 18; pBalance.Scale = 2;
                            pBalance.Value = loanCaseDTO.TotalLoansBalance;

                            var pMonthly = updateCmd.Parameters.Add("@MonthlyPaybackAmount", SqlDbType.Decimal);
                            pMonthly.Precision = 18; pMonthly.Scale = 2;
                            pMonthly.Value = loanCaseDTO.MonthlyPaybackAmount;

                            var pAppraised = updateCmd.Parameters.Add("@AppraisedAmount", SqlDbType.Decimal);
                            pAppraised.Precision = 18; pAppraised.Scale = 2;
                            pAppraised.Value = loanCaseDTO.AppraisedAmount;

                            var pProductBalance = updateCmd.Parameters.Add("@LoanProductLoanBalance", SqlDbType.Decimal);
                            pProductBalance.Precision = 18; pProductBalance.Scale = 2;
                            pProductBalance.Value = loanCaseDTO.LoanProductLoanBalance;

                            updateCmd.Parameters.Add("@Status", SqlDbType.Int).Value = loanCaseDTO.Status;
                            updateCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = loanCaseDTO.Id;

                            if (await updateCmd.ExecuteNonQueryAsync() != 1)
                                throw new Exception("Update failed: unexpected rows affected.");
                        }

                        tx.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"STEP 5 FAILED - SQL Update: {ex.Message} | Inner: {ex.InnerException?.Message}"
                });
            }

            // ================== SMS TO LOANEE ONLY ==================
            try
            {
                string loaneeFullName = $"{loanCaseDTO.Customer.IndividualFirstName} {loanCaseDTO.Customer.IndividualLastName}";

                string loaneeMessage =
                    $"Dear {loaneeFullName}, " +
                    $"your {loanProductDTO.Description} loan of KES {loanCaseDTO.AmountApplied:N0} " +
                    $"has been approved and is awaiting disbursement.";

                await SmsHelper.SendMessageAsync(loanCaseDTO.Customer.AddressMobileLine, loaneeMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SMS FAILED: {ex.Message}");
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Loan approved successfully."
            });
        }


        [HttpPost]
        [Route("LoanCancellation")]
        public async Task<IHttpActionResult> LoanCancellation([FromBody] LoanCaseDTO loanCaseDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (loanCaseDTO == null)
                return Json(new ApiResponse<object> { Success = false, Message = "Invalid data.", Data = null });



            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {
                var result = await master._channelService.CancelLoanCaseAsync(loanCaseDTO, loanCaseDTO.LoanAuditOption, serviceHeader);


                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "loan  Cancelled successfully."
                });
            }

            return Json(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed.",
                Data = loanCaseDTO.ErrorMessages
            });
        }

        [HttpPost]
        [Route("TransferToDefaulters")]
        public async Task<IHttpActionResult> TransferToDefaulters([FromBody] TransferToDefaultersRequest request)
        {
            if (request == null || request.LoanCaseId == Guid.Empty)
                return BadRequest("LoanCaseId is required.");

            if (string.IsNullOrWhiteSpace(request.TransferredBy))
                return BadRequest("TransferredBy is required.");

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // ── 1. Load the original loan case ───────────────────────────
                    Guid loaneeCustomerId = Guid.Empty;
                    Guid loanProductId = Guid.Empty;
                    decimal outstandingBalance = 0m;
                    string loanReference = "";
                    string loanProductName = "";
                    int loanCaseStatus = 0;
                    int caseNumber = 0;
                    string loaneeFirst = "";
                    string loaneeLast = "";
                    string loaneeIdNo = "";

                    using (var cmd = new SqlCommand(@"
                SELECT
                    lc.CustomerId,
                    lc.LoanProductId,
                    lc.TotalLoansBalance,
                    lc.Reference,
                    lc.Status,
                    lc.CaseNumber,
                    ISNULL(lp.Description, '')          AS LoanProductName,
                    ISNULL(c.Individual_FirstName, '')   AS LoaneeFirst,
                    ISNULL(c.Individual_LastName,  '')   AS LoaneeLastName,
                    ISNULL(c.Individual_IdentityCardNumber, '') AS LoaneeIDNo
                FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanCases] lc
                INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Customers] c
                    ON lc.CustomerId = c.Id
                LEFT JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanProducts] lp
                    ON lc.LoanProductId = lp.Id
                WHERE lc.Id = @LoanCaseId", conn))
                    {
                        cmd.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = request.LoanCaseId;
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            if (!await r.ReadAsync())
                                return BadRequest("Loan case not found.");

                            loaneeCustomerId = r.GetGuid(r.GetOrdinal("CustomerId"));
                            loanProductId = r.GetGuid(r.GetOrdinal("LoanProductId"));
                            outstandingBalance = Convert.ToDecimal(r["TotalLoansBalance"]);
                            loanReference = r["Reference"]?.ToString();
                            loanCaseStatus = Convert.ToInt32(r["Status"]);
                            caseNumber = Convert.ToInt32(r["CaseNumber"]);
                            loanProductName = r["LoanProductName"]?.ToString();
                            loaneeFirst = r["LoaneeFirst"]?.ToString() ?? "";
                            loaneeLast = r["LoaneeLastName"]?.ToString() ?? "";
                            loaneeIdNo = r["LoaneeIDNo"]?.ToString();
                        }
                    }

                    if (outstandingBalance <= 0)
                        return BadRequest("Loan has no outstanding balance — nothing to transfer.");

                    // ── 2. Load guarantors for this loan ──────────────────────────
                    var guarantors = new List<GuarantorInfo>();

                    using (var cmd = new SqlCommand(@"
                SELECT
                    g.Id                                AS GuarantorId,
                    g.CustomerId                        AS GuarantorCustomerId,
                    g.AmountGuaranteed,
                    ISNULL(c.Individual_FirstName, '')  AS GuarantorFirst,
                    ISNULL(c.Individual_LastName,  '')  AS GuarantorLast,
                    ISNULL(c.Individual_IdentityCardNumber, '') AS GuarantorIDNo,
                    c.Address_MobileLine                AS GuarantorPhone
                FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanGuarantors] g
                INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Customers] c
                    ON g.CustomerId = c.Id
                WHERE g.LoanCaseId = @LoanCaseId
                  AND g.Status     = 1", conn)) // Status=1 = Approved guarantors only
                    {
                        cmd.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = request.LoanCaseId;
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            while (await r.ReadAsync())
                            {
                                guarantors.Add(new GuarantorInfo
                                {
                                    GuarantorId = r.GetGuid(r.GetOrdinal("GuarantorId")),
                                    CustomerId = r.GetGuid(r.GetOrdinal("GuarantorCustomerId")),
                                    AmountGuaranteed = Convert.ToDecimal(r["AmountGuaranteed"]),
                                    FirstName = r["GuarantorFirst"]?.ToString() ?? "",
                                    LastName = r["GuarantorLast"]?.ToString() ?? "",
                                    IDNumber = r["GuarantorIDNo"]?.ToString(),
                                    Phone = r["GuarantorPhone"]?.ToString()
                                });
                            }
                        }
                    }

                    if (!guarantors.Any())
                        return BadRequest("No approved guarantors found for this loan. Cannot transfer to defaulters.");

                    // ── 3. Load DEFAULTER LOAN product (Code=7) ───────────────────
                    Guid defaulterProductId = Guid.Empty;
                    Guid defaulterCoAId = Guid.Empty;
                    Guid defaulterBranchId = Guid.Parse("5C84C824-0CE3-455B-A5C5-994E2BFBA380");
                    decimal defaulterAPR = 0m;
                    short defaulterTermMonths = 48;
                    short defaulterMinGuarantors = 0;
                    short defaulterMaxGuarantees = 50;
                    bool defaulterBypassAudit = true;
                    bool defaulterTrackArrears = true;

                    using (var cmd = new SqlCommand(@"
                SELECT
                    lp.Id,
                    lp.ChartOfAccountId,
                    lp.LoanInterest_AnnualPercentageRate,
                    lp.LoanRegistration_TermInMonths,
                    lp.LoanRegistration_MinimumGuarantors,
                    lp.LoanRegistration_MaximumGuarantees,
                    lp.LoanRegistration_BypassAudit,
                    lp.LoanRegistration_TrackArrears
                FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanProducts] lp
                WHERE lp.Code = 7", conn)) // DEFAULTER LOAN
                    {
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            if (!await r.ReadAsync())
                                return BadRequest("DEFAULTER LOAN product (Code=7) not found.");

                            defaulterProductId = r.GetGuid(r.GetOrdinal("Id"));
                            var coaOrd = r.GetOrdinal("ChartOfAccountId");
                            if (!r.IsDBNull(coaOrd)) defaulterCoAId = r.GetGuid(coaOrd);
                            defaulterAPR = Convert.ToDecimal(r["LoanInterest_AnnualPercentageRate"]);
                            defaulterTermMonths = Convert.ToInt16(r["LoanRegistration_TermInMonths"]);
                            defaulterMinGuarantors = Convert.ToInt16(r["LoanRegistration_MinimumGuarantors"]);
                            defaulterMaxGuarantees = Convert.ToInt16(r["LoanRegistration_MaximumGuarantees"]);
                            defaulterBypassAudit = Convert.ToBoolean(r["LoanRegistration_BypassAudit"]);
                            defaulterTrackArrears = Convert.ToBoolean(r["LoanRegistration_TrackArrears"]);
                        }
                    }

                    // ── 4. Calculate equal share per guarantor ────────────────────
                    int guarantorCount = guarantors.Count;
                    decimal sharePerGuarantor = Math.Round(outstandingBalance / guarantorCount, 2);

                    // Distribute any rounding remainder to the last guarantor
                    decimal lastShare = outstandingBalance - (sharePerGuarantor * (guarantorCount - 1));

                    // ── 5. Get active posting period ──────────────────────────────
                    Guid postingPeriodId = Guid.Empty;
                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1 Id FROM swiftFin_PostingPeriods
                WHERE GETDATE() BETWEEN Duration_StartDate AND Duration_EndDate
                  AND IsActive = 1", conn))
                    {
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null) postingPeriodId = (Guid)result;
                    }

                    if (postingPeriodId == Guid.Empty)
                        return BadRequest("No active posting period found.");

                    var createdDefaulterLoans = new List<object>();

                    using (var tx = conn.BeginTransaction(System.Data.IsolationLevel.Serializable))
                    {
                        try
                        {
                            // ── 6. Get next CaseNumber under lock ─────────────────
                            int nextCaseNumber = 1;
                            using (var cmd = new SqlCommand(@"
                        SELECT ISNULL(MAX(CaseNumber), 0) + 1
                        FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanCases]
                        WITH (UPDLOCK, HOLDLOCK)", conn, tx))
                            {
                                nextCaseNumber = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                            }

                            // ── 7. Create ONE defaulter loan case per guarantor ───
                            for (int i = 0; i < guarantors.Count; i++)
                            {
                                var guarantor = guarantors[i];
                                decimal amount = (i == guarantors.Count - 1) ? lastShare : sharePerGuarantor;
                                Guid newLoanId = Guid.NewGuid();
                                string newRef = $"DEF-{caseNumber}-{guarantor.FirstName.ToUpper()}-{nextCaseNumber}";

                                // ── Insert defaulter loan case ────────────────────
                                using (var cmd = new SqlCommand(@"
                            INSERT INTO [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanCases]
                            (
                                Id, CustomerId, LoanProductId, BranchId,
                                CaseNumber, AmountApplied, AppraisedAmount,
                                ApprovedAmount, ApprovedPrincipalPayment, ApprovedInterestPayment,
                                MonthlyPaybackAmount, TotalPaybackAmount, TotalLoansBalance,
                                LoanProductLoanBalance, LoanProductInvestmentsBalance, LoanProductLatestIncome,
                                LoanInterest_AnnualPercentageRate, LoanInterest_ChargeMode,
                                LoanInterest_RecoveryMode, LoanInterest_CalculationMode,
                                LoanRegistration_TermInMonths, LoanRegistration_MinimumGuarantors,
                                LoanRegistration_MaximumGuarantees, LoanRegistration_AllowSelfGuarantee,
                                LoanRegistration_MinimumMembershipPeriod, LoanRegistration_BypassAudit,
                                LoanRegistration_TrackArrears, LoanRegistration_RejectIfMemberHasBalance,
                                LoanRegistration_SecurityRequired,
                                Status, BatchNumber, IsBatched,
                                ReceivedDate, DisbursedDate, DisbursedAmount,
                                Remarks, Reference, SequentialId, CreatedBy, CreatedDate
                            )
                            VALUES
                            (
                                @Id, @CustomerId, @LoanProductId, @BranchId,
                                @CaseNumber, @Amount, @Amount,
                                @Amount, @Amount, 0,
                                @Amount, @Amount, @Amount,
                                @Amount, 0, 0,
                                @APR, 301, 401, 201,
                                @TermMonths, @MinGuarantors,
                                @MaxGuarantees, 1,
                                0, @BypassAudit,
                                @TrackArrears, 0, 0,
                                48829, 0, 0,
                                @Now, @Now, @Amount,
                                @Remarks, @Reference, @SequentialId, @CreatedBy, @Now
                            )", conn, tx))
                                {
                                    cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = newLoanId;
                                    cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = guarantor.CustomerId;
                                    cmd.Parameters.Add("@LoanProductId", SqlDbType.UniqueIdentifier).Value = defaulterProductId;
                                    cmd.Parameters.Add("@BranchId", SqlDbType.UniqueIdentifier).Value = defaulterBranchId;
                                    cmd.Parameters.Add("@CaseNumber", SqlDbType.Int).Value = nextCaseNumber;
                                    cmd.Parameters.Add("@Amount", SqlDbType.Decimal).Value = amount;
                                    cmd.Parameters.Add("@APR", SqlDbType.Float).Value = (double)defaulterAPR;
                                    cmd.Parameters.Add("@TermMonths", SqlDbType.SmallInt).Value = defaulterTermMonths;
                                    cmd.Parameters.Add("@MinGuarantors", SqlDbType.SmallInt).Value = defaulterMinGuarantors;
                                    cmd.Parameters.Add("@MaxGuarantees", SqlDbType.SmallInt).Value = defaulterMaxGuarantees;
                                    cmd.Parameters.Add("@BypassAudit", SqlDbType.Bit).Value = defaulterBypassAudit;
                                    cmd.Parameters.Add("@TrackArrears", SqlDbType.Bit).Value = defaulterTrackArrears;
                                    cmd.Parameters.Add("@Now", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                                    cmd.Parameters.Add("@Remarks", SqlDbType.NVarChar).Value =
                                        $"Defaulter transfer from loan {loanReference} " +
                                        $"(Case #{caseNumber}). Original borrower: {loaneeFirst} {loaneeLast}. " +
                                        $"Transferred by: {request.TransferredBy}. " +
                                        $"Reason: {request.Remarks ?? "Loan default"}";
                                    cmd.Parameters.Add("@Reference", SqlDbType.NVarChar).Value = newRef;
                                    cmd.Parameters.Add("@SequentialId", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                                    cmd.Parameters.Add("@CreatedBy", SqlDbType.NVarChar).Value = request.TransferredBy;
                                    await cmd.ExecuteNonQueryAsync();
                                }

                                createdDefaulterLoans.Add(new
                                {
                                    defaulterLoanCaseId = newLoanId,
                                    caseNumber = nextCaseNumber,
                                    reference = newRef,
                                    guarantorCustomerId = guarantor.CustomerId,
                                    guarantorName = $"{guarantor.FirstName} {guarantor.LastName}".Trim(),
                                    amountAssigned = amount
                                });

                                nextCaseNumber++;
                            }

                            // ── 8. Write off original loanee loan ─────────────────
                            // Status 48833 = Written Off (check your status enum and adjust if different)
                            using (var cmd = new SqlCommand(@"
                        UPDATE [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanCases]
                        SET
                            Status            = 48833,
                            TotalLoansBalance = 0,
                            Remarks           = ISNULL(Remarks, '') +
                                                ' | Written off ' + CONVERT(VARCHAR, GETDATE(), 23) +
                                                ' — transferred to guarantors by ' + @TransferredBy
                        WHERE Id = @LoanCaseId", conn, tx))
                            {
                                cmd.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = request.LoanCaseId;
                                cmd.Parameters.Add("@TransferredBy", SqlDbType.NVarChar).Value = request.TransferredBy;
                                await cmd.ExecuteNonQueryAsync();
                            }

                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }

                    // ── 9. SMS to each guarantor ──────────────────────────────────
                    string loaneeName = $"{loaneeFirst} {loaneeLast}".Trim();

                    foreach (var guarantor in guarantors)
                    {
                        try
                        {
                            string phone = await GetPhoneNumber(guarantor.IDNumber);
                            if (string.IsNullOrWhiteSpace(phone)) continue;

                            var created = createdDefaulterLoans
                                .FirstOrDefault(d =>
                                {
                                    var t = d.GetType();
                                    return (Guid)t.GetProperty("guarantorCustomerId").GetValue(d) == guarantor.CustomerId;
                                });

                            decimal assigned = created != null
                                ? (decimal)created.GetType().GetProperty("amountAssigned").GetValue(created)
                                : sharePerGuarantor;

                            string ref_ = created != null
                                ? created.GetType().GetProperty("reference").GetValue(created)?.ToString()
                                : "";

                            string msg =
                                $"Dear {guarantor.FirstName} {guarantor.LastName}, " +
                                $"a loan of KES {assigned:N2} has been transferred to you " +
                                $"as guarantor for {loaneeName}'s defaulted {loanProductName} loan " +
                                $"(Ref: {loanReference}). Your defaulter loan reference is {ref_}. " +
                                $"Please contact us immediately. RUBANI SACCO.";

                            await SmsHelper.SendMessageAsync(phone, msg);
                        }
                        catch { }
                    }

                    // ── 10. SMS to loanee ─────────────────────────────────────────
                    try
                    {
                        string loaneePhone = await GetPhoneNumber(loaneeIdNo);
                        if (!string.IsNullOrWhiteSpace(loaneePhone))
                        {
                            string msg =
                                $"Dear {loaneeName}, your {loanProductName} loan " +
                                $"(Ref: {loanReference}, Balance: KES {outstandingBalance:N2}) " +
                                $"has been written off and transferred to your guarantors " +
                                $"due to non-payment. Please contact us urgently. RUBANI SACCO.";
                            await SmsHelper.SendMessageAsync(loaneePhone, msg);
                        }
                    }
                    catch { }

                    return Ok(new
                    {
                        success = true,
                        message = $"Loan transferred to {guarantorCount} guarantor(s) as defaulter loans.",
                        originalLoanCaseId = request.LoanCaseId,
                        originalReference = loanReference,
                        originalBalance = outstandingBalance,
                        guarantorCount = guarantorCount,
                        sharePerGuarantor = sharePerGuarantor,
                        defaulterLoansCreated = createdDefaulterLoans
                    });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(
                    new Exception($"Failed to transfer loan to defaulters: {ex.Message}", ex));
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // MODELS
        // ─────────────────────────────────────────────────────────────────────────────
        public class TransferToDefaultersRequest
        {
            public Guid LoanCaseId { get; set; }
            public string TransferredBy { get; set; }
            public string Remarks { get; set; }
        }


        private async Task<string> GetPhoneNumber(string idNumber)
        {
            if (string.IsNullOrWhiteSpace(idNumber)) return null;

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
            SELECT TOP 1 PhoneNumber
            FROM Registration
            WHERE IDNumber = @IDNumber
              AND Status   = 'Active'", conn))
                {
                    cmd.Parameters.Add("@IDNumber", SqlDbType.NVarChar).Value = idNumber;
                    var result = await cmd.ExecuteScalarAsync();
                    return result != null && result != DBNull.Value
                        ? result.ToString()
                        : null;
                }
            }
        }
        private class GuarantorInfo
        {
            public Guid GuarantorId { get; set; }
            public Guid CustomerId { get; set; }
            public decimal AmountGuaranteed { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string IDNumber { get; set; }
            public string Phone { get; set; }
        }



        // Request DTO
        public class LoanTopUpRequest
        {
            public Guid OriginalLoanCaseId { get; set; }
            public decimal TopUpAmount { get; set; }
            public Guid BankAccountId { get; set; }
            public ServiceHeader ServiceHeader { get; set; } // pass user info, context, etc.
        }






        #region  PDF

        [HttpGet]
        [Route("CustomerLoanLedgerPdf")]
        public IHttpActionResult CustomerLoanLedgerPdf(Guid customerId)
        {
            var results = new List<CustomerLoanLedgerDto>();

            #region SQL
            var sql = @"
WITH Tx AS (
    SELECT
        ca.Id AS CustomerAccountId,
        c.Individual_FirstName + ' ' + c.Individual_LastName AS FullName,
        CONCAT(
            RIGHT('000' + CAST(b.Code AS VARCHAR(10)), 3), '-',
            RIGHT('000000' + CAST(c.SerialNumber AS VARCHAR(10)), 6), '-',
            RIGHT('000' + CAST(ca.CustomerAccountType_ProductCode AS VARCHAR(10)), 3), '-',
            RIGHT('000' + CAST(ca.CustomerAccountType_TargetProductCode AS VARCHAR(10)), 3)
        ) AS AccountNumber,
        lp.Description AS LoanProductName,
        je.CreatedDate,
        je.Amount
    FROM [swiftFin_JournalEntries] je
    INNER JOIN [swiftFin_CustomerAccounts] ca ON je.CustomerAccountId = ca.Id
    INNER JOIN [swiftFin_Customers] c ON ca.CustomerId = c.Id
    INNER JOIN [swiftFin_LoanProducts] lp ON ca.CustomerAccountType_TargetProductId = lp.Id
    LEFT JOIN [swiftFin_Branches] b ON ca.BranchId = b.Id
    WHERE ca.CustomerId = @CustomerId
)
SELECT *,
    SUM(Amount) OVER (
        PARTITION BY CustomerAccountId
        ORDER BY CreatedDate
        ROWS UNBOUNDED PRECEDING
    ) AS RunningBalance
FROM Tx
ORDER BY AccountNumber, CreatedDate;";
            #endregion

            #region DATA FETCH
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new CustomerLoanLedgerDto
                        {
                            CustomerAccountId = reader.GetGuid(reader.GetOrdinal("CustomerAccountId")),
                            FullName = reader.GetString(reader.GetOrdinal("FullName")),
                            AccountNumber = reader.GetString(reader.GetOrdinal("AccountNumber")),
                            LoanProductName = reader.GetString(reader.GetOrdinal("LoanProductName")),
                            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                            Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                            RunningBalance = reader.GetDecimal(reader.GetOrdinal("RunningBalance"))
                        });
                    }
                }
            }

            if (!results.Any())
                return BadRequest("No ledger records found.");
            #endregion

            #region PDF BUILD
            byte[] pdfBytes;

            using (var ms = new MemoryStream())
            {
                var doc = new Document(PageSize.A4, 36, 36, 36, 36);
                var writer = PdfWriter.GetInstance(doc, ms);
                writer.PageEvent = new PageBackground(); // light blue page tint

                doc.Open();

                AddHeader(doc, results.First());
                AddLedgerTable(doc, results);
                AddFooter(doc);

                doc.Close();
                pdfBytes = ms.ToArray();
            }
            #endregion

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(pdfBytes)
            };

            response.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

            response.Content.Headers.ContentDisposition =
                new System.Net.Http.Headers.ContentDispositionHeaderValue("inline")
                {
                    FileName = "Rubani_Loan_Ledger.pdf"
                };

            return ResponseMessage(response);
        }


        // ============================
        // PDF STYLES
        // ============================
        static Font H1 = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
        static Font H2 = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
        static Font Normal = FontFactory.GetFont(FontFactory.HELVETICA, 9);
        static Font Bold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);

        static BaseColor BrandBlue = new BaseColor(220, 235, 250);
        public class PageBackground : PdfPageEventHelper
        {
            public override void OnEndPage(PdfWriter writer, Document document)
            {
                var cb = writer.DirectContentUnder;
                cb.SetColorFill(new BaseColor(245, 250, 255));

                cb.Rectangle(
                    document.LeftMargin,
                    document.BottomMargin,
                    document.PageSize.Width - document.LeftMargin - document.RightMargin,
                    document.PageSize.Height - document.TopMargin - document.BottomMargin
                );
                cb.Fill();
            }
        }


        // ============================
        // HEADER
        // ============================
        private void AddHeader(Document doc, CustomerLoanLedgerDto first)
        {
            var headerTable = new PdfPTable(2);
            headerTable.WidthPercentage = 100;
            headerTable.SetWidths(new float[] { 1f, 3f });

            var logoPath = HttpContext.Current.Server.MapPath("~/Assets/Images/Rubani-logo.jpeg");
            var logo = Image.GetInstance(logoPath);
            logo.ScaleToFit(80f, 80f);

            var logoCell = new PdfPCell(logo)
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 5
            };

            var titleCell = new PdfPCell
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_CENTER
            };

            titleCell.AddElement(new Paragraph("RUBANI SACCO SOCIETY LTD", H1));
            titleCell.AddElement(new Paragraph("Loan Account Statement", H2));

            headerTable.AddCell(logoCell);
            headerTable.AddCell(titleCell);

            doc.Add(headerTable);
            doc.Add(new Paragraph(" "));

            var info = new PdfPTable(2);
            info.WidthPercentage = 100;
            info.SetWidths(new float[] { 1, 1 });

            info.AddCell(InfoCell($"Member Name: {first.FullName}"));
            info.AddCell(InfoCell($"Account No: {first.AccountNumber}"));
            info.AddCell(InfoCell($"Loan Product: {first.LoanProductName}"));
            info.AddCell(InfoCell($"Generated: {DateTime.Now:dd MMM yyyy}"));

            doc.Add(info);
            doc.Add(new Paragraph(" "));
        }


        // ============================
        // LEDGER TABLE (NO IDS)
        // ============================
        private void AddLedgerTable(Document doc, List<CustomerLoanLedgerDto> data)
        {
            var table = new PdfPTable(4);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1.2f, 2.5f, 1f, 1f });

            table.AddCell(HeaderCell("Date"));
            table.AddCell(HeaderCell("Description"));
            table.AddCell(HeaderCell("Amount"));
            table.AddCell(HeaderCell("Balance"));

            foreach (var tx in data)
            {
                table.AddCell(Cell(tx.CreatedDate.ToString("dd-MMM-yyyy")));
                table.AddCell(Cell(tx.LoanProductName));
                table.AddCell(Cell(tx.Amount.ToString("N2"), Element.ALIGN_RIGHT));
                table.AddCell(Cell(tx.RunningBalance.ToString("N2"), Element.ALIGN_RIGHT));
            }

            doc.Add(table);

            var closing = data.Last().RunningBalance;
            doc.Add(new Paragraph($"Closing Balance: {closing:N2}", Bold)
            { Alignment = Element.ALIGN_RIGHT });
        }

        // ============================
        // FOOTER
        // ============================
        private void AddFooter(Document doc)
        {
            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph("System Generated Statement", Normal));
            doc.Add(new Paragraph("Rubani SACCO Society Ltd", Bold));
        }

        // ============================
        // CELL HELPERS
        // ============================

        private PdfPCell Cell(string text, int align = Element.ALIGN_LEFT)
        {
            return new PdfPCell(new Phrase(text, Normal))
            {
                HorizontalAlignment = align,
                Padding = 5
            };
        }

        private PdfPCell HeaderCell(string text)
        {
            return new PdfPCell(new Phrase(text, Bold))
            {
                BackgroundColor = BrandBlue,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 6
            };
        }



        private PdfPCell InfoCell(string text)
        {
            return new PdfPCell(new Phrase(text, Normal))
            {
                Border = Rectangle.NO_BORDER,
                Padding = 4
            };
        }


        // ============================
        // DTO
        // ============================
        private class CustomerLoanLedgerDto
        {
            public Guid CustomerAccountId { get; set; } // internal only
            public string FullName { get; set; }
            public string AccountNumber { get; set; }
            public string LoanProductName { get; set; }
            public DateTime CreatedDate { get; set; }
            public decimal Amount { get; set; }
            public decimal RunningBalance { get; set; }
        }
        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }

            public static ApiResponse<T> Ok(T data, string message = null) =>
                new ApiResponse<T> { Success = true, Message = message, Data = data };

            public static ApiResponse<T> Fail(string message, T data = default) =>
                new ApiResponse<T> { Success = false, Message = message, Data = data };
        }





        [HttpPost]
        [Route("send")]
        public async Task<IHttpActionResult> SendMessage()
        {
            SendMessageRequest request = new SendMessageRequest();
            request.PhoneNumber = "254742199073";
            request.Message = "hello there test";

            if (request == null)
                return BadRequest("Invalid request payload.");

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                return BadRequest("Phone number is required.");

            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Message cannot be empty.");

            bool sent = await SmsHelper.SendMessageAsync(request.PhoneNumber, request.Message);

            if (sent)
            {
                return Ok(new
                {
                    success = true,
                    message = "Message sent successfully.",
                    phone = request.PhoneNumber,
                    text = request.Message
                });
            }

            return Ok(new
            {
                success = false,
                message = "FAILED to send message."
            });
        }

        private LoanCaseStatus? ResolveLoanStatus(string status)
        {

            switch (status.Trim().ToLower())
            {
                case "registered":
                    return LoanCaseStatus.Registered;

                case "appraised":
                    return LoanCaseStatus.Appraised;

                case "approved":
                    return LoanCaseStatus.Approved;

                case "disbursed":
                    return LoanCaseStatus.Disbursed;

                case "rejected":
                    return LoanCaseStatus.Rejected;

                case "deferred":
                    return LoanCaseStatus.Deferred;

                case "audited":
                case "verified": // supporting alias
                    return LoanCaseStatus.Audited;

                case "restructured":
                    return LoanCaseStatus.Restructured;

                default:
                    return null;
            }
        }

        private LoanCaseFilter? ResolveLoanFilter(int filterType)
        {
            if (Enum.IsDefined(typeof(LoanCaseFilter), filterType))
                return (LoanCaseFilter)filterType;

            return null;
        }
        public class SendMessageRequest
        {
            public string PhoneNumber { get; set; }
            public string Message { get; set; }
        }
        public sealed class LoanAppraisalRequest
        {
            [Required]
            public Guid LoanCaseId { get; set; }

            [Range(0, int.MaxValue)]
            public int LoanAuditOption { get; set; }
        }
        private IHttpActionResult FailureResponse(string message) =>
            Content(HttpStatusCode.InternalServerError,
                new ApiResponse<object> { Success = false, Message = message });

        private IHttpActionResult ValidationErrorResponse(object errors) =>
            Content(HttpStatusCode.ExpectationFailed,
                new ApiResponse<object> { Success = false, Message = "Validation failed.", Data = errors });

        private IHttpActionResult NotFoundResponse(string message) =>
            Content(HttpStatusCode.NotFound,
                new ApiResponse<object> { Success = false, Message = message });

        private ApiResponse<object> SuccessResponse(string message) =>
            new ApiResponse<object> { Success = true, Message = message };
        private void MapLoanProductAttributes(LoanCaseDTO2 dto, LoanProductDTO p)
        {
            if (dto == null || p == null)
                return;

            dto.LoanRegistrationPaymentFrequencyPerYear = p.LoanRegistrationPaymentFrequencyPerYear;
            dto.LoanRegistrationMinimumAmount = p.LoanRegistrationMinimumAmount;
            dto.LoanRegistrationMinimumInterestAmount = p.LoanRegistrationMinimumInterestAmount;
            dto.LoanRegistrationMinimumGuarantors = p.LoanRegistrationMinimumGuarantors;
            dto.LoanRegistrationMinimumMembershipPeriod = p.LoanRegistrationMinimumMembershipPeriod;
            dto.LoanRegistrationMaximumGuarantees = p.LoanRegistrationMaximumGuarantees;
            dto.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement = p.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement;
            dto.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage = p.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage;
            dto.LoanRegistrationLoanProductSection = p.LoanRegistrationLoanProductSection;
            dto.LoanRegistrationLoanProductCategory = p.LoanRegistrationLoanProductCategory;
            dto.LoanRegistrationConsecutiveIncome = p.LoanRegistrationConsecutiveIncome;
            dto.LoanRegistrationInvestmentsMultiplier = p.LoanRegistrationInvestmentsMultiplier;
            dto.LoanRegistrationRejectIfMemberHasBalance = p.LoanRegistrationRejectIfMemberHasBalance;
            dto.LoanRegistrationSecurityRequired = p.LoanRegistrationSecurityRequired;
            dto.LoanRegistrationAllowSelfGuarantee = p.LoanRegistrationAllowSelfGuarantee;
            dto.LoanRegistrationGracePeriod = p.LoanRegistrationGracePeriod;
            dto.LoanRegistrationPaymentDueDate = p.LoanRegistrationPaymentDueDate;
            dto.LoanRegistrationPayoutRecoveryMode = p.LoanRegistrationPayoutRecoveryMode;
            dto.LoanRegistrationPayoutRecoveryPercentage = p.LoanRegistrationPayoutRecoveryPercentage;
            dto.LoanRegistrationAggregateCheckOffRecoveryMode = p.LoanRegistrationAggregateCheckOffRecoveryMode;
            dto.LoanRegistrationChargeClearanceFee = p.LoanRegistrationChargeClearanceFee;
            dto.LoanRegistrationMicrocredit = p.LoanRegistrationMicrocredit;
            dto.LoanRegistrationStandingOrderTrigger = p.LoanRegistrationStandingOrderTrigger;
            dto.LoanRegistrationTrackArrears = p.LoanRegistrationTrackArrears;
            dto.LoanRegistrationChargeArrearsFee = p.LoanRegistrationChargeArrearsFee;
            dto.LoanRegistrationEnforceSystemAppraisalRecommendation = p.LoanRegistrationEnforceSystemAppraisalRecommendation;
            dto.LoanRegistrationBypassAudit = p.LoanRegistrationBypassAudit;
            dto.LoanRegistrationGuarantorSecurityMode = p.LoanRegistrationGuarantorSecurityMode;
            dto.LoanRegistrationRoundingType = p.LoanRegistrationRoundingType;
            dto.LoanRegistrationDisburseMicroLoanLessDeductions = p.LoanRegistrationDisburseMicroLoanLessDeductions;
            dto.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal = p.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal;
            dto.LoanRegistrationThrottleScheduledArrearsRecovery = p.LoanRegistrationThrottleScheduledArrearsRecovery;
            dto.LoanRegistrationCreateStandingOrderOnLoanAudit = p.LoanRegistrationCreateStandingOrderOnLoanAudit;

            // Interest attributes
            dto.LoanInterestAnnualPercentageRate = p.LoanInterestAnnualPercentageRate;
            dto.LoanInterestChargeMode = p.LoanInterestChargeMode;
            dto.LoanInterestRecoveryMode = p.LoanInterestRecoveryMode;
            dto.LoanInterestCalculationMode = p.LoanInterestCalculationMode;

            // Loan product descriptions
            dto.LoanProductDescription = p.Description;
            dto.InterestCalculationModeDescription = p.LoanInterestCalculationModeDescription;
            dto.LoanProductSectionDescription = p.LoanRegistrationLoanProductSectionDescription;

            // Term & ceilings
            dto.LoanRegistrationTermInMonths = p.LoanRegistrationTermInMonths;
            dto.LoanRegistrationMaximumAmount = p.LoanRegistrationMaximumAmount;

            // Take home rules
            dto.TakeHomeType = p.TakeHomeType;
            dto.TakeHomePercentage = p.TakeHomePercentage;
            dto.TakeHomeFixedAmount = p.TakeHomeFixedAmount;

            // Core identity linkage
            dto.LoanProductId = p.Id;
        }
        #endregion

    }
}
