using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TestApis.Models;
using TestApis.Services;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace TestApis.Controllers
{
    [RoutePrefix("api/reporting")]
    public class ReportingController : ApiController
    {
        private readonly MasterController master;
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        private readonly CustomerStatementService _statementService = new CustomerStatementService();
        private readonly CustomerService _customerService = new CustomerService();
        public ReportingController()
        {
            master = new MasterController();
        }

        public class SASRAForm6Line
        {
            public string ReportSection { get; set; } 
            public string LineItem { get; set; }
            public decimal? Amount { get; set; } 
            public int DisplayOrder { get; set; }
        }

        public class SASRAForm6Metadata
        {
            public string SaccoName { get; set; } 
            public DateTime FiscalStartDate { get; set; }
            public DateTime PeriodEndingDate { get; set; }
            public DateTime GeneratedDate { get; set; }
        }

        public class SASRAForm6Report
        {
            public List<SASRAForm6Line> ReportLines { get; set; } = new List<SASRAForm6Line>();
            public SASRAForm6Metadata Metadata { get; set; } 
        }

        public class BalanceSheetEntry
        {
            public string AccountTypeCode { get; set; }
            public string SortOrder { get; set; }
            public string AccountCode { get; set; } 
            public string AccountName { get; set; } 
            public decimal Credit { get; set; }
            public decimal Debit { get; set; }
            public int AccountType { get; set; }
            public string SubDescription { get; set; }
        }


        public class IncomeStatementEntry
        {
            public string AccNo { get; set; } 
            public string AccName { get; set; } 
            public decimal Balance { get; set; }
        }

        public class IncomeStatementSection
        {
            public string SectionName { get; set; }
            public List<IncomeStatementEntry> Entries { get; set; } = new List<IncomeStatementEntry>();
        }

        public class IncomeStatementResult
        {
            public List<IncomeStatementSection> Sections { get; set; } = new List<IncomeStatementSection>();
            public IncomeStatementEntry NetIncome { get; set; }
        }


        public class DepositRangeResponse
        {
            public string Code { get; set; }
            public string Description { get; set; }
            public decimal Range1 { get; set; }
            public decimal Range2 { get; set; }
            public decimal Balance { get; set; }
            public int Countx { get; set; }
            public string OrderCode { get; set; }
            public string OrderName { get; set; }
        }

        [HttpGet]
        [Route("StatementOfDepositReturn")]
        public async Task<IHttpActionResult> GetStatementOfDepositReturn(DateTime endDate)
        {
            var results = new List<DepositRangeResponse>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (var cmd = new SqlCommand("dbo.sp_StatementOfDepositReturn", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@enddate", SqlDbType.DateTime).Value = endDate;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new DepositRangeResponse
                                {
                                    Code = reader["Code"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Range1 = reader.GetDecimal(reader.GetOrdinal("Range1")),
                                    Range2 = reader.GetDecimal(reader.GetOrdinal("Range2")),
                                    Balance = reader.GetDecimal(reader.GetOrdinal("Balance")),
                                    Countx = Convert.ToInt32(reader["Countx"]),
                                    OrderCode = reader["OrderCode"].ToString(),
                                    OrderName = reader["OrderName"].ToString()
                                });
                            }
                        }
                    }
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                // Log exception here with your logging framework
                return InternalServerError(ex);
            }
        }
    

        [HttpGet,Route("IncomeStatement")]
        public async Task<IHttpActionResult> GetIncomeStatement([FromUri] DateTime endDate, [FromUri] Guid postingPeriod)
        {
            var result = await GetIncomeStatementAsync(endDate, postingPeriod);
            return Ok(result);
        }


        public async Task<IncomeStatementResult> GetIncomeStatementAsync(DateTime endDate, Guid postingPeriod)
        {
            var result = new IncomeStatementResult();
            var incomeEntries = new List<IncomeStatementEntry>();
            var expenseEntries = new List<IncomeStatementEntry>();
            IncomeStatementEntry netIncome = null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.sp_IncomeStatement", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@EndDate", endDate);
                cmd.Parameters.AddWithValue("@PostingPeriod", postingPeriod);

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var section = reader["Section"].ToString();
                        var entry = new IncomeStatementEntry
                        {
                            AccNo = reader["AccNo"]?.ToString() ?? string.Empty,
                            AccName = reader["AccName"]?.ToString() ?? string.Empty,
                            Balance = reader.GetDecimal(reader.GetOrdinal("Balance"))
                        };

                        if (section == "Income")
                            incomeEntries.Add(entry);
                        else if (section == "Expenses")
                            expenseEntries.Add(entry);
                        else if (section == "Net")
                            netIncome = entry;
                    }
                }
            }

            result.Sections.Add(new IncomeStatementSection { SectionName = "Income", Entries = incomeEntries });
            result.Sections.Add(new IncomeStatementSection { SectionName = "Expenses", Entries = expenseEntries });
            result.NetIncome = netIncome ?? new IncomeStatementEntry { AccName = "PROFIT / LOSS", Balance = 0 };

            return result;
        }


        [HttpGet]
        [Route("ConsolidatedBalanceSheet")]
        public async Task<IHttpActionResult> GetConsolidatedBalanceSheet([FromUri] DateTime endDate)
        {
            try
            {
                var data = await GetConsolidatedBalanceSheetAsync(endDate);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public async Task<List<BalanceSheetEntry>> GetConsolidatedBalanceSheetAsync(DateTime endDate)
        {
            var result = new List<BalanceSheetEntry>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.sp_ConsolidatedBalanceSheet", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate;

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var entry = new BalanceSheetEntry
                        {
                            AccountTypeCode = reader["AccountTypeCode"].ToString(),
                            SortOrder = reader["SortOrder"].ToString(),
                            AccountCode = reader["AccountCode"].ToString(),
                            AccountName = reader["AccountName"].ToString(),
                            Credit = Convert.ToDecimal(reader["Credit"]),
                            Debit = Convert.ToDecimal(reader["Debit"]),
                            AccountType = Convert.ToInt32(reader["AccountType"]),
                            SubDescription = reader["subdescription"]?.ToString()
                        };

                        result.Add(entry);
                    }
                }
            }

            return result;
        }

        [HttpGet]
        [Route("SASRAForm6")]
        public async Task<IHttpActionResult> GetSASRAForm6([FromUri] DateTime startDate, [FromUri] DateTime endDate, [FromUri] string saccoName = "SWIZZ REGULATED NON-WDT SACCO LTD")
        {
            try
            {
                var report = await GenerateReportAsync(startDate, endDate, saccoName);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        public async Task<SASRAForm6Report> GenerateReportAsync(DateTime startDate, DateTime endDate, string saccoName = "SWIZZ REGULATED NON-WDT SACCO LTD")
        {
            var report = new SASRAForm6Report();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.sp_GenerateSASRAForm6_Report", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate;
                cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate;
                cmd.Parameters.Add("@SaccoName", SqlDbType.NVarChar, 200).Value = saccoName;

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    // First result set = Report Lines
                    while (await reader.ReadAsync())
                    {
                        report.ReportLines.Add(new SASRAForm6Line
                        {
                            ReportSection = reader["ReportSection"].ToString(),
                            LineItem = reader["LineItem"].ToString(),
                            Amount = reader["Amount"] as decimal?,
                            DisplayOrder = Convert.ToInt32(reader["DisplayOrder"])
                        });
                    }

                    // Move to second result set = Metadata
                    if (await reader.NextResultAsync() && await reader.ReadAsync())
                    {
                        report.Metadata = new SASRAForm6Metadata
                        {
                            SaccoName = reader["SaccoName"].ToString(),
                            FiscalStartDate = Convert.ToDateTime(reader["FiscalStartDate"]),
                            PeriodEndingDate = Convert.ToDateTime(reader["PeriodEndingDate"]),
                            GeneratedDate = Convert.ToDateTime(reader["GeneratedDate"])
                        };
                    }
                }
            }

            return report;
        }

        [HttpGet, Route("pdf/{customerId:guid}")]
        public IHttpActionResult DownloadStatementPdf(Guid customerId, DateTime startDate, DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                    return Content(System.Net.HttpStatusCode.BadRequest,
                    new { success = false, message = "Start date cannot be after end date" });
                var customer = _customerService.GetById(customerId);
                if (customer == null)
                    return Content(System.Net.HttpStatusCode.NotFound,
                    new { success = false, message = "Customer not found" });
                // Get transactions
                var statement = _statementService.GetCustomerStatementByCustomerId(customerId, startDate, endDate)
    .ToList();
                // Calculate balances
                var openingBalance = _statementService.GetCustomerBalanceAsOfDate(customerId, startDate.AddSeconds(-1));
                var closingBalance = _statementService.GetCustomerBalanceAsOfDate(customerId, endDate);
                // Calculate totals
                decimal totalDebit = 0, totalCredit = 0;
                foreach (var transaction in statement)
                {
                    totalDebit += transaction.Debit;
                    totalCredit += transaction.Credit;
                }
                // Get product breakdown
                var productBreakdown = _statementService.GetStatementByProduct(customerId, startDate, endDate)
    .ToList();
                // Prepare summary
                var summary = new CustomerStatementSummaryDTO
                {
                    CustomerName = customer.FullName,
                    SerialNumber = customer.SerialNumber.ToString(),
                    FirstTransactionDate = startDate,
                    LastTransactionDate = endDate,
                    OpeningBalance = openingBalance,
                    ClosingBalance = closingBalance,
                    TotalTransactions = statement.Count,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
                    NetBalance = totalCredit - totalDebit,
                    ProductBreakdown = productBreakdown,
                    FullAccount = statement.FirstOrDefault()?.FullAccount
                };
                // Prepare customer info with AccountName
                var customerInfo = new
                {
                    // For Member Name - get from first transaction's AccountName
                    AccountName = statement.FirstOrDefault()?.AccountName ?? customer.FullName,
                    FullName = customer.FullName,
                    SerialNumber = customer.SerialNumber,
                    Reference1 = customer.Reference1,  // Will be used as Payroll No
                    Reference2 = customer.Reference2,  // Will be used as Member No (with zeros)
                    Reference3 = customer.Reference3,
                    AddressAddressLine1 = customer.AddressAddressLine1,
                    AddressAddressLine2 = customer.AddressAddressLine2,
                    AddressMobileLine = customer.AddressMobileLine,
                    IndividualIdentityCardNumber = customer.IndividualIdentityCardNumber,
                    RegistrationDate = customer.CreatedDate
                };
                // Prepare product summaries
                var productSummaries = new List<ProductTransactionSummary>();
                foreach (var product in productBreakdown)
                {
                    var productTransactions = statement
                    .Where(t => t.Product == product.ProductName)
                    .ToList();
                    productSummaries.Add(new ProductTransactionSummary
                    {
                        ProductName = product.ProductName,
                        ProductType = product.ProductType,
                        Transactions = productTransactions,
                        TotalDebit = product.TotalDebit,
                        TotalCredit = product.TotalCredit,
                        NetBalance = product.NetBalance
                    });
                }
                var pdfService = new CustomerStatementPdfService();
                var pdfBytes = pdfService.GenerateCustomerStatementPdf(
                statement, summary, customerInfo, startDate, endDate,
                openingBalance, closingBalance, productSummaries);
                return new System.Web.Http.Results.ResponseMessageResult(
                new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new System.Net.Http.ByteArrayContent(pdfBytes)
                    {
                        Headers =
                {
ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf"),
ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
{
FileName = $"Rubani_Statement_{customer.Reference2}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf"
}
                }
                    }
                });
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError,
                new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        [Route("members-list-pdf")]
        public async Task<IHttpActionResult> DownloadMembersListPdf(
[FromUri] int pageIndex = 0,
[FromUri] int pageSize = 20)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();
                // Get paginated customers
                var customersPage = await master._channelService.FindCustomersInPageAsync(
     pageIndex,
     pageSize,
     serviceHeader
     );
                if (customersPage == null || customersPage.PageCollection == null || !customersPage.PageCollection.Any())
                {
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No members found.",
                        Data = null
                    });
                }
                // Extract values with proper type casting
                int currentPageIndex = Convert.ToInt32(customersPage.PageIndex);
                int currentPageSize = Convert.ToInt32(customersPage.PageSize);
                int totalCount = Convert.ToInt32(customersPage.TotalCount);
                int totalPages = Convert.ToInt32(customersPage.TotalPages);
                int pageCollectionCount = customersPage.PageCollection.Count;
                // Convert to MemberSummaryDTO list
                var members = new List<MemberSummaryDTO>();
                // Process each customer to get their details
                foreach (var customer in customersPage.PageCollection)
                {
                    // Get accounts for each customer to calculate totals
                    List<AccountBalanceDTO> accounts = null;
                    try
                    {
                        var customerAccounts = await master._channelService.FindCustomerAccountsByCustomerIdAsync(
                        customer.Id,
                        true, // includeAccountBalances
                                               true, // includeProductDescription
                                               false, // includeInterestBalanceForLoanAccounts
                                               false, // considerMaturityPeriodForInvestmentAccounts
                                               serviceHeader
                        );
                        if (customerAccounts != null && customerAccounts.Any())
                        {
                            accounts = customerAccounts.Select(a => new AccountBalanceDTO
                            {
                                BookBalance = a.BookBalance,
                                AvailableBalance = a.AvailableBalance
                            }).ToList();
                        }
                    }
                    catch (Exception accountEx)
                    {
                        // Log error but continue processing other customers
                        System.Diagnostics.Trace.TraceError($"Error fetching accounts for customer {customer.Id}: {accountEx.Message}");
                        // Continue with zero balances if accounts can't be fetched
                    }
                    // Combine IndividualFirstName and IndividualLastName for full name
                    string fullName = "";
                    if (!string.IsNullOrEmpty(customer.IndividualFirstName) && !string.IsNullOrEmpty(customer.IndividualLastName))
                    {
                        fullName = $"{customer.IndividualFirstName} {customer.IndividualLastName}";
                    }
                    else if (!string.IsNullOrEmpty(customer.FullName))
                    {
                        fullName = customer.FullName; // Fallback to FullName if available
                    }
                    else if (!string.IsNullOrEmpty(customer.IndividualFirstName))
                    {
                        fullName = customer.IndividualFirstName; // Only first name
                    }
                    else if (!string.IsNullOrEmpty(customer.IndividualLastName))
                    {
                        fullName = customer.IndividualLastName; // Only last name
                    }
                    else
                    {
                        fullName = "N/A";
                    }
                    var member = new MemberSummaryDTO
                    {
                        MembershipNumber = customer.Reference2, // Member No
                        FullName = fullName,
                        IdNumber = customer.IndividualIdentityCardNumber,
                        Mobile = customer.AddressMobileLine,
                        //Branch = customer.BranchDescription,
                        RegistrationDate = customer.RegistrationDate,
                        Status = customer.RecordStatusDescription,
                        TotalAccounts = accounts?.Count ?? 0,
                        TotalBalance = accounts?.Sum(a => a.BookBalance) ?? 0
                    };
                    members.Add(member);
                }
                // Generate PDF
                var pdfService = new MembersListPdfService();
                var pdfBytes = pdfService.GenerateMembersListPdf(
                members,
                totalCount,
                currentPageIndex,
                currentPageSize);
                // Return PDF file
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(pdfBytes)
                };
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"Rubani_Members_List_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };
                return ResponseMessage(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error generating members list PDF: {ex.Message}");
                System.Diagnostics.Trace.TraceError($"Stack Trace: {ex.StackTrace}");
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while generating the report.",
                    Data = new { Error = ex.Message, InnerError = ex.InnerException?.Message }
                });
            }
        }
        // Helper DTO for account balances (add this class inside ValuesController or at the end of the file)
        public class AccountBalanceDTO
        {
            public decimal BookBalance { get; set; }
            public decimal AvailableBalance { get; set; }
        }
        public class LoanKpiReportDto
        {
            public decimal TotalOutstandingLoansForActive { get; set; }
            public int TotalMembership { get; set; }
            public decimal MaximumLoanRepayment { get; set; }
            public decimal MaximumOutstandingIndividualLoan { get; set; }
            public decimal TotalOutstandingLoansForMembersAbove75 { get; set; }
            public string MainMembersOccupation { get; set; }
        }

        [HttpGet]
        [Route("CICRUBANISACCODATARENEWALTEMPLATE")]
        public async Task<IHttpActionResult> GetLoanKpiReport()
        {
            const string sql = @"
WITH ActiveLoans AS (
    SELECT
        lc.CustomerId,
        lc.TotalLoansBalance,
        lc.TotalPaybackAmount
    FROM swiftFin_LoanCases lc
),
CustomerAges AS (
    SELECT
        c.Id AS CustomerId,
        DATEDIFF(YEAR, c.Individual_BirthDate, GETDATE())
            - CASE
                WHEN DATEADD(YEAR, DATEDIFF(YEAR, c.Individual_BirthDate, GETDATE()), c.Individual_BirthDate) > GETDATE()
                THEN 1 ELSE 0
              END AS Age,
        c.Individual_EmploymentDesignation
    FROM swiftFin_Customers c
    WHERE c.RecordStatus = 1
)
SELECT
    SUM(al.TotalLoansBalance)                                                AS TotalOutstandingLoansForActive,
    (SELECT COUNT(*) FROM swiftFin_Customers WHERE RecordStatus = 1)          AS TotalMembership,
    MAX(al.TotalPaybackAmount)                                                AS MaximumLoanRepayment,
    MAX(al.TotalLoansBalance)                                                 AS MaximumOutstandingIndividualLoan,
    SUM(CASE WHEN ca.Age > 75 THEN al.TotalLoansBalance ELSE 0 END)           AS TotalOutstandingLoansForMembersAbove75,
    (
        SELECT TOP 1 ca2.Individual_EmploymentDesignation
        FROM CustomerAges ca2
        WHERE ca2.Individual_EmploymentDesignation IS NOT NULL
        GROUP BY ca2.Individual_EmploymentDesignation
        ORDER BY COUNT(*) DESC
    )                                                                         AS MainMembersOccupation
FROM ActiveLoans al
LEFT JOIN CustomerAges ca ON al.CustomerId = ca.CustomerId;
";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (!reader.Read())
                        return Ok(new LoanKpiReportDto());

                    var result = new LoanKpiReportDto
                    {
                        TotalOutstandingLoansForActive =
                            reader["TotalOutstandingLoansForActive"] as decimal? ?? 0,

                        TotalMembership =
                            reader["TotalMembership"] as int? ?? 0,

                        MaximumLoanRepayment =
                            reader["MaximumLoanRepayment"] as decimal? ?? 0,

                        MaximumOutstandingIndividualLoan =
                            reader["MaximumOutstandingIndividualLoan"] as decimal? ?? 0,

                        TotalOutstandingLoansForMembersAbove75 =
                            reader["TotalOutstandingLoansForMembersAbove75"] as decimal? ?? 0,

                        MainMembersOccupation =
                            reader["MainMembersOccupation"]?.ToString()
                    };

                    return Ok(result);
                }
            }
        }


        public class ActiveMemberLoanDto
        {
            public string FullName { get; set; }
            public string IdNumber { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public string Gender { get; set; }
            public int OriginalLoanTerm { get; set; }
            public DateTime? LoanIssueDate { get; set; }
            public decimal OriginalLoanAmount { get; set; }
            public decimal OutstandingLoanBalance { get; set; }
            public int OutstandingLoanTerm { get; set; }
            public string OutstandingDepositBalance { get; set; }
            public string InterestRateApplicable { get; set; }
        }




        [HttpGet]
        [Route("CICRUBANIYEARLYDATARENEWTEMPLATE")]
        public async Task<IHttpActionResult> CICRUBANIYEARLYDATARENEWTEMPLATE()
        {
            const string sql = @"
SELECT
    c.Individual_FirstName + ' ' + c.Individual_LastName AS FullName,
    c.Reference2 AS IdNumber,
    c.Individual_BirthDate AS DateOfBirth,
    c.Individual_Gender AS Gender,
    lc.LoanRegistration_TermInMonths AS OriginalLoanTerm,
    lc.DisbursedDate AS LoanIssueDate,
    lc.AmountApplied AS OriginalLoanAmount,
    lc.TotalLoansBalance AS OutstandingLoanBalance,
    DATEDIFF(MONTH, GETDATE(), DATEADD(MONTH, lc.LoanRegistration_TermInMonths, lc.ReceivedDate)) AS OutstandingLoanTerm,
    '' AS OutstandingDepositBalance,
    CONCAT(lc.LoanInterest_AnnualPercentageRate, '%') AS InterestRateApplicable
FROM swiftFin_LoanCases lc
INNER JOIN swiftFin_Customers c ON lc.CustomerId = c.Id
LEFT JOIN swiftFin_CustomerAccounts ca ON ca.CustomerId = c.Id
WHERE c.RecordStatus = 1
ORDER BY c.Individual_FirstName, c.Individual_LastName;
";

            var result = new List<ActiveMemberLoanDto>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new ActiveMemberLoanDto
                        {
                            FullName = reader["FullName"]?.ToString(),
                            IdNumber = reader["IdNumber"]?.ToString(),
                            DateOfBirth = reader["DateOfBirth"] as DateTime?,
                            Gender = reader["Gender"]?.ToString(),
                            OriginalLoanTerm = reader["OriginalLoanTerm"] as int? ?? 0,
                            LoanIssueDate = reader["LoanIssueDate"] as DateTime?,
                            OriginalLoanAmount = reader["OriginalLoanAmount"] as decimal? ?? 0,
                            OutstandingLoanBalance = reader["OutstandingLoanBalance"] as decimal? ?? 0,
                            OutstandingLoanTerm = reader["OutstandingLoanTerm"] as int? ?? 0,
                            OutstandingDepositBalance = reader["OutstandingDepositBalance"]?.ToString(),
                            InterestRateApplicable = reader["InterestRateApplicable"]?.ToString()
                        });
                    }
                }
            }

            return Ok(result);
        }




        [HttpGet]
        [Route("GetLoanSummary")]
        public async Task<IHttpActionResult> GetLoanSummary()
        {
            var loans = new List<LoanSummaryDto>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT
                    c.Individual_FirstName + ' ' + c.Individual_LastName AS [Member Name],
                    c.Reference2 AS [ID Number],
                    lc.AmountApplied AS [Loan Amount],
                    lc.LoanRegistration_TermInMonths AS [Repayment Period (Months)],
                    lc.DisbursedDate AS [Date Loan Granted]
                FROM swiftFin_LoanCases lc
                INNER JOIN swiftFin_Customers c ON lc.CustomerId = c.Id
                WHERE c.RecordStatus = 1
                ORDER BY c.Individual_FirstName, c.Individual_LastName;
            ", conn))
            {
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        loans.Add(new LoanSummaryDto
                        {
                            MemberName = reader["Member Name"] as string,
                            IdNumber = reader["ID Number"] as string,
                            LoanAmount = reader["Loan Amount"] as decimal?, // safe cast
                            RepaymentPeriodMonths = reader["Repayment Period (Months)"] as int?, // safe cast
                            DateLoanGranted = reader["Date Loan Granted"] as DateTime? // safe cast
                        });
                    }
                }
            }

            return Ok(loans);
        }

        [HttpGet]
        [Route("GetCustomerLoanLedger")]
        public IHttpActionResult GetCustomerLoanLedger(Guid customerId)
        {
            var results = new List<CustomerLoanLedgerDto>();

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
        je.Id AS JournalEntryId,
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
        ORDER BY CreatedDate, JournalEntryId
        ROWS UNBOUNDED PRECEDING
    ) AS RunningBalance
FROM Tx
ORDER BY AccountNumber, CreatedDate;
";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var dto = new CustomerLoanLedgerDto();

                        dto.CustomerAccountId = reader.GetGuid(reader.GetOrdinal("CustomerAccountId"));
                        dto.FullName = reader.GetString(reader.GetOrdinal("FullName"));
                        dto.AccountNumber = reader.GetString(reader.GetOrdinal("AccountNumber"));
                        dto.LoanProductName = reader.GetString(reader.GetOrdinal("LoanProductName"));
                        dto.JournalEntryId = reader.GetGuid(reader.GetOrdinal("JournalEntryId"));
                        dto.CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"));
                        dto.Amount = reader.GetDecimal(reader.GetOrdinal("Amount"));
                        dto.RunningBalance = reader.GetDecimal(reader.GetOrdinal("RunningBalance"));

                        results.Add(dto);
                    }
                }
            }
            if (results.Count == 0)
            {
                return BadRequest("No ledger records found for this customer.");
            }



            return Ok(results);
        }
    }
}
public class CustomerLoanLedgerDto
{
    public Guid CustomerAccountId { get; set; }
    public string FullName { get; set; }
    public string AccountNumber { get; set; }
    public string LoanProductName { get; set; }
    public Guid JournalEntryId { get; set; }
    public DateTime CreatedDate { get; set; }
    public decimal Amount { get; set; }
    public decimal RunningBalance { get; set; }
}


public class LoanSummaryDto
{
    public string MemberName { get; set; }
    public string IdNumber { get; set; }
    public decimal? LoanAmount { get; set; }
    public int? RepaymentPeriodMonths { get; set; }
    public DateTime? DateLoanGranted { get; set; }
}






