using System;
using System.Collections.Generic;

namespace Procurement.Models
{
    public class GoodsReceivedNoteDto
    {
        public long GRNId { get; set; }
        public string GRNNumber { get; set; }
        public long PurchaseOrderId { get; set; }
        public DateTime ReceivedDate { get; set; }
        public Guid ReceivedBy { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // Purchase Order Info
        public string PONumber { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime ExpectedDeliveryDate { get; set; }

        // Vendor Info
        public string VendorName { get; set; }
        public string VendorPhone { get; set; }
        public string VendorEmail { get; set; }

        // Employee Info
        public string ReceivedByFirstName { get; set; }
        public string ReceivedByLastName { get; set; }

        // GRN Lines
        public List<GoodsReceivedLineDto> Lines { get; set; } = new List<GoodsReceivedLineDto>();
    }

    public class GoodsReceivedLineDto
    {
        public long GRNLineId { get; set; }
        public long GRNId { get; set; }
        public long POLineId { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public string ConditionRemarks { get; set; }

        // PO Line Info
        public string ItemDescription { get; set; }
        public decimal QuantityOrdered { get; set; }
        public decimal UnitPrice { get; set; }
    }
}


