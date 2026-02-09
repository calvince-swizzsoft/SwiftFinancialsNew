using Application.MainBoundedContext.DTO.AccountsModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TestApis.Services
{
    public class CustomerReceiptService
    {
        private readonly string _connectionString;

        public CustomerReceiptService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get all customer receipts (journals)
        public IEnumerable<JournalDTO> GetAll()
        {
            var list = new List<JournalDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT j.*, 
                                b.Description as BranchDescription,
                                b.Address_Email as BranchAddressEmail,
                                b.CompanyId as BranchCompanyId,
                                c.Description as BranchCompanyDescription,
                                p.Description as PostingPeriodDescription
                                FROM [swiftFin_Journals] j
                                LEFT JOIN [swiftFin_Branches] b ON j.BranchId = b.Id
                                LEFT JOIN [swiftFin_Companies] c ON b.CompanyId = c.Id
                                LEFT JOIN [swiftFin_PostingPeriods] p ON j.PostingPeriodId = p.Id
                                WHERE j.TransactionCode = 2 -- Assuming 2 is Customer Receipt Transaction Code
                                ORDER BY j.CreatedDate DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        // Get receipt by ID
        public JournalDTO GetById(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT j.*, 
                                b.Description as BranchDescription,
                                b.Address_Email as BranchAddressEmail,
                                b.CompanyId as BranchCompanyId,
                                c.Description as BranchCompanyDescription,
                                p.Description as PostingPeriodDescription
                                FROM [swiftFin_Journals] j
                                LEFT JOIN [swiftFin_Branches] b ON j.BranchId = b.Id
                                LEFT JOIN [swiftFin_Companies] c ON b.CompanyId = c.Id
                                LEFT JOIN [swiftFin_PostingPeriods] p ON j.PostingPeriodId = p.Id
                                WHERE j.Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return Map(reader);
                    }
                }
            }
            return null;
        }

        // Get receipts by customer reference (Reference1, Reference2, Reference3 from customers)
        public IEnumerable<JournalDTO> GetByCustomerReference(string customerReference)
        {
            var list = new List<JournalDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT j.*, 
                                b.Description as BranchDescription,
                                b.Address_Email as BranchAddressEmail,
                                b.CompanyId as BranchCompanyId,
                                c.Description as BranchCompanyDescription,
                                p.Description as PostingPeriodDescription
                                FROM [swiftFin_Journals] j
                                LEFT JOIN [swiftFin_Branches] b ON j.BranchId = b.Id
                                LEFT JOIN [swiftFin_Companies] c ON b.CompanyId = c.Id
                                LEFT JOIN [swiftFin_PostingPeriods] p ON j.PostingPeriodId = p.Id
                                WHERE j.TransactionCode = 2 -- Customer Receipt Transaction Code
                                AND (j.Reference LIKE @CustomerReference 
                                     OR j.PrimaryDescription LIKE @CustomerReference 
                                     OR j.SecondaryDescription LIKE @CustomerReference)
                                ORDER BY j.CreatedDate DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerReference", "%" + customerReference + "%");
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        // Get receipts by customer ID (via reference mapping)
        public IEnumerable<JournalDTO> GetByCustomerId(Guid customerId)
        {
            var list = new List<JournalDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                // First get customer references
                string customerQuery = @"SELECT Reference1, Reference2, Reference3 
                                       FROM [swiftFin_Customers] 
                                       WHERE Id = @CustomerId";
                List<string> customerReferences = new List<string>();

                using (var cmd = new SqlCommand(customerQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerId", customerId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (!string.IsNullOrEmpty(reader["Reference1"]?.ToString()))
                                customerReferences.Add(reader["Reference1"].ToString());
                            if (!string.IsNullOrEmpty(reader["Reference2"]?.ToString()))
                                customerReferences.Add(reader["Reference2"].ToString());
                            if (!string.IsNullOrEmpty(reader["Reference3"]?.ToString()))
                                customerReferences.Add(reader["Reference3"].ToString());
                        }
                    }
                }

                if (customerReferences.Count == 0)
                    return list;

                // Now get receipts for these references
                conn.Close();
                string receiptQuery = @"SELECT j.*, 
                                       b.Description as BranchDescription,
                                       b.Address_Email as BranchAddressEmail,
                                       b.CompanyId as BranchCompanyId,
                                       c.Description as BranchCompanyDescription,
                                       p.Description as PostingPeriodDescription
                                       FROM [swiftFin_Journals] j
                                       LEFT JOIN [swiftFin_Branches] b ON j.BranchId = b.Id
                                       LEFT JOIN [swiftFin_Companies] c ON b.CompanyId = c.Id
                                       LEFT JOIN [swiftFin_PostingPeriods] p ON j.PostingPeriodId = p.Id
                                       WHERE j.TransactionCode = 2 -- Customer Receipt Transaction Code
                                       AND (";

                // Build OR conditions for each reference
                for (int i = 0; i < customerReferences.Count; i++)
                {
                    if (i > 0) receiptQuery += " OR ";
                    receiptQuery += $"j.Reference = @Ref{i}";
                }
                receiptQuery += ") ORDER BY j.CreatedDate DESC";

                using (var cmd = new SqlCommand(receiptQuery, conn))
                {
                    for (int i = 0; i < customerReferences.Count; i++)
                    {
                        cmd.Parameters.AddWithValue($"@Ref{i}", customerReferences[i]);
                    }

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        // Get receipts by date range
        public IEnumerable<JournalDTO> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            var list = new List<JournalDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT j.*, 
                                b.Description as BranchDescription,
                                b.Address_Email as BranchAddressEmail,
                                b.CompanyId as BranchCompanyId,
                                c.Description as BranchCompanyDescription,
                                p.Description as PostingPeriodDescription
                                FROM [swiftFin_Journals] j
                                LEFT JOIN [swiftFin_Branches] b ON j.BranchId = b.Id
                                LEFT JOIN [swiftFin_Companies] c ON b.CompanyId = c.Id
                                LEFT JOIN [swiftFin_PostingPeriods] p ON j.PostingPeriodId = p.Id
                                WHERE j.TransactionCode = 2 -- Customer Receipt Transaction Code
                                AND j.CreatedDate >= @StartDate 
                                AND j.CreatedDate <= @EndDate
                                ORDER BY j.CreatedDate DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        // Get receipts by branch
        public IEnumerable<JournalDTO> GetByBranchId(Guid branchId)
        {
            var list = new List<JournalDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT j.*, 
                                b.Description as BranchDescription,
                                b.Address_Email as BranchAddressEmail,
                                b.CompanyId as BranchCompanyId,
                                c.Description as BranchCompanyDescription,
                                p.Description as PostingPeriodDescription
                                FROM [swiftFin_Journals] j
                                LEFT JOIN [swiftFin_Branches] b ON j.BranchId = b.Id
                                LEFT JOIN [swiftFin_Companies] c ON b.CompanyId = c.Id
                                LEFT JOIN [swiftFin_PostingPeriods] p ON j.PostingPeriodId = p.Id
                                WHERE j.TransactionCode = 2 -- Customer Receipt Transaction Code
                                AND j.BranchId = @BranchId
                                ORDER BY j.CreatedDate DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BranchId", branchId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        // Search receipts by multiple criteria
        public IEnumerable<JournalDTO> Search(string searchQuery, DateTime? startDate = null, DateTime? endDate = null, Guid? branchId = null)
        {
            var list = new List<JournalDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT j.*, 
                                b.Description as BranchDescription,
                                b.Address_Email as BranchAddressEmail,
                                b.CompanyId as BranchCompanyId,
                                c.Description as BranchCompanyDescription,
                                p.Description as PostingPeriodDescription
                                FROM [swiftFin_Journals] j
                                LEFT JOIN [swiftFin_Branches] b ON j.BranchId = b.Id
                                LEFT JOIN [swiftFin_Companies] c ON b.CompanyId = c.Id
                                LEFT JOIN [swiftFin_PostingPeriods] p ON j.PostingPeriodId = p.Id
                                WHERE 1=1"; // Start with always true condition

                // Add TransactionCode filter for Customer Receipts
                // Note: You might need to adjust this based on your actual TransactionCode values
                query += " AND j.TransactionCode = 2"; // Assuming 4 is Customer Receipt

                // Add search conditions
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query += @" AND (j.Reference LIKE @SearchQuery 
                                  OR j.PrimaryDescription LIKE @SearchQuery 
                                  OR j.SecondaryDescription LIKE @SearchQuery
                                  OR j.ApplicationUserName LIKE @SearchQuery)";
                }

                if (startDate.HasValue)
                {
                    query += " AND j.CreatedDate >= @StartDate";
                }

                if (endDate.HasValue)
                {
                    query += " AND j.CreatedDate <= @EndDate";
                }

                if (branchId.HasValue)
                {
                    query += " AND j.BranchId = @BranchId";
                }

                query += " ORDER BY j.CreatedDate DESC";

                using (var cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(searchQuery))
                    {
                        cmd.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");
                    }

                    if (startDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@StartDate", startDate.Value);
                    }

                    if (endDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@EndDate", endDate.Value);
                    }

                    if (branchId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@BranchId", branchId.Value);
                    }

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        // Get receipt summary statistics
        public ReceiptSummaryDTO GetReceiptSummary(Guid? branchId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT 
                                COUNT(*) as TotalReceipts,
                                ISNULL(SUM(TotalValue), 0) as TotalAmount,
                                MIN(CreatedDate) as FirstReceiptDate,
                                MAX(CreatedDate) as LastReceiptDate
                                FROM [swiftFin_Journals]
                                WHERE TransactionCode = 2"; // Assuming 4 is Customer Receipt

                if (branchId.HasValue)
                {
                    query += " AND BranchId = @BranchId";
                }

                if (startDate.HasValue)
                {
                    query += " AND CreatedDate >= @StartDate";
                }

                if (endDate.HasValue)
                {
                    query += " AND CreatedDate <= @EndDate";
                }

                using (var cmd = new SqlCommand(query, conn))
                {
                    if (branchId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@BranchId", branchId.Value);
                    }

                    if (startDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@StartDate", startDate.Value);
                    }

                    if (endDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@EndDate", endDate.Value);
                    }

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ReceiptSummaryDTO
                            {
                                TotalReceipts = Convert.ToInt32(reader["TotalReceipts"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                FirstReceiptDate = reader["FirstReceiptDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FirstReceiptDate"]),
                                LastReceiptDate = reader["LastReceiptDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastReceiptDate"])
                            };
                        }
                    }
                }
            }

            return new ReceiptSummaryDTO();
        }

        // Get receipts with pagination
        public IEnumerable<JournalDTO> GetAllWithPagination(int page = 1, int pageSize = 20)
        {
            var list = new List<JournalDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT j.*, 
                                b.Description as BranchDescription,
                                b.Address_Email as BranchAddressEmail,
                                b.CompanyId as BranchCompanyId,
                                c.Description as BranchCompanyDescription,
                                p.Description as PostingPeriodDescription
                                FROM [swiftFin_Journals] j
                                LEFT JOIN [swiftFin_Branches] b ON j.BranchId = b.Id
                                LEFT JOIN [swiftFin_Companies] c ON b.CompanyId = c.Id
                                LEFT JOIN [swiftFin_PostingPeriods] p ON j.PostingPeriodId = p.Id
                                WHERE j.TransactionCode = 2 -- Customer Receipt Transaction Code
                                ORDER BY j.CreatedDate DESC
                                OFFSET @Offset ROWS
                                FETCH NEXT @PageSize ROWS ONLY";

                using (var cmd = new SqlCommand(query, conn))
                {
                    int offset = (page - 1) * pageSize;
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        // Get total count of receipts
        public int GetTotalCount()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT COUNT(*) 
                                FROM [swiftFin_Journals] 
                                WHERE TransactionCode = 2"; // Assuming 4 is Customer Receipt
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        // Create a new receipt (journal)
        public JournalDTO Create(JournalDTO receipt)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                // Generate new Guid if not provided
                if (receipt.Id == Guid.Empty)
                    receipt.Id = Guid.NewGuid();

                // Generate SequentialId if not provided
                if (receipt.SequentialId == Guid.Empty)
                    receipt.SequentialId = Guid.NewGuid();

                receipt.CreatedDate = DateTime.Now;

                // IMPORTANT: The table doesn't have a Type column, so we need to adjust the INSERT query
                string query = @"INSERT INTO [swiftFin_Journals] 
                                ([Id], [ParentId], [PostingPeriodId], [BranchId], 
                                 [AlternateChannelLogId], [TotalValue], [PrimaryDescription], 
                                 [SecondaryDescription], [Reference], [ApplicationUserName], 
                                 [EnvironmentUserName], [EnvironmentMachineName], [EnvironmentDomainName], 
                                 [EnvironmentOSVersion], [EnvironmentMACAddress], [EnvironmentMotherboardSerialNumber], 
                                 [EnvironmentProcessorId], [EnvironmentIPAddress], [ModuleNavigationItemCode], 
                                 [TransactionCode], [ValueDate], [SuppressAccountAlert], [IsLocked], 
                                 [IntegrityHash], [SequentialId], [CreatedBy], [CreatedDate])
                                VALUES 
                                (@Id, @ParentId, @PostingPeriodId, @BranchId, 
                                 @AlternateChannelLogId, @TotalValue, @PrimaryDescription, 
                                 @SecondaryDescription, @Reference, @ApplicationUserName, 
                                 @EnvironmentUserName, @EnvironmentMachineName, @EnvironmentDomainName, 
                                 @EnvironmentOSVersion, @EnvironmentMACAddress, @EnvironmentMotherboardSerialNumber, 
                                 @EnvironmentProcessorId, @EnvironmentIPAddress, @ModuleNavigationItemCode, 
                                 @TransactionCode, @ValueDate, @SuppressAccountAlert, @IsLocked, 
                                 @IntegrityHash, @SequentialId, @CreatedBy, @CreatedDate)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, receipt);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return GetById(receipt.Id);
        }

        // Update receipt (only if not locked/reversed)
        public void Update(JournalDTO receipt)
        {
            // Check if receipt is locked (reversed)
            var existing = GetById(receipt.Id);
            if (existing != null && existing.IsLocked)
            {
                throw new InvalidOperationException("Cannot update a locked/reversed receipt.");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE [swiftFin_Journals] 
                                SET [ParentId] = @ParentId,
                                    [PostingPeriodId] = @PostingPeriodId,
                                    [BranchId] = @BranchId,
                                    [AlternateChannelLogId] = @AlternateChannelLogId,
                                    [TotalValue] = @TotalValue,
                                    [PrimaryDescription] = @PrimaryDescription,
                                    [SecondaryDescription] = @SecondaryDescription,
                                    [Reference] = @Reference,
                                    [ApplicationUserName] = @ApplicationUserName,
                                    [EnvironmentUserName] = @EnvironmentUserName,
                                    [EnvironmentMachineName] = @EnvironmentMachineName,
                                    [EnvironmentDomainName] = @EnvironmentDomainName,
                                    [EnvironmentOSVersion] = @EnvironmentOSVersion,
                                    [EnvironmentMACAddress] = @EnvironmentMACAddress,
                                    [EnvironmentMotherboardSerialNumber] = @EnvironmentMotherboardSerialNumber,
                                    [EnvironmentProcessorId] = @EnvironmentProcessorId,
                                    [EnvironmentIPAddress] = @EnvironmentIPAddress,
                                    [ModuleNavigationItemCode] = @ModuleNavigationItemCode,
                                    [TransactionCode] = @TransactionCode,
                                    [ValueDate] = @ValueDate,
                                    [SuppressAccountAlert] = @SuppressAccountAlert,
                                    [IntegrityHash] = @IntegrityHash,
                                    [SequentialId] = @SequentialId
                                WHERE [Id] = @Id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, receipt);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Delete receipt (only if not locked/reversed)
        public void Delete(Guid id)
        {
            // Check if receipt is locked (reversed)
            var existing = GetById(id);
            if (existing != null && existing.IsLocked)
            {
                throw new InvalidOperationException("Cannot delete a locked/reversed receipt.");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM [swiftFin_Journals] WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Lock/Reverse a receipt
        public void LockReceipt(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "UPDATE [swiftFin_Journals] SET IsLocked = 1 WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Unlock a receipt
        public void UnlockReceipt(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "UPDATE [swiftFin_Journals] SET IsLocked = 0 WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void AddParams(SqlCommand cmd, JournalDTO receipt)
        {
            cmd.Parameters.AddWithValue("@Id", receipt.Id);
            cmd.Parameters.AddWithValue("@ParentId", receipt.ParentId.HasValue ? (object)receipt.ParentId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@PostingPeriodId", receipt.PostingPeriodId);
            cmd.Parameters.AddWithValue("@BranchId", receipt.BranchId);
            cmd.Parameters.AddWithValue("@AlternateChannelLogId", receipt.AlternateChannelLogId.HasValue ? (object)receipt.AlternateChannelLogId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@TotalValue", receipt.TotalValue);
            cmd.Parameters.AddWithValue("@PrimaryDescription", receipt.PrimaryDescription ?? "");
            cmd.Parameters.AddWithValue("@SecondaryDescription", receipt.SecondaryDescription ?? "");
            cmd.Parameters.AddWithValue("@Reference", receipt.Reference ?? "");
            cmd.Parameters.AddWithValue("@ApplicationUserName", receipt.ApplicationUserName ?? "");
            cmd.Parameters.AddWithValue("@EnvironmentUserName", receipt.EnvironmentUserName ?? "");
            cmd.Parameters.AddWithValue("@EnvironmentMachineName", receipt.EnvironmentMachineName ?? "");
            cmd.Parameters.AddWithValue("@EnvironmentDomainName", receipt.EnvironmentDomainName ?? "");
            cmd.Parameters.AddWithValue("@EnvironmentOSVersion", receipt.EnvironmentOSVersion ?? "");
            cmd.Parameters.AddWithValue("@EnvironmentMACAddress", receipt.EnvironmentMACAddress ?? "");
            cmd.Parameters.AddWithValue("@EnvironmentMotherboardSerialNumber", receipt.EnvironmentMotherboardSerialNumber ?? "");
            cmd.Parameters.AddWithValue("@EnvironmentProcessorId", receipt.EnvironmentProcessorId ?? "");
            cmd.Parameters.AddWithValue("@EnvironmentIPAddress", receipt.EnvironmentIPAddress ?? "");
            cmd.Parameters.AddWithValue("@ModuleNavigationItemCode", receipt.ModuleNavigationItemCode);
            cmd.Parameters.AddWithValue("@TransactionCode", receipt.TransactionCode);
            cmd.Parameters.AddWithValue("@ValueDate", receipt.ValueDate.HasValue ? (object)receipt.ValueDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@SuppressAccountAlert", receipt.SuppressAccountAlert);
            cmd.Parameters.AddWithValue("@IsLocked", receipt.IsLocked);
            cmd.Parameters.AddWithValue("@IntegrityHash", receipt.IntegrityHash ?? "");
            cmd.Parameters.AddWithValue("@SequentialId", receipt.SequentialId);
            cmd.Parameters.AddWithValue("@CreatedBy", receipt.CreatedBy ?? "");
            cmd.Parameters.AddWithValue("@CreatedDate", receipt.CreatedDate);
        }

        private JournalDTO Map(IDataReader reader)
        {
            return new JournalDTO
            {
                Id = (Guid)reader["Id"],
                ParentId = reader["ParentId"] == DBNull.Value ? (Guid?)null : (Guid)reader["ParentId"],
                PostingPeriodId = (Guid)reader["PostingPeriodId"],
                BranchId = (Guid)reader["BranchId"],
                AlternateChannelLogId = reader["AlternateChannelLogId"] == DBNull.Value ? (Guid?)null : (Guid)reader["AlternateChannelLogId"],
                TotalValue = Convert.ToDecimal(reader["TotalValue"]),
                PrimaryDescription = reader["PrimaryDescription"]?.ToString(),
                SecondaryDescription = reader["SecondaryDescription"]?.ToString(),
                Reference = reader["Reference"]?.ToString(),
                ApplicationUserName = reader["ApplicationUserName"]?.ToString(),
                EnvironmentUserName = reader["EnvironmentUserName"]?.ToString(),
                EnvironmentMachineName = reader["EnvironmentMachineName"]?.ToString(),
                EnvironmentDomainName = reader["EnvironmentDomainName"]?.ToString(),
                EnvironmentOSVersion = reader["EnvironmentOSVersion"]?.ToString(),
                EnvironmentMACAddress = reader["EnvironmentMACAddress"]?.ToString(),
                EnvironmentMotherboardSerialNumber = reader["EnvironmentMotherboardSerialNumber"]?.ToString(),
                EnvironmentProcessorId = reader["EnvironmentProcessorId"]?.ToString(),
                EnvironmentIPAddress = reader["EnvironmentIPAddress"]?.ToString(),
                ModuleNavigationItemCode = Convert.ToInt32(reader["ModuleNavigationItemCode"]),
                TransactionCode = Convert.ToInt32(reader["TransactionCode"]),
                ValueDate = reader["ValueDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ValueDate"]),
                SuppressAccountAlert = Convert.ToBoolean(reader["SuppressAccountAlert"]),
                IsLocked = Convert.ToBoolean(reader["IsLocked"]),
                IntegrityHash = reader["IntegrityHash"]?.ToString(),
                SequentialId = (Guid)reader["SequentialId"],
                CreatedBy = reader["CreatedBy"]?.ToString(),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),

                // Note: Type property doesn't exist in your table
                // Type = Convert.ToByte(reader["Type"]), // REMOVED

                // Set Type based on TransactionCode (if needed)
                Type = GetTypeFromTransactionCode(Convert.ToInt32(reader["TransactionCode"])),

                // Joined fields
                BranchDescription = reader["BranchDescription"]?.ToString(),
                BranchAddressEmail = reader["BranchAddressEmail"]?.ToString(),
                BranchCompanyId = reader["BranchCompanyId"] == DBNull.Value ? Guid.Empty : (Guid)reader["BranchCompanyId"],
                BranchCompanyDescription = reader["BranchCompanyDescription"]?.ToString(),
                PostingPeriodDescription = reader["PostingPeriodDescription"]?.ToString()
            };
        }

        // Helper method to determine Type from TransactionCode
        private byte GetTypeFromTransactionCode(int transactionCode)
        {
            // This is a mapping based on your business logic
            // You need to adjust this based on your actual TransactionCode values
            switch (transactionCode)
            {
                case 2: // Assuming 4 is Customer Receipt
                    return 2; // JournalVoucherType.CustomerReceipt
                // Add other mappings as needed
                default:
                    return 0; // Unknown type
            }
        }
    }

    // DTO for receipt summary
    public class ReceiptSummaryDTO
    {
        public int TotalReceipts { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime? FirstReceiptDate { get; set; }
        public DateTime? LastReceiptDate { get; set; }
    }
}