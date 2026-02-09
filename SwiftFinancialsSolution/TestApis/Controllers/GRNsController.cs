using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using Procurement.Models;

namespace Procurement.Controllers
{

    [RoutePrefix("api/grns")]
    public class GRNsController : ApiController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        private readonly string baseUrl = "https://localhost:44327/api/items";

        // GET: api/grns
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetAll()
        {
            var grns = new List<GoodsReceivedNoteDto>();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                const string sql = @"
            SELECT 
                g.GRNId, g.GRNNumber, g.PurchaseOrderId, g.ReceivedDate, g.ReceivedBy, g.Status, g.CreatedAt,
                po.PONumber, po.TotalAmount, po.OrderDate, po.ExpectedDeliveryDate,
                v.VendorName, v.Phone AS VendorPhone, v.Email AS VendorEmail,
                e.Individual_FirstName, e.Individual_LastName,
                gl.GRNLineId, gl.POLineId, gl.ReceivedQuantity, gl.ConditionRemarks,
                pol.ItemDescription, pol.QuantityOrdered, pol.UnitPrice
            FROM dbo.GoodsReceivedNotes g
            LEFT JOIN dbo.PurchaseOrders po ON g.PurchaseOrderId = po.PurchaseOrderId
            LEFT JOIN dbo.Vendors v ON po.SupplierId = v.VendorId
            LEFT JOIN dbo.swiftFin_Customers e ON g.ReceivedBy = e.Id
            LEFT JOIN dbo.GoodsReceivedLines gl ON g.GRNId = gl.GRNId
            LEFT JOIN dbo.PurchaseOrderLines pol ON gl.POLineId = pol.POLineId
            ORDER BY g.PurchaseOrderId DESC;";

                var tempDict = new Dictionary<long, GoodsReceivedNoteDto>();

                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var grnId = reader.GetInt64(0);

                        if (!tempDict.TryGetValue(grnId, out var grn))
                        {
                            grn = new GoodsReceivedNoteDto
                            {
                                GRNId = grnId,
                                GRNNumber = reader.GetString(1),
                                PurchaseOrderId = reader.GetInt64(2),
                                ReceivedDate = reader.GetDateTime(3),
                                ReceivedBy = reader.GetGuid(4),
                                Status = reader.GetString(5),
                                CreatedAt = reader.GetDateTime(6),
                                PONumber = reader.IsDBNull(7) ? null : reader.GetString(7),
                                TotalAmount = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8),
                                OrderDate = reader.IsDBNull(9) ? DateTime.MinValue : reader.GetDateTime(9),
                                ExpectedDeliveryDate = reader.IsDBNull(10) ? DateTime.MinValue : reader.GetDateTime(10),
                                VendorName = reader.IsDBNull(11) ? "" : reader.GetString(11),
                                VendorPhone = reader.IsDBNull(12) ? "" : reader.GetString(12),
                                VendorEmail = reader.IsDBNull(13) ? "" : reader.GetString(13),
                                ReceivedByFirstName = reader.IsDBNull(14) ? "" : reader.GetString(14),
                                ReceivedByLastName = reader.IsDBNull(15) ? "" : reader.GetString(15),
                                Lines = new List<GoodsReceivedLineDto>()
                            };
                            tempDict[grnId] = grn;
                        }

                        if (!reader.IsDBNull(16))
                        {
                            grn.Lines.Add(new GoodsReceivedLineDto
                            {
                                
                                GRNLineId = reader.GetInt64(16),
                                GRNId = grnId,
                                POLineId = reader.GetInt64(17),
                                ReceivedQuantity = reader.GetDecimal(18),
                                ConditionRemarks = reader.IsDBNull(19) ? "" : reader.GetString(19),
                                ItemDescription = reader.IsDBNull(20) ? "" : reader.GetString(20),
                                QuantityOrdered = reader.IsDBNull(21) ? 0 : reader.GetDecimal(21),
                                UnitPrice = reader.IsDBNull(22) ? 0 : reader.GetDecimal(22)
                            });
                        }
                    }
                }

                grns = tempDict.Values.ToList();
            }

            return Ok(grns);
        }





        [HttpPost]
        [Route("{purchaseOrderId:long}/ConvertPurchaseOrderToGRN2/{receivedQuantity:decimal}")]
        public async Task<IHttpActionResult> ConvertPurchaseOrderToGRN2(long purchaseOrderId, decimal receivedQuantity)
        {
            if (receivedQuantity <= 0)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    message = "The received quantity must be greater than zero."
                });
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Fetch the Purchase Order and its lines
                        var poLines = new List<(long POLineId, Guid? ItemId, decimal QuantityOrdered, decimal ReceivedQuantity, string ItemDescription)>();

                        using (var cmd = new SqlCommand(@"
                    SELECT pol.POLineId, pol.ItemId, pol.QuantityOrdered, 
                           ISNULL(pol.ReceivedQuantity, 0) AS ReceivedQuantity, 
                           pol.ItemDescription
                    FROM dbo.PurchaseOrderLines pol
                    INNER JOIN dbo.PurchaseOrders po ON pol.PurchaseOrderId = po.PurchaseOrderId
                    WHERE po.PurchaseOrderId = @PurchaseOrderId;", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (!reader.HasRows)
                                {
                                    return Content(HttpStatusCode.NotFound, new
                                    {
                                        message = "No purchase order lines were found. Please check the Purchase Order ID and try again."
                                    });
                                }

                                while (await reader.ReadAsync())
                                {
                                    poLines.Add((
                                        POLineId: reader.GetInt64(0),
                                        ItemId: reader["ItemId"] != DBNull.Value ? (Guid)reader["ItemId"] : (Guid?)null,
                                        QuantityOrdered: reader.GetDecimal(2),
                                        ReceivedQuantity: reader.GetDecimal(3),
                                        ItemDescription: reader.IsDBNull(4) ? "Unnamed Item" : reader.GetString(4)
                                    ));
                                }
                            }
                        }

                        // 2. Calculate remaining quantity across all lines
                        var totalOrderedQty = poLines.Sum(l => l.QuantityOrdered);
                        var totalAlreadyReceivedQty = poLines.Sum(l => l.ReceivedQuantity);
                        var totalRemainingQty = totalOrderedQty - totalAlreadyReceivedQty;

                        if (totalRemainingQty <= 0)
                        {
                            return Content(HttpStatusCode.BadRequest, new
                            {
                                message = "This purchase order has already been fully received. No quantities remain."
                            });
                        }

                        if (receivedQuantity > totalRemainingQty)
                        {
                            return Content(HttpStatusCode.BadRequest, new
                            {
                                message = "Received quantity exceeds the remaining quantity for this purchase order.",
                                totalOrderedQty,
                                totalAlreadyReceivedQty,
                                totalRemainingQty,
                                attemptedToReceive = receivedQuantity
                            });
                        }

                        // 3. Generate GRN number
                        var grnNumber = $"GRN-{DateTime.UtcNow:yyyyMMddHHmmss}";

                        // 4. Insert GRN Header
                        long grnId;
                        using (var cmd = new SqlCommand(@"
                    INSERT INTO dbo.GoodsReceivedNotes 
                        (GRNNumber, PurchaseOrderId, ReceivedDate, ReceivedBy, Status, CreatedAt)
                    VALUES 
                        (@GRNNumber, @PurchaseOrderId, SYSUTCDATETIME(), @ReceivedBy, @Status, SYSUTCDATETIME());
                    SELECT CAST(SCOPE_IDENTITY() AS bigint);", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@GRNNumber", grnNumber);
                            cmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
                            cmd.Parameters.AddWithValue("@ReceivedBy", Guid.NewGuid()); // Replace with actual user ID
                            cmd.Parameters.AddWithValue("@Status", "Received");

                            grnId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                        }

                        // 5. Allocate the received quantity across lines
                        decimal qtyLeftToAllocate = receivedQuantity;
                        bool allFullyReceived = true;

                        foreach (var line in poLines.Where(l => l.QuantityOrdered > l.ReceivedQuantity))
                        {
                            if (qtyLeftToAllocate <= 0) break;

                            var remainingLineQty = line.QuantityOrdered - line.ReceivedQuantity;
                            var qtyToReceive = Math.Min(remainingLineQty, qtyLeftToAllocate);

                            // Insert GRN Line
                            using (var ln = new SqlCommand(@"
                        INSERT INTO dbo.GoodsReceivedLines 
                            (GRNId, POLineId, ReceivedQuantity, ConditionRemarks)
                        VALUES (@GRNId, @POLineId, @ReceivedQuantity, @ConditionRemarks);", conn, tx))
                            {
                                ln.Parameters.AddWithValue("@GRNId", grnId);
                                ln.Parameters.AddWithValue("@POLineId", line.POLineId);
                                ln.Parameters.AddWithValue("@ReceivedQuantity", qtyToReceive);
                                ln.Parameters.AddWithValue("@ConditionRemarks", $"Received {qtyToReceive} of {line.ItemDescription}");
                                await ln.ExecuteNonQueryAsync();
                            }

                            // Update PO Line
                            using (var upd = new SqlCommand(@"
                        UPDATE dbo.PurchaseOrderLines
                        SET ReceivedQuantity = ISNULL(ReceivedQuantity, 0) + @ReceivedQuantity
                        WHERE POLineId = @POLineId;", conn, tx))
                            {
                                upd.Parameters.AddWithValue("@POLineId", line.POLineId);
                                upd.Parameters.AddWithValue("@ReceivedQuantity", qtyToReceive);
                                await upd.ExecuteNonQueryAsync();
                            }

                            qtyLeftToAllocate -= qtyToReceive;

                            if (line.ReceivedQuantity + qtyToReceive < line.QuantityOrdered)
                                allFullyReceived = false;
                        }

                        // 6. Update Purchase Order Status
                        var newStatus = allFullyReceived ? "Fully Received" : "Partially Received";

                        using (var poStatusCmd = new SqlCommand(@"
                    UPDATE dbo.PurchaseOrders
                    SET Status = @Status
                    WHERE PurchaseOrderId = @PurchaseOrderId;", conn, tx))
                        {
                            poStatusCmd.Parameters.AddWithValue("@Status", newStatus);
                            poStatusCmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
                            await poStatusCmd.ExecuteNonQueryAsync();
                        }

                        // 7. Commit transaction
                        tx.Commit();

                        // 8. Update local inventory and external system
                        try
                        {
                            foreach (var line in poLines.Where(l => l.ItemId.HasValue && l.ReceivedQuantity > 0))
                            {
                                // Fetch current balance
                                decimal currentBalance = 0;
                                using (var balanceCmd = new SqlCommand(@"
                            SELECT ISNULL(SUM(Quantity), 0) 
                            FROM swiftFin_InventoryTransactions 
                            WHERE ItemId = @ItemId;", conn))
                                {
                                    balanceCmd.Parameters.AddWithValue("@ItemId", line.ItemId.Value);
                                    currentBalance = Convert.ToDecimal(await balanceCmd.ExecuteScalarAsync());
                                }

                                // Calculate new balance
                                decimal newBalance = currentBalance + line.ReceivedQuantity;

                                //    // Insert into local inventory transactions
                                //    using (var insertTxnCmd = new SqlCommand(@"
                                //INSERT INTO swiftFin_InventoryTransactions
                                //    (Id, TransactionDate, DocumentNo, ItemId, LocationId, EntryType, Quantity, UnitCost, ReferenceJournalId, SequentialId, CreatedBy, CreatedDate)
                                //VALUES
                                //    (NEWID(), SYSUTCDATETIME(), @DocumentNo, @ItemId, @LocationId, @EntryType, @Quantity, @UnitCost, @ReferenceJournalId, @SequentialId, @CreatedBy, SYSUTCDATETIME());", conn))
                                //    {
                                //        insertTxnCmd.Parameters.AddWithValue("@DocumentNo", grnNumber);
                                //        insertTxnCmd.Parameters.AddWithValue("@ItemId", line.ItemId.Value);
                                //        insertTxnCmd.Parameters.AddWithValue("@LocationId", 1); // Default or actual location
                                //        insertTxnCmd.Parameters.AddWithValue("@EntryType", "Receipt");
                                //        insertTxnCmd.Parameters.AddWithValue("@Quantity", line.ReceivedQuantity);
                                //        insertTxnCmd.Parameters.AddWithValue("@UnitCost", 0); // Replace with actual cost if needed
                                //        insertTxnCmd.Parameters.AddWithValue("@ReferenceJournalId", DBNull.Value);
                                //        insertTxnCmd.Parameters.AddWithValue("@SequentialId", 0);
                                //        insertTxnCmd.Parameters.AddWithValue("@CreatedBy", Guid.NewGuid()); // Replace with actual user ID

                                //        await insertTxnCmd.ExecuteNonQueryAsync();
                                //    }

                                // Push update to external system
                                var balanceDto = new { InventoryBalance = receivedQuantity };

                                using (var client = new HttpClient())
                                {
                                    var json = JsonConvert.SerializeObject(balanceDto);
                                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                                    var response = await client.PutAsync($"{baseUrl}/{line.ItemId}/balance", content);

                                    if (!response.IsSuccessStatusCode)
                                    {
                                        Console.WriteLine($"Failed to update external inventory for Item {line.ItemId}: {response.StatusCode}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log any errors during inventory updates
                            Console.WriteLine($"Inventory update failed: {ex.Message}");
                        }

                        // 9. Return success response
                        return Ok(new
                        {
                            message = $"GRN #{grnNumber} created successfully for Purchase Order #{purchaseOrderId}.",
                            grnId,
                            grnNumber,
                            purchaseOrderStatus = newStatus,
                            totalRemainingQtyBefore = totalRemainingQty,
                            totalReceived = receivedQuantity,
                            remainingAfter = totalRemainingQty - receivedQuantity
                        });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return Content(HttpStatusCode.InternalServerError, new
                        {
                            message = "An unexpected error occurred while creating the GRN.",
                            details = ex.Message
                        });
                    }
                }
            }
        }






        [HttpPost]
        [Route("{id:long}/ReceiveGoods")]
        public async Task<IHttpActionResult> ReceiveGoods(long id, [FromBody] GoodsReceivedNoteDto grn)
        {
            if (grn == null || grn.Lines == null || !grn.Lines.Any())
            {
                return BadRequest("No lines provided in the GRN.");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // Fetch PO lines to validate quantities
                var poLines = new Dictionary<long, (decimal OrderedQty, decimal ReceivedQty)>();

                const string fetchPoSql = @"
            SELECT 
                pol.POLineId, 
                pol.QuantityOrdered, 
                ISNULL(pol.ReceivedQuantity, 0) AS ReceivedQuantity
            FROM dbo.PurchaseOrderLines pol
            WHERE pol.PurchaseOrderId = @PurchaseOrderId";

                using (var cmd = new SqlCommand(fetchPoSql, conn))
                {
                    cmd.Parameters.AddWithValue("@PurchaseOrderId", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var lineId = reader.GetInt64(reader.GetOrdinal("POLineId"));
                            var orderedQty = reader.GetDecimal(reader.GetOrdinal("QuantityOrdered"));
                            var receivedQty = reader.GetDecimal(reader.GetOrdinal("ReceivedQuantity"));

                            poLines[lineId] = (orderedQty, receivedQty);
                        }
                    }
                }

                // Validate GRN line quantities
                foreach (var line in grn.Lines)
                {
                    if (!poLines.ContainsKey(line.POLineId))
                    {
                        return BadRequest($"POLineId {line.POLineId} does not exist for this Purchase Order.");
                    }

                    var (orderedQty, alreadyReceived) = poLines[line.POLineId];
                    var remainingQty = orderedQty - alreadyReceived;

                    if (line.ReceivedQuantity <= 0)
                    {
                        return BadRequest($"Received quantity for POLineId {line.POLineId} must be greater than zero.");
                    }

                    if (line.ReceivedQuantity > remainingQty)
                    {
                        return BadRequest(
                            $"Received quantity for POLineId {line.POLineId} exceeds the remaining quantity. " +
                            $"Ordered: {orderedQty}, Already Received: {alreadyReceived}, Remaining: {remainingQty}."
                        );
                    }
                }

                // Save GRN
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Insert GRN Header
                        const string insertGrnSql = @"
                    INSERT INTO dbo.GoodsReceivedNotes (PurchaseOrderId, ReceivedDate, ReceivedBy, Remarks)
                    OUTPUT INSERTED.GRNId
                    VALUES (@PurchaseOrderId, @ReceivedDate, @ReceivedBy, @Remarks);";

                        long grnId;
                        using (var insertGrnCmd = new SqlCommand(insertGrnSql, conn, tran))
                        {
                            insertGrnCmd.Parameters.AddWithValue("@PurchaseOrderId", grn.PurchaseOrderId);
                            insertGrnCmd.Parameters.AddWithValue("@ReceivedDate", grn.ReceivedDate);
                            insertGrnCmd.Parameters.AddWithValue("@ReceivedBy", grn.ReceivedBy);
                            insertGrnCmd.Parameters.AddWithValue("@Remarks", grn.Lines[0].ConditionRemarks);

                            grnId = (long)await insertGrnCmd.ExecuteScalarAsync();
                        }

                        // Insert GRN Lines and update PO Line quantities
                        foreach (var line in grn.Lines)
                        {
                            const string insertGrnLineSql = @"
                        INSERT INTO dbo.GoodsReceivedNoteLines (GRNId, POLineId, ReceivedQuantity, ConditionRemarks)
                        VALUES (@GRNId, @POLineId, @ReceivedQuantity, @ConditionRemarks);";

                            using (var insertGrnLineCmd = new SqlCommand(insertGrnLineSql, conn, tran))
                            {
                                insertGrnLineCmd.Parameters.AddWithValue("@GRNId", grnId);
                                insertGrnLineCmd.Parameters.AddWithValue("@POLineId", line.POLineId);
                                insertGrnLineCmd.Parameters.AddWithValue("@ReceivedQuantity", line.ReceivedQuantity);
                                insertGrnLineCmd.Parameters.AddWithValue("@ConditionRemarks", line.ConditionRemarks ?? (object)DBNull.Value);

                                await insertGrnLineCmd.ExecuteNonQueryAsync();
                            }

                            const string updatePoLineSql = @"
                        UPDATE dbo.PurchaseOrderLines
                        SET ReceivedQuantity = ReceivedQuantity + @ReceivedQuantity
                        WHERE POLineId = @POLineId;";

                            using (var updatePoLineCmd = new SqlCommand(updatePoLineSql, conn, tran))
                            {
                                updatePoLineCmd.Parameters.AddWithValue("@POLineId", line.POLineId);
                                updatePoLineCmd.Parameters.AddWithValue("@ReceivedQuantity", line.ReceivedQuantity);

                                await updatePoLineCmd.ExecuteNonQueryAsync();
                            }
                        }

                        // Commit transaction
                        tran.Commit();

                        return Ok(new { Message = "Goods received successfully", GRNId = grnId });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }
        }






        // POST: api/grns
        [HttpPost]
        [Route("create")]
        public async Task<IHttpActionResult> Create(GoodsReceivedNoteDto model)
        {
            if (model == null || model.Lines == null || model.Lines.Count == 0)
            {
                return BadRequest("GRN must contain at least one line item.");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var poLines = new Dictionary<long, decimal>();

                        using (var cmd = new SqlCommand(@"
                            SELECT POLineId, QuantityOrdered, ISNULL(ReceivedQuantity, 0) AS ReceivedQuantity
                            FROM dbo.PurchaseOrderLines
                            WHERE PurchaseOrderId = @PurchaseOrderId;", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@PurchaseOrderId", model.PurchaseOrderId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var poLineId = reader.GetInt64(0);
                                    var quantityOrdered = reader.GetDecimal(1);
                                    var receivedQuantity = reader.GetDecimal(2);
                                    poLines[poLineId] = quantityOrdered - receivedQuantity;
                                }
                            }
                        }

                        foreach (var line in model.Lines)
                        {
                            if (!poLines.ContainsKey(line.POLineId))
                            {
                                return BadRequest($"Invalid PO line ID: {line.POLineId}");
                            }

                            var remainingQty = poLines[line.POLineId];
                            if (line.ReceivedQuantity > remainingQty)
                            {
                                return BadRequest($"Cannot receive more than remaining quantity for item {line.POLineId}. Remaining: {remainingQty}");
                            }
                        }

                        var grnNumber = "GRN-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

                        long grnId;
                        using (var cmd = new SqlCommand(@"
                            INSERT INTO dbo.GoodsReceivedNotes (GRNNumber, PurchaseOrderId, ReceivedDate, ReceivedBy, Status, CreatedAt)
                            VALUES (@GRNNumber, @PurchaseOrderId, @ReceivedDate, @ReceivedBy, @Status, SYSUTCDATETIME());
                            SELECT CAST(SCOPE_IDENTITY() AS bigint);", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@GRNNumber", grnNumber);
                            cmd.Parameters.AddWithValue("@PurchaseOrderId", model.PurchaseOrderId);
                            cmd.Parameters.AddWithValue("@ReceivedDate", model.ReceivedDate);
                            cmd.Parameters.AddWithValue("@ReceivedBy", model.ReceivedBy);
                            cmd.Parameters.AddWithValue("@Status", "Received");
                            grnId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                        }

                        bool allFullyReceived = true;

                        foreach (var line in model.Lines)
                        {
                            using (var ln = new SqlCommand(@"
                                INSERT INTO dbo.GoodsReceivedLines (GRNId, POLineId, ReceivedQuantity, ConditionRemarks)
                                VALUES (@GRNId, @POLineId, @ReceivedQuantity, @ConditionRemarks);", conn, tx))
                            {
                                ln.Parameters.AddWithValue("@GRNId", grnId);
                                ln.Parameters.AddWithValue("@POLineId", line.POLineId);
                                ln.Parameters.AddWithValue("@ReceivedQuantity", line.ReceivedQuantity);
                                ln.Parameters.AddWithValue("@ConditionRemarks", line.ConditionRemarks ?? string.Empty);
                                await ln.ExecuteNonQueryAsync();
                            }

                            using (var upd = new SqlCommand(@"
                                UPDATE dbo.PurchaseOrderLines
                                SET ReceivedQuantity = ISNULL(ReceivedQuantity, 0) + @ReceivedQuantity
                                WHERE POLineId = @POLineId;", conn, tx))
                            {
                                upd.Parameters.AddWithValue("@POLineId", line.POLineId);
                                upd.Parameters.AddWithValue("@ReceivedQuantity", line.ReceivedQuantity);
                                await upd.ExecuteNonQueryAsync();
                            }

                            if (line.ReceivedQuantity < poLines[line.POLineId])
                            {
                                allFullyReceived = false;
                            }
                        }

                        var newStatus = allFullyReceived ? "Fully Received" : "Partially Received";

                        using (var poStatusCmd = new SqlCommand(@"
                            UPDATE dbo.PurchaseOrders
                            SET Status = @Status
                            WHERE PurchaseOrderId = @PurchaseOrderId;", conn, tx))
                        {
                            poStatusCmd.Parameters.AddWithValue("@Status", newStatus);
                            poStatusCmd.Parameters.AddWithValue("@PurchaseOrderId", model.PurchaseOrderId);
                            await poStatusCmd.ExecuteNonQueryAsync();
                        }

                        tx.Commit();
                        return Ok(new { grnId, status = newStatus, message = "GRN created successfully" });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return InternalServerError(new Exception("Error creating GRN: " + ex.Message));
                    }
                }
            }
        }
    }
}
