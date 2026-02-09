using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using TestApis.Models;

namespace TestApis.Services
{
    public class EmployeeBranchService
    {
        private readonly string _connectionString;

        public EmployeeBranchService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public List<EmployeeBranch> GetAll()
        {
            var branches = new List<EmployeeBranch>();

            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "SELECT Code, BranchName, BranchNumber, BankCode FROM EmployeeBranches ORDER BY BranchName";
                SqlCommand cmd = new SqlCommand(sql, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    branches.Add(new EmployeeBranch
                    {
                        Code = (int)reader["Code"],
                        BranchName = reader["BranchName"].ToString(),
                        BranchNumber = reader["BranchNumber"].ToString(),
                        BankCode = (int)reader["BankCode"]
                    });
                }
            }
            return branches;
        }

        public EmployeeBranch GetById(int id)
        {
            EmployeeBranch branch = null;
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "SELECT Code, BranchName, BranchNumber, BankCode FROM EmployeeBranches WHERE Code=@Code";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Code", id);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    branch = new EmployeeBranch
                    {
                        Code = (int)reader["Code"],
                        BranchName = reader["BranchName"].ToString(),
                        BranchNumber = reader["BranchNumber"].ToString(),
                        BankCode = (int)reader["BankCode"]
                    };
                }
            }
            return branch;
        }

        //internal void Add(EmployeeBranch branch)
        //{
        //    throw new NotImplementedException();
        //}

        public void Add(EmployeeBranch branch)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "INSERT INTO EmployeeBranches (BranchName, BranchNumber, BankCode) VALUES (@BranchName, @BranchNumber, @BankCode)";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@BranchName", branch.BranchName);
                cmd.Parameters.AddWithValue("@BranchNumber", branch.BranchNumber);
                cmd.Parameters.AddWithValue("@BankCode", branch.BankCode);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(EmployeeBranch branch)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "UPDATE EmployeeBranches SET BranchName=@BranchName, BranchNumber=@BranchNumber, BankCode=@BankCode WHERE Code=@Code";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Code", branch.Code);
                cmd.Parameters.AddWithValue("@BranchName", branch.BranchName);
                cmd.Parameters.AddWithValue("@BranchNumber", branch.BranchNumber);
                cmd.Parameters.AddWithValue("@BankCode", branch.BankCode);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                string sql = "DELETE FROM EmployeeBranches WHERE Code=@Code";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Code", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
