using Application.MainBoundedContext.DTO.FrontOfficeModule;
using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class CashWithdrawalRequestController : MasterController
    {
        // GET: FrontOffice/CashWithdrawalRequest
        public async Task<ActionResult> Index()
        {

            await ServeNavigationMenus();
            return View();
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

                var observableCollection1 = await _channelService.FindCashDepositRequestsAsync(GetServiceHeader());

                //cashWithdrawalRequestDTO.CashDepositRequests = observableCollection.ToList();

                var model = new CashWithdrawalRequestDTO
                {
                    // Initialize with empty list if no data is available
                    CashDepositRequests = observableCollection1.ToList()
                };


                return View(model);
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

            var observableCollection = await _channelService.FindCashDepositRequestsAsync(GetServiceHeader());

            cashWithdrawalRequestDTO.CashDepositRequests = observableCollection.ToList();

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
    }
}