using Application.MainBoundedContext.DTO.AccountsModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TestApis.Services
{
    public class ChartOfAccountService
    {
        private readonly string _connectionString;

        public ChartOfAccountService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public IEnumerable<ChartOfAccountDTO> GetAll()
        {
            var list = new List<ChartOfAccountDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT a.*, 
                                p.AccountName as ParentAccountName,
                                c.Description as CostCenterDescription
                                FROM [swiftFin_ChartOfAccounts] a
                                LEFT JOIN [swiftFin_ChartOfAccounts] p ON a.ParentId = p.Id
                                LEFT JOIN [swiftFin_CostCenters] c ON a.CostCenterId = c.Id
                                ORDER BY a.AccountCode";
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

        public ChartOfAccountDTO GetById(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT a.*, 
                                p.AccountName as ParentAccountName,
                                c.Description as CostCenterDescription
                                FROM [swiftFin_ChartOfAccounts] a
                                LEFT JOIN [swiftFin_ChartOfAccounts] p ON a.ParentId = p.Id
                                LEFT JOIN [swiftFin_CostCenters] c ON a.CostCenterId = c.Id
                                WHERE a.Id = @Id";
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

        public ChartOfAccountDTO GetByAccountCode(int accountCode)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT a.*, 
                                p.AccountName as ParentAccountName,
                                c.Description as CostCenterDescription
                                FROM [swiftFin_ChartOfAccounts] a
                                LEFT JOIN [swiftFin_ChartOfAccounts] p ON a.ParentId = p.Id
                                LEFT JOIN [swiftFin_CostCenters] c ON a.CostCenterId = c.Id
                                WHERE a.AccountCode = @AccountCode";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AccountCode", accountCode);
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

        public IEnumerable<ChartOfAccountDTO> GetByAccountType(int accountType)
        {
            var list = new List<ChartOfAccountDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT a.*, 
                                p.AccountName as ParentAccountName,
                                c.Description as CostCenterDescription
                                FROM [swiftFin_ChartOfAccounts] a
                                LEFT JOIN [swiftFin_ChartOfAccounts] p ON a.ParentId = p.Id
                                LEFT JOIN [swiftFin_CostCenters] c ON a.CostCenterId = c.Id
                                WHERE a.AccountType = @AccountType
                                ORDER BY a.AccountCode";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AccountType", accountType);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        public IEnumerable<ChartOfAccountDTO> GetByParentId(Guid? parentId)
        {
            var list = new List<ChartOfAccountDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT a.*, 
                                p.AccountName as ParentAccountName,
                                c.Description as CostCenterDescription
                                FROM [swiftFin_ChartOfAccounts] a
                                LEFT JOIN [swiftFin_ChartOfAccounts] p ON a.ParentId = p.Id
                                LEFT JOIN [swiftFin_CostCenters] c ON a.CostCenterId = c.Id
                                WHERE a.ParentId " + (parentId.HasValue ? "= @ParentId" : "IS NULL") + 
                                " ORDER BY a.AccountCode";
                
                using (var cmd = new SqlCommand(query, conn))
                {
                    if (parentId.HasValue)
                        cmd.Parameters.AddWithValue("@ParentId", parentId.Value);
                    
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        public IEnumerable<ChartOfAccountDTO> Search(string searchQuery)
        {
            var list = new List<ChartOfAccountDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT a.*, 
                                p.AccountName as ParentAccountName,
                                c.Description as CostCenterDescription
                                FROM [swiftFin_ChartOfAccounts] a
                                LEFT JOIN [swiftFin_ChartOfAccounts] p ON a.ParentId = p.Id
                                LEFT JOIN [swiftFin_CostCenters] c ON a.CostCenterId = c.Id
                                WHERE a.AccountName LIKE @SearchQuery 
                                   OR a.AccountCode LIKE @SearchQuery
                                   OR CAST(a.AccountCode AS VARCHAR) LIKE @SearchQuery
                                ORDER BY a.AccountCode";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        public IEnumerable<ChartOfAccountDTO> GetHierarchy()
        {
            var accounts = GetAll();
            return BuildHierarchy(accounts, null);
        }

        private IEnumerable<ChartOfAccountDTO> BuildHierarchy(IEnumerable<ChartOfAccountDTO> accounts, Guid? parentId)
        {
            var result = new List<ChartOfAccountDTO>();

            foreach (var account in accounts)
            {
                if ((account.ParentId == null && parentId == null) ||
                    (account.ParentId != null && account.ParentId.Value == parentId))
                {
                    var children = BuildHierarchy(accounts, account.Id);

                    ((HashSet<ChartOfAccountDTO>)account.Children).Clear();

                    foreach (var child in children)
                    {
                        ((HashSet<ChartOfAccountDTO>)account.Children).Add(child);
                    }

                    result.Add(account);
                }
            }

            return result;
        }

        public int GenerateAccountCode(Guid? parentId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                // If parentId is null, generate root level account code
                if (parentId == null)
                {
                    string query = @"SELECT ISNULL(MAX(AccountCode), 0) 
                                    FROM [swiftFin_ChartOfAccounts] 
                                    WHERE ParentId IS NULL 
                                    AND AccountCode < 1000";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        conn.Open();
                        var result = cmd.ExecuteScalar();
                        int maxCode = result == DBNull.Value ? 0 : Convert.ToInt32(result);
                        return maxCode + 1;
                    }
                }
                else
                {
                    // Get parent account code and depth
                    string getParentQuery = @"SELECT AccountCode, Depth FROM [swiftFin_ChartOfAccounts] WHERE Id = @ParentId";
                    int parentCode = 0;
                    int parentDepth = 0;
                    
                    using (var cmd = new SqlCommand(getParentQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ParentId", parentId.Value);
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                parentCode = Convert.ToInt32(reader["AccountCode"]);
                                parentDepth = Convert.ToInt32(reader["Depth"]);
                            }
                        }
                    }

                    // Generate child account code based on parent
                    string getMaxChildQuery = @"SELECT ISNULL(MAX(AccountCode), 0) 
                                               FROM [swiftFin_ChartOfAccounts] 
                                               WHERE ParentId = @ParentId
                                               AND AccountCode >= @ParentCode * 10
                                               AND AccountCode < (@ParentCode + 1) * 10";
                    
                    using (var cmd = new SqlCommand(getMaxChildQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ParentId", parentId.Value);
                        cmd.Parameters.AddWithValue("@ParentCode", parentCode);
                        
                        var result = cmd.ExecuteScalar();
                        int maxChildCode = result == DBNull.Value ? 0 : Convert.ToInt32(result);
                        
                        if (maxChildCode == 0)
                            return parentCode * 10 + 1;
                        else
                            return maxChildCode + 1;
                    }
                }
            }
        }

        public ChartOfAccountDTO Create(ChartOfAccountDTO account)
        {
            // Validate account code uniqueness
            var existingByCode = GetByAccountCode(account.AccountCode);
            if (existingByCode != null)
            {
                throw new InvalidOperationException($"Account code {account.AccountCode} already exists.");
            }

            // Generate account code if not provided
            if (account.AccountCode == 0)
            {
                account.AccountCode = GenerateAccountCode(account.ParentId);
            }

            // Calculate depth
            if (account.ParentId == null)
            {
                account.Depth = 0;
            }
            else
            {
                var parent = GetById(account.ParentId.Value);
                if (parent == null)
                    throw new InvalidOperationException("Parent account not found.");
                
                account.Depth = parent.Depth + 1;
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                // Generate new Guid if not provided
                if (account.Id == Guid.Empty)
                    account.Id = Guid.NewGuid();

                account.CreatedDate = DateTime.Now;

                string query = @"INSERT INTO [swiftFin_ChartOfAccounts] 
                                ([Id], [ParentId], [CostCenterId], [AccountType], 
                                 [AccountCategory], [AccountCode], [AccountName], 
                                 [Depth], [IsControlAccount], [IsReconciliationAccount], 
                                 [PostAutomaticallyOnly], [IsLocked], [CreatedDate])
                                VALUES 
                                (@Id, @ParentId, @CostCenterId, @AccountType, 
                                 @AccountCategory, @AccountCode, @AccountName, 
                                 @Depth, @IsControlAccount, @IsReconciliationAccount, 
                                 @PostAutomaticallyOnly, @IsLocked, @CreatedDate)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, account);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return GetById(account.Id);
        }

        public void Update(ChartOfAccountDTO account)
        {
            // Check if account exists
            var existing = GetById(account.Id);
            if (existing == null)
                throw new KeyNotFoundException("Account not found.");

            // Validate account code uniqueness (excluding current account)
            var existingByCode = GetByAccountCode(account.AccountCode);
            if (existingByCode != null && existingByCode.Id != account.Id)
            {
                throw new InvalidOperationException($"Account code {account.AccountCode} already exists.");
            }

            // Check if trying to make an account its own parent
            if (account.ParentId == account.Id)
            {
                throw new InvalidOperationException("An account cannot be its own parent.");
            }

            // Check for circular reference
            if (account.ParentId.HasValue)
            {
                if (IsCircularReference(account.Id, account.ParentId.Value))
                {
                    throw new InvalidOperationException("Circular reference detected. Cannot set this parent.");
                }
            }

            // Recalculate depth if parent changed
            if (existing.ParentId != account.ParentId)
            {
                if (account.ParentId == null)
                {
                    account.Depth = 0;
                }
                else
                {
                    var parent = GetById(account.ParentId.Value);
                    if (parent == null)
                        throw new InvalidOperationException("Parent account not found.");
                    
                    account.Depth = parent.Depth + 1;
                }
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE [swiftFin_ChartOfAccounts] 
                                SET [ParentId] = @ParentId,
                                    [CostCenterId] = @CostCenterId,
                                    [AccountType] = @AccountType,
                                    [AccountCategory] = @AccountCategory,
                                    [AccountCode] = @AccountCode,
                                    [AccountName] = @AccountName,
                                    [Depth] = @Depth,
                                    [IsControlAccount] = @IsControlAccount,
                                    [IsReconciliationAccount] = @IsReconciliationAccount,
                                    [PostAutomaticallyOnly] = @PostAutomaticallyOnly,
                                    [IsLocked] = @IsLocked
                                WHERE [Id] = @Id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, account);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            // Update depths of children if parent changed
            if (existing.ParentId != account.ParentId || existing.Depth != account.Depth)
            {
                UpdateChildrenDepth(account.Id, account.Depth + 1);
            }
        }

        private bool IsCircularReference(Guid accountId, Guid parentId)
        {
            var currentParentId = parentId;
            while (currentParentId != null)
            {
                if (currentParentId == accountId)
                    return true;
                
                var parentAccount = GetById(currentParentId);
                if (parentAccount == null || parentAccount.ParentId == null)
                    break;
                
                currentParentId = parentAccount.ParentId.Value;
            }
            return false;
        }

        private void UpdateChildrenDepth(Guid parentId, int depth)
        {
            var children = GetByParentId(parentId);
            foreach (var child in children)
            {
                child.Depth = depth;
                
                using (var conn = new SqlConnection(_connectionString))
                {
                    string query = @"UPDATE [swiftFin_ChartOfAccounts] 
                                    SET Depth = @Depth 
                                    WHERE Id = @Id";
                    
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Depth", depth);
                        cmd.Parameters.AddWithValue("@Id", child.Id);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                
                // Recursively update grandchildren
                UpdateChildrenDepth(child.Id, depth + 1);
            }
        }

        public void Delete(Guid id)
        {
            // Check if account exists
            var account = GetById(id);
            if (account == null)
                throw new KeyNotFoundException("Account not found.");

            // Check if account has children
            var children = GetByParentId(id);
            if (children != null && children.GetEnumerator().MoveNext())
            {
                throw new InvalidOperationException("Cannot delete account that has child accounts.");
            }

            // Check if account is locked
            if (account.IsLocked)
            {
                throw new InvalidOperationException("Cannot delete a locked account.");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM [swiftFin_ChartOfAccounts] WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public IEnumerable<ChartOfAccountDTO> GetControlAccounts()
        {
            var list = new List<ChartOfAccountDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT a.*, 
                                p.AccountName as ParentAccountName,
                                c.Description as CostCenterDescription
                                FROM [swiftFin_ChartOfAccounts] a
                                LEFT JOIN [swiftFin_ChartOfAccounts] p ON a.ParentId = p.Id
                                LEFT JOIN [swiftFin_CostCenters] c ON a.CostCenterId = c.Id
                                WHERE a.IsControlAccount = 1
                                ORDER BY a.AccountCode";
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

        public IEnumerable<ChartOfAccountDTO> GetReconciliationAccounts()
        {
            var list = new List<ChartOfAccountDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT a.*, 
                                p.AccountName as ParentAccountName,
                                c.Description as CostCenterDescription
                                FROM [swiftFin_ChartOfAccounts] a
                                LEFT JOIN [swiftFin_ChartOfAccounts] p ON a.ParentId = p.Id
                                LEFT JOIN [swiftFin_CostCenters] c ON a.CostCenterId = c.Id
                                WHERE a.IsReconciliationAccount = 1
                                ORDER BY a.AccountCode";
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

        private void AddParams(SqlCommand cmd, ChartOfAccountDTO account)
        {
            cmd.Parameters.AddWithValue("@Id", account.Id);
            cmd.Parameters.AddWithValue("@ParentId", account.ParentId.HasValue ? (object)account.ParentId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@CostCenterId", account.CostCenterId.HasValue ? (object)account.CostCenterId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@AccountType", account.AccountType);
            cmd.Parameters.AddWithValue("@AccountCategory", account.AccountCategory);
            cmd.Parameters.AddWithValue("@AccountCode", account.AccountCode);
            cmd.Parameters.AddWithValue("@AccountName", account.AccountName ?? "");
            cmd.Parameters.AddWithValue("@Depth", account.Depth);
            cmd.Parameters.AddWithValue("@IsControlAccount", account.IsControlAccount);
            cmd.Parameters.AddWithValue("@IsReconciliationAccount", account.IsReconciliationAccount);
            cmd.Parameters.AddWithValue("@PostAutomaticallyOnly", account.PostAutomaticallyOnly);
            cmd.Parameters.AddWithValue("@IsLocked", account.IsLocked);
            cmd.Parameters.AddWithValue("@CreatedDate", account.CreatedDate);
        }

        private ChartOfAccountDTO Map(IDataReader reader)
        {
            return new ChartOfAccountDTO
            {
                Id = (Guid)reader["Id"],
                ParentId = reader["ParentId"] == DBNull.Value ? (Guid?)null : (Guid)reader["ParentId"],
                ParentAccountName = reader["ParentAccountName"]?.ToString(),
                CostCenterId = reader["CostCenterId"] == DBNull.Value ? (Guid?)null : (Guid)reader["CostCenterId"],
                CostCenterDescription = reader["CostCenterDescription"]?.ToString(),
                AccountType = Convert.ToInt32(reader["AccountType"]),
                AccountCategory = Convert.ToInt32(reader["AccountCategory"]),
                AccountCode = Convert.ToInt32(reader["AccountCode"]),
                AccountName = reader["AccountName"]?.ToString(),
                Depth = Convert.ToInt32(reader["Depth"]),
                IsControlAccount = Convert.ToBoolean(reader["IsControlAccount"]),
                IsReconciliationAccount = Convert.ToBoolean(reader["IsReconciliationAccount"]),
                PostAutomaticallyOnly = Convert.ToBoolean(reader["PostAutomaticallyOnly"]),
                IsLocked = Convert.ToBoolean(reader["IsLocked"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
            };
        }
    }
}