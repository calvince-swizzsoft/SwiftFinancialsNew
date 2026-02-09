using Application.MainBoundedContext.DTO.RegistryModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TestApis.Services
{
    public class DivisionService
    {
        private readonly string _connectionString;

        public DivisionService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public bool EmployerExists(Guid employerId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(1) FROM [swiftFin_Employers] WHERE Id = @EmployerId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployerId", employerId);
                    conn.Open();
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public bool HasZones(Guid divisionId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(1) FROM [swiftFin_Zones] WHERE DivisionId = @DivisionId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DivisionId", divisionId);
                    conn.Open();
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public IEnumerable<DivisionDTO> GetAll()
        {
            var list = new List<DivisionDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT d.*, e.Description as EmployerDescription
                                FROM [swiftFin_Divisions] d
                                LEFT JOIN [swiftFin_Employers] e ON d.EmployerId = e.Id
                                ORDER BY d.Description";
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

        public DivisionDTO GetById(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT d.*, e.Description as EmployerDescription
                                FROM [swiftFin_Divisions] d
                                LEFT JOIN [swiftFin_Employers] e ON d.EmployerId = e.Id
                                WHERE d.Id = @Id";
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

        public DivisionDTO GetWithZones(Guid id)
        {
            var division = GetById(id);
            if (division != null)
            {
                // You can extend this to include zones if needed
                // division.Zones = GetZonesByDivisionId(id);
            }
            return division;
        }

        public IEnumerable<DivisionDTO> GetByEmployerId(Guid employerId)
        {
            var list = new List<DivisionDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT d.*, e.Description as EmployerDescription
                                FROM [swiftFin_Divisions] d
                                LEFT JOIN [swiftFin_Employers] e ON d.EmployerId = e.Id
                                WHERE d.EmployerId = @EmployerId
                                ORDER BY d.Description";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployerId", employerId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        public IEnumerable<DivisionDTO> GetByName(string name)
        {
            var list = new List<DivisionDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT d.*, e.Description as EmployerDescription
                                FROM [swiftFin_Divisions] d
                                LEFT JOIN [swiftFin_Employers] e ON d.EmployerId = e.Id
                                WHERE d.Description LIKE @Name
                                ORDER BY d.Description";
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

        private IEnumerable<ZoneDTO> GetZonesByDivisionId(Guid divisionId)
        {
            var list = new List<ZoneDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT z.*, d.Description as DivisionDescription, 
                                        de.Description as DivisionEmployerDescription
                                FROM [swiftFin_Zones] z
                                LEFT JOIN [swiftFin_Divisions] d ON z.DivisionId = d.Id
                                LEFT JOIN [swiftFin_Employers] de ON z.DivisionEmployerId = de.Id
                                WHERE z.DivisionId = @DivisionId
                                ORDER BY z.Description";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DivisionId", divisionId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(MapZone(reader));
                }
            }
            return list;
        }

        public DivisionDTO Create(DivisionDTO division)
        {
            // Validate EmployerId exists
            if (!EmployerExists(division.EmployerId))
            {
                throw new Exception("Employer does not exist");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                // Generate new Guid if not provided
                if (division.Id == Guid.Empty)
                    division.Id = Guid.NewGuid();

                division.CreatedDate = DateTime.Now;

                string query = @"INSERT INTO [swiftFin_Divisions] 
                                ([Id], [EmployerId], [Description], [CreatedDate])
                                VALUES 
                                (@Id, @EmployerId, @Description, @CreatedDate)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, division);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            // Return the created division with employer details
            return GetById(division.Id);
        }

        public void Update(DivisionDTO division)
        {
            // Validate EmployerId exists if being changed
            if (!EmployerExists(division.EmployerId))
            {
                throw new Exception("Employer does not exist");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE [swiftFin_Divisions] 
                                SET [EmployerId] = @EmployerId,
                                    [Description] = @Description
                                WHERE [Id] = @Id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, division);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM [swiftFin_Divisions] WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void AddParams(SqlCommand cmd, DivisionDTO division)
        {
            cmd.Parameters.AddWithValue("@Id", division.Id);
            cmd.Parameters.AddWithValue("@EmployerId", division.EmployerId);
            cmd.Parameters.AddWithValue("@Description", division.Description ?? "");
            cmd.Parameters.AddWithValue("@CreatedDate", division.CreatedDate);
        }

        private DivisionDTO Map(IDataReader reader)
        {
            return new DivisionDTO
            {
                Id = (Guid)reader["Id"],
                EmployerId = (Guid)reader["EmployerId"],
                EmployerDescription = reader["EmployerDescription"]?.ToString(),
                Description = reader["Description"]?.ToString(),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
            };
        }

        private ZoneDTO MapZone(IDataReader reader)
        {
            return new ZoneDTO
            {
                Id = (Guid)reader["Id"],
                DivisionId = (Guid)reader["DivisionId"],
                DivisionDescription = reader["DivisionDescription"]?.ToString(),
                DivisionEmployerId = reader["DivisionEmployerId"] == DBNull.Value ? Guid.Empty : (Guid)reader["DivisionEmployerId"],
                DivisionEmployerDescription = reader["DivisionEmployerDescription"]?.ToString(),
                Description = reader["Description"]?.ToString(),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
            };
        }
    }
}