using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TestApis.Models;

namespace TestApis.Services
{
    public class ItemService
    {
        private readonly string _connectionString;

        public ItemService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get all items
        public List<Item> GetAll()
        {
            var list = new List<Item>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, ItemId, ItemNo, Description, 
                        ItemCategoryId, CategoryDescription,
                        UnitOfMeasureId, UnitOfMeasureDescription,
                        LocationId, LocationDescription,
                        InventoryBalance, CostingMethod, SequentialId, 
                        CreatedBy, CreatedDate
                 FROM swiftFin_Items";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Item
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        ItemId = reader["ItemId"].ToString(),
                        ItemNo = reader["ItemNo"].ToString(),
                        Description = reader["Description"].ToString(),
                        ItemCategoryId = reader.GetGuid(reader.GetOrdinal("ItemCategoryId")),
                        CategoryDescription = reader["CategoryDescription"].ToString(),
                        UnitOfMeasureId = reader.GetGuid(reader.GetOrdinal("UnitOfMeasureId")),
                        UnitOfMeasureDescription = reader["UnitOfMeasureDescription"].ToString(),
                        LocationId = reader.GetGuid(reader.GetOrdinal("LocationId")),
                        LocationDescription = reader["LocationDescription"].ToString(),
                        InventoryBalance = Convert.ToDecimal(reader["InventoryBalance"]),
                        CostingMethod = reader["CostingMethod"]?.ToString(),
                        SequentialId = reader.GetGuid(reader.GetOrdinal("SequentialId")),
                        CreatedBy = reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    });

                }
            }

            return list;
        }

        // Get item by Id
        public Item GetById(Guid id)
        {
            Item item = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, ItemId, ItemNo, Description, ItemCategoryId, CategoryDescription, UnitOfMeasureId, UnitOfMeasureDescription,
                        LocationId, LocationDescription, InventoryBalance, CostingMethod, SequentialId, 
                                        CreatedBy, CreatedDate
                                 FROM swiftFin_Items WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    item = new Item
                    {
                        Id = (Guid)reader["Id"],
                        ItemId = reader["ItemId"].ToString(),
                        ItemNo = reader["ItemNo"].ToString(),
                        Description = reader["Description"].ToString(),
                        ItemCategoryId = reader.GetGuid(reader.GetOrdinal("ItemCategoryId")),
                        CategoryDescription = reader["CategoryDescription"].ToString(),
                        UnitOfMeasureId = reader.GetGuid(reader.GetOrdinal("UnitOfMeasureId")),
                        UnitOfMeasureDescription = reader["UnitOfMeasureDescription"].ToString(),
                        LocationId = reader.GetGuid(reader.GetOrdinal("LocationId")),
                        LocationDescription = reader["LocationDescription"].ToString(),
                        InventoryBalance = Convert.ToDecimal(reader["InventoryBalance"]),
                        CostingMethod = reader["CostingMethod"]?.ToString(),
                        SequentialId = reader.GetGuid(reader.GetOrdinal("SequentialId")),
                        CreatedBy = reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                }
            }

            return item;
        }

        // Add new item
        public void Add(Item item)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO swiftFin_Items
                    (Id, ItemId, ItemNo, Description, ItemCategoryId, CategoryDescription,
                     UnitOfMeasureId, UnitOfMeasureDescription, 
                     LocationId, LocationDescription,
                     InventoryBalance, CostingMethod, SequentialId, CreatedBy, CreatedDate)
                    VALUES
                    (@Id, @ItemId, @ItemNo, @Description, @ItemCategoryId, @CategoryDescription,
                     @UnitOfMeasureId, @UnitOfMeasureDescription,
                     @LocationId, @LocationDescription,
                     @InventoryBalance, @CostingMethod, @SequentialId, @CreatedBy, @CreatedDate)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", item.Id);
                cmd.Parameters.AddWithValue("@ItemId", item.ItemId ?? "");
                cmd.Parameters.AddWithValue("@ItemNo", item.ItemNo ?? "");
                cmd.Parameters.AddWithValue("@Description", item.Description ?? "");
                cmd.Parameters.AddWithValue("@ItemCategoryId", item.ItemCategoryId);
                cmd.Parameters.AddWithValue("@CategoryDescription", item.CategoryDescription ?? "");
                cmd.Parameters.AddWithValue("@UnitOfMeasureId", item.UnitOfMeasureId);
                cmd.Parameters.AddWithValue("@UnitOfMeasureDescription", item.UnitOfMeasureDescription ?? "");
                cmd.Parameters.AddWithValue("@LocationId", item.LocationId);
                cmd.Parameters.AddWithValue("@LocationDescription", item.LocationDescription ?? "");
                cmd.Parameters.AddWithValue("@InventoryBalance", item.InventoryBalance);
                cmd.Parameters.AddWithValue("@CostingMethod", item.CostingMethod ?? "Average");
                cmd.Parameters.AddWithValue("@SequentialId", item.SequentialId);
                cmd.Parameters.AddWithValue("@CreatedBy", item.CreatedBy ?? "System");
                cmd.Parameters.AddWithValue("@CreatedDate", item.CreatedDate);


                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private bool CategoryExists(Guid categoryId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM swiftFin_Categories WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", categoryId);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public bool Update(Item item)
        {
            if (!CategoryExists(item.ItemCategoryId))
            {
                Console.Error.WriteLine("Invalid ItemCategoryId: no matching category exists.");
                return false;
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(@"
                    UPDATE swiftFin_Items 
                       SET ItemId=@ItemId, 
                           ItemNo=@ItemNo, 
                           Description=@Description,
                           ItemCategoryId=@ItemCategoryId, 
                           CategoryDescription=@CategoryDescription,
                           UnitOfMeasureId=@UnitOfMeasureId, 
                           UnitOfMeasureDescription=@UnitOfMeasureDescription,
                           LocationId=@LocationId, 
                           LocationDescription=@LocationDescription,
                           InventoryBalance=@InventoryBalance, 
                           CostingMethod=@CostingMethod
                     WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", item.Id);
                    cmd.Parameters.AddWithValue("@ItemId", item.ItemId ?? "");
                    cmd.Parameters.AddWithValue("@ItemNo", item.ItemNo ?? "");
                    cmd.Parameters.AddWithValue("@Description", item.Description ?? "");
                    cmd.Parameters.AddWithValue("@ItemCategoryId", item.ItemCategoryId);
                    cmd.Parameters.AddWithValue("@CategoryDescription", item.CategoryDescription ?? "");
                    cmd.Parameters.AddWithValue("@UnitOfMeasureId", item.UnitOfMeasureId);
                    cmd.Parameters.AddWithValue("@UnitOfMeasureDescription", item.UnitOfMeasureDescription ?? "");
                    cmd.Parameters.AddWithValue("@LocationId", item.LocationId);
                    cmd.Parameters.AddWithValue("@LocationDescription", item.LocationDescription ?? "");
                    cmd.Parameters.AddWithValue("@InventoryBalance", item.InventoryBalance);
                    cmd.Parameters.AddWithValue("@CostingMethod", item.CostingMethod ?? "Average");

                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine("SQL error during update: " + ex.Message);
                return false;
            }
        }

        public bool UpdateInventoryBalance(Guid id, decimal amountToAdd)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Get current balance
                    decimal originalBalance = 0;
                    using (var checkCmd = new SqlCommand("SELECT ISNULL(InventoryBalance,0) FROM swiftFin_Items WHERE Id=@Id", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Id", id);
                        var result = checkCmd.ExecuteScalar();
                        if (result == null) return false;
                        originalBalance = Convert.ToDecimal(result);
                    }

                    decimal newBalance = originalBalance + amountToAdd;

                    // Update item balance
                    using (var cmd = new SqlCommand(@"
                UPDATE swiftFin_Items
                   SET InventoryBalance = @NewBalance
                 WHERE Id = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@NewBalance", newBalance);
                        cmd.ExecuteNonQuery();
                    }

                    // Insert StockJournal entry
                    using (var logCmd = new SqlCommand(@"
                INSERT INTO swiftFin_StockJournal
                (Id, ItemId, ItemNo, Description, ActionType, Quantity, OriginalBalance, NewBalance, CreatedBy, CreatedDate)
                SELECT NEWID(), Id, ItemNo, Description, 'Increase', @Quantity, @OriginalBalance, @NewBalance, 'System', GETDATE()
                FROM swiftFin_Items WHERE Id=@Id", conn))
                    {
                        logCmd.Parameters.AddWithValue("@Id", id);
                        logCmd.Parameters.AddWithValue("@Quantity", amountToAdd);
                        logCmd.Parameters.AddWithValue("@OriginalBalance", originalBalance);
                        logCmd.Parameters.AddWithValue("@NewBalance", newBalance);
                        logCmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine("SQL error while updating InventoryBalance: " + ex.Message);
                return false;
            }
        }


        public bool ReduceInventoryBalance(Guid id, decimal amountToReduce)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Get current balance
                    decimal originalBalance = 0;
                    using (var checkCmd = new SqlCommand("SELECT ISNULL(InventoryBalance,0) FROM swiftFin_Items WHERE Id=@Id", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Id", id);
                        var result = checkCmd.ExecuteScalar();
                        if (result == null) return false;
                        originalBalance = Convert.ToDecimal(result);
                    }

                    if (amountToReduce <= 0)
                        throw new ArgumentException("Amount to reduce must be greater than zero.");

                    if (originalBalance < amountToReduce)
                        throw new InvalidOperationException($"Cannot reduce {amountToReduce}. Current balance is {originalBalance}.");

                    decimal newBalance = originalBalance - amountToReduce;

                    // Update item balance
                    using (var cmd = new SqlCommand(@"
                UPDATE swiftFin_Items
                   SET InventoryBalance = @NewBalance
                 WHERE Id = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@NewBalance", newBalance);
                        cmd.ExecuteNonQuery();
                    }

                    // Insert StockJournal entry
                    using (var logCmd = new SqlCommand(@"
                INSERT INTO swiftFin_StockJournal
                (Id, ItemId, ItemNo, Description, ActionType, Quantity, OriginalBalance, NewBalance, CreatedBy, CreatedDate)
                SELECT NEWID(), Id, ItemNo, Description, 'Reduce', @Quantity, @OriginalBalance, @NewBalance, 'System', GETDATE()
                FROM swiftFin_Items WHERE Id=@Id", conn))
                    {
                        logCmd.Parameters.AddWithValue("@Id", id);
                        logCmd.Parameters.AddWithValue("@Quantity", amountToReduce);
                        logCmd.Parameters.AddWithValue("@OriginalBalance", originalBalance);
                        logCmd.Parameters.AddWithValue("@NewBalance", newBalance);
                        logCmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine("SQL error while reducing InventoryBalance: " + ex.Message);
                return false;
            }
        }





        // Delete item
        public bool Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"DELETE FROM swiftFin_Items WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }


        public List<Item> GetFiltered(DateTime? asAtDate, Guid? locationId)
        {
            var list = new List<Item>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, ItemId, ItemNo, Description, 
                                ItemCategoryId, CategoryDescription,
                                UnitOfMeasureId, UnitOfMeasureDescription,
                                LocationId, LocationDescription,
                                InventoryBalance, CostingMethod, SequentialId, 
                                CreatedBy, CreatedDate
                         FROM swiftFin_Items
                         WHERE (CreatedDate <= @AsAtDate OR @AsAtDate IS NULL)
                           AND (LocationId = @LocationId OR @LocationId IS NULL)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@AsAtDate", (object)asAtDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LocationId", (object)locationId ?? DBNull.Value);

                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Item
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        ItemId = reader["ItemId"].ToString(),
                        ItemNo = reader["ItemNo"].ToString(),
                        Description = reader["Description"].ToString(),
                        ItemCategoryId = reader.GetGuid(reader.GetOrdinal("ItemCategoryId")),
                        CategoryDescription = reader["CategoryDescription"].ToString(),
                        UnitOfMeasureId = reader.GetGuid(reader.GetOrdinal("UnitOfMeasureId")),
                        UnitOfMeasureDescription = reader["UnitOfMeasureDescription"].ToString(),
                        LocationId = reader.GetGuid(reader.GetOrdinal("LocationId")),
                        LocationDescription = reader["LocationDescription"].ToString(),
                        InventoryBalance = Convert.ToDecimal(reader["InventoryBalance"]),
                        CostingMethod = reader["CostingMethod"]?.ToString(),
                        SequentialId = reader.GetGuid(reader.GetOrdinal("SequentialId")),
                        CreatedBy = reader["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    });
                }
            }

            return list;
        }


        public List<StockJournal> GetStockJournalByItem(Guid itemId)
        {
            var list = new List<StockJournal>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, ItemId, ItemNo, Description, ActionType, Quantity,
                                OriginalBalance, NewBalance, CreatedBy, CreatedDate
                         FROM swiftFin_StockJournal
                         WHERE ItemId = @ItemId
                         ORDER BY CreatedDate DESC";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ItemId", itemId);

                    conn.Open();
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        list.Add(new StockJournal
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("Id")),
                            ItemId = reader.GetGuid(reader.GetOrdinal("ItemId")),
                            ItemNo = reader["ItemNo"].ToString(),
                            Description = reader["Description"].ToString(),
                            ActionType = reader["ActionType"].ToString(),
                            Quantity = Convert.ToDecimal(reader["Quantity"]),
                            OriginalBalance = Convert.ToDecimal(reader["OriginalBalance"]),
                            NewBalance = Convert.ToDecimal(reader["NewBalance"]),
                            CreatedBy = reader["CreatedBy"].ToString(),
                            CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                        });
                    }
                }
            }

            return list;
        }


    }
}
