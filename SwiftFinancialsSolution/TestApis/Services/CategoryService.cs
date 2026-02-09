using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class CategoryService
    {
        private readonly string _connectionString;

        public CategoryService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }
        public List<Category> GetAll()
        {
            var list = new List<Category>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, Description, IsLocked, SequentialId, CreatedBy, CreatedDate 
                         FROM swiftFin_Categories";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {

                    list.Add(new Category
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Description = reader["Description"].ToString(),
                        IsLocked = Convert.ToBoolean(reader["IsLocked"]),
                        SequentialId = reader.GetGuid(reader.GetOrdinal("SequentialId")),
                        CreatedBy = reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    });
                }
            }

            return list;
        }


        // Get category by Id
        public Category GetById(Guid id)
        {
            Category category = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, Description, IsLocked, SequentialId, CreatedBy, CreatedDate 
                         FROM swiftFin_Categories WHERE Id=@Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    category = new Category
                    {
                        Id = (Guid)reader["Id"],
                        Description = reader["Description"].ToString(),
                        IsLocked = Convert.ToBoolean(reader["IsLocked"]),
                        SequentialId = reader.GetGuid(reader.GetOrdinal("SequentialId")),
                        CreatedBy = reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                }
            }

            return category;
        }


        // Add new category
        public void Add(Category category)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO swiftFin_Categories 
                                 (Id, Description, IsLocked, CreatedBy, CreatedDate) 
                                 VALUES (@Id, @Description, @IsLocked, @CreatedBy, @CreatedDate)";

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@Id", category.Id);
                cmd.Parameters.AddWithValue("@Description", category.Description);
                cmd.Parameters.AddWithValue("@IsLocked", category.IsLocked);
                cmd.Parameters.AddWithValue("@CreatedBy", category.CreatedBy ?? "System");
                cmd.Parameters.AddWithValue("@CreatedDate", category.CreatedDate);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Update category
        public void Update(Category category)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE swiftFin_Categories 
                         SET Description=@Description, IsLocked=@IsLocked 
                         WHERE Id=@Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", category.Id);
                cmd.Parameters.AddWithValue("@Description", category.Description);
                cmd.Parameters.AddWithValue("@IsLocked", category.IsLocked);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }


        // Delete category
        public void Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM swiftFin_Categories WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

    }
}
