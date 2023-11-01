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

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength,jQueryDataTablesModel.iColumns, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts , considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

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

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            return View();
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
            var CustomerAccount = await _channelService.FindCustomerAccountAsync(id, includeInterestBalanceForLoanAccounts, includeBalances, includeProductDescription, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            return View();
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
    }
}