using Dapper;
using SwiftFinancials.Web.Areas.Admin.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Areas.Admin.Data
{
    public class ReportRepository
    {
        private readonly string _connectionString;
        private readonly Dictionary<string, string> _storedProcedures;

        public ReportRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

            _storedProcedures = new Dictionary<string, string>
        {
            { "Loans", "sp_LoansByStatus" },
            { "Members By Age", "Sp_MembersListingByAge" },
            { "Customers By Gender", "sp_CustomerByGender" },
            { "Trial Balace", "sp_TrialBalance" },
            { "Statement Of Deposit Return", "sp_StatementOfDepositReturn" }
        };
        }

        private IDbConnection Connection => new SqlConnection(_connectionString);

        public IEnumerable<DynamicReport> GetAllReports()
        {
            using (var db = Connection)
            {
                string sql = "SELECT * FROM swiftFin_DynamicReports";
                return db.Query<DynamicReport>(sql);
            }
        }


        public bool ReportExists(string name, string storedProcedureName)
        {
            using (var db = Connection)
            {
                string sql = "SELECT COUNT(*) FROM swiftFin_DynamicReports WHERE Name = @Name AND StoredProcedureName = @StoredProcedureName";
                int count = db.QuerySingle<int>(sql, new { Name = name, StoredProcedureName = storedProcedureName });
                return count > 0;
            }
        }



        public void AddReport(DynamicReport report)
        {
            using (var db = Connection)
            {
                string sql = "INSERT INTO swiftFin_DynamicReports (Name, StoredProcedureName, CreatedAt) VALUES (@Name, @StoredProcedureName, @CreatedAt)";
                db.Execute(sql, new { report.Name, report.StoredProcedureName, report.CreatedAt });
            }
        }

        public void UpdateReport(DynamicReport report)
        {
            using (var db = Connection)
            {
                string sql = "UPDATE swiftFin_DynamicReports SET Name = @Name, StoredProcedureName = @StoredProcedureName WHERE Id = @Id";
                db.Execute(sql, report);
            }
        }

        public void DeleteReport(int id)
        {
            using (var db = Connection)
            {
                string sql = "DELETE FROM swiftFin_DynamicReports WHERE Id = @Id";
                db.Execute(sql, new { Id = id });
            }
        }

        public DynamicReport GetReportById(int id)
        {
            using (var db = Connection)
            {
                string sql = "SELECT * FROM swiftFin_DynamicReports WHERE Id = @Id";
                return db.QueryFirstOrDefault<DynamicReport>(sql, new { Id = id });
            }
        }

        public string GetStoredProcedureName(string reportName)
        {
            _storedProcedures.TryGetValue(reportName, out string storedProcedure);
            return storedProcedure;
        }

    }
}