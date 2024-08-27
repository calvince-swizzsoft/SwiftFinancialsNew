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
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class LoanRegistrationController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.LoanCaseFilterSelectList = GetLoanCaseFilterTypeSelectList(string.Empty);

            return View();
        }


        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanCasesByFilterInPageAsync(jQueryDataTablesModel.sSearch, 10, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        [HttpPost]
        public async Task<JsonResult> GetAllLoanCases(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            double positiveInfinity = double.PositiveInfinity;
            int positiveInfinityAsInt = positiveInfinity > int.MaxValue ? int.MaxValue : (int)positiveInfinity;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanCasesByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, 0, positiveInfinityAsInt, true, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(LoanCase => LoanCase.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());

            var loanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(id, GetServiceHeader());

            ViewBag.LoanGuarantors = loanGuarantors;

            return View(loanCaseDTO);
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
            ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(string.Empty);
            ViewBag.LoanGuarantorDTOs = null;

            return View();
        }


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var cusomerEditDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());

            if (cusomerEditDTO.Status != (int)LoanCaseStatus.Registered)
            {
                TempData["LoanCaseStatusInvalid"] = "Editing Loan Cases is only available for registered loans !";
                return View("index");
            }

            LoanCaseDTO loanCaseDTO = new LoanCaseDTO();

            if (cusomerEditDTO != null)
            {
                loanCaseDTO.CaseNumber = cusomerEditDTO.CaseNumber;
                loanCaseDTO.CustomerId = cusomerEditDTO.CustomerId;
                loanCaseDTO.CustomerIndividualFirstName = cusomerEditDTO.CustomerIndividualSalutationDescription + " " + cusomerEditDTO.CustomerIndividualFirstName + " " + cusomerEditDTO.CustomerIndividualLastName;
                loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = cusomerEditDTO.CustomerStationZoneDivisionEmployerDescription;
                loanCaseDTO.CustomerStation = cusomerEditDTO.CustomerStation;
                loanCaseDTO.CustomerReference1 = cusomerEditDTO.CustomerReference1;
                loanCaseDTO.CustomerReference2 = cusomerEditDTO.CustomerReference2;
                loanCaseDTO.CustomerReference3 = cusomerEditDTO.CustomerReference3;
                loanCaseDTO.BranchId = cusomerEditDTO.BranchId;
                loanCaseDTO.BranchDescription = cusomerEditDTO.BranchDescription;
                loanCaseDTO.LoanProductId = cusomerEditDTO.LoanProductId;
                loanCaseDTO.LoanProductDescription = cusomerEditDTO.LoanProductDescription;
                loanCaseDTO.InterestCalculationModeDescription = cusomerEditDTO.LoanInterestCalculationModeDescription;
                loanCaseDTO.LoanInterestCalculationMode = cusomerEditDTO.LoanInterestCalculationMode;
                loanCaseDTO.LoanInterestAnnualPercentageRate = cusomerEditDTO.LoanInterestAnnualPercentageRate;
                loanCaseDTO.LoanProductSectionDescription = cusomerEditDTO.LoanRegistrationLoanProductSectionDescription;
                loanCaseDTO.LoanRegistrationTermInMonths = cusomerEditDTO.LoanRegistrationTermInMonths;
                loanCaseDTO.LoanProductInvestmentsBalance = cusomerEditDTO.LoanProductInvestmentsBalance;
                loanCaseDTO.LoanProductLoanBalance = cusomerEditDTO.LoanProductLoanBalance;
                loanCaseDTO.LoanRegistrationMaximumAmount = cusomerEditDTO.LoanRegistrationMaximumAmount;
                loanCaseDTO.MaximumAmountPercentage = cusomerEditDTO.MaximumAmountPercentage;
                loanCaseDTO.LoanProductLatestIncome = cusomerEditDTO.LoanProductLatestIncome;
                loanCaseDTO.SavingsProductId = cusomerEditDTO.SavingsProductId;
                loanCaseDTO.SavingsProductDescription = cusomerEditDTO.SavingsProductDescription;
                loanCaseDTO.RegistrationRemarkId = cusomerEditDTO.RegistrationRemarkId;
                loanCaseDTO.Remarks = cusomerEditDTO.Remarks;
                loanCaseDTO.LoanPurposeId = cusomerEditDTO.LoanPurposeId;
                loanCaseDTO.LoanPurposeDescription = cusomerEditDTO.LoanPurposeDescription;
                loanCaseDTO.AmountApplied = cusomerEditDTO.AmountApplied;
                loanCaseDTO.Reference = cusomerEditDTO.Reference;
                loanCaseDTO.Status = cusomerEditDTO.Status;

                Session["editViewLoanee"] = loanCaseDTO;
                Session["Status"] = loanCaseDTO.Status;
                Session["CaseNumber"] = loanCaseDTO.CaseNumber;
            }

            return View(loanCaseDTO);
        }


        // Create Lookups
        public async Task<ActionResult> LoaneeLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            // Check Sessions for data and keep in controls
            if (Session["LoanPurposeId"] != null)
            {
                loanCaseDTO.LoanPurposeId = (Guid)Session["LoanPurposeId"];
                loanCaseDTO.LoanPurposeDescription = Session["LoanPurposeDescription"].ToString();
            }

            if (Session["LoanProductId"] != null)
            {
                loanCaseDTO.LoanProductId = (Guid)Session["LoanProductId"];
                loanCaseDTO.LoanProductDescription = Session["LoanProductDescription"].ToString();
                loanCaseDTO.InterestCalculationModeDescription = Session["InterestCalculationModeDescription"].ToString();
                loanCaseDTO.LoanInterestAnnualPercentageRate = Convert.ToDouble(Session["LoanInterestAnnualPercentageRate"].ToString());
                loanCaseDTO.LoanProductSectionDescription = Session["LoanProductSectionDescription"].ToString();
                loanCaseDTO.LoanRegistrationTermInMonths = Convert.ToInt32(Session["LoanRegistrationTermInMonths"].ToString());
                loanCaseDTO.LoanRegistrationMaximumAmount = Convert.ToDecimal(Session["LoanRegistrationMaximumAmount"].ToString());

                loanCaseDTO.LoanInterestRecoveryMode = Convert.ToInt32(Session["LoanInterestRecoveryMode"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationMode"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationMode"].ToString());
            }

            if (Session["SavingsProductId"] != null)
            {
                loanCaseDTO.SavingsProductId = (Guid)Session["SavingsProductId"];
                loanCaseDTO.SavingsProductDescription = Session["SavingsProductDescription"].ToString();
            }

            if (Session["RegistrationRemarkId"] != null)
            {
                loanCaseDTO.RegistrationRemarkId = (Guid)Session["RegistrationRemarkId"];
                loanCaseDTO.Remarks = Session["Remarks"].ToString();
            }


            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            if (customer != null)
            {
                loanCaseDTO.CustomerId = customer.Id;
                loanCaseDTO.CustomerIndividualFirstName = customer.IndividualSalutationDescription + " " + customer.IndividualFirstName + " " + customer.IndividualLastName;
                loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                loanCaseDTO.CustomerStation = customer.StationDescription;
                loanCaseDTO.CustomerReference1 = customer.Reference1;
                loanCaseDTO.CustomerReference2 = customer.Reference2;
                loanCaseDTO.CustomerReference3 = customer.Reference3;

                Session["CustomerId"] = loanCaseDTO.CustomerId;
                Session["CustomerIndividualFirstName"] = loanCaseDTO.CustomerIndividualFirstName;
                Session["CustomerStationZoneDivisionEmployerDescription"] = loanCaseDTO.CustomerStationZoneDivisionEmployerDescription;
                Session["CustomerStation"] = loanCaseDTO.CustomerStation;
                Session["CustomerReference1"] = loanCaseDTO.CustomerReference1;
                Session["CustomerReference2"] = loanCaseDTO.CustomerReference2;
                Session["CustomerReference3"] = loanCaseDTO.CustomerReference3;


                //// Standing Orders
                ObservableCollection<Guid> customerAccountId = new ObservableCollection<Guid>(); 
                var customerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(parseId, true, true, true,true, GetServiceHeader());

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


                //// Income History
                //// Payouts
                var payouts = await _channelService.FindLoanDisbursementBatchEntriesByCustomerIdAsync((int)BatchStatus.Posted, parseId, GetServiceHeader());
                if (payouts != null)
                {
                    ViewBag.Payouts = payouts;
                }
               

                ////Salary
                // No method fetching by customerId



                //// Loan Applications
                var loanApplications = await _channelService.FindLoanCasesByCustomerIdInProcessAsync(parseId, GetServiceHeader());
                if (loanApplications != null)
                {
                    ViewBag.LoanApplications = loanApplications;
                }

                //// Collaterals...
                // No method fetching by customerId

            }


            return View("Create", loanCaseDTO);
        }


        public async Task<ActionResult> LoanPurposeLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }


            // Check Sessions for data and keep in controls
            if (Session["CustomerId"] != null)
            {
                loanCaseDTO.CustomerId = (Guid)Session["CustomerId"];
                loanCaseDTO.CustomerIndividualFirstName = Session["CustomerIndividualFirstName"].ToString();
                loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = Session["CustomerStationZoneDivisionEmployerDescription"].ToString();
                loanCaseDTO.CustomerStation = Session["CustomerStation"].ToString();
                loanCaseDTO.CustomerReference1 = Session["CustomerReference1"].ToString();
                loanCaseDTO.CustomerReference2 = Session["CustomerReference2"].ToString();
                loanCaseDTO.CustomerReference3 = Session["CustomerReference3"].ToString();
            }

            if (Session["LoanProductId"] != null)
            {
                loanCaseDTO.LoanProductId = (Guid)Session["LoanProductId"];
                loanCaseDTO.LoanProductDescription = Session["LoanProductDescription"].ToString();
                loanCaseDTO.InterestCalculationModeDescription = Session["InterestCalculationModeDescription"].ToString();
                loanCaseDTO.LoanInterestAnnualPercentageRate = Convert.ToDouble(Session["LoanInterestAnnualPercentageRate"].ToString());
                loanCaseDTO.LoanProductSectionDescription = Session["LoanProductSectionDescription"].ToString();
                loanCaseDTO.LoanRegistrationTermInMonths = Convert.ToInt32(Session["LoanRegistrationTermInMonths"].ToString());
                loanCaseDTO.LoanRegistrationMaximumAmount = Convert.ToDecimal(Session["LoanRegistrationMaximumAmount"].ToString());

                loanCaseDTO.LoanInterestRecoveryMode = Convert.ToInt32(Session["LoanInterestRecoveryMode"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationMode"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationMode"].ToString());
            }

            if (Session["SavingsProductId"] != null)
            {
                loanCaseDTO.SavingsProductId = (Guid)Session["SavingsProductId"];
                loanCaseDTO.SavingsProductDescription = Session["SavingsProductDescription"].ToString();
            }

            if (Session["RegistrationRemarkId"] != null)
            {
                loanCaseDTO.RegistrationRemarkId = (Guid)Session["RegistrationRemarkId"];
                loanCaseDTO.Remarks = Session["Remarks"].ToString();
            }

            //if (Session["BranchId"] != null)
            //{
            //    loanCaseDTO.BranchId = (Guid)Session["BranchId"]; ;
            //    loanCaseDTO.BranchDescription = Session["BranchDescription"].ToString();
            //}



            var loanPurpose = await _channelService.FindLoanPurposeAsync(parseId, GetServiceHeader());
            if (loanPurpose != null)
            {
                loanCaseDTO.LoanPurposeId = loanPurpose.Id;
                loanCaseDTO.LoanPurposeDescription = loanPurpose.Description;

                Session["LoanPurposeId"] = loanCaseDTO.LoanPurposeId;
                Session["LoanPurposeDescription"] = loanCaseDTO.LoanPurposeDescription;
            }

            return View("create", loanCaseDTO);
        }


        public async Task<ActionResult> LoanProductLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }


            // Check Sessions for data and keep in controls
            if (Session["CustomerId"] != null)
            {
                loanCaseDTO.CustomerId = (Guid)Session["CustomerId"];
                loanCaseDTO.CustomerIndividualFirstName = Session["CustomerIndividualFirstName"].ToString();
                loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = Session["CustomerStationZoneDivisionEmployerDescription"].ToString();
                loanCaseDTO.CustomerStation = Session["CustomerStation"].ToString();
                loanCaseDTO.CustomerReference1 = Session["CustomerReference1"].ToString();
                loanCaseDTO.CustomerReference2 = Session["CustomerReference2"].ToString();
                loanCaseDTO.CustomerReference3 = Session["CustomerReference3"].ToString();
            }

            if (Session["LoanPurposeId"] != null)
            {
                loanCaseDTO.LoanPurposeId = (Guid)Session["LoanPurposeId"];
                loanCaseDTO.LoanPurposeDescription = Session["LoanPurposeDescription"].ToString();
            }

            if (Session["SavingsProductId"] != null)
            {
                loanCaseDTO.SavingsProductId = (Guid)Session["SavingsProductId"];
                loanCaseDTO.SavingsProductDescription = Session["SavingsProductDescription"].ToString();
            }

            if (Session["RegistrationRemarkId"] != null)
            {
                loanCaseDTO.RegistrationRemarkId = (Guid)Session["RegistrationRemarkId"];
                loanCaseDTO.Remarks = Session["Remarks"].ToString();
            }

            //if (Session["BranchId"] != null)
            //{
            //    loanCaseDTO.BranchId = (Guid)Session["BranchId"]; ;
            //    loanCaseDTO.BranchDescription = Session["BranchDescription"].ToString();
            //}



            var loanProduct = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());
            if (loanProduct != null)
            {
                loanCaseDTO.LoanProductId = loanProduct.Id;
                loanCaseDTO.LoanProductDescription = loanProduct.Description;
                loanCaseDTO.InterestCalculationModeDescription = loanProduct.LoanInterestCalculationModeDescription;
                loanCaseDTO.LoanInterestAnnualPercentageRate = loanProduct.LoanInterestAnnualPercentageRate;
                loanCaseDTO.LoanProductSectionDescription = loanProduct.LoanRegistrationLoanProductSectionDescription;
                loanCaseDTO.LoanRegistrationTermInMonths = loanProduct.LoanRegistrationTermInMonths;
                loanCaseDTO.LoanRegistrationMaximumAmount = loanProduct.LoanRegistrationMaximumAmount;
                loanCaseDTO.LoanInterestChargeMode = loanProduct.LoanInterestChargeMode;
                loanCaseDTO.LoanInterestRecoveryMode = loanProduct.LoanInterestRecoveryMode;
                loanCaseDTO.LoanInterestCalculationMode = loanProduct.LoanInterestCalculationMode;

                loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear = loanProduct.LoanRegistrationPaymentFrequencyPerYear;
                loanCaseDTO.LoanRegistrationMinimumAmount = loanProduct.LoanRegistrationMinimumAmount;
                loanCaseDTO.LoanRegistrationMinimumInterestAmount = loanProduct.LoanRegistrationMinimumInterestAmount;
                loanCaseDTO.LoanRegistrationMinimumGuarantors = loanProduct.LoanRegistrationMinimumGuarantors;
                loanCaseDTO.LoanRegistrationMinimumMembershipPeriod = loanProduct.LoanRegistrationMinimumMembershipPeriod;
                loanCaseDTO.LoanRegistrationMaximumGuarantees = loanProduct.LoanRegistrationMaximumGuarantees;
                loanCaseDTO.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement = loanProduct.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement;
                loanCaseDTO.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage = loanProduct.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage;
                loanCaseDTO.LoanRegistrationLoanProductSection = loanProduct.LoanRegistrationLoanProductSection;
                loanCaseDTO.LoanRegistrationLoanProductCategory = loanProduct.LoanRegistrationLoanProductCategory;
                loanCaseDTO.LoanRegistrationConsecutiveIncome = loanProduct.LoanRegistrationConsecutiveIncome;
                loanCaseDTO.LoanRegistrationInvestmentsMultiplier = loanProduct.LoanRegistrationInvestmentsMultiplier;
                loanCaseDTO.LoanRegistrationRejectIfMemberHasBalance = loanProduct.LoanRegistrationRejectIfMemberHasBalance;
                loanCaseDTO.LoanRegistrationSecurityRequired = loanProduct.LoanRegistrationSecurityRequired;
                loanCaseDTO.LoanRegistrationAllowSelfGuarantee = loanProduct.LoanRegistrationAllowSelfGuarantee;
                loanCaseDTO.LoanRegistrationGracePeriod = loanProduct.LoanRegistrationGracePeriod;
                loanCaseDTO.LoanRegistrationPaymentDueDate = loanProduct.LoanRegistrationPaymentDueDate;
                loanCaseDTO.LoanRegistrationPayoutRecoveryMode = loanProduct.LoanRegistrationPayoutRecoveryMode;
                loanCaseDTO.LoanRegistrationPayoutRecoveryPercentage = loanProduct.LoanRegistrationPayoutRecoveryPercentage;
                loanCaseDTO.LoanRegistrationAggregateCheckOffRecoveryMode = loanProduct.LoanRegistrationAggregateCheckOffRecoveryMode;
                loanCaseDTO.LoanRegistrationChargeClearanceFee = loanProduct.LoanRegistrationChargeClearanceFee;
                loanCaseDTO.LoanRegistrationMicrocredit = loanProduct.LoanRegistrationMicrocredit;
                loanCaseDTO.LoanRegistrationStandingOrderTrigger = loanProduct.LoanRegistrationStandingOrderTrigger;
                loanCaseDTO.LoanRegistrationTrackArrears = loanProduct.LoanRegistrationTrackArrears;
                loanCaseDTO.LoanRegistrationChargeArrearsFee = loanProduct.LoanRegistrationChargeArrearsFee;
                loanCaseDTO.LoanRegistrationEnforceSystemAppraisalRecommendation = loanProduct.LoanRegistrationEnforceSystemAppraisalRecommendation;
                loanCaseDTO.LoanRegistrationBypassAudit = loanProduct.LoanRegistrationBypassAudit;
                loanCaseDTO.LoanRegistrationGuarantorSecurityMode = loanProduct.LoanRegistrationGuarantorSecurityMode;
                loanCaseDTO.LoanRegistrationRoundingType = loanProduct.LoanRegistrationRoundingType;
                loanCaseDTO.LoanRegistrationDisburseMicroLoanLessDeductions = loanProduct.LoanRegistrationDisburseMicroLoanLessDeductions;
                loanCaseDTO.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal = loanProduct.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal;
                loanCaseDTO.LoanRegistrationThrottleScheduledArrearsRecovery = loanProduct.LoanRegistrationThrottleScheduledArrearsRecovery;
                loanCaseDTO.LoanRegistrationCreateStandingOrderOnLoanAudit = loanProduct.LoanRegistrationCreateStandingOrderOnLoanAudit;
                loanCaseDTO.TakeHomeType = loanProduct.TakeHomeType;
                loanCaseDTO.TakeHomePercentage = loanProduct.TakeHomePercentage;
                loanCaseDTO.TakeHomeFixedAmount = loanProduct.TakeHomeFixedAmount;

                // Calculate Loan Balance
                // Calculate Investments Balace
                // Find Maximum Percentage

                Session["LoanProductId"] = loanCaseDTO.LoanProductId;
                Session["LoanProductDescription"] = loanCaseDTO.LoanProductDescription;
                Session["InterestCalculationModeDescription"] = loanCaseDTO.InterestCalculationModeDescription;
                Session["LoanInterestAnnualPercentageRate"] = loanCaseDTO.LoanInterestAnnualPercentageRate;
                Session["LoanProductSectionDescription"] = loanCaseDTO.LoanProductSectionDescription;
                Session["LoanRegistrationTermInMonths"] = loanCaseDTO.LoanRegistrationTermInMonths;
                Session["LoanRegistrationMaximumAmount"] = loanCaseDTO.LoanRegistrationMaximumAmount;

                Session["LoanInterestRecoveryMode"] = loanCaseDTO.LoanInterestRecoveryMode;
                Session["LoanInterestCalculationMode"] = loanCaseDTO.LoanInterestCalculationMode;


                Session["LoanRegistrationPaymentFrequencyPerYear"] = loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear;
                Session["LoanRegistrationMinimumAmount"] = loanCaseDTO.LoanRegistrationMinimumAmount;
                Session["LoanRegistrationMinimumInterestAmount"] = loanCaseDTO.LoanRegistrationMinimumInterestAmount;
                Session["LoanRegistrationMinimumGuarantors"] = loanCaseDTO.LoanRegistrationMinimumGuarantors;
                Session["LoanRegistrationMinimumMembershipPeriod"] = loanCaseDTO.LoanRegistrationMinimumMembershipPeriod;
                Session["LoanRegistrationMaximumGuarantees"] = loanCaseDTO.LoanRegistrationMaximumGuarantees;
                Session["LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement"] = loanCaseDTO.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement;
                Session["LoanRegistrationMaximumSelfGuaranteeEligiblePercentage"] = loanCaseDTO.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage;
                Session["LoanRegistrationLoanProductSection"] = loanCaseDTO.LoanRegistrationLoanProductSection;
                Session["LoanRegistrationLoanProductCategory"] = loanCaseDTO.LoanRegistrationLoanProductCategory;
                Session["LoanRegistrationConsecutiveIncome"] = loanCaseDTO.LoanRegistrationConsecutiveIncome;
                Session["LoanRegistrationInvestmentsMultiplier"] = loanCaseDTO.LoanRegistrationInvestmentsMultiplier;
                Session["LoanRegistrationRejectIfMemberHasBalance"] = loanCaseDTO.LoanRegistrationRejectIfMemberHasBalance;
                Session["LoanRegistrationSecurityRequired"] = loanCaseDTO.LoanRegistrationSecurityRequired;
                Session["LoanRegistrationAllowSelfGuarantee"] = loanCaseDTO.LoanRegistrationAllowSelfGuarantee;
                Session["LoanRegistrationGracePeriod"] = loanCaseDTO.LoanRegistrationGracePeriod;
                Session["LoanRegistrationPaymentDueDate"] = loanCaseDTO.LoanRegistrationPaymentDueDate;
                Session["LoanRegistrationPayoutRecoveryMode"] = loanCaseDTO.LoanRegistrationPayoutRecoveryMode;
                Session["LoanRegistrationPayoutRecoveryPercentage"] = loanCaseDTO.LoanRegistrationPayoutRecoveryPercentage;
                Session["LoanRegistrationAggregateCheckOffRecoveryMode"] = loanCaseDTO.LoanRegistrationAggregateCheckOffRecoveryMode;
                Session["LoanRegistrationChargeClearanceFee"] = loanCaseDTO.LoanRegistrationChargeClearanceFee;
                Session["LoanRegistrationMicrocredit"] = loanCaseDTO.LoanRegistrationMicrocredit;
                Session["LoanRegistrationStandingOrderTrigger"] = loanCaseDTO.LoanRegistrationStandingOrderTrigger;
                Session["LoanRegistrationTrackArrears"] = loanCaseDTO.LoanRegistrationTrackArrears;
                Session["LoanRegistrationChargeArrearsFee"] = loanCaseDTO.LoanRegistrationChargeArrearsFee;
                Session["LoanRegistrationEnforceSystemAppraisalRecommendation"] = loanCaseDTO.LoanRegistrationEnforceSystemAppraisalRecommendation;
                Session["LoanRegistrationBypassAudit"] = loanCaseDTO.LoanRegistrationBypassAudit;
                Session["LoanRegistrationGuarantorSecurityMode"] = loanCaseDTO.LoanRegistrationGuarantorSecurityMode;
                Session["LoanRegistrationRoundingType"] = loanCaseDTO.LoanRegistrationRoundingType;
                Session["LoanRegistrationDisburseMicroLoanLessDeductions"] = loanCaseDTO.LoanRegistrationDisburseMicroLoanLessDeductions;
                Session["LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal"] = loanCaseDTO.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal;
                Session["LoanRegistrationThrottleScheduledArrearsRecovery"] = loanCaseDTO.LoanRegistrationThrottleScheduledArrearsRecovery;
                Session["LoanRegistrationCreateStandingOrderOnLoanAudit"] = loanCaseDTO.LoanRegistrationCreateStandingOrderOnLoanAudit;
                Session["LoanRegistrationCreateStandingOrderOnLoanAudit"] = loanCaseDTO.LoanRegistrationCreateStandingOrderOnLoanAudit;

            }

            return View("create", loanCaseDTO);
        }


        public async Task<ActionResult> SavingsProductLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }


            // Check Sessions for data and keep in controls
            if (Session["CustomerId"] != null)
            {
                loanCaseDTO.CustomerId = (Guid)Session["CustomerId"];
                loanCaseDTO.CustomerIndividualFirstName = Session["CustomerIndividualFirstName"].ToString();
                loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = Session["CustomerStationZoneDivisionEmployerDescription"].ToString();
                loanCaseDTO.CustomerStation = Session["CustomerStation"].ToString();
                loanCaseDTO.CustomerReference1 = Session["CustomerReference1"].ToString();
                loanCaseDTO.CustomerReference2 = Session["CustomerReference2"].ToString();
                loanCaseDTO.CustomerReference3 = Session["CustomerReference3"].ToString();
            }

            if (Session["LoanPurposeId"] != null)
            {
                loanCaseDTO.LoanPurposeId = (Guid)Session["LoanPurposeId"];
                loanCaseDTO.LoanPurposeDescription = Session["LoanPurposeDescription"].ToString();
            }

            if (Session["LoanProductId"] != null)
            {
                loanCaseDTO.LoanProductId = (Guid)Session["LoanProductId"];
                loanCaseDTO.LoanProductDescription = Session["LoanProductDescription"].ToString();
                loanCaseDTO.InterestCalculationModeDescription = Session["InterestCalculationModeDescription"].ToString();
                loanCaseDTO.LoanInterestAnnualPercentageRate = Convert.ToDouble(Session["LoanInterestAnnualPercentageRate"].ToString());
                loanCaseDTO.LoanProductSectionDescription = Session["LoanProductSectionDescription"].ToString();
                loanCaseDTO.LoanRegistrationTermInMonths = Convert.ToInt32(Session["LoanRegistrationTermInMonths"].ToString());
                loanCaseDTO.LoanRegistrationMaximumAmount = Convert.ToDecimal(Session["LoanRegistrationMaximumAmount"].ToString());

                loanCaseDTO.LoanInterestRecoveryMode = Convert.ToInt32(Session["LoanInterestRecoveryMode"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationMode"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationMode"].ToString());
            }

            if (Session["RegistrationRemarkId"] != null)
            {
                loanCaseDTO.RegistrationRemarkId = (Guid)Session["RegistrationRemarkId"];
                loanCaseDTO.Remarks = Session["Remarks"].ToString();
            }

            //if (Session["BranchId"] != null)
            //{
            //    loanCaseDTO.BranchId = (Guid)Session["BranchId"]; ;
            //    loanCaseDTO.BranchDescription = Session["BranchDescription"].ToString();
            //}



            var savingsProduct = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());
            if (savingsProduct != null)
            {
                loanCaseDTO.SavingsProductId = savingsProduct.Id;
                loanCaseDTO.SavingsProductDescription = savingsProduct.Description;

                Session["SavingsProductId"] = loanCaseDTO.SavingsProductId;
                Session["SavingsProductDescription"] = loanCaseDTO.SavingsProductDescription;
            }

            return View("create", loanCaseDTO);
        }


        public async Task<ActionResult> LoaningRemarksLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }


            // Check Sessions for data and keep in controls
            if (Session["CustomerId"] != null)
            {
                loanCaseDTO.CustomerId = (Guid)Session["CustomerId"];
                loanCaseDTO.CustomerIndividualFirstName = Session["CustomerIndividualFirstName"].ToString();
                loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = Session["CustomerStationZoneDivisionEmployerDescription"].ToString();
                loanCaseDTO.CustomerStation = Session["CustomerStation"].ToString();
                loanCaseDTO.CustomerReference1 = Session["CustomerReference1"].ToString();
                loanCaseDTO.CustomerReference2 = Session["CustomerReference2"].ToString();
                loanCaseDTO.CustomerReference3 = Session["CustomerReference3"].ToString();
            }

            if (Session["LoanPurposeId"] != null)
            {
                loanCaseDTO.LoanPurposeId = (Guid)Session["LoanPurposeId"];
                loanCaseDTO.LoanPurposeDescription = Session["LoanPurposeDescription"].ToString();
            }

            if (Session["LoanProductId"] != null)
            {
                loanCaseDTO.LoanProductId = (Guid)Session["LoanProductId"];
                loanCaseDTO.LoanProductDescription = Session["LoanProductDescription"].ToString();
                loanCaseDTO.InterestCalculationModeDescription = Session["InterestCalculationModeDescription"].ToString();
                loanCaseDTO.LoanInterestAnnualPercentageRate = Convert.ToDouble(Session["LoanInterestAnnualPercentageRate"].ToString());
                loanCaseDTO.LoanProductSectionDescription = Session["LoanProductSectionDescription"].ToString();
                loanCaseDTO.LoanRegistrationTermInMonths = Convert.ToInt32(Session["LoanRegistrationTermInMonths"].ToString());
                loanCaseDTO.LoanRegistrationMaximumAmount = Convert.ToDecimal(Session["LoanRegistrationMaximumAmount"].ToString());

                loanCaseDTO.LoanInterestRecoveryMode = Convert.ToInt32(Session["LoanInterestRecoveryMode"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationMode"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationMode"].ToString());
            }

            if (Session["SavingsProductId"] != null)
            {
                loanCaseDTO.SavingsProductId = (Guid)Session["SavingsProductId"];
                loanCaseDTO.SavingsProductDescription = Session["SavingsProductDescription"].ToString();
            }

            //if (Session["BranchId"] != null)
            //{
            //    loanCaseDTO.BranchId = (Guid)Session["BranchId"]; ;
            //    loanCaseDTO.BranchDescription = Session["BranchDescription"].ToString();
            //}


            var loaningRemarks = await _channelService.FindLoaningRemarkAsync(parseId, GetServiceHeader());
            if (loaningRemarks != null)
            {
                loanCaseDTO.RegistrationRemarkId = loaningRemarks.Id;
                loanCaseDTO.Remarks = loaningRemarks.Description;

                Session["RegistrationRemarkId"] = loanCaseDTO.RegistrationRemarkId;
                Session["Remarks"] = loanCaseDTO.Remarks;
            }


            return View("create", loanCaseDTO);
        }



        // Edit Lookups
        public async Task<ActionResult> LoaneeLookupEdit(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("edit");
            }

            loanCaseDTO = Session["editViewLoanee"] as LoanCaseDTO;

            // Check Sessions for data and keep in controls
            if (Session["LoanPurposeIdE"] != null)
            {
                loanCaseDTO.LoanPurposeId = (Guid)Session["LoanPurposeIdE"];
                loanCaseDTO.LoanPurposeDescription = Session["LoanPurposeDescriptionE"].ToString();
            }

            if (Session["LoanProductIdE"] != null)
            {
                loanCaseDTO.LoanProductId = (Guid)Session["LoanProductIdE"];
                loanCaseDTO.LoanProductDescription = Session["LoanProductDescriptionE"].ToString();
                loanCaseDTO.InterestCalculationModeDescription = Session["InterestCalculationModeDescriptionE"].ToString();
                loanCaseDTO.LoanInterestAnnualPercentageRate = Convert.ToDouble(Session["LoanInterestAnnualPercentageRateE"].ToString());
                loanCaseDTO.LoanProductSectionDescription = Session["LoanProductSectionDescriptionE"].ToString();
                loanCaseDTO.LoanRegistrationTermInMonths = Convert.ToInt32(Session["LoanRegistrationTermInMonthsE"].ToString());
                loanCaseDTO.LoanRegistrationMaximumAmount = Convert.ToDecimal(Session["LoanRegistrationMaximumAmountE"].ToString());

                loanCaseDTO.LoanInterestRecoveryMode = Convert.ToInt32(Session["LoanInterestRecoveryModeE"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationModeE"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationModeE"].ToString());
            }

            if (Session["SavingsProductIdE"] != null)
            {
                loanCaseDTO.SavingsProductId = (Guid)Session["SavingsProductIdE"];
                loanCaseDTO.SavingsProductDescription = Session["SavingsProductDescriptionE"].ToString();
            }

            if (Session["RegistrationRemarkIdE"] != null)
            {
                loanCaseDTO.RegistrationRemarkId = (Guid)Session["RegistrationRemarkIdE"];
                loanCaseDTO.Remarks = Session["RemarksE"].ToString();
            }


            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            if (customer != null)
            {
                loanCaseDTO.CustomerId = customer.Id;
                loanCaseDTO.CustomerIndividualFirstName = customer.IndividualSalutationDescription + " " + customer.IndividualFirstName + " " + customer.IndividualLastName;
                loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                loanCaseDTO.CustomerStation = customer.StationDescription;
                loanCaseDTO.CustomerReference1 = customer.Reference1;
                loanCaseDTO.CustomerReference2 = customer.Reference2;
                loanCaseDTO.CustomerReference3 = customer.Reference3;

                Session["CustomerIdE"] = loanCaseDTO.CustomerId;
                Session["CustomerIndividualFirstNameE"] = loanCaseDTO.CustomerIndividualFirstName;
                Session["CustomerStationZoneDivisionEmployerDescriptionE"] = loanCaseDTO.CustomerStationZoneDivisionEmployerDescription;
                Session["CustomerStationE"] = loanCaseDTO.CustomerStation;
                Session["CustomerReference1E"] = loanCaseDTO.CustomerReference1;
                Session["CustomerReference2E"] = loanCaseDTO.CustomerReference2;
                Session["CustomerReference3E"] = loanCaseDTO.CustomerReference3;

                var customerAccountList = await _channelService.FindCustomerAccountsByCustomerIdAsync(parseId, false, false, false,
                    false, GetServiceHeader());

                ObservableCollection<StandingOrderDTO> items = new ObservableCollection<StandingOrderDTO>();

                foreach (var item in customerAccountList)
                {
                    var customerAccounts = item.Id;

                    var standingOrders = await _channelService.FindStandingOrdersByBeneficiaryCustomerAccountIdAsync(customerAccounts, false, GetServiceHeader());
                    if (standingOrders != null)
                    {
                        ViewBag.StandingOrders = standingOrders;
                    }
                }

                var loanapplications = await _channelService.FindLoanCasesByCustomerIdInProcessAsync(parseId, GetServiceHeader());
                if (loanapplications != null)
                    ViewBag.LoanApplications = loanapplications;
            }


            return View("edit", loanCaseDTO);
        }


        public async Task<ActionResult> LoanPurposeLookupEdit(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("edit");
            }

            loanCaseDTO = Session["editViewLoanee"] as LoanCaseDTO;

            // Check Sessions for data and keep in controls
            if (Session["LoanProductIdE"] != null)
            {
                loanCaseDTO.LoanProductId = (Guid)Session["LoanProductIdE"];
                loanCaseDTO.LoanProductDescription = Session["LoanProductDescriptionE"].ToString();
                loanCaseDTO.InterestCalculationModeDescription = Session["InterestCalculationModeDescriptionE"].ToString();
                loanCaseDTO.LoanInterestAnnualPercentageRate = Convert.ToDouble(Session["LoanInterestAnnualPercentageRateE"].ToString());
                loanCaseDTO.LoanProductSectionDescription = Session["LoanProductSectionDescriptionE"].ToString();
                loanCaseDTO.LoanRegistrationTermInMonths = Convert.ToInt32(Session["LoanRegistrationTermInMonthsE"].ToString());
                loanCaseDTO.LoanRegistrationMaximumAmount = Convert.ToDecimal(Session["LoanRegistrationMaximumAmountE"].ToString());

                loanCaseDTO.LoanInterestRecoveryMode = Convert.ToInt32(Session["LoanInterestRecoveryModeE"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationModeE"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationModeE"].ToString());
            }

            if (Session["SavingsProductIdE"] != null)
            {
                loanCaseDTO.SavingsProductId = (Guid)Session["SavingsProductIdE"];
                loanCaseDTO.SavingsProductDescription = Session["SavingsProductDescriptionE"].ToString();
            }

            if (Session["RegistrationRemarkIdE"] != null)
            {
                loanCaseDTO.RegistrationRemarkId = (Guid)Session["RegistrationRemarkIdE"];
                loanCaseDTO.Remarks = Session["RemarksE"].ToString();
            }

            var loanPurpose = await _channelService.FindLoanPurposeAsync(parseId, GetServiceHeader());
            if (loanPurpose != null)
            {
                loanCaseDTO.LoanPurposeId = loanPurpose.Id;
                loanCaseDTO.LoanPurposeDescription = loanPurpose.Description;

                Session["LoanPurposeIdE"] = loanCaseDTO.LoanPurposeId;
                Session["LoanPurposeDescriptionE"] = loanCaseDTO.LoanPurposeDescription;
            }

            return View("edit", loanCaseDTO);
        }


        public async Task<ActionResult> LoanProductLookupEdit(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("edit");
            }

            loanCaseDTO = Session["editViewLoanee"] as LoanCaseDTO;

            // Check Sessions for data and keep in controls
            if (Session["LoanPurposeIdE"] != null)
            {
                loanCaseDTO.LoanPurposeId = (Guid)Session["LoanPurposeIdE"];
                loanCaseDTO.LoanPurposeDescription = Session["LoanPurposeDescriptionE"].ToString();
            }

            if (Session["SavingsProductIdE"] != null)
            {
                loanCaseDTO.SavingsProductId = (Guid)Session["SavingsProductIdE"];
                loanCaseDTO.SavingsProductDescription = Session["SavingsProductDescriptionE"].ToString();
            }

            if (Session["RegistrationRemarkIdE"] != null)
            {
                loanCaseDTO.RegistrationRemarkId = (Guid)Session["RegistrationRemarkIdE"];
                loanCaseDTO.Remarks = Session["RemarksE"].ToString();
            }

            var loanProduct = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());
            if (loanProduct != null)
            {
                loanCaseDTO.LoanProductId = loanProduct.Id;
                loanCaseDTO.LoanProductDescription = loanProduct.Description;
                loanCaseDTO.InterestCalculationModeDescription = loanProduct.LoanInterestCalculationModeDescription;
                loanCaseDTO.LoanInterestAnnualPercentageRate = loanProduct.LoanInterestAnnualPercentageRate;
                loanCaseDTO.LoanProductSectionDescription = loanProduct.LoanRegistrationLoanProductSectionDescription;
                loanCaseDTO.LoanRegistrationTermInMonths = loanProduct.LoanRegistrationTermInMonths;
                loanCaseDTO.LoanRegistrationMaximumAmount = loanProduct.LoanRegistrationMaximumAmount;
                loanCaseDTO.LoanInterestChargeMode = loanProduct.LoanInterestChargeMode;
                loanCaseDTO.LoanInterestRecoveryMode = loanProduct.LoanInterestRecoveryMode;
                loanCaseDTO.LoanInterestCalculationMode = loanProduct.LoanInterestCalculationMode;

                loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear = loanProduct.LoanRegistrationPaymentFrequencyPerYear;
                loanCaseDTO.LoanRegistrationMinimumAmount = loanProduct.LoanRegistrationMinimumAmount;
                loanCaseDTO.LoanRegistrationMinimumInterestAmount = loanProduct.LoanRegistrationMinimumInterestAmount;
                loanCaseDTO.LoanRegistrationMinimumGuarantors = loanProduct.LoanRegistrationMinimumGuarantors;
                loanCaseDTO.LoanRegistrationMinimumMembershipPeriod = loanProduct.LoanRegistrationMinimumMembershipPeriod;
                loanCaseDTO.LoanRegistrationMaximumGuarantees = loanProduct.LoanRegistrationMaximumGuarantees;
                loanCaseDTO.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement = loanProduct.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement;
                loanCaseDTO.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage = loanProduct.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage;
                loanCaseDTO.LoanRegistrationLoanProductSection = loanProduct.LoanRegistrationLoanProductSection;
                loanCaseDTO.LoanRegistrationLoanProductCategory = loanProduct.LoanRegistrationLoanProductCategory;
                loanCaseDTO.LoanRegistrationConsecutiveIncome = loanProduct.LoanRegistrationConsecutiveIncome;
                loanCaseDTO.LoanRegistrationInvestmentsMultiplier = loanProduct.LoanRegistrationInvestmentsMultiplier;
                loanCaseDTO.LoanRegistrationRejectIfMemberHasBalance = loanProduct.LoanRegistrationRejectIfMemberHasBalance;
                loanCaseDTO.LoanRegistrationSecurityRequired = loanProduct.LoanRegistrationSecurityRequired;
                loanCaseDTO.LoanRegistrationAllowSelfGuarantee = loanProduct.LoanRegistrationAllowSelfGuarantee;
                loanCaseDTO.LoanRegistrationGracePeriod = loanProduct.LoanRegistrationGracePeriod;
                loanCaseDTO.LoanRegistrationPaymentDueDate = loanProduct.LoanRegistrationPaymentDueDate;
                loanCaseDTO.LoanRegistrationPayoutRecoveryMode = loanProduct.LoanRegistrationPayoutRecoveryMode;
                loanCaseDTO.LoanRegistrationPayoutRecoveryPercentage = loanProduct.LoanRegistrationPayoutRecoveryPercentage;
                loanCaseDTO.LoanRegistrationAggregateCheckOffRecoveryMode = loanProduct.LoanRegistrationAggregateCheckOffRecoveryMode;
                loanCaseDTO.LoanRegistrationChargeClearanceFee = loanProduct.LoanRegistrationChargeClearanceFee;
                loanCaseDTO.LoanRegistrationMicrocredit = loanProduct.LoanRegistrationMicrocredit;
                loanCaseDTO.LoanRegistrationStandingOrderTrigger = loanProduct.LoanRegistrationStandingOrderTrigger;
                loanCaseDTO.LoanRegistrationTrackArrears = loanProduct.LoanRegistrationTrackArrears;
                loanCaseDTO.LoanRegistrationChargeArrearsFee = loanProduct.LoanRegistrationChargeArrearsFee;
                loanCaseDTO.LoanRegistrationEnforceSystemAppraisalRecommendation = loanProduct.LoanRegistrationEnforceSystemAppraisalRecommendation;
                loanCaseDTO.LoanRegistrationBypassAudit = loanProduct.LoanRegistrationBypassAudit;
                loanCaseDTO.LoanRegistrationGuarantorSecurityMode = loanProduct.LoanRegistrationGuarantorSecurityMode;
                loanCaseDTO.LoanRegistrationRoundingType = loanProduct.LoanRegistrationRoundingType;
                loanCaseDTO.LoanRegistrationDisburseMicroLoanLessDeductions = loanProduct.LoanRegistrationDisburseMicroLoanLessDeductions;
                loanCaseDTO.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal = loanProduct.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal;
                loanCaseDTO.LoanRegistrationThrottleScheduledArrearsRecovery = loanProduct.LoanRegistrationThrottleScheduledArrearsRecovery;
                loanCaseDTO.LoanRegistrationCreateStandingOrderOnLoanAudit = loanProduct.LoanRegistrationCreateStandingOrderOnLoanAudit;
                loanCaseDTO.TakeHomeType = loanProduct.TakeHomeType;
                loanCaseDTO.TakeHomePercentage = loanProduct.TakeHomePercentage;
                loanCaseDTO.TakeHomeFixedAmount = loanProduct.TakeHomeFixedAmount;

                // Calculate Loan Balance
                // Calculate Investments Balace
                // Find Maximum Percentage

                Session["LoanProductIdE"] = loanCaseDTO.LoanProductId;
                Session["LoanProductDescriptionE"] = loanCaseDTO.LoanProductDescription;
                Session["InterestCalculationModeDescriptionE"] = loanCaseDTO.InterestCalculationModeDescription;
                Session["LoanInterestAnnualPercentageRateE"] = loanCaseDTO.LoanInterestAnnualPercentageRate;
                Session["LoanProductSectionDescriptionE"] = loanCaseDTO.LoanProductSectionDescription;
                Session["LoanRegistrationTermInMonthsE"] = loanCaseDTO.LoanRegistrationTermInMonths;
                Session["LoanRegistrationMaximumAmountE"] = loanCaseDTO.LoanRegistrationMaximumAmount;

                Session["LoanInterestRecoveryModeE"] = loanCaseDTO.LoanInterestRecoveryMode;
                Session["LoanInterestCalculationModeE"] = loanCaseDTO.LoanInterestCalculationMode;


                Session["LoanRegistrationPaymentFrequencyPerYearE"] = loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear;
                Session["LoanRegistrationMinimumAmountE"] = loanCaseDTO.LoanRegistrationMinimumAmount;
                Session["LoanRegistrationMinimumInterestAmountE"] = loanCaseDTO.LoanRegistrationMinimumInterestAmount;
                Session["LoanRegistrationMinimumGuarantorsE"] = loanCaseDTO.LoanRegistrationMinimumGuarantors;
                Session["LoanRegistrationMinimumMembershipPeriodE"] = loanCaseDTO.LoanRegistrationMinimumMembershipPeriod;
                Session["LoanRegistrationMaximumGuaranteesE"] = loanCaseDTO.LoanRegistrationMaximumGuarantees;
                Session["LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlementE"] = loanCaseDTO.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement;
                Session["LoanRegistrationMaximumSelfGuaranteeEligiblePercentageE"] = loanCaseDTO.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage;
                Session["LoanRegistrationLoanProductSectionE"] = loanCaseDTO.LoanRegistrationLoanProductSection;
                Session["LoanRegistrationLoanProductCategoryE"] = loanCaseDTO.LoanRegistrationLoanProductCategory;
                Session["LoanRegistrationConsecutiveIncomeE"] = loanCaseDTO.LoanRegistrationConsecutiveIncome;
                Session["LoanRegistrationInvestmentsMultiplierE"] = loanCaseDTO.LoanRegistrationInvestmentsMultiplier;
                Session["LoanRegistrationRejectIfMemberHasBalanceE"] = loanCaseDTO.LoanRegistrationRejectIfMemberHasBalance;
                Session["LoanRegistrationSecurityRequiredE"] = loanCaseDTO.LoanRegistrationSecurityRequired;
                Session["LoanRegistrationAllowSelfGuaranteeE"] = loanCaseDTO.LoanRegistrationAllowSelfGuarantee;
                Session["LoanRegistrationGracePeriodE"] = loanCaseDTO.LoanRegistrationGracePeriod;
                Session["LoanRegistrationPaymentDueDateE"] = loanCaseDTO.LoanRegistrationPaymentDueDate;
                Session["LoanRegistrationPayoutRecoveryModeE"] = loanCaseDTO.LoanRegistrationPayoutRecoveryMode;
                Session["LoanRegistrationPayoutRecoveryPercentageE"] = loanCaseDTO.LoanRegistrationPayoutRecoveryPercentage;
                Session["LoanRegistrationAggregateCheckOffRecoveryModeE"] = loanCaseDTO.LoanRegistrationAggregateCheckOffRecoveryMode;
                Session["LoanRegistrationChargeClearanceFeeE"] = loanCaseDTO.LoanRegistrationChargeClearanceFee;
                Session["LoanRegistrationMicrocreditE"] = loanCaseDTO.LoanRegistrationMicrocredit;
                Session["LoanRegistrationStandingOrderTriggerE"] = loanCaseDTO.LoanRegistrationStandingOrderTrigger;
                Session["LoanRegistrationTrackArrearsE"] = loanCaseDTO.LoanRegistrationTrackArrears;
                Session["LoanRegistrationChargeArrearsFeeE"] = loanCaseDTO.LoanRegistrationChargeArrearsFee;
                Session["LoanRegistrationEnforceSystemAppraisalRecommendationE"] = loanCaseDTO.LoanRegistrationEnforceSystemAppraisalRecommendation;
                Session["LoanRegistrationBypassAuditE"] = loanCaseDTO.LoanRegistrationBypassAudit;
                Session["LoanRegistrationGuarantorSecurityModeE"] = loanCaseDTO.LoanRegistrationGuarantorSecurityMode;
                Session["LoanRegistrationRoundingTypeE"] = loanCaseDTO.LoanRegistrationRoundingType;
                Session["LoanRegistrationDisburseMicroLoanLessDeductionsE"] = loanCaseDTO.LoanRegistrationDisburseMicroLoanLessDeductions;
                Session["LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisalE"] = loanCaseDTO.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal;
                Session["LoanRegistrationThrottleScheduledArrearsRecoveryE"] = loanCaseDTO.LoanRegistrationThrottleScheduledArrearsRecovery;
                Session["LoanRegistrationCreateStandingOrderOnLoanAuditE"] = loanCaseDTO.LoanRegistrationCreateStandingOrderOnLoanAudit;
                Session["LoanRegistrationCreateStandingOrderOnLoanAuditE"] = loanCaseDTO.LoanRegistrationCreateStandingOrderOnLoanAudit;

            }

            return View("edit", loanCaseDTO);
        }


        public async Task<ActionResult> SavingsProductLookupEdit(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("edit");
            }

            loanCaseDTO = Session["editViewLoanee"] as LoanCaseDTO;

            // Check Sessions for data and keep in controls
            if (Session["LoanPurposeIdE"] != null)
            {
                loanCaseDTO.LoanPurposeId = (Guid)Session["LoanPurposeIdE"];
                loanCaseDTO.LoanPurposeDescription = Session["LoanPurposeDescriptionE"].ToString();
            }

            if (Session["LoanProductIdE"] != null)
            {
                loanCaseDTO.LoanProductId = (Guid)Session["LoanProductIdE"];
                loanCaseDTO.LoanProductDescription = Session["LoanProductDescriptionE"].ToString();
                loanCaseDTO.InterestCalculationModeDescription = Session["InterestCalculationModeDescriptionE"].ToString();
                loanCaseDTO.LoanInterestAnnualPercentageRate = Convert.ToDouble(Session["LoanInterestAnnualPercentageRateE"].ToString());
                loanCaseDTO.LoanProductSectionDescription = Session["LoanProductSectionDescriptionE"].ToString();
                loanCaseDTO.LoanRegistrationTermInMonths = Convert.ToInt32(Session["LoanRegistrationTermInMonthsE"].ToString());
                loanCaseDTO.LoanRegistrationMaximumAmount = Convert.ToDecimal(Session["LoanRegistrationMaximumAmountE"].ToString());

                loanCaseDTO.LoanInterestRecoveryMode = Convert.ToInt32(Session["LoanInterestRecoveryModeE"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationModeE"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationModeE"].ToString());
            }

            if (Session["RegistrationRemarkIdE"] != null)
            {
                loanCaseDTO.RegistrationRemarkId = (Guid)Session["RegistrationRemarkIdE"];
                loanCaseDTO.Remarks = Session["RemarksE"].ToString();
            }

            var savingsProduct = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());
            if (savingsProduct != null)
            {
                loanCaseDTO.SavingsProductId = savingsProduct.Id;
                loanCaseDTO.SavingsProductDescription = savingsProduct.Description;

                Session["SavingsProductIdE"] = loanCaseDTO.SavingsProductId;
                Session["SavingsProductDescriptionE"] = loanCaseDTO.SavingsProductDescription;
            }

            return View("edit", loanCaseDTO);
        }


        public async Task<ActionResult> LoaningRemarksLookupEdit(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("edit");
            }

            loanCaseDTO = Session["editViewLoanee"] as LoanCaseDTO;

            // Check Sessions for data and keep in controls

            if (Session["LoanPurposeIdE"] != null)
            {
                loanCaseDTO.LoanPurposeId = (Guid)Session["LoanPurposeIdE"];
                loanCaseDTO.LoanPurposeDescription = Session["LoanPurposeDescriptionE"].ToString();
            }

            if (Session["LoanProductIdE"] != null)
            {
                loanCaseDTO.LoanProductId = (Guid)Session["LoanProductIdE"];
                loanCaseDTO.LoanProductDescription = Session["LoanProductDescriptionE"].ToString();
                loanCaseDTO.InterestCalculationModeDescription = Session["InterestCalculationModeDescriptionE"].ToString();
                loanCaseDTO.LoanInterestAnnualPercentageRate = Convert.ToDouble(Session["LoanInterestAnnualPercentageRateE"].ToString());
                loanCaseDTO.LoanProductSectionDescription = Session["LoanProductSectionDescriptionE"].ToString();
                loanCaseDTO.LoanRegistrationTermInMonths = Convert.ToInt32(Session["LoanRegistrationTermInMonthsE"].ToString());
                loanCaseDTO.LoanRegistrationMaximumAmount = Convert.ToDecimal(Session["LoanRegistrationMaximumAmountE"].ToString());

                loanCaseDTO.LoanInterestRecoveryMode = Convert.ToInt32(Session["LoanInterestRecoveryModeE"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationModeE"].ToString());
                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["LoanInterestCalculationModeE"].ToString());
            }

            if (Session["SavingsProductIdE"] != null)
            {
                loanCaseDTO.SavingsProductId = (Guid)Session["SavingsProductIdE"];
                loanCaseDTO.SavingsProductDescription = Session["SavingsProductDescriptionE"].ToString();
            }

            var loaningRemarks = await _channelService.FindLoaningRemarkAsync(parseId, GetServiceHeader());
            if (loaningRemarks != null)
            {
                loanCaseDTO.RegistrationRemarkId = loaningRemarks.Id;
                loanCaseDTO.Remarks = loaningRemarks.Description;

                Session["RegistrationRemarkIdE"] = loanCaseDTO.RegistrationRemarkId;
                Session["RemarksE"] = loanCaseDTO.Remarks;
            }


            return View("edit", loanCaseDTO);
        }



        // Create Loan Case
        [HttpPost]
        public async Task<ActionResult> Create(LoanCaseDTO loanCaseDTO)
        {
            if (Session["CustomerId"] == null || Session["LoanPurposeId"] == null || Session["LoanProductId"] == null ||
                Session["SavingsProductId"] == null || Session["RegistrationRemarkId"] == null)
            {
                TempData["EmptyData"] = "Could not create Loan Case. Kindly make sure to provide all the required details !";

                return View();
            }

            var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            loanCaseDTO.BranchId = (Guid)userDTO.BranchId;

            loanCaseDTO.ReceivedDate = DateTime.Today;

            // Loanee
            loanCaseDTO.CustomerId = (Guid)Session["CustomerId"];
            loanCaseDTO.CustomerIndividualFirstName = Session["CustomerIndividualFirstName"].ToString();
            loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = Session["CustomerStationZoneDivisionEmployerDescription"].ToString();
            loanCaseDTO.CustomerStation = Session["CustomerStation"].ToString();
            loanCaseDTO.CustomerReference1 = Session["CustomerReference1"].ToString();
            loanCaseDTO.CustomerReference2 = Session["CustomerReference2"].ToString();
            loanCaseDTO.CustomerReference3 = Session["CustomerReference3"].ToString();

            // Loan Purpose
            loanCaseDTO.LoanPurposeId = (Guid)Session["LoanPurposeId"];
            loanCaseDTO.LoanPurposeDescription = Session["LoanPurposeDescription"].ToString();


            // Loan Product
            loanCaseDTO.LoanProductId = (Guid)Session["LoanProductId"];
            loanCaseDTO.LoanProductDescription = Session["LoanProductDescription"].ToString();
            loanCaseDTO.InterestCalculationModeDescription = Session["InterestCalculationModeDescription"].ToString();
            loanCaseDTO.LoanInterestAnnualPercentageRate = Convert.ToDouble(Session["LoanInterestAnnualPercentageRate"].ToString());
            loanCaseDTO.LoanProductSectionDescription = Session["LoanProductSectionDescription"].ToString();
            loanCaseDTO.LoanRegistrationTermInMonths = Convert.ToInt32(Session["LoanRegistrationTermInMonths"].ToString());
            loanCaseDTO.LoanRegistrationMaximumAmount = Convert.ToDecimal(Session["LoanRegistrationMaximumAmount"].ToString());


            var loanProduct = await _channelService.FindLoanProductAsync(loanCaseDTO.LoanProductId, GetServiceHeader());

            if (loanProduct != null)
            {
                loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear = loanProduct.LoanRegistrationPaymentFrequencyPerYear;
                loanCaseDTO.LoanRegistrationMinimumAmount = loanProduct.LoanRegistrationMinimumAmount;
                loanCaseDTO.LoanRegistrationMinimumInterestAmount = loanProduct.LoanRegistrationMinimumInterestAmount;
                loanCaseDTO.LoanRegistrationMinimumGuarantors = loanProduct.LoanRegistrationMinimumGuarantors;
                loanCaseDTO.LoanRegistrationMinimumMembershipPeriod = loanProduct.LoanRegistrationMinimumMembershipPeriod;
                loanCaseDTO.LoanRegistrationMaximumGuarantees = loanProduct.LoanRegistrationMaximumGuarantees;
                loanCaseDTO.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement = loanProduct.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement;
                loanCaseDTO.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage = loanProduct.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage;
                loanCaseDTO.LoanRegistrationLoanProductSection = loanProduct.LoanRegistrationLoanProductSection;
                loanCaseDTO.LoanRegistrationLoanProductCategory = loanProduct.LoanRegistrationLoanProductCategory;
                loanCaseDTO.LoanRegistrationConsecutiveIncome = loanProduct.LoanRegistrationConsecutiveIncome;
                loanCaseDTO.LoanRegistrationInvestmentsMultiplier = loanProduct.LoanRegistrationInvestmentsMultiplier;
                loanCaseDTO.LoanRegistrationRejectIfMemberHasBalance = loanProduct.LoanRegistrationRejectIfMemberHasBalance;
                loanCaseDTO.LoanRegistrationSecurityRequired = loanProduct.LoanRegistrationSecurityRequired;
                loanCaseDTO.LoanRegistrationAllowSelfGuarantee = loanProduct.LoanRegistrationAllowSelfGuarantee;
                loanCaseDTO.LoanRegistrationGracePeriod = loanProduct.LoanRegistrationGracePeriod;
                loanCaseDTO.LoanRegistrationPaymentDueDate = loanProduct.LoanRegistrationPaymentDueDate;
                loanCaseDTO.LoanRegistrationPayoutRecoveryMode = loanProduct.LoanRegistrationPayoutRecoveryMode;
                loanCaseDTO.LoanRegistrationPayoutRecoveryPercentage = loanProduct.LoanRegistrationPayoutRecoveryPercentage;
                loanCaseDTO.LoanRegistrationAggregateCheckOffRecoveryMode = loanProduct.LoanRegistrationAggregateCheckOffRecoveryMode;
                loanCaseDTO.LoanRegistrationChargeClearanceFee = loanProduct.LoanRegistrationChargeClearanceFee;
                loanCaseDTO.LoanRegistrationMicrocredit = loanProduct.LoanRegistrationMicrocredit;
                loanCaseDTO.LoanRegistrationStandingOrderTrigger = loanProduct.LoanRegistrationStandingOrderTrigger;
                loanCaseDTO.LoanRegistrationTrackArrears = loanProduct.LoanRegistrationTrackArrears;
                loanCaseDTO.LoanRegistrationChargeArrearsFee = loanProduct.LoanRegistrationChargeArrearsFee;
                loanCaseDTO.LoanRegistrationEnforceSystemAppraisalRecommendation = loanProduct.LoanRegistrationEnforceSystemAppraisalRecommendation;
                loanCaseDTO.LoanRegistrationBypassAudit = loanProduct.LoanRegistrationBypassAudit;
                loanCaseDTO.LoanRegistrationGuarantorSecurityMode = loanProduct.LoanRegistrationGuarantorSecurityMode;
                loanCaseDTO.LoanRegistrationRoundingType = loanProduct.LoanRegistrationRoundingType;
                loanCaseDTO.LoanRegistrationDisburseMicroLoanLessDeductions = loanProduct.LoanRegistrationDisburseMicroLoanLessDeductions;
                loanCaseDTO.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal = loanProduct.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal;
                loanCaseDTO.LoanRegistrationThrottleScheduledArrearsRecovery = loanProduct.LoanRegistrationThrottleScheduledArrearsRecovery;
                loanCaseDTO.LoanRegistrationCreateStandingOrderOnLoanAudit = loanProduct.LoanRegistrationCreateStandingOrderOnLoanAudit;
                loanCaseDTO.TakeHomeType = loanProduct.TakeHomeType;
                loanCaseDTO.TakeHomePercentage = loanProduct.TakeHomePercentage;
                loanCaseDTO.TakeHomeFixedAmount = loanProduct.TakeHomeFixedAmount;


                loanCaseDTO.LoanInterestChargeMode = loanProduct.LoanInterestChargeMode;
                loanCaseDTO.LoanInterestRecoveryMode = loanProduct.LoanInterestRecoveryMode;
                loanCaseDTO.LoanInterestCalculationMode = loanProduct.LoanInterestCalculationMode;


            }


            // Savings Product
            loanCaseDTO.SavingsProductId = (Guid)Session["SavingsProductId"];
            loanCaseDTO.SavingsProductDescription = Session["SavingsProductDescription"].ToString();

            // Loaning Remarks
            loanCaseDTO.RegistrationRemarkId = (Guid)Session["RegistrationRemarkId"];
            loanCaseDTO.Remarks = Session["Remarks"].ToString();



            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {
                if (loanCaseDTO.AmountApplied < 1)
                {
                    TempData["lessAmountApplied"] = "The amount you are applying for is too low. Enter amount greater than " + loanCaseDTO.AmountApplied;
                    return View("create", loanCaseDTO);
                }

                var loanCase = await _channelService.AddLoanCaseAsync(loanCaseDTO, GetServiceHeader());


                if (loanCase.ErrorMessageResult != null)
                {
                    await ServeNavigationMenus();

                    TempData["existngApplicationError"] = loanCase.ErrorMessageResult;

                    return View();
                }

                // Clear sessions

                // Loanee sessions
                Session.Remove("CustomerId");
                Session.Remove("CustomerIndividualFirstName");
                Session.Remove("CustomerStationZoneDivisionEmployerDescription");
                Session.Remove("CustomerStation");
                Session.Remove("CustomerReference1");
                Session.Remove("CustomerReference2");
                Session.Remove("CustomerReference3");

                // Loan Purpose sessions
                Session.Remove("LoanPurposeId");
                Session.Remove("LoanPurposeDescription");

                // Loan Product Sessions
                Session.Remove("LoanProductId");
                Session.Remove("LoanProductDescription");
                Session.Remove("InterestCalculationModeDescription");
                Session.Remove("LoanInterestAnnualPercentageRate");
                Session.Remove("LoanProductSectionDescription");
                Session.Remove("LoanRegistrationTermInMonths");
                Session.Remove("LoanRegistrationMaximumAmount");

                // Savings Products Sessions
                Session.Remove("SavingsProductId");
                Session.Remove("SavingsProductDescription");

                // Loan Registration Remarks Sessions
                Session.Remove("RegistrationRemarkId");
                Session.Remove("Remarks");


                TempData["AlertMessage"] = "Loan registration successful.";
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanCaseDTO.ErrorMessages;
                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());

                TempData["ErrorMessage"] = "Loan registration failed ";
                return View(loanCaseDTO);
            }
        }


        // Edit Loan Case
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(LoanCaseDTO loanCaseDTO)
        {
            var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            loanCaseDTO.BranchId = (Guid)userDTO.BranchId;
            loanCaseDTO.AmountApplied = loanCaseDTO.AmountApplied;
            loanCaseDTO.Reference = loanCaseDTO.Reference;
            loanCaseDTO.CaseNumber = Convert.ToInt32(Session["CaseNumber"].ToString());
            loanCaseDTO.Status = Convert.ToInt32(Session["Status"].ToString());

            if (Session["LoanPurposeIdE"] != null)
            {
                // Loan Purpose
                loanCaseDTO.LoanPurposeId = (Guid)Session["LoanPurposeIdE"];
                loanCaseDTO.LoanPurposeDescription = Session["LoanPurposeDescriptionE"].ToString();
            }

            if (Session["LoanProductIdE"] != null)
            {
                // Loan Product
                loanCaseDTO.LoanProductId = (Guid)Session["LoanProductIdE"];
                loanCaseDTO.LoanProductDescription = Session["LoanProductDescriptionE"].ToString();
                loanCaseDTO.InterestCalculationModeDescription = Session["InterestCalculationModeDescriptionE"].ToString();
                loanCaseDTO.LoanInterestAnnualPercentageRate = Convert.ToDouble(Session["LoanInterestAnnualPercentageRateE"].ToString());
                loanCaseDTO.LoanProductSectionDescription = Session["LoanProductSectionDescriptionE"].ToString();
                loanCaseDTO.LoanRegistrationTermInMonths = Convert.ToInt32(Session["LoanRegistrationTermInMonthsE"].ToString());
                loanCaseDTO.LoanRegistrationMaximumAmount = Convert.ToDecimal(Session["LoanRegistrationMaximumAmountE"].ToString());

                var loanProduct = await _channelService.FindLoanProductAsync(loanCaseDTO.LoanProductId, GetServiceHeader());

                if (loanProduct != null)
                {
                    loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear = loanProduct.LoanRegistrationPaymentFrequencyPerYear;
                    loanCaseDTO.LoanRegistrationMinimumAmount = loanProduct.LoanRegistrationMinimumAmount;
                    loanCaseDTO.LoanRegistrationMinimumInterestAmount = loanProduct.LoanRegistrationMinimumInterestAmount;
                    loanCaseDTO.LoanRegistrationMinimumGuarantors = loanProduct.LoanRegistrationMinimumGuarantors;
                    loanCaseDTO.LoanRegistrationMinimumMembershipPeriod = loanProduct.LoanRegistrationMinimumMembershipPeriod;
                    loanCaseDTO.LoanRegistrationMaximumGuarantees = loanProduct.LoanRegistrationMaximumGuarantees;
                    loanCaseDTO.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement = loanProduct.LoanRegistrationExcludeOutstandingLoansOnMaximumEntitlement;
                    loanCaseDTO.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage = loanProduct.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage;
                    loanCaseDTO.LoanRegistrationLoanProductSection = loanProduct.LoanRegistrationLoanProductSection;
                    loanCaseDTO.LoanRegistrationLoanProductCategory = loanProduct.LoanRegistrationLoanProductCategory;
                    loanCaseDTO.LoanRegistrationConsecutiveIncome = loanProduct.LoanRegistrationConsecutiveIncome;
                    loanCaseDTO.LoanRegistrationInvestmentsMultiplier = loanProduct.LoanRegistrationInvestmentsMultiplier;
                    loanCaseDTO.LoanRegistrationRejectIfMemberHasBalance = loanProduct.LoanRegistrationRejectIfMemberHasBalance;
                    loanCaseDTO.LoanRegistrationSecurityRequired = loanProduct.LoanRegistrationSecurityRequired;
                    loanCaseDTO.LoanRegistrationAllowSelfGuarantee = loanProduct.LoanRegistrationAllowSelfGuarantee;
                    loanCaseDTO.LoanRegistrationGracePeriod = loanProduct.LoanRegistrationGracePeriod;
                    loanCaseDTO.LoanRegistrationPaymentDueDate = loanProduct.LoanRegistrationPaymentDueDate;
                    loanCaseDTO.LoanRegistrationPayoutRecoveryMode = loanProduct.LoanRegistrationPayoutRecoveryMode;
                    loanCaseDTO.LoanRegistrationPayoutRecoveryPercentage = loanProduct.LoanRegistrationPayoutRecoveryPercentage;
                    loanCaseDTO.LoanRegistrationAggregateCheckOffRecoveryMode = loanProduct.LoanRegistrationAggregateCheckOffRecoveryMode;
                    loanCaseDTO.LoanRegistrationChargeClearanceFee = loanProduct.LoanRegistrationChargeClearanceFee;
                    loanCaseDTO.LoanRegistrationMicrocredit = loanProduct.LoanRegistrationMicrocredit;
                    loanCaseDTO.LoanRegistrationStandingOrderTrigger = loanProduct.LoanRegistrationStandingOrderTrigger;
                    loanCaseDTO.LoanRegistrationTrackArrears = loanProduct.LoanRegistrationTrackArrears;
                    loanCaseDTO.LoanRegistrationChargeArrearsFee = loanProduct.LoanRegistrationChargeArrearsFee;
                    loanCaseDTO.LoanRegistrationEnforceSystemAppraisalRecommendation = loanProduct.LoanRegistrationEnforceSystemAppraisalRecommendation;
                    loanCaseDTO.LoanRegistrationBypassAudit = loanProduct.LoanRegistrationBypassAudit;
                    loanCaseDTO.LoanRegistrationGuarantorSecurityMode = loanProduct.LoanRegistrationGuarantorSecurityMode;
                    loanCaseDTO.LoanRegistrationRoundingType = loanProduct.LoanRegistrationRoundingType;
                    loanCaseDTO.LoanRegistrationDisburseMicroLoanLessDeductions = loanProduct.LoanRegistrationDisburseMicroLoanLessDeductions;
                    loanCaseDTO.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal = loanProduct.LoanRegistrationConsiderInvestmentsBalanceForIncomeBasedLoanAppraisal;
                    loanCaseDTO.LoanRegistrationThrottleScheduledArrearsRecovery = loanProduct.LoanRegistrationThrottleScheduledArrearsRecovery;
                    loanCaseDTO.LoanRegistrationCreateStandingOrderOnLoanAudit = loanProduct.LoanRegistrationCreateStandingOrderOnLoanAudit;
                    loanCaseDTO.TakeHomeType = loanProduct.TakeHomeType;
                    loanCaseDTO.TakeHomePercentage = loanProduct.TakeHomePercentage;
                    loanCaseDTO.TakeHomeFixedAmount = loanProduct.TakeHomeFixedAmount;


                    loanCaseDTO.LoanInterestChargeMode = loanProduct.LoanInterestChargeMode;
                    loanCaseDTO.LoanInterestRecoveryMode = loanProduct.LoanInterestRecoveryMode;
                    loanCaseDTO.LoanInterestCalculationMode = loanProduct.LoanInterestCalculationMode;


                }

            }

            if (Session["SavingsProductIdE"] != null)
            {
                // Savings Product
                loanCaseDTO.SavingsProductId = (Guid)Session["SavingsProductIdE"];
                loanCaseDTO.SavingsProductDescription = Session["SavingsProductDescriptionE"].ToString();
            }


            if (Session["RegistrationRemarkIdE"] != null)
            {
                // Loaning Remarks
                loanCaseDTO.RegistrationRemarkId = (Guid)Session["RegistrationRemarkIdE"];
                loanCaseDTO.Remarks = Session["RemarksE"].ToString();
            }


            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {
                await _channelService.UpdateLoanCaseAsync(loanCaseDTO, GetServiceHeader());

                // Loanee sessions
                Session.Remove("CustomerIdE");
                Session.Remove("CustomerIndividualFirstNameE");
                Session.Remove("CustomerStationZoneDivisionEmployerDescriptionE");
                Session.Remove("CustomerStationE");
                Session.Remove("CustomerReference1E");
                Session.Remove("CustomerReference2E");
                Session.Remove("CustomerReference3E");

                // Loan Purpose sessions
                Session.Remove("LoanPurposeIdE");
                Session.Remove("LoanPurposeDescriptionE");

                // Loan Product Sessions
                Session.Remove("LoanProductIdE");
                Session.Remove("LoanProductDescriptionE");
                Session.Remove("InterestCalculationModeDescriptionE");
                Session.Remove("LoanInterestAnnualPercentageRateE");
                Session.Remove("LoanProductSectionDescriptionE");
                Session.Remove("LoanRegistrationTermInMonthsE");
                Session.Remove("LoanRegistrationMaximumAmountE");

                // Savings Products Sessions
                Session.Remove("SavingsProductIdE");
                Session.Remove("SavingsProductDescriptionE");

                // Loan Registration Remarks Sessions
                Session.Remove("RegistrationRemarkIdE");
                Session.Remove("RemarksE");

                TempData["Edit"] = "Loan Case edited Successfully";

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanCaseDTO.ErrorMessages;

                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());

                TempData["EditError"] = "Loan Case Unsuccessful";

                return View(loanCaseDTO);
            }
        }
    }
}