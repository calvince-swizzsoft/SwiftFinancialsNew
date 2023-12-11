using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.ComponentModel.DataAnnotations;

namespace Application.MainBoundedContext.DTO.FrontOfficeModule
{
    public class CashTransferRequestDTO
    {
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
        public byte Status { get; set; }

        [Display(Name = "Status")]
        public string StatusDescription
        {
            get
            {
                return Enum.IsDefined(typeof(CashTransferRequestStatus), (int)Status) ? EnumHelper.GetDescription((CashTransferRequestStatus)Status) : string.Empty;
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
    }
}
