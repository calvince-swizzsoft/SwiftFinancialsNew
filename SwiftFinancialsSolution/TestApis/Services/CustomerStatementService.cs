using Application.MainBoundedContext.DTO.RegistryModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TestApis.Services
{
    public class CustomerStatementService
    {
        private readonly string _connectionString;

        public CustomerStatementService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get customer statement using parameters similar to the stored procedure
        public IEnumerable<CustomerStatementDTO> GetCustomerStatement(
            DateTime startDate,
            DateTime endDate,
            string searchBy,
            string searchString)
        {
            var list = new List<CustomerStatementDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                // Adjust end date to include the entire day
                endDate = endDate.Date.AddDays(1).AddSeconds(-1);

                // Simpler query without complex string concatenation that causes overflow
                string query = @"
                    SELECT 
                        -- Build account number dynamically with proper casting
                        CASE 
                            WHEN ca.CustomerAccountType_ProductCode IS NOT NULL 
                                 AND ca.CustomerAccountType_TargetProductCode IS NOT NULL
                                 AND b.Code IS NOT NULL
                            THEN 
                                -- Use CONCAT to avoid arithmetic overflow
                                CONCAT(
                                    LEFT(CONCAT('000', CAST(b.Code AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000000', CAST(c.SerialNumber AS VARCHAR(10))), 6),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_ProductCode AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_TargetProductCode AS VARCHAR(10))), 3)
                                )
                            ELSE 'N/A'
                        END as AccountNumber,
                        -- Get customer name directly from Customers table (simpler)
                        CASE 
                            WHEN c.[Type] = 1 THEN 
                                CONCAT(
                                    CASE c.Individual_Salutation
                                        WHEN 1 THEN 'Mr. '
                                        WHEN 2 THEN 'Mrs. '
                                        WHEN 3 THEN 'Miss '
                                        WHEN 4 THEN 'Dr. '
                                        WHEN 5 THEN 'Prof. '
                                        ELSE ''
                                    END,
                                    ISNULL(c.Individual_FirstName, ''),
                                    ' ',
                                    ISNULL(c.Individual_LastName, '')
                                )
                            ELSE ISNULL(c.NonIndividual_Description, '')
                        END as AccountName, 
                        -- Product information - check both savings and loan products
                        CASE 
                            WHEN sp.Id IS NOT NULL THEN sp.Description
                            WHEN lp.Id IS NOT NULL THEN lp.Description
                            WHEN ca.CustomerAccountType_TargetProductId IS NOT NULL THEN 'Unknown Product'
                            ELSE 'General Account'
                        END as Product,
                        CASE 
                            WHEN sp.Id IS NOT NULL THEN 'Savings'
                            WHEN lp.Id IS NOT NULL THEN 'Loan'
                            ELSE 'Other'
                        END as ProductType,
                        ISNULL(sp.Id, lp.Id) as ProductId, 
                        je.CreatedDate as TrxDate, 
                        j.PrimaryDescription,
                        j.SecondaryDescription,  
                        j.Reference,
                        CASE WHEN je.Amount < 0 THEN je.Amount * -1 ELSE 0 END AS Debit, 
                        CASE WHEN je.Amount > 0 THEN je.Amount ELSE 0 END AS Credit, 
                        je.Amount as RunningTotal,
                        c.Reference1 as CustomerReference1,
                        c.Reference2 as CustomerReference2,
                        c.Reference3 as CustomerReference3
                    FROM [swiftFin_JournalEntries] je
                    INNER JOIN [swiftFin_Journals] j ON je.JournalId = j.Id
                    INNER JOIN [swiftFin_CustomerAccounts] ca ON je.CustomerAccountId = ca.Id
                    INNER JOIN [swiftFin_Customers] c ON ca.CustomerId = c.Id
                    LEFT JOIN [swiftFin_Branches] b ON ca.BranchId = b.Id
                    -- Join with Savings Products (if CustomerAccountType_TargetProductId matches)
                    LEFT JOIN [swiftFin_SavingsProducts] sp ON ca.CustomerAccountType_TargetProductId = sp.Id
                    -- Join with Loan Products (if CustomerAccountType_TargetProductId matches)
                    LEFT JOIN [swiftFin_LoanProducts] lp ON ca.CustomerAccountType_TargetProductId = lp.Id
                    WHERE je.CustomerAccountId IS NOT NULL
                    AND (
                        (@SearchBy = 'IdentityCardNumber' AND c.Individual_IdentityCardNumber = @SearchString)
                        OR (@SearchBy = 'SerialNumber' AND CAST(c.SerialNumber AS VARCHAR(20)) = @SearchString)
                        OR (@SearchBy = 'PersonalFileNumber' AND c.Individual_PayrollNumbers = @SearchString)
                        OR (@SearchBy = 'Reference1' AND c.Reference1 = @SearchString)
                        OR (@SearchBy = 'Reference2' AND c.Reference2 = @SearchString)
                        OR (@SearchBy = 'Reference3' AND c.Reference3 = @SearchString)
                    )
                    AND je.CreatedDate BETWEEN @StartDate AND @EndDate
                    ORDER BY je.CreatedDate";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    cmd.Parameters.AddWithValue("@SearchBy", searchBy ?? "");
                    cmd.Parameters.AddWithValue("@SearchString", searchString ?? "");

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        // Simplified version for specific customer
        public IEnumerable<CustomerStatementDTO> GetCustomerStatementByCustomerId(
            Guid customerId,
            DateTime startDate,
            DateTime endDate)
        {
            var list = new List<CustomerStatementDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                // Adjust end date to include the entire day
                endDate = endDate.Date.AddDays(1).AddSeconds(-1);

                string query = @"
                    SELECT 
                        je.CreatedDate as TrxDate,
                        j.PrimaryDescription,
                        j.SecondaryDescription,
                        j.Reference,
                        CASE WHEN je.Amount < 0 THEN je.Amount * -1 ELSE 0 END as Debit,
                        CASE WHEN je.Amount > 0 THEN je.Amount ELSE 0 END as Credit,
                        je.Amount as RunningTotal,
                        -- Build account number dynamically
                        CASE 
                            WHEN ca.CustomerAccountType_ProductCode IS NOT NULL 
                                 AND ca.CustomerAccountType_TargetProductCode IS NOT NULL
                                 AND b.Code IS NOT NULL
                            THEN 
                                CONCAT(
                                    LEFT(CONCAT('000', CAST(b.Code AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000000', CAST(c.SerialNumber AS VARCHAR(10))), 6),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_ProductCode AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_TargetProductCode AS VARCHAR(10))), 3)
                                )
                            ELSE 'N/A'
                        END as AccountNumber,
                        c.Reference1 as CustomerReference1,
                        c.Reference2 as CustomerReference2,
                        c.Reference3 as CustomerReference3,
                        -- Customer info
                        c.SerialNumber,
                        -- Get customer name from Customers table (Individual_FirstName + Individual_LastName)
                        CASE 
                            WHEN c.[Type] = 1 THEN 
                                CONCAT(
                                    CASE c.Individual_Salutation
                                        WHEN 1 THEN 'Mr. '
                                        WHEN 2 THEN 'Mrs. '
                                        WHEN 3 THEN 'Miss '
                                        WHEN 4 THEN 'Dr. '
                                        WHEN 5 THEN 'Prof. '
                                        ELSE ''
                                    END,
                                    ISNULL(c.Individual_FirstName, ''),
                                    ' ',
                                    ISNULL(c.Individual_LastName, '')
                                )
                            ELSE ISNULL(c.NonIndividual_Description, '')
                        END as AccountName,
                        -- Product info - check both savings and loan products
                        CASE 
                            WHEN sp.Id IS NOT NULL THEN sp.Description
                            WHEN lp.Id IS NOT NULL THEN lp.Description
                            WHEN ca.CustomerAccountType_TargetProductId IS NOT NULL THEN 'Unknown Product'
                            ELSE 'General Account'
                        END as Product,
                        ISNULL(sp.Id, lp.Id) as ProductId
                    FROM [swiftFin_JournalEntries] je
                    INNER JOIN [swiftFin_Journals] j ON je.JournalId = j.Id
                    INNER JOIN [swiftFin_CustomerAccounts] ca ON je.CustomerAccountId = ca.Id
                    INNER JOIN [swiftFin_Customers] c ON ca.CustomerId = c.Id
                    LEFT JOIN [swiftFin_Branches] b ON ca.BranchId = b.Id
                    -- Check for savings product (using TargetProductId)
                    LEFT JOIN [swiftFin_SavingsProducts] sp ON ca.CustomerAccountType_TargetProductId = sp.Id
                    -- Check for loan product (using TargetProductId)
                    LEFT JOIN [swiftFin_LoanProducts] lp ON ca.CustomerAccountType_TargetProductId = lp.Id
                    WHERE c.Id = @CustomerId 
                        AND je.CreatedDate BETWEEN @StartDate AND @EndDate
                        AND je.CustomerAccountId IS NOT NULL
                    ORDER BY je.CreatedDate";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerId", customerId);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(MapSimple(reader));
                }
            }
            return list;
        }

        // Get statement summary/balance
        public CustomerStatementSummaryDTO GetStatementSummary(
            string searchBy,
            string searchString,
            DateTime startDate,
            DateTime endDate)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                // Adjust end date to include the entire day
                endDate = endDate.Date.AddDays(1).AddSeconds(-1);

                string query = @"
                    SELECT 
                        COUNT(*) as TotalTransactions,
                        ISNULL(SUM(CASE WHEN je.Amount < 0 THEN je.Amount * -1 ELSE 0 END), 0) as TotalDebit,
                        ISNULL(SUM(CASE WHEN je.Amount > 0 THEN je.Amount ELSE 0 END), 0) as TotalCredit,
                        ISNULL(SUM(je.Amount), 0) as NetBalance,
                        MIN(je.CreatedDate) as FirstTransactionDate,
                        MAX(je.CreatedDate) as LastTransactionDate,
                        -- Customer name from Customers table (Individual_FirstName + Individual_LastName)
                        CASE 
                            WHEN c.[Type] = 1 THEN 
                                CONCAT(
                                    CASE c.Individual_Salutation
                                        WHEN 1 THEN 'Mr. '
                                        WHEN 2 THEN 'Mrs. '
                                        WHEN 3 THEN 'Miss '
                                        WHEN 4 THEN 'Dr. '
                                        WHEN 5 THEN 'Prof. '
                                        ELSE ''
                                    END,
                                    ISNULL(c.Individual_FirstName, ''),
                                    ' ',
                                    ISNULL(c.Individual_LastName, '')
                                )
                            ELSE ISNULL(c.NonIndividual_Description, '')
                        END as CustomerName,
                        c.SerialNumber,
                        -- Build account number dynamically
                        CASE 
                            WHEN ca.CustomerAccountType_ProductCode IS NOT NULL 
                                 AND ca.CustomerAccountType_TargetProductCode IS NOT NULL
                                 AND b.Code IS NOT NULL
                            THEN 
                                CONCAT(
                                    LEFT(CONCAT('000', CAST(b.Code AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000000', CAST(c.SerialNumber AS VARCHAR(10))), 6),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_ProductCode AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_TargetProductCode AS VARCHAR(10))), 3)
                                )
                            ELSE 'N/A'
                        END as AccountNumber
                    FROM [swiftFin_JournalEntries] je
                    INNER JOIN [swiftFin_Journals] j ON je.JournalId = j.Id
                    INNER JOIN [swiftFin_CustomerAccounts] ca ON je.CustomerAccountId = ca.Id
                    INNER JOIN [swiftFin_Customers] c ON ca.CustomerId = c.Id
                    LEFT JOIN [swiftFin_Branches] b ON ca.BranchId = b.Id
                    WHERE 
                        je.CreatedDate BETWEEN @StartDate AND @EndDate
                        AND je.CustomerAccountId IS NOT NULL
                        AND (
                            (@SearchBy = 'IdentityCardNumber' AND c.Individual_IdentityCardNumber = @SearchString)
                            OR (@SearchBy = 'SerialNumber' AND CAST(c.SerialNumber AS VARCHAR(20)) = @SearchString)
                            OR (@SearchBy = 'PersonalFileNumber' AND c.Individual_PayrollNumbers = @SearchString)
                            OR (@SearchBy = 'Reference1' AND c.Reference1 = @SearchString)
                            OR (@SearchBy = 'Reference2' AND c.Reference2 = @SearchString)
                            OR (@SearchBy = 'Reference3' AND c.Reference3 = @SearchString)
                            OR (@SearchString = '' AND @SearchBy = '')
                        )
                    GROUP BY 
                        CASE 
                            WHEN c.[Type] = 1 THEN 
                                CONCAT(
                                    CASE c.Individual_Salutation
                                        WHEN 1 THEN 'Mr. '
                                        WHEN 2 THEN 'Mrs. '
                                        WHEN 3 THEN 'Miss '
                                        WHEN 4 THEN 'Dr. '
                                        WHEN 5 THEN 'Prof. '
                                        ELSE ''
                                    END,
                                    ISNULL(c.Individual_FirstName, ''),
                                    ' ',
                                    ISNULL(c.Individual_LastName, '')
                                )
                            ELSE ISNULL(c.NonIndividual_Description, '')
                        END,
                        c.SerialNumber,
                        CASE 
                            WHEN ca.CustomerAccountType_ProductCode IS NOT NULL 
                                 AND ca.CustomerAccountType_TargetProductCode IS NOT NULL
                                 AND b.Code IS NOT NULL
                            THEN 
                                CONCAT(
                                    LEFT(CONCAT('000', CAST(b.Code AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000000', CAST(c.SerialNumber AS VARCHAR(10))), 6),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_ProductCode AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_TargetProductCode AS VARCHAR(10))), 3)
                                )
                            ELSE 'N/A'
                        END";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    cmd.Parameters.AddWithValue("@SearchBy", searchBy ?? "");
                    cmd.Parameters.AddWithValue("@SearchString", searchString ?? "");

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new CustomerStatementSummaryDTO
                            {
                                TotalTransactions = Convert.ToInt32(reader["TotalTransactions"]),
                                TotalDebit = Convert.ToDecimal(reader["TotalDebit"]),
                                TotalCredit = Convert.ToDecimal(reader["TotalCredit"]),
                                NetBalance = Convert.ToDecimal(reader["NetBalance"]),
                                FirstTransactionDate = reader["FirstTransactionDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FirstTransactionDate"]),
                                LastTransactionDate = reader["LastTransactionDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastTransactionDate"]),
                                CustomerName = reader["CustomerName"]?.ToString(),
                                SerialNumber = reader["SerialNumber"]?.ToString(),
                                FullAccount = reader["AccountNumber"]?.ToString()
                            };
                        }
                    }
                }
            }

            return new CustomerStatementSummaryDTO();
        }

        // Get opening balance (balance before start date)
        public decimal GetOpeningBalance(string searchBy, string searchString, DateTime startDate)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT ISNULL(SUM(je.Amount), 0) as OpeningBalance
                    FROM [swiftFin_JournalEntries] je
                    INNER JOIN [swiftFin_CustomerAccounts] ca ON je.CustomerAccountId = ca.Id
                    INNER JOIN [swiftFin_Customers] c ON ca.CustomerId = c.Id
                    WHERE 
                        je.CreatedDate < @StartDate
                        AND je.CustomerAccountId IS NOT NULL
                        AND (
                            (@SearchBy = 'IdentityCardNumber' AND c.Individual_IdentityCardNumber = @SearchString)
                            OR (@SearchBy = 'SerialNumber' AND CAST(c.SerialNumber AS VARCHAR(20)) = @SearchString)
                            OR (@SearchBy = 'PersonalFileNumber' AND c.Individual_PayrollNumbers = @SearchString)
                            OR (@SearchBy = 'Reference1' AND c.Reference1 = @SearchString)
                            OR (@SearchBy = 'Reference2' AND c.Reference2 = @SearchString)
                            OR (@SearchBy = 'Reference3' AND c.Reference3 = @SearchString)
                            OR (@SearchString = '' AND @SearchBy = '')
                        )";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@SearchBy", searchBy ?? "");
                    cmd.Parameters.AddWithValue("@SearchString", searchString ?? "");

                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToDecimal(result);
                }
            }
        }

        // Get customer account balance as of a specific date
        public decimal GetCustomerBalanceAsOfDate(Guid customerId, DateTime asOfDate)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT ISNULL(SUM(je.Amount), 0) as Balance
                    FROM [swiftFin_JournalEntries] je
                    INNER JOIN [swiftFin_CustomerAccounts] ca ON je.CustomerAccountId = ca.Id
                    WHERE ca.CustomerId = @CustomerId 
                        AND je.CreatedDate <= @AsOfDate
                        AND je.CustomerAccountId IS NOT NULL";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerId", customerId);
                    cmd.Parameters.AddWithValue("@AsOfDate", asOfDate);

                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToDecimal(result);
                }
            }
        }

        // Get statement with product breakdown
        public IEnumerable<CustomerProductStatementDTO> GetStatementByProduct(
            Guid customerId,
            DateTime startDate,
            DateTime endDate)
        {
            var list = new List<CustomerProductStatementDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                endDate = endDate.Date.AddDays(1).AddSeconds(-1);

                string query = @"
                    SELECT 
                        CASE 
                            WHEN sp.Id IS NOT NULL THEN sp.Description
                            WHEN lp.Id IS NOT NULL THEN lp.Description
                            WHEN ca.CustomerAccountType_TargetProductId IS NOT NULL THEN 'Unknown Product'
                            ELSE 'General Account'
                        END as ProductName,
                        CASE 
                            WHEN sp.Id IS NOT NULL THEN 'Savings'
                            WHEN lp.Id IS NOT NULL THEN 'Loan'
                            ELSE 'Other'
                        END as ProductType,
                        COUNT(je.Id) as TransactionCount,
                        ISNULL(SUM(CASE WHEN je.Amount < 0 THEN je.Amount * -1 ELSE 0 END), 0) as TotalDebit,
                        ISNULL(SUM(CASE WHEN je.Amount > 0 THEN je.Amount ELSE 0 END), 0) as TotalCredit,
                        ISNULL(SUM(je.Amount), 0) as NetBalance,
                        MIN(je.CreatedDate) as FirstTransaction,
                        MAX(je.CreatedDate) as LastTransaction,
                        -- Build account number dynamically
                        CASE 
                            WHEN ca.CustomerAccountType_ProductCode IS NOT NULL 
                                 AND ca.CustomerAccountType_TargetProductCode IS NOT NULL
                                 AND b.Code IS NOT NULL
                            THEN 
                                CONCAT(
                                    LEFT(CONCAT('000', CAST(b.Code AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000000', CAST(c.SerialNumber AS VARCHAR(10))), 6),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_ProductCode AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_TargetProductCode AS VARCHAR(10))), 3)
                                )
                            ELSE 'N/A'
                        END as AccountNumber
                    FROM [swiftFin_JournalEntries] je
                    INNER JOIN [swiftFin_CustomerAccounts] ca ON je.CustomerAccountId = ca.Id
                    INNER JOIN [swiftFin_Customers] c ON ca.CustomerId = c.Id
                    LEFT JOIN [swiftFin_Branches] b ON ca.BranchId = b.Id
                    -- Check for savings product (using TargetProductId)
                    LEFT JOIN [swiftFin_SavingsProducts] sp ON ca.CustomerAccountType_TargetProductId = sp.Id
                    -- Check for loan product (using TargetProductId)
                    LEFT JOIN [swiftFin_LoanProducts] lp ON ca.CustomerAccountType_TargetProductId = lp.Id
                    WHERE ca.CustomerId = @CustomerId 
                        AND je.CreatedDate BETWEEN @StartDate AND @EndDate
                        AND je.CustomerAccountId IS NOT NULL
                    GROUP BY 
                        CASE 
                            WHEN sp.Id IS NOT NULL THEN sp.Description
                            WHEN lp.Id IS NOT NULL THEN lp.Description
                            WHEN ca.CustomerAccountType_TargetProductId IS NOT NULL THEN 'Unknown Product'
                            ELSE 'General Account'
                        END,
                        CASE 
                            WHEN sp.Id IS NOT NULL THEN 'Savings'
                            WHEN lp.Id IS NOT NULL THEN 'Loan'
                            ELSE 'Other'
                        END,
                        CASE 
                            WHEN ca.CustomerAccountType_ProductCode IS NOT NULL 
                                 AND ca.CustomerAccountType_TargetProductCode IS NOT NULL
                                 AND b.Code IS NOT NULL
                            THEN 
                                CONCAT(
                                    LEFT(CONCAT('000', CAST(b.Code AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000000', CAST(c.SerialNumber AS VARCHAR(10))), 6),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_ProductCode AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_TargetProductCode AS VARCHAR(10))), 3)
                                )
                            ELSE 'N/A'
                        END
                    ORDER BY ProductType, ProductName";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerId", customerId);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(MapProductStatement(reader));
                }
            }
            return list;
        }

        // Get customer's current balances by product type
        public IEnumerable<CustomerProductBalanceDTO> GetCustomerProductBalances(Guid customerId)
        {
            var list = new List<CustomerProductBalanceDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT 
                        CASE 
                            WHEN sp.Id IS NOT NULL THEN sp.Description
                            WHEN lp.Id IS NOT NULL THEN lp.Description
                            WHEN ca.CustomerAccountType_TargetProductId IS NOT NULL THEN 'Unknown Product'
                            ELSE 'General Account'
                        END as ProductName,
                        CASE 
                            WHEN sp.Id IS NOT NULL THEN 'Savings'
                            WHEN lp.Id IS NOT NULL THEN 'Loan'
                            ELSE 'Other'
                        END as ProductType,
                        ISNULL(sp.Id, lp.Id) as ProductId,
                        -- Build account number dynamically
                        CASE 
                            WHEN ca.CustomerAccountType_ProductCode IS NOT NULL 
                                 AND ca.CustomerAccountType_TargetProductCode IS NOT NULL
                                 AND b.Code IS NOT NULL
                            THEN 
                                CONCAT(
                                    LEFT(CONCAT('000', CAST(b.Code AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000000', CAST(c.SerialNumber AS VARCHAR(10))), 6),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_ProductCode AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_TargetProductCode AS VARCHAR(10))), 3)
                                )
                            ELSE 'N/A'
                        END as AccountNumber,
                        ISNULL(SUM(je.Amount), 0) as CurrentBalance,
                        COUNT(je.Id) as TransactionCount,
                        MIN(je.CreatedDate) as FirstTransactionDate,
                        MAX(je.CreatedDate) as LastTransactionDate
                    FROM [swiftFin_JournalEntries] je
                    INNER JOIN [swiftFin_CustomerAccounts] ca ON je.CustomerAccountId = ca.Id
                    INNER JOIN [swiftFin_Customers] c ON ca.CustomerId = c.Id
                    LEFT JOIN [swiftFin_Branches] b ON ca.BranchId = b.Id
                    -- Check for savings product (using TargetProductId)
                    LEFT JOIN [swiftFin_SavingsProducts] sp ON ca.CustomerAccountType_TargetProductId = sp.Id
                    -- Check for loan product (using TargetProductId)
                    LEFT JOIN [swiftFin_LoanProducts] lp ON ca.CustomerAccountType_TargetProductId = lp.Id
                    WHERE ca.CustomerId = @CustomerId 
                        AND je.CustomerAccountId IS NOT NULL
                    GROUP BY 
                        CASE 
                            WHEN sp.Id IS NOT NULL THEN sp.Description
                            WHEN lp.Id IS NOT NULL THEN lp.Description
                            WHEN ca.CustomerAccountType_TargetProductId IS NOT NULL THEN 'Unknown Product'
                            ELSE 'General Account'
                        END,
                        CASE 
                            WHEN sp.Id IS NOT NULL THEN 'Savings'
                            WHEN lp.Id IS NOT NULL THEN 'Loan'
                            ELSE 'Other'
                        END,
                        ISNULL(sp.Id, lp.Id),
                        CASE 
                            WHEN ca.CustomerAccountType_ProductCode IS NOT NULL 
                                 AND ca.CustomerAccountType_TargetProductCode IS NOT NULL
                                 AND b.Code IS NOT NULL
                            THEN 
                                CONCAT(
                                    LEFT(CONCAT('000', CAST(b.Code AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000000', CAST(c.SerialNumber AS VARCHAR(10))), 6),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_ProductCode AS VARCHAR(10))), 3),
                                    '-',
                                    LEFT(CONCAT('000', CAST(ca.CustomerAccountType_TargetProductCode AS VARCHAR(10))), 3)
                                )
                            ELSE 'N/A'
                        END
                    ORDER BY ProductType, ProductName";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerId", customerId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(MapProductBalance(reader));
                }
            }
            return list;
        }

        private CustomerStatementDTO Map(IDataReader reader)
        {
            var dto = new CustomerStatementDTO
            {
                FullAccount = reader["AccountNumber"]?.ToString(),
                AccountName = reader["AccountName"]?.ToString(),
                Product = reader["Product"]?.ToString(),
                ProductType = reader["ProductType"]?.ToString(),
                TransactionDate = Convert.ToDateTime(reader["TrxDate"]),
                Description = reader["PrimaryDescription"]?.ToString(),
                Reference = reader["Reference"]?.ToString(),
                Debit = Convert.ToDecimal(reader["Debit"]),
                Credit = Convert.ToDecimal(reader["Credit"]),
                RunningTotal = Convert.ToDecimal(reader["RunningTotal"]),
                Reference1 = reader["CustomerReference1"]?.ToString(),
                Reference2 = reader["CustomerReference2"]?.ToString(),
                Reference3 = reader["CustomerReference3"]?.ToString()
            };

            // Handle ProductId - it might be Guid or string
            object productId = reader["ProductId"];
            if (productId != DBNull.Value)
            {
                if (productId is Guid)
                    dto.ProductId = (Guid)productId;
                else if (Guid.TryParse(productId.ToString(), out Guid guidValue))
                    dto.ProductId = guidValue;
                else
                    dto.ProductId = Guid.Empty;
            }

            return dto;
        }

        private CustomerStatementDTO MapSimple(IDataReader reader)
        {
            var dto = new CustomerStatementDTO
            {
                FullAccount = reader["AccountNumber"]?.ToString(),
                AccountName = reader["AccountName"]?.ToString(),
                Product = reader["Product"]?.ToString(),
                TransactionDate = Convert.ToDateTime(reader["TrxDate"]),
                Description = (reader["PrimaryDescription"]?.ToString() + " " + reader["SecondaryDescription"]?.ToString()).Trim(),
                Reference = reader["Reference"]?.ToString(),
                Debit = Convert.ToDecimal(reader["Debit"]),
                Credit = Convert.ToDecimal(reader["Credit"]),
                RunningTotal = Convert.ToDecimal(reader["RunningTotal"]),
                Reference1 = reader["CustomerReference1"]?.ToString(),
                Reference2 = reader["CustomerReference2"]?.ToString(),
                Reference3 = reader["CustomerReference3"]?.ToString()
            };

            // Handle ProductId
            object productId = reader["ProductId"];
            if (productId != DBNull.Value)
            {
                if (Guid.TryParse(productId.ToString(), out Guid guidValue))
                    dto.ProductId = guidValue;
            }

            return dto;
        }

        private CustomerProductStatementDTO MapProductStatement(IDataReader reader)
        {
            return new CustomerProductStatementDTO
            {
                ProductName = reader["ProductName"]?.ToString(),
                ProductType = reader["ProductType"]?.ToString(),
                TransactionCount = Convert.ToInt32(reader["TransactionCount"]),
                TotalDebit = Convert.ToDecimal(reader["TotalDebit"]),
                TotalCredit = Convert.ToDecimal(reader["TotalCredit"]),
                NetBalance = Convert.ToDecimal(reader["NetBalance"]),
                FirstTransaction = reader["FirstTransaction"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FirstTransaction"]),
                LastTransaction = reader["LastTransaction"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastTransaction"]),
                FullAccount = reader["AccountNumber"]?.ToString()
            };
        }

        private CustomerProductBalanceDTO MapProductBalance(IDataReader reader)
        {
            return new CustomerProductBalanceDTO
            {
                ProductName = reader["ProductName"]?.ToString(),
                ProductType = reader["ProductType"]?.ToString(),
                ProductId = reader["ProductId"] == DBNull.Value ? Guid.Empty : (Guid)reader["ProductId"],
                FullAccount = reader["AccountNumber"]?.ToString(),
                CurrentBalance = Convert.ToDecimal(reader["CurrentBalance"]),
                TransactionCount = Convert.ToInt32(reader["TransactionCount"]),
                FirstTransactionDate = reader["FirstTransactionDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FirstTransactionDate"]),
                LastTransactionDate = reader["LastTransactionDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastTransactionDate"])
            };
        }
    }

    // DTOs for Customer Statement
    public class CustomerStatementDTO
    {
        public string FullAccount { get; set; }
        public string AccountName { get; set; }
        public string Product { get; set; }
        public string ProductType { get; set; } // Savings, Loan, Other
        public Guid ProductId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningTotal { get; set; }
        public string Reference1 { get; set; }
        public string Reference2 { get; set; }
        public string Reference3 { get; set; }
    }

    public class CustomerProductStatementDTO
    {
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal NetBalance { get; set; }
        public DateTime? FirstTransaction { get; set; }
        public DateTime? LastTransaction { get; set; }
        public string FullAccount { get; set; }
    }

    public class CustomerProductBalanceDTO
    {
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public Guid ProductId { get; set; }
        public string FullAccount { get; set; }
        public decimal CurrentBalance { get; set; }
        public int TransactionCount { get; set; }
        public DateTime? FirstTransactionDate { get; set; }
        public DateTime? LastTransactionDate { get; set; }
    }

    public class CustomerStatementSummaryDTO
    {
        public int TotalTransactions { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal NetBalance { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public DateTime? FirstTransactionDate { get; set; }
        public DateTime? LastTransactionDate { get; set; }
        public string CustomerName { get; set; }
        public string SerialNumber { get; set; }
        public string FullAccount { get; set; }
        public List<CustomerProductStatementDTO> ProductBreakdown { get; set; }
        public List<CustomerProductBalanceDTO> ProductBalances { get; set; }
    }

    public class CustomerStatementRequestDTO
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string SearchBy { get; set; } // IdentityCardNumber, SerialNumber, PersonalFileNumber, Reference1, Reference2, Reference3
        public string SearchString { get; set; }
        public Guid? CustomerId { get; set; }
        public bool IncludeOpeningBalance { get; set; } = true;
        public bool IncludeProductBreakdown { get; set; } = false;
        public bool IncludeProductBalances { get; set; } = false;
    }
}