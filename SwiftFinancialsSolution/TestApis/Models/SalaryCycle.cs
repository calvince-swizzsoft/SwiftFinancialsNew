using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApis.Models
{
    public class SalaryCycle
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsProcessed { get; set; }
    }
}