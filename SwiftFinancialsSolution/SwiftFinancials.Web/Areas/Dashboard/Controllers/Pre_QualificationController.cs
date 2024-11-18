using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.List;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Dashboard.Controllers
{
    public class Pre_QualificationController : MasterController
    {
        public async Task<ActionResult> Appraise()
        {
            return View();
        }



        public async Task<ActionResult> Print()
        {
            await ServeNavigationMenus();

            Guid id = (Guid)Session["loanCaseId"];

            var loanCase = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());

            LoanCaseDTO loanCaseDTO = new LoanCaseDTO();

            loanCaseDTO = loanCase as LoanCaseDTO;


            //// Standing Orders
            ObservableCollection<Guid> customerAccountId = new ObservableCollection<Guid>();
            var customerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(loanCase.CustomerId, true, true, true, true, GetServiceHeader());

            foreach (var accounts in customerAccounts)
            {
                customerAccountId.Add(accounts.Id);
            }

            List<StandingOrderDTO> allStandingOrders = new List<StandingOrderDTO>();

            // Iterate through each account ID and collect standing orders
            foreach (var Ids in customerAccountId)
            {
                var standingOrders = await _channelService.FindStandingOrdersByBeneficiaryCustomerAccountIdAsync(Ids, true, GetServiceHeader());
                if (standingOrders != null && standingOrders.Any())
                {
                    allStandingOrders.AddRange(standingOrders); // Add standing orders to the collection
                }
                else
                {
                    TempData["EmptystandingOrders"] = "Selected Customer has no Standing Orders.";
                }
            }
            ViewBag.StandingOrders = allStandingOrders;



            //// Loan Applications
            var loanApplications = await _channelService.FindLoanCasesByCustomerIdInProcessAsync(loanCase.CustomerId, GetServiceHeader());
            if (loanApplications != null)
            {
                ViewBag.LoanApplications = loanApplications;
            }



            // repayment schedule
            var APR = loanCase.LoanInterestAnnualPercentageRate;
            var AmountApplied = loanCase.AmountApplied;
            var ICM = loanCase.LoanInterestCalculationModeDescription;
            var TIM = loanCase.LoanRegistrationTermInMonths;


            ViewBag.FormData = loanCaseDTO;

            ViewBag.APR = APR;
            ViewBag.AmountApplied = AmountApplied;
            ViewBag.ICM = ICM;
            ViewBag.TIM = TIM;

            return View();
        }



        // Attached Loans
        SelectionList<AttachedLoanDTO> _attachedLoans;
        public SelectionList<AttachedLoanDTO> AttachedLoans
        {
            get
            {
                if (_attachedLoans == null)
                    _attachedLoans = new SelectionList<AttachedLoanDTO>(new List<AttachedLoanDTO> { });
                return _attachedLoans;
            }
            set
            {
                if (_attachedLoans != value)
                {
                    _attachedLoans = value;
                    //RaisePropertyChanged(() => AttachedLoans);
                }
            }
        }


        // Outstanding Loan Balance
        public async Task<decimal> GetOutstandingLoansBalanceAsync()
        {
            var result = 0m;

            var selectedLoanCase = Session["selectedLoanCase"] as LoanCaseDTO;

            if (selectedLoanCase != null)
            {
                var attachedLoanDTOs = await _channelService.FindAttachedLoansByLoanCaseIdAsync(selectedLoanCase.Id);

                if (attachedLoanDTOs != null && attachedLoanDTOs.Any())
                {
                    // Clear previous attached loans and add new ones
                    AttachedLoans.Clear();

                    foreach (var item in attachedLoanDTOs)
                    {
                        var selectionItem = new SelectionItem<AttachedLoanDTO>(true, item);
                        AttachedLoans.Add(selectionItem);
                    }
                }

                foreach (var selectionItem in AttachedLoans)
                {
                    var totalValue = selectionItem.Item.PrincipalBalance
                                   + selectionItem.Item.InterestBalance
                                   + selectionItem.Item.CarryForwardsBalance;

                    // Check if the loan belongs to the selected loan product
                    if (selectionItem.Item.CustomerAccountCustomerAccountTypeTargetProductId == selectedLoanCase.LoanProductId)
                    {
                        if (!selectionItem.IsSelected)
                        {
                            result += totalValue;
                        }
                    }
                    else if (selectionItem.Item.CustomerAccountTypeTargetProductProductSection == selectedLoanCase.LoanRegistrationLoanProductSection)
                    {
                        result += selectionItem.IsSelected ? -totalValue : totalValue;
                    }
                }
            }

            return result < 0m ? 0m : result;
        }



        public decimal MaximumLoan
        {
            get
            {
                var selectedLoanCase = Session["selectedLoanCase"] as LoanCaseDTO;

                return (selectedLoanCase != null) ? selectedLoanCase.LoanProductInvestmentsBalance * Convert.ToDecimal(selectedLoanCase.LoanRegistrationInvestmentsMultiplier) : 0m;
            }
            set { }
        }



        // Maximum Entitled
        public async Task<decimal> GetMaximumEntitledAsync()
        {
            var selectedLoanCase = Session["selectedLoanCase"] as LoanCaseDTO;

            // Calculate the maximum entitled amount
            decimal maximumEntitled = MaximumLoan;

            if (selectedLoanCase != null && !selectedLoanCase.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement)
            {
                var outstandingBalance = await GetOutstandingLoansBalanceAsync();
                maximumEntitled += outstandingBalance;
            }

            return maximumEntitled;
        }


        decimal _netIncome;
        public decimal NetIncome
        {
            get { return _netIncome; }
            set
            {
                if (_netIncome != value)
                {
                    _netIncome = Math.Abs(value);
                }
            }
        }


        // Monthly Additions
        private decimal _totalIncomeAdditions;

        public async Task<decimal> CalculateTotalIncomeAdditionsAsync()
        {
            var selectedLoanCase = Session["selectedLoanCase"] as LoanCaseDTO;

            if (selectedLoanCase != null)
            {
                var loanAppraisalFactors = await _channelService.FindLoanAppraisalFactorsByLoanCaseIdAsync(selectedLoanCase.Id, GetServiceHeader());

                if (loanAppraisalFactors != null)
                {
                    _totalIncomeAdditions = loanAppraisalFactors
                        .Where(x => x.Type == (int)IncomeAdjustmentType.Allowance && x.IsEnabled)
                        .Sum(x => x.Amount);
                }
                else
                {
                    _totalIncomeAdditions = 0m;
                }
            }
            else
            {
                _totalIncomeAdditions = 0m;
            }

            return _totalIncomeAdditions;
        }




        // Monthly Deductions
        private decimal _totalIncomeDeductions;

        public async Task<decimal> CalculateTotalIncomeDeductionsAsync()
        {
            var selectedLoanCase = Session["selectedLoanCase"] as LoanCaseDTO;

            if (selectedLoanCase != null)
            {
                var loanAppraisalFactors = await _channelService.FindLoanAppraisalFactorsByLoanCaseIdAsync(selectedLoanCase.Id, GetServiceHeader());

                if (loanAppraisalFactors != null)
                {
                    _totalIncomeDeductions = loanAppraisalFactors
                        .Where(x => x.Type == (int)IncomeAdjustmentType.Deduction && x.IsEnabled)
                        .Sum(x => x.Amount);
                }
                else
                {
                    _totalIncomeDeductions = 0m;
                }
            }
            else
            {
                _totalIncomeDeductions = 0m;
            }

            return _totalIncomeDeductions;
        }

        public decimal TotalIncomeDeductions
        {
            get => _totalIncomeDeductions;
            set
            {
                if (_totalIncomeDeductions != value)
                {
                    _totalIncomeDeductions = value;
                }
            }
        }



        // Monthly Ability
        private decimal _monthlyAbility;

        public async Task<decimal> CalculateMonthlyAbilityAsync()
        {
            var result = 0m;

            var selectedLoanCase = Session["selectedLoanCase"] as LoanCaseDTO;

            if (selectedLoanCase != null)
            {
                switch ((LoanProductSection)selectedLoanCase.LoanRegistrationLoanProductSection)
                {
                    case LoanProductSection.BOSA:
                        result = NetIncome + (await CalculateTotalIncomeAdditionsAsync()) - (await CalculateTotalIncomeDeductionsAsync());
                        break;
                    case LoanProductSection.FOSA:
                        result = NetIncome;
                        break;
                    default:
                        break;
                }
            }

            _monthlyAbility = result;

            return _monthlyAbility;
            // Notify that the MonthlyAbility property has changed
        }



        // Take Home Retention
        public decimal TakeHomeRetention
        {
            get
            {
                var result = 0m;

                var selectedLoanCase = Session["selectedLoanCase"] as LoanCaseDTO;

                if (selectedLoanCase == null) return result;

                switch ((ChargeType)selectedLoanCase.TakeHomeType)
                {
                    case ChargeType.Percentage:
                        result = Math.Round(Convert.ToDecimal((selectedLoanCase.TakeHomePercentage * Convert.ToDouble(NetIncome)) / 100), 4);
                        break;
                    case ChargeType.FixedAmount:
                        result = selectedLoanCase.TakeHomeFixedAmount;
                        break;
                    default:
                        break;
                }

                return result;
            }
            set { }
        }



        // Two Thirds To Repay
        private decimal _twoThirdsToRepayLoan;

        public async Task<decimal> CalculateTwoThirdsToRepayLoanAsync()
        {
            var result = 0m;

            var selectedLoanCase = Session["selectedLoanCase"] as LoanCaseDTO;

            if (selectedLoanCase != null)
            {
                switch ((ChargeType)selectedLoanCase.TakeHomeType)
                {
                    case ChargeType.Percentage:
                        result = Math.Round(
                            Convert.ToDecimal(((100d - selectedLoanCase.TakeHomePercentage) * Convert.ToDouble(NetIncome)) / 100),
                            4)
                            + await CalculateTotalIncomeAdditionsAsync()
                            - await CalculateTotalIncomeDeductionsAsync();
                        break;

                    case ChargeType.FixedAmount:
                        result = (NetIncome - selectedLoanCase.TakeHomeFixedAmount)
                            + await CalculateTotalIncomeAdditionsAsync()
                            - await CalculateTotalIncomeDeductionsAsync();
                        break;

                    default:
                        break;
                }
            }

            _twoThirdsToRepayLoan = result;

            return _twoThirdsToRepayLoan;
            // Notify that the TwoThirdsToRepayLoan property has changed
        }



        // Ability Over Loan Period
        private decimal _abilityOverLoanPeriod;

        public async Task<decimal> CalculateAbilityOverLoanPeriodAsync()
        {
            var result = 0m;

            var selectedLoanCase = Session["selectedLoanCase"] as LoanCaseDTO;

            if (selectedLoanCase != null)
            {
                var twoThirdsToRepayLoan = await CalculateTwoThirdsToRepayLoanAsync();
                result = twoThirdsToRepayLoan * selectedLoanCase.LoanRegistrationTermInMonths;
            }

            _abilityOverLoanPeriod = result;

            return _abilityOverLoanPeriod;
            // Notify that the AbilityOverLoanPeriod property has changed
        }




        decimal _loanPrincipal;
        public decimal LoanPrincipal
        {
            get { return _loanPrincipal; }
            set
            {
                if (_loanPrincipal != value)
                {
                    _loanPrincipal = value;
                }
            }
        }



        decimal _loanInterest;
        public decimal LoanInterest
        {
            get { return _loanInterest; }
            set
            {
                if (_loanInterest != value)
                {
                    _loanInterest = value;
                }
            }
        }



        decimal _totalLoan;
        public decimal TotalLoan
        {
            get => _totalLoan;
            set
            {
                if (_totalLoan != value)
                {
                    _totalLoan = value;
                }
            }
        }


        // Total Loan
        public async Task<decimal> CalculateTotalLoanAsync()
        {
            var selectedLoanCase = Session["selectedLoanCase"] as LoanCaseDTO;

            if (selectedLoanCase == null)
            {
                TotalLoan = 0m;
                return TotalLoan;
            }

            // Calculate AbilityOverLoanPeriod (this needs to be defined earlier in your code)
            TotalLoan = (await CalculateAbilityOverLoanPeriodAsync());

            // Calculate LoanPrincipal using the asynchronous service call
            _loanPrincipal = (decimal)await _channelService.PVAsync(
                selectedLoanCase.LoanRegistrationTermInMonths,
                selectedLoanCase.LoanRegistrationPaymentFrequencyPerYear,
                selectedLoanCase.LoanInterestAnnualPercentageRate,
                -(double)await CalculateTwoThirdsToRepayLoanAsync(),
                0d,
                selectedLoanCase.LoanRegistrationPaymentDueDate
            );

            // Calculate LoanInterest
            _loanInterest = TotalLoan - _loanPrincipal;

            return TotalLoan;
        }



        // Total Loan Case Security
        private decimal _totalLoanCaseSecurity;

        public async Task CalculateTotalLoanCaseSecurityAsync()
        {
            var result = 0m;

            var selectedLoanCase = Session["selectedLoanCase"] as LoanCaseDTO;

            if (selectedLoanCase != null)
            {
                var loanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(selectedLoanCase.Id, GetServiceHeader());

                if (loanGuarantors != null)
                {
                    result += loanGuarantors.Sum(x => x.AmountGuaranteed) + loanGuarantors.Sum(x => x.AmountPledged);
                }

                var loanCollaterals = await _channelService.FindLoanCollateralsByLoanCaseIdAsync(selectedLoanCase.Id, GetServiceHeader());
                if (loanCollaterals != null)
                {
                    result += loanCollaterals.Sum(x => x.Value);
                }
            }

            _totalLoanCaseSecurity = result;
        }
    }
}