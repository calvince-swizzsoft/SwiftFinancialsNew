using System;

namespace Procurement.Models
{
    public class ItemDto
    {
        public long ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Unit { get; set; }
        public bool IsStocked { get; set; } = true;
        public decimal DefaultPrice { get; set; }
    }

   
}
