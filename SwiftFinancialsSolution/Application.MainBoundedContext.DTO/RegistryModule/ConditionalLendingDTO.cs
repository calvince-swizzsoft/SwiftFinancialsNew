using System;
using System.ComponentModel.DataAnnotations;

namespace Application.MainBoundedContext.DTO.RegistryModule
{
    public class ConditionalLendingDTO
    {
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [Display(Name = "Loan Product")]
        public Guid LoanProductId { get; set; }

        [Display(Name = "Loan Product")]
        public string LoanProductDescription { get; set; }

        [Display(Name = "Name")]
        public string Description { get; set; }

        [Display(Name = "Is Locked")]
        public bool IsLocked { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }
    }
}
