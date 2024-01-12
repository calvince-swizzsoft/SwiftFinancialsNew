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
    public class CashTransferController : MasterController
    {

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            //Guid parseId;

            //if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            //{
            //    return View();
            //}

            //bool includeBalances = false;
            //bool includeProductDescription = false;
            //bool includeInterestBalanceForLoanAccounts = false;
            //bool considerMaturityPeriodForInvestmentAccounts = false;


            //var customer = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            //CashTransferRequestDTO cashDepositRequestDTO = new CashTransferRequestDTO();

            //if (customer != null)
            //{
                //cashDepositRequestDTO.CustomerAccountCustomerId = customer.Id;
                //cashDepositRequestDTO.CustomerAccountId = customer.Id;
                //cashDepositRequestDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerIndividualFirstName;
                ////  accountClosureRequestDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                //cashDepositRequestDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                //cashDepositRequestDTO.Remarks = customer.Remarks;
                //cashDepositRequestDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                //cashDepositRequestDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                //cashDepositRequestDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                //cashDepositRequestDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                //cashDepositRequestDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                //cashDepositRequestDTO.CustomerAccountCustomerPersonalIdentificationNumber = customer.CustomerPersonalIdentificationNumber;


           // }

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            ViewBag.customertypeSelectList = GetCustomerTypeSelectList(string.Empty);

            return View("Create");
        }

        [HttpPost]
        public async Task<ActionResult> Create(CashTransferRequestDTO cashTransferRequestDTO)
        {
            if (ModelState.IsValid)
            {
                await _channelService.AddCashTransferRequestAsync(cashTransferRequestDTO, GetServiceHeader());

                return RedirectToAction("Create");
            }
            else
            {

                ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(cashTransferRequestDTO.Status.ToString());

                return View(cashTransferRequestDTO);
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

