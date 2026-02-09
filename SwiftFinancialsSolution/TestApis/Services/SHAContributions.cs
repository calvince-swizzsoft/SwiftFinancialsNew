using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class SHAContributionService
    {
        private readonly string _connectionString;

        public SHAContributionService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public List<SHAContributions> GetAll()
        {
            var contributions = new List<SHAContributions>();
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "SELECT Id, ContributionRate, ContributionAmount FROM SHAContributions ORDER BY Id";
                SqlCommand cmd = new SqlCommand(sql, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    contributions.Add(new SHAContributions
                    {
                        Id = (int)reader["Id"],
                        ContributionRate = (decimal)reader["ContributionRate"],
                        ContributionAmount = (decimal)reader["ContributionAmount"]
                    });
                }
            }
            return contributions;
        }

        public SHAContributions GetById(int id)
        {
            SHAContributions contribution = null;
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "SELECT Id, ContributionRate, ContributionAmount FROM SHAContributions WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    contribution = new SHAContributions
                    {
                        Id = (int)reader["Id"],
                        ContributionRate = (decimal)reader["ContributionRate"],
                        ContributionAmount = (decimal)reader["ContributionAmount"]
                    };
                }
            }
            return contribution;
        }

        public void Add(SHAContributions contribution)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "INSERT INTO SHAContributions (ContributionRate, ContributionAmount) VALUES (@Rate, @Amount)";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Rate", contribution.ContributionRate);
                cmd.Parameters.AddWithValue("@Amount", contribution.ContributionAmount);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(SHAContributions contribution)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "UPDATE SHAContributions SET ContributionRate=@Rate, ContributionAmount=@Amount WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Id", contribution.Id);
                cmd.Parameters.AddWithValue("@Rate", contribution.ContributionRate);
                cmd.Parameters.AddWithValue("@Amount", contribution.ContributionAmount);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "DELETE FROM SHAContributions WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Id", id);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
