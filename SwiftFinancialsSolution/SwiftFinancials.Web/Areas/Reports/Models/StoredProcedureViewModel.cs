using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Areas.Reports.Models
{
    public class StoredProcedureViewModel
    {
        public string StoredProcedureName { get; set; }
        public List<StoredProcedureParameter> Parameters { get; set; }
    }
}