using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Areas.Procurement.Model
{
    public class RequestForQuotationDTO
    {
        public Guid RFQID { get; set; }
        public Guid RequisitionID { get; set; }
        public string RFQNumber { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ResponseDeadline { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}