using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AdministrationModule.Aggregates.BankAgg
{
    public static class BankFactory
    {
        public static Bank CreateBank(int code, string description)
        {
            var bank = new Bank()
            {
                Code = (short)code,
                Description = description
            };

            bank.GenerateNewIdentity();

            bank.CreatedDate = DateTime.Now;

            return bank;
        }
    }
}
