using Domain.Seedwork.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AccountsModule.Aggregates.ChartOfAccountAgg
{
    public static class ChartOfAccountSpecifications
    {
        public static ISpecification<ChartOfAccount> DefaultSpec()
        {
            Specification<ChartOfAccount> specification = new TrueSpecification<ChartOfAccount>();

            return specification;
        }

        public static ISpecification<ChartOfAccount> ParentChartOfAccounts()
        {
            Specification<ChartOfAccount> specification = new TrueSpecification<ChartOfAccount>();

            specification &= new DirectSpecification<ChartOfAccount>(c => c.ParentId == null);

            return specification;
        }

        public static ISpecification<ChartOfAccount> ChartOfAccountWithAccountCode(int chartOfAccountCode)
        {
            Specification<ChartOfAccount> specification = new TrueSpecification<ChartOfAccount>();

            specification &= new DirectSpecification<ChartOfAccount>(c => c.AccountCode == chartOfAccountCode);

            return specification;
        }

        public static ISpecification<ChartOfAccount> ChartOfAccountWithId(params Guid[] chartOfAccountIds)
        {
            Specification<ChartOfAccount> specification = new TrueSpecification<ChartOfAccount>();

            var chartOfAccountIdSpecs = new List<Specification<ChartOfAccount>>();

            if (chartOfAccountIds != null)
            {
                Array.ForEach(chartOfAccountIds, (chartOfAccountIdId) =>
                {
                    var alternateChannelLogIdSpec = new DirectSpecification<ChartOfAccount>(x => x.Id == chartOfAccountIdId);

                    chartOfAccountIdSpecs.Add(alternateChannelLogIdSpec);
                });

                if (chartOfAccountIdSpecs.Any())
                {
                    var spec0 = chartOfAccountIdSpecs[0];

                    for (int i = 1; i < chartOfAccountIdSpecs.Count; i++)
                    {
                        spec0 |= chartOfAccountIdSpecs[i];
                    }

                    specification &= (spec0);
                }
            }

            return specification;
        }
    }
}
