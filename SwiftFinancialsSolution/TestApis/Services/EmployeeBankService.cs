using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using TestApis.Models;

namespace TestApis.Services
{
    public class EmployeeBankService
    {
        private readonly string _connectionString;

        public EmployeeBankService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public List<EmployeeBank> GetAll()
        {
            var banks = new List<EmployeeBank>();

            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "SELECT Code, Name, BankCode, PostalAddress FROM EmployeeBanks ORDER BY Name";
                SqlCommand cmd = new SqlCommand(sql, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    banks.Add(new EmployeeBank
                    {
                        Code = (int)reader["Code"],
                        Name = reader["Name"].ToString(),
                        BankCode = reader["BankCode"].ToString(),
                        PostalAddress = reader["PostalAddress"]?.ToString()
                    });
                }
            }
            return banks;
        }

        public EmployeeBank GetById(int id)
        {
            EmployeeBank bank = null;
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "SELECT Code, Name, BankCode, PostalAddress FROM EmployeeBanks WHERE Code=@Code";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Code", id);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    bank = new EmployeeBank
                    {
                        Code = (int)reader["Code"],
                        Name = reader["Name"].ToString(),
                        BankCode = reader["BankCode"].ToString(),
                        PostalAddress = reader["PostalAddress"]?.ToString()
                    };
                }
            }
            return bank;
        }

        public void Add(EmployeeBank bank)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "INSERT INTO EmployeeBanks (Name, BankCode, PostalAddress) VALUES (@Name, @BankCode, @PostalAddress)";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Name", bank.Name);
                cmd.Parameters.AddWithValue("@BankCode", bank.BankCode);
                cmd.Parameters.AddWithValue("@PostalAddress", (object)bank.PostalAddress ?? DBNull.Value);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(EmployeeBank bank)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "UPDATE EmployeeBanks SET Name=@Name, BankCode=@BankCode, PostalAddress=@PostalAddress WHERE Code=@Code";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Code", bank.Code);
                cmd.Parameters.AddWithValue("@Name", bank.Name);
                cmd.Parameters.AddWithValue("@BankCode", bank.BankCode);
                cmd.Parameters.AddWithValue("@PostalAddress", (object)bank.PostalAddress ?? DBNull.Value);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "DELETE FROM EmployeeBanks WHERE Code=@Code";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Code", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
