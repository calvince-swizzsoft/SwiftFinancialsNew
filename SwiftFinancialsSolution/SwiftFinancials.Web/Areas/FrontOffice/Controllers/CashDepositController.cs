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
    public class CashDepositController : MasterController
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

            DateTime startDate = DateTime.Now;

            DateTime endDate = DateTime.Now;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCashDepositRequestsByFilterInPageAsync(startDate, endDate, jQueryDataTablesModel.iColumns, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.sEcho, 1, 1, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CashDepositRequestDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var tellerDTO = await _channelService.FindCashDepositRequestAsync(id);

            return View(tellerDTO);
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

            CashDepositRequestDTO cashDepositRequestDTO = new CashDepositRequestDTO();

            if (customer != null)
            {
                cashDepositRequestDTO.CustomerAccountCustomerId = customer.Id;
                cashDepositRequestDTO.CustomerAccountId = customer.Id;
                cashDepositRequestDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerIndividualFirstName;
                //  accountClosureRequestDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                cashDepositRequestDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                cashDepositRequestDTO.Remarks = customer.Remarks;
                cashDepositRequestDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                cashDepositRequestDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                cashDepositRequestDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                cashDepositRequestDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                cashDepositRequestDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;                
                cashDepositRequestDTO.CustomerAccountCustomerPersonalIdentificationNumber = customer.CustomerPersonalIdentificationNumber;
                cashDepositRequestDTO.CustomerAccountRemarks = customer.Remarks;
                cashDepositRequestDTO.BranchDescription = customer.BranchDescription;

            }

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            ViewBag.customertypeSelectList = GetCustomerTypeSelectList(string.Empty);

            return View(cashDepositRequestDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(CashDepositRequestDTO cashDepositRequestDTO)
        {
            cashDepositRequestDTO.ValidateAll();

            if (!cashDepositRequestDTO.HasErrors)
            {
                await _channelService.AddCashDepositRequestAsync(cashDepositRequestDTO, GetServiceHeader());

                return RedirectToAction("Create");
            }
            else
            {
                var errorMessages = cashDepositRequestDTO.ErrorMessages;
                ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(cashDepositRequestDTO.CustomerAccountCustomerTypeDescription.ToString());

                return View(cashDepositRequestDTO);
            }
        }



        [HttpGet]
        public async Task<JsonResult> GetTellersAsync()
        {
            var tellersDTOs = await _channelService.FindTellersAsync(GetServiceHeader());

            return Json(tellersDTOs, JsonRequestBehavior.AllowGet);

        }
    }
}

