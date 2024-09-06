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
using System.Windows.Forms;

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

            CustomerTransactionModel model = new CustomerTransactionModel();

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                ViewBag.ApportionToSelectList = GetApportionToSelectList(string.Empty);
                return View(model);
            }

            //customer account uncleared cheques
            var unclearedChequescollection = await _channelService.FindUnClearedExternalChequesByCustomerAccountIdAsync(model.DebitCustomerAccount.Id, GetServiceHeader());
            var _unclearedCheques = unclearedChequescollection.ToList();
            model.CustomerAccountUnclearedCheques = _unclearedCheques;

            //customer account signatories
            var signatoriesCollection = await _channelService.FindCustomerAccountSignatoriesByCustomerAccountIdAsync(model.DebitCustomerAccount.Id, GetServiceHeader());
            var _signatories = signatoriesCollection.ToList();
            model.CustomerAccountSignatories = _signatories;


            //customer acount ministatement
            var miniStatementOrdersCollection = await _channelService.FindElectronicStatementOrdersByCustomerAccountIdAsync(model.DebitCustomerAccount.Id, true, GetServiceHeader());
            var _miniStatement = miniStatementOrdersCollection.ToList();
            model.CustomerAccountMiniStatement = _miniStatement;

            ViewBag.ApportionToSelectList = GetApportionToSelectList(string.Empty);

            return View(model);
        }


        public async Task<JsonResult> GetCustomerDetailsJson(Guid? id)
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

            return Json(customer, JsonRequestBehavior.AllowGet);
        }



        [HttpPost]
        public async Task<ActionResult> Create(CustomerTransactionModel model)
        {

            var currentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());

            model.PostingPeriodId = currentPostingPeriod.Id;
            model.DebitChartOfAccountId = model.DebitCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId;
            model.CreditCustomerAccount = model.DebitCustomerAccount;
            model.CreditCustomerAccountId = model.CreditCustomerAccount.Id;
            model.CreditChartOfAccountId = model.CreditCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId;


            var selectedBranch = await _channelService.FindBranchAsync(model.BranchId, GetServiceHeader());
            var currentUser = await _applicationUserManager.FindByEmailAsync("calvince.ochieng@swizzsoft.com");       
            var currentTeller = await _channelService.FindTellerByEmployeeIdAsync((Guid)currentUser.EmployeeId, true, GetServiceHeader());
            
            model.PrimaryDescription = "ok";
            model.SecondaryDescription = string.Format("B{0}/T{1}/#{2}", selectedBranch.Code, currentTeller.Code, currentTeller.ItemsCount);

            model.ValidateAll();
    
             if (!model.HasErrors)
             {
                
                
                decimal totalValue = model.CustomerAccount.TotalValue;

                var transactionModel = new TransactionModel();

                transactionModel.TotalValue = model.TotalValue;
                transactionModel.DebitCustomerAccount = model.DebitCustomerAccount;
                transactionModel.DebitChartOfAccountId = model.DebitCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId;
              
                transactionModel.BranchId = model.BranchId;
                transactionModel.PrimaryDescription = model.PrimaryDescription;
                transactionModel.SecondaryDescription = model.SecondaryDescription; 
                transactionModel.Reference = model.Reference;
                transactionModel.PostingPeriodId = currentPostingPeriod.Id;

                var tariffs = await _channelService.ComputeTellerCashTariffsAsync(model.DebitCustomerAccount, totalValue, 2, GetServiceHeader());
                var dynamicCharges = await _channelService.FindDynamicChargesAsync(GetServiceHeader());
                ObservableCollection<ApportionmentWrapper> apportionmentsObservableCollection = new ObservableCollection<ApportionmentWrapper>(model.Apportionments);

                var journal = _channelService.AddJournalWithApportionmentsAsync(transactionModel, apportionmentsObservableCollection, tariffs, dynamicCharges, GetServiceHeader());


                if (journal != null && journal.Exception == null)
                {

                    MessageBox.Show(
                                                       "Operation Success",
                                                       "Customer Receipts",
                                                       MessageBoxButtons.OK,
                                                       MessageBoxIcon.Information,
                                                       MessageBoxDefaultButton.Button1,
                                                       MessageBoxOptions.ServiceNotification
                                                   );
                }

                else
                {

                    MessageBox.Show(
                                                       "Operation Failed",
                                                       "Custmer Receipts",
                                                       MessageBoxButtons.OK,
                                                       MessageBoxIcon.Information,
                                                       MessageBoxDefaultButton.Button1,
                                                       MessageBoxOptions.ServiceNotification
                                                   );
                }

                //service.BeginAddJournalWithApportionments(transactionModel.BranchId, transactionModel.AlternateChannelLogId, transactionModel.TotalValue, transactionModel.PrimaryDescription, transactionModel.SecondaryDescription, transactionModel.Reference, transactionModel.ModuleNavigationItemCode, transactionModel.TransactionCode, transactionModel.ValueDate, transactionModel.DebitChartOfAccountId, transactionModel.CreditCustomerAccount, transactionModel.DebitCustomerAccount, apportionments.ExtendedToList(), tariffs.ExtendedToList(), dynamicCharges.ExtendedToList(), asyncCallback, service);

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