using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Areas.Accounts.Models.GLAccountsDetermination
{
    public class GLAccountsDet
    {
        public string systemGLAccountCode { get; set; }

        public string mappedGLAccountName { get; set; }

        public string mappedGLAccountcostCenter { get; set; }

        public string createdDate { get; set; }
    }

    public class GLADRply
    {
        public string response { get; set; }
    }
}