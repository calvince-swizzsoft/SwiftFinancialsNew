using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TestApis.Models
{
    public class CustomerDTO2
    {
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [Display(Name = "Branch")]
        public Guid BranchId { get; set; }

        [Display(Name = "Branch")]
        public string BranchDescription { get; set; } = string.Empty;

        [Display(Name = "Branch E-mail")]
        public string BranchAddressEmail { get; set; } = string.Empty;

        [Display(Name = "Company")]
        public string BranchCompanyDescription { get; set; } = string.Empty;

        [Display(Name = "Type")]
        public byte Type { get; set; }

        [Display(Name = "Type")]
        public string TypeDescription => string.Empty;

        [Display(Name = "Serial Number")]
        public int SerialNumber { get; set; }

        [Display(Name = "Serial Number")]
        public string PaddedSerialNumber => SerialNumber.ToString().PadLeft(7, '0');

        [Display(Name = "Personal Identification Number")]
        public string PersonalIdentificationNumber { get; set; } = string.Empty;

        [Display(Name = "Individual Type")]
        public byte IndividualType { get; set; }

        [Display(Name = "Individual Type")]
        public string IndividualTypeDescription => string.Empty;

        [Display(Name = "First Name")]
        public string IndividualFirstName { get; set; } = string.Empty;

        [Display(Name = "Other Names")]
        public string IndividualLastName { get; set; } = string.Empty;

        [Display(Name = "Identity Card Type")]
        public byte IndividualIdentityCardType { get; set; }

        [Display(Name = "Identity Card Type")]
        public string IndividualIdentityCardTypeDescription => string.Empty;

        [Display(Name = "Identity Card Number")]
        public string IndividualIdentityCardNumber { get; set; } = string.Empty;

        [Display(Name = "Identity Card Serial #")]
        public string IndividualIdentityCardSerialNumber { get; set; } = string.Empty;

        [Display(Name = "Payroll Numbers")]
        public string IndividualPayrollNumbers { get; set; } = string.Empty;

        [Display(Name = "Salutation")]
        public byte IndividualSalutation { get; set; }

        [Display(Name = "Salutation")]
        public string IndividualSalutationDescription => string.Empty;

        [Display(Name = "Gender")]
        public byte IndividualGender { get; set; }

        [Display(Name = "Gender")]
        public string IndividualGenderDescription => string.Empty;

        [Display(Name = "Marital Status")]
        public byte IndividualMaritalStatus { get; set; }

        [Display(Name = "Marital Status")]
        public string IndividualMaritalStatusDescription => string.Empty;

        [Display(Name = "Nationality")]
        public byte IndividualNationality { get; set; }

        [Display(Name = "Nationality")]
        public string IndividualNationalityDescription => string.Empty;

        [Display(Name = "Birth Date")]
        public DateTime? IndividualBirthDate { get; set; }

        [Display(Name = "Start Date")]
        public DateTime DurationStartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime DurationEndDate { get; set; }

        [Display(Name = "Employment Designation")]
        public string IndividualEmploymentDesignation { get; set; } = string.Empty;

        [Display(Name = "Employment Terms-Of-Service")]
        public byte? IndividualEmploymentTermsOfService { get; set; }

        [Display(Name = "Employment Terms-Of-Service")]
        public string IndividualEmploymentTermsOfServiceDescription => string.Empty;

        [Display(Name = "Employment Date")]
        public DateTime? IndividualEmploymentDate { get; set; }

        [Display(Name = "Classification")]
        public byte IndividualClassification { get; set; }

        [Display(Name = "Classification")]
        public string IndividualClassificationDescription => string.Empty;

        [Display(Name = "Group Name")]
        public string NonIndividualDescription { get; set; } = string.Empty;

        [Display(Name = "Registration Number")]
        public string NonIndividualRegistrationNumber { get; set; } = string.Empty;

        [Display(Name = "Registration Serial #")]
        public string NonIndividualRegistrationSerialNumber { get; set; } = string.Empty;

        [Display(Name = "Date Established")]
        public DateTime? NonIndividualDateEstablished { get; set; }

        [Display(Name = "Address Line 1")]
        public string AddressAddressLine1 { get; set; } = string.Empty;

        [Display(Name = "Address Line 2")]
        public string AddressAddressLine2 { get; set; } = string.Empty;

        [Display(Name = "Street")]
        public string AddressStreet { get; set; } = string.Empty;

        [Display(Name = "Postal Code")]
        public string AddressPostalCode { get; set; } = string.Empty;

        [Display(Name = "City")]
        public string AddressCity { get; set; } = string.Empty;

        [Display(Name = "E-mail")]
        public string AddressEmail { get; set; } = string.Empty;

        [Display(Name = "Land Line")]
        public string AddressLandLine { get; set; } = string.Empty;

        [Display(Name = "Mobile Line")]
        public string AddressMobileLine { get; set; } = string.Empty;

        [Display(Name = "Passport")]
        public byte[] PassportBuffer { get; set; }

        [Display(Name = "Signature")]
        public byte[] SignatureBuffer { get; set; }

        [Display(Name = "Identity Card (Front)")]
        public byte[] IdentityCardFrontSideBuffer { get; set; }

        [Display(Name = "Identity Card (Back)")]
        public byte[] IdentityCardBackSideBuffer { get; set; }

        [Display(Name = "Biometric Fingerprint Image")]
        public byte[] BiometricFingerprintBuffer { get; set; }

        [Display(Name = "Biometric Fingerprint Template")]
        public byte[] BiometricFingerprintTemplateBuffer { get; set; }

        [Display(Name = "Biometric Finger-Vein Template")]
        public byte[] BiometricFingerVeinTemplateBuffer { get; set; }

        public bool BiometricEnrollment { get; set; }

        [Display(Name = "Station")]
        public Guid? StationId { get; set; }

        [Display(Name = "Station")]
        public string StationDescription { get; set; } = string.Empty;

        [Display(Name = "Zone")]
        public Guid? StationZoneId { get; set; }

        [Display(Name = "Zone")]
        public string StationZoneDescription { get; set; } = string.Empty;

        [Display(Name = "Division")]
        public Guid? StationZoneDivisionId { get; set; }

        [Display(Name = "Division")]
        public string StationZoneDivisionDescription { get; set; } = string.Empty;

        [Display(Name = "Employer")]
        public Guid? StationZoneDivisionEmployerId { get; set; }

        [Display(Name = "Employer")]
        public string StationZoneDivisionEmployerDescription { get; set; } = string.Empty;

        [Display(Name = "Employer Retirement Age")]
        public byte? StationZoneDivisionEmployerRetirementAge { get; set; }

        [Display(Name = "Enforce retirement age?")]
        public bool? StationZoneDivisionEmployerEnforceRetirementAge { get; set; }

        [Display(Name = "Account Number")]
        public string Reference1 { get; set; } = string.Empty;

        [Display(Name = "Membership Number")]
        public string Reference2 { get; set; } = string.Empty;

        [Display(Name = "Personal File Number")]
        public string Reference3 { get; set; } = string.Empty;

        [Display(Name = "Remarks")]
        public string Remarks { get; set; } = string.Empty;

        [Display(Name = "Is Defaulter?")]
        public bool IsDefaulter { get; set; }

        [Display(Name = "Is Locked?")]
        public bool IsLocked { get; set; }

        [Display(Name = "Inhibit Guaranteeing?")]
        public bool InhibitGuaranteeing { get; set; }

        [Display(Name = "Passport Image")]
        public Guid? PassportImageId { get; set; }

        [Display(Name = "Signature Image")]
        public Guid? SignatureImageId { get; set; }

        [Display(Name = "Identity Card (Front) Image")]
        public Guid? IdentityCardFrontSideImageId { get; set; }

        [Display(Name = "Identity Card (Back) Image")]
        public Guid? IdentityCardBackSideImageId { get; set; }

        [Display(Name = "Biometric Fingerprint Image")]
        public Guid? BiometricFingerprintImageId { get; set; }

        [Display(Name = "Biometric Fingerprint Template")]
        public Guid? BiometricFingerprintTemplateId { get; set; }

        [Display(Name = "Biometric Fingerprint Template Format")]
        public byte BiometricFingerprintTemplateFormat { get; set; }

        [Display(Name = "Biometric Finger-Vein Template")]
        public Guid? BiometricFingerVeinTemplateId { get; set; }

        [Display(Name = "Biometric Finger-Vein Template Format")]
        public byte BiometricFingerVeinTemplateFormat { get; set; }

        [Display(Name = "Registration Date")]
        public DateTime? RegistrationDate { get; set; }

        [Display(Name = "Recruited By")]
        public string RecruitedBy { get; set; } = string.Empty;

        [Display(Name = "Record Status")]
        public byte RecordStatus { get; set; }

        [Display(Name = "Record Status")]
        public string RecordStatusDescription => string.Empty;

        [Display(Name = "Modified By")]
        public string ModifiedBy { get; set; } = string.Empty;

        [Display(Name = "Modified Date")]
        public DateTime? ModifiedDate { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; } = string.Empty;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Administrative Division")]
        public Guid? AdministrativeDivisionId { get; set; }

        [Display(Name = "Administrative Division")]
        public string AdministrativeDivisionDescription { get; set; } = string.Empty;

        [Display(Name = "Name")]
        public string FullName => string.Empty;

        [Display(Name = "Identification Number")]
        public string IdentificationNumber => string.Empty;

        [Display(Name = "Age")]
        public int Age => -1;

        [Display(Name = "Membership Period (Months)")]
        public int MembershipPeriod => -1;

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(string.Empty);
            stringBuilder.AppendLine(PaddedSerialNumber);
            stringBuilder.AppendLine(Reference1);
            return stringBuilder.ToString();
        }

        public string ErrorMessageResult { get; set; } = string.Empty;
        public string ErrorMessages { get; set; } = string.Empty;
       

        public string GuarantorDesc { get; set; } = string.Empty;
        public object ZoneDivisionEmployerDescription { get; set; }
        public bool HasErrors { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public List<CustomerDTO2> customerDTO2s { get; set; } 

        public List<MemberBankDetailDTO> BankDetails { get; set; } = new List<MemberBankDetailDTO>();
        
        public List<NextOfKinDTO2> NextOfKin { get; set; } = new List<NextOfKinDTO2>();

    }
}
