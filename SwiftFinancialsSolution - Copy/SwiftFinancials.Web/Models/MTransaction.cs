using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SwiftFinancials.Web.Models
{
    public class MTransaction
    {
        public long ID { get; set; }
         public DateTime? TransactionDate { get; set; }
        public string ClientCode { get; set; }
        public string SessionID { get; set; }
        public string AccountNo { get; set; }
        public string AccountName { get; set; }
        public string DocumentNo { get; set; }
        public string TransactionType { get; set; }
        public string Telephone { get; set; }
        public bool? Posted { get; set; }
        public DateTime? DatePosted { get; set; }
        public string DestinationAccount { get; set; }
        public string LoanNo { get; set; }
        public int Status { get; set; }
        public string Comments { get; set; }
        public string Amount { get; set; }
        public string Charge { get; set; }
        public string Description { get; set; }
        public string ApplicationType { get; set; }
    }
}