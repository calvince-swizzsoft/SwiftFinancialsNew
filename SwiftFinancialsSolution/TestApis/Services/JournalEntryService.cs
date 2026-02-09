using Application.MainBoundedContext.DTO.AccountsModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TestApis.Services
{
    public class JournalEntryService
    {
        private readonly string _connectionString;

        public JournalEntryService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get all journal entries for a specific journal ID
        public IEnumerable<JournalEntryDTO> GetByJournalId(Guid journalId)
        {
            var list = new List<JournalEntryDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT 
                        je.*,
                        -- Journal details
                        j.ParentId as JournalParentId,
                        j.PostingPeriodId as JournalPostingPeriodId,
                        j.BranchId as JournalBranchId,
                        j.AlternateChannelLogId as JournalAlternateChannelLogId,
                        j.TotalValue as JournalTotalValue,
                        j.PrimaryDescription as JournalPrimaryDescription,
                        j.SecondaryDescription as JournalSecondaryDescription,
                        j.Reference as JournalReference,
                        j.ApplicationUserName as JournalApplicationUserName,
                        j.EnvironmentUserName as JournalEnvironmentUserName,
                        j.EnvironmentMachineName as JournalEnvironmentMachineName,
                        j.EnvironmentDomainName as JournalEnvironmentDomainName,
                        j.EnvironmentOSVersion as JournalEnvironmentOSVersion,
                        j.EnvironmentMACAddress as JournalEnvironmentMACAddress,
                        j.EnvironmentMotherboardSerialNumber as JournalEnvironmentMotherboardSerialNumber,
                        j.EnvironmentProcessorId as JournalEnvironmentProcessorId,
                        j.EnvironmentIPAddress as JournalEnvironmentIPAddress,
                        j.ModuleNavigationItemCode as JournalModuleNavigationItemCode,
                        j.TransactionCode as JournalTransactionCode,
                        j.ValueDate as JournalValueDate,
                        j.SuppressAccountAlert as JournalSuppressAccountAlert,
                        j.CreatedDate as JournalCreatedDate,
                        j.IsLocked as JournalIsLocked,
                        -- Chart of Account details
                        coa.AccountType as ChartOfAccountAccountType,
                        coa.AccountCode as ChartOfAccountAccountCode,
                        coa.AccountName as ChartOfAccountAccountName,
                        coa.CostCenterId as ChartOfAccountCostCenterId,
                        cc.Description as ChartOfAccountCostCenterDescription,
                        -- Contra Chart of Account details
                        ccoa.AccountType as ContraChartOfAccountAccountType,
                        ccoa.AccountCode as ContraChartOfAccountAccountCode,
                        ccoa.AccountName as ContraChartOfAccountAccountName,
                        ccoa.CostCenterId as ContraChartOfAccountCostCenterId,
                        ccc.Description as ContraChartOfAccountCostCenterDescription,
                        -- Posting Period details
                        pp.Description as JournalPostingPeriodDescription,
                        -- Branch details
                        b.Description as JournalBranchDescription,
                        b.Address_Email as JournalBranchAddressEmail,
                        bc.Description as JournalBranchCompanyDescription,
                        -- Customer Account details (from swiftFin_CustomerAccounts only)
                        ca.BranchCode as CustomerAccountBranchCode,
                        ca.SerialNumber as CustomerAccountCustomerSerialNumber,
                        -- Customer details
                        c.Id as CustomerAccountCustomerId,
                        c.Type as CustomerAccountCustomerType,
                        ci.Salutation as CustomerAccountCustomerIndividualSalutation,
                        ci.FirstName as CustomerAccountCustomerIndividualFirstName,
                        ci.LastName as CustomerAccountCustomerIndividualLastName,
                        ci.PayrollNumbers as CustomerAccountCustomerIndividualPayrollNumbers,
                        ci.IdentityCardNumber as CustomerAccountCustomerIndividualIdentityCardNumber,
                        cn.Description as CustomerAccountCustomerNonIndividualDescription,
                        c.Reference1 as CustomerAccountCustomerReference1,
                        c.Reference2 as CustomerAccountCustomerReference2,
                        c.Reference3 as CustomerAccountCustomerReference3
                    FROM [swiftFin_JournalEntries] je
                    INNER JOIN [swiftFin_Journals] j ON je.JournalId = j.Id
                    LEFT JOIN [swiftFin_ChartOfAccounts] coa ON je.ChartOfAccountId = coa.Id
                    LEFT JOIN [swiftFin_ChartOfAccounts] ccoa ON je.ContraChartOfAccountId = ccoa.Id
                    LEFT JOIN [swiftFin_CostCenters] cc ON coa.CostCenterId = cc.Id
                    LEFT JOIN [swiftFin_CostCenters] ccc ON ccoa.CostCenterId = ccc.Id
                    LEFT JOIN [swiftFin_PostingPeriods] pp ON j.PostingPeriodId = pp.Id
                    LEFT JOIN [swiftFin_Branches] b ON j.BranchId = b.Id
                    LEFT JOIN [swiftFin_Companies] bc ON b.CompanyId = bc.Id
                    LEFT JOIN [swiftFin_CustomerAccounts] ca ON je.CustomerAccountId = ca.Id
                    LEFT JOIN [swiftFin_Customers] c ON ca.CustomerId = c.Id
                    LEFT JOIN [swiftFin_CustomerIndividuals] ci ON c.Id = ci.CustomerId
                    LEFT JOIN [swiftFin_CustomerNonIndividuals] cn ON c.Id = cn.CustomerId
                    WHERE je.JournalId = @JournalId
                    ORDER BY je.CreatedDate DESC";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@JournalId", journalId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        // Simplified version if you don't have all the tables
        public IEnumerable<JournalEntryDTO> GetByJournalIdSimple(Guid journalId)
        {
            var list = new List<JournalEntryDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                // Simpler query without CustomerAccountTypes and TargetProducts
                string query = @"
                    SELECT 
                        je.*,
                        -- Basic journal details
                        j.PrimaryDescription as JournalPrimaryDescription,
                        j.SecondaryDescription as JournalSecondaryDescription,
                        j.Reference as JournalReference,
                        j.TotalValue as JournalTotalValue,
                        j.TransactionCode as JournalTransactionCode,
                        j.CreatedDate as JournalCreatedDate,
                        j.IsLocked as JournalIsLocked,
                        -- Chart of Account details
                        coa.AccountType as ChartOfAccountAccountType,
                        coa.AccountCode as ChartOfAccountAccountCode,
                        coa.AccountName as ChartOfAccountAccountName,
                        -- Contra Chart of Account details
                        ccoa.AccountType as ContraChartOfAccountAccountType,
                        ccoa.AccountCode as ContraChartOfAccountAccountCode,
                        ccoa.AccountName as ContraChartOfAccountAccountName,
                        -- Customer Account basics
                        -- Customer basics
                        c.Reference1 as CustomerAccountCustomerReference1,
                        c.Reference2 as CustomerAccountCustomerReference2,
                        c.Reference3 as CustomerAccountCustomerReference3
                    FROM [swiftFin_JournalEntries] je
                    INNER JOIN [swiftFin_Journals] j ON je.JournalId = j.Id
                    LEFT JOIN [swiftFin_ChartOfAccounts] coa ON je.ChartOfAccountId = coa.Id
                    LEFT JOIN [swiftFin_ChartOfAccounts] ccoa ON je.ContraChartOfAccountId = ccoa.Id
                    LEFT JOIN [swiftFin_CustomerAccounts] ca ON je.CustomerAccountId = ca.Id
                    LEFT JOIN [swiftFin_Customers] c ON ca.CustomerId = c.Id
                    WHERE je.JournalId = @JournalId
                    ORDER BY je.CreatedDate DESC";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@JournalId", journalId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(MapSimple(reader));
                }
            }
            return list;
        }

        // Even simpler version - just the basic fields from journal entries
        public IEnumerable<JournalEntryDTO> GetByJournalIdBasic(Guid journalId)
        {
            var list = new List<JournalEntryDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                // Most basic query
                string query = @"
                    SELECT 
                        je.*,
                        j.Reference as JournalReference,
                        j.PrimaryDescription as JournalPrimaryDescription,
                        coa.AccountName as ChartOfAccountAccountName,
                        ccoa.AccountName as ContraChartOfAccountAccountName
                    FROM [swiftFin_JournalEntries] je
                    LEFT JOIN [swiftFin_Journals] j ON je.JournalId = j.Id
                    LEFT JOIN [swiftFin_ChartOfAccounts] coa ON je.ChartOfAccountId = coa.Id
                    LEFT JOIN [swiftFin_ChartOfAccounts] ccoa ON je.ContraChartOfAccountId = ccoa.Id
                    WHERE je.JournalId = @JournalId
                    ORDER BY je.CreatedDate DESC";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@JournalId", journalId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(MapBasic(reader));
                }
            }
            return list;
        }

        // Get journal entry by ID
        public JournalEntryDTO GetById(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                // Using the simple version
                string query = @"
                    SELECT 
                        je.*,
                        j.PrimaryDescription as JournalPrimaryDescription,
                        j.SecondaryDescription as JournalSecondaryDescription,
                        j.Reference as JournalReference,
                        j.TotalValue as JournalTotalValue,
                        j.TransactionCode as JournalTransactionCode,
                        coa.AccountType as ChartOfAccountAccountType,
                        coa.AccountCode as ChartOfAccountAccountCode,
                        coa.AccountName as ChartOfAccountAccountName,
                        ccoa.AccountType as ContraChartOfAccountAccountType,
                        ccoa.AccountCode as ContraChartOfAccountAccountCode,
                        ccoa.AccountName as ContraChartOfAccountAccountName
                    FROM [swiftFin_JournalEntries] je
                    INNER JOIN [swiftFin_Journals] j ON je.JournalId = j.Id
                    LEFT JOIN [swiftFin_ChartOfAccounts] coa ON je.ChartOfAccountId = coa.Id
                    LEFT JOIN [swiftFin_ChartOfAccounts] ccoa ON je.ContraChartOfAccountId = ccoa.Id
                    WHERE je.Id = @Id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapSimple(reader);
                    }
                }
            }
            return null;
        }

        // Get entry summary for a journal
        public JournalEntrySummaryDTO GetEntrySummary(Guid journalId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT 
                        COUNT(*) as TotalEntries,
                        ISNULL(SUM(Amount), 0) as TotalAmount,
                        ISNULL(MIN(Amount), 0) as MinAmount,
                        ISNULL(MAX(Amount), 0) as MaxAmount,
                        ISNULL(AVG(Amount), 0) as AverageAmount
                    FROM [swiftFin_JournalEntries]
                    WHERE JournalId = @JournalId";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@JournalId", journalId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new JournalEntrySummaryDTO
                            {
                                TotalEntries = Convert.ToInt32(reader["TotalEntries"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                MinAmount = Convert.ToDecimal(reader["MinAmount"]),
                                MaxAmount = Convert.ToDecimal(reader["MaxAmount"]),
                                AverageAmount = Convert.ToDecimal(reader["AverageAmount"])
                            };
                        }
                    }
                }
            }

            return new JournalEntrySummaryDTO();
        }

        // Updated Map method - simplified
        private JournalEntryDTO Map(IDataReader reader)
        {
            var entry = new JournalEntryDTO
            {
                Id = (Guid)reader["Id"],
                JournalId = (Guid)reader["JournalId"],
                SequentialId = (Guid)reader["SequentialId"],
                ChartOfAccountId = (Guid)reader["ChartOfAccountId"],
                ContraChartOfAccountId = (Guid)reader["ContraChartOfAccountId"],
                Amount = Convert.ToDecimal(reader["Amount"]),
                ValueDate = reader["ValueDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ValueDate"]),
                IntegrityHash = reader["IntegrityHash"]?.ToString(),
                CreatedBy = reader["CreatedBy"]?.ToString(),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
            };

            // Set nullable customer account ID
            entry.CustomerAccountId = reader["CustomerAccountId"] == DBNull.Value ? (Guid?)null : (Guid)reader["CustomerAccountId"];

            // Map basic journal details
            entry.JournalParentId = reader["JournalParentId"] == DBNull.Value ? (Guid?)null : (Guid)reader["JournalParentId"];
            entry.JournalPostingPeriodId = reader["JournalPostingPeriodId"] == DBNull.Value ? Guid.Empty : (Guid)reader["JournalPostingPeriodId"];
            entry.JournalBranchId = reader["JournalBranchId"] == DBNull.Value ? Guid.Empty : (Guid)reader["JournalBranchId"];
            entry.JournalTotalValue = reader["JournalTotalValue"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["JournalTotalValue"]);
            entry.JournalPrimaryDescription = reader["JournalPrimaryDescription"]?.ToString();
            entry.JournalSecondaryDescription = reader["JournalSecondaryDescription"]?.ToString();
            entry.JournalReference = reader["JournalReference"]?.ToString();
            entry.JournalTransactionCode = reader["JournalTransactionCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["JournalTransactionCode"]);
            entry.JournalCreatedDate = reader["JournalCreatedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["JournalCreatedDate"]);
            entry.JournalIsLocked = reader["JournalIsLocked"] != DBNull.Value && Convert.ToBoolean(reader["JournalIsLocked"]);

            // Chart of Account details
            entry.ChartOfAccountAccountType = reader["ChartOfAccountAccountType"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ChartOfAccountAccountType"]);
            entry.ChartOfAccountAccountCode = reader["ChartOfAccountAccountCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ChartOfAccountAccountCode"]);
            entry.ChartOfAccountAccountName = reader["ChartOfAccountAccountName"]?.ToString();

            // Contra Chart of Account details
            entry.ContraChartOfAccountAccountType = reader["ContraChartOfAccountAccountType"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ContraChartOfAccountAccountType"]);
            entry.ContraChartOfAccountAccountCode = reader["ContraChartOfAccountAccountCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ContraChartOfAccountAccountCode"]);
            entry.ContraChartOfAccountAccountName = reader["ContraChartOfAccountAccountName"]?.ToString();

            // Customer Account details - only if we have the data
            if (entry.CustomerAccountId.HasValue)
            {
                entry.CustomerAccountCustomerSerialNumber = reader["CustomerAccountCustomerSerialNumber"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["CustomerAccountCustomerSerialNumber"]);

                // Customer details
                entry.CustomerAccountCustomerId = reader["CustomerAccountCustomerId"] == DBNull.Value ? (Guid?)null : (Guid)reader["CustomerAccountCustomerId"];
                entry.CustomerAccountCustomerReference1 = reader["CustomerAccountCustomerReference1"]?.ToString();
                entry.CustomerAccountCustomerReference2 = reader["CustomerAccountCustomerReference2"]?.ToString();
                entry.CustomerAccountCustomerReference3 = reader["CustomerAccountCustomerReference3"]?.ToString();
            }

            // Set defaults for missing fields from CustomerAccountTypes
            entry.CustomerAccountCustomerAccountTypeProductCode = null;
            entry.CustomerAccountCustomerAccountTypeTargetProductId = null;
            entry.CustomerAccountCustomerAccountTypeTargetProductCode = null;
            entry.CustomerAccountAccountTypeTargetProductDescription = null;

            return entry;
        }

        // Simple mapping for basic query
        private JournalEntryDTO MapSimple(IDataReader reader)
        {
            var entry = new JournalEntryDTO
            {
                Id = (Guid)reader["Id"],
                JournalId = (Guid)reader["JournalId"],
                SequentialId = (Guid)reader["SequentialId"],
                ChartOfAccountId = (Guid)reader["ChartOfAccountId"],
                ContraChartOfAccountId = (Guid)reader["ContraChartOfAccountId"],
                Amount = Convert.ToDecimal(reader["Amount"]),
                ValueDate = reader["ValueDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ValueDate"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
            };

            // Set nullable customer account ID
            entry.CustomerAccountId = reader["CustomerAccountId"] == DBNull.Value ? (Guid?)null : (Guid)reader["CustomerAccountId"];

            // Basic journal info
            entry.JournalReference = reader["JournalReference"]?.ToString();
            entry.JournalPrimaryDescription = reader["JournalPrimaryDescription"]?.ToString();
            entry.JournalSecondaryDescription = reader["JournalSecondaryDescription"]?.ToString();
            entry.JournalTransactionCode = reader["JournalTransactionCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["JournalTransactionCode"]);

            // Basic account info
            entry.ChartOfAccountAccountName = reader["ChartOfAccountAccountName"]?.ToString();
            entry.ContraChartOfAccountAccountName = reader["ContraChartOfAccountAccountName"]?.ToString();

            return entry;
        }

        // Basic mapping for simplest query
        private JournalEntryDTO MapBasic(IDataReader reader)
        {
            var entry = new JournalEntryDTO
            {
                Id = (Guid)reader["Id"],
                JournalId = (Guid)reader["JournalId"],
                SequentialId = (Guid)reader["SequentialId"],
                ChartOfAccountId = (Guid)reader["ChartOfAccountId"],
                ContraChartOfAccountId = (Guid)reader["ContraChartOfAccountId"],
                Amount = Convert.ToDecimal(reader["Amount"]),
                ValueDate = reader["ValueDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ValueDate"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
            };

            // Set nullable customer account ID
            entry.CustomerAccountId = reader["CustomerAccountId"] == DBNull.Value ? (Guid?)null : (Guid)reader["CustomerAccountId"];

            // Very basic info
            entry.JournalReference = reader["JournalReference"]?.ToString();
            entry.JournalPrimaryDescription = reader["JournalPrimaryDescription"]?.ToString();
            entry.ChartOfAccountAccountName = reader["ChartOfAccountAccountName"]?.ToString();
            entry.ContraChartOfAccountAccountName = reader["ContraChartOfAccountAccountName"]?.ToString();

            return entry;
        }
    }

    // DTO for journal entry summary
    public class JournalEntrySummaryDTO
    {
        public int TotalEntries { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal AverageAmount { get; set; }
    }
}