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
    public class CashWithdrawalController : MasterController
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
            int pageIndex = 0;
            int pageSize = 0;

            int searchRecordCount = 0;

            DateTime startDate = DateTime.Now;

            DateTime endDate = DateTime.Now;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCashWithdrawalRequestsByFilterInPageAsync(startDate, endDate, jQueryDataTablesModel.iColumns, jQueryDataTablesModel.sSearch, jQueryDataTablesModel.sEcho, pageIndex, pageSize, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CashWithdrawalRequestDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var tellerDTO = await _channelService.FindCashWithdrawalRequestAsync(id);

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

            CashWithdrawalRequestDTO cashWithdrawalRequestDTO = new CashWithdrawalRequestDTO();

            if (customer != null)
            {
                cashWithdrawalRequestDTO.CustomerAccountCustomerId = customer.Id;
                cashWithdrawalRequestDTO.CustomerAccountId = customer.Id;
                cashWithdrawalRequestDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerIndividualFirstName;
                //  accountClosureRequestDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                cashWithdrawalRequestDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                cashWithdrawalRequestDTO.CustomerAccountCustomerAccountTypeTargetProductCode = customer.CustomerAccountTypeProductCode;
                cashWithdrawalRequestDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                cashWithdrawalRequestDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                cashWithdrawalRequestDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                cashWithdrawalRequestDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                cashWithdrawalRequestDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                cashWithdrawalRequestDTO.CustomerAccountCustomerPersonalIdentificationNumber = customer.CustomerPersonalIdentificationNumber;
                cashWithdrawalRequestDTO.CustomerAccountRemarks = customer.Remarks;
                cashWithdrawalRequestDTO.BranchDescription = customer.BranchDescription;
                cashWithdrawalRequestDTO.BranchId = customer.BranchId;
                //cashWithdrawalRequestDTO.CustomerAccountRemarks = customer.Remarks;
            }

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(string.Empty);

            return View(cashWithdrawalRequestDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(CashWithdrawalRequestDTO cashWithdrawalRequestDTO)
        {
            cashWithdrawalRequestDTO.ValidateAll();

            if (!cashWithdrawalRequestDTO.HasErrors)
            {
                await _channelService.AddCashWithdrawalRequestAsync(cashWithdrawalRequestDTO, GetServiceHeader());

                return RedirectToAction("Create");
            }
            else
            {
                var errorMessages = cashWithdrawalRequestDTO.ErrorMessages;
                ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(cashWithdrawalRequestDTO.CustomerAccountCustomerTypeDescription.ToString());

                return View(cashWithdrawalRequestDTO);
            }
        }



        public async Task<ActionResult> Approval()
        {
            await ServeNavigationMenus();
            Guid id = new Guid();

            ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);


            var CashWithdrawal = await _channelService.FindCashWithdrawalRequestAsync(id, GetServiceHeader());

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
            return View(CashWithdrawal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Approval(CashWithdrawalRequestDTO cashWithdrawalRequestDTO)
        {
            cashWithdrawalRequestDTO.ValidateAll();

            int customerTransactionAuthOption = 0;
            if (!cashWithdrawalRequestDTO.HasErrors)
            {
                await _channelService.AuthorizeCashWithdrawalRequestAsync(cashWithdrawalRequestDTO, customerTransactionAuthOption, GetServiceHeader());

                return RedirectToAction("Index");
            }
            else
            {
                var errorMessages = cashWithdrawalRequestDTO.ErrorMessages;
                ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(cashWithdrawalRequestDTO.Category.ToString());
                return View(cashWithdrawalRequestDTO);
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

