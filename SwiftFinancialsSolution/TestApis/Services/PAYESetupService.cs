using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using TestApis.Models;

namespace TestApis.Services
{
    public class PAYESetupService
    {
        private readonly string _connectionString;

        public PAYESetupService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public List<PAYESetup> GetAll()
        {
            var list = new List<PAYESetup>();

            using (var conn = new SqlConnection(_connectionString))
            {
                var query = "SELECT Id, Type, LowerLimit, UpperLimit, BandAmount, Rate, ReliefAmount FROM PAYESetup";
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new PAYESetup
                            {
                                Id = (int)reader["Id"],
                                Type = reader["Type"].ToString(),
                                LowerLimit = (decimal)reader["LowerLimit"],
                                UpperLimit = (decimal)reader["UpperLimit"],
                                BandAmount = (decimal)reader["BandAmount"],
                                Rate = (decimal)reader["Rate"],
                                ReliefAmount = (decimal)reader["ReliefAmount"]
                            });
                        }
                    }
                }
            }
            return list;
        }

        public PAYESetup GetById(int id)
        {
            PAYESetup item = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var query = "SELECT Id, Type, LowerLimit, UpperLimit, BandAmount, Rate, ReliefAmount FROM PAYESetup WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            item = new PAYESetup
                            {
                                Id = (int)reader["Id"],
                                Type = reader["Type"].ToString(),
                                LowerLimit = (decimal)reader["LowerLimit"],
                                UpperLimit = (decimal)reader["UpperLimit"],
                                BandAmount = (decimal)reader["BandAmount"],
                                Rate = (decimal)reader["Rate"],
                                ReliefAmount = (decimal)reader["ReliefAmount"]
                            };
                        }
                    }
                }
            }
            return item;
        }

        public void Add(PAYESetup setup)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = @"INSERT INTO PAYESetup (Type, LowerLimit, UpperLimit, BandAmount, Rate, ReliefAmount) 
                              VALUES (@Type, @LowerLimit, @UpperLimit, @BandAmount, @Rate, @ReliefAmount)";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Type", setup.Type);
                    cmd.Parameters.AddWithValue("@LowerLimit", setup.LowerLimit);
                    cmd.Parameters.AddWithValue("@UpperLimit", setup.UpperLimit);
                    cmd.Parameters.AddWithValue("@BandAmount", setup.BandAmount);
                    cmd.Parameters.AddWithValue("@Rate", setup.Rate);
                    cmd.Parameters.AddWithValue("@ReliefAmount", setup.ReliefAmount);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(PAYESetup setup)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = @"UPDATE PAYESetup 
                              SET Type=@Type, LowerLimit=@LowerLimit, UpperLimit=@UpperLimit, BandAmount=@BandAmount, 
                                  Rate=@Rate, ReliefAmount=@ReliefAmount
                              WHERE Id=@Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", setup.Id);
                    cmd.Parameters.AddWithValue("@Type", setup.Type);
                    cmd.Parameters.AddWithValue("@LowerLimit", setup.LowerLimit);
                    cmd.Parameters.AddWithValue("@UpperLimit", setup.UpperLimit);
                    cmd.Parameters.AddWithValue("@BandAmount", setup.BandAmount);
                    cmd.Parameters.AddWithValue("@Rate", setup.Rate);
                    cmd.Parameters.AddWithValue("@ReliefAmount", setup.ReliefAmount);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = "DELETE FROM PAYESetup WHERE Id=@Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}