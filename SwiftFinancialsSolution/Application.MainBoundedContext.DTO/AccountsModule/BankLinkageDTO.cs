using Application.Seedwork;
using Infrastructure.Crosscutting.Framework.Attributes;
using Infrastructure.Crosscutting.Framework.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Application.MainBoundedContext.DTO.AccountsModule
{
    public class BankLinkageDTO : BindingModelBase<BankLinkageDTO>
    {
        public BankLinkageDTO()
        {
            AddAllAttributeValidators();
        }

        [DataMember]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember]
        [Display(Name = "Bank")]
        [Required]
        public string BankName { get; set; }

        [DataMember]
        [Display(Name = "Bank Branch")]
        [Required]
        public string BankBranchName { get; set; }

        [DataMember]
        [Display(Name = "Bank Account Number")]
        [Required]
        public string BankAccountNumber { get; set; }

        [DataMember]
        [Display(Name = "Remarks")]
        public string Remarks { get; set; }

        [DataMember]
        [Display(Name = "Is Locked?")]
        public bool IsLocked { get; set; }

        [DataMember]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [DataMember]
        [Display(Name = "Branch")]
        [ValidGuid]
        public Guid BranchId { get; set; }

        [DataMember]
        [Display(Name = "Branch Name")]
        public string BranchDescription { get; set; }

        [DataMember]
        [Display(Name = "G/L Account")]
        [ValidGuid]
        public Guid ChartOfAccountId { get; set; }

        [DataMember]
        [Display(Name = "G/L Account Type")]
        public int ChartOfAccountAccountType { get; set; }

        [DataMember]
        [Display(Name = "G/L Account Code")]
        public int ChartOfAccountAccountCode { get; set; }

        [DataMember]
        [Display(Name = "G/L Account Name")]
        public string ChartOfAccountAccountName { get; set; }

        [DataMember]
        [Display(Name = "G/L Account Name")]
        public string ChartOfAccountName
        {
            get
            {
                return string.Format("{0}-{1} {2}", ChartOfAccountAccountType.FirstDigit(), ChartOfAccountAccountCode, ChartOfAccountAccountName);
            }
        }

        [DataMember]
        [Display(Name = "G/L Account Cost Center")]
        public Guid? ChartOfAccountCostCenterId { get; set; }

        [DataMember]
        [Display(Name = "G/L Account Cost Center")]
        public string ChartOfAccountCostCenterDescription { get; set; }
    }
}
