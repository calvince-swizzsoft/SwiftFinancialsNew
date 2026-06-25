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
    public class ImprestLineDTO
    {

        [DataMember]
        [Display(Name = "ExpenseCategory")]
        public string ExpenseCategory { get; set; }

        [DataMember]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember]
        [ValidGuid]
        [Display(Name = "Imprest Id")]
        public Guid ImprestId { get; set; }

        [DataMember]
        [Display(Name = "Line No")]
        public int LineNo { get; set; }

        [DataMember]
        [ValidGuid]
        [Display(Name = "Expense Account Id")]
        public Guid ExpenseChartOfAccountId { get; set; }

        [DataMember]
        [Display(Name = "Expense Account")]
        public string ExpenseChartOfAccountName { get; set; }

        [DataMember]
        [Display(Name = "Description")]
        public string Description { get; set; }


        [DataMember]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }
    }
}
