using Application.Seedwork;
using Infrastructure.Crosscutting.Framework.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SwiftFinancials.Presentation.Shared.Application.MainBoundedContext.DTO.AccountsModule
{
    public class ImprestDTO : BindingModelBase<ImprestDTO>
    {

        public ImprestDTO()
        {
            AddAllAttributeValidators();
        }


        [Display(Name = "BankId")]
        public Guid BankId { get; set; }

        [DataMember]
            [Display(Name = "BranchId")]
            public Guid BranchId { get; set; }

            [DataMember]
            [Display(Name = "BankBranchName")]
            public string BankBranchName { get; set; }


            [DataMember]
            [Display(Name = "Posted")]
            public Boolean Posted { get; set; }

            [DataMember]
            [Display(Name = "Id")]
            public Guid Id { get; set; }

            [DataMember]
            [Display(Name = "Imprest No")]
            public string ImprestNo { get; set; }

            [DataMember]
            [Display(Name = "Employee No")]
            public string EmployeeNo { get; set; }

            [DataMember]
            [ValidGuid]
            [Display(Name = "Employee Id")]
            public Guid EmployeeId { get; set; }

            [DataMember]
            [Display(Name = "Employee Name")]
            public string EmployeeName { get; set; }

            [DataMember]
            [Display(Name = "Request Date")]
            public DateTime RequestDate { get; set; }

            [DataMember]
            [Display(Name = "Purpose")]
            public string Purpose { get; set; }

            [DataMember]
            [Display(Name = "Status")]
            public string Status { get; set; }

            [DataMember]
            [Display(Name = "Amount Requested")]
            public decimal AmountRequested { get; set; }

            [DataMember]
            [Display(Name = "Amount Approved")]
            public decimal AmountApproved { get; set; }

            [DataMember]
            [Display(Name = "Paying Bank Account")]
            public Guid BankChartOfAccountId { get; set; }

            [DataMember]
            [Display(Name = "Imprest Lines")]
            public HashSet<ImprestLineDTO> ImprestLines { get; set; }

            [DataMember]
            [Display(Name = "Remarks")]
            public string Remarks { get; set; }
        }
    }
