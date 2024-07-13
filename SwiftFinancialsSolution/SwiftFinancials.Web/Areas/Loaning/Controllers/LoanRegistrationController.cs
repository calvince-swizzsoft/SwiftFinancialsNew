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


        [HttpPost]
        public async Task<ActionResult> Create(LoanCaseDTO loanCaseDTO)
        {
            var receiveddate = Request["receiveddate"];

            loanCaseDTO.ReceivedDate = DateTime.ParseExact(Request["receiveddate"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {
                var loanCase = await _channelService.AddLoanCaseAsync(loanCaseDTO, GetServiceHeader());

                TempData["AlertMessage"] = "Loan registration successful.";

                Session["AmountApplied"] = loanCaseDTO.AmountApplied;

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanCaseDTO.ErrorMessages;
                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());

                TempData["AlertMessage"] = "Loan registration failed " + Convert.ToString(errorMessages);

                return View(loanCaseDTO);
            }
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
                return View();
            }

            if (Session["loanPurposeId"] != null)
            {
                loanCaseDTO.LoanPurposeId = (Guid)Session["loanPurposeId"];
                loanCaseDTO.LoanPurposeDescription = Session["loanDescription"].ToString();
            }

            if (Session["loanProductId"] != null)
            {
                loanCaseDTO.LoanProductId = (Guid)Session["loanProductId"];
                loanCaseDTO.LoanProductDescription = Session["loanProductDescription"].ToString();

                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["interestCalculationMode"].ToString());
                loanCaseDTO.LoanInterestAnnualPercentageRate = Convert.ToDouble(Session["annualPercentageRate"].ToString());
            }

            if (Session["savingsProductId"] != null)
            {
                loanCaseDTO.SavingsProductId = (Guid)Session["savingsProductId"];
                loanCaseDTO.SavingsProductDescription = Session["savingsProductDescription"].ToString();
            }

            if (Session["remarkId"] != null)
            {
                loanCaseDTO.RegistrationRemarkId = (Guid)Session["remarkId"];
                loanCaseDTO.Remarks = Session["remarkDescription"].ToString();
            }


            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            if (customer != null)
            {
                //Loanee
                loanCaseDTO.CustomerId = customer.Id;

                loanCaseDTO.CustomerIndividualFirstName = customer.IndividualFirstName;
                loanCaseDTO.CustomerIndividualLastName = customer.IndividualLastName;

                loanCaseDTO.CustomerName = loanCaseDTO.CustomerIndividualFirstName + " " + loanCaseDTO.CustomerIndividualLastName;

                loanCaseDTO.CustomerReference2 = customer.Reference2;
                loanCaseDTO.CustomerReference1 = customer.Reference1;
                loanCaseDTO.CustomerIndividualIdentityCardNumber = customer.IdentificationNumber;
                loanCaseDTO.CustomerReference3 = customer.Reference3;

                loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                loanCaseDTO.CustomerStation = customer.StationDescription;

                Session["loaneeId"] = loanCaseDTO.CustomerId;
                Session["loaneeName"] = loanCaseDTO.CustomerName;
                Session["ref2"] = loanCaseDTO.CustomerReference2;
                Session["ref1"] = loanCaseDTO.CustomerReference1;
                Session["IDNo"] = loanCaseDTO.CustomerIndividualIdentityCardNumber;
                Session["ref3"] = loanCaseDTO.CustomerReference3;
                Session["employer"] = loanCaseDTO.CustomerStationZoneDivisionEmployerDescription;
                Session["station"] = loanCaseDTO.CustomerStation;
            }

            return View("Create", loanCaseDTO);
        }


        public async Task<ActionResult> LoanPurposeLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            if (Session["loaneeId"] != null)
            {
                loanCaseDTO.CustomerId = (Guid)Session["loaneeId"];
                loanCaseDTO.CustomerName = Session["loaneeName"].ToString();
                loanCaseDTO.CustomerReference2 = Session["ref2"].ToString();
                loanCaseDTO.CustomerReference1 = Session["ref1"].ToString();
                loanCaseDTO.CustomerIndividualIdentityCardNumber = Session["IDNo"].ToString();
                loanCaseDTO.CustomerReference3 = Session["ref3"].ToString();
                loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = Session["employer"].ToString();
                loanCaseDTO.CustomerStation = Session["station"].ToString();
            }

            if (Session["loanProductId"] != null)
            {
                loanCaseDTO.LoanProductId = (Guid)Session["loanProductId"];
                loanCaseDTO.LoanProductDescription = Session["loanProductDescription"].ToString();

                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["interestCalculationMode"].ToString());
                loanCaseDTO.LoanInterestAnnualPercentageRate = Convert.ToDouble(Session["annualPercentageRate"].ToString());
            }

            if (Session["savingsProductId"] != null)
            {
                loanCaseDTO.SavingsProductId = (Guid)Session["savingsProductId"];
                loanCaseDTO.SavingsProductDescription = Session["savingsProductDescription"].ToString();
            }

            if (Session["remarkId"] != null)
            {
                loanCaseDTO.RegistrationRemarkId = (Guid)Session["remarkId"];
                loanCaseDTO.Remarks = Session["remarkDescription"].ToString();
            }



            var loanPurpose = await _channelService.FindLoanPurposeAsync(parseId, GetServiceHeader());
            if (loanPurpose != null)
            {
                loanCaseDTO.LoanPurposeId = loanPurpose.Id;
                loanCaseDTO.LoanPurposeDescription = loanPurpose.Description;

                Session["loanPurposeId"] = loanCaseDTO.LoanPurposeId;
                Session["loanDescription"] = loanCaseDTO.LoanPurposeDescription;
            }

            return View("create", loanCaseDTO);
        }


        public async Task<ActionResult> LoanProductLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            if (Session["loaneeId"] != null)
            {
                loanCaseDTO.CustomerId = (Guid)Session["loaneeId"];
                loanCaseDTO.CustomerName = Session["loaneeName"].ToString();
                loanCaseDTO.CustomerReference2 = Session["ref2"].ToString();
                loanCaseDTO.CustomerReference1 = Session["ref1"].ToString();
                loanCaseDTO.CustomerIndividualIdentityCardNumber = Session["IDNo"].ToString();
                loanCaseDTO.CustomerReference3 = Session["ref3"].ToString();
                loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = Session["employer"].ToString();
                loanCaseDTO.CustomerStation = Session["station"].ToString();
            }

            if (Session["loanPurposeId"] != null)
            {
                loanCaseDTO.LoanPurposeId = (Guid)Session["loanPurposeId"];
                loanCaseDTO.LoanPurposeDescription = Session["loanDescription"].ToString();
            }

            if (Session["savingsProductId"] != null)
            {
                loanCaseDTO.SavingsProductId = (Guid)Session["savingsProductId"];
                loanCaseDTO.SavingsProductDescription = Session["savingsProductDescription"].ToString();
            }

            if (Session["remarkId"] != null)
            {
                loanCaseDTO.RegistrationRemarkId = (Guid)Session["remarkId"];
                loanCaseDTO.Remarks = Session["remarkDescription"].ToString();
            }


            var loanProduct = await _channelService.FindLoanProductAsync(parseId, GetServiceHeader());
            if (loanProduct != null)
            {
                loanCaseDTO.LoanProductId = loanProduct.Id;
                loanCaseDTO.LoanProductDescription = loanProduct.Description;
                loanCaseDTO.LoanInterestCalculationMode = loanProduct.LoanInterestCalculationMode;
                loanCaseDTO.LoanInterestAnnualPercentageRate = loanProduct.LoanInterestAnnualPercentageRate;

                Session["loanProductId"] = loanCaseDTO.LoanProductId;
                Session["loanProductDescription"] = loanCaseDTO.LoanProductDescription;

                Session["interestCalculationMode"] = loanCaseDTO.LoanInterestCalculationMode;
                Session["annualPercentageRate"] = loanCaseDTO.LoanInterestAnnualPercentageRate;
            }

            return View("create", loanCaseDTO);
        }


        public async Task<ActionResult> SavingsProductLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            if (Session["loaneeId"] != null)
            {
                loanCaseDTO.CustomerId = (Guid)Session["loaneeId"];
                loanCaseDTO.CustomerName = Session["loaneeName"].ToString();
                loanCaseDTO.CustomerReference2 = Session["ref2"].ToString();
                loanCaseDTO.CustomerReference1 = Session["ref1"].ToString();
                loanCaseDTO.CustomerIndividualIdentityCardNumber = Session["IDNo"].ToString();
                loanCaseDTO.CustomerReference3 = Session["ref3"].ToString();
                loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = Session["employer"].ToString();
                loanCaseDTO.CustomerStation = Session["station"].ToString();
            }

            if (Session["loanPurposeId"] != null)
            {
                loanCaseDTO.LoanPurposeId = (Guid)Session["loanPurposeId"];
                loanCaseDTO.LoanPurposeDescription = Session["loanDescription"].ToString();
            }

            if (Session["loanProductId"] != null)
            {
                loanCaseDTO.LoanProductId = (Guid)Session["loanProductId"];
                loanCaseDTO.LoanProductDescription = Session["loanProductDescription"].ToString();

                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["interestCalculationMode"].ToString());
                loanCaseDTO.LoanInterestAnnualPercentageRate = Convert.ToDouble(Session["annualPercentageRate"].ToString());
            }

            if (Session["remarkId"] != null)
            {
                loanCaseDTO.RegistrationRemarkId = (Guid)Session["remarkId"];
                loanCaseDTO.Remarks = Session["remarkDescription"].ToString();
            }

            var savingsProduct = await _channelService.FindSavingsProductAsync(parseId, GetServiceHeader());
            if (savingsProduct != null)
            {
                loanCaseDTO.SavingsProductId = savingsProduct.Id;
                loanCaseDTO.SavingsProductDescription = savingsProduct.Description;

                Session["savingsProductId"] = loanCaseDTO.SavingsProductId;
                Session["savingsProductDescription"] = loanCaseDTO.SavingsProductDescription;
            }

            return View("create", loanCaseDTO);
        }


        public async Task<ActionResult> LoaningRemarksLookup(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            if (Session["loaneeId"] != null)
            {
                loanCaseDTO.CustomerId = (Guid)Session["loaneeId"];
                loanCaseDTO.CustomerName = Session["loaneeName"].ToString();
                loanCaseDTO.CustomerReference2 = Session["ref2"].ToString();
                loanCaseDTO.CustomerReference1 = Session["ref1"].ToString();
                loanCaseDTO.CustomerIndividualIdentityCardNumber = Session["IDNo"].ToString();
                loanCaseDTO.CustomerReference3 = Session["ref3"].ToString();
                loanCaseDTO.CustomerStationZoneDivisionEmployerDescription = Session["employer"].ToString();
                loanCaseDTO.CustomerStation = Session["station"].ToString();
            }

            if (Session["loanPurposeId"] != null)
            {
                loanCaseDTO.LoanPurposeId = (Guid)Session["loanPurposeId"];
                loanCaseDTO.LoanPurposeDescription = Session["loanDescription"].ToString();
            }

            if (Session["loanProductId"] != null)
            {
                loanCaseDTO.LoanProductId = (Guid)Session["loanProductId"];
                loanCaseDTO.LoanProductDescription = Session["loanProductDescription"].ToString();

                loanCaseDTO.LoanInterestCalculationMode = Convert.ToInt32(Session["interestCalculationMode"].ToString());
                loanCaseDTO.LoanInterestAnnualPercentageRate = Convert.ToDouble(Session["annualPercentageRate"].ToString());
            }

            if (Session["savingsProductId"] != null)
            {
                loanCaseDTO.SavingsProductId = (Guid)Session["savingsProductId"];
                loanCaseDTO.SavingsProductDescription = Session["savingsProductDescription"].ToString();
            }


            var loanRemark = await _channelService.FindLoaningRemarkAsync(parseId, GetServiceHeader());
            if (loanRemark != null)
            {
                loanCaseDTO.RegistrationRemarkId = loanRemark.Id;
                loanCaseDTO.Remarks = loanRemark.Description;

                Session["remarkId"] = loanCaseDTO.RegistrationRemarkId;
                Session["remarkDescription"] = loanCaseDTO.Remarks;
            }

            return View("create", loanCaseDTO);
        }



        public async Task<ActionResult> GuarantorLookUp(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var guarantor = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            if (guarantor != null)
            {
                loanCaseDTO.GuarantorId = guarantor.Id;
                loanCaseDTO.GuarantorName = guarantor.IndividualFirstName + " " + guarantor.IndividualLastName;
                loanCaseDTO.GuarantorEmployerId = (Guid)guarantor.StationZoneDivisionEmployerId;
                loanCaseDTO.GuarantorEmployerDescription = guarantor.StationZoneDivisionEmployerDescription;
                loanCaseDTO.GuarantorStationId = (Guid)guarantor.StationId;
                loanCaseDTO.GuarantorStationDescription = guarantor.StationDescription;
                loanCaseDTO.GuarantorIdentificationNumber = guarantor.IdentificationNumber;
                loanCaseDTO.GuarantorReference1 = guarantor.Reference1;
                loanCaseDTO.GuarantorReference2 = guarantor.Reference2;
                loanCaseDTO.GuarantorReference3 = guarantor.Reference3;

                Session["GuarantorId"] = loanCaseDTO.GuarantorId;
                Session["GuarantorName"] = loanCaseDTO.GuarantorName;
                Session["GuarantorEmployerId"] = loanCaseDTO.GuarantorEmployerId;
                Session["GuarantorEmployerDescription"] = loanCaseDTO.GuarantorEmployerDescription;
                Session["GuarantorStationId"] = loanCaseDTO.GuarantorStationId;
                Session["GuarantorStationDescription"] = loanCaseDTO.GuarantorStationDescription;
                Session["GuarantorIdentificationNumber"] = loanCaseDTO.GuarantorIdentificationNumber;
                Session["GuarantorReference1"] = loanCaseDTO.GuarantorReference1;
                Session["GuarantorReference2"] = loanCaseDTO.GuarantorReference2;
                Session["GuarantorReference3"] = loanCaseDTO.GuarantorReference3;

            }

            return View("create", loanCaseDTO);
        }
    }
}