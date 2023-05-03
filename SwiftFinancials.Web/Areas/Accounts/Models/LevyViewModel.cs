using Application.Seedwork;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace SwiftFinancials.Web.Areas.Accounts.Models
{
    public class LevyViewModel : BindingModelBase<LevyViewModel>
    {
        public LevyViewModel()
        {
            AddAllAttributeValidators();
        }

        [DataMember]
        [Display(Name = "Name")]
        [Required]
        public string LevyDescription { get; set; }

        [DataMember]
        [Display(Name = "Is Locked?")]
        public bool LevyIsLocked { get; set; }

        [DataMember]
        [Display(Name = "Levy Split Name")]
        [Required]
        public string LevySplitDescription { get; set; }

        [DataMember]
        [Display(Name = "COA")]
        public Guid LevySplitChartOfAccountId { get; set; }

        [DataMember]
        [Display(Name = "G/L Account Name")]
        public string LevySplitChartOfAccountAccountName { get; set; }

        [DataMember]
        [Display(Name = "Percentage")]
        public double LevySplitPercentage { get; set; }

    }
}