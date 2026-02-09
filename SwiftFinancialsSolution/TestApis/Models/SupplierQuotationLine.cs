using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApis.Models
{
    public class SupplierQuotationLine
    {
        public int Id { get; set; }
        public int QuotationId { get; set; }
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string Notes { get; set; }
    }
}