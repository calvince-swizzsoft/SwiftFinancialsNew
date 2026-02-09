using MimeKit;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using TestApis.Models;
using System.Data;
using SwiftFinancials.Controllers;

namespace TestApis.Controllers
{
    [RoutePrefix("api/BidAnalysis")]
    public class BidAnalysisController : ApiController
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        public class BidAnalysis
        {
            public int BidId { get; set; }
            public int ProjectId { get; set; }
            public string BidTitle { get; set; }
            public DateTime BidDate { get; set; }
            public string Description { get; set; }
            public string Status { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedDate { get; set; }
            public string EvaluationResult { get; set; }
            public bool AutoEvaluate { get; set; } = false;
            public List<BidVendor> Vendors { get; set; } = new List<BidVendor>();
        }

        public class BidVendor
        {
            public int VendorId { get; set; }
            public int BidId { get; set; }
            public string VendorName { get; set; }
            public string ContactPerson { get; set; }
            public string ContactEmail { get; set; }
            public string ContactPhone { get; set; }
            public decimal QuotationAmount { get; set; }
            public string DeliveryPeriod { get; set; }
            public string Remarks { get; set; }


        }

        public class ManualEvaluationRequest
        {
            public string SelectedVendor { get; set; }
        }


        [HttpPost]
        [Route("createmember")]
        public async Task<IHttpActionResult> CreateMember([FromBody] CustomerDTO2 model)
        {
            if (model == null)
                return BadRequest("Member data is required.");

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {

                        var memberId = Guid.NewGuid();

                        const string memberSql = @"
INSERT INTO swiftFin_Customers
(
    Id, StationId, Type, SerialNumber, PersonalIdentificationNumber,
    Individual_Type, Individual_FirstName, Individual_LastName,
    Individual_IdentityCardType, Individual_IdentityCardNumber, Individual_IdentityCardSerialNumber,
    Individual_PayrollNumbers, Individual_Salutation, Individual_Gender, Individual_MaritalStatus,
    Individual_Nationality, Individual_BirthDate, Individual_EmploymentDesignation, Individual_EmploymentTermsOfService,
    Individual_EmploymentDate, Individual_Classification, NonIndividual_Description, NonIndividual_RegistrationNumber,
    NonIndividual_RegistrationSerialNumber, NonIndividual_DateEstablished, Address_AddressLine1, Address_AddressLine2,
    Address_Street, Address_PostalCode, Address_City, Address_Email, Address_LandLine, Address_MobileLine,
    PassportImageId, SignatureImageId, IdentityCardFrontSideImageId, IdentityCardBackSideImageId,
    BiometricFingerprintImageId, BiometricFingerprintTemplateId, BiometricFingerprintTemplateFormat,
    BiometricFingerVeinTemplateId, BiometricFingerVeinTemplateFormat, RegistrationDate, Reference1, Reference2,
    Reference3, Remarks, IsDefaulter, IsLocked, InhibitGuaranteeing, RecruitedBy, RecordStatus,
    ModifiedBy, ModifiedDate, AdministrativeDivisionId, SequentialId, CreatedBy, CreatedDate
)
VALUES
(
    @Id, @StationId, @Type, @SerialNumber, @PersonalIdentificationNumber,
    @Individual_Type, @Individual_FirstName, @Individual_LastName,
    @Individual_IdentityCardType, @Individual_IdentityCardNumber, @Individual_IdentityCardSerialNumber,
    @Individual_PayrollNumbers, @Individual_Salutation, @Individual_Gender, @Individual_MaritalStatus,
    @Individual_Nationality, @Individual_BirthDate, @Individual_EmploymentDesignation, @Individual_EmploymentTermsOfService,
    @Individual_EmploymentDate, @Individual_Classification, @NonIndividual_Description, @NonIndividual_RegistrationNumber,
    @NonIndividual_RegistrationSerialNumber, @NonIndividual_DateEstablished, @Address_AddressLine1, @Address_AddressLine2,
    @Address_Street, @Address_PostalCode, @Address_City, @Address_Email, @Address_LandLine, @Address_MobileLine,
    @PassportImageId, @SignatureImageId, @IdentityCardFrontSideImageId, @IdentityCardBackSideImageId,
    @BiometricFingerprintImageId, @BiometricFingerprintTemplateId, @BiometricFingerprintTemplateFormat,
    @BiometricFingerVeinTemplateId, @BiometricFingerVeinTemplateFormat, @RegistrationDate, @Reference1, @Reference2,
    @Reference3, @Remarks, @IsDefaulter, @IsLocked, @InhibitGuaranteeing, @RecruitedBy, @RecordStatus,
    @ModifiedBy, @ModifiedDate, @AdministrativeDivisionId, @SequentialId, @CreatedBy, @CreatedDate
)";

                        var cmd = new SqlCommand(memberSql, conn, tx);

                        // Map all parameters from your DTO
                        // Member GUIDs
                        cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = memberId;
                        cmd.Parameters.Add("@StationId", SqlDbType.UniqueIdentifier).Value = model.StationId;

                        cmd.Parameters.Add("@PassportImageId", SqlDbType.UniqueIdentifier).Value =
                            model.PassportImageId != Guid.Empty ? (object)model.PassportImageId : DBNull.Value;
                        cmd.Parameters.Add("@SignatureImageId", SqlDbType.UniqueIdentifier).Value =
                            model.SignatureImageId != Guid.Empty ? (object)model.SignatureImageId : DBNull.Value;
                        cmd.Parameters.Add("@IdentityCardFrontSideImageId", SqlDbType.UniqueIdentifier).Value =
                            model.IdentityCardFrontSideImageId != Guid.Empty ? (object)model.IdentityCardFrontSideImageId : DBNull.Value;
                        cmd.Parameters.Add("@IdentityCardBackSideImageId", SqlDbType.UniqueIdentifier).Value =
                            model.IdentityCardBackSideImageId != Guid.Empty ? (object)model.IdentityCardBackSideImageId : DBNull.Value;
                        cmd.Parameters.Add("@BiometricFingerprintImageId", SqlDbType.UniqueIdentifier).Value =
                            model.BiometricFingerprintImageId != Guid.Empty ? (object)model.BiometricFingerprintImageId : DBNull.Value;
                        cmd.Parameters.Add("@BiometricFingerprintTemplateId", SqlDbType.UniqueIdentifier).Value =
                            model.BiometricFingerprintTemplateId != Guid.Empty ? (object)model.BiometricFingerprintTemplateId : DBNull.Value;
                        cmd.Parameters.Add("@BiometricFingerVeinTemplateId", SqlDbType.UniqueIdentifier).Value =
                            model.BiometricFingerVeinTemplateId != Guid.Empty ? (object)model.BiometricFingerVeinTemplateId : DBNull.Value;
                        cmd.Parameters.Add("@AdministrativeDivisionId", SqlDbType.UniqueIdentifier).Value =
                            model.AdministrativeDivisionId != null && model.AdministrativeDivisionId != Guid.Empty
                                ? (object)model.AdministrativeDivisionId
                                : DBNull.Value;
                        cmd.Parameters.Add("@SequentialId", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();

                        // Other non-GUID parameters
                        cmd.Parameters.AddWithValue("@Type", model.Type);
                        cmd.Parameters.AddWithValue("@SerialNumber", model.SerialNumber);
                        cmd.Parameters.AddWithValue("@PersonalIdentificationNumber", model.PersonalIdentificationNumber ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Individual_Type", model.IndividualType);
                        cmd.Parameters.AddWithValue("@Individual_FirstName", model.IndividualFirstName);
                        cmd.Parameters.AddWithValue("@Individual_LastName", model.IndividualLastName);
                        cmd.Parameters.AddWithValue("@Individual_IdentityCardType", model.IndividualIdentityCardType);
                        cmd.Parameters.AddWithValue("@Individual_IdentityCardNumber", model.IndividualIdentityCardNumber);
                        cmd.Parameters.AddWithValue("@Individual_IdentityCardSerialNumber", model.IndividualIdentityCardSerialNumber ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Individual_PayrollNumbers", model.IndividualPayrollNumbers ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Individual_Salutation", model.IndividualSalutation);
                        cmd.Parameters.AddWithValue("@Individual_Gender", model.IndividualGender);
                        cmd.Parameters.AddWithValue("@Individual_MaritalStatus", model.IndividualMaritalStatus);
                        cmd.Parameters.AddWithValue("@Individual_Nationality", model.IndividualNationality);
                        cmd.Parameters.AddWithValue("@Individual_BirthDate", model.IndividualBirthDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Individual_EmploymentDesignation", model.IndividualEmploymentDesignation ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Individual_EmploymentTermsOfService", model.IndividualEmploymentTermsOfService);
                        cmd.Parameters.AddWithValue("@Individual_EmploymentDate", model.IndividualEmploymentDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Individual_Classification", model.IndividualClassification);
                        cmd.Parameters.AddWithValue("@NonIndividual_Description", model.NonIndividualDescription ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NonIndividual_RegistrationNumber", model.NonIndividualRegistrationNumber ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NonIndividual_RegistrationSerialNumber", model.NonIndividualRegistrationSerialNumber ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NonIndividual_DateEstablished", model.NonIndividualDateEstablished ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address_AddressLine1", model.AddressAddressLine1 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address_AddressLine2", model.AddressAddressLine2 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address_Street", model.AddressStreet ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address_PostalCode", model.AddressPostalCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address_City", model.AddressCity ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address_Email", model.AddressEmail ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address_LandLine", model.AddressLandLine ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address_MobileLine", model.AddressMobileLine ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BiometricFingerprintTemplateFormat", model.BiometricFingerprintTemplateFormat);
                        cmd.Parameters.AddWithValue("@BiometricFingerVeinTemplateFormat", model.BiometricFingerVeinTemplateFormat);
                        cmd.Parameters.AddWithValue("@RegistrationDate", model.RegistrationDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Reference1", model.Reference1 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Reference2", model.Reference2 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Reference3", model.Reference3 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsDefaulter", model.IsDefaulter);
                        cmd.Parameters.AddWithValue("@IsLocked", model.IsLocked);
                        cmd.Parameters.AddWithValue("@InhibitGuaranteeing", model.InhibitGuaranteeing);
                        cmd.Parameters.AddWithValue("@RecruitedBy", model.RecruitedBy ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RecordStatus", model.RecordStatus);
                        cmd.Parameters.AddWithValue("@ModifiedBy", model.ModifiedBy ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ModifiedDate", model.ModifiedDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy ?? "SYSTEM");
                        cmd.Parameters.AddWithValue("@CreatedDate", model.CreatedDate == DateTime.MinValue ? DateTime.UtcNow : model.CreatedDate);

                        await cmd.ExecuteNonQueryAsync();

                        // Continue with BankDetails and NextOfKin insert logic as in your original code
                        // ...
                        tx.Commit();
                        return Ok(new { Message = "Member created successfully", model });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Submits a supplier quotation and its line items transactionally.
        /// </summary>
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

        /// <summary>
        /// Performs bid analysis and awards a vendor for a given RFQ.
        /// </summary>
        [HttpPost]
        [Route("AwardVendor")]
        public IHttpActionResult AwardVendor([FromBody] SupplierQuotation supplierQuotation)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var quotations = GetQuotationsForRFQ(conn, supplierQuotation.RFQId);
                    if (quotations.Count == 0)
                        return BadRequestResponse("No quotations found for this RFQ.", "NO_BIDS");

                    // Simple evaluation — lowest unit price wins
                    var vendorQuotation = quotations
      .Where(q => q.VendorId == supplierQuotation.VendorId)
      .FirstOrDefault();

                    if (vendorQuotation == null)
                        return BadRequestResponse("No quotation found for this vendor.", "NOT_FOUND");


                    UpdateRFQStatus(conn, supplierQuotation.RFQId, "Awarded", supplierQuotation.VendorId, supplierQuotation.VendorName);
                    NotifySupplierByEmail(supplierQuotation.ContactPerson, supplierQuotation.RFQId, supplierQuotation.VendorName);

                    return OkResponse($"RFQ {supplierQuotation.RFQId} awarded to vendor {supplierQuotation.VendorName}.", vendorQuotation);
                }
            }
            catch (Exception ex)
            {
                return ErrorResponse("Failed to perform bid analysis or award vendor.", "AWARD_ERROR", ex);
            }
        }

        /// <summary>
        /// Analyzes related RFQs within the same category.
        /// </summary>
        [HttpGet]
        [Route("AnalyzeRelatedRFQs/{rfqId}")]
        public IHttpActionResult AnalyzeRelatedRFQs(int rfqId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var relatedRfqs = GetRelatedRFQs(conn, rfqId);
                    var analysisResults = new List<object>();

                    foreach (var related in relatedRfqs)
                    {
                        var quotations = GetQuotationsForRFQ(conn, related.Id);
                        if (quotations.Count == 0) continue;

                        analysisResults.Add(new { RelatedRFQ = related });
                    }

                    return OkResponse("Related RFQ analysis completed successfully.", analysisResults);
                }
            }
            catch (Exception ex)
            {
                return ErrorResponse("Failed to analyze related RFQs.", "RELATED_ANALYSIS_ERROR", ex);
            }
        }

        private int InsertQuotation(SqlConnection conn, SqlTransaction transaction, SupplierQuotation quotation)
        {
            using (var cmd = new SqlCommand(@"
                INSERT INTO SupplierQuotation (RFQId, SupplierId, SupplierName, TotalAmount, CreatedAt)
                OUTPUT INSERTED.Id
                VALUES (@RFQId, @SupplierId, @SupplierName, @TotalAmount, GETDATE())", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@RFQId", quotation.RFQId);
                cmd.Parameters.AddWithValue("@SupplierId", quotation.VendorId);
                cmd.Parameters.AddWithValue("@SupplierName", quotation.VendorName);
                cmd.Parameters.AddWithValue("@TotalAmount", quotation.TaxAmount);

                return (int)cmd.ExecuteScalar();
            }
        }

        private void InsertQuotationLines(SqlConnection conn, SqlTransaction transaction, int quotationId, List<SupplierQuotationLine> lines)
        {
            foreach (var line in lines)
            {
                using (var cmd = new SqlCommand(@"
                    INSERT INTO SupplierQuotationLine (QuotationId, ItemId, Description, Quantity, UnitPrice, Total)
                    VALUES (@QuotationId, @ItemId, @Description, @Quantity, @UnitPrice, @Total)", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@QuotationId", quotationId);
                    cmd.Parameters.AddWithValue("@ItemId", line.ItemCode);
                    cmd.Parameters.AddWithValue("@Description", line.ItemDescription);
                    cmd.Parameters.AddWithValue("@Quantity", line.Quantity);
                    cmd.Parameters.AddWithValue("@UnitPrice", line.UnitPrice);
                    cmd.Parameters.AddWithValue("@Total", line.UnitPrice * line.Quantity);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private List<SupplierQuotation> GetQuotationsForRFQ(SqlConnection conn, int rfqId)
        {
            var list = new List<SupplierQuotation>();
            using (var cmd = new SqlCommand("SELECT Id, VendorId, VendorName, ContactPerson, TaxAmount FROM SupplierQuotation WHERE RFQId = @RFQId", conn))
            {
                cmd.Parameters.AddWithValue("@RFQId", rfqId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SupplierQuotation
                        {
                            Id = reader.GetInt32(0),
                            VendorId = reader.GetInt32(1),
                            VendorName = reader.GetString(2),
                            ContactPerson = reader.GetString(3),
                            TaxAmount = reader.GetDecimal(4)
                        });
                    }
                }
            }
            return list;
        }

        private void UpdateRFQStatus(SqlConnection conn, int rfqId, string newStatus, int awardedVendorId, string awardedVendorName)
        {
            // 1️ Update the RFQ header to "Awarded"
            using (var cmd = new SqlCommand(@"
        UPDATE RequestForQuotation 
        SET Status = @Status, 
            VendorId = @VendorId, 
            VendorName = @VendorName 
        WHERE Id = @RFQId", conn))
            {
                cmd.Parameters.AddWithValue("@Status", newStatus);
                cmd.Parameters.AddWithValue("@VendorId", awardedVendorId);
                cmd.Parameters.AddWithValue("@VendorName", awardedVendorName);
                cmd.Parameters.AddWithValue("@RFQId", rfqId);

                cmd.ExecuteNonQuery();
            }

            // 2️ Update all supplier quotations related to this RFQ
            // Mark the awarded vendor’s quotation as "Awarded"
            using (var cmdAwarded = new SqlCommand(@"
        UPDATE SupplierQuotation 
        SET Status = 'Awarded' 
        WHERE RFQId = @RFQId AND VendorId = @VendorId", conn))
            {
                cmdAwarded.Parameters.AddWithValue("@RFQId", rfqId);
                cmdAwarded.Parameters.AddWithValue("@VendorId", awardedVendorId);
                cmdAwarded.ExecuteNonQuery();
            }

            // 3️ Mark all other quotations as "Not Awarded"
            using (var cmdOthers = new SqlCommand(@"
        UPDATE SupplierQuotation 
        SET Status = 'Not Awarded' 
        WHERE RFQId = @RFQId AND VendorId <> @VendorId", conn))
            {
                cmdOthers.Parameters.AddWithValue("@RFQId", rfqId);
                cmdOthers.Parameters.AddWithValue("@VendorId", awardedVendorId);
                cmdOthers.ExecuteNonQuery();
            }
        }
        private void NotifySupplierByEmail(string supplierEmail, int rfqId, string supplierName)
        {

            var SmtpPass = "qjcg lizx qpgw psna";
            var FromAddress = "johnkarenju690@gmail.com";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Procurement Team", "johnkarenju690@gmail.com"));
            message.To.Add(new MailboxAddress(supplierName, supplierEmail));
            message.Subject = $"RFQ #{rfqId} Award Notification";
            message.Body = new TextPart("plain")
            {
                Text = $"Dear {supplierName},\n\n" +
                       $"Congratulations! Your quotation has been awarded for RFQ #{rfqId}.\n\n" +
                       $"Thank you for participating.\n\n" +
                       $"Best Regards,\nProcurement Team"
            };

            using (var client = new SmtpClient())
            {
                client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                client.Authenticate(FromAddress, SmtpPass);
                client.Send(message);
                client.Disconnect(true);
            }
        }

        private List<RequestForQuotation> GetRelatedRFQs(SqlConnection conn, int rfqId)
        {
            var list = new List<RequestForQuotation>();

            const string query = @"
        SELECT TOP (1000)
            Id,
            RFQId,
            VendorId,
            VendorName,
            QuotationNumber,
            Currency,
            Discount,
            TaxAmount,
            ShippingCost,
            PaymentTerms,
            WarrantyInfo,
            ContactPerson,
            Notes,
            CreatedDate,
            Status
        FROM SupplierQuotation
        WHERE RFQId = @RFQId";

            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@RFQId", SqlDbType.Int).Value = rfqId;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Map SupplierQuotation fields to RequestForQuotation model
                        var rfq = new RequestForQuotation
                        {
                            Id = reader["RFQId"] != DBNull.Value ? Convert.ToInt32(reader["RFQId"]) : 0,
                            VendorId = reader["VendorId"] != DBNull.Value ? Convert.ToInt32(reader["VendorId"]) : 0,
                            VendorName = reader["VendorName"] as string,
                            RFQNumber = reader["QuotationNumber"] as string,
                            EstimatedBudget = 0, // Not in SupplierQuotation, optional placeholder
                            Status = reader["Status"] as string,
                            CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : DateTime.MinValue,
                            Lines = new List<RFQLine>() // Will load lines later
                        };

                        list.Add(rfq);
                    }
                }
            }

            // Fetch lines for each RFQ outside reader to avoid open reader conflicts
            foreach (var rfq in list)
            {
                rfq.Lines = GetRFQLines(conn, rfq.Id);
            }

            return list;
        }

        private RequestForQuotation ReadRFQ(SqlDataReader reader)
        {
            return new RequestForQuotation
            {
                Id = (int)reader["Id"],
                VendorId = (int)reader["VendorId"],
                VendorName = reader["VendorName"].ToString(),
                ItemDescription = reader["ItemDescription"].ToString(),
                Quantity = (int)reader["Quantity"],
                ExpectedDeliveryDate = (DateTime)reader["ExpectedDeliveryDate"],
                CreatedDate = (DateTime)reader["CreatedDate"],
                Status = reader["Status"].ToString(),
                RFQNumber = reader["RFQNumber"].ToString(),
                Priority = reader["Priority"].ToString(),
                Department = reader["Department"].ToString(),
                RequestedBy = reader["RequestedBy"].ToString(),
                EstimatedBudget = (decimal)reader["EstimatedBudget"],
                DeliveryLocation = reader["DeliveryLocation"].ToString(),
                AdditionalNotes = reader["AdditionalNotes"].ToString(),
                Lines = new List<RFQLine>()
            };
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

        private IHttpActionResult OkResponse(string message, object data) =>
            Ok(new { success = true, message, data });

        private IHttpActionResult BadRequestResponse(string message, string code) =>
            Content(System.Net.HttpStatusCode.BadRequest, new { success = false, code, message });

        private IHttpActionResult ErrorResponse(string message, string code, Exception ex) =>
            Content(System.Net.HttpStatusCode.InternalServerError, new { success = false, code, message, error = ex.Message });

        [HttpPost]
        [Route("CreateBidWithVendors")]
        public async Task<IHttpActionResult> CreateBidWithVendors([FromBody] BidAnalysis bid)
        {
            if (bid == null || bid.Vendors == null || !bid.Vendors.Any())
                return BadRequest("Invalid bid or vendor data.");

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1️⃣ Create Bid
                        var cmdBid = new SqlCommand(@"
                            INSERT INTO BidAnalysis (ProjectId, BidTitle, BidDate, Description, Status, CreatedBy, CreatedDate)
                            OUTPUT INSERTED.BidId
                            VALUES (@ProjectId, @BidTitle, @BidDate, @Description, @Status, @CreatedBy, GETDATE());
                        ", conn, tran);

                        cmdBid.Parameters.AddWithValue("@ProjectId", bid.ProjectId);
                        cmdBid.Parameters.AddWithValue("@BidTitle", bid.BidTitle);
                        cmdBid.Parameters.AddWithValue("@BidDate", bid.BidDate);
                        cmdBid.Parameters.AddWithValue("@Description", (object)bid.Description ?? DBNull.Value);
                        cmdBid.Parameters.AddWithValue("@Status", (object)bid.Status ?? "Pending");
                        cmdBid.Parameters.AddWithValue("@CreatedBy", (object)bid.CreatedBy ?? "System");

                        bid.BidId = (int)await cmdBid.ExecuteScalarAsync();

                        // 2️⃣ Insert Vendors
                        foreach (var vendor in bid.Vendors)
                        {
                            var cmdVendor = new SqlCommand(@"
                                INSERT INTO BidVendor (BidId, VendorName, ContactPerson, ContactEmail, ContactPhone, QuotationAmount, DeliveryPeriod, Remarks)
                                OUTPUT INSERTED.VendorId
                                VALUES (@BidId, @VendorName, @ContactPerson, @ContactEmail, @ContactPhone, @QuotationAmount, @DeliveryPeriod, @Remarks);
                            ", conn, tran);

                            cmdVendor.Parameters.AddWithValue("@BidId", bid.BidId);
                            cmdVendor.Parameters.AddWithValue("@VendorName", vendor.VendorName);
                            cmdVendor.Parameters.AddWithValue("@ContactPerson", (object)vendor.ContactPerson ?? DBNull.Value);
                            cmdVendor.Parameters.AddWithValue("@ContactEmail", (object)vendor.ContactEmail ?? DBNull.Value);
                            cmdVendor.Parameters.AddWithValue("@ContactPhone", (object)vendor.ContactPhone ?? DBNull.Value);
                            cmdVendor.Parameters.AddWithValue("@QuotationAmount", vendor.QuotationAmount);
                            cmdVendor.Parameters.AddWithValue("@DeliveryPeriod", (object)vendor.DeliveryPeriod ?? DBNull.Value);
                            cmdVendor.Parameters.AddWithValue("@Remarks", (object)vendor.Remarks ?? DBNull.Value);

                            vendor.VendorId = (int)await cmdVendor.ExecuteScalarAsync();
                            vendor.BidId = bid.BidId;
                        }

                        tran.Commit();
                        return Ok(bid);
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }
        }

        [HttpPost]
        [Route("Bidings")]
        public async Task<IHttpActionResult> Bidings([FromBody] BidAnalysis bid)
        {
            if (bid == null || bid.Vendors == null || !bid.Vendors.Any())
                return BadRequest("Invalid bid or vendor data.");

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1️⃣ Create Bid
                        var cmdBid = new SqlCommand(@"
                            INSERT INTO BidAnalysis (ProjectId, BidTitle, BidDate, Description, Status, CreatedBy, CreatedDate)
                            OUTPUT INSERTED.BidId
                            VALUES (@ProjectId, @BidTitle, @BidDate, @Description, @Status, @CreatedBy, GETDATE());
                        ", conn, tran);

                        cmdBid.Parameters.AddWithValue("@ProjectId", bid.ProjectId);
                        cmdBid.Parameters.AddWithValue("@BidTitle", bid.BidTitle);
                        cmdBid.Parameters.AddWithValue("@BidDate", bid.BidDate);
                        cmdBid.Parameters.AddWithValue("@Description", (object)bid.Description ?? DBNull.Value);
                        cmdBid.Parameters.AddWithValue("@Status", (object)bid.Status ?? "Pending");
                        cmdBid.Parameters.AddWithValue("@CreatedBy", (object)bid.CreatedBy ?? "System");

                        bid.BidId = (int)await cmdBid.ExecuteScalarAsync();

                        // 2️⃣ Insert Vendors
                        foreach (var vendor in bid.Vendors)
                        {
                            var cmdVendor = new SqlCommand(@"
                                INSERT INTO BidVendor (BidId, VendorName, ContactPerson, ContactEmail, ContactPhone, QuotationAmount, DeliveryPeriod, Remarks)
                                OUTPUT INSERTED.VendorId
                                VALUES (@BidId, @VendorName, @ContactPerson, @ContactEmail, @ContactPhone, @QuotationAmount, @DeliveryPeriod, @Remarks);
                            ", conn, tran);

                            cmdVendor.Parameters.AddWithValue("@BidId", bid.BidId);
                            cmdVendor.Parameters.AddWithValue("@VendorName", vendor.VendorName);
                            cmdVendor.Parameters.AddWithValue("@ContactPerson", (object)vendor.ContactPerson ?? DBNull.Value);
                            cmdVendor.Parameters.AddWithValue("@ContactEmail", (object)vendor.ContactEmail ?? DBNull.Value);
                            cmdVendor.Parameters.AddWithValue("@ContactPhone", (object)vendor.ContactPhone ?? DBNull.Value);
                            cmdVendor.Parameters.AddWithValue("@QuotationAmount", vendor.QuotationAmount);
                            cmdVendor.Parameters.AddWithValue("@DeliveryPeriod", (object)vendor.DeliveryPeriod ?? DBNull.Value);
                            cmdVendor.Parameters.AddWithValue("@Remarks", (object)vendor.Remarks ?? DBNull.Value);

                            vendor.VendorId = (int)await cmdVendor.ExecuteScalarAsync();
                            vendor.BidId = bid.BidId;
                        }

                        // 3️⃣ Auto Evaluation (optional)
                        if (bid.AutoEvaluate)
                        {
                            var evaluationResult = EvaluateBidAutomatically(bid);
                            bid.EvaluationResult = evaluationResult;

                            var cmdEval = new SqlCommand(@"
                                UPDATE BidAnalysis 
                                SET EvaluationResult = @EvaluationResult, Status = 'Auto Evaluated'
                                WHERE BidId = @BidId;
                            ", conn, tran);

                            cmdEval.Parameters.AddWithValue("@EvaluationResult", evaluationResult);
                            cmdEval.Parameters.AddWithValue("@BidId", bid.BidId);
                            await cmdEval.ExecuteNonQueryAsync();
                        }

                        tran.Commit();
                        return Ok(bid);
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }
        }

        [HttpPost]
        [Route("EvaluateBidManual/{bidId}")]
        public async Task<IHttpActionResult> EvaluateBidManual(int bidId, [FromBody] ManualEvaluationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.SelectedVendor))
                return BadRequest("Missing vendor selection.");

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
                    UPDATE BidAnalysis
                    SET EvaluationResult = @SelectedVendor, Status = 'Manually Evaluated'
                    WHERE BidId = @BidId;
                ", conn);

                cmd.Parameters.AddWithValue("@SelectedVendor", request.SelectedVendor);
                cmd.Parameters.AddWithValue("@BidId", bidId);
                await cmd.ExecuteNonQueryAsync();
            }

            return Ok($"Bid {bidId} manually evaluated and awarded to {request.SelectedVendor}.");
        }
        private string EvaluateBidAutomatically(BidAnalysis bid)
        {
            if (bid?.Vendors == null || !bid.Vendors.Any())
                return "No vendors available for evaluation.";

            // Example scoring: lowest quotation wins (add your own logic later)
            var bestVendor = bid.Vendors
                .OrderBy(v => v.QuotationAmount)
                .ThenBy(v => v.DeliveryPeriod)
                .FirstOrDefault();

            return $"Best Vendor: {bestVendor.VendorName}, Amount: {bestVendor.QuotationAmount:C}, Delivery: {bestVendor.DeliveryPeriod}";
        }

        [HttpGet]
        [Route("GetAllBids")]
        public async Task<IHttpActionResult> GetAllBids()
        {
            var list = new List<BidAnalysis>();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var cmd = new SqlCommand("SELECT * FROM BidAnalysis ORDER BY CreatedDate DESC", conn);
                var rdr = await cmd.ExecuteReaderAsync();

                while (await rdr.ReadAsync())
                {
                    list.Add(new BidAnalysis
                    {
                        BidId = (int)rdr["BidId"],
                        ProjectId = (int)rdr["ProjectId"],
                        BidTitle = rdr["BidTitle"].ToString(),
                        BidDate = Convert.ToDateTime(rdr["BidDate"]),
                        Description = rdr["Description"].ToString(),
                        Status = rdr["Status"].ToString(),
                        CreatedBy = rdr["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(rdr["CreatedDate"])
                    });
                }
                rdr.Close();

                // Attach Vendors
                foreach (var bid in list)
                {
                    var cmdVendors = new SqlCommand("SELECT * FROM BidVendor WHERE BidId=@BidId", conn);
                    cmdVendors.Parameters.AddWithValue("@BidId", bid.BidId);
                    var rdrV = await cmdVendors.ExecuteReaderAsync();
                    while (await rdrV.ReadAsync())
                    {
                        bid.Vendors.Add(new BidVendor
                        {
                            VendorId = (int)rdrV["VendorId"],
                            VendorName = rdrV["VendorName"].ToString(),
                            ContactPerson = rdrV["ContactPerson"].ToString(),
                            ContactEmail = rdrV["ContactEmail"].ToString(),
                            ContactPhone = rdrV["ContactPhone"].ToString(),
                            QuotationAmount = Convert.ToDecimal(rdrV["QuotationAmount"]),
                            DeliveryPeriod = rdrV["DeliveryPeriod"].ToString(),
                            Remarks = rdrV["Remarks"].ToString()
                        });
                    }
                    rdrV.Close();
                }
            }

            return Ok(list);
        }

        [HttpGet]
        [Route("GetBidById/{bidId}")]
        public async Task<IHttpActionResult> GetBidById(int bidId)
        {
            BidAnalysis bid = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var cmd = new SqlCommand("SELECT * FROM BidAnalysis WHERE BidId=@BidId", conn);
                cmd.Parameters.AddWithValue("@BidId", bidId);

                var rdr = await cmd.ExecuteReaderAsync();
                if (await rdr.ReadAsync())
                {
                    bid = new BidAnalysis
                    {
                        BidId = (int)rdr["BidId"],
                        ProjectId = (int)rdr["ProjectId"],
                        BidTitle = rdr["BidTitle"].ToString(),
                        BidDate = Convert.ToDateTime(rdr["BidDate"]),
                        Description = rdr["Description"].ToString(),
                        Status = rdr["Status"].ToString(),
                        CreatedBy = rdr["CreatedBy"].ToString(),
                        CreatedDate = Convert.ToDateTime(rdr["CreatedDate"]),
                        Vendors = new List<BidVendor>()
                    };
                }
                rdr.Close();

                if (bid == null) return NotFound();

                var cmdVendors = new SqlCommand("SELECT * FROM BidVendor WHERE BidId=@BidId", conn);
                cmdVendors.Parameters.AddWithValue("@BidId", bidId);
                var rdrV = await cmdVendors.ExecuteReaderAsync();

                while (await rdrV.ReadAsync())
                {
                    bid.Vendors.Add(new BidVendor
                    {
                        VendorId = (int)rdrV["VendorId"],
                        VendorName = rdrV["VendorName"].ToString(),
                        QuotationAmount = Convert.ToDecimal(rdrV["QuotationAmount"]),
                        ContactPerson = rdrV["ContactPerson"].ToString(),
                        ContactEmail = rdrV["ContactEmail"].ToString(),
                        ContactPhone = rdrV["ContactPhone"].ToString(),
                        DeliveryPeriod = rdrV["DeliveryPeriod"].ToString(),
                        Remarks = rdrV["Remarks"].ToString()
                    });
                }
            }

            return Ok(bid);
        }

    }
}