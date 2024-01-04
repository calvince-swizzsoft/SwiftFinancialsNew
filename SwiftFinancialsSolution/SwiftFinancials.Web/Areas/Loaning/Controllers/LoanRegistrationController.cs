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

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
            ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(string.Empty);
            ViewBag.LoanGuarantorDTOs = null;

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            var loanProduct = await _channelService.FindLoanProductsAsync(GetServiceHeader());

            //await GetLoanProducts(id);

            //LoanProductDTO loanProductDTO = new LoanProductDTO();

            LoanCaseDTO loanCaseDTO = new LoanCaseDTO();
            if (customer != null)
            {
                loanCaseDTO.CustomerId = customer.Id;
                loanCaseDTO.CustomerIndividualFirstName = customer.IndividualFirstName;
                loanCaseDTO.CustomerIndividualLastName = customer.IndividualLastName;
                loanCaseDTO.CustomerReference2 = customer.Reference2;
                loanCaseDTO.CustomerReference1 = customer.Reference1;
                loanCaseDTO.CustomerIndividualIdentityCardNumber = customer.IdentificationNumber;
                //loanCaseDTO.Employers[0].Description = customer.StationZoneDivisionEmployerDescription;
                loanCaseDTO.CustomerReference3 = customer.Reference3;
            }

            return View(loanCaseDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(LoanCaseDTO loanCaseDTO)
        {
            var receiveddate = Request["receiveddate"];

            loanCaseDTO.ReceivedDate = DateTime.ParseExact(Request["receiveddate"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
            loanCaseDTO.AmountApplied = 4000;

            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {
                var loanCase = await _channelService.AddLoanCaseAsync(loanCaseDTO, GetServiceHeader());

                if (loanCase != null)
                {
                    var loanGuarantors = new ObservableCollection<LoanGuarantorDTO>();

                    foreach (var loanGuarantorDTO in loanCaseDTO.LoanGuarantors)
                    {
                        loanGuarantorDTO.LoanCaseId = loanCase.Id;
                        loanGuarantorDTO.LoaneeCustomerId = loanGuarantorDTO.LoaneeCustomerId;
                        loanGuarantorDTO.LoanCaseId = loanCase.Id;
                        loanGuarantorDTO.CustomerIndividualIdentityCardNumber = loanGuarantorDTO.CustomerIndividualIdentityCardNumber;
                        loanGuarantorDTO.AmountGuaranteed = loanGuarantorDTO.AmountGuaranteed;
                        loanGuarantorDTO.AppraisalFactor = loanGuarantorDTO.AppraisalFactor;
                        loanGuarantorDTO.CommittedShares = loanGuarantorDTO.CommittedShares;
                        loanGuarantorDTO.CustomerId = loanGuarantorDTO.CustomerId;
                        loanGuarantors.Add(loanGuarantorDTO);
                    };

                    if (loanGuarantors.Any())
                        await _channelService.UpdateLoanGuarantorsByLoanCaseIdAsync(loanCase.Id, loanGuarantors, GetServiceHeader());
                }

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


        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());
            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
            ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(string.Empty);

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

        public async Task<ActionResult> Appraise(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());

            ViewBag.LoanAppraisalOptionSelectList = GetLoanAppraisalOptionSelectList(string.Empty);

            return View(loanCaseDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Appraise(LoanCaseDTO loanCaseDTO)
        {
            var loanAppraisalOption = loanCaseDTO.LoanAppraisalOption;

            loanCaseDTO.ValidateAll();
            if (!loanCaseDTO.HasErrors)
            {
                await _channelService.AppraiseLoanCaseAsync(loanCaseDTO, loanAppraisalOption, 1, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanCaseDTO.ErrorMessages;
                ViewBag.LoanAppraisalOptionSelectList = GetLoanAppraisalOptionSelectList(loanCaseDTO.LoanApprovalOption.ToString());
                return View(loanCaseDTO);
            }
        }

        public async Task<ActionResult> Approve(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());
            ViewBag.LoanApprovalOptionSelectList = GetLoanApprovalOptionSelectList(string.Empty);

            return View(loanCaseDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approve(LoanCaseDTO loanCaseDTO)
        {
            var loanApprovalOption = loanCaseDTO.LoanApprovalOption;
            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {
                await _channelService.ApproveLoanCaseAsync(loanCaseDTO, loanApprovalOption, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanCaseDTO.ErrorMessages;
                ViewBag.LoanApprovalOptionSelectList = GetLoanApprovalOptionSelectList(loanCaseDTO.LoanApprovalOption.ToString());
                return View(loanCaseDTO);
            }
        }

        public async Task<ActionResult> Verify(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());
            ViewBag.LoanAuditOptionSelectList = GetLoanAuditOptionSelectList(string.Empty);

            return View(loanCaseDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(LoanCaseDTO loanCaseDTO)
        {
            var loanAuditOption = loanCaseDTO.LoanAuditOption;
            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {
                await _channelService.AuditLoanCaseAsync(loanCaseDTO, loanAuditOption, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanCaseDTO.ErrorMessages;
                ViewBag.LoanAuditOptionSelectList = GetLoanAuditOptionSelectList(loanCaseDTO.LoanAuditOption.ToString());
                return View(loanCaseDTO);
            }
        }

        public async Task<ActionResult> Cancel(Guid id)
        {
            await ServeNavigationMenus();

            var loanCaseDTO = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());
            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
            ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(string.Empty);
            ViewBag.LoanCancellationOptionSelectList = GetLoanCancellationOptionSelectList(string.Empty);

            return View(loanCaseDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Cancel(LoanCaseDTO loanCaseDTO)
        {
            var loanCancellationOption = loanCaseDTO.LoanCancellationOption;
            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {
                await _channelService.AuditLoanCaseAsync(loanCaseDTO, loanCancellationOption, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = loanCaseDTO.ErrorMessages;
                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(loanCaseDTO.LoanInterestCalculationMode.ToString());
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(loanCaseDTO.LoanRegistrationLoanProductCategory.ToString());
                ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(loanCaseDTO.LoanRegistrationPaymentFrequencyPerYear.ToString());
                ViewBag.LoanCancellationOptionSelectList = GetLoanCancellationOptionSelectList(loanCaseDTO.LoanCancellationOption.ToString());
                return View(loanCaseDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetLoanCasesAsync()
        {
            var loanCasesDTOs = await _channelService.FindLoanCasesAsync(GetServiceHeader());

            return Json(loanCasesDTOs, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetLoanCaseByLoanCaseNumberAsync(int caseNumber)
        {
            var loanCasesDTOs = await _channelService.FindLoanCaseByLoanCaseNumberAsync(caseNumber, GetServiceHeader());

            return Json(loanCasesDTOs, JsonRequestBehavior.AllowGet);
        }


        public async Task<ActionResult> StandingOrder(Guid? id)
        {
            return View();
        }
    }
}