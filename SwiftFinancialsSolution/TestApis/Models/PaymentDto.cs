using System;
using System.Collections.Generic;

namespace Procurement.Models
{
    public class PaymentDto
    {
        public long PaymentId { get; set; }
        public string PaymentNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public Guid CreatedBy { get; set; }
        public List<PaymentLineDto> Lines { get; set; } = new List<PaymentLineDto>();
    }

    public class PaymentLineDto
    {
        public long PaymentLineId { get; set; }
        public long PaymentId { get; set; }
        public long InvoiceId { get; set; }
        public decimal Amount { get; set; }
    }
}
