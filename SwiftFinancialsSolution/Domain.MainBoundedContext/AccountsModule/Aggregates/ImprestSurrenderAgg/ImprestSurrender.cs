using Domain.Seedwork;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AccountsModule.Aggregates.ImprestSurrender
{
    public class ImprestSurrender : Entity
    {

        public string SurrenderNo { get; set; }

        public Guid ImprestId { get; set; }

        public DateTime SurrenderDate { get; set; }

        public decimal AmountIssued { get; set; }

        public decimal AmountSpent { get; set; }

        public decimal AmountReturned { get; set; }

        public decimal ReimbursableAmount { get; set; }

        public SurrenderStatus Status { get; set; }

        public ICollection<ImprestSurrenderLine> Lines { get; set; } = new List<ImprestSurrenderLine>();
    }
}
