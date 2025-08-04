using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Areas.Procurement.Model
{
    public class PurchaseRequisitionDTO
    {
        public Guid RequisitionID { get; set; }
        public Guid RequesterID { get; set; }
        public Guid DepartmentID { get; set; }
        public DateTime RequisitionDate { get; set; }
        public DateTime RequiredDate { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public List<RequisitionItemDTO> Items { get; set; }
    }
}