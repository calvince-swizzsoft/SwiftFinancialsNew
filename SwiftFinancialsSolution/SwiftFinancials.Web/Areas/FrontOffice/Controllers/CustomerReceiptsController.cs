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
using SwiftFinancials.Presentation.Infrastructure.Models;
using Infrastructure.Crosscutting.Framework.Utils;
using System.Net;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

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


        public async Task<ActionResult> Create(Guid? id, int? role)
        {
            await ServeNavigationMenus();
            Guid parseId;



            var model = new CustomerTransactionModel();

            // Initialize both models
            model.ApportionmentWrapper.CreditCustomerAccount = new CustomerAccountDTO();

            //debit model
            model.CustomerAccount = new CustomerAccountDTO();

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                ViewBag.ApportionToSelectList = GetApportionToSelectList(string.Empty);
                return View();
            }

   
            //string description = GetEnumDescription(transactionRole);


            bool includeBalances = true;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = true;
            bool considerMaturityPeriodForInvestmentAccounts = true;


            var customer = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());


            if (role.HasValue)
            {
                CustomerAccountTransactionRole transactionRole = (CustomerAccountTransactionRole)role;
                switch (transactionRole)
                {

                    case CustomerAccountTransactionRole.CreditCustomerAccount:


                        if (Session["debitAcct"] != null)
                        {
                            model.CustomerAccount = Session["debitAcct"] as CustomerAccountDTO;
                        }

                        if (customer != null)
                        {
                            model.ApportionmentWrapper.FullAccountNumber = customer.FullAccountNumber;
                            model.ApportionmentWrapper.CustomerFullName = customer.CustomerFullName;
                            model.ApportionmentWrapper.CustomerReference1 = customer.CustomerReference1;
                            model.ApportionmentWrapper.CreditCustomerAccount.Id = customer.Id;
                            model.ApportionmentWrapper.CreditCustomerAccount.CustomerId = customer.CustomerId;

                            model.ApportionmentWrapper.CreditCustomerAccount.CustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                            model.ApportionmentWrapper.CreditCustomerAccount.CustomerSerialNumber = customer.CustomerSerialNumber;
                            model.ApportionmentWrapper.CreditCustomerAccount.CustomerReference1 = customer.CustomerReference1;
                            model.ApportionmentWrapper.CreditCustomerAccount.CustomerReference2 = customer.CustomerReference2;
                            model.ApportionmentWrapper.CreditCustomerAccount.CustomerReference3 = customer.CustomerReference3;
                            model.ApportionmentWrapper.CreditCustomerAccount.CustomerIndividualIdentityCardNumber = customer.CustomerIndividualIdentityCardNumber;
                            model.ApportionmentWrapper.CreditCustomerAccount.Remarks = customer.Remarks;
                            model.ApportionmentWrapper.CreditCustomerAccount.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                            model.ApportionmentWrapper.CreditCustomerAccount.BranchId = customer.BranchId;
                            model.ApportionmentWrapper.CreditCustomerAccount.BranchDescription = customer.BranchDescription;
                            model.ApportionmentWrapper.CreditCustomerAccount.CustomerAccountTypeTargetProductId = customer.CustomerAccountTypeTargetProductId;
                            model.ApportionmentWrapper.CreditCustomerAccount.CustomerAccountTypeTargetProductCode = customer.CustomerAccountTypeTargetProductCode;
                            model.ApportionmentWrapper.CreditCustomerAccount.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                            model.ApportionmentWrapper.CreditCustomerAccount.CustomerAccountTypeTargetProductParentId = customer.CustomerAccountTypeTargetProductParentId;
                            model.ApportionmentWrapper.CreditCustomerAccount.AvailableBalance = customer.AvailableBalance;
                            model.ApportionmentWrapper.CreditCustomerAccount.BookBalance = customer.BookBalance;
                            model.ApportionmentWrapper.ProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                           
                        }


                        // After autofilling data

                        Session["creditAcct"] = model.CreditCustomerAccount;



                        break;

                    case CustomerAccountTransactionRole.DebitCustomerAccount:

                        if (Session["creditAcct"] != null)
                        {
                            model.CreditCustomerAccount = Session["creditAcct"] as CustomerAccountDTO;
                        }


                        //CustomerAccountDTO CustomerAccount = new CustomerAccountDTO();

                        if (model != null)
                        {


                            model.CustomerAccount.Id = customer.Id;
                            model.CustomerAccount.CustomerId = customer.CustomerId;

                            model.CustomerAccount.CustomerIndividualFirstName = customer.CustomerFullName;
                            model.CustomerAccount.CustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                            model.CustomerAccount.CustomerSerialNumber = customer.CustomerSerialNumber;
                            model.CustomerAccount.CustomerReference1 = customer.CustomerReference1;
                            model.CustomerAccount.CustomerReference2 = customer.CustomerReference2;
                            model.CustomerAccount.CustomerReference3 = customer.CustomerReference3;
                            model.CustomerAccount.CustomerIndividualIdentityCardNumber = customer.CustomerIndividualIdentityCardNumber;
                            model.CustomerAccount.Remarks = customer.Remarks;
                            model.CustomerAccount.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                            model.CustomerAccount.BranchId = model.BranchId;
                            model.CustomerAccount.BranchDescription = customer.BranchDescription;
                            model.CustomerAccount.CustomerAccountTypeTargetProductId = customer.CustomerAccountTypeTargetProductId;
                            model.CustomerAccount.CustomerAccountTypeTargetProductCode = customer.CustomerAccountTypeTargetProductCode;
                            model.CustomerAccount.CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription;
                            model.CustomerAccount.CustomerAccountTypeTargetProductParentId = customer.CustomerAccountTypeTargetProductParentId;
                            model.CustomerAccount.AvailableBalance = customer.AvailableBalance;
                            model.CustomerAccount.BookBalance = customer.BookBalance;

                        }

                        // After autofilling data

                        Session["debitAcct"] = model.CustomerAccount;

                        break;
                   }

                }

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(string.Empty);

            ViewBag.ApportionToSelectList = GetApportionToSelectList(string.Empty);

            return View(model);
        }


        public async Task<JsonResult> GetCustomerDetailsJson(Guid? id, int? role)
        {
            Guid parseId;

            if (!Guid.TryParse(id.ToString(), out parseId))
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            bool includeBalances = true;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = true;
            bool considerMaturityPeriodForInvestmentAccounts = true;

            var customer = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            if (customer == null)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            var model = new
            {
               FullAccountNumber = customer.FullAccountNumber,
                CustomerFullName = customer.CustomerFullName,
                CustomerReference1 = customer.CustomerReference1,
                CustomerReference2 = customer.CustomerReference2,
                BranchDescription = customer.BranchDescription,
                AvailableBalance =  customer.AvailableBalance,
                BranchId = customer.BranchId,
                CustomerAccountTypeTargetProductId = customer.CustomerAccountTypeTargetProductId,
                CustomerAccountTypeTargetProductCode = customer.CustomerAccountTypeTargetProductCode,
                CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription,
                CustomerAccountTypeTargetProductParentId = customer.CustomerAccountTypeTargetProductParentId,
                Id = customer.Id,
                CustomerId = customer.CustomerId

        };

            return Json(model, JsonRequestBehavior.AllowGet);
        }






        [HttpPost]
        public async Task<ActionResult> Create(CustomerTransactionModel model)
        {
            model.CustomerAccount.ValidateAll();
            var rawApportionments = model.Apportionments;
            // Deserialize the ApportionmentsData JSON field
            //var apportionmentsJson = Request.Form["ApportionmentsData"];
            //var apportionments = JsonConvert.DeserializeObject<List<ApportionmentWrapper>>(apportionmentsJson);

            // Add the deserialized apportionments to your model
            //model.Apportionments = apportionments;


             if (!model.HasErrors)
            {
                var currentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
                
                decimal totalValue = model.CustomerAccount.TotalValue;
                
                int apportionTo = model.CustomerAccount.Type;

                var transactionModel = new TransactionModel();

                transactionModel.TotalValue = model.TotalValue;
                transactionModel.DebitCustomerAccount = model.CustomerAccount;
                transactionModel.DebitChartOfAccountId = model.CustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId;

                transactionModel.PostingPeriodId = currentPostingPeriod.Id;

                var tariffs = await _channelService.ComputeTellerCashTariffsAsync(model.CustomerAccount, totalValue, 2, GetServiceHeader());
                var dynamicCharges = await _channelService.FindDynamicChargesAsync(GetServiceHeader());

                
                ObservableCollection<ApportionmentWrapper> apportionmentsObservableCollection = new ObservableCollection<ApportionmentWrapper>(model.Apportionments);


                await _channelService.AddJournalWithApportionmentsAsync(transactionModel, apportionmentsObservableCollection, tariffs, dynamicCharges, GetServiceHeader());

                ViewBag.ApportionToSelectList = GetApportionToSelectList(string.Empty);
                
                return RedirectToAction("Create");
            }
            else
            {
                var errorMessages = model.ErrorMessages;
    
                ViewBag.ApportionToSelectList = GetApportionToSelectList(string.Empty);
                
                return View(model);
            }
        }
    }
}