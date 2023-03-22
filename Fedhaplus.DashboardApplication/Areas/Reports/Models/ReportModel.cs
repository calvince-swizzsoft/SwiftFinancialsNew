using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Fedhaplus.DashboardApplication.Areas.Reports.Models
{
    public class ReportModel
    {
        [Display(Name = "Category")]
        public Guid ItemCategoryId { get; set; }

        [Display(Name = "Category")]
        public string Category { get; set; }
    }
}