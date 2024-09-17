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
    public class IntraAccountTransferController : MasterController
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
            var p = await _channelService.FindCustomerAccountSignatoriesByCustomerAccountIdAsync(customerAccountDTO.Id, GetServiceHeader());

            ViewBag.customerAccountSignatoryDTOs = p;
            TempData["customerAccountSignatoryDTOs"] = p;
            return View(customerAccountDTO);
        }
        public async Task<ActionResult> Search(Guid? id, InterAccountTransferBatchDTO standingOrderDTO)
        {
            await ServeNavigationMenus();


            Guid parseId;

            ViewBag.MonthsSelectList = GetMonthsAsync(string.Empty);
            ViewBag.ChargeTypeSelectList = GetChargeTypeSelectList(string.Empty);
            ViewBag.loanRegistrationStandingOrderTriggers = GetLoanRegistrationStandingOrderTriggerSelectList(string.Empty);


            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            if (Session["Customers"] != null)
            {
                standingOrderDTO.Customers = Session["Customers"] as CustomerAccountDTO;
            }

            if (Session["benefactorAccounts"] != null)
            {
                standingOrderDTO.interAccountTransferBatch = Session["benefactorAccounts"] as CustomerAccountDTO;
            }



            var beneficiaryAccounts = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            if (beneficiaryAccounts != null)
            {

                standingOrderDTO.interAccountTransferBatch = beneficiaryAccounts;
               
                //customerAccountSignatoryDTO.FirstName = benefactorAccounts.CustomerFullName;
                standingOrderDTO.CustomerAccountCustomerIndividualFirstName = beneficiaryAccounts.CustomerFullName;


            }
            Session["Customers"] = standingOrderDTO.interAccountTransferBatch;
            return View("Create", standingOrderDTO);
        }

        public async Task<ActionResult> Create(Guid? id, InterAccountTransferBatchDTO standingOrderDTO)
        {
            await ServeNavigationMenus();
            if (Session["interAccountTransferBatch"] != null)
            {
                standingOrderDTO.Customers = Session["interAccountTransferBatch"] as CustomerAccountDTO;
            }
            if(Session["Customers"]!=null)
            {
                standingOrderDTO.interAccountTransferBatch = Session["Customers"] as CustomerAccountDTO;
            }
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
            var beneficiaryAccounts = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            if (beneficiaryAccounts != null)
            {
                standingOrderDTO.Customers = beneficiaryAccounts;
                //customerAccountSignatoryDTO.FirstName = benefactorAccounts.CustomerFullName;
                standingOrderDTO.CustomerAccountCustomerIndividualFirstName = beneficiaryAccounts.CustomerFullName;


            }
            Session["interAccountTransferBatch"] = standingOrderDTO.Customers;
            return View("Create", standingOrderDTO);
        }
        //[HttpPost]
        //public async Task<ActionResult> Add(Guid? id, InterAccountTransferBatchDTO customerAccountSignatoryDTO, CustomerAccountDTO customerAccountDTO)
        //{
        //    await ServeNavigationMenus();

        //    customerAccountSignatoryDTOs = TempData["customerAccountSignatoryDTO"] as ObservableCollection<CustomerAccountSignatoryDTO>;

        //    if (customerAccountSignatoryDTOs == null)
        //        customerAccountSignatoryDTOs = new ObservableCollection<CustomerAccountSignatoryDTO>();

        //    foreach (var Signatory in customerAccountSignatoryDTO.customerAccountSignatoryDTOs)
        //    {
        //        Signatory.Id = customerAccountSignatoryDTO.Id;
        //        Signatory.FirstName = customerAccountSignatoryDTO.CustomerAccountCustomerFullName;
        //        Signatory.LastName = Signatory.LastName;
        //        Signatory.AddressAddressLine1 = Signatory.AddressAddressLine1;
        //        Signatory.AddressAddressLine2 = Signatory.AddressAddressLine2;
        //        Signatory.AddressCity = Signatory.AddressCity;
        //        Signatory.AddressEmail = Signatory.AddressEmail;
        //        Signatory.AddressMobileLine = Signatory.AddressMobileLine;
        //        Signatory.Relationship = Signatory.Relationship;
        //        customerAccountSignatoryDTOs.Add(Signatory);
        //    };

        //    TempData["customerAccountSignatoryDTO"] = customerAccountSignatoryDTOs;

        //    TempData["customerAccountSignatoryDTO"] = customerAccountSignatoryDTOs;
        //    Session["customerAccountSignatoryDTO"] = customerAccountSignatoryDTOs;
        //    if (Session["benefactorAccounts"] != null)
        //    {
        //        customerAccountDTO = Session["benefactorAccounts"] as CustomerAccountDTO;
        //    }
        //    ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerAccountSignatoryDTO.CustomerAccountCustomerType.ToString());
        //    ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(customerAccountSignatoryDTO.IdentityCardType.ToString());
        //    ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(customerAccountSignatoryDTO.IdentityCardType.ToString());
        //    ViewBag.SalutationSelectList = GetSalutationSelectList(customerAccountSignatoryDTO.Salutation.ToString());
        //    ViewBag.GenderSelectList = GetGenderSelectList(customerAccountSignatoryDTO.Gender.ToString());
        //    ViewBag.signatoryRelationshipSelectList = GetsignatoryRelationshipSelectList(customerAccountSignatoryDTO.Relationship.ToString());

        //    return View("Create");
        //}

        [HttpPost]
        public async Task<ActionResult> Create(InterAccountTransferBatchDTO customerBindingModel, ObservableCollection<InterAccountTransferBatchEntryDTO> interAccountTransferBatchEntryCollection)
        {

            customerBindingModel.ValidateAll();

            if (!customerBindingModel.HasErrors)
            {
                await _channelService.AddInterAccountTransferBatchAsync(customerBindingModel, GetServiceHeader());
                await _channelService.UpdateInterAccountTransferBatchEntryCollectionAsync(customerBindingModel.Id, interAccountTransferBatchEntryCollection, GetServiceHeader());
                TempData["AlertMessage"] = "Customer Account signatory created successfully";
                return RedirectToAction("Create");
            }
            else
            {
                var errorMessages = customerBindingModel.ErrorMessages;

                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(customerBindingModel.Status.ToString());
                ViewBag.IndividualTypeSelectList = GetIndividualTypeSelectList(customerBindingModel.Status.ToString());
                ViewBag.IdentityCardSelectList = GetIdentityCardTypeSelectList(customerBindingModel.Status.ToString());
                ViewBag.SalutationSelectList = GetSalutationSelectList(customerBindingModel.Status.ToString());
                ViewBag.GenderSelectList = GetGenderSelectList(customerBindingModel.Status.ToString());
                ViewBag.signatoryRelationshipSelectList = GetsignatoryRelationshipSelectList(customerBindingModel.Status.ToString());

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
