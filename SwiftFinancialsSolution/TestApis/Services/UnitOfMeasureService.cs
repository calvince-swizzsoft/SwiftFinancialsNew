using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class UnitOfMeasureService
    {
        private readonly string _connectionString;

        public UnitOfMeasureService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get all units
        public List<UnitOfMeasure> GetAll()
        {
            var list = new List<UnitOfMeasure>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, Code, Description, SequentialId, CreatedBy, CreatedDate 
                                 FROM swiftFin_UnitOfMeasure";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new UnitOfMeasure
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

        // Get unit by Id
        public UnitOfMeasure GetById(Guid id)
        {
            UnitOfMeasure unit = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, Code, Description, SequentialId, CreatedBy, CreatedDate 
                                 FROM swiftFin_UnitOfMeasure WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();

                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    unit = new UnitOfMeasure
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

            return unit;
        }

        // Add new unit
        public void Add(UnitOfMeasure unit)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO swiftFin_UnitOfMeasure
                                (Id, Code, Description, CreatedBy, CreatedDate)
                                VALUES (@Id, @Code, @Description, @CreatedBy, @CreatedDate)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", unit.Id);
                cmd.Parameters.AddWithValue("@Code", unit.Code);
                cmd.Parameters.AddWithValue("@Description", unit.Description);
                cmd.Parameters.AddWithValue("@CreatedBy", unit.CreatedBy ?? "System");
                cmd.Parameters.AddWithValue("@CreatedDate", unit.CreatedDate);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Update unit
        public void Update(UnitOfMeasure unit)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE swiftFin_UnitOfMeasure
                                 SET Code=@Code, Description=@Description
                                 WHERE Id=@Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", unit.Id);
                cmd.Parameters.AddWithValue("@Code", unit.Code);
                cmd.Parameters.AddWithValue("@Description", unit.Description);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Delete unit
        public void Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM swiftFin_UnitOfMeasure WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
