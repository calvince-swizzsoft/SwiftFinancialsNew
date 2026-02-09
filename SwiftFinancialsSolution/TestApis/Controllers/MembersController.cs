using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Http;
using TestApis.Models;
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
    [RoutePrefix("api/members")]
    public class MembersController : ApiController
    {
        private readonly string _conn;

        public MembersController()
        {
            _conn = System.Configuration.ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }
        //public class MemberBankDetailDTO
        //{
        //    public Guid Id { get; set; }
        //    public Guid CustomerId { get; set; }
        //    public string BankName { get; set; }
        //    public string BranchName { get; set; }
        //    public string AccountName { get; set; }
        //    public string AccountNumber { get; set; }
        //    public bool IsPrimaryAccount { get; set; }
        //    public DateTime CreatedDate { get; set; }
        //}
        //public class MemberEverythingDTO
        //{
        //    public CustomerDTO Member { get; set; }
        //    public List<MemberBankDetailDTO> BankDetails { get; set; }
        //    public List<NextOfKinDTO> NextOfKin { get; set; }
        //}

        //public class MemberFullDetailsDTO
        //{
        //    public CustomerDTO Member { get; set; }
        //    public List<MemberBankDetailDTO> BankDetails { get; set; }
        //    public List<NextOfKinDTO> NextOfKin { get; set; }
        //}



        // ============================================================
        // GET ALL MEMBERS (FULL DETAILS)
        // ============================================================

        [HttpPost]
        [Route("create")]
        public async Task<IHttpActionResult> CreateMember([FromBody] CustomerDTO2 model)
        {
            if (model == null)
                return BadRequest("Member data is required.");

            using (var conn = new SqlConnection(_conn))
            {
                await conn.OpenAsync();

                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var memberId = Guid.NewGuid();

                        const string sql = @"
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

                        var cmd = new SqlCommand(sql, conn, tx);

                        // -----------------------------
                        // GUID HANDLING (C# 7 SAFE)
                        // -----------------------------
                        cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = memberId;

                        cmd.Parameters.Add("@StationId", SqlDbType.UniqueIdentifier).Value =
                            model.StationId == Guid.Empty ? (object)DBNull.Value : model.StationId;

                        cmd.Parameters.Add("@PassportImageId", SqlDbType.UniqueIdentifier).Value =
                            model.PassportImageId == Guid.Empty ? (object)DBNull.Value : model.PassportImageId;

                        cmd.Parameters.Add("@SignatureImageId", SqlDbType.UniqueIdentifier).Value =
                            model.SignatureImageId == Guid.Empty ? (object)DBNull.Value : model.SignatureImageId;

                        cmd.Parameters.Add("@IdentityCardFrontSideImageId", SqlDbType.UniqueIdentifier).Value =
                            model.IdentityCardFrontSideImageId == Guid.Empty ? (object)DBNull.Value : model.IdentityCardFrontSideImageId;

                        cmd.Parameters.Add("@IdentityCardBackSideImageId", SqlDbType.UniqueIdentifier).Value =
                            model.IdentityCardBackSideImageId == Guid.Empty ? (object)DBNull.Value : model.IdentityCardBackSideImageId;

                        cmd.Parameters.Add("@BiometricFingerprintImageId", SqlDbType.UniqueIdentifier).Value =
                            model.BiometricFingerprintImageId == Guid.Empty ? (object)DBNull.Value : model.BiometricFingerprintImageId;

                        cmd.Parameters.Add("@BiometricFingerprintTemplateId", SqlDbType.UniqueIdentifier).Value =
                            model.BiometricFingerprintTemplateId == Guid.Empty ? (object)DBNull.Value : model.BiometricFingerprintTemplateId;

                        cmd.Parameters.Add("@BiometricFingerVeinTemplateId", SqlDbType.UniqueIdentifier).Value =
                            model.BiometricFingerVeinTemplateId == Guid.Empty ? (object)DBNull.Value : model.BiometricFingerVeinTemplateId;

                        cmd.Parameters.Add("@AdministrativeDivisionId", SqlDbType.UniqueIdentifier).Value =
                            model.AdministrativeDivisionId == Guid.Empty ? (object)DBNull.Value : model.AdministrativeDivisionId;

                        cmd.Parameters.Add("@SequentialId", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();

                        // -----------------------------
                        // SCALARS (C# 7 SAFE)
                        // -----------------------------
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

                        cmd.Parameters.AddWithValue("@Individual_BirthDate", model.IndividualBirthDate.HasValue ? (object)model.IndividualBirthDate.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Individual_EmploymentDesignation", model.IndividualEmploymentDesignation ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Individual_EmploymentTermsOfService", model.IndividualEmploymentTermsOfService);

                        cmd.Parameters.AddWithValue("@Individual_EmploymentDate", model.IndividualEmploymentDate.HasValue ? (object)model.IndividualEmploymentDate.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Individual_Classification", model.IndividualClassification);

                        cmd.Parameters.AddWithValue("@NonIndividual_Description", model.NonIndividualDescription ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NonIndividual_RegistrationNumber", model.NonIndividualRegistrationNumber ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NonIndividual_RegistrationSerialNumber", model.NonIndividualRegistrationSerialNumber ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NonIndividual_DateEstablished", model.NonIndividualDateEstablished.HasValue ? (object)model.NonIndividualDateEstablished.Value : DBNull.Value);

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

                        cmd.Parameters.AddWithValue("@RegistrationDate", model.RegistrationDate.HasValue ? (object)model.RegistrationDate.Value : DBNull.Value);
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
                        cmd.Parameters.AddWithValue("@ModifiedDate", model.ModifiedDate.HasValue ? (object)model.ModifiedDate.Value : DBNull.Value);

                        cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy ?? "SYSTEM");
                        cmd.Parameters.AddWithValue("@CreatedDate", model.CreatedDate == DateTime.MinValue ? (object)DateTime.UtcNow : model.CreatedDate);

                        await cmd.ExecuteNonQueryAsync();

                        tx.Commit();

                        return Ok(new { Message = "Member created successfully", MemberId = memberId });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }
        }



        [HttpGet]
        [Route("full")]
        public async Task<IHttpActionResult> GetAllMembersFull()
        {
            var list = new List<CustomerDTO2>();

            var members = await GetAllMembersInternal();

            foreach (var m in members)
            {
                list.Add(new CustomerDTO2
                {
                    customerDTO2s = await GetMemberInternal(m.Id),
                    BankDetails = await GetBankDetails(m.Id),
                    NextOfKin = await GetNextOfKin(m.Id)
                });
            }

            return Ok(members);
        }

        // ============================================================
        // GET SINGLE MEMBER (FULL DETAILS)
        // ============================================================
        //[HttpGet]
        //[Route("{id:guid}/full")]
        //public async Task<IHttpActionResult> GetMemberFull(Guid id)
        //{
        //    var member = await GetMemberInternal(id);
        //    if (member == null)
        //        return NotFound();

        //    var response = new MemberFullDetailsDTO
        //    {
        //        Member = member,
        //        BankDetails = await GetBankDetails(id),
        //        NextOfKin = await GetNextOfKin(id)
        //    };

        //    return Ok(response);
        //}

        // ============================================================
        // GET NEXT OF KIN FOR MEMBER
        // ============================================================
        [HttpGet]
        [Route("{customerId:guid}/kin")]
        public async Task<IHttpActionResult> GetKin(Guid customerId)
        {
            return Ok(await GetNextOfKin(customerId));
        }

        // ============================================================
        // CREATE NEXT OF KIN
        // ============================================================
        [HttpPost]
        [Route("{customerId:guid}/kin/add")]
        public async Task<IHttpActionResult> AddKin(Guid customerId, [FromBody] NextOfKinDTO2 model)
        {
            model.Id = Guid.NewGuid();
            model.CustomerId = customerId;
            model.CreatedDate = DateTime.Now;

            const string sql = @"
                INSERT INTO swiftFin_NextOfKin
                (Id, CustomerId, Salutation, FirstName, LastName, IdentityCardNumber, 
                 Gender, Relationship, IdentityCardType,
                 Address_AddressLine1, Address_AddressLine2, Address_Street,
                 Address_PostalCode, Address_City, Address_Email, Address_LandLine, Address_MobileLine,
                 NominatedPercentage, Remarks, SequentialId, CreatedBy, CreatedDate)
                VALUES
                (@Id, @CustomerId, @Salutation, @FirstName, @LastName, @IdNo,
                 @Gender, @Relationship, @IdType,
                 @AL1, @AL2, @Street,
                 @Postal, @City, @Email, @Land, @Mobile,
                 @Percent, @Remarks, @Seq, @CreatedBy, @CreatedDate)";

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@Salutation", model.Salutation);
                cmd.Parameters.AddWithValue("@FirstName", model.FirstName ?? "");
                cmd.Parameters.AddWithValue("@LastName", model.LastName ?? "");
                cmd.Parameters.AddWithValue("@IdNo", model.IdentityCardNumber ?? "");
                cmd.Parameters.AddWithValue("@Gender", model.Gender);
                cmd.Parameters.AddWithValue("@Relationship", model.Relationship);
                cmd.Parameters.AddWithValue("@IdType", model.IdentityCardType);

                cmd.Parameters.AddWithValue("@AL1", model.AddressAddressLine1 ?? "");
                cmd.Parameters.AddWithValue("@AL2", model.AddressAddressLine2 ?? "");
                cmd.Parameters.AddWithValue("@Street", model.AddressStreet ?? "");
                cmd.Parameters.AddWithValue("@Postal", model.AddressPostalCode ?? "");
                cmd.Parameters.AddWithValue("@City", model.AddressCity ?? "");
                cmd.Parameters.AddWithValue("@Email", model.AddressEmail ?? "");
                cmd.Parameters.AddWithValue("@Land", model.AddressLandLine ?? "");
                cmd.Parameters.AddWithValue("@Mobile", model.AddressMobileLine ?? "");

                cmd.Parameters.AddWithValue("@Percent", model.NominatedPercentage);
                cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? "");

                cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy ?? "System");
                cmd.Parameters.AddWithValue("@CreatedDate", model.CreatedDate);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

            }

            return Ok(new { model.Id });
        }

        // ============================================================
        // INTERNAL ADO.NET QUERIES
        // ============================================================

        private async Task<List<CustomerDTO2>> GetMemberInternal(Guid id)
        {
            const string sql = @"SELECT * FROM swiftFin_Customers WHERE Id=@Id";

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                await conn.OpenAsync();

                var list = new List<CustomerDTO2>();

                using (var rdr = await cmd.ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        list.Add(MapCustomer(rdr));
                    }
                }

                return list;
            }
        }


        private async Task<List<CustomerDTO2>> GetAllMembersInternal()
        {
            var list = new List<CustomerDTO2>();

            const string sql = @"SELECT TOP 5000 * FROM swiftFin_Customers ORDER BY CreatedDate DESC";

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, conn))
            {
                await conn.OpenAsync();
                using (var rdr = await cmd.ExecuteReaderAsync())
                    while (await rdr.ReadAsync())
                        list.Add(MapCustomer(rdr));
            }

            return list;
        }

        private async Task<List<MemberBankDetailDTO>> GetBankDetails(Guid customerId)
        {
            var list = new List<MemberBankDetailDTO>();

            const string sql = @"SELECT * FROM swiftFin_MemberBankDetails WHERE CustomerId=@Id";

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", customerId);
                await conn.OpenAsync();

                using (var rdr = await cmd.ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        list.Add(new MemberBankDetailDTO
                        {
                            Id = rdr.GetGuid("Id"),
                            CustomerId = rdr.GetGuid("CustomerId"),
                            BankName = rdr.GetStringSafe("BankName"),
                            BranchName = rdr.GetStringSafe("BranchName"),
                            AccountName = rdr.GetStringSafe("AccountName"),
                            AccountNumber = rdr.GetStringSafe("AccountNumber"),
                            IsPrimaryAccount = rdr.GetBool("IsPrimaryAccount"),
                            CreatedDate = rdr.GetDate("CreatedDate")
                        });
                    }
                }
            }

            return list;
        }

        private async Task<List<NextOfKinDTO2>> GetNextOfKin(Guid customerId)
        {
            var list = new List<NextOfKinDTO2>();

            const string sql = @"SELECT * FROM swiftFin_NextOfKin WHERE CustomerId=@Id";

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", customerId);
                await conn.OpenAsync();

                using (var rdr = await cmd.ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        list.Add(MapKin(rdr));
                    }
                }
            }

            return list;
        }

        // ============================================================
        // ROW MAPPERS
        // ============================================================

        private NextOfKinDTO2 MapKin(SqlDataReader r)
        {
            return new NextOfKinDTO2
            {
                Id = r.GetGuid("Id"),
                CustomerId = r.GetGuid("CustomerId"),
                Salutation = r.GetByte("Salutation"),
                FirstName = r.GetStringSafe("FirstName"),
                LastName = r.GetStringSafe("LastName"),
                IdentityCardNumber = r.GetStringSafe("IdentityCardNumber"),
                Gender = r.GetByte("Gender"),
                Relationship = r.GetByte("Relationship"),
                IdentityCardType = r.GetByte("IdentityCardType"),
                AddressAddressLine1 = r.GetStringSafe("Address_AddressLine1"),
                AddressAddressLine2 = r.GetStringSafe("Address_AddressLine2"),
                AddressStreet = r.GetStringSafe("Address_Street"),
                AddressPostalCode = r.GetStringSafe("Address_PostalCode"),
                AddressCity = r.GetStringSafe("Address_City"),
                AddressEmail = r.GetStringSafe("Address_Email"),
                AddressLandLine = r.GetStringSafe("Address_LandLine"),
                AddressMobileLine = r.GetStringSafe("Address_MobileLine"),
                NominatedPercentage = r.GetDouble("NominatedPercentage"),
                Remarks = r.GetStringSafe("Remarks"),
                CreatedBy = r.GetStringSafe("CreatedBy"),
                CreatedDate = r.GetDate("CreatedDate")
            };
        }

        private CustomerDTO2 MapCustomer(SqlDataReader r)
        {
            return new CustomerDTO2
            {
                Id = r.GetGuidSafe("Id"),
                StationId = r.GetGuidSafe("StationId"),
                Type = r.GetByteSafe("Type"),
                SerialNumber = r.GetInt32Safe("SerialNumber"),
                PersonalIdentificationNumber = r.GetStringSafe("PersonalIdentificationNumber"),
                IndividualType = r.GetByteSafe("Individual_Type"),
                IndividualFirstName = r.GetStringSafe("Individual_FirstName"),
                IndividualLastName = r.GetStringSafe("Individual_LastName"),
                IndividualIdentityCardType = r.GetByteSafe("Individual_IdentityCardType"),
                IndividualIdentityCardNumber = r.GetStringSafe("Individual_IdentityCardNumber"),
                IndividualIdentityCardSerialNumber = r.GetStringSafe("Individual_IdentityCardSerialNumber"),
                IndividualPayrollNumbers = r.GetStringSafe("Individual_PayrollNumbers"),
                IndividualSalutation = r.GetByteSafe("Individual_Salutation"),
                IndividualGender = r.GetByteSafe("Individual_Gender"),
                IndividualMaritalStatus = r.GetByteSafe("Individual_MaritalStatus"),
                IndividualNationality = r.GetByteSafe("Individual_Nationality"),
                IndividualBirthDate = r.GetDateOrNull("Individual_BirthDate"),
                IndividualEmploymentTermsOfService = r.GetByteSafe("Individual_EmploymentTermsOfService"),
                IndividualClassification = r.GetByteSafe("Individual_Classification"),
                NonIndividualDescription = r.GetStringSafe("NonIndividual_Description"),
                NonIndividualRegistrationNumber = r.GetStringSafe("NonIndividual_RegistrationNumber"),
                NonIndividualRegistrationSerialNumber = r.GetStringSafe("NonIndividual_RegistrationSerialNumber"),
                NonIndividualDateEstablished = r.GetDateOrNull("NonIndividual_DateEstablished"),
                AddressAddressLine1 = r.GetStringSafe("Address_AddressLine1"),
                AddressAddressLine2 = r.GetStringSafe("Address_AddressLine2"),
                AddressStreet = r.GetStringSafe("Address_Street"),
                AddressPostalCode = r.GetStringSafe("Address_PostalCode"),
                AddressCity = r.GetStringSafe("Address_City"),
                AddressEmail = r.GetStringSafe("Address_Email"),
                AddressLandLine = r.GetStringSafe("Address_LandLine"),
                AddressMobileLine = r.GetStringSafe("Address_MobileLine"),
                PassportImageId = r.GetGuidSafe("PassportImageId"),
                SignatureImageId = r.GetGuidSafe("SignatureImageId"),
                IdentityCardFrontSideImageId = r.GetGuidSafe("IdentityCardFrontSideImageId"),
                IdentityCardBackSideImageId = r.GetGuidSafe("IdentityCardBackSideImageId"),
                BiometricFingerprintImageId = r.GetGuidSafe("BiometricFingerprintImageId"),
                BiometricFingerprintTemplateId = r.GetGuidSafe("BiometricFingerprintTemplateId"),
                BiometricFingerprintTemplateFormat = r.GetByteSafe("BiometricFingerprintTemplateFormat"),
                BiometricFingerVeinTemplateId = r.GetGuidSafe("BiometricFingerVeinTemplateId"),
                BiometricFingerVeinTemplateFormat = r.GetByteSafe("BiometricFingerVeinTemplateFormat"),
                RegistrationDate = r.GetDateOrNull("RegistrationDate"),
                Reference1 = r.GetStringSafe("Reference1"),
                Reference2 = r.GetStringSafe("Reference2"),
                Reference3 = r.GetStringSafe("Reference3"),
                Remarks = r.GetStringSafe("Remarks"),
                IsDefaulter = r.GetBoolSafe("IsDefaulter"),
                IsLocked = r.GetBoolSafe("IsLocked"),
                InhibitGuaranteeing = r.GetBoolSafe("InhibitGuaranteeing"),
                RecruitedBy = r.GetStringSafe("RecruitedBy"),
                RecordStatus = r.GetByteSafe("RecordStatus"),
                ModifiedBy = r.GetStringSafe("ModifiedBy"),
                ModifiedDate = r.GetDateOrNull("ModifiedDate"),
                AdministrativeDivisionId = r.GetGuidSafe("AdministrativeDivisionId"),
                CreatedBy = r.GetStringSafe("CreatedBy"),
                CreatedDate = r.GetDateOrNull("CreatedDate") ?? DateTime.UtcNow
            };
        }
    }
    public static class SqlReaderExtensions
    {
        public static Guid GetGuidSafe(this SqlDataReader r, string col) => r.HasColumn(col) && r[col] != DBNull.Value ? (Guid)r[col] : Guid.Empty;
        public static byte GetByteSafe(this SqlDataReader r, string col) => r.HasColumn(col) && r[col] != DBNull.Value ? Convert.ToByte(r[col]) : (byte)0;
        public static int GetInt32Safe(this SqlDataReader r, string col) => r.HasColumn(col) && r[col] != DBNull.Value ? Convert.ToInt32(r[col]) : 0;
        public static bool GetBoolSafe(this SqlDataReader r, string col) => r.HasColumn(col) && r[col] != DBNull.Value && Convert.ToBoolean(r[col]);

        public static bool HasColumn(this SqlDataReader r, string columnName)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (r.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            return false;
        }
        public static string GetStringSafe(this SqlDataReader r, string col)
            => r[col] == DBNull.Value ? "" : r[col].ToString();

        public static Guid GetGuid(this SqlDataReader r, string col)
            => (Guid)r[col];

        public static byte GetByte(this SqlDataReader r, string col)
            => Convert.ToByte(r[col]);

        public static int GetInt32(this SqlDataReader r, string col)
            => Convert.ToInt32(r[col]);

        public static bool GetBool(this SqlDataReader r, string col)
            => Convert.ToBoolean(r[col]);

        public static double GetDouble(this SqlDataReader r, string col)
            => Convert.ToDouble(r[col]);

        public static DateTime GetDate(this SqlDataReader r, string col)
            => Convert.ToDateTime(r[col]);

        public static DateTime? GetDateOrNull(this SqlDataReader r, string col)
            => r[col] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r[col]);
    }

}
