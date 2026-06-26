using Application.Seedwork;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Application.MainBoundedContext.DTO.FrontOfficeModule
{
    public class CashTransferRequestDTO : BindingModelBase<CashTransferRequestDTO>
    {

        public CashTransferRequestDTO()
        {
            AddAllAttributeValidators();
        }

        [Display(Name = "Id")]
        public Guid Id { get; set; } 

        [Display(Name = "Employee")]
        public Guid? EmployeeId { get; set; }

        [Display(Name = "Customer")]
        public Guid EmployeeCustomerId { get; set; }

        [Display(Name = "Salutation")]
        public byte EmployeeCustomerIndividualSalutation { get; set; }

        [Display(Name = "Salutation")]
        public string EmployeeCustomerIndividualSalutationDescription
        {
            get
            {
                return Enum.IsDefined(typeof(Salutation), (int)EmployeeCustomerIndividualSalutation) ? EnumHelper.GetDescription((Salutation)EmployeeCustomerIndividualSalutation) : string.Empty;
            }
        }

        [Display(Name = "First Name")]
        public string EmployeeCustomerIndividualFirstName { get; set; }

        [Display(Name = "Other Names")]
        public string EmployeeCustomerIndividualLastName { get; set; }

        [Display(Name = "Employee Name")]
        public string EmployeeCustomerFullName
        {
            get
            {
                return string.Format("{0} {1} {2}", EmployeeCustomerIndividualSalutationDescription, EmployeeCustomerIndividualFirstName, EmployeeCustomerIndividualLastName);
            }
        }

        [Display(Name = "Status")]
        public int Status { get; set; }

        [Display(Name = "Status")]
        public string StatusDescription
        {
            get
            {
                return Enum.IsDefined(typeof(CashTransferRequestStatus), Status) ? EnumHelper.GetDescription((CashTransferRequestStatus)Status) : string.Empty;
            }
        }

  
        [Display(Name = "Utilized")]
        public bool Utilized { get; set; }

        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Display(Name = "Reference")]
        public string Reference { get; set; }

        [Display(Name = "Remarks")]
        public string Remarks { get; set; }

        [Display(Name = "Acknowledged By")]
        public string AcknowledgedBy { get; set; }

        [Display(Name = "Acknowledged Date")]
        public DateTime? AcknowledgedDate { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "From")]
        public string CreatedBy { get; set; }

        //[DataMember]
        //[Display(Name = "TransactionType")]
        //public int TransactionType { get; set; }

        //[DataMember]
        //[Display(Name = "TransactionType")]
        //public string TransactionTypeDescription
        //{
        //    get
        //    {
        //        return Enum.IsDefined(typeof(TreasuryTransactionType), TransactionType) ? EnumHelper.GetDescription((TreasuryTransactionType)TransactionType) : string.Empty;
        //    }
        //}

        [DataMember]
        [Display(Name = "Cash Balance Status")]
        public int TellerCashBalanceStatusValue { get; set; } // Changed the name to avoid conflict

        [DataMember]
        [Display(Name = "Cash Balance Status")]
        public string TellerCashBalanceStatus
        {
            get
            {
                return Enum.IsDefined(typeof(TellerCashBalanceStatus), TellerCashBalanceStatusValue)
                    ? EnumHelper.GetDescription((TellerCashBalanceStatus)TellerCashBalanceStatusValue)
                    : string.Empty;
            }
        }


        [Display(Name = "Opening Balance")]
        public decimal OpeningBalance { get; set; }

        [Display(Name = "Closing Balance")]
        public decimal ClosingBalance { get; set; }

        [Display(Name = "Total Payments")]
        public decimal TotalDebits { get; set; }

        [Display(Name = "Total Receipts")]
        public decimal TotalCredits { get; set; }

        [Display(Name = "Expected Cash")]
        public decimal BookBalance { get; set; }

        [Display(Name = "Cheques Pensing Transfer")]
        public decimal UntransferredChequesValue { get; set; }

        [DataMember]
        [Display(Name = "Closing Balance Status")]
        public string ClosingBalanceStatus { get; set; }


    }
}
