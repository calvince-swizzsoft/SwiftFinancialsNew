using System;

namespace TestApis.Models
{
    public class PAYEPersonalRelief
    {
        public int Id { get; set; }
        public int TaxYear { get; set; }
        public decimal MonthlyRelief { get; set; }
        public decimal AnnualRelief { get; set; } // generated column
        public DateTime CreatedAt { get; set; }
    }
}
