using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using System.Threading.Tasks;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Models;
using System.Windows.Forms;
using Microsoft.AspNet.Identity;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class SundryPaymentsController : MasterController
    {


        EmployeeDTO _selectedEmployee;

        CustomerAccountDTO _selectedCustomerAccount;

        BranchDTO _selectedBranch;

        TellerDTO _selectedTeller;

        CustomerDTO _selectedCustomer;

        PostingPeriodDTO _currentPostingPeriod;

        private readonly string _connectionString;



        decimal PreviousTellerBalance;
        decimal NewTellerBalance;
        private PageCollectionInfo<GeneralLedgerTransaction> TellerStatements;



        private bool IsBusy { get; set; }
        public PostingPeriodDTO CurrentPostingPeriod
        {

            get { return _currentPostingPeriod; }

            set
            {
                if (_currentPostingPeriod != value)
                {
                    _currentPostingPeriod = value;
                }
            }
        }

        public CustomerDTO SelectedCustomer

        {

            get { return _selectedCustomer; }

            set
            {
                if (_selectedCustomer != value)
                {

                    _selectedCustomer = value;
                }
            }
        }

        public EmployeeDTO SelectedEmployee
        {
            get { return _selectedEmployee; }
            set
            {
                if (_selectedEmployee != value)
                {
                    _selectedEmployee = value;

                }
            }
        }


        public CustomerAccountDTO SelectedCustomerAccount
        {
            get { return _selectedCustomerAccount; }
            set
            {
                if (_selectedCustomerAccount != value)
                {
                    _selectedCustomerAccount = value;

                }
            }
        }

        public BranchDTO SelectedBranch
        {
            get { return _selectedBranch; }
            set
            {
                if (_selectedBranch != value)
                {
                    _selectedBranch = value;

                }
            }
        }

        public TellerDTO SelectedTeller
        {
            get { return _selectedTeller; }
            set
            {
                if (_selectedTeller != value)
                {
                    _selectedTeller = value;

                }
            }
        }

        GeneralTransactionType _generalTransactionType;
        public GeneralTransactionType GeneralTransactionType
        {
            get { return _generalTransactionType; }
            set
            {
                if (_generalTransactionType != value)
                {
                    _generalTransactionType = value;

                }
            }
        }

        // GET: FrontOffice/SundryPayments
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
            ViewBag.TransactionTypeSelectList = GetGeneralTransactionTypeSelectList(string.Empty);

            ViewBag.AccountClosureRequestStatus = GetAccountClosureSelectList(string.Empty);

            ViewBag.AccountClosureRequestCustomerAccountCustomerFilter = GetCustomerFilterSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            ViewBag.TransactionTypeSelectList = GetGeneralTransactionTypeSelectList(string.Empty);

            ViewBag.AccountClosureRequestStatus = GetAccountClosureSelectList(string.Empty);

            ViewBag.AccountClosureCustomerAcountCustomerFilter = GetCustomerFilterSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(ExpensePayableDTO expensePayableDTO)
        {


             _currentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());

            var currentUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            _selectedTeller = await _channelService.FindTellerByEmployeeIdAsync((Guid)currentUser.EmployeeId, true, GetServiceHeader());

            _selectedBranch = await _channelService.FindBranchAsync(SelectedTeller.EmployeeBranchId, GetServiceHeader());

            expensePayableDTO.BranchId = SelectedTeller.EmployeeBranchId;


            expensePayableDTO.ValidateAll();


            if (!expensePayableDTO.HasErrors)
            {
                decimal totalValue = expensePayableDTO.TotalValue;
                int generalTransactionType = expensePayableDTO.TransactionType;

                var transactionModel = new TransactionModel();
                transactionModel.TotalValue = totalValue;
                transactionModel.BranchId = expensePayableDTO.BranchId;

                transactionModel.PostingPeriodId = CurrentPostingPeriod.Id;
                //transactionModel.DebitChartOfAccountId = SelectedCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId;
                transactionModel.PrimaryDescription = "ok";
                transactionModel.SecondaryDescription = string.Format("B{0}/T{1}/#{2}", SelectedBranch.Code, _selectedTeller.Code, _selectedTeller.ItemsCount);
                transactionModel.Reference = string.Format("{0}", SelectedTeller.ChartOfAccountId);

                

                //await _channelService.ComputeTellerCashTariffsAsync(customerAccountDTO, totalValue, frontOfficeTransactionType, GetServiceHeader());

                switch ((GeneralTransactionType)expensePayableDTO.TransactionType)
                {
                    case GeneralTransactionType.CashPayment:

                        transactionModel.TransactionCode = (int)SystemTransactionCode.GeneralCashPayment;

                        if (SelectedTeller != null && !SelectedTeller.IsLocked)
                            transactionModel.DebitChartOfAccountId = expensePayableDTO.ChartOfAccountId;
                        transactionModel.CreditChartOfAccountId = (Guid)SelectedTeller.ChartOfAccountId;

                        var journal = await _channelService.AddJournalAsync(transactionModel, null, GetServiceHeader());


                        if (journal != null)
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



                        break;

                    case GeneralTransactionType.CashPaymentAccountClosure:
                
                        if (SelectedCustomerAccount != null)
                        {
                            transactionModel.DebitCustomerAccount = SelectedCustomerAccount;
                            transactionModel.DebitCustomerAccountId = SelectedCustomerAccount.Id;
                            transactionModel.CreditCustomerAccountId = SelectedCustomerAccount.Id;
                            transactionModel.CreditCustomerAccount = SelectedCustomerAccount;
                            transactionModel.CreditChartOfAccountId = SelectedCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId;
                        }

                        if (SelectedTeller != null && !SelectedTeller.IsLocked)
                            transactionModel.DebitChartOfAccountId = SelectedTeller.ChartOfAccountId ?? Guid.Empty;

                        break;
                }

                
                return RedirectToAction("Create");
            }
            else
            {
                var errorMessages = expensePayableDTO.ErrorMessages;
                // ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(cashDepositRequestDTO.CustomerAccountCustomerTypeDescription.ToString());
                ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(expensePayableDTO.Type.ToString());

                return View(expensePayableDTO);
            }
        }

        [HttpPost] 
        public async Task<JsonResult> FetchAccountClosureRequestsTable(JQueryDataTablesModel jQueryDataTablesModel, int status)

        {
            int customerFilter = 1;

            bool includeProductDescription = true; 
            //var currentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());

            //var currentUser = await _applicationUserManager.FindByEmailAsync("calvince.ochieng@swizzsoft.com");
            //var currentTeller = await _channelService.FindTellerByEmployeeIdAsync((Guid)currentUser.EmployeeId, true, GetServiceHeader());

            int totalRecordCount = 0;

            int searchRecordCount = 0;

            DateTime startDate = DateTime.Now;

            DateTime endDate = DateTime.Now;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;


            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindAccountClosureRequestsByStatusAndFilterInPageAsync(startDate, endDate, status, jQueryDataTablesModel.sSearch, customerFilter, pageIndex, jQueryDataTablesModel.iDisplayLength, includeProductDescription, GetServiceHeader());

                if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
                {
                    totalRecordCount = pageCollectionInfo.ItemsCount;


                    pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.CreatedDate).ToList();


                    searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                    return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
                }
                else return this.DataTablesJson(items: new List<AccountClosureRequestDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }
    }
}