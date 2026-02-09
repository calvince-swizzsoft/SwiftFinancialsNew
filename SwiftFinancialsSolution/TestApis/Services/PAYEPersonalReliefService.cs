using SwiftFinancials.Web.Areas.Payroll.models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class PAYEPersonalReliefService
    {
        private readonly string _connStr =
            ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public List<PAYEPersonalRelief> GetAll()
        {
            var list = new List<PAYEPersonalRelief>();
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("SELECT * FROM PAYEPersonalRelief", conn))
            {
                conn.Open();
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    list.Add(new PAYEPersonalRelief
                    {
                        Id = (int)rdr["Id"],
                        TaxYear = (int)rdr["TaxYear"],
                        MonthlyRelief = (decimal)rdr["MonthlyRelief"],
                        AnnualRelief = (decimal)rdr["AnnualRelief"],
                        CreatedAt = (DateTime)rdr["CreatedAt"]
                    });
                }
            }
            return list;
        }

        public PAYEPersonalRelief GetById(int id)
        {
            PAYEPersonalRelief relief = null;
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("SELECT * FROM PAYEPersonalRelief WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    relief = new PAYEPersonalRelief
                    {
                        Id = (int)rdr["Id"],
                        TaxYear = (int)rdr["TaxYear"],
                        MonthlyRelief = (decimal)rdr["MonthlyRelief"],
                        AnnualRelief = (decimal)rdr["AnnualRelief"],
                        CreatedAt = (DateTime)rdr["CreatedAt"]
                    };
                }
            }
            return relief;
        }

        public void Add(PAYEPersonalRelief relief)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "INSERT INTO PAYEPersonalRelief (TaxYear, MonthlyRelief) VALUES (@TaxYear, @MonthlyRelief)", conn))
            {
                cmd.Parameters.AddWithValue("@TaxYear", relief.TaxYear);
                cmd.Parameters.AddWithValue("@MonthlyRelief", relief.MonthlyRelief);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(PAYEPersonalRelief relief)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "UPDATE PAYEPersonalRelief SET TaxYear=@TaxYear, MonthlyRelief=@MonthlyRelief WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", relief.Id);
                cmd.Parameters.AddWithValue("@TaxYear", relief.TaxYear);
                cmd.Parameters.AddWithValue("@MonthlyRelief", relief.MonthlyRelief);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("DELETE FROM PAYEPersonalRelief WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
