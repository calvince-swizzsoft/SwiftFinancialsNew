using Domain.MainBoundedContext.AdministrationModule.Aggregates.BankBranchAgg;
using System;
using System.Collections.Generic;

namespace Domain.MainBoundedContext.AdministrationModule.Aggregates.BankAgg
{
    public class Bank : Domain.Seedwork.Entity
    {
        public short Code { get; set; }

        public string Description { get; set; }

        

        HashSet<BankBranch> _bankBranches;
        public virtual ICollection<BankBranch> BankBranches
        {
            get
            {
                if (_bankBranches == null)
                {
                    _bankBranches = new HashSet<BankBranch>();
                }
                return _bankBranches;
            }
            private set
            {
                _bankBranches = new HashSet<BankBranch>(value);
            }
        }
    }
}
