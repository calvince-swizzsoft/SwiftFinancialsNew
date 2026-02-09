using System;
using System.Collections.Generic;

namespace Procurement.Models
{
    public class SupplierInvoiceDto
    {
        public long InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public long VendorId { get; set; }
        public long? PurchaseOrderId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal InvoiceAmount { get; set; }
        public string Status { get; set; }
        public List<SupplierInvoiceLineDto> Lines { get; set; } = new List<SupplierInvoiceLineDto>();
    }

    public class SupplierInvoiceLineDto
    {
        public long InvoiceLineId { get; set; }
        public long InvoiceId { get; set; }
        public long? POLineId { get; set; }
        public string Description { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Amount { get; set; }
    }
}
