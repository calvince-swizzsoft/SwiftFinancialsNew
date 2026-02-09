using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using TestApis.Models;

namespace TestApis.Services
{
    public class HousingLevyContributionService
    {
        private readonly string _connectionString;

        public HousingLevyContributionService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public List<HousingLevyContribution> GetAll()
        {
            var list = new List<HousingLevyContribution>();
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = "SELECT Id, EmployeeAmount, EmployerAmount, Total FROM HousingLevyContributions";
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new HousingLevyContribution
                            {
                                Id = (int)reader["Id"],
                                EmployeeAmount = (decimal)reader["EmployeeAmount"],
                                EmployerAmount = (decimal)reader["EmployerAmount"],
                                Total = (decimal)reader["Total"]
                            });
                        }
                    }
                }
            }
            return list;
        }

        public HousingLevyContribution GetById(int id)
        {
            HousingLevyContribution item = null;
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = "SELECT Id, EmployeeAmount, EmployerAmount, Total FROM HousingLevyContributions WHERE Id=@Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            item = new HousingLevyContribution
                            {
                                Id = (int)reader["Id"],
                                EmployeeAmount = (decimal)reader["EmployeeAmount"],
                                EmployerAmount = (decimal)reader["EmployerAmount"],
                                Total = (decimal)reader["Total"]
                            };
                        }
                    }
                }
            }
            return item;
        }

        public void Add(HousingLevyContribution contribution)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = @"INSERT INTO HousingLevyContributions (EmployeeAmount, EmployerAmount, Total) 
                              VALUES (@EmployeeAmount, @EmployerAmount, @Total)";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeAmount", contribution.EmployeeAmount);
                    cmd.Parameters.AddWithValue("@EmployerAmount", contribution.EmployerAmount);
                    cmd.Parameters.AddWithValue("@Total", contribution.Total);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(HousingLevyContribution contribution)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = @"UPDATE HousingLevyContributions 
                              SET EmployeeAmount=@EmployeeAmount, EmployerAmount=@EmployerAmount, Total=@Total 
                              WHERE Id=@Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", contribution.Id);
                    cmd.Parameters.AddWithValue("@EmployeeAmount", contribution.EmployeeAmount);
                    cmd.Parameters.AddWithValue("@EmployerAmount", contribution.EmployerAmount);
                    cmd.Parameters.AddWithValue("@Total", contribution.Total);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = "DELETE FROM HousingLevyContributions WHERE Id=@Id";
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
   