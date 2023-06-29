using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class LoanProductController : MasterController
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

            var pageCollectionInfo = await _channelService.FindLoanProductsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanProductDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loanProductDTO = await _channelService.FindLoanProductAsync(id, GetServiceHeader());

            return View(loanProductDTO);
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.LoanInterestChargeModeSelectList = GetLoanInterestChargeModeSelectList(string.Empty);
            ViewBag.LoanInterestRecoveryModeSelectList = GetLoanInterestRecoveryModeSelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductCategorySelectList = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
            ViewBag.LoanRegistrationRoundingTypeSelectList = GetLoanRegistrationRoundingTypeSelectList(string.Empty);
            ViewBag.LoanRegistrationGuarantorSecurityModeSelectList = GetLoanRegistrationGuarantorSecurityModeSelectList(string.Empty);
            ViewBag.LoanRegistrationAggregateCheckOffRecoveryModeSelectList = GetLoanRegistrationAggregateCheckOffRecoveryModeSelectList(string.Empty);
            ViewBag.LoanRegistrationStandingOrderTriggerSelectList = GetLoanRegistrationStandingOrderTriggerSelectList(string.Empty);
            ViewBag.PrioritySelectList = GetRecoveryPrioritySelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);
            ViewBag.LoanRegistrationPayoutRecoveryModeSelectList = GetLoanRegistrationPayoutRecoveryModeSelectList(string.Empty);
            ViewBag.TakeHomeTypeSelectList = GetTakeHomeTypeSelectList(string.Empty);
            ViewBag.LoanRegistrationPaymentDueDateSelectList = GetLoanRegistrationPaymentDueDateSelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(LoanProductDTO LoanProductDTO)
        {
            LoanProductDTO.ValidateAll();

            if (!LoanProductDTO.HasErrors)
            {
                await _channelService.AddLoanProductAsync(LoanProductDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = LoanProductDTO.ErrorMessages;
                ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(LoanProductDTO.LoanInterestCalculationMode.ToString());
                ViewBag.LoanInterestChargeModeSelectList = GetLoanInterestChargeModeSelectList(LoanProductDTO.LoanInterestChargeMode.ToString());
                ViewBag.LoanInterestRecoveryModeSelectList = GetLoanInterestRecoveryModeSelectList(LoanProductDTO.LoanInterestRecoveryMode.ToString());
                ViewBag.LoanRegistrationLoanProductCategorySelectList = GetLoanRegistrationLoanProductCategorySelectList(LoanProductDTO.LoanRegistrationLoanProductCategory.ToString());
                ViewBag.LoanRegistrationRoundingTypeSelectList = GetLoanRegistrationRoundingTypeSelectList(LoanProductDTO.LoanRegistrationRoundingType.ToString());
                ViewBag.LoanRegistrationGuarantorSecurityModeSelectList = GetLoanRegistrationGuarantorSecurityModeSelectList(LoanProductDTO.LoanRegistrationGuarantorSecurityMode.ToString());
                ViewBag.LoanRegistrationAggregateCheckOffRecoveryModeSelectList = GetLoanRegistrationAggregateCheckOffRecoveryModeSelectList(LoanProductDTO.LoanRegistrationAggregateCheckOffRecoveryMode.ToString());
                ViewBag.LoanRegistrationStandingOrderTriggerSelectList = GetLoanRegistrationStandingOrderTriggerSelectList(LoanProductDTO.LoanRegistrationStandingOrderTrigger.ToString());
                ViewBag.PrioritySelectList = GetRecoveryPrioritySelectList(LoanProductDTO.Priority.ToString());
                ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductSectionsSelectList(LoanProductDTO.LoanRegistrationLoanProductSection.ToString());
                ViewBag.LoanRegistrationPayoutRecoveryModeSelectList = GetLoanRegistrationPayoutRecoveryModeSelectList(LoanProductDTO.LoanRegistrationPayoutRecoveryMode.ToString());
                ViewBag.LoanRegistrationPaymentDueDateSelectList = GetLoanRegistrationPaymentDueDateSelectList(LoanProductDTO.LoanRegistrationPaymentDueDate.ToString());
                ViewBag.TakeHomeTypeSelectList = GetTakeHomeTypeSelectList(LoanProductDTO.TakeHomeType.ToString());
                return View(LoanProductDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var loanProductDTO = await _channelService.FindLoanProductAsync(id, GetServiceHeader());

            return View(loanProductDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, LoanProductDTO loanProductBindingModel)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateLoanProductAsync(loanProductBindingModel, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(loanProductBindingModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetLoanProductsAsync()
        {
            var loanProductsDTOs = await _channelService.FindLoanProductsAsync(GetServiceHeader());

            return Json(loanProductsDTOs, JsonRequestBehavior.AllowGet);
        }
    }
}