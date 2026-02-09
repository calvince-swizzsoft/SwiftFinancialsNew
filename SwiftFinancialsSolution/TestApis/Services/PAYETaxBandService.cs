using SwiftFinancials.Web.Areas.Payroll.models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class PAYETaxBandService
    {
        private readonly string _connStr =
            ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public List<PAYETaxBand> GetAll()
        {
            var list = new List<PAYETaxBand>();
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("SELECT * FROM PAYETaxBands", conn))
            {
                conn.Open();
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    list.Add(new PAYETaxBand
                    {
                        Id = (int)rdr["Id"],
                        TaxYear = (int)rdr["TaxYear"],
                        LowerLimit = (decimal)rdr["LowerLimit"],
                        UpperLimit = rdr["UpperLimit"] as decimal?,
                        Rate = (decimal)rdr["Rate"],
                        CreatedAt = (DateTime)rdr["CreatedAt"]
                    });
                }
            }
            return list;
        }

        public PAYETaxBand GetById(int id)
        {
            PAYETaxBand band = null;
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("SELECT * FROM PAYETaxBands WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    band = new PAYETaxBand
                    {
                        Id = (int)rdr["Id"],
                        TaxYear = (int)rdr["TaxYear"],
                        LowerLimit = (decimal)rdr["LowerLimit"],
                        UpperLimit = rdr["UpperLimit"] as decimal?,
                        Rate = (decimal)rdr["Rate"],
                        CreatedAt = (DateTime)rdr["CreatedAt"]
                    };
                }
            }
            return band;
        }

        public void Add(PAYETaxBand band)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "INSERT INTO PAYETaxBands (TaxYear, LowerLimit, UpperLimit, Rate) " +
                "VALUES (@TaxYear, @LowerLimit, @UpperLimit, @Rate)", conn))
            {
                cmd.Parameters.AddWithValue("@TaxYear", band.TaxYear);
                cmd.Parameters.AddWithValue("@LowerLimit", band.LowerLimit);
                cmd.Parameters.AddWithValue("@UpperLimit", (object)band.UpperLimit ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Rate", band.Rate);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(PAYETaxBand band)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "UPDATE PAYETaxBands SET TaxYear=@TaxYear, LowerLimit=@LowerLimit, " +
                "UpperLimit=@UpperLimit, Rate=@Rate WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", band.Id);
                cmd.Parameters.AddWithValue("@TaxYear", band.TaxYear);
                cmd.Parameters.AddWithValue("@LowerLimit", band.LowerLimit);
                cmd.Parameters.AddWithValue("@UpperLimit", (object)band.UpperLimit ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Rate", band.Rate);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("DELETE FROM PAYETaxBands WHERE Id=@Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
