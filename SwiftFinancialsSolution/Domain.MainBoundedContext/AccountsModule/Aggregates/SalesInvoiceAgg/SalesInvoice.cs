using Domain.MainBoundedContext.AccountsModule.Aggregates.PurchaseInvoiceLineAgg;
using Domain.MainBoundedContext.AccountsModule.Aggregates.SalesInvoiceLineAgg;
using Domain.Seedwork;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AccountsModule.Aggregates.SalesInvoiceAgg
{
    public class SalesInvoice : Entity
    {

        //No
        public int SalesInvoiceNo { get; set; }
        // allow pick from vendors
        public int CustomerNo { get; set; }

        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }

        //should allow date capture and validate the date capturd
        public DateTime DocumentDate { get; set; }

        public DateTime PostingDate { get; set; }

        public DateTime DueDate { get; set; }

        //Approval Status Should be Open, Pending Approval, Approved
        public string ApprovalStatus { get; set; }


        public Boolean Posted { get; set; }


        HashSet<SalesInvoiceLine> _salesInvoiceLines;
        public virtual ICollection<SalesInvoiceLine> SalesInvoiceLines
        {
            get
            {
                if (_salesInvoiceLines == null)
                {
                    _salesInvoiceLines = new HashSet<SalesInvoiceLine>();
                }
                return _salesInvoiceLines;
            }
            private set
            {
                _salesInvoiceLines = new HashSet<SalesInvoiceLine>(value);
            }
        }



        public void AddLine(int type, int salesInvoiceLnNo, string salesInvoiceLnDescription, int salesInvoiceLnQuantity, decimal salesInvoiceLnTotalAmount, Guid salesInvoiceLineCreditChartOfAccountId, ServiceHeader serviceHeader)
        {
            var salesInvoiceLine = SalesInvoiceLineFactory.CreateSalesInvoiceLine(this.Id, type, salesInvoiceLnNo, salesInvoiceLnDescription, salesInvoiceLnQuantity, salesInvoiceLnTotalAmount, salesInvoiceLineCreditChartOfAccountId);
            //CreateJournalEntry(this.Id, chartOfAccountId, contraChartOfAccountId, customerAccountId, amount, this.ValueDate, serviceHeader);

            this.SalesInvoiceLines.Add(salesInvoiceLine);
        }






    }
}