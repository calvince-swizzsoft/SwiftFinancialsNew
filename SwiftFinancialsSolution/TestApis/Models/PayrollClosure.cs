using System;

namespace TestApis.Models
{
    public class PayrollClosure
    {
        public int Id { get; set; }
        public int SalaryCycleId { get; set; }
        public DateTime ClosureDate { get; set; }
        public string ClosedBy { get; set; }
        public bool IsClosed { get; set; }
        public bool IsPostedToGL { get; set; }
        public bool PayslipsGenerated { get; set; }
    }
}
