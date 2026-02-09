using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Web.Http;
using TestApis.Helpers;
using TestApis.Models;
using TestApis.Services;

namespace TestApis.Controllers
{
    [RoutePrefix("api/MemberPortal")]
    public class MemberPortalController : ApiController
    {
        private readonly MasterController master;
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public MemberPortalController()
        {
            master = new MasterController();
        }

        #region DTO
        private readonly string _cs = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        private readonly CustomerStatementService _statementService = new CustomerStatementService();
        private readonly CustomerService _customerService = new CustomerService();

        private IHttpActionResult ApiResponse(bool success, string message, object data = null)
        {
            return Ok(new { success, message, data });
        }
        public class Login { public string memberNo; public string pin; }

        public class CustomerStatementRequestDto
        {
            public string IdentityCardNo { get; set; }

            public DateTime startDate { get; set; }

            public DateTime endDate { get; set; }

            public bool IncludeProductBreakdown { get; set; } = true;
        }
        public class Registration
        {
            public int memberNo; public string idNumber;
        }
        #endregion

        [HttpPost]
        [Route("Register")]
        public async Task<IHttpActionResult> Register([FromBody] Registration registration)
        {
            using (var conn = new SqlConnection(_cs))
            {
                conn.Open();

                // 1. Fetch customer
                var cmdCustomer = new SqlCommand(@"
                    SELECT TOP 1
                       Reference2,
                        Individual_FirstName,
                        Individual_LastName,
                        Individual_IdentityCardNumber,
                        Individual_BirthDate,
                        Individual_Gender,
                        Address_MobileLine,
                        Address_Email,
                        SequentialId
                    FROM swiftFin_Customers
                    WHERE Reference2 = @ref
                      AND Individual_IdentityCardNumber = @id", conn);

                cmdCustomer.Parameters.AddWithValue("@ref", registration.memberNo);
                cmdCustomer.Parameters.AddWithValue("@id", registration.idNumber);

                using (var rd = cmdCustomer.ExecuteReader())
                {
                    if (!rd.Read())
                        return Json(new { success = false, message = "Invalid reference or ID" });

                    var memberNo = rd["Reference2"].ToString();



                    // 2. Check existing registration
                    var checkCmd = new SqlCommand(
                        "SELECT 1 FROM Registration WHERE IDNumber = @id", conn);
                    checkCmd.Parameters.AddWithValue("@id", registration.idNumber);

                    if (checkCmd.ExecuteScalar() != null)
                        return Json(new { success = false, message = "Already registered" });

                    // 3. Generate PIN
                    var pin = new Random().Next(1000, 9999).ToString();
                    string phoneNumber = rd["Address_MobileLine"].ToString();
                    string message =
                        "Dear Member, thank you for registering for Alternative Channels. " +
                        "Your PIN for Web Portal and Mobile App is: " + pin + ". " +
                        "Do not share this PIN with anyone.";
                    await SmsHelper.SendPin(pin, message, phoneNumber);
                    PinSecurity.Create(pin, out var hash, out var salt);

                    // 4. Insert registration
                    var insertCmd = new SqlCommand(@"
                        INSERT INTO Registration
                        (
                            FullNames, PhoneNumber, EmailAddress, IDNumber,
                            DateOfBirth, Gender, MemberNo,
                            PIN, IMSI, FirstLogin, Approved, Status,
                            Trials, CreatedAt, CreatedBy
                        )
                        VALUES
                        (
                            @names, @phone, @email, @id,
                            @dob, @gender, @memberNo,
                            @pin, @salt, 1, 1, 'Active',
                            0, GETUTCDATE(), 'SYSTEM'
                        )", conn);
                    insertCmd.Parameters.AddWithValue("@names", rd["Individual_FirstName"] + " " + rd["Individual_LastName"]);
                    insertCmd.Parameters.AddWithValue("@phone", rd["Address_MobileLine"]);
                    insertCmd.Parameters.AddWithValue("@email", rd["Address_Email"]);
                    insertCmd.Parameters.AddWithValue("@id", rd["Individual_IdentityCardNumber"]);
                    insertCmd.Parameters.AddWithValue("@dob", rd["Individual_BirthDate"] ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@gender", rd["Individual_Gender"]);
                    insertCmd.Parameters.AddWithValue("@memberNo", memberNo);
                    insertCmd.Parameters.AddWithValue("@pin", Convert.ToBase64String(hash));
                    insertCmd.Parameters.AddWithValue("@salt", Convert.ToBase64String(salt));

                    insertCmd.ExecuteNonQuery();
                    rd.Close();

                    return Json(new { success = true, memberNo, pin });
                }
            }
        }

        [HttpPost]
        [Route("Login")]
        public IHttpActionResult login([FromBody] Login login)
        {

            using (var conn = new SqlConnection(_cs))
            {
                conn.Open();

                var cmd = new SqlCommand(@"
                    SELECT PIN, IMSI, Trials
                    FROM Registration
                    WHERE MemberNo = @memberNo
                      AND Status = 'Active'", conn);

                cmd.Parameters.AddWithValue("@memberNo", login.memberNo);

                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read())
                        return Json(new { success = false, message = "Invalid credentials" });

                    var trials = (int)rd["Trials"];
                    if (trials >= 5)
                        return Json(new { success = false, message = "Account locked" });

                    var hash = Convert.FromBase64String(rd["PIN"].ToString());
                    var salt = Convert.FromBase64String(rd["IMSI"].ToString());

                    if (!PinSecurity.Verify(login.pin, hash, salt))
                    {
                        rd.Close();
                        IncrementTrials(conn, login.memberNo);
                        return Json(new { success = false, message = "Invalid credentials" });
                    }

                    rd.Close();
                    ResetTrials(conn, login.memberNo);

                    return Json(new { success = true, login.memberNo });
                }
            }
        }


        [HttpGet, Route("GetStatementByMemberNo")]
        public IHttpActionResult GetStatementByMemberNo([FromUri] string MemberNo, [FromUri] DateTime startDate, [FromUri] DateTime endDate, [FromUri] bool IncludeProductBreakdown = true)
        {
            try
            {
                if (startDate > endDate)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                                   new { success = false, message = "Start date cannot be after end date" });

                // Get customer details
                var customer = _customerService.GetByMemberNo(MemberNo);


                if (customer == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                                   new { success = false, message = "Customer not found" });
                if (startDate == null)
                {
                    startDate = (DateTime)customer.RegistrationDate;
                }
                if (endDate == null)
                {
                    endDate = DateTime.Now;
                }

                // Get statement
                var statement = _statementService.GetCustomerStatementByCustomerId(customer.Id, startDate, endDate);
                var statementList = (System.Collections.Generic.List<CustomerStatementDTO>)statement;

                // Calculate opening balance
                var openingBalance = _statementService.GetCustomerBalanceAsOfDate(customer.Id, startDate.AddSeconds(-1));

                // Calculate running totals including opening balance
                decimal runningTotal = openingBalance;
                foreach (var transaction in statementList)
                {
                    runningTotal += transaction.Credit - transaction.Debit;
                    transaction.RunningTotal = runningTotal;
                }

                // Calculate summary
                var summary = new CustomerStatementSummaryDTO
                {
                    CustomerName = customer.FullName,
                    SerialNumber = customer.SerialNumber.ToString(),
                    FirstTransactionDate = startDate,
                    LastTransactionDate = endDate,
                    OpeningBalance = openingBalance,
                    ClosingBalance = runningTotal
                };

                // Calculate totals
                decimal totalDebit = 0, totalCredit = 0;
                foreach (var transaction in statementList)
                {
                    totalDebit += transaction.Debit;
                    totalCredit += transaction.Credit;
                }

                summary.TotalTransactions = statementList.Count;
                summary.TotalDebit = totalDebit;
                summary.TotalCredit = totalCredit;
                summary.NetBalance = totalCredit - totalDebit;

                // Get product breakdown if requested
                if (IncludeProductBreakdown)
                {
                    summary.ProductBreakdown = new System.Collections.Generic.List<CustomerProductStatementDTO>(
                        _statementService.GetStatementByProduct(customer.Id, startDate, endDate)
                    );
                }

                return ApiResponse(true, "Customer statement retrieved successfully", new
                {
                    customer = new
                    {
                        customer.FullName,
                        customer.SerialNumber,
                        customer.Reference1,
                        customer.Reference2,
                        customer.Reference3,
                        customer.AddressAddressLine1,
                        customer.AddressAddressLine2,
                        customer.AddressMobileLine,
                        customer.IndividualIdentityCardNumber
                    },
                    statement = statement,
                    summary = summary,
                    openingBalance = openingBalance,
                    closingBalance = runningTotal
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                               new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetMemberWithDetails/by-reference2/{reference2}")]
        public async Task<IHttpActionResult> GetMemberWithDetailsByReference2(string reference2, [FromUri] bool includeAccounts = true, [FromUri] bool includeNextOfKin = true, [FromUri] bool includeAccountBalances = true, [FromUri] bool includeProductDescription = true, [FromUri] bool includeInterestBalanceForLoanAccounts = false, [FromUri] bool considerMaturityPeriodForInvestmentAccounts = false, [FromUri] bool includeStatements = false, [FromUri] DateTime? statementStartDate = null, [FromUri] DateTime? statementEndDate = null)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                // Strategic assumption: Reference2 is unique (Membership Number)
                var customers = await master._channelService.FindCustomersAsync(serviceHeader);

                var customer = customers?.FirstOrDefault(c => c.Reference2 == reference2);

                if (customer == null)
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Member with Reference2 '{reference2}' not found.",
                        Data = null
                    });
                }

                var memberDetail = new
                {
                    Customer = new
                    {
                        customer.Id,
                        customer.FullName,
                        customer.SerialNumber,
                        customer.PaddedSerialNumber,
                        customer.Type,
                        customer.TypeDescription,
                        customer.IndividualType,
                        customer.IndividualTypeDescription,
                        customer.IndividualFirstName,
                        customer.IndividualLastName,
                        customer.IndividualIdentityCardNumber,
                        customer.AddressMobileLine,
                        customer.AddressEmail,
                        customer.PersonalIdentificationNumber,
                        customer.Reference1,
                        customer.Reference2,
                        customer.Reference3,
                        customer.BranchDescription,
                        customer.RegistrationDate,
                        customer.RecordStatus,
                        customer.RecordStatusDescription,
                        customer.IsDefaulter,
                        customer.IsLocked,
                        customer.Age,
                        customer.MembershipPeriod
                    },
                    Accounts = new List<object>(),
                    NextOfKin = new List<object>(),
                    Statements = new List<object>()
                };

                // Accounts
                if (includeAccounts)
                {
                    var accounts = await master._channelService
                        .FindCustomerAccountsByCustomerIdAsync(
                            customer.Id,
                            includeAccountBalances,
                            includeProductDescription,
                            includeInterestBalanceForLoanAccounts,
                            considerMaturityPeriodForInvestmentAccounts,
                            serviceHeader
                        );

                    if (accounts != null && accounts.Any())
                    {
                        var accountIds = accounts.Select(a => a.Id).ToList();

                        Dictionary<Guid, List<object>> accountStatements = null;
                        if (includeStatements)
                        {
                            accountStatements = await GetAccountStatementsAsync(
                                accountIds,
                                statementStartDate,
                                statementEndDate
                            );
                        }

                        foreach (var account in accounts)
                        {
                            memberDetail.Accounts.Add(new
                            {
                                account.Id,
                                account.FullAccountNumber,
                                account.CustomerAccountTypeTargetProductDescription,
                                account.CustomerAccountTypeProductCode,
                                account.CustomerAccountTypeProductCodeDescription,
                                account.Status,
                                account.StatusDescription,
                                account.RecordStatus,
                                account.RecordStatusDescription,
                                account.BookBalance,
                                account.AvailableBalance,
                                account.PrincipalBalance,
                                account.InterestBalance,
                                account.CarryForwardsBalance,
                                account.PrincipalArrearagesBalance,
                                account.InterestArrearagesBalance,
                                account.CreatedDate,
                                account.Remarks
                            });

                            if (includeStatements &&
                                accountStatements != null &&
                                accountStatements.TryGetValue(account.Id, out var stmts))
                            {
                                memberDetail.Statements.AddRange(stmts);
                            }
                        }
                    }
                }

                // Next of Kin
                if (includeNextOfKin)
                {
                    var nextOfKins = await master._channelService
                        .FindNextOfKinCollectionByCustomerIdAsync(customer.Id, serviceHeader);

                    if (nextOfKins != null)
                    {
                        foreach (var nok in nextOfKins)
                        {
                            memberDetail.NextOfKin.Add(new
                            {
                                nok.Id,
                                nok.FullName,
                                nok.Relationship,
                                nok.RelationshipDescription,
                                nok.AddressMobileLine,
                                nok.AddressEmail,
                                nok.AddressAddressLine1,
                                nok.AddressCity,
                                nok.NominatedPercentage,
                                nok.CreatedDate
                            });
                        }
                    }
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Member retrieved successfully.",
                    Data = memberDetail
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"GetMemberWithDetailsByReference2 failed: {ex}");

                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to retrieve member details.",
                    Data = new { ex.Message, Inner = ex.InnerException?.Message }
                });
            }
        }

        public class CustomerAccountDto
        {
            public Guid CustomerId { get; set; }
            public string Reference2 { get; set; }
            public string FullName { get; set; }
            public Guid CustomerAccountId { get; set; }
            public int ProductCode { get; set; }
            public string AccountType { get; set; }
            public string ProductDescription { get; set; }
            public string AccountStatus { get; set; }
            public decimal AccountBalance { get; set; }
            public DateTime? LastTransactionDate { get; set; }
        }


        [HttpGet]
        [Route("accountsStatistics/{reference2}")]
        public IHttpActionResult GetCustomerAccounts([FromUri] string reference2)
        {
            var result = new List<CustomerAccountDto>();

            string sql = @"
SELECT
    c.Id                                AS CustomerId,
    c.Reference2,
    c.Individual_FirstName + ' ' + c.Individual_LastName AS FullName,
    ca.Id                               AS CustomerAccountId,
    ca.CustomerAccountType_ProductCode  AS ProductCode,
    CASE 
        WHEN lp.Id IS NOT NULL THEN 'Loan'
        WHEN sp.Id IS NOT NULL THEN 'Savings'
        ELSE 'Unknown'
    END AS AccountType,
    COALESCE(lp.Description, sp.Description) AS ProductDescription,
    CASE 
        WHEN ca.Status = 1 THEN 'Active'
        WHEN ca.Status = 0 THEN 'Inactive'
        ELSE 'Unknown'
    END AS AccountStatus,
    SUM(ISNULL(je.Amount,0) * CASE 
            WHEN je.ChartOfAccountId = COALESCE(lp.ChartOfAccountId, sp.ChartOfAccountId) THEN 1
            WHEN je.ContraChartOfAccountId = COALESCE(lp.ChartOfAccountId, sp.ChartOfAccountId) THEN -1
            ELSE 0
        END) AS AccountBalance,
    MAX(je.ValueDate) AS LastTransactionDate
FROM swiftFin_Customers c
JOIN swiftFin_CustomerAccounts ca
    ON ca.CustomerId = c.Id
LEFT JOIN swiftFin_LoanProducts lp
    ON lp.Id = ca.CustomerAccountType_TargetProductId
LEFT JOIN swiftFin_SavingsProducts sp
    ON sp.Id = ca.CustomerAccountType_TargetProductId
LEFT JOIN swiftFin_JournalEntries je
    ON je.ChartOfAccountId = COALESCE(lp.ChartOfAccountId, sp.ChartOfAccountId)
       OR je.ContraChartOfAccountId = COALESCE(lp.ChartOfAccountId, sp.ChartOfAccountId)
WHERE c.Reference2 = @ref
GROUP BY
    c.Id,
    c.Reference2,
    c.Individual_FirstName,
    c.Individual_LastName,
    ca.Id,
    ca.CustomerAccountType_ProductCode,
    lp.Id,
    sp.Id,
    lp.ChartOfAccountId,
    sp.ChartOfAccountId,
    lp.Description,
    sp.Description,
    ca.Status
ORDER BY AccountBalance DESC;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ref", reference2);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var dto = new CustomerAccountDto
                        {
                            CustomerId = reader["CustomerId"] != DBNull.Value ? (Guid)reader["CustomerId"] : Guid.Empty,
                            Reference2 = reader["Reference2"] != DBNull.Value ? reader["Reference2"].ToString() : null,
                            FullName = reader["FullName"] != DBNull.Value ? reader["FullName"].ToString() : null,
                            CustomerAccountId = reader["CustomerAccountId"] != DBNull.Value ? (Guid)reader["CustomerAccountId"] : Guid.Empty,
                            ProductCode = reader["ProductCode"] != DBNull.Value ? Convert.ToInt32(reader["ProductCode"]) : 0,
                            AccountType = reader["AccountType"] != DBNull.Value ? reader["AccountType"].ToString() : null,
                            ProductDescription = reader["ProductDescription"] != DBNull.Value ? reader["ProductDescription"].ToString() : null,
                            AccountStatus = reader["AccountStatus"] != DBNull.Value ? reader["AccountStatus"].ToString() : null,
                            AccountBalance = reader["AccountBalance"] != DBNull.Value ? Math.Abs(Convert.ToDecimal(reader["AccountBalance"])) : 0m,
                            LastTransactionDate = reader["LastTransactionDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["LastTransactionDate"]) : null
                        };


                        result.Add(dto);
                    }
                }
            }

            return Ok(result);
        }

        public async Task<Dictionary<Guid, List<object>>> GetAccountStatementsAsync(
            List<Guid> accountIds,
            DateTime? startDate,
            DateTime? endDate)
        {
            var accountStatements = new Dictionary<Guid, List<object>>();

            try
            {
                // Create a connection to the database
                using (var connection = new SqlConnection(GetConnectionString()))
                {
                    await connection.OpenAsync();

                    // Build the SQL query to get journal entries and journal data
                    var sql = @"
                SELECT 
                    -- Journal Entry fields
                    je.Id as JournalEntryId,
                    je.JournalId,
                    je.ChartOfAccountId,
                    je.ContraChartOfAccountId,
                    je.CustomerAccountId,
                    je.Amount,
                    je.ValueDate as JournalEntryValueDate,
                    je.IntegrityHash as JournalEntryIntegrityHash,
                    je.SequentialId as JournalEntrySequentialId,
                    je.CreatedBy as JournalEntryCreatedBy,
                    je.CreatedDate as JournalEntryCreatedDate,
                    
                    -- Journal fields
                    j.Id as JournalId,
                    j.ParentId,
                    j.PostingPeriodId,
                    j.BranchId,
                    j.AlternateChannelLogId,
                    j.TotalValue,
                    j.PrimaryDescription,
                    j.SecondaryDescription,
                    j.Reference,
                    j.ApplicationUserName,
                    j.EnvironmentUserName,
                    j.EnvironmentMachineName,
                    j.EnvironmentDomainName,
                    j.EnvironmentOSVersion,
                    j.EnvironmentMACAddress,
                    j.EnvironmentMotherboardSerialNumber,
                    j.EnvironmentProcessorId,
                    j.EnvironmentIPAddress,
                    j.ModuleNavigationItemCode,
                    j.TransactionCode,
                    j.ValueDate as JournalValueDate,
                    j.SuppressAccountAlert,
                    j.IsLocked,
                    j.IntegrityHash as JournalIntegrityHash,
                    j.SequentialId as JournalSequentialId,
                    j.CreatedBy as JournalCreatedBy,
                    j.CreatedDate as JournalCreatedDate
                    
                FROM swiftFin_JournalEntries je
                INNER JOIN swiftFin_Journals j ON je.JournalId = j.Id
                WHERE je.CustomerAccountId IN ({0})";

                    // Add date filtering if provided
                    if (startDate.HasValue || endDate.HasValue)
                    {
                        sql += " AND (";
                        var conditions = new List<string>();

                        if (startDate.HasValue)
                        {
                            conditions.Add("(je.ValueDate >= @StartDate OR j.ValueDate >= @StartDate OR je.CreatedDate >= @StartDate OR j.CreatedDate >= @StartDate)");
                        }

                        if (endDate.HasValue)
                        {
                            conditions.Add("(je.ValueDate <= @EndDate OR j.ValueDate <= @EndDate OR je.CreatedDate <= @EndDate OR j.CreatedDate <= @EndDate)");
                        }

                        sql += string.Join(" AND ", conditions) + ")";
                    }

                    sql += " ORDER BY ISNULL(je.ValueDate, j.ValueDate) DESC, je.CreatedDate DESC, j.CreatedDate DESC";

                    // Create parameterized query
                    var accountIdParameters = string.Join(",", accountIds.Select((id, index) => $"@AccountId{index}"));
                    sql = string.Format(sql, accountIdParameters);

                    using (var command = new SqlCommand(sql, connection))
                    {
                        // Add account ID parameters
                        for (int i = 0; i < accountIds.Count; i++)
                        {
                            command.Parameters.AddWithValue($"@AccountId{i}", accountIds[i]);
                        }

                        // Add date parameters if provided
                        if (startDate.HasValue)
                        {
                            command.Parameters.AddWithValue("@StartDate", startDate.Value);
                        }

                        if (endDate.HasValue)
                        {
                            command.Parameters.AddWithValue("@EndDate", endDate.Value);
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var customerAccountId = reader.GetGuid(reader.GetOrdinal("CustomerAccountId"));

                                if (!accountStatements.ContainsKey(customerAccountId))
                                {
                                    accountStatements[customerAccountId] = new List<object>();
                                }

                                // Get journal entry value date
                                DateTime? journalEntryValueDate = null;
                                var journalEntryValueDateOrdinal = reader.GetOrdinal("JournalEntryValueDate");
                                if (!reader.IsDBNull(journalEntryValueDateOrdinal))
                                {
                                    journalEntryValueDate = reader.GetDateTime(journalEntryValueDateOrdinal);
                                }

                                // Get journal value date
                                DateTime? journalValueDate = null;
                                var journalValueDateOrdinal = reader.GetOrdinal("JournalValueDate");
                                if (!reader.IsDBNull(journalValueDateOrdinal))
                                {
                                    journalValueDate = reader.GetDateTime(journalValueDateOrdinal);
                                }

                                var statement = new
                                {
                                    // Journal Entry details
                                    JournalEntry = new
                                    {
                                        Id = reader.GetGuid(reader.GetOrdinal("JournalEntryId")),
                                        JournalId = reader.GetGuid(reader.GetOrdinal("JournalId")),
                                        ChartOfAccountId = reader.GetGuid(reader.GetOrdinal("ChartOfAccountId")),
                                        ContraChartOfAccountId = reader.GetGuid(reader.GetOrdinal("ContraChartOfAccountId")),
                                        CustomerAccountId = customerAccountId,
                                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                                        ValueDate = journalEntryValueDate,
                                        IntegrityHash = reader.GetString(reader.GetOrdinal("JournalEntryIntegrityHash")),
                                        SequentialId = reader.GetGuid(reader.GetOrdinal("JournalEntrySequentialId")),
                                        CreatedBy = reader.GetString(reader.GetOrdinal("JournalEntryCreatedBy")),
                                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("JournalEntryCreatedDate"))
                                    },

                                    // Journal details
                                    Journal = new
                                    {
                                        Id = reader.GetGuid(reader.GetOrdinal("JournalId")),
                                        ParentId = reader.IsDBNull(reader.GetOrdinal("ParentId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("ParentId")),
                                        PostingPeriodId = reader.GetGuid(reader.GetOrdinal("PostingPeriodId")),
                                        BranchId = reader.GetGuid(reader.GetOrdinal("BranchId")),
                                        AlternateChannelLogId = reader.IsDBNull(reader.GetOrdinal("AlternateChannelLogId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("AlternateChannelLogId")),
                                        TotalValue = reader.GetDecimal(reader.GetOrdinal("TotalValue")),
                                        PrimaryDescription = reader.GetString(reader.GetOrdinal("PrimaryDescription")),
                                        SecondaryDescription = reader.GetString(reader.GetOrdinal("SecondaryDescription")),
                                        Reference = reader.GetString(reader.GetOrdinal("Reference")),
                                        ApplicationUserName = reader.GetString(reader.GetOrdinal("ApplicationUserName")),
                                        EnvironmentUserName = reader.GetString(reader.GetOrdinal("EnvironmentUserName")),
                                        EnvironmentMachineName = reader.GetString(reader.GetOrdinal("EnvironmentMachineName")),
                                        EnvironmentDomainName = reader.GetString(reader.GetOrdinal("EnvironmentDomainName")),
                                        EnvironmentOSVersion = reader.GetString(reader.GetOrdinal("EnvironmentOSVersion")),
                                        EnvironmentMACAddress = reader.GetString(reader.GetOrdinal("EnvironmentMACAddress")),
                                        EnvironmentMotherboardSerialNumber = reader.GetString(reader.GetOrdinal("EnvironmentMotherboardSerialNumber")),
                                        EnvironmentProcessorId = reader.GetString(reader.GetOrdinal("EnvironmentProcessorId")),
                                        EnvironmentIPAddress = reader.GetString(reader.GetOrdinal("EnvironmentIPAddress")),
                                        ModuleNavigationItemCode = reader.GetInt32(reader.GetOrdinal("ModuleNavigationItemCode")),
                                        TransactionCode = reader.GetInt32(reader.GetOrdinal("TransactionCode")),
                                        ValueDate = journalValueDate,
                                        SuppressAccountAlert = reader.GetBoolean(reader.GetOrdinal("SuppressAccountAlert")),
                                        IsLocked = reader.GetBoolean(reader.GetOrdinal("IsLocked")),
                                        IntegrityHash = reader.GetString(reader.GetOrdinal("JournalIntegrityHash")),
                                        SequentialId = reader.GetGuid(reader.GetOrdinal("JournalSequentialId")),
                                        CreatedBy = reader.GetString(reader.GetOrdinal("JournalCreatedBy")),
                                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("JournalCreatedDate"))
                                    },

                                    // Combined statement summary
                                    StatementSummary = new
                                    {
                                        TransactionId = reader.GetGuid(reader.GetOrdinal("JournalId")),
                                        TransactionDate = reader.GetDateTime(reader.GetOrdinal("JournalCreatedDate")),
                                        ValueDate = journalEntryValueDate ?? journalValueDate ?? reader.GetDateTime(reader.GetOrdinal("JournalCreatedDate")),
                                        Description = reader.GetString(reader.GetOrdinal("PrimaryDescription")),
                                        Reference = reader.GetString(reader.GetOrdinal("Reference")),
                                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                                        TransactionCode = reader.GetInt32(reader.GetOrdinal("TransactionCode")),
                                        IsReversed = reader.GetBoolean(reader.GetOrdinal("IsLocked"))
                                    }
                                };

                                accountStatements[customerAccountId].Add(statement);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in GetAccountStatementsAsync: {ex.Message}");
                System.Diagnostics.Trace.TraceError($"Stack Trace: {ex.StackTrace}");
                throw;
            }

            return accountStatements;
        }
        [HttpGet]
        [Route("GetLoanProducts")]
        public async Task<IHttpActionResult> GetLoanProducts([FromUri] string search = null, [FromUri] int pageIndex = 0, [FromUri] int pageSize = 20)
        {
            if (pageIndex < 0 || pageSize <= 0)
                return BadRequest("Invalid paging parameters.");

            try
            {
                var serviceHeader = master.GetServiceHeader();

                var loanProducts = await master._channelService.FindLoanProductsByFilterInPageAsync(search, pageIndex, pageSize, serviceHeader);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = loanProducts.PageCollection != null && loanProducts.PageCollection.Any()
                        ? $"{loanProducts.ItemsCount} loan products retrieved."
                        : "No loan products found.",
                    Data = loanProducts.PageCollection
                });
            }
            catch (Exception ex)
            {
                // log ex here (Serilog / AppInsights / ELK — non-negotiable)
                return InternalServerError(new Exception("Failed to retrieve loan products." + ex));
            }
        }



        [HttpPost]
        [Route("LoanApplication")]
        public async Task<IHttpActionResult> Create([FromBody] LoanCaseDTO2 loanCaseDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            try
            {
                // 1. Fetch loan product
                var loanProduct = await master._channelService.FindLoanProductAsync(loanCaseDTO.LoanProductId, serviceHeader);
                var branches = await master._channelService.FindBranchesAsync(serviceHeader);
                var branch = branches?.FirstOrDefault(c => c.Description != null && c.Description.StartsWith("Rubani", StringComparison.OrdinalIgnoreCase));
                if (branch != null)
                {
                    loanCaseDTO.BranchId = branch.Id;
                }
                if (loanProduct == null)
                    return BadRequest("Invalid loan product.");

                // 2. Parse collaterals
                var collateralGuidList = loanCaseDTO.collateralIds?
                    .Split(',')
                    .Where(x => Guid.TryParse(x, out _))
                    .Select(Guid.Parse)
                    .ToList() ?? new List<Guid>();

                var collateralDocuments = new List<CustomerDocumentDTO>();

                foreach (var id in collateralGuidList)
                {
                    var doc = await master._channelService.FindCustomerDocumentAsync(id, serviceHeader);
                    if (doc != null)
                        collateralDocuments.Add(doc);
                }


                // 3. Get guarantors from client payload (NOT Session)
                var guarantors = loanCaseDTO.Guarantors ?? new List<LoanGuarantorDTO>();

                // Apply required rules
                if (loanProduct.LoanRegistrationMinimumGuarantors > guarantors.Count)
                {

                    return BadRequest($"Loan product requires minimum {loanProduct.LoanRegistrationMinimumGuarantors} guarantors.");
                }

                else if (loanProduct.LoanRegistrationMinimumGuarantors < guarantors.Count)
                {
                    var guaranteedSum = guarantors.Sum(g => g.AmountGuaranteed);

                    if (guaranteedSum < loanCaseDTO.AmountApplied)
                    {
                        return BadRequest("Total amount guaranteed does not secure the applied amount.");
                    }
                }


                // 4. Membership period validation
                var customers = await master._channelService.FindCustomersAsync(serviceHeader);

                var customer = customers?.FirstOrDefault(c => c.Reference2 == loanCaseDTO.CustomerReference2);
                if (customer != null)
                {
                    loanCaseDTO.CustomerId = customer.Id;

                    var months = ((DateTime.Now.Year - customer.CreatedDate.Year) * 12) +
                                 (DateTime.Now.Month - customer.CreatedDate.Month);
                    if (months < loanProduct.LoanRegistrationMinimumMembershipPeriod)
                    {
                        return BadRequest("Member does not meet minimum membership period or does not exist.");
                    }
                }

                // 5. Merge loan product rules into loanCaseDTO
                MapLoanProductAttributes(loanCaseDTO, loanProduct);


                // 6. Create loan
                loanCaseDTO.CreatedBy = User.Identity.Name;
                loanCaseDTO.Status = (int)LoanCaseStatus.Deferred;

                var createResult = await master._channelService.AddLoanCaseAsync(loanCaseDTO.MapTo<LoanCaseDTO>(), serviceHeader);
                // 7. Attach sector classification


                if (createResult.ErrorMessageResult != null)
                    return Ok(new
                    {
                        success = false,
                        message = "Error Posting This Loan.",
                        loanCaseId = createResult.ErrorMessageResult
                    });

                // 7. Attach collaterals
                if (collateralDocuments.Any())
                {
                    await master._channelService.UpdateLoanCollateralsByLoanCaseIdAsync(createResult.Id, new ObservableCollection<CustomerDocumentDTO>(collateralDocuments), serviceHeader);
                }

                // 8. Attach guarantors
                if (guarantors.Any())
                {
                    await master._channelService.UpdateLoanGuarantorsByLoanCaseIdAsync(createResult.Id, new ObservableCollection<LoanGuarantorDTO>(guarantors), serviceHeader);
                }
     //           string message =
     //$"Dear {customer.IndividualFirstName} {customer.IndividualLastName}, " +
     //$"your loan application of KES {loanCaseDTO.AmountApplied:N0} has been successfully registered and is currently under review. " +
     //$"We will notify you once processing is complete.";
                //await SmsHelper.SendMessageAsync(customer.AddressMobileLine, message);

                return Ok(new
                {
                    success = true,
                    message = "Loan created successfully.",
                    loanCaseId = createResult.Id
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        [HttpGet]
        [Route("GetAllLoanByMemberNo")]
        public async Task<IHttpActionResult> GetAllLoanByMemberNo(string MemberNo)
        {
            var serviceHeader = master.GetServiceHeader();

            var pageInfo = await master._channelService.FindLoanCasesAsync(serviceHeader);
            var loanCase = pageInfo?
                .Where(c =>
                    c.CustomerReference2 == MemberNo &&
                    c.Status == (int)LoanCaseStatus.Deferred)
                .OrderByDescending(c => c.CreatedDate) // or ApprovedOn / Id
                .FirstOrDefault();
            if (loanCase == null)
                return BadRequest("Loans Not Found.");

            return Ok(loanCase);
        }
        [HttpPost]
        [Route("UpdateLoanCase")]
        public async Task<IHttpActionResult> UpdateLoanCase(LoanCaseDTO2 loanCaseDTO)
        {
            if (loanCaseDTO == null)
                return BadRequest("Invalid payload.");

            var serviceHeader = master.GetServiceHeader();

            // Fetch core entities
            var loanProduct = await master._channelService
                .FindLoanProductAsync(loanCaseDTO.LoanProductId, serviceHeader);

            if (loanProduct == null)
                return BadRequest("Loan product not found.");

            var loanCase = await master._channelService
                .FindLoanCaseAsync(loanCaseDTO.Id, serviceHeader);

            if (loanCase == null)
                return BadRequest("Loan case not found.");

            // Normalize guarantors
            var guarantors = loanCaseDTO.Guarantors ?? new List<LoanGuarantorDTO>();

            // ===== Business Rules =====

            if (guarantors.Count < loanProduct.LoanRegistrationMinimumGuarantors)
                return BadRequest(
                    $"Loan product requires at least {loanProduct.LoanRegistrationMinimumGuarantors} guarantors."
                );

            var guaranteedSum = guarantors.Sum(g => g.AmountGuaranteed);

            if (guaranteedSum < loanCaseDTO.AmountApplied)
                return BadRequest("Total guaranteed amount is insufficient to cover the applied loan amount.");

            // ===== State Mutation =====

            loanCase.AmountApplied = loanCaseDTO.AmountApplied;
            loanCase.LoanRegistrationTermInMonths = loanCaseDTO.LoanRegistrationTermInMonths;
            loanCase.Status = (int)LoanCaseStatus.Registered;

            // ===== Persistence =====

            var loanUpdated = await master._channelService
                .UpdateLoanCaseAsync(loanCase, serviceHeader);

            if (!loanUpdated)
                return BadRequest("Loan case update failed.");

            var guarantorsUpdated = await master._channelService
                .UpdateLoanGuarantorsByLoanCaseIdAsync(
                    loanCase.Id,
                    new ObservableCollection<LoanGuarantorDTO>(guarantors),
                    serviceHeader
                );

            if (!guarantorsUpdated)
                return BadRequest("Guarantor update failed.");

            // ===== Response =====

            return Ok(new
            {
                success = true,
                loanCaseReference = loanCase.Reference
            });
        }



        [HttpGet]
        [Route("GetCustomers")]
        public async Task<IHttpActionResult> GetCustomers()
        {
            // Build service context
            var serviceHeader = master.GetServiceHeader();

            // Execute domain call
            var customers = await master._channelService
                                        .FindCustomersAsync(serviceHeader);

            // Guardrail: empty result ≠ error
            if (customers == null || !customers.Any())
                return Ok(new
                {
                    success = true,
                    data = Array.Empty<object>(),
                    message = "No customers found"
                });

            // Standardized response envelope
            return Ok(new
            {
                success = true,
                data = customers,
                count = customers.Count()
            });
        }



        #region
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



        // ===================== HELPERS =====================
        private string GetConnectionString()
        {
            // Get connection string from your configuration
            // This could be from web.config, appsettings.json, or other configuration source
            return System.Configuration.ConfigurationManager.ConnectionStrings["SwiftFinancialsDB_Live"].ConnectionString;
        }


        private void IncrementTrials(SqlConnection conn, string memberNo)
        {
            var cmd = new SqlCommand(
                "UPDATE Registration SET Trials = Trials + 1 WHERE MemberNo = @m", conn);
            cmd.Parameters.AddWithValue("@m", memberNo);
            cmd.ExecuteNonQuery();
        }

        private void ResetTrials(SqlConnection conn, string memberNo)
        {
            var cmd = new SqlCommand(
                "UPDATE Registration SET Trials = 0, FirstLogin = 0 WHERE MemberNo = @m", conn);
            cmd.Parameters.AddWithValue("@m", memberNo);
            cmd.ExecuteNonQuery();
        }
    }

    // ===================== SECURITY =====================
    static class PinSecurity
    {
        public static void Create(string pin, out byte[] hash, out byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(pin, 16, 100_000, HashAlgorithmName.SHA256))
            {
                salt = pbkdf2.Salt;
                hash = pbkdf2.GetBytes(32);
            }
        }

        public static bool Verify(string pin, byte[] hash, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(pin, salt, 100_000, HashAlgorithmName.SHA256))
            {
                var computed = pbkdf2.GetBytes(32);
                return FixedTimeEquals(computed, hash);
            }
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            var diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }

    }
}
