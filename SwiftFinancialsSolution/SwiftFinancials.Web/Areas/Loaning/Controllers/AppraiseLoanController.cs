using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.List;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class AppraiseLoanController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync((int)LoanCaseStatus.Registered, jQueryDataTablesModel.sSearch, (int)LoanCaseFilter.CustomerFirstName, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Appraise(Guid Id, Guid? id)
        {
            await ServeNavigationMenus();
            int caseNumber = 0;

            ViewBag.LoanAppraisalOptionSelectList = GetLoanAppraisalOptionSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            var loanBalance = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());

            var loaneeCustomer = await _channelService.FindLoanCaseAsync(Id, GetServiceHeader());


            LoanCaseDTO loanCaseDTO = new LoanCaseDTO();


            if (loaneeCustomer != null)
            {
                loanCaseDTO.CaseNumber = loaneeCustomer.CaseNumber;
                loanCaseDTO.CustomerId = loaneeCustomer.CustomerId;
                loanCaseDTO.CustomerIndividualFirstName = loaneeCustomer.CustomerIndividualSalutationDescription + " " + loaneeCustomer.CustomerIndividualFirstName + " " + loaneeCustomer.CustomerIndividualLastName;
                loanCaseDTO.CustomerReference2 = loaneeCustomer.CustomerReference2;
                loanCaseDTO.CustomerReference1 = loaneeCustomer.CustomerReference1;
                loanCaseDTO.LoanProductDescription = loaneeCustomer.LoanProductDescription;
                loanCaseDTO.CustomerReference3 = loaneeCustomer.CustomerReference3;
                loanCaseDTO.LoanPurposeDescription = loaneeCustomer.LoanPurposeDescription;
                loanCaseDTO.LoanRegistrationMaximumAmount = loaneeCustomer.LoanRegistrationMaximumAmount;
                loanCaseDTO.MaximumAmountPercentage = loaneeCustomer.MaximumAmountPercentage;
                loanCaseDTO.AmountApplied = loaneeCustomer.AmountApplied;
                loanCaseDTO.LoanRegistrationTermInMonths = loaneeCustomer.LoanRegistrationTermInMonths;
                loanCaseDTO.BranchDescription = loaneeCustomer.BranchDescription;
                loanCaseDTO.Reference = loaneeCustomer.Reference;

                Session["selectedLoanCase"] = loanCaseDTO;

                // Loan Accounts
                int[] array = new int[] { 2 };
                var loanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(loaneeCustomer.CustomerId, array, true, true, true, true, GetServiceHeader());
                if (loanAccounts != null)
                {

                }


                // Standing Orders
                ObservableCollection<Guid> customerAccountId = new ObservableCollection<Guid>();
                var customerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(Id, true, true, true, true, GetServiceHeader());

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
                }
                ViewBag.StandingOrders = allStandingOrders;



            }

            loanCaseDTO.LoanRegistrationOutstandingLoansBalance = await GetOutstandingLoansBalanceAsync();
            loanCaseDTO.LoanRegistrationTotalIncome = await CalculateTotalIncomeAdditionsAsync();
            loanCaseDTO.LoanRegistrationMaximumEntitled = await GetMaximumEntitledAsync();
            loanCaseDTO.LoanRegistrationAbilityToPay = await CalculateMonthlyAbilityAsync();
            loanCaseDTO.LoanRegistrationAbilityToPayOverLoanTerm = await CalculateAbilityOverLoanPeriodAsync();
            loanCaseDTO.TakeHomeFixedAmount = await CalculateTwoThirdsToRepayLoanAsync();

            return View(loanCaseDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Appraise(LoanCaseDTO loanCaseDTO)
        {
            var loanDTO = await _channelService.FindLoanCaseAsync(loanCaseDTO.Id, GetServiceHeader());

            loanDTO.AppraisedAmount = loanCaseDTO.AppraisedAmount;
            loanDTO.SystemAppraisalRemarks = loanCaseDTO.SystemAppraisalRemarks;
            loanDTO.AppraisalRemarks = loanCaseDTO.AppraisalRemarks;
            loanDTO.AppraisedAmountRemarks = loanCaseDTO.AppraisedAmountRemarks;

            loanDTO.ValidateAll();

            if (loanDTO.AppraisedAmount == 0 || loanDTO.SystemAppraisalRemarks == string.Empty || loanDTO.AppraisalRemarks == string.Empty)
            {
                TempData["AppraisedAmount"] = "Appraised amount, System Appraisal Remarks and Appraisal Remarks required to appraise loan.";
                ViewBag.LoanAppraisalOptionSelectList = GetLoanAppraisalOptionSelectList(loanCaseDTO.LoanAppraisalOption.ToString());
                return View();
            }

            if (!loanDTO.HasErrors)
            {
                var appraiseLoanSuccess = await _channelService.AppraiseLoanCaseAsync(loanDTO, loanCaseDTO.LoanAppraisalOption, 1, GetServiceHeader());

                TempData["approve"] = "Loan Appraisal Successful";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanDTO.ErrorMessages.ToString();

                TempData["BugdetBalance"] = errorMessages;

                ViewBag.LoanAppraisalOptionSelectList = GetLoanAppraisalOptionSelectList(loanCaseDTO.LoanAppraisalOption.ToString());

                TempData["approveError"] = "Loan Appraisal Unsuccessful";

                return View(loanCaseDTO);
            }
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