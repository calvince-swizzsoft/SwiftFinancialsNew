using SwiftFinancials.Web.Areas.Payroll.models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class EmployeeDeductionService
    {
        private readonly string _connectionString;

        public EmployeeDeductionService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public IEnumerable<EmployeeDeduction> GetAll()
        {
            var list = new List<EmployeeDeduction>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM EmployeeDeductions";
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

        public EmployeeDeduction GetById(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM EmployeeDeductions WHERE Id=@Id";
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

        public void Add(EmployeeDeduction deduction)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO EmployeeDeductions
                                (EmployeeNumber, DeductionCode, StartDate, EndDate, Amount, CreatedAt)
                                VALUES (@EmployeeNumber, @DeductionCode, @StartDate, @EndDate, @Amount, GETDATE())";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeNumber", deduction.EmployeeNumber);
                    cmd.Parameters.AddWithValue("@DeductionCode", deduction.DeductionCode);
                    cmd.Parameters.AddWithValue("@StartDate", deduction.StartDate);
                    cmd.Parameters.AddWithValue("@EndDate", (object)deduction.EndDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Amount", deduction.Amount);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(EmployeeDeduction deduction)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE EmployeeDeductions
                                SET EmployeeNumber=@EmployeeNumber,
                                    DeductionCode=@DeductionCode,
                                    StartDate=@StartDate,
                                    EndDate=@EndDate,
                                    Amount=@Amount
                                WHERE Id=@Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", deduction.Id);
                    cmd.Parameters.AddWithValue("@EmployeeNumber", deduction.EmployeeNumber);
                    cmd.Parameters.AddWithValue("@DeductionCode", deduction.DeductionCode);
                    cmd.Parameters.AddWithValue("@StartDate", deduction.StartDate);
                    cmd.Parameters.AddWithValue("@EndDate", (object)deduction.EndDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Amount", deduction.Amount);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM EmployeeDeductions WHERE Id=@Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private EmployeeDeduction Map(IDataReader reader)
        {
            return new EmployeeDeduction
            {
                Id = Convert.ToInt32(reader["Id"]),
                EmployeeNumber = Convert.ToInt32(reader["EmployeeNumber"]),
                DeductionCode = Convert.ToInt32(reader["DeductionCode"]),
                StartDate = Convert.ToDateTime(reader["StartDate"]),
                EndDate = reader["EndDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["EndDate"]),
                Amount = Convert.ToDecimal(reader["Amount"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            };
        }
    }
}
