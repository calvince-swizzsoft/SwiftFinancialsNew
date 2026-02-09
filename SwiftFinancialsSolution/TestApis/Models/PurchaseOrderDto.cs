using System;
using System.Collections.Generic;

namespace Procurement.Models
{
    public class PurchaseOrderDto
    {
        public long PurchaseOrderId { get; set; }
        public string PONumber { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; }

        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string Projectcode { get; set; }
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; }
        public Guid CreatedBy { get; set; }
        public List<PurchaseOrderLineDto> Lines { get; set; } = new List<PurchaseOrderLineDto>();
    }

    public class PurchaseOrderLineDto
    {
        public long POLineId { get; set; }
        public long PurchaseOrderId { get; set; }
        public int LineNumber { get; set; }
        public Guid? ItemId { get; set; }
        public string ItemDescription { get; set; }
        public decimal QuantityOrdered { get; set; }
        public decimal UnitPrice { get; set; }
        public int BudgetLine { get; set; }
        public string Budgetdescription { get; set; }
        public decimal ReceivedQuantity { get; set; }

    }
}
