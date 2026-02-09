using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class InventoryPostingAccountService
    {
        private readonly string _connectionString;

        public InventoryPostingAccountService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public List<InventoryPostingAccount> GetAll()
        {
            var list = new List<InventoryPostingAccount>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, Code, Description, ChartOfAccountId, SequentialId, CreatedBy, CreatedDate 
                                 FROM swiftFin_InventoryPostingAccount";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new InventoryPostingAccount
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Code = reader["Code"].ToString(),
                        Description = reader["Description"].ToString(),
                        ChartOfAccountId = reader.GetGuid(reader.GetOrdinal("ChartOfAccountId")),
                        SequentialId = reader.GetGuid(reader.GetOrdinal("SequentialId")),
                        CreatedBy = reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    });
                }
            }
            return list;
        }

        public InventoryPostingAccount GetById(Guid id)
        {
            InventoryPostingAccount account = null;
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, Code, Description, ChartOfAccountId, SequentialId, CreatedBy, CreatedDate 
                                 FROM swiftFin_InventoryPostingAccount WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    account = new InventoryPostingAccount
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Code = reader["Code"].ToString(),
                        Description = reader["Description"].ToString(),
                        ChartOfAccountId = reader.GetGuid(reader.GetOrdinal("ChartOfAccountId")),
                        SequentialId = reader.GetGuid(reader.GetOrdinal("SequentialId")),
                        CreatedBy = reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                }
            }
            return account;
        }

        public void Add(InventoryPostingAccount account)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO swiftFin_InventoryPostingAccount
                                 (Id, Code, Description, ChartOfAccountId, CreatedBy, CreatedDate)
                                 VALUES (@Id, @Code, @Description, @ChartOfAccountId, @CreatedBy, @CreatedDate)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", account.Id);
                cmd.Parameters.AddWithValue("@Code", account.Code);
                cmd.Parameters.AddWithValue("@Description", account.Description ?? "");
                cmd.Parameters.AddWithValue("@ChartOfAccountId", account.ChartOfAccountId);
                cmd.Parameters.AddWithValue("@CreatedBy", account.CreatedBy ?? "System");
                cmd.Parameters.AddWithValue("@CreatedDate", account.CreatedDate);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(InventoryPostingAccount account)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE swiftFin_InventoryPostingAccount
                                 SET Code=@Code, Description=@Description, ChartOfAccountId=@ChartOfAccountId
                                 WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", account.Id);
                cmd.Parameters.AddWithValue("@Code", account.Code);
                cmd.Parameters.AddWithValue("@Description", account.Description ?? "");
                cmd.Parameters.AddWithValue("@ChartOfAccountId", account.ChartOfAccountId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM swiftFin_InventoryPostingAccount WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
