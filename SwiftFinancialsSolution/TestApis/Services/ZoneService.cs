using Application.MainBoundedContext.DTO.RegistryModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TestApis.Services
{
    public class ZoneService
    {
        private readonly string _connectionString;

        public ZoneService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public bool DivisionExists(Guid divisionId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(1) FROM [swiftFin_Divisions] WHERE Id = @DivisionId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DivisionId", divisionId);
                    conn.Open();
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public bool HasStations(Guid zoneId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(1) FROM [swiftFin_Stations] WHERE ZoneId = @ZoneId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ZoneId", zoneId);
                    conn.Open();
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public IEnumerable<ZoneDTO> GetAll()
        {
            var list = new List<ZoneDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT z.*, 
                                        d.Description as DivisionDescription, 
                                        e.Id as DivisionEmployerId,
                                        e.Description as DivisionEmployerDescription
                                FROM [swiftFin_Zones] z
                                LEFT JOIN [swiftFin_Divisions] d ON z.DivisionId = d.Id
                                LEFT JOIN [swiftFin_Employers] e ON d.EmployerId = e.Id
                                ORDER BY z.Description";
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

        public ZoneDTO GetById(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT z.*, 
                                        d.Description as DivisionDescription, 
                                        e.Id as DivisionEmployerId,
                                        e.Description as DivisionEmployerDescription
                                FROM [swiftFin_Zones] z
                                LEFT JOIN [swiftFin_Divisions] d ON z.DivisionId = d.Id
                                LEFT JOIN [swiftFin_Employers] e ON d.EmployerId = e.Id
                                WHERE z.Id = @Id";
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

        public ZoneDTO GetWithStations(Guid id)
        {
            var zone = GetById(id);
            if (zone != null)
            {
                zone.Stations = GetStationsByZoneId(id) as IList<StationDTO>;
            }
            return zone;
        }

        public IEnumerable<ZoneDTO> GetByDivisionId(Guid divisionId)
        {
            var list = new List<ZoneDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT z.*, 
                                        d.Description as DivisionDescription, 
                                        e.Id as DivisionEmployerId,
                                        e.Description as DivisionEmployerDescription
                                FROM [swiftFin_Zones] z
                                LEFT JOIN [swiftFin_Divisions] d ON z.DivisionId = d.Id
                                LEFT JOIN [swiftFin_Employers] e ON d.EmployerId = e.Id
                                WHERE z.DivisionId = @DivisionId
                                ORDER BY z.Description";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DivisionId", divisionId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        public IEnumerable<ZoneDTO> GetByEmployerId(Guid employerId)
        {
            var list = new List<ZoneDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT z.*, 
                                        d.Description as DivisionDescription, 
                                        e.Id as DivisionEmployerId,
                                        e.Description as DivisionEmployerDescription
                                FROM [swiftFin_Zones] z
                                LEFT JOIN [swiftFin_Divisions] d ON z.DivisionId = d.Id
                                LEFT JOIN [swiftFin_Employers] e ON d.EmployerId = e.Id
                                WHERE d.EmployerId = @EmployerId
                                ORDER BY z.Description";
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

        public IEnumerable<ZoneDTO> GetByName(string name)
        {
            var list = new List<ZoneDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT z.*, 
                                        d.Description as DivisionDescription, 
                                        e.Id as DivisionEmployerId,
                                        e.Description as DivisionEmployerDescription
                                FROM [swiftFin_Zones] z
                                LEFT JOIN [swiftFin_Divisions] d ON z.DivisionId = d.Id
                                LEFT JOIN [swiftFin_Employers] e ON d.EmployerId = e.Id
                                WHERE z.Description LIKE @Name
                                ORDER BY z.Description";
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

        private IEnumerable<StationDTO> GetStationsByZoneId(Guid zoneId)
        {
            var list = new List<StationDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT s.*, 
                                        z.Description as ZoneDescription,
                                        d.EmployerId as ZoneDivisionEmployerId
                                FROM [swiftFin_Stations] s
                                LEFT JOIN [swiftFin_Zones] z ON s.ZoneId = z.Id
                                LEFT JOIN [swiftFin_Divisions] d ON z.DivisionId = d.Id
                                WHERE s.ZoneId = @ZoneId
                                ORDER BY s.Description";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ZoneId", zoneId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(MapStation(reader));
                }
            }
            return list;
        }

        public ZoneDTO Create(ZoneDTO zone)
        {
            // Validate DivisionId exists
            if (!DivisionExists(zone.DivisionId))
            {
                throw new Exception("Division does not exist");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                // Generate new Guid if not provided
                if (zone.Id == Guid.Empty)
                    zone.Id = Guid.NewGuid();

                zone.CreatedDate = DateTime.Now;

                string query = @"INSERT INTO [swiftFin_Zones] 
                                ([Id], [DivisionId], [Description], [CreatedDate])
                                VALUES 
                                (@Id, @DivisionId, @Description, @CreatedDate)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, zone);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            // Return the created zone with division and employer details
            return GetById(zone.Id);
        }

        public void Update(ZoneDTO zone)
        {
            // Validate DivisionId exists if being changed
            if (!DivisionExists(zone.DivisionId))
            {
                throw new Exception("Division does not exist");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE [swiftFin_Zones] 
                                SET [DivisionId] = @DivisionId,
                                    [Description] = @Description
                                WHERE [Id] = @Id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, zone);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM [swiftFin_Zones] WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void AddParams(SqlCommand cmd, ZoneDTO zone)
        {
            cmd.Parameters.AddWithValue("@Id", zone.Id);
            cmd.Parameters.AddWithValue("@DivisionId", zone.DivisionId);
            cmd.Parameters.AddWithValue("@Description", zone.Description ?? "");
            cmd.Parameters.AddWithValue("@CreatedDate", zone.CreatedDate);
        }

        private ZoneDTO Map(IDataReader reader)
        {
            return new ZoneDTO
            {
                Id = (Guid)reader["Id"],
                DivisionId = (Guid)reader["DivisionId"],
                DivisionDescription = reader["DivisionDescription"]?.ToString(),
                DivisionEmployerId = reader["DivisionEmployerId"] == DBNull.Value ? Guid.Empty : (Guid)reader["DivisionEmployerId"],
                DivisionEmployerDescription = reader["DivisionEmployerDescription"]?.ToString(),
                Description = reader["Description"]?.ToString(),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                Stations = new List<StationDTO>() // Initialize empty list
            };
        }

        private StationDTO MapStation(IDataReader reader)
        {
            return new StationDTO
            {
                Id = (Guid)reader["Id"],
                ZoneId = (Guid)reader["ZoneId"],
                ZoneDescription = reader["ZoneDescription"]?.ToString(),
                ZoneDivisionId = reader["ZoneDivisionId"] == DBNull.Value ? Guid.Empty : (Guid)reader["ZoneDivisionId"],
                ZoneDivisionEmployerId = reader["ZoneDivisionEmployerId"] == DBNull.Value ? Guid.Empty : (Guid)reader["ZoneDivisionEmployerId"],
                Description = reader["Description"]?.ToString(),
                AddressAddressLine1 = reader["Address_AddressLine1"]?.ToString(),
                AddressAddressLine2 = reader["Address_AddressLine2"]?.ToString(),
                AddressStreet = reader["Address_Street"]?.ToString(),
                AddressPostalCode = reader["Address_PostalCode"]?.ToString(),
                AddressCity = reader["Address_City"]?.ToString(),
                AddressEmail = reader["Address_Email"]?.ToString(),
                AddressLandLine = reader["Address_LandLine"]?.ToString(),
                AddressMobileLine = reader["Address_MobileLine"]?.ToString(),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
            };
        }
    }
}