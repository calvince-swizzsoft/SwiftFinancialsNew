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

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class AppraiseLoanController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            ViewBag.LoanCaseFilterSelectList = GetLoanCaseFilterTypeSelectList(string.Empty);
            ViewBag.LoanCaseStatusSelectList = GetLoanCaseStatusSelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, string filterValue, int filterType)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync(
                (int)LoanCaseStatus.Registered,
                filterValue,
                filterType,
                0,
                int.MaxValue,
                includeBatchStatus: true,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(loanCase => loanCase.CreatedDate)
                    .ToList();

                totalRecordCount = sortedData.Count;

                var paginatedData = sortedData
                    .Skip(jQueryDataTablesModel.iDisplayStart)
                    .Take(jQueryDataTablesModel.iDisplayLength)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? sortedData.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: paginatedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }

            return this.DataTablesJson(
                items: new List<LoanCaseDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }

        public async Task<ActionResult> IncomeAdjustmentsLookUp(Guid? id, IncomeAdjustmentDTO model)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            var incomeAdjustment = await _channelService.FindIncomeAdjustmentAsync(parseId, GetServiceHeader());
            if (incomeAdjustment != null)
            {
                model.Id = incomeAdjustment.Id;
                model.Description = incomeAdjustment.Description;
                model.Type = incomeAdjustment.Type;
                model.typeTypeDescription = incomeAdjustment.TypeDescription;


                return Json(new
                {
                    success = true,
                    data = new
                    {
                        IncomeAdjustment = model.Description,
                        Id = model.Id,
                        Type = model.Type,
                        TypeDescription = model.typeTypeDescription
                    }
                });
            }

            return Json(new { success = false, message = "Income Adjustment not found" });
        }

        public async Task<ActionResult> Appraise(Guid Id, Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.LoanAppraisalOptionSelectList = GetLoanAppraisalOptionSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var attachedLoans = await _channelService.FindAttachedLoansByLoanCaseIdAsync(parseId, GetServiceHeader());

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            var loanBalance = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());

            var loaneeCustomer = await _channelService.FindLoanCaseAsync(Id, GetServiceHeader());

            //var AttachedLoans = await FindAttachedLoans(parseId);

            LoanCaseDTO loanCaseDTO = new LoanCaseDTO();


            if (loaneeCustomer != null)
            {
                loanCaseDTO = loaneeCustomer;

                //// Standing Orders
                ObservableCollection<Guid> customerAccountId = new ObservableCollection<Guid>();
                var customerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(loaneeCustomer.CustomerId, true, true, true, true, GetServiceHeader());

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


                //// Income History
                //// Payouts
                var payouts = await _channelService.FindLoanDisbursementBatchEntriesByCustomerIdAsync((int)BatchStatus.Posted, loaneeCustomer.CustomerId, GetServiceHeader());
                if (payouts != null)
                {
                    ViewBag.Payouts = payouts;
                }


                ////Salary
                // No method fetching by customerId



                //// Loan Applications
                var loanApplications = await _channelService.FindLoanCasesByCustomerIdInProcessAsync(loaneeCustomer.CustomerId, GetServiceHeader());
                if (loanApplications != null)
                {
                    ViewBag.LoanApplications = loanApplications;
                }

                //// Collaterals...
                // No method fetching by customerId

                // Guarantors
                var loanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(parseId, GetServiceHeader());
                if (loanGuarantors != null)
                {
                    ViewBag.LoanGuarantors = loanGuarantors;
                }


                // Loan Accounts
                var findloanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(loaneeCustomer.CustomerId, true, true, true, true, GetServiceHeader());
                var LoanAccounts = findloanAccounts.Where(L => L.CustomerAccountTypeProductCode == (int)ProductCode.Loan);
                if (LoanAccounts != null)
                {
                    ViewBag.CustomerAccounts = LoanAccounts;
                    Session["printCustomerId"] = loaneeCustomer.CustomerId;
                }


                // Repayment Schedule .....................................
                ViewBag.APR = loaneeCustomer.LoanInterestAnnualPercentageRate;
                ViewBag.InterestCalculationMode = loaneeCustomer.LoanInterestCalculationModeDescription;
                ViewBag.AmountApplied = loaneeCustomer.AmountApplied;
                ViewBag.TermInMonths = loaneeCustomer.LoanRegistrationTermInMonths;


                // Print Data
                Session["formData"] = loanCaseDTO;
                Session["standingOrders"] = allStandingOrders;
                Session["payouts"] = payouts;
                Session["loanApplications"] = loanApplications;
                Session["loanGuarantors"] = loanGuarantors;
                Session["loanAccounts"] = LoanAccounts;

                Session["APR"] = loanCaseDTO.LoanInterestAnnualPercentageRate;
                Session["InterestCalculationMode"] = loanCaseDTO.LoanInterestCalculationModeDescription;
                Session["AmountApplied"] = loanCaseDTO.AmountApplied;
                Session["TermInMonths"] = loanCaseDTO.LoanRegistrationTermInMonths;
            }



            loanCaseDTO.LoanRegistrationOutstandingLoansBalance = await GetOutstandingLoansBalanceAsync();
            loanCaseDTO.LoanRegistrationTotalIncome = await CalculateTotalIncomeAdditionsAsync();
            loanCaseDTO.LoanRegistrationMaximumEntitled = await GetMaximumEntitledAsync();
            loanCaseDTO.LoanRegistrationAbilityToPay = await CalculateMonthlyAbilityAsync();
            loanCaseDTO.LoanRegistrationAbilityToPayOverLoanTerm = await CalculateAbilityOverLoanPeriodAsync();
            loanCaseDTO.TakeHomeFixedAmount = await CalculateTwoThirdsToRepayLoanAsync();

            Session["Model"] = loaneeCustomer;

            return View(loanCaseDTO);
        }

        [HttpPost]
        public async Task<JsonResult> Add(LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            var loanAppraisalFactors = Session["loanAppraisalFactors"] as ObservableCollection<IncomeAdjustmentDTO>;

            if (loanAppraisalFactors == null)
            {
                loanAppraisalFactors = new ObservableCollection<IncomeAdjustmentDTO>();
            }

            foreach (var incomeadjustment in loanCaseDTO.incomeAdjustmentDTO)
            {
                var existingEntry = loanAppraisalFactors.FirstOrDefault(e => e.Id == incomeadjustment.Id);

                if (existingEntry != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "The Selected Income Adjustment has already been added to the Income Adjustments List."
                    });
                }

                loanAppraisalFactors.Add(incomeadjustment);
            }

            Session["loanAppraisalFactors"] = loanAppraisalFactors;

            return Json(new { success = true, entries = loanAppraisalFactors });
        }

        [HttpPost]
        public async Task<JsonResult> Remove(Guid id)
        {
            await ServeNavigationMenus();

            var loanAppraisalFactors = Session["loanAppraisalFactors"] as ObservableCollection<IncomeAdjustmentDTO>;

            if (loanAppraisalFactors != null)
            {
                var entryToRemove = loanAppraisalFactors.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    loanAppraisalFactors.Remove(entryToRemove);
                    Session["loanAppraisalFactors"] = loanAppraisalFactors;
                }
            }

            return Json(new { success = true, data = loanAppraisalFactors });
        }


        public async Task<ActionResult> Print()
        {
            await ServeNavigationMenus();

            Guid id = (Guid)Session["loanCaseId"];
            Guid customerId = (Guid)Session["printCustomerId"];

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


            // Guarantors
            var loanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(id, GetServiceHeader());
            if (loanGuarantors != null)
            {
                ViewBag.PrintLoanGuarantors = loanGuarantors;
            }

            // Loan Accounts
            var findloanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(customerId, true, true, true, true, GetServiceHeader());
            var LoanAccounts = findloanAccounts.Where(L => L.CustomerAccountTypeProductCode == (int)ProductCode.Loan);
            if (LoanAccounts != null)
            {
                ViewBag.PrintCustomerAccounts = LoanAccounts;
            }





            ViewBag.FormData = loanCaseDTO;

            ViewBag.APR = APR;
            ViewBag.AmountApplied = AmountApplied;
            ViewBag.ICM = ICM;
            ViewBag.TIM = TIM;

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Appraise(LoanCaseDTO loanCaseDTO, ObservableCollection<LoanAppraisalFactorDTO> incomeAdjustments)
        {
            var findLoanCaseDetails = await _channelService.FindLoanCaseAsync(loanCaseDTO.Id, GetServiceHeader());

            loanCaseDTO.LoanProductId = findLoanCaseDetails.LoanProductId;
            loanCaseDTO.LoanPurposeId = findLoanCaseDetails.LoanPurposeId;
            loanCaseDTO.SavingsProductId = findLoanCaseDetails.SavingsProductId;
            loanCaseDTO.AppraisedDate = DateTime.Now;

            loanCaseDTO.ValidateAll();


            if (!loanCaseDTO.HasErrors)
            {
                var appraiseLoanSuccess = await _channelService.AppraiseLoanCaseAsync(loanCaseDTO, loanCaseDTO.LoanAppraisalOption, 1234, GetServiceHeader());

                await _channelService.UpdateLoanAppraisalFactorsAsync(loanCaseDTO.Id, incomeAdjustments, GetServiceHeader());

                TempData["Success"] = "Operation Completed Successfully.";

                string message2 = string.Format(
                              "Would you like to print?"
                          );

                DialogResult result2 = MessageBox.Show(
                    message2,
                    "Loan Appraisal",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );

                if (result2 == DialogResult.Yes)
                {
                    return RedirectToAction("Print");
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            else
            {
                await ServeNavigationMenus();

                var errorMessages = loanCaseDTO.ErrorMessages.ToString();

                TempData["BugdetBalance"] = errorMessages;

                ViewBag.LoanAppraisalOptionSelectList = GetLoanAppraisalOptionSelectList(loanCaseDTO.LoanAppraisalOption.ToString());

                TempData["Fail"] = "Operation Failed!";
                return View(loanCaseDTO);
            }
        }




        
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
                }
            }
        }

        
        public async Task<decimal> GetOutstandingLoansBalanceAsync()
        {
            var result = 0m;

            var selectedLoanCase = Session["selectedLoanCase"] as LoanCaseDTO;

            if (selectedLoanCase != null)
            {
                var attachedLoanDTOs = await _channelService.FindAttachedLoansByLoanCaseIdAsync(selectedLoanCase.Id);

                if (attachedLoanDTOs != null && attachedLoanDTOs.Any())
                {
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


        public async Task<decimal> GetMaximumEntitledAsync()
        {
            var selectedLoanCase = Session["selectedLoanCase"] as LoanCaseDTO;

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
        }

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
        }

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