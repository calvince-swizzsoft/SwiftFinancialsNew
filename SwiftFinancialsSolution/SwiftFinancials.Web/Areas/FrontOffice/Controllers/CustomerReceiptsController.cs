using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class CustomerReceiptsController : MasterController
    {
        // GET: FrontOffice/CustomerReceipts
        public async Task<ActionResult> Index()
        {

            await ServeNavigationMenus();
            return View();

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
                customerAccountDTO.BranchId = customer.BranchId;
                customerAccountDTO.BranchDescription = customer.BranchDescription;
                customerAccountDTO.CustomerAccountTypeTargetProductId = customer.CustomerAccountTypeTargetProductId;
                customerAccountDTO.CustomerAccountTypeTargetProductCode = customer.CustomerAccountTypeTargetProductCode;
                customerAccountDTO.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                customerAccountDTO.CustomerAccountTypeTargetProductParentId = customer.CustomerAccountTypeTargetProductParentId;
                customerAccountDTO.AvailableBalance = customer.AvailableBalance;
                customerAccountDTO.BookBalance = customer.BookBalance;


            }

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(string.Empty);

            return View(customerAccountDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Create(CustomerAccountDTO customerAccountDTO)


        {


            customerAccountDTO.ValidateAll();


            if (!customerAccountDTO.HasErrors)
            {
                decimal totalValue = customerAccountDTO.TotalValue;
                int frontOfficeTransactionType = customerAccountDTO.Type;

                await _channelService.ComputeTellerCashTariffsAsync(customerAccountDTO, totalValue, frontOfficeTransactionType, GetServiceHeader());

                if (customerAccountDTO.TypeDescription.Equals("Cash Withdrawal"))
                {

                    CashWithdrawalRequestDTO cashWithdrawalRequestDTO = new CashWithdrawalRequestDTO();

                    cashWithdrawalRequestDTO.ValidateAll();

                    await _channelService.AddCashWithdrawalRequestAsync(cashWithdrawalRequestDTO, GetServiceHeader());

                }

                else if (customerAccountDTO.TypeDescription.Equals("Cash Deposit"))
                {


                    CashDepositRequestDTO cashDepositRequestDTO = new CashDepositRequestDTO();

                    cashDepositRequestDTO.ValidateAll();

                    await _channelService.AddCashDepositRequestAsync(cashDepositRequestDTO, GetServiceHeader());

                }

                else if (customerAccountDTO.TypeDescription.Equals("Cheque Deposit"))
                {


                    ExternalChequeDTO chequeDepositDTO = new ExternalChequeDTO();
                    chequeDepositDTO.ValidateAll();

                    await _channelService.AddExternalChequeAsync(chequeDepositDTO, GetServiceHeader());

                }

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
    }
}