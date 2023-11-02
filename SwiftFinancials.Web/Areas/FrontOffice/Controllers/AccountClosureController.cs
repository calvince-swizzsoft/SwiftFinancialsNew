using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class AccountClosureController : MasterController
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
            bool includeProductDescription = false;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindAccountClosureRequestsByFilterInPageAsync(jQueryDataTablesModel.sSearch, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, jQueryDataTablesModel.iColumns, includeProductDescription, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(postingPeriod => postingPeriod.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<AccountClosureRequestDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var accountClosureDTO = await _channelService.FindAccountClosureRequestAsync(id, true);

            return View(accountClosureDTO);
        }


        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;


            var customer = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            AccountClosureRequestDTO accountClosureRequestDTO = new AccountClosureRequestDTO();

            if (customer != null)
            {
                accountClosureRequestDTO.CustomerAccountCustomerId = customer.Id;
                accountClosureRequestDTO.CustomerAccountId = customer.Id;
                accountClosureRequestDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerIndividualFirstName;
                accountClosureRequestDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                accountClosureRequestDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                accountClosureRequestDTO.CustomerAccountCustomerIndividualIdentityCardNumber = customer.CustomerIndividualIdentityCardNumber;


            }

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            ViewBag.customertypeSelectList = GetCustomerTypeSelectList(string.Empty);

            return View(accountClosureRequestDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(AccountClosureRequestDTO accountClosureRequestDTO)
        {
            accountClosureRequestDTO.ValidateAll();

            if (!accountClosureRequestDTO.HasErrors)
            {
                await _channelService.AddAccountClosureRequestAsync(accountClosureRequestDTO, GetServiceHeader());

                ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(accountClosureRequestDTO.CustomerAccountCustomerType.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = accountClosureRequestDTO.ErrorMessages;

                return View(accountClosureRequestDTO);
            }
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            await ServeNavigationMenus();

            var accountClosureRequestDTO = await _channelService.FindAccountClosureRequestAsync(id, true);

            return View(accountClosureRequestDTO);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, AccountClosureRequestDTO accountClosureRequestDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.UpdateAccountClosureRequestAsync(accountClosureRequestDTO, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                return View(accountClosureRequestDTO);
            }
        }

        //[HttpGet]
        //public async Task<JsonResult> GetTellersAsync()
        //{
        //    var tellersDTOs = await _channelService.FindTellersAsync(GetServiceHeader());

        //    return Json(tellersDTOs, JsonRequestBehavior.AllowGet);
    }
}
