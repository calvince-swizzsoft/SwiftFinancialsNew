using Procurement.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using TestApis.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftFinancials.Controllers
{
    #region MODELS

    public class RequestForQuotation
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public string ItemDescription { get; set; }
        public int Quantity { get; set; }
        public DateTime ExpectedDeliveryDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; }
        public int projectid { get; set; }

        // Extended Fields
        public string RFQNumber { get; set; }
        public string Priority { get; set; }
        public string Department { get; set; }
        public string RequestedBy { get; set; }
        public decimal EstimatedBudget { get; set; }
        public string DeliveryLocation { get; set; }
        public string AdditionalNotes { get; set; }
        public string FileBase64 { get; set; }


        public List<RFQLine> Lines { get; set; } = new List<RFQLine>();
        public List<int> VendorIds { get; set; } = new List<int>();
    }
    public class RFQLine
    {
        public int Id { get; set; }
        public int RFQId { get; set; }
        public int projectid { get; set; }
        public string projectDescription { get; set; }
        public string BudgetDescription { get; set; }

        public long? BudgetLineId { get; set; }
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        public int Quantity { get; set; }
        public string UnitOfMeasure { get; set; }
        public decimal EstimatedUnitPrice { get; set; }
        public decimal EstimatedTotal => Quantity * EstimatedUnitPrice;
        public DateTime CreatedDate { get; set; }
        public string Notes { get; set; }
    }


    public class RFQApprovalRequest
    {
        public int RFQId { get; set; }
        public List<int> VendorIds { get; set; } = new List<int>();
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string ErrorCode { get; set; }
    }

   

    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [AllowAnonymous]
    [RoutePrefix("api/rfq")]
    #endregion
    public class RFQController : ApiController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        #region RFQ CREATION

        [HttpPost]
        [Route("CreateRFQ")]
        public IHttpActionResult CreateRFQ([FromBody] RequestForQuotation rfq)
        {

            if (rfq == null || rfq.Lines == null || rfq.Lines.Count == 0 || rfq.VendorIds == null || rfq.VendorIds.Count == 0)
                return BadRequestResponse("Invalid RFQ data  missing vendors or line items.", "INVALID_INPUT");

            var notifiedVendors = new List<object>();
            rfq.RFQNumber = "RFQ-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    foreach (var vendorId in rfq.VendorIds)
                    {
                        string vendorName = GetVendorName(vendorId);
                        string vendorEmail = GetVendorEmail(vendorId);
                        int newRFQId = 0;

                        //  Transaction only wraps database work
                        using (var transaction = conn.BeginTransaction())
                        {
                            try
                            {
                                newRFQId = InsertRFQ(conn, transaction, rfq, vendorId, vendorName);
                                InsertRFQLines(conn, transaction, newRFQId, rfq.Lines);
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                notifiedVendors.Add(new { Vendor = vendorName, Email = vendorEmail, Status = "DB Insert Failed", Error = ex.Message });
                                continue; // Move to next vendor
                            }
                        }

                        //  Send email AFTER transaction is done
                        try
                        {
                            rfq.VendorName = vendorName;
                            string emailStatus = SendVendorNotification(vendorEmail, vendorName, rfq);
                            notifiedVendors.Add(new { Vendor = vendorName, Email = vendorEmail, Status = emailStatus });
                        }
                        catch (Exception ex)
                        {
                            notifiedVendors.Add(new { Vendor = vendorName, Email = vendorEmail, Status = "Email Failed", Error = ex.Message });
                        }
                    }
                }

                return OkResponse("RFQs created and notifications sent.", notifiedVendors);
            }
            catch (Exception ex)
            {
                return ErrorResponse("Error creating RFQs or sending emails.", "RFQ_MULTIPLE_ERROR", ex);
            }
        }


        [HttpPost]
        [Route("CreateRFQ2")]
        public IHttpActionResult CreateRFQ2([FromBody] RequestForQuotation rfq)
        {
            if (rfq == null)
                return BadRequestResponse("RFQ payload is required.", "INVALID_INPUT");

            if (rfq.Lines == null || !rfq.Lines.Any())
                return BadRequestResponse("RFQ Lines are required.", "INVALID_INPUT");

            rfq.RFQNumber = "RFQ-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            var notifiedVendors = new List<object>();
            var hasVendors = rfq.VendorIds != null && rfq.VendorIds.Any();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // =========================================
                    // CASE 1 — NO VENDORS (SINGLE GENERIC RFQ)
                    // =========================================
                    if (!hasVendors)
                    {
                        using (var tx = conn.BeginTransaction())
                        {
                            try
                            {
                                var newRFQId = InsertRFQ2(conn, tx, rfq, 0, null);
                                InsertRFQLines(conn, tx, newRFQId, rfq.Lines);

                                tx.Commit();

                                return OkResponse("RFQ created successfully (no vendor assigned).", new
                                {
                                    RFQId = newRFQId,
                                    RFQNumber = rfq.RFQNumber,
                                    VendorsProcessed = 0
                                });
                            }
                            catch (Exception ex)
                            {
                                tx.Rollback();
                                return ErrorResponse("RFQ creation failed.", "RFQ_CREATE_FAILED", ex);
                            }
                        }
                    }

                    // =========================================
                    // CASE 2 — VENDORS EXIST (PER-VENDOR RFQ)
                    // =========================================
                    foreach (var vendorId in rfq.VendorIds)
                    {
                        var vendorName = GetVendorName(vendorId);
                        var vendorEmail = GetVendorEmail(vendorId);
                        int newRFQId;

                        using (var tx = conn.BeginTransaction())
                        {
                            try
                            {
                                newRFQId = InsertRFQ2(conn, tx, rfq, vendorId, vendorName);
                                InsertRFQLines(conn, tx, newRFQId, rfq.Lines);

                                tx.Commit();
                            }
                            catch (Exception ex)
                            {
                                tx.Rollback();
                                notifiedVendors.Add(new
                                {
                                    Vendor = vendorName,
                                    Email = vendorEmail,
                                    Status = "DB_FAILED",
                                    Error = ex.Message
                                });
                                continue;
                            }
                        }

                        try
                        {
                            rfq.VendorName = vendorName;
                            var emailStatus = SendVendorNotification(vendorEmail, vendorName, rfq);

                            notifiedVendors.Add(new
                            {
                                Vendor = vendorName,
                                Email = vendorEmail,
                                Status = emailStatus
                            });
                        }
                        catch (Exception ex)
                        {
                            notifiedVendors.Add(new
                            {
                                Vendor = vendorName,
                                Email = vendorEmail,
                                Status = "EMAIL_FAILED",
                                Error = ex.Message
                            });
                        }
                    }

                    return OkResponse("RFQs processed.", notifiedVendors);
                }
            }
            catch (Exception ex)
            {
                return ErrorResponse("System failure during RFQ processing.", "RFQ_PROCESSING_ERROR", ex);
            }
        }


        [HttpPut]
        [Route("api/rfq/{id}/file")]
        public ApiResponse UpdateRFQFile(int id, [FromBody] RequestForQuotation rfq )
        {
            var response = new ApiResponse();
            string fileBase64 = rfq.FileBase64;
            if (string.IsNullOrWhiteSpace(fileBase64))
            {
                response.Success = false;
                response.Message = "FileBase64 cannot be empty.";
                response.ErrorCode = "VALIDATION_ERROR";
                return response;
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        using (var cmd = new SqlCommand(@"
                    UPDATE RequestForQuotation
                    SET FileBase64 = @FileBase64
                    WHERE Id = @Id", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", id);
                            cmd.Parameters.AddWithValue("@FileBase64", fileBase64);

                            int rows = cmd.ExecuteNonQuery();

                            if (rows == 0)
                            {
                                transaction.Rollback();
                                response.Success = false;
                                response.Message = $"RFQ with Id {id} not found.";
                                response.ErrorCode = "NOT_FOUND";
                                return response;
                            }

                            transaction.Commit();

                            response.Success = true;
                            response.Message = "File updated successfully.";
                            response.Data = new { RFQId = id };
                            return response;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while updating the file.";
                response.ErrorCode = "SERVER_ERROR";
                response.Data = ex.Message;
                return response;
            }
        }
        private int InsertRFQ2(SqlConnection conn, SqlTransaction transaction, RequestForQuotation rfq, int vendorId, string vendorName)
        {
            using (var cmd = new SqlCommand(@"
        INSERT INTO RequestForQuotation 
        (VendorId, VendorName, ItemDescription, Quantity, ExpectedDeliveryDate, Status, CreatedDate, RFQNumber, Priority, Department, RequestedBy, EstimatedBudget, DeliveryLocation, AdditionalNotes, FileBase64) 
        OUTPUT INSERTED.Id 
        VALUES 
        (@VendorId, @VendorName, @ItemDescription, @Quantity, @ExpectedDeliveryDate, 'Open', GETDATE(), @RFQNumber, @Priority, @Department, @RequestedBy, @EstimatedBudget, @DeliveryLocation, @AdditionalNotes, @FileBase64)",
                conn, transaction))
            {
                cmd.Parameters.AddWithValue("@VendorId", vendorId);
                cmd.Parameters.AddWithValue("@VendorName", vendorName ?? string.Empty);
                cmd.Parameters.AddWithValue("@ItemDescription", rfq.ItemDescription ?? string.Empty);
                cmd.Parameters.AddWithValue("@Quantity", rfq.Quantity);
                cmd.Parameters.AddWithValue("@ExpectedDeliveryDate", rfq.ExpectedDeliveryDate);
                cmd.Parameters.AddWithValue("@RFQNumber", rfq.RFQNumber ?? Guid.NewGuid().ToString());
                cmd.Parameters.AddWithValue("@Priority", rfq.Priority ?? "Normal");
                cmd.Parameters.AddWithValue("@Department", rfq.Department ?? "General");
                cmd.Parameters.AddWithValue("@RequestedBy", rfq.RequestedBy ?? "System");
                cmd.Parameters.AddWithValue("@EstimatedBudget", rfq.EstimatedBudget);
                cmd.Parameters.AddWithValue("@DeliveryLocation", rfq.DeliveryLocation ?? string.Empty);
                cmd.Parameters.AddWithValue("@AdditionalNotes", rfq.AdditionalNotes ?? string.Empty);
                cmd.Parameters.AddWithValue("@FileBase64", rfq.FileBase64 ?? string.Empty);

                int rfqId = (int)cmd.ExecuteScalar();

                const string updateSql = @"
            UPDATE dbo.PurchaseRequisitions 
            SET Status = @Status 
            WHERE RequisitionId = @RequisitionId;";

                using (var upd = new SqlCommand(updateSql, conn, transaction))
                {
                    upd.Parameters.AddWithValue("@Status", "ConvertedToRFQ");
                    upd.Parameters.AddWithValue("@RequisitionId", rfq.Id);

                    upd.ExecuteNonQuery();
                }

                return rfqId;
            }
        }


        private int InsertRFQ(SqlConnection conn, SqlTransaction transaction, RequestForQuotation rfq, int vendorId, string vendorName)
        {
            using (var cmd = new SqlCommand(@"
        INSERT INTO RequestForQuotation 
        (VendorId, VendorName, ItemDescription, Quantity, ExpectedDeliveryDate, Status, CreatedDate, RFQNumber, Priority, Department, RequestedBy, EstimatedBudget, DeliveryLocation, AdditionalNotes, FileBase64) 
        OUTPUT INSERTED.Id 
        VALUES 
        (@VendorId, @VendorName, @ItemDescription, @Quantity, @ExpectedDeliveryDate, 'Open', GETDATE(), @RFQNumber, @Priority, @Department, @RequestedBy, @EstimatedBudget, @DeliveryLocation, @AdditionalNotes, @FileBase64)",
                conn, transaction))
            {
                cmd.Parameters.AddWithValue("@VendorId", vendorId);
                cmd.Parameters.AddWithValue("@VendorName", vendorName ?? string.Empty);
                cmd.Parameters.AddWithValue("@ItemDescription", rfq.ItemDescription ?? string.Empty);
                cmd.Parameters.AddWithValue("@Quantity", rfq.Quantity);
                cmd.Parameters.AddWithValue("@ExpectedDeliveryDate", rfq.ExpectedDeliveryDate);
                cmd.Parameters.AddWithValue("@RFQNumber", rfq.RFQNumber ?? Guid.NewGuid().ToString());
                cmd.Parameters.AddWithValue("@Priority", rfq.Priority ?? "Normal");
                cmd.Parameters.AddWithValue("@Department", rfq.Department ?? "General");
                cmd.Parameters.AddWithValue("@RequestedBy", rfq.RequestedBy ?? "System");
                cmd.Parameters.AddWithValue("@EstimatedBudget", rfq.EstimatedBudget);
                cmd.Parameters.AddWithValue("@DeliveryLocation", rfq.DeliveryLocation ?? string.Empty);
                cmd.Parameters.AddWithValue("@AdditionalNotes", rfq.AdditionalNotes ?? string.Empty);
                cmd.Parameters.AddWithValue("@FileBase64", rfq.FileBase64 ?? string.Empty);

                return (int)cmd.ExecuteScalar();
            }
        }


        private void InsertRFQLines(SqlConnection conn, SqlTransaction transaction, int rfqId, List<RFQLine> lines)
        {
            foreach (var line in lines)
            {
                using (var cmd = new SqlCommand(@"
            INSERT INTO RFQLines 
                (RFQId, ProjectId, ProjectDescription, BudgetLineId, BudgetDescription, 
                 ItemCode, ItemDescription, Quantity, UnitOfMeasure, 
                 EstimatedUnitPrice, Notes, CreatedDate)
            VALUES 
                (@RFQId, @ProjectId, @ProjectDescription, @BudgetLineId, @BudgetDescription, 
                 @ItemCode, @ItemDescription, @Quantity, @UnitOfMeasure, 
                 @EstimatedUnitPrice, @Notes, GETDATE())",
                    conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@RFQId", rfqId);
                    cmd.Parameters.AddWithValue("@ProjectId", line.projectid);
                    cmd.Parameters.AddWithValue("@ProjectDescription", string.IsNullOrEmpty(line.projectDescription) ? (object)DBNull.Value : line.projectDescription);
                    cmd.Parameters.AddWithValue("@BudgetLineId", line.BudgetLineId.HasValue ? (object)line.BudgetLineId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@BudgetDescription", string.IsNullOrEmpty(line.BudgetDescription) ? (object)DBNull.Value : line.BudgetDescription);
                    cmd.Parameters.AddWithValue("@ItemCode", string.IsNullOrEmpty(line.ItemCode) ? (object)DBNull.Value : line.ItemCode);
                    cmd.Parameters.AddWithValue("@ItemDescription", string.IsNullOrEmpty(line.ItemDescription) ? (object)DBNull.Value : line.ItemDescription);
                    cmd.Parameters.AddWithValue("@Quantity", line.Quantity);
                    cmd.Parameters.AddWithValue("@UnitOfMeasure", string.IsNullOrEmpty(line.UnitOfMeasure) ? (object)"Units" : line.UnitOfMeasure);
                    cmd.Parameters.AddWithValue("@EstimatedUnitPrice", line.EstimatedUnitPrice);
                    cmd.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(line.Notes) ? (object)DBNull.Value : line.Notes);

                    cmd.ExecuteNonQuery();
                }
            }
        }



        #endregion

        #region RFQ RETRIEVAL

        [HttpGet]
        [Route("GetRFQs")]
        public IHttpActionResult GetRFQs()
        {
            var rfqs = new List<RequestForQuotation>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Fetch RFQs
                    using (var cmd = new SqlCommand("SELECT * FROM RequestForQuotation order by id desc", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rfqs.Add(ReadRFQ(reader));
                        }
                    }

                    // Fetch RFQ lines
                    foreach (var rfq in rfqs)
                        rfq.Lines = GetRFQLines(conn, rfq.Id);
                }

                return OkResponse("RFQs retrieved successfully.", rfqs);
            }
            catch (Exception ex)
            {
                return ErrorResponse("Failed to retrieve RFQs.", "RFQ_FETCH_ERROR", ex);
            }
        }


        [HttpGet]
        [Route("GetRFQById/{id}")]
        public IHttpActionResult GetRFQById(int id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    RequestForQuotation rfq = null;

                    // Fetch RFQ header
                    using (var cmd = new SqlCommand("SELECT * FROM RequestForQuotation WHERE Id = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                rfq = ReadRFQ(reader);
                            }
                        }

                        // Fetch RFQ lines (after closing the reader)
                        if (rfq != null)
                        {
                            rfq.Lines = GetRFQLines(conn, rfq.Id);
                        }
                    }


                    // If not found, return 404
                    if (rfq == null)
                        return OkResponse($"RFQ with ID {id} not found.");

                    // Fetch RFQ lines
                    rfq.Lines = GetRFQLines(conn, id);

                    return OkResponse("RFQ retrieved successfully.", rfq);
                }
            }
            catch (Exception ex)
            {
                return ErrorResponse("Failed to retrieve RFQ.", "RFQ_FETCH_ERROR", ex);
            }
        }



        [HttpPost]
        [Route("ApproveRFQ")]
        public IHttpActionResult ApproveRFQ([FromBody] RFQApprovalRequest approval)
        {
            if (approval == null || approval.VendorIds == null || approval.VendorIds.Count == 0)
                return BadRequestResponse("No vendors selected for approval.", "INVALID_APPROVAL_REQUEST");

            var approvalResults = new List<object>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Fetch RFQ details from DB
                    RequestForQuotation rfq = GetRFQById(conn, approval.RFQId);
                    if (rfq == null)
                        return BadRequestResponse("RFQ not found.", "RFQ_NOT_FOUND");

                    rfq.Lines = GetRFQLines(conn, rfq.Id);

                    // Approve for each vendor
                    foreach (var vendorId in approval.VendorIds)
                    {
                        string vendorEmail = GetVendorEmail(vendorId);
                        string vendorName = GetVendorName(vendorId);

                        if (string.IsNullOrWhiteSpace(vendorEmail))
                        {
                            approvalResults.Add(new { VendorId = vendorId, VendorName = vendorName, Status = "No Email Found" });
                            continue;
                        }

                        // Mark RFQ as Approved for that Vendor
                        using (var cmd = new SqlCommand("UPDATE RequestForQuotation SET Status='Approved' WHERE Id=@Id", conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", rfq.Id);
                            cmd.ExecuteNonQuery();
                        }

                        // Send approval email with PDF
                        string status = SendVendorApprovalEmail(vendorEmail, vendorName, rfq);
                        approvalResults.Add(new { Vendor = vendorName, Email = vendorEmail, Status = status });
                    }
                }

                return OkResponse("RFQ approved and notifications sent.", approvalResults);
            }
            catch (Exception ex)
            {
                return ErrorResponse("Error approving RFQ or sending emails.", "APPROVAL_ERROR", ex);
            }
        }

        #endregion

        #region SUPPLIER QUOTATIONS

        [HttpPost]
        [Route("SubmitQuotationWithLines")]
        public IHttpActionResult SubmitQuotationWithLines([FromBody] SupplierQuotation quotation)
        {
            if (quotation == null || quotation.Lines == null || quotation.Lines.Count == 0)
                return BadRequestResponse("Invalid quotation data — missing line items.", "INVALID_INPUT");

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            int quotationId = InsertQuotation(conn, transaction, quotation);
                            InsertQuotationLines(conn, transaction, quotationId, quotation.Lines);

                            transaction.Commit();
                            quotation.Id = quotationId;

                            return OkResponse("Quotation and lines submitted successfully.", quotation);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return ErrorResponse("Failed to submit quotation with lines.", "QUOTATION_SUBMIT_ERROR", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ErrorResponse("Unexpected error while submitting quotation.", "QUOTATION_GENERAL_ERROR", ex);
            }
        }

        private int InsertQuotation(SqlConnection conn, SqlTransaction transaction, SupplierQuotation quotation)
        {
            using (var cmd = new SqlCommand(@"
        INSERT INTO SupplierQuotation 
        (RFQId, VendorId, VendorName, QuotationNumber, Currency, Discount, TaxAmount, ShippingCost, 
         PaymentTerms, WarrantyInfo, ContactPerson, Notes, CreatedDate, Status)
        OUTPUT INSERTED.Id
        VALUES 
        (@RFQId, @VendorId, @VendorName, @QuotationNumber, @Currency, @Discount, @TaxAmount, @ShippingCost,
         @PaymentTerms, @WarrantyInfo, @ContactPerson, @Notes, GETDATE(), 'Submitted')
    ", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@RFQId", quotation.RFQId);
                cmd.Parameters.AddWithValue("@VendorId", quotation.VendorId);
                cmd.Parameters.AddWithValue("@VendorName", quotation.VendorName ?? "");
                cmd.Parameters.AddWithValue("@QuotationNumber", quotation.QuotationNumber ?? Guid.NewGuid().ToString());
                cmd.Parameters.AddWithValue("@Currency", quotation.Currency ?? "KES");
                cmd.Parameters.AddWithValue("@Discount", quotation.Discount);
                cmd.Parameters.AddWithValue("@TaxAmount", quotation.TaxAmount);
                cmd.Parameters.AddWithValue("@ShippingCost", quotation.ShippingCost);
                cmd.Parameters.AddWithValue("@PaymentTerms", quotation.PaymentTerms ?? "");
                cmd.Parameters.AddWithValue("@WarrantyInfo", quotation.WarrantyInfo ?? "");
                cmd.Parameters.AddWithValue("@ContactPerson", quotation.ContactPerson ?? "");
                cmd.Parameters.AddWithValue("@Notes", quotation.Notes ?? "");

                return (int)cmd.ExecuteScalar();
            }
        }

        private void InsertQuotationLines(SqlConnection conn, SqlTransaction transaction, int quotationId, List<SupplierQuotationLine> lines)
        {
            foreach (var line in lines)
            {
                line.DeliveryDate = DateTime.Now;
                using (var cmd = new SqlCommand(@"
            INSERT INTO SupplierQuotationLine 
            (QuotationId, ItemCode, ItemDescription, Quantity, UnitOfMeasure, UnitPrice, DeliveryDate, Notes, CreatedDate)
            VALUES 
            (@QuotationId, @ItemCode, @ItemDescription, @Quantity, @UnitOfMeasure, @UnitPrice, @DeliveryDate, @Notes, GETDATE())
        ", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@QuotationId", quotationId);
                    cmd.Parameters.AddWithValue("@ItemCode", (object)line.ItemCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ItemDescription", (object)line.ItemDescription ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Quantity", line.Quantity);
                    cmd.Parameters.AddWithValue("@UnitOfMeasure", (object)line.UnitOfMeasure ?? "Units");
                    cmd.Parameters.AddWithValue("@UnitPrice", line.UnitPrice);
                    cmd.Parameters.AddWithValue("@DeliveryDate", line.DeliveryDate);
                    cmd.Parameters.AddWithValue("@Notes", (object)line.Notes ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
        }


        [HttpGet]
        [Route("GetQuotationById/{id}")]
        public IHttpActionResult GetQuotationById(int id)
        {
            SupplierQuotation quotation = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Header
                using (var cmd = new SqlCommand("SELECT * FROM SupplierQuotation WHERE Id=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            quotation = new SupplierQuotation
                            {
                                Id = id,
                                RFQId = Convert.ToInt32(rdr["RFQId"]),
                                VendorId = Convert.ToInt32(rdr["VendorId"]),
                                VendorName = rdr["VendorName"].ToString(),
                                QuotationNumber = rdr["QuotationNumber"].ToString(),
                                Currency = rdr["Currency"].ToString(),
                                Discount = Convert.ToDecimal(rdr["Discount"]),
                                TaxAmount = Convert.ToDecimal(rdr["TaxAmount"]),
                                ShippingCost = Convert.ToDecimal(rdr["ShippingCost"]),
                                PaymentTerms = rdr["PaymentTerms"].ToString(),
                                WarrantyInfo = rdr["WarrantyInfo"].ToString(),
                                ContactPerson = rdr["ContactPerson"].ToString(),
                                Notes = rdr["Notes"].ToString(),
                                CreatedDate = Convert.ToDateTime(rdr["CreatedDate"]),
                                Status = rdr["Status"].ToString(),
                                Lines = new List<SupplierQuotationLine>()
                            };
                        }
                    }
                }

                if (quotation == null)
                    return NotFound();

                // Lines
                using (var cmdLines = new SqlCommand("SELECT * FROM SupplierQuotationLine WHERE QuotationId=@Id", conn))
                {
                    cmdLines.Parameters.AddWithValue("@Id", id);
                    using (var rdr = cmdLines.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            quotation.Lines.Add(new SupplierQuotationLine
                            {
                                Id = Convert.ToInt32(rdr["Id"]),
                                ItemCode = rdr["ItemCode"].ToString(),
                                ItemDescription = rdr["ItemDescription"].ToString(),
                                Quantity = Convert.ToDecimal(rdr["Quantity"]),
                                UnitOfMeasure = rdr["UnitOfMeasure"].ToString(),
                                UnitPrice = Convert.ToDecimal(rdr["UnitPrice"]),
                                DeliveryDate = Convert.ToDateTime(rdr["DeliveryDate"]),
                                Notes = rdr["Notes"].ToString()
                            });
                        }
                    }
                }
            }

            return Ok(quotation);
        }


        [HttpGet]
        [Route("GetAllQuotations")]
        public IHttpActionResult GetAllQuotations()
        {
            var quotations = new List<SupplierQuotation>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Fetch all quotations
                using (var cmd = new SqlCommand("SELECT * FROM SupplierQuotation", conn))
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var quotation = new SupplierQuotation
                            {
                                Id = Convert.ToInt32(rdr["Id"]),
                                RFQId = Convert.ToInt32(rdr["RFQId"]),
                                VendorId = Convert.ToInt32(rdr["VendorId"]),
                                VendorName = rdr["VendorName"].ToString(),
                                QuotationNumber = rdr["QuotationNumber"].ToString(),
                                Currency = rdr["Currency"].ToString(),
                                Discount = Convert.ToDecimal(rdr["Discount"]),
                                TaxAmount = Convert.ToDecimal(rdr["TaxAmount"]),
                                ShippingCost = Convert.ToDecimal(rdr["ShippingCost"]),
                                PaymentTerms = rdr["PaymentTerms"].ToString(),
                                WarrantyInfo = rdr["WarrantyInfo"].ToString(),
                                ContactPerson = rdr["ContactPerson"].ToString(),
                                Notes = rdr["Notes"].ToString(),
                                CreatedDate = Convert.ToDateTime(rdr["CreatedDate"]),
                                Status = rdr["Status"].ToString(),
                                Lines = new List<SupplierQuotationLine>()
                            };

                            quotations.Add(quotation);
                        }
                    }
                }

                // Fetch all lines for the quotations in one go
                using (var cmdLines = new SqlCommand("SELECT * FROM SupplierQuotationLine", conn))
                {
                    using (var rdr = cmdLines.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            int quotationId = Convert.ToInt32(rdr["QuotationId"]);

                            var line = new SupplierQuotationLine
                            {
                                Id = Convert.ToInt32(rdr["Id"]),
                                ItemCode = rdr["ItemCode"].ToString(),
                                ItemDescription = rdr["ItemDescription"].ToString(),
                                Quantity = Convert.ToDecimal(rdr["Quantity"]),
                                UnitOfMeasure = rdr["UnitOfMeasure"].ToString(),
                                UnitPrice = Convert.ToDecimal(rdr["UnitPrice"]),
                                DeliveryDate = Convert.ToDateTime(rdr["DeliveryDate"]),
                                Notes = rdr["Notes"].ToString()
                            };

                            // Attach line to its parent quotation
                            var parentQuotation = quotations.FirstOrDefault(q => q.Id == quotationId);
                            if (parentQuotation != null)
                                parentQuotation.Lines.Add(line);
                        }
                    }
                }
            }

            return Ok(quotations);
        }
        [HttpPost]
        [Route("CreatePurchaseOrder")]
        public async Task<IHttpActionResult> CreatePurchaseOrder(PurchaseOrderDto model)
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
                        using (var cmd = new SqlCommand(@"
    UPDATE RequestForQuotation 
    SET Status = @Status
    WHERE Id = @RFQId", conn))
                        {
                            cmd.Parameters.AddWithValue("@Status", "ConvertedToPO");
                            cmd.Parameters.AddWithValue("@RFQId", model.PurchaseOrderId); // ✅ Use RFQ ID, not PO ID
                            cmd.ExecuteNonQuery();
                        }


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

        #endregion

        #region HELPERS

        private string SendVendorNotification(string vendorEmail, string vendorName, RequestForQuotation rfq)
        {
            if (string.IsNullOrWhiteSpace(vendorEmail)) return "No Email Found";

            string subject = $"RFQ Invitation: {rfq.RFQNumber}";

            // 🖼️ Load and convert logo to Base64 for embedding in email
            string logoPath = @"C:\Users\Dorothy Mogoi\OneDrive\Pictures\ADRA - Vertical - Logo.png";
            string logoBase64 = "";
            if (File.Exists(logoPath))
            {
                byte[] logoBytes = File.ReadAllBytes(logoPath);
                logoBase64 = $"data:image/png;base64,{Convert.ToBase64String(logoBytes)}";
            }

            //  Elegant HTML body inspired by your uploaded invoice design
            string body = $@"
<html>
<head>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f3f6f5;
            margin: 0;
            padding: 0;
            color: #333;
        }}
        .container {{
            max-width: 750px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 10px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.08);
            overflow: hidden;
            border-top: 5px solid #007e33;
        }}
        .header {{
            background-color: #002147;
            color: #fff;
            padding: 20px;
            display: flex;
            align-items: center;
            justify-content: space-between;
        }}
        .header img {{
            height: 50px;
        }}
        .header h1 {{
            margin: 0;
            font-size: 22px;
            letter-spacing: 0.5px;
            text-align: right;
        }}
        .invoice-meta {{
            background-color: #f4f4f4;
            padding: 15px 25px;
            border-bottom: 2px solid #007e33;
        }}
        .invoice-meta p {{
            margin: 4px 0;
            font-size: 14px;
        }}
        .content {{
            padding: 30px;
            line-height: 1.8;
        }}
        .highlight {{
            color: #007e33;
            font-weight: 600;
        }}
        .rfq-details {{
            background-color: #f8fff8;
            border: 1px solid #c6ecc6;
            border-radius: 8px;
            padding: 15px 20px;
            margin: 20px 0;
        }}
        .rfq-details p {{
            margin: 6px 0;
        }}
        .button {{
            display: inline-block;
            background: #007e33;
            color: #fff;
            padding: 12px 25px;
            border-radius: 6px;
            text-decoration: none;
            font-weight: 600;
            transition: background 0.3s ease;
        }}
        .button:hover {{
            background: #00b35a;
        }}
        .footer {{
            background-color: #f4f4f4;
            text-align: center;
            padding: 15px;
            font-size: 12px;
            color: #666;
        }}
        .footer a {{
            color: #007e33;
            text-decoration: none;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <img src='{logoBase64}' alt='Company Logo'/>
            <h1>REQUEST FOR QUOTATION (RFQ) INVITATION</h1>
        </div>

        <div class='invoice-meta'>
            <p><strong>RFQ Number:</strong> {rfq.RFQNumber}</p>
            <p><strong>Date:</strong> {DateTime.Now:yyyy-MM-dd}</p>
            <p><strong>Recipient:</strong> {vendorName}</p>
        </div>

        <div class='content'>
            <p>Dear <strong>{vendorName}</strong>,</p>

            <p>
                You are invited to submit a quotation for the following Request for Quotation (RFQ):
            </p>

            <div class='rfq-details'>
                <p><strong>RFQ Number:</strong> {rfq.RFQNumber}</p>
                <p><strong>Department:</strong> {rfq.Department}</p>
                <p><strong>Priority:</strong> {rfq.Priority}</p>
                <p><strong>Expected Delivery Date:</strong> {rfq.ExpectedDeliveryDate:yyyy-MM-dd}</p>
            </div>

            <p>
                Please review the attached RFQ document for itemized requirements and submit your quotation 
                by the stipulated deadline.
            </p>

<a href='https://07dabf2873b3.ngrok-free.app/api/rfq/GetRFQById/{rfq.Id}' class='button'>View RFQ Details</a>

            <p style='margin-top: 25px;'>
                We appreciate your continued partnership and look forward to receiving your competitive quotation.
            </p>

            <p>Warm regards,<br/>
            <strong>Swift Financials Procurement Team</strong></p>
        </div>

        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} Swift Financials Ltd. All rights reserved.</p>
            <p><a href='https://www.swiftfinancials.co.ke'>www.swiftfinancials.co.ke</a></p>
        </div>
    </div>
</body>
</html>";

            try
            {
                using (var ms = new MemoryStream())
                {
                    Document doc = new Document(PageSize.A4, 50, 50, 25, 25);
                    PdfWriter.GetInstance(doc, ms);
                    doc.Open();

                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, new BaseColor(0, 126, 51));
                    var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, BaseColor.BLACK);

                    // Add logo to PDF
                    if (File.Exists(logoPath))
                    {
                        iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(logoPath);
                        logo.ScaleToFit(120f, 60f);
                        logo.Alignment = Element.ALIGN_RIGHT;
                        doc.Add(logo);
                    }

                    doc.Add(new Paragraph($"REQUEST FOR QUOTATION (RFQ): {rfq.RFQNumber}", titleFont));
                    doc.Add(new Paragraph($"Date: {DateTime.Now:yyyy-MM-dd}\n\n", normalFont));
                    doc.Add(new Paragraph($"To: {vendorName}", normalFont));
                    doc.Add(new Paragraph($"Email: {vendorEmail}\n\n", normalFont));
                    doc.Add(new Paragraph($"Department: {rfq.Department}", normalFont));
                    doc.Add(new Paragraph($"Priority: {rfq.Priority}", normalFont));
                    doc.Add(new Paragraph($"Expected Delivery Date: {rfq.ExpectedDeliveryDate:yyyy-MM-dd}\n\n", normalFont));

                    // Create a neat table for RFQ lines
                    PdfPTable table = new PdfPTable(3);
                    table.WidthPercentage = 100;
                    table.SpacingBefore = 10f;
                    table.SpacingAfter = 10f;

                    var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.WHITE);
                    var greenHeader = new BaseColor(0, 126, 51);

                    table.AddCell(new PdfPCell(new Phrase("Item Description", headerFont)) { BackgroundColor = greenHeader, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 8 });
                    table.AddCell(new PdfPCell(new Phrase("Quantity", headerFont)) { BackgroundColor = greenHeader, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 8 });
                    table.AddCell(new PdfPCell(new Phrase("Unit of Measure", headerFont)) { BackgroundColor = greenHeader, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 8 });

                    foreach (var line in rfq.Lines)
                    {
                        table.AddCell(new PdfPCell(new Phrase(line.ItemDescription, normalFont)) { Padding = 6 });
                        table.AddCell(new PdfPCell(new Phrase(line.Quantity.ToString(), normalFont)) { Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER });
                        table.AddCell(new PdfPCell(new Phrase(line.UnitOfMeasure, normalFont)) { Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER });
                    }

                    doc.Add(table);
                    doc.Close();


                    var pdfBytes = GenerateRFQInvoice(rfq, logoPath);
                    EmailHelper.SendEmailWithAttachment(vendorEmail, subject, body, pdfBytes, $"RFQ_{rfq.RFQNumber}.pdf");

                    return "Email with PDF Sent";
                }
            }
            catch (Exception ex)
            {
                return $"Email Failed: {ex.Message}";
            }
        }



        public byte[] GenerateRFQInvoice(RequestForQuotation rfq, string logoPath)
        {
            using (var ms = new MemoryStream())
            {
                // === Initialize Document ===
                var document = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter.GetInstance(document, ms);
                document.Open();

                // === BRAND COLORS (ADRA Kenya Palette) ===
                var adraGreen = new BaseColor(0, 102, 76);       // Primary ADRA green
                var lightGray = new BaseColor(245, 245, 245);
                var softBorder = new BaseColor(200, 200, 200);
                var darkText = BaseColor.BLACK;

                // === FONT STYLES ===
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.WHITE);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.WHITE);
                var sectionHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, adraGreen);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, darkText);
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, darkText);
                var smallItalic = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9, BaseColor.GRAY);

                // ------------------------------------------------------------
                // 🧾 PAGE 1 — RFQ DETAILS
                // ------------------------------------------------------------

                // === Header (Logo + Title) ===
                PdfPTable header = new PdfPTable(2)
                {
                    WidthPercentage = 100
                };
                header.SetWidths(new float[] { 50, 50 });

                // Logo
                PdfPCell logoCell = new PdfPCell { Border = Rectangle.NO_BORDER };
                string logoUrl = "https://adrakenya.org/assets/2019/11/adra-vertical-logo.png";
                var logo = iTextSharp.text.Image.GetInstance(new Uri(logoUrl));
                logo.ScaleToFit(120f, 60f);
                logo.Alignment = Element.ALIGN_LEFT;
                logoCell.AddElement(logo);

                // Title
                PdfPCell titleCell = new PdfPCell(new Phrase("REQUEST FOR QUOTATION", titleFont))
                {
                    BackgroundColor = adraGreen,
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 15
                };

                header.AddCell(logoCell);
                header.AddCell(titleCell);
                document.Add(header);

                // Divider line
                PdfPTable dividerTable = new PdfPTable(1) { WidthPercentage = 100 };
                dividerTable.AddCell(new PdfPCell
                {
                    BackgroundColor = adraGreen,
                    Border = Rectangle.NO_BORDER,
                    FixedHeight = 6f
                });
                document.Add(dividerTable);

                // === RFQ Parties (To/From) ===
                PdfPTable infoTable = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 15f
                };
                infoTable.SetWidths(new float[] { 50, 50 });

                PdfPCell leftInfo = new PdfPCell { Border = Rectangle.NO_BORDER };
                leftInfo.AddElement(new Paragraph("RFQ To:", sectionHeaderFont));
                leftInfo.AddElement(new Paragraph(rfq.VendorName, normalFont));
                leftInfo.AddElement(new Paragraph(rfq.AdditionalNotes ?? "", normalFont));

                PdfPCell rightInfo = new PdfPCell { Border = Rectangle.NO_BORDER };
                rightInfo.AddElement(new Paragraph("RFQ From:", sectionHeaderFont));
                rightInfo.AddElement(new Paragraph("ADRA Kenya", normalFont));
                rightInfo.AddElement(new Paragraph("Procurement Department", normalFont));
                rightInfo.AddElement(new Paragraph("www.adrakenya.org", normalFont));

                infoTable.AddCell(leftInfo);
                infoTable.AddCell(rightInfo);
                document.Add(infoTable);

                // === RFQ Items Table ===
                PdfPTable itemsTable = new PdfPTable(4)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 20f
                };
                itemsTable.SetWidths(new float[] { 50, 15, 15, 20 }); // Description | Quantity | Unit | Amount

                // Table Headers
                string[] headers = { "Item Description", "Quantity", "Unit", "Amount (KES)" };
                foreach (var h in headers)
                {
                    itemsTable.AddCell(new PdfPCell(new Phrase(h, headerFont))
                    {
                        BackgroundColor = adraGreen,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 6
                    });
                }

                // Table Rows
                foreach (var line in rfq.Lines)
                {
                    itemsTable.AddCell(new PdfPCell(new Phrase(line.ItemDescription, normalFont)) { Padding = 5 });
                    itemsTable.AddCell(new PdfPCell(new Phrase(line.Quantity.ToString(), normalFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });
                    itemsTable.AddCell(new PdfPCell(new Phrase(line.UnitOfMeasure, normalFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });

                    // Blank Amount column (for vendors to fill)
                    itemsTable.AddCell(new PdfPCell(new Phrase("", normalFont))
                    {
                        Padding = 5,
                        HorizontalAlignment = Element.ALIGN_CENTER
                    });
                }

                document.Add(itemsTable);

                // === Budget Summary ===
                PdfPTable totalTable = new PdfPTable(2)
                {
                    WidthPercentage = 40,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    SpacingBefore = 20f
                };

                totalTable.AddCell(new PdfPCell(new Phrase("Estimated Total:", sectionHeaderFont))
                {
                    BackgroundColor = adraGreen,
                    Border = Rectangle.NO_BORDER,
                    Padding = 8
                });

                totalTable.AddCell(new PdfPCell(new Phrase(rfq.EstimatedBudget.ToString("N2"), sectionHeaderFont))
                {
                    BackgroundColor = adraGreen,
                    Border = Rectangle.NO_BORDER,
                    Padding = 8,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });

                document.Add(totalTable);

                // Footer note
                var footer = new Paragraph("\nThank you for your collaboration.", sectionHeaderFont)
                {
                    Alignment = Element.ALIGN_CENTER
                };
                document.Add(footer);

                // ------------------------------------------------------------
                // 📜 PAGE 2 — TERMS & CONDITIONS
                // ------------------------------------------------------------
                document.NewPage();

                // Header
                PdfPTable tcHeader = new PdfPTable(1) { WidthPercentage = 100 };
                tcHeader.AddCell(new PdfPCell(new Phrase("ADRA PROCUREMENT - TERMS & CONDITIONS", titleFont))
                {
                    BackgroundColor = adraGreen,
                    Padding = 12,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Border = Rectangle.NO_BORDER
                });
                document.Add(tcHeader);

                // Divider
                PdfPTable tcDivider = new PdfPTable(1) { WidthPercentage = 100 };
                tcDivider.AddCell(new PdfPCell
                {
                    BackgroundColor = adraGreen,
                    Border = Rectangle.NO_BORDER,
                    FixedHeight = 4f
                });
                document.Add(tcDivider);

                // Terms Container
                PdfPTable termsContainer = new PdfPTable(1) { WidthPercentage = 100 };
                PdfPCell tcContent = new PdfPCell
                {
                    BackgroundColor = lightGray,
                    BorderColor = softBorder,
                    Padding = 15
                };

                tcContent.AddElement(new Paragraph("Below are the terms under which ADRA procurement shall operate. Submission of a quotation implies acceptance of these terms.", normalFont));
                tcContent.AddElement(new Paragraph("\nTERMS & CONDITIONS", sectionHeaderFont));

                var terms = new List<string>
        {
            "1. All quotations must be submitted by the stipulated deadline — late submissions will not be considered.",
            "2. Prices must be quoted in Kenya Shillings (KES), inclusive of all applicable taxes and duties.",
            "3. ADRA reserves the right to accept or reject any quotation in whole or in part without giving reasons.",
            "4. Vendors must strictly adhere to the technical specifications detailed in the RFQ.",
            "5. Delivery must be made within the agreed schedule. Failure may lead to cancellation or penalty.",
            "6. Payment will be made within 30 days after acceptance of goods/services, upon submission of a valid invoice and supporting documents.",
            "7. Any form of corruption, collusion, or unethical conduct will lead to disqualification.",
            "8. Vendors must comply with ADRA’s ethical procurement policies and relevant laws.",
            "9. All communication must quote the RFQ number.",
            "10. Disputes shall be resolved under the laws of Kenya, in competent Kenyan courts."
        };

                foreach (var term in terms)
                    tcContent.AddElement(new Paragraph(term, smallFont) { SpacingAfter = 6f });

                tcContent.AddElement(new Paragraph("\nAuthorized by:", sectionHeaderFont));
                tcContent.AddElement(new Paragraph("ADRA Procurement Department", normalFont));
                tcContent.AddElement(new Paragraph("Nairobi, Kenya", smallItalic));

                termsContainer.AddCell(tcContent);
                document.Add(termsContainer);

                // Disclaimer footer
                var disclaimer = new Paragraph("\nThis document is confidential and intended solely for the recipient. Unauthorized sharing or duplication is prohibited.\nThis is an electronic generated document and does not require signatures", smallItalic)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 10f
                };
                document.Add(disclaimer);

                // === Finalize ===
                document.Close();
                return ms.ToArray();
            }
        }

        private byte[] GenerateRFQInvoic(RequestForQuotation rfq, string logoPath)
        {
            using (var ms = new MemoryStream())
            {



                Document doc = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter writer = PdfWriter.GetInstance(doc, ms);
                doc.Open();

                // ----- COLORS (ADRA style) -----
                // Based on ADRA Kenya’s green / teal logo hue
                var adraGreen = new BaseColor(0, 102, 76);        // adjust to match exact shade
                var lightGray = new BaseColor(245, 245, 245);
                var softBorder = new BaseColor(200, 200, 200);
                var darkText = BaseColor.BLACK;

                // ----- FONTS -----
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.WHITE);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.WHITE);
                var sectionHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, adraGreen);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, darkText);
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, darkText);
                var smallItalic = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9, BaseColor.GRAY);

                // ===== FIRST PAGE =====

                // Header with logo and title bar
                PdfPTable header = new PdfPTable(2);
                header.WidthPercentage = 100;
                header.SetWidths(new float[] { 50, 50 });

                PdfPCell logoCell = new PdfPCell();
                //if (File.Exists(logoPath))
                //{
                //    //string logoUrl = "https://adra.org/wp-content/uploads/2021/06/ADRA_Logo_Green_RGB.png";
                //    //iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(new Uri(logoUrl));
                //    //logo.ScaleToFit(120f, 60f);
                //    //logo.Alignment = Element.ALIGN_LEFT;
                //    //logoCell.AddElement(logo);




                //    //var logo = iTextSharp.text.Image.GetInstance(logoPath);
                //    //logo.ScaleToFit(120f, 60f);
                //    //logo.Alignment = Element.ALIGN_LEFT;
                //    //logoCell.AddElement(logo);
                //}

                string logoUrl = "https://adrakenya.org/assets/2019/11/adra-vertical-logo.png";
                iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(new Uri(logoUrl));
                logo.ScaleToFit(120f, 60f);
                logo.Alignment = Element.ALIGN_LEFT;
                logoCell.AddElement(logo);

                logoCell.Border = Rectangle.NO_BORDER;




                PdfPCell titleCell = new PdfPCell(new Phrase("REQUEST FOR QUOTATION", titleFont))
                {
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Border = Rectangle.NO_BORDER,
                    BackgroundColor = adraGreen,
                    Padding = 15
                };

                header.AddCell(logoCell);
                header.AddCell(titleCell);
                doc.Add(header);

                // Thin colored divider
                PdfPCell divider = new PdfPCell(new Phrase(""))
                {
                    Border = Rectangle.NO_BORDER,
                    BackgroundColor = adraGreen,
                    FixedHeight = 6f
                };
                PdfPTable dividerTable = new PdfPTable(1) { WidthPercentage = 100 };
                dividerTable.AddCell(divider);
                doc.Add(dividerTable);

                // RFQ Info block
                PdfPTable infoTable = new PdfPTable(2)
                {
                    SpacingBefore = 15f,
                    WidthPercentage = 100
                };
                infoTable.SetWidths(new float[] { 50, 50 });

                PdfPCell leftInfo = new PdfPCell();
                leftInfo.AddElement(new Paragraph("RFQ To:", sectionHeaderFont));
                leftInfo.AddElement(new Paragraph(rfq.VendorName, normalFont));
                leftInfo.AddElement(new Paragraph(rfq.AdditionalNotes, normalFont));
                leftInfo.Border = Rectangle.NO_BORDER;

                PdfPCell rightInfo = new PdfPCell();
                rightInfo.AddElement(new Paragraph("RFQ From:", sectionHeaderFont));
                rightInfo.AddElement(new Paragraph("ADRA Kenya", normalFont));
                rightInfo.AddElement(new Paragraph("Procurement Department", normalFont));
                rightInfo.AddElement(new Paragraph("www.adrakenya.org", normalFont));
                rightInfo.Border = Rectangle.NO_BORDER;

                infoTable.AddCell(leftInfo);
                infoTable.AddCell(rightInfo);
                doc.Add(infoTable);

                // RFQ items table
                PdfPTable table = new PdfPTable(3)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 20f
                };
                table.SetWidths(new float[] { 60, 20, 20 });

                table.AddCell(new PdfPCell(new Phrase("Item Description", headerFont)) { BackgroundColor = adraGreen, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });
                table.AddCell(new PdfPCell(new Phrase("Quantity", headerFont)) { BackgroundColor = adraGreen, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });
                table.AddCell(new PdfPCell(new Phrase("Unit", headerFont)) { BackgroundColor = adraGreen, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });

                foreach (var line in rfq.Lines)
                {
                    table.AddCell(new PdfPCell(new Phrase(line.ItemDescription, normalFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(line.Quantity.ToString(), normalFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });
                    table.AddCell(new PdfPCell(new Phrase(line.UnitOfMeasure, normalFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });
                }

                doc.Add(table);

                // Total summary
                PdfPTable totalTable = new PdfPTable(2)
                {
                    WidthPercentage = 40,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    SpacingBefore = 20f
                };

                string formattedBudget = rfq.EstimatedBudget.ToString("N2");

                totalTable.AddCell(new PdfPCell(new Phrase("Total:", sectionHeaderFont))
                {
                    BackgroundColor = adraGreen,
                    Border = Rectangle.NO_BORDER,
                    Padding = 8
                });

                totalTable.AddCell(new PdfPCell(new Phrase(formattedBudget, sectionHeaderFont))
                {
                    BackgroundColor = adraGreen,
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 8
                });

                doc.Add(totalTable);

                // Footer line
                Paragraph footerFirstPage = new Paragraph("\nThank you for your collaboration.", sectionHeaderFont)
                {
                    Alignment = Element.ALIGN_CENTER
                };
                doc.Add(footerFirstPage);

                // ===== SECOND PAGE: TERMS & CONDITIONS =====
                doc.NewPage();

                // Header bar for page 2
                PdfPTable tcHeader = new PdfPTable(1) { WidthPercentage = 100 };
                PdfPCell tcHeaderCell = new PdfPCell(new Phrase("ADRA PROCUREMENT - TERMS & CONDITIONS", titleFont))
                {
                    BackgroundColor = adraGreen,
                    Padding = 12,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Border = Rectangle.NO_BORDER
                };
                tcHeader.AddCell(tcHeaderCell);
                doc.Add(tcHeader);

                // Divider line
                PdfPCell tcLine = new PdfPCell(new Phrase(""))
                {
                    BackgroundColor = adraGreen,
                    Border = Rectangle.NO_BORDER,
                    FixedHeight = 4f
                };
                PdfPTable tcDiv = new PdfPTable(1) { WidthPercentage = 100 };
                tcDiv.AddCell(tcLine);
                doc.Add(tcDiv);

                // Container for terms block
                PdfPTable tcContainer = new PdfPTable(1) { WidthPercentage = 100 };
                PdfPCell tcContentCell = new PdfPCell()
                {
                    BackgroundColor = lightGray,
                    BorderColor = softBorder,
                    Padding = 15
                };

                tcContentCell.AddElement(new Paragraph("Below are the terms under which ADRA procurement shall operate. Submission of a quotation implies acceptance of these terms.", normalFont));
                tcContentCell.AddElement(new Paragraph("\nTERMS & CONDITIONS", sectionHeaderFont));

                List<string> terms = new List<string>
        {
            "1. All quotations must be submitted by the stipulated deadline — late submissions will not be considered.",
            "2. Prices must be quoted in Kenya Shillings (KES), inclusive of all applicable taxes and duties.",
            "3. ADRA reserves the absolute right to accept or reject any quotation in whole or in part without giving reasons.",
            "4. Vendors must strictly adhere to technical specifications detailed in the RFQ.",
            "5. Delivery must be made within the agreed schedule. Failure may lead to cancellation or penalty.",
            "6. Payment will be made within 30 days after acceptance of goods/services, upon submission of a valid invoice and supporting documents.",
            "7. Any form of corruption, collusion or unethical conduct will lead to disqualification.",
            "8. Vendors must comply with ADRA’s ethical procurement policies and relevant laws.",
            "9. All communication must quote the RFQ number.",
            "10. Disputes shall be resolved under the laws of Kenya, in competent Kenyan courts."
        };

                foreach (var term in terms)
                {
                    tcContentCell.AddElement(new Paragraph(term, smallFont) { SpacingAfter = 6f });
                }

                tcContentCell.AddElement(new Paragraph("\nAuthorized by:", sectionHeaderFont));
                tcContentCell.AddElement(new Paragraph("ADRA Procurement Department", normalFont));
                tcContentCell.AddElement(new Paragraph("Nairobi, Kenya", smallItalic));

                tcContainer.AddCell(tcContentCell);
                doc.Add(tcContainer);

                // Footer disclaimer
                Paragraph disclaimer = new Paragraph("\nThis document is confidential and for the intended recipient only. Unauthorized sharing or use is prohibited.", smallItalic)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 10f
                };
                doc.Add(disclaimer);

                doc.Close();
                return ms.ToArray();
            }
        }

        private byte[] GenerateRFQPDF(RequestForQuotation rfq)
        {
            using (var ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter writer = PdfWriter.GetInstance(doc, ms);
                doc.Open();

                // 🎨 Theme colors
                var darkGreen = new BaseColor(0, 66, 37);
                var lightGreen = new BaseColor(167, 201, 87);
                var accentGray = new BaseColor(242, 242, 242);
                var textGray = new BaseColor(51, 51, 51);

                // 🖋️ Fonts
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, darkGreen);
                var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, darkGreen);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.WHITE);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, textGray);

                // 🌿 Logo (directly from ADRA website)
                string logoUrl = "https://adra.org/wp-content/uploads/2021/06/ADRA_Logo_Green_RGB.png";
                iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(new Uri(logoUrl));
                logo.ScaleToFit(120f, 60f);
                logo.Alignment = Element.ALIGN_LEFT;

                // --- HEADER ---
                PdfPTable header = new PdfPTable(2);
                header.WidthPercentage = 100;
                header.SetWidths(new float[] { 30, 70 });

                PdfPCell logoCell = new PdfPCell(logo) { Border = Rectangle.NO_BORDER, PaddingBottom = 5 };
                PdfPCell titleCell = new PdfPCell(new Phrase("REQUEST FOR QUOTATION (RFQ)", titleFont))
                {
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Border = Rectangle.NO_BORDER
                };

                header.AddCell(logoCell);
                header.AddCell(titleCell);
                doc.Add(header);

                // Divider
                PdfPCell divider = new PdfPCell(new Phrase("")) { Border = Rectangle.NO_BORDER, BackgroundColor = lightGreen, FixedHeight = 5 };
                PdfPTable dividerTable = new PdfPTable(1);
                dividerTable.WidthPercentage = 100;
                dividerTable.AddCell(divider);
                doc.Add(dividerTable);

                // --- RFQ INFO ---
                PdfPTable infoTable = new PdfPTable(2);
                infoTable.WidthPercentage = 100;
                infoTable.SpacingBefore = 15;
                infoTable.SetWidths(new float[] { 50, 50 });

                PdfPCell leftInfo = new PdfPCell();
                leftInfo.AddElement(new Paragraph("RFQ To:", sectionFont));
                leftInfo.AddElement(new Paragraph(rfq.VendorName, normalFont));
                leftInfo.AddElement(new Paragraph(rfq.RequestedBy, normalFont));
                leftInfo.AddElement(new Paragraph(rfq.AdditionalNotes ?? "", normalFont));
                leftInfo.Border = Rectangle.NO_BORDER;

                PdfPCell rightInfo = new PdfPCell();
                rightInfo.AddElement(new Paragraph("RFQ From:", sectionFont));
                rightInfo.AddElement(new Paragraph("ADRA Kenya", normalFont));
                rightInfo.AddElement(new Paragraph("Procurement Department", normalFont));
                rightInfo.AddElement(new Paragraph("www.adrakenya.org", normalFont));
                rightInfo.Border = Rectangle.NO_BORDER;

                infoTable.AddCell(leftInfo);
                infoTable.AddCell(rightInfo);
                doc.Add(infoTable);

                // --- ITEMS TABLE ---
                PdfPTable table = new PdfPTable(3);
                table.WidthPercentage = 100;
                table.SpacingBefore = 20;
                table.SetWidths(new float[] { 60, 20, 20 });

                table.AddCell(new PdfPCell(new Phrase("Item Description", headerFont)) { BackgroundColor = darkGreen, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Quantity", headerFont)) { BackgroundColor = darkGreen, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Unit", headerFont)) { BackgroundColor = darkGreen, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER });

                foreach (var line in rfq.Lines)
                {
                    table.AddCell(new PdfPCell(new Phrase(line.ItemDescription, normalFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(line.Quantity.ToString(), normalFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });
                    table.AddCell(new PdfPCell(new Phrase(line.UnitOfMeasure, normalFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });
                }

                doc.Add(table);

                // --- TOTAL ---
                PdfPTable totalTable = new PdfPTable(2);
                totalTable.WidthPercentage = 40;
                totalTable.HorizontalAlignment = Element.ALIGN_RIGHT;
                totalTable.SpacingBefore = 20;

                totalTable.AddCell(new PdfPCell(new Phrase("Estimated Total:", sectionFont)) { Border = Rectangle.NO_BORDER, BackgroundColor = lightGreen, Padding = 6 });
                totalTable.AddCell(new PdfPCell(new Phrase($"{rfq.EstimatedBudget:C}", sectionFont)) { Border = Rectangle.NO_BORDER, BackgroundColor = lightGreen, Padding = 6, HorizontalAlignment = Element.ALIGN_RIGHT });
                doc.Add(totalTable);

                // --- CONTACT ---
                PdfPTable contact = new PdfPTable(1);
                contact.WidthPercentage = 100;
                contact.SpacingBefore = 30;
                PdfPCell contactCell = new PdfPCell();
                contactCell.BackgroundColor = accentGray;
                contactCell.AddElement(new Paragraph("For any queries, please contact:", sectionFont));
                contactCell.AddElement(new Paragraph("Email: procurement@adrakenya.org", normalFont));
                contactCell.AddElement(new Paragraph("Phone: +254 700 123 456", normalFont));
                contactCell.Padding = 10;
                contactCell.Border = Rectangle.NO_BORDER;
                contact.AddCell(contactCell);
                doc.Add(contact);

                // --- PAGE BREAK ---
                doc.NewPage();

                // 🌍 SECOND PAGE: Terms & Conditions
                doc.Add(new Paragraph("TERMS & CONDITIONS – ADRA PROCUREMENT", titleFont));
                doc.Add(new Paragraph("\nPlease review the following terms carefully before submitting your quotation:\n\n", normalFont));

                // Bullet points
                var terms = new[]
                {
            "All quotations must be submitted before the specified deadline.",
            "Prices should be quoted in Kenyan Shillings (KES) and must be VAT inclusive.",
            "Suppliers must indicate delivery timelines and warranty information where applicable.",
            "Payment will be made via bank transfer within 30 days upon receipt of goods/services and invoice.",
            "ADRA Kenya reserves the right to reject any or all quotations without assigning reasons.",
            "All suppliers must comply with ADRA Kenya’s Code of Conduct and Ethical Standards."
        };

                foreach (var term in terms)
                {
                    Paragraph p = new Paragraph("• " + term, normalFont);
                    p.SpacingBefore = 5;
                    doc.Add(p);
                }

                doc.Add(new Paragraph("\nBy submitting your quotation, you acknowledge and agree to abide by these terms.", normalFont));

                // --- FOOTER ---
                Paragraph footer = new Paragraph("\nThank you for partnering with ADRA Kenya.", sectionFont);
                footer.Alignment = Element.ALIGN_CENTER;
                doc.Add(footer);

                doc.Close();
                return ms.ToArray();
            }
        }

        private byte[] GenerateRFQInvoicePDF(RequestForQuotation rfq, string logoPath)
        {
            using (var ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter writer = PdfWriter.GetInstance(doc, ms);
                doc.Open();

                // Colors
                var darkBlue = new BaseColor(0, 33, 71);
                var yellow = new BaseColor(247, 183, 49);
                var green = new BaseColor(0, 126, 51);
                var lightGray = new BaseColor(245, 245, 245);

                // Fonts
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.WHITE);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.WHITE);
                var sectionHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, darkBlue);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

                // --- HEADER ---
                PdfPTable header = new PdfPTable(2);
                header.WidthPercentage = 100;
                header.SetWidths(new float[] { 50, 50 });

                // Logo
                PdfPCell logoCell = new PdfPCell();
                if (File.Exists(logoPath))
                {
                    var logo = iTextSharp.text.Image.GetInstance(logoPath);
                    logo.ScaleToFit(120f, 60f);
                    logo.Alignment = Element.ALIGN_LEFT;
                    logoCell.AddElement(logo);
                }
                logoCell.Border = Rectangle.NO_BORDER;

                // Title
                PdfPCell titleCell = new PdfPCell(new Phrase("REQUEST FOR QUOTATION", titleFont));
                titleCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                titleCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                titleCell.Border = Rectangle.NO_BORDER;
                titleCell.BackgroundColor = darkBlue;
                titleCell.Padding = 15;

                header.AddCell(logoCell);
                header.AddCell(titleCell);
                doc.Add(header);

                // Yellow divider
                PdfPCell divider = new PdfPCell(new Phrase(""));
                divider.Border = Rectangle.NO_BORDER;
                divider.BackgroundColor = yellow;
                divider.FixedHeight = 6f;
                PdfPTable dividerTable = new PdfPTable(1);
                dividerTable.WidthPercentage = 100;
                dividerTable.AddCell(divider);
                doc.Add(dividerTable);

                // --- INVOICE INFO ---
                PdfPTable infoTable = new PdfPTable(2);
                infoTable.SpacingBefore = 15f;
                infoTable.WidthPercentage = 100;
                infoTable.SetWidths(new float[] { 50, 50 });

                PdfPCell leftInfo = new PdfPCell();
                leftInfo.AddElement(new Paragraph("RFQ To:", sectionHeaderFont));
                leftInfo.AddElement(new Paragraph(rfq.VendorName, normalFont));
                leftInfo.AddElement(new Paragraph(rfq.AdditionalNotes, normalFont));
                leftInfo.Border = Rectangle.NO_BORDER;

                PdfPCell rightInfo = new PdfPCell();
                rightInfo.AddElement(new Paragraph("RFQ From:", sectionHeaderFont));
                rightInfo.AddElement(new Paragraph("Swift Financials Ltd", normalFont));
                rightInfo.AddElement(new Paragraph("Procurement Department", normalFont));
                rightInfo.AddElement(new Paragraph("www.swiftfinancials.co.ke", normalFont));
                rightInfo.Border = Rectangle.NO_BORDER;

                infoTable.AddCell(leftInfo);
                infoTable.AddCell(rightInfo);
                doc.Add(infoTable);

                // --- RFQ DETAILS TABLE ---
                PdfPTable table = new PdfPTable(3);
                table.WidthPercentage = 100;
                table.SpacingBefore = 20f;
                table.SetWidths(new float[] { 60, 20, 20 });

                table.AddCell(new PdfPCell(new Phrase("Item Description", headerFont)) { BackgroundColor = yellow, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });
                table.AddCell(new PdfPCell(new Phrase("Quantity", headerFont)) { BackgroundColor = yellow, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });
                table.AddCell(new PdfPCell(new Phrase("Unit", headerFont)) { BackgroundColor = yellow, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });
                table.AddCell(new PdfPCell(new Phrase("Amount", headerFont)) { BackgroundColor = yellow, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });

                foreach (var line in rfq.Lines)
                {
                    table.AddCell(new PdfPCell(new Phrase(line.ItemDescription, normalFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(line.Quantity.ToString(), normalFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });
                    table.AddCell(new PdfPCell(new Phrase(line.UnitOfMeasure, normalFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });
                    table.AddCell(new PdfPCell(new Phrase("", normalFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });

                }

                doc.Add(table);

                // --- PAYMENT + CONTACT SECTION ---
                PdfPTable bottomInfo = new PdfPTable(2);
                bottomInfo.WidthPercentage = 100;
                bottomInfo.SpacingBefore = 25f;
                bottomInfo.SetWidths(new float[] { 50, 50 });

                PdfPCell payment = new PdfPCell();
                payment.BackgroundColor = lightGray;
                payment.AddElement(new Paragraph("Payment Method:", sectionHeaderFont));
                payment.AddElement(new Paragraph("Account: 333 2156 6354", normalFont));
                payment.AddElement(new Paragraph("Name: Swift Financials Ltd", normalFont));
                payment.AddElement(new Paragraph("Branch: Nairobi", normalFont));
                payment.Padding = 10;
                payment.Border = Rectangle.NO_BORDER;

                PdfPCell contact = new PdfPCell();
                contact.BackgroundColor = lightGray;
                contact.AddElement(new Paragraph("Contact Info:", sectionHeaderFont));
                contact.AddElement(new Paragraph("Email: info@swiftfinancials.co.ke", normalFont));
                contact.AddElement(new Paragraph("Phone: +254 712 345 678", normalFont));
                contact.AddElement(new Paragraph("Web: www.swiftfinancials.co.ke", normalFont));
                contact.Padding = 10;
                contact.Border = Rectangle.NO_BORDER;

                bottomInfo.AddCell(payment);
                bottomInfo.AddCell(contact);
                doc.Add(bottomInfo);

                // --- TOTAL SECTION ---
                PdfPTable totalTable = new PdfPTable(2);
                totalTable.WidthPercentage = 40;
                totalTable.HorizontalAlignment = Element.ALIGN_RIGHT;
                totalTable.SpacingBefore = 20f;
                string formattedBudget = rfq.EstimatedBudget.ToString();

                totalTable.AddCell(new PdfPCell(new Phrase("Total:", sectionHeaderFont))
                {
                    BackgroundColor = yellow,
                    Border = Rectangle.NO_BORDER,
                    Padding = 8
                });

                totalTable.AddCell(new PdfPCell(new Phrase(formattedBudget, sectionHeaderFont))
                {
                    BackgroundColor = yellow,
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 8
                });


                doc.Add(totalTable);

                // --- FOOTER ---
                Paragraph footer = new Paragraph("\nThank you for your business!", sectionHeaderFont);
                footer.Alignment = Element.ALIGN_CENTER;
                doc.Add(footer);

                doc.Close();
                return ms.ToArray();
            }
        }

        private string SendVendorApprovalEmail(string vendorEmail, string vendorName, RequestForQuotation rfq)
        {
            if (string.IsNullOrWhiteSpace(vendorEmail)) return "No Email Found";

            string subject = $"RFQ Approval Notice: {rfq.RFQNumber}";

            // Convert logo image to Base64
            string logoPath = @"C:\Users\Dorothy Mogoi\OneDrive\Pictures\ADRA - Vertical - Logo.png";
            string logoBase64 = "";
            if (File.Exists(logoPath))
            {
                byte[] logoBytes = File.ReadAllBytes(logoPath);
                logoBase64 = $"data:image/png;base64,{Convert.ToBase64String(logoBytes)}";
            }

            //  HTML email with embedded logo and invoice-style layout
            string body = $@"
<html>
<head>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f7f6;
            margin: 0;
            padding: 0;
            color: #333;
        }}
        .container {{
            max-width: 750px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 10px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.08);
            overflow: hidden;
            border-top: 5px solid #007e33;
        }}
        .header {{
            background-color: #002147;
            color: #fff;
            padding: 20px;
            display: flex;
            align-items: center;
            justify-content: space-between;
        }}
        .header img {{
            height: 50px;
        }}
        .header h1 {{
            margin: 0;
            font-size: 22px;
            letter-spacing: 1px;
            text-align: right;
        }}
        .invoice-meta {{
            background-color: #f4f4f4;
            padding: 15px 25px;
            border-bottom: 2px solid #007e33;
        }}
        .invoice-meta p {{
            margin: 4px 0;
            font-size: 14px;
        }}
        .content {{
            padding: 30px;
            line-height: 1.8;
        }}
        .highlight {{
            color: #007e33;
            font-weight: 600;
        }}
        .rfq-details {{
            background-color: #f8fdf8;
            border: 1px solid #d2f0d2;
            border-radius: 8px;
            padding: 15px 20px;
            margin: 20px 0;
        }}
        .rfq-details p {{
            margin: 6px 0;
        }}
        .button {{
            display: inline-block;
            background: #007e33;
            color: #fff;
            padding: 12px 25px;
            border-radius: 6px;
            text-decoration: none;
            font-weight: 600;
            transition: background 0.3s ease;
        }}
        .button:hover {{
            background: #00b34f;
        }}
        .footer {{
            background-color: #f4f4f4;
            text-align: center;
            padding: 15px;
            font-size: 12px;
            color: #666;
        }}
        .footer a {{
            color: #007e33;
            text-decoration: none;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <img src='{logoBase64}' alt='Company Logo'/>
            <h1>INVOICE / RFQ APPROVAL NOTICE</h1>
        </div>

        <div class='invoice-meta'>
            <p><strong>Invoice ID:</strong> {rfq.RFQNumber}</p>
            <p><strong>Date:</strong> {DateTime.Now:yyyy-MM-dd}</p>
            <p><strong>Issued To:</strong> {vendorName}</p>
        </div>

        <div class='content'>
            <p>Dear <strong>{vendorName}</strong>,</p>
            <p>Your quotation has been <span class='highlight'>approved</span> for the RFQ below:</p>

            <div class='rfq-details'>
                <p><strong>RFQ Number:</strong> {rfq.RFQNumber}</p>
                <p><strong>Department:</strong> {rfq.Department}</p>
                <p><strong>Priority:</strong> {rfq.Priority}</p>
                <p><strong>Expected Delivery:</strong> {rfq.ExpectedDeliveryDate:yyyy-MM-dd}</p>
            </div>

            <p>
                Kindly confirm your expected delivery timelines at your earliest convenience.
            </p>

            <a href='#' class='button'>Confirm Delivery Schedule</a>

            <p style='margin-top: 25px;'>Thank you for your partnership with 
            <span class='highlight'>Swift Financials Procurement</span>.</p>

            <p>Warm regards,<br/><strong>Swift Financials Procurement Team</strong></p>
        </div>

        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} Swift Financials Ltd. All rights reserved.</p>
            <p><a href='https://adrakenya.org/'>www.Adrakenya.org</a></p>
        </div>
    </div>
</body>
</html>";

            try
            {
                using (var ms = new MemoryStream())
                {
                    Document doc = new Document(PageSize.A4, 50, 50, 25, 25);
                    PdfWriter.GetInstance(doc, ms);
                    doc.Open();

                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, new BaseColor(0, 126, 51));
                    var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, BaseColor.BLACK);

                    // Add logo
                    if (File.Exists(logoPath))
                    {
                        iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(logoPath);
                        logo.ScaleToFit(120f, 60f);
                        logo.Alignment = Element.ALIGN_RIGHT;
                        doc.Add(logo);
                    }

                    doc.Add(new Paragraph($"RFQ APPROVAL NOTICE — {rfq.RFQNumber}", titleFont));
                    doc.Add(new Paragraph($"Date: {DateTime.Now:yyyy-MM-dd}\n\n", normalFont));
                    doc.Add(new Paragraph($"Vendor: {vendorName}", normalFont));
                    doc.Add(new Paragraph($"Email: {vendorEmail}\n\n", normalFont));
                    doc.Add(new Paragraph($"Department: {rfq.Department}", normalFont));
                    doc.Add(new Paragraph($"Priority: {rfq.Priority}", normalFont));
                    doc.Add(new Paragraph($"Expected Delivery Date: {rfq.ExpectedDeliveryDate:yyyy-MM-dd}\n\n", normalFont));

                    PdfPTable table = new PdfPTable(3);
                    table.WidthPercentage = 100;
                    table.SpacingBefore = 10f;
                    table.SpacingAfter = 10f;

                    var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.WHITE);
                    var greenHeader = new BaseColor(0, 126, 51);

                    table.AddCell(new PdfPCell(new Phrase("Item Description", headerFont)) { BackgroundColor = greenHeader, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 8 });
                    table.AddCell(new PdfPCell(new Phrase("Quantity", headerFont)) { BackgroundColor = greenHeader, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 8 });
                    table.AddCell(new PdfPCell(new Phrase("Unit of Measure", headerFont)) { BackgroundColor = greenHeader, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 8 });

                    foreach (var line in rfq.Lines)
                    {
                        table.AddCell(new PdfPCell(new Phrase(line.ItemDescription, normalFont)) { Padding = 6 });
                        table.AddCell(new PdfPCell(new Phrase(line.Quantity.ToString(), normalFont)) { Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER });
                        table.AddCell(new PdfPCell(new Phrase(line.UnitOfMeasure, normalFont)) { Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER });
                    }

                    doc.Add(table);
                    doc.Close();

                    var pdfBytes = ms.ToArray();
                    EmailHelper.SendEmailWithAttachment(vendorEmail, subject, body, pdfBytes, $"RFQ_Approval_{rfq.RFQNumber}.pdf");

                    return "Approval Email Sent";
                }
            }
            catch (Exception ex)
            {
                return $"Approval Email Failed: {ex.Message}";
            }
        }

        private List<RFQLine> GetRFQLines(SqlConnection conn, int rfqId)
        {
            var lines = new List<RFQLine>();

            using (var cmd = new SqlCommand(@"
        SELECT 
            Id,
            RFQId,
            ProjectId,
            ProjectDescription,
            BudgetLineId,
            BudgetDescription,
            ItemCode,
            ItemDescription,
            Quantity,
            UnitOfMeasure,
            EstimatedUnitPrice,
            Notes,
            CreatedDate
        FROM RFQLines
        WHERE RFQId = @RFQId", conn))
            {
                cmd.Parameters.AddWithValue("@RFQId", rfqId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var line = new RFQLine
                        {
                            Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                            RFQId = reader["RFQId"] != DBNull.Value ? Convert.ToInt32(reader["RFQId"]) : 0,
                            projectid = reader["ProjectId"] != DBNull.Value ? Convert.ToInt32(reader["ProjectId"]) : 0,
                            projectDescription = reader["ProjectDescription"] as string,
                            BudgetLineId = reader["BudgetLineId"] != DBNull.Value ? Convert.ToInt64(reader["BudgetLineId"]) : (long?)null,
                            BudgetDescription = reader["BudgetDescription"] as string,
                            ItemCode = reader["ItemCode"] as string,
                            ItemDescription = reader["ItemDescription"] as string,
                            Quantity = reader["Quantity"] != DBNull.Value ? Convert.ToInt32(reader["Quantity"]) : 0,
                            UnitOfMeasure = reader["UnitOfMeasure"] as string,
                            EstimatedUnitPrice = reader["EstimatedUnitPrice"] != DBNull.Value ? Convert.ToDecimal(reader["EstimatedUnitPrice"]) : 0,
                            Notes = reader["Notes"] as string,
                            CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : DateTime.MinValue
                        };

                        lines.Add(line);
                    }
                }
            }

            return lines;
        }

        private RequestForQuotation ReadRFQ(SqlDataReader reader)
        {
            return new RequestForQuotation
            {
                Id = reader["Id"] != DBNull.Value ? (int)reader["Id"] : 0,
                VendorId = reader["VendorId"] != DBNull.Value ? (int)reader["VendorId"] : 0,
                VendorName = reader["VendorName"]?.ToString() ?? string.Empty,
                ItemDescription = reader["ItemDescription"]?.ToString() ?? string.Empty,
                Quantity = reader["Quantity"] != DBNull.Value ? Convert.ToInt32(reader["Quantity"]) : 0,
                ExpectedDeliveryDate = reader["ExpectedDeliveryDate"] != DBNull.Value ? Convert.ToDateTime(reader["ExpectedDeliveryDate"]) : DateTime.MinValue,
                CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : DateTime.MinValue,
                Status = reader["Status"]?.ToString() ?? string.Empty,
                RFQNumber = reader["RFQNumber"]?.ToString() ?? string.Empty,
                Priority = reader["Priority"]?.ToString() ?? "Normal",
                Department = reader["Department"]?.ToString() ?? "General",
                RequestedBy = reader["RequestedBy"]?.ToString() ?? "System",
                EstimatedBudget = reader["EstimatedBudget"] != DBNull.Value ? Convert.ToDecimal(reader["EstimatedBudget"]) : 0,
                DeliveryLocation = reader["DeliveryLocation"]?.ToString() ?? string.Empty,
                AdditionalNotes = reader["AdditionalNotes"]?.ToString() ?? string.Empty,
                FileBase64 = reader["FileBase64"]?.ToString() ?? string.Empty,
                Lines = new List<RFQLine>()
            };
        }

        private string GetVendorEmail(int vendorId) => GetVendorField(vendorId, "Email");

        private string GetVendorName(int vendorId) => GetVendorField(vendorId, "VendorName");

        private string GetVendorField(int vendorId, string fieldName)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT {fieldName} FROM Vendors WHERE VendorId=@Id";
                    cmd.Parameters.AddWithValue("@Id", vendorId);
                    conn.Open();
                    return cmd.ExecuteScalar()?.ToString() ?? "";
                }
            }
            catch { return ""; }
        }

        private RequestForQuotation GetRFQById(SqlConnection conn, int rfqId)
        {
            RequestForQuotation rfq = null;

            string query = @"
        SELECT RFQId, RFQNumber, Priority, Department, RequestedBy, 
               ExpectedDeliveryDate, DeliveryLocation, AdditionalNotes,FileBase64
        FROM RFQs
        WHERE RFQId = @RFQId";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@RFQId", rfqId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        rfq = new RequestForQuotation
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("RFQId")),
                            RFQNumber = reader.GetString(reader.GetOrdinal("RFQNumber")),
                            Priority = reader.GetString(reader.GetOrdinal("Priority")),
                            Department = reader.GetString(reader.GetOrdinal("Department")),
                            RequestedBy = reader.GetString(reader.GetOrdinal("RequestedBy")),
                            ExpectedDeliveryDate = reader.GetDateTime(reader.GetOrdinal("ExpectedDeliveryDate")),
                            DeliveryLocation = reader.GetString(reader.GetOrdinal("DeliveryLocation")),
                            AdditionalNotes = reader.GetString(reader.GetOrdinal("AdditionalNotes")),
                            FileBase64 = reader.GetString(reader.GetOrdinal("FileBase64")),
                            Lines = new List<RFQLine>()
                        };
                    }
                }
            }

            // Load lines for this RFQ
            if (rfq != null)
            {
                rfq.Lines = GetRFQLines(conn, rfq.Id);
            }

            return rfq;
        }

        private IHttpActionResult OkResponse(string message, object data = null) =>
            Ok(new ApiResponse { Success = true, Message = message, Data = data });

        private IHttpActionResult ErrorResponse(string message, string code, Exception ex) =>
            Content(HttpStatusCode.InternalServerError, new ApiResponse { Success = false, Message = message, ErrorCode = code, Data = ex.Message });

        private IHttpActionResult BadRequestResponse(string message, string code) =>
            Content(HttpStatusCode.BadRequest, new ApiResponse { Success = false, Message = message, ErrorCode = code });

        #endregion

        #region EMAIL HELPER

        public static class EmailHelper
        {
            private static readonly string SmtpHost = "smtp.gmail.com";
            private static readonly int SmtpPort = 587;
            private static readonly string SmtpUser = "johnkarenju690@gmail.com"; // ✅ Your actual sender email
            private static readonly string SmtpPass = "qjcg lizx qpgw psna";       // ✅ Gmail App password
            private static readonly string FromAddress = "johnkarenju690@gmail.com";

            public static void SendEmailWithAttachment(string toEmail, string subject, string body, byte[] attachmentBytes, string attachmentName)
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Swift Financials Procurement", FromAddress));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = body };
                builder.Attachments.Add(attachmentName, attachmentBytes, new ContentType("application", "pdf"));
                message.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    client.Connect(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
                    client.Authenticate(SmtpUser, SmtpPass);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
        }

        #endregion

    }
}
