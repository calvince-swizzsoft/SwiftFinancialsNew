using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApis.Models
{
    public class AccountDetails
    {
        public int Code { get; set; }                // Primary Key
        public string Name { get; set; }             // Friendly name
        public string LinkedGLAccount { get; set; }  // GL account number/code
        public bool TaxableEarnings { get; set; }    // Checkbox
        public bool AllowableDeductions { get; set; }// Checkbox
    }
}