using System;

namespace TestApis.Models
{
    public class FixedAsset
    {
        public Guid Id { get; set; }
        public string No { get; set; }                
        public string SerialNo { get; set; }
        public string AssetName { get; set; }
        public string ResponsibleEmployee { get; set; }

        
        public Guid FASubClassId { get; set; }
        public string FASubClassDescription { get; set; }
        public Guid LocationId { get; set; }
        
        public string LocationDescription { get; set; }

        public decimal BookValue { get; set; }
        public bool IsInactive { get; set; }

        public string DepreciationMethod { get; set; } 
        public DateTime DepreciationStartDate { get; set; }
        public int NoOfDepreciationYears { get; set; }
        public DateTime DepreciationEndingDate { get; set; }
        public decimal ReducingBalancePercentage { get; set; }

        public string FAGroup { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }
}
