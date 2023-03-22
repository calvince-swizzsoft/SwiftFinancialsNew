using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Fedhaplus.DashboardApplication.Areas.Transactions.Models
{
    public class PurchaseOrderItemsModel
    {
        [DataMember]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember]
        [Display(Name = "Purchase Order")]
        public Guid PurchaseOrderId { get; set; }

        [DataMember]
        [Display(Name = "Reference Number")]
        public int PurchaseOrderReferenceNumber { get; set; }

        [DataMember]
        [Display(Name = "Supplier")]
        public Guid PurchaseOrderSupplierId { get; set; }

        [DataMember]
        [Display(Name = "Supplier")]
        public string PurchaseOrderSupplierDescription { get; set; }

        [DataMember]
        [Display(Name = "Payment Method")]
        public int PaymentMethod { get; set; }

        [DataMember]
        [Display(Name = "Payment Method")]
        public string PaymentMethodDescription => EnumHelper.GetDescription((TransactionPaymentMethod)PaymentMethod);

        [DataMember]
        [Display(Name = "Item")]
        public Guid ItemRegisterId { get; set; }

        [DataMember]
        [Display(Name = "Item")]
        public string ItemRegisterDescription { get; set; }

        [DataMember]
        [Display(Name = "Item Category")]
        public string ItemRegisterItemCategoryDescription { get; set; }

        [DataMember]
        [Display(Name = "Unit Of Measure")]
        public string ItemRegisterUnitOfMeasureDescription { get; set; }

        [DataMember]
        [Display(Name = "Asset Type")]
        public string ItemRegisterAssetTypeDescription { get; set; }

        [DataMember]
        [Display(Name = "Maximum Order")]
        public int ItemRegisterMaximumOrder { get; set; }

        [DataMember]
        [Display(Name = "Quantity")]
        [Range(1, 100000, ErrorMessage = "Quantity must be between 1 and 100000")]
        public int Quantity { get; set; }

        [DataMember]
        [Display(Name = "Unit Cost")]
        [Range(1, 1000000000, ErrorMessage = "Unit Cost must be between $1 and $1000000000")]
        public decimal UnitCost { get; set; }

        [DataMember]
        [Display(Name = "Sub Total")]
        public decimal SubTotal { get; set; }

        [DataMember]
        [Display(Name = "Total Cost")]
        public decimal TotalCost { get; set; }

        [DataMember]
        [Display(Name = "Is VAT Inclusive?")]
        public bool IsVATInclusive { get; set; }

        [DataMember]
        [Display(Name = "VAT")]
        public decimal VAT { get; set; }

        [DataMember]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [DataMember]
        [Display(Name = "Total VAT")]
        public decimal TotalVAT { get; set; }

        [DataMember]
        [Display(Name = "Is Posted?")]
        public bool Posted { get; set; }

        [DataMember]
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [DataMember]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [DataMember]
        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; }

        [DataMember]
        [Display(Name = "Balance")]
        public decimal Balance { get; set; }

        [DataMember]
        [Display(Name = "Trx Reference")]
        public decimal TrxReference { get; set; }
    }
}