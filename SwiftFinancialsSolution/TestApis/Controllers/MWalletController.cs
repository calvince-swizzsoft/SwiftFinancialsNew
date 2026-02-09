using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Http;

namespace TestApis.Controllers
{
    [RoutePrefix("api/mwallet")]
    public class MWalletController : ApiController
    {
        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        // ================= DTO =================
        public class MWalletTxnDto
        {
            public string SessionID { get; set; }
            public string AccountNo { get; set; }          // Source Wallet
            public string DestinationAccount { get; set; } // Transfer
            public string TransactionType { get; set; }    // Deposit | Withdrawal | Transfer | LoanRepayment
            public decimal Amount { get; set; }
            public decimal Charge { get; set; }
            public string Telephone { get; set; }
            public string DocumentNo { get; set; }
            public string Description { get; set; }
            public string ApplicationType { get; set; }
            public Guid? LoanCustomerAccountId { get; set; }
        }

        [HttpGet]
        [Route("account/{accountNo}/balance")]
        public async Task<IHttpActionResult> GetAccountBalance(string accountNo)
        {
            using (SqlConnection con = new SqlConnection(_conn))
            {
                await con.OpenAsync();
                var wallet = await GetWallet(con, null, accountNo);
                if (wallet == null) return BadRequest("Account not found");

                decimal balance = await GetBalance(con, null, wallet.CustomerAccountId, wallet.ProductId, 4);
                return Ok(new { AccountNo = accountNo, Balance = balance });
            }
        }

        // =========================================================
        // ================= POST TRANSACTION ======================
        // =========================================================

        [HttpPost]
        [Route("post")]
        public async Task<IHttpActionResult> Post(MWalletTxnDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.SessionID))
                return BadRequest("Invalid payload");

            using (var con = new SqlConnection(_conn))
            {
                await con.OpenAsync();
                using (var tx = con.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        if (await IsDuplicate(con, tx, dto.SessionID))
                            return Ok(new { success = true, message = "Already processed" });

                        var src = await GetWallet(con, tx, dto.AccountNo);
                        if (src == null)
                            return BadRequest("Source wallet not found");

                        // ---- Balance Check ----
                        if (!dto.TransactionType.Equals("Deposit", StringComparison.OrdinalIgnoreCase))
                        {
                            var bal = await GetBalance(con, tx,
                                src.CustomerAccountId, src.ProductId, 4);

                            if (bal < (dto.Amount + dto.Charge))
                                return BadRequest("Insufficient balance");
                        }

                        var mTxnId = await InsertMTransaction(con, tx, dto);
                        var journalId = await InsertJournal(con, tx, dto, src.BranchId);

                        switch (dto.TransactionType)
                        {
                            case "Deposit":
                                await PostDeposit(con, tx, dto, src, journalId);
                                break;

                            case "Withdrawal":
                                await PostWithdrawal(con, tx, dto, src, journalId);
                                break;

                            case "Transfer":
                                var dst = await GetWallet(con, tx, dto.DestinationAccount);
                                if (dst == null)
                                    return BadRequest("Destination wallet not found");

                                await PostTransfer(con, tx, dto, src, dst, journalId);
                                break;

                            case "LoanRepayment":
                                if (!dto.LoanCustomerAccountId.HasValue)
                                    return BadRequest("Loan account required");

                                await PostLoanRepayment(con, tx, dto, src, journalId);
                                break;

                            default:
                                return BadRequest("Unsupported transaction type");
                        }

                        await FinalizeMTransaction(con, tx, mTxnId, journalId);

                        tx.Commit();
                        return Ok(new { success = true, journalId });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }
        }

        // =========================================================
        // ===== GET MEMBER ACCOUNTS + BALANCES (Reference2) =======
        // =========================================================
        [HttpGet]
        [Route("member/{memberNo}/accounts")]
        public async Task<IHttpActionResult> GetMemberAccounts(string memberNo)
        {
            using (var con = new SqlConnection(_conn))
            {
                await con.OpenAsync();

                // Get Customer
                var cmdCust = new SqlCommand(@"
            SELECT Id, Individual_FirstName, Individual_LastName 
            FROM swiftFin_Customers
            WHERE Reference2 = @m AND RecordStatus = 1", con);
                cmdCust.Parameters.AddWithValue("@m", memberNo);

                Guid customerId;
                string customerName;
                using (var r = await cmdCust.ExecuteReaderAsync())
                {
                    if (!r.Read()) return BadRequest("Member not found");
                    customerId = (Guid)r["Id"];
                    customerName = $"{r["Individual_FirstName"]} {r["Individual_LastName"]}";
                }

                // Get all accounts (Savings + Loan)
                var cmd = new SqlCommand(@"
            SELECT
    ca.Id AS CustomerAccountId,
    ca.SequentialId AS AccountNo,
    ca.CustomerAccountType_ProductCode AS ProductCode,
    ca.CustomerAccountType_TargetProductId AS ProductId,
    ca.BranchId,
    COALESCE(sp.Description, lp.Description) AS ProductDescription,
    COALESCE(sp.ChartOfAccountId, lp.ChartOfAccountId) AS ChartOfAccountId,
    sp.MaximumAllowedDeposit,
    sp.MaximumAllowedWithdrawal,
    sp.IsLocked AS SavingsIsLocked,
    lp.IsLocked AS LoanIsLocked,
    CASE ca.CustomerAccountType_ProductCode
        WHEN 1 THEN 'Savings'
        WHEN 2 THEN 'Loan'
        WHEN 3 THEN 'Investment'
        WHEN 4 THEN 'Wallet'
    END AS AccountType
FROM swiftFin_CustomerAccounts ca
LEFT JOIN swiftFin_SavingsProducts sp
  ON sp.Id = ca.CustomerAccountType_TargetProductId AND ca.CustomerAccountType_ProductCode = 1
LEFT JOIN swiftFin_LoanProducts lp
  ON lp.Id = ca.CustomerAccountType_TargetProductId AND ca.CustomerAccountType_ProductCode = 2
WHERE ca.CustomerId = @cid
  AND ca.Status = 1
", con);

                cmd.Parameters.AddWithValue("@cid", customerId);

                var list = new List<object>();

                using (var r = await cmd.ExecuteReaderAsync())
                {
                    while (r.Read())
                    {
                        var acctId = (Guid)r["CustomerAccountId"];
                        var prodId = (Guid)r["ProductId"];
                        ProductCode prodCode = (ProductCode)(byte)r["ProductCode"];

                        var bal = await GetBalance(con, null, acctId, prodId, (int)prodCode);
                        list.Add(new
                        {
                            CustomerName = customerName,
                            AccountNo = r["AccountNo"].ToString(),
                            AccountType = r["AccountType"].ToString(),
                            ProductCode = prodCode,
                            ProductDescription = r["ProductDescription"].ToString(),
                            Balance = bal,

                            // Dynamic permissions
                            CanDeposit = prodCode == ProductCode.Savings
             //&& r["MaximumAllowedDeposit"] != DBNull.Value
             //&& Convert.ToDecimal(r["MaximumAllowedDeposit"]) > 0
             && !(r["SavingsIsLocked"] != DBNull.Value && (bool)r["SavingsIsLocked"]),

                            CanWithdraw = prodCode == ProductCode.Savings
              //&& r["MaximumAllowedWithdrawal"] != DBNull.Value
              //&& Convert.ToDecimal(r["MaximumAllowedWithdrawal"]) > 0
              && !(r["SavingsIsLocked"] != DBNull.Value && (bool)r["SavingsIsLocked"]),

                            CanRepayLoan = prodCode == ProductCode.Loan
               && !(r["LoanIsLocked"] != DBNull.Value && (bool)r["LoanIsLocked"])
                        });


                    }
                }

                return Ok(list);
            }
        }



        // =========================================================
        // ================= POSTING LOGIC =========================
        // =========================================================

        private async Task PostDeposit(SqlConnection c, SqlTransaction t, MWalletTxnDto d, dynamic src, Guid j)
        {
            await Entry(c, t, j, src.SavingsCOA, src.ClearingCOA, src.CustomerAccountId, d.Amount);
            await Entry(c, t, j, src.ClearingCOA, src.SavingsCOA, src.CustomerAccountId, -d.Amount);
        }

        private async Task PostWithdrawal(SqlConnection c, SqlTransaction t, MWalletTxnDto d, dynamic src, Guid j)
        {
            await Entry(c, t, j, src.ClearingCOA, src.SavingsCOA, src.CustomerAccountId, d.Amount);
            await Entry(c, t, j, src.SavingsCOA, src.ClearingCOA, src.CustomerAccountId, -d.Amount);
        }

        private async Task PostTransfer(SqlConnection c, SqlTransaction t, MWalletTxnDto d, dynamic src, dynamic dst, Guid j)
        {
            await Entry(c, t, j, dst.SavingsCOA, src.SavingsCOA, src.CustomerAccountId, -d.Amount);
            await Entry(c, t, j, dst.SavingsCOA, src.SavingsCOA, dst.CustomerAccountId, d.Amount);
        }

        private async Task PostLoanRepayment(SqlConnection c, SqlTransaction t, MWalletTxnDto d, dynamic src, Guid j)
        {
            var loanCoa = await GetLoanCOA(c, t, d.LoanCustomerAccountId.Value);

            await Entry(c, t, j, loanCoa, src.SavingsCOA, src.CustomerAccountId, d.Amount);
            await Entry(c, t, j, src.SavingsCOA, loanCoa, src.CustomerAccountId, -d.Amount);
        }

        // =========================================================
        // ================= DATA ACCESS ===========================
        // =========================================================

        private async Task<bool> IsDuplicate(SqlConnection c, SqlTransaction t, string sid)
        {
            var cmd = new SqlCommand(
                "SELECT 1 FROM MTransaction WHERE SessionID=@s AND Posted=1", c, t);
            cmd.Parameters.AddWithValue("@s", sid);
            return (await cmd.ExecuteScalarAsync()) != null;
        }

        private async Task<dynamic> GetWallet(SqlConnection c, SqlTransaction t, string acc)
        {
            var cmd = new SqlCommand(@"
                SELECT ca.Id CustomerAccountId,
                       ca.BranchId,
                       ca.CustomerAccountType_TargetProductId ProductId,
                       sp.ChartOfAccountId SavingsCOA
                FROM swiftFin_CustomerAccounts ca
                JOIN swiftFin_SavingsProducts sp
                  ON sp.Id = ca.CustomerAccountType_TargetProductId
                WHERE ca.CustomerAccountType_ProductCode = 4
                  AND ca.SequentialId = @acc", c, t);

            cmd.Parameters.AddWithValue("@acc", acc);

            using (var r = await cmd.ExecuteReaderAsync())
            {
                if (!r.Read()) return null;

                return new
                {
                    CustomerAccountId = (Guid)r["CustomerAccountId"],
                    BranchId = (Guid)r["BranchId"],
                    ProductId = (Guid)r["ProductId"],
                    SavingsCOA = (Guid)r["SavingsCOA"],
                    ClearingCOA = new Guid("B21C7C83-F484-4828-A92E-0A1FEA8DE84A") // system clearing
                };
            }
        }

        private async Task<decimal> GetBalance(SqlConnection c, SqlTransaction t,
            Guid acctId, Guid prodId, int prodCode)
        {
            var cmd = new SqlCommand("sp_CustomerAccountBalance", c, t);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@CustomerAccountID", acctId);
            cmd.Parameters.AddWithValue("@Type", 1);
            cmd.Parameters.AddWithValue("@considerMaturityPeriodForInvestmentAccounts", 0);
            cmd.Parameters.AddWithValue("@CutoffDate", DateTime.Now);
            cmd.Parameters.AddWithValue("@CustomerAccountType_TargetProductId", prodId);
            cmd.Parameters.AddWithValue("@CustomerAccountType_ProductCode", prodCode);

            return Convert.ToDecimal(await cmd.ExecuteScalarAsync());
        }

        private async Task<Guid> InsertMTransaction(SqlConnection c, SqlTransaction t, MWalletTxnDto d)
        {
            var id = Guid.NewGuid();

            var cmd = new SqlCommand(@"
                INSERT INTO MTransaction
                (ID, TransactionDate, SessionID, AccountNo, DestinationAccount, DocumentNo,
                 TransactionType, Telephone, Posted, Status, Amount, Charge,
                 Description, ApplicationType)
                VALUES
                (@id, GETDATE(), @sid, @acc, @dst, @doc,
                 @type, @tel, 0, 0, @amt, @chg,
                 @desc, @app)", c, t);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@sid", d.SessionID);
            cmd.Parameters.AddWithValue("@acc", d.AccountNo);
            cmd.Parameters.AddWithValue("@dst", (object)d.DestinationAccount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@doc", d.DocumentNo ?? d.SessionID);
            cmd.Parameters.AddWithValue("@type", d.TransactionType);
            cmd.Parameters.AddWithValue("@tel", d.Telephone ?? "");
            cmd.Parameters.AddWithValue("@amt", d.Amount);
            cmd.Parameters.AddWithValue("@chg", d.Charge);
            cmd.Parameters.AddWithValue("@desc", d.Description ?? "");
            cmd.Parameters.AddWithValue("@app", d.ApplicationType ?? "MWallet");

            await cmd.ExecuteNonQueryAsync();
            return id;
        }

        private async Task<Guid> InsertJournal(SqlConnection c, SqlTransaction t, MWalletTxnDto d, Guid branch)
        {
            var id = Guid.NewGuid();

            var cmd = new SqlCommand(@"
                INSERT INTO swiftFin_Journals
                (Id, BranchId, TotalValue, PrimaryDescription, Reference,
                 TransactionCode, ValueDate, CreatedDate)
                VALUES
                (@id, @b, @v, @p, @r, 0, CAST(GETDATE() AS DATE), GETDATE())", c, t);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@b", branch);
            cmd.Parameters.AddWithValue("@v", d.Amount);
            cmd.Parameters.AddWithValue("@p", "M-Wallet " + d.TransactionType);
            cmd.Parameters.AddWithValue("@r", d.DocumentNo ?? d.SessionID);

            await cmd.ExecuteNonQueryAsync();
            return id;
        }

        private async Task Entry(SqlConnection c, SqlTransaction t, Guid j,
            Guid coa, Guid contra, Guid acct, decimal amt)
        {
            var cmd = new SqlCommand(@"
                INSERT INTO swiftFin_JournalEntries
                (Id, JournalId, ChartOfAccountId, ContraChartOfAccountId,
                 CustomerAccountId, Amount, ValueDate, CreatedDate)
                VALUES
                (NEWID(), @j, @c, @cc, @a, @amt, CAST(GETDATE() AS DATE), GETDATE())", c, t);

            cmd.Parameters.AddWithValue("@j", j);
            cmd.Parameters.AddWithValue("@c", coa);
            cmd.Parameters.AddWithValue("@cc", contra);
            cmd.Parameters.AddWithValue("@a", acct);
            cmd.Parameters.AddWithValue("@amt", amt);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<Guid> GetLoanCOA(SqlConnection c, SqlTransaction t, Guid loanAcct)
        {
            var cmd = new SqlCommand(@"
                SELECT lp.ChartOfAccountId
                FROM swiftFin_CustomerAccounts ca
                JOIN SwiftFin_LoanProducts lp
                  ON lp.Id = ca.CustomerAccountType_TargetProductId
                WHERE ca.Id = @id", c, t);

            cmd.Parameters.AddWithValue("@id", loanAcct);
            return (Guid)await cmd.ExecuteScalarAsync();
        }

        private async Task<Guid> GetCustomerByMemberNo(SqlConnection c, string memberNo)
        {
            var cmd = new SqlCommand(@"
                SELECT Id FROM swiftFin_Customers
                WHERE Reference2 = @m
                  AND RecordStatus = 1", c);

            cmd.Parameters.AddWithValue("@m", memberNo);

            var res = await cmd.ExecuteScalarAsync();
            return res == null ? Guid.Empty : (Guid)res;
        }

        private async Task FinalizeMTransaction(SqlConnection c, SqlTransaction t, Guid id, Guid j)
        {
            var cmd = new SqlCommand(@"
                UPDATE MTransaction
                   SET Posted=1,
                       Status=2,
                       DatePosted=GETDATE(),
                       Comments='Posted: '+CAST(@j AS NVARCHAR(50))
                 WHERE ID=@id", c, t);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@j", j);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
