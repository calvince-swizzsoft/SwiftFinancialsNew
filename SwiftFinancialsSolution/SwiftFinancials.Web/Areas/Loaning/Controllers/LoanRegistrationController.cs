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

        [HttpPost]
        public async Task<ActionResult> ExportToExcel(JQueryDataTablesModel jQueryDataTablesModel)
        {
            if (jQueryDataTablesModel.sEcho <= 0)
            {
                throw new InvalidOperationException("Expected the request to have a sEcho value greater than 0");
            }

            // Retrieve the data using the same service call as in the Index action
            var pageCollectionInfo = await _channelService.FindLoanCasesByFilterInPageAsync(
                jQueryDataTablesModel.sSearch,
                10,
                jQueryDataTablesModel.iDisplayStart,
                jQueryDataTablesModel.iDisplayLength,
                true,
                GetServiceHeader()
            );

            var page = pageCollectionInfo.PageCollection;

            // Generate the Excel file from the pageCollection
            byte[] fileContent = GenerateExcelFile(page);

            // Return the Excel file as a downloadable file
            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LoanCases.xlsx");
        }

        private byte[] GenerateExcelFile(IEnumerable<LoanCaseDTO> loanCases)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Registered Loans");

                // Load the logo image
                var logoPath = Server.MapPath("~/Images/MIA.JPG");
                var logo = Image.FromFile(logoPath);

                // Adjust the height of the first row to create space for the logo
                worksheet.Row(1).Height = 150; // Adjust the row height to fit the logo size

                // Insert the logo into the worksheet
                var picture = worksheet.Drawings.AddPicture("Flow Finance", logo);
                picture.SetPosition(0, 0, 0, 0); // Position the image in the top-left corner (row 1, column 1)

                // Start the headers after the logo
                int headerRowStart = 5; // Adjust this if the logo is larger or smaller


                var headers = new[] { "CASE NUMBER", "CUSTOMER NAME", "AMOUNT APPLIED", "BRANCH", "PAYMENT PER PERIOD", "RECEIVED DATE", "CREATED DATE" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cells[headerRowStart, i + 1];
                    cell.Value = headers[i];

                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Blue);
                    cell.Style.Font.Color.SetColor(System.Drawing.Color.White);

                    cell.Style.Font.Bold = true;
                }

                int row = headerRowStart + 1;
                foreach (var loanCase in loanCases)
                {
                    worksheet.Cells[row, 1].Value = loanCase.CaseNumber;
                    worksheet.Cells[row, 2].Value = loanCase.CustomerName;
                    worksheet.Cells[row, 3].Value = loanCase.AmountApplied;
                    worksheet.Cells[row, 4].Value = loanCase.BranchDescription;
                    worksheet.Cells[row, 5].Value = loanCase.PaymentPerPeriod;
                    worksheet.Cells[row, 6].Value = loanCase.ReceivedDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 7].Value = loanCase.CreatedDate.ToString("yyyy-MM-dd");
                    // Add more fields as needed

                    row++;
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            }
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
                        allStandingOrders.AddRange(standingOrders); // Add standing orders to the collection
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
                foreach (var collateral in collaterals)
                {
                    if (collateral.CreatedDate != null) 
                    {
                        collateral.CreatedDate = Convert.ToDateTime(collateral.CreatedDate);
                        collateral.ModifiedDate = Convert.ToDateTime(collateral.ModifiedDate);
                    }
                }

                var investmentsBalance = await _channelService.ComputeEligibleLoanAppraisalInvestmentsBalanceAsync(loanCaseDTO.CustomerId, loanCaseDTO.LoanProductId, GetServiceHeader());
                loanCaseDTO.LoanProductInvestmentsBalance = investmentsBalance;

                // Latest Income
                var latestIncome = await _channelService.FindLoanAppraisalCreditBatchEntriesByCustomerIdAsync(loanCaseDTO.CustomerId, loanCaseDTO.LoanProductId, true, GetServiceHeader());
                loanCaseDTO.LoanProductLatestIncome = latestIncome.Sum(x => x.Balance);

                // loanBalance
                var findCustomerLoanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(loanCaseDTO.CustomerId, loanCaseDTO.LoanProductId,
                    true, true, true, true, GetServiceHeader());
                var bookBalanceTotal = findCustomerLoanAccounts.Sum(q => q.BookBalance);
                var carryForwardsTotal = findCustomerLoanAccounts.Sum(c => c.CarryForwardsBalance);
                var LoanBalance = bookBalanceTotal + carryForwardsTotal;
                loanCaseDTO.LoanProductLoanBalance = LoanBalance;

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
                        Collaterals = collaterals,
                        PassportPhoto = loanCaseDTO.PassportPhoto != null ? Convert.ToBase64String(loanCaseDTO.PassportPhoto) : null,
                        SignaturePhoto = loanCaseDTO.SignaturePhoto != null ? Convert.ToBase64String(loanCaseDTO.SignaturePhoto) : null,
                        IDFront = loanCaseDTO.IDCardFrontPhoto != null ? Convert.ToBase64String(loanCaseDTO.IDCardFrontPhoto) : null,
                        IDBack = loanCaseDTO.IDCardBackPhoto != null ? Convert.ToBase64String(loanCaseDTO.IDCardBackPhoto) : null
                    }
                });
            }
            return Json(new { success = false, message = "Customer not found" });
        }

        public async Task<ActionResult> LoanPurposeLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
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

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
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

                Guid customerId = (Guid)Session["CustomerId"];
                var investmentsBalance = await _channelService.ComputeEligibleLoanAppraisalInvestmentsBalanceAsync(customerId, parseId, GetServiceHeader());

                var findloanBalanceCustomerAccount = await _channelService.FindCustomerAccountsByCustomerIdAndCustomerAccountTypeTargetProductIdAsync(customerId, parseId, true, true, true, true,
                    GetServiceHeader());
                var bookBal = findloanBalanceCustomerAccount.Sum(x => x.BookBalance);
                var carryForward = findloanBalanceCustomerAccount.Sum(u => u.CarryForwardsBalance);
                var loanBalance = bookBal + carryForward;

                var latestIncome = await _channelService.FindLoanAppraisalCreditBatchEntriesByCustomerIdAsync(customerId, parseId, true, GetServiceHeader());

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
                        LoanBalance = loanBalance
                    }
                });
            }
            return Json(new { success = false, message = "Loan Product not found" });
        }

        public async Task<ActionResult> SavingsProductLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

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



        // Find Actions....
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
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)RecordStatus.Approved, text, customerFilter, pageIndex, pageSize, GetServiceHeader());


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
                    items: new List<CustomerDTO> { },
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }


        [HttpPost]
        public async Task<ActionResult> Create(LoanCaseDTO loanCaseDTO, string collateralIds)
        {
            await ServeNavigationMenus();

            if (string.IsNullOrEmpty(collateralIds))
            {
                string message = string.Format("No Collaterals Attached for this Loanee. Do you want to proceed?");

                DialogResult result = MessageBox.Show(
                    message,
                    "Loan Registration",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );


                if (result == DialogResult.Yes)
                {
                    // Proceed
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


                    MessageBox.Show(Form.ActiveForm, "Operation cancelled.", "Loan Registration", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification);
                    return View(loanCaseDTO);
                }
            }


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



            var takeBranchId = Guid.Empty;

            var findBranch = await _channelService.FindBranchesAsync(GetServiceHeader());
            foreach (var id in findBranch)
            {
                takeBranchId = id.Id;
            }

            loanCaseDTO.BranchId = takeBranchId;

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

            try
            {
                if (!loanCaseDTO.HasErrors)
                {
                    try
                    {
                        // Check member's period to validate if qualify to apply for the product
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


                            MessageBox.Show(Form.ActiveForm, "The selected Member's Registration Period is less than the minimum required to apply for the selected Loan Product.", 
                                "Loan Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                            return View(loanCaseDTO);
                        }


                        string message = string.Format(
                                            "Do you want to proceed with loan application for\n{0}",
                                            loanCaseDTO.CustomerIndividualFirstName + "?"
                                        );

                        // Show the message box with Yes/No options
                        DialogResult result = MessageBox.Show(
                            message,
                            "Loan Registration",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.ServiceNotification
                        );


                        if (result == DialogResult.Yes)
                        {

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

                                MessageBox.Show(Form.ActiveForm, loanCase.ErrorMessageResult, "Loan Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                                return View();
                            }

                            await _channelService.UpdateLoanCollateralsByLoanCaseIdAsync(loanCase.Id, collateralDocuments, GetServiceHeader());

                            MessageBox.Show(Form.ActiveForm, "Operation completed successfully.", "Loan Registration", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            await ServeNavigationMenus();

                            var errorMessages = loanCaseDTO.ErrorMessages;
                            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                            ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                            ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());


                            ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                            ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                            ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());

                            MessageBox.Show(Form.ActiveForm, "Operation cancelled.", "Loan Registration", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                            return View(loanCaseDTO);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(Form.ActiveForm, ex.ToString(), "Loan Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                        return View(loanCaseDTO);
                    }

                }
                else
                {
                    await ServeNavigationMenus();

                    var errorMessages = loanCaseDTO.ErrorMessages;
                    ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                    ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                    ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());

                    ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                    ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                    ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());


                    MessageBox.Show(Form.ActiveForm, "Operation failed.", "Loan Registration", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return View(loanCaseDTO);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(Form.ActiveForm, ex.ToString(), "Loan Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());

                ViewBag.recordStatus = GetRecordStatusSelectList(loanCaseDTO.RecordStatusDescription.ToString());
                ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
                ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(loanCaseDTO.LoanRegistrationLoanProductSectionDescription.ToString());


                return View(loanCaseDTO);
            }
        }
    }
}