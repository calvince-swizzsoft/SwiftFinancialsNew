using System;

namespace Procurement.Models
{
    public class ItemDto2
    {
        public Guid Id { get; set; }
        public string ItemId { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }

        public Guid ItemCategoryId { get; set; }
        public string CategoryDescription { get; set; }

        public Guid UnitOfMeasureId { get; set; }
        public string UnitOfMeasureDescription { get; set; }

        public Guid LocationId { get; set; }
        public string LocationDescription { get; set; }

        public string InventoryBalance { get; set; }
        public string CostingMethod { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}