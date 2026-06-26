using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApis.Models
{
    public class CustomerJournalDTO
    {
        public string FullName { get; set; }
        public string Address_MobileLine { get; set; }
        public string Address_Email { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
    }
}