using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class FAClassService
    {
        private readonly string _connectionString;

        public FAClassService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get all FA classes
        public List<FAClass> GetAll()
        {
            var list = new List<FAClass>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, Code, Description, IsLocked, CreatedDate, CreatedBy
                                 FROM FAClass";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new FAClass
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

        // Get FAClass by Id
        public FAClass GetById(Guid id)
        {
            FAClass faClass = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, Code, Description, IsLocked, CreatedDate, CreatedBy
                                 FROM FAClass WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    faClass = new FAClass
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

            return faClass;
        }

        // Add new FAClass
        public void Add(FAClass faClass)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO FAClass
                                (Id, Code, Description, IsLocked, CreatedDate, CreatedBy)
                                 VALUES
                                (@Id, @Code, @Description, @IsLocked, @CreatedDate, @CreatedBy)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", faClass.Id);
                cmd.Parameters.AddWithValue("@Code", faClass.Code ?? "");
                cmd.Parameters.AddWithValue("@Description", faClass.Description ?? "");
                cmd.Parameters.AddWithValue("@IsLocked", faClass.IsLocked);
                cmd.Parameters.AddWithValue("@CreatedDate", faClass.CreatedDate);
                cmd.Parameters.AddWithValue("@CreatedBy", faClass.CreatedBy ?? "System");

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Update FAClass
        public bool Update(FAClass faClass)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE FAClass 
                                 SET Code=@Code, Description=@Description, IsLocked=@IsLocked
                                 WHERE Id=@Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", faClass.Id);
                cmd.Parameters.AddWithValue("@Code", faClass.Code ?? "");
                cmd.Parameters.AddWithValue("@Description", faClass.Description ?? "");
                cmd.Parameters.AddWithValue("@IsLocked", faClass.IsLocked);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // Delete FAClass
        public bool Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"DELETE FROM FAClass WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
