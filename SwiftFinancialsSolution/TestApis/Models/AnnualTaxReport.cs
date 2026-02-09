using System;

namespace TestApis.Models
{
    public class AnnualTaxReport
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int TaxYear { get; set; }

        public decimal TotalGross { get; set; }
        public decimal TotalPAYE { get; set; }
        public decimal TotalNSSF { get; set; }
        public decimal TotalSHIF { get; set; }
        public decimal TotalHousingLevy { get; set; }
        public decimal TotalOtherDeductions { get; set; }
        public decimal TotalNet { get; set; }

        public DateTime GeneratedAt { get; set; }
    }
}
