using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using Newtonsoft.Json;
using TestApis.Helpers;

namespace TestApis.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [AllowAnonymous]
    [RoutePrefix("api/ussd")]
    public class USSDController : ApiController
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;

        // ─────────────────────────────────────────────────────────────────────
        // STATUS CODES
        // ─────────────────────────────────────────────────────────────────────
        private const string SC_OK = "000";
        private const string SC_ACCOUNT_NOT_FOUND = "001";
        private const string SC_INVALID_REQUEST = "002";
        private const string SC_NO_RECORDS = "003";
        private const string SC_MEMBER_NOT_FOUND = "3005";
        private const string SC_SYSTEM_ERROR = "999";
        private const string SC_NOT_ELIGIBLE = "004";
        private const string SC_OUTSTANDING_BALANCE = "005";

        // ─────────────────────────────────────────────────────────────────────
        // CASH ADVANCE CONSTANTS
        // ─────────────────────────────────────────────────────────────────────
        private const decimal MAX_ADVANCE_AMOUNT = 5000.00m;
        private const decimal FEE_PERCENTAGE = 0.05m; // 5%
        private const int MIN_MEMBERSHIP_MONTHS = 3;
        private const string DEFAULT_BRANCH_ID = "5C84C824-0CE3-455B-A5C5-994E2BFBA380";
        private const string B2C_DISBURSE_URL = "https://b2cmanager.co.ke/SWIZZMOB/MPESA2Account";
        private const string ORG_CODE = "88";
        private const bool B2C_LIVE_MODE = false; // flip to true when ready to go live

        // ─────────────────────────────────────────────────────────────────────
        // PHONE HELPERS
        // ─────────────────────────────────────────────────────────────────────
        private string NormalisePhone(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            raw = raw.Trim().Replace(" ", "").Replace("-", "");
            if (raw.StartsWith("+")) raw = raw.Substring(1);
            if (raw.StartsWith("254") && raw.Length == 12) raw = "0" + raw.Substring(3);
            return raw;
        }

        private (string local, string international, string withPlus) GetPhoneVariants(string phone)
        {
            string local = phone;
            string international = phone.StartsWith("0") ? "254" + phone.Substring(1) : phone;
            string withPlus = phone.StartsWith("0") ? "+254" + phone.Substring(1) : "+" + phone;
            return (local, international, withPlus);
        }

        // ─────────────────────────────────────────────────────────────────────
        // SHARED: resolve member from phone
        // ─────────────────────────────────────────────────────────────────────
        private async Task<MemberRecord> FindMemberByPhone(SqlConnection conn, string phone)
        {
            var v = GetPhoneVariants(NormalisePhone(phone));
            using (var cmd = new SqlCommand(@"
SELECT TOP 1
    Id, Reference2,
    Individual_FirstName, Individual_LastName,
    Individual_IdentityCardNumber,
    Address_MobileLine, RegistrationDate, CreatedDate
FROM swiftFin_Customers
WHERE (Address_MobileLine = @L OR Address_MobileLine = @I OR Address_MobileLine = @P)
  AND RecordStatus IN (1, 2)
ORDER BY CreatedDate DESC", conn))
            {
                cmd.Parameters.Add("@L", SqlDbType.NVarChar, 20).Value = v.local;
                cmd.Parameters.Add("@I", SqlDbType.NVarChar, 20).Value = v.international;
                cmd.Parameters.Add("@P", SqlDbType.NVarChar, 20).Value = v.withPlus;
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    if (!await r.ReadAsync()) return null;
                    return new MemberRecord
                    {
                        CustomerId = (Guid)r["Id"],
                        MemberNo = r["Reference2"]?.ToString(),
                        FirstName = r["Individual_FirstName"]?.ToString()?.Trim(),
                        LastName = r["Individual_LastName"]?.ToString()?.Trim(),
                        IDNumber = r["Individual_IdentityCardNumber"]?.ToString(),
                        Phone = r["Address_MobileLine"]?.ToString(),
                        RegistrationDate = r["RegistrationDate"] != DBNull.Value
                                                ? (DateTime?)Convert.ToDateTime(r["RegistrationDate"]) : null,
                        CreatedDate = Convert.ToDateTime(r["CreatedDate"])
                    };
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 1. GET MEMBER PROFILE
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("GetMemberProfile")]
        public async Task<IHttpActionResult> GetMemberProfile([FromBody] UssdPhoneRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST", Accounts = new List<object>() });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var member = await FindMemberByPhone(conn, request.PhoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_ACCOUNT_NOT_FOUND, StatusDescription = "ACCOUNTNOTFOUND", Accounts = new List<object>() });

                    var accounts = await GetBOSAAccountList(conn, member.CustomerId,
                        $"{member.FirstName} {member.LastName}".Trim());

                    if (!accounts.Any())
                        return Ok(new { StatusCode = SC_NO_RECORDS, StatusDescription = "NOACCOUNTSFOUND", Accounts = new List<object>() });

                    return Ok(new { StatusCode = SC_OK, StatusDescription = "OK", Accounts = accounts });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "SYSTEMERROR", Accounts = new List<object>() });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. GET WALLET ACCOUNTS
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("GetWalletAccounts")]
        public async Task<IHttpActionResult> GetWalletAccounts([FromBody] UssdPhoneRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST", Accounts = new List<object>() });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var member = await FindMemberByPhone(conn, request.PhoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_MEMBER_NOT_FOUND, StatusDescription = "MEMBERNOTFOUND", Accounts = new List<object>() });

                    var accounts = new List<object>();
                    using (var cmd = new SqlCommand(@"
SELECT ca.Id AS AccountId, sp.Description AS AccountName
FROM swiftFin_CustomerAccounts ca
INNER JOIN swiftFin_SavingsProducts sp ON sp.Id = ca.CustomerAccountType_TargetProductId
WHERE ca.CustomerId                           = @CustomerId
  AND ca.CustomerAccountType_ProductCode      = 4
  AND ca.CustomerAccountType_TargetProductCode = 17", conn))
                    {
                        cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = member.CustomerId;
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            while (await r.ReadAsync())
                            {
                                accounts.Add(new
                                {
                                    AccountNo = r["AccountId"].ToString(),
                                    AccountName = "M-WALLET"
                                });
                            }
                        }
                    }

                    if (!accounts.Any())
                        return Ok(new { StatusCode = SC_NO_RECORDS, StatusDescription = "NOWALLET", Accounts = accounts });

                    return Ok(new { StatusCode = SC_OK, StatusDescription = "OK", Accounts = accounts });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "SYSTEMERROR", Accounts = new List<object>() });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3. GET WALLET BALANCE
        // Now reflects the TRUE net balance — can be negative if member owes
        // money from a cash advance (no ABS() — raw signed sum)
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("GetWalletBalance")]
        public async Task<IHttpActionResult> GetWalletBalance([FromBody] UssdPhoneRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST", AccountBalance = "0" });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var member = await FindMemberByPhone(conn, request.PhoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_MEMBER_NOT_FOUND, StatusDescription = "MEMBERNOTFOUND", AccountBalance = "0" });

                    decimal balance = await GetMWalletBalance(conn, member.CustomerId);

                    return Ok(new
                    {
                        StatusCode = SC_OK,
                        StatusDescription = "OK",
                        AccountBalance = balance.ToString("F2") // can be negative if member owes
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "SYSTEMERROR", AccountBalance = "0" });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 4. ENQUIRE WALLET BALANCE — SMS shows owed amount if negative
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("EnquireWalletBalance")]
        public async Task<IHttpActionResult> EnquireWalletBalance([FromBody] UssdDocumentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST" });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var member = await FindMemberByPhone(conn, request.PhoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_MEMBER_NOT_FOUND, StatusDescription = "MEMBERNOTFOUND" });

                    decimal balance = await GetMWalletBalance(conn, member.CustomerId);
                    string fullName = $"{member.FirstName} {member.LastName}".Trim();

                    string smsMsg;
                    if (balance < 0)
                    {
                        smsMsg =
                            $"Dear {fullName}, your M-WALLET balance is KES {balance:N2}. " +
                            $"You owe KES {Math.Abs(balance):N2} from a cash advance. " +
                            $"Please repay to restore your balance. RUBANI SACCO.";
                    }
                    else
                    {
                        smsMsg =
                            $"Dear {fullName}, your M-WALLET balance is KES {balance:N2}. " +
                            $"Thank you for using RUBANI SACCO Mobile Banking.";
                    }

                    await SmsHelper.SendMessageAsync(request.PhoneNumber, smsMsg);

                    return Ok(new
                    {
                        StatusCode = SC_OK,
                        StatusDescription = "Balance enquiry processed. SMS sent.",
                        DocumentNo = request.DocumentNo,
                        AccountNo = request.AccountNo
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "SYSTEMERROR" });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 5. ENQUIRE BOSA BALANCE
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("EnquireBOSABalance")]
        public async Task<IHttpActionResult> EnquireBOSABalance([FromBody] UssdDocumentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST" });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var member = await FindMemberByPhone(conn, request.PhoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_MEMBER_NOT_FOUND, StatusDescription = "MEMBERNOTFOUND" });

                    var accountBalances = await GetBOSABalances(conn, member.CustomerId);

                    string smsMsg = accountBalances.Any()
                        ? string.Join("\n", accountBalances.Select(b => b.Name + ": KES " + b.Balance.ToString("N0")))
                          + "\nRUBANI SACCO"
                        : "No savings balances found.\nRUBANI SACCO";

                    await SmsHelper.SendMessageAsync(request.PhoneNumber, smsMsg);

                    return Ok(new { StatusCode = SC_OK, StatusDescription = "Balance enquiry processed. SMS sent." });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "SYSTEMERROR" });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 6. ENQUIRE LOAN BALANCES
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("EnquireLoanBalances")]
        public async Task<IHttpActionResult> EnquireLoanBalances([FromBody] UssdDocumentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST" });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var member = await FindMemberByPhone(conn, request.PhoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_MEMBER_NOT_FOUND, StatusDescription = "MEMBERNOTFOUND" });

                    var loans = await GetOutstandingLoansList(conn, member.CustomerId);

                    string smsMsg = loans.Any()
                        ? string.Join("\n", loans.Select(l =>
                            l.LoanName + " - " + decimal.Parse(l.BalanceAmount).ToString("N0")))
                          + "\nRUBANI SACCO"
                        : "You have no outstanding loans.\nRUBANI SACCO";

                    await SmsHelper.SendMessageAsync(request.PhoneNumber, smsMsg);

                    return Ok(new { StatusCode = SC_OK, StatusDescription = "Loan balances processed. SMS sent." });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "SYSTEMERROR" });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 7. BOSA MINI STATEMENT
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("GetBOSAMiniStatement")]
        public async Task<IHttpActionResult> GetBOSAMiniStatement([FromBody] UssdDocumentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST" });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var member = await FindMemberByPhone(conn, request.PhoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_MEMBER_NOT_FOUND, StatusDescription = "MEMBERNOTFOUND" });

                    var lines = new List<string>();
                    using (var cmd = new SqlCommand(@"
SELECT TOP 5
    CONVERT(VARCHAR(6), j.ValueDate, 6)     AS ShortDate,
    ISNULL(sp.Description, 'Deposits')      AS ProductName,
    ABS(je.Amount)                          AS Amount
FROM swiftFin_JournalEntries je
INNER JOIN swiftFin_Journals j          ON j.Id  = je.JournalId
INNER JOIN swiftFin_CustomerAccounts ca ON ca.Id = je.CustomerAccountId
LEFT JOIN  swiftFin_SavingsProducts sp  ON sp.Id = ca.CustomerAccountType_TargetProductId
WHERE ca.CustomerId = @CustomerId
  AND ca.CustomerAccountType_ProductCode IN (1, 2, 3, 4)
  AND je.Amount > 0
ORDER BY j.ValueDate DESC, j.CreatedDate DESC", conn))
                    {
                        cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = member.CustomerId;
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            while (await r.ReadAsync())
                                lines.Add($"{r["ShortDate"]} {r["ProductName"]} {Convert.ToDecimal(r["Amount"]):N0}");
                        }
                    }

                    string smsMsg = lines.Any()
                        ? string.Join("\n", lines) + "\nRUBANI SACCO"
                        : "No recent transactions found.\nRUBANI SACCO";

                    await SmsHelper.SendMessageAsync(request.PhoneNumber, smsMsg);

                    return Ok(new { StatusCode = SC_OK, StatusDescription = "Mini statement processed. SMS sent." });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "SYSTEMERROR" });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 8. M-WALLET MINI STATEMENT — now also shows advances/repayments
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("GetWalletMiniStatement")]
        public async Task<IHttpActionResult> GetWalletMiniStatement([FromBody] UssdDocumentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST" });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var member = await FindMemberByPhone(conn, request.PhoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_MEMBER_NOT_FOUND, StatusDescription = "MEMBERNOTFOUND" });

                    var lines = new List<string>();
                    using (var cmd = new SqlCommand(@"
SELECT TOP 5
    CONVERT(VARCHAR(6), j.ValueDate, 6)  AS ShortDate,
    j.PrimaryDescription                 AS Description,
    je.Amount                            AS Amount
FROM swiftFin_JournalEntries je
INNER JOIN swiftFin_Journals j          ON j.Id  = je.JournalId
INNER JOIN swiftFin_CustomerAccounts ca ON ca.Id = je.CustomerAccountId
WHERE ca.CustomerId                           = @CustomerId
  AND ca.CustomerAccountType_ProductCode      = 4
  AND ca.CustomerAccountType_TargetProductCode = 17
ORDER BY j.ValueDate DESC, j.CreatedDate DESC", conn))
                    {
                        cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = member.CustomerId;
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            while (await r.ReadAsync())
                            {
                                decimal amt = Convert.ToDecimal(r["Amount"]);
                                // POSITIVE amount = deposit/repayment, NEGATIVE = advance/withdrawal
                                string sign = amt < 0 ? "-" : "+";
                                lines.Add($"{r["ShortDate"]} {r["Description"]} {sign}{Math.Abs(amt):N0}");
                            }
                        }
                    }

                    string smsMsg = lines.Any()
                        ? string.Join("\n", lines) + "\nRUBANI SACCO"
                        : "No recent M-WALLET transactions.\nRUBANI SACCO";

                    await SmsHelper.SendMessageAsync(request.PhoneNumber, smsMsg);

                    return Ok(new { StatusCode = SC_OK, StatusDescription = "Mini statement processed. SMS sent." });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "SYSTEMERROR" });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 9. GET OUTSTANDING LOANS
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("GetOutstandingLoans")]
        public async Task<IHttpActionResult> GetOutstandingLoans([FromBody] UssdPhoneRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST", LoanBalances = new List<object>() });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var member = await FindMemberByPhone(conn, request.PhoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_MEMBER_NOT_FOUND, StatusDescription = "MEMBERNOTFOUND", LoanBalances = new List<object>() });

                    var loans = await GetOutstandingLoansList(conn, member.CustomerId);

                    if (!loans.Any())
                        return Ok(new { StatusCode = SC_NO_RECORDS, StatusDescription = "NOOUTSTANDINGLOANS", LoanBalances = loans });

                    return Ok(new { StatusCode = SC_OK, StatusDescription = "OK", LoanBalances = loans });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "SYSTEMERROR", LoanBalances = new List<object>() });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 10. REQUEST CASH ADVANCE (against M-WALLET)
        // Eligibility: 3+ months membership AND no outstanding negative balance.
        // Member receives (amount - 5% fee); full amount is what's owed
        // (deducted by posting a single negative journal entry for the full
        // requested amount — the fee is simply not disbursed, same net effect
        // as "owes full amount, receives less").
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("RequestCashAdvance")]
        public async Task<IHttpActionResult> RequestCashAdvance([FromBody] CashAdvanceRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST" });

            if (request.AmountRequested <= 0)
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDAMOUNT" });

            if (request.AmountRequested > MAX_ADVANCE_AMOUNT)
                return Ok(new
                {
                    StatusCode = SC_INVALID_REQUEST,
                    StatusDescription = $"MAXIMUM AMOUNT IS KES {MAX_ADVANCE_AMOUNT:N0}"
                });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var member = await FindMemberByPhone(conn, request.PhoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_MEMBER_NOT_FOUND, StatusDescription = "MEMBERNOTFOUND" });

                    // ── Eligibility 1: 3+ months membership ─────────────────
                    DateTime membershipStart = member.RegistrationDate ?? member.CreatedDate;
                    int months = ((DateTime.Now.Year - membershipStart.Year) * 12)
                               + (DateTime.Now.Month - membershipStart.Month);
                    if (DateTime.Now.Day < membershipStart.Day) months--;

                    if (months < MIN_MEMBERSHIP_MONTHS)
                        return Ok(new
                        {
                            StatusCode = SC_NOT_ELIGIBLE,
                            StatusDescription =
                                $"NEED {MIN_MEMBERSHIP_MONTHS} MONTHS MEMBERSHIP. CURRENT: {months}"
                        });

                    // ── Get M-WALLET account + CoA ───────────────────────────
                    Guid walletAccountId = Guid.Empty;
                    Guid walletCoAId = Guid.Empty;

                    using (var cmd = new SqlCommand(@"
SELECT ca.Id AS AccountId, sp.ChartOfAccountId AS WalletCoA
FROM swiftFin_CustomerAccounts ca
INNER JOIN swiftFin_SavingsProducts sp ON sp.Id = ca.CustomerAccountType_TargetProductId
WHERE ca.CustomerId                           = @CustomerId
  AND ca.CustomerAccountType_ProductCode      = 4
  AND ca.CustomerAccountType_TargetProductCode = 17", conn))
                    {
                        cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = member.CustomerId;
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            if (!await r.ReadAsync())
                                return Ok(new { StatusCode = SC_NO_RECORDS, StatusDescription = "NOWALLET" });

                            walletAccountId = r.GetGuid(r.GetOrdinal("AccountId"));
                            walletCoAId = r.GetGuid(r.GetOrdinal("WalletCoA"));
                        }
                    }

                    // ── Eligibility 2: no existing negative (owed) balance ──
                    decimal currentBalance = await GetMWalletBalance(conn, member.CustomerId);

                    if (currentBalance < 0)
                        return Ok(new
                        {
                            StatusCode = SC_OUTSTANDING_BALANCE,
                            StatusDescription =
                                $"YOU OWE KES {Math.Abs(currentBalance):N2}. REPAY BEFORE REQUESTING AGAIN."
                        });

                    // ── Calculate fee + disbursement ────────────────────────
                    decimal requestedAmount = request.AmountRequested;
                    decimal fee = Math.Round(requestedAmount * FEE_PERCENTAGE, 2);
                    decimal amountToDisburse = requestedAmount - fee;
                    decimal amountOwed = requestedAmount; // wallet goes negative by this much

                    if (string.IsNullOrWhiteSpace(member.Phone))
                        return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "NOPHONENUMBER" });

                    string sessionId = Guid.NewGuid().ToString("N");

                    // ── Call B2C disbursement (stubbed) ─────────────────────
                    var disburseResult = await CallB2CDisbursementApi(sessionId, member.Phone, amountToDisburse);

                    if (!disburseResult.Success)
                        return Ok(new
                        {
                            StatusCode = SC_SYSTEM_ERROR,
                            StatusDescription = $"DISBURSEMENT FAILED: {disburseResult.Message}",
                            sessionId
                        });

                    // ── Post journal: wallet balance reduces by amountOwed ──
                    bool posted = await PostCashAdvanceJournal(
                        conn, walletAccountId, walletCoAId, amountOwed, sessionId);

                    if (!posted)
                        return Ok(new
                        {
                            StatusCode = SC_SYSTEM_ERROR,
                            StatusDescription = "DISBURSED BUT JOURNAL POSTING FAILED — CONTACT SUPPORT",
                            sessionId,
                            mpesaReference = disburseResult.MpesaReference
                        });

                    // ── SMS confirmation ─────────────────────────────────────
                    try
                    {
                        string fullName = $"{member.FirstName} {member.LastName}".Trim();
                        string msg =
                            $"Dear {fullName}, your cash advance of KES {amountToDisburse:N2} " +
                            $"(after {FEE_PERCENTAGE * 100:N0}% fee of KES {fee:N2}) has been sent to " +
                            $"{member.Phone}. Your M-WALLET balance is now KES {(currentBalance - amountOwed):N2}. " +
                            $"Ref: {sessionId}.";
                        await SmsHelper.SendMessageAsync(member.Phone, msg);
                    }
                    catch { }

                    return Ok(new
                    {
                        StatusCode = SC_OK,
                        StatusDescription = "CASH ADVANCE DISBURSED",
                        sessionId,
                        amountRequested = requestedAmount,
                        fee,
                        amountDisbursed = amountToDisburse,
                        newBalance = currentBalance - amountOwed,
                        mpesaReference = disburseResult.MpesaReference
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "SYSTEMERROR" });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 11. REPAY CASH ADVANCE — brings M-WALLET balance back up
        // Call this from your repayment confirmation flow (checkoff, paybill, etc.)
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("RepayCashAdvance")]
        public async Task<IHttpActionResult> RepayCashAdvance([FromBody] CashAdvanceRepaymentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST" });

            if (request.AmountPaid <= 0)
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDAMOUNT" });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var member = await FindMemberByPhone(conn, request.PhoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_MEMBER_NOT_FOUND, StatusDescription = "MEMBERNOTFOUND" });

                    Guid walletAccountId = Guid.Empty;
                    Guid walletCoAId = Guid.Empty;

                    using (var cmd = new SqlCommand(@"
SELECT ca.Id AS AccountId, sp.ChartOfAccountId AS WalletCoA
FROM swiftFin_CustomerAccounts ca
INNER JOIN swiftFin_SavingsProducts sp ON sp.Id = ca.CustomerAccountType_TargetProductId
WHERE ca.CustomerId                           = @CustomerId
  AND ca.CustomerAccountType_ProductCode      = 4
  AND ca.CustomerAccountType_TargetProductCode = 17", conn))
                    {
                        cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = member.CustomerId;
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            if (!await r.ReadAsync())
                                return Ok(new { StatusCode = SC_NO_RECORDS, StatusDescription = "NOWALLET" });

                            walletAccountId = r.GetGuid(r.GetOrdinal("AccountId"));
                            walletCoAId = r.GetGuid(r.GetOrdinal("WalletCoA"));
                        }
                    }

                    Guid postingPeriodId = Guid.Empty;
                    using (var cmd = new SqlCommand(@"
SELECT TOP 1 Id FROM swiftFin_PostingPeriods
WHERE GETDATE() BETWEEN Duration_StartDate AND Duration_EndDate AND IsActive = 1", conn))
                    {
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null) postingPeriodId = (Guid)result;
                    }

                    if (postingPeriodId == Guid.Empty)
                        return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "NO ACTIVE POSTING PERIOD" });

                    Guid journalId = Guid.NewGuid();
                    DateTime now = DateTime.Now;

                    using (var cmd = new SqlCommand(@"
INSERT INTO swiftFin_Journals
    (Id, PostingPeriodId, BranchId, TotalValue, PrimaryDescription,
     SecondaryDescription, Reference, ValueDate, IsLocked,
     SequentialId, CreatedBy, CreatedDate)
VALUES
    (@Id, @PostingPeriodId, @BranchId, @TotalValue, 'M-Wallet Advance Repayment',
     'Repayment', @Reference, @ValueDate, 0,
     @SequentialId, 'System', @CreatedDate)", conn))
                    {
                        cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = journalId;
                        cmd.Parameters.Add("@PostingPeriodId", SqlDbType.UniqueIdentifier).Value = postingPeriodId;
                        cmd.Parameters.Add("@BranchId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(DEFAULT_BRANCH_ID);
                        cmd.Parameters.Add("@TotalValue", SqlDbType.Decimal).Value = request.AmountPaid;
                        cmd.Parameters.Add("@Reference", SqlDbType.NVarChar).Value = request.Reference ?? "Wallet Repayment";
                        cmd.Parameters.Add("@ValueDate", SqlDbType.Date).Value = now.Date;
                        cmd.Parameters.Add("@SequentialId", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                        cmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime2).Value = now;
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // POSITIVE entry brings wallet balance back up
                    using (var cmd = new SqlCommand(@"
INSERT INTO swiftFin_JournalEntries
    (Id, JournalId, ChartOfAccountId, ContraChartOfAccountId, CustomerAccountId,
     Amount, ValueDate, SequentialId, CreatedBy, CreatedDate)
VALUES
    (@Id, @JournalId, @ChartOfAccountId, @ContraChartOfAccountId, @CustomerAccountId,
     @Amount, @ValueDate, @SequentialId, 'System', @CreatedDate)", conn))
                    {
                        cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                        cmd.Parameters.Add("@JournalId", SqlDbType.UniqueIdentifier).Value = journalId;
                        cmd.Parameters.Add("@ChartOfAccountId", SqlDbType.UniqueIdentifier).Value = walletCoAId;
                        cmd.Parameters.Add("@ContraChartOfAccountId", SqlDbType.UniqueIdentifier).Value = walletCoAId;
                        cmd.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = walletAccountId;
                        cmd.Parameters.Add("@Amount", SqlDbType.Decimal).Value = request.AmountPaid; // POSITIVE
                        cmd.Parameters.Add("@ValueDate", SqlDbType.Date).Value = now.Date;
                        cmd.Parameters.Add("@SequentialId", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                        cmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime2).Value = now;
                        await cmd.ExecuteNonQueryAsync();
                    }

                    decimal newBalance = await GetMWalletBalance(conn, member.CustomerId);

                    return Ok(new
                    {
                        StatusCode = SC_OK,
                        StatusDescription = "REPAYMENT RECORDED",
                        amountPaid = request.AmountPaid,
                        newBalance
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "SYSTEMERROR" });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // EXISTING ENDPOINT
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("GetMemberInfo")]
        public async Task<IHttpActionResult> GetMemberInfo([FromBody] UssdPhoneRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST" });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var member = await FindMemberByPhone(conn, request.PhoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_MEMBER_NOT_FOUND, StatusDescription = "MEMBERNOTFOUND" });

                    return Ok(new
                    {
                        StatusCode = SC_OK,
                        StatusDescription = "OK",
                        MemberName = $"{member.FirstName} {member.LastName}".Trim(),
                        Phone = member.Phone,
                        MemberNumber = member.MemberNo
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "SYSTEMERROR" });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        // Sums all journal entries for the member's M-WALLET account
        // No ABS() — raw signed sum so it can go negative when member owes
        // money from a cash advance
        private async Task<decimal> GetMWalletBalance(SqlConnection conn, Guid customerId)
        {
            using (var cmd = new SqlCommand(@"
SELECT ISNULL(SUM(je.Amount), 0)
FROM swiftFin_JournalEntries je
INNER JOIN swiftFin_CustomerAccounts ca ON ca.Id = je.CustomerAccountId
WHERE ca.CustomerId                           = @CustomerId
  AND ca.CustomerAccountType_ProductCode      = 4
  AND ca.CustomerAccountType_TargetProductCode = 17", conn))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
            }
        }

        // Loads BOSA savings accounts
        private async Task<List<UssdAccount>> GetBOSAAccountList(
            SqlConnection conn, Guid customerId, string memberName)
        {
            var accounts = new List<UssdAccount>();

            using (var cmd = new SqlCommand(@"
SELECT ca.Id AS AccountId, sp.Description AS AccountName
FROM swiftFin_CustomerAccounts ca
INNER JOIN swiftFin_SavingsProducts sp ON sp.Id = ca.CustomerAccountType_TargetProductId
WHERE ca.CustomerId = @CustomerId
  AND ca.CustomerAccountType_ProductCode IN (1,2,3,4)
ORDER BY ca.CreatedDate", conn))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        accounts.Add(new UssdAccount
                        {
                            AccountNo = r["AccountId"].ToString(),
                            AccountName = r["AccountName"].ToString(),
                            AccountType = "BOSA",
                            AccountStatus = "Active"
                        });
                    }
                }
            }

            return accounts;
        }

        // Fetches BOSA balances
        private async Task<List<ProductBalance>> GetBOSABalances(SqlConnection conn, Guid customerId)
        {
            var result = new List<ProductBalance>();
            try
            {
                using (var cmd = new SqlCommand("sp_GenerateAllSharesStatement", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                    cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = DBNull.Value;
                    cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value = DBNull.Value;

                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        await r.NextResultAsync();
                        await r.NextResultAsync();

                        while (await r.ReadAsync())
                        {
                            string name = r["ProductName"]?.ToString() ?? "";
                            decimal balance = r["TotalContribution"] != DBNull.Value
                                              ? Convert.ToDecimal(r["TotalContribution"]) : 0m;
                            if (!string.IsNullOrWhiteSpace(name))
                                result.Add(new ProductBalance { Name = name, Balance = balance });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetBOSABalances error: {ex.Message}");
            }
            return result;
        }

        // Fetches active loans with balance > 0
        private async Task<List<UssdLoanBalance>> GetOutstandingLoansList(
            SqlConnection conn, Guid customerId)
        {
            var loans = new List<UssdLoanBalance>();
            using (var cmd = new SqlCommand(@"
SELECT
    CAST(lc.CaseNumber AS VARCHAR(20))          AS LoanNo,
    lp.Description                              AS LoanName,
    CAST(lc.TotalLoansBalance AS VARCHAR(30))   AS BalanceAmount
FROM swiftFin_LoanCases lc
INNER JOIN swiftFin_LoanProducts lp ON lp.Id = lc.LoanProductId
WHERE lc.CustomerId        = @CustomerId
  AND lc.TotalLoansBalance > 0
  AND lc.DisbursedDate     IS NOT NULL
ORDER BY lc.DisbursedDate", conn))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        loans.Add(new UssdLoanBalance
                        {
                            LoanNo = r["LoanNo"].ToString(),
                            LoanName = r["LoanName"].ToString(),
                            BalanceAmount = r["BalanceAmount"].ToString()
                        });
                    }
                }
            }
            return loans;
        }

        // ─────────────────────────────────────────────────────────────────────
        // B2C DISBURSEMENT — stubbed
        // ─────────────────────────────────────────────────────────────────────
        private async Task<B2CDisburseResult> CallB2CDisbursementApi(
            string sessionId, string phoneNumber, decimal amount)
        {
            if (!B2C_LIVE_MODE)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[B2C STUB] Would disburse KES {amount:N2} to {phoneNumber} (session: {sessionId})");

                await Task.Delay(100);

                return new B2CDisburseResult
                {
                    Success = true,
                    Message = "Stubbed — no live disbursement made.",
                    MpesaReference = $"STUB-{sessionId.Substring(0, 8).ToUpper()}"
                };
            }

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    var payload = new
                    {
                        sessionID = sessionId,
                        phonenumber = phoneNumber,
                        amount = amount.ToString("F2"),
                        accno = sessionId,
                        transactionType = "CashAdvance",
                        orgCode = ORG_CODE,
                        callbackURL = "" // TODO: set callback URL
                    };

                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(B2C_DISBURSE_URL, content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        return new B2CDisburseResult
                        {
                            Success = false,
                            Message = $"API returned {response.StatusCode}: {responseBody}"
                        };

                    dynamic result = JsonConvert.DeserializeObject(responseBody);

                    return new B2CDisburseResult
                    {
                        Success = true,
                        Message = "Disbursed successfully.",
                        MpesaReference = result?.TransactionReference?.ToString() ?? sessionId
                    };
                }
            }
            catch (Exception ex)
            {
                return new B2CDisburseResult
                {
                    Success = false,
                    Message = $"API call failed: {ex.Message}"
                };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Posts the advance journal: NEGATIVE entry on M-WALLET CoA
        // (drops the wallet balance by the full requested amount)
        // ─────────────────────────────────────────────────────────────────────
        private async Task<bool> PostCashAdvanceJournal(
            SqlConnection conn, Guid walletAccountId, Guid walletCoAId,
            decimal amountOwed, string sessionId)
        {
            try
            {
                Guid postingPeriodId = Guid.Empty;
                using (var cmd = new SqlCommand(@"
SELECT TOP 1 Id FROM swiftFin_PostingPeriods
WHERE GETDATE() BETWEEN Duration_StartDate AND Duration_EndDate AND IsActive = 1", conn))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null) postingPeriodId = (Guid)result;
                }

                if (postingPeriodId == Guid.Empty) return false;

                Guid journalId = Guid.NewGuid();
                DateTime now = DateTime.Now;

                using (var cmd = new SqlCommand(@"
INSERT INTO swiftFin_Journals
    (Id, PostingPeriodId, BranchId, TotalValue, PrimaryDescription,
     SecondaryDescription, Reference, ValueDate, IsLocked,
     SequentialId, CreatedBy, CreatedDate)
VALUES
    (@Id, @PostingPeriodId, @BranchId, @TotalValue, 'M-Wallet Cash Advance',
     'B2C Disbursement', @Reference, @ValueDate, 0,
     @SequentialId, 'System', @CreatedDate)", conn))
                {
                    cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = journalId;
                    cmd.Parameters.Add("@PostingPeriodId", SqlDbType.UniqueIdentifier).Value = postingPeriodId;
                    cmd.Parameters.Add("@BranchId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(DEFAULT_BRANCH_ID);
                    cmd.Parameters.Add("@TotalValue", SqlDbType.Decimal).Value = amountOwed;
                    cmd.Parameters.Add("@Reference", SqlDbType.NVarChar).Value = sessionId;
                    cmd.Parameters.Add("@ValueDate", SqlDbType.Date).Value = now.Date;
                    cmd.Parameters.Add("@SequentialId", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                    cmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime2).Value = now;
                    await cmd.ExecuteNonQueryAsync();
                }

                // NEGATIVE entry — wallet balance drops by full amount owed
                using (var cmd = new SqlCommand(@"
INSERT INTO swiftFin_JournalEntries
    (Id, JournalId, ChartOfAccountId, ContraChartOfAccountId, CustomerAccountId,
     Amount, ValueDate, SequentialId, CreatedBy, CreatedDate)
VALUES
    (@Id, @JournalId, @ChartOfAccountId, @ContraChartOfAccountId, @CustomerAccountId,
     @Amount, @ValueDate, @SequentialId, 'System', @CreatedDate)", conn))
                {
                    cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                    cmd.Parameters.Add("@JournalId", SqlDbType.UniqueIdentifier).Value = journalId;
                    cmd.Parameters.Add("@ChartOfAccountId", SqlDbType.UniqueIdentifier).Value = walletCoAId;
                    cmd.Parameters.Add("@ContraChartOfAccountId", SqlDbType.UniqueIdentifier).Value = walletCoAId;
                    cmd.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = walletAccountId;
                    cmd.Parameters.Add("@Amount", SqlDbType.Decimal).Value = -amountOwed; // NEGATIVE
                    cmd.Parameters.Add("@ValueDate", SqlDbType.Date).Value = now.Date;
                    cmd.Parameters.Add("@SequentialId", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                    cmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime2).Value = now;
                    await cmd.ExecuteNonQueryAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PostCashAdvanceJournal error: {ex.Message}");
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // INSTANT LOAN CONSTANTS
        // ─────────────────────────────────────────────────────────────────────────────
        private static readonly Guid INSTANT_LOAN_PRODUCT_ID = Guid.Parse("BBDB34E3-2B6E-F111-B6B3-80CE62222714"); // from SQL above
        private static readonly Guid GL_INSTANT_LOAN = Guid.Parse("AADC1BCF-146E-F111-B6B3-80CE62222714");
        private static readonly Guid GL_INTEREST_INCOME = Guid.Parse("0A8358BB-166E-F111-B6B3-80CE62222714");
        private static readonly Guid GL_SWIZZ_COMMISSION = Guid.Parse("0A026037-176E-F111-B6B3-80CE62222714");
        private static readonly Guid GL_MWALLET = Guid.Parse("9937E516-D3A7-4C92-B906-B40348D72AE2");

        private const double INSTANT_LOAN_INTEREST_RATE = 0.05; // 5%
        private const double INSTANT_LOAN_COMMISSION_RATE = 0.02; // 2%

        // ─────────────────────────────────────────────────────────────────────────────
        // CHECK INSTANT LOAN ELIGIBILITY
        // GET /api/ussd/CheckInstantLoanEligibility?phoneNumber=0712345678
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpGet]
        [Route("CheckInstantLoanEligibility")]
        public async Task<IHttpActionResult> CheckInstantLoanEligibility([FromUri] string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST" });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var member = await FindMemberByPhone(conn, phoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_MEMBER_NOT_FOUND, StatusDescription = "MEMBERNOTFOUND" });

                    // ── Check 1: 3+ months membership ────────────────────────────
                    DateTime membershipStart = member.RegistrationDate ?? member.CreatedDate;
                    int months = ((DateTime.Now.Year - membershipStart.Year) * 12)
                               + (DateTime.Now.Month - membershipStart.Month);
                    if (DateTime.Now.Day < membershipStart.Day) months--;

                    if (months < 3)
                        return Ok(new
                        {
                            StatusCode = SC_NOT_ELIGIBLE,
                            StatusDescription = $"NOT ELIGIBLE",
                            IsEligible = false,
                            Reason = $"Minimum 3 months membership required. Current: {months} month(s).",
                            MembershipMonths = months
                        });

                    // ── Check 2: no active instant loan (TotalLoansBalance > 0) ──
                    decimal outstandingBalance = 0m;
                    string existingRef = null;

                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1 TotalLoansBalance, Reference
                FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanCases]
                WHERE CustomerId    = @CustomerId
                  AND LoanProductId = @LoanProductId
                  AND TotalLoansBalance > 0", conn))
                    {
                        cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = member.CustomerId;
                        cmd.Parameters.Add("@LoanProductId", SqlDbType.UniqueIdentifier).Value = INSTANT_LOAN_PRODUCT_ID;
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            if (await r.ReadAsync())
                            {
                                outstandingBalance = Convert.ToDecimal(r["TotalLoansBalance"]);
                                existingRef = r["Reference"]?.ToString();
                            }
                        }
                    }

                    if (outstandingBalance > 0)
                        return Ok(new
                        {
                            StatusCode = SC_OUTSTANDING_BALANCE,
                            StatusDescription = "OUTSTANDING BALANCE",
                            IsEligible = false,
                            Reason = $"You have an outstanding Instant Loan balance of KES {outstandingBalance:N2} (Ref: {existingRef}). Please repay before requesting a new loan.",
                            OutstandingBalance = outstandingBalance
                        });

                    // ── Eligible ─────────────────────────────────────────────────
                    return Ok(new
                    {
                        StatusCode = SC_OK,
                        StatusDescription = "ELIGIBLE",
                        IsEligible = true,
                        MembershipMonths = months,
                        MaxAmount = 50000,
                        InterestRate = $"{INSTANT_LOAN_INTEREST_RATE * 100}%",
                        CommissionRate = $"{INSTANT_LOAN_COMMISSION_RATE * 100}%",
                        MemberName = $"{member.FirstName} {member.LastName}".Trim(),
                        MemberNo = member.MemberNo
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "SYSTEMERROR" });
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // REQUEST INSTANT LOAN
        // POST /api/ussd/RequestInstantLoan
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("RequestInstantLoan")]
        public async Task<IHttpActionResult> RequestInstantLoan([FromBody] InstantLoanRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST" });

            if (request.Amount <= 0)
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALID AMOUNT" });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var member = await FindMemberByPhone(conn, request.PhoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_MEMBER_NOT_FOUND, StatusDescription = "MEMBERNOTFOUND" });

                    // ── Re-run eligibility checks ─────────────────────────────────
                    DateTime membershipStart = member.RegistrationDate ?? member.CreatedDate;
                    int months = ((DateTime.Now.Year - membershipStart.Year) * 12)
                               + (DateTime.Now.Month - membershipStart.Month);
                    if (DateTime.Now.Day < membershipStart.Day) months--;

                    if (months < 3)
                        return Ok(new
                        {
                            StatusCode = SC_NOT_ELIGIBLE,
                            StatusDescription = $"NOT ELIGIBLE — NEED 3 MONTHS MEMBERSHIP. CURRENT: {months}"
                        });

                    decimal outstanding = 0m;
                    using (var cmd = new SqlCommand(@"
                SELECT ISNULL(SUM(TotalLoansBalance), 0)
                FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanCases]
                WHERE CustomerId    = @CustomerId
                  AND LoanProductId = @LoanProductId
                  AND TotalLoansBalance > 0", conn))
                    {
                        cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = member.CustomerId;
                        cmd.Parameters.Add("@LoanProductId", SqlDbType.UniqueIdentifier).Value = INSTANT_LOAN_PRODUCT_ID;
                        var result = await cmd.ExecuteScalarAsync();
                        outstanding = result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
                    }

                    if (outstanding > 0)
                        return Ok(new
                        {
                            StatusCode = SC_OUTSTANDING_BALANCE,
                            StatusDescription = $"YOU HAVE AN OUTSTANDING BALANCE OF KES {outstanding:N2}. REPAY FIRST."
                        });

                    // ── Get M-WALLET account Id ────────────────────────────────────
                    Guid walletAccountId = Guid.Empty;
                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1 ca.Id
                FROM swiftFin_CustomerAccounts ca
                WHERE ca.CustomerId                           = @CustomerId
                  AND ca.CustomerAccountType_ProductCode      = 4
                  AND ca.CustomerAccountType_TargetProductCode = 17", conn))
                    {
                        cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = member.CustomerId;
                        var result = await cmd.ExecuteScalarAsync();
                        if (result == null || result == DBNull.Value)
                            return Ok(new { StatusCode = SC_NO_RECORDS, StatusDescription = "NO M-WALLET ACCOUNT FOUND" });
                        walletAccountId = (Guid)result;
                    }

                    // ── Get active posting period ─────────────────────────────────
                    Guid postingPeriodId = Guid.Empty;
                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1 Id FROM swiftFin_PostingPeriods
                WHERE GETDATE() BETWEEN Duration_StartDate AND Duration_EndDate
                  AND IsActive = 1", conn))
                    {
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null) postingPeriodId = (Guid)result;
                    }

                    if (postingPeriodId == Guid.Empty)
                        return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "NO ACTIVE POSTING PERIOD" });

                    // ── Calculate amounts ─────────────────────────────────────────
                    decimal principal = request.Amount;
                    decimal interest = Math.Round(principal * (decimal)INSTANT_LOAN_INTEREST_RATE, 2);
                    decimal commission = Math.Round(principal * (decimal)INSTANT_LOAN_COMMISSION_RATE, 2);

                    // ── Generate LoanCase ─────────────────────────────────────────
                    Guid loanCaseId = Guid.NewGuid();
                    string loanRef = $"INSTLOAN-{DateTime.UtcNow:yyyyMMdd}-{loanCaseId.ToString().Substring(0, 8).ToUpper()}";
                    int caseNumber = 1;

                    using (var tx = conn.BeginTransaction(System.Data.IsolationLevel.Serializable))
                    {
                        try
                        {
                            // ── Next CaseNumber under lock ────────────────────────
                            using (var cmd = new SqlCommand(@"
                        SELECT ISNULL(MAX(CaseNumber), 0) + 1
                        FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanCases]
                        WITH (UPDLOCK, HOLDLOCK)", conn, tx))
                            {
                                caseNumber = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                            }

                            // ── Insert LoanCase ───────────────────────────────────
                            using (var cmd = new SqlCommand(@"
                        INSERT INTO [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanCases]
                        (
                            Id, CustomerId, LoanProductId, BranchId,
                            CaseNumber, AmountApplied, AppraisedAmount,
                            ApprovedAmount, ApprovedPrincipalPayment, ApprovedInterestPayment,
                            MonthlyPaybackAmount, TotalPaybackAmount, TotalLoansBalance,
                            LoanProductLoanBalance, LoanProductInvestmentsBalance, LoanProductLatestIncome,
                            LoanInterest_AnnualPercentageRate, LoanInterest_ChargeMode,
                            LoanInterest_RecoveryMode, LoanInterest_CalculationMode,
                            LoanRegistration_TermInMonths, LoanRegistration_MinimumGuarantors,
                            LoanRegistration_MaximumGuarantees, LoanRegistration_AllowSelfGuarantee,
                            LoanRegistration_MinimumMembershipPeriod, LoanRegistration_BypassAudit,
                            LoanRegistration_TrackArrears, LoanRegistration_RejectIfMemberHasBalance,
                            LoanRegistration_SecurityRequired,
                            Status, BatchNumber, IsBatched, ReceivedDate, DisbursedDate, DisbursedAmount,
                            Reference, SequentialId, CreatedBy, CreatedDate
                        )
                        VALUES
                        (
                            @Id, @CustomerId, @LoanProductId, @BranchId,
                            @CaseNumber, @Principal, @Principal,
                            @Principal, @Principal, @Interest,
                            @Principal, @Principal, @Principal,
                            @Principal, 0, 0,
                            5, 301, 401, 202,
                            1, 0, 0, 1,
                            3, 1, 1, 0, 0,
                            48872, 0, 0, @Now, @Now, @Principal,
                            @Reference, @SequentialId, 'MEMBER_PORTAL', @Now
                        )", conn, tx))
                            {
                                cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = loanCaseId;
                                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = member.CustomerId;
                                cmd.Parameters.Add("@LoanProductId", SqlDbType.UniqueIdentifier).Value = INSTANT_LOAN_PRODUCT_ID;
                                cmd.Parameters.Add("@BranchId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(DEFAULT_BRANCH_ID);
                                cmd.Parameters.Add("@CaseNumber", SqlDbType.Int).Value = caseNumber;
                                cmd.Parameters.Add("@Principal", SqlDbType.Decimal).Value = principal;
                                cmd.Parameters.Add("@Interest", SqlDbType.Decimal).Value = interest;
                                cmd.Parameters.Add("@Now", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                                cmd.Parameters.Add("@Reference", SqlDbType.NVarChar).Value = loanRef;
                                cmd.Parameters.Add("@SequentialId", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // ─────────────────────────────────────────────────────
                            // THREE JOURNAL ENTRIES
                            // ─────────────────────────────────────────────────────

                            // ── Journal 1: Disbursement ───────────────────────────
                            // DR Instant Loan GL +5000 / CR M-Wallet GL -5000
                            Guid journal1Id = Guid.NewGuid();
                            using (var cmd = new SqlCommand(@"
                        INSERT INTO swiftFin_Journals
                            (Id, PostingPeriodId, BranchId, TotalValue, PrimaryDescription,
                             SecondaryDescription, Reference, ValueDate, IsLocked,
                             SequentialId, CreatedBy, CreatedDate)
                        VALUES
                            (@Id, @PostingPeriodId, @BranchId, @TotalValue, 'Instant Loan Disbursement',
                             'Loan to M-Wallet', @Reference, @ValueDate, 0,
                             @SequentialId, 'MEMBER_PORTAL', @CreatedDate)", conn, tx))
                            {
                                cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = journal1Id;
                                cmd.Parameters.Add("@PostingPeriodId", SqlDbType.UniqueIdentifier).Value = postingPeriodId;
                                cmd.Parameters.Add("@BranchId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(DEFAULT_BRANCH_ID);
                                cmd.Parameters.Add("@TotalValue", SqlDbType.Decimal).Value = principal;
                                cmd.Parameters.Add("@Reference", SqlDbType.NVarChar).Value = loanRef;
                                cmd.Parameters.Add("@ValueDate", SqlDbType.Date).Value = DateTime.UtcNow.Date;
                                cmd.Parameters.Add("@SequentialId", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                                cmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // DR Instant Loan GL +principal (POSITIVE = debit)
                            await InsertJournalEntry(conn, tx, journal1Id, GL_INSTANT_LOAN, GL_MWALLET, walletAccountId, principal, loanRef);
                            // CR M-Wallet GL -principal (NEGATIVE = credit, increases wallet balance)
                            await InsertJournalEntry(conn, tx, journal1Id, GL_MWALLET, GL_INSTANT_LOAN, walletAccountId, -principal, loanRef);

                            // ── Journal 2: Interest ───────────────────────────────
                            // DR M-Wallet GL +interest / CR Interest Income -interest
                            Guid journal2Id = Guid.NewGuid();
                            using (var cmd = new SqlCommand(@"
                        INSERT INTO swiftFin_Journals
                            (Id, PostingPeriodId, BranchId, TotalValue, PrimaryDescription,
                             SecondaryDescription, Reference, ValueDate, IsLocked,
                             SequentialId, CreatedBy, CreatedDate)
                        VALUES
                            (@Id, @PostingPeriodId, @BranchId, @TotalValue, 'Instant Loan Interest',
                             '5% Interest Charge', @Reference, @ValueDate, 0,
                             @SequentialId, 'MEMBER_PORTAL', @CreatedDate)", conn, tx))
                            {
                                cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = journal2Id;
                                cmd.Parameters.Add("@PostingPeriodId", SqlDbType.UniqueIdentifier).Value = postingPeriodId;
                                cmd.Parameters.Add("@BranchId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(DEFAULT_BRANCH_ID);
                                cmd.Parameters.Add("@TotalValue", SqlDbType.Decimal).Value = interest;
                                cmd.Parameters.Add("@Reference", SqlDbType.NVarChar).Value = loanRef;
                                cmd.Parameters.Add("@ValueDate", SqlDbType.Date).Value = DateTime.UtcNow.Date;
                                cmd.Parameters.Add("@SequentialId", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                                cmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // DR M-Wallet GL +interest (reduces wallet balance — debit on wallet)
                            await InsertJournalEntry(conn, tx, journal2Id, GL_MWALLET, GL_INTEREST_INCOME, walletAccountId, interest, loanRef);
                            // CR Interest Income -interest
                            await InsertJournalEntry(conn, tx, journal2Id, GL_INTEREST_INCOME, GL_MWALLET, walletAccountId, -interest, loanRef);

                            // ── Journal 3: Swizz Commission ───────────────────────
                            // DR M-Wallet GL +commission / CR Swizz Commission Income -commission
                            Guid journal3Id = Guid.NewGuid();
                            using (var cmd = new SqlCommand(@"
                        INSERT INTO swiftFin_Journals
                            (Id, PostingPeriodId, BranchId, TotalValue, PrimaryDescription,
                             SecondaryDescription, Reference, ValueDate, IsLocked,
                             SequentialId, CreatedBy, CreatedDate)
                        VALUES
                            (@Id, @PostingPeriodId, @BranchId, @TotalValue, 'Instant Loan Swizz Commission',
                             '2% Commission Charge', @Reference, @ValueDate, 0,
                             @SequentialId, 'MEMBER_PORTAL', @CreatedDate)", conn, tx))
                            {
                                cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = journal3Id;
                                cmd.Parameters.Add("@PostingPeriodId", SqlDbType.UniqueIdentifier).Value = postingPeriodId;
                                cmd.Parameters.Add("@BranchId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(DEFAULT_BRANCH_ID);
                                cmd.Parameters.Add("@TotalValue", SqlDbType.Decimal).Value = commission;
                                cmd.Parameters.Add("@Reference", SqlDbType.NVarChar).Value = loanRef;
                                cmd.Parameters.Add("@ValueDate", SqlDbType.Date).Value = DateTime.UtcNow.Date;
                                cmd.Parameters.Add("@SequentialId", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                                cmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // DR M-Wallet GL +commission (reduces wallet balance)
                            await InsertJournalEntry(conn, tx, journal3Id, GL_MWALLET, GL_SWIZZ_COMMISSION, walletAccountId, commission, loanRef);
                            // CR Swizz Commission Income -commission
                            await InsertJournalEntry(conn, tx, journal3Id, GL_SWIZZ_COMMISSION, GL_MWALLET, walletAccountId, -commission, loanRef);

                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }

                    // ── Net wallet balance after all three journals ────────────────
                    // = +principal - interest - commission
                    decimal netWalletIncrease = principal - interest - commission;

                    // ── SMS ───────────────────────────────────────────────────────
                    try
                    {
                        string fullName = $"{member.FirstName} {member.LastName}".Trim();
                        string msg =
                            $"Dear {fullName}, your Instant Loan of KES {principal:N2} has been approved. " +
                            $"Interest: KES {interest:N2} (5%), Commission: KES {commission:N2} (2%). " +
                            $"Net credited to M-WALLET: KES {netWalletIncrease:N2}. " +
                            $"Repay KES {principal:N2} to clear. Ref: {loanRef}.";
                        await SmsHelper.SendMessageAsync(member.Phone, msg);
                    }
                    catch { }

                    return Ok(new
                    {
                        StatusCode = SC_OK,
                        StatusDescription = "INSTANT LOAN DISBURSED",
                        loanCaseId = loanCaseId,
                        loanReference = loanRef,
                        principal = principal,
                        interest = interest,
                        commission = commission,
                        netWalletCredit = netWalletIncrease,
                        amountOwed = principal
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = $"SYSTEMERROR: {ex.Message}" });
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // REPAY INSTANT LOAN
        // POST /api/ussd/RepayInstantLoan
        // Debits M-WALLET balance and reduces LoanCase.TotalLoansBalance
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("RepayInstantLoan")]
        public async Task<IHttpActionResult> RepayInstantLoan([FromBody] InstantLoanRepayRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALIDREQUEST" });

            if (request.AmountPaid <= 0)
                return Ok(new { StatusCode = SC_INVALID_REQUEST, StatusDescription = "INVALID AMOUNT" });

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var member = await FindMemberByPhone(conn, request.PhoneNumber);
                    if (member == null)
                        return Ok(new { StatusCode = SC_MEMBER_NOT_FOUND, StatusDescription = "MEMBERNOTFOUND" });

                    // ── Find active instant loan ──────────────────────────────────
                    Guid loanCaseId = Guid.Empty;
                    decimal currentBalance = 0m;
                    string loanRef = "";

                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1 Id, TotalLoansBalance, Reference
                FROM [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanCases]
                WHERE CustomerId    = @CustomerId
                  AND LoanProductId = @LoanProductId
                  AND TotalLoansBalance > 0
                ORDER BY CreatedDate DESC", conn))
                    {
                        cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = member.CustomerId;
                        cmd.Parameters.Add("@LoanProductId", SqlDbType.UniqueIdentifier).Value = INSTANT_LOAN_PRODUCT_ID;
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            if (!await r.ReadAsync())
                                return Ok(new { StatusCode = SC_NO_RECORDS, StatusDescription = "NO OUTSTANDING INSTANT LOAN" });

                            loanCaseId = r.GetGuid(r.GetOrdinal("Id"));
                            currentBalance = Convert.ToDecimal(r["TotalLoansBalance"]);
                            loanRef = r["Reference"]?.ToString();
                        }
                    }

                    // ── Cap repayment at outstanding balance ──────────────────────
                    decimal repayAmount = Math.Min(request.AmountPaid, currentBalance);

                    // ── Get M-WALLET account ──────────────────────────────────────
                    Guid walletAccountId = Guid.Empty;
                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1 Id FROM swiftFin_CustomerAccounts
                WHERE CustomerId                           = @CustomerId
                  AND CustomerAccountType_ProductCode      = 4
                  AND CustomerAccountType_TargetProductCode = 17", conn))
                    {
                        cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = member.CustomerId;
                        var result = await cmd.ExecuteScalarAsync();
                        if (result == null) return Ok(new { StatusCode = SC_NO_RECORDS, StatusDescription = "NO M-WALLET ACCOUNT" });
                        walletAccountId = (Guid)result;
                    }

                    // ── Check wallet has enough balance ───────────────────────────
                    decimal walletBalance = await GetMWalletBalance(conn, member.CustomerId);
                    if (walletBalance < repayAmount)
                        return Ok(new
                        {
                            StatusCode = SC_INVALID_REQUEST,
                            StatusDescription = $"INSUFFICIENT WALLET BALANCE. AVAILABLE: KES {walletBalance:N2}"
                        });

                    // ── Get posting period ─────────────────────────────────────────
                    Guid postingPeriodId = Guid.Empty;
                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1 Id FROM swiftFin_PostingPeriods
                WHERE GETDATE() BETWEEN Duration_StartDate AND Duration_EndDate AND IsActive = 1", conn))
                    {
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null) postingPeriodId = (Guid)result;
                    }

                    if (postingPeriodId == Guid.Empty)
                        return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = "NO ACTIVE POSTING PERIOD" });

                    string repayRef = $"REPAY-{loanRef}";

                    using (var tx = conn.BeginTransaction(System.Data.IsolationLevel.Serializable))
                    {
                        try
                        {
                            // ── Repayment journal ─────────────────────────────────
                            // DR M-Wallet GL +repayAmount (reduces wallet balance)
                            // CR Instant Loan GL -repayAmount (reduces loan balance)
                            Guid journalId = Guid.NewGuid();
                            using (var cmd = new SqlCommand(@"
                        INSERT INTO swiftFin_Journals
                            (Id, PostingPeriodId, BranchId, TotalValue, PrimaryDescription,
                             SecondaryDescription, Reference, ValueDate, IsLocked,
                             SequentialId, CreatedBy, CreatedDate)
                        VALUES
                            (@Id, @PostingPeriodId, @BranchId, @TotalValue, 'Instant Loan Repayment',
                             'Repayment via M-Wallet', @Reference, @ValueDate, 0,
                             @SequentialId, 'MEMBER_PORTAL', @CreatedDate)", conn, tx))
                            {
                                cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = journalId;
                                cmd.Parameters.Add("@PostingPeriodId", SqlDbType.UniqueIdentifier).Value = postingPeriodId;
                                cmd.Parameters.Add("@BranchId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(DEFAULT_BRANCH_ID);
                                cmd.Parameters.Add("@TotalValue", SqlDbType.Decimal).Value = repayAmount;
                                cmd.Parameters.Add("@Reference", SqlDbType.NVarChar).Value = repayRef;
                                cmd.Parameters.Add("@ValueDate", SqlDbType.Date).Value = DateTime.UtcNow.Date;
                                cmd.Parameters.Add("@SequentialId", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                                cmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // DR M-Wallet +repayAmount (POSITIVE = debit, reduces wallet)
                            await InsertJournalEntry(conn, tx, journalId, GL_MWALLET, GL_INSTANT_LOAN, walletAccountId, repayAmount, repayRef);
                            // CR Instant Loan GL -repayAmount (NEGATIVE = credit, reduces what's owed)
                            await InsertJournalEntry(conn, tx, journalId, GL_INSTANT_LOAN, GL_MWALLET, walletAccountId, -repayAmount, repayRef);

                            // ── Reduce TotalLoansBalance on LoanCase ──────────────
                            decimal newLoanBalance = currentBalance - repayAmount;
                            using (var cmd = new SqlCommand(@"
                        UPDATE [SwiftFinancialsDB_Live].[dbo].[swiftFin_LoanCases]
                        SET TotalLoansBalance = @NewBalance
                        WHERE Id = @LoanCaseId", conn, tx))
                            {
                                cmd.Parameters.Add("@NewBalance", SqlDbType.Decimal).Value = newLoanBalance;
                                cmd.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = loanCaseId;
                                await cmd.ExecuteNonQueryAsync();
                            }

                            tx.Commit();

                            // ── SMS ───────────────────────────────────────────────
                            try
                            {
                                string fullName = $"{member.FirstName} {member.LastName}".Trim();
                                string msg = newLoanBalance <= 0
                                    ? $"Dear {fullName}, your Instant Loan (Ref: {loanRef}) has been fully repaid. Thank you. RUBANI SACCO."
                                    : $"Dear {fullName}, KES {repayAmount:N2} repaid for Instant Loan (Ref: {loanRef}). Remaining balance: KES {newLoanBalance:N2}. RUBANI SACCO.";
                                await SmsHelper.SendMessageAsync(member.Phone, msg);
                            }
                            catch { }

                            return Ok(new
                            {
                                StatusCode = SC_OK,
                                StatusDescription = newLoanBalance <= 0 ? "LOAN FULLY REPAID" : "REPAYMENT RECORDED",
                                amountPaid = repayAmount,
                                previousBalance = currentBalance,
                                newLoanBalance = newLoanBalance,
                                fullyRepaid = newLoanBalance <= 0
                            });
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(new { StatusCode = SC_SYSTEM_ERROR, StatusDescription = $"SYSTEMERROR: {ex.Message}" });
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // SHARED JOURNAL ENTRY HELPER
        // ─────────────────────────────────────────────────────────────────────────────
        private async Task InsertJournalEntry(
            SqlConnection conn, SqlTransaction tx,
            Guid journalId, Guid chartOfAccountId, Guid contraChartOfAccountId,
            Guid customerAccountId, decimal amount, string reference)
        {
            using (var cmd = new SqlCommand(@"
        INSERT INTO swiftFin_JournalEntries
            (Id, JournalId, ChartOfAccountId, ContraChartOfAccountId, CustomerAccountId,
             Amount, ValueDate, SequentialId, CreatedBy, CreatedDate)
        VALUES
            (@Id, @JournalId, @ChartOfAccountId, @ContraChartOfAccountId, @CustomerAccountId,
             @Amount, @ValueDate, @SequentialId, 'MEMBER_PORTAL', @CreatedDate)", conn, tx))
            {
                cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                cmd.Parameters.Add("@JournalId", SqlDbType.UniqueIdentifier).Value = journalId;
                cmd.Parameters.Add("@ChartOfAccountId", SqlDbType.UniqueIdentifier).Value = chartOfAccountId;
                cmd.Parameters.Add("@ContraChartOfAccountId", SqlDbType.UniqueIdentifier).Value = contraChartOfAccountId;
                cmd.Parameters.Add("@CustomerAccountId", SqlDbType.UniqueIdentifier).Value = customerAccountId;
                cmd.Parameters.Add("@Amount", SqlDbType.Decimal).Value = amount;
                cmd.Parameters.Add("@ValueDate", SqlDbType.Date).Value = DateTime.UtcNow.Date;
                cmd.Parameters.Add("@SequentialId", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                cmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // MODELS
        // ─────────────────────────────────────────────────────────────────────────────
        public class InstantLoanRequest
        {
            public string PhoneNumber { get; set; }
            public decimal Amount { get; set; }
        }

        public class InstantLoanRepayRequest
        {
            public string PhoneNumber { get; set; }
            public decimal AmountPaid { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────────
        // MODELS
        // ─────────────────────────────────────────────────────────────────────
        public class UssdPhoneRequest
        {
            public string PhoneNumber { get; set; }
        }

        public class UssdDocumentRequest
        {
            public string PhoneNumber { get; set; }
            public string DocumentNo { get; set; }
            public string AccountNo { get; set; }
        }

        public class CashAdvanceRequest
        {
            public string PhoneNumber { get; set; }
            public decimal AmountRequested { get; set; }
        }

        public class CashAdvanceRepaymentRequest
        {
            public string PhoneNumber { get; set; }
            public decimal AmountPaid { get; set; }
            public string Reference { get; set; }
        }

        public class UssdAccount
        {
            public string AccountNo { get; set; }
            public string AccountName { get; set; }
            public string AccountType { get; set; }
            public string AccountStatus { get; set; }
        }

        public class UssdLoanBalance
        {
            public string LoanNo { get; set; }
            public string LoanName { get; set; }
            public string BalanceAmount { get; set; }
        }

        private class MemberRecord
        {
            public Guid CustomerId { get; set; }
            public string MemberNo { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string IDNumber { get; set; }
            public string Phone { get; set; }
            public DateTime? RegistrationDate { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        private class ProductBalance
        {
            public string Name { get; set; }
            public decimal Balance { get; set; }
        }

        private class B2CDisburseResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public string MpesaReference { get; set; }
        }
    }
}