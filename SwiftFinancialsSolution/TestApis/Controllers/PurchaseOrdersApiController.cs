using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Procurement.Models;

namespace Procurement.ApiControllers
{
    [RoutePrefix("api/purchaseorders")]
    public class PurchaseOrdersApiController : ApiController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        // ================================
        // GET: All Purchase Orders
        // ================================
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetAll()
        {
            var purchaseOrders = new List<PurchaseOrderDto>();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // --- Fetch PO Headers ---
                const string headerSql = @"
            SELECT 
                po.PurchaseOrderId,
                po.PONumber,
                po.SupplierId,
                ISNULL(v.VendorName, 'No Vendor') AS VendorName,
                po.OrderDate,
                po.ExpectedDeliveryDate,
                ISNULL(po.Currency, 'N/A') AS Currency,
                ISNULL(po.Status, 'Pending') AS Status,
                ISNULL(po.TotalAmount, 0) AS TotalAmount,
                ISNULL(CONVERT(NVARCHAR(36), po.CreatedBy), '00000000-0000-0000-0000-000000000000') AS CreatedBy,
                ISNULL(p.ProjectCode, 'N/A') AS ProjectCode,
                ISNULL(p.ProjectId, 0) AS ProjectId,
                ISNULL(p.Description, 'No Project') AS ProjectDescription
            FROM dbo.PurchaseOrders AS po
            LEFT JOIN dbo.Vendors AS v ON po.SupplierId = v.VendorId
            LEFT JOIN dbo.Projects AS p ON po.ProjectId = p.ProjectId
            ORDER BY po.PurchaseOrderId DESC;";

                using (var cmd = new SqlCommand(headerSql, conn))
                using (var rdr = await cmd.ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        purchaseOrders.Add(new PurchaseOrderDto
                        {
                            PurchaseOrderId = rdr["PurchaseOrderId"] != DBNull.Value ? Convert.ToInt64(rdr["PurchaseOrderId"]) : 0,
                            PONumber = rdr["PONumber"] as string,
                            SupplierId = rdr["SupplierId"] != DBNull.Value ? Convert.ToInt64(rdr["SupplierId"]) : 0,
                            SupplierName = rdr["VendorName"] as string,
                            OrderDate = rdr["OrderDate"] != DBNull.Value ? Convert.ToDateTime(rdr["OrderDate"]) : DateTime.MinValue,
                            ExpectedDeliveryDate = rdr["ExpectedDeliveryDate"] != DBNull.Value
                                ? Convert.ToDateTime(rdr["ExpectedDeliveryDate"])
                                : (DateTime?)null,
                            Currency = rdr["Currency"] as string,
                            Status = rdr["Status"] as string,
                            TotalAmount = rdr["TotalAmount"] != DBNull.Value
                                ? Convert.ToDecimal(rdr["TotalAmount"])
                                : 0m,
                            CreatedBy = Guid.TryParse(rdr["CreatedBy"].ToString(), out var createdBy) ? createdBy : Guid.Empty,
                            Projectcode = rdr["ProjectCode"] as string,
                            ProjectId = rdr["ProjectId"] != DBNull.Value ? Convert.ToInt32(rdr["ProjectId"]) : 0,
                            ProjectDescription = rdr["ProjectDescription"] as string,
                            Lines = new List<PurchaseOrderLineDto>()
                        });
                    }
                }

                // --- Fetch PO Lines ---
                const string lineSql = @"
            SELECT 
                pol.PurchaseOrderId,
                pol.LineNumber,
                pol.ItemId,
                pol.ItemDescription,
                pol.QuantityOrdered,
                pol.UnitPrice,
                ISNULL(pol.BudgetLine, 0) AS BudgetLine,
                ISNULL(b.Description, 'No Budget') AS BudgetDescription,
                ISNULL(pol.ReceivedQuantity, 0) AS ReceivedQuantity
            FROM dbo.PurchaseOrderLines pol
            LEFT JOIN dbo.BudgetLines b ON pol.BudgetLine = b.BudgetLineId
            ORDER BY pol.PurchaseOrderId, pol.LineNumber;";

                using (var cmd = new SqlCommand(lineSql, conn))
                using (var rdr = await cmd.ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        var poId = rdr["PurchaseOrderId"] != DBNull.Value ? Convert.ToInt64(rdr["PurchaseOrderId"]) : 0;
                        var po = purchaseOrders.FirstOrDefault(p => p.PurchaseOrderId == poId);

                        if (po != null)
                        {
                            po.Lines.Add(new PurchaseOrderLineDto
                            {
                                LineNumber = rdr["LineNumber"] != DBNull.Value ? Convert.ToInt32(rdr["LineNumber"]) : 0,
                                ItemId = rdr["ItemId"] != DBNull.Value ? Guid.Parse(rdr["ItemId"].ToString()) : (Guid?)null,
                                ItemDescription = rdr["ItemDescription"] as string,
                                QuantityOrdered = rdr["QuantityOrdered"] != DBNull.Value ? Convert.ToDecimal(rdr["QuantityOrdered"]) : 0m,
                                UnitPrice = rdr["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(rdr["UnitPrice"]) : 0m,
                                BudgetLine = rdr["BudgetLine"] != DBNull.Value ? Convert.ToInt32(rdr["BudgetLine"]) : 0,
                                Budgetdescription = rdr["BudgetDescription"] as string,
                                ReceivedQuantity = rdr["ReceivedQuantity"] != DBNull.Value
                                    ? Convert.ToDecimal(rdr["ReceivedQuantity"])
                                    : 0m
                            });
                        }
                    }
                }
            }

            return Ok(purchaseOrders);
        }





        [HttpGet]
        [Route("status/{status}")]
        public async Task<IHttpActionResult> GetByStatus(string status)
        {
            var purchaseOrders = new List<PurchaseOrderDto>();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // --- Fetch PO Headers filtered by Status ---
                const string headerSql = @"
            SELECT 
                po.PurchaseOrderId,
                po.PONumber,
                po.SupplierId,
                ISNULL(v.VendorName, 'No Vendor') AS VendorName,
                po.OrderDate,
                po.ExpectedDeliveryDate,
                ISNULL(po.Currency, 'N/A') AS Currency,
                ISNULL(po.Status, 'Pending') AS Status,
                ISNULL(po.TotalAmount, 0) AS TotalAmount,
                ISNULL(CONVERT(NVARCHAR(36), po.CreatedBy), '00000000-0000-0000-0000-000000000000') AS CreatedBy,
                ISNULL(p.ProjectCode, 'N/A') AS ProjectCode,
                ISNULL(p.ProjectId, 0) AS ProjectId,
                ISNULL(p.Description, 'No Project') AS ProjectDescription
            FROM dbo.PurchaseOrders AS po
            LEFT JOIN dbo.Vendors AS v ON po.SupplierId = v.VendorId
            LEFT JOIN dbo.Projects AS p ON po.ProjectId = p.ProjectId
            WHERE po.Status = @Status
            ORDER BY po.PurchaseOrderId DESC;";

                using (var cmd = new SqlCommand(headerSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Status", status ?? (object)DBNull.Value);

                    using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                        {
                            purchaseOrders.Add(new PurchaseOrderDto
                            {
                                PurchaseOrderId = rdr["PurchaseOrderId"] != DBNull.Value ? Convert.ToInt64(rdr["PurchaseOrderId"]) : 0,
                                PONumber = rdr["PONumber"] as string,
                                SupplierId = rdr["SupplierId"] != DBNull.Value ? Convert.ToInt64(rdr["SupplierId"]) : 0,
                                SupplierName = rdr["VendorName"] as string,
                                OrderDate = rdr["OrderDate"] != DBNull.Value ? Convert.ToDateTime(rdr["OrderDate"]) : DateTime.MinValue,
                                ExpectedDeliveryDate = rdr["ExpectedDeliveryDate"] != DBNull.Value
                                    ? Convert.ToDateTime(rdr["ExpectedDeliveryDate"])
                                    : (DateTime?)null,
                                Currency = rdr["Currency"] as string,
                                Status = rdr["Status"] as string,
                                TotalAmount = rdr["TotalAmount"] != DBNull.Value
                                    ? Convert.ToDecimal(rdr["TotalAmount"])
                                    : 0m,
                                CreatedBy = Guid.TryParse(rdr["CreatedBy"].ToString(), out var createdBy) ? createdBy : Guid.Empty,
                                Projectcode = rdr["ProjectCode"] as string,
                                ProjectId = rdr["ProjectId"] != DBNull.Value ? Convert.ToInt32(rdr["ProjectId"]) : 0,
                                ProjectDescription = rdr["ProjectDescription"] as string,
                                Lines = new List<PurchaseOrderLineDto>()
                            });
                        }
                    }
                }

                // --- Fetch PO Lines for filtered POs ---
                if (purchaseOrders.Any())
                {
                    const string lineSql = @"
                SELECT 
                    pol.PurchaseOrderId, 
                    pol.LineNumber, 
                    pol.ItemId, 
                    pol.ItemDescription,
                    pol.QuantityOrdered, 
                    pol.UnitPrice,
                    ISNULL(pol.BudgetLine, 0) AS BudgetLine,
                    ISNULL(b.Description, 'No Budget') AS BudgetDescription,
                    ISNULL(pol.ReceivedQuantity, 0) AS ReceivedQuantity
                FROM dbo.PurchaseOrderLines pol
                LEFT JOIN dbo.BudgetLines b ON pol.BudgetLine = b.BudgetLineId
                WHERE pol.PurchaseOrderId IN (
                    SELECT PurchaseOrderId FROM dbo.PurchaseOrders WHERE Status = @Status
                )
                ORDER BY pol.PurchaseOrderId, pol.LineNumber;";

                    using (var cmd = new SqlCommand(lineSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", status ?? (object)DBNull.Value);

                        using (var rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                var poId = rdr["PurchaseOrderId"] != DBNull.Value ? Convert.ToInt64(rdr["PurchaseOrderId"]) : 0;
                                var po = purchaseOrders.FirstOrDefault(p => p.PurchaseOrderId == poId);

                                if (po != null)
                                {
                                    po.Lines.Add(new PurchaseOrderLineDto
                                    {
                                        LineNumber = rdr["LineNumber"] != DBNull.Value ? Convert.ToInt32(rdr["LineNumber"]) : 0,
                                        ItemId = rdr["ItemId"] != DBNull.Value ? Guid.Parse(rdr["ItemId"].ToString()) : (Guid?)null,
                                        ItemDescription = rdr["ItemDescription"] as string,
                                        QuantityOrdered = rdr["QuantityOrdered"] != DBNull.Value ? Convert.ToDecimal(rdr["QuantityOrdered"]) : 0m,
                                        UnitPrice = rdr["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(rdr["UnitPrice"]) : 0m,
                                        BudgetLine = rdr["BudgetLine"] != DBNull.Value ? Convert.ToInt32(rdr["BudgetLine"]) : 0,
                                        Budgetdescription = rdr["BudgetDescription"] as string,
                                        ReceivedQuantity = rdr["ReceivedQuantity"] != DBNull.Value
                                            ? Convert.ToDecimal(rdr["ReceivedQuantity"])
                                            : 0m
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return Ok(purchaseOrders);
        }




        // ================================
        // GET: Vendors
        // ================================
        [HttpGet]
        [Route("vendors")]
        public async Task<IHttpActionResult> GetVendors()
        {
            var vendors = new List<object>();
            const string sql = @"
                SELECT VendorId, VendorCode, VendorName, TaxId, Address, Phone, Email, IsActive
                FROM dbo.Vendors
                WHERE IsActive = 1
                ORDER BY VendorName;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        vendors.Add(new
                        {
                            VendorId = reader["VendorId"],
                            VendorCode = reader["VendorCode"].ToString(),
                            VendorName = reader["VendorName"].ToString(),
                            TaxId = reader["TaxId"].ToString(),
                            Address = reader["Address"].ToString(),
                            Phone = reader["Phone"].ToString(),
                            Email = reader["Email"].ToString()
                        });
                    }
                }
            }
            return Ok(vendors);
        }

        // ================================
        // GET: Items
        // ================================
        [HttpGet]
        [Route("items")]
        public async Task<IHttpActionResult> GetItems()
        {
            const string itemSql = @"SELECT ItemId, ItemCode, ItemName, Unit, DefaultPrice 
                                     FROM dbo.Items 
                                     ORDER BY ItemName;";

            var items = new List<ItemDto>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(itemSql, conn))
            {
                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new ItemDto
                        {
                            ItemId = Convert.ToInt32(reader["ItemId"]),
                            ItemCode = reader["ItemCode"]?.ToString(),
                            ItemName = reader["ItemName"]?.ToString(),
                            Unit = reader["Unit"]?.ToString(),
                            DefaultPrice = reader["DefaultPrice"] != DBNull.Value
                                           ? Convert.ToDecimal(reader["DefaultPrice"])
                                           : 0
                        });
                    }
                }
            }
            return Ok(items);
        }

        // ================================
        // POST: Create Purchase Order
        // ================================
        [HttpPost]
        [Route("create")]
        public async Task<IHttpActionResult> Create(PurchaseOrderDto model)
        {
            if (model == null || model.Lines == null || model.Lines.Count == 0)
                return BadRequest("PO must have at least one line item.");

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var tx = conn.BeginTransaction())
                {
                    var poNumber = "PO-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

                    try
                    {
                        // Insert PO Header (includes Project fields)
                        const string insertHeader = @"
                    INSERT INTO dbo.PurchaseOrders 
                        (PONumber, SupplierId, OrderDate, ExpectedDeliveryDate, Status, TotalAmount, Currency, 
                         CreatedBy, CreatedAt, ProjectId, ProjectCode, ProjectDescription)
                    VALUES 
                        (@PONumber, @SupplierId, @OrderDate, @ExpectedDeliveryDate, 'Draft', 0, @Currency, 
                         @CreatedBy, SYSUTCDATETIME(), @ProjectId, @ProjectCode, @ProjectDescription);
                    SELECT CAST(SCOPE_IDENTITY() AS bigint);";

                        long poId;
                        using (var cmd = new SqlCommand(insertHeader, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@PONumber", poNumber);
                            cmd.Parameters.AddWithValue("@SupplierId", model.SupplierId);
                            cmd.Parameters.AddWithValue("@OrderDate", model.OrderDate);
                            cmd.Parameters.AddWithValue("@ExpectedDeliveryDate", (object)model.ExpectedDeliveryDate ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Currency", model.Currency ?? "N/A");
                            cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);

                            // Project details
                            cmd.Parameters.AddWithValue("@ProjectId", model.ProjectId > 0 ? (object)model.ProjectId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProjectCode", model.Projectcode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProjectDescription", model.ProjectDescription ?? (object)DBNull.Value);

                            poId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                        }

                        // Insert PO Lines (includes Budget details)
                        const string insertLine = @"
                    INSERT INTO dbo.PurchaseOrderLines 
                        (PurchaseOrderId, LineNumber, ItemId, ItemDescription, QuantityOrdered, UnitPrice, 
                         BudgetLine, ReceivedQuantity, BudgetDescription)
                    VALUES 
                        (@PurchaseOrderId, @LineNumber, @ItemId, @ItemDescription, @QuantityOrdered, @UnitPrice, 
                         @BudgetLine, @ReceivedQuantity, @BudgetDescription);";

                        foreach (var line in model.Lines)
                        {
                            using (var ln = new SqlCommand(insertLine, conn, tx))
                            {
                                ln.Parameters.AddWithValue("@PurchaseOrderId", poId);
                                ln.Parameters.AddWithValue("@LineNumber", line.LineNumber);
                                ln.Parameters.AddWithValue("@ItemId", (object)line.ItemId ?? DBNull.Value);
                                ln.Parameters.AddWithValue("@ItemDescription", line.ItemDescription ?? string.Empty);
                                ln.Parameters.AddWithValue("@QuantityOrdered", line.QuantityOrdered);
                                ln.Parameters.AddWithValue("@UnitPrice", line.UnitPrice);
                                ln.Parameters.AddWithValue("@BudgetLine", line.BudgetLine > 0 ? (object)line.BudgetLine : DBNull.Value);
                                ln.Parameters.AddWithValue("@ReceivedQuantity", line.ReceivedQuantity);
                                ln.Parameters.AddWithValue("@BudgetDescription", line.Budgetdescription ?? (object)DBNull.Value);

                                await ln.ExecuteNonQueryAsync();
                            }
                        }

                        // Update total amount
                        const string updateTotal = @"
                    UPDATE dbo.PurchaseOrders
                    SET TotalAmount = (
                        SELECT SUM(ISNULL(QuantityOrdered * UnitPrice, 0)) 
                        FROM dbo.PurchaseOrderLines 
                        WHERE PurchaseOrderId = @PoId
                    )
                    WHERE PurchaseOrderId = @PoId;";

                        using (var upd = new SqlCommand(updateTotal, conn, tx))
                        {
                            upd.Parameters.AddWithValue("@PoId", poId);
                            await upd.ExecuteNonQueryAsync();
                        }

                        tx.Commit();

                        return Ok(new
                        {
                            success = true,
                            message = "Purchase Order created successfully!",
                            poId,
                            poNumber,
                            project = new
                            {
                                model.ProjectId,
                                model.Projectcode,
                                model.ProjectDescription
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }
        }


        [HttpPost]
        [Route("create2")]
        public async Task<IHttpActionResult> create2(PurchaseOrderDto model)
        {
            if (model == null || model.Lines == null || model.Lines.Count == 0)
                return BadRequest("PO must have at least one line item.");

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var tx = conn.BeginTransaction())
                {
                    var poNumber = "PO-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

                    try
                    {
                        // Insert PO Header (includes Project fields)
                        const string insertHeader = @"
                    INSERT INTO dbo.PurchaseOrders 
                        (PONumber, SupplierId, OrderDate, ExpectedDeliveryDate, Status, TotalAmount, Currency, 
                         CreatedBy, CreatedAt, ProjectId, ProjectCode, ProjectDescription)
                    VALUES 
                        (@PONumber, @SupplierId, @OrderDate, @ExpectedDeliveryDate, 'Draft', 0, @Currency, 
                         @CreatedBy, SYSUTCDATETIME(), @ProjectId, @ProjectCode, @ProjectDescription);
                    SELECT CAST(SCOPE_IDENTITY() AS bigint);";

                        long poId;
                        using (var cmd = new SqlCommand(insertHeader, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@PONumber", poNumber);
                            cmd.Parameters.AddWithValue("@SupplierId", model.SupplierId);
                            cmd.Parameters.AddWithValue("@OrderDate", model.OrderDate);
                            cmd.Parameters.AddWithValue("@ExpectedDeliveryDate", (object)model.ExpectedDeliveryDate ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Currency", model.Currency ?? "N/A");
                            cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);

                            // Project details
                            cmd.Parameters.AddWithValue("@ProjectId", model.ProjectId > 0 ? (object)model.ProjectId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProjectCode", model.Projectcode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProjectDescription", model.ProjectDescription ?? (object)DBNull.Value);

                            poId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                        }

                        // Insert PO Lines (includes Budget details)
                        const string insertLine = @"
                    INSERT INTO dbo.PurchaseOrderLines 
                        (PurchaseOrderId, LineNumber, ItemId, ItemDescription, QuantityOrdered, UnitPrice, 
                         BudgetLine, ReceivedQuantity, BudgetDescription)
                    VALUES 
                        (@PurchaseOrderId, @LineNumber, @ItemId, @ItemDescription, @QuantityOrdered, @UnitPrice, 
                         @BudgetLine, @ReceivedQuantity, @BudgetDescription);";

                        foreach (var line in model.Lines)
                        {
                            using (var ln = new SqlCommand(insertLine, conn, tx))
                            {
                                ln.Parameters.AddWithValue("@PurchaseOrderId", poId);
                                ln.Parameters.AddWithValue("@LineNumber", line.LineNumber);
                                ln.Parameters.AddWithValue("@ItemId", (object)line.ItemId ?? DBNull.Value);
                                ln.Parameters.AddWithValue("@ItemDescription", line.ItemDescription ?? string.Empty);
                                ln.Parameters.AddWithValue("@QuantityOrdered", line.QuantityOrdered);
                                ln.Parameters.AddWithValue("@UnitPrice", line.UnitPrice);
                                ln.Parameters.AddWithValue("@BudgetLine", line.BudgetLine > 0 ? (object)line.BudgetLine : DBNull.Value);
                                ln.Parameters.AddWithValue("@ReceivedQuantity", line.ReceivedQuantity);
                                ln.Parameters.AddWithValue("@BudgetDescription", line.Budgetdescription ?? (object)DBNull.Value);

                                await ln.ExecuteNonQueryAsync();
                            }
                        }

                        // Update total amount
                        const string updateTotal = @"
                    UPDATE dbo.PurchaseOrders
                    SET TotalAmount = (
                        SELECT SUM(ISNULL(QuantityOrdered * UnitPrice, 0)) 
                        FROM dbo.PurchaseOrderLines 
                        WHERE PurchaseOrderId = @PoId
                    )
                    WHERE PurchaseOrderId = @PoId;";

                        using (var upd = new SqlCommand(updateTotal, conn, tx))
                        {
                            upd.Parameters.AddWithValue("@PoId", poId);
                            await upd.ExecuteNonQueryAsync();
                        }

                        const string updateRfqStatus = @"
    UPDATE RequestForQuotation
    SET Status = @NewStatus      
    WHERE Id = @RfqId;";

                        using (var rfqCmd = new SqlCommand(updateRfqStatus, conn, tx))
                        {
                            rfqCmd.Parameters.AddWithValue("@NewStatus", "ConvertedToPO");
                            rfqCmd.Parameters.AddWithValue("@RfqId", model.PurchaseOrderId);
                            await rfqCmd.ExecuteNonQueryAsync();
                        }

                        tx.Commit();

                        return Ok(new
                        {
                            success = true,
                            message = "Purchase Order created successfully!",
                            poId,
                            poNumber,
                            project = new
                            {
                                model.ProjectId,
                                model.Projectcode,
                                model.ProjectDescription
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }
        }

        // ================================
        // POST: Convert PR to PO
        // ================================
        //[HttpPost]
        //[Route("convertprtopo")]
        //public async Task<IHttpActionResult> ConvertPrToPo(long requisitionId, Guid createdBy)
        //{
        //    const string checkSql = "SELECT Status FROM dbo.PurchaseRequisitions WHERE RequisitionId = @RequisitionId;";

        //    using (var conn = new SqlConnection(_connectionString))
        //    {
        //        await conn.OpenAsync();

        //        using (var checkCmd = new SqlCommand(checkSql, conn))
        //        {
        //            checkCmd.Parameters.AddWithValue("@RequisitionId", requisitionId);

        //            var statusObj = await checkCmd.ExecuteScalarAsync();
        //            if (statusObj == null)
        //                return NotFound();

        //            if (statusObj.ToString() != "Approved")
        //                return BadRequest("PR must be Approved to convert.");

        //            using (var tx = conn.BeginTransaction())
        //            {
        //                try
        //                {
        //                    // Generate PO Number
        //                    var poNumber = "PO-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        //                    // Create PO header
        //                    const string createPo = @"
        //                        INSERT INTO dbo.PurchaseOrders 
        //                            (PONumber, SupplierId, OrderDate, Status, TotalAmount, CreatedBy, CreatedAt)
        //                        VALUES 
        //                            (@PONumber, NULL, SYSUTCDATETIME(), 'Draft', 0, @CreatedBy, SYSUTCDATETIME());
        //                        SELECT SCOPE_IDENTITY();";

        //                    long poId;
        //                    using (var poCmd = new SqlCommand(createPo, conn, tx))
        //                    {
        //                        poCmd.Parameters.AddWithValue("@PONumber", poNumber);
        //                        poCmd.Parameters.AddWithValue("@CreatedBy", createdBy);
        //                        poId = Convert.ToInt64(await poCmd.ExecuteScalarAsync());
        //                    }

        //                    // Fetch PR lines
        //                    var prLines = new List<PurchaseRequisitionLineDto>();
        //                    const string linesSql = @"
        //                        SELECT LineNumber, ItemId, ItemDescription, Quantity, UnitPrice
        //                        FROM dbo.PurchaseRequisitionLines 
        //                        WHERE RequisitionId = @RequisitionId 
        //                        ORDER BY LineNumber;";

        //                    using (var linesCmd = new SqlCommand(linesSql, conn, tx))
        //                    {
        //                        linesCmd.Parameters.AddWithValue("@RequisitionId", requisitionId);
        //                        using (var rdr = await linesCmd.ExecuteReaderAsync())
        //                        {
        //                            while (await rdr.ReadAsync())
        //                            {
        //                                prLines.Add(new PurchaseRequisitionLineDto
        //                                {
        //                                    LineNumber = Convert.ToInt32(rdr["LineNumber"]),
        //                                    ItemId = rdr["ItemId"] != DBNull.Value ? Guid.Parse(rdr["ItemId"].ToString()) : Guid.Empty,
        //                                    ItemDescription = rdr["ItemDescription"].ToString(),
        //                                    Quantity = Convert.ToDecimal(rdr["Quantity"]),
        //                                    UnitPrice = Convert.ToDecimal(rdr["UnitPrice"])
        //                                });
        //                            }
        //                        }
        //                    }

        //                    // Insert into PO lines
        //                    const string insertPOLine = @"
        //                        INSERT INTO dbo.PurchaseOrderLines 
        //                            (PurchaseOrderId, LineNumber, ItemId, ItemDescription, QuantityOrdered, UnitPrice)
        //                        VALUES 
        //                            (@PurchaseOrderId, @LineNumber, @ItemId, @ItemDescription, @QuantityOrdered, @UnitPrice);";

        //                    foreach (var line in prLines)
        //                    {
        //                        using (var insertCmd = new SqlCommand(insertPOLine, conn, tx))
        //                        {
        //                            insertCmd.Parameters.AddWithValue("@PurchaseOrderId", poId);
        //                            insertCmd.Parameters.AddWithValue("@LineNumber", line.LineNumber);
        //                            insertCmd.Parameters.AddWithValue("@ItemId", (object)line.ItemId ?? DBNull.Value);
        //                            insertCmd.Parameters.AddWithValue("@ItemDescription", line.ItemDescription);
        //                            insertCmd.Parameters.AddWithValue("@QuantityOrdered", line.Quantity);
        //                            insertCmd.Parameters.AddWithValue("@UnitPrice", line.UnitPrice);
        //                            await insertCmd.ExecuteNonQueryAsync();
        //                        }
        //                    }

        //                    // Update PO total
        //                    const string updateTotal = @"
        //                        UPDATE dbo.PurchaseOrders
        //                        SET TotalAmount = (
        //                            SELECT SUM(ISNULL(QuantityOrdered * UnitPrice, 0)) 
        //                            FROM dbo.PurchaseOrderLines 
        //                            WHERE PurchaseOrderId = @PoId
        //                        )
        //                        WHERE PurchaseOrderId = @PoId;";
        //                    using (var uCmd = new SqlCommand(updateTotal, conn, tx))
        //                    {
        //                        uCmd.Parameters.AddWithValue("@PoId", poId);
        //                        await uCmd.ExecuteNonQueryAsync();
        //                    }

        //                    // Update PR status
        //                    const string updPr = @"
        //                        UPDATE dbo.PurchaseRequisitions 
        //                        SET Status = 'ConvertedToPO' 
        //                        WHERE RequisitionId = @RequisitionId;";
        //                    using (var up = new SqlCommand(updPr, conn, tx))
        //                    {
        //                        up.Parameters.AddWithValue("@RequisitionId", requisitionId);
        //                        await up.ExecuteNonQueryAsync();
        //                    }

        //                    tx.Commit();
        //                    return Ok(new { success = true, message = $"PR #{requisitionId} converted to PO #{poNumber}", poId });
        //                }
        //                catch (Exception ex)
        //                {
        //                    tx.Rollback();
        //                    return InternalServerError(ex);
        //                }
        //            }
        //        }
        //    }
        //}






        [HttpPost]
        [Route("convertprtopo")]
        public async Task<IHttpActionResult> ConvertPrToPo(int requisitionId, Guid createdBy)
        {
            const string checkSql = "SELECT Status, ProjectId, ProjectDescription FROM dbo.PurchaseRequisitions WHERE RequisitionId = @RequisitionId;";

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                int? projectId = null;
                string projectDescription = null;

                // ✅ Fetch PR header info (status + project details)
                using (var checkCmd = new SqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@RequisitionId", requisitionId);
                    using (var reader = await checkCmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                            return NotFound();

                        var status = reader["Status"]?.ToString();
                        if (status != "Approved")
                            return BadRequest("PR must be Approved to convert.");

                        projectId = reader["ProjectId"] != DBNull.Value ? Convert.ToInt32(reader["ProjectId"]) : (int?)null;
                        projectDescription = reader["ProjectDescription"]?.ToString();
                    }
                }

                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // ✅ Generate PO Number
                        var poNumber = "PO-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

                        // ✅ Insert PO Header
                        const string createPo = @"
                    INSERT INTO dbo.PurchaseOrders 
                        (PONumber, SupplierId, OrderDate, Status, TotalAmount, CreatedBy, CreatedAt, ProjectId, ProjectDescription)
                    VALUES 
                        (@PONumber, NULL, SYSUTCDATETIME(), 'Draft', 0, @CreatedBy, SYSUTCDATETIME(), @ProjectId, @ProjectDescription);
                    SELECT SCOPE_IDENTITY();";

                        long poId;
                        using (var poCmd = new SqlCommand(createPo, conn, tx))
                        {
                            poCmd.Parameters.AddWithValue("@PONumber", poNumber);
                            poCmd.Parameters.AddWithValue("@CreatedBy", createdBy);
                            poCmd.Parameters.AddWithValue("@ProjectId", (object)projectId ?? DBNull.Value);
                            poCmd.Parameters.AddWithValue("@ProjectDescription", (object)projectDescription ?? DBNull.Value);
                            poId = Convert.ToInt64(await poCmd.ExecuteScalarAsync());
                        }

                        // ✅ Fetch PR Lines
                        var prLines = new List<PurchaseRequisitionLineDto>();
                        const string linesSql = @"
                    SELECT LineNumber, ItemId, ItemDescription, Quantity, UnitPrice
                    FROM dbo.PurchaseRequisitionLines 
                    WHERE RequisitionId = @RequisitionId 
                    ORDER BY LineNumber;";

                        using (var linesCmd = new SqlCommand(linesSql, conn, tx))
                        {
                            linesCmd.Parameters.AddWithValue("@RequisitionId", requisitionId);
                            using (var rdr = await linesCmd.ExecuteReaderAsync())
                            {
                                while (await rdr.ReadAsync())
                                {
                                    prLines.Add(new PurchaseRequisitionLineDto
                                    {
                                        LineNumber = Convert.ToInt32(rdr["LineNumber"]),
                                        ItemId = rdr["ItemId"] != DBNull.Value ? Guid.Parse(rdr["ItemId"].ToString()) : Guid.Empty,
                                        ItemDescription = rdr["ItemDescription"].ToString(),
                                        Quantity = Convert.ToDecimal(rdr["Quantity"]),
                                        UnitPrice = Convert.ToDecimal(rdr["UnitPrice"])
                                    });
                                }
                            }
                        }

                        // ✅ Insert PO Lines
                        const string insertPOLine = @"
                    INSERT INTO dbo.PurchaseOrderLines 
                        (PurchaseOrderId, LineNumber, ItemId, ItemDescription, QuantityOrdered, UnitPrice)
                    VALUES 
                        (@PurchaseOrderId, @LineNumber, @ItemId, @ItemDescription, @QuantityOrdered, @UnitPrice);";

                        foreach (var line in prLines)
                        {
                            using (var insertCmd = new SqlCommand(insertPOLine, conn, tx))
                            {
                                insertCmd.Parameters.AddWithValue("@PurchaseOrderId", poId);
                                insertCmd.Parameters.AddWithValue("@LineNumber", line.LineNumber);
                                insertCmd.Parameters.AddWithValue("@ItemId", (object)line.ItemId ?? DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@ItemDescription", line.ItemDescription);
                                insertCmd.Parameters.AddWithValue("@QuantityOrdered", line.Quantity);
                                insertCmd.Parameters.AddWithValue("@UnitPrice", line.UnitPrice);
                                await insertCmd.ExecuteNonQueryAsync();
                            }
                        }

                        // ✅ Update PO Total
                        const string updateTotal = @"
                    UPDATE dbo.PurchaseOrders
                    SET TotalAmount = (
                        SELECT SUM(ISNULL(QuantityOrdered * UnitPrice, 0)) 
                        FROM dbo.PurchaseOrderLines 
                        WHERE PurchaseOrderId = @PoId
                    )
                    WHERE PurchaseOrderId = @PoId;";
                        using (var uCmd = new SqlCommand(updateTotal, conn, tx))
                        {
                            uCmd.Parameters.AddWithValue("@PoId", poId);
                            await uCmd.ExecuteNonQueryAsync();
                        }

                        // ✅ Update PR Status
                        const string updPr = @"
                    UPDATE dbo.PurchaseRequisitions 
                    SET Status = 'ConvertedToPO' 
                    WHERE RequisitionId = @RequisitionId;";
                        using (var up = new SqlCommand(updPr, conn, tx))
                        {
                            up.Parameters.AddWithValue("@RequisitionId", requisitionId);
                            await up.ExecuteNonQueryAsync();
                        }

                        tx.Commit();
                        return Ok(new { success = true, message = $"PR #{requisitionId} converted to PO #{poNumber}", poId });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }
        }


    }
}
