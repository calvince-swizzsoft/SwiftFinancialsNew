using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Areas.Procurement.Model
{
    public class RequisitionApprovalDTO
    {
        public Guid ApprovalID { get; set; }
        public Guid RequisitionID { get; set; }
        public Guid ApproverID { get; set; }
        public string ApprovalStatus { get; set; }
        public DateTime ApprovalDate { get; set; }
        public string Comments { get; set; }
    }
}