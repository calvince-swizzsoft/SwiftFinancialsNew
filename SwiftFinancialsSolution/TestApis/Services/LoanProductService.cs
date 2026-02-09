using Application.MainBoundedContext.DTO.AccountsModule;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TestApis.Services
{
    public class LoanProductService
    {
        private readonly string _connectionString;

        public LoanProductService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public IEnumerable<LoanProductDTO> GetAll()
        {
            var list = new List<LoanProductDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT lp.*,
                                pa.AccountCode as ChartOfAccountAccountCode,
                                pa.AccountName as ChartOfAccountAccountName,
                                pa.AccountType as ChartOfAccountAccountType,
                                ira.AccountCode as InterestReceivedChartOfAccountAccountCode,
                                ira.AccountName as InterestReceivedChartOfAccountAccountName,
                                ira.AccountType as InterestReceivedChartOfAccountAccountType,
                                irv.AccountCode as InterestReceivableChartOfAccountAccountCode,
                                irv.AccountName as InterestReceivableChartOfAccountAccountName,
                                irv.AccountType as InterestReceivableChartOfAccountAccountType,
                                icc.AccountCode as InterestChargedChartOfAccountAccountCode,
                                icc.AccountName as InterestChargedChartOfAccountAccountName,
                                icc.AccountType as InterestChargedChartOfAccountAccountType
                                FROM [swiftFin_LoanProducts] lp
                                LEFT JOIN [swiftFin_ChartOfAccounts] pa ON lp.ChartOfAccountId = pa.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] ira ON lp.InterestReceivedChartOfAccountId = ira.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] irv ON lp.InterestReceivableChartOfAccountId = irv.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] icc ON lp.InterestChargedChartOfAccountId = icc.Id
                                ORDER BY lp.Code";
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        public LoanProductDTO GetById(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT lp.*,
                                pa.AccountCode as ChartOfAccountAccountCode,
                                pa.AccountName as ChartOfAccountAccountName,
                                pa.AccountType as ChartOfAccountAccountType,
                                ira.AccountCode as InterestReceivedChartOfAccountAccountCode,
                                ira.AccountName as InterestReceivedChartOfAccountAccountName,
                                ira.AccountType as InterestReceivedChartOfAccountAccountType,
                                irv.AccountCode as InterestReceivableChartOfAccountAccountCode,
                                irv.AccountName as InterestReceivableChartOfAccountAccountName,
                                irv.AccountType as InterestReceivableChartOfAccountAccountType,
                                icc.AccountCode as InterestChargedChartOfAccountAccountCode,
                                icc.AccountName as InterestChargedChartOfAccountAccountName,
                                icc.AccountType as InterestChargedChartOfAccountAccountType
                                FROM [swiftFin_LoanProducts] lp
                                LEFT JOIN [swiftFin_ChartOfAccounts] pa ON lp.ChartOfAccountId = pa.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] ira ON lp.InterestReceivedChartOfAccountId = ira.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] irv ON lp.InterestReceivableChartOfAccountId = irv.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] icc ON lp.InterestChargedChartOfAccountId = icc.Id
                                WHERE lp.Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return Map(reader);
                    }
                }
            }
            return null;
        }

        public LoanProductDTO GetByCode(int code)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT lp.*,
                                pa.AccountCode as ChartOfAccountAccountCode,
                                pa.AccountName as ChartOfAccountAccountName,
                                pa.AccountType as ChartOfAccountAccountType,
                                ira.AccountCode as InterestReceivedChartOfAccountAccountCode,
                                ira.AccountName as InterestReceivedChartOfAccountAccountName,
                                ira.AccountType as InterestReceivedChartOfAccountAccountType,
                                irv.AccountCode as InterestReceivableChartOfAccountAccountCode,
                                irv.AccountName as InterestReceivableChartOfAccountAccountName,
                                irv.AccountType as InterestReceivableChartOfAccountAccountType,
                                icc.AccountCode as InterestChargedChartOfAccountAccountCode,
                                icc.AccountName as InterestChargedChartOfAccountAccountName,
                                icc.AccountType as InterestChargedChartOfAccountAccountType
                                FROM [swiftFin_LoanProducts] lp
                                LEFT JOIN [swiftFin_ChartOfAccounts] pa ON lp.ChartOfAccountId = pa.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] ira ON lp.InterestReceivedChartOfAccountId = ira.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] irv ON lp.InterestReceivableChartOfAccountId = irv.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] icc ON lp.InterestChargedChartOfAccountId = icc.Id
                                WHERE lp.Code = @Code";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return Map(reader);
                    }
                }
            }
            return null;
        }

        public IEnumerable<LoanProductDTO> GetByCategory(int category)
        {
            var list = new List<LoanProductDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT lp.*,
                                pa.AccountCode as ChartOfAccountAccountCode,
                                pa.AccountName as ChartOfAccountAccountName,
                                pa.AccountType as ChartOfAccountAccountType,
                                ira.AccountCode as InterestReceivedChartOfAccountAccountCode,
                                ira.AccountName as InterestReceivedChartOfAccountAccountName,
                                ira.AccountType as InterestReceivedChartOfAccountAccountType,
                                irv.AccountCode as InterestReceivableChartOfAccountAccountCode,
                                irv.AccountName as InterestReceivableChartOfAccountAccountName,
                                irv.AccountType as InterestReceivableChartOfAccountAccountType,
                                icc.AccountCode as InterestChargedChartOfAccountAccountCode,
                                icc.AccountName as InterestChargedChartOfAccountAccountName,
                                icc.AccountType as InterestChargedChartOfAccountAccountType
                                FROM [swiftFin_LoanProducts] lp
                                LEFT JOIN [swiftFin_ChartOfAccounts] pa ON lp.ChartOfAccountId = pa.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] ira ON lp.InterestReceivedChartOfAccountId = ira.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] irv ON lp.InterestReceivableChartOfAccountId = irv.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] icc ON lp.InterestChargedChartOfAccountId = icc.Id
                                WHERE lp.LoanRegistration_LoanProductCategory = @Category
                                ORDER BY lp.Code";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Category", category);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        public IEnumerable<LoanProductDTO> Search(string searchQuery)
        {
            var list = new List<LoanProductDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT lp.*,
                                pa.AccountCode as ChartOfAccountAccountCode,
                                pa.AccountName as ChartOfAccountAccountName,
                                pa.AccountType as ChartOfAccountAccountType,
                                ira.AccountCode as InterestReceivedChartOfAccountAccountCode,
                                ira.AccountName as InterestReceivedChartOfAccountAccountName,
                                ira.AccountType as InterestReceivedChartOfAccountAccountType,
                                irv.AccountCode as InterestReceivableChartOfAccountAccountCode,
                                irv.AccountName as InterestReceivableChartOfAccountAccountName,
                                irv.AccountType as InterestReceivableChartOfAccountAccountType,
                                icc.AccountCode as InterestChargedChartOfAccountAccountCode,
                                icc.AccountName as InterestChargedChartOfAccountAccountName,
                                icc.AccountType as InterestChargedChartOfAccountAccountType
                                FROM [swiftFin_LoanProducts] lp
                                LEFT JOIN [swiftFin_ChartOfAccounts] pa ON lp.ChartOfAccountId = pa.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] ira ON lp.InterestReceivedChartOfAccountId = ira.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] irv ON lp.InterestReceivableChartOfAccountId = irv.Id
                                LEFT JOIN [swiftFin_ChartOfAccounts] icc ON lp.InterestChargedChartOfAccountId = icc.Id
                                WHERE lp.Description LIKE @SearchQuery 
                                   OR lp.Code LIKE @SearchQuery
                                   OR CAST(lp.Code AS VARCHAR) LIKE @SearchQuery
                                ORDER BY lp.Code";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        public int GenerateCode()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT ISNULL(MAX(Code), 0) + 1 FROM [swiftFin_LoanProducts]";
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        public LoanProductDTO Create(LoanProductDTO loanProduct)
        {
            // Check if product with same code already exists
            var existingByCode = GetByCode(loanProduct.Code);
            if (existingByCode != null)
            {
                throw new InvalidOperationException($"Loan product with code {loanProduct.Code} already exists.");
            }

            // Check if product with same name already exists
            var existingByName = GetByName(loanProduct.Description);
            if (existingByName != null)
            {
                throw new InvalidOperationException($"Loan product with name '{loanProduct.Description}' already exists.");
            }

            // Generate code if not provided
            if (loanProduct.Code == 0)
            {
                loanProduct.Code = GenerateCode();
            }

            // Validate term vs payment frequency
            if (!ValidateTermVsPaymentFrequency(loanProduct.LoanRegistrationTermInMonths, loanProduct.LoanRegistrationPaymentFrequencyPerYear))
            {
                throw new InvalidOperationException("Term in months is not valid for the selected payment frequency.");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                // Generate new Guid if not provided
                if (loanProduct.Id == Guid.Empty)
                    loanProduct.Id = Guid.NewGuid();

                loanProduct.CreatedDate = DateTime.Now;

                // Fixed SQL query matching your table structure
                string query = @"INSERT INTO [swiftFin_LoanProducts] 
                                ([Id], [Code], [Description], 
                                 [LoanInterest_AnnualPercentageRate], [LoanInterest_ChargeMode], 
                                 [LoanInterest_RecoveryMode], [LoanInterest_CalculationMode], 
                                 [LoanRegistration_TermInMonths], [LoanRegistration_MinimumAmount], 
                                 [LoanRegistration_MaximumAmount], [LoanRegistration_MinimumInterestAmount], 
                                 [LoanRegistration_LoanProductSection], [LoanRegistration_LoanProductCategory], 
                                 [LoanRegistration_ConsecutiveIncome], [LoanRegistration_InvestmentsMultiplier], 
                                 [LoanRegistration_MinimumGuarantors], [LoanRegistration_MaximumGuarantees], 
                                 [LoanRegistration_RejectIfMemberHasBalance], [LoanRegistration_SecurityRequired], 
                                 [LoanRegistration_AllowSelfGuarantee], [LoanRegistration_GracePeriod], 
                                 [LoanRegistration_MinimumMembershipPeriod], [LoanRegistration_PaymentFrequencyPerYear], 
                                 [LoanRegistration_PaymentDueDate], [LoanRegistration_PayoutRecoveryMode], 
                                 [LoanRegistration_PayoutRecoveryPercentage], [LoanRegistration_AggregateCheckOffRecoveryMode], 
                                 [LoanRegistration_ChargeClearanceFee], [LoanRegistration_Microcredit], 
                                 [LoanRegistration_StandingOrderTrigger], [LoanRegistration_TrackArrears], 
                                 [LoanRegistration_ChargeArrearsFee], [LoanRegistration_EnforceSystemAppraisalRecommendation], 
                                 [LoanRegistration_BypassAudit], [LoanRegistration_MaximumSelfGuaranteeEligiblePercentage], 
                                 [LoanRegistration_GuarantorSecurityMode], [LoanRegistration_RoundingType], 
                                 [LoanRegistration_DisburseMicroLoanLessDeductions], [LoanRegistration_ExcludeOutstandingLoansOnMaximumEntitlement], 
                                 [LoanRegistration_ConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal], [LoanRegistration_ThrottleScheduledArrearsRecovery], 
                                 [LoanRegistration_CreateStandingOrderOnLoanAudit], [TakeHome_Type], 
                                 [TakeHome_Percentage], [TakeHome_FixedAmount], [Priority], 
                                 [IsLocked], [CreatedDate], [ChartOfAccountId], 
                                 [InterestReceivedChartOfAccountId], [InterestReceivableChartOfAccountId], 
                                 [InterestChargedChartOfAccountId])
                                VALUES 
                                (@Id, @Code, @Description, 
                                 @LoanInterestAnnualPercentageRate, @LoanInterestChargeMode, 
                                 @LoanInterestRecoveryMode, @LoanInterestCalculationMode, 
                                 @LoanRegistrationTermInMonths, @LoanRegistrationMinimumAmount, 
                                 @LoanRegistrationMaximumAmount, @LoanRegistrationMinimumInterestAmount, 
                                 @LoanRegistrationLoanProductSection, @LoanRegistrationLoanProductCategory, 
                                 @LoanRegistrationConsecutiveIncome, @LoanRegistrationInvestmentsMultiplier, 
                                 @LoanRegistrationMinimumGuarantors, @LoanRegistrationMaximumGuarantees, 
                                 @LoanRegistrationRejectIfMemberHasBalance, @LoanRegistrationSecurityRequired, 
                                 @LoanRegistrationAllowSelfGuarantee, @LoanRegistrationGracePeriod, 
                                 @LoanRegistrationMinimumMembershipPeriod, @LoanRegistrationPaymentFrequencyPerYear, 
                                 @LoanRegistrationPaymentDueDate, @LoanRegistrationPayoutRecoveryMode, 
                                 @LoanRegistrationPayoutRecoveryPercentage, @LoanRegistrationAggregateCheckOffRecoveryMode, 
                                 @LoanRegistrationChargeClearanceFee, @LoanRegistrationMicrocredit, 
                                 @LoanRegistrationStandingOrderTrigger, @LoanRegistrationTrackArrears, 
                                 @LoanRegistrationChargeArrearsFee, @LoanRegistrationEnforceSystemAppraisalRecommendation, 
                                 @LoanRegistrationBypassAudit, @LoanRegistrationMaximumSelfGuaranteeEligiblePercentage, 
                                 @LoanRegistrationGuarantorSecurityMode, @LoanRegistrationRoundingType, 
                                 @LoanRegistrationDisburseMicroLoanLessDeductions, @LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement, 
                                 @LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal, @LoanRegistrationThrottleScheduledArrearsRecovery, 
                                 @LoanRegistrationCreateStandingOrderOnLoanAudit, @TakeHomeType, 
                                 @TakeHomePercentage, @TakeHomeFixedAmount, @Priority, 
                                 @IsLocked, @CreatedDate, @ChartOfAccountId, 
                                 @InterestReceivedChartOfAccountId, @InterestReceivableChartOfAccountId, 
                                 @InterestChargedChartOfAccountId)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, loanProduct);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return GetById(loanProduct.Id);
        }

        public void Update(LoanProductDTO loanProduct)
        {
            // Check if product exists
            var existing = GetById(loanProduct.Id);
            if (existing == null)
                throw new KeyNotFoundException("Loan product not found.");

            // Check if product is locked
            if (existing.IsLocked)
                throw new InvalidOperationException("Cannot update a locked loan product.");

            // Check if another product with same code exists (excluding current)
            var existingByCode = GetByCode(loanProduct.Code);
            if (existingByCode != null && existingByCode.Id != loanProduct.Id)
            {
                throw new InvalidOperationException($"Loan product with code {loanProduct.Code} already exists.");
            }

            // Check if another product with same name exists (excluding current)
            var existingByName = GetByName(loanProduct.Description);
            if (existingByName != null && existingByName.Id != loanProduct.Id)
            {
                throw new InvalidOperationException($"Loan product with name '{loanProduct.Description}' already exists.");
            }

            // Validate term vs payment frequency
            if (!ValidateTermVsPaymentFrequency(loanProduct.LoanRegistrationTermInMonths, loanProduct.LoanRegistrationPaymentFrequencyPerYear))
            {
                throw new InvalidOperationException("Term in months is not valid for the selected payment frequency.");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                // Fixed UPDATE query to match your table structure
                string query = @"UPDATE [swiftFin_LoanProducts] 
                                SET [Code] = @Code,
                                    [Description] = @Description,
                                    [LoanInterest_AnnualPercentageRate] = @LoanInterestAnnualPercentageRate,
                                    [LoanInterest_ChargeMode] = @LoanInterestChargeMode,
                                    [LoanInterest_RecoveryMode] = @LoanInterestRecoveryMode,
                                    [LoanInterest_CalculationMode] = @LoanInterestCalculationMode,
                                    [LoanRegistration_TermInMonths] = @LoanRegistrationTermInMonths,
                                    [LoanRegistration_MinimumAmount] = @LoanRegistrationMinimumAmount,
                                    [LoanRegistration_MaximumAmount] = @LoanRegistrationMaximumAmount,
                                    [LoanRegistration_MinimumInterestAmount] = @LoanRegistrationMinimumInterestAmount,
                                    [LoanRegistration_LoanProductSection] = @LoanRegistrationLoanProductSection,
                                    [LoanRegistration_LoanProductCategory] = @LoanRegistrationLoanProductCategory,
                                    [LoanRegistration_ConsecutiveIncome] = @LoanRegistrationConsecutiveIncome,
                                    [LoanRegistration_InvestmentsMultiplier] = @LoanRegistrationInvestmentsMultiplier,
                                    [LoanRegistration_MinimumGuarantors] = @LoanRegistrationMinimumGuarantors,
                                    [LoanRegistration_MaximumGuarantees] = @LoanRegistrationMaximumGuarantees,
                                    [LoanRegistration_RejectIfMemberHasBalance] = @LoanRegistrationRejectIfMemberHasBalance,
                                    [LoanRegistration_SecurityRequired] = @LoanRegistrationSecurityRequired,
                                    [LoanRegistration_AllowSelfGuarantee] = @LoanRegistrationAllowSelfGuarantee,
                                    [LoanRegistration_GracePeriod] = @LoanRegistrationGracePeriod,
                                    [LoanRegistration_MinimumMembershipPeriod] = @LoanRegistrationMinimumMembershipPeriod,
                                    [LoanRegistration_PaymentFrequencyPerYear] = @LoanRegistrationPaymentFrequencyPerYear,
                                    [LoanRegistration_PaymentDueDate] = @LoanRegistrationPaymentDueDate,
                                    [LoanRegistration_PayoutRecoveryMode] = @LoanRegistrationPayoutRecoveryMode,
                                    [LoanRegistration_PayoutRecoveryPercentage] = @LoanRegistrationPayoutRecoveryPercentage,
                                    [LoanRegistration_AggregateCheckOffRecoveryMode] = @LoanRegistrationAggregateCheckOffRecoveryMode,
                                    [LoanRegistration_ChargeClearanceFee] = @LoanRegistrationChargeClearanceFee,
                                    [LoanRegistration_Microcredit] = @LoanRegistrationMicrocredit,
                                    [LoanRegistration_StandingOrderTrigger] = @LoanRegistrationStandingOrderTrigger,
                                    [LoanRegistration_TrackArrears] = @LoanRegistrationTrackArrears,
                                    [LoanRegistration_ChargeArrearsFee] = @LoanRegistrationChargeArrearsFee,
                                    [LoanRegistration_EnforceSystemAppraisalRecommendation] = @LoanRegistrationEnforceSystemAppraisalRecommendation,
                                    [LoanRegistration_BypassAudit] = @LoanRegistrationBypassAudit,
                                    [LoanRegistration_MaximumSelfGuaranteeEligiblePercentage] = @LoanRegistrationMaximumSelfGuaranteeEligiblePercentage,
                                    [LoanRegistration_GuarantorSecurityMode] = @LoanRegistrationGuarantorSecurityMode,
                                    [LoanRegistration_RoundingType] = @LoanRegistrationRoundingType,
                                    [LoanRegistration_DisburseMicroLoanLessDeductions] = @LoanRegistrationDisburseMicroLoanLessDeductions,
                                    [LoanRegistration_ExcludeOutstandingLoansOnMaximumEntitlement] = @LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement,
                                    [LoanRegistration_ConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal] = @LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal,
                                    [LoanRegistration_ThrottleScheduledArrearsRecovery] = @LoanRegistrationThrottleScheduledArrearsRecovery,
                                    [LoanRegistration_CreateStandingOrderOnLoanAudit] = @LoanRegistrationCreateStandingOrderOnLoanAudit,
                                    [TakeHome_Type] = @TakeHomeType,
                                    [TakeHome_Percentage] = @TakeHomePercentage,
                                    [TakeHome_FixedAmount] = @TakeHomeFixedAmount,
                                    [Priority] = @Priority,
                                    [IsLocked] = @IsLocked,
                                    [ChartOfAccountId] = @ChartOfAccountId,
                                    [InterestReceivedChartOfAccountId] = @InterestReceivedChartOfAccountId,
                                    [InterestReceivableChartOfAccountId] = @InterestReceivableChartOfAccountId,
                                    [InterestChargedChartOfAccountId] = @InterestChargedChartOfAccountId
                                WHERE [Id] = @Id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, loanProduct);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(Guid id)
        {
            // Check if product exists
            var product = GetById(id);
            if (product == null)
                throw new KeyNotFoundException("Loan product not found.");

            // Check if product is locked
            if (product.IsLocked)
                throw new InvalidOperationException("Cannot delete a locked loan product.");

            // Check if product is being used
            if (IsProductInUse(id))
            {
                throw new InvalidOperationException("Cannot delete loan product because it is being used by loan applications.");
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM [swiftFin_LoanProducts] WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool IsProductInUse(Guid productId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT COUNT(*) FROM [swiftFin_Loans] WHERE LoanProductId = @ProductId";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result) > 0;
                }
            }
        }

        private LoanProductDTO GetByName(string name)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT TOP 1 * FROM [swiftFin_LoanProducts] WHERE Description = @Description";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Description", name);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return Map(reader);
                    }
                }
            }
            return null;
        }

        private bool ValidateTermVsPaymentFrequency(int termInMonths, int paymentFrequency)
        {
            // Convert to enum value for validation
            switch ((PaymentFrequencyPerYear)paymentFrequency)
            {
                case PaymentFrequencyPerYear.SemiAnnual:
                    return (termInMonths % 6) == 0;
                case PaymentFrequencyPerYear.TriAnnual:
                    return (termInMonths % 4) == 0;
                case PaymentFrequencyPerYear.Quarterly:
                    return (termInMonths % 3) == 0;
                case PaymentFrequencyPerYear.BiMonthly:
                case PaymentFrequencyPerYear.SemiMonthly:
                case PaymentFrequencyPerYear.BiWeekly:
                    return ((termInMonths * 2) % 2) == 0;
                case PaymentFrequencyPerYear.Annual:
                    return (termInMonths % 12) == 0;
                case PaymentFrequencyPerYear.Weekly:
                case PaymentFrequencyPerYear.Daily:
                case PaymentFrequencyPerYear.Monthly:
                default:
                    return true;
            }
        }

        private void AddParams(SqlCommand cmd, LoanProductDTO loanProduct)
        {
            cmd.Parameters.AddWithValue("@Id", loanProduct.Id);
            cmd.Parameters.AddWithValue("@Code", loanProduct.Code);
            cmd.Parameters.AddWithValue("@Description", loanProduct.Description ?? "");
            cmd.Parameters.AddWithValue("@LoanInterestAnnualPercentageRate", loanProduct.LoanInterestAnnualPercentageRate);
            cmd.Parameters.AddWithValue("@LoanInterestChargeMode", loanProduct.LoanInterestChargeMode);
            cmd.Parameters.AddWithValue("@LoanInterestRecoveryMode", loanProduct.LoanInterestRecoveryMode);
            cmd.Parameters.AddWithValue("@LoanInterestCalculationMode", loanProduct.LoanInterestCalculationMode);
            cmd.Parameters.AddWithValue("@LoanRegistrationTermInMonths", loanProduct.LoanRegistrationTermInMonths);
            cmd.Parameters.AddWithValue("@LoanRegistrationMinimumAmount", loanProduct.LoanRegistrationMinimumAmount);
            cmd.Parameters.AddWithValue("@LoanRegistrationMaximumAmount", loanProduct.LoanRegistrationMaximumAmount);
            cmd.Parameters.AddWithValue("@LoanRegistrationMinimumInterestAmount", loanProduct.LoanRegistrationMinimumInterestAmount);
            cmd.Parameters.AddWithValue("@LoanRegistrationLoanProductSection", loanProduct.LoanRegistrationLoanProductSection);
            cmd.Parameters.AddWithValue("@LoanRegistrationLoanProductCategory", loanProduct.LoanRegistrationLoanProductCategory);
            cmd.Parameters.AddWithValue("@LoanRegistrationConsecutiveIncome", loanProduct.LoanRegistrationConsecutiveIncome);
            cmd.Parameters.AddWithValue("@LoanRegistrationInvestmentsMultiplier", loanProduct.LoanRegistrationInvestmentsMultiplier);
            cmd.Parameters.AddWithValue("@LoanRegistrationMinimumGuarantors", loanProduct.LoanRegistrationMinimumGuarantors);
            cmd.Parameters.AddWithValue("@LoanRegistrationMaximumGuarantees", loanProduct.LoanRegistrationMaximumGuarantees);
            cmd.Parameters.AddWithValue("@LoanRegistrationRejectIfMemberHasBalance", loanProduct.LoanRegistrationRejectIfMemberHasBalance);
            cmd.Parameters.AddWithValue("@LoanRegistrationSecurityRequired", loanProduct.LoanRegistrationSecurityRequired);
            cmd.Parameters.AddWithValue("@LoanRegistrationAllowSelfGuarantee", loanProduct.LoanRegistrationAllowSelfGuarantee);
            cmd.Parameters.AddWithValue("@LoanRegistrationGracePeriod", loanProduct.LoanRegistrationGracePeriod);
            cmd.Parameters.AddWithValue("@LoanRegistrationMinimumMembershipPeriod", loanProduct.LoanRegistrationMinimumMembershipPeriod);
            cmd.Parameters.AddWithValue("@LoanRegistrationPaymentFrequencyPerYear", loanProduct.LoanRegistrationPaymentFrequencyPerYear);
            cmd.Parameters.AddWithValue("@LoanRegistrationPaymentDueDate", loanProduct.LoanRegistrationPaymentDueDate);
            cmd.Parameters.AddWithValue("@LoanRegistrationPayoutRecoveryMode", loanProduct.LoanRegistrationPayoutRecoveryMode);
            cmd.Parameters.AddWithValue("@LoanRegistrationPayoutRecoveryPercentage", loanProduct.LoanRegistrationPayoutRecoveryPercentage);
            cmd.Parameters.AddWithValue("@LoanRegistrationAggregateCheckOffRecoveryMode", loanProduct.LoanRegistrationAggregateCheckOffRecoveryMode);
            cmd.Parameters.AddWithValue("@LoanRegistrationChargeClearanceFee", loanProduct.LoanRegistrationChargeClearanceFee);
            cmd.Parameters.AddWithValue("@LoanRegistrationMicrocredit", loanProduct.LoanRegistrationMicrocredit);
            cmd.Parameters.AddWithValue("@LoanRegistrationStandingOrderTrigger", loanProduct.LoanRegistrationStandingOrderTrigger);
            cmd.Parameters.AddWithValue("@LoanRegistrationTrackArrears", loanProduct.LoanRegistrationTrackArrears);
            cmd.Parameters.AddWithValue("@LoanRegistrationChargeArrearsFee", loanProduct.LoanRegistrationChargeArrearsFee);
            cmd.Parameters.AddWithValue("@LoanRegistrationEnforceSystemAppraisalRecommendation", loanProduct.LoanRegistrationEnforceSystemAppraisalRecommendation);
            cmd.Parameters.AddWithValue("@LoanRegistrationBypassAudit", loanProduct.LoanRegistrationBypassAudit);
            cmd.Parameters.AddWithValue("@LoanRegistrationMaximumSelfGuaranteeEligiblePercentage", loanProduct.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage);
            cmd.Parameters.AddWithValue("@LoanRegistrationGuarantorSecurityMode", loanProduct.LoanRegistrationGuarantorSecurityMode);
            cmd.Parameters.AddWithValue("@LoanRegistrationRoundingType", loanProduct.LoanRegistrationRoundingType);
            cmd.Parameters.AddWithValue("@LoanRegistrationDisburseMicroLoanLessDeductions", loanProduct.LoanRegistrationDisburseMicroLoanLessDeductions);
            cmd.Parameters.AddWithValue("@LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement", loanProduct.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement);
            cmd.Parameters.AddWithValue("@LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal", loanProduct.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal);
            cmd.Parameters.AddWithValue("@LoanRegistrationThrottleScheduledArrearsRecovery", loanProduct.LoanRegistrationThrottleScheduledArrearsRecovery);
            cmd.Parameters.AddWithValue("@LoanRegistrationCreateStandingOrderOnLoanAudit", loanProduct.LoanRegistrationCreateStandingOrderOnLoanAudit);
            cmd.Parameters.AddWithValue("@TakeHomeType", loanProduct.TakeHomeType);
            cmd.Parameters.AddWithValue("@TakeHomePercentage", loanProduct.TakeHomePercentage);
            cmd.Parameters.AddWithValue("@TakeHomeFixedAmount", loanProduct.TakeHomeFixedAmount);
            cmd.Parameters.AddWithValue("@Priority", loanProduct.Priority);
            cmd.Parameters.AddWithValue("@IsLocked", loanProduct.IsLocked);
            cmd.Parameters.AddWithValue("@CreatedDate", loanProduct.CreatedDate);
            cmd.Parameters.AddWithValue("@ChartOfAccountId", loanProduct.ChartOfAccountId);
            cmd.Parameters.AddWithValue("@InterestReceivedChartOfAccountId", loanProduct.InterestReceivedChartOfAccountId);
            cmd.Parameters.AddWithValue("@InterestReceivableChartOfAccountId", loanProduct.InterestReceivableChartOfAccountId);
            cmd.Parameters.AddWithValue("@InterestChargedChartOfAccountId", loanProduct.InterestChargedChartOfAccountId);
        }

        private LoanProductDTO Map(IDataReader reader)
        {
            return new LoanProductDTO
            {
                Id = (Guid)reader["Id"],
                Code = Convert.ToInt32(reader["Code"]),
                Description = reader["Description"]?.ToString(),
                LoanInterestAnnualPercentageRate = Convert.ToDouble(reader["LoanInterest_AnnualPercentageRate"]),
                LoanInterestChargeMode = Convert.ToInt32(reader["LoanInterest_ChargeMode"]),
                LoanInterestRecoveryMode = Convert.ToInt32(reader["LoanInterest_RecoveryMode"]),
                LoanInterestCalculationMode = Convert.ToInt32(reader["LoanInterest_CalculationMode"]),
                LoanRegistrationTermInMonths = Convert.ToInt32(reader["LoanRegistration_TermInMonths"]),
                LoanRegistrationMinimumAmount = Convert.ToDecimal(reader["LoanRegistration_MinimumAmount"]),
                LoanRegistrationMaximumAmount = Convert.ToDecimal(reader["LoanRegistration_MaximumAmount"]),
                LoanRegistrationMinimumInterestAmount = Convert.ToDecimal(reader["LoanRegistration_MinimumInterestAmount"]),
                LoanRegistrationLoanProductSection = Convert.ToInt32(reader["LoanRegistration_LoanProductSection"]),
                LoanRegistrationLoanProductCategory = Convert.ToInt32(reader["LoanRegistration_LoanProductCategory"]),
                LoanRegistrationConsecutiveIncome = Convert.ToInt32(reader["LoanRegistration_ConsecutiveIncome"]),
                LoanRegistrationInvestmentsMultiplier = Convert.ToDouble(reader["LoanRegistration_InvestmentsMultiplier"]),
                LoanRegistrationMinimumGuarantors = Convert.ToInt32(reader["LoanRegistration_MinimumGuarantors"]),
                LoanRegistrationMaximumGuarantees = Convert.ToInt32(reader["LoanRegistration_MaximumGuarantees"]),
                LoanRegistrationRejectIfMemberHasBalance = Convert.ToBoolean(reader["LoanRegistration_RejectIfMemberHasBalance"]),
                LoanRegistrationSecurityRequired = Convert.ToBoolean(reader["LoanRegistration_SecurityRequired"]),
                LoanRegistrationAllowSelfGuarantee = Convert.ToBoolean(reader["LoanRegistration_AllowSelfGuarantee"]),
                LoanRegistrationGracePeriod = Convert.ToInt32(reader["LoanRegistration_GracePeriod"]),
                LoanRegistrationMinimumMembershipPeriod = Convert.ToInt32(reader["LoanRegistration_MinimumMembershipPeriod"]),
                LoanRegistrationPaymentFrequencyPerYear = Convert.ToInt32(reader["LoanRegistration_PaymentFrequencyPerYear"]),
                LoanRegistrationPaymentDueDate = Convert.ToInt32(reader["LoanRegistration_PaymentDueDate"]),
                LoanRegistrationPayoutRecoveryMode = Convert.ToInt32(reader["LoanRegistration_PayoutRecoveryMode"]),
                LoanRegistrationPayoutRecoveryPercentage = Convert.ToDouble(reader["LoanRegistration_PayoutRecoveryPercentage"]),
                LoanRegistrationAggregateCheckOffRecoveryMode = Convert.ToInt32(reader["LoanRegistration_AggregateCheckOffRecoveryMode"]),
                LoanRegistrationChargeClearanceFee = Convert.ToBoolean(reader["LoanRegistration_ChargeClearanceFee"]),
                LoanRegistrationMicrocredit = Convert.ToBoolean(reader["LoanRegistration_Microcredit"]),
                LoanRegistrationStandingOrderTrigger = Convert.ToInt32(reader["LoanRegistration_StandingOrderTrigger"]),
                LoanRegistrationTrackArrears = Convert.ToBoolean(reader["LoanRegistration_TrackArrears"]),
                LoanRegistrationChargeArrearsFee = Convert.ToBoolean(reader["LoanRegistration_ChargeArrearsFee"]),
                LoanRegistrationEnforceSystemAppraisalRecommendation = Convert.ToBoolean(reader["LoanRegistration_EnforceSystemAppraisalRecommendation"]),
                LoanRegistrationBypassAudit = Convert.ToBoolean(reader["LoanRegistration_BypassAudit"]),
                LoanRegistrationMaximumSelfGuaranteeEligiblePercentage = Convert.ToDouble(reader["LoanRegistration_MaximumSelfGuaranteeEligiblePercentage"]),
                LoanRegistrationGuarantorSecurityMode = Convert.ToInt32(reader["LoanRegistration_GuarantorSecurityMode"]),
                LoanRegistrationRoundingType = Convert.ToInt32(reader["LoanRegistration_RoundingType"]),
                LoanRegistrationDisburseMicroLoanLessDeductions = Convert.ToBoolean(reader["LoanRegistration_DisburseMicroLoanLessDeductions"]),
                LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement = Convert.ToBoolean(reader["LoanRegistration_ExcludeOutstandingLoansOnMaximumEntitlement"]),
                LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal = Convert.ToBoolean(reader["LoanRegistration_ConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal"]),
                LoanRegistrationThrottleScheduledArrearsRecovery = Convert.ToBoolean(reader["LoanRegistration_ThrottleScheduledArrearsRecovery"]),
                LoanRegistrationCreateStandingOrderOnLoanAudit = Convert.ToBoolean(reader["LoanRegistration_CreateStandingOrderOnLoanAudit"]),
                TakeHomeType = Convert.ToInt32(reader["TakeHome_Type"]),
                TakeHomePercentage = Convert.ToDouble(reader["TakeHome_Percentage"]),
                TakeHomeFixedAmount = Convert.ToDecimal(reader["TakeHome_FixedAmount"]),
                Priority = Convert.ToInt32(reader["Priority"]),
                IsLocked = Convert.ToBoolean(reader["IsLocked"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                ChartOfAccountId = (Guid)reader["ChartOfAccountId"],
                InterestReceivedChartOfAccountId = (Guid)reader["InterestReceivedChartOfAccountId"],
                InterestReceivableChartOfAccountId = (Guid)reader["InterestReceivableChartOfAccountId"],
                InterestChargedChartOfAccountId = (Guid)reader["InterestChargedChartOfAccountId"],
                // Chart of account details
                ChartOfAccountAccountCode = reader["ChartOfAccountAccountCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ChartOfAccountAccountCode"]),
                ChartOfAccountAccountName = reader["ChartOfAccountAccountName"]?.ToString(),
                ChartOfAccountAccountType = reader["ChartOfAccountAccountType"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ChartOfAccountAccountType"]),
                InterestReceivedChartOfAccountAccountCode = reader["InterestReceivedChartOfAccountAccountCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InterestReceivedChartOfAccountAccountCode"]),
                InterestReceivedChartOfAccountAccountName = reader["InterestReceivedChartOfAccountAccountName"]?.ToString(),
                InterestReceivedChartOfAccountAccountType = reader["InterestReceivedChartOfAccountAccountType"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InterestReceivedChartOfAccountAccountType"]),
                InterestReceivableChartOfAccountAccountCode = reader["InterestReceivableChartOfAccountAccountCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InterestReceivableChartOfAccountAccountCode"]),
                InterestReceivableChartOfAccountAccountName = reader["InterestReceivableChartOfAccountAccountName"]?.ToString(),
                InterestReceivableChartOfAccountAccountType = reader["InterestReceivableChartOfAccountAccountType"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InterestReceivableChartOfAccountAccountType"]),
                InterestChargedChartOfAccountAccountCode = reader["InterestChargedChartOfAccountAccountCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InterestChargedChartOfAccountAccountCode"]),
                InterestChargedChartOfAccountAccountName = reader["InterestChargedChartOfAccountAccountName"]?.ToString(),
                InterestChargedChartOfAccountAccountType = reader["InterestChargedChartOfAccountAccountType"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InterestChargedChartOfAccountAccountType"])
            };
        }
    }
}