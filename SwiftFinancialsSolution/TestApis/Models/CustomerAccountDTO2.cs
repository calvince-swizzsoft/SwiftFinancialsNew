using Application.Seedwork;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace TestApis.Models
{
    public class CustomerAccountDTO2 : BindingModelBase<CustomerAccountDTO2>
    {
        public CustomerAccountDTO2()
        {
            AddAllAttributeValidators();
            ProductCodes = new List<int>();
            InvestmentProductIds = new List<Guid>();
            SavingsProductIds = new List<Guid>();
        }

        [DataMember] public Guid Id { get; set; }
        [DataMember] public Guid CustomerId { get; set; }
        [DataMember] public Guid CustomerStationZoneDivisionEmployerId { get; set; }
        [DataMember] public string CustomerStationZoneDivisionEmployerDescription { get; set; }

        [DataMember] public byte CustomerType { get; set; }
        [DataMember] public string CustomerTypeDescription => EnumHelper.GetDescription((CustomerType)CustomerType);

        [DataMember] public byte CustomerIndividualSalutation { get; set; }
        [DataMember] public string CustomerIndividualSalutationDescription => EnumHelper.GetDescription((Salutation)CustomerIndividualSalutation);

        [DataMember] public byte CustomerIndividualGender { get; set; }
        [DataMember] public string CustomerIndividualGenderDescription => EnumHelper.GetDescription((Gender)CustomerIndividualGender);

        [DataMember] public byte CustomerIndividualMaritalStatus { get; set; }
        [DataMember] public string CustomerIndividualMaritalStatusDescription => EnumHelper.GetDescription((MaritalStatus)CustomerIndividualMaritalStatus);

        [DataMember] public byte CustomerIndividualIdentityCardType { get; set; }
        [DataMember] public string CustomerIndividualIdentityCardTypeDescription => EnumHelper.GetDescription((IdentityCardType)CustomerIndividualIdentityCardType);
        [DataMember] public string CustomerIndividualIdentityCardNumber { get; set; }

        [DataMember] public byte CustomerIndividualNationality { get; set; }
        [DataMember] public string CustomerIndividualNationalityDescription => EnumHelper.GetDescription((Nationality)CustomerIndividualNationality);

        [DataMember] public byte? CustomerIndividualEmploymentTermsOfService { get; set; }
        [DataMember]
        public string CustomerIndividualEmploymentTermsOfServiceDescription =>
            CustomerIndividualEmploymentTermsOfService.HasValue
                ? EnumHelper.GetDescription((TermsOfService)CustomerIndividualEmploymentTermsOfService.Value)
                : string.Empty;

        [DataMember] public int CustomerSerialNumber { get; set; }
        [DataMember] public string PaddedCustomerSerialNumber => CustomerSerialNumber.ToString().PadLeft(7, '0');
        [DataMember] public string CustomerIndividualPayrollNumbers { get; set; }
        [DataMember] public string CustomerIndividualFirstName { get; set; }
        [DataMember] public string CustomerIndividualLastName { get; set; }
        [DataMember] public byte CustomerIndividualType { get; set; }
        [DataMember] public string CustomerIndividualTypeDescription => EnumHelper.GetDescription((IndividualType)CustomerIndividualType);
        [DataMember] public byte CustomerIndividualClassification { get; set; }
        [DataMember] public string CustomerIndividualClassificationDescription => EnumHelper.GetDescription((CustomerClassification)CustomerIndividualClassification);

        [DataMember] public string CustomerNonIndividualDescription { get; set; }
        [DataMember] public string CustomerNonIndividualRegistrationNumber { get; set; }
        [DataMember] public string CustomerPersonalIdentificationNumber { get; set; }
        [DataMember] public DateTime? CustomerNonIndividualDateEstablished { get; set; }
        [DataMember] public string CustomerFullName { get; set; }
        [DataMember] public string CustomerIdentificationNumber { get; set; }

        [DataMember] public string CustomerAddressMobileLine { get; set; }
        [DataMember] public string CustomerAddressEmail { get; set; }
        [DataMember] public string CustomerReference1 { get; set; }
        [DataMember] public string CustomerReference2 { get; set; }
        [DataMember] public string CustomerReference3 { get; set; }
        [DataMember] public bool CustomerIsDefaulter { get; set; }

        [DataMember] public Guid BranchId { get; set; }
        [DataMember] public int BranchCode { get; set; }
        [DataMember] public string BranchDescription { get; set; }
        [DataMember] public string BranchAddressCity { get; set; }
        [DataMember] public string BranchAddressStreet { get; set; }
        [DataMember] public string BranchAddressEmail { get; set; }
        [DataMember] public string BranchAddressLandLine { get; set; }
        [DataMember] public string BranchAddressMobileLine { get; set; }

        [DataMember] public Guid BranchCompanyId { get; set; }
        [DataMember] public string BranchCompanyDescription { get; set; }
        [DataMember] public string BranchCompanyAddressCity { get; set; }
        [DataMember] public string BranchCompanyAddressStreet { get; set; }
        [DataMember] public string BranchCompanyAddressEmail { get; set; }
        [DataMember] public string BranchCompanyAddressLandLine { get; set; }
        [DataMember] public string BranchCompanyAddressMobileLine { get; set; }
        [DataMember] public string BranchCompanyRecoveryPriority { get; set; }
        [DataMember] public bool BranchCompanyEnforceInvestmentProductExemptions { get; set; }

        // Product info - only IDs and codes
        [DataMember] public int CustomerAccountTypeProductCode { get; set; }
        [DataMember]
        public string CustomerAccountTypeProductCodeDescription =>
            Enum.IsDefined(typeof(ProductCode), CustomerAccountTypeProductCode)
                ? EnumHelper.GetDescription((ProductCode)CustomerAccountTypeProductCode)
                : string.Empty;

        [DataMember] public Guid CustomerAccountTypeTargetProductId { get; set; }
        [DataMember] public int CustomerAccountTypeTargetProductCode { get; set; }
        [DataMember] public string CustomerAccountTypeTargetProductDescription { get; set; }
        [DataMember] public int? CustomerAccountTypeTargetProductLoanProductSection { get; set; }
        [DataMember]
        public string CustomerAccountTypeTargetProductLoanProductSectionDescription =>
            CustomerAccountTypeTargetProductLoanProductSection.HasValue
                ? Enum.IsDefined(typeof(LoanProductSection), CustomerAccountTypeTargetProductLoanProductSection.Value)
                    ? EnumHelper.GetDescription((LoanProductSection)CustomerAccountTypeTargetProductLoanProductSection.Value)
                    : string.Empty
                : string.Empty;

        [DataMember]
        public string FullAccountNumber =>
            $"{BranchCode.ToString().PadLeft(3, '0')}-{CustomerSerialNumber.ToString().PadLeft(7, '0')}-{CustomerAccountTypeProductCode.ToString().PadLeft(3, '0')}-{CustomerAccountTypeTargetProductCode.ToString().PadLeft(3, '0')}";

        // Balances
        [DataMember] public decimal BookBalance { get; set; }
        [DataMember] public decimal AvailableBalance { get; set; }
        [DataMember] public decimal PrincipalBalance { get; set; }
        [DataMember] public decimal InterestBalance { get; set; }
        [DataMember] public decimal CarryForwardsBalance { get; set; }
        [DataMember] public decimal PrincipalArrearagesBalance { get; set; }
        [DataMember] public decimal InterestArrearagesBalance { get; set; }

        // Flattened lists for products
        [DataMember] public List<int> ProductCodes { get; set; }
        [DataMember] public List<Guid> InvestmentProductIds { get; set; }
        [DataMember] public List<Guid> SavingsProductIds { get; set; }

        [DataMember] public string ErrorMessageResult { get; set; }
        [DataMember] public decimal NewAvailableBalance { get; set; }

        // Additional info
        [DataMember] public double PaymentPerPeriod { get; set; }
        [DataMember] public double NumberOfPeriods { get; set; }
        [DataMember] public string Reference { get; set; }
    }
}
