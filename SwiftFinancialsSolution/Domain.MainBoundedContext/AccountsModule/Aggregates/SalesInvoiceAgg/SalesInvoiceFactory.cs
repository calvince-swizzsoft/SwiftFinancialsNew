using Domain.MainBoundedContext.AccountsModule.Aggregates.PurchaseInvoiceAgg;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AccountsModule.Aggregates.SalesInvoiceAgg
{ 
    public static class SalesInvoiceFactory
    {

       public static SalesInvoice CreateSalesInvoice(int customerNo, string customerName, string customerAddress, DateTime documentDate, DateTime postingDate, DateTime dueDate, string approvalStatus, ServiceHeader serviceHeader)
        {
            var salesInvoice = new SalesInvoice();

            //purchaseInvoice.No = no;
            salesInvoice.CustomerNo = customerNo;
            salesInvoice.CustomerName = customerName;
            salesInvoice.DueDate = dueDate;
            salesInvoice.ApprovalStatus = approvalStatus;
            salesInvoice.CreatedDate = DateTime.Now;

            salesInvoice.GenerateNewIdentity();

            return salesInvoice;

        }

    }
}