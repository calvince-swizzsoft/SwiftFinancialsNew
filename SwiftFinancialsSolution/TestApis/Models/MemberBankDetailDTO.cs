using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestApis.Models
{
    public class MemberBankDetailDTO
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string BankName { get; set; }
        public string BranchName { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public bool IsPrimaryAccount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}