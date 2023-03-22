using Infrastructure.Crosscutting.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AdministrationModule.Aggregates.ModuleNavigationItemAgg
{
    public class ModuleNavigationItem : Domain.Seedwork.Entity
    {
        public Guid ModuleId { get; set; }

        public string ModuleDescription { get; set; }

        [Index("IX_ModuleNavigationItem_ItemCode")]
        public int ItemCode { get; set; }

        public string ItemDescription { get; set; }

        [Index("IX_ModuleNavigationItem_ParentItemCode")]
        public int ParentItemCode { get; set; }

        public string ParentItemDescription { get; set; }

        

        
    }
}
