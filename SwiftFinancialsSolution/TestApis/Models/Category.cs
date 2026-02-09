using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApis.Models
{
    public class Category
    {
        public Guid Id { get; set; }              
        public string Description { get; set; }   
        public bool IsLocked { get; set; }        
        public Guid SequentialId { get; set; }     
        public string CreatedBy { get; set; }    
        public DateTime CreatedDate { get; set; } 
    }
}