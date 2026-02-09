using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class LocationService
    {
        private readonly string _connectionString;

        public LocationService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get all locations
        public List<Location> GetAll()
        {
            var list = new List<Location>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, Code, Description, SequentialId, CreatedBy, CreatedDate 
                                 FROM swiftFin_Location";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Location
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Code = reader["Code"].ToString(),
                        Description = reader["Description"].ToString(),
                        SequentialId = reader.GetGuid(reader.GetOrdinal("SequentialId")),
                        CreatedBy = reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    });
                }
            }

            return list;
        }

        // Get location by Id
        public Location GetById(Guid id)
        {
            Location location = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, Code, Description, SequentialId, CreatedBy, CreatedDate 
                                 FROM swiftFin_Location WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();

                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    location = new Location
                    {
                        Id = (Guid)reader["Id"],
                        Code = reader["Code"].ToString(),
                        Description = reader["Description"].ToString(),
                        SequentialId = reader.GetGuid(reader.GetOrdinal("SequentialId")),
                        CreatedBy = reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                }
            }

            return location;
        }

        // Add new location
        public void Add(Location location)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO swiftFin_Location
                                (Id, Code, Description, CreatedBy, CreatedDate)
                                VALUES (@Id, @Code, @Description, @CreatedBy, @CreatedDate)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", location.Id);
                cmd.Parameters.AddWithValue("@Code", location.Code);
                cmd.Parameters.AddWithValue("@Description", location.Description);
                cmd.Parameters.AddWithValue("@CreatedBy", location.CreatedBy ?? "System");
                cmd.Parameters.AddWithValue("@CreatedDate", location.CreatedDate);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Update location
        public void Update(Location location)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE swiftFin_Location
                                 SET Code=@Code, Description=@Description
                                 WHERE Id=@Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", location.Id);
                cmd.Parameters.AddWithValue("@Code", location.Code);
                cmd.Parameters.AddWithValue("@Description", location.Description);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Delete location
        public void Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM swiftFin_Location WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
