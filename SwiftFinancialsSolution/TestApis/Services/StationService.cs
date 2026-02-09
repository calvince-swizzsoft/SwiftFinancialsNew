using Application.MainBoundedContext.DTO.RegistryModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TestApis.Services
{
    public class StationService
    {
        private readonly string _connectionString;

        public StationService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public bool ZoneExists(Guid zoneId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(1) FROM [swiftFin_Zones] WHERE Id = @ZoneId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ZoneId", zoneId);
                    conn.Open();
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public IEnumerable<StationDTO> GetAll()
        {
            var list = new List<StationDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT s.*, z.Description as ZoneDescription
                                FROM [swiftFin_Stations] s
                                LEFT JOIN [swiftFin_Zones] z ON s.ZoneId = z.Id
                                ORDER BY s.Description";
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

        public StationDTO GetById(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT s.*, z.Description as ZoneDescription
                                FROM [swiftFin_Stations] s
                                LEFT JOIN [swiftFin_Zones] z ON s.ZoneId = z.Id
                                WHERE s.Id = @Id";
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

        public IEnumerable<StationDTO> GetByZoneId(Guid zoneId)
        {
            var list = new List<StationDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT s.*, z.Description as ZoneDescription
                                FROM [swiftFin_Stations] s
                                LEFT JOIN [swiftFin_Zones] z ON s.ZoneId = z.Id
                                WHERE s.ZoneId = @ZoneId
                                ORDER BY s.Description";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ZoneId", zoneId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        public IEnumerable<StationDTO> GetByName(string name)
        {
            var list = new List<StationDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT s.*, z.Description as ZoneDescription
                                FROM [swiftFin_Stations] s
                                LEFT JOIN [swiftFin_Zones] z ON s.ZoneId = z.Id
                                WHERE s.Description LIKE @Name
                                ORDER BY s.Description";
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

        public StationDTO Create(StationDTO station)
        {
            // Validate ZoneId exists
            if (!ZoneExists(station.ZoneId))
            {
                throw new Exception("Zone does not exist");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                // Generate new Guid if not provided
                if (station.Id == Guid.Empty)
                    station.Id = Guid.NewGuid();

                station.CreatedDate = DateTime.Now;

                // Note: The table doesn't have ZoneDivisionId or ZoneDivisionEmployerId columns
                string query = @"INSERT INTO [swiftFin_Stations] 
                                ([Id], [ZoneId], [Description], 
                                 [Address_AddressLine1], [Address_AddressLine2], 
                                 [Address_Street], [Address_PostalCode], [Address_City], 
                                 [Address_Email], [Address_LandLine], [Address_MobileLine], 
                                 [CreatedDate])
                                VALUES 
                                (@Id, @ZoneId, @Description, 
                                 @Address_AddressLine1, @Address_AddressLine2, 
                                 @Address_Street, @Address_PostalCode, @Address_City, 
                                 @Address_Email, @Address_LandLine, @Address_MobileLine, 
                                 @CreatedDate)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, station);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            // Return the created station with zone details
            return GetById(station.Id);
        }

        public void Update(StationDTO station)
        {
            // Validate ZoneId exists if being changed
            if (!ZoneExists(station.ZoneId))
            {
                throw new Exception("Zone does not exist");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                // Note: The table doesn't have ZoneDivisionId or ZoneDivisionEmployerId columns
                string query = @"UPDATE [swiftFin_Stations] 
                                SET [ZoneId] = @ZoneId,
                                    [Description] = @Description,
                                    [Address_AddressLine1] = @Address_AddressLine1,
                                    [Address_AddressLine2] = @Address_AddressLine2,
                                    [Address_Street] = @Address_Street,
                                    [Address_PostalCode] = @Address_PostalCode,
                                    [Address_City] = @Address_City,
                                    [Address_Email] = @Address_Email,
                                    [Address_LandLine] = @Address_LandLine,
                                    [Address_MobileLine] = @Address_MobileLine
                                WHERE [Id] = @Id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, station);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM [swiftFin_Stations] WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void AddParams(SqlCommand cmd, StationDTO station)
        {
            cmd.Parameters.AddWithValue("@Id", station.Id);
            cmd.Parameters.AddWithValue("@ZoneId", station.ZoneId);
            cmd.Parameters.AddWithValue("@Description", station.Description ?? "");
            cmd.Parameters.AddWithValue("@Address_AddressLine1", station.AddressAddressLine1 ?? "");
            cmd.Parameters.AddWithValue("@Address_AddressLine2", station.AddressAddressLine2 ?? "");
            cmd.Parameters.AddWithValue("@Address_Street", station.AddressStreet ?? "");
            cmd.Parameters.AddWithValue("@Address_PostalCode", station.AddressPostalCode ?? "");
            cmd.Parameters.AddWithValue("@Address_City", station.AddressCity ?? "");
            cmd.Parameters.AddWithValue("@Address_Email", station.AddressEmail ?? "");
            cmd.Parameters.AddWithValue("@Address_LandLine", station.AddressLandLine ?? "");
            cmd.Parameters.AddWithValue("@Address_MobileLine", station.AddressMobileLine ?? "");
            cmd.Parameters.AddWithValue("@CreatedDate", station.CreatedDate);
        }

        private StationDTO Map(IDataReader reader)
        {
            return new StationDTO
            {
                Id = (Guid)reader["Id"],
                ZoneId = (Guid)reader["ZoneId"],
                ZoneDescription = reader["ZoneDescription"]?.ToString(),
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