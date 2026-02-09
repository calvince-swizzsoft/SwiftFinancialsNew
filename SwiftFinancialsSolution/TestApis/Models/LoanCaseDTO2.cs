using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Application.Seedwork;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Application.MainBoundedContext.DTO.BackOfficeModule;

namespace TestApis.Models
{
    public class LoanCaseDTO2
    {

        public LoanCaseDTO2()
        {
        }


        public Guid Id { get; set; }


        public Guid ParentId { get; set; }

        public string collateralIds { get; set; }


        public Guid BranchId { get; set; }


        public string BranchDescription { get; set; }


        public string BranchAddressEmail { get; set; }


        public Guid BranchCompanyId { get; set; }


        public string BranchCompanyDescription { get; set; }


        public string BranchCompanyAddressCity { get; set; }


        public string BranchCompanyAddressStreet { get; set; }


        public string BranchCompanyAddressEmail { get; set; }


        public string BranchCompanyAddressLandLine { get; set; }


        public string BranchCompanyAddressMobileLine { get; set; }


        public bool BranchCompanyEnforceBudgetControl { get; set; }


        public Guid CustomerId { get; set; }


        public int CustomerType { get; set; }


        public string CustomerTypeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(CustomerType), CustomerType) ? EnumHelper.GetDescription((CustomerType)CustomerType) : string.Empty;
            }
        }


        public int CustomerIndividualSalutation { get; set; }


        public string CustomerIndividualSalutationDescription
        {
            get
            {
                return Enum.IsDefined(typeof(Salutation), CustomerIndividualSalutation) ? EnumHelper.GetDescription((Salutation)CustomerIndividualSalutation) : string.Empty;
            }
        }


        public int CustomerSerialNumber { get; set; }


        public string PaddedCustomerSerialNumber
        {
            get
            {
                return string.Format("{0}", CustomerSerialNumber).PadLeft(7, '0');
            }
        }


        public string CustomerIndividualFirstName { get; set; }


        public string CustomerIndividualLastName { get; set; }


        public string CustomerNonIndividualDescription { get; set; }


        public string CustomerNonIndividualRegistrationNumber { get; set; }


        public string CustomerPersonalIdentificationNumber { get; set; }


        public DateTime? CustomerNonIndividualDateEstablished { get; set; }


        public string CustomerFullName
        {
            get
            {
                var result = string.Empty;

                switch ((CustomerType)CustomerType)
                {
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Individual:
                        result = string.Format("{0} {1} {2}", CustomerIndividualSalutationDescription, CustomerIndividualFirstName, CustomerIndividualLastName).Trim();
                        break;
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Partnership:
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Corporation:
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.MicroCredit:
                        result = CustomerNonIndividualDescription;
                        break;
                    default:
                        break;
                }

                return result;
            }
        }


        public string CustomerName { get; set; }


        public string CustomerLoaneeFullName { get; set; }


        public string CustomerIndividualIdentityCardNumber { get; set; }


        public string CustomerIndividualPayrollNumbers { get; set; }


        public DateTime? CustomerIndividualBirthDate { get; set; }


        public int CustomerAge
        {
            get
            {
                var result = -1;

                switch ((CustomerType)CustomerType)
                {
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Individual:
                        if (CustomerIndividualBirthDate.HasValue && CustomerIndividualBirthDate.Value <= DateTime.Today)
                            result = UberUtil.GetAge(CustomerIndividualBirthDate.Value);
                        break;
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Partnership:
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Corporation:
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.MicroCredit:
                        if (CustomerNonIndividualDateEstablished.HasValue && CustomerNonIndividualDateEstablished.Value <= DateTime.Today)
                            result = UberUtil.GetAge(CustomerNonIndividualDateEstablished.Value);
                        break;
                    default:
                        break;
                }

                return result;
            }
        }


        public string CustomerReference1 { get; set; }


        public string CustomerReference2 { get; set; }


        public string CustomerReference3 { get; set; }


        public string CustomerAddressMobileLine { get; set; }


        public string CustomerAddressEmail { get; set; }


        public int CustomerStationZoneDivisionEmployerRetirementAge { get; set; }

        public string CustomerStationZoneDivisionEmployerDescription { get; set; }



        public string GuarantorCustomerStationZoneDivisionEmployerDescription { get; set; }



        public string CustomerStation { get; set; }



        public string GuarantorCustomerStation { get; set; }


        public bool CustomerStationZoneDivisionEmployerEnforceRetirementAge { get; set; }



        public Guid LoanProductId { get; set; }



        public string LoanProductDescription { get; set; }



        public Guid? LoanPurposeId { get; set; }



        public string LoanPurposeDescription { get; set; }




        public Guid? SavingsProductId { get; set; }


        public int SavingsProductCode { get; set; }

        public string SavingsProductDescription { get; set; }

        public Guid SavingsProductChartOfAccountId { get; set; }


        public Guid RegistrationRemarkId { get; set; }


        public int CaseNumber { get; set; }


        public string PaddedCaseNumber
        {
            get
            {
                return string.Format("{0}", CaseNumber).PadLeft(7, '0');
            }
        }


        public string Remarks { get; set; }


        [RegularExpression(@"^\s*(?=.*[1-9])\d*(?:\.\d{1,2})?\s*$", ErrorMessage = "Amount applied must be greater than zero!")]
        public decimal AmountApplied { get; set; }


        [Display(Name = "Received Date")]
        public DateTime ReceivedDate { get; set; }


        [Display(Name = "Appraised By")]
        public string AppraisedBy { get; set; }


        [Display(Name = "Appraised Date")]
        public DateTime? AppraisedDate { get; set; }


        [Display(Name = "System Appraisal Remarks")]
        public string SystemAppraisalRemarks { get; set; }


        [Display(Name = "System Appraised Amount")]
        public decimal SystemAppraisedAmount { get; set; }


        [Display(Name = "Appraisal Remarks")]
        public string AppraisalRemarks { get; set; }


        [Display(Name = "Appraised Amount")]
        public decimal AppraisedAmount { get; set; }


        [Display(Name = "Appraised Amount Remarks")]
        public string AppraisedAmountRemarks { get; set; }


        [Display(Name = "Appraised Net Income")]
        public decimal AppraisedNetIncome { get; set; }


        [Display(Name = "Appraised Ability")]
        public decimal AppraisedAbility { get; set; }


        [Display(Name = "Approved Amount")]
        public decimal ApprovedAmount { get; set; }


        [Display(Name = "Approved Amount Remarks")]
        public string ApprovedAmountRemarks { get; set; }


        [Display(Name = "Approved Standing Order Principal")]
        public decimal ApprovedPrincipalPayment { get; set; }


        [Display(Name = "Approved Standing Order Interest")]
        public decimal ApprovedInterestPayment { get; set; }


        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }


        [Display(Name = "Approved Date")]
        public DateTime? ApprovedDate { get; set; }


        [Display(Name = "Approval Remarks")]
        public string ApprovalRemarks { get; set; }


        [Display(Name = "Verified By")]
        public string AuditedBy { get; set; }


        [Display(Name = "Verified Date")]
        public DateTime? AuditedDate { get; set; }


        [Display(Name = "Verification Remarks")]
        public string AuditRemarks { get; set; }


        [Display(Name = "Top-Up Amount")]
        public decimal AuditTopUpAmount { get; set; }


        [Display(Name = "Cancelled By")]
        public string CancelledBy { get; set; }


        [Display(Name = "Cancellation Date")]
        public DateTime? CancelledDate { get; set; }


        [Display(Name = "Is Batched?")]
        public bool IsBatched { get; set; }


        [Display(Name = "Is Batched?")]
        public string IsBatchedDescription
        {
            get
            {
                return IsBatched ? "Yes" : "No";
            }
        }


        [Display(Name = "Batch Number")]
        public int BatchNumber { get; set; }


        [Display(Name = "Batch Number")]
        public string PaddedBatchNumber
        {
            get
            {
                return string.Format("{0}", BatchNumber).PadLeft(7, '0');
            }
        }


        [Display(Name = "Batched By")]
        public string BatchedBy { get; set; }


        [Display(Name = "Disbursement Remarks")]
        public string DisbursementRemarks { get; set; }


        [Display(Name = "Disbursed By")]
        public string DisbursedBy { get; set; }


        [Display(Name = "Disbursed Date")]
        public DateTime? DisbursedDate { get; set; }


        [Display(Name = "Disbursed Amount")]
        public decimal DisbursedAmount { get; set; }


        [Display(Name = "Monthly Payback Amount")]
        public decimal MonthlyPaybackAmount { get; set; }


        [Display(Name = "Total Payback Amount")]
        public decimal TotalPaybackAmount { get; set; }


        [Display(Name = "Status")]
        public int Status { get; set; }


        [Display(Name = "Status")]
        public string StatusDescription
        {
            get
            {
                return Enum.IsDefined(typeof(LoanCaseStatus), Status) ? EnumHelper.GetDescription((LoanCaseStatus)Status) : string.Empty;
            }
        }


        [Display(Name = "Verification Action")]
        public int LoanAuditOption { get; set; }


        [Display(Name = "Verification Action")]
        public string LoanAuditOptionDescription
        {
            get
            {
                return Enum.IsDefined(typeof(LoanAuditOption), LoanAuditOption) ? EnumHelper.GetDescription((LoanAuditOption)LoanAuditOption) : string.Empty;
            }
        }


        [Display(Name = "Loan Appraisal Option")]
        public int LoanAppraisalOption { get; set; }


        [Display(Name = "Loan Appraisal Option")]
        public string LoanAppraisalOptionDescription
        {
            get
            {
                return Enum.IsDefined(typeof(LoanAppraisalOption), LoanAppraisalOption) ? EnumHelper.GetDescription((LoanAppraisalOption)LoanAppraisalOption) : string.Empty;
            }
        }


        [Display(Name = "Loan Cancellation Option")]
        public int LoanCancellationOption { get; set; }


        [Display(Name = "Loan Cancellation Option")]
        public string LoanCancellationOptionDescription
        {
            get
            {
                return Enum.IsDefined(typeof(LoanCancellationOption), LoanCancellationOption) ? EnumHelper.GetDescription((LoanCancellationOption)LoanCancellationOption) : string.Empty;
            }
        }


        [Display(Name = "Investments Balance")]
        public decimal LoanProductInvestmentsBalance { get; set; }


        [Display(Name = "Total Shares")]
        public decimal LoanProductTotalSharesInvestmentsBalance { get; set; }


        [Display(Name = "Committed Shares")]
        public decimal LoanProductCommittedSharesInvestmentsBalance { get; set; }


        [Display(Name = "Loan Balance")]
        public decimal LoanProductLoanBalance { get; set; }


        [Display(Name = "BOSA Loans Balance")]
        public decimal TotalLoansBalance { get; set; }


        [Display(Name = "Attached Loans Balance")]
        public decimal TotalAttachedLoansBalance { get; set; }


        [Display(Name = "Latest Income")]
        public decimal LoanProductLatestIncome { get; set; }


        [Display(Name = "Annual Percentage Rate")]
        public double LoanInterestAnnualPercentageRate { get; set; }


        [Display(Name = "Interest Charge Mode")]
        public int LoanInterestChargeMode { get; set; }


        [Display(Name = "Interest Charge Mode")]
        public string LoanInterestChargeModeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(InterestChargeMode), LoanInterestChargeMode) ? EnumHelper.GetDescription((InterestChargeMode)LoanInterestChargeMode) : string.Empty;
            }
        }


        [Display(Name = "Loan Approval Option")]
        public int LoanApprovalOption { get; set; }


        [Display(Name = "Loan Approval Option")]
        public string LoanApprovalOptionDescription
        {
            get
            {
                return Enum.IsDefined(typeof(LoanApprovalOption), LoanApprovalOption) ? EnumHelper.GetDescription((LoanApprovalOption)LoanApprovalOption) : string.Empty;
            }
        }


        [Display(Name = "Interest Recovery Mode")]
        public int LoanInterestRecoveryMode { get; set; }


        [Display(Name = "Interest Recovery Mode")]
        public string LoanInterestRecoveryModeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(InterestRecoveryMode), LoanInterestRecoveryMode) ? EnumHelper.GetDescription((InterestRecoveryMode)LoanInterestRecoveryMode) : string.Empty;
            }
        }


        [Display(Name = "Interest Calculation Mode")]
        public int LoanInterestCalculationMode { get; set; }


        [Display(Name = "Interest Calculation Mode")]
        public string LoanInterestCalculationModeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(InterestCalculationMode), LoanInterestCalculationMode) ? EnumHelper.GetDescription((InterestCalculationMode)LoanInterestCalculationMode) : string.Empty;
            }
        }


        [Display(Name = "Term (Months)")]
        [RegularExpression(@"^\s*(?=.*[1-9])\d*(?:\.\d{1,2})?\s*$", ErrorMessage = "Term in months must be greater than zero!")]
        public int LoanRegistrationTermInMonths { get; set; }


        [Display(Name = "Minimum Principal Amount")]
        public decimal LoanRegistrationMinimumAmount { get; set; }


        [Display(Name = "Maximum Principal Amount")]
        public decimal LoanRegistrationMaximumAmount { get; set; }


        [Display(Name = "Minimum Chargeable Interest Amount")]
        public decimal LoanRegistrationMinimumInterestAmount { get; set; }


        [Display(Name = "Section")]
        public int LoanRegistrationLoanProductSection { get; set; }


        [Display(Name = "Section")]
        public string LoanRegistrationLoanProductSectionDescription
        {
            get
            {
                return Enum.IsDefined(typeof(LoanProductSection), LoanRegistrationLoanProductSection) ? EnumHelper.GetDescription((LoanProductSection)LoanRegistrationLoanProductSection) : string.Empty;
            }
        }


        [Display(Name = "Category")]
        public int LoanRegistrationLoanProductCategory { get; set; }


        [Display(Name = "Category")]
        public string LoanRegistrationLoanProductCategoryDescription
        {
            get
            {
                return Enum.IsDefined(typeof(LoanProductCategory), LoanRegistrationLoanProductCategory) ? EnumHelper.GetDescription((LoanProductCategory)LoanRegistrationLoanProductCategory) : string.Empty;
            }
        }


        [Display(Name = "Consecutive Income")]
        public int LoanRegistrationConsecutiveIncome { get; set; }


        [Display(Name = "Investments Multiplier")]
        public double LoanRegistrationInvestmentsMultiplier { get; set; }


        [Display(Name = "Minimum Guarantors")]
        public int LoanRegistrationMinimumGuarantors { get; set; }


        [Display(Name = "Maximum Guarantees")]
        public int LoanRegistrationMaximumGuarantees { get; set; }


        [Display(Name = "Reject if owing?")]
        public bool LoanRegistrationRejectIfMemberHasBalance { get; set; }


        [Display(Name = "Security is required?")]
        public bool LoanRegistrationSecurityRequired { get; set; }


        [Display(Name = "Allow self-guarantee?")]
        public bool LoanRegistrationAllowSelfGuarantee { get; set; }


        [Display(Name = "Grace Period")]
        public int LoanRegistrationGracePeriod { get; set; }


        [Display(Name = "Minimum Membership Period")]
        public int LoanRegistrationMinimumMembershipPeriod { get; set; }


        [Display(Name = "Payment Frequency Per Year")]
        public int LoanRegistrationPaymentFrequencyPerYear { get; set; }


        [Display(Name = "Payment Frequency Per Year")]
        public string LoanRegistrationPaymentFrequencyPerYearDescription
        {
            get
            {
                return Enum.IsDefined(typeof(PaymentFrequencyPerYear), LoanRegistrationPaymentFrequencyPerYear) ? EnumHelper.GetDescription((PaymentFrequencyPerYear)LoanRegistrationPaymentFrequencyPerYear) : string.Empty;
            }
        }


        [Display(Name = "Payment Due Date")]
        public int LoanRegistrationPaymentDueDate { get; set; }


        [Display(Name = "Payment Due Date")]
        public string LoanRegistrationPaymentDueDateDescription
        {
            get
            {
                return Enum.IsDefined(typeof(PaymentDueDate), LoanRegistrationPaymentDueDate) ? EnumHelper.GetDescription((PaymentDueDate)LoanRegistrationPaymentDueDate) : string.Empty;
            }
        }


        [Display(Name = "Payout Recovery Mode")]
        public int LoanRegistrationPayoutRecoveryMode { get; set; }


        [Display(Name = "Payout Recovery Mode")]
        public string LoanRegistrationPayoutRecoveryModeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(PayoutRecoveryMode), LoanRegistrationPayoutRecoveryMode) ? EnumHelper.GetDescription((PayoutRecoveryMode)LoanRegistrationPayoutRecoveryMode) : string.Empty;
            }
        }


        [Display(Name = "Payout Recovery Percentage")]
        public double LoanRegistrationPayoutRecoveryPercentage { get; set; }


        [Display(Name = "Aggregate Check-Off Recovery Mode")]
        public int LoanRegistrationAggregateCheckOffRecoveryMode { get; set; }


        [Display(Name = "Aggregate Check-Off Recovery Mode")]
        public string LoanRegistrationAggregateCheckOffRecoveryModeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(AggregateCheckOffRecoveryMode), LoanRegistrationAggregateCheckOffRecoveryMode) ? EnumHelper.GetDescription((AggregateCheckOffRecoveryMode)LoanRegistrationAggregateCheckOffRecoveryMode) : string.Empty;
            }
        }


        [Display(Name = "Charge clearance fee?")]
        public bool LoanRegistrationChargeClearanceFee { get; set; }


        [Display(Name = "Microcredit?")]
        public bool LoanRegistrationMicrocredit { get; set; }


        [Display(Name = "Standing Order Trigger")]
        public int LoanRegistrationStandingOrderTrigger { get; set; }


        [Display(Name = "Standing Order Trigger")]
        public string LoanRegistrationStandingOrderTriggerDescription
        {
            get
            {
                return Enum.IsDefined(typeof(StandingOrderTrigger), LoanRegistrationStandingOrderTrigger) ? EnumHelper.GetDescription((StandingOrderTrigger)LoanRegistrationStandingOrderTrigger) : string.Empty;
            }
        }


        [Display(Name = "Track arrears?")]
        public bool LoanRegistrationTrackArrears { get; set; }


        [Display(Name = "Charge arrears fee?")]
        public bool LoanRegistrationChargeArrearsFee { get; set; }


        [Display(Name = "Enforce system appraisal recommendation?")]
        public bool LoanRegistrationEnforceSystemAppraisalRecommendation { get; set; }


        [Display(Name = "Bypass verification?")]
        public bool LoanRegistrationBypassAudit { get; set; }


        [Display(Name = "Maximum self-guarantee eligible percentage")]
        public double LoanRegistrationMaximumSelfGuaranteeEligiblePercentage { get; set; }


        [Display(Name = "Guarantor Security Mode")]
        public int LoanRegistrationGuarantorSecurityMode { get; set; }


        [Display(Name = "Guarantor Security Mode")]
        public string LoanRegistrationGuarantorSecurityModeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(GuarantorSecurityMode), LoanRegistrationGuarantorSecurityMode) ? EnumHelper.GetDescription((GuarantorSecurityMode)LoanRegistrationGuarantorSecurityMode) : string.Empty;
            }
        }


        [Display(Name = "Rounding Type")]
        public int LoanRegistrationRoundingType { get; set; }


        [Display(Name = "Rounding Type")]
        public string LoanRegistrationRoundingTypeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(RoundingType), LoanRegistrationRoundingType) ? EnumHelper.GetDescription((RoundingType)LoanRegistrationRoundingType) : string.Empty;
            }
        }


        [Display(Name = "Disburse micro loan less deductions?")]
        public bool LoanRegistrationDisburseMicroLoanLessDeductions { get; set; }


        [Display(Name = "Exclude outstanding loans on maximum entitlement?")]
        public bool LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement { get; set; }


        [Display(Name = "Consider investments balance for income-based loan appraisal?")]
        public bool LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal { get; set; }


        [Display(Name = "Throttle scheduled arrears recovery?")]
        public bool LoanRegistrationThrottleScheduledArrearsRecovery { get; set; }


        [Display(Name = "Create standing order on loan verification?")]
        public bool LoanRegistrationCreateStandingOrderOnLoanAudit { get; set; }


        [Display(Name = "Maximum Amount Percentage")]
        public double MaximumAmountPercentage { get; set; }


        [Display(Name = "Take-Home Type")]
        public int TakeHomeType { get; set; }


        [Display(Name = "Take-Home Type")]
        public string TakeHomeTypeDescription
        {
            get
            {
                return Enum.IsDefined(typeof(ChargeType), TakeHomeType) ? EnumHelper.GetDescription((ChargeType)TakeHomeType) : string.Empty;
            }
        }


        [Display(Name = "Take-Home Percentage")]
        public double TakeHomePercentage { get; set; }


        [Display(Name = "Take-Home Fixed Amount")]
        public decimal TakeHomeFixedAmount { get; set; }


        [Display(Name = "Reference")]
        public string Reference { get; set; }


        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }


        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Total Number of Guarantors")]
        public int TotalNumberOfGuarantors { get; set; }

        [Display(Name = "Amount Guaranteed")]
        [CustomValidation(typeof(LoanCaseDTO2), "ValidateLoanSecurity", ErrorMessage = "Security is required for the selected loan product and following conditions must be met:-\n\n-If guarantor security mode is income, the total number of guarantors must not be less than the minimum required\n-If guarantor security mode is investments, the total amount guaranteed must not be less than the amount applied")]
        public decimal TotalAmountGuaranteed { get; set; }

        [Display(Name = "Amount Pledged")]
        public decimal TotalAmountPledged { get; set; }

        [Display(Name = "Total Collateral")]
        public decimal TotalCollateralAmount { get; set; }

        [Display(Name = "Total Number of Consecutive Incomes")]
        [CustomValidation(typeof(LoanCaseDTO2), "ValidateConsecutiveIncome", ErrorMessage = "The number of consecutive incomes falls short of the minimum required!")]
        public int TotalNumberOfIncomes { get; set; }


        [Display(Name = "Amount Applied Range Validation")]
        [CustomValidation(typeof(LoanCaseDTO2), "ValidateAmountApplied", ErrorMessage = "Amount applied is out of range for the selected loan product!")]
        public string RangeValidation { get; set; }


        [Display(Name = "Retirement Age Validation")]
        [CustomValidation(typeof(LoanCaseDTO2), "ValidateRetirementAge", ErrorMessage = "Retirement age restriction will be violated!")]
        public string RetirementAgeValidation { get; set; }


        [Display(Name = "Loan Cycle Range (Lower Limit)")]
        public decimal LoanCycleRangeLowerLimit { get; set; }


        [Display(Name = "Loan Cycle Range (Upper Limit)")]
        public decimal LoanCycleRangeUpperLimit { get; set; }


        [Display(Name = "Branch Budget Balance Validation")]
        [CustomValidation(typeof(LoanCaseDTO2), "ValidateBudgetBalance", ErrorMessage = "Amount applied will exceed the branch budget balance for the selected loan product!")]
        public decimal BranchBudgetBalance { get; set; }

        public static ValidationResult ValidateBudgetBalance(object value, ValidationContext context)
        {
            var bindingModel = context.ObjectInstance as LoanCaseDTO2;
            if (bindingModel == null)
                throw new NotSupportedException("ObjectInstance must be LoanCaseDTO");

            if (bindingModel.BranchCompanyEnforceBudgetControl && bindingModel.AmountApplied > bindingModel.BranchBudgetBalance)
            {
                return new ValidationResult("Amount applied will exceed the branch budget balance!");
            }

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateAmountApplied(object value, ValidationContext context)
        {
            var bindingModel = context.ObjectInstance as LoanCaseDTO2;

            if (bindingModel == null)
                throw new NotSupportedException("ObjectInstance must be LoanCaseDTO");

            if (bindingModel.LoanRegistrationMicrocredit)
            {
                if (bindingModel.AmountApplied > bindingModel.LoanCycleRangeUpperLimit)
                    return new ValidationResult("Amount applied is out of range!");
            }
            else if ((bindingModel.AmountApplied < bindingModel.LoanRegistrationMinimumAmount || bindingModel.AmountApplied > bindingModel.LoanRegistrationMaximumAmount))
            {
                return new ValidationResult("Amount applied is out of range!");
            }

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateLoanSecurity(object value, ValidationContext context)
        {
            var bindingModel = context.ObjectInstance as LoanCaseDTO2;
            if (bindingModel == null)
                throw new NotSupportedException("ObjectInstance must be LoanCaseDTO");

            if (!bindingModel.LoanRegistrationMicrocredit && bindingModel.LoanRegistrationSecurityRequired)
            {
                if (bindingModel.LoanRegistrationGuarantorSecurityMode == (int)GuarantorSecurityMode.Income && (bindingModel.TotalNumberOfGuarantors < bindingModel.LoanRegistrationMinimumGuarantors))
                    return new ValidationResult("The total number of guarantors is less than the minimum required!");
                else if (bindingModel.LoanRegistrationGuarantorSecurityMode == (int)GuarantorSecurityMode.Investments && ((bindingModel.TotalAmountGuaranteed + bindingModel.TotalCollateralAmount) < bindingModel.AmountApplied))
                    return new ValidationResult("The total amount guaranteed is less than amount applied!");
            }

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateConsecutiveIncome(object value, ValidationContext context)
        {
            var bindingModel = context.ObjectInstance as LoanCaseDTO2;
            if (bindingModel == null)
                throw new NotSupportedException("ObjectInstance must be LoanCaseDTO");

            if (!bindingModel.LoanRegistrationMicrocredit && bindingModel.LoanRegistrationLoanProductSection == (int)LoanProductSection.FOSA && (bindingModel.TotalNumberOfIncomes < bindingModel.LoanRegistrationConsecutiveIncome))
                return new ValidationResult("TotalNumberOfIncomes Specification Not Satisfied!");

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateRetirementAge(object value, ValidationContext context)
        {
            var bindingModel = context.ObjectInstance as LoanCaseDTO2;
            if (bindingModel == null)
                throw new NotSupportedException("ObjectInstance must be LoanCaseDTO");

            if (bindingModel.CustomerStationZoneDivisionEmployerEnforceRetirementAge && bindingModel.CustomerAge <= 0)
                return new ValidationResult("RetirementAge Specification Not Satisfied!");
            else
            {
                var ageAtLoanTermCompletion = bindingModel.CustomerAge;

                switch ((CustomerType)bindingModel.CustomerType)
                {
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Individual:
                        if (bindingModel.CustomerIndividualBirthDate.HasValue)
                            ageAtLoanTermCompletion = UberUtil.GetAge(bindingModel.CustomerIndividualBirthDate.Value.AddMonths(bindingModel.LoanRegistrationTermInMonths * -1));
                        break;
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Partnership:
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Corporation:
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.MicroCredit:
                        if (bindingModel.CustomerNonIndividualDateEstablished.HasValue)
                            ageAtLoanTermCompletion = UberUtil.GetAge(bindingModel.CustomerNonIndividualDateEstablished.Value.AddMonths(bindingModel.LoanRegistrationTermInMonths * -1));
                        break;
                    default:
                        break;
                }

                if (bindingModel.CustomerStationZoneDivisionEmployerEnforceRetirementAge && ageAtLoanTermCompletion <= 0)
                    return new ValidationResult("RetirementAge Specification Not Satisfied!");
                else if (bindingModel.CustomerStationZoneDivisionEmployerEnforceRetirementAge && (ageAtLoanTermCompletion > bindingModel.CustomerStationZoneDivisionEmployerRetirementAge))
                    return new ValidationResult("RetirementAge Specification Not Satisfied!");

                return ValidationResult.Success;
            }
        }



        [Display(Name = "Employer Name")]
        public string EmployerName { get; set; }

        // Additional Guarantor DTOs

        [Display(Name = "Customer")]
        public Guid GuarantorId { get; set; }


        [Display(Name = "First Name")]
        public string GuarantorIndividualFirstName { get; set; }


        [Display(Name = "Type")]
        public string GuarantorTypeDescription { get; set; }


        [Display(Name = "Remarks")]
        public string GuarantorRemarks { get; set; }


        [Display(Name = "Other Names")]
        public string GuarantorIndividualLastName { get; set; }


        [Display(Name = "Substitute Guarantor")]
        public string GuarantorName { get; set; }


        [Display(Name = "Station")]
        public Guid GuarantorStationId { get; set; }


        [Display(Name = "Station")]
        public string GuarantorStationDescription { get; set; }


        [Display(Name = "Employer")]
        public Guid GuarantorEmployerId { get; set; }


        [Display(Name = "Employer")]
        public string GuarantorEmployerDescription { get; set; }


        [Display(Name = "Identification Number")]
        public string GuarantorIdentificationNumber { get; set; }


        [Display(Name = "Account Number")]
        public string GuarantorReference1 { get; set; }


        [Display(Name = "Membership Number")]
        public string GuarantorReference2 { get; set; }


        [Display(Name = "Personal File Number")]
        public string GuarantorReference3 { get; set; }


        [Display(Name = "Appraisal Factor")]
        public int AppraisalFactor { get; set; }


        [Display(Name = "Total Shares")]
        public decimal GuarantorTotalshares { get; set; }


        [Display(Name = "Committed Shares")]
        public decimal GuarantorCommittedShares { get; set; }


        [Display(Name = "Amount Guaranteed")]
        public decimal GuarantorAmountGuaranteed { get; set; }


        [Display(Name = "Interest Calculation Mode")]
        public string InterestCalculationModeDescription { get; set; }


        [Display(Name = "Section")]
        public int LoanProductSectionSection { get; set; }


        [Display(Name = "Section")]
        public string LoanProductSectionDescription { get; set; }



        [Display(Name = "Full Account Number")]
        public string CustomerAccountFullAccountNumber { get; set; }


        // Additional Customer Accounts DTOs

        [Display(Name = "Customer Account")]
        public Guid CustomerAccountId { get; set; }


        [Display(Name = "Full Account Number")]
        public string CustomerAccountFullNumber { get; set; }


        // Loan Case Filter

        [Display(Name = "Loan Case Filter")]
        public int filterText { get; set; }



        [Display(Name = "Loan Case Filter")]
        public string filterTextDescription
        {
            get
            {
                return Enum.IsDefined(typeof(LoanCaseFilter), filterText) ? EnumHelper.GetDescription((LoanCaseFilter)filterText) : string.Empty;
            }
        }


        public string ErrorMessageResult { get; set; }


        // Additional Fields for Loan Qualification Section in Loan Appraisal

        [Display(Name = "Maximum Loan")]
        public decimal LoanRegistrationMaximumLoan { get; set; }


        [Display(Name = "Outstanding Loans Balance")]
        public decimal LoanRegistrationOutstandingLoansBalance { get; set; }


        [Display(Name = "Maxmimum Entitled")]
        public decimal LoanRegistrationMaximumEntitled { get; set; }


        [Display(Name = "Net Income")]
        public decimal LoanRegistrationNetIncome { get; set; }


        [Display(Name = "Total Allowance")]
        public decimal LoanRegistrationTotalAllowance { get; set; }


        [Display(Name = "Total Deduction")]
        public decimal LoanRegistrationTotalDeduction { get; set; }


        [Display(Name = "Total Income")]
        public decimal LoanRegistrationTotalIncome { get; set; }



        [Display(Name = "Ability to Pay")]
        public decimal LoanRegistrationAbilityToPay { get; set; }


        [Display(Name = "Ability to Pay Over Loan Term")]
        public decimal LoanRegistrationAbilityToPayOverLoanTerm { get; set; }


        [Display(Name = "Loan + Interest")]
        public decimal LoanRegistrationLoanPlusInterest { get; set; }


        [Display(Name = "Loan Part")]
        public decimal LoanRegistrationLoanPart { get; set; }


        [Display(Name = "Interest Part")]
        public decimal LoanRegistrationInterestPart { get; set; }


        [Display(Name = "Account Status")]
        public string AccountStatus { get; set; }


        [Display(Name = "Principal Balance")]
        public decimal PrincipalBalance { get; set; }


        [Display(Name = "Interest Balance")]
        public decimal InterestBalance { get; set; }


        [Display(Name = "Payment Per Period")]
        public double PaymentPerPeriod { get; set; }


        [Display(Name = "Number Of Periods")]
        public int NumberOfPeriods { get; set; }



        // Additional DTO

        // Reports DTOs

        public DateTime EndDate { get; set; }


        public DateTime StartDate { get; set; }



        // 

        [Display(Name = "Security Qualification")]
        public double LoanQualificationSecurityQualification { get; set; }


        [Display(Name = "System Recommendation")]
        public double LoanQualificationSystemRecommendation { get; set; }


        [Display(Name = "Income Qualification")]
        public double LoanQualificationIncomeQualification { get; set; }


        [Display(Name = "Investments Qualification")]
        public double LoanQualificationInvestmentsQualification { get; set; }


        [Display(Name = "Attached Loans Balance")]
        public double LoanQualificationAttachedLoansBalance { get; set; }


        [Display(Name = "Total Loans + Interest")]
        public double LoanQualificationTotalLoansPlusInterest { get; set; }


        [Display(Name = "Loan Amount")]
        public double LoanQualificationLoanAmount { get; set; }




        [Display(Name = "Standing Order Principal")]
        public double StandingOrderPrincipal { get; set; }


        [Display(Name = "Standing Order Interest")]
        public double StandingOrderInterest { get; set; }






        [Display(Name = "Record Status")]
        public int RecordStatus { get; set; }


        [Display(Name = "Record Status")]
        public string RecordStatusDescription
        {
            get
            {
                return Enum.IsDefined(typeof(RecordStatus), RecordStatus) ? EnumHelper.GetDescription((RecordStatus)RecordStatus) : string.Empty;
            }
        }

        [Display(Name = "Customer Filter")]
        public int CustomerFilter { get; set; }

        [Display(Name = "Customer Filter")]
        public string CustomerFilterDescription
        {
            get
            {
                return EnumHelper.GetDescription((CustomerFilter)CustomerFilter);
            }
        }
        public Guid DocumentID { get; set; }  // Unique identifier for the document

        public byte[] PassportPhoto { get; set; }
        public byte[] SignaturePhoto { get; set; }
        public byte[] IDCardFrontPhoto { get; set; }
        public byte[] IDCardBackPhoto { get; set; }


        public string LoanStatus { get; set; }


        //Additional DTOs

        public string FullAccountNumber { get; set; }



        public string loanProductSection { get; set; }


        public string loanProductPaymentFrequencyPerYear { get; set; }


        public string LoanProductCategory { get; set; }
        public List<LoanGuarantorDTO> Guarantors { get;  set; }
        public string SectorCode { get;  set; }
        public string SubSectorCode { get;  set; }
    }
}