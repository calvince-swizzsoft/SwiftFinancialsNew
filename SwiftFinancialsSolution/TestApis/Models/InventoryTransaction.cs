using System;

namespace TestApis.Models
{
    public class InventoryTransaction
    {
        public Guid Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public string DocumentNo { get; set; }

        public Guid ItemId { get; set; }          // FK -> swiftFin_Items
        public Guid LocationId { get; set; }      // FK -> Locations

        public string EntryType { get; set; }     // Purchase, Sale, Transfer, Adjustment
        public decimal Quantity { get; set; }
        public decimal? UnitCost { get; set; }    // nullable if not applicable
        public decimal TotalCost { get; set; }    // can be auto-calculated in service

        public Guid? ReferenceJournalId { get; set; } // FK -> swiftFin_ItemJournals
        public Guid SequentialId { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
