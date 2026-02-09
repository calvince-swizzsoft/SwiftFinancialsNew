using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class FALocationService
    {
        private readonly string _connectionString;

        public FALocationService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get all Locations
        public List<FALocation> GetAll()
        {
            var list = new List<FALocation>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, Code, Description, IsLocked, CreatedDate, CreatedBy
                                 FROM FALocation";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new FALocation
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Code = reader["Code"].ToString(),
                        Description = reader["Description"].ToString(),
                        IsLocked = Convert.ToBoolean(reader["IsLocked"]),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        CreatedBy = reader["CreatedBy"].ToString()
                    });
                }
            }

            return list;
        }

        // Get by Id
        public FALocation GetById(Guid id)
        {
            FALocation location = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, Code, Description, IsLocked, CreatedDate, CreatedBy
                                 FROM FALocation
                                 WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    location = new FALocation
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Code = reader["Code"].ToString(),
                        Description = reader["Description"].ToString(),
                        IsLocked = Convert.ToBoolean(reader["IsLocked"]),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        CreatedBy = reader["CreatedBy"].ToString()
                    };
                }
            }

            return location;
        }

        // Add
        public void Add(FALocation location)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO FALocation
                                (Id, Code, Description, IsLocked, CreatedDate, CreatedBy)
                                 VALUES
                                (@Id, @Code, @Description, @IsLocked, @CreatedDate, @CreatedBy)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", location.Id);
                cmd.Parameters.AddWithValue("@Code", location.Code ?? "");
                cmd.Parameters.AddWithValue("@Description", location.Description ?? "");
                cmd.Parameters.AddWithValue("@IsLocked", location.IsLocked);
                cmd.Parameters.AddWithValue("@CreatedDate", location.CreatedDate);
                cmd.Parameters.AddWithValue("@CreatedBy", location.CreatedBy ?? "System");

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Update
        public bool Update(FALocation location)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE FALocation 
                                 SET Code=@Code, Description=@Description, IsLocked=@IsLocked
                                 WHERE Id=@Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", location.Id);
                cmd.Parameters.AddWithValue("@Code", location.Code ?? "");
                cmd.Parameters.AddWithValue("@Description", location.Description ?? "");
                cmd.Parameters.AddWithValue("@IsLocked", location.IsLocked);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // Delete
        public bool Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"DELETE FROM FALocation WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
