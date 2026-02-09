using System;

namespace TestApis.Models
{
    public class VendorBankDetailDto
    {
        public long VendorBankId { get; set; }
        public long VendorId { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string Branch { get; set; }
        public string Currency { get; set; }
        public bool IsPrimary { get; set; }
    }
}
