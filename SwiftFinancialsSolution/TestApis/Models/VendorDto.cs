using System;
using System.Collections.Generic;

namespace TestApis.Models
{
    public class VendorDto
    {
        public long VendorId { get; set; }
        public string VendorCode { get; set; }
        public string VendorName { get; set; }
        public string TaxId { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; } = true;

        // Added for tab section
        public List<VendorBankDetailDto> BankDetails { get; set; } = new List<VendorBankDetailDto>();
    }
}
