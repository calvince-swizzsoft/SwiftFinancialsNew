using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using TestApis.Models;

namespace TestApis.Services
{
    public class AccountDetailsService
    {
        private readonly string _connectionString;

        public AccountDetailsService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public List<AccountDetails> GetAll()
        {
            var list = new List<AccountDetails>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT Code, Name, LinkedGLAccount, TaxableEarnings, AllowableDeductions FROM AccountDetails";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new AccountDetails
                    {
                        Code = Convert.ToInt32(reader["Code"]),
                        Name = reader["Name"].ToString(),
                        LinkedGLAccount = reader["LinkedGLAccount"].ToString(),
                        TaxableEarnings = Convert.ToBoolean(reader["TaxableEarnings"]),
                        AllowableDeductions = Convert.ToBoolean(reader["AllowableDeductions"])
                    });
                }
            }
            return list;
        }

        public AccountDetails GetById(int id)
        {
            AccountDetails account = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT Code, Name, LinkedGLAccount, TaxableEarnings, AllowableDeductions FROM AccountDetails WHERE Code=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    account = new AccountDetails
                    {
                        Code = Convert.ToInt32(reader["Code"]),
                        Name = reader["Name"].ToString(),
                        LinkedGLAccount = reader["LinkedGLAccount"].ToString(),
                        TaxableEarnings = Convert.ToBoolean(reader["TaxableEarnings"]),
                        AllowableDeductions = Convert.ToBoolean(reader["AllowableDeductions"])
                    };
                }
            }
            return account;
        }

        public void Add(AccountDetails account)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO AccountDetails (Name, LinkedGLAccount, TaxableEarnings, AllowableDeductions) 
                                 VALUES (@Name, @LinkedGLAccount, @TaxableEarnings, @AllowableDeductions)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", account.Name);
                cmd.Parameters.AddWithValue("@LinkedGLAccount", account.LinkedGLAccount);
                cmd.Parameters.AddWithValue("@TaxableEarnings", account.TaxableEarnings);
                cmd.Parameters.AddWithValue("@AllowableDeductions", account.AllowableDeductions);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(AccountDetails account)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE AccountDetails 
                                 SET Name=@Name, LinkedGLAccount=@LinkedGLAccount, TaxableEarnings=@TaxableEarnings, AllowableDeductions=@AllowableDeductions 
                                 WHERE Code=@Code";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Code", account.Code);
                cmd.Parameters.AddWithValue("@Name", account.Name);
                cmd.Parameters.AddWithValue("@LinkedGLAccount", account.LinkedGLAccount);
                cmd.Parameters.AddWithValue("@TaxableEarnings", account.TaxableEarnings);
                cmd.Parameters.AddWithValue("@AllowableDeductions", account.AllowableDeductions);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM AccountDetails WHERE Code=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}