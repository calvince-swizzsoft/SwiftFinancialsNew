using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using SwiftFinancials.Presentation.Infrastructure.Util;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;


namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class CustomerAccountsController : MasterController
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

            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, jQueryDataTablesModel.iColumns, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();
            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;
            var CustomerAccount = await _channelService.FindCustomerAccountAsync(id, includeInterestBalanceForLoanAccounts, includeBalances, includeProductDescription, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            return View();
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            ViewBag.CustomerAccountManagementActionSelectList = GetCustomerAccountManagementActionSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            CustomerAccountDTO customerAccountDTO = new CustomerAccountDTO();

            if (customer != null)
            {

                customerAccountDTO.CustomerId = customer.Id;
                customerAccountDTO.CustomerIndividualFirstName = customer.FullName;
                customerAccountDTO.CustomerIndividualPayrollNumbers = customer.IndividualPayrollNumbers;
                customerAccountDTO.CustomerSerialNumber = customer.SerialNumber;
                customerAccountDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                customerAccountDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
            }
            return View(customerAccountDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(CustomerAccountDTO customerAccountDTO)
        {


            customerAccountDTO.ValidateAll();

            if (!customerAccountDTO.HasErrors)
            {
                await _channelService.AddCustomerAccountAsync(customerAccountDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = customerAccountDTO.ErrorMessages;

                return View(customerAccountDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();
            await ServeNavigationMenus();
            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }
            var CustomerAccount = await _channelService.FindCustomerAccountAsync(id, includeInterestBalanceForLoanAccounts, includeBalances, includeProductDescription, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            CustomerAccountDTO customerAccountDTO = new CustomerAccountDTO();

            if (CustomerAccount != null)
            {

                customerAccountDTO.CustomerId = CustomerAccount.Id;
                customerAccountDTO.CustomerIndividualFirstName = CustomerAccount.CustomerIndividualFirstName;
                customerAccountDTO.CustomerIndividualPayrollNumbers = CustomerAccount.CustomerIndividualPayrollNumbers;
                customerAccountDTO.CustomerSerialNumber = CustomerAccount.CustomerSerialNumber;
                customerAccountDTO.CustomerIndividualIdentityCardNumber = CustomerAccount.CustomerIndividualIdentityCardNumber;
                customerAccountDTO.CustomerStationZoneDivisionEmployerDescription = CustomerAccount.CustomerStationZoneDivisionEmployerDescription;
            }
            return View(customerAccountDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, CustomerAccountDTO customerAccountDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateCustomerAccountAsync(customerAccountDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(customerAccountDTO);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomersCountAsync(CustomerAccountDTO customerAccountDTO)
        {
            await ServeNavigationMenus();
            var customerAccountId = customerAccountDTO.Id;
            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;
            var CustomerAccount = await _channelService.FindCustomerAccountAsync(customerAccountId, includeInterestBalanceForLoanAccounts, includeBalances, includeProductDescription, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            return Json(customerAccountDTO, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> CustomerManagement(Guid id)
        {
            await ServeNavigationMenus();
            bool includeInterestBalanceForLoanAccounts = false;
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;


            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            CustomerAccountDTO customerAccountDTO = new CustomerAccountDTO();

            var CustomerAccount = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            if (CustomerAccount != null)
            {
                customerAccountDTO.CustomerId = CustomerAccount.Id;
                customerAccountDTO.CustomerIndividualFirstName = CustomerAccount.CustomerIndividualFirstName;
                customerAccountDTO.CustomerIndividualPayrollNumbers = CustomerAccount.CustomerIndividualPayrollNumbers;
                customerAccountDTO.CustomerSerialNumber = CustomerAccount.CustomerSerialNumber;
                customerAccountDTO.CustomerIndividualIdentityCardNumber = CustomerAccount.CustomerIndividualIdentityCardNumber;
                customerAccountDTO.CustomerStationZoneDivisionEmployerDescription = CustomerAccount.CustomerStationZoneDivisionEmployerDescription;
            }
            return View(customerAccountDTO);
        }

        [HttpPost]
        public async Task<ActionResult> CustomerManagement(Guid id, CustomerAccountDTO customerAccountHistoryDTO)
        {
            if (ModelState.IsValid)
            {
                int managementAction = 0;

                string remarks = "";

                int remarkType = 0;

                await _channelService.ManageCustomerAccountAsync(id, managementAction, remarks, remarkType, GetServiceHeader());

                ViewBag.CustomerAccountManagementActionSelectList = GetCustomerAccountManagementActionSelectList(customerAccountHistoryDTO.CustomerIndividualSalutationDescription.ToString());
                return RedirectToAction("Index");
            }
            else
            {
                return View(customerAccountHistoryDTO);
            }
        }

    }
}