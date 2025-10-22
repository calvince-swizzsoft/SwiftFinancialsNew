using Domain.Seedwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AccountsModule.Aggregates.ReceiptLineAgg
{
    public class ReceiptLine : Entity
    {
        public Guid ReceiptId { get; set; }
        public Guid InvoiceId { get; set; }

        public int InvoiceNo { get; set; }
        //public int Type { get; set; }

        public int No { get; set; }

        public Guid ChartOfAccountId { get; set; }

        public string Description { get; set; }

        public Decimal Amount { get; set; }

        public int AccountType { get; set; }

        public int DocumentType { get; set; }
    }
}
