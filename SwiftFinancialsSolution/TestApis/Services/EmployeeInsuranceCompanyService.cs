using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using TestApis.Models;

namespace TestApis.Services
{
    public class EmployeeInsuranceCompanyService
    {
        private readonly string _connectionString;

        public EmployeeInsuranceCompanyService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public List<EmployeeInsuranceCompany> GetAll()
        {
            var companies = new List<EmployeeInsuranceCompany>();

            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "SELECT Code, Name, Address FROM EmployeeInsuranceCompanies ORDER BY Name";
                SqlCommand cmd = new SqlCommand(sql, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    companies.Add(new EmployeeInsuranceCompany
                    {
                        Code = (int)reader["Code"],
                        Name = reader["Name"].ToString(),
                        Address = reader["Address"]?.ToString()
                    });
                }
            }
            return companies;
        }

        public EmployeeInsuranceCompany GetById(int id)
        {
            EmployeeInsuranceCompany company = null;
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "SELECT Code, Name, Address FROM EmployeeInsuranceCompanies WHERE Code=@Code";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Code", id);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    company = new EmployeeInsuranceCompany
                    {
                        Code = (int)reader["Code"],
                        Name = reader["Name"].ToString(),
                        Address = reader["Address"]?.ToString()
                    };
                }
            }
            return company;
        }

        public void Add(EmployeeInsuranceCompany company)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "INSERT INTO EmployeeInsuranceCompanies (Name, Address) VALUES (@Name, @Address)";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Name", company.Name);
                cmd.Parameters.AddWithValue("@Address", (object)company.Address ?? DBNull.Value);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(EmployeeInsuranceCompany company)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "UPDATE EmployeeInsuranceCompanies SET Name=@Name, Address=@Address WHERE Code=@Code";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Code", company.Code);
                cmd.Parameters.AddWithValue("@Name", company.Name);
                cmd.Parameters.AddWithValue("@Address", (object)company.Address ?? DBNull.Value);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "DELETE FROM EmployeeInsuranceCompanies WHERE Code=@Code";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Code", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}