using System;
using System.Collections.Generic;

namespace Procurement.Models
{
    public class PurchaseRequisitionDto
    {
        public long RequisitionId { get; set; }
        public string RequisitionNumber { get; set; }
        public Guid RequestedBy { get; set; }
        public string RequestedByFullname { get; set; }

        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; }

        public string Purpose { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string Projectcode { get;  set; }
        public int ProjectId { get;  set; }
        public string ProjectDescription { get;  set; }
        public List<PurchaseRequisitionLineDto> Lines { get; set; } = new List<PurchaseRequisitionLineDto>();

    }

    public class PurchaseRequisitionLineDto
    {
        public long PRLineId { get; set; }
        public long RequisitionId { get; set; }
        public int LineNumber { get; set; }
        public Guid? ItemId { get; set; }
        public string ItemDescription { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string AccountCode { get; set; }
        public int BudgetLine { get;  set; }
        public string Budgetdescription { get;  set; }
    }
}
