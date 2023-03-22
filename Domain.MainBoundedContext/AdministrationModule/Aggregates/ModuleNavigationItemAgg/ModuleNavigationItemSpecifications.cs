using Domain.Seedwork.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AdministrationModule.Aggregates.ModuleNavigationItemAgg
{
    public static class ModuleNavigationItemSpecifications
    {
        public static Specification<ModuleNavigationItem> ModuleNavigationItemCode(int itemCode)
        {
            Specification<ModuleNavigationItem> specification = new DirectSpecification<ModuleNavigationItem>(m => m.ItemCode == itemCode);

            return specification;
        }
    }
}
