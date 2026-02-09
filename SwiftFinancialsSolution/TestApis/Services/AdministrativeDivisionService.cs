using Application.MainBoundedContext.DTO.RegistryModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TestApis.Services
{
    public class AdministrativeDivisionService
    {
        private readonly string _connectionString;

        public AdministrativeDivisionService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public bool ParentExists(Guid parentId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(1) FROM [swiftFin_AdministrativeDivisions] WHERE Id = @ParentId AND IsLocked = 0";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ParentId", parentId);
                    conn.Open();
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public bool WouldCreateCircularReference(Guid divisionId, Guid proposedParentId)
        {
            // Check if the proposed parent is already a child of this division
            if (divisionId == proposedParentId)
                return true;

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"WITH DivisionHierarchy AS (
                                    SELECT Id, ParentId
                                    FROM [swiftFin_AdministrativeDivisions]
                                    WHERE Id = @ProposedParentId
                                    UNION ALL
                                    SELECT ad.Id, ad.ParentId
                                    FROM [swiftFin_AdministrativeDivisions] ad
                                    INNER JOIN DivisionHierarchy dh ON ad.ParentId = dh.Id
                                )
                                SELECT COUNT(1) FROM DivisionHierarchy WHERE Id = @DivisionId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DivisionId", divisionId);
                    cmd.Parameters.AddWithValue("@ProposedParentId", proposedParentId);
                    conn.Open();
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public bool HasChildren(Guid divisionId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(1) FROM [swiftFin_AdministrativeDivisions] WHERE ParentId = @DivisionId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DivisionId", divisionId);
                    conn.Open();
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public IEnumerable<AdministrativeDivisionDTO> GetAll()
        {
            var list = new List<AdministrativeDivisionDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT ad.*, parent.Description as ParentDescription
                                FROM [swiftFin_AdministrativeDivisions] ad
                                LEFT JOIN [swiftFin_AdministrativeDivisions] parent ON ad.ParentId = parent.Id
                                ORDER BY ad.Depth, ad.Description";
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

        public AdministrativeDivisionDTO GetById(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT ad.*, parent.Description as ParentDescription
                                FROM [swiftFin_AdministrativeDivisions] ad
                                LEFT JOIN [swiftFin_AdministrativeDivisions] parent ON ad.ParentId = parent.Id
                                WHERE ad.Id = @Id";
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

        public IEnumerable<AdministrativeDivisionDTO> GetByType(int type)
        {
            var list = new List<AdministrativeDivisionDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT ad.*, parent.Description as ParentDescription
                                FROM [swiftFin_AdministrativeDivisions] ad
                                LEFT JOIN [swiftFin_AdministrativeDivisions] parent ON ad.ParentId = parent.Id
                                WHERE ad.Type = @Type
                                ORDER BY ad.Depth, ad.Description";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Type", type);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        public IEnumerable<AdministrativeDivisionDTO> GetByParentId(Guid parentId)
        {
            var list = new List<AdministrativeDivisionDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT ad.*, parent.Description as ParentDescription
                                FROM [swiftFin_AdministrativeDivisions] ad
                                LEFT JOIN [swiftFin_AdministrativeDivisions] parent ON ad.ParentId = parent.Id
                                WHERE ad.ParentId = @ParentId
                                ORDER BY ad.Description";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ParentId", parentId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        public IEnumerable<AdministrativeDivisionDTO> GetRootDivisions()
        {
            var list = new List<AdministrativeDivisionDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT ad.*, parent.Description as ParentDescription
                                FROM [swiftFin_AdministrativeDivisions] ad
                                LEFT JOIN [swiftFin_AdministrativeDivisions] parent ON ad.ParentId = parent.Id
                                WHERE ad.ParentId IS NULL
                                ORDER BY ad.Description";
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

        public IEnumerable<AdministrativeDivisionDTO> GetByName(string name)
        {
            var list = new List<AdministrativeDivisionDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT ad.*, parent.Description as ParentDescription
                                FROM [swiftFin_AdministrativeDivisions] ad
                                LEFT JOIN [swiftFin_AdministrativeDivisions] parent ON ad.ParentId = parent.Id
                                WHERE ad.Description LIKE @Name
                                ORDER BY ad.Depth, ad.Description";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", "%" + name + "%");
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        public IEnumerable<AdministrativeDivisionDTO> Search(string searchQuery)
        {
            var list = new List<AdministrativeDivisionDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT ad.*, parent.Description as ParentDescription
                                FROM [swiftFin_AdministrativeDivisions] ad
                                LEFT JOIN [swiftFin_AdministrativeDivisions] parent ON ad.ParentId = parent.Id
                                WHERE ad.Description LIKE @SearchQuery OR ad.Remarks LIKE @SearchQuery
                                ORDER BY ad.Depth, ad.Description";
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

        public IEnumerable<AdministrativeDivisionDTO> GetHierarchy()
        {
            var divisions = new Dictionary<Guid, AdministrativeDivisionDTO>();
            var rootDivisions = new List<AdministrativeDivisionDTO>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT ad.*, parent.Description as ParentDescription
                                FROM [swiftFin_AdministrativeDivisions] ad
                                LEFT JOIN [swiftFin_AdministrativeDivisions] parent ON ad.ParentId = parent.Id
                                ORDER BY ad.Depth";
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var division = Map(reader);
                            divisions[division.Id] = division;
                        }
                    }
                }

                // Build hierarchy
                foreach (var division in divisions.Values)
                {
                    if (division.ParentId.HasValue && divisions.ContainsKey(division.ParentId.Value))
                    {
                        var parent = divisions[division.ParentId.Value];
                        ((HashSet<AdministrativeDivisionDTO>)parent.Children).Add(division);
                    }
                    else
                    {
                        rootDivisions.Add(division);
                    }
                }
            }

            return rootDivisions;
        }

        public AdministrativeDivisionDTO Create(AdministrativeDivisionDTO division)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                // Generate new Guid if not provided
                if (division.Id == Guid.Empty)
                    division.Id = Guid.NewGuid();

                // Calculate depth
                division.Depth = CalculateDepth(division.ParentId);

                division.CreatedDate = DateTime.Now;

                string query = @"INSERT INTO [swiftFin_AdministrativeDivisions] 
                                ([Id], [ParentId], [Description], [Depth], [Type], [Remarks], [IsLocked], [CreatedBy], [CreatedDate])
                                VALUES 
                                (@Id, @ParentId, @Description, @Depth, @Type, @Remarks, @IsLocked, @CreatedBy, @CreatedDate)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, division);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            // Return the created division with parent details
            return GetById(division.Id);
        }

        public void Update(AdministrativeDivisionDTO division)
        {
            // Calculate new depth if parent changed
            var existingDivision = GetById(division.Id);
            if (existingDivision != null && existingDivision.ParentId != division.ParentId)
            {
                division.Depth = CalculateDepth(division.ParentId);
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE [swiftFin_AdministrativeDivisions] 
                                SET [ParentId] = @ParentId,
                                    [Description] = @Description,
                                    [Depth] = @Depth,
                                    [Type] = @Type,
                                    [Remarks] = @Remarks,
                                    [IsLocked] = @IsLocked
                                WHERE [Id] = @Id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, division);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            // Update depths of children if parent changed
            if (existingDivision != null && existingDivision.ParentId != division.ParentId)
            {
                UpdateChildrenDepths(division.Id, division.Depth + 1);
            }
        }

        public void Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM [swiftFin_AdministrativeDivisions] WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private int CalculateDepth(Guid? parentId)
        {
            if (!parentId.HasValue)
                return 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT Depth FROM [swiftFin_AdministrativeDivisions] WHERE Id = @ParentId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ParentId", parentId.Value);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) + 1 : 0;
                }
            }
        }

        private void UpdateChildrenDepths(Guid parentId, int newDepth)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE [swiftFin_AdministrativeDivisions] 
                                SET Depth = @NewDepth
                                WHERE ParentId = @ParentId;

                                -- Recursively update children of children
                                UPDATE child
                                SET child.Depth = parent.Depth + 1
                                FROM [swiftFin_AdministrativeDivisions] child
                                INNER JOIN [swiftFin_AdministrativeDivisions] parent ON child.ParentId = parent.Id
                                WHERE parent.ParentId = @ParentId";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ParentId", parentId);
                    cmd.Parameters.AddWithValue("@NewDepth", newDepth);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void AddParams(SqlCommand cmd, AdministrativeDivisionDTO division)
        {
            cmd.Parameters.AddWithValue("@Id", division.Id);
            cmd.Parameters.AddWithValue("@ParentId", division.ParentId.HasValue ? (object)division.ParentId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", division.Description ?? "");
            cmd.Parameters.AddWithValue("@Depth", division.Depth);
            cmd.Parameters.AddWithValue("@Type", division.Type);
            cmd.Parameters.AddWithValue("@Remarks", division.Remarks ?? "");
            cmd.Parameters.AddWithValue("@IsLocked", division.IsLocked);
            cmd.Parameters.AddWithValue("@CreatedBy", division.CreatedBy ?? "");
            cmd.Parameters.AddWithValue("@CreatedDate", division.CreatedDate);
        }

        private AdministrativeDivisionDTO Map(IDataReader reader)
        {
            return new AdministrativeDivisionDTO
            {
                Id = (Guid)reader["Id"],
                ParentId = reader["ParentId"] == DBNull.Value ? (Guid?)null : (Guid)reader["ParentId"],
                Description = reader["Description"]?.ToString(),
                Depth = Convert.ToInt32(reader["Depth"]),
                Type = Convert.ToByte(reader["Type"]),
                Remarks = reader["Remarks"]?.ToString(),
                IsLocked = Convert.ToBoolean(reader["IsLocked"]),
                CreatedBy = reader["CreatedBy"]?.ToString(),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
            };
        }
    }
}