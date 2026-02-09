using SwiftFinancials.Web.Areas.Payroll.models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TestApis.Services
{
    public class PayrollEntryService
    {
        private readonly string _connectionString;

        public PayrollEntryService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public IEnumerable<PayrollEntry> GetAll()
        {
            var list = new List<PayrollEntry>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM PayrollEntries ORDER BY CreatedAt DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(Map(reader));
                    }
                }
            }
            return list;
        }

        public PayrollEntry GetById(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM PayrollEntries WHERE Id=@Id";
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

        public void Add(PayrollEntry entry)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO PayrollEntries
                                (EmployeeId, SalaryCycleId, GrossSalary, PAYE, NSSF, SHIF, HousingLevy, OtherDeductions,
                                 PostedToGL, PayslipGenerated, CreatedAt, UpdatedAt)
                                 VALUES (@EmployeeId, @SalaryCycleId, @GrossSalary, @PAYE, @NSSF, @SHIF, @HousingLevy, 
                                         @OtherDeductions, @PostedToGL, @PayslipGenerated, GETDATE(), GETDATE())";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeId", entry.EmployeeId);
                    cmd.Parameters.AddWithValue("@SalaryCycleId", entry.SalaryCycleId);
                    cmd.Parameters.AddWithValue("@GrossSalary", entry.GrossSalary);
                    cmd.Parameters.AddWithValue("@PAYE", entry.PAYE);
                    cmd.Parameters.AddWithValue("@NSSF", entry.NSSF);
                    cmd.Parameters.AddWithValue("@SHIF", entry.SHIF);
                    cmd.Parameters.AddWithValue("@HousingLevy", entry.HousingLevy);
                    cmd.Parameters.AddWithValue("@OtherDeductions", entry.OtherDeductions);
                    cmd.Parameters.AddWithValue("@PostedToGL", entry.PostedToGL);
                    cmd.Parameters.AddWithValue("@PayslipGenerated", entry.PayslipGenerated);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(PayrollEntry entry)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE PayrollEntries
                                 SET EmployeeId=@EmployeeId, SalaryCycleId=@SalaryCycleId, GrossSalary=@GrossSalary,
                                     PAYE=@PAYE, NSSF=@NSSF, SHIF=@SHIF, HousingLevy=@HousingLevy, OtherDeductions=@OtherDeductions,
                                     PostedToGL=@PostedToGL, PayslipGenerated=@PayslipGenerated, UpdatedAt=GETDATE()
                                 WHERE Id=@Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", entry.Id);
                    cmd.Parameters.AddWithValue("@EmployeeId", entry.EmployeeId);
                    cmd.Parameters.AddWithValue("@SalaryCycleId", entry.SalaryCycleId);
                    cmd.Parameters.AddWithValue("@GrossSalary", entry.GrossSalary);
                    cmd.Parameters.AddWithValue("@PAYE", entry.PAYE);
                    cmd.Parameters.AddWithValue("@NSSF", entry.NSSF);
                    cmd.Parameters.AddWithValue("@SHIF", entry.SHIF);
                    cmd.Parameters.AddWithValue("@HousingLevy", entry.HousingLevy);
                    cmd.Parameters.AddWithValue("@OtherDeductions", entry.OtherDeductions);
                    cmd.Parameters.AddWithValue("@PostedToGL", entry.PostedToGL);
                    cmd.Parameters.AddWithValue("@PayslipGenerated", entry.PayslipGenerated);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM PayrollEntries WHERE Id=@Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private PayrollEntry Map(IDataReader reader)
        {
            return new PayrollEntry
            {
                Id = Convert.ToInt32(reader["Id"]),
                EmployeeId = Convert.ToInt32(reader["EmployeeId"]),
                SalaryCycleId = Convert.ToInt32(reader["SalaryCycleId"]),
                GrossSalary = Convert.ToDecimal(reader["GrossSalary"]),
                PAYE = Convert.ToDecimal(reader["PAYE"]),
                NSSF = Convert.ToDecimal(reader["NSSF"]),
                SHIF = Convert.ToDecimal(reader["SHIF"]),
                HousingLevy = Convert.ToDecimal(reader["HousingLevy"]),
                OtherDeductions = Convert.ToDecimal(reader["OtherDeductions"]),
                NetSalary = Convert.ToDecimal(reader["NetSalary"]),
                PostedToGL = Convert.ToBoolean(reader["PostedToGL"]),
                PayslipGenerated = Convert.ToBoolean(reader["PayslipGenerated"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"])
            };
        }
    }
}
