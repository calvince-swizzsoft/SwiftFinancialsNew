using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Areas.Admin.Models
{
    public class DynamicReport
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Report Name")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Display(Name = "SP")]
        [MaxLength(100)]
        public string StoredProcedureName { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}