using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.MainBoundedContext.AccountsModule.Aggregates.BankLinkageAgg
{
    public static class BankLinkageFactory
    {
        public static BankLinkage CreateBankLinkage(Guid branchId, Guid chartOfAccountId, string bankName, string bankBranchName, string bankAccountNumber, string remarks)
        {
            var bankLinkage = new BankLinkage();

            bankLinkage.GenerateNewIdentity();

            bankLinkage.BranchId = branchId;

            bankLinkage.ChartOfAccountId = chartOfAccountId;

            bankLinkage.BankName = bankName;

            bankLinkage.BankBranchName = bankBranchName;

            bankLinkage.BankAccountNumber = bankAccountNumber;

            bankLinkage.Remarks = remarks;

            bankLinkage.CreatedDate = DateTime.Now;

            return bankLinkage;
        }
    }
}
