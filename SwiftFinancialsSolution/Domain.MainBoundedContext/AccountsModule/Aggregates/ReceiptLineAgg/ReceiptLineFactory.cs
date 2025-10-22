using Domain.MainBoundedContext.AccountsModule.Aggregates.PaymentLineAgg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AccountsModule.Aggregates.ReceiptLineAgg
{
    public static class ReceiptLineFactory
    {

        public static ReceiptLine CreateReceiptLine(Guid receiptId, int type, int no, string description, decimal amount, Guid chartOfAccountId, int accounType, int documentType)
        {

            var receiptLine = new ReceiptLine();

            receiptLine.GenerateNewIdentity();

            receiptLine.ReceiptId = receiptId;
            //purchaseInvoiceLine.PurchaseInvoiceNo = purchaseInvoiceNo;
            receiptLine.AccountType = type;
            receiptLine.No = no;
            receiptLine.Description = description;
            receiptLine.Amount = amount;

            receiptLine.ChartOfAccountId = chartOfAccountId;

            //paymentLine.DocumentNo = documentNO;

            receiptLine.AccountType = accounType;

            receiptLine.DocumentType = documentType;

            receiptLine.CreatedDate = DateTime.Now;

            return receiptLine;
        }
    }
}
