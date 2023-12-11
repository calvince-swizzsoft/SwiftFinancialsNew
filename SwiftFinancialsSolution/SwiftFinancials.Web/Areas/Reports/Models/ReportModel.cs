using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Areas.Reports.Models
{
    public class ReportModel
    {
        [Display(Name = "Category")]
        public Guid CategoryId { get; set; }

        [Display(Name = "Category")]
        public string Category { get; set; }
    }
}