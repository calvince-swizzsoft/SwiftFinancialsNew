using System;

namespace TestApis.Models
{
    public class FALocation
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public bool IsLocked { get; set; } 
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }
}
