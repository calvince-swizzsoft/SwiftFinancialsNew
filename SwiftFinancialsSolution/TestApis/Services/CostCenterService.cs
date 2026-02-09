using Application.MainBoundedContext.DTO.AccountsModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TestApis.Services
{
    public class CostCenterService
    {
        private readonly string _connectionString;

        public CostCenterService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public IEnumerable<CostCenterDTO> GetAll()
        {
            var list = new List<CostCenterDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT * FROM [swiftFin_CostCenters] ORDER BY Description";
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

        public CostCenterDTO GetById(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT * FROM [swiftFin_CostCenters] WHERE Id = @Id";
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

        public CostCenterDTO GetByName(string name)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT * FROM [swiftFin_CostCenters] WHERE Description = @Description";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Description", name);
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

        public IEnumerable<CostCenterDTO> Search(string searchQuery)
        {
            var list = new List<CostCenterDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT * FROM [swiftFin_CostCenters] 
                                WHERE Description LIKE @SearchQuery
                                ORDER BY Description";
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

        public CostCenterDTO Create(CostCenterDTO costCenter)
        {
            // Check if cost center with same name already exists
            var existing = GetByName(costCenter.Description);
            if (existing != null)
            {
                throw new InvalidOperationException($"Cost center with name '{costCenter.Description}' already exists.");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                // Generate new Guid if not provided
                if (costCenter.Id == Guid.Empty)
                    costCenter.Id = Guid.NewGuid();

                costCenter.CreatedDate = DateTime.Now;

                string query = @"INSERT INTO [swiftFin_CostCenters] 
                                ([Id], [Description], [IsLocked], [CreatedDate])
                                VALUES 
                                (@Id, @Description, @IsLocked, @CreatedDate)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, costCenter);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return GetById(costCenter.Id);
        }

        public void Update(CostCenterDTO costCenter)
        {
            // Check if another cost center with same name already exists (excluding current one)
            var existingByName = GetByName(costCenter.Description);
            if (existingByName != null && existingByName.Id != costCenter.Id)
            {
                throw new InvalidOperationException($"Another cost center with name '{costCenter.Description}' already exists.");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE [swiftFin_CostCenters] 
                                SET [Description] = @Description,
                                    [IsLocked] = @IsLocked
                                WHERE [Id] = @Id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, costCenter);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(Guid id)
        {
            // Check if cost center is being used before deletion
            if (IsCostCenterInUse(id))
            {
                throw new InvalidOperationException("Cannot delete cost center because it is being used by other records.");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM [swiftFin_CostCenters] WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool IsCostCenterInUse(Guid costCenterId)
        {
            // Check if cost center is referenced in other tables
            // You'll need to add checks for your specific tables
            // For example:
            // - swiftFin_Accounts
            // - swiftFin_Transactions
            // - swiftFin_Budgets
            // etc.

            using (var conn = new SqlConnection(_connectionString))
            {
                // Example: Check if used in accounts table
                string query = @"SELECT COUNT(*) FROM [swiftFin_Accounts] WHERE CostCenterId = @CostCenterId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CostCenterId", costCenterId);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result) > 0;
                }
            }
        }

        public int GetUsageCount(Guid costCenterId)
        {
            // Get total count of references to this cost center
            int totalCount = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                // List of tables that reference cost centers
                var referenceTables = new[]
                {
                    "swiftFin_Accounts",
                    "swiftFin_Transactions",
                    "swiftFin_Budgets",
                    "swiftFin_JournalEntries"
                };

                foreach (var table in referenceTables)
                {
                    try
                    {
                        string query = $"SELECT COUNT(*) FROM [{table}] WHERE CostCenterId = @CostCenterId";
                        using (var cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@CostCenterId", costCenterId);
                            if (conn.State != ConnectionState.Open)
                                conn.Open();

                            var result = cmd.ExecuteScalar();
                            totalCount += Convert.ToInt32(result);
                        }
                    }
                    catch
                    {
                        // Table might not exist or might not have CostCenterId column
                        // Continue with next table
                    }
                }
            }

            return totalCount;
        }

        private void AddParams(SqlCommand cmd, CostCenterDTO costCenter)
        {
            cmd.Parameters.AddWithValue("@Id", costCenter.Id);
            cmd.Parameters.AddWithValue("@Description", costCenter.Description ?? "");
            cmd.Parameters.AddWithValue("@IsLocked", costCenter.IsLocked);
            cmd.Parameters.AddWithValue("@CreatedDate", costCenter.CreatedDate);
        }

        private CostCenterDTO Map(IDataReader reader)
        {
            return new CostCenterDTO
            {
                Id = (Guid)reader["Id"],
                Description = reader["Description"]?.ToString(),
                IsLocked = Convert.ToBoolean(reader["IsLocked"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
            };
        }
    }

    public class CostCenterUsageDTO
    {
        public Guid CostCenterId { get; set; }
        public string CostCenterName { get; set; }
        public int UsageCount { get; set; }
        public bool IsLocked { get; set; }
        public bool CanDelete { get { return UsageCount == 0 && !IsLocked; } }
    }
}