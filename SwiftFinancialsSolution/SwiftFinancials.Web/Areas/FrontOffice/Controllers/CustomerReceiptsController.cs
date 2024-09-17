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


        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        
        {
            var currentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());

            var currentUser = await _applicationUserManager.FindByEmailAsync("calvince.ochieng@swizzsoft.com");
            var currentTeller = await _channelService.FindTellerByEmployeeIdAsync((Guid)currentUser.EmployeeId, true, GetServiceHeader());

            int totalRecordCount = 0;

            int searchRecordCount = 0;

            DateTime startDate = DateTime.Now;

            DateTime endDate = DateTime.Now;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;


            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();


            if (currentTeller != null)
            {
                var pageCollectionInfo = await _channelService.FindGeneralLedgerTransactionsByChartOfAccountIdAndDateRangeAndFilterInPageAsync(
                    pageIndex,
                    jQueryDataTablesModel.iDisplayLength,
                    (Guid)currentTeller.ChartOfAccountId,
                    currentPostingPeriod.DurationStartDate,
                    currentPostingPeriod.DurationEndDate,
                    jQueryDataTablesModel.sSearch,
                    20,
                    1,
                    true,
                    GetServiceHeader()
                );


                if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
                {
                    totalRecordCount = pageCollectionInfo.ItemsCount;


                    pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.JournalCreatedDate).ToList();


                    searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                    return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
                }
                else return this.DataTablesJson(items: new List<GeneralLedgerTransaction> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);

            }

            return this.DataTablesJson(items: new List<GeneralLedgerTransaction> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);

        }


        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            Guid parseId;

            TransactionModel model = new TransactionModel();

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

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());

            if (customer == null)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            return Json(customer, JsonRequestBehavior.AllowGet);
        }


        public async Task<JsonResult> GetCustomerAccountDetailsJson(Guid? id)
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

            var customerAccount = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            if (customerAccount == null)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            return Json(customerAccount, JsonRequestBehavior.AllowGet);
        }



        [HttpPost]
        public async Task<ActionResult> Create(TransactionModel model)
        {

            var currentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());

            var currentUser = await _applicationUserManager.FindByEmailAsync("calvince.ochieng@swizzsoft.com");
            var currentTeller = await _channelService.FindTellerByEmployeeIdAsync((Guid)currentUser.EmployeeId, true, GetServiceHeader());

            
            model.BranchId = currentTeller.EmployeeBranchId;

            var selectedBranch = await _channelService.FindBranchAsync(model.BranchId, GetServiceHeader());

            model.PostingPeriodId = currentPostingPeriod.Id;

 
            model.DebitChartOfAccountId = (Guid)currentTeller.ChartOfAccountId;
            model.CreditChartOfAccountId = (Guid)currentTeller.ChartOfAccountId;

            model.PrimaryDescription = "ok";
            model.SecondaryDescription = string.Format("B{0}/T{1}/#{2}", selectedBranch.Code, currentTeller.Code, currentTeller.ItemsCount);

        
            model.ValidateAll();
    
             if (!model.HasErrors)
             {
              //# TODO refer tariffs with amos
                //var tariffs = await _channelService.ComputeTellerCashTariffsAsync(model.DebitCustomerAccount, model.TotalValue, 2, GetServiceHeader());
                var dynamicCharges = await _channelService.FindDynamicChargesAsync(GetServiceHeader());
                ObservableCollection<ApportionmentWrapper> apportionmentsObservableCollection = new ObservableCollection<ApportionmentWrapper>(model.Apportionments);

                var journal = _channelService.AddJournalWithApportionmentsAsync(model, apportionmentsObservableCollection, null, dynamicCharges, GetServiceHeader());


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

                    return Json(new { success = true, message = "Operation Success" });
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

                    return Json(new { success = false, message = "Operation Failed" });
                }

                //service.BeginAddJournalWithApportionments(transactionModel.BranchId, transactionModel.AlternateChannelLogId, transactionModel.TotalValue, transactionModel.PrimaryDescription, transactionModel.SecondaryDescription, transactionModel.Reference, transactionModel.ModuleNavigationItemCode, transactionModel.TransactionCode, transactionModel.ValueDate, transactionModel.DebitChartOfAccountId, transactionModel.CreditCustomerAccount, transactionModel.DebitCustomerAccount, apportionments.ExtendedToList(), tariffs.ExtendedToList(), dynamicCharges.ExtendedToList(), asyncCallback, service);
                //journal.PostDoubleEntries(debitChartOfAccountId, creditChartOfAccountId, creditCustomerAccountDTO.Id, debitCustomerAccountDTO.Id, serviceHeader);
                //ViewBag.ApportionToSelectList = GetApportionToSelectList(string.Empty);


            }
            else
            {
                var errorMessages = model.ErrorMessages;
    
                ViewBag.ApportionToSelectList = GetApportionToSelectList(string.Empty);

                return Json(new { success = false, errorMessages = errorMessages });
            }
        }

    }

}