using System;

namespace SwiftFinancials.Web.Areas.Payroll.models
{
    public class PayrollEntry
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int SalaryCycleId { get; set; }
        public decimal GrossSalary { get; set; }

        // Deductions
        public decimal PAYE { get; set; }
        public decimal NSSF { get; set; }
        public decimal SHIF { get; set; }
        public decimal HousingLevy { get; set; }
        public decimal OtherDeductions { get; set; }

        // Derived
        public decimal NetSalary { get; set; }

        // Flags
        public bool PostedToGL { get; set; }
        public bool PayslipGenerated { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
