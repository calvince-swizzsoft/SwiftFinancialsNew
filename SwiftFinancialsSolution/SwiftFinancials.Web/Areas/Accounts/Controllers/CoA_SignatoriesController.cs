using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class CoA_SignatoriesController : MasterController
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

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByFilterInPageAsync(jQueryDataTablesModel.sSearch, 0, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(ChartOfAccountDTO => ChartOfAccountDTO.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<ChartOfAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            var customerAccountDTO = await _channelService.FindCustomerAccountAsync(id, false, false, false, false, GetServiceHeader());
           var p= await _channelService.FindCustomerAccountSignatoriesByCustomerAccountIdAsync(customerAccountDTO.Id, GetServiceHeader());
            
            ViewBag.customerAccountSignatoryDTOs = p;
            TempData["customerAccountSignatoryDTOs"] = p;
            return View(customerAccountDTO);
        }


        public async Task<ActionResult> Create(Guid? id, CustomerAccountSignatoryDTO customerAccountSignatoryDTO)
        {
            await ServeNavigationMenus();


            ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(string.Empty);
            ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(string.Empty);
            ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(string.Empty);
            ViewBag.SalutationSelectList = GetSalutationSelectList(string.Empty);
            ViewBag.GenderSelectList = GetGenderSelectList(string.Empty);
            ViewBag.MaritalStatusSelectList = GetMaritalStatusSelectList(string.Empty);
            ViewBag.signatoryRelationshipSelectList = GetsignatoryRelationshipSelectList(string.Empty);
            ViewBag.IndividualEmploymentTermsOfServiceSelectList = GetTermsOfServiceSelectList(string.Empty);
            ViewBag.IndividualClassificationSelectList = GetCustomerClassificationSelectList(string.Empty);

            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.loanRegistrationStandingOrderTriggers = GetLoanRegistrationStandingOrderTriggerSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;
            var benefactorAccounts = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());


            if (customerAccountSignatoryDTO != null)
            {
                customerAccountSignatoryDTO.CustomerAccountId = benefactorAccounts.Id;
                customerAccountSignatoryDTO.Customers = benefactorAccounts;
                //customerAccountSignatoryDTO.FirstName = benefactorAccounts.CustomerFullName;
                customerAccountSignatoryDTO.Salutation = benefactorAccounts.CustomerIndividualSalutation ;
                //customerAccountSignatoryDTO.AddressEmail = benefactorAccounts.CustomerAddressEmail;
               

            }

            
            return View(customerAccountSignatoryDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(CustomerAccountSignatoryDTO customerBindingModel)
        {

            customerBindingModel.ValidateAll();

            if (!customerBindingModel.HasErrors)
            {
                await _channelService.AddCustomerAccountSignatoryAsync(customerBindingModel, GetServiceHeader());
                TempData["AlertMessage"] = "Customer Account signatory created successfully";
                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerBindingModel.ErrorMessages;

                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerBindingModel.CustomerAccountCustomerType.ToString());
                ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(customerBindingModel.IdentityCardType.ToString());
                ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(customerBindingModel.IdentityCardType.ToString());
                ViewBag.SalutationSelectList = GetSalutationSelectList(customerBindingModel.Salutation.ToString());
                ViewBag.GenderSelectList = GetGenderSelectList(customerBindingModel.Gender.ToString());
                ViewBag.signatoryRelationshipSelectList = GetsignatoryRelationshipSelectList(customerBindingModel.Relationship.ToString());

                return View(customerBindingModel);
            }
        }






        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            ViewBag.record = GetRecordStatusSelectList(string.Empty);
            var customerAccountDTO = await _channelService.FindCustomerAccountAsync(id, false, false, false, false, GetServiceHeader());
            return View(customerAccountDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CustomerAccountDTO customerAccountDTO)
        {
            customerAccountDTO.CreatedDate = DateTime.Today;
            ViewBag.record = GetRecordStatusSelectList(customerAccountDTO.RecordStatus.ToString());
            if (!customerAccountDTO.HasErrors)
            {
                await _channelService.UpdateCustomerAccountAsync(customerAccountDTO, GetServiceHeader());

                TempData["Edit"] = "Edited Customer Account successfully";

                return RedirectToAction("Index");
            }

            TempData["EditError"] = "Failed to Edit Customer Account";
            return View(customerAccountDTO);
        }
    }
}
