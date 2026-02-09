using SwiftFinancials.Web.Areas.Payroll.models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class EmployeeEarningService
    {
        private readonly string _connectionString;

        public EmployeeEarningService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public IEnumerable<EmployeeEarning> GetAll()
        {
            var list = new List<EmployeeEarning>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM EmployeeEarnings";
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

        public EmployeeEarning GetById(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM EmployeeEarnings WHERE Id=@Id";
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

        public void Add(EmployeeEarning earning)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO EmployeeEarnings
                                (EmployeeNumber, EarningCode, StartDate, EndDate, Amount, CreatedAt)
                                VALUES (@EmployeeNumber, @EarningCode, @StartDate, @EndDate, @Amount, GETDATE())";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeNumber", earning.EmployeeNumber);
                    cmd.Parameters.AddWithValue("@EarningCode", earning.EarningCode);
                    cmd.Parameters.AddWithValue("@StartDate", earning.StartDate);
                    cmd.Parameters.AddWithValue("@EndDate", (object)earning.EndDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Amount", earning.Amount);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(EmployeeEarning earning)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE EmployeeEarnings 
                                SET EmployeeNumber=@EmployeeNumber,
                                    EarningCode=@EarningCode,
                                    StartDate=@StartDate,
                                    EndDate=@EndDate,
                                    Amount=@Amount
                                WHERE Id=@Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", earning.Id);
                    cmd.Parameters.AddWithValue("@EmployeeNumber", earning.EmployeeNumber);
                    cmd.Parameters.AddWithValue("@EarningCode", earning.EarningCode);
                    cmd.Parameters.AddWithValue("@StartDate", earning.StartDate);
                    cmd.Parameters.AddWithValue("@EndDate", (object)earning.EndDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Amount", earning.Amount);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM EmployeeEarnings WHERE Id=@Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private EmployeeEarning Map(IDataReader reader)
        {
            return new EmployeeEarning
            {
                Id = Convert.ToInt32(reader["Id"]),
                EmployeeNumber = Convert.ToInt32(reader["EmployeeNumber"]),
                EarningCode = Convert.ToInt32(reader["EarningCode"]),
                StartDate = Convert.ToDateTime(reader["StartDate"]),
                EndDate = reader["EndDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["EndDate"]),
                Amount = Convert.ToDecimal(reader["Amount"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            };
        }
    }
}
