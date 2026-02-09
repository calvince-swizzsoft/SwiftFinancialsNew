using System;

namespace TestApis.Models
{
    public class FASubClass
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public bool IsLocked { get; set; }
        public Guid FAClassId { get; set; }

        public string FAClassDescription { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }
}
