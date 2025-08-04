using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Areas.Procurement.Model
{
    public class VendorDTO
    {
        public Guid VendorID { get; set; }
        public string VendorName { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string TaxNumber { get; set; }
        public bool IsActive { get; set; }
    }
}