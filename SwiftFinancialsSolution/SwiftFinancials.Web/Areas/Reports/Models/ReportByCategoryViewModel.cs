using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Areas.Reports.Models
{
    public class ReportByCategoryViewModel
    {
        public string CategoryName { get; set; }
        public int ReportID { get; set; }
        public string FileName { get; set; }
        public string ReportName { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}