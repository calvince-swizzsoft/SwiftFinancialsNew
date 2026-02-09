using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class PayrollClosureService
    {
        private readonly string _connectionString;

        public PayrollClosureService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public IEnumerable<PayrollClosure> GetAll()
        {
            var list = new List<PayrollClosure>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM PayrollClosure ORDER BY ClosureDate DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(Map(reader));
                    }
                }
            }
            return list;
        }

        public PayrollClosure GetById(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM PayrollClosure WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return Map(reader);
                    }
                }
            }
            return null;
        }

        public void Add(PayrollClosure closure)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO PayrollClosure 
                                (SalaryCycleId, ClosureDate, ClosedBy, IsClosed, IsPostedToGL, PayslipsGenerated)
                                 VALUES (@SalaryCycleId, @ClosureDate, @ClosedBy, @IsClosed, @IsPostedToGL, @PayslipsGenerated)";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SalaryCycleId", closure.SalaryCycleId);
                    cmd.Parameters.AddWithValue("@ClosureDate", closure.ClosureDate);
                    cmd.Parameters.AddWithValue("@ClosedBy", closure.ClosedBy);
                    cmd.Parameters.AddWithValue("@IsClosed", closure.IsClosed);
                    cmd.Parameters.AddWithValue("@IsPostedToGL", closure.IsPostedToGL);
                    cmd.Parameters.AddWithValue("@PayslipsGenerated", closure.PayslipsGenerated);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(PayrollClosure closure)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE PayrollClosure
                                 SET SalaryCycleId=@SalaryCycleId, ClosureDate=@ClosureDate, ClosedBy=@ClosedBy,
                                     IsClosed=@IsClosed, IsPostedToGL=@IsPostedToGL, PayslipsGenerated=@PayslipsGenerated
                                 WHERE Id=@Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", closure.Id);
                    cmd.Parameters.AddWithValue("@SalaryCycleId", closure.SalaryCycleId);
                    cmd.Parameters.AddWithValue("@ClosureDate", closure.ClosureDate);
                    cmd.Parameters.AddWithValue("@ClosedBy", closure.ClosedBy);
                    cmd.Parameters.AddWithValue("@IsClosed", closure.IsClosed);
                    cmd.Parameters.AddWithValue("@IsPostedToGL", closure.IsPostedToGL);
                    cmd.Parameters.AddWithValue("@PayslipsGenerated", closure.PayslipsGenerated);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM PayrollClosure WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private PayrollClosure Map(IDataReader reader)
        {
            return new PayrollClosure
            {
                Id = Convert.ToInt32(reader["Id"]),
                SalaryCycleId = Convert.ToInt32(reader["SalaryCycleId"]),
                ClosureDate = Convert.ToDateTime(reader["ClosureDate"]),
                ClosedBy = reader["ClosedBy"].ToString(),
                IsClosed = Convert.ToBoolean(reader["IsClosed"]),
                IsPostedToGL = Convert.ToBoolean(reader["IsPostedToGL"]),
                PayslipsGenerated = Convert.ToBoolean(reader["PayslipsGenerated"])
            };
        }
    }
}
