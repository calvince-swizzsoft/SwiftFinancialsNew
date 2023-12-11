using System;
using System.ComponentModel.DataAnnotations;

namespace Application.MainBoundedContext.DTO.RegistryModule
{
    public class StationDTO
    { 
        [Display(Name = "Id")]
        public Guid Id { get; set; }
        
        [Display(Name = "Zone")]
        public Guid ZoneId { get; set; }
        
        [Display(Name = "Zone")]
        public string ZoneDescription { get; set; }
        
        [Display(Name = "Division")]
        public Guid ZoneDivisionId { get; set; }
        
        [Display(Name = "Division")]
        public string ZoneDivisionDescription { get; set; }
        
        [Display(Name = "Employer")]
        public Guid ZoneDivisionEmployerId { get; set; }
        
        [Display(Name = "Employer")]
        public string ZoneDivisionEmployerDescription { get; set; }
        
        [Display(Name = "Name")]
        public string Description { get; set; }
        
        [Display(Name = "Address Line 1")]
        public string AddressAddressLine1 { get; set; }
        
        [Display(Name = "Address Line 2")]
        public string AddressAddressLine2 { get; set; }
        
        [Display(Name = "Street")]
        public string AddressStreet { get; set; }
        
        [Display(Name = "Postal Code")]
        public string AddressPostalCode { get; set; }
        
        [Display(Name = "City")]
        public string AddressCity { get; set; }
        
        [Display(Name = "E-mail")]
        public string AddressEmail { get; set; }
        
        [Display(Name = "Land Line")]
        public string AddressLandLine { get; set; }
        
        [Display(Name = "Mobile Line")]
        public string AddressMobileLine { get; set; }
        
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }
    }
}
