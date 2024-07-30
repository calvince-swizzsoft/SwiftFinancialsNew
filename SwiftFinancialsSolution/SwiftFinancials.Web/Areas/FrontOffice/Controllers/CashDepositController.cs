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
            ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(string.Empty);
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

            CustomerAccountDTO customerAccountDTO = new CustomerAccountDTO();

            if (customer != null)
            {
                customerAccountDTO.Id = customer.Id;
                customerAccountDTO.CustomerId = customer.CustomerId;
                
                customerAccountDTO.CustomerIndividualFirstName = customer.CustomerFullName;
                customerAccountDTO.CustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                customerAccountDTO.CustomerSerialNumber = customer.CustomerSerialNumber;
                customerAccountDTO.CustomerReference1 = customer.CustomerReference1;
                customerAccountDTO.CustomerReference2 = customer.CustomerReference2;
                customerAccountDTO.CustomerReference3 = customer.CustomerReference3;
                customerAccountDTO.CustomerIndividualIdentityCardNumber = customer.CustomerIndividualIdentityCardNumber;
                customerAccountDTO.Remarks = customer.Remarks;
                customerAccountDTO.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
               

            }

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(string.Empty);

            return View(customerAccountDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(CustomerAccountDTO customerAccountDTO)

        
        {
            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;


            var customer = await _channelService.FindCustomerAccountAsync(customerAccountDTO.Id, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            customerAccountDTO.CustomerId = customer.CustomerId;
            customerAccountDTO.BranchId = customer.BranchId;
            customerAccountDTO.CustomerAccountTypeTargetProductId = customer.CustomerAccountTypeTargetProductId;

            customerAccountDTO.ValidateAll();
           
            if (!customerAccountDTO.HasErrors)
            {
                decimal totalValue = customerAccountDTO.TotalValue;
                int frontOfficeTransactionType = customerAccountDTO.Type;

                await _channelService.ComputeTellerCashTariffsAsync(customerAccountDTO, totalValue, frontOfficeTransactionType, GetServiceHeader());

                ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(customerAccountDTO.CustomerAccountManagementActionDescription.ToString());

                return RedirectToAction("Create");
            }
            else
            {
                var errorMessages = customerAccountDTO.ErrorMessages;
                // ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(cashDepositRequestDTO.CustomerAccountCustomerTypeDescription.ToString());
                ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(customerAccountDTO.Type.ToString());

                return View(customerAccountDTO);
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

