using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Presentation.Infrastructure.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.UI.WebControls;
using TestApis.Helpers;
using TestApis.Models;
using Image = iTextSharp.text.Image;

namespace TestApis.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [AllowAnonymous]
    [RoutePrefix("api/Loaning")]
    public class LoaningController : ApiController
    {
        private readonly MasterController master;

        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        private readonly HttpClient _httpClient;

        public LoaningController()
        {
            master = new MasterController();
            _httpClient = new HttpClient(); // FIXED

        }



        [HttpGet]
        [Route("GetLoansBy")]
        public async Task<IHttpActionResult> GetLoansBy(
     string status = "Registered",
     string filterValue = "",
     int filterType = 0,
     int pageIndex = 0,
     int pageSize = 50)
        {
            var serviceHeader = master.GetServiceHeader();

            var resolvedStatus = ResolveLoanStatus(status);
            if (resolvedStatus == null)
                return BadRequest($"Invalid loan status '{status}'.");

            var resolvedFilter = ResolveLoanFilter(filterType);
            if (resolvedFilter == null)
                return BadRequest($"Invalid filterType '{filterType}'.");

            var pageInfo = await master._channelService
                .FindLoanCasesByStatusAndFilterInPageAsync(
                    (int)resolvedStatus.Value,
                    filterValue,
                    filterType,
                    pageIndex,
                    pageSize,
                    includeBatchStatus: true,
                    serviceHeader);

            if (pageInfo?.PageCollection == null || !pageInfo.PageCollection.Any())
            {
                return Ok(new
                {
                    items = Array.Empty<LoanCaseDTO>(),
                    pageIndex,
                    pageSize
                });
            }

            // HARD GUARANTEE: guarantors are fully packed before response
            foreach (var loanCase in pageInfo.PageCollection)
            {
                var guarantors =
                    await master._channelService
                        .FindLoanGuarantorsByLoanCaseIdAsync(loanCase.Id, serviceHeader);

                loanCase.Guarantors = guarantors?.ToList()
                    ?? new List<LoanGuarantorDTO>();
            }

            return Ok(new
            {
                items = pageInfo.PageCollection,
                pageIndex,
                pageSize
            });

        }
        [HttpPost]
        [Route("Topup")]
        public async Task<IHttpActionResult> TopUpLoanAsync([FromBody] LoanTopUpRequest request)
        {
            if (request.TopUpAmount <= 0)
                return Ok(new { success = false, message = "Top-up amount must be greater than zero" });

            // 1. Load original loan case
            var originalLoanCase = await master._channelService.FindLoanCaseAsync(request.OriginalLoanCaseId, request.ServiceHeader);
            if (originalLoanCase == null)
                return Ok(new { success = false, message = "Original loan case not found" });

            // 2. Check eligibility
            var remainingEligibility = originalLoanCase.LoanRegistrationMaximumAmount - originalLoanCase.TotalLoansBalance;
            if (request.TopUpAmount > remainingEligibility)
                return Ok(new { success = false, message = "Top-up exceeds maximum entitlement" });

            // 3. Calculate interest & repayment
            decimal interestRate = (decimal)originalLoanCase.LoanInterestAnnualPercentageRate / 100m;
            int termMonths = originalLoanCase.LoanRegistrationTermInMonths;

            decimal totalPayback = request.TopUpAmount * (1 + interestRate); // simple interest
            decimal monthlyPayback = totalPayback / termMonths;

            // 4. Create top-up loan case
            var topUpLoanCase = new LoanCaseDTO
            {
                ParentId = originalLoanCase.Id,
                BranchId = originalLoanCase.BranchId,
                CustomerId = originalLoanCase.CustomerId,
                LoanProductId = originalLoanCase.LoanProductId,
                LoanProductCode = originalLoanCase.LoanProductCode,
                AmountApplied = request.TopUpAmount,
                AppraisedAmount = request.TopUpAmount,
                ApprovedAmount = request.TopUpAmount,
                AppraisedDate = DateTime.Now,
                MonthlyPaybackAmount = monthlyPayback,
                TotalPaybackAmount = totalPayback,
                Status = (int)LoanCaseStatus.Approved,
                CreatedDate = DateTime.Now
            };

            topUpLoanCase = await master._channelService.AddLoanCaseAsync(topUpLoanCase, request.ServiceHeader);
            if (topUpLoanCase == null)
                return Ok(new { success = false, message = "Failed to create top-up loan case" });

            // 5. Load or create customer loan account
            var customerLoanAccount = await master._channelService.FindCustomerAccountAsync(originalLoanCase.CustomerAccountId, true, true, true, false, request.ServiceHeader);
            if (customerLoanAccount == null)
            {
                var customerAccountDTO = new CustomerAccountDTO
                {
                    BranchId = topUpLoanCase.BranchId,
                    CustomerId = topUpLoanCase.CustomerId,
                    CustomerAccountTypeProductCode = (int)ProductCode.Loan,
                    CustomerAccountTypeTargetProductId = topUpLoanCase.LoanProductId,
                    CustomerAccountTypeTargetProductCode = topUpLoanCase.LoanProductCode,
                    Status = (int)CustomerAccountStatus.Normal,
                    RecordStatus = (int)RecordStatus.Approved
                };
                customerLoanAccount = await master._channelService.AddCustomerAccountAsync(customerAccountDTO, request.ServiceHeader);
            }

            // 6. Load bank account
            var bankAccount = await master._channelService.FindBankLinkagesAsync();
            if (bankAccount == null)
                return Ok(new { success = false, message = "Bank account not found" });

            // 7. Post transaction to journal
            var transactionModel = new CustomerTransactionModel
            {
                BranchId = topUpLoanCase.BranchId,
                TotalValue = request.TopUpAmount,
                DebitCustomerAccountId = customerLoanAccount.Id,
                CreditCustomerAccountId = customerLoanAccount.Id,
                DebitCustomerAccount = customerLoanAccount,
                CreditCustomerAccount = customerLoanAccount,
                DebitChartOfAccountId = customerLoanAccount.CustomerAccountTypeTargetProductChartOfAccountId,
            };

            var postingPeriod = transactionModel.PostingPeriodId != Guid.Empty
                ? await master._channelService.FindPostingPeriodAsync(transactionModel.PostingPeriodId, request.ServiceHeader)
                : await master._channelService.FindCurrentPostingPeriodAsync(request.ServiceHeader);

            if (postingPeriod == null)
                return BadRequest("Posting period not found");

            var journal = await master._channelService.AddJournalWithCustomerAccountAsync(transactionModel, request.ServiceHeader);
            if (journal == null)
                return Ok(new { success = false, message = "Failed to post journal entry" });

            // 8. Update original loan balances
            originalLoanCase.TotalLoansBalance += request.TopUpAmount;
            originalLoanCase.LoanProductLoanBalance += request.TopUpAmount;
            await master._channelService.UpdateLoanCaseAsync(originalLoanCase, request.ServiceHeader);

            // 9. Add entry to attached loans table
            var attachedLoan = new AttachedLoanDTO
            {
                LoanCaseId = topUpLoanCase.Id,
                CustomerAccountId = customerLoanAccount.Id,
                PrincipalBalance = request.TopUpAmount,
                InterestBalance = totalPayback - request.TopUpAmount,
                CarryForwardsBalance = 0,
                ClearanceCharges = 0,
                CreatedDate = DateTime.Now
            };
            ObservableCollection<AttachedLoanDTO> attachedLoanDTOs = new ObservableCollection<AttachedLoanDTO>();

            var attachedLoanList = new List<AttachedLoanDTO> { attachedLoan };
            await master._channelService.UpdateAttachedLoansByLoanCaseIdAsync(request.OriginalLoanCaseId, attachedLoanDTOs, request.ServiceHeader);

            return Ok(new
            {
                success = true,
                message = "Loan top-up processed successfully",
                TopUpLoanCaseId = topUpLoanCase.Id,
                UpdatedTotalBalance = originalLoanCase.TotalLoansBalance,
                MonthlyPaybackAmount = monthlyPayback,
                TotalPaybackAmount = totalPayback
            });
        }




        [HttpGet]
        [Route("GetLoansByFilters")]
        public async Task<IHttpActionResult> GetLoans(int status = (int)LoanCaseStatus.Registered, string filterValue = "", int filterType = 0, int pageIndex = 0, int pageSize = 50)
        {
            var serviceHeader = master.GetServiceHeader();

            var pageInfo = await master._channelService.FindLoanCasesByStatusAndFilterInPageAsync(status, filterValue, filterType, pageIndex, pageSize, includeBatchStatus: true, serviceHeader);

            if (pageInfo == null || pageInfo.PageCollection == null)
            {
                return Ok(new { items = new List<LoanCaseDTO>(), pageIndex = pageIndex, pageSize = pageSize });
            }

            return Ok(new { items = pageInfo.PageCollection, pageIndex = pageIndex, pageSize = pageSize });
        }


        [HttpGet]
        [Route("GetAllLoans")]
        public async Task<IHttpActionResult> GetAllLoans()
        {
            var serviceHeader = master.GetServiceHeader();

            var pageInfo = await master._channelService.FindLoanCasesAsync(serviceHeader);

            return Ok(pageInfo);
        }

        [HttpGet]
        [Route("printshedule")]
        public async Task<IHttpActionResult> printshedule()
        {
            var serviceHeader = master.GetServiceHeader();
            var pageInfo = await master._channelService.FindLoanCasesAsync(serviceHeader);
            var loanCaseDTO = pageInfo?.FirstOrDefault(c => c.CustomerReference2 == "0004");
            var doublee = await master._channelService.FVAsync(loanCaseDTO.LoanRegistrationTermInMonths, loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear, loanCaseDTO.LoanInterestAnnualPercentageRate, (double)loanCaseDTO.MonthlyPaybackAmount, (double)loanCaseDTO.ApprovedAmount, loanCaseDTO.LoanRegistrationPaymentDueDate, serviceHeader);
            var repayment = await master._channelService.PrintLoanRepaymentScheduleAsync(loanCaseDTO, serviceHeader);

            return Ok(repayment);
        }



        [HttpGet]
        [Route("GetAllLoanByMemberNo")]
        public async Task<IHttpActionResult> GetAllLoanByMemberNo(string memberNo)
        {
            var serviceHeader = master.GetServiceHeader();

            var pageInfo = await master._channelService.FindLoanCasesAsync(serviceHeader);

            if (pageInfo == null || !pageInfo.Any())
                return BadRequest("No loans found.");

            var memberLoans = pageInfo
                .Where(c => c.CustomerReference2 == memberNo)
                .ToList();

            if (!memberLoans.Any())
                return BadRequest("No loans found for the specified member.");

            return Ok(memberLoans);
        }



        [HttpGet]
        [Route("GetPostingPeriods")]
        public async Task<IHttpActionResult> GetPostingPeriods()
        {
            var serviceHeader = master.GetServiceHeader();

            var pageInfo = await master._channelService.FindPostingPeriodsAsync(serviceHeader);
            if (pageInfo == null)
                return BadRequest("Posting Periods Not Found.");

            return Ok(pageInfo);
        }

        #region Loan Application Original

        //        [HttpPost]
        //        [Route("LoanApplication")]
        //        public async Task<IHttpActionResult> Create([FromBody] LoanCaseDTO2 loanCaseDTO)
        //        {

        //            try
        //            {
        //                var serviceHeader = master.GetServiceHeader();

        //                if (loanCaseDTO == null)
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Invalid request payload."));
        //                // 1. Validate member
        //                var customer = await master._channelService
        //                    .FindCustomerAsync(loanCaseDTO.CustomerId, serviceHeader);

        //                if (customer == null)
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Member not found."));


        //                // 2. Validate loan product
        //                var loanProduct = await master._channelService.FindLoanProductAsync(loanCaseDTO.LoanProductId, serviceHeader);

        //                if (loanProduct == null)
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Invalid loan product."));

        //                var products = await master._channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(loanCaseDTO.CustomerId, new[] { (int)ProductCode.Savings, (int)ProductCode.Loan, (int)ProductCode.Investment }, true, true, true, true, serviceHeader);



        //                if (loanCaseDTO.AmountApplied == 0)
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Amount Cannot be zero."));

        //                var multiplier = loanProduct.LoanRegistrationInvestmentsMultiplier;


        //                loanCaseDTO.ReceivedDate = DateTime.UtcNow;
        //                //decimal rate = Convert.ToDecimal( loanProduct.LoanInterestAnnualPercentageRate / 100); // if stored as 12 not 0.12
        //                //decimal principal = loanCaseDTO.AmountApplied;
        //                //decimal termInYears = loanProduct.LoanRegistrationTermInMonths / 12m;

        //                //loanCaseDTO.ApprovedInterestPayment = principal * rate * termInYears;
        //                //loanCaseDTO.TotalPaybackAmount = loanCaseDTO.AmountApplied + loanCaseDTO.ApprovedInterestPayment;



        //                //var productName = loanProduct.Description?.Trim();

        //                //// 3. Enforce Savings Boost ? Development Loan dependency
        //                //if (string.Equals(productName, "Savings Boost", StringComparison.OrdinalIgnoreCase))
        //                //{
        //                //    var memberLoans = await master._channelService
        //                //        .FindLoanCasesByCustomerIdInProcessAsync(loanCaseDTO.CustomerId, serviceHeader);

        //                //}

        //                // 4. Branch resolution (isolated, explicit)
        //                var branches = await master._channelService.FindBranchesAsync(serviceHeader);
        //                var branch = branches?.FirstOrDefault(b =>
        //                    b.Description != null &&
        //                    b.Description.StartsWith("Rubani", StringComparison.OrdinalIgnoreCase));

        //                if (branch != null)
        //                    loanCaseDTO.BranchId = branch.Id;

        //                // 5. Membership duration validation
        //                var membershipMonths = ((DateTime.UtcNow.Year - customer.CreatedDate.Year) * 12) + (DateTime.UtcNow.Month - customer.CreatedDate.Month);

        //                if (membershipMonths < loanProduct.LoanRegistrationMinimumMembershipPeriod)

        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Member does not meet minimum membership period."));

        //                // 6. Guarantor validation
        //                var guarantors = loanCaseDTO.Guarantors ?? new List<LoanGuarantorDTO>();

        //                if (!guarantors.Any())
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "At least one guarantor is required."));

        //                if (guarantors.Count < loanProduct.LoanRegistrationMinimumGuarantors)
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", $"Minimum {loanProduct.LoanRegistrationMinimumGuarantors} guarantors required."));

        //                //if (guarantors.Select(g => g.CustomerReference2).Distinct().Count() != guarantors.Count)
        //                //    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Duplicate guarantors detected."));

        //                if (guarantors.Sum(g => g.AmountGuaranteed) < loanCaseDTO.AmountApplied)
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Loan is not fully secured by guarantors."));

        //                // 7. Collaterals
        //                var collateralDocuments = new List<CustomerDocumentDTO>();

        //                if (!string.IsNullOrWhiteSpace(loanCaseDTO.collateralIds))
        //                {
        //                    var collateralIds = loanCaseDTO.collateralIds
        //                        .Split(',')
        //                        .Where(x => Guid.TryParse(x, out _))
        //                        .Select(Guid.Parse);

        //                    foreach (var id in collateralIds)
        //                    {
        //                        var doc = await master._channelService.FindCustomerDocumentAsync(id, serviceHeader);

        //                        if (doc != null)
        //                            collateralDocuments.Add(doc);
        //                    }
        //                }

        //                // 8. Apply loan product rules
        //                MapLoanProductAttributes(loanCaseDTO, loanProduct);
        //                var investmentProducts = products.Where(p => p.CustomerAccountTypeProductCode == (int)ProductCode.Savings).ToList();

        //                List<decimal> iBalance = new List<decimal>();

        //                foreach (var investmentsBalances in investmentProducts)
        //                {
        //                    iBalance.Add(investmentsBalances.BookBalance);
        //                }
        //                var investmentsBalance = iBalance.Sum();
        //                if (loanCaseDTO.Remarks == "Boosted")
        //                {
        //                    var decimalreferenceamount = Convert.ToDecimal(loanCaseDTO.Reference);
        //                    investmentsBalance += decimalreferenceamount;
        //                }

        //                decimal loanLimit = investmentsBalance * Convert.ToDecimal(multiplier);

        //                if (loanCaseDTO.AmountApplied > loanLimit)
        //                {
        //                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", $"Amount applied exceeds your available loan limit of {loanLimit:N2}."));
        //                }

        //                // 9. Persist loan
        //                loanCaseDTO.CreatedBy = User.Identity.Name;
        //                loanCaseDTO.Status = 48826;
        //                loanCaseDTO.LoanStatus = "48826";
        //                loanCaseDTO.ReceivedDate = DateTime.UtcNow;

        //                var createResult = await master._channelService
        //                    .AddLoanCaseAsync(
        //                        loanCaseDTO.MapTo<LoanCaseDTO>(),
        //                        serviceHeader);



        //                if (!string.IsNullOrWhiteSpace(createResult.ErrorMessageResult))
        //                {
        //                    return Ok(ApiResponse<string>.Fail(
        //                        "Error posting this loan.",
        //                        createResult.ErrorMessageResult
        //                    ));
        //                }

        //                // 10. Sector classification
        //                using (var conn = new SqlConnection(_connectionString))
        //                using (var cmd = new SqlCommand(@"
        //            INSERT INTO LoanCaseSectorClassification
        //                (LoanCaseId, SectorCode, SubSectorCode)
        //            VALUES
        //                (@LoanCaseId, @SectorCode, @SubSectorCode)", conn))
        //                {
        //                    cmd.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = createResult.Id;
        //                    cmd.Parameters.Add("@SectorCode", SqlDbType.VarChar, 20).Value = loanCaseDTO.SectorCode;
        //                    cmd.Parameters.Add("@SubSectorCode", SqlDbType.VarChar, 30).Value = loanCaseDTO.SubSectorCode;

        //                    await conn.OpenAsync();
        //                    await cmd.ExecuteNonQueryAsync();
        //                }

        //                // 11. Attach collaterals
        //                if (collateralDocuments.Any())
        //                {
        //                    await master._channelService
        //                        .UpdateLoanCollateralsByLoanCaseIdAsync(
        //                            createResult.Id,
        //                            new ObservableCollection<CustomerDocumentDTO>(collateralDocuments),
        //                            serviceHeader);
        //                }

        //                // 12. Attach guarantors
        //                await master._channelService
        //                    .UpdateLoanGuarantorsByLoanCaseIdAsync(
        //                        createResult.Id,
        //                        new ObservableCollection<LoanGuarantorDTO>(guarantors),
        //                        serviceHeader);

        //                // 13. Notify member
        //                //var sms =
        //                //    $"Dear {customer.IndividualFirstName} {customer.IndividualLastName}, " +
        //                //    $"your loan application of KES {loanCaseDTO.AmountApplied:N0} has been successfully registered and is under review.";

        //                //await SmsHelper.SendMessageAsync(customer.AddressMobileLine, sms);

        //                var fullName = $"{customer.IndividualFirstName} {customer.IndividualLastName}";
        //                var loanName = loanProduct.Description;
        //                var reference = loanCaseDTO.Reference;


        //                decimal principal = loanCaseDTO.AmountApplied;

        //                decimal annualRatePercent = (decimal)loanProduct.LoanInterestAnnualPercentageRate; // e.g. 12
        //                decimal monthlyRate = (annualRatePercent / 100m) / 12m;

        //                int months = loanProduct.LoanRegistrationTermInMonths;

        //                // EMI (annuity)
        //                decimal pow = (decimal)Math.Pow((double)(1 + monthlyRate), months);

        //                decimal rawEmi = principal * (monthlyRate * pow) / (pow - 1);

        //                // ROUND EMI FIRST (bank-grade behavior)
        //                decimal monthlyPayback = Math.Round(rawEmi, 2);

        //                // Totals derived from rounded EMI
        //                decimal totalPayback = Math.Round(monthlyPayback * months, 2);
        //                decimal totalInterest = Math.Round(totalPayback - principal, 2);

        //                // Assign snapshot
        //                loanCaseDTO.MonthlyPaybackAmount = monthlyPayback;
        //                loanCaseDTO.TotalPaybackAmount = totalPayback;
        //                loanCaseDTO.ApprovedInterestPayment = totalInterest;
        //                loanCaseDTO.AppraisedAmount = principal;
        //                loanCaseDTO.ApprovedAmount = principal;

        //                // Opening balances
        //                loanCaseDTO.TotalLoansBalance = totalPayback;          // total obligation
        //                loanCaseDTO.LoanProductLoanBalance = principal;        // outstanding principal at disbursement


        //                try
        //                {
        //                    using (SqlConnection conn = new SqlConnection(_connectionString))
        //                    {
        //                        conn.Open();

        //                        using (SqlTransaction tx = conn.BeginTransaction())
        //                        {
        //                            // 1. VALIDATE EXISTENCE
        //                            using (SqlCommand checkCmd = new SqlCommand(
        //                                "SELECT COUNT(1) FROM swiftFin_LoanCases WHERE Id = @Id", conn, tx))
        //                            {
        //                                checkCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
        //                                    .Value = createResult.Id;

        //                                if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
        //                                    throw new Exception("Validation failed: Loan case not found for update.");
        //                            }

        //                            // 2. UPDATE FINANCIAL & APPRAISAL SNAPSHOT
        //                            using (SqlCommand updateCmd = new SqlCommand(@"
        //UPDATE swiftFin_LoanCases
        //SET
        //    ApprovedAmount            = @ApprovedAmount,
        //    ApprovedInterestPayment   = @ApprovedInterestPayment,
        //    TotalPaybackAmount        = @TotalPaybackAmount,
        //    TotalLoansBalance         = @TotalLoansBalance,
        //    MonthlyPaybackAmount      = @MonthlyPaybackAmount,
        //    AppraisedAmount           = @AppraisedAmount,
        //    LoanProductLoanBalance    = @LoanProductLoanBalance
        //WHERE Id = @Id
        //", conn, tx))
        //                            {
        //                                var pApproved = updateCmd.Parameters.Add("@ApprovedAmount", SqlDbType.Decimal);
        //                                pApproved.Precision = 18; pApproved.Scale = 2;
        //                                pApproved.Value = loanCaseDTO.ApprovedAmount = loanCaseDTO.AmountApplied;

        //                                var pInterest = updateCmd.Parameters.Add("@ApprovedInterestPayment", SqlDbType.Decimal);
        //                                pInterest.Precision = 18; pInterest.Scale = 2;
        //                                pInterest.Value = loanCaseDTO.ApprovedInterestPayment;

        //                                var pTotal = updateCmd.Parameters.Add("@TotalPaybackAmount", SqlDbType.Decimal);
        //                                pTotal.Precision = 18; pTotal.Scale = 2;
        //                                pTotal.Value = loanCaseDTO.TotalPaybackAmount;

        //                                var pBalance = updateCmd.Parameters.Add("@TotalLoansBalance", SqlDbType.Decimal);
        //                                pBalance.Precision = 18; pBalance.Scale = 2;
        //                                pBalance.Value = loanCaseDTO.TotalLoansBalance = loanCaseDTO.ApprovedAmount + loanCaseDTO.ApprovedInterestPayment;

        //                                var pMonthly = updateCmd.Parameters.Add("@MonthlyPaybackAmount", SqlDbType.Decimal);
        //                                pMonthly.Precision = 18; pMonthly.Scale = 2;
        //                                pMonthly.Value = loanCaseDTO.MonthlyPaybackAmount;

        //                                var pAppraised = updateCmd.Parameters.Add("@AppraisedAmount", SqlDbType.Decimal);
        //                                pAppraised.Precision = 18; pAppraised.Scale = 2;
        //                                pAppraised.Value = loanCaseDTO.AppraisedAmount;

        //                                var pProductBalance = updateCmd.Parameters.Add("@LoanProductLoanBalance", SqlDbType.Decimal);
        //                                pProductBalance.Precision = 18; pProductBalance.Scale = 2;
        //                                pProductBalance.Value = loanCaseDTO.LoanProductLoanBalance;

        //                                updateCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
        //                                    .Value = createResult.Id;

        //                                if (updateCmd.ExecuteNonQuery() != 1)
        //                                    throw new Exception("Update failed: unexpected number of rows affected.");
        //                            }

        //                            tx.Commit();
        //                        }
        //                    }
        //                }
        //                catch
        //                {
        //                    throw; // bubble up to API/logging layer
        //                }

        //                return Ok(ApiResponse<string>.Ok(
        //                    "Loan created successfully .",
        //                    $"{loanName} loan for {fullName} has been registered successfully. witha a boost of ksh {reference}"
        //                ));

        //            }
        //            catch (Exception ex)
        //            {
        //                return Ok(ApiResponse<string>.Fail(
        //                    "System error occurred.",
        //                    ex.Message
        //                ));
        //            }
        //        }

        #endregion
        [HttpPost]
        [Route("LoanApplication")]
        public async Task<IHttpActionResult> Create([FromBody] LoanCaseDTO2 loanCaseDTO)
        {
            try
            {
                var serviceHeader = master.GetServiceHeader();

                if (loanCaseDTO == null)
                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Invalid request payload."));

                // 1. MEMBER
                var customer = await master._channelService.FindCustomerAsync(loanCaseDTO.CustomerId, serviceHeader);

                if (customer == null)
                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Member not found."));

                // 2. PRODUCT
                var loanProduct = await master._channelService
                    .FindLoanProductAsync(loanCaseDTO.LoanProductId, serviceHeader);

                if (loanProduct == null)
                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Invalid loan product."));

                if (loanCaseDTO.AmountApplied <= 0)
                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Amount must be greater than zero."));

                var branches = await master._channelService.FindBranchesAsync(serviceHeader);
                var branch = branches?.FirstOrDefault(b =>
                    b.Description != null &&
                    b.Description.StartsWith("Rubani", StringComparison.OrdinalIgnoreCase));
                if (branch != null)
                    loanCaseDTO.BranchId = branch.Id;

                // ================== VALIDATIONS ==================

                // ---- Amount basic ----
                if (loanCaseDTO.AmountApplied <= 0)
                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", "Amount must be greater than zero."));

                // ---- Amount vs product limits ----
                if (loanCaseDTO.AmountApplied < loanProduct.LoanRegistrationMinimumAmount)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        $"Minimum loan amount is {loanProduct.LoanRegistrationMinimumAmount:N2}."));

                if (loanCaseDTO.AmountApplied > loanProduct.LoanRegistrationMaximumAmount)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        $"Maximum loan amount is {loanProduct.LoanRegistrationMaximumAmount:N2}."));

                // ---- Membership period ----
                int membershipMonths;

                if (customer.RegistrationDate != null && customer.RegistrationDate.Value <= DefaultSettings.Instance.ServerDate)
                {
                    membershipMonths = UberUtil.GetPeriod(DefaultSettings.Instance.ServerDate, customer.RegistrationDate.Value);
                }
                else
                {
                    membershipMonths = -1;
                }

                if (membershipMonths < loanProduct.LoanRegistrationMinimumMembershipPeriod)
                {
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "Member does not meet minimum membership period."));
                }

                // ---- Interest sanity ----
                if (loanProduct.LoanInterestAnnualPercentageRate <= 0)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "Invalid interest configuration for selected product."));

                // ================== CHECK FOR EXISTING ACTIVE LOAN ==================
                // Check if user already has an active loan for the same product that is not finished
                var allLoans = await master._channelService.FindLoanCasesAsync(serviceHeader);

                var existingActiveLoan = allLoans?.FirstOrDefault(l =>
                    l.CustomerId == customer.Id &&
                    l.LoanProductId == loanProduct.Id &&
                    l.TotalLoansBalance > 0  // Loan is not finished (has balance)
                );

                if (existingActiveLoan != null)
                {
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        $"You already have an active {loanProduct.Description} loan that is not yet completed. " +
                        $"Reference: {existingActiveLoan.Reference ?? existingActiveLoan.Id.ToString()}, " +
                        $"Outstanding Balance: {existingActiveLoan.TotalLoansBalance:N2}. " +
                        $"Please complete the existing loan before applying for a new one."
                    ));
                }
                // ================== END ACTIVE LOAN CHECK ==================

                // ---- Guarantors ----
                var guarantors = loanCaseDTO.Guarantors ?? new List<LoanGuarantorDTO>();

                if (!guarantors.Any())
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "At least one guarantor is required."));

                if (guarantors.Count < loanProduct.LoanRegistrationMinimumGuarantors)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        $"Minimum {loanProduct.LoanRegistrationMinimumGuarantors} guarantors required."));

                if (loanProduct.LoanRegistrationMaximumGuarantees > 0 &&
                    guarantors.Count > loanProduct.LoanRegistrationMaximumGuarantees)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        $"Maximum {loanProduct.LoanRegistrationMaximumGuarantees} guarantors allowed."));

                if (guarantors.Any(g => g.AmountGuaranteed <= 0))
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "Guarantor amounts must be greater than zero."));

                if (guarantors.Select(g => g.CustomerId).Distinct().Count() != guarantors.Count)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "Duplicate guarantors detected."));

                if (!loanProduct.LoanRegistrationAllowSelfGuarantee &&
                    guarantors.Any(g => g.CustomerId == loanCaseDTO.CustomerId))
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "Self-guarantee is not allowed for this product."));

                if (guarantors.Sum(g => g.AmountGuaranteed) < loanCaseDTO.AmountApplied)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "Loan is not fully secured by guarantors."));

                // ---- Term consistency ----
                if (loanCaseDTO.LoanRegistrationTermInMonths > 0 &&
                    loanCaseDTO.LoanRegistrationTermInMonths != loanProduct.LoanRegistrationTermInMonths)
                    return Ok(ApiResponse<string>.Fail(
                        "Error posting this loan.",
                        "Invalid loan term for selected product."));

                // ================== END VALIDATIONS ==================

                // 6. COLLATERALS
                var collateralDocuments = new List<CustomerDocumentDTO>();

                if (!string.IsNullOrWhiteSpace(loanCaseDTO.collateralIds))
                {
                    var collateralIds = loanCaseDTO.collateralIds
                        .Split(',')
                        .Where(x => Guid.TryParse(x, out _))
                        .Select(Guid.Parse);

                    foreach (var id in collateralIds)
                    {
                        var doc = await master._channelService.FindCustomerDocumentAsync(id, serviceHeader);
                        if (doc != null) collateralDocuments.Add(doc);
                    }
                }

                // 7. PRODUCT RULES
                MapLoanProductAttributes(loanCaseDTO, loanProduct);

                // 8. EMI + BALANCES
                decimal principal = loanCaseDTO.AmountApplied;
                decimal netDisbursement = principal;

                decimal annualRatePercent = (decimal)loanProduct.LoanInterestAnnualPercentageRate;
                decimal monthlyRate = (annualRatePercent / 100m) / 12m;
                int months = loanProduct.LoanRegistrationTermInMonths;

                decimal pow = (decimal)Math.Pow((double)(1 + monthlyRate), months);
                decimal rawEmi = principal * (monthlyRate * pow) / (pow - 1);

                decimal monthlyPayback = Math.Round(rawEmi, 2);
                decimal totalPayback = Math.Round(monthlyPayback * months, 2);
                decimal totalInterest = Math.Round(totalPayback - principal, 2);

                loanCaseDTO.MonthlyPaybackAmount = monthlyPayback;
                loanCaseDTO.TotalPaybackAmount = totalPayback;
                loanCaseDTO.ApprovedInterestPayment = totalInterest;
                loanCaseDTO.AppraisedAmount = principal;
                loanCaseDTO.ApprovedAmount = principal;

                loanCaseDTO.TotalLoansBalance = totalPayback;
                loanCaseDTO.LoanProductLoanBalance = principal;

                // 9. CREATE LOAN
                loanCaseDTO.CreatedBy = User.Identity.Name;
                loanCaseDTO.Status = 48826;
                loanCaseDTO.ReceivedDate = DateTime.UtcNow;

                // Fix for BatchNumber constraint
                if (loanCaseDTO.BatchNumber == null || loanCaseDTO.BatchNumber == 0)
                {
                    // Generate a unique batch number using timestamp
                    loanCaseDTO.BatchNumber = int.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
                }

                var createResult = await master._channelService
                    .AddLoanCaseAsync(loanCaseDTO.MapTo<LoanCaseDTO>(), serviceHeader);

                if (!string.IsNullOrWhiteSpace(createResult.ErrorMessageResult))
                    return Ok(ApiResponse<string>.Fail("Error posting this loan.", createResult.ErrorMessageResult));

                #region // 10. SECTOR
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(@"
INSERT INTO LoanCaseSectorClassification
    (LoanCaseId, SectorCode, SubSectorCode)
VALUES
    (@LoanCaseId, @SectorCode, @SubSectorCode)", conn))
                {
                    cmd.Parameters.Add("@LoanCaseId", SqlDbType.UniqueIdentifier).Value = createResult.Id;
                    cmd.Parameters.Add("@SectorCode", SqlDbType.VarChar, 20).Value = loanCaseDTO.SectorCode;
                    cmd.Parameters.Add("@SubSectorCode", SqlDbType.VarChar, 30).Value = loanCaseDTO.SubSectorCode;

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
                #endregion

                // 11. COLLATERALS
                if (collateralDocuments.Any())
                {
                    await master._channelService.UpdateLoanCollateralsByLoanCaseIdAsync(
                        createResult.Id,
                        new ObservableCollection<CustomerDocumentDTO>(collateralDocuments),
                        serviceHeader);
                }

                // 12. GUARANTORS
                await master._channelService.UpdateLoanGuarantorsByLoanCaseIdAsync(
                    createResult.Id,
                    new ObservableCollection<LoanGuarantorDTO>(guarantors),
                    serviceHeader);

                var fullName = $"{customer.IndividualFirstName} {customer.IndividualLastName}";
                var loanName = loanProduct.Description;

                return Ok(ApiResponse<string>.Ok(
                    "Loan created successfully.",
                    $"{loanName} loan for {fullName} registered successfully. Net disbursement: {netDisbursement:N2}"
                ));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<string>.Fail("System error occurred.", ex.Message));
            }
        }


        [HttpPut]
        [Route("UpdateLoanCase")]
        public async Task<IHttpActionResult> UpdateLoanCase(LoanCaseDTO loanCaseDTO)
        {
            try
            {
                // Validate input
                if (loanCaseDTO == null || loanCaseDTO.Id == Guid.Empty)
                {
                    return Ok(ApiResponse<string>.Fail(
                        "Invalid request. Loan case data is required and ID must be provided."));
                }

                var serviceHeader = master.GetServiceHeader();

                // Optional: Add authorization check (implement this method if needed)
                // if (!UserHasPermission(serviceHeader, "UpdateLoanCase"))
                // {
                //     return Unauthorized();
                // }

                var success = await master._channelService.UpdateLoanCaseAsync(loanCaseDTO, serviceHeader);

                if (success)
                {
                    return Ok(ApiResponse<bool>.Ok(true, "Loan case updated successfully."));
                }
                else
                {
                    return Ok(ApiResponse<bool>.Fail(
                        "Failed to update loan case. The loan case could not be updated."));
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                // _logger.LogError(ex, "Error updating loan case with ID: {LoanCaseId}", loanCaseDTO?.Id);

                return InternalServerError(new Exception("An error occurred while updating the loan case.", ex));
            }
        }


        [HttpPost]
        [Route("loan/appraisal")]
        public async Task<IHttpActionResult> LoanAppraisal([FromBody] LoanCaseDTO request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var serviceHeader = master.GetServiceHeader();

            var loanCase = await master._channelService.FindLoanCaseAsync(request.Id, serviceHeader);
            if (request.IsBatched == true)
            {
                decimal Intrest = request.InterestBalance;
                decimal totalIncome = request.LoanRegistrationTotalIncome;
                decimal existingDeductions = request.LoanRegistrationTotalDeduction;

                decimal newLoanRepayment = request.LoanRegistrationAbilityToPay;

                decimal maxAllowedDeductions = totalIncome * 2m / 3m;
                decimal projectedTotalDeductions = existingDeductions + newLoanRepayment;

                if (projectedTotalDeductions > maxAllowedDeductions)
                {
                    return BadRequest("2/3 rule violation. Total deductions exceed allowable limit.");

                }
            }
            //loanCase.AppraisalRemarks = request.AppraisalRemarks;

            ////loanCase.AppraisedAmount = request.AppraisedAmount;
            ////loanCase.AppraisedAmountRemarks = request.AppraisedAmountRemarks;
            ////loanCase.AppraisedNetIncome = request.AppraisedNetIncome;
            ////loanCase.AppraisedAbility = request.AppraisedAbility;
            ////loanCase.ApprovedAmount = request.ApprovedAmount;

            //loanCase.LoanRegistrationMaximumEntitled = request.LoanRegistrationMaximumEntitled;
            //loanCase.LoanRegistrationNetIncome = request.LoanRegistrationNetIncome;
            //loanCase.LoanRegistrationTotalAllowance = request.LoanRegistrationTotalAllowance;
            //loanCase.LoanRegistrationTotalDeduction = request.LoanRegistrationTotalDeduction;
            //loanCase.LoanRegistrationTotalIncome = request.LoanRegistrationTotalIncome;
            //loanCase.LoanRegistrationAbilityToPay = request.LoanRegistrationAbilityToPay;
            //loanCase.LoanRegistrationAbilityToPayOverLoanTerm = request.LoanRegistrationAbilityToPayOverLoanTerm;
            //loanCase.LoanRegistrationLoanPlusInterest = request.LoanRegistrationLoanPlusInterest;

            loanCase.TotalLoansBalance = request.TotalLoansBalance;
            //loanCase.LoanProductInvestmentsBalance = request.LoanProductInvestmentsBalance;
            //loanCase.LoanProductTotalSharesInvestmentsBalance = request.LoanProductTotalSharesInvestmentsBalance;
            //loanCase.Status = (int)LoanCaseStatus.Registered;


            if (loanCase == null)
                return NotFoundResponse("Loan case not found.");

            //loanCase.ValidateAll();
            //if (loanCase.HasErrors)
            //    return ValidationErrorResponse(loanCase.ErrorMessages);

            // Phase 1: Appraisal

            var appraised = await master._channelService.AppraiseLoanCaseAsync(loanCase, (int)LoanAppraisalOption.Appraise, 1, serviceHeader);

            if (!appraised)
                return FailureResponse("Loan appraisal failed.");


            // Refresh state only after successful appraisal
            //  await master._channelService.UpdateLoanCaseAsync(loanCase, serviceHeader);
            loanCase = await master._channelService.FindLoanCaseAsync(loanCase.Id, serviceHeader);

            loanCase.Status = (int)LoanCaseStatus.Approved;

            // Phase 2: Audit
            var audited = await master._channelService.AuditLoanCaseAsync(loanCase, (int)LoanAuditOption.Audit, serviceHeader);

            if (!audited)
            {
                string message1 =
                    $"Dear {loanCase.Customer.IndividualFirstName} {loanCase.Customer.IndividualLastName}, " +
                    $"loan application of KES {loanCase.AmountApplied:N0} did not meet the appraisal requirements at this time. " +
                    $"Please contact us for further clarification or future consideration.";
                await SmsHelper.SendMessageAsync(loanCase.Customer.AddressMobileLine, message1);

                return FailureResponse("Loan audit failed.");
            }
            //        string message = $"Dear {loanCase.Customer.IndividualFirstName} {loanCase.Customer.IndividualLastName}, " +
            //$"your loan application of KES {loanCase.AmountApplied:N0} has successfully passed appraisal and is awaiting final approval. " +
            //$"We will keep you informed of the next steps.";
            //        await SmsHelper.SendMessageAsync(loanCase.Customer.AddressMobileLine, message);


            //decimal rate = Convert.ToDecimal(loanCase.LoanInterestAnnualPercentageRate / 100); // if stored as 12 not 0.12
            //decimal termInYears = loanCase.LoanRegistrationTermInMonths / 12m;

            //loanCase.ApprovedInterestPayment = principal * rate * termInYears;
            //loanCase.TotalPaybackAmount = loanCase.AmountApplied + loanCase.ApprovedInterestPayment;
            //loanCase.AppraisedAmount = loanCase.AmountApplied;


            decimal principal = loanCase.AmountApplied;

            decimal annualRatePercent = (decimal)loanCase.LoanInterestAnnualPercentageRate; // e.g. 12
            decimal monthlyRate = (annualRatePercent / 100m) / 12m;

            int months = loanCase.LoanRegistrationTermInMonths;

            // EMI (annuity)
            decimal pow = (decimal)Math.Pow((double)(1 + monthlyRate), months);

            decimal rawEmi = principal * (monthlyRate * pow) / (pow - 1);

            // ROUND EMI FIRST (bank-grade behavior)
            decimal monthlyPayback = Math.Round(rawEmi, 2);

            // Totals derived from rounded EMI
            decimal totalPayback = Math.Round(monthlyPayback * months, 2);
            decimal totalInterest = Math.Round(totalPayback - principal, 2);

            // Assign snapshot
            loanCase.MonthlyPaybackAmount = monthlyPayback;
            loanCase.TotalPaybackAmount = totalPayback;
            loanCase.ApprovedInterestPayment = totalInterest;
            loanCase.AppraisedAmount = principal;
            loanCase.ApprovedAmount = principal;

            // Opening balances
            loanCase.TotalLoansBalance = totalPayback;          // total obligation
            loanCase.LoanProductLoanBalance = principal;        // outstanding principal at disbursement

            #region
            //            try
            //            {
            //                using (SqlConnection conn = new SqlConnection(_connectionString))
            //                {
            //                    conn.Open();

            //                    using (SqlTransaction tx = conn.BeginTransaction())
            //                    {
            //                        // 1. VALIDATE EXISTENCE
            //                        using (SqlCommand checkCmd = new SqlCommand(
            //                            "SELECT COUNT(1) FROM swiftFin_LoanCases WHERE Id = @Id", conn, tx))
            //                        {
            //                            checkCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
            //                                .Value = loanCase.Id;

            //                            if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
            //                                throw new Exception("Validation failed: Loan case not found for update.");
            //                        }

            //                        // 2. UPDATE FINANCIAL & APPRAISAL SNAPSHOT
            //                        using (SqlCommand updateCmd = new SqlCommand(@"
            //UPDATE swiftFin_LoanCases
            //SET
            //    ApprovedAmount            = @ApprovedAmount,
            //    ApprovedInterestPayment   = @ApprovedInterestPayment,
            //    TotalPaybackAmount        = @TotalPaybackAmount,
            //    TotalLoansBalance         = @TotalLoansBalance,
            //    MonthlyPaybackAmount      = @MonthlyPaybackAmount,
            //    AppraisedAmount           = @AppraisedAmount,
            //    LoanProductLoanBalance    = @LoanProductLoanBalance
            //WHERE Id = @Id
            //", conn, tx))
            //                        {
            //                            var pApproved = updateCmd.Parameters.Add("@ApprovedAmount", SqlDbType.Decimal);
            //                            pApproved.Precision = 18; pApproved.Scale = 2;
            //                            pApproved.Value = loanCase.ApprovedAmount = loanCase.AmountApplied;

            //                            var pInterest = updateCmd.Parameters.Add("@ApprovedInterestPayment", SqlDbType.Decimal);
            //                            pInterest.Precision = 18; pInterest.Scale = 2;
            //                            pInterest.Value = loanCase.ApprovedInterestPayment;

            //                            var pTotal = updateCmd.Parameters.Add("@TotalPaybackAmount", SqlDbType.Decimal);
            //                            pTotal.Precision = 18; pTotal.Scale = 2;
            //                            pTotal.Value = loanCase.TotalPaybackAmount;

            //                            var pBalance = updateCmd.Parameters.Add("@TotalLoansBalance", SqlDbType.Decimal);
            //                            pBalance.Precision = 18; pBalance.Scale = 2;
            //                            pBalance.Value = loanCase.TotalLoansBalance = loanCase.ApprovedAmount + loanCase.ApprovedInterestPayment;

            //                            var pMonthly = updateCmd.Parameters.Add("@MonthlyPaybackAmount", SqlDbType.Decimal);
            //                            pMonthly.Precision = 18; pMonthly.Scale = 2;
            //                            pMonthly.Value = loanCase.MonthlyPaybackAmount;

            //                            var pAppraised = updateCmd.Parameters.Add("@AppraisedAmount", SqlDbType.Decimal);
            //                            pAppraised.Precision = 18; pAppraised.Scale = 2;
            //                            pAppraised.Value = loanCase.AppraisedAmount;

            //                            var pProductBalance = updateCmd.Parameters.Add("@LoanProductLoanBalance", SqlDbType.Decimal);
            //                            pProductBalance.Precision = 18; pProductBalance.Scale = 2;
            //                            pProductBalance.Value = loanCase.LoanProductLoanBalance;

            //                            updateCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
            //                                .Value = loanCase.Id;

            //                            if (updateCmd.ExecuteNonQuery() != 1)
            //                                throw new Exception("Update failed: unexpected number of rows affected.");
            //                        }

            //                        tx.Commit();
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                throw; // bubble up to API/logging layer
            //            }
            #endregion

            return Ok(SuccessResponse("Loan appraised and audited successfully."));
        }


        [HttpPost]
        [Route("Approve")]
        public async Task<IHttpActionResult> ApproveLoan([FromBody] LoanCaseDTO request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var serviceHeader = master.GetServiceHeader();

            var loanCases = await master._channelService.FindLoanCasesAsync(serviceHeader);

            var loanCaseDTO = loanCases?.FirstOrDefault(c => c.Id == request.Id);

            if (loanCaseDTO == null)
            {
                return Content(HttpStatusCode.NotFound, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Loan case not found."
                });
            }

            //loanCaseDTO.ValidateAll();

            //if (loanCaseDTO.HasErrors)
            //{
            //    return Content(HttpStatusCode.InternalServerError, new ApiResponse<object>
            //    {
            //        Success = false,
            //        Message = "Validation failed.",
            //        Data = loanCaseDTO.ErrorMessages
            //    });
            //}
            loanCaseDTO.ApprovedAmount = loanCaseDTO.AmountApplied;


            var auditResult = await master._channelService.ApproveLoanCaseAsync(loanCaseDTO, (int)LoanApprovalOption.Approve, serviceHeader);

            if (!auditResult)
            {
                return Content(HttpStatusCode.InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Loan Approval failed during approval."
                });
            }

            var loanProductDTO = await master._channelService.FindLoanProductAsync(loanCaseDTO.LoanProductId, serviceHeader);

            string message =
    $"Dear {loanCaseDTO.Customer.IndividualFirstName} {loanCaseDTO.Customer.IndividualLastName}, " +
    $"Your {loanProductDTO.Description} Loan of KES {loanCaseDTO.AmountApplied:N0} has been approved, " +
    $"awaiting disbursement.";
            await SmsHelper.SendMessageAsync(loanCaseDTO.Customer.AddressMobileLine, message);

            decimal principal = loanCaseDTO.AmountApplied;

            decimal annualRatePercent = (decimal)loanCaseDTO.LoanInterestAnnualPercentageRate; // e.g. 12
            decimal monthlyRate = (annualRatePercent / 100m) / 12m;

            int months = loanCaseDTO.LoanRegistrationTermInMonths;

            // EMI (annuity)
            decimal pow = (decimal)Math.Pow((double)(1 + monthlyRate), months);

            decimal rawEmi = principal * (monthlyRate * pow) / (pow - 1);

            // ROUND EMI FIRST (bank-grade behavior)
            decimal monthlyPayback = Math.Round(rawEmi, 2);

            // Totals derived from rounded EMI
            decimal totalPayback = Math.Round(monthlyPayback * months, 2);
            decimal totalInterest = Math.Round(totalPayback - principal, 2);

            // Assign snapshot
            loanCaseDTO.MonthlyPaybackAmount = monthlyPayback;
            loanCaseDTO.TotalPaybackAmount = totalPayback;
            loanCaseDTO.ApprovedInterestPayment = totalInterest;
            loanCaseDTO.AppraisedAmount = principal;
            loanCaseDTO.ApprovedAmount = principal;

            // Opening balances
            loanCaseDTO.TotalLoansBalance = principal;          // total obligation
            loanCaseDTO.LoanProductLoanBalance = principal;        // outstanding principal at disbursement

            loanCaseDTO.Status = (int)LoanCaseStatus.Approved;

            #region
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        // 1. VALIDATE EXISTENCE
                        using (SqlCommand checkCmd = new SqlCommand(
                            "SELECT COUNT(1) FROM swiftFin_LoanCases WHERE Id = @Id", conn, tx))
                        {
                            checkCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                                .Value = loanCaseDTO.Id;

                            if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
                                throw new Exception("Validation failed: Loan case not found for update.");
                        }

                        // 2. UPDATE FINANCIAL & APPRAISAL SNAPSHOT INCLUDING STATUS
                        using (SqlCommand updateCmd = new SqlCommand(@"
            UPDATE swiftFin_LoanCases
            SET
                ApprovedAmount            = @ApprovedAmount,
                ApprovedInterestPayment   = @ApprovedInterestPayment,
                TotalPaybackAmount        = @TotalPaybackAmount,
                TotalLoansBalance         = @TotalLoansBalance,
                MonthlyPaybackAmount      = @MonthlyPaybackAmount,
                AppraisedAmount           = @AppraisedAmount,
                LoanProductLoanBalance    = @LoanProductLoanBalance,
                Status                    = @Status
            WHERE Id = @Id
            ", conn, tx))
                        {
                            var pApproved = updateCmd.Parameters.Add("@ApprovedAmount", SqlDbType.Decimal);
                            pApproved.Precision = 18; pApproved.Scale = 2;
                            pApproved.Value = loanCaseDTO.ApprovedAmount = loanCaseDTO.AmountApplied;

                            var pInterest = updateCmd.Parameters.Add("@ApprovedInterestPayment", SqlDbType.Decimal);
                            pInterest.Precision = 18; pInterest.Scale = 2;
                            pInterest.Value = loanCaseDTO.ApprovedInterestPayment;

                            var pTotal = updateCmd.Parameters.Add("@TotalPaybackAmount", SqlDbType.Decimal);
                            pTotal.Precision = 18; pTotal.Scale = 2;
                            pTotal.Value = loanCaseDTO.TotalPaybackAmount;

                            var pBalance = updateCmd.Parameters.Add("@TotalLoansBalance", SqlDbType.Decimal);
                            pBalance.Precision = 18; pBalance.Scale = 2;
                            pBalance.Value = loanCaseDTO.TotalLoansBalance;

                            var pMonthly = updateCmd.Parameters.Add("@MonthlyPaybackAmount", SqlDbType.Decimal);
                            pMonthly.Precision = 18; pMonthly.Scale = 2;
                            pMonthly.Value = loanCaseDTO.MonthlyPaybackAmount;

                            var pAppraised = updateCmd.Parameters.Add("@AppraisedAmount", SqlDbType.Decimal);
                            pAppraised.Precision = 18; pAppraised.Scale = 2;
                            pAppraised.Value = loanCaseDTO.AppraisedAmount;

                            var pProductBalance = updateCmd.Parameters.Add("@LoanProductLoanBalance", SqlDbType.Decimal);
                            pProductBalance.Precision = 18; pProductBalance.Scale = 2;
                            pProductBalance.Value = loanCaseDTO.LoanProductLoanBalance;

                            // NEW: Status
                            updateCmd.Parameters.Add("@Status", SqlDbType.Int).Value = loanCaseDTO.Status;

                            updateCmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                                .Value = loanCaseDTO.Id;

                            if (updateCmd.ExecuteNonQuery() != 1)
                                throw new Exception("Update failed: unexpected number of rows affected.");
                        }

                        tx.Commit();
                    }
                }
            }
            catch
            {
                throw; // bubble up to API/logging layer
            }
            #endregion

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Loan Approval successfully."
            });
        }


        [HttpPost]
        [Route("LoanCancellation")]
        public async Task<IHttpActionResult> LoanCancellation([FromBody] LoanCaseDTO loanCaseDTO)
        {
            var serviceHeader = master.GetServiceHeader();

            if (loanCaseDTO == null)
                return Json(new ApiResponse<object> { Success = false, Message = "Invalid data.", Data = null });



            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {
                var result = await master._channelService.CancelLoanCaseAsync(loanCaseDTO, loanCaseDTO.LoanAuditOption, serviceHeader);


                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Message = "loan  Cancelled successfully."
                });
            }

            return Json(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed.",
                Data = loanCaseDTO.ErrorMessages
            });
        }



        // Request DTO
        public class LoanTopUpRequest
        {
            public Guid OriginalLoanCaseId { get; set; }
            public decimal TopUpAmount { get; set; }
            public Guid BankAccountId { get; set; }
            public ServiceHeader ServiceHeader { get; set; } // pass user info, context, etc.
        }






        #region  PDF

        [HttpGet]
        [Route("CustomerLoanLedgerPdf")]
        public IHttpActionResult CustomerLoanLedgerPdf(Guid customerId)
        {
            var results = new List<CustomerLoanLedgerDto>();

            #region SQL
            var sql = @"
WITH Tx AS (
    SELECT
        ca.Id AS CustomerAccountId,
        c.Individual_FirstName + ' ' + c.Individual_LastName AS FullName,
        CONCAT(
            RIGHT('000' + CAST(b.Code AS VARCHAR(10)), 3), '-',
            RIGHT('000000' + CAST(c.SerialNumber AS VARCHAR(10)), 6), '-',
            RIGHT('000' + CAST(ca.CustomerAccountType_ProductCode AS VARCHAR(10)), 3), '-',
            RIGHT('000' + CAST(ca.CustomerAccountType_TargetProductCode AS VARCHAR(10)), 3)
        ) AS AccountNumber,
        lp.Description AS LoanProductName,
        je.CreatedDate,
        je.Amount
    FROM [swiftFin_JournalEntries] je
    INNER JOIN [swiftFin_CustomerAccounts] ca ON je.CustomerAccountId = ca.Id
    INNER JOIN [swiftFin_Customers] c ON ca.CustomerId = c.Id
    INNER JOIN [swiftFin_LoanProducts] lp ON ca.CustomerAccountType_TargetProductId = lp.Id
    LEFT JOIN [swiftFin_Branches] b ON ca.BranchId = b.Id
    WHERE ca.CustomerId = @CustomerId
)
SELECT *,
    SUM(Amount) OVER (
        PARTITION BY CustomerAccountId
        ORDER BY CreatedDate
        ROWS UNBOUNDED PRECEDING
    ) AS RunningBalance
FROM Tx
ORDER BY AccountNumber, CreatedDate;";
            #endregion

            #region DATA FETCH
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@CustomerId", SqlDbType.UniqueIdentifier).Value = customerId;
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new CustomerLoanLedgerDto
                        {
                            CustomerAccountId = reader.GetGuid(reader.GetOrdinal("CustomerAccountId")),
                            FullName = reader.GetString(reader.GetOrdinal("FullName")),
                            AccountNumber = reader.GetString(reader.GetOrdinal("AccountNumber")),
                            LoanProductName = reader.GetString(reader.GetOrdinal("LoanProductName")),
                            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                            Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                            RunningBalance = reader.GetDecimal(reader.GetOrdinal("RunningBalance"))
                        });
                    }
                }
            }

            if (!results.Any())
                return BadRequest("No ledger records found.");
            #endregion

            #region PDF BUILD
            byte[] pdfBytes;

            using (var ms = new MemoryStream())
            {
                var doc = new Document(PageSize.A4, 36, 36, 36, 36);
                var writer = PdfWriter.GetInstance(doc, ms);
                writer.PageEvent = new PageBackground(); // light blue page tint

                doc.Open();

                AddHeader(doc, results.First());
                AddLedgerTable(doc, results);
                AddFooter(doc);

                doc.Close();
                pdfBytes = ms.ToArray();
            }
            #endregion

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(pdfBytes)
            };

            response.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

            response.Content.Headers.ContentDisposition =
                new System.Net.Http.Headers.ContentDispositionHeaderValue("inline")
                {
                    FileName = "Rubani_Loan_Ledger.pdf"
                };

            return ResponseMessage(response);
        }


        // ============================
        // PDF STYLES
        // ============================
        static Font H1 = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
        static Font H2 = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
        static Font Normal = FontFactory.GetFont(FontFactory.HELVETICA, 9);
        static Font Bold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);

        static BaseColor BrandBlue = new BaseColor(220, 235, 250);
        public class PageBackground : PdfPageEventHelper
        {
            public override void OnEndPage(PdfWriter writer, Document document)
            {
                var cb = writer.DirectContentUnder;
                cb.SetColorFill(new BaseColor(245, 250, 255));

                cb.Rectangle(
                    document.LeftMargin,
                    document.BottomMargin,
                    document.PageSize.Width - document.LeftMargin - document.RightMargin,
                    document.PageSize.Height - document.TopMargin - document.BottomMargin
                );
                cb.Fill();
            }
        }


        // ============================
        // HEADER
        // ============================
        private void AddHeader(Document doc, CustomerLoanLedgerDto first)
        {
            var headerTable = new PdfPTable(2);
            headerTable.WidthPercentage = 100;
            headerTable.SetWidths(new float[] { 1f, 3f });

            var logoPath = HttpContext.Current.Server.MapPath("~/Assets/Images/Rubani-logo.jpeg");
            var logo = Image.GetInstance(logoPath);
            logo.ScaleToFit(80f, 80f);

            var logoCell = new PdfPCell(logo)
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 5
            };

            var titleCell = new PdfPCell
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_CENTER
            };

            titleCell.AddElement(new Paragraph("RUBANI SACCO SOCIETY LTD", H1));
            titleCell.AddElement(new Paragraph("Loan Account Statement", H2));

            headerTable.AddCell(logoCell);
            headerTable.AddCell(titleCell);

            doc.Add(headerTable);
            doc.Add(new Paragraph(" "));

            var info = new PdfPTable(2);
            info.WidthPercentage = 100;
            info.SetWidths(new float[] { 1, 1 });

            info.AddCell(InfoCell($"Member Name: {first.FullName}"));
            info.AddCell(InfoCell($"Account No: {first.AccountNumber}"));
            info.AddCell(InfoCell($"Loan Product: {first.LoanProductName}"));
            info.AddCell(InfoCell($"Generated: {DateTime.Now:dd MMM yyyy}"));

            doc.Add(info);
            doc.Add(new Paragraph(" "));
        }


        // ============================
        // LEDGER TABLE (NO IDS)
        // ============================
        private void AddLedgerTable(Document doc, List<CustomerLoanLedgerDto> data)
        {
            var table = new PdfPTable(4);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1.2f, 2.5f, 1f, 1f });

            table.AddCell(HeaderCell("Date"));
            table.AddCell(HeaderCell("Description"));
            table.AddCell(HeaderCell("Amount"));
            table.AddCell(HeaderCell("Balance"));

            foreach (var tx in data)
            {
                table.AddCell(Cell(tx.CreatedDate.ToString("dd-MMM-yyyy")));
                table.AddCell(Cell(tx.LoanProductName));
                table.AddCell(Cell(tx.Amount.ToString("N2"), Element.ALIGN_RIGHT));
                table.AddCell(Cell(tx.RunningBalance.ToString("N2"), Element.ALIGN_RIGHT));
            }

            doc.Add(table);

            var closing = data.Last().RunningBalance;
            doc.Add(new Paragraph($"Closing Balance: {closing:N2}", Bold)
            { Alignment = Element.ALIGN_RIGHT });
        }

        // ============================
        // FOOTER
        // ============================
        private void AddFooter(Document doc)
        {
            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph("System Generated Statement", Normal));
            doc.Add(new Paragraph("Rubani SACCO Society Ltd", Bold));
        }

        // ============================
        // CELL HELPERS
        // ============================

        private PdfPCell Cell(string text, int align = Element.ALIGN_LEFT)
        {
            return new PdfPCell(new Phrase(text, Normal))
            {
                HorizontalAlignment = align,
                Padding = 5
            };
        }

        private PdfPCell HeaderCell(string text)
        {
            return new PdfPCell(new Phrase(text, Bold))
            {
                BackgroundColor = BrandBlue,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 6
            };
        }



        private PdfPCell InfoCell(string text)
        {
            return new PdfPCell(new Phrase(text, Normal))
            {
                Border = Rectangle.NO_BORDER,
                Padding = 4
            };
        }


        // ============================
        // DTO
        // ============================
        private class CustomerLoanLedgerDto
        {
            public Guid CustomerAccountId { get; set; } // internal only
            public string FullName { get; set; }
            public string AccountNumber { get; set; }
            public string LoanProductName { get; set; }
            public DateTime CreatedDate { get; set; }
            public decimal Amount { get; set; }
            public decimal RunningBalance { get; set; }
        }
        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }

            public static ApiResponse<T> Ok(T data, string message = null) =>
                new ApiResponse<T> { Success = true, Message = message, Data = data };

            public static ApiResponse<T> Fail(string message, T data = default) =>
                new ApiResponse<T> { Success = false, Message = message, Data = data };
        }





        [HttpPost]
        [Route("send")]
        public async Task<IHttpActionResult> SendMessage()
        {
            SendMessageRequest request = new SendMessageRequest();
            request.PhoneNumber = "254742199073";
            request.Message = "hello there test";

            if (request == null)
                return BadRequest("Invalid request payload.");

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                return BadRequest("Phone number is required.");

            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Message cannot be empty.");

            bool sent = await SmsHelper.SendMessageAsync(request.PhoneNumber, request.Message);

            if (sent)
            {
                return Ok(new
                {
                    success = true,
                    message = "Message sent successfully.",
                    phone = request.PhoneNumber,
                    text = request.Message
                });
            }

            return Ok(new
            {
                success = false,
                message = "FAILED to send message."
            });
        }

        private LoanCaseStatus? ResolveLoanStatus(string status)
        {

            switch (status.Trim().ToLower())
            {
                case "registered":
                    return LoanCaseStatus.Registered;

                case "appraised":
                    return LoanCaseStatus.Appraised;

                case "approved":
                    return LoanCaseStatus.Approved;

                case "disbursed":
                    return LoanCaseStatus.Disbursed;

                case "rejected":
                    return LoanCaseStatus.Rejected;

                case "deferred":
                    return LoanCaseStatus.Deferred;

                case "audited":
                case "verified": // supporting alias
                    return LoanCaseStatus.Audited;

                case "restructured":
                    return LoanCaseStatus.Restructured;

                default:
                    return null;
            }
        }

        private LoanCaseFilter? ResolveLoanFilter(int filterType)
        {
            if (Enum.IsDefined(typeof(LoanCaseFilter), filterType))
                return (LoanCaseFilter)filterType;

            return null;
        }
        public class SendMessageRequest
        {
            public string PhoneNumber { get; set; }
            public string Message { get; set; }
        }
        public sealed class LoanAppraisalRequest
        {
            [Required]
            public Guid LoanCaseId { get; set; }

            [Range(0, int.MaxValue)]
            public int LoanAuditOption { get; set; }
        }
        private IHttpActionResult FailureResponse(string message) =>
            Content(HttpStatusCode.InternalServerError,
                new ApiResponse<object> { Success = false, Message = message });

        private IHttpActionResult ValidationErrorResponse(object errors) =>
            Content(HttpStatusCode.ExpectationFailed,
                new ApiResponse<object> { Success = false, Message = "Validation failed.", Data = errors });

        private IHttpActionResult NotFoundResponse(string message) =>
            Content(HttpStatusCode.NotFound,
                new ApiResponse<object> { Success = false, Message = message });

        private ApiResponse<object> SuccessResponse(string message) =>
            new ApiResponse<object> { Success = true, Message = message };
        private void MapLoanProductAttributes(LoanCaseDTO2 dto, LoanProductDTO p)
        {
            if (dto == null || p == null)
                return;

            dto.LoanRegistrationPaymentFrequencyPerYear = p.LoanRegistrationPaymentFrequencyPerYear;
            dto.LoanRegistrationMinimumAmount = p.LoanRegistrationMinimumAmount;
            dto.LoanRegistrationMinimumInterestAmount = p.LoanRegistrationMinimumInterestAmount;
            dto.LoanRegistrationMinimumGuarantors = p.LoanRegistrationMinimumGuarantors;
            dto.LoanRegistrationMinimumMembershipPeriod = p.LoanRegistrationMinimumMembershipPeriod;
            dto.LoanRegistrationMaximumGuarantees = p.LoanRegistrationMaximumGuarantees;
            dto.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement = p.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement;
            dto.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage = p.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage;
            dto.LoanRegistrationLoanProductSection = p.LoanRegistrationLoanProductSection;
            dto.LoanRegistrationLoanProductCategory = p.LoanRegistrationLoanProductCategory;
            dto.LoanRegistrationConsecutiveIncome = p.LoanRegistrationConsecutiveIncome;
            dto.LoanRegistrationInvestmentsMultiplier = p.LoanRegistrationInvestmentsMultiplier;
            dto.LoanRegistrationRejectIfMemberHasBalance = p.LoanRegistrationRejectIfMemberHasBalance;
            dto.LoanRegistrationSecurityRequired = p.LoanRegistrationSecurityRequired;
            dto.LoanRegistrationAllowSelfGuarantee = p.LoanRegistrationAllowSelfGuarantee;
            dto.LoanRegistrationGracePeriod = p.LoanRegistrationGracePeriod;
            dto.LoanRegistrationPaymentDueDate = p.LoanRegistrationPaymentDueDate;
            dto.LoanRegistrationPayoutRecoveryMode = p.LoanRegistrationPayoutRecoveryMode;
            dto.LoanRegistrationPayoutRecoveryPercentage = p.LoanRegistrationPayoutRecoveryPercentage;
            dto.LoanRegistrationAggregateCheckOffRecoveryMode = p.LoanRegistrationAggregateCheckOffRecoveryMode;
            dto.LoanRegistrationChargeClearanceFee = p.LoanRegistrationChargeClearanceFee;
            dto.LoanRegistrationMicrocredit = p.LoanRegistrationMicrocredit;
            dto.LoanRegistrationStandingOrderTrigger = p.LoanRegistrationStandingOrderTrigger;
            dto.LoanRegistrationTrackArrears = p.LoanRegistrationTrackArrears;
            dto.LoanRegistrationChargeArrearsFee = p.LoanRegistrationChargeArrearsFee;
            dto.LoanRegistrationEnforceSystemAppraisalRecommendation = p.LoanRegistrationEnforceSystemAppraisalRecommendation;
            dto.LoanRegistrationBypassAudit = p.LoanRegistrationBypassAudit;
            dto.LoanRegistrationGuarantorSecurityMode = p.LoanRegistrationGuarantorSecurityMode;
            dto.LoanRegistrationRoundingType = p.LoanRegistrationRoundingType;
            dto.LoanRegistrationDisburseMicroLoanLessDeductions = p.LoanRegistrationDisburseMicroLoanLessDeductions;
            dto.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal = p.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal;
            dto.LoanRegistrationThrottleScheduledArrearsRecovery = p.LoanRegistrationThrottleScheduledArrearsRecovery;
            dto.LoanRegistrationCreateStandingOrderOnLoanAudit = p.LoanRegistrationCreateStandingOrderOnLoanAudit;

            // Interest attributes
            dto.LoanInterestAnnualPercentageRate = p.LoanInterestAnnualPercentageRate;
            dto.LoanInterestChargeMode = p.LoanInterestChargeMode;
            dto.LoanInterestRecoveryMode = p.LoanInterestRecoveryMode;
            dto.LoanInterestCalculationMode = p.LoanInterestCalculationMode;

            // Loan product descriptions
            dto.LoanProductDescription = p.Description;
            dto.InterestCalculationModeDescription = p.LoanInterestCalculationModeDescription;
            dto.LoanProductSectionDescription = p.LoanRegistrationLoanProductSectionDescription;

            // Term & ceilings
            dto.LoanRegistrationTermInMonths = p.LoanRegistrationTermInMonths;
            dto.LoanRegistrationMaximumAmount = p.LoanRegistrationMaximumAmount;

            // Take home rules
            dto.TakeHomeType = p.TakeHomeType;
            dto.TakeHomePercentage = p.TakeHomePercentage;
            dto.TakeHomeFixedAmount = p.TakeHomeFixedAmount;

            // Core identity linkage
            dto.LoanProductId = p.Id;
        }
        #endregion

    }
}
