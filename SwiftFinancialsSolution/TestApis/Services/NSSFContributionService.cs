using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Controllers;
using TestApis.Models;

namespace TestApis.Services
{
    public class NSSFContributionService
    {
        private readonly string _connectionString;

        public NSSFContributionService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get all contributions
        public List<NSSFContribution> GetAll()
        {
            var contributions = new List<NSSFContribution>();
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "SELECT Id, EmployeeAmount, EmployerAmount, Total FROM NSSFContributions ORDER BY Id";
                SqlCommand cmd = new SqlCommand(sql, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    contributions.Add(new NSSFContribution
                    {
                        Id = (int)reader["Id"],
                        EmployeeAmount = (decimal)reader["EmployeeAmount"],
                        EmployerAmount = (decimal)reader["EmployerAmount"],
                        Total = (decimal)reader["Total"]
                    });
                }
            }
            return contributions;
        }

        internal void AddNSSFContribution(NSSFContributionsController contribution)
        {
            throw new NotImplementedException();
        }

        internal void AddNSSFContribution(NSSFContribution contribution)
        {
            throw new NotImplementedException();
        }

        // Get by ID
        public NSSFContribution GetById(int id)
        {
            NSSFContribution contribution = null;
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "SELECT Id, EmployeeAmount, EmployerAmount, Total FROM NSSFContributions WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    contribution = new NSSFContribution
                    {
                        Id = (int)reader["Id"],
                        EmployeeAmount = (decimal)reader["EmployeeAmount"],
                        EmployerAmount = (decimal)reader["EmployerAmount"],
                        Total = (decimal)reader["Total"]
                    };
                }
            }
            return contribution;
        }

        // -----------------------------
        // Main public Add method (used by controller)
        // -----------------------------
        public void Add(NSSFContribution contribution)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "INSERT INTO NSSFContributions (EmployeeAmount, EmployerAmount, Total) VALUES (@Employee, @Employer, @Total)";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Employee", contribution.EmployeeAmount);
                cmd.Parameters.AddWithValue("@Employer", contribution.EmployerAmount);
                cmd.Parameters.AddWithValue("@Total", contribution.Total);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // -----------------------------
        // Optional internal helper methods (no conflict)
        // -----------------------------
        internal void AddInternal(NSSFContribution contribution)
        {
            // Can delegate to main Add
            Add(contribution);
        }

        internal void NSSFContribution(NSSFContribution contribution)
        {
            // Can also delegate to main Add
            Add(contribution);
        }

        // Update an existing contribution
        public void Update(NSSFContribution contribution)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "UPDATE NSSFContributions SET EmployeeAmount=@Employee, EmployerAmount=@Employer, Total=@Total WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Id", contribution.Id);
                cmd.Parameters.AddWithValue("@Employee", contribution.EmployeeAmount);
                cmd.Parameters.AddWithValue("@Employer", contribution.EmployerAmount);
                cmd.Parameters.AddWithValue("@Total", contribution.Total);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Delete a contribution
        public void Delete(int id)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "DELETE FROM NSSFContributions WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Id", id);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
