using SwiftFinancials.Web.Areas.Payroll.models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class AnnualTaxReportService
    {
        private readonly string _connectionString;

        public AnnualTaxReportService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public IEnumerable<AnnualTaxReport> GetAll()
        {
            var list = new List<AnnualTaxReport>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM AnnualTaxReports ORDER BY TaxYear DESC, EmployeeId";
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

        public AnnualTaxReport GetByEmployeeYear(int employeeId, int taxYear)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM AnnualTaxReports WHERE EmployeeId=@EmployeeId AND TaxYear=@TaxYear";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                    cmd.Parameters.AddWithValue("@TaxYear", taxYear);
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

        public void Add(AnnualTaxReport report)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO AnnualTaxReports 
                                (EmployeeId, TaxYear, TotalGross, TotalPAYE, TotalNSSF, TotalSHIF, 
                                 TotalHousingLevy, TotalOtherDeductions, TotalNet, GeneratedAt)
                                VALUES (@EmployeeId, @TaxYear, @TotalGross, @TotalPAYE, @TotalNSSF, @TotalSHIF,
                                        @TotalHousingLevy, @TotalOtherDeductions, @TotalNet, GETDATE())";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeId", report.EmployeeId);
                    cmd.Parameters.AddWithValue("@TaxYear", report.TaxYear);
                    cmd.Parameters.AddWithValue("@TotalGross", report.TotalGross);
                    cmd.Parameters.AddWithValue("@TotalPAYE", report.TotalPAYE);
                    cmd.Parameters.AddWithValue("@TotalNSSF", report.TotalNSSF);
                    cmd.Parameters.AddWithValue("@TotalSHIF", report.TotalSHIF);
                    cmd.Parameters.AddWithValue("@TotalHousingLevy", report.TotalHousingLevy);
                    cmd.Parameters.AddWithValue("@TotalOtherDeductions", report.TotalOtherDeductions);
                    cmd.Parameters.AddWithValue("@TotalNet", report.TotalNet);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private AnnualTaxReport Map(IDataReader reader)
        {
            return new AnnualTaxReport
            {
                Id = Convert.ToInt32(reader["Id"]),
                EmployeeId = Convert.ToInt32(reader["EmployeeId"]),
                TaxYear = Convert.ToInt32(reader["TaxYear"]),
                TotalGross = Convert.ToDecimal(reader["TotalGross"]),
                TotalPAYE = Convert.ToDecimal(reader["TotalPAYE"]),
                TotalNSSF = Convert.ToDecimal(reader["TotalNSSF"]),
                TotalSHIF = Convert.ToDecimal(reader["TotalSHIF"]),
                TotalHousingLevy = Convert.ToDecimal(reader["TotalHousingLevy"]),
                TotalOtherDeductions = Convert.ToDecimal(reader["TotalOtherDeductions"]),
                TotalNet = Convert.ToDecimal(reader["TotalNet"]),
                GeneratedAt = Convert.ToDateTime(reader["GeneratedAt"])
            };
        }
    }
}
