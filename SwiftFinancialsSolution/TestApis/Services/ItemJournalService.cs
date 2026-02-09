using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class ItemJournalService
    {
        private readonly string _connectionString;

        public ItemJournalService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get all journals
        public List<ItemJournal> GetAll()
        {
            var list = new List<ItemJournal>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, PostingDate, EntryType, DocumentNo, ItemId, ItemName, 
                                        ItemLocationId, Quantity, SequentialId, CreatedBy, CreatedDate, Status
                                 FROM swiftFin_ItemJournals";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new ItemJournal
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        PostingDate = Convert.ToDateTime(reader["PostingDate"]),
                        EntryType = reader["EntryType"].ToString(),
                        DocumentNo = reader["DocumentNo"].ToString(),
                        ItemId = reader.GetGuid(reader.GetOrdinal("ItemId")),
                        ItemName = reader["ItemName"].ToString(),
                        ItemLocationId = reader.GetGuid(reader.GetOrdinal("ItemLocationId")),
                        Quantity = Convert.ToDecimal(reader["Quantity"]),
                        SequentialId = reader.GetGuid(reader.GetOrdinal("SequentialId")),
                        CreatedBy = reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        Status = reader["Status"].ToString(),


                    });
                }
            }

            return list;
        }

        // Get by Id
        public ItemJournal GetById(Guid id)
        {
            ItemJournal journal = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, PostingDate, EntryType, DocumentNo, ItemId, ItemName, 
                                        ItemLocationId, Quantity, SequentialId, CreatedBy, CreatedDate
                                 FROM swiftFin_ItemJournals WHERE Id=@Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    journal = new ItemJournal
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        PostingDate = Convert.ToDateTime(reader["PostingDate"]),
                        EntryType = reader["EntryType"].ToString(),
                        DocumentNo = reader["DocumentNo"].ToString(),
                        ItemId = reader.GetGuid(reader.GetOrdinal("ItemId")),
                        ItemName = reader["ItemName"].ToString(),
                        ItemLocationId = reader.GetGuid(reader.GetOrdinal("ItemLocationId")),
                        Quantity = Convert.ToDecimal(reader["Quantity"]),
                        SequentialId = reader.GetGuid(reader.GetOrdinal("SequentialId")),
                        CreatedBy = reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                }
            }

            return journal;
        }

        // Add new journal
        public void Add(ItemJournal journal)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO swiftFin_ItemJournals
                        (Id, PostingDate, EntryType, DocumentNo, ItemId, ItemName, ItemLocationId, Quantity,
                         SequentialId, CreatedBy, CreatedDate)
                         VALUES
                        (@Id, @PostingDate, @EntryType, @DocumentNo, @ItemId, @ItemName, @ItemLocationId, @Quantity,
                         @SequentialId, @CreatedBy, @CreatedDate)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", journal.Id);
                cmd.Parameters.AddWithValue("@PostingDate", journal.PostingDate);
                cmd.Parameters.AddWithValue("@EntryType", journal.EntryType ?? "");
                cmd.Parameters.AddWithValue("@DocumentNo", journal.DocumentNo ?? "");
                cmd.Parameters.AddWithValue("@ItemId", journal.ItemId);
                cmd.Parameters.AddWithValue("@ItemName", journal.ItemName ?? "");
                cmd.Parameters.AddWithValue("@ItemLocationId", journal.ItemLocationId);
                cmd.Parameters.AddWithValue("@Quantity", journal.Quantity);
                cmd.Parameters.AddWithValue("@SequentialId", journal.SequentialId);
                cmd.Parameters.AddWithValue("@CreatedBy", journal.CreatedBy ?? "System");
                cmd.Parameters.AddWithValue("@CreatedDate", journal.CreatedDate);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Update
        public bool Update(ItemJournal journal)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE swiftFin_ItemJournals 
                                 SET PostingDate=@PostingDate, EntryType=@EntryType, DocumentNo=@DocumentNo, ItemId=@ItemId,
                                     ItemName=@ItemName, ItemLocationId=@ItemLocationId, Quantity=@Quantity
                                 WHERE Id=@Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", journal.Id);
                cmd.Parameters.AddWithValue("@PostingDate", journal.PostingDate);
                cmd.Parameters.AddWithValue("@EntryType", journal.EntryType ?? "");
                cmd.Parameters.AddWithValue("@DocumentNo", journal.DocumentNo ?? "");
                cmd.Parameters.AddWithValue("@ItemId", journal.ItemId);
                cmd.Parameters.AddWithValue("@ItemName", journal.ItemName ?? "");
                cmd.Parameters.AddWithValue("@ItemLocationId", journal.ItemLocationId);
                cmd.Parameters.AddWithValue("@Quantity", journal.Quantity);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // Delete
        public bool Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"DELETE FROM swiftFin_ItemJournals WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool PostJournal(Guid journalId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Get the journal
                        var journalCmd = new SqlCommand(@"
                            SELECT Id, PostingDate, EntryType, DocumentNo, ItemId, ItemName, 
                                   ItemLocationId, Quantity, SequentialId, CreatedBy, CreatedDate, IsPosted
                            FROM swiftFin_ItemJournals
                            WHERE Id=@Id", conn, tran);

                        journalCmd.Parameters.AddWithValue("@Id", journalId);
                        var reader = journalCmd.ExecuteReader();

                        if (!reader.Read())
                            throw new Exception("Journal not found.");

                        if (Convert.ToBoolean(reader["IsPosted"]))
                            throw new Exception("Journal already posted.");

                        var entryType = reader["EntryType"].ToString();
                        var itemId = (Guid)reader["ItemId"];
                        var locationId = (Guid)reader["ItemLocationId"];
                        var qty = Convert.ToDecimal(reader["Quantity"]);
                        var postingDate = Convert.ToDateTime(reader["PostingDate"]);
                        var documentNo = reader["DocumentNo"].ToString();
                        reader.Close();

                        // 2. Insert into InventoryTransactions
                        var txnId = Guid.NewGuid();
                        var txnCmd = new SqlCommand(@"
                            INSERT INTO swiftFin_InventoryTransactions
                            (Id, TransactionDate, DocumentNo, ItemId, LocationId, EntryType,
                             Quantity, UnitCost, ReferenceJournalId, SequentialId, CreatedBy, CreatedDate)
                            VALUES
                            (@Id, @TransactionDate, @DocumentNo, @ItemId, @LocationId, @EntryType,
                             @Quantity, @UnitCost, @ReferenceJournalId, @SequentialId, @CreatedBy, @CreatedDate)",
                                    conn, tran);

                        txnCmd.Parameters.AddWithValue("@Id", txnId);
                        txnCmd.Parameters.AddWithValue("@TransactionDate", postingDate);
                        txnCmd.Parameters.AddWithValue("@DocumentNo", documentNo ?? "");
                        txnCmd.Parameters.AddWithValue("@ItemId", itemId);
                        txnCmd.Parameters.AddWithValue("@LocationId", locationId);
                        txnCmd.Parameters.AddWithValue("@EntryType", entryType ?? "");
                        txnCmd.Parameters.AddWithValue("@Quantity", qty);
                        txnCmd.Parameters.AddWithValue("@UnitCost", 0);
                        txnCmd.Parameters.AddWithValue("@ReferenceJournalId", journalId);
                        txnCmd.Parameters.AddWithValue("@SequentialId", Guid.NewGuid());
                        txnCmd.Parameters.AddWithValue("@CreatedBy", "System");
                        txnCmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                        txnCmd.ExecuteNonQuery();

                        // 3. Update Item InventoryBalance
                        decimal qtyChange = 0;
                        if (entryType == "Purchase" || entryType == "Adjustment+" || entryType == "TransferIn")
                            qtyChange = qty;
                        else if (entryType == "Sale" || entryType == "Adjustment-" || entryType == "TransferOut")
                            qtyChange = -qty;

                        if (qtyChange != 0)
                        {
                            var updateItemCmd = new SqlCommand(@"
                                UPDATE swiftFin_Items
                                SET InventoryBalance = InventoryBalance + @QtyChange
                                WHERE Id=@ItemId", conn, tran);

                            updateItemCmd.Parameters.AddWithValue("@QtyChange", qtyChange);
                            updateItemCmd.Parameters.AddWithValue("@ItemId", itemId);
                            updateItemCmd.ExecuteNonQuery();
                        }

                        // 4. Mark journal as posted
                        var updateJournalCmd = new SqlCommand(@"
                            UPDATE swiftFin_ItemJournals 
                            SET IsPosted = 1, Status = 'Posted'
                            WHERE Id = @Id", conn, tran);
                        updateJournalCmd.Parameters.AddWithValue("@Id", journalId);
                        updateJournalCmd.ExecuteNonQuery();




                        tran.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        throw new Exception($"Error posting journal: {ex.Message}", ex);
                    }
                }
            }
        }

        public bool CancelJournal(Guid journalId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    UPDATE swiftFin_ItemJournals
                    SET IsPosted = 0, Status = 'Cancelled'
                    WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", journalId);

                return cmd.ExecuteNonQuery() > 0;
            }
        }



    }
}
