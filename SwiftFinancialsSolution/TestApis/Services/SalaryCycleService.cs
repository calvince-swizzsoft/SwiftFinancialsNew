using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class SalaryCycleService
    {
        private readonly string _connectionString;

        public SalaryCycleService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public IEnumerable<SalaryCycle> GetAll()
        {
            var list = new List<SalaryCycle>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM SalaryCycle ORDER BY StartDate DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(Map(reader));
                        }
                    }
                }
            }
            return list;
        }

        public SalaryCycle GetById(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM SalaryCycle WHERE Id = @Id";
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

        public void Add(SalaryCycle cycle)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO SalaryCycle (Name, StartDate, EndDate, IsProcessed) 
                                 VALUES (@Name, @StartDate, @EndDate, @IsProcessed)";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", cycle.Name);
                    cmd.Parameters.AddWithValue("@StartDate", cycle.StartDate);
                    cmd.Parameters.AddWithValue("@EndDate", cycle.EndDate);
                    cmd.Parameters.AddWithValue("@IsProcessed", cycle.IsProcessed);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(SalaryCycle cycle)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE SalaryCycle 
                                 SET Name=@Name, StartDate=@StartDate, EndDate=@EndDate, IsProcessed=@IsProcessed 
                                 WHERE Id=@Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", cycle.Id);
                    cmd.Parameters.AddWithValue("@Name", cycle.Name);
                    cmd.Parameters.AddWithValue("@StartDate", cycle.StartDate);
                    cmd.Parameters.AddWithValue("@EndDate", cycle.EndDate);
                    cmd.Parameters.AddWithValue("@IsProcessed", cycle.IsProcessed);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM SalaryCycle WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private SalaryCycle Map(IDataReader reader)
        {
            return new SalaryCycle
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString(),
                StartDate = Convert.ToDateTime(reader["StartDate"]),
                EndDate = Convert.ToDateTime(reader["EndDate"]),
                IsProcessed = Convert.ToBoolean(reader["IsProcessed"])
            };
        }
    }
}
