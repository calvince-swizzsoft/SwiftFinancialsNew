//using Procurement.Models;
//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Data;
//using System.Data.SqlClient;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web.Http;
//using System.Web.Http.Cors;
//using TestApis.Models;

//namespace TestApis.Controllers
//{
//    [EnableCors(origins: "*", headers: "*", methods: "*")]
//    [AllowAnonymous]
//    [RoutePrefix("api/procurement")]
//    public class ProcurementController : ApiController
//    {
//        private readonly string _connString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
//        private readonly string baseUrl = "https://4a68aa2818b9.ngrok-free.app/api/items";

//        #region Helpers

//        private static object ApiSuccess(object data, string message = null)
//            => new { success = true, message, data };

//        private static object ApiFail(string message, object data = null)
//            => new { success = false, message, data };

//        private static string TimestampCode(string prefix)
//            => $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}";

//        #endregion

//        #region Vendors

//        [HttpGet]
//        [Route("vendors")]
//        public async Task<IHttpActionResult> GetVendors()
//        {
//            var vendors = new List<VendorDto>();
//            const string sql = @"
//                SELECT VendorId, VendorCode, VendorName, TaxId, Address, Phone, Email, IsActive
//                FROM dbo.Vendors
//                WHERE IsActive = 1
//                ORDER BY VendorName;";

//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var cmd = new SqlCommand(sql, conn))
//                {
//                    await conn.OpenAsync();
//                    using (var rdr = await cmd.ExecuteReaderAsync())
//                    {
//                        while (await rdr.ReadAsync())
//                        {
//                            vendors.Add(new VendorDto
//                            {
//                                VendorId = Convert.ToInt64(rdr["VendorId"]),
//                                VendorCode = rdr["VendorCode"] as string,
//                                VendorName = rdr["VendorName"] as string,
//                                TaxId = rdr["TaxId"] as string,
//                                Address = rdr["Address"] as string,
//                                Phone = rdr["Phone"] as string,
//                                Email = rdr["Email"] as string,
//                                IsActive = Convert.ToBoolean(rdr["IsActive"])
//                            });
//                        }
//                    }
//                }

//                if (!vendors.Any())
//                    return Ok(ApiFail("No vendors found"));

//                return Ok(ApiSuccess(vendors, "Vendors retrieved"));
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        [HttpGet]
//        [Route("vendors/{id:long}")]
//        public async Task<IHttpActionResult> GetVendor(long id)
//        {
//            VendorDto vendor = null;
//            const string sql = @"
//                SELECT VendorId, VendorCode, VendorName, TaxId, Address, Phone, Email, IsActive
//                FROM dbo.Vendors
//                WHERE VendorId = @VendorId;";

//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var cmd = new SqlCommand(sql, conn))
//                {
//                    cmd.Parameters.AddWithValue("@VendorId", id);
//                    await conn.OpenAsync();
//                    using (var rdr = await cmd.ExecuteReaderAsync())
//                    {
//                        if (await rdr.ReadAsync())
//                        {
//                            vendor = new VendorDto
//                            {
//                                VendorId = Convert.ToInt64(rdr["VendorId"]),
//                                VendorCode = rdr["VendorCode"] as string,
//                                VendorName = rdr["VendorName"] as string,
//                                TaxId = rdr["TaxId"] as string,
//                                Address = rdr["Address"] as string,
//                                Phone = rdr["Phone"] as string,
//                                Email = rdr["Email"] as string,
//                                IsActive = Convert.ToBoolean(rdr["IsActive"])
//                            };
//                        }
//                    }
//                }

//                if (vendor == null)
//                    return Ok(ApiFail("Vendor not found"));

//                return Ok(ApiSuccess(vendor, "Vendor retrieved"));
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        [HttpPost]
//        [Route("vendors")]
//        public async Task<IHttpActionResult> CreateVendor([FromBody] VendorDto dto)
//        {
//            if (dto == null) return BadRequest("Invalid vendor payload");

//            const string dupCheck = "SELECT COUNT(1) FROM dbo.Vendors WHERE VendorName = @Name AND (TaxId = @TaxId OR (@TaxId='' AND TaxId IS NULL));";
//            const string insertSql = @"
//                INSERT INTO dbo.Vendors (VendorCode, VendorName, TaxId, Address, Phone, Email, IsActive, CreatedAt)
//                VALUES (@VendorCode, @VendorName, @TaxId, @Address, @Phone, @Email, 1, SYSUTCDATETIME());
//                SELECT CAST(SCOPE_IDENTITY() AS bigint);";

//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var dupCmd = new SqlCommand(dupCheck, conn))
//                using (var cmd = new SqlCommand(insertSql, conn))
//                {
//                    dupCmd.Parameters.AddWithValue("@Name", (object)(dto.VendorName ?? string.Empty));
//                    dupCmd.Parameters.AddWithValue("@TaxId", (object)(dto.TaxId ?? string.Empty));

//                    cmd.Parameters.AddWithValue("@VendorCode", (object)(dto.VendorCode ?? TimestampCode("VND")));
//                    cmd.Parameters.AddWithValue("@VendorName", (object)(dto.VendorName ?? string.Empty));
//                    cmd.Parameters.AddWithValue("@TaxId", (object)(dto.TaxId ?? (object)DBNull.Value) ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@Address", (object)(dto.Address ?? string.Empty));
//                    cmd.Parameters.AddWithValue("@Phone", (object)(dto.Phone ?? string.Empty));
//                    cmd.Parameters.AddWithValue("@Email", (object)(dto.Email ?? string.Empty));

//                    await conn.OpenAsync();
//                    var dup = Convert.ToInt32(await dupCmd.ExecuteScalarAsync());
//                    if (dup > 0) return Ok(ApiFail("Duplicate vendor exists"));

//                    var id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
//                    return Ok(ApiSuccess(new { vendorId = id }, "Vendor created"));
//                }
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        [HttpPut]
//        [Route("vendors/{id:long}")]
//        public async Task<IHttpActionResult> UpdateVendor(long id, [FromBody] VendorDto dto)
//        {
//            if (dto == null) return BadRequest("Invalid vendor payload");

//            const string sql = @"
//                UPDATE dbo.Vendors
//                SET VendorCode = ISNULL(@VendorCode, VendorCode),
//                    VendorName = ISNULL(@VendorName, VendorName),
//                    TaxId = @TaxId,
//                    Address = ISNULL(@Address, Address),
//                    Phone = ISNULL(@Phone, Phone),
//                    Email = ISNULL(@Email, Email),
//                    IsActive = ISNULL(@IsActive, IsActive)
//                WHERE VendorId = @VendorId;";

//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var cmd = new SqlCommand(sql, conn))
//                {
//                    cmd.Parameters.AddWithValue("@VendorId", id);
//                    cmd.Parameters.AddWithValue("@VendorCode", (object)dto.VendorCode ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@VendorName", (object)dto.VendorName ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@TaxId", (object)dto.TaxId ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@Address", (object)dto.Address ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@Phone", (object)dto.Phone ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@Email", (object)dto.Email ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@IsActive", (object)dto.IsActive ?? DBNull.Value);

//                    await conn.OpenAsync();
//                    var rows = await cmd.ExecuteNonQueryAsync();
//                    return Ok(ApiSuccess(new { updated = rows > 0 }, rows > 0 ? "Vendor updated" : "No rows affected"));
//                }
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }


//        // POST: api/vendors
//        [HttpPost]
//        [Route("PostVendor")]
//        public async Task<IHttpActionResult> PostVendor([FromBody] VendorDto model)
//        {
//            if (!ModelState.IsValid)
//                return BadRequest(ModelState);

//            const string dupCheck = @"SELECT COUNT(1) FROM dbo.Vendors 
//                                      WHERE VendorName = @Name 
//                                      AND (TaxId = @TaxId OR (@TaxId='' AND TaxId IS NULL));";

//            const string vendorCodeCheck = @"SELECT COUNT(1) FROM dbo.Vendors WHERE VendorCode = @VendorCode;";

//            const string insertSql = @"INSERT INTO dbo.Vendors 
//                                        (VendorCode, VendorName, TaxId, Address, Phone, Email, IsActive, CreatedAt) 
//                                       VALUES (@VendorCode, @VendorName, @TaxId, @Address, @Phone, @Email, 1, SYSUTCDATETIME()); 
//                                       SELECT CAST(SCOPE_IDENTITY() AS bigint);";

//            using (var conn = new SqlConnection(_connString))
//            {
//                await conn.OpenAsync();
//                using (var dup = new SqlCommand(dupCheck, conn))
//                using (var codeCheck = new SqlCommand(vendorCodeCheck, conn))
//                using (var cmd = new SqlCommand(insertSql, conn))
//                {
//                    // Duplicate vendor check
//                    dup.Parameters.AddWithValue("@Name", model.VendorName ?? string.Empty);
//                    dup.Parameters.AddWithValue("@TaxId", model.TaxId ?? string.Empty);

//                    var existing = Convert.ToInt32(await dup.ExecuteScalarAsync());
//                    if (existing > 0)
//                        return Content(System.Net.HttpStatusCode.Conflict, "Duplicate vendor exists.");

//                    // Generate a unique 4-digit vendor code
//                    string generatedCode = GenerateUniqueVendorCode(conn, codeCheck);

//                    // Insert vendor
//                    cmd.Parameters.AddWithValue("@VendorCode", generatedCode);
//                    cmd.Parameters.AddWithValue("@VendorName", model.VendorName ?? string.Empty);
//                    cmd.Parameters.AddWithValue("@TaxId", (object)model.TaxId ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@Address", model.Address ?? string.Empty);
//                    cmd.Parameters.AddWithValue("@Phone", model.Phone ?? string.Empty);
//                    cmd.Parameters.AddWithValue("@Email", model.Email ?? string.Empty);

//                    var id = Convert.ToInt64(await cmd.ExecuteScalarAsync());

//                    return Ok(new { Message = "Vendor created successfully", VendorId = id, VendorCode = generatedCode });
//                }
//            }

//        }
//        private string GenerateUniqueVendorCode(SqlConnection conn, SqlCommand codeCheck)
//        {
//            var random = new Random();
//            string code;
//            int exists;

//            conn.Open();

//            do
//            {
//                // Generate a random 4-digit number (1000-9999)
//                code = random.Next(1000, 9999).ToString();

//                // Check if code already exists in DB
//                codeCheck.Parameters.Clear();
//                codeCheck.Parameters.AddWithValue("@VendorCode", code);

//                exists = Convert.ToInt32(codeCheck.ExecuteScalar());

//            } while (exists > 0); // Keep generating until we find a unique code

//            return code;
//        }

//        // PUT: api/vendors/{id}
//        [HttpPut]
//        [Route("ChangeVendor/{id:long}")]
//        public async Task<IHttpActionResult> ChangeVendor(long id, [FromBody] VendorDto model)
//        {
//            if (!ModelState.IsValid)
//                return BadRequest(ModelState);

//            using (var conn = new SqlConnection(_connString))
//            {
//                await conn.OpenAsync();
//                using (var tran = conn.BeginTransaction())
//                {
//                    try
//                    {
//                        // Update Vendor
//                        const string updateVendorSql = @"UPDATE dbo.Vendors 
//                                                     SET VendorName = @VendorName,
//                                                         TaxId = @TaxId,
//                                                         Address = @Address,
//                                                         Phone = @Phone,
//                                                         Email = @Email,
//                                                         IsActive = @IsActive
//                                                     WHERE VendorId = @VendorId;";

//                        using (var cmd = new SqlCommand(updateVendorSql, conn, tran))
//                        {
//                            cmd.Parameters.AddWithValue("@VendorId", id);
//                            cmd.Parameters.AddWithValue("@VendorName", model.VendorName ?? string.Empty);
//                            cmd.Parameters.AddWithValue("@TaxId", (object)model.TaxId ?? DBNull.Value);
//                            cmd.Parameters.AddWithValue("@Address", model.Address ?? string.Empty);
//                            cmd.Parameters.AddWithValue("@Phone", model.Phone ?? string.Empty);
//                            cmd.Parameters.AddWithValue("@Email", model.Email ?? string.Empty);
//                            cmd.Parameters.AddWithValue("@IsActive", model.IsActive);
//                            await cmd.ExecuteNonQueryAsync();
//                        }

//                        // Update or Insert Bank Details
//                        foreach (var bank in model.BankDetails)
//                        {
//                            if (bank.VendorBankId == 0)
//                            {
//                                const string insertBankSql = @"INSERT INTO dbo.VendorBankDetails
//                                                           (VendorId, BankName, AccountNumber, Branch, Currency, IsPrimary)
//                                                           VALUES (@VendorId, @BankName, @AccountNumber, @Branch, @Currency, @IsPrimary);";

//                                using (var cmd = new SqlCommand(insertBankSql, conn, tran))
//                                {
//                                    cmd.Parameters.AddWithValue("@VendorId", id);
//                                    cmd.Parameters.AddWithValue("@BankName", bank.BankName);
//                                    cmd.Parameters.AddWithValue("@AccountNumber", bank.AccountNumber);
//                                    cmd.Parameters.AddWithValue("@Branch", bank.Branch);
//                                    cmd.Parameters.AddWithValue("@Currency", bank.Currency);
//                                    cmd.Parameters.AddWithValue("@IsPrimary", bank.IsPrimary);
//                                    await cmd.ExecuteNonQueryAsync();
//                                }
//                            }
//                            else
//                            {
//                                const string updateBankSql = @"UPDATE dbo.VendorBankDetails
//                                                           SET BankName = @BankName,
//                                                               AccountNumber = @AccountNumber,
//                                                               Branch = @Branch,
//                                                               Currency = @Currency,
//                                                               IsPrimary = @IsPrimary
//                                                           WHERE VendorBankId = @VendorBankId;";

//                                using (var cmd = new SqlCommand(updateBankSql, conn, tran))
//                                {
//                                    cmd.Parameters.AddWithValue("@VendorBankId", bank.VendorBankId);
//                                    cmd.Parameters.AddWithValue("@BankName", bank.BankName);
//                                    cmd.Parameters.AddWithValue("@AccountNumber", bank.AccountNumber);
//                                    cmd.Parameters.AddWithValue("@Branch", bank.Branch);
//                                    cmd.Parameters.AddWithValue("@Currency", bank.Currency);
//                                    cmd.Parameters.AddWithValue("@IsPrimary", bank.IsPrimary);
//                                    await cmd.ExecuteNonQueryAsync();
//                                }
//                            }
//                        }

//                        tran.Commit();
//                        return Ok(new { Message = "Vendor updated successfully" });
//                    }
//                    catch (Exception ex)
//                    {
//                        tran.Rollback();
//                        return InternalServerError(ex);
//                    }
//                }
//            }

//        }
//        [HttpDelete]
//        [Route("vendors/{id:long}")]
//        public async Task<IHttpActionResult> DeleteVendor(long id)
//        {
//            const string sql = "UPDATE dbo.Vendors SET IsActive = 0 WHERE VendorId = @VendorId;";
//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var cmd = new SqlCommand(sql, conn))
//                {
//                    cmd.Parameters.AddWithValue("@VendorId", id);
//                    await conn.OpenAsync();
//                    var rows = await cmd.ExecuteNonQueryAsync();
//                    return Ok(ApiSuccess(new { deleted = rows > 0 }, rows > 0 ? "Vendor deactivated" : "Vendor not found"));
//                }
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        #endregion

//        #region Items

//        [HttpGet]
//        [Route("items")]
//        public async Task<IHttpActionResult> GetItems()
//        {
//            var items = new List<ItemDto>();
//            const string sql = "SELECT ItemId, ItemCode, ItemName, Unit, IsStocked, DefaultPrice FROM dbo.Items ORDER BY ItemName;";

//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var cmd = new SqlCommand(sql, conn))
//                {
//                    await conn.OpenAsync();
//                    using (var rdr = await cmd.ExecuteReaderAsync())
//                    {
//                        while (await rdr.ReadAsync())
//                        {
//                            items.Add(new ItemDto
//                            {
//                                ItemId = Convert.ToInt64(rdr["ItemId"]),
//                                ItemCode = rdr["ItemCode"] as string,
//                                ItemName = rdr["ItemName"] as string,
//                                Unit = rdr["Unit"] as string,
//                                IsStocked = Convert.ToBoolean(rdr["IsStocked"]),
//                                DefaultPrice = rdr["DefaultPrice"] != DBNull.Value ? Convert.ToDecimal(rdr["DefaultPrice"]) : 0m
//                            });
//                        }
//                    }
//                }
//                return Ok(ApiSuccess(items, "Items retrieved"));
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        [HttpPost]
//        [Route("items")]
//        public async Task<IHttpActionResult> CreateItem([FromBody] ItemDto dto)
//        {
//            if (dto == null) return BadRequest("Invalid item payload");

//            const string sql = @"
//                INSERT INTO dbo.Items (ItemCode, ItemName, Unit, IsStocked, DefaultPrice, CreatedAt)
//                VALUES (@ItemCode, @ItemName, @Unit, @IsStocked, @DefaultPrice, SYSUTCDATETIME());
//                SELECT CAST(SCOPE_IDENTITY() AS bigint);";

//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var cmd = new SqlCommand(sql, conn))
//                {
//                    cmd.Parameters.AddWithValue("@ItemCode", (object)(dto.ItemCode ?? string.Empty));
//                    cmd.Parameters.AddWithValue("@ItemName", (object)(dto.ItemName ?? string.Empty));
//                    cmd.Parameters.AddWithValue("@Unit", (object)(dto.Unit ?? "pcs"));
//                    cmd.Parameters.AddWithValue("@IsStocked", dto.IsStocked);
//                    cmd.Parameters.AddWithValue("@DefaultPrice", dto.DefaultPrice);

//                    await conn.OpenAsync();
//                    var id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
//                    return Ok(ApiSuccess(new { itemId = id }, "Item created"));
//                }
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        #endregion

//        #region Purchase Requisitions (PR)

//        [HttpGet]
//        [Route("requisitions")]
//        public async Task<IHttpActionResult> GetRequisitions([FromUri] string status = null, [FromUri] int? departmentId = null)
//        {
//            var list = new List<PurchaseRequisitionDto>();
//            var sql = @"
//                SELECT RequisitionId, RequisitionNumber, RequestedBy, DepartmentId, Purpose, RequestDate, Status, TotalAmount
//                FROM dbo.PurchaseRequisitions
//                WHERE (@Status IS NULL OR Status = @Status)
//                  AND (@DepartmentId IS NULL OR DepartmentId = @DepartmentId)
//                ORDER BY RequestDate DESC;";

//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var cmd = new SqlCommand(sql, conn))
//                {
//                    cmd.Parameters.AddWithValue("@Status", (object)status ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("@DepartmentId", (object)departmentId ?? DBNull.Value);

//                    await conn.OpenAsync();
//                    using (var rdr = await cmd.ExecuteReaderAsync())
//                    {
//                        while (await rdr.ReadAsync())
//                        {
//                            list.Add(new PurchaseRequisitionDto
//                            {
//                                RequisitionId = Convert.ToInt64(rdr["RequisitionId"]),
//                                RequisitionNumber = rdr["RequisitionNumber"] as string,
//                                RequestedBy = rdr["RequestedBy"] != DBNull.Value ? Guid.Parse(rdr["RequestedBy"].ToString()) : Guid.Empty,
//                                DepartmentId = Convert.ToInt32(rdr["DepartmentId"]),
//                                Purpose = rdr["Purpose"] as string,
//                                RequestDate = Convert.ToDateTime(rdr["RequestDate"]),
//                                Status = rdr["Status"] as string,
//                                TotalAmount = rdr["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(rdr["TotalAmount"]) : 0m
//                            });
//                        }
//                    }
//                }
//                return Ok(ApiSuccess(list, "Requisitions retrieved"));
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        [HttpGet]
//        [Route("requisitions/{id:long}")]
//        public async Task<IHttpActionResult> GetRequisition(long id)
//        {
//            var dto = new PurchaseRequisitionDto();

//            var headerSql = @"
//                SELECT RequisitionId, RequisitionNumber, RequestedBy, DepartmentId, Purpose, RequestDate, Status, TotalAmount
//                FROM dbo.PurchaseRequisitions
//                WHERE RequisitionId = @RequisitionId;";

//            var linesSql = @"
//                SELECT PRLineId, LineNumber, ItemId, ItemDescription, Quantity, UnitPrice, AccountCode
//                FROM dbo.PurchaseRequisitionLines WHERE RequisitionId = @RequisitionId ORDER BY LineNumber;";

//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var headerCmd = new SqlCommand(headerSql, conn))
//                using (var linesCmd = new SqlCommand(linesSql, conn))
//                {
//                    headerCmd.Parameters.AddWithValue("@RequisitionId", id);
//                    linesCmd.Parameters.AddWithValue("@RequisitionId", id);

//                    await conn.OpenAsync();
//                    using (var rdr = await headerCmd.ExecuteReaderAsync())
//                    {
//                        if (!await rdr.ReadAsync()) return Ok(ApiFail("Requisition not found"));
//                        dto.RequisitionId = Convert.ToInt64(rdr["RequisitionId"]);
//                        dto.RequisitionNumber = rdr["RequisitionNumber"] as string;
//                        dto.RequestedBy = rdr["RequestedBy"] != DBNull.Value ? Guid.Parse(rdr["RequestedBy"].ToString()) : Guid.Empty;
//                        dto.DepartmentId = Convert.ToInt32(rdr["DepartmentId"]);
//                        dto.Purpose = rdr["Purpose"] as string;
//                        dto.RequestDate = Convert.ToDateTime(rdr["RequestDate"]);
//                        dto.Status = rdr["Status"] as string;
//                        dto.TotalAmount = rdr["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(rdr["TotalAmount"]) : 0m;
//                    }

//                    using (var rdr2 = await linesCmd.ExecuteReaderAsync())
//                    {
//                        while (await rdr2.ReadAsync())
//                        {
//                            dto.Lines.Add(new PurchaseRequisitionLineDto
//                            {
//                                PRLineId = Convert.ToInt64(rdr2["PRLineId"]),
//                                RequisitionId = dto.RequisitionId,
//                                LineNumber = Convert.ToInt32(rdr2["LineNumber"]),
//                                ItemId = rdr2["ItemId"] != DBNull.Value ? Convert.ToInt64(rdr2["ItemId"]) : (long?)null,
//                                ItemDescription = rdr2["ItemDescription"] as string,
//                                Quantity = Convert.ToDecimal(rdr2["Quantity"]),
//                                UnitPrice = Convert.ToDecimal(rdr2["UnitPrice"]),
//                                AccountCode = rdr2["AccountCode"] as string
//                            });
//                        }
//                    }
//                }
//                return Ok(ApiSuccess(dto, "Requisition retrieved"));
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        [HttpPost]
//        [Route("requisitions")]
//        public async Task<IHttpActionResult> CreateRequisition([FromBody] PurchaseRequisitionDto dto)
//        {
//            if (dto == null || dto.Lines == null || dto.Lines.Count == 0)
//                return BadRequest("Requisition must contain at least one line");

//            const string insertHeader = @"
//                INSERT INTO dbo.PurchaseRequisitions (RequisitionNumber, RequestedBy, DepartmentId, Purpose, RequestDate, Status, TotalAmount, CreatedAt)
//                VALUES (@RequisitionNumber, @RequestedBy, @DepartmentId, @Purpose, @RequestDate, @Status, @TotalAmount, SYSUTCDATETIME());
//                SELECT CAST(SCOPE_IDENTITY() AS bigint);";

//            const string insertLine = @"
//                INSERT INTO dbo.PurchaseRequisitionLines (RequisitionId, LineNumber, ItemId, ItemDescription, Quantity, UnitPrice, AccountCode)
//                VALUES (@RequisitionId, @LineNumber, @ItemId, @ItemDescription, @Quantity, @UnitPrice, @AccountCode);";

//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var hdrCmd = new SqlCommand(insertHeader, conn))
//                using (var lineCmd = new SqlCommand(insertLine, conn))
//                {
//                    var reqNumber = TimestampCode("PR");

//                    hdrCmd.Parameters.AddWithValue("@RequisitionNumber", reqNumber);
//                    hdrCmd.Parameters.AddWithValue("@RequestedBy", dto.RequestedBy == Guid.Empty ? Guid.NewGuid() : dto.RequestedBy);
//                    hdrCmd.Parameters.AddWithValue("@DepartmentId", dto.DepartmentId);
//                    hdrCmd.Parameters.AddWithValue("@Purpose", (object)(dto.Purpose ?? string.Empty));
//                    hdrCmd.Parameters.AddWithValue("@RequestDate", dto.RequestDate == default ? DateTime.UtcNow : dto.RequestDate);
//                    hdrCmd.Parameters.AddWithValue("@Status", (object)(dto.Status ?? "Draft"));
//                    hdrCmd.Parameters.AddWithValue("@TotalAmount", dto.TotalAmount);

//                    await conn.OpenAsync();
//                    using (var tx = conn.BeginTransaction())
//                    {
//                        hdrCmd.Transaction = tx;
//                        var reqId = Convert.ToInt64(await hdrCmd.ExecuteScalarAsync());

//                        lineCmd.Transaction = tx;
//                        lineCmd.Parameters.Add("@RequisitionId", SqlDbType.BigInt).Value = reqId;
//                        lineCmd.Parameters.Add("@LineNumber", SqlDbType.Int);
//                        lineCmd.Parameters.Add("@ItemId", SqlDbType.BigInt);
//                        lineCmd.Parameters.Add("@ItemDescription", SqlDbType.NVarChar, 1000);
//                        lineCmd.Parameters.Add("@Quantity", SqlDbType.Decimal);
//                        lineCmd.Parameters.Add("@UnitPrice", SqlDbType.Decimal);
//                        lineCmd.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50);

//                        foreach (var line in dto.Lines)
//                        {
//                            lineCmd.Parameters["@LineNumber"].Value = line.LineNumber;
//                            lineCmd.Parameters["@ItemId"].Value = (object)line.ItemId ?? DBNull.Value;
//                            lineCmd.Parameters["@ItemDescription"].Value = (object)(line.ItemDescription ?? string.Empty);
//                            lineCmd.Parameters["@Quantity"].Value = line.Quantity;
//                            lineCmd.Parameters["@UnitPrice"].Value = line.UnitPrice;
//                            lineCmd.Parameters["@AccountCode"].Value = (object)(line.AccountCode ?? string.Empty);
//                            await lineCmd.ExecuteNonQueryAsync();
//                        }

//                        // compute total
//                        const string totalUpdate = @"
//                            UPDATE dbo.PurchaseRequisitions
//                            SET TotalAmount = (SELECT SUM(ISNULL(Quantity*UnitPrice,0)) FROM dbo.PurchaseRequisitionLines WHERE RequisitionId = @RequisitionId)
//                            WHERE RequisitionId = @RequisitionId;";
//                        using (var upd = new SqlCommand(totalUpdate, conn, tx))
//                        {
//                            upd.Parameters.AddWithValue("@RequisitionId", reqId);
//                            await upd.ExecuteNonQueryAsync();
//                        }

//                        tx.Commit();
//                        return Ok(ApiSuccess(new { requisitionId = reqId, requisitionNumber = reqNumber }, "Requisition created"));
//                    }
//                }
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        [HttpPost]
//        [Route("requisitions/{requisitionId:long}/submit")]
//        public async Task<IHttpActionResult> SubmitRequisition(long requisitionId, [FromUri] Guid submittedBy)
//        {
//            const string checkSql = "SELECT Status, TotalAmount, DepartmentId FROM dbo.PurchaseRequisitions WHERE RequisitionId = @RequisitionId;";
//            const string updateSql = "UPDATE dbo.PurchaseRequisitions SET Status = @Status WHERE RequisitionId = @RequisitionId;";
//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var checkCmd = new SqlCommand(checkSql, conn))
//                using (var updCmd = new SqlCommand(updateSql, conn))
//                {
//                    checkCmd.Parameters.AddWithValue("@RequisitionId", requisitionId);
//                    updCmd.Parameters.AddWithValue("@RequisitionId", requisitionId);
//                    await conn.OpenAsync();

//                    string status = null;
//                    using (var rdr = await checkCmd.ExecuteReaderAsync())
//                    {
//                        if (!await rdr.ReadAsync()) return Ok(ApiFail("Requisition not found"));
//                        status = rdr["Status"] as string;
//                    }
//                    if (!string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase))
//                        return Ok(ApiFail("Only Draft requisitions can be submitted"));

//                    updCmd.Parameters.AddWithValue("@Status", "Submitted");
//                    await updCmd.ExecuteNonQueryAsync();

//                    const string insertQueue = @"
//                        INSERT INTO dbo.ApprovalQueue (EntityType, EntityId, ApproverRoleId, Status, AssignedAt)
//                        VALUES ('PR', @RequisitionId, @ApproverRoleId, 'Pending', SYSUTCDATETIME());";
//                    using (var qCmd = new SqlCommand(insertQueue, conn))
//                    {
//                        qCmd.Parameters.AddWithValue("@RequisitionId", requisitionId);
//                        qCmd.Parameters.AddWithValue("@ApproverRoleId", 2);
//                        await qCmd.ExecuteNonQueryAsync();
//                    }

//                    const string hist = @"
//                        INSERT INTO dbo.PRStatusHistory (RequisitionId, OldStatus, NewStatus, ChangedBy, ChangedAt, Comments)
//                        VALUES (@RequisitionId, 'Draft', 'Submitted', @ChangedBy, SYSUTCDATETIME(), @Comments);";
//                    using (var hCmd = new SqlCommand(hist, conn))
//                    {
//                        hCmd.Parameters.AddWithValue("@RequisitionId", requisitionId);
//                        hCmd.Parameters.AddWithValue("@ChangedBy", submittedBy);
//                        hCmd.Parameters.AddWithValue("@Comments", "Submitted by user");
//                        await hCmd.ExecuteNonQueryAsync();
//                    }

//                    return Ok(ApiSuccess(new { submitted = true }, "Requisition submitted"));
//                }
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        [HttpPost]
//        [Route("requisitions/{requisitionId:long}/approve")]
//        public async Task<IHttpActionResult> ApproveRequisition(long requisitionId, [FromUri] Guid approverId, [FromUri] bool approve, [FromUri] string comments = "")
//        {
//            const string findQueue = @"
//                SELECT TOP 1 ApprovalQueueId FROM dbo.ApprovalQueue
//                WHERE EntityType = 'PR' AND EntityId = @RequisitionId AND Status = 'Pending'
//                ORDER BY ApprovalQueueId ASC;";
//            const string updateQueue = @"
//                UPDATE dbo.ApprovalQueue
//                SET Status = @Status, ApproverUserId = @ApproverUserId, ActionAt = SYSUTCDATETIME(), Comments = @Comments
//                WHERE ApprovalQueueId = @ApprovalQueueId;";
//            const string updatePRStatus = "UPDATE dbo.PurchaseRequisitions SET Status = @Status WHERE RequisitionId = @RequisitionId;";

//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var findCmd = new SqlCommand(findQueue, conn))
//                using (var updQueueCmd = new SqlCommand(updateQueue, conn))
//                using (var updPRCmd = new SqlCommand(updatePRStatus, conn))
//                {
//                    findCmd.Parameters.AddWithValue("@RequisitionId", requisitionId);
//                    await conn.OpenAsync();
//                    var queueIdObj = await findCmd.ExecuteScalarAsync();
//                    if (queueIdObj == null) return Ok(ApiFail("No pending approval found"));

//                    var qId = Convert.ToInt64(queueIdObj);

//                    updQueueCmd.Parameters.AddWithValue("@ApprovalQueueId", qId);
//                    updQueueCmd.Parameters.AddWithValue("@Status", approve ? "Approved" : "Rejected");
//                    updQueueCmd.Parameters.AddWithValue("@ApproverUserId", approverId);
//                    updQueueCmd.Parameters.AddWithValue("@Comments", (object)(comments ?? string.Empty));
//                    await updQueueCmd.ExecuteNonQueryAsync();

//                    updPRCmd.Parameters.AddWithValue("@Status", approve ? "Approved" : "Rejected");
//                    updPRCmd.Parameters.AddWithValue("@RequisitionId", requisitionId);
//                    await updPRCmd.ExecuteNonQueryAsync();

//                    const string hist = @"
//                        INSERT INTO dbo.PRStatusHistory (RequisitionId, OldStatus, NewStatus, ChangedBy, ChangedAt, Comments)
//                        VALUES (@RequisitionId, @OldStatus, @NewStatus, @ChangedBy, SYSUTCDATETIME(), @Comments);";
//                    using (var hCmd = new SqlCommand(hist, conn))
//                    {
//                        hCmd.Parameters.AddWithValue("@RequisitionId", requisitionId);
//                        hCmd.Parameters.AddWithValue("@OldStatus", "Submitted");
//                        hCmd.Parameters.AddWithValue("@NewStatus", approve ? "Approved" : "Rejected");
//                        hCmd.Parameters.AddWithValue("@ChangedBy", approverId);
//                        hCmd.Parameters.AddWithValue("@Comments", (object)(comments ?? string.Empty));
//                        await hCmd.ExecuteNonQueryAsync();
//                    }

//                    return Ok(ApiSuccess(new { approved = approve }, approve ? "Requisition approved" : "Requisition rejected"));
//                }
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        #endregion

//        #region Purchase Orders (PO)

//        [HttpPost]
//        [Route("requisitions/{requisitionId:long}/convert-to-po")]
//        public async Task<IHttpActionResult> ConvertPrToPo(long requisitionId, [FromUri] Guid createdBy)
//        {
//            const string checkSql = "SELECT Status FROM dbo.PurchaseRequisitions WHERE RequisitionId = @RequisitionId;";
//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var checkCmd = new SqlCommand(checkSql, conn))
//                {
//                    checkCmd.Parameters.AddWithValue("@RequisitionId", requisitionId);
//                    await conn.OpenAsync();
//                    var statusObj = await checkCmd.ExecuteScalarAsync();
//                    if (statusObj == null) return Ok(ApiFail("Requisition not found"));
//                    var status = statusObj as string;
//                    if (!string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase))
//                        return Ok(ApiFail("Requisition must be Approved to convert"));

//                    using (var tx = conn.BeginTransaction())
//                    {
//                        var poNumber = TimestampCode("PO");
//                        const string createPo = @"
//                            INSERT INTO dbo.PurchaseOrders (PONumber, SupplierId, OrderDate, Status, TotalAmount, CreatedBy, CreatedAt)
//                            VALUES (@PONumber, NULL, SYSUTCDATETIME(), 'Draft', 0, @CreatedBy, SYSUTCDATETIME());
//                            SELECT CAST(SCOPE_IDENTITY() AS bigint);";
//                        long poId;
//                        using (var poCmd = new SqlCommand(createPo, conn, tx))
//                        {
//                            poCmd.Parameters.AddWithValue("@PONumber", poNumber);
//                            poCmd.Parameters.AddWithValue("@CreatedBy", createdBy);
//                            poId = Convert.ToInt64(await poCmd.ExecuteScalarAsync());
//                        }

//                        const string linesSql = @"
//                            SELECT LineNumber, ItemId, ItemDescription, Quantity, UnitPrice
//                            FROM dbo.PurchaseRequisitionLines WHERE RequisitionId = @RequisitionId ORDER BY LineNumber;";
//                        using (var linesCmd = new SqlCommand(linesSql, conn, tx))
//                        {
//                            linesCmd.Parameters.AddWithValue("@RequisitionId", requisitionId);
//                            using (var rdr = await linesCmd.ExecuteReaderAsync())
//                            {
//                                const string insertPOLine = @"
//                                    INSERT INTO dbo.PurchaseOrderLines (PurchaseOrderId, LineNumber, ItemId, ItemDescription, QuantityOrdered, UnitPrice)
//                                    VALUES (@PurchaseOrderId, @LineNumber, @ItemId, @ItemDescription, @QuantityOrdered, @UnitPrice);";
//                                while (await rdr.ReadAsync())
//                                {
//                                    using (var insertCmd = new SqlCommand(insertPOLine, conn, tx))
//                                    {
//                                        insertCmd.Parameters.AddWithValue("@PurchaseOrderId", poId);
//                                        insertCmd.Parameters.AddWithValue("@LineNumber", Convert.ToInt32(rdr["LineNumber"]));
//                                        insertCmd.Parameters.AddWithValue("@ItemId", rdr["ItemId"] != DBNull.Value ? Convert.ToInt64(rdr["ItemId"]) : (object)DBNull.Value);
//                                        insertCmd.Parameters.AddWithValue("@ItemDescription", rdr["ItemDescription"] as string ?? string.Empty);
//                                        insertCmd.Parameters.AddWithValue("@QuantityOrdered", Convert.ToDecimal(rdr["Quantity"]));
//                                        insertCmd.Parameters.AddWithValue("@UnitPrice", Convert.ToDecimal(rdr["UnitPrice"]));
//                                        await insertCmd.ExecuteNonQueryAsync();
//                                    }

//                                    const string reserveSql = @"
//                                        INSERT INTO dbo.InventoryReservations (ReferenceType, ReferenceId, ItemId, Quantity, ReservedAt)
//                                        VALUES ('PO', @PoId, @ItemId, @Qty, SYSUTCDATETIME());";
//                                    using (var resCmd = new SqlCommand(reserveSql, conn, tx))
//                                    {
//                                        resCmd.Parameters.AddWithValue("@PoId", poId);
//                                        resCmd.Parameters.AddWithValue("@ItemId", rdr["ItemId"] != DBNull.Value ? Convert.ToInt64(rdr["ItemId"]) : (object)DBNull.Value);
//                                        resCmd.Parameters.AddWithValue("@Qty", Convert.ToDecimal(rdr["Quantity"]));
//                                        await resCmd.ExecuteNonQueryAsync();
//                                    }
//                                }
//                            }
//                        }

//                        const string updateTotal = @"
//                            UPDATE dbo.PurchaseOrders
//                            SET TotalAmount = (SELECT SUM(ISNULL(QuantityOrdered*UnitPrice,0)) FROM dbo.PurchaseOrderLines WHERE PurchaseOrderId = @PoId)
//                            WHERE PurchaseOrderId = @PoId;";
//                        using (var uCmd = new SqlCommand(updateTotal, conn, tx))
//                        {
//                            uCmd.Parameters.AddWithValue("@PoId", poId);
//                            await uCmd.ExecuteNonQueryAsync();
//                        }

//                        const string updPr = "UPDATE dbo.PurchaseRequisitions SET Status = 'ConvertedToPO' WHERE RequisitionId = @RequisitionId;";
//                        using (var up = new SqlCommand(updPr, conn, tx))
//                        {
//                            up.Parameters.AddWithValue("@RequisitionId", requisitionId);
//                            await up.ExecuteNonQueryAsync();
//                        }

//                        const string audit = "INSERT INTO dbo.AuditTrail (EntityType, EntityId, Action, PerformedBy, Details) VALUES ('PO', @PoId, 'CreatedFromPR', @PerformedBy, @Details);";
//                        using (var aCmd = new SqlCommand(audit, conn, tx))
//                        {
//                            aCmd.Parameters.AddWithValue("@PoId", poId);
//                            aCmd.Parameters.AddWithValue("@PerformedBy", createdBy);
//                            aCmd.Parameters.AddWithValue("@Details", "Created from PR:" + requisitionId);
//                            await aCmd.ExecuteNonQueryAsync();
//                        }

//                        tx.Commit();
//                        return Ok(ApiSuccess(new { purchaseOrderId = poId, poNumber }, "PO created from requisition"));
//                    }
//                }
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        [HttpGet]
//        [Route("po/{id:long}")]
//        public async Task<IHttpActionResult> GetPurchaseOrder(long id)
//        {
//            var dto = new PurchaseOrderDto();
//            const string hdrSql = @"
//                SELECT PurchaseOrderId, PONumber, SupplierId, OrderDate, ExpectedDeliveryDate, Currency, Status, TotalAmount, CreatedBy
//                FROM dbo.PurchaseOrders WHERE PurchaseOrderId = @Id;";
//            const string linesSql = @"
//                SELECT POLineId, LineNumber, ItemId, ItemDescription, QuantityOrdered, UnitPrice, ReceivedQuantity
//                FROM dbo.PurchaseOrderLines WHERE PurchaseOrderId = @Id ORDER BY LineNumber;";
//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var hdr = new SqlCommand(hdrSql, conn))
//                using (var ln = new SqlCommand(linesSql, conn))
//                {
//                    hdr.Parameters.AddWithValue("@Id", id);
//                    ln.Parameters.AddWithValue("@Id", id);
//                    await conn.OpenAsync();
//                    using (var rdr = await hdr.ExecuteReaderAsync())
//                    {
//                        if (!await rdr.ReadAsync()) return Ok(ApiFail("PO not found"));
//                        dto.PurchaseOrderId = Convert.ToInt64(rdr["PurchaseOrderId"]);
//                        dto.PONumber = rdr["PONumber"] as string;
//                        dto.SupplierId = rdr["SupplierId"] != DBNull.Value ? Convert.ToInt64(rdr["SupplierId"]) : 0;
//                        dto.OrderDate = Convert.ToDateTime(rdr["OrderDate"]);
//                        dto.ExpectedDeliveryDate = rdr["ExpectedDeliveryDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(rdr["ExpectedDeliveryDate"]) : null;
//                        dto.Currency = rdr["Currency"] as string;
//                        dto.Status = rdr["Status"] as string;
//                        dto.TotalAmount = rdr["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(rdr["TotalAmount"]) : 0m;
//                        dto.CreatedBy = rdr["CreatedBy"] != DBNull.Value ? Guid.Parse(rdr["CreatedBy"].ToString()) : Guid.Empty;
//                    }

//                    using (var rdr2 = await ln.ExecuteReaderAsync())
//                    {
//                        while (await rdr2.ReadAsync())
//                        {
//                            dto.Lines.Add(new PurchaseOrderLineDto
//                            {
//                                POLineId = Convert.ToInt64(rdr2["POLineId"]),
//                                PurchaseOrderId = dto.PurchaseOrderId,
//                                LineNumber = Convert.ToInt32(rdr2["LineNumber"]),
//                                ItemId = rdr2["ItemId"] != DBNull.Value ? Convert.ToInt64(rdr2["ItemId"]) : (long?)null,
//                                ItemDescription = rdr2["ItemDescription"] as string,
//                                QuantityOrdered = Convert.ToDecimal(rdr2["QuantityOrdered"]),
//                                UnitPrice = Convert.ToDecimal(rdr2["UnitPrice"]),
//                                ReceivedQuantity = rdr2["ReceivedQuantity"] != DBNull.Value ? Convert.ToDecimal(rdr2["ReceivedQuantity"]) : 0m
//                            });
//                        }
//                    }
//                }
//                return Ok(ApiSuccess(dto, "PO retrieved"));
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        [HttpPost]
//        [Route("po/{purchaseOrderId:long}/issue")]
//        public async Task<IHttpActionResult> IssuePurchaseOrder(long purchaseOrderId, [FromUri] Guid issuedBy)
//        {
//            const string sql = "UPDATE dbo.PurchaseOrders SET Status = 'Issued' WHERE PurchaseOrderId = @PurchaseOrderId;";

//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var cmd = new SqlCommand(sql, conn))
//                {
//                    cmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
//                    await conn.OpenAsync();
//                    var rows = await cmd.ExecuteNonQueryAsync();
//                    return Ok(ApiSuccess(new { issued = rows > 0 }, rows > 0 ? "PO issued" : "PO not found"));
//                }
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        #endregion

//        #region Goods Received (GRN)

//        [HttpPost]
//        [Route("grn")]
//        public async Task<IHttpActionResult> CreateGrn([FromBody] GoodsReceivedNoteDto dto)
//        {
//            if (dto == null || dto.Lines == null || dto.Lines.Count == 0)
//                return BadRequest("Invalid GRN payload");

//            const string insertGrn = @"
//                INSERT INTO dbo.GoodsReceivedNotes (GRNNumber, PurchaseOrderId, ReceivedDate, ReceivedBy, Status, CreatedAt)
//                VALUES (@GRNNumber, @PurchaseOrderId, @ReceivedDate, @ReceivedBy, @Status, SYSUTCDATETIME());
//                SELECT CAST(SCOPE_IDENTITY() AS bigint);";
//            const string insertLine = @"
//                INSERT INTO dbo.GoodsReceivedLines (GRNId, POLineId, ReceivedQuantity, ConditionRemarks)
//                VALUES (@GRNId, @POLineId, @ReceivedQuantity, @ConditionRemarks);";
//            const string updatePoLineReceived = @"
//                UPDATE dbo.PurchaseOrderLines
//                SET ReceivedQuantity = ISNULL(ReceivedQuantity,0) + @ReceivedQuantity
//                WHERE POLineId = @POLineId;";

//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var grnCmd = new SqlCommand(insertGrn, conn))
//                using (var lineCmd = new SqlCommand(insertLine, conn))
//                using (var updatePoLine = new SqlCommand(updatePoLineReceived, conn))
//                {
//                    var grnNumber = TimestampCode("GRN");
//                    grnCmd.Parameters.AddWithValue("@GRNNumber", grnNumber);
//                    grnCmd.Parameters.AddWithValue("@PurchaseOrderId", dto.PurchaseOrderId);
//                    grnCmd.Parameters.AddWithValue("@ReceivedDate", dto.ReceivedDate == default ? DateTime.UtcNow : dto.ReceivedDate);
//                    grnCmd.Parameters.AddWithValue("@ReceivedBy", dto.ReceivedBy);
//                    grnCmd.Parameters.AddWithValue("@Status", (object)(dto.Status ?? "Received"));

//                    await conn.OpenAsync();
//                    using (var tx = conn.BeginTransaction())
//                    {
//                        grnCmd.Transaction = tx;
//                        lineCmd.Transaction = tx;
//                        updatePoLine.Transaction = tx;

//                        var grnId = Convert.ToInt64(await grnCmd.ExecuteScalarAsync());

//                        lineCmd.Parameters.Add("@GRNId", SqlDbType.BigInt).Value = grnId;
//                        lineCmd.Parameters.Add("@POLineId", SqlDbType.BigInt);
//                        lineCmd.Parameters.Add("@ReceivedQuantity", SqlDbType.Decimal);
//                        lineCmd.Parameters.Add("@ConditionRemarks", SqlDbType.NVarChar, 500);

//                        updatePoLine.Parameters.Add("@POLineId", SqlDbType.BigInt);
//                        updatePoLine.Parameters.Add("@ReceivedQuantity", SqlDbType.Decimal);

//                        foreach (var line in dto.Lines)
//                        {
//                            lineCmd.Parameters["@POLineId"].Value = line.POLineId;
//                            lineCmd.Parameters["@ReceivedQuantity"].Value = line.ReceivedQuantity;
//                            lineCmd.Parameters["@ConditionRemarks"].Value = (object)(line.ConditionRemarks ?? string.Empty);
//                            await lineCmd.ExecuteNonQueryAsync();

//                            updatePoLine.Parameters["@POLineId"].Value = line.POLineId;
//                            updatePoLine.Parameters["@ReceivedQuantity"].Value = line.ReceivedQuantity;
//                            await updatePoLine.ExecuteNonQueryAsync();
//                        }

//                        // TODO: Optionally update PO status to PartiallyReceived/Closed based on qty checks

//                        tx.Commit();
//                        return Ok(ApiSuccess(new { grnId = grnId, grnNumber = grnNumber }, "GRN created"));
//                    }
//                }
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        #endregion

//        #region Supplier Invoices & Payments (AP)

//        [HttpPost]
//        [Route("invoices")]
//        public async Task<IHttpActionResult> CreateInvoice([FromBody] SupplierInvoiceDto dto)
//        {
//            if (dto == null) return BadRequest("Invalid invoice payload");

//            const string insertInvoice = @"
//                INSERT INTO dbo.SupplierInvoices (InvoiceNumber, VendorId, PurchaseOrderId, InvoiceDate, DueDate, InvoiceAmount, Status, CreatedAt)
//                VALUES (@InvoiceNumber, @VendorId, @PurchaseOrderId, @InvoiceDate, @DueDate, @InvoiceAmount, @Status, SYSUTCDATETIME());
//                SELECT CAST(SCOPE_IDENTITY() AS bigint);";
//            const string insertLine = @"
//                INSERT INTO dbo.SupplierInvoiceLines (InvoiceId, POLineId, Description, Quantity, UnitPrice, Amount)
//                VALUES (@InvoiceId, @POLineId, @Description, @Quantity, @UnitPrice, @Amount);";

//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var invCmd = new SqlCommand(insertInvoice, conn))
//                using (var lineCmd = new SqlCommand(insertLine, conn))
//                {
//                    invCmd.Parameters.AddWithValue("@InvoiceNumber", (object)(dto.InvoiceNumber ?? TimestampCode("INV")));
//                    invCmd.Parameters.AddWithValue("@VendorId", dto.VendorId);
//                    invCmd.Parameters.AddWithValue("@PurchaseOrderId", (object)dto.PurchaseOrderId ?? DBNull.Value);
//                    invCmd.Parameters.AddWithValue("@InvoiceDate", dto.InvoiceDate == default ? DateTime.UtcNow : dto.InvoiceDate);
//                    invCmd.Parameters.AddWithValue("@DueDate", (object)dto.DueDate ?? DBNull.Value);
//                    invCmd.Parameters.AddWithValue("@InvoiceAmount", dto.InvoiceAmount);
//                    invCmd.Parameters.AddWithValue("@Status", (object)(dto.Status ?? "Pending"));

//                    await conn.OpenAsync();
//                    using (var tx = conn.BeginTransaction())
//                    {
//                        invCmd.Transaction = tx;
//                        var invoiceId = Convert.ToInt64(await invCmd.ExecuteScalarAsync());

//                        lineCmd.Transaction = tx;
//                        lineCmd.Parameters.Add("@InvoiceId", SqlDbType.BigInt).Value = invoiceId;
//                        lineCmd.Parameters.Add("@POLineId", SqlDbType.BigInt);
//                        lineCmd.Parameters.Add("@Description", SqlDbType.NVarChar, 1000);
//                        lineCmd.Parameters.Add("@Quantity", SqlDbType.Decimal);
//                        lineCmd.Parameters.Add("@UnitPrice", SqlDbType.Decimal);
//                        lineCmd.Parameters.Add("@Amount", SqlDbType.Decimal);

//                        if (dto.Lines != null)
//                        {
//                            foreach (var line in dto.Lines)
//                            {
//                                lineCmd.Parameters["@POLineId"].Value = (object)line.POLineId ?? DBNull.Value;
//                                lineCmd.Parameters["@Description"].Value = (object)(line.Description ?? string.Empty);
//                                lineCmd.Parameters["@Quantity"].Value = (object)line.Quantity ?? DBNull.Value;
//                                lineCmd.Parameters["@UnitPrice"].Value = (object)line.UnitPrice ?? DBNull.Value;
//                                lineCmd.Parameters["@Amount"].Value = (object)line.Amount ?? DBNull.Value;
//                                await lineCmd.ExecuteNonQueryAsync();
//                            }
//                        }

//                        // TODO: Optionally perform 2-way or 3-way match logic here

//                        tx.Commit();
//                        return Ok(ApiSuccess(new { invoiceId = invoiceId }, "Invoice created"));
//                    }
//                }
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        [HttpPost]
//        [Route("payments")]
//        public async Task<IHttpActionResult> CreatePayment([FromBody] PaymentDto dto)
//        {
//            if (dto == null || dto.Lines == null || dto.Lines.Count == 0)
//                return BadRequest("Invalid payment payload");

//            const string insertPayment = @"
//                INSERT INTO dbo.Payments (PaymentNumber, PaymentDate, PaymentMethod, Amount, CreatedBy, CreatedAt)
//                VALUES (@PaymentNumber, @PaymentDate, @PaymentMethod, @Amount, @CreatedBy, SYSUTCDATETIME());
//                SELECT CAST(SCOPE_IDENTITY() AS bigint);";
//            const string insertPaymentLine = @"
//                INSERT INTO dbo.PaymentLines (PaymentId, InvoiceId, Amount) VALUES (@PaymentId, @InvoiceId, @Amount);";
//            const string updateInvoice = @"
//                UPDATE dbo.SupplierInvoices
//                SET InvoiceAmount = InvoiceAmount - @Amount,
//                    Status = CASE WHEN (InvoiceAmount - @Amount) <= 0 THEN 'Paid' ELSE Status END
//                WHERE InvoiceId = @InvoiceId;";

//            try
//            {
//                using (var conn = new SqlConnection(_connString))
//                using (var pCmd = new SqlCommand(insertPayment, conn))
//                using (var plCmd = new SqlCommand(insertPaymentLine, conn))
//                using (var updInvCmd = new SqlCommand(updateInvoice, conn))
//                {
//                    pCmd.Parameters.AddWithValue("@PaymentNumber", (object)(dto.PaymentNumber ?? TimestampCode("PAY")));
//                    pCmd.Parameters.AddWithValue("@PaymentDate", dto.PaymentDate == default ? DateTime.UtcNow : dto.PaymentDate);
//                    pCmd.Parameters.AddWithValue("@PaymentMethod", (object)(dto.PaymentMethod ?? "EFT"));
//                    pCmd.Parameters.AddWithValue("@Amount", dto.Amount);
//                    pCmd.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy);

//                    await conn.OpenAsync();
//                    using (var tx = conn.BeginTransaction())
//                    {
//                        pCmd.Transaction = tx;
//                        plCmd.Transaction = tx;
//                        updInvCmd.Transaction = tx;

//                        var paymentId = Convert.ToInt64(await pCmd.ExecuteScalarAsync());

//                        plCmd.Parameters.Add("@PaymentId", SqlDbType.BigInt).Value = paymentId;
//                        plCmd.Parameters.Add("@InvoiceId", SqlDbType.BigInt);
//                        plCmd.Parameters.Add("@Amount", SqlDbType.Decimal);

//                        updInvCmd.Parameters.Add("@Amount", SqlDbType.Decimal);
//                        updInvCmd.Parameters.Add("@InvoiceId", SqlDbType.BigInt);

//                        foreach (var line in dto.Lines)
//                        {
//                            plCmd.Parameters["@InvoiceId"].Value = line.InvoiceId;
//                            plCmd.Parameters["@Amount"].Value = line.Amount;
//                            await plCmd.ExecuteNonQueryAsync();

//                            updInvCmd.Parameters["@Amount"].Value = line.Amount;
//                            updInvCmd.Parameters["@InvoiceId"].Value = line.InvoiceId;
//                            await updInvCmd.ExecuteNonQueryAsync();
//                        }

//                        tx.Commit();
//                        return Ok(ApiSuccess(new { paymentId = paymentId }, "Payment created"));
//                    }
//                }
//            }
//            catch (Exception ex) { return InternalServerError(ex); }
//        }

//        #endregion

//        #region DTO Classes

//        public class VendorDto
//        {
//            public long VendorId { get; set; }
//            public string VendorCode { get; set; }
//            public string VendorName { get; set; }
//            public string TaxId { get; set; }
//            public string Address { get; set; }
//            public string Phone { get; set; }
//            public string Email { get; set; }
//            public bool IsActive { get; set; } = true;

//            // Added for tab section
//            public List<VendorBankDetailDto> BankDetails { get; set; } = new List<VendorBankDetailDto>();
//        }

//        public class ItemDto
//        {
//            public long ItemId { get; set; }
//            public string ItemCode { get; set; }
//            public string ItemName { get; set; }
//            public string Unit { get; set; }
//            public bool IsStocked { get; set; } = true;
//            public decimal DefaultPrice { get; set; }
//        }

//        //public class PurchaseRequisitionDto
//        //{
//        //    public long RequisitionId { get; set; }
//        //    public string RequisitionNumber { get; set; }
//        //    public Guid RequestedBy { get; set; }
//        //    public int DepartmentId { get; set; }
//        //    public string Purpose { get; set; }
//        //    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
//        //    public string Status { get; set; } = "Draft";
//        //    public decimal TotalAmount { get; set; }
//        //    public List<PurchaseRequisitionLineDto> Lines { get; set; } = new List<PurchaseRequisitionLineDto>();
//        //}

//        //public class PurchaseRequisitionLineDto
//        //{
//        //    public long PRLineId { get; set; }
//        //    public long RequisitionId { get; set; }
//        //    public int LineNumber { get; set; }
//        //    public long? ItemId { get; set; }
//        //    public string ItemDescription { get; set; }
//        //    public decimal Quantity { get; set; }
//        //    public decimal UnitPrice { get; set; }
//        //    public string AccountCode { get; set; }
//        //    public decimal EstimatedAmount => Quantity * UnitPrice;
//        //}

//        //public class PurchaseOrderDto
//        //{
//        //    public long PurchaseOrderId { get; set; }
//        //    public string PONumber { get; set; }
//        //    public long SupplierId { get; set; }
//        //    public DateTime OrderDate { get; set; }
//        //    public DateTime? ExpectedDeliveryDate { get; set; }
//        //    public string Currency { get; set; }
//        //    public string Status { get; set; }
//        //    public decimal TotalAmount { get; set; }
//        //    public Guid CreatedBy { get; set; }
//        //    public List<PurchaseOrderLineDto> Lines { get; set; } = new List<PurchaseOrderLineDto>();
//        //}

//        //public class PurchaseOrderLineDto
//        //{
//        //    public long POLineId { get; set; }
//        //    public long PurchaseOrderId { get; set; }
//        //    public int LineNumber { get; set; }
//        //    public long? ItemId { get; set; }
//        //    public string ItemDescription { get; set; }
//        //    public decimal QuantityOrdered { get; set; }
//        //    public decimal UnitPrice { get; set; }
//        //    public decimal ReceivedQuantity { get; set; }
//        //    public decimal Amount => QuantityOrdered * UnitPrice;
//        //}

//        //public class GoodsReceivedNoteDto
//        //{
//        //    public long GRNId { get; set; }
//        //    public string GRNNumber { get; set; }
//        //    public long PurchaseOrderId { get; set; }
//        //    public DateTime ReceivedDate { get; set; }
//        //    public Guid ReceivedBy { get; set; }
//        //    public string Status { get; set; }
//        //    public List<GoodsReceivedLineDto> Lines { get; set; } = new List<GoodsReceivedLineDto>();
//        //}

//        //public class GoodsReceivedLineDto
//        //{
//        //    public long GRNLineId { get; set; }
//        //    public long GRNId { get; set; }
//        //    public long POLineId { get; set; }
//        //    public decimal ReceivedQuantity { get; set; }
//        //    public string ConditionRemarks { get; set; }
//        //}

//        public class SupplierInvoiceDto
//        {
//            public long InvoiceId { get; set; }
//            public string InvoiceNumber { get; set; }
//            public long VendorId { get; set; }
//            public long? PurchaseOrderId { get; set; }
//            public DateTime InvoiceDate { get; set; }
//            public DateTime? DueDate { get; set; }
//            public decimal InvoiceAmount { get; set; }
//            public string Status { get; set; }
//            public List<SupplierInvoiceLineDto> Lines { get; set; } = new List<SupplierInvoiceLineDto>();
//        }

//        public class SupplierInvoiceLineDto
//        {
//            public long InvoiceLineId { get; set; }
//            public long InvoiceId { get; set; }
//            public long? POLineId { get; set; }
//            public string Description { get; set; }
//            public decimal? Quantity { get; set; }
//            public decimal? UnitPrice { get; set; }
//            public decimal? Amount { get; set; }
//        }

//        public class PaymentDto
//        {
//            public long PaymentId { get; set; }
//            public string PaymentNumber { get; set; }
//            public DateTime PaymentDate { get; set; }
//            public string PaymentMethod { get; set; }
//            public decimal Amount { get; set; }
//            public Guid CreatedBy { get; set; }
//            public List<PaymentLineDto> Lines { get; set; } = new List<PaymentLineDto>();
//        }

//        public class PaymentLineDto
//        {
//            public long PaymentLineId { get; set; }
//            public long PaymentId { get; set; }
//            public long InvoiceId { get; set; }
//            public decimal Amount { get; set; }
//        }

//        #endregion
//    }
//}
