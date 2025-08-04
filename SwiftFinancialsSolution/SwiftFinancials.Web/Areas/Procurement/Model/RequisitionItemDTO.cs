using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Areas.Procurement.Model
{
    public class RequisitionItemDTO
    {
        public Guid RequisitionItemID { get; set; }
        public Guid RequisitionID { get; set; }
        public Guid ItemID { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public string UnitOfMeasure { get; set; }
        public decimal EstimatedUnitPrice { get; set; }
        public DateTime NeededByDate { get; set; }
    }
}