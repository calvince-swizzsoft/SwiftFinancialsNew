using System;

namespace TestApis.Models
{
    public class EmployeeProfile
    {
        public int EmployeeNumber { get; set; }   // PK
        public string Name { get; set; }
        public string Branch { get; set; }
        public string Designation { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string JobGroup { get; set; }
        public bool Disabled { get; set; }
        public string NSSFNumber { get; set; }
        public string SHANumber { get; set; }
        public string KRAPIN { get; set; }
        public string AccountNumber { get; set; }
        public string BankCode { get; set; }
        public string BranchCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
