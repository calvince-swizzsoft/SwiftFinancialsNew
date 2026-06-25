using Domain.MainBoundedContext.AccountsModule.Aggregates;
using Domain.MainBoundedContext.AccountsModule.Aggregates.ImprestLineAgg;
using Domain.Seedwork;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AccountsModule.Aggregates.ImprestAgg
{
    public class Imprest : Entity
    {
        public String No { get; set; }

        public string Amount { get; set; }

        public DateTime RequestDate { get; set; }

        public string EmployeeNo { get; set; }

        public string EmployeeName { get; set; }

        public decimal AmountRequested { get; set; }

        public string Purpose { get; set; }

         public ImprestStatus Status { get; set; }

        public ICollection<ImprestLine> Lines { get; set; } = new List<ImprestLine>();

        public Boolean Posted { get; set; }

        public void AddLine(
    int lineNo,
    string expenseCategory,
    string description,
    decimal amount)
        {
            var imprestLine = ImprestLineFactory.CreateImprestLine(
                this.Id,
                expenseCategory,
                description,
                amount);

            this.Lines.Add(imprestLine);
        }



    }
}
