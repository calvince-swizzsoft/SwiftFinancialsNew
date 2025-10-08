using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AccountsModule.Aggregates.PurchaseInvoiceAgg
{
    public static class PurchaseInvoiceFactory
    {

        public static PurchaseInvoice CreatePurchaseInvoice(string no, int vendorNo, string vendorName, string vendorAddress, DateTime documentDate, DateTime postingDate, DateTime dueDate, string approvalStatus, Decimal PaidAmount, Decimal RemainingAmount, ServiceHeader serviceHeader)
        {
            var purchaseInvoice = new PurchaseInvoice();

            purchaseInvoice.No = no;
            purchaseInvoice.VendorNo = vendorNo;
            purchaseInvoice.VendorName = vendorName;
            purchaseInvoice.VendorAddress = vendorAddress;
            purchaseInvoice.DocumentDate = documentDate;
            purchaseInvoice.PostingDate = postingDate;
            purchaseInvoice.DueDate = dueDate;
            purchaseInvoice.ApprovalStatus = approvalStatus;
            purchaseInvoice.PaidAmount = PaidAmount;
            purchaseInvoice.RemainingAmount = RemainingAmount;
            purchaseInvoice.CreatedDate = DateTime.Now; 

            purchaseInvoice.GenerateNewIdentity();

            return purchaseInvoice;

        }
    }
}
