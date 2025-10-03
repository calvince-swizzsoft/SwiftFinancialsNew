using Application.Seedwork;
using Domain.MainBoundedContext.AccountsModule.Aggregates.PurchaseCreditMemoLineAgg;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Application.MainBoundedContext.DTO.AccountsModule
{
    public class SalesCreditMemoDTO : BindingModelBase<SalesCreditMemoDTO>
    {

        public SalesCreditMemoDTO()
        {

            AddAllAttributeValidators();
        }


        [DataMember]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember]
        [Display(Name = "No")]
        public int No { get; set; }

        [DataMember]
        [Display(Name = "CustomerNo")]
        public int CustomerNo { get; set; }

        [DataMember]
        [Display(Name = "CustomerName")]
        public string CustomerName { get; set; }

        [DataMember]
        [Display(Name = "CustomerAddress")]
        public string CustomerAddress { get; set; }


        [DataMember]
        [Display(Name = "DocumentDate")]
        public DateTime DocumentDate { get; set; }

        [DataMember]
        [Display(Name = "PostingDate")]
        public DateTime PostingDate { get; set; }

        [DataMember]
        [Display(Name = "DueDate")]
        public DateTime DueDate { get; set; }

        [DataMember]
        [Display(Name = "ApprovalStatus")]
        public string ApprovalStatus { get; set; }


        [DataMember]
        [Display(Name = "SalesCreditMemoLines")]
        public HashSet<SalesCreditMemoLineDTO> SalesCreditMemoLines { get; set; }

        [Display(Name = "BankId")]
        public Guid BankId { get; set; }

        [Display(Name = "BrahnchId")]
        public Guid BranchId { get; set; }

        [Display(Name = "BankBranchName")]
        public string BankBranchName { get; set; }


        //[Display(Name = "InvoiceNo")]
        //public int InvoiceNo { get; set; }

        [Display(Name = "SalesInvoiceId")]
        public Guid SalesInvoiceId { get; set; }

        [Display(Name = ("Posted"))]
        public Boolean Posted { get; set; }




        //[Display(Name = "ChartOfAccountId")]
        //public string ChartOfAccountId { get; set; }

    }
}
