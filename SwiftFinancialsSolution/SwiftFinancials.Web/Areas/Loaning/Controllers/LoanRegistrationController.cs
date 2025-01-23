using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Extensions;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using OfficeOpenXml;
using SwiftFinancials.Web.Areas.Registry.DocumentsModel;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using SwiftFinancials.Web.PDF;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class LoanRegistrationController : MasterController
    {
        #region Get Documents
        private readonly string _connectionString;
        public LoanRegistrationController()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        // Get Documents ...........................
        private async Task<List<Document>> GetDocumentsAsync(Guid id)
        {
            var documents = new List<Document>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT PassportPhoto, SignaturePhoto, IDCardFrontPhoto, IDCardBackPhoto FROM swiftFin_SpecimenCapture WHERE CustomerId = @CustomerId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerId", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            documents.Add(new Document
                            {
                                PassportPhoto = reader.IsDBNull(0) ? null : (byte[])reader[0],
                                SignaturePhoto = reader.IsDBNull(1) ? null : (byte[])reader[1],
                                IDCardFrontPhoto = reader.IsDBNull(2) ? null : (byte[])reader[2],
                                IDCardBackPhoto = reader.IsDBNull(3) ? null : (byte[])reader[3]
                            });
                        }
                    }
                }
            }

            return documents;
        }
        #endregion

        #region Index
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.LoanCaseFilterSelectList = GetLoanCaseFilterTypeSelectList(string.Empty);
            ViewBag.LoanCaseStatusSelectList = GetLoanCaseStatusSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int loanCaseStatus, string filterValue, int filterType)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync(
                loanCaseStatus,
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
        #endregion

        #region Lookups
        public async Task<ActionResult> LoaneeLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }
            Session["CustomerId"] = parseId;

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            if (customer != null)
            {
                if (customer.RecordStatus != (int)RecordStatus.Approved)
                {
                    ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                    ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                    ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());


                    ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                    ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                    ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());


                    MessageBox.Show(Form.ActiveForm, "The selected Customer has not yet been approved.", "Loan Registration", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification);
                    return Json(new { success = false, message = "" });
                }

                var documents = await GetDocumentsAsync(parseId);

                if (documents.Any())
                {
                    var document = documents.First();
                    TempData["PassportPhoto"] = document.PassportPhoto;
                    TempData["SignaturePhoto"] = document.SignaturePhoto;
                    TempData["IDCardFrontPhoto"] = document.IDCardFrontPhoto;
                    TempData["IDCardBackPhoto"] = document.IDCardBackPhoto;

                    loanCaseDTO.PassportPhoto = document.PassportPhoto;
                    loanCaseDTO.SignaturePhoto = document.SignaturePhoto;
                    loanCaseDTO.IDCardFrontPhoto = document.IDCardFrontPhoto;
                    loanCaseDTO.IDCardBackPhoto = document.IDCardBackPhoto;
                }


                loanCaseDTO.CustomerId = customer.Id;
                loanCaseDTO.CustomerIndividualFirstName = customer.IndividualSalutationDescription + " " + customer.IndividualFirstName + " " + customer.IndividualLastName;
                loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                loanCaseDTO.CustomerStation = customer.StationDescription;
                loanCaseDTO.CustomerReference1 = customer.Reference1;
                loanCaseDTO.CustomerReference2 = customer.Reference2;
                loanCaseDTO.CustomerReference3 = customer.Reference3;

                //// Standing Orders
                ObservableCollection<Guid> customerAccountId = new ObservableCollection<Guid>();
                var customerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(parseId, true, true, true, true, GetServiceHeader());
                foreach (var accounts in customerAccounts)
                {
                    customerAccountId.Add(accounts.Id);
                }
                List<StandingOrderDTO> allStandingOrders = new List<StandingOrderDTO>();
                foreach (var Ids in customerAccountId)
                {
                    var standingOrders = await _channelService.FindStandingOrdersByBeneficiaryCustomerAccountIdAsync(Ids, true, GetServiceHeader());
                    if (standingOrders != null && standingOrders.Any())
                    {
                        allStandingOrders.AddRange(standingOrders);
                    }
                }

                //// Payouts
                var payouts = await _channelService.FindCreditBatchEntriesByCustomerIdAsync((int)CreditBatchType.Payout, parseId, true, GetServiceHeader());

                ////Salary
                // No method fetching by customerId


                //// Loan Applications
                var loanApplications = await _channelService.FindLoanCasesByCustomerIdInProcessAsync(parseId, GetServiceHeader());


                //// Collaterals...
                var collaterals = await _channelService.FindCustomerDocumentsByCustomerIdAndTypeAsync(parseId, (int)CustomerDocumentType.Collateral, GetServiceHeader());
                var releasedCollaterals = collaterals.Where(rC => rC.CollateralStatus == (int)CollateralStatus.Released);
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CustomerId = loanCaseDTO.CustomerId,
                        CustomerIndividualFirstName = loanCaseDTO.CustomerIndividualFirstName,
                        CustomerStationZoneDivisionEmployerDescription = loanCaseDTO.CustomerStationZoneDivisionEmployerDescription,
                        CustomerStation = loanCaseDTO.CustomerStation,
                        CustomerReference1 = loanCaseDTO.CustomerReference1,
                        CustomerReference2 = loanCaseDTO.CustomerReference2,
                        CustomerReference3 = loanCaseDTO.CustomerReference3,


                        StandingOrders = allStandingOrders,
                        Payouts = payouts,
                        LoanApplications = loanApplications,
                        Collaterals = releasedCollaterals,
                        PassportPhoto = loanCaseDTO.PassportPhoto != null ? Convert.ToBase64String(loanCaseDTO.PassportPhoto) : null,
                        SignaturePhoto = loanCaseDTO.SignaturePhoto != null ? Convert.ToBase64String(loanCaseDTO.SignaturePhoto) : null,
                        IDFront = loanCaseDTO.IDCardFrontPhoto != null ? Convert.ToBase64String(loanCaseDTO.IDCardFrontPhoto) : null,
                        IDBack = loanCaseDTO.IDCardBackPhoto != null ? Convert.ToBase64String(loanCaseDTO.IDCardBackPhoto) : null
                    }
                });
            }
            return Json(new { success = false, message = "Customer Account not found" });
        }

        public async Task<ActionResult> LoanPurposeLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            if (Session["LoanProductIdID"] == null || Session["CustomerId"] == null)
            {
                await ServeNavigationMenus();

                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());

                ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());

                TempData["loanProductAndLoaneeRequired"] = "Loanee and Loan Product Required to proceed!";
                return View("Create");
            }

            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            var loanPurpose = await _channelService.FindLoanPurposeAsync(parseId, GetServiceHeader());
            if (loanPurpose != null)
            {
                loanCaseDTO.LoanPurposeId = loanPurpose.Id;
                loanCaseDTO.LoanPurposeDescription = loanPurpose.Description;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        LoanPurposeId = loanCaseDTO.LoanPurposeId,
                        LoanPurposeDescription = loanCaseDTO.LoanPurposeDescription
                    }
                });
            }
            return Json(new { success = false, message = "Loan Purpose not found" });
        }

        public async Task<ActionResult> LoanProductLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            if (Session["CustomerId"] == null)
            {
                await ServeNavigationMenus();

                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());

                ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());

                TempData["loaneeRequired"] = "Loanee Required!";
                return View("Create");
            }

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }
            Session["LoanProductIdID"] = parseId;
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
                loanCaseDTO.LoanRegistrationMinimumGuarantors = loanProduct.LoanRegistrationMinimumGuarantors;
                loanCaseDTO.LoanRegistrationMaximumGuarantees = loanProduct.LoanRegistrationMaximumGuarantees;
                loanCaseDTO.LoanProductCategory = loanProduct.LoanRegistrationLoanProductCategoryDescription;
                loanCaseDTO.LoanRegistrationInvestmentsMultiplier = loanProduct.LoanRegistrationInvestmentsMultiplier;

                Guid customerId = (Guid)Session["CustomerId"];

                var products = await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(customerId, new[] { (int)ProductCode.Savings, (int)ProductCode.Loan, (int)ProductCode.Investment },
                   true, true, true, true, GetServiceHeader());
                var investmentProducts = products.Where(p => p.CustomerAccountTypeProductCode == (int)ProductCode.Investment).ToList();

                List<decimal> iBalance = new List<decimal>();

                foreach (var investmentsBalances in investmentProducts)
                {
                    iBalance.Add(investmentsBalances.BookBalance);
                }
                var investmentsBalance = iBalance.Sum();

                // Latest Income
                var latestIncome = await _channelService.FindLoanAppraisalCreditBatchEntriesByCustomerIdAsync(customerId, loanCaseDTO.LoanProductId, true, GetServiceHeader());
                loanCaseDTO.LoanProductLatestIncome = latestIncome.Sum(x => x.Balance);

                // loanBalance
                var findCustomerLoanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(loanCaseDTO.CustomerId, loanCaseDTO.LoanProductId,
                    true, true, true, true, GetServiceHeader());
                var bookBalanceTotal = findCustomerLoanAccounts.Sum(q => q.BookBalance);
                var carryForwardsTotal = findCustomerLoanAccounts.Sum(c => c.CarryForwardsBalance);
                var LoanBalance = bookBalanceTotal + carryForwardsTotal;
                loanCaseDTO.LoanProductLoanBalance = LoanBalance;


                var findloanBalanceCustomerAccount = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(customerId, parseId, true, true, true, true,
                    GetServiceHeader());
                var bookBal = findloanBalanceCustomerAccount.Sum(x => x.BookBalance);
                var carryForward = findloanBalanceCustomerAccount.Sum(u => u.CarryForwardsBalance);
                var loanBalance = bookBal + carryForward;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        LoanProductId = loanCaseDTO.LoanProductId,
                        LoanProductDescription = loanCaseDTO.LoanProductDescription,
                        InterestCalculationModeDescription = loanCaseDTO.InterestCalculationModeDescription,
                        LoanInterestAnnualPercentageRate = loanCaseDTO.LoanInterestAnnualPercentageRate,
                        LoanProductSectionDescription = loanCaseDTO.LoanProductSectionDescription,
                        LoanRegistrationTermInMonths = loanCaseDTO.LoanRegistrationTermInMonths,
                        LoanRegistrationMaximumAmount = loanCaseDTO.LoanRegistrationMaximumAmount,
                        MaximumAmountPercentage = loanCaseDTO.MaximumAmountPercentage,
                        LatestIncome = latestIncome,
                        InvestmentsBalance = investmentsBalance,
                        LoanBalance = loanBalance,
                        LoanRegistrationMinimumGuarantors = loanCaseDTO.LoanRegistrationMinimumGuarantors,
                        LoanRegistrationMaximumGuarantees = loanCaseDTO.LoanRegistrationMaximumGuarantees,
                        LoanProductCategory = loanCaseDTO.LoanProductCategory,
                        LoanRegistrationInvestmentsMultiplier = loanCaseDTO.LoanRegistrationInvestmentsMultiplier
                    }
                });
            }
            return Json(new { success = false, message = "Loan Product not found" });
        }

        public async Task<ActionResult> SavingsProductLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            if (Session["LoanProductIdID"] == null || Session["CustomerId"] == null)
            {
                await ServeNavigationMenus();

                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());

                ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());

                TempData["loanProductAndLoaneeRequired"] = "Loanee and Loan Product Required to proceed!";
                return View("Create");
            }

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            var savingsProduct = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());
            if (savingsProduct != null)
            {
                loanCaseDTO.SavingsProductId = savingsProduct.Id;
                loanCaseDTO.SavingsProductDescription = savingsProduct.Description;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        SavingsProductId = loanCaseDTO.SavingsProductId,
                        SavingsProductDescription = loanCaseDTO.SavingsProductDescription
                    }
                });
            }
            return Json(new { success = false, message = "Savings Product not found" });
        }

        public async Task<ActionResult> LoaningRemarksLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            if (Session["LoanProductIdID"] == null || Session["CustomerId"] == null)
            {
                await ServeNavigationMenus();

                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());

                ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());

                TempData["loanProductAndLoaneeRequired"] = "Loanee and Loan Product Required to proceed!";
                return View("Create");
            }

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            var loaningRemarks = await _channelService.FindLoaningRemarkAsync(parseId, GetServiceHeader());
            if (loaningRemarks != null)
            {
                loanCaseDTO.RegistrationRemarkId = loaningRemarks.Id;
                loanCaseDTO.Remarks = loaningRemarks.Description;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        RegistrationRemarkId = loanCaseDTO.RegistrationRemarkId,
                        Remarks = loanCaseDTO.Remarks
                    }
                });
            }
            return Json(new { success = false, message = "Loaning Remark not found" });
        }

        [HttpPost]
        public async Task<ActionResult> LoanGuarantorLookUp(Guid id, LoanCaseDTO loanCaseDTO)
        {
            if (Session["LoanProductIdID"] == null || Session["CustomerId"] == null)
            {
                await ServeNavigationMenus();

                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());

                ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());

                TempData["loanProductAndLoaneeRequired"] = "Loanee and Loan Product Required to proceed!";
                return View("Create");
            }

            if (id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }

            var guarantorLookUp = new LoanGuarantorDTO();

            var loanGuarantor = await _channelService.FindCustomerAsync(id, GetServiceHeader());
            if (loanGuarantor != null)
            {
                Guid LoanProductId = Guid.Empty;

                if (Session["LoanProductIdID"] != null)
                    LoanProductId = (Guid)Session["LoanProductIdID"];
                else
                {
                    ViewBag.CustomerFilter = GetCustomerFilterSelectList(string.Empty);

                    ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
                    ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
                    ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(string.Empty);

                    ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
                    ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);

                    ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);

                    TempData["LoanProductRequired"] = "Loan Product Required!";
                    return View("Create");
                }

                var products = await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(id, new[] { (int)ProductCode.Savings, (int)ProductCode.Loan, (int)ProductCode.Investment },
                    true, true, true, true, GetServiceHeader());
                var savingsProducts = products.Where(p => p.CustomerAccountTypeProductCode == (int)ProductCode.Savings).ToList();
                var loanProducts = products.Where(p => p.CustomerAccountTypeProductCode == (int)ProductCode.Loan).ToList();
                var investmentProducts = products.Where(p => p.CustomerAccountTypeProductCode == (int)ProductCode.Investment).ToList();

                List<decimal> sBalance = new List<decimal>();
                List<decimal> iBalance = new List<decimal>();

                foreach (var savingsBalances in savingsProducts)
                {
                    sBalance.Add(savingsBalances.BookBalance);
                }

                foreach (var investmentsBalances in investmentProducts)
                {
                    iBalance.Add(investmentsBalances.BookBalance);
                }

                guarantorLookUp.TotalShares = sBalance.Sum() + iBalance.Sum();
                //guarantorLookUp.TotalShares = await _channelService.ComputeEligibleLoanAppraisalInvestmentsBalanceAsync(id, LoanProductId, GetServiceHeader());

                guarantorLookUp.GuarantorFullName = loanGuarantor.FullName;
                guarantorLookUp.GuarantorId = loanGuarantor.Id;
                guarantorLookUp.GuarantorEmployerDescription = loanGuarantor.StationZoneDivisionEmployerDescription;
                guarantorLookUp.GuarantorStationDescription = loanGuarantor.StationDescription;
                guarantorLookUp.GuarantorSerialNumber = loanGuarantor.SerialNumber;
                guarantorLookUp.GuarantorIdentificationNumber = loanGuarantor.IdentificationNumber;
                guarantorLookUp.GuarantorPayrollNumber = loanGuarantor.IndividualPayrollNumbers;

                guarantorLookUp.AppraisalFactor = await _channelService.GetGuarantorAppraisalFactorAsync(LoanProductId, guarantorLookUp.TotalShares, GetServiceHeader());

                var findAnotherGuarantee = await _channelService.FindLoanGuarantorsByCustomerIdAsync(guarantorLookUp.GuarantorId, GetServiceHeader());

                var totalAmountsGuaranteed = new ObservableCollection<decimal>();
                foreach (var sum in findAnotherGuarantee)
                {
                    totalAmountsGuaranteed.Add(sum.AmountGuaranteed);
                }
                decimal totalSum = totalAmountsGuaranteed.Sum();

                guarantorLookUp.CommittedShares = totalSum;
                //guarantorLookUp.CommittedShares = findAnotherGuarantee.Where(x => x.Status == (int)LoanGuarantorStatus.Attached && 
                //x.LoanCaseStatus.In(new int[] { (int)LoanCaseStatus.Audited, (int)LoanCaseStatus.Disbursed })).Sum(x => x.AmountGuaranteed);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        GuarantorFullName = guarantorLookUp.GuarantorFullName,
                        GuarantorId = guarantorLookUp.GuarantorId,
                        GuarantorEmployerDescription = guarantorLookUp.GuarantorEmployerDescription,
                        GuarantorStationDescription = guarantorLookUp.GuarantorStationDescription,
                        GuarantorSerialNumber = guarantorLookUp.GuarantorSerialNumber,
                        GuarantorIdentificationNumber = guarantorLookUp.GuarantorIdentificationNumber,
                        GuarantorPayrollNumber = guarantorLookUp.GuarantorPayrollNumber,
                        AppraisalFactor = guarantorLookUp.AppraisalFactor,
                        CommittedShares = guarantorLookUp.CommittedShares,
                        TotalShares = guarantorLookUp.TotalShares
                    }
                });
            }

            return Json(new { success = false, message = "Customer not found" });
        }
        #endregion

        #region Find Actions
        [HttpPost]
        public async Task<JsonResult> LoanProductIndex(JQueryDataTablesModel jQueryDataTablesModel, int section, string sectionValue)
        {

            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanProductsByLoanProductSectionAndFilterInPageAsync(section, sectionValue, pageIndex, pageSize, GetServiceHeader());


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                var orderedPageCollection = pageCollectionInfo.PageCollection
                    .OrderByDescending(item => item.CreatedDate)
                    .ToList();

                return this.DataTablesJson(
                    items: orderedPageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<LoanProductDTO> { },
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }

        [HttpPost]
        public async Task<JsonResult> LoanPurposeIndex(JQueryDataTablesModel jQueryDataTablesModel)
        {

            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanPurposesByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, pageSize, GetServiceHeader());


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                var orderedPageCollection = pageCollectionInfo.PageCollection
                    .OrderByDescending(item => item.CreatedDate)
                    .ToList();

                return this.DataTablesJson(
                    items: orderedPageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<LoanPurposeDTO> { },
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }

        [HttpPost]
        public async Task<JsonResult> SavingsProductIndex(JQueryDataTablesModel jQueryDataTablesModel)
        {

            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindSavingsProductsByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, pageSize, GetServiceHeader());


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                var orderedPageCollection = pageCollectionInfo.PageCollection
                    .OrderByDescending(item => item.CreatedDate)
                    .ToList();

                return this.DataTablesJson(
                    items: orderedPageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<SavingsProductDTO> { },
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }

        [HttpPost]
        public async Task<JsonResult> RegistrationRemarksIndex(JQueryDataTablesModel jQueryDataTablesModel)
        {

            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoaningRemarksByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, pageSize, GetServiceHeader());


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                var orderedPageCollection = pageCollectionInfo.PageCollection
                    .OrderByDescending(item => item.CreatedDate)
                    .ToList();

                return this.DataTablesJson(
                    items: orderedPageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<LoaningRemarkDTO> { },
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }

        [HttpPost]
        public async Task<JsonResult> CustomerIndex(JQueryDataTablesModel jQueryDataTablesModel, int recordStatus, string text, int customerFilter)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;


            var pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)RecordStatus.Approved, text, customerFilter, 0, int.MaxValue, GetServiceHeader());


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(customer => customer.CreatedDate)
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
                items: new List<CustomerDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }


        [HttpPost]
        public async Task<JsonResult> GuarantorIndex(JQueryDataTablesModel jQueryDataTablesModel, int grecordStatus, string gtext, int gcustomerFilter)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;


            var pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)RecordStatus.Approved, gtext, gcustomerFilter, 0, int.MaxValue, GetServiceHeader());


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(customer => customer.CreatedDate)
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
                items: new List<CustomerDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }
        #endregion

        [HttpPost]
        public async Task<ActionResult> Add(LoanCaseDTO loancaseDTO)
        {
            await ServeNavigationMenus();

            if (loancaseDTO.LoanProductId == Guid.Empty || loancaseDTO.LoanProductDescription == string.Empty || loancaseDTO.loanProductSection == "")
            {
                TempData["EmptyLoanProduct"] = "Loan Product required to proceed to add Guarantors!";

                return Json(new
                {
                    success = false
                });
            }

            if (loancaseDTO.Guarantor[0].AmountGuaranteed <= 0)
            {
                return Json(new { success = false, message = "Amount Guaranteed cannot be 0 or equal to 0!" });
            }

            var loanguarantorsDTOs = Session["loanguarantorsDTOs"] as ObservableCollection<LoanGuarantorDTO>;

            if (loanguarantorsDTOs == null)
            {
                loanguarantorsDTOs = new ObservableCollection<LoanGuarantorDTO>();
            }

            var totalGuarantorsCount = loanguarantorsDTOs.Count;

            foreach (var guarantorDTO in loancaseDTO.Guarantor)
            {
                var existingEntry = loanguarantorsDTOs.FirstOrDefault(e => e.GuarantorId == guarantorDTO.GuarantorId);

                if (existingEntry != null)
                {
                    return Json(new { success = false, message = "The selected Customer has already been added to the guarantors list!" });
                }

                var loanProductDetails = await _channelService.FindLoanProductAsync(loancaseDTO.LoanProductId, GetServiceHeader());
                var isSelfGuarantee = loanProductDetails.LoanRegistrationAllowSelfGuarantee;
                if (guarantorDTO.Id == loancaseDTO.CustomerId && !isSelfGuarantee)
                {
                    Session["loanguarantorsDTOs"] = null;
                    return Json(new { success = false, message = "The selected Loan Product does not allow self Guarantee!" });
                }

                var maximumGuarantees = loanProductDetails.LoanRegistrationMaximumGuarantees;

                if (loancaseDTO.Guarantor[0].AmountGuaranteed > (loancaseDTO.Guarantor[0].TotalShares - loancaseDTO.Guarantor[0].CommittedShares))
                {
                    TempData["AmountGuaranteedGreater"] = $"Amount Guaranteed must be less than or equal to Total Shares minus Committed Shares" +
                        $" ({loancaseDTO.Guarantor[0].TotalShares} - {loancaseDTO.Guarantor[0].CommittedShares} = " +
                        $"{loancaseDTO.Guarantor[0].TotalShares - loancaseDTO.Guarantor[0].CommittedShares}) and the number of Maximum Guarantees must not be exceeded!";
                    Session["loanguarantorsDTOs"] = null;
                    return Json(new { success = false, message = "Failed to add Loan Guarantor. Amount Guaranteed exceeded Total Shares." });
                }

                if (totalGuarantorsCount > maximumGuarantees)
                {
                    TempData["MaximumGuaranteedExceeded"] = "Maximum Guarantees must not be exceeded!";

                    return Json(new { success = false, message = "Failed to add Loan Guarantor. Amount Guaranteed exceeded Total Shares." });
                }

                loanguarantorsDTOs.Add(guarantorDTO);
            }

            Session["loanguarantorsDTOs"] = loanguarantorsDTOs;
            Session["guarantorDTO"] = loancaseDTO.Guarantor;

            return Json(new { success = true, entries = loanguarantorsDTOs });
        }

        [HttpPost]
        public async Task<JsonResult> Remove(Guid id)
        {
            await ServeNavigationMenus();

            var loanguarantorsDTOs = Session["loanguarantorsDTOs"] as ObservableCollection<LoanGuarantorDTO>;

            if (loanguarantorsDTOs != null)
            {
                var entryToRemove = loanguarantorsDTOs.FirstOrDefault(e => e.GuarantorId == id);
                if (entryToRemove != null)
                {
                    loanguarantorsDTOs.Remove(entryToRemove);

                    Session["loanguarantorsDTOs"] = loanguarantorsDTOs;
                }
            }

            return Json(new { success = true, data = loanguarantorsDTOs });
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());

            var loanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(id, GetServiceHeader());
            var loanCollaterals = await _channelService.FindLoanCollateralsByLoanCaseIdAsync(id, GetServiceHeader());

            ViewBag.LoanGuarantors = loanGuarantors;
            ViewBag.Collaterals = loanCollaterals;

            return View(loanCaseDTO);
        }

        #region Create
        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.CustomerFilter = GetCustomerFilterSelectList(string.Empty);

            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
            ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(string.Empty);

            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);

            ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(LoanCaseDTO loanCaseDTO, string collateralIds)
        {
            await ServeNavigationMenus();

            try
            {
                var loanProduct = await _channelService.FindLoanProductAsync(loanCaseDTO.LoanProductId, GetServiceHeader());
                var guarantorSecurityMode = loanProduct.LoanRegistrationGuarantorSecurityMode;

                var collateralIdList = collateralIds.Split(',').ToList();
                List<Guid> collateralGuidList = new List<Guid>();

                foreach (var collateralId in collateralIdList)
                {
                    if (Guid.TryParse(collateralId, out Guid collateralGuid))
                    {
                        collateralGuidList.Add(collateralGuid);
                    }
                }

                List<CustomerDocumentDTO> dc = new List<CustomerDocumentDTO>();

                foreach (var collateral in collateralGuidList)
                {
                    var document = await _channelService.FindCustomerDocumentAsync(collateral, GetServiceHeader());
                    if (document != null)
                    {
                        dc.Add(document);
                    }
                }
                ObservableCollection<CustomerDocumentDTO> collateralDocuments = new ObservableCollection<CustomerDocumentDTO>(dc);


                #region Check Guarantor Security Mode
                if (Session["loanguarantorsDTOs"] == null && loanProduct.LoanRegistrationMinimumGuarantors > 1)
                {
                    await ServeNavigationMenus();

                    ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                    ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                    ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());


                    ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                    ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                    ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());

                    TempData["noGuarantorsSubmit"] = $"A minimum of {loanProduct.LoanRegistrationMinimumGuarantors} guarantors is required for the selected Loan Product";
                    return View("Create", loanCaseDTO);
                }

                var guarantors = Session["loanguarantorsDTOs"] as ObservableCollection<LoanGuarantorDTO>;

                foreach (var guarantor in guarantors)
                {
                    guarantor.LoanProductId = loanCaseDTO.LoanProductId;
                    guarantor.LoaneeCustomerId = loanCaseDTO.CustomerId;
                    guarantor.CustomerId = guarantor.GuarantorId;

                    #region Commented Out Code
                    //var SelectedBranch = await _channelService.FindBranchAsync(loanCaseDTO.BranchId, GetServiceHeader());

                    //if (guarantorSecurityMode == (int)GuarantorSecurityMode.Income)
                    //{
                    //    var creditBatchEntries = await _channelService.FindLoanAppraisalCreditBatchEntriesByCustomerIdAsync(loanCaseDTO.CustomerId, loanCaseDTO.LoanProductId, true);

                    //    if (creditBatchEntries != null && creditBatchEntries.Any())
                    //    {
                    //        guarantor.AmountPledged = creditBatchEntries
                    //            .TakeLast(1)
                    //            .Where(x => x.CreditBatchStatus == (int)BatchStatus.Posted)
                    //            .Sum(y => y.Principal + y.Interest);
                    //    }

                    //    #region IFF employee, override latest income value

                    //    var paySlips = await _channelService.FindLoanAppraisalPaySlipsByCustomerIdAsync(loanCaseDTO.CustomerId, loanCaseDTO.LoanProductId, GetServiceHeader());

                    //    if (paySlips != null && paySlips.Any())
                    //    {
                    //        guarantor.AmountPledged = paySlips
                    //            .TakeLast(1)
                    //            .Where(x => x.SalaryPeriodStatus == (int)SalaryPeriodStatus.Closed)
                    //            .Sum(y => y.NetPay);
                    //    }

                    //    #endregion
                    //}
                    //else if (guarantorSecurityMode == (int)GuarantorSecurityMode.Investments)
                    //{
                    //    guarantor.CommittedShares = (SelectedBranch != null && SelectedBranch.CompanyTrackGuarantorCommittedInvestments
                    //        ? guarantor.CommittedShares
                    //        : 0m);

                    //    var availableAmount = guarantor.TotalShares - guarantor.CommittedShares;
                    //    availableAmount = (availableAmount * -1 < 0m) ? availableAmount : 0m;

                    //    if (loanCaseDTO.CustomerId == guarantor.CustomerId) /*self-guarantee so adjust available amount as per max eligible percentage*/
                    //    {
                    //        availableAmount = Math.Round(Convert.ToDecimal((loanProduct.LoanRegistrationMaximumSelfGuaranteeEligiblePercentage * Convert.ToDouble(availableAmount)) / 100), 4);
                    //        guarantor.AmountGuaranteed = availableAmount;
                    //    }
                    //    else
                    //    {
                    //        guarantor.AmountGuaranteed = availableAmount;
                    //    }
                    //}
                    #endregion
                }
                #endregion

                if (loanProduct.LoanRegistrationMinimumGuarantors > 0)
                {
                    if (guarantors.Count < loanProduct.LoanRegistrationMinimumGuarantors)
                    {
                        await ServeNavigationMenus();

                        ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                        ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                        ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());


                        ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                        ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                        ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());

                        TempData["lessGuarantors"] = $"The selected Loan Product requires a minimum of {loanProduct.LoanRegistrationMinimumGuarantors} guarantors and a maximum of " +
                            $"{loanProduct.LoanRegistrationMaximumGuarantees}.";
                        Session["loanguarantorsDTOs"] = null;
                        return View("Create", loanCaseDTO);
                    }
                }

                decimal totalAmountGuaranteed = guarantors.Sum(t => t.AmountGuaranteed);
                if (totalAmountGuaranteed < loanCaseDTO.AmountApplied)
                {
                    await ServeNavigationMenus();

                    ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                    ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                    ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());


                    ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                    ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                    ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());


                    TempData["totalAmountGuaranteed<AmountApplied"] = "The Total Amount Guaranteed does not fully secure the Applied Amount!";
                    Session["loanguarantorsDTOs"] = null;
                    return View("Create");
                }

                var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
                if (userDTO.BranchId != null)
                {
                    loanCaseDTO.BranchId = (Guid)userDTO.BranchId;
                }

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
                }

                loanCaseDTO.ValidateAll();


                if (loanCaseDTO.AmountApplied != 0 && loanCaseDTO.CustomerId != Guid.Empty && loanCaseDTO.LoanProductId != Guid.Empty && loanCaseDTO.SavingsProductId != Guid.Empty &&
                    loanCaseDTO.LoanPurposeId != Guid.Empty && loanCaseDTO.RegistrationRemarkId != Guid.Empty)
                {
                    var membershipPeriod = loanCaseDTO.LoanRegistrationMinimumMembershipPeriod;
                    var fullCustomerDetails = await _channelService.FindCustomerAsync(loanCaseDTO.CustomerId, GetServiceHeader());
                    var customerRegistrationDate = fullCustomerDetails.CreatedDate;
                    var currentDate = DateTime.Now;

                    int totalMonths = (currentDate.Year - customerRegistrationDate.Year) * 12
                          + currentDate.Month - customerRegistrationDate.Month;

                    if (totalMonths < membershipPeriod)
                    {
                        await ServeNavigationMenus();

                        ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                        ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                        ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());


                        ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                        ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                        ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());

                        TempData["lessMinimumMembershipPeriod"] = "The selected Member Registration Period is less than the minimum required to apply for the selected Loan Product.";
                        Session["loanguarantorsDTOs"] = null;
                        return View("Create");
                    }

                    var loanCase = await _channelService.AddLoanCaseAsync(loanCaseDTO, GetServiceHeader());

                    if (loanCase.ErrorMessageResult != null)
                    {
                        await ServeNavigationMenus();

                        ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                        ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                        ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());


                        ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                        ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                        ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());

                        TempData["ErrorMessageResult"] = loanCase.ErrorMessageResult;
                        Session["loanguarantorsDTOs"] = null;
                        return View("Create");
                    }

                    if (collateralIds != null || collateralIds != "" || collateralIds != string.Empty)
                        await _channelService.UpdateLoanCollateralsByLoanCaseIdAsync(loanCase.Id, collateralDocuments, GetServiceHeader());

                    if (guarantors != null)
                        await _channelService.UpdateLoanGuarantorsByLoanCaseIdAsync(loanCase.Id, guarantors, GetServiceHeader());

                    TempData["Success"] = "Operation Completed Successfully.";
                    Session["loanguarantorsDTOs"] = null;
                    return RedirectToAction("Index");
                }
                else
                {
                    await ServeNavigationMenus();

                    ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                    ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                    ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());

                    ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                    ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                    ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());


                    var errorMessages = loanCaseDTO.ErrorMessages;
                    string errorMessage = string.Join("\n", errorMessages.Where(msg => !string.IsNullOrWhiteSpace(msg)));

                    TempData["ErrorMessage"] = "Operation Unsuccessful: " + errorMessage;
                    Session["loanguarantorsDTOs"] = null;
                    return View(loanCaseDTO);
                }

            }
            catch (Exception ex)
            {
                TempData["Exception"] = ex.ToString();
                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());

                ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());
                Session["loanguarantorsDTOs"] = null;
                return View(loanCaseDTO);
            }
        }
        #endregion
    }
}