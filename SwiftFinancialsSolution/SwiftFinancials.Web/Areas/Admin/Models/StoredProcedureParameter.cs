using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Areas.Admin.Models
{
    public class StoredProcedureParameter
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string Value { get; set; }
    }
}