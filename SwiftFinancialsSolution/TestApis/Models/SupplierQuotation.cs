using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApis.Models
{
    public class SupplierQuotation
    {
        public int Id { get; set; }
        public int RFQId { get; set; }
        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public string QuotationNumber { get; set; }
        public string Currency { get; set; }
        public decimal Discount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public string PaymentTerms { get; set; }
        public string WarrantyInfo { get; set; }
        public string ContactPerson { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; }

        public List<SupplierQuotationLine> Lines { get; set; } = new List<SupplierQuotationLine>();
    }
}