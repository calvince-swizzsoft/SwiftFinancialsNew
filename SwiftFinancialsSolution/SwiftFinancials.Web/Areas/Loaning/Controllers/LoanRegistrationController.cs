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
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class LoanRegistrationController : MasterController
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

            var pageCollectionInfo = await _channelService.FindLoanCasesByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iColumns, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());

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

            LoanCaseDTO loanCaseDTO = new LoanCaseDTO();

            if (cusomerEditDTO != null)
            {
                //Loanee
                loanCaseDTO.CustomerId = cusomerEditDTO.Id;
                loanCaseDTO.CustomerLoaneeFullName = cusomerEditDTO.CustomerFullName;
                loanCaseDTO.AmountApplied = cusomerEditDTO.AmountApplied;
                //loanCaseDTO.BranchDescription = cusomerEditDTO.BranchDescription;
                loanCaseDTO.CustomerReference1 = cusomerEditDTO.CustomerReference1;
                loanCaseDTO.CustomerReference2 = cusomerEditDTO.CustomerReference2;
                loanCaseDTO.CustomerReference3 = cusomerEditDTO.CustomerReference3;
                loanCaseDTO.SavingsProductDescription = cusomerEditDTO.SavingsProductDescription;
                //loanCaseDTO.LoanPurposeDescription = cusomerEditDTO.LoanPurposeDescription;
                loanCaseDTO.Remarks = cusomerEditDTO.Remarks;
                loanCaseDTO.MaximumAmountPercentage = cusomerEditDTO.MaximumAmountPercentage;
                loanCaseDTO.LoanRegistrationTermInMonths = cusomerEditDTO.LoanRegistrationTermInMonths;
                loanCaseDTO.LoanRegistrationMaximumAmount = cusomerEditDTO.LoanRegistrationMaximumAmount;
                loanCaseDTO.LoanInterestAnnualPercentageRate = cusomerEditDTO.LoanInterestAnnualPercentageRate;
            }

            return View(loanCaseDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(LoanCaseDTO loanCaseDTO)
        {
            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {
                await _channelService.UpdateLoanCaseAsync(loanCaseDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanCaseDTO.ErrorMessages;

                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());


                return View(loanCaseDTO);
            }
        }


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

            if (Session["BranchId"] != null)
            {
                loanCaseDTO.BranchId = (Guid)Session["BranchId"]; ;
                loanCaseDTO.BranchDescription = Session["BranchDescription"].ToString();
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

                //var standingOrders = await _channelService.FindStandingOrdersByBeneficiaryCustomerAccountIdAsync(parseId, true, GetServiceHeader());
                //if (standingOrders != null)
                //    ViewBag.StandingOrders = standingOrders;

                var loanapplications = await _channelService.FindLoanCasesByCustomerIdInProcessAsync(customer.Id, GetServiceHeader());
                if (loanapplications != null)
                    ViewBag.LoanApplications = loanapplications;
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

            if (Session["BranchId"] != null)
            {
                loanCaseDTO.BranchId = (Guid)Session["BranchId"]; ;
                loanCaseDTO.BranchDescription = Session["BranchDescription"].ToString();
            }



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

            if (Session["BranchId"] != null)
            {
                loanCaseDTO.BranchId = (Guid)Session["BranchId"]; ;
                loanCaseDTO.BranchDescription = Session["BranchDescription"].ToString();
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
            }

            if (Session["RegistrationRemarkId"] != null)
            {
                loanCaseDTO.RegistrationRemarkId = (Guid)Session["RegistrationRemarkId"];
                loanCaseDTO.Remarks = Session["Remarks"].ToString();
            }

            if (Session["BranchId"] != null)
            {
                loanCaseDTO.BranchId = (Guid)Session["BranchId"]; ;
                loanCaseDTO.BranchDescription = Session["BranchDescription"].ToString();
            }



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
            }

            if (Session["SavingsProductId"] != null)
            {
                loanCaseDTO.SavingsProductId = (Guid)Session["SavingsProductId"];
                loanCaseDTO.SavingsProductDescription = Session["SavingsProductDescription"].ToString();
            }

            if (Session["BranchId"] != null)
            {
                loanCaseDTO.BranchId = (Guid)Session["BranchId"]; ;
                loanCaseDTO.BranchDescription = Session["BranchDescription"].ToString();
            }


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



        public async Task<ActionResult> BranchesLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

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



            var branch = await _channelService.FindBranchAsync(parseId, GetServiceHeader());
            if (branch != null)
            {
                loanCaseDTO.BranchId = branch.Id;
                loanCaseDTO.BranchDescription = branch.Description;

                Session["BranchId"] = loanCaseDTO.BranchId;
                Session["BranchDescription"] = loanCaseDTO.BranchDescription;
            }

            return View("Create", loanCaseDTO);
        }






        [HttpPost]
        public async Task<ActionResult> Create(LoanCaseDTO loanCaseDTO)
        {
            if (Session["CustomerId"] == null || Session["LoanPurposeId"] == null || Session["LoanProductId"] == null ||
                Session["SavingsProductId"] == null || Session["RegistrationRemarkId"] == null || Session["BranchId"] == null)
            {
                TempData["EmptyData"] = "Could not create Loan Case. Kindly make sure to provide all the required details !";

                return View();
            }

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

            // Savings Product
            loanCaseDTO.SavingsProductId = (Guid)Session["SavingsProductId"];
            loanCaseDTO.SavingsProductDescription = Session["SavingsProductDescription"].ToString();

            // Loaning Remarks
            loanCaseDTO.RegistrationRemarkId = (Guid)Session["RegistrationRemarkId"];
            loanCaseDTO.Remarks = Session["Remarks"].ToString();

            // Branch
            loanCaseDTO.BranchId = (Guid)Session["BranchId"]; ;
            loanCaseDTO.BranchDescription = Session["BranchDescription"].ToString();

            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {
                var loanCase = await _channelService.AddLoanCaseAsync(loanCaseDTO, GetServiceHeader());

                Session.Clear();

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

    }
}