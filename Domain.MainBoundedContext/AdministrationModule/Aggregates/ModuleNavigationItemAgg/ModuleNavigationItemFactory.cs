using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AdministrationModule.Aggregates.ModuleNavigationItemAgg
{
    public static class ModuleNavigationItemFactory
    {
        public static ModuleNavigationItem CreateModuleNavigationItem(Guid moduleId, string moduleDescription, int itemCode, string itemDescription, int parentItemCode, string parentItemDescription)
        {
            var moduleNavigationItem = new ModuleNavigationItem();

            moduleNavigationItem.GenerateNewIdentity();

            moduleNavigationItem.ModuleId = moduleId;

            moduleNavigationItem.ModuleDescription = moduleDescription;

            moduleNavigationItem.ItemCode = itemCode;

            moduleNavigationItem.ItemDescription = itemDescription;

            moduleNavigationItem.ParentItemCode = parentItemCode;

            moduleNavigationItem.ParentItemDescription = parentItemDescription;

            moduleNavigationItem.CreatedDate = DateTime.Now;

            return moduleNavigationItem;
        }
    }
}
