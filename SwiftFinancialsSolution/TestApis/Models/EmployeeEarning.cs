using System;

namespace TestApis.Models
{
    public class EmployeeEarning
    {
        public int Id { get; set; }
        public int EmployeeNumber { get; set; }   // FK to EmployeeProfiles
        public int EarningCode { get; set; }      // FK to AccountDetails.Code
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }    // null = open-ended
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
