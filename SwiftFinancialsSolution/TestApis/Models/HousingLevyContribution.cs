using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApis.Models
{
    public class HousingLevyContribution
    {
        public int Id { get; set; }
        public decimal EmployeeAmount { get; set; }
        public decimal EmployerAmount { get; set; }
        public decimal Total { get; set; }
    }
}