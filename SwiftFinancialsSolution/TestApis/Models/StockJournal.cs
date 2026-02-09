using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApis.Models
{
    public class StockJournal
    {
        public Guid Id { get; set; }
        public Guid ItemId { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public string ActionType { get; set; } // Increase / Reduce
        public decimal Quantity { get; set; }
        public decimal OriginalBalance { get; set; }
        public decimal NewBalance { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }

}