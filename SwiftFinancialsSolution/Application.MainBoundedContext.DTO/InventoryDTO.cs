using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
namespace Application.MainBoundedContext.DTO
{
    public class InventoryCategoryDTO
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        [Display(Name = "Name")]
        [Required]
        public string Name { get; set; }

        [DataMember]
        [Display(Name = "Remarks")]
        [Required]
        public string Remarks { get; set; }
    }
}
