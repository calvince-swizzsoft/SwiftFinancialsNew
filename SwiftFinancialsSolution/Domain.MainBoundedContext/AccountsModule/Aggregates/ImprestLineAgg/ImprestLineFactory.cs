using Domain.MainBoundedContext.AccountsModule.Aggregates.PurchaseInvoiceLineAgg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AccountsModule.Aggregates.ImprestLineAgg
{
    public class ImprestLineFactory
    {

        public Guid ImprestId { get; set; }

        public string ExpenseCategory { get; set; }

        public string Description { get; set; }

        public decimal Amount { get; set; }

        public static ImprestLine CreateImprestLine(Guid imprestId, string expenseCategory, string Description, decimal Amount)
        {

            var imprestLine = new ImprestLine();

            imprestLine.GenerateNewIdentity();


            imprestLine.ImprestId = imprestId;

            //purchaseInvoiceLine.PurchaseInvoiceNo = purchaseInvoiceNo;

            imprestLine.ExpenseCategory = expenseCategory;
            imprestLine.Description = Description;
            imprestLine.Amount = Amount;

            //imprestLine.DebitChartOfAccountId = debitChartOfAccountId;


            imprestLine.CreatedDate = DateTime.Now;

            return imprestLine;
        }

    }
}
