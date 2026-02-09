using SwiftFinancials.Web.Areas.Payroll.models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class EmployeeProfileService
    {
        private readonly string _connectionString;

        public EmployeeProfileService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public IEnumerable<EmployeeProfile> GetAll()
        {
            var list = new List<EmployeeProfile>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM EmployeeProfiles";
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

        public EmployeeProfile GetById(int employeeNumber)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM EmployeeProfiles WHERE EmployeeNumber=@EmployeeNumber";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeNumber", employeeNumber);
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

        public void Add(EmployeeProfile profile)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO EmployeeProfiles
                                (EmployeeNumber, Name, Branch, Designation, StartDate, EndDate, JobGroup,
                                 Disabled, NSSFNumber, SHANumber, KRAPIN, AccountNumber, BankCode, BranchCode, CreatedAt, UpdatedAt)
                                VALUES
                                (@EmployeeNumber, @Name, @Branch, @Designation, @StartDate, @EndDate, @JobGroup,
                                 @Disabled, @NSSFNumber, @SHANumber, @KRAPIN, @AccountNumber, @BankCode, @BranchCode, GETDATE(), GETDATE())";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, profile);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(EmployeeProfile profile)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE EmployeeProfiles
                                SET Name=@Name,
                                    Branch=@Branch,
                                    Designation=@Designation,
                                    StartDate=@StartDate,
                                    EndDate=@EndDate,
                                    JobGroup=@JobGroup,
                                    Disabled=@Disabled,
                                    NSSFNumber=@NSSFNumber,
                                    SHANumber=@SHANumber,
                                    KRAPIN=@KRAPIN,
                                    AccountNumber=@AccountNumber,
                                    BankCode=@BankCode,
                                    BranchCode=@BranchCode,
                                    UpdatedAt=GETDATE()
                                WHERE EmployeeNumber=@EmployeeNumber";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, profile);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int employeeNumber)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM EmployeeProfiles WHERE EmployeeNumber=@EmployeeNumber";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeNumber", employeeNumber);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void AddParams(SqlCommand cmd, EmployeeProfile profile)
        {
            cmd.Parameters.AddWithValue("@EmployeeNumber", profile.EmployeeNumber);
            cmd.Parameters.AddWithValue("@Name", profile.Name ?? "");
            cmd.Parameters.AddWithValue("@Branch", profile.Branch ?? "");
            cmd.Parameters.AddWithValue("@Designation", profile.Designation ?? "");
            cmd.Parameters.AddWithValue("@StartDate", profile.StartDate);
            cmd.Parameters.AddWithValue("@EndDate", (object)profile.EndDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@JobGroup", profile.JobGroup ?? "");
            cmd.Parameters.AddWithValue("@Disabled", profile.Disabled);
            cmd.Parameters.AddWithValue("@NSSFNumber", profile.NSSFNumber ?? "");
            cmd.Parameters.AddWithValue("@SHANumber", profile.SHANumber ?? "");
            cmd.Parameters.AddWithValue("@KRAPIN", profile.KRAPIN ?? "");
            cmd.Parameters.AddWithValue("@AccountNumber", profile.AccountNumber ?? "");
            cmd.Parameters.AddWithValue("@BankCode", profile.BankCode ?? "");
            cmd.Parameters.AddWithValue("@BranchCode", profile.BranchCode ?? "");
        }

        private EmployeeProfile Map(IDataReader reader)
        {
            return new EmployeeProfile
            {
                EmployeeNumber = Convert.ToInt32(reader["EmployeeNumber"]),
                Name = reader["Name"].ToString(),
                Branch = reader["Branch"].ToString(),
                Designation = reader["Designation"].ToString(),
                StartDate = Convert.ToDateTime(reader["StartDate"]),
                EndDate = reader["EndDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["EndDate"]),
                JobGroup = reader["JobGroup"].ToString(),
                Disabled = Convert.ToBoolean(reader["Disabled"]),
                NSSFNumber = reader["NSSFNumber"].ToString(),
                SHANumber = reader["SHANumber"].ToString(),
                KRAPIN = reader["KRAPIN"].ToString(),
                AccountNumber = reader["AccountNumber"].ToString(),
                BankCode = reader["BankCode"].ToString(),
                BranchCode = reader["BranchCode"].ToString(),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"])
            };
        }
    }
}
