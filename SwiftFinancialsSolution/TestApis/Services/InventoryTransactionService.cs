using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class InventoryTransactionService
    {
        private readonly string _connectionString;

        public InventoryTransactionService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get all transactions
        public List<InventoryTransaction> GetAll()
        {
            var list = new List<InventoryTransaction>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, TransactionDate, DocumentNo, ItemId, LocationId,
                                        EntryType, Quantity, UnitCost, ReferenceJournalId,
                                        SequentialId, CreatedBy, CreatedDate
                                 FROM swiftFin_InventoryTransactions";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new InventoryTransaction
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        TransactionDate = Convert.ToDateTime(reader["TransactionDate"]),
                        DocumentNo = reader["DocumentNo"].ToString(),
                        ItemId = reader.GetGuid(reader.GetOrdinal("ItemId")),
                        LocationId = reader.GetGuid(reader.GetOrdinal("LocationId")),
                        EntryType = reader["EntryType"].ToString(),
                        Quantity = Convert.ToDecimal(reader["Quantity"]),
                        UnitCost = reader["UnitCost"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["UnitCost"]),
                        ReferenceJournalId = reader["ReferenceJournalId"] == DBNull.Value ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("ReferenceJournalId")),
                        SequentialId = reader.GetGuid(reader.GetOrdinal("SequentialId")),
                        CreatedBy = reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    });
                }
            }

            return list;
        }

        // Get transaction by Id
        public InventoryTransaction GetById(Guid id)
        {
            InventoryTransaction txn = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, TransactionDate, DocumentNo, ItemId, LocationId,
                                        EntryType, Quantity, UnitCost, ReferenceJournalId,
                                        SequentialId, CreatedBy, CreatedDate
                                 FROM swiftFin_InventoryTransactions WHERE Id=@Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    txn = new InventoryTransaction
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        TransactionDate = Convert.ToDateTime(reader["TransactionDate"]),
                        DocumentNo = reader["DocumentNo"].ToString(),
                        ItemId = reader.GetGuid(reader.GetOrdinal("ItemId")),
                        LocationId = reader.GetGuid(reader.GetOrdinal("LocationId")),
                        EntryType = reader["EntryType"].ToString(),
                        Quantity = Convert.ToDecimal(reader["Quantity"]),
                        UnitCost = reader["UnitCost"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["UnitCost"]),
                        ReferenceJournalId = reader["ReferenceJournalId"] == DBNull.Value ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("ReferenceJournalId")),
                        SequentialId = reader.GetGuid(reader.GetOrdinal("SequentialId")),
                        CreatedBy = reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                }
            }

            return txn;
        }

        // Add new transaction
        public void Add(InventoryTransaction txn)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO swiftFin_InventoryTransactions
                        (Id, TransactionDate, DocumentNo, ItemId, LocationId, EntryType,
                         Quantity, UnitCost, ReferenceJournalId, SequentialId, CreatedBy, CreatedDate)
                         VALUES
                        (@Id, @TransactionDate, @DocumentNo, @ItemId, @LocationId, @EntryType,
                         @Quantity, @UnitCost, @ReferenceJournalId, @SequentialId, @CreatedBy, @CreatedDate)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", txn.Id);
                cmd.Parameters.AddWithValue("@TransactionDate", txn.TransactionDate);
                cmd.Parameters.AddWithValue("@DocumentNo", txn.DocumentNo ?? "");
                cmd.Parameters.AddWithValue("@ItemId", txn.ItemId);
                cmd.Parameters.AddWithValue("@LocationId", txn.LocationId);
                cmd.Parameters.AddWithValue("@EntryType", txn.EntryType ?? "");
                cmd.Parameters.AddWithValue("@Quantity", txn.Quantity);
                cmd.Parameters.AddWithValue("@UnitCost", txn.UnitCost);
                cmd.Parameters.AddWithValue("@ReferenceJournalId", (object)txn.ReferenceJournalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SequentialId", txn.SequentialId);
                cmd.Parameters.AddWithValue("@CreatedBy", txn.CreatedBy ?? "System");
                cmd.Parameters.AddWithValue("@CreatedDate", txn.CreatedDate);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Update transaction
        public bool Update(InventoryTransaction txn)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE swiftFin_InventoryTransactions 
                                 SET TransactionDate=@TransactionDate, DocumentNo=@DocumentNo,
                                     ItemId=@ItemId, LocationId=@LocationId, EntryType=@EntryType,
                                     Quantity=@Quantity, UnitCost=@UnitCost, ReferenceJournalId=@ReferenceJournalId
                                 WHERE Id=@Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", txn.Id);
                cmd.Parameters.AddWithValue("@TransactionDate", txn.TransactionDate);
                cmd.Parameters.AddWithValue("@DocumentNo", txn.DocumentNo ?? "");
                cmd.Parameters.AddWithValue("@ItemId", txn.ItemId);
                cmd.Parameters.AddWithValue("@LocationId", txn.LocationId);
                cmd.Parameters.AddWithValue("@EntryType", txn.EntryType ?? "");
                cmd.Parameters.AddWithValue("@Quantity", txn.Quantity);
                cmd.Parameters.AddWithValue("@UnitCost", txn.UnitCost);
                cmd.Parameters.AddWithValue("@ReferenceJournalId", (object)txn.ReferenceJournalId ?? DBNull.Value);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // Delete transaction
        public bool Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"DELETE FROM swiftFin_InventoryTransactions WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
