using System;

namespace TestApis.Models
{
    public class ItemJournal
    {
        public Guid Id { get; set; }
        public DateTime PostingDate { get; set; }
        public string EntryType { get; set; }   // Purchase, Sale, Adjustment+, Adjustment-, etc.
        public string DocumentNo { get; set; }
        public Guid ItemId { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public Guid ItemLocationId { get; set; }
        public decimal Quantity { get; set; }
        public Guid SequentialId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }

        public bool IsPosted { get; set; }       // Draft (false) or Posted (true)
        public string Status { get; set; }       // "Draft", "Posted", "Cancelled"
    }
}
