using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Http;
using TestApis.Models;

namespace Procurement.ApiControllers
{
    [RoutePrefix("api/vendors")]
    public class VendorsApiController : ApiController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        private readonly string baseUrl = "https://4a68aa2818b9.ngrok-free.app/api/items";

        // GET: api/vendors
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetVendors()
        {
            try
            {
                var list = new List<VendorDto>();

                const string vendorSql = @"
            SELECT VendorId, VendorCode, VendorName, TaxId, Address, Phone, Email, IsActive 
            FROM dbo.Vendors 
            WHERE IsActive = 1 
            ORDER BY VendorId DESC;";

                const string bankSql = @"
            SELECT VendorBankId, VendorId, BankName, AccountNumber, Branch, Currency, IsPrimary
            FROM dbo.VendorBankDetails
            WHERE VendorId = @VendorId;";

                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // Load vendors
                    using (var cmd = new SqlCommand(vendorSql, conn))
                    using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                        {
                            list.Add(new VendorDto
                            {
                                VendorId = rdr["VendorId"] != DBNull.Value ? Convert.ToInt64(rdr["VendorId"]) : 0,
                                VendorCode = rdr["VendorCode"] as string,
                                VendorName = rdr["VendorName"] as string,
                                TaxId = rdr["TaxId"] as string,
                                Address = rdr["Address"] as string,
                                Phone = rdr["Phone"] as string,
                                Email = rdr["Email"] as string,
                                IsActive = rdr["IsActive"] != DBNull.Value && Convert.ToBoolean(rdr["IsActive"]),
                                BankDetails = new List<VendorBankDetailDto>() // initialize bank details list
                            });
                        }
                    }

                    // Load bank details for each vendor
                    foreach (var vendor in list)
                    {
                        using (var bankCmd = new SqlCommand(bankSql, conn))
                        {
                            bankCmd.Parameters.AddWithValue("@VendorId", vendor.VendorId);

                            using (var bankRdr = await bankCmd.ExecuteReaderAsync())
                            {
                                while (await bankRdr.ReadAsync())
                                {
                                    vendor.BankDetails.Add(new VendorBankDetailDto
                                    {
                                        VendorBankId = bankRdr["VendorBankId"] != DBNull.Value ? Convert.ToInt64(bankRdr["VendorBankId"]) : 0,
                                        VendorId = bankRdr["VendorId"] != DBNull.Value ? Convert.ToInt64(bankRdr["VendorId"]) : 0,
                                        BankName = bankRdr["BankName"] as string,
                                        AccountNumber = bankRdr["AccountNumber"] as string,
                                        Branch = bankRdr["Branch"] as string,
                                        Currency = bankRdr["Currency"] as string,
                                        IsPrimary = bankRdr["IsPrimary"] != DBNull.Value && Convert.ToBoolean(bankRdr["IsPrimary"])
                                    });
                                }
                            }
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Vendors retrieved successfully",
                    data = list
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error retrieving vendors",
                    error = ex.Message
                });
            }
        }


        // GET: api/vendors/{id}
        [HttpGet]
        [Route("{id:long}")]
        public async Task<IHttpActionResult> GetVendor(long id)
        {
            try
            {
                var vendor = new VendorDto { BankDetails = new List<VendorBankDetailDto>() };

                const string vendorSql = @"SELECT VendorId, VendorCode, VendorName, TaxId, Address, Phone, Email, IsActive
                                           FROM dbo.Vendors WHERE VendorId = @Id;";

                const string bankSql = @"SELECT VendorBankId, VendorId, BankName, AccountNumber, Branch, Currency, IsPrimary
                                         FROM dbo.VendorBankDetails WHERE VendorId = @Id;";

                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // Vendor details
                    using (var cmdVendor = new SqlCommand(vendorSql, conn))
                    {
                        cmdVendor.Parameters.AddWithValue("@Id", id);
                        using (var rdr = await cmdVendor.ExecuteReaderAsync())
                        {
                            if (!await rdr.ReadAsync())
                                return Json(new { success = false, message = "Vendor not found" });

                            vendor.VendorId = Convert.ToInt64(rdr["VendorId"]);
                            vendor.VendorCode = rdr["VendorCode"] as string;
                            vendor.VendorName = rdr["VendorName"] as string;
                            vendor.TaxId = rdr["TaxId"] as string;
                            vendor.Address = rdr["Address"] as string;
                            vendor.Phone = rdr["Phone"] as string;
                            vendor.Email = rdr["Email"] as string;
                            vendor.IsActive = Convert.ToBoolean(rdr["IsActive"]);
                        }
                    }

                    // Bank details
                    using (var cmdBank = new SqlCommand(bankSql, conn))
                    {
                        cmdBank.Parameters.AddWithValue("@Id", id);
                        using (var rdr = await cmdBank.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                vendor.BankDetails.Add(new VendorBankDetailDto
                                {
                                    VendorBankId = Convert.ToInt64(rdr["VendorBankId"]),
                                    VendorId = Convert.ToInt64(rdr["VendorId"]),
                                    BankName = rdr["BankName"] as string,
                                    AccountNumber = rdr["AccountNumber"] as string,
                                    Branch = rdr["Branch"] as string,
                                    Currency = rdr["Currency"] as string,
                                    IsPrimary = Convert.ToBoolean(rdr["IsPrimary"])
                                });
                            }
                        }
                    }
                }

                return Ok(new { success = true, message = "Vendor retrieved successfully", data = vendor });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving vendor", error = ex.Message });
            }
        }

        // POST: api/vendors
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> CreateVendor([FromBody] VendorDto model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                const string dupCheck = @"SELECT COUNT(1) FROM dbo.Vendors 
                                          WHERE VendorName = @Name 
                                          AND (TaxId = @TaxId OR (@TaxId = '' AND TaxId IS NULL));";

                const string vendorCodeCheck = @"SELECT COUNT(1) FROM dbo.Vendors WHERE VendorCode = @VendorCode;";

                const string insertSql = @"INSERT INTO dbo.Vendors 
                                           (VendorCode, VendorName, TaxId, Address, Phone, Email, IsActive, CreatedAt) 
                                           VALUES (@VendorCode, @VendorName, @TaxId, @Address, @Phone, @Email, 1, SYSUTCDATETIME()); 
                                           SELECT CAST(SCOPE_IDENTITY() AS bigint);";

                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (var dup = new SqlCommand(dupCheck, conn))
                    using (var codeCheck = new SqlCommand(vendorCodeCheck, conn))
                    using (var cmd = new SqlCommand(insertSql, conn))
                    {
                        // Duplicate vendor check
                        dup.Parameters.AddWithValue("@Name", model.VendorName ?? string.Empty);
                        dup.Parameters.AddWithValue("@TaxId", model.TaxId ?? string.Empty);

                        var existing = Convert.ToInt32(await dup.ExecuteScalarAsync());
                        if (existing > 0)
                            return Json(new { success = false, message = "Duplicate vendor exists." });

                        // Generate unique vendor code
                        string generatedCode = await GenerateUniqueVendorCodeAsync(conn, codeCheck);

                        // Insert vendor
                        cmd.Parameters.AddWithValue("@VendorCode", generatedCode);
                        cmd.Parameters.AddWithValue("@VendorName", model.VendorName ?? string.Empty);
                        cmd.Parameters.AddWithValue("@TaxId", (object)model.TaxId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", model.Address ?? string.Empty);
                        cmd.Parameters.AddWithValue("@Phone", model.Phone ?? string.Empty);
                        cmd.Parameters.AddWithValue("@Email", model.Email ?? string.Empty);

                        var id = Convert.ToInt64(await cmd.ExecuteScalarAsync());

                        return Ok(new
                        {
                            success = true,
                            message = "Vendor created successfully",
                            data = new { VendorId = id, VendorCode = generatedCode }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating vendor", error = ex.Message });
            }
        }

        // PUT: api/vendors/{id}
        [HttpPut]
        [Route("{id:long}")]
        public async Task<IHttpActionResult> UpdateVendor(long id, [FromBody] VendorDto model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            // Update Vendor
                            const string updateVendorSql = @"UPDATE dbo.Vendors 
                                                             SET VendorName = @VendorName,
                                                                 TaxId = @TaxId,
                                                                 Address = @Address,
                                                                 Phone = @Phone,
                                                                 Email = @Email,
                                                                 IsActive = @IsActive
                                                             WHERE VendorId = @VendorId;";

                            using (var cmd = new SqlCommand(updateVendorSql, conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@VendorId", id);
                                cmd.Parameters.AddWithValue("@VendorName", model.VendorName ?? string.Empty);
                                cmd.Parameters.AddWithValue("@TaxId", (object)model.TaxId ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Address", model.Address ?? string.Empty);
                                cmd.Parameters.AddWithValue("@Phone", model.Phone ?? string.Empty);
                                cmd.Parameters.AddWithValue("@Email", model.Email ?? string.Empty);
                                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Update or Insert Bank Details
                            foreach (var bank in model.BankDetails ?? new List<VendorBankDetailDto>())
                            {
                                if (bank.VendorBankId == 0)
                                {
                                    const string insertBankSql = @"INSERT INTO dbo.VendorBankDetails
                                                                   (VendorId, BankName, AccountNumber, Branch, Currency, IsPrimary)
                                                                   VALUES (@VendorId, @BankName, @AccountNumber, @Branch, @Currency, @IsPrimary);";

                                    using (var cmd = new SqlCommand(insertBankSql, conn, tran))
                                    {
                                        cmd.Parameters.AddWithValue("@VendorId", id);
                                        cmd.Parameters.AddWithValue("@BankName", bank.BankName ?? string.Empty);
                                        cmd.Parameters.AddWithValue("@AccountNumber", bank.AccountNumber ?? string.Empty);
                                        cmd.Parameters.AddWithValue("@Branch", bank.Branch ?? string.Empty);
                                        cmd.Parameters.AddWithValue("@Currency", bank.Currency ?? string.Empty);
                                        cmd.Parameters.AddWithValue("@IsPrimary", bank.IsPrimary);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else
                                {
                                    const string updateBankSql = @"UPDATE dbo.VendorBankDetails
                                                                   SET BankName = @BankName,
                                                                       AccountNumber = @AccountNumber,
                                                                       Branch = @Branch,
                                                                       Currency = @Currency,
                                                                       IsPrimary = @IsPrimary
                                                                   WHERE VendorBankId = @VendorBankId;";

                                    using (var cmd = new SqlCommand(updateBankSql, conn, tran))
                                    {
                                        cmd.Parameters.AddWithValue("@VendorBankId", bank.VendorBankId);
                                        cmd.Parameters.AddWithValue("@BankName", bank.BankName ?? string.Empty);
                                        cmd.Parameters.AddWithValue("@AccountNumber", bank.AccountNumber ?? string.Empty);
                                        cmd.Parameters.AddWithValue("@Branch", bank.Branch ?? string.Empty);
                                        cmd.Parameters.AddWithValue("@Currency", bank.Currency ?? string.Empty);
                                        cmd.Parameters.AddWithValue("@IsPrimary", bank.IsPrimary);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            tran.Commit();
                            return Ok(new { success = true, message = "Vendor updated successfully" });
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            return Json(new { success = false, message = "Error updating vendor", error = ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error connecting to database", error = ex.Message });
            }
        }

        // Helper to generate unique vendor code
        private async Task<string> GenerateUniqueVendorCodeAsync(SqlConnection conn, SqlCommand codeCheck)
        {
            var random = new Random();
            string code;
            int exists;

            do
            {
                code = random.Next(1000, 9999).ToString();

                codeCheck.Parameters.Clear();
                codeCheck.Parameters.AddWithValue("@VendorCode", code);

                exists = Convert.ToInt32(await codeCheck.ExecuteScalarAsync());

            } while (exists > 0);

            return code;
        }
    }
}
