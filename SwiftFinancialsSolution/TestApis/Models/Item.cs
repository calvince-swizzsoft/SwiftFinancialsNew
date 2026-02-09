using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApis.Models
{
    public class Item
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
        public decimal InventoryBalance { get; set; }
        public string CostingMethod { get; set; }   // Average, FIFO, LIFO
        public Guid SequentialId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }

}