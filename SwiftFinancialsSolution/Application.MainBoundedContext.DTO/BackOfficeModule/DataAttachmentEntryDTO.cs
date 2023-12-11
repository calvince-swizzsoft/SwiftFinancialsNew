
using Infrastructure.Crosscutting.Framework.Attributes;
using Application.Seedwork;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Application.MainBoundedContext.DTO.BackOfficeModule
{
    public class DataAttachmentEntryDTO : BindingModelBase<DataAttachmentEntryDTO>
    {
        public DataAttachmentEntryDTO()
        {
            AddAllAttributeValidators();
        }

        [DataMember]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember]
        [Display(Name = "Data Attachment Period")]
        [ValidGuid]
        public Guid DataAttachmentPeriodId { get; set; }

        [DataMember]
        [Display(Name = "Customer Account")]
        [ValidGuid]
        public Guid CustomerAccountId { get; set; }

        [DataMember]
        [Display(Name = "Customer")]
        public Guid CustomerAccountCustomerId { get; set; }

        [DataMember]
        [Display(Name = "Employer")]
        public Guid CustomerAccountCustomerStationZoneDivisionEmployerId { get; set; }

        [DataMember]
        [Display(Name = "Employer")]
        public string CustomerAccountCustomerStationZoneDivisionEmployerDescription { get; set; }

        [DataMember]
        [Display(Name = "Customer Type")]
        public int CustomerAccountCustomerType { get; set; }

        [DataMember]
        [Display(Name = "Customer Type")]
        public string CustomerAccountCustomerTypeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(CustomerType), CustomerAccountCustomerType) ? EnumHelper.GetDescription((CustomerType)CustomerAccountCustomerType) : string.Empty;
            }
        }

        [DataMember]
        [Display(Name = "Branch")]
        public Guid CustomerAccountBranchId { get; set; }

        [DataMember]
        [Display(Name = "Branch Code")]
        public int CustomerAccountBranchCode { get; set; }

        [DataMember]
        [Display(Name = "Customer Account Product")]
        public Guid CustomerAccountCustomerAccountTypeTargetProductId { get; set; }

        [DataMember]
        [Display(Name = "Product Code")]
        public int CustomerAccountCustomerAccountTypeTargetProductCode { get; set; }

        [DataMember]
        [Display(Name = "Customer Account Product Code")]
        public int CustomerAccountCustomerAccountTypeProductCode { get; set; }

        [DataMember]
        [Display(Name = "Customer Account Remarks")]
        public string CustomerAccountRemarks { get; set; }

        [DataMember]
        [Display(Name = "Customer Account Is Locked?")]
        public bool CustomerAccountIsLocked { get; set; }

        [DataMember]
        [Display(Name = "Serial Number")]
        public int CustomerAccountCustomerSerialNumber { get; set; }

        [DataMember]
        [Display(Name = "Payroll Numbers")]
        public string CustomerAccountCustomerIndividualPayrollNumbers { get; set; }

        [DataMember]
        [Display(Name = "Full Account Number")]
        public string FullAccountNumber
        {
            get
            {
                return string.Format("{0}-{1}-{2}-{3}",
                            CustomerAccountBranchCode.ToString().PadLeft(3, '0'),
                            CustomerAccountCustomerSerialNumber.ToString().PadLeft(7, '0'),
                            CustomerAccountCustomerAccountTypeProductCode.ToString().PadLeft(3, '0'),
                            CustomerAccountCustomerAccountTypeTargetProductCode.ToString().PadLeft(3, '0'));
            }
        }

        [DataMember]
        [Display(Name = "Salutation")]
        public int CustomerAccountCustomerIndividualSalutation { get; set; }

        [DataMember]
        [Display(Name = "Salutation")]
        public string CustomerAccountCustomerIndividualSalutationDescription
        {
            get
            {
                return Enum.IsDefined(typeof(Salutation), CustomerAccountCustomerIndividualSalutation) ? EnumHelper.GetDescription((Salutation)CustomerAccountCustomerIndividualSalutation) : string.Empty;
            }
        }

        [DataMember]
        [Display(Name = "First Name")]
        public string CustomerAccountCustomerIndividualFirstName { get; set; }

        [DataMember]
        [Display(Name = "Other Names")]
        public string CustomerAccountCustomerIndividualLastName { get; set; }

        [DataMember]
        [Display(Name = "Group Name")]
        public string CustomerAccountCustomerNonIndividualDescription { get; set; }

        [DataMember]
        [Display(Name = "Registration Number")]
        public string CustomerAccountCustomerNonIndividualRegistrationNumber { get; set; }

        [DataMember]
        [Display(Name = "Personal Identification Number")]
        public string CustomerAccountCustomerPersonalIdentificationNumber { get; set; }

        [DataMember]
        [Display(Name = "Date Established")]
        public DateTime? CustomerAccountCustomerNonIndividualDateEstablished { get; set; }

        [DataMember]
        [Display(Name = "Customer Name")]
        public string CustomerFullName
        {
            get
            {
                var result = string.Empty;

                switch ((CustomerType)CustomerAccountCustomerType)
                {
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Individual:
                        result = string.Format("{0} {1} {2}", CustomerAccountCustomerIndividualSalutationDescription, CustomerAccountCustomerIndividualFirstName, CustomerAccountCustomerIndividualLastName).Trim();
                        break;
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Partnership:
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Corporation:
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.MicroCredit:
                        result = CustomerAccountCustomerNonIndividualDescription;
                        break;
                    default:
                        break;
                }

                return result;
            }
        }

        [DataMember]
        [Display(Name = "Product Name")]
        public string ProductDescription { get; set; }

        [DataMember]
        [Display(Name = "Product G/L Account")]
        public Guid ProductChartOfAccountId { get; set; }

        [DataMember]
        [Display(Name = "Product G/L Account Code")]
        public int ProductChartOfAccountCode { get; set; }

        [DataMember]
        [Display(Name = "Product G/L Account Name")]
        public string ProductChartOfAccountName { get; set; }

        [DataMember]
        [Display(Name = "Transaction Type")]
        public int TransactionType { get; set; }

        [DataMember]
        [Display(Name = "Transaction Type")]
        public string TransactionTypeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(DataAttachmentTransactionType), TransactionType) ? string.Format("{0}", EnumHelper.GetDescription((DataAttachmentTransactionType)TransactionType)) : string.Empty;
            }
        }

        [DataMember]
        [Display(Name = "Sequence Number")]
        public int SequenceNumber { get; set; }

        [DataMember]
        [Display(Name = "New Amount")]
        public decimal NewAmount { get; set; }

        [DataMember]
        [Display(Name = "Current Amount")]
        public decimal CurrentAmount { get; set; }

        [DataMember]
        [Display(Name = "New Balance")]
        public decimal NewBalance { get; set; }

        [DataMember]
        [Display(Name = "Current Balance")]
        public decimal CurrentBalance { get; set; }

        [DataMember]
        [Display(Name = "New Ability")]
        public decimal NewAbility { get; set; }

        [DataMember]
        [Display(Name = "Current Ability")]
        public decimal CurrentAbility { get; set; }

        [DataMember]
        [Display(Name = "Remarks")]
        public string Remarks { get; set; }

        [DataMember]
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [DataMember]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }
    }
}
