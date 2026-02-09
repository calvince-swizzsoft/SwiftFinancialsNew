using System;

namespace TestApis.Models
{
    public class EmployeeDeduction
    {
        public int Id { get; set; }
        public int EmployeeNumber { get; set; }   // FK to EmployeeProfiles
        public int DeductionCode { get; set; }    // FK to AccountDetails.Code
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }    // NULL = open-ended
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
