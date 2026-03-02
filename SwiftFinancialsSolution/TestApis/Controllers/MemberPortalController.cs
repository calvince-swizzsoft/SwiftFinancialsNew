using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Extensions;
using Infrastructure.Crosscutting.Framework.Utils;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using SwiftFinancials.Presentation.Infrastructure.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using TestApis.Helpers;
using TestApis.Models;
using TestApis.Services;
using static TestApis.Controllers.ValuesController;

namespace TestApis.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [AllowAnonymous]
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
        public async Task<IHttpActionResult> GetMemberWithDetailsByReference2(
    string reference2,
    [FromUri] bool includeAccounts = true,
    [FromUri] bool includeNextOfKin = true,
    [FromUri] bool includeAccountBalances = true,
    [FromUri] bool includeProductDescription = true,
    [FromUri] bool includeInterestBalanceForLoanAccounts = false,
    [FromUri] bool considerMaturityPeriodForInvestmentAccounts = false,
    [FromUri] bool includeStatements = false,
    [FromUri] DateTime? statementStartDate = null,
    [FromUri] DateTime? statementEndDate = null)
        {
            try
            {
                var memberDetail = new
                {
                    Customer = (object)null,
                    Accounts = new List<object>(),
                    NextOfKin = new List<object>(),
                    Statements = new List<object>()
                };

                // Get customer details using ADO.NET
                string customerSql = @"
SELECT 
    c.Id,
    c.Individual_FirstName + ' ' + c.Individual_LastName AS FullName,
    c.SerialNumber,
    c.Type,
    c.Individual_Type AS IndividualType,
    c.Individual_FirstName AS IndividualFirstName,
    c.Individual_LastName AS IndividualLastName,
    c.Individual_IdentityCardNumber AS IndividualIdentityCardNumber,
    c.Address_MobileLine AS AddressMobileLine,
    c.Address_Email AS AddressEmail,
    c.PersonalIdentificationNumber,
    c.Reference1,
    c.Reference2,
    c.Reference3,
    c.RegistrationDate,
    c.RecordStatus,
    c.IsDefaulter,
    c.IsLocked,
    c.NonIndividual_DateEstablished AS NonIndividualDateEstablished,
    c.Individual_BirthDate AS IndividualBirthDate
FROM swiftFin_Customers c
WHERE c.Reference2 = @reference2";

                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // Get customer
                    object customerObj = null;
                    using (var cmd = new SqlCommand(customerSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@reference2", reference2);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                customerObj = new
                                {
                                    Id = reader["Id"] != DBNull.Value ? (Guid)reader["Id"] : Guid.Empty,
                                    FullName = reader["FullName"]?.ToString(),
                                    SerialNumber = reader["SerialNumber"] != DBNull.Value ? Convert.ToInt32(reader["SerialNumber"]) : 0,
                                    Type = reader["Type"] != DBNull.Value ? Convert.ToInt32(reader["Type"]) : 0,
                                    IndividualType = reader["IndividualType"] != DBNull.Value ? Convert.ToInt32(reader["IndividualType"]) : 0,
                                    IndividualFirstName = reader["IndividualFirstName"]?.ToString(),
                                    IndividualLastName = reader["IndividualLastName"]?.ToString(),
                                    IndividualIdentityCardNumber = reader["IndividualIdentityCardNumber"]?.ToString(),
                                    AddressMobileLine = reader["AddressMobileLine"]?.ToString(),
                                    AddressEmail = reader["AddressEmail"]?.ToString(),
                                    PersonalIdentificationNumber = reader["PersonalIdentificationNumber"]?.ToString(),
                                    Reference1 = reader["Reference1"]?.ToString(),
                                    Reference2 = reader["Reference2"]?.ToString(),
                                    Reference3 = reader["Reference3"]?.ToString(),
                                    RegistrationDate = reader["RegistrationDate"] != DBNull.Value ? (DateTime?)reader["RegistrationDate"] : null,
                                    RecordStatus = reader["RecordStatus"] != DBNull.Value ? Convert.ToInt32(reader["RecordStatus"]) : 0,
                                    IsDefaulter = reader["IsDefaulter"] != DBNull.Value && (bool)reader["IsDefaulter"],
                                    IsLocked = reader["IsLocked"] != DBNull.Value && (bool)reader["IsLocked"],
                                    NonIndividualDateEstablished = reader["NonIndividualDateEstablished"] != DBNull.Value ? (DateTime?)reader["NonIndividualDateEstablished"] : null,
                                    IndividualBirthDate = reader["IndividualBirthDate"] != DBNull.Value ? (DateTime?)reader["IndividualBirthDate"] : null
                                };
                            }
                        }
                    }

                    if (customerObj == null)
                    {
                        return Json(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Member with Reference2 '{reference2}' not found.",
                            Data = null
                        });
                    }

                    // Set customer in memberDetail
                    memberDetail = new
                    {
                        Customer = customerObj,
                        Accounts = new List<object>(),
                        NextOfKin = new List<object>(),
                        Statements = new List<object>()
                    };

                    var customerId = (Guid)customerObj.GetType().GetProperty("Id").GetValue(customerObj);

                    // Get accounts if requested - UPDATED Status mapping
                    if (includeAccounts && customerId != Guid.Empty)
                    {
                        string accountsSql = @"
SELECT 
    ca.Id,
    -- Build FullAccountNumber from components (you may need to adjust this based on your actual logic)
    CAST(b.Code AS VARCHAR) + '-' + CAST(c.SerialNumber AS VARCHAR) + '-' + 
    CAST(ca.CustomerAccountType_ProductCode AS VARCHAR) + '-' + 
    CAST(ca.CustomerAccountType_TargetProductCode AS VARCHAR) AS FullAccountNumber,
    COALESCE(lp.Description, sp.Description) AS ProductDescription,
    ca.CustomerAccountType_ProductCode,
    ca.CustomerAccountType_TargetProductCode,
    ca.Status,
    CASE 
        WHEN ca.Status = 0 THEN 'Active'      -- Based on your feedback: 0 = Active
        WHEN ca.Status = 1 THEN 'Inactive'    -- Assuming 1 = Inactive
        WHEN ca.Status = 2 THEN 'Dormant'     -- You can add other statuses as needed
        WHEN ca.Status = 3 THEN 'Closed'
        ELSE 'Unknown'
    END AS StatusDescription,
    ca.RecordStatus,
    ca.CreatedDate,
    ca.Remarks,
    -- Get balance from JournalEntries if needed
    (SELECT ISNULL(SUM(Amount), 0) FROM swiftFin_JournalEntries je WHERE je.CustomerAccountId = ca.Id) AS CalculatedBalance
FROM swiftFin_CustomerAccounts ca
INNER JOIN swiftFin_Customers c ON ca.CustomerId = c.Id
LEFT JOIN swiftFin_Branches b ON ca.BranchId = b.Id
LEFT JOIN swiftFin_LoanProducts lp ON lp.Id = ca.CustomerAccountType_TargetProductId
LEFT JOIN swiftFin_SavingsProducts sp ON sp.Id = ca.CustomerAccountType_TargetProductId
WHERE ca.CustomerId = @customerId";

                        var accounts = new List<object>();
                        using (var cmd = new SqlCommand(accountsSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@customerId", customerId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var accountObj = new
                                    {
                                        Id = reader["Id"] != DBNull.Value ? (Guid)reader["Id"] : Guid.Empty,
                                        FullAccountNumber = reader["FullAccountNumber"]?.ToString(),
                                        ProductDescription = reader["ProductDescription"]?.ToString(),
                                        CustomerAccountTypeProductCode = reader["CustomerAccountType_ProductCode"] != DBNull.Value ? Convert.ToInt32(reader["CustomerAccountType_ProductCode"]) : 0,
                                        CustomerAccountTypeTargetProductCode = reader["CustomerAccountType_TargetProductCode"] != DBNull.Value ? Convert.ToInt32(reader["CustomerAccountType_TargetProductCode"]) : 0,
                                        Status = reader["Status"] != DBNull.Value ? Convert.ToInt32(reader["Status"]) : 0,
                                        StatusDescription = reader["StatusDescription"]?.ToString() ??
                                            (reader["Status"] != DBNull.Value ?
                                                (Convert.ToInt32(reader["Status"]) == 0 ? "Active" : "Unknown") : "Unknown"),
                                        RecordStatus = reader["RecordStatus"] != DBNull.Value ? Convert.ToInt32(reader["RecordStatus"]) : 0,
                                        CreatedDate = reader["CreatedDate"] != DBNull.Value ? (DateTime?)reader["CreatedDate"] : null,
                                        Remarks = reader["Remarks"]?.ToString(),
                                        // Include balance if needed
                                        Balance = includeAccountBalances && reader["CalculatedBalance"] != DBNull.Value ?
                                            Convert.ToDecimal(reader["CalculatedBalance"]) : 0
                                    };
                                    accounts.Add(accountObj);
                                }
                            }
                        }

                        // Update memberDetail with accounts
                        var tempMemberDetail = memberDetail;
                        memberDetail = new
                        {
                            Customer = tempMemberDetail.Customer,
                            Accounts = accounts,
                            NextOfKin = tempMemberDetail.NextOfKin,
                            Statements = tempMemberDetail.Statements
                        };

                        // Get statements if requested
                        if (includeStatements && accounts.Any())
                        {
                            var statements = await GetAccountStatementsUsingAdoNetAsync(conn, customerId, statementStartDate, statementEndDate);

                            tempMemberDetail = memberDetail;
                            memberDetail = new
                            {
                                Customer = tempMemberDetail.Customer,
                                Accounts = tempMemberDetail.Accounts,
                                NextOfKin = tempMemberDetail.NextOfKin,
                                Statements = statements
                            };
                        }
                    }

                    // Get next of kin if requested
                    if (includeNextOfKin && customerId != Guid.Empty)
                    {
                        string nokSql = @"
SELECT 
    Id,
    FirstName + ' ' + LastName AS FullName,
    FirstName,
    LastName,
    Relationship,
    -- You might need to join with a lookup table for RelationshipDescription
    Address_MobileLine AS AddressMobileLine,
    Address_Email AS AddressEmail,
    Address_AddressLine1 AS AddressAddressLine1,
    Address_City AS AddressCity,
    NominatedPercentage,
    CreatedDate
FROM swiftFin_NextOfKin
WHERE CustomerId = @customerId";

                        var nextOfKins = new List<object>();
                        using (var cmd = new SqlCommand(nokSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@customerId", customerId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var nokObj = new
                                    {
                                        Id = reader["Id"] != DBNull.Value ? (Guid)reader["Id"] : Guid.Empty,
                                        FullName = reader["FullName"]?.ToString(),
                                        FirstName = reader["FirstName"]?.ToString(),
                                        LastName = reader["LastName"]?.ToString(),
                                        Relationship = reader["Relationship"] != DBNull.Value ? Convert.ToInt32(reader["Relationship"]) : 0,
                                        // RelationshipDescription = reader["RelationshipDescription"]?.ToString(), // Comment out if not exists
                                        AddressMobileLine = reader["AddressMobileLine"]?.ToString(),
                                        AddressEmail = reader["AddressEmail"]?.ToString(),
                                        AddressAddressLine1 = reader["AddressAddressLine1"]?.ToString(),
                                        AddressCity = reader["AddressCity"]?.ToString(),
                                        NominatedPercentage = reader["NominatedPercentage"] != DBNull.Value ? Convert.ToDecimal(reader["NominatedPercentage"]) : 0,
                                        CreatedDate = reader["CreatedDate"] != DBNull.Value ? (DateTime?)reader["CreatedDate"] : null
                                    };
                                    nextOfKins.Add(nokObj);
                                }
                            }
                        }

                        // Update memberDetail with next of kin
                        var tempMemberDetail = memberDetail;
                        memberDetail = new
                        {
                            Customer = tempMemberDetail.Customer,
                            Accounts = tempMemberDetail.Accounts,
                            NextOfKin = nextOfKins,
                            Statements = tempMemberDetail.Statements
                        };
                    }
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Member retrieved successfully.",
                    Data = new
                    {
                        Member = memberDetail,
                        Summary = new
                        {
                            IncludeAccounts = includeAccounts,
                            IncludeNextOfKin = includeNextOfKin,
                            IncludeStatements = includeStatements,
                            AccountCount = ((List<object>)memberDetail.Accounts).Count,
                            NextOfKinCount = ((List<object>)memberDetail.NextOfKin).Count,
                            StatementCount = ((List<object>)memberDetail.Statements).Count,
                            StatementDateRange = includeStatements ? new
                            {
                                StartDate = statementStartDate?.ToString("yyyy-MM-dd"),
                                EndDate = statementEndDate?.ToString("yyyy-MM-dd")
                            } : null
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error in GetMemberWithDetailsByReference2: {ex.Message}");
                System.Diagnostics.Trace.TraceError($"Stack Trace: {ex.StackTrace}");

                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving member details.",
                    Data = new { Error = ex.Message, InnerError = ex.InnerException?.Message }
                });
            }
        }

        private async Task<List<object>> GetAccountStatementsUsingAdoNetAsync(
            SqlConnection connection,
            Guid customerId,
            DateTime? startDate,
            DateTime? endDate)
        {
            var statements = new List<object>();

            string statementsSql = @"
SELECT 
    je.Id,
    je.ValueDate,
    je.Description,
    je.Amount,
    je.Balance,
    ca.Id AS AccountId,
    ca.FullAccountNumber
FROM swiftFin_JournalEntries je
INNER JOIN swiftFin_CustomerAccounts ca ON je.CustomerAccountId = ca.Id
WHERE ca.CustomerId = @customerId
AND (@startDate IS NULL OR je.ValueDate >= @startDate)
AND (@endDate IS NULL OR je.ValueDate <= @endDate)
ORDER BY je.ValueDate DESC";

            using (var cmd = new SqlCommand(statementsSql, connection))
            {
                cmd.Parameters.AddWithValue("@customerId", customerId);
                cmd.Parameters.AddWithValue("@startDate", startDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@endDate", endDate ?? (object)DBNull.Value);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var statementObj = new
                        {
                            Id = reader["Id"] != DBNull.Value ? (Guid)reader["Id"] : Guid.Empty,
                            ValueDate = reader["ValueDate"] != DBNull.Value ? (DateTime?)reader["ValueDate"] : null,
                            Description = reader["Description"]?.ToString(),
                            Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0,
                            Balance = reader["Balance"] != DBNull.Value ? Convert.ToDecimal(reader["Balance"]) : 0,
                            AccountId = reader["AccountId"] != DBNull.Value ? (Guid)reader["AccountId"] : Guid.Empty,
                            FullAccountNumber = reader["FullAccountNumber"]?.ToString()
                        };
                        statements.Add(statementObj);
                    }
                }
            }

            return statements;
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
        [Route("GetCustomerShareStatement/{customerAccountId}")]
        public async Task<HttpResponseMessage> GetCustomerShareStatement(Guid customerAccountId, DateTime? startDate = null, DateTime? endDate = null, bool downloadPdf = false)
        {
            try
            {
                // Create a simple model for SQL results
                var customerData = new
                {
                    FirstName = "",
                    LastName = "",
                    Mobile = "",
                    Email = "",
                    Reference2 = "",
                    Reference3 = "",
                    BranchCode = 0,
                    CustomerSerialNumber = 0,
                    ProductCode = 0,
                    TargetProductCode = 0
                };

                var statementRows = new List<CustomerShareStatementRow>();
                decimal totalContribution = 0;

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get customer and account information - FIXED query
                    string customerQuery = @"
               SELECT TOP 1 
                   c.Individual_FirstName,
                   c.Individual_LastName,
                   c.Address_MobileLine,
                   c.Address_Email,
                   c.Reference2,
                   c.Reference3,
                   b.Code as BranchCode,
                   c.SerialNumber as CustomerSerialNumber,
                   ca.CustomerAccountType_ProductCode,
                   ca.CustomerAccountType_TargetProductCode
               FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_CustomerAccounts] ca
               INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Customers] c 
                   ON ca.CustomerId = c.Id
               INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Branches] b
                   ON ca.BranchId = b.Id
               WHERE ca.Id = @CustomerAccountId";

                    using (var cmd = new SqlCommand(customerQuery, connection))
                    {
                        cmd.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = customerAccountId;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                customerData = new
                                {
                                    FirstName = reader["Individual_FirstName"]?.ToString() ?? "",
                                    LastName = reader["Individual_LastName"]?.ToString() ?? "",
                                    Mobile = reader["Address_MobileLine"]?.ToString() ?? "",
                                    Email = reader["Address_Email"]?.ToString() ?? "",
                                    Reference2 = reader["Reference2"]?.ToString() ?? "",
                                    Reference3 = reader["Reference3"]?.ToString() ?? "",
                                    BranchCode = Convert.ToInt32(reader["BranchCode"]),
                                    CustomerSerialNumber = Convert.ToInt32(reader["CustomerSerialNumber"]),
                                    ProductCode = Convert.ToInt32(reader["CustomerAccountType_ProductCode"]),
                                    TargetProductCode = Convert.ToInt32(reader["CustomerAccountType_TargetProductCode"])
                                };
                            }
                            else
                            {
                                // Customer not found
                                var response = Request.CreateResponse(HttpStatusCode.NotFound);
                                response.Content = new StringContent(
                                    JsonConvert.SerializeObject(new ApiResponse<object>
                                    {
                                        Success = false,
                                        Message = "Customer account not found.",
                                        Data = null
                                    }),
                                    Encoding.UTF8,
                                    "application/json");
                                return response;
                            }
                        }
                    }

                    // Now get share statement
                    using (var command = new SqlCommand("usp_GetCustomerShareStatement", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = customerAccountId;
                        command.Parameters.Add("@StartDate", SqlDbType.Date).Value = (object)startDate ?? DBNull.Value;
                        command.Parameters.Add("@EndDate", SqlDbType.Date).Value = (object)endDate ?? DBNull.Value;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // First result set: Statement rows
                            while (await reader.ReadAsync())
                            {
                                var row = new CustomerShareStatementRow
                                {
                                    Date = reader["Date"].ToString(),
                                    ShareContribution = Convert.ToDecimal(reader["Share Contribution"]),
                                    Cumulative = Convert.ToDecimal(reader["Cumulative"]),
                                    Description = reader["Description"].ToString()
                                };
                                statementRows.Add(row);
                            }

                            // Move to second result set: Total contribution
                            if (await reader.NextResultAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    totalContribution = reader["TotalContribution"] != DBNull.Value ?
                                        Convert.ToDecimal(reader["TotalContribution"]) : 0;
                                }
                            }
                        }
                    }
                }

                // Build the full account number using the same logic as in the DTO
                string fullAccountNumber = string.Format("{0}-{1}-{2}-{3}",
                    customerData.BranchCode.ToString().PadLeft(3, '0'),
                    customerData.CustomerSerialNumber.ToString().PadLeft(7, '0'),
                    customerData.ProductCode.ToString().PadLeft(3, '0'),
                    customerData.TargetProductCode.ToString().PadLeft(3, '0'));

                // Create the result object with customer info
                var shareStatementResult = new
                {
                    Customer = new
                    {
                        FullName = $"{customerData.FirstName} {customerData.LastName}".Trim(),
                        AccountNumber = fullAccountNumber,
                        StaffNo = customerData.Reference2,
                        PFNumber = customerData.Reference3,
                        Mobile = customerData.Mobile,
                        Email = customerData.Email
                    },
                    Statement = statementRows,
                    TotalContribution = totalContribution
                };

                // If PDF download is requested
                if (downloadPdf)
                {
                    byte[] pdfBytes = GenerateShareStatementPdf(customerData, fullAccountNumber, statementRows, totalContribution, startDate, endDate);

                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(pdfBytes)
                    };

                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                    string customerName = $"{customerData.FirstName}_{customerData.LastName}".Replace(" ", "_");
                    response.Content.Headers.ContentDisposition =
                        new ContentDispositionHeaderValue("attachment")
                        {
                            FileName = $"ShareStatement_{customerName}_{DateTime.Now:yyyyMMdd}.pdf"
                        };

                    return response;
                }
                else
                {
                    // Return JSON response
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(
                        JsonConvert.SerializeObject(new ApiResponse<object>
                        {
                            Success = true,
                            Message = statementRows.Count > 0 ?
                                $"Share statement retrieved successfully. Total: {totalContribution:C}" :
                                "No transactions found for the given period.",
                            Data = shareStatementResult
                        }),
                        Encoding.UTF8,
                        "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(
                    JsonConvert.SerializeObject(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "An error occurred while retrieving customer share statement.",
                        Data = ex.Message
                    }),
                    Encoding.UTF8,
                    "application/json");
                return response;
            }
        }
        #region GenerateShareStatementPdf
        private byte[] GenerateShareStatementPdf(dynamic customerData, string fullAccountNumber,
 List<CustomerShareStatementRow> statementRows, decimal totalContribution,
 DateTime? startDate = null, DateTime? endDate = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Create document with smaller margins for better space utilization
                Document document = new Document(PageSize.A4, 30, 30, 50, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // ===== RUBANI SACCO COLOR THEME =====
                BaseColor SkyBlue = new BaseColor(0, 174, 239); // #00AEEF
                BaseColor Red = new BaseColor(255, 0, 0);       // #FF0000
                BaseColor DarkGray = new BaseColor(26, 26, 26); // #1A1A1A
                BaseColor LightGray = new BaseColor(217, 217, 217); // #D9D9D9
                BaseColor White = BaseColor.WHITE;
                BaseColor TableHeaderBlue = new BaseColor(173, 216, 230); // Light blue for table headers

                // Fonts using Rubani Sacco theme
                Font titleFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, DarkGray));
                Font headerFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, DarkGray));
                Font subHeaderFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, White));
                Font normalFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA, 9));
                Font boldFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9));
                Font smallFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA, 8, DarkGray));
                Font amountFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, DarkGray));

                // ===== CUSTOM HEADER WITH RUBANI SACCO LOGO =====
                try
                {
                    // Create a table with 1 column for logo on top, then company info below
                    PdfPTable headerTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    // Row 1: Logo centered at top
                    PdfPCell logoCell = new PdfPCell();
                    logoCell.Border = Rectangle.NO_BORDER;
                    logoCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    logoCell.PaddingBottom = 5f;

                    // Try to load logo from local path
                    string logoPath = @"C:/Users/Karenju/Desktop/testapidebug/Assets/Images/rubani-logo.jpeg";
                    if (File.Exists(logoPath))
                    {
                        try
                        {
                            Image logo = Image.GetInstance(logoPath);
                            logo.ScaleToFit(100, 100); // Increased size for better visibility
                            logoCell.AddElement(logo);
                        }
                        catch (Exception)
                        {
                            // Fallback to text if image fails to load
                            logoCell.AddElement(new Paragraph("RUBANI SACCO",
                                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, SkyBlue))
                            {
                                Alignment = Element.ALIGN_CENTER
                            });
                        }
                    }
                    else
                    {
                        // Use text if no logo file
                        logoCell.AddElement(new Paragraph("RUBANI SACCO",
                            FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, SkyBlue))
                        {
                            Alignment = Element.ALIGN_CENTER
                        });
                    }

                    headerTable.AddCell(logoCell);

                    // Row 2: Company Info - LEFT ALIGNED BELOW LOGO
                    PdfPCell infoCell = new PdfPCell();
                    infoCell.Border = Rectangle.NO_BORDER;
                    infoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    infoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    infoCell.PaddingTop = 5f;

                    // Company name - LEFT ALIGNED
                    var companyNamePara = new Paragraph("RUBANI SACCO",
                        FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, SkyBlue))
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(companyNamePara);

                    // Address - LEFT ALIGNED
                    var address = new Paragraph("Rubani House, Off Airport North Embakasi",
                        FontFactory.GetFont(FontFactory.HELVETICA, 10))
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(address);

                    // Email - LEFT ALIGNED
                    var email = new Paragraph("rubanisacco@gmail.com",
                        FontFactory.GetFont(FontFactory.HELVETICA, 10))
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(email);

                    headerTable.AddCell(infoCell);

                    document.Add(headerTable);

                    // Add decorative line (Blue-Red-Blue)
                    var lineTable = new PdfPTable(3)
                    {
                        WidthPercentage = 100,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        SpacingAfter = 10f
                    };
                    lineTable.SetWidths(new float[] { 33, 34, 33 });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = Red,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    document.Add(lineTable);
                }
                catch (Exception)
                {
                    // Fallback header if anything goes wrong
                    var fallbackPara = new Paragraph("RUBANI SACCO\nRubani House, Off Airport North Embakasi\nrubanisacco@gmail.com",
                        FontFactory.GetFont(FontFactory.HELVETICA, 10))
                    {
                        Alignment = Element.ALIGN_LEFT,
                        SpacingAfter = 15f,
                        IndentationLeft = 0f
                    };
                    document.Add(fallbackPara);
                }

                // ===== STATEMENT TITLE =====
                document.Add(new Paragraph("SHARES STATEMENT", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10f
                });

                // ===== MEMBER INFORMATION SECTION =====
                string fullName = $"{customerData.FirstName} {customerData.LastName}".Trim().ToUpper();
                string staffNo = customerData.Reference2;
                string pfNumber = customerData.Reference3;

                // Use Paragraphs for member info (like in loan statement)
                Paragraph memberInfo = new Paragraph();
                memberInfo.Alignment = Element.ALIGN_LEFT;

                // Add member info with proper formatting
                memberInfo.Add(new Chunk("Name: ", boldFont));
                memberInfo.Add(new Chunk(fullName, normalFont));
                memberInfo.Add(new Chunk("   MemberNo: ", boldFont));
                memberInfo.Add(new Chunk(staffNo ?? "N/A", normalFont));
                memberInfo.Add(Chunk.NEWLINE);

                memberInfo.Add(new Chunk("Staff No: ", boldFont));
                memberInfo.Add(new Chunk(pfNumber ?? "N/A", normalFont));
                memberInfo.Add(new Chunk("   Account No: ", boldFont));
                memberInfo.Add(new Chunk(fullAccountNumber, normalFont));

                memberInfo.SpacingAfter = 15f;
                document.Add(memberInfo);

                // ===== STATEMENT PERIOD SECTION =====
                if (startDate.HasValue || endDate.HasValue)
                {
                    string periodText = "Statement Period: ";
                    if (startDate.HasValue && endDate.HasValue)
                        periodText += $"{startDate.Value:dd/MM/yyyy} to {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        periodText += $"From {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        periodText += $"To {endDate.Value:dd/MM/yyyy}";

                    var periodPara = new Paragraph(periodText, boldFont)
                    {
                        Alignment = Element.ALIGN_LEFT,
                        SpacingAfter = 10f
                    };
                    document.Add(periodPara);
                }

                // ===== SUMMARY SECTION =====
                PdfPTable summaryTable = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingAfter = 15f
                };
                summaryTable.SetWidths(new float[] { 50, 50 });

                // Summary header
                var summaryHeader = new PdfPCell(new Phrase("SUMMARY", subHeaderFont))
                {
                    BackgroundColor = DarkGray,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 8,
                    Colspan = 2,
                    Border = Rectangle.NO_BORDER,
                    BorderWidthBottom = 2f,
                    BorderColorBottom = SkyBlue
                };
                summaryTable.AddCell(summaryHeader);

                // Total Contribution row
                summaryTable.AddCell(new PdfPCell(new Phrase("Total Share Contribution:", boldFont))
                {
                    Border = Rectangle.NO_BORDER,
                    Padding = 8,
                    BackgroundColor = TableHeaderBlue
                });

                summaryTable.AddCell(new PdfPCell(new Phrase(totalContribution.ToString("N2"), amountFont))
                {
                    Border = Rectangle.NO_BORDER,
                    Padding = 8,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = TableHeaderBlue
                });

                document.Add(summaryTable);

                // ===== TRANSACTIONS SECTION =====
                if (statementRows != null && statementRows.Count > 0)
                {
                    // Section header
                    var sectionHeaderPara = new Paragraph("TRANSACTION DETAILS",
                        FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, White))
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5f
                    };

                    // Create a background for the header
                    PdfPTable headerBgTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    PdfPCell headerCell = new PdfPCell(sectionHeaderPara)
                    {
                        BackgroundColor = DarkGray,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 8,
                        Border = Rectangle.NO_BORDER,
                        BorderWidthBottom = 2f,
                        BorderColorBottom = SkyBlue
                    };
                    headerBgTable.AddCell(headerCell);
                    document.Add(headerBgTable);

                    // Transactions table with 4 columns - NO BORDERS
                    PdfPTable transTable = new PdfPTable(4)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 5f
                    };
                    transTable.SetWidths(new float[] { 20, 40, 20, 20 });

                    // Table headers - NO BORDERS
                    string[] headers = { "Date", "Description", "Share Contribution", "Cumulative" };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        PdfPCell headerCellItem = new PdfPCell(new Phrase(headers[i], headerFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5,
                            // NO BORDERS - remove all border widths
                            BorderWidthTop = 0f,
                            BorderWidthBottom = 0f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f
                        };
                        transTable.AddCell(headerCellItem);
                    }

                    // Add transactions with NO BORDERS between rows
                    for (int rowIndex = 0; rowIndex < statementRows.Count; rowIndex++)
                    {
                        var row = statementRows[rowIndex];

                        // Date cell - NO BORDERS
                        PdfPCell dateCell = new PdfPCell(new Phrase(row.Date, normalFont));
                        dateCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        dateCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        dateCell.BorderWidthTop = 0f;
                        dateCell.BorderWidthBottom = 0f;
                        dateCell.BorderWidthLeft = 0f;
                        dateCell.BorderWidthRight = 0f;
                        transTable.AddCell(dateCell);

                        // Description cell - NO BORDERS
                        PdfPCell descCell = new PdfPCell(new Phrase(row.Description ?? "", normalFont));
                        descCell.HorizontalAlignment = Element.ALIGN_LEFT;
                        descCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        descCell.BorderWidthTop = 0f;
                        descCell.BorderWidthBottom = 0f;
                        descCell.BorderWidthLeft = 0f;
                        descCell.BorderWidthRight = 0f;
                        transTable.AddCell(descCell);

                        // Share Contribution cell - NO BORDERS
                        PdfPCell shareCell = new PdfPCell(new Phrase(row.ShareContribution.ToString("N2"), normalFont));
                        shareCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        shareCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        shareCell.BorderWidthTop = 0f;
                        shareCell.BorderWidthBottom = 0f;
                        shareCell.BorderWidthLeft = 0f;
                        shareCell.BorderWidthRight = 0f;
                        transTable.AddCell(shareCell);

                        // Cumulative cell - NO BORDERS
                        PdfPCell cumulativeCell = new PdfPCell(new Phrase(row.Cumulative.ToString("N2"), normalFont));
                        cumulativeCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cumulativeCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        cumulativeCell.BorderWidthTop = 0f;
                        cumulativeCell.BorderWidthBottom = 0f;
                        cumulativeCell.BorderWidthLeft = 0f;
                        cumulativeCell.BorderWidthRight = 0f;
                        transTable.AddCell(cumulativeCell);
                    }

                    document.Add(transTable);
                }
                else
                {
                    var noTransPara = new Paragraph("No transactions found for the selected period.", normalFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 20f,
                        SpacingAfter = 20f
                    };
                    document.Add(noTransPara);
                }

                // ===== GRAND TOTAL SECTION =====
                document.Add(new Paragraph("\n"));
                PdfPTable grandTotalTable = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingAfter = 20f
                };
                grandTotalTable.SetWidths(new float[] { 70, 30 });

                grandTotalTable.AddCell(new PdfPCell(new Phrase("GRAND TOTAL SHARE CONTRIBUTION:",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, DarkGray)))
                {
                    Border = Rectangle.NO_BORDER,
                    Padding = 10,
                    BackgroundColor = TableHeaderBlue
                });

                grandTotalTable.AddCell(new PdfPCell(new Phrase(totalContribution.ToString("N2"),
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, Red)))
                {
                    Border = Rectangle.NO_BORDER,
                    Padding = 10,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    BackgroundColor = TableHeaderBlue
                });

                document.Add(grandTotalTable);

                // ===== CUSTOM FOOTER =====
                document.Add(new Paragraph("\n"));
                var footerPara = new Paragraph(
                    $"Statement Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Page: 1",
                    smallFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 10f
                };
                document.Add(footerPara);

                // ===== FOOTER NOTES =====
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph("This is a system generated statement.", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });
                document.Add(new Paragraph("For any queries, contact: rubanisacco@gmail.com", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }

        // Updated helper method with NO borders at all
        private PdfPCell CreateShareStyledCell(string text, Font font, int alignment = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text ?? "", font));
            cell.HorizontalAlignment = alignment;
            cell.Padding = 5f;

            // REMOVE ALL BORDERS
            cell.BorderWidthLeft = 0f;
            cell.BorderWidthRight = 0f;
            cell.BorderWidthTop = 0f;
            cell.BorderWidthBottom = 0f;

            return cell;
        }

        public class SasraForm6Row
        {
            public string ReportSection { get; set; }
            public string LineItem { get; set; }
            public decimal? Amount { get; set; }
            public int DisplayOrder { get; set; }
        }

        public class SasraForm6Meta
        {
            public string SaccoName { get; set; }
            public DateTime FiscalStartDate { get; set; }
            public DateTime PeriodEndingDate { get; set; }
            public DateTime GeneratedDate { get; set; }
        }

        public class SasraReportRow
        {
            public string ReportSection { get; set; }
            public string LineItem { get; set; }
            public decimal? Amount { get; set; }
            public int DisplayOrder { get; set; }
        }

        public class SasraReportMeta
        {
            public string SaccoName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DateTime GeneratedDate { get; set; }
        }

        public class SasraForm5Row
        {
            public string RefNo { get; set; }
            public string Description { get; set; }
            public decimal Amount { get; set; }
        }

        public class SasraFormMeta
        {
            public string SaccoName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DateTime GeneratedDate { get; set; }
        }


        [HttpGet]
        [Route("GenerateSasraForm6Pdf")]
        public HttpResponseMessage GenerateSasraForm6Pdf(DateTime startDate, DateTime endDate)
        {
            var reportRows = new List<SasraForm6Row>();
            SasraForm6Meta meta = null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.sp_GenerateSASRAForm6_Report", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate;
                cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate;

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    // 🔹 First result set (report body)
                    while (reader.Read())
                    {
                        reportRows.Add(new SasraForm6Row
                        {
                            ReportSection = reader["ReportSection"].ToString(),
                            LineItem = reader["LineItem"].ToString(),
                            Amount = reader["Amount"] == DBNull.Value
                                ? (decimal?)null
                                : Convert.ToDecimal(reader["Amount"]),
                            DisplayOrder = Convert.ToInt32(reader["DisplayOrder"])
                        });
                    }

                    // 🔹 Second result set (metadata)
                    if (reader.NextResult() && reader.Read())
                    {
                        meta = new SasraForm6Meta
                        {
                            SaccoName = reader["SaccoName"].ToString(),
                            FiscalStartDate = Convert.ToDateTime(reader["FiscalStartDate"]),
                            PeriodEndingDate = Convert.ToDateTime(reader["PeriodEndingDate"]),
                            GeneratedDate = Convert.ToDateTime(reader["GeneratedDate"])
                        };
                    }
                }
            }

            // ================= PDF GENERATION =================
            var ms = new MemoryStream();
            var document = new Document(PageSize.A4, 36, 36, 36, 36);
            PdfWriter.GetInstance(document, ms);

            document.Open();

            // 🔹 Fonts
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var amountFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            // 🔹 Header
            document.Add(new Paragraph(meta.SaccoName, titleFont));
            document.Add(new Paragraph("SASRA FORM 6 – CAPITAL ADEQUACY REPORT", titleFont));
            document.Add(new Paragraph(
                $"Period: {meta.FiscalStartDate:dd-MMM-yyyy} to {meta.PeriodEndingDate:dd-MMM-yyyy}",
                normalFont));
            document.Add(new Paragraph($"Generated: {meta.GeneratedDate:dd-MMM-yyyy HH:mm}", normalFont));
            document.Add(new Paragraph(" "));

            // 🔹 Table with NO BORDERS
            PdfPTable table = new PdfPTable(2)
            {
                WidthPercentage = 100
            };

            // Set border width to 0 to remove all grid lines
            table.DefaultCell.BorderWidth = 0;
            table.DefaultCell.Border = Rectangle.NO_BORDER;

            table.SetWidths(new float[] { 70, 30 });

            string currentSection = null;

            foreach (var row in reportRows.OrderBy(r => r.DisplayOrder))
            {
                // Section Header
                if (row.ReportSection != currentSection)
                {
                    currentSection = row.ReportSection;

                    var sectionCell = new PdfPCell(new Phrase(row.LineItem, sectionFont))
                    {
                        Colspan = 2,
                        BackgroundColor = BaseColor.LIGHT_GRAY,
                        Padding = 5,
                        Border = Rectangle.NO_BORDER,  // Remove border from section cells too
                        BorderWidth = 0
                    };
                    table.AddCell(sectionCell);
                    continue;
                }

                // Regular row cells
                var descriptionCell = new PdfPCell(new Phrase(row.LineItem, normalFont))
                {
                    Border = Rectangle.NO_BORDER,
                    BorderWidth = 0,
                    PaddingBottom = 3  // Add some spacing between rows
                };
                table.AddCell(descriptionCell);

                var amountCell = new PdfPCell(new Phrase(
                    row.Amount.HasValue ? row.Amount.Value.ToString("N2") : "",
                    amountFont))
                {
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Border = Rectangle.NO_BORDER,
                    BorderWidth = 0,
                    PaddingBottom = 3
                };
                table.AddCell(amountCell);
            }

            document.Add(table);
            document.Close();

            // ================= RESPONSE =================
            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(ms.ToArray())
            };

            result.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/pdf");

            result.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"SASRA_Form6_{endDate:yyyyMMdd}.pdf"
                };

            return result;
        }


        [HttpGet]
        [Route("GenerateSasraForm2LiquidityPdf")]
        public HttpResponseMessage GenerateSasraForm2LiquidityPdf(
     DateTime startDate,
     DateTime endDate)
        {
            var rows = new List<SasraReportRow>();
            SasraReportMeta meta = null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                "dbo.sp_GenerateSASRAForm2_Liquidity_Report", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate;
                cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate;

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    // ===== Result Set 1: Report Body =====
                    while (reader.Read())
                    {
                        rows.Add(new SasraReportRow
                        {
                            ReportSection = reader["ReportSection"].ToString(),
                            LineItem = reader["LineItem"].ToString(),
                            Amount = reader["Amount"] == DBNull.Value
                                ? (decimal?)null
                                : Convert.ToDecimal(reader["Amount"]),
                            DisplayOrder = Convert.ToInt32(reader["DisplayOrder"])
                        });
                    }

                    // ===== Result Set 2: Metadata =====
                    if (reader.NextResult() && reader.Read())
                    {
                        meta = new SasraReportMeta
                        {
                            SaccoName = reader["SaccoName"].ToString(),
                            StartDate = Convert.ToDateTime(reader["StartDate"]),
                            EndDate = Convert.ToDateTime(reader["EndDate"]),
                            GeneratedDate = Convert.ToDateTime(reader["GeneratedDate"])
                        };
                    }
                }
            }

            // ================= PDF =================
            var ms = new MemoryStream();
            var doc = new Document(PageSize.A4, 36, 36, 36, 36);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            // ===== Header =====
            doc.Add(new Paragraph(meta.SaccoName, titleFont));
            doc.Add(new Paragraph("SASRA FORM 2 – LIQUIDITY RETURN", titleFont));
            doc.Add(new Paragraph(
                $"Period: {meta.StartDate:dd-MMM-yyyy} to {meta.EndDate:dd-MMM-yyyy}",
                normalFont));
            doc.Add(new Paragraph(
                $"Generated: {meta.GeneratedDate:dd-MMM-yyyy HH:mm}",
                normalFont));
            doc.Add(new Paragraph(" "));

            // ===== Table =====
            PdfPTable table = new PdfPTable(2)
            {
                WidthPercentage = 100
            };
            table.SetWidths(new float[] { 70, 30 });

            // Remove all table borders
            table.DefaultCell.Border = Rectangle.NO_BORDER;

            string currentSection = null;

            foreach (var row in rows.OrderBy(r => r.DisplayOrder))
            {
                // Section headers
                if (row.Amount == null)
                {
                    var sectionCell = new PdfPCell(
                        new Phrase(row.LineItem, sectionFont))
                    {
                        Colspan = 2,
                        BackgroundColor = BaseColor.LIGHT_GRAY,
                        Padding = 6,
                        Border = Rectangle.NO_BORDER  // Remove border for section cells too
                    };
                    table.AddCell(sectionCell);
                    continue;
                }

                // Create normal cells with NO_BORDER
                var lineItemCell = new PdfPCell(
                    new Phrase(row.LineItem, normalFont))
                {
                    Border = Rectangle.NO_BORDER
                };

                var amountCell = new PdfPCell(
                    new Phrase(row.Amount.Value.ToString("N2"), normalFont))
                {
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Border = Rectangle.NO_BORDER
                };

                table.AddCell(lineItemCell);
                table.AddCell(amountCell);
            }

            doc.Add(table);
            doc.Close();

            // ================= RESPONSE =================
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(ms.ToArray())
            };

            response.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/pdf");

            response.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"SASRA_Form2_Liquidity_{endDate:yyyyMMdd}.pdf"
                };

            return response;
        }

        [HttpGet]
        [Route("GenerateSasraForm5InvestmentPdf")]
        public HttpResponseMessage GenerateSasraForm5InvestmentPdf(
    DateTime startDate,
    DateTime endDate)
        {
            var rows = new List<SasraForm5Row>();
            SasraFormMeta meta = null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                "dbo.sp_GenerateSASRAForm5_Investment_Return", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate;
                cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate;

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    // ===== Result Set 1: Form 5 Rows =====
                    while (reader.Read())
                    {
                        rows.Add(new SasraForm5Row
                        {
                            RefNo = reader["RefNo"].ToString(),
                            Description = reader["Description"].ToString(),
                            Amount = Convert.ToDecimal(reader["Amount"])
                        });
                    }

                    // ===== Result Set 2: Metadata =====
                    if (reader.NextResult() && reader.Read())
                    {
                        meta = new SasraFormMeta
                        {
                            SaccoName = reader["SaccoName"].ToString(),
                            StartDate = Convert.ToDateTime(reader["StartDate"]),
                            EndDate = Convert.ToDateTime(reader["EndDate"]),
                            GeneratedDate = Convert.ToDateTime(reader["GeneratedDate"])
                        };
                    }
                }
            }

            // ================= PDF GENERATION =================
            var ms = new MemoryStream();
            var doc = new Document(PageSize.A4, 36, 36, 36, 36);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            // ===== Header =====
            doc.Add(new Paragraph(meta.SaccoName, titleFont));
            doc.Add(new Paragraph("SASRA FORM 5 – INVESTMENT RETURN", titleFont));
            doc.Add(new Paragraph(
                $"Period: {meta.StartDate:dd-MMM-yyyy} to {meta.EndDate:dd-MMM-yyyy}",
                normalFont));
            doc.Add(new Paragraph(
                $"Generated: {meta.GeneratedDate:dd-MMM-yyyy HH:mm}",
                normalFont));
            doc.Add(new Paragraph(" "));

            // ===== Table =====
            PdfPTable table = new PdfPTable(3)
            {
                WidthPercentage = 100
            };
            table.SetWidths(new float[] { 15, 55, 30 });

            // Remove all table borders
            table.DefaultCell.Border = Rectangle.NO_BORDER;
            table.DefaultCell.Padding = 4;
            table.DefaultCell.PaddingBottom = 6;

            // Table header - create cells with NO_BORDER
            var refHeaderCell = new PdfPCell(new Phrase("Ref", headerFont))
            {
                Border = Rectangle.NO_BORDER,
                PaddingBottom = 8
            };

            var descHeaderCell = new PdfPCell(new Phrase("Description", headerFont))
            {
                Border = Rectangle.NO_BORDER,
                PaddingBottom = 8
            };

            var amountHeaderCell = new PdfPCell(new Phrase("Amount / %", headerFont))
            {
                HorizontalAlignment = Element.ALIGN_RIGHT,
                Border = Rectangle.NO_BORDER,
                PaddingBottom = 8
            };

            table.AddCell(refHeaderCell);
            table.AddCell(descHeaderCell);
            table.AddCell(amountHeaderCell);

            // Optional: Add a separator line after headers (if you want some visual separation)
            // If you want a clean look without any lines, remove this section
            var separatorCell = new PdfPCell(new Phrase(""))
            {
                Colspan = 3,
                Border = Rectangle.BOTTOM_BORDER,
                BorderWidthBottom = 0.5f,
                BorderColorBottom = BaseColor.LIGHT_GRAY,
                Padding = 0,
                FixedHeight = 1
            };
            table.AddCell(separatorCell);

            foreach (var row in rows)
            {
                var refCell = new PdfPCell(new Phrase(row.RefNo, normalFont))
                {
                    Border = Rectangle.NO_BORDER
                };

                var descCell = new PdfPCell(new Phrase(row.Description, normalFont))
                {
                    Border = Rectangle.NO_BORDER
                };

                var amountCell = new PdfPCell(
                    new Phrase(row.Amount.ToString("N2"), normalFont))
                {
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Border = Rectangle.NO_BORDER
                };

                table.AddCell(refCell);
                table.AddCell(descCell);
                table.AddCell(amountCell);
            }

            doc.Add(table);
            doc.Close();

            // ================= HTTP RESPONSE =================
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(ms.ToArray())
            };

            response.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/pdf");

            response.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"SASRA_Form5_Investment_{endDate:yyyyMMdd}.pdf"
                };

            return response;
        }

        [HttpGet]
        [Route("GenerateSasraForm7IncomeStatementPdf")]
        public HttpResponseMessage GenerateSasraForm7IncomeStatementPdf(
    DateTime startDate,
    DateTime endDate,
    string csCode = null)
        {
            var rows = new List<SasraForm7Row>();
            SasraForm7Meta meta = null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.sp_GenerateSASRAForm7_Report", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate;
                cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate;
                cmd.Parameters.Add("@CSCode", SqlDbType.NVarChar, 50).Value =
                    string.IsNullOrEmpty(csCode) ? DBNull.Value : (object)csCode;

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    // ===== Result Set 1: Form 7 Rows =====
                    while (reader.Read())
                    {
                        rows.Add(new SasraForm7Row
                        {
                            RefNo = reader["RefNo"].ToString(),
                            LineItem = reader["LineItem"].ToString(),
                            AmountInThousands = reader["AmountInThousands"].ToString(),
                            DisplayOrder = Convert.ToInt32(reader["DisplayOrder"]),
                            IsBold = Convert.ToBoolean(reader["IsBold"]),
                            IsSubTotal = Convert.ToBoolean(reader["IsSubTotal"]),
                            IsTotal = Convert.ToBoolean(reader["IsTotal"])
                        });
                    }

                    // ===== Result Set 2: Metadata =====
                    if (reader.NextResult() && reader.Read())
                    {
                        meta = new SasraForm7Meta
                        {
                            SaccoName = reader["SaccoName"].ToString(),
                            CSCode = reader["CSCode"].ToString(),
                            PeriodStartDate = Convert.ToDateTime(reader["PeriodStartDate"]),
                            PeriodEndDate = Convert.ToDateTime(reader["PeriodEndDate"]),
                            FinalNetIncome = Convert.ToDecimal(reader["FinalNetIncome"]),
                            TotalIncome = Convert.ToDecimal(reader["TotalIncome"]),
                            TotalExpense = Convert.ToDecimal(reader["TotalExpense"]),
                            GeneratedDate = Convert.ToDateTime(reader["GeneratedDate"])
                        };
                    }
                }
            }

            // ================= PDF GENERATION =================
            var ms = new MemoryStream();
            var doc = new Document(PageSize.A4.Rotate(), 25, 25, 25, 25); // Landscape for better fit
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            var subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
            var italicFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9);

            // ===== Header Section =====
            // Main title
            var titleParagraph = new Paragraph("FORM 7 SASRA/007", titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 5
            };
            doc.Add(titleParagraph);

            var subtitleParagraph = new Paragraph("STATEMENT OF COMPREHENSIVE INCOME", subtitleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 10
            };
            doc.Add(subtitleParagraph);

            // SACCO Information - Borderless table
            var saccoInfoTable = new PdfPTable(4)
            {
                WidthPercentage = 100,
                SpacingBefore = 5,
                SpacingAfter = 10
            };
            saccoInfoTable.SetWidths(new float[] { 30, 20, 30, 20 });
            saccoInfoTable.DefaultCell.Border = Rectangle.NO_BORDER; // Remove all borders

            saccoInfoTable.AddCell(CreateBorderlessCell("SACCO SOCIETY:", boldFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell(meta.SaccoName, normalFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell("CS NUMBER:", boldFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell(meta.CSCode ?? "N/A", normalFont, Element.ALIGN_LEFT));

            saccoInfoTable.AddCell(CreateBorderlessCell("FINANCIAL YEAR:", boldFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell($"{meta.PeriodStartDate:yyyy} - {meta.PeriodEndDate:yyyy}", normalFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell("REPORTING PERIOD:", boldFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell($"{meta.PeriodStartDate:dd-MMM-yyyy} to {meta.PeriodEndDate:dd-MMM-yyyy}", normalFont, Element.ALIGN_LEFT));

            saccoInfoTable.AddCell(CreateBorderlessCell("GENERATED:", boldFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell(meta.GeneratedDate.ToString("dd-MMM-yyyy HH:mm"), normalFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell("", boldFont, Element.ALIGN_LEFT));
            saccoInfoTable.AddCell(CreateBorderlessCell("", normalFont, Element.ALIGN_LEFT));

            doc.Add(saccoInfoTable);

            // ===== Income Statement Table =====
            var incomeTable = new PdfPTable(3)
            {
                WidthPercentage = 100,
                SpacingBefore = 10
            };
            incomeTable.SetWidths(new float[] { 15, 70, 15 });
            incomeTable.DefaultCell.Border = Rectangle.NO_BORDER; // Remove all borders

            // Table header - also borderless
            incomeTable.AddCell(CreateBorderlessCell("Ref No.", boldFont, Element.ALIGN_CENTER));
            incomeTable.AddCell(CreateBorderlessCell("", boldFont, Element.ALIGN_CENTER));
            incomeTable.AddCell(CreateBorderlessCell("KShs.'000'", boldFont, Element.ALIGN_CENTER));

            incomeTable.AddCell(CreateBorderlessCell("", boldFont, Element.ALIGN_CENTER));
            incomeTable.AddCell(CreateBorderlessCell("Year to Date", boldFont, Element.ALIGN_CENTER));
            incomeTable.AddCell(CreateBorderlessCell("", boldFont, Element.ALIGN_CENTER));

            // Add borderless rows
            foreach (var row in rows.Where(r => r.RefNo != "HEADER" && r.RefNo != "AUTH" && r.RefNo != "NOTE"))
            {
                // Determine font style based on row properties
                var cellFont = row.IsBold ? boldFont :
                              (row.IsSubTotal || row.IsTotal) ? boldFont : normalFont;

                // Reference Number cell
                incomeTable.AddCell(CreateBorderlessCell(row.RefNo, cellFont,
                    row.IsBold ? Element.ALIGN_LEFT : Element.ALIGN_LEFT));

                // Description cell
                incomeTable.AddCell(CreateBorderlessCell(row.LineItem, cellFont,
                    row.IsBold ? Element.ALIGN_LEFT : Element.ALIGN_LEFT));

                // Amount cell
                if (!string.IsNullOrEmpty(row.AmountInThousands))
                {
                    if (decimal.TryParse(row.AmountInThousands, out decimal amount))
                    {
                        incomeTable.AddCell(CreateBorderlessCell(amount.ToString("N2"), cellFont,
                            row.IsBold ? Element.ALIGN_RIGHT : Element.ALIGN_RIGHT));
                    }
                    else
                    {
                        incomeTable.AddCell(CreateBorderlessCell(row.AmountInThousands, cellFont,
                            row.IsBold ? Element.ALIGN_CENTER : Element.ALIGN_CENTER));
                    }
                }
                else
                {
                    incomeTable.AddCell(CreateBorderlessCell("", cellFont, Element.ALIGN_RIGHT));
                }
            }

            doc.Add(incomeTable);

            // ===== Summary Section =====
            doc.Add(new Paragraph(" "));

            var summaryTable = new PdfPTable(2)
            {
                WidthPercentage = 50,
                SpacingBefore = 20,
                HorizontalAlignment = Element.ALIGN_RIGHT
            };
            summaryTable.SetWidths(new float[] { 60, 40 });
            summaryTable.DefaultCell.Border = Rectangle.NO_BORDER; // Remove all borders

            summaryTable.AddCell(CreateBorderlessCell("Total Income:", boldFont, Element.ALIGN_LEFT));
            summaryTable.AddCell(CreateBorderlessCell((meta.TotalIncome / 1000).ToString("N2"), boldFont, Element.ALIGN_RIGHT));

            summaryTable.AddCell(CreateBorderlessCell("Total Expense:", boldFont, Element.ALIGN_LEFT));
            summaryTable.AddCell(CreateBorderlessCell((meta.TotalExpense / 1000).ToString("N2"), boldFont, Element.ALIGN_RIGHT));

            summaryTable.AddCell(CreateBorderlessCell("Net Income (After Tax & Donations):", boldFont, Element.ALIGN_LEFT));
            summaryTable.AddCell(CreateBorderlessCell((meta.FinalNetIncome / 1000).ToString("N2"), boldFont, Element.ALIGN_RIGHT));

            doc.Add(summaryTable);

            // ===== Notes and Authorization =====
            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph(" "));

            // Notes
            var notes = rows.Where(r => r.RefNo == "NOTE").ToList();
            foreach (var note in notes)
            {
                doc.Add(new Paragraph(note.LineItem, smallFont));
            }

            // Authorization section
            var authRows = rows.Where(r => r.RefNo == "AUTH").OrderBy(r => r.DisplayOrder).ToList();

            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph("AUTHORIZATION:", boldFont));

            foreach (var auth in authRows.Skip(1)) // Skip the "AUTHORIZATION:" header since we already added it
            {
                if (auth.DisplayOrder == 106) // Declaration text
                {
                    doc.Add(new Paragraph(auth.LineItem, italicFont) { SpacingBefore = 10 });
                }
                else
                {
                    var font = auth.DisplayOrder >= 107 ? normalFont : smallFont;
                    var alignment = auth.LineItem.Contains("sign") || auth.LineItem.Contains("Name") ?
                                  Element.ALIGN_LEFT : Element.ALIGN_LEFT;
                    doc.Add(new Paragraph(auth.LineItem, font) { Alignment = alignment });
                }
            }

            doc.Close();

            // ================= HTTP RESPONSE =================
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(ms.ToArray())
            };

            response.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/pdf");

            response.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"SASRA_Form7_IncomeStatement_{endDate:yyyyMMdd}.pdf"
                };

            return response;
        }

        // Helper method to create borderless table cells
        private PdfPCell CreateBorderlessCell(string text, Font font, int alignment)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = alignment,
                Padding = 6,
                PaddingBottom = 8,
                Border = Rectangle.NO_BORDER // This removes all border lines
            };

            return cell;
        }

        // Model classes
        public class SasraForm7Row
        {
            public string RefNo { get; set; }
            public string LineItem { get; set; }
            public string AmountInThousands { get; set; }
            public int DisplayOrder { get; set; }
            public bool IsBold { get; set; }
            public bool IsSubTotal { get; set; }
            public bool IsTotal { get; set; }
        }

        public class SasraForm7Meta
        {
            public string SaccoName { get; set; }
            public string CSCode { get; set; }
            public DateTime PeriodStartDate { get; set; }
            public DateTime PeriodEndDate { get; set; }
            public decimal FinalNetIncome { get; set; }
            public decimal TotalIncome { get; set; }
            public decimal TotalExpense { get; set; }
            public DateTime GeneratedDate { get; set; }
        }



        [HttpGet]
        [Route("GetLoanStatement/{loanCaseId}")]
        public async Task<HttpResponseMessage> GetLoanStatement(Guid loanCaseId, bool downloadPdf = false)
        {
            try
            {
                // Create a simple model for SQL results
                var customerData = new
                {
                    FirstName = "",
                    LastName = "",
                    Mobile = "",
                    Email = "",
                    Reference2 = "",
                    Reference3 = "",
                    BranchCode = 0,
                    CustomerSerialNumber = 0,
                    ProductCode = 0,
                    TargetProductCode = 0
                };

                var loanHeader = new
                {
                    LoanNumber = "",
                    LoanProductType = "",
                    AppliedLoanAmount = 0m,  // Changed from ApprovedLoanAmount
                    MonthlyRepayment = 0m,
                    CustomerAccountId = Guid.Empty,
                    MemberNumber = ""
                };

                var statementRows = new List<LoanStatementRow>();
                var summary = new LoanSummary();
                var recentTransactions = new List<RecentTransaction>(); // Not used in updated SP

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("sp_GenerateMemberLoanStatement", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = loanCaseId;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // Result Set 1: Loan Header
                            if (await reader.ReadAsync())
                            {
                                loanHeader = new
                                {
                                    LoanNumber = reader["LoanNumber"]?.ToString() ?? "",
                                    LoanProductType = reader["LoanProductType"]?.ToString() ?? "",
                                    AppliedLoanAmount = reader["AppliedLoanAmount"] != DBNull.Value ? Convert.ToDecimal(reader["AppliedLoanAmount"]) : 0m, // Changed
                                    MonthlyRepayment = reader["MonthlyRepayment"] != DBNull.Value ? Convert.ToDecimal(reader["MonthlyRepayment"]) : 0m,
                                    CustomerAccountId = reader["CustomerAccountId"] != DBNull.Value ? (Guid)reader["CustomerAccountId"] : Guid.Empty,
                                    MemberNumber = reader["MemberNumber"]?.ToString() ?? ""
                                };
                            }
                            else
                            {
                                // If no loan header found, return not found
                                var errorResponse = Request.CreateResponse(HttpStatusCode.NotFound);
                                errorResponse.Content = new StringContent(
                                    JsonConvert.SerializeObject(new ApiResponse<object>
                                    {
                                        Success = false,
                                        Message = "Loan case not found.",
                                        Data = null
                                    }),
                                    Encoding.UTF8,
                                    "application/json");
                                return errorResponse;
                            }

                            // Result Set 2: Statement rows
                            if (await reader.NextResultAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var row = new LoanStatementRow
                                    {
                                        PostingDate = reader["PostingDate"] != DBNull.Value ? Convert.ToDateTime(reader["PostingDate"]).ToString("yyyy-MM-dd") : "",
                                        TransactionType = reader["TransactionType"]?.ToString() ?? "",
                                        DocumentNo = reader["DocumentNo"]?.ToString() ?? "",
                                        Description = reader["Description"]?.ToString() ?? "",
                                        Debit = reader["Debit"] != DBNull.Value ? Convert.ToDecimal(reader["Debit"]) : 0m,
                                        Credit = reader["Credit"] != DBNull.Value ? Convert.ToDecimal(reader["Credit"]) : 0m,
                                        Balance = reader["RunningBalance"] != DBNull.Value ? Convert.ToDecimal(reader["RunningBalance"]) : 0m // Changed from Balance to RunningBalance
                                    };
                                    statementRows.Add(row);
                                }
                            }

                            // Result Set 3: Summary
                            if (await reader.NextResultAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    summary = new LoanSummary
                                    {
                                        TotalDisbursed = reader["TotalDisbursed"] != DBNull.Value ? Convert.ToDecimal(reader["TotalDisbursed"]) : 0m,
                                        TotalPrincipalRepaid = reader["TotalPrincipalPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalPrincipalPaid"]) : 0m, // Changed from TotalPrincipalRepaid
                                        TotalInterestPaid = reader["TotalInterestPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalInterestPaid"]) : 0m,
                                        TotalInterestAccrued = reader["TotalInterestCharged"] != DBNull.Value ? Convert.ToDecimal(reader["TotalInterestCharged"]) : 0m, // Changed from TotalInterestAccrued
                                        OutstandingLoanAmount = reader["OutstandingPrincipal"] != DBNull.Value ? Convert.ToDecimal(reader["OutstandingPrincipal"]) : 0m, // Changed from OutstandingLoanAmount
                                        OutstandingLoanInterest = reader["OutstandingInterest"] != DBNull.Value ? Convert.ToDecimal(reader["OutstandingInterest"]) : 0m, // Changed from OutstandingLoanInterest
                                        TotalOutstandingBalance = reader["TotalOutstandingBalance"] != DBNull.Value ? Convert.ToDecimal(reader["TotalOutstandingBalance"]) : 0m
                                    };
                                }
                            }

                            // Note: The updated stored procedure only returns 3 result sets, not 4
                            // So we don't need to read the 4th result set (RecentTransactions)
                        }
                    }

                    // Get customer details using CustomerAccountId from loan header
                    if (loanHeader.CustomerAccountId != Guid.Empty)
                    {
                        string customerQuery = @"
                SELECT TOP 1 
                    c.Individual_FirstName,
                    c.Individual_LastName,
                    c.Address_MobileLine,
                    c.Address_Email,
                    c.Reference2,
                    c.Reference3,
                    ISNULL(b.Code, 0) as BranchCode,
                    ISNULL(c.SerialNumber, 0) as CustomerSerialNumber,
                    ISNULL(ca.CustomerAccountType_ProductCode, 0) as ProductCode,
                    ISNULL(ca.CustomerAccountType_TargetProductCode, 0) as TargetProductCode
                FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_CustomerAccounts] ca
                INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Customers] c 
                    ON ca.CustomerId = c.Id
                LEFT JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Branches] b
                    ON ca.BranchId = b.Id
                WHERE ca.Id = @CustomerAccountId";

                        using (var cmd = new SqlCommand(customerQuery, connection))
                        {
                            cmd.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = loanHeader.CustomerAccountId;

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    customerData = new
                                    {
                                        FirstName = reader["Individual_FirstName"]?.ToString() ?? "",
                                        LastName = reader["Individual_LastName"]?.ToString() ?? "",
                                        Mobile = reader["Address_MobileLine"]?.ToString() ?? "",
                                        Email = reader["Address_Email"]?.ToString() ?? "",
                                        Reference2 = reader["Reference2"]?.ToString() ?? "",
                                        Reference3 = reader["Reference3"]?.ToString() ?? "",
                                        BranchCode = reader["BranchCode"] != DBNull.Value ? Convert.ToInt32(reader["BranchCode"]) : 0,
                                        CustomerSerialNumber = reader["CustomerSerialNumber"] != DBNull.Value ? Convert.ToInt32(reader["CustomerSerialNumber"]) : 0,
                                        ProductCode = reader["ProductCode"] != DBNull.Value ? Convert.ToInt32(reader["ProductCode"]) : 0,
                                        TargetProductCode = reader["TargetProductCode"] != DBNull.Value ? Convert.ToInt32(reader["TargetProductCode"]) : 0
                                    };
                                }
                            }
                        }
                    }
                }

                // Build the full account number
                string fullAccountNumber = string.Format("{0}-{1}-{2}-{3}",
                    customerData.BranchCode.ToString().PadLeft(3, '0'),
                    customerData.CustomerSerialNumber.ToString().PadLeft(7, '0'),
                    customerData.ProductCode.ToString().PadLeft(3, '0'),
                    customerData.TargetProductCode.ToString().PadLeft(3, '0'));

                // Create the result object
                var loanStatementResult = new
                {
                    Customer = new
                    {
                        FullName = $"{customerData.FirstName} {customerData.LastName}".Trim(),
                        AccountNumber = fullAccountNumber,
                        StaffNo = customerData.Reference2,
                        PFNumber = customerData.Reference3,
                        Mobile = customerData.Mobile,
                        Email = customerData.Email
                    },
                    LoanDetails = new
                    {
                        LoanNumber = loanHeader.LoanNumber,
                        LoanProductType = loanHeader.LoanProductType,
                        AppliedAmount = loanHeader.AppliedLoanAmount, // Changed from ApprovedAmount
                        MonthlyRepayment = loanHeader.MonthlyRepayment,
                        MemberNumber = loanHeader.MemberNumber
                    },
                    Statement = statementRows,
                    Summary = summary
                    // Note: RecentTransactions is not included as it's not returned by the updated SP
                };

                // If PDF download is requested
                if (downloadPdf)
                {
                    byte[] pdfBytes = GenerateLoanStatementPdf(customerData, loanHeader, fullAccountNumber,
                        statementRows, summary, recentTransactions);

                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(pdfBytes)
                    };

                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                    string customerName = $"{customerData.FirstName}_{customerData.LastName}".Replace(" ", "_");
                    response.Content.Headers.ContentDisposition =
                        new ContentDispositionHeaderValue("attachment")
                        {
                            FileName = $"LoanStatement_{loanHeader.LoanNumber}_{customerName}_{DateTime.Now:yyyyMMdd}.pdf"
                        };

                    return response;
                }
                else
                {
                    // Return JSON response
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(
                        JsonConvert.SerializeObject(new ApiResponse<object>
                        {
                            Success = true,
                            Message = statementRows.Count > 0 ?
                                $"Loan statement retrieved successfully. Outstanding Balance: {summary.TotalOutstandingBalance:C}" :
                                "No transactions found for this loan.",
                            Data = loanStatementResult
                        }),
                        Encoding.UTF8,
                        "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(
                    JsonConvert.SerializeObject(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "An error occurred while retrieving loan statement.",
                        Data = ex.Message + " | Inner: " + (ex.InnerException?.Message ?? "None")
                    }),
                    Encoding.UTF8,
                    "application/json");
                return response;
            }
        }

        private byte[] GenerateLoanStatementPdf(dynamic customerData, dynamic loanHeader, string fullAccountNumber,
    List<LoanStatementRow> statementRows, LoanSummary summary, List<RecentTransaction> recentTransactions)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Create document
                Document document = new Document(PageSize.A4, 30, 30, 50, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // ===== RUBANI SACCO COLOR THEME =====
                BaseColor SkyBlue = new BaseColor(0, 174, 239); // #00AEEF
                BaseColor Red = new BaseColor(255, 0, 0);       // #FF0000
                BaseColor DarkGray = new BaseColor(26, 26, 26); // #1A1A1A
                BaseColor LightGray = new BaseColor(217, 217, 217); // #D9D9D9
                BaseColor White = BaseColor.WHITE;

                // Fonts
                Font titleFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, DarkGray));
                Font headerFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, DarkGray));
                Font subHeaderFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, White));
                Font normalFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA, 9));
                Font boldFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9));
                Font smallFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA, 8, DarkGray));
                Font companyNameFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, SkyBlue));
                Font companyInfoFont = new Font(FontFactory.GetFont(FontFactory.HELVETICA, 10));

                // ===== CUSTOM HEADER WITH RUBANI SACCO LOGO =====
                try
                {
                    // Create a table with 1 column for left-aligned content
                    PdfPTable headerTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    // Row 1: Logo left-aligned at top
                    PdfPCell logoCell = new PdfPCell();
                    logoCell.Border = Rectangle.NO_BORDER;
                    logoCell.HorizontalAlignment = Element.ALIGN_LEFT; // Changed from CENTER to LEFT
                    logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    logoCell.PaddingBottom = 5f;

                    // Try to load logo from local path
                    string logoPath = @"C:\Users\ADMIN\source\repos\SwiftFinancialsNew\SwiftFinancialsSolution\TestApis\Assets\Images\rubani-logo.jpeg";
                    if (File.Exists(logoPath))
                    {
                        try
                        {
                            Image logo = Image.GetInstance(logoPath);
                            logo.ScaleToFit(100, 100); // Increased size for better visibility
                            logoCell.AddElement(logo);
                        }
                        catch (Exception)
                        {
                            // Fallback to text if image fails to load
                            logoCell.AddElement(new Paragraph("RUBANI SACCO", companyNameFont)
                            {
                                Alignment = Element.ALIGN_LEFT // Changed from CENTER to LEFT
                            });
                        }
                    }
                    else
                    {
                        // Use text if no logo file
                        logoCell.AddElement(new Paragraph("RUBANI SACCO", companyNameFont)
                        {
                            Alignment = Element.ALIGN_LEFT // Changed from CENTER to LEFT
                        });
                    }

                    headerTable.AddCell(logoCell);

                    // Row 2: Company Info - LEFT ALIGNED
                    PdfPCell infoCell = new PdfPCell();
                    infoCell.Border = Rectangle.NO_BORDER;
                    infoCell.HorizontalAlignment = Element.ALIGN_LEFT; // Changed from CENTER to LEFT
                    infoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    infoCell.PaddingTop = 5f;

                    // Company name - LEFT ALIGNED
                    var companyNamePara = new Paragraph("RUBANI SACCO", companyNameFont)
                    {
                        Alignment = Element.ALIGN_LEFT // Changed from CENTER to LEFT
                    };
                    infoCell.AddElement(companyNamePara);

                    // Address - LEFT ALIGNED
                    var address = new Paragraph("Rubani House, Off Airport North Embakasi", companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT // Changed from CENTER to LEFT
                    };
                    infoCell.AddElement(address);

                    // Email - LEFT ALIGNED
                    var email = new Paragraph("rubanisacco@gmail.com", companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT // Changed from CENTER to LEFT
                    };
                    infoCell.AddElement(email);

                    headerTable.AddCell(infoCell);

                    document.Add(headerTable);

                    // Add decorative line (Blue-Red-Blue) - Still centered for visual appeal
                    var lineTable = new PdfPTable(3)
                    {
                        WidthPercentage = 100,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        SpacingAfter = 10f
                    };
                    lineTable.SetWidths(new float[] { 33, 34, 33 });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = Red,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    document.Add(lineTable);
                }
                catch (Exception)
                {
                    // Fallback header with left alignment
                    var fallbackPara = new Paragraph("RUBANI SACCO\nRubani House, Off Airport North Embakasi\nrubanisacco@gmail.com",
                        companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT, // Changed from CENTER to LEFT
                        SpacingAfter = 15f
                    };
                    document.Add(fallbackPara);
                }

                // ===== STATEMENT TITLE =====
                // Keep title centered for emphasis
                document.Add(new Paragraph("LOAN STATEMENT", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10f
                });

                // ===== MEMBER INFORMATION SECTION =====
                string fullName = $"{customerData.FirstName} {customerData.LastName}".Trim().ToUpper();
                string staffNo = customerData.Reference2;
                string pfNumber = customerData.Reference3;

                // Member info - LEFT ALIGNED
                Paragraph memberInfo = new Paragraph();
                memberInfo.Alignment = Element.ALIGN_LEFT; // Changed from CENTER to LEFT

                // Add member info with proper formatting
                memberInfo.Add(new Chunk("Name: ", boldFont));
                memberInfo.Add(new Chunk(fullName, normalFont));
                memberInfo.Add(new Chunk("   MemberNo: ", boldFont));
                memberInfo.Add(new Chunk(staffNo ?? "N/A", normalFont));
                memberInfo.Add(Chunk.NEWLINE);

                memberInfo.Add(new Chunk("Account No: ", boldFont));
                memberInfo.Add(new Chunk(fullAccountNumber, normalFont));
                memberInfo.Add(new Chunk("   Company: ", boldFont));
                memberInfo.Add(new Chunk("RUBANI SACCO", normalFont));

                memberInfo.SpacingAfter = 15f;
                document.Add(memberInfo);

                // ===== LOAN DETAILS SECTION =====
                // Loan details - LEFT ALIGNED
                Paragraph loanDetails = new Paragraph();
                loanDetails.Alignment = Element.ALIGN_LEFT; // Changed from CENTER to LEFT

                loanDetails.Add(new Chunk("Loan Number: ", boldFont));
                loanDetails.Add(new Chunk(loanHeader.LoanNumber, normalFont));
                loanDetails.Add(new Chunk("   Loan Product: ", boldFont));
                loanDetails.Add(new Chunk(loanHeader.LoanProductType, normalFont));
                loanDetails.Add(Chunk.NEWLINE);

                loanDetails.Add(new Chunk("Applied Amount: ", boldFont));
                loanDetails.Add(new Chunk(loanHeader.AppliedLoanAmount.ToString("N2"), normalFont));
                loanDetails.Add(new Chunk("   Monthly Repayment: ", boldFont));
                loanDetails.Add(new Chunk(loanHeader.MonthlyRepayment.ToString("N2"), normalFont));

                loanDetails.SpacingAfter = 5f;
                document.Add(loanDetails);

                // ===== ADD TOTAL OUTSTANDING BALANCE BELOW MONTHLY REPAYMENT =====
                Paragraph outstandingBalance = new Paragraph();
                outstandingBalance.Alignment = Element.ALIGN_LEFT; // Changed from CENTER to LEFT

                outstandingBalance.Add(new Chunk("TOTAL OUTSTANDING BALANCE: ",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, DarkGray)));
                outstandingBalance.Add(new Chunk(summary.OutstandingLoanAmount.ToString("N2"),
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, Red)));

                outstandingBalance.SpacingAfter = 15f;
                document.Add(outstandingBalance);

                // ===== LOAN TRANSACTIONS SECTION =====
                if (statementRows != null && statementRows.Count > 0)
                {
                    // Section header - Keep centered for visual hierarchy
                    var sectionHeader = new Paragraph("LOAN TRANSACTIONS",
                        FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, White))
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5f
                    };

                    // Create a background for the header
                    PdfPTable headerBgTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    PdfPCell headerCell = new PdfPCell(sectionHeader)
                    {
                        BackgroundColor = DarkGray,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 8,
                        Border = Rectangle.NO_BORDER,
                        BorderWidthBottom = 2f,
                        BorderColorBottom = SkyBlue
                    };
                    headerBgTable.AddCell(headerCell);
                    document.Add(headerBgTable);

                    // Transactions table
                    PdfPTable transTable = new PdfPTable(6)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 5f
                    };
                    // Column widths: Date, Type, Description, Debit, Credit, RunningBalance
                    transTable.SetWidths(new float[] { 12, 12, 40, 18, 18, 20 });

                    // Table headers
                    string[] headers = { "Date", "Type", "Description", "Debit", "Credit", "Running Balance" };

                    // Add header cells with NO BORDERS
                    for (int i = 0; i < headers.Length; i++)
                    {
                        PdfPCell headerCellItem = new PdfPCell(new Phrase(headers[i], headerFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5,
                            // NO BORDERS - remove all border widths
                            BorderWidthTop = 0f,
                            BorderWidthBottom = 0f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f
                        };
                        transTable.AddCell(headerCellItem);
                    }

                    // Add transactions with NO BORDERS between rows
                    for (int rowIndex = 0; rowIndex < statementRows.Count; rowIndex++)
                    {
                        var row = statementRows[rowIndex];

                        // Date cell - NO BORDERS
                        PdfPCell dateCell = new PdfPCell(new Phrase(row.PostingDate, normalFont));
                        dateCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        dateCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        dateCell.BorderWidthTop = 0f;
                        dateCell.BorderWidthBottom = 0f;
                        dateCell.BorderWidthLeft = 0f;
                        dateCell.BorderWidthRight = 0f;
                        transTable.AddCell(dateCell);

                        // Type cell - NO BORDERS
                        PdfPCell typeCell = new PdfPCell(new Phrase(row.TransactionType, normalFont));
                        typeCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        typeCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        typeCell.BorderWidthTop = 0f;
                        typeCell.BorderWidthBottom = 0f;
                        typeCell.BorderWidthLeft = 0f;
                        typeCell.BorderWidthRight = 0f;
                        transTable.AddCell(typeCell);

                        // Description cell - NO BORDERS
                        PdfPCell descCell = new PdfPCell(new Phrase(row.Description ?? "", normalFont));
                        descCell.HorizontalAlignment = Element.ALIGN_LEFT;
                        descCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        descCell.BorderWidthTop = 0f;
                        descCell.BorderWidthBottom = 0f;
                        descCell.BorderWidthLeft = 0f;
                        descCell.BorderWidthRight = 0f;
                        transTable.AddCell(descCell);

                        // Debit cell - NO BORDERS
                        PdfPCell debitCell = new PdfPCell(new Phrase(row.Debit.ToString("N2"), normalFont));
                        debitCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        debitCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        debitCell.BorderWidthTop = 0f;
                        debitCell.BorderWidthBottom = 0f;
                        debitCell.BorderWidthLeft = 0f;
                        debitCell.BorderWidthRight = 0f;
                        transTable.AddCell(debitCell);

                        // Credit cell - NO BORDERS
                        PdfPCell creditCell = new PdfPCell(new Phrase(row.Credit.ToString("N2"), normalFont));
                        creditCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        creditCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        creditCell.BorderWidthTop = 0f;
                        creditCell.BorderWidthBottom = 0f;
                        creditCell.BorderWidthLeft = 0f;
                        creditCell.BorderWidthRight = 0f;
                        transTable.AddCell(creditCell);

                        // Running Balance cell - NO BORDERS
                        // Handle NULL values for Interest Paid rows
                        string balanceText = row.Balance == 0 && row.TransactionType == "Interest Paid"
                            ? ""  // Empty string for Interest Paid rows
                            : row.Balance.ToString("N2");

                        PdfPCell balanceCell = new PdfPCell(new Phrase(balanceText, normalFont));
                        balanceCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        balanceCell.Padding = 5f;
                        // NO BORDERS - remove all border widths
                        balanceCell.BorderWidthTop = 0f;
                        balanceCell.BorderWidthBottom = 0f;
                        balanceCell.BorderWidthLeft = 0f;
                        balanceCell.BorderWidthRight = 0f;
                        transTable.AddCell(balanceCell);
                    }

                    document.Add(transTable);
                }
                else
                {
                    var noTransPara = new Paragraph("No transactions found", normalFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 20f,
                        SpacingAfter = 20f
                    };
                    document.Add(noTransPara);
                }

                // ===== CUSTOM FOOTER =====
                document.Add(new Paragraph("\n"));
                var footerPara = new Paragraph(
                    $"Statement Date: {DateTime.Now:dd/MM/yyyy} | Printed on: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Page: 1",
                    smallFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 10f
                };
                document.Add(footerPara);

                // ===== FOOTER NOTES =====
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph("This is a system generated statement.", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });
                document.Add(new Paragraph("For any queries, contact: rubanisacco@gmail.com", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }

        // Helper method to add summary rows
        private void AddSummaryRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont)
        {
            PdfPCell labelCell = new PdfPCell(new Phrase(label, labelFont));
            labelCell.Border = Rectangle.NO_BORDER;
            labelCell.Padding = 5f;
            table.AddCell(labelCell);

            PdfPCell valueCell = new PdfPCell(new Phrase(value, valueFont));
            valueCell.Border = Rectangle.NO_BORDER;
            valueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            valueCell.Padding = 5f;
            table.AddCell(valueCell);
        }










        [HttpGet]
        [Route("GetMemberStatement/{customerId}")]
        public async Task<HttpResponseMessage> GetMemberStatement(
     Guid customerId,
     DateTime? startDate = null,
     DateTime? endDate = null,
     bool downloadPdf = false)
        {
            try
            {
                var memberStatement = new MemberStatementResult
                {
                    CustomerId = customerId,
                    StartDate = startDate,
                    EndDate = endDate
                };

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // ===== GET LOANS INFORMATION =====
                    var allLoanStatements = new List<LoanStatementResult>();

                    // ADD BACK THE LOAN STORED PROCEDURE CALL
                    using (var loanCommand = new SqlCommand("sp_GenerateMemberLoanStatement", connection))
                    {
                        loanCommand.CommandType = CommandType.StoredProcedure;
                        loanCommand.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                        if (startDate.HasValue)
                            loanCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate.Value.Date;
                        else
                            loanCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = DBNull.Value;

                        if (endDate.HasValue)
                            loanCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate.Value.Date;
                        else
                            loanCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = DBNull.Value;

                        using (var reader = await loanCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // First result set: Loan Header for current loan
                                var loanHeader = new
                                {
                                    LoanNumber = reader["LoanNumber"]?.ToString() ?? "",
                                    LoanProductType = reader["LoanProductType"]?.ToString() ?? "",
                                    AppliedLoanAmount = reader["AppliedLoanAmount"] != DBNull.Value ? Convert.ToDecimal(reader["AppliedLoanAmount"]) : 0m,
                                    MonthlyRepayment = reader["MonthlyRepayment"] != DBNull.Value ? Convert.ToDecimal(reader["MonthlyRepayment"]) : 0m,
                                    CustomerAccountId = reader["CustomerAccountId"] != DBNull.Value ? (Guid)reader["CustomerAccountId"] : Guid.Empty,
                                    MemberNumber = reader["MemberNumber"]?.ToString() ?? "",
                                    DisbursedDate = reader["DisbursedDate"] != DBNull.Value ?
                                        Convert.ToDateTime(reader["DisbursedDate"]).ToString("yyyy-MM-dd") : ""
                                };

                                var statementRows = new List<LoanStatementRow>();
                                var summary = new LoanSummary();
                                DateTime? statementStartDate = null;
                                DateTime? statementEndDate = null;

                                // Result Set 2: Statement rows
                                if (await reader.NextResultAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        var row = new LoanStatementRow
                                        {
                                            TransDate = reader["TransDate"] != DBNull.Value ?
                                                Convert.ToDateTime(reader["TransDate"]).ToString("yyyy-MM-dd") : "",
                                            OpeningBalance = reader["OpeningBalance"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["OpeningBalance"]) : 0m,
                                            Principle = reader["Principle"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["Principle"]) : 0m,
                                            Interest = reader["Interest"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["Interest"]) : 0m,
                                            Amount = reader["Amount"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["Amount"]) : 0m,
                                            LoanBalance = reader["LoanBalance"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["LoanBalance"]) : 0m,
                                            PostingDate = reader["TransDate"] != DBNull.Value ?
                                                Convert.ToDateTime(reader["TransDate"]).ToString("yyyy-MM-dd") : "",
                                            Balance = reader["LoanBalance"] != DBNull.Value ?
                                                Convert.ToDecimal(reader["LoanBalance"]) : 0m
                                        };
                                        statementRows.Add(row);
                                    }
                                }

                                // Result Set 3: Summary
                                if (await reader.NextResultAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        summary = new LoanSummary
                                        {
                                            TotalDisbursed = reader["TotalDisbursed"] != DBNull.Value ? Convert.ToDecimal(reader["TotalDisbursed"]) : 0m,
                                            TotalPrincipalRepaid = reader["TotalPrincipalPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalPrincipalPaid"]) : 0m,
                                            TotalInterestPaid = reader["TotalInterestPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalInterestPaid"]) : 0m,
                                            TotalInterestAccrued = reader["TotalInterestCharged"] != DBNull.Value ? Convert.ToDecimal(reader["TotalInterestCharged"]) : 0m,
                                            OutstandingLoanAmount = reader["OutstandingPrincipal"] != DBNull.Value ? Convert.ToDecimal(reader["OutstandingPrincipal"]) : 0m,
                                            OutstandingLoanInterest = reader["OutstandingInterest"] != DBNull.Value ? Convert.ToDecimal(reader["OutstandingInterest"]) : 0m,
                                            TotalOutstandingBalance = reader["TotalOutstandingBalance"] != DBNull.Value ? Convert.ToDecimal(reader["TotalOutstandingBalance"]) : 0m,
                                            OpeningBalance = reader["OpeningBalance"] != DBNull.Value ? Convert.ToDecimal(reader["OpeningBalance"]) : 0m
                                        };

                                        if (reader["StartDate"] != DBNull.Value)
                                            statementStartDate = Convert.ToDateTime(reader["StartDate"]);
                                        if (reader["EndDate"] != DBNull.Value)
                                            statementEndDate = Convert.ToDateTime(reader["EndDate"]);
                                    }
                                }

                                // Get customer details for this loan
                                var customerData = await GetCustomerDetails(connection, loanHeader.CustomerAccountId, customerId);

                                // Build the full account number
                                string fullAccountNumber = string.Format("{0}-{1}-{2}-{3}",
                                    customerData.BranchCode.ToString().PadLeft(3, '0'),
                                    customerData.CustomerSerialNumber.ToString().PadLeft(7, '0'),
                                    customerData.ProductCode.ToString().PadLeft(3, '0'),
                                    customerData.TargetProductCode.ToString().PadLeft(3, '0'));

                                // Create the loan statement result
                                var loanStatementResult = new LoanStatementResult
                                {
                                    LoanNumber = loanHeader.LoanNumber,
                                    Customer = new CustomerInfo
                                    {
                                        FullName = $"{customerData.FirstName} {customerData.LastName}".Trim(),
                                        AccountNumber = fullAccountNumber,
                                        StaffNo = customerData.Reference2,
                                        PFNumber = customerData.Reference3,
                                        Mobile = customerData.Mobile,
                                        Email = customerData.Email
                                    },
                                    LoanDetails = new LoanDetails
                                    {
                                        LoanNumber = loanHeader.LoanNumber,
                                        LoanProductType = loanHeader.LoanProductType,
                                        AppliedAmount = loanHeader.AppliedLoanAmount,
                                        MonthlyRepayment = loanHeader.MonthlyRepayment,
                                        MemberNumber = loanHeader.MemberNumber,
                                        DisbursedDate = loanHeader.DisbursedDate
                                    },
                                    Statement = statementRows,
                                    Summary = summary,
                                    StartDate = statementStartDate,
                                    EndDate = statementEndDate
                                };

                                allLoanStatements.Add(loanStatementResult);

                                // Move to next loan (if any)
                                await reader.NextResultAsync();
                            }
                        }
                    }

                    // ===== GET SHARES INFORMATION =====
                    var allSharesStatements = new List<SharesStatementResult>();

                    using (var sharesCommand = new SqlCommand("sp_GenerateAllSharesStatement", connection))
                    {
                        sharesCommand.CommandType = CommandType.StoredProcedure;
                        sharesCommand.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                        if (startDate.HasValue)
                            sharesCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate.Value.Date;
                        else
                            sharesCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = DBNull.Value;

                        if (endDate.HasValue)
                            sharesCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate.Value.Date;
                        else
                            sharesCommand.Parameters.Add("@EndDate", SqlDbType.Date).Value = DBNull.Value;

                        using (var reader = await sharesCommand.ExecuteReaderAsync())
                        {
                            // Dictionary to group transactions by account
                            var accountTransactions = new Dictionary<Guid, List<SharesTransaction>>();
                            var accountDetails = new Dictionary<Guid, (string ProductName, decimal TotalContribution)>();

                            // First result set: Detailed Statement
                            while (await reader.ReadAsync())
                            {
                                // Skip if it's a message result set
                                if (reader.FieldCount == 1 && reader.GetName(0) == "Message")
                                    continue;

                                var customerAccountId = reader["CustomerAccountId"] != DBNull.Value ?
                                    (Guid)reader["CustomerAccountId"] : Guid.Empty;

                                var transaction = new SharesTransaction
                                {
                                    TransactionDate = reader["Date"]?.ToString() ?? "",
                                    Description = reader["Description"]?.ToString() ?? "",
                                    DepositAmount = reader["Share Contribution"] != DBNull.Value ?
                                        Convert.ToDecimal(reader["Share Contribution"]) : 0m,
                                    WithdrawalAmount = 0m,
                                    RunningBalance = reader["Cumulative"] != DBNull.Value ?
                                        Convert.ToDecimal(reader["Cumulative"]) : 0m
                                };

                                if (!accountTransactions.ContainsKey(customerAccountId))
                                    accountTransactions[customerAccountId] = new List<SharesTransaction>();

                                accountTransactions[customerAccountId].Add(transaction);
                            }

                            // Second result set: Total Contribution Per Account
                            if (await reader.NextResultAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    // Skip if it's a message
                                    if (reader.FieldCount == 1 && reader.GetName(0) == "Message")
                                        continue;

                                    var customerAccountId = reader["CustomerAccountId"] != DBNull.Value ?
                                        (Guid)reader["CustomerAccountId"] : Guid.Empty;
                                    var productName = reader["ProductName"]?.ToString() ?? "";
                                    var totalContribution = reader["TotalContribution"] != DBNull.Value ?
                                        Convert.ToDecimal(reader["TotalContribution"]) : 0m;

                                    accountDetails[customerAccountId] = (productName, totalContribution);
                                }
                            }

                            // Create shares statement results for each account
                            foreach (var account in accountDetails)
                            {
                                var transactions = accountTransactions.ContainsKey(account.Key)
                                    ? accountTransactions[account.Key]
                                    : new List<SharesTransaction>();

                                // Calculate summary values from transactions
                                decimal openingBalance = 0m;
                                decimal totalDeposits = transactions.Sum(t => t.DepositAmount);
                                decimal closingBalance = transactions.Any()
                                    ? transactions.Last().RunningBalance
                                    : 0m;

                                // Use the TotalContribution from the SP for the summary
                                decimal actualTotalContribution = account.Value.TotalContribution;

                                // Create shares statement result
                                var sharesStatementResult = new SharesStatementResult
                                {
                                    StatementType = "SHARES/SAVINGS STATEMENT",
                                    ProductName = account.Value.ProductName,
                                    AccountType = "Share Account",
                                    ProductCode = 0,
                                    Period = $"{(startDate.HasValue ? startDate.Value.ToString("dd/MM/yyyy") : "Beginning")} to {(endDate.HasValue ? endDate.Value.ToString("dd/MM/yyyy") : DateTime.Now.ToString("dd/MM/yyyy"))}",
                                    OpeningBalance = openingBalance,
                                    TotalDeposits = totalDeposits,
                                    TotalWithdrawals = 0m,
                                    ClosingBalance = closingBalance,
                                    Transactions = transactions,
                                    Summary = new SharesAccountSummary
                                    {
                                        AccountName = account.Value.ProductName,
                                        AccountType = "Share Account",
                                        OpeningBalance = openingBalance,
                                        TotalDeposits = actualTotalContribution,
                                        TotalWithdrawals = 0m,
                                        ClosingBalance = closingBalance,
                                        NetMovement = actualTotalContribution
                                    }
                                };

                                allSharesStatements.Add(sharesStatementResult);
                            }
                        }
                    }

                    // Get customer info
                    var customerInfo = allLoanStatements.FirstOrDefault()?.Customer ??
                                     (allSharesStatements.Count > 0 ?
                                         new CustomerInfo
                                         {
                                             FullName = await GetCustomerName(connection, customerId),
                                             AccountNumber = "N/A",
                                             StaffNo = await GetCustomerStaffNo(connection, customerId),
                                             Mobile = await GetCustomerMobile(connection, customerId),
                                             Email = await GetCustomerEmail(connection, customerId),
                                             PFNumber = await GetCustomerPFNumber(connection, customerId)
                                         } : null);

                    if (customerInfo == null)
                    {
                        customerInfo = new CustomerInfo
                        {
                            FullName = await GetCustomerName(connection, customerId),
                            AccountNumber = "N/A",
                            StaffNo = await GetCustomerStaffNo(connection, customerId),
                            Mobile = await GetCustomerMobile(connection, customerId),
                            Email = await GetCustomerEmail(connection, customerId),
                            PFNumber = await GetCustomerPFNumber(connection, customerId)
                        };
                    }

                    // Populate member statement
                    memberStatement.Customer = customerInfo;
                    memberStatement.LoanStatements = allLoanStatements;
                    memberStatement.SharesStatements = allSharesStatements;

                    // Calculate totals
                    memberStatement.TotalLoanBalance = allLoanStatements.Sum(l => l.Summary?.TotalOutstandingBalance ?? 0);
                    memberStatement.TotalSharesBalance = allSharesStatements.Sum(s => s.ClosingBalance);
                    memberStatement.TotalAccounts = allLoanStatements.Count + allSharesStatements.Count;
                }

                // Check if we found any data
                if (memberStatement.LoanStatements.Count == 0 && memberStatement.SharesStatements.Count == 0)
                {
                    var errorResponse = Request.CreateResponse(HttpStatusCode.NotFound);
                    errorResponse.Content = new StringContent(
                        JsonConvert.SerializeObject(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "No loan or shares accounts found for this customer.",
                            Data = null
                        }),
                        Encoding.UTF8,
                        "application/json");
                    return errorResponse;
                }

                // If PDF download is requested
                if (downloadPdf)
                {
                    byte[] pdfBytes = GenerateMemberStatementPdf(memberStatement, startDate, endDate);

                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(pdfBytes)
                    };

                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                    string customerName = memberStatement.Customer?.FullName?.Replace(" ", "_") ?? "Customer";
                    string dateRange = "";
                    if (startDate.HasValue && endDate.HasValue)
                        dateRange = $"{startDate.Value:yyyyMMdd}_{endDate.Value:yyyyMMdd}";
                    else if (startDate.HasValue)
                        dateRange = $"from_{startDate.Value:yyyyMMdd}";
                    else if (endDate.HasValue)
                        dateRange = $"to_{endDate.Value:yyyyMMdd}";

                    response.Content.Headers.ContentDisposition =
                        new ContentDispositionHeaderValue("attachment")
                        {
                            FileName = $"MemberStatement_{customerName}_{dateRange}_{DateTime.Now:yyyyMMdd}.pdf"
                        };

                    return response;
                }
                else
                {
                    // Return JSON response
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(
                        JsonConvert.SerializeObject(new ApiResponse<object>
                        {
                            Success = true,
                            Message = $"Found {memberStatement.LoanStatements.Count} loan(s) and {memberStatement.SharesStatements.Count} shares/savings account(s).",
                            Data = memberStatement
                        }),
                        Encoding.UTF8,
                        "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(
                    JsonConvert.SerializeObject(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "An error occurred while retrieving member statement.",
                        Data = ex.Message + " | Inner: " + (ex.InnerException?.Message ?? "None")
                    }),
                    Encoding.UTF8,
                    "application/json");
                return response;
            }
        }
        private async Task<CustomerData> GetCustomerDetails(SqlConnection connection, Guid customerAccountId, Guid customerId)
        {
            string customerQuery = @"
        SELECT TOP 1
            c.Individual_FirstName,
            c.Individual_LastName,
            c.Address_MobileLine,
            c.Address_Email,
            c.Reference2,
            c.Reference3,
            ISNULL(b.Code, 0) as BranchCode,
            ISNULL(c.SerialNumber, 0) as CustomerSerialNumber,
            ISNULL(ca.CustomerAccountType_ProductCode, 0) as ProductCode,
            ISNULL(ca.CustomerAccountType_TargetProductCode, 0) as TargetProductCode
        FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_CustomerAccounts] ca
        INNER JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Customers] c
            ON ca.CustomerId = c.Id
        LEFT JOIN [SwiftFinancialsDB_Live].[dbo].[swiftFin_Branches] b
            ON ca.BranchId = b.Id
        WHERE (ca.Id = @CustomerAccountId OR c.Id = @CustomerId)
        ORDER BY ca.CreatedDate DESC";

            using (var cmd = new SqlCommand(customerQuery, connection))
            {
                cmd.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = customerAccountId;
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new CustomerData
                        {
                            FirstName = reader["Individual_FirstName"]?.ToString() ?? "",
                            LastName = reader["Individual_LastName"]?.ToString() ?? "",
                            Mobile = reader["Address_MobileLine"]?.ToString() ?? "",
                            Email = reader["Address_Email"]?.ToString() ?? "",
                            Reference2 = reader["Reference2"]?.ToString() ?? "",
                            Reference3 = reader["Reference3"]?.ToString() ?? "",
                            BranchCode = reader["BranchCode"] != DBNull.Value ? Convert.ToInt32(reader["BranchCode"]) : 0,
                            CustomerSerialNumber = reader["CustomerSerialNumber"] != DBNull.Value ? Convert.ToInt32(reader["CustomerSerialNumber"]) : 0,
                            ProductCode = reader["ProductCode"] != DBNull.Value ? Convert.ToInt32(reader["ProductCode"]) : 0,
                            TargetProductCode = reader["TargetProductCode"] != DBNull.Value ? Convert.ToInt32(reader["TargetProductCode"]) : 0
                        };
                    }
                }
            }

            // Return default if no customer found
            return new CustomerData();
        }

        private async Task<string> GetCustomerName(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT 
            CASE 
                WHEN Type = 1
                THEN CONCAT(ISNULL(Individual_FirstName, ''), ' ', ISNULL(Individual_LastName, ''))
                ELSE ISNULL(NonIndividual_Description, '')
            END AS FullName
        FROM swiftFin_Customers
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString()?.Trim() ?? "Customer";
            }
        }

        private async Task<string> GetCustomerStaffNo(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT Reference2 
        FROM swiftFin_Customers 
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                return (await cmd.ExecuteScalarAsync())?.ToString() ?? "N/A";
            }
        }

        private async Task<string> GetCustomerMobile(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT Address_MobileLine 
        FROM swiftFin_Customers 
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                return (await cmd.ExecuteScalarAsync())?.ToString() ?? "N/A";
            }
        }

        private async Task<string> GetCustomerEmail(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT Address_Email 
        FROM swiftFin_Customers 
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                return (await cmd.ExecuteScalarAsync())?.ToString() ?? "N/A";
            }
        }

        private async Task<string> GetCustomerPFNumber(SqlConnection connection, Guid customerId)
        {
            string query = @"
        SELECT Reference3 
        FROM swiftFin_Customers 
        WHERE Id = @CustomerId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                return (await cmd.ExecuteScalarAsync())?.ToString() ?? "N/A";
            }
        }

        public class MemberStatementResult
        {
            public Guid CustomerId { get; set; }
            public CustomerInfo Customer { get; set; }
            public List<LoanStatementResult> LoanStatements { get; set; } = new List<LoanStatementResult>();
            public List<SharesStatementResult> SharesStatements { get; set; } = new List<SharesStatementResult>();
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public decimal TotalLoanBalance { get; set; }
            public decimal TotalSharesBalance { get; set; }
            public int TotalAccounts { get; set; }
        }

        public class SharesStatementResult
        {
            public string StatementType { get; set; }
            public string ProductName { get; set; }
            public string AccountType { get; set; }
            public int ProductCode { get; set; }
            public string Period { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal TotalDeposits { get; set; }
            public decimal TotalWithdrawals { get; set; }
            public decimal ClosingBalance { get; set; }
            public List<SharesTransaction> Transactions { get; set; } = new List<SharesTransaction>();
            public SharesAccountSummary Summary { get; set; }
        }

        public class SharesTransaction
        {
            public string TransactionDate { get; set; }
            public string Description { get; set; }
            public decimal DepositAmount { get; set; }
            public decimal WithdrawalAmount { get; set; }
            public decimal RunningBalance { get; set; }
        }

        public class SharesAccountSummary
        {
            public string AccountName { get; set; }
            public string AccountType { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal TotalDeposits { get; set; }
            public decimal TotalWithdrawals { get; set; }
            public decimal ClosingBalance { get; set; }
            public decimal NetMovement { get; set; }
        }

        private byte[] GenerateMemberStatementPdf(MemberStatementResult memberStatement, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Create document with same margins as individual statements
                Document document = new Document(PageSize.A4, 30, 30, 50, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // ===== RUBANI SACCO COLOR THEME =====
                BaseColor SkyBlue = new BaseColor(0, 174, 239); // #00AEEF
                BaseColor Red = new BaseColor(255, 0, 0);       // #FF0000
                BaseColor Green = new BaseColor(0, 150, 0);     // #009600
                BaseColor DarkGray = new BaseColor(26, 26, 26); // #1A1A1A
                BaseColor LightGray = new BaseColor(217, 217, 217); // #D9D9D9
                BaseColor MediumGray = new BaseColor(128, 128, 128); // #808080 - Added for section headers
                BaseColor White = BaseColor.WHITE;

                // ===== FONTS - BOOK ANTIQUA FONT WITH SIZE 11 =====
                // Load Book Antiqua font (make sure it's available on your system)
                // You may need to install Book Antiqua font or use a different serif font
                string bookAntiquaFontName = "Book Antiqua";

                // Try to create Book Antiqua fonts, fallback to Times if not available
                Font titleFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 16f, Font.BOLD, DarkGray);
                Font normalFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                Font boldFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, DarkGray);
                Font smallFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.NORMAL, DarkGray);
                Font companyNameFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 14f, Font.BOLD, SkyBlue);
                Font companyInfoFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                Font sectionHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, SkyBlue);
                Font tableHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.BOLD, DarkGray);
                Font tableCellFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, Font.NORMAL, DarkGray);

                // Fallback fonts if Book Antiqua is not available
                try
                {
                    // Test if Book Antiqua is available
                    var testFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f);
                }
                catch
                {
                    // Fallback to Times New Roman if Book Antiqua is not available
                    bookAntiquaFontName = "Times New Roman";
                    titleFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 16f, Font.BOLD, DarkGray);
                    normalFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                    boldFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, DarkGray);
                    smallFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.NORMAL, DarkGray);
                    companyNameFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 14f, Font.BOLD, SkyBlue);
                    companyInfoFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.NORMAL, DarkGray);
                    sectionHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, SkyBlue);
                    tableHeaderFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 9f, Font.BOLD, DarkGray);
                    tableCellFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, Font.NORMAL, DarkGray);
                }

                // ===== CUSTOM HEADER WITH RUBANI SACCO LOGO =====
                try
                {
                    // Create a table with 1 column for left-aligned content
                    PdfPTable headerTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 8f
                    };

                    // Row 1: Logo left-aligned at top
                    PdfPCell logoCell = new PdfPCell();
                    logoCell.Border = Rectangle.NO_BORDER;
                    logoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    logoCell.PaddingBottom = 3f;

                    // Try to load logo from local path
                    string logoPath = @"C:\Users\ADMIN\source\repos\SwiftFinancialsNew\SwiftFinancialsSolution\TestApis\Assets\Images\rubani-logo.jpeg";
                    if (File.Exists(logoPath))
                    {
                        try
                        {
                            Image logo = Image.GetInstance(logoPath);
                            logo.ScaleToFit(100, 100);
                            logoCell.AddElement(logo);
                        }
                        catch (Exception)
                        {
                            logoCell.AddElement(new Paragraph("RUBANI SACCO", companyNameFont)
                            {
                                Alignment = Element.ALIGN_LEFT
                            });
                        }
                    }
                    else
                    {
                        logoCell.AddElement(new Paragraph("RUBANI SACCO", companyNameFont)
                        {
                            Alignment = Element.ALIGN_LEFT
                        });
                    }

                    headerTable.AddCell(logoCell);

                    // Row 2: Company Info - LEFT ALIGNED
                    PdfPCell infoCell = new PdfPCell();
                    infoCell.Border = Rectangle.NO_BORDER;
                    infoCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    infoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    infoCell.PaddingTop = 3f;

                    var companyNamePara = new Paragraph("RUBANI SACCO", companyNameFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(companyNamePara);

                    var address = new Paragraph("Rubani House, Off Airport North Embakasi", companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(address);

                    var email = new Paragraph("rubanisacco@gmail.com", companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT
                    };
                    infoCell.AddElement(email);

                    headerTable.AddCell(infoCell);
                    document.Add(headerTable);

                    // Add decorative line (Blue-Red-Blue)
                    var lineTable = new PdfPTable(3)
                    {
                        WidthPercentage = 100,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        SpacingAfter = 8f
                    };
                    lineTable.SetWidths(new float[] { 33, 34, 33 });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = Red,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    lineTable.AddCell(new PdfPCell()
                    {
                        BackgroundColor = SkyBlue,
                        FixedHeight = 2f,
                        Border = Rectangle.NO_BORDER
                    });

                    document.Add(lineTable);
                }
                catch (Exception)
                {
                    var fallbackPara = new Paragraph("RUBANI SACCO\nRubani House, Off Airport North Embakasi\nrubanisacco@gmail.com",
                        companyInfoFont)
                    {
                        Alignment = Element.ALIGN_LEFT,
                        SpacingAfter = 10f
                    };
                    document.Add(fallbackPara);
                }

                // ===== MEMBER DETAILED STATEMENT TITLE =====
                string titleText = "MEMBER DETAILED STATEMENT";
                if (startDate.HasValue || endDate.HasValue)
                {
                    titleText = "MEMBER DETAILED STATEMENT";
                    string dateRangeText = "";

                    if (startDate.HasValue && endDate.HasValue)
                        dateRangeText = $"{startDate.Value:dd/MM/yyyy} to {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        dateRangeText = $"From {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        dateRangeText = $"To {endDate.Value:dd/MM/yyyy}";

                    if (!string.IsNullOrEmpty(dateRangeText))
                    {
                        document.Add(new Paragraph(titleText, titleFont)
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 3f
                        });

                        document.Add(new Paragraph(dateRangeText,
                            FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, DarkGray))
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 8f
                        });
                    }
                    else
                    {
                        document.Add(new Paragraph(titleText, titleFont)
                        {
                            Alignment = Element.ALIGN_CENTER,
                            SpacingAfter = 8f
                        });
                    }
                }
                else
                {
                    document.Add(new Paragraph(titleText, titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 8f
                    });
                }

                // ===== MEMBER INFORMATION SECTION =====
                if (memberStatement.Customer != null)
                {
                    // Create a 2-column table for better alignment
                    PdfPTable memberInfoTable = new PdfPTable(2)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };
                    memberInfoTable.SetWidths(new float[] { 40, 60 });

                    // Left column: Name, Staff No, Mobile
                    Paragraph leftColumn = new Paragraph();
                    leftColumn.Add(new Chunk("Name: ", boldFont));
                    leftColumn.Add(new Chunk(memberStatement.Customer.FullName, normalFont));
                    leftColumn.Add(Chunk.NEWLINE);
                    leftColumn.Add(new Chunk("Staff No: ", boldFont));
                    leftColumn.Add(new Chunk(memberStatement.Customer.PFNumber ?? "N/A", normalFont));
                    leftColumn.Add(Chunk.NEWLINE);
                    leftColumn.Add(new Chunk("Mobile: ", boldFont));
                    leftColumn.Add(new Chunk(memberStatement.Customer.Mobile ?? "N/A", normalFont));

                    PdfPCell leftCell = new PdfPCell(leftColumn)
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        Padding = 3
                    };
                    memberInfoTable.AddCell(leftCell);

                    // Right column: MemberNo, Account No, Email
                    Paragraph rightColumn = new Paragraph();
                    rightColumn.Add(new Chunk("MemberNo: ", boldFont));
                    rightColumn.Add(new Chunk(memberStatement.Customer.StaffNo ?? "N/A", normalFont));
                    rightColumn.Add(Chunk.NEWLINE);
                    rightColumn.Add(new Chunk("Account No: ", boldFont));
                    rightColumn.Add(new Chunk(memberStatement.Customer.AccountNumber, normalFont));
                    rightColumn.Add(Chunk.NEWLINE);
                    rightColumn.Add(new Chunk("Email: ", boldFont));
                    rightColumn.Add(new Chunk(memberStatement.Customer.Email ?? "N/A", normalFont));

                    PdfPCell rightCell = new PdfPCell(rightColumn)
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        Padding = 3
                    };
                    memberInfoTable.AddCell(rightCell);

                    document.Add(memberInfoTable);
                }

                // ===== STATEMENT PERIOD SECTION =====
                if (startDate.HasValue || endDate.HasValue)
                {
                    string periodText = "Statement Period: ";
                    if (startDate.HasValue && endDate.HasValue)
                        periodText += $"{startDate.Value:dd/MM/yyyy} to {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        periodText += $"From {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        periodText += $"To {endDate.Value:dd/MM/yyyy}";

                    var periodPara = new Paragraph(periodText, boldFont)
                    {
                        Alignment = Element.ALIGN_LEFT,
                        SpacingAfter = 8f
                    };
                    document.Add(periodPara);
                }

                // In the PDF generation method, update the shares section:

                // ===== SHARES/SAVINGS DETAILED SECTION =====
                if (memberStatement.SharesStatements.Count > 0)
                {
                    var sharesHeader = new Paragraph("SHARES/SAVINGS STATEMENT",
                        FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, White))
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 3f
                    };

                    PdfPTable sharesHeaderTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    PdfPCell sharesHeaderCell = new PdfPCell(sharesHeader)
                    {
                        BackgroundColor = MediumGray,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 6,
                        Border = Rectangle.NO_BORDER,
                        BorderWidthBottom = 2f,
                        BorderColorBottom = SkyBlue
                    };
                    sharesHeaderTable.AddCell(sharesHeaderCell);
                    document.Add(sharesHeaderTable);

                    int sharesCounter = 1;
                    foreach (var shares in memberStatement.SharesStatements)
                    {
                        // Account Header - Show product name only
                        var accountHeaderPara = new Paragraph($"ACCOUNT #{sharesCounter}: {shares.ProductName}", sectionHeaderFont)
                        {
                            Alignment = Element.ALIGN_LEFT,
                            SpacingAfter = 8f
                        };
                        document.Add(accountHeaderPara);

                        // Transactions Section - Show ALL transactions (no limit)
                        if (shares.Transactions.Count > 0)
                        {
                            // Use all transactions, not just first 5
                            var allTransactions = shares.Transactions;

                            PdfPTable transTable = new PdfPTable(5)
                            {
                                WidthPercentage = 100,
                                SpacingAfter = 5f
                            };
                            transTable.SetWidths(new float[] { 20, 35, 15, 15, 15 });

                            // Table headers - using tableHeaderFont
                            PdfPCell dateHeaderCell = new PdfPCell(new Phrase("Date", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 1f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray,
                                BorderColorLeft = DarkGray
                            };
                            transTable.AddCell(dateHeaderCell);

                            PdfPCell descHeaderCell = new PdfPCell(new Phrase("Description", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(descHeaderCell);

                            PdfPCell depositHeaderCell = new PdfPCell(new Phrase("Deposit", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(depositHeaderCell);

                            // Withdrawal header - kept for consistency but will be empty for shares
                            PdfPCell withdrawalHeaderCell = new PdfPCell(new Phrase("Withdrawal", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(withdrawalHeaderCell);

                            PdfPCell balanceHeaderCell = new PdfPCell(new Phrase("Balance", tableHeaderFont))
                            {
                                BackgroundColor = LightGray,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 4,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthTop = 1f,
                                BorderWidthBottom = 1f,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 1f,
                                BorderColorTop = DarkGray,
                                BorderColorBottom = DarkGray,
                                BorderColorRight = DarkGray
                            };
                            transTable.AddCell(balanceHeaderCell);

                            // Add ALL transactions - using tableCellFont
                            for (int i = 0; i < allTransactions.Count; i++)
                            {
                                var transaction = allTransactions[i];
                                bool isLastRow = (i == allTransactions.Count - 1);

                                PdfPCell dateCell = new PdfPCell(new Phrase(transaction.TransactionDate, tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_CENTER,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 1f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorLeft = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(dateCell);

                                PdfPCell descCell = new PdfPCell(new Phrase(transaction.Description ?? "", tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_LEFT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(descCell);

                                PdfPCell depositCell = new PdfPCell(new Phrase(transaction.DepositAmount.ToString("N2"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(depositCell);

                                // Withdrawal cell - always empty for shares
                                PdfPCell withdrawalCell = new PdfPCell(new Phrase("", tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(withdrawalCell);

                                PdfPCell balanceCell = new PdfPCell(new Phrase(transaction.RunningBalance.ToString("N2"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 1f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorRight = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(balanceCell);
                            }

                            document.Add(transTable);

                        }

                        // Account Summary - Show net movement only
                        if (shares.Summary != null)
                        {
                            var summaryPara = new Paragraph();
                            summaryPara.Alignment = Element.ALIGN_LEFT;

                            string accountTypeName = shares.ProductName ?? shares.AccountType;
                            string totalLabel = accountTypeName.Contains("Share", StringComparison.OrdinalIgnoreCase) ?
                                               "Total Share Capital" : $"Total {accountTypeName}";

                            summaryPara.Add(new Chunk($"{totalLabel}: ", boldFont));
                            summaryPara.Add(new Chunk(shares.Summary.NetMovement.ToString("N2"),
                                FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, shares.Summary.NetMovement >= 0 ? Green : Red)));

                            summaryPara.SpacingAfter = 8f;
                            document.Add(summaryPara);
                        }

                        sharesCounter++;
                    }
                }
                // ===== LOANS DETAILED SECTION =====
                if (memberStatement.LoanStatements.Count > 0)
                {
                    var loansHeader = new Paragraph("LOANS STATEMENT",
                        FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12f, Font.BOLD, White))
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 3f
                    };

                    PdfPTable loansHeaderTable = new PdfPTable(1)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 10f
                    };

                    PdfPCell loansHeaderCell = new PdfPCell(loansHeader)
                    {
                        BackgroundColor = MediumGray,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 6,
                        Border = Rectangle.NO_BORDER,
                        BorderWidthBottom = 2f,
                        BorderColorBottom = SkyBlue
                    };
                    loansHeaderTable.AddCell(loansHeaderCell);
                    document.Add(loansHeaderTable);

                    int loanCounter = 1;
                    foreach (var loan in memberStatement.LoanStatements)
                    {
                        // Loan Header
                        var loanHeaderPara = new Paragraph($"LOAN #{loanCounter}: {loan.LoanNumber}", sectionHeaderFont)
                        {
                            Alignment = Element.ALIGN_LEFT,
                            SpacingAfter = 3f
                        };
                        document.Add(loanHeaderPara);

                        // Loan Details - 3 column layout with Disbursed Date centered
                        PdfPTable loanDetailsTable = new PdfPTable(3)
                        {
                            WidthPercentage = 100,
                            SpacingAfter = 3f  // Reduced from 5f
                        };
                        loanDetailsTable.SetWidths(new float[] { 33, 34, 33 });

                        // Format the disbursed date
                        string mainDisbursedDateDisplay = "N/A";
                        if (!string.IsNullOrEmpty(loan.LoanDetails.DisbursedDate))
                        {
                            DateTime disbursedDate;
                            if (DateTime.TryParse(loan.LoanDetails.DisbursedDate, out disbursedDate))
                            {
                                mainDisbursedDateDisplay = disbursedDate.ToString("dd/MM/yyyy");
                            }
                            else
                            {
                                mainDisbursedDateDisplay = loan.LoanDetails.DisbursedDate;
                            }
                        }

                        // Column 1: Loan Product - Left aligned
                        Paragraph col1Details = new Paragraph();
                        col1Details.Add(new Chunk("Loan Product: ", boldFont));
                        col1Details.Add(new Chunk(loan.LoanDetails.LoanProductType, normalFont));

                        PdfPCell col1Cell = new PdfPCell(col1Details)
                        {
                            Border = Rectangle.NO_BORDER,
                            Padding = 3,
                            HorizontalAlignment = Element.ALIGN_LEFT,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };
                        loanDetailsTable.AddCell(col1Cell);

                        // Column 2: Disbursed Date - CENTERED
                        Paragraph col2Details = new Paragraph();
                        col2Details.Add(new Chunk("Disbursed Date: ", boldFont));
                        col2Details.Add(new Chunk(mainDisbursedDateDisplay, normalFont));

                        PdfPCell col2Cell = new PdfPCell(col2Details)
                        {
                            Border = Rectangle.NO_BORDER,
                            Padding = 3,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };
                        loanDetailsTable.AddCell(col2Cell);

                        // Column 3: Issued Amount - Right aligned
                        Paragraph col3Details = new Paragraph();
                        col3Details.Add(new Chunk("Issued Amount: ", boldFont));
                        col3Details.Add(new Chunk(loan.LoanDetails.AppliedAmount.ToString("N0"), normalFont));

                        PdfPCell col3Cell = new PdfPCell(col3Details)
                        {
                            Border = Rectangle.NO_BORDER,
                            Padding = 3,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };
                        loanDetailsTable.AddCell(col3Cell);

                        document.Add(loanDetailsTable);

                        // CURRENT OUTSTANDING - Single line, left and right aligned
                        if (loan.Summary != null)
                        {
                            PdfPTable outstandingTable = new PdfPTable(2)
                            {
                                WidthPercentage = 100,
                                SpacingAfter = 5f  // Reduced from 10f
                            };
                            outstandingTable.SetWidths(new float[] { 50, 50 });

                            // Left cell: "CURRENT OUTSTANDING:" label
                            PdfPCell labelCell = new PdfPCell(new Paragraph("CURRENT OUTSTANDING:",
                                FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, DarkGray)))
                            {
                                Border = Rectangle.NO_BORDER,
                                HorizontalAlignment = Element.ALIGN_LEFT,
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 3,
                                PaddingTop = 0
                            };
                            outstandingTable.AddCell(labelCell);

                            // Right cell: Value
                            PdfPCell valueCell = new PdfPCell(new Paragraph(loan.Summary.TotalOutstandingBalance.ToString("N0"),
                                FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 11f, Font.BOLD, Red)))
                            {
                                Border = Rectangle.NO_BORDER,
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                VerticalAlignment = Element.ALIGN_MIDDLE,
                                Padding = 3,
                                PaddingTop = 0
                            };
                            outstandingTable.AddCell(valueCell);

                            document.Add(outstandingTable);
                        }

                        // Transaction Table
                        PdfPTable transTable = new PdfPTable(6)
                        {
                            WidthPercentage = 100,
                            SpacingAfter = 5f  // Reduced from 10f
                        };
                        transTable.SetWidths(new float[] { 15, 18, 15, 15, 15, 22 });

                        // Table headers - using tableHeaderFont
                        PdfPCell dateHeaderCell = new PdfPCell(new Phrase("Date", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 1f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray,
                            BorderColorLeft = DarkGray
                        };
                        transTable.AddCell(dateHeaderCell);

                        PdfPCell openingBalanceHeaderCell = new PdfPCell(new Phrase("Opening Balance", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(openingBalanceHeaderCell);

                        PdfPCell principleHeaderCell = new PdfPCell(new Phrase("Principle", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(principleHeaderCell);

                        PdfPCell interestHeaderCell = new PdfPCell(new Phrase("Interest", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(interestHeaderCell);

                        PdfPCell amountHeaderCell = new PdfPCell(new Phrase("Amount", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 0f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray
                        };
                        transTable.AddCell(amountHeaderCell);

                        PdfPCell loanBalanceHeaderCell = new PdfPCell(new Phrase("Loan Balance", tableHeaderFont))
                        {
                            BackgroundColor = LightGray,
                            HorizontalAlignment = Element.ALIGN_RIGHT,
                            Padding = 4,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            BorderWidthTop = 1f,
                            BorderWidthBottom = 1f,
                            BorderWidthLeft = 0f,
                            BorderWidthRight = 1f,
                            BorderColorTop = DarkGray,
                            BorderColorBottom = DarkGray,
                            BorderColorRight = DarkGray
                        };
                        transTable.AddCell(loanBalanceHeaderCell);

                        // Add transactions if they exist - using tableCellFont
                        if (loan.Statement != null && loan.Statement.Count > 0)
                        {
                            for (int i = 0; i < loan.Statement.Count; i++)
                            {
                                var row = loan.Statement[i];
                                bool isLastRow = (i == loan.Statement.Count - 1);

                                // Format date
                                string transDate = "";
                                if (!string.IsNullOrEmpty(row.TransDate))
                                {
                                    DateTime date;
                                    if (DateTime.TryParse(row.TransDate, out date))
                                        transDate = date.ToString("dd/MM/yyyy");
                                    else
                                        transDate = row.TransDate;
                                }

                                // Date cell
                                PdfPCell dateCell = new PdfPCell(new Phrase(transDate, tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_CENTER,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 1f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorLeft = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(dateCell);

                                // Opening Balance cell
                                PdfPCell openingBalanceCell = new PdfPCell(new Phrase(row.OpeningBalance.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(openingBalanceCell);

                                // Principle cell
                                PdfPCell principleCell = new PdfPCell(new Phrase(row.Principle.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(principleCell);

                                // Interest cell
                                PdfPCell interestCell = new PdfPCell(new Phrase(row.Interest.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(interestCell);

                                // Amount cell - with color coding
                                Font amountCellFont = FontFactory.GetFont(bookAntiquaFontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, Font.BOLD, row.Amount > 0 ? Green : DarkGray);
                                PdfPCell amountCell = new PdfPCell(new Phrase(row.Amount.ToString("N0"), amountCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 0f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(amountCell);

                                // Loan Balance cell
                                PdfPCell balanceCell = new PdfPCell(new Phrase(row.LoanBalance.ToString("N0"), tableCellFont))
                                {
                                    HorizontalAlignment = Element.ALIGN_RIGHT,
                                    Padding = 3,
                                    PaddingTop = 6,
                                    PaddingBottom = 6,
                                    BorderWidthLeft = 0f,
                                    BorderWidthRight = 1f,
                                    BorderWidthTop = 0f,
                                    BorderWidthBottom = isLastRow ? 1f : 0f,
                                    BorderColorRight = DarkGray,
                                    BorderColorBottom = isLastRow ? DarkGray : BaseColor.WHITE
                                };
                                transTable.AddCell(balanceCell);
                            }

                            if (loan.Statement.Count > 10)
                            {
                                var moreTransPara = new Paragraph($"... and {loan.Statement.Count - 10} more transactions", smallFont)
                                {
                                    Alignment = Element.ALIGN_CENTER,
                                    SpacingBefore = 3f,
                                    SpacingAfter = 3f  // Reduced from 8f
                                };
                                document.Add(moreTransPara);
                            }
                        }
                        else
                        {
                            // No transactions - show initial disbursement
                            decimal issuedAmount = loan.LoanDetails.AppliedAmount;

                            // Use a different variable name for the "else" block
                            string noTransactionsDateDisplay = "N/A";
                            if (!string.IsNullOrEmpty(loan.LoanDetails.DisbursedDate))
                            {
                                DateTime noTransactionsDate;
                                if (DateTime.TryParse(loan.LoanDetails.DisbursedDate, out noTransactionsDate))
                                {
                                    noTransactionsDateDisplay = noTransactionsDate.ToString("dd/MM/yyyy");
                                }
                                else
                                {
                                    noTransactionsDateDisplay = loan.LoanDetails.DisbursedDate;
                                }
                            }

                            // Date cell
                            PdfPCell dateCell = new PdfPCell(new Phrase(noTransactionsDateDisplay, tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 1f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorLeft = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(dateCell);

                            // Opening Balance cell
                            PdfPCell openingBalanceCell = new PdfPCell(new Phrase(issuedAmount.ToString("N0"), tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(openingBalanceCell);

                            // Principle cell
                            PdfPCell principleCell = new PdfPCell(new Phrase("0", tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(principleCell);

                            // Interest cell
                            PdfPCell interestCell = new PdfPCell(new Phrase("0", tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(interestCell);

                            // Amount cell
                            PdfPCell amountCell = new PdfPCell(new Phrase("0", tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 0f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(amountCell);

                            // Loan Balance cell
                            PdfPCell balanceCell = new PdfPCell(new Phrase(issuedAmount.ToString("N0"), tableCellFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Padding = 3,
                                PaddingTop = 6,
                                PaddingBottom = 6,
                                BorderWidthLeft = 0f,
                                BorderWidthRight = 1f,
                                BorderWidthTop = 0f,
                                BorderWidthBottom = 1f,
                                BorderColorRight = DarkGray,
                                BorderColorBottom = DarkGray
                            };
                            transTable.AddCell(balanceCell);
                        }

                        document.Add(transTable);

                        // Loan Summary - Moved closer to table
                        //if (loan.Summary != null)
                        //{
                        //    // Removed document.Add(new Paragraph("\n")); to eliminate blank line

                        //    var summaryPara = new Paragraph();
                        //    summaryPara.Alignment = Element.ALIGN_LEFT;

                        //    string periodText = "";
                        //    if (startDate.HasValue && endDate.HasValue)
                        //        periodText = $"for period {startDate.Value:dd/MM/yyyy} - {endDate.Value:dd/MM/yyyy}";
                        //    else if (startDate.HasValue)
                        //        periodText = $"from {startDate.Value:dd/MM/yyyy}";
                        //    else if (endDate.HasValue)
                        //        periodText = $"up to {endDate.Value:dd/MM/yyyy}";

                        //    if (!string.IsNullOrEmpty(periodText))
                        //    {
                        //        summaryPara.Add(new Chunk($"Summary {periodText}: ", boldFont));
                        //    }
                        //    else
                        //    {
                        //        summaryPara.Add(new Chunk("Loan Summary: ", boldFont));
                        //    }

                        //    summaryPara.Add(new Chunk($"Principal Paid: {loan.Summary.TotalPrincipalRepaid:N0}", normalFont));
                        //    summaryPara.Add(new Chunk(" | ", normalFont));
                        //    summaryPara.Add(new Chunk($"Interest Accrued: {loan.Summary.TotalInterestAccrued:N0}", normalFont));
                        //    summaryPara.Add(new Chunk(" | ", normalFont));
                        //    summaryPara.Add(new Chunk($"Interest Paid: {loan.Summary.TotalInterestPaid:N0}", normalFont));

                        //    summaryPara.SpacingAfter = 8f;  // Reduced from 15f
                        //    document.Add(summaryPara);
                        //}

                        loanCounter++;
                    }
                }

                // ===== FOOTER =====
                document.Add(new Paragraph("\n"));
                string footerText = $"Statement Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

                if (startDate.HasValue || endDate.HasValue)
                {
                    string dateRangeInfo = "";
                    if (startDate.HasValue && endDate.HasValue)
                        dateRangeInfo = $" | Period: {startDate.Value:dd/MM/yyyy} - {endDate.Value:dd/MM/yyyy}";
                    else if (startDate.HasValue)
                        dateRangeInfo = $" | From: {startDate.Value:dd/MM/yyyy}";
                    else if (endDate.HasValue)
                        dateRangeInfo = $" | Up to: {endDate.Value:dd/MM/yyyy}";

                    footerText += dateRangeInfo;
                }

                footerText += $" | Total Accounts: {memberStatement.TotalAccounts}";

                var footerPara = new Paragraph(footerText, smallFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 8f
                };
                document.Add(footerPara);

                // ===== FOOTER NOTES =====
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph("This is a system generated detailed statement for all member accounts.", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });
                document.Add(new Paragraph("For any queries, contact: rubanisacco@gmail.com", smallFont)
                {
                    Alignment = Element.ALIGN_CENTER
                });

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }

        // Helper method to create cells with NO BORDERS (not used anymore, but kept for backward compatibility)
        private PdfPCell CreateStyledCell(string text, Font font, int alignment = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text ?? "", font));
            cell.HorizontalAlignment = alignment;
            cell.Padding = 4f;

            // REMOVE ALL BORDERS
            cell.BorderWidthLeft = 0f;
            cell.BorderWidthRight = 0f;
            cell.BorderWidthTop = 0f;
            cell.BorderWidthBottom = 0f;

            return cell;
        }


        public class LoanPaymentRow
        {
            public string TransactionDate { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal Principal { get; set; }
            public decimal Interest { get; set; }
            public decimal Amount { get; set; }
            public decimal LoanBalance { get; set; }
            public string TransactionType { get; set; }
            public string Description { get; set; }
        }

        public class LoanDetails
        {
            public string LoanNumber { get; set; }
            public string LoanProductType { get; set; }
            public decimal AppliedAmount { get; set; }
            public decimal MonthlyRepayment { get; set; }
            public string MemberNumber { get; set; }
            public string DisbursedDate { get; set; }
        }

        public class LoanStatementResult
        {
            public string LoanNumber { get; set; }
            public CustomerInfo Customer { get; set; }
            public LoanDetails LoanDetails { get; set; }
            public List<LoanStatementRow> Statement { get; set; }
            public LoanSummary Summary { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }
        public class CustomerData
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Mobile { get; set; }
            public string Email { get; set; }
            public string Reference2 { get; set; }
            public string Reference3 { get; set; }
            public int BranchCode { get; set; }
            public int CustomerSerialNumber { get; set; }
            public int ProductCode { get; set; }
            public int TargetProductCode { get; set; }
        }

        public class CustomerInfo
        {
            public string FullName { get; set; }
            public string AccountNumber { get; set; }
            public string StaffNo { get; set; }
            public string PFNumber { get; set; }
            public string Mobile { get; set; }
            public string Email { get; set; }
        }



        // Helper method to create cells WITH BORDERS for loan table alignment
        private PdfPCell CreateTableCell(string text, Font font, int alignment = Element.ALIGN_LEFT, BaseColor borderColor = null, bool showBorders = false)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text ?? "", font));
            cell.HorizontalAlignment = alignment;
            cell.Padding = 5f;
            cell.PaddingTop = 8f;
            cell.PaddingBottom = 8f;

            if (showBorders && borderColor != null)
            {
                // Add borders for better alignment
                cell.BorderWidthLeft = 1f;
                cell.BorderWidthRight = 1f;
                cell.BorderWidthTop = 1f;
                cell.BorderWidthBottom = 1f;
                cell.BorderColor = borderColor;
            }
            else
            {
                // No borders for shares table
                cell.BorderWidthLeft = 0f;
                cell.BorderWidthRight = 0f;
                cell.BorderWidthTop = 0f;
                cell.BorderWidthBottom = 0f;
            }

            return cell;
        }




        #endregion

        public class CustomerShareStatementRow
        {
            public string Date { get; set; }
            public decimal ShareContribution { get; set; }
            public decimal Cumulative { get; set; }
            public string Description { get; set; }
        }

        public class CustomerShareStatementResult
        {
            public List<CustomerShareStatementRow> Statement { get; set; }
            public decimal TotalContribution { get; set; }
        }

        public class LoanStatementRow
        {
            public string PostingDate { get; set; }
            public string TransactionType { get; set; }
            public string DocumentNo { get; set; }
            public string Description { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public decimal Balance { get; set; }
            public decimal Amount { get; set; }

            public string TransDate { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal Principle { get; set; }
            public decimal Interest { get; set; }
            public decimal LoanBalance { get; set; }
        }

        public class LoanSummary
        {
            public decimal TotalDisbursed { get; set; }
            public decimal TotalPrincipalRepaid { get; set; }
            public decimal TotalInterestPaid { get; set; }
            public decimal TotalInterestAccrued { get; set; }
            public decimal OutstandingLoanAmount { get; set; }
            public decimal OutstandingLoanInterest { get; set; }
            public decimal TotalOutstandingBalance { get; set; }
            public decimal OpeningBalance { get; set; }

        }

        public class RecentTransaction
        {
            public string PostingDate { get; set; }
            public string TransactionType { get; set; }
            public string DocumentNo { get; set; }
            public string Description { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public decimal Balance { get; set; }
        }


        public class CreditBatchImportRequest
        {
            public string FileName { get; set; }
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
