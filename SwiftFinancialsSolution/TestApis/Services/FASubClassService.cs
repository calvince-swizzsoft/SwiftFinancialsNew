using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class FASubClassService
    {
        private readonly string _connectionString;

        public FASubClassService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get all SubClasses
        public List<FASubClass> GetAll()
        {
            var list = new List<FASubClass>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT sc.Id, sc.Code, sc.Description, sc.IsLocked, sc.FAClassId,
                                        c.Description AS FAClassDescription, sc.CreatedDate, sc.CreatedBy
                                 FROM FASubClass sc
                                 INNER JOIN FAClass c ON sc.FAClassId = c.Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new FASubClass
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Code = reader["Code"].ToString(),
                        Description = reader["Description"].ToString(),
                        IsLocked = Convert.ToBoolean(reader["IsLocked"]),
                        FAClassId = reader.GetGuid(reader.GetOrdinal("FAClassId")),
                        FAClassDescription = reader["FAClassDescription"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        CreatedBy = reader["CreatedBy"].ToString()
                    });
                }
            }

            return list;
        }

        // Get by Id
        public FASubClass GetById(Guid id)
        {
            FASubClass subClass = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT sc.Id, sc.Code, sc.Description, sc.IsLocked, sc.FAClassId,
                                        c.Description AS FAClassDescription, sc.CreatedDate, sc.CreatedBy
                                 FROM FASubClass sc
                                 INNER JOIN FAClass c ON sc.FAClassId = c.Id
                                 WHERE sc.Id = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    subClass = new FASubClass
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Code = reader["Code"].ToString(),
                        Description = reader["Description"].ToString(),
                        IsLocked = Convert.ToBoolean(reader["IsLocked"]),
                        FAClassId = reader.GetGuid(reader.GetOrdinal("FAClassId")),
                        FAClassDescription = reader["FAClassDescription"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        CreatedBy = reader["CreatedBy"].ToString()
                    };
                }
            }

            return subClass;
        }

        // Add
        public void Add(FASubClass subClass)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO FASubClass
                                (Id, Code, Description, IsLocked, FAClassId, CreatedDate, CreatedBy)
                                 VALUES
                                (@Id, @Code, @Description, @IsLocked, @FAClassId, @CreatedDate, @CreatedBy)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", subClass.Id);
                cmd.Parameters.AddWithValue("@Code", subClass.Code ?? "");
                cmd.Parameters.AddWithValue("@Description", subClass.Description ?? "");
                cmd.Parameters.AddWithValue("@IsLocked", subClass.IsLocked);
                cmd.Parameters.AddWithValue("@FAClassId", subClass.FAClassId);
                cmd.Parameters.AddWithValue("@CreatedDate", subClass.CreatedDate);
                cmd.Parameters.AddWithValue("@CreatedBy", subClass.CreatedBy ?? "System");

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Update
        public bool Update(FASubClass subClass)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE FASubClass 
                                 SET Code=@Code, Description=@Description, IsLocked=@IsLocked, FAClassId=@FAClassId
                                 WHERE Id=@Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", subClass.Id);
                cmd.Parameters.AddWithValue("@Code", subClass.Code ?? "");
                cmd.Parameters.AddWithValue("@Description", subClass.Description ?? "");
                cmd.Parameters.AddWithValue("@IsLocked", subClass.IsLocked);
                cmd.Parameters.AddWithValue("@FAClassId", subClass.FAClassId);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // Delete
        public bool Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"DELETE FROM FASubClass WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
