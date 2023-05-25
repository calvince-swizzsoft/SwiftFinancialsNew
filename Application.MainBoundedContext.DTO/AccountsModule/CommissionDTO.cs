using Application.Seedwork;
using Infrastructure.Crosscutting.Framework.Attributes;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Application.MainBoundedContext.DTO.AccountsModule
{
    public class CommissionDTO : BindingModelBase<CommissionDTO>
    {
        public CommissionDTO()
        {
            AddAllAttributeValidators();
        }

        [DataMember]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember]
        [Display(Name = "Name")]
        [Required]
        public string Description { get; set; }

        [DataMember]
        [Display(Name = "Maximum Charge")]
        public decimal MaximumCharge { get; set; }

        [DataMember]
        [Display(Name = "Rounding Type")]
        public int RoundingType { get; set; }

        [DataMember]
        [Display(Name = "Rounding Type")]
        public string RoundingTypeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(RoundingType), RoundingType) ? EnumHelper.GetDescription((RoundingType)RoundingType) : string.Empty;
            }
        }

        [DataMember]
        [Display(Name = "Is Locked?")]
        public bool IsLocked { get; set; }

        [DataMember]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [DataMember]
        [Display(Name = "Commission")]
        public Guid CommissionId { get; set; }

        [DataMember]
        [Display(Name = "Commission Name")]
        public string CommissionDescription { get; set; }

        [DataMember]
        [Display(Name = "G/L Account")]
        [ValidGuid]
        public Guid ChartOfAccountId { get; set; }
        [DataMember]
        [Display(Name = "G/L Account Type")]
        public int ChartOfAccountAccountType { get; set; }

        [DataMember]
        [Display(Name = "G/L Account Name")]
        public string ChartOfAccountAccountName { get; set; }

        [DataMember]
        [Display(Name = "Commission Splits Total Percentage")]
        [CustomValidation(typeof(CommissionDTO), "CheckCommissionSplitsTotalPercentage", ErrorMessage = "The sum of commission split percentage entries must be equal to 100%!")]
        public double CommissionSplitsTotalPercentage { get; set; }

        [DataMember]
        [Display(Name = "Leviable?")]
        public bool Leviable { get; set; }

        [DataMember]
        [Display(Name = "Levy")]
        [ValidGuid]
        public Guid LevyId { get; set; }

        [DataMember]
        [Display(Name = "Levy")]
        public LevyDTO Levy { get; set; }

        [DataMember]
        [Display(Name = "Charge Benefactor")]
        public int ChargeBenefactor { get; set; }

        [DataMember]
        [Display(Name = "Charge Benefactor")]
        public string ChargeBenefactorDescription
        {
            get
            {
                return Enum.IsDefined(typeof(ChargeBenefactor), ChargeBenefactor) ? EnumHelper.GetDescription((ChargeBenefactor)ChargeBenefactor) : string.Empty;
            }
        }

        [DataMember]
        [Display(Name = "Charge Basis Value")]
        public int ChargeBasisValue { get; set; }

        [DataMember]
        [Display(Name = "Charge Basis Value")]
        public string ChargeBasisValueDescription
        {
            get
            {
                return Enum.IsDefined(typeof(LoanProductChargeBasisValue), ChargeBasisValue) ? EnumHelper.GetDescription((LoanProductChargeBasisValue)ChargeBasisValue) : string.Empty;
            }
        }

        [DataMember]
        [Display(Name = "System Transaction Type")]
        public int SystemTransactionType { get; set; }

        [DataMember]
        [Display(Name = "Complement Type")]
        public int ComplementType { get; set; }

        [DataMember]
        [Display(Name = "Complement Type")]
        public string ComplementTypeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(ChargeType), ComplementType) ? EnumHelper.GetDescription((ChargeType)ComplementType) : string.Empty;
            }
        }

        [DataMember]
        [Display(Name = "Complement Percentage")]
        public double ComplementPercentage { get; set; }

        [DataMember]
        [Display(Name = "Complement Fixed Amount")]
        public decimal ComplementFixedAmount { get; set; }

        [DataMember]
        [Display(Name = "Name")]
        public string ExtendedChargeBenefactorDescription
        {
            get
            {
                return string.Format("{0} (borne by {1})", Description, ChargeBenefactorDescription);
            }
        }

        [DataMember]
        [Display(Name = "Name")]
        public string ExtendedChargeBasisValueDescription
        {
            get
            {
                return string.Format("{0} (basis value is {1})", Description, ChargeBasisValueDescription);
            }
        }

        public static ValidationResult CheckCommissionSplitsTotalPercentage(object value, ValidationContext context)
        {
            var bindingModel = context.ObjectInstance as CommissionDTO;
            if (bindingModel == null)
                throw new NotSupportedException("ObjectInstance must be CommissionDTO");

            if ((double)value != 100d)
                return new ValidationResult(string.Format("The sum of commission split percentage entries ({0}) must be equal to 100%!", value));

            return ValidationResult.Success;
        }
        public IList<LevyDTO> Levies { get; set; }

        public IList<LevySplitDTO> LevySplits { get; set; }

        public CommissionSplitDTO CommissionSplit { get; set; }

        public CommissionLevyDTO CommissionLevy { get; set; }

        public IList<CommissionLevyDTO> CommissionLevies { get; set; }

        public IList<CommissionSplitDTO> CommissionSplits { get; set; }
    }
}
