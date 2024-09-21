using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Domain.MainBoundedContext.AccountsModule.Aggregates.PostingPeriodAgg;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class CashDepositController : MasterController
    {

        EmployeeDTO _selectedEmployee;

        CustomerAccountDTO _selectedCustomerAccount;

        BranchDTO _selectedBranch;

        TellerDTO _selectedTeller;

        PaymentVoucherDTO _selectedPaymentVoucher;

        CustomerDTO _selectedCustomer;

        PostingPeriodDTO _currentPostingPeriod;




        decimal PreviousTellerBalance;
        decimal NewTellerBalance;
        private PageCollectionInfo<GeneralLedgerTransaction> TellerStatements;



        private bool IsBusy { get; set; } // Property to indicate if an operation is in progress

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



        public PaymentVoucherDTO SelectedPaymentVoucher
        {
            get { return _selectedPaymentVoucher; }

            set
            {
                if (_selectedPaymentVoucher != value)
                {

                    _selectedPaymentVoucher = value;
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



        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }


        [HttpPost]
        public async Task<JsonResult> FetchCustomerAccountsTable(JQueryDataTablesModel jQueryDataTablesModel, int productCode, int customerFilter)
        {
            

            int totalRecordCount = 0;

            int searchRecordCount = 0;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();


            var pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeAndFilterInPageAsync(productCode, jQueryDataTablesModel.sSearch, customerFilter, pageIndex, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());
            //var pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeFilterInPageAsync(productCode, recordStatus, jQueryDataTablesModel.sSearch, 2, pageIndex, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            _currentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
            _selectedTeller = await GetCurrentTeller();

            int totalRecordCount = 0;

            int searchRecordCount = 0;

            DateTime startDate = DateTime.Now;

            DateTime endDate = DateTime.Now;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;


            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();



            if (SelectedTeller != null)
            {
                var pageCollectionInfo = await _channelService.FindGeneralLedgerTransactionsByChartOfAccountIdAndDateRangeAndFilterInPageAsync(
                    pageIndex,
                    jQueryDataTablesModel.iDisplayLength,
                    (Guid)SelectedTeller.ChartOfAccountId,
                    CurrentPostingPeriod.DurationStartDate,
                    CurrentPostingPeriod.DurationEndDate,
                    jQueryDataTablesModel.sSearch,
                    20,
                    1,
                    true,
                    GetServiceHeader()
                );


                if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
                {
                    totalRecordCount = pageCollectionInfo.ItemsCount;

                    //pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.JournalCreatedDate).ToList();

                    searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                    return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
                }
                else return this.DataTablesJson(items: new List<GeneralLedgerTransaction> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);

            }

            return this.DataTablesJson(items: new List<GeneralLedgerTransaction> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);

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
            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.CustomerFilterSelectList = GetCustomerFilterSelectList(string.Empty);

            CustomerTransactionModel transactionModel = new CustomerTransactionModel();


            _currentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
            _selectedTeller = await GetCurrentTeller();

            if (_selectedTeller != null)
            {
                var chartOfAccountId = _selectedTeller.ChartOfAccountId;
                var chartOfAccount = await _channelService.FindChartOfAccountAsync((Guid)chartOfAccountId, GetServiceHeader());
                var generalLedgerAccount = await _channelService.FindGeneralLedgerAccountAsync((Guid)chartOfAccountId, true, GetServiceHeader());


                _selectedEmployee = await _channelService.FindEmployeeAsync((Guid)_selectedTeller.EmployeeId, GetServiceHeader());

                var TellerStatements = await _channelService.FindGeneralLedgerTransactionsByChartOfAccountIdAndDateRangeAndFilterInPageAsync(0, 10, (Guid)SelectedTeller.ChartOfAccountId, CurrentPostingPeriod.DurationStartDate, CurrentPostingPeriod.DurationEndDate, "", 0, 2, true, GetServiceHeader());


                SelectedTeller.BookBalance = generalLedgerAccount.Balance;
                transactionModel.Teller.BookBalance = SelectedTeller.BookBalance;
                transactionModel.TellerStatements = TellerStatements;

            }



            if (id == null || id == Guid.Empty || !Guid.TryParse(id.ToString(), out Guid parseId))
            {

                return View(transactionModel);
            }

            bool includeBalances = true;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            // Fetch customer details
            var customerAccount = await _channelService.FindCustomerAccountAsync(
                parseId,
                includeBalances,
                includeProductDescription,
                includeInterestBalanceForLoanAccounts,
                considerMaturityPeriodForInvestmentAccounts,
                GetServiceHeader()
            );

            if (customerAccount != null)
            {
                transactionModel.CustomerAccount = new CustomerAccountDTO
                {
                    Id = customerAccount.Id,
                    CustomerId = customerAccount.CustomerId,
                    CustomerIndividualFirstName = customerAccount.CustomerFullName,
                    CustomerIndividualPayrollNumbers = customerAccount.CustomerIndividualPayrollNumbers,
                    CustomerSerialNumber = customerAccount.CustomerSerialNumber,
                    CustomerReference1 = customerAccount.CustomerReference1,
                    CustomerReference2 = customerAccount.CustomerReference2,
                    CustomerReference3 = customerAccount.CustomerReference3,
                    CustomerIndividualIdentityCardNumber = customerAccount.CustomerIndividualIdentityCardNumber,
                    Remarks = customerAccount.Remarks,
                    CustomerAccountTypeTargetProductDescription = customerAccount.CustomerAccountTypeTargetProductDescription,
                    BranchId = customerAccount.BranchId,
                    BranchDescription = customerAccount.BranchDescription,
                    CustomerAccountTypeTargetProductId = customerAccount.CustomerAccountTypeTargetProductId,
                    CustomerAccountTypeTargetProductCode = customerAccount.CustomerAccountTypeTargetProductCode,
                    CustomerAccountTypeTargetProductParentId = customerAccount.CustomerAccountTypeTargetProductParentId,
                    CustomerAccountTypeProductCode = customerAccount.CustomerAccountTypeProductCode,
                    AvailableBalance = customerAccount.AvailableBalance,
                    NewAvailableBalance = customerAccount.NewAvailableBalance,
                    BookBalance = customerAccount.BookBalance,
                    CustomerAccountTypeTargetProductMaximumAllowedDeposit = customerAccount.CustomerAccountTypeTargetProductMaximumAllowedDeposit,
                    CustomerAccountTypeTargetProductMaximumAllowedWithdrawal = customerAccount.CustomerAccountTypeTargetProductMaximumAllowedWithdrawal,
                    CustomerAccountTypeTargetProductChartOfAccountId = customerAccount.CustomerAccountTypeTargetProductChartOfAccountId

                };

                //customer account uncleared cheques
                var uncleatedChequescollection = await _channelService.FindUnClearedExternalChequesByCustomerAccountIdAsync(customerAccount.Id, GetServiceHeader());
                var _unclearedCheques = uncleatedChequescollection.ToList();
                transactionModel.CustomerAccountUnclearedCheques = _unclearedCheques;

                //customer account signatories
                var signatoriesCollection = await _channelService.FindCustomerAccountSignatoriesByCustomerAccountIdAsync(customerAccount.Id, GetServiceHeader());
                var _signatories = signatoriesCollection.ToList();
                transactionModel.CustomerAccountSignatories = _signatories;


                //customer acount ministatement
                var miniStatementOrdersCollection = await _channelService.FindElectronicStatementOrdersByCustomerAccountIdAsync(customerAccount.Id, true, GetServiceHeader());
                var _miniStatement = miniStatementOrdersCollection.ToList();
                transactionModel.CustomerAccountMiniStatement = _miniStatement;

                var customerDTO = await _channelService.FindCustomerAsync(customerAccount.CustomerId, GetServiceHeader());
                transactionModel.BranchId = customerAccount.BranchId;
                transactionModel.CustomerDTO = customerDTO;

                _ = _selectedTeller != null ? transactionModel.Teller = _selectedTeller : TempData["Missing Teller"] = "You are working without a Recognized Teller";

                ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
                return View(transactionModel);
            }
            // If no customer is found, return the view with no model
            return View();
        }


        private async Task ProcessCustomerTransactionAsync(CustomerTransactionModel transactionModel)
        {

            var proceedAuthorizedCashWithdrawalRequest = default(bool);
            var proceedCashWithdrawalAuthorizationRequest = default(bool);
            var proceedAuthorizedCashDepositRequest = default(bool);
            var proceedCashDepositAuthorizationRequest = default(bool);



            System.Globalization.NumberFormatInfo _nfi = new CultureInfo("en-US", false).NumberFormat;


            try
            {


                int frontOfficeTransactionType = transactionModel.CustomerAccount.Type;

                var tariffs = await _channelService.ComputeTellerCashTariffsAsync(transactionModel.CustomerAccount, transactionModel.TotalValue, frontOfficeTransactionType, GetServiceHeader());

                switch ((FrontOfficeTransactionType)frontOfficeTransactionType)
                {
                    case FrontOfficeTransactionType.CashDeposit:
                        var cashDepositCategory = CashDepositCategory.WithinLimits;

                        if (transactionModel.TotalValue > SelectedCustomerAccount.CustomerAccountTypeTargetProductMaximumAllowedDeposit)
                        {
                            cashDepositCategory = CashDepositCategory.AboveMaximumAllowed;
                        }

                        switch (cashDepositCategory)
                        {
                            case CashDepositCategory.WithinLimits:
                                var withinLimitsCashDepositJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(transactionModel, tariffs, GetServiceHeader());

                                transactionModel.CustomerAccount.NewAvailableBalance = transactionModel.CustomerAccount.AvailableBalance + transactionModel.TotalValue;
                                var updateWithinLimitResult = await _channelService.UpdateCustomerAccountAsync(transactionModel.CustomerAccount, GetServiceHeader());


                                if (updateWithinLimitResult)
                                {
                                    string message = $"Operation success: Customer's new balance is {transactionModel.CustomerAccount.NewAvailableBalance}";

                                    MessageBox.Show(
                                                      message,
                                                      "CashDeposit Request",
                                                      MessageBoxButtons.OK,
                                                      MessageBoxIcon.Information,
                                                      MessageBoxDefaultButton.Button1,
                                                      MessageBoxOptions.ServiceNotification
                                                  );
                                }


                                //PrintReceipt(withinLimitsCashDepositJournal);
                                break;

                            case CashDepositCategory.AboveMaximumAllowed:

                                var createNewCashDepositRequest = default(bool);

                                var actionableCashDepositRequests = await _channelService.FindActionableCashDepositRequestsByCustomerAccountAsync(SelectedCustomerAccount, GetServiceHeader());

                                if (actionableCashDepositRequests != null && actionableCashDepositRequests.Any())
                                {
                                    var targetCashDepositRequest = actionableCashDepositRequests.Where(x => x.Id == transactionModel.CashDepositRequest.Id).FirstOrDefault();

                                    if (targetCashDepositRequest != null)
                                    {

                                        // Check if another operation is already in progress
                                        if (IsBusy)
                                        {
                                            MessageBox.Show("Please wait until the current operation is complete.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            return;
                                        }

                                        // Set IsBusy to true to indicate an ongoing operation
                                        IsBusy = true;


                                        // Format the message
                                        string message = string.Format(
                                            "{0} Authorization Request of {1} is {2} for this customer account.\n\nDo you want to proceed?",
                                            EnumHelper.GetDescription(CashDepositCategory.AboveMaximumAllowed),
                                            string.Format(_nfi, "{0:C}", targetCashDepositRequest.Amount),
                                            targetCashDepositRequest.StatusDescription,
                                            MessageBoxDefaultButton.Button1,
                                            MessageBoxOptions.ServiceNotification
                                        );

                                        // Show a message box with Yes/No options
                                        DialogResult result = MessageBox.Show(
                                            Form.ActiveForm,
                                            message,
                                            "Authorization Request",
                                            MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Question,
                                            MessageBoxDefaultButton.Button1,
                                            MessageBoxOptions.ServiceNotification
                                        );

                                        if (result == DialogResult.Yes && !proceedAuthorizedCashDepositRequest)
                                        {
                                            proceedAuthorizedCashDepositRequest = true;

                                            #region Proceed with Authorized Transaction Request

                                            if (targetCashDepositRequest.Status == (int)CashDepositRequestAuthStatus.Authorized)
                                            {
                                                if (await _channelService.PostCashDepositRequestAsync(targetCashDepositRequest, GetServiceHeader()))
                                                {
                                                    var authorizedJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(transactionModel, tariffs);

                                                    //basic
                                                    transactionModel.CustomerAccount.NewAvailableBalance = transactionModel.CustomerAccount.AvailableBalance + transactionModel.TotalValue;
                                                    var updateAboveLimitResult = await _channelService.UpdateCustomerAccountAsync(transactionModel.CustomerAccount, GetServiceHeader());


                                                    if (updateAboveLimitResult)
                                                    {
                                                        TempData["RequestSuccess"] = $"success: customer new balance is {transactionModel.CustomerAccount.NewAvailableBalance}";
                                                    }

                                                    //PrintReceipt(authorizedJournal);

                                                }
                                                else
                                                {
                                                    MessageBox.Show(
                                                        "Sorry, but the authorized cash deposit request could not be marked as posted!",
                                                        "Authorization Request",
                                                        MessageBoxButtons.OK,
                                                        MessageBoxIcon.Exclamation
                                                    );

                                                    //ResetView();
                                                }
                                            }
                                            else
                                            {
                                                IsBusy = false;
                                            }

                                            #endregion
                                        }
                                        else
                                        {
                                            IsBusy = false;
                                        }
                                    }
                                    else createNewCashDepositRequest = true;
                                }
                                else createNewCashDepositRequest = true;

                                if (createNewCashDepositRequest)
                                {
                                    string message = string.Format(
                                        "{0}.\nDo you want to proceed and place a cash deposit authorization request?",
                                        EnumHelper.GetDescription(cashDepositCategory)
                                    );

                                    // Show the message box with Yes/No options
                                    DialogResult result = MessageBox.Show(
                                        message,
                                        "Authorization Request",
                                        MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Question,
                                        MessageBoxDefaultButton.Button1,
                                        MessageBoxOptions.ServiceNotification
                                    );

                                    if (result == DialogResult.Yes && !proceedCashDepositAuthorizationRequest)
                                    {
                                        proceedCashDepositAuthorizationRequest = true;

                                        #region Proceed to Place Cash Deposit Authorization Request

                                        var customerTransactionAuthRequest = new CashDepositRequestDTO
                                        {
                                            BranchId = SelectedBranch.Id,
                                            CustomerAccountId = SelectedCustomerAccount.Id,
                                            CustomerAccountCustomerAccountTypeTargetProductId = SelectedCustomerAccount.CustomerAccountTypeTargetProductId,
                                            CustomerAccountCustomerAccountTypeProductCode = SelectedCustomerAccount.CustomerAccountTypeProductCode,
                                            CustomerAccountCustomerAccountTypeTargetProductCode = SelectedCustomerAccount.CustomerAccountTypeTargetProductCode,
                                            Amount = transactionModel.TotalValue,
                                            Remarks = transactionModel.Reference,
                                        };

                                        try
                                        {
                                            var opResult = await _channelService.AddCashDepositRequestAsync(customerTransactionAuthRequest);

                                            if (opResult != null)
                                            {
                                                // Clear data and reset view
                                                transactionModel = null;
                                                SelectedCustomerAccount = null;
                                                //SelectedCustomer = null;

                                                //WithDrawalAmount = 0m;
                                                //DepositAmount = 0m;
                                                //Tariffs = null;

                                                // Notify users
                                                //await _onlineNotifier.NotifyUsersInPermissionType((int)SystemPermissionType.CashDepositRequestAuthorization, string.Format("{0} Request!", EnumHelper.GetDescription(cashDepositCategory)));

                                                MessageBox.Show(
                                                    Form.ActiveForm,
                                                    "Operation completed successfully.",
                                                    "Success",
                                                    MessageBoxButtons.OK,
                                                    MessageBoxIcon.Information,
                                                     MessageBoxDefaultButton.Button1,
                                                     MessageBoxOptions.ServiceNotification
                                                );
                                            }
                                            else
                                            {
                                                // Show failure message
                                                MessageBox.Show(
                                                    "Operation failed!",
                                                    "Error",
                                                    MessageBoxButtons.OK,
                                                    MessageBoxIcon.Error,
                                                     MessageBoxDefaultButton.Button1,
                                                     MessageBoxOptions.ServiceNotification
                                                );

                                                // Reset the view or form
                                                //ResetView();
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            // Show exception message
                                            MessageBox.Show(

                                                $"An error occurred: {ex.Message}",
                                                "Error",
                                                MessageBoxButtons.OK,
                                                MessageBoxIcon.Error,
                                                 MessageBoxDefaultButton.Button1,
                                                     MessageBoxOptions.ServiceNotification
                                            );

                                            // Reset the view or form
                                            //ResetView();
                                        }

                                        #endregion
                                    }
                                    else
                                    {
                                        IsBusy = false;
                                    }
                                }


                                break;

                            // Handle other categories if needed
                            default:
                                break;
                        }

                        break;

                    // Handle other transaction types if needed

                    case FrontOfficeTransactionType.CashWithdrawal:
                    case FrontOfficeTransactionType.CashWithdrawalPaymentVoucher:

                        if (transactionModel.Teller.BookBalance < transactionModel.TotalValue)
                        {
                            MessageBox.Show(
                                "Sorry, but your teller G/L account has insufficient cash!", // Message
                                "Cash Withdrawal", // Title of the message box
                                MessageBoxButtons.OK, // Button to display
                                MessageBoxIcon.Exclamation, // Icon to display (exclamation mark)
                                 MessageBoxDefaultButton.Button1,
                                MessageBoxOptions.ServiceNotification
                            );

                            //ResetView();
                        }

                        else
                        {
                            var cashWithdrawalCategory = CashWithdrawalCategory.WithinLimits;

                            if (FrontOfficeTransactionType == FrontOfficeTransactionType.CashWithdrawalPaymentVoucher)
                            {
                                cashWithdrawalCategory = CashWithdrawalCategory.PaymentVoucher;
                            }
                            else if (transactionModel.TotalValue > SelectedCustomerAccount.CustomerAccountTypeTargetProductMaximumAllowedWithdrawal)
                            {
                                cashWithdrawalCategory = CashWithdrawalCategory.AboveMaximumAllowed;
                            }
                            else if (((transactionModel.TotalValue + tariffs.Where(x => x.ChargeBenefactor == (int)ChargeBenefactor.Customer).Sum(x => x.Amount)) > SelectedCustomerAccount.AvailableBalance) && ((transactionModel.TotalValue + tariffs.Sum(x => x.Amount)) <= (SelectedCustomerAccount.AvailableBalance + SelectedCustomerAccount.CustomerAccountTypeTargetProductMinimumBalance)))
                            {
                                cashWithdrawalCategory = CashWithdrawalCategory.BelowMinimumBalance;
                            }

                            //TODO: maybe u want to Check for OverDraw earlier 
                            else if ((transactionModel.TotalValue + tariffs.Where(x => x.ChargeBenefactor == (int)ChargeBenefactor.Customer).Sum(x => x.Amount)) > (SelectedCustomerAccount.AvailableBalance + SelectedCustomerAccount.CustomerAccountTypeTargetProductMinimumBalance))
                            {
                                cashWithdrawalCategory = CashWithdrawalCategory.Overdraw;
                            }

                            switch (cashWithdrawalCategory)
                            {
                                case CashWithdrawalCategory.AboveMaximumAllowed:
                                case CashWithdrawalCategory.BelowMinimumBalance:
                                case CashWithdrawalCategory.PaymentVoucher:

                                    var createNewCashWithdrawalRequest = default(bool);

                                    var actionableCashWithdrawalRequests = await _channelService.FindMatureCashWithdrawalRequestsByCustomerAccountIdAsync(SelectedCustomerAccount, GetServiceHeader());

                                    if (actionableCashWithdrawalRequests != null && actionableCashWithdrawalRequests.Any())
                                    {
                                        if ((actionableCashWithdrawalRequests.Where(x => x.Category == (int)cashWithdrawalCategory)).Any())
                                        {
                                            var targetCashWithdrawalRequest = actionableCashWithdrawalRequests.Where(x => x.Category == (int)cashWithdrawalCategory && x.Amount == transactionModel.TotalValue).FirstOrDefault();

                                            if (targetCashWithdrawalRequest != null)
                                            {
                                                var result = MessageBox.Show(
                                                    string.Format("{0} Authorization Request of {1} is {2} for this customer account.\n\nDo you want to proceed?",
                                                    targetCashWithdrawalRequest.TypeDescription,
                                                    string.Format(_nfi, "{0:C}", targetCashWithdrawalRequest.Amount),
                                                    targetCashWithdrawalRequest.StatusDescription),
                                                    "CashWithdrawal Request",
                                                    MessageBoxButtons.YesNo,
                                                    MessageBoxIcon.Question,
                                                     MessageBoxDefaultButton.Button1,
                                                     MessageBoxOptions.ServiceNotification
                                                );

                                                if (result == DialogResult.Yes && !proceedAuthorizedCashWithdrawalRequest)
                                                {
                                                    proceedAuthorizedCashWithdrawalRequest = true;

                                                    #region Proceed with Authorized Transaction Request?

                                                    if (targetCashWithdrawalRequest.Status == (int)CashWithdrawalRequestAuthStatus.Authorized)
                                                    {
                                                        if (cashWithdrawalCategory == CashWithdrawalCategory.PaymentVoucher)
                                                        {
                                                            SelectedPaymentVoucher.Amount = transactionModel.TotalValue;
                                                            SelectedPaymentVoucher.Reference = transactionModel.Reference;

                                                            SelectedPaymentVoucher.ValidateAll();

                                                            if (SelectedPaymentVoucher.HasErrors)
                                                            {
                                                                MessageBox.Show(
                                                                    string.Join(Environment.NewLine, SelectedPaymentVoucher.ErrorMessages),
                                                                    "CashWithdrawal Request",
                                                                    MessageBoxButtons.OK,
                                                                    MessageBoxIcon.Exclamation,
                                                                     MessageBoxDefaultButton.Button1,
                                                                     MessageBoxOptions.ServiceNotification
                                                                );

                                                                //ResetView();
                                                            }
                                                            else
                                                            {


                                                                bool paymentSuccess = await _channelService.PayCashWithdrawalRequestAsync(targetCashWithdrawalRequest, SelectedPaymentVoucher);

                                                                if (paymentSuccess)
                                                                {



                                                                    var authorizedJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(transactionModel, tariffs);

                                                                    //PrintReceipt(authorizedJournal);
                                                                }
                                                                else
                                                                {
                                                                    MessageBox.Show(
                                                                        "Sorry, but the authorized cash withdrawal request could not be marked as paid!",
                                                                        "Cashwithdrawal Request",
                                                                        MessageBoxButtons.OK,
                                                                        MessageBoxIcon.Exclamation,
                                                                         MessageBoxDefaultButton.Button1,
                                                                       MessageBoxOptions.ServiceNotification
                                                                    );

                                                                    //ResetView();
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (SelectedCustomer.BiometricFingerprintTemplateBuffer != null && SelectedBranch.CompanyEnforceBiometricsForCashWithdrawal)
                                                            {
                                                                //SendCustomerDetailsToAwaitVerification();

                                                                //if (!(await WaitCustomerVerification())) return;
                                                            }

                                                            // Placeholder for the payment processing logic without payment voucher

                                                            bool paymentSuccess = await _channelService.PayCashWithdrawalRequestAsync(targetCashWithdrawalRequest, null);

                                                            if (paymentSuccess)
                                                            {


                                                                var authorizedJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(transactionModel, tariffs);

                                                                //PrintReceipt(authorizedJournal);
                                                            }
                                                            else
                                                            {
                                                                MessageBox.Show(
                                                                    "Sorry, but the authorized cash withdrawal request could not be marked as paid!",
                                                                    "Cashwithdrawal Request",
                                                                    MessageBoxButtons.OK,
                                                                    MessageBoxIcon.Exclamation,
                                                                     MessageBoxDefaultButton.Button1,
                                                                     MessageBoxOptions.ServiceNotification
                                                                );

                                                                //ResetView();
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        IsBusy = false;
                                                    }

                                                    #endregion
                                                }
                                                else
                                                {
                                                    IsBusy = false;
                                                }
                                            }

                                            else createNewCashWithdrawalRequest = true;
                                        }
                                        else createNewCashWithdrawalRequest = true;
                                    }
                                    else createNewCashWithdrawalRequest = true;

                                    if (createNewCashWithdrawalRequest)
                                    {
                                        var result = MessageBox.Show(
                                            string.Format("{0}.\nDo you want to proceed and place a cash withdrawal authorization request?",
                                                EnumHelper.GetDescription(cashWithdrawalCategory)),
                                            "CashWithdrawal Request",
                                            MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Question,
                                             MessageBoxDefaultButton.Button1,
                                              MessageBoxOptions.ServiceNotification
                                        );

                                        if (result == DialogResult.Yes && !proceedCashWithdrawalAuthorizationRequest)
                                        {
                                            proceedCashWithdrawalAuthorizationRequest = true;

                                            #region Proceed to Place Cash Withdrawal Authorization Request?

                                            var totalValueOfUnclearedCheques = 0m;

                                            // Placeholder for finding uncleared external cheques by customer account ID

                                            var unclearedExternalCheques = await _channelService.FindUnClearedExternalChequesByCustomerAccountIdAsync(SelectedCustomerAccount.Id, GetServiceHeader());


                                            if (unclearedExternalCheques != null && unclearedExternalCheques.Any())
                                            {
                                                totalValueOfUnclearedCheques = unclearedExternalCheques.Sum(x => x.Amount);
                                            }

                                            if ((SelectedCustomerAccount.BookBalance - totalValueOfUnclearedCheques) < 0m)
                                            {
                                                MessageBox.Show(
                                                    "Sorry, but the customer's account will be overdrawn!",
                                                    "CashWithdrawal Request",
                                                    MessageBoxButtons.OK,
                                                    MessageBoxIcon.Exclamation,
                                                     MessageBoxDefaultButton.Button1,
                                                    MessageBoxOptions.ServiceNotification
                                                );

                                                //ResetView();
                                            }
                                            else
                                            {
                                                var customerTransactionAuthRequest = new CashWithdrawalRequestDTO
                                                {
                                                    BranchId = SelectedBranch.Id,
                                                    CustomerAccountId = SelectedCustomerAccount.Id,
                                                    CustomerAccountCustomerAccountTypeTargetProductId = SelectedCustomerAccount.CustomerAccountTypeTargetProductId,
                                                    CustomerAccountCustomerAccountTypeProductCode = SelectedCustomerAccount.CustomerAccountTypeProductCode,
                                                    CustomerAccountCustomerAccountTypeTargetProductCode = SelectedCustomerAccount.CustomerAccountTypeTargetProductCode,
                                                    Type = (int)CashWithdrawalRequestType.ImmediateNotice,
                                                    Category = (int)cashWithdrawalCategory,
                                                    Amount = transactionModel.TotalValue,
                                                    Remarks = transactionModel.Reference,
                                                };

                                                // Placeholder for adding a cash withdrawal request

                                                var addRequestResult = await _channelService.AddCashWithdrawalRequestAsync(customerTransactionAuthRequest, GetServiceHeader());
                                                //var addRequestResult = await AddCashWithdrawalRequestAsync(customerTransactionAuthRequest);

                                                if (addRequestResult != null)
                                                {
                                                    transactionModel = null;
                                                    SelectedCustomerAccount = null;
                                                    SelectedCustomer = null;
                                                    //WithDrawalAmount = 0m;
                                                    //DepositAmount = 0m;
                                                    tariffs = null;

                                                    // Placeholder for notifying users in permission type
                                                    // await NotifyUsersInPermissionTypeAsync(
                                                    //(int)SystemPermissionType.CashWithdrawalRequestAuthorization,
                                                    //string.Format("{0} Request!", EnumHelper.GetDescription(cashWithdrawalCategory))
                                                    //);

                                                    MessageBox.Show(
                                                        "Operation completed successfully.",
                                                        "CashWithdrawal Request",
                                                        MessageBoxButtons.OK,
                                                        MessageBoxIcon.Information,
                                                        MessageBoxDefaultButton.Button1,
                                                        MessageBoxOptions.ServiceNotification
                                                    );
                                                }
                                                else
                                                {
                                                    MessageBox.Show(
                                                        "Operation failed!",
                                                        "CashWithdrawal Request",
                                                        MessageBoxButtons.OK,
                                                        MessageBoxIcon.Exclamation,
                                                        MessageBoxDefaultButton.Button1,
                                                        MessageBoxOptions.ServiceNotification
                                                    );

                                                    //ResetView();
                                                }
                                            }

                                            #endregion
                                        }
                                        else
                                        {
                                            IsBusy = false;
                                        }
                                    }


                                    break;

                                case CashWithdrawalCategory.WithinLimits:

                                    //if (SelectedCustomer.BiometricFingerprintTemplateBuffer != null && SelectedBranch.CompanyEnforceBiometricsForCashWithdrawal)
                                    //{
                                    //SendCustomerDetailsToAwaitVerification();

                                    //if (!(await WaitCustomerVerification())) return;
                                    //}

                                    var withinLimitsJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(transactionModel, tariffs, GetServiceHeader());

                                    MessageBox.Show(
                                                       "Operation success",
                                                       "CashWithdrawal Request",
                                                       MessageBoxButtons.OK,
                                                       MessageBoxIcon.Information,
                                                       MessageBoxDefaultButton.Button1,
                                                       MessageBoxOptions.ServiceNotification
                                                   );

                                    //PrintReceipt(withinLimitsJournal);

                                    break;

                                case CashWithdrawalCategory.Overdraw:

                                    MessageBox.Show("Sorry, but the customer's account will be overdrawn!", "CashWithdrawal Request", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1,
                                              MessageBoxOptions.ServiceNotification);


                                    //ResetView();

                                    break;

                                default:
                                    break;
                            }
                        }

                        break;

                    case FrontOfficeTransactionType.ChequeDeposit:

                        var NewExternalCheque = transactionModel.ChequeDeposit;
                        NewExternalCheque.TellerId = SelectedTeller.Id;

                        NewExternalCheque.CustomerAccountId = SelectedCustomerAccount.Id;

                        NewExternalCheque.ValidateAll();

                        if (NewExternalCheque.HasErrors)
                        {
                            //_messageService.ShowExclamation(string.Join(Environment.NewLine, NewExternalCheque.ErrorMessages), this.DisplayName);

                            //ResetView();
                        }
                        else
                        {
                            transactionModel.PrimaryDescription = string.Format("{0} - {1}", transactionModel.PrimaryDescription, NewExternalCheque.Number);

                            var externalChequeResult = await _channelService.AddExternalChequeAsync(NewExternalCheque, GetServiceHeader());

                            if (externalChequeResult != null)
                             {


                                MessageBox.Show("Operation Error", "ChequeDeposit Request", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);

                             }

                            var chequeDepositJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(transactionModel, tariffs, GetServiceHeader());
                            
                            if (chequeDepositJournal != null && !chequeDepositJournal.HasErrors)
                            {
                                MessageBox.Show("Operation Success", "ChequeDeposit Request", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                            }

                            else
                            {
                                MessageBox.Show("Operation failed", "ChequeDeposit Request", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                            }
                        }
                        break;
                    default:
                        //throw new InvalidOperationException("Unsupported transaction type.");
                        MessageBox.Show("You may have entered the wrong transaction type", "CashWithdrawal Request", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1,
                                             MessageBoxOptions.ServiceNotification);
                        return;
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }


        FrontOfficeTransactionType _frontOfficeTransactionType;
        public FrontOfficeTransactionType FrontOfficeTransactionType
        {
            get { return _frontOfficeTransactionType; }
            set
            {
                if (_frontOfficeTransactionType != value)
                {
                    _frontOfficeTransactionType = value;

                }
            }
        }



        [HttpPost]
        public async Task<ActionResult> Create(CustomerTransactionModel transactionModel)
        {

            SelectedCustomerAccount = transactionModel.CustomerAccount;
            //SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(transactionModel.CustomerAccount.Id, false, true, false, false, GetServiceHeader());
            SelectedBranch = await _channelService.FindBranchAsync(transactionModel.BranchId, GetServiceHeader());
            _selectedTeller = await GetCurrentTeller();

            var postingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
            transactionModel.PostingPeriodId = postingPeriod.Id;
            //transactionModel.DebitChartOfAccountId = SelectedCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId;
            transactionModel.PrimaryDescription = "ok";
            transactionModel.SecondaryDescription = string.Format("B{0}/T{1}/#{2}", SelectedBranch.Code, _selectedTeller.Code, _selectedTeller.ItemsCount);
            transactionModel.Reference = string.Format("{0}", SelectedCustomerAccount.CustomerReference1);
            transactionModel.CreditChartOfAccountId = (Guid)transactionModel.Teller.ChartOfAccountId;

            if (transactionModel.CashWithdrawal.Amount > 0 )
            {

                transactionModel.TotalValue = transactionModel.CashWithdrawal.Amount;
            }

            if (transactionModel.ChequeDeposit.Amount > 0)
            {
                transactionModel.TotalValue = transactionModel.ChequeDeposit.Amount;
            }

            

            switch ((FrontOfficeTransactionType)transactionModel.CustomerAccount.Type)
            {
                case FrontOfficeTransactionType.CashDeposit:
                case FrontOfficeTransactionType.ChequeDeposit:    
                  

                    if (SelectedTeller != null && !SelectedTeller.IsLocked)
                        transactionModel.DebitChartOfAccountId = SelectedTeller.ChartOfAccountId ?? Guid.Empty;

                    if (SelectedCustomerAccount != null)
                    {
                        transactionModel.DebitCustomerAccountId = SelectedCustomerAccount.Id;
                        transactionModel.DebitCustomerAccount = SelectedCustomerAccount;
                        transactionModel.CreditCustomerAccountId = SelectedCustomerAccount.Id;
                        transactionModel.CreditCustomerAccount = SelectedCustomerAccount;
                        transactionModel.CreditChartOfAccountId = SelectedCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId;
                    }

                    break;

                case FrontOfficeTransactionType.CashWithdrawal:

                    if (SelectedCustomerAccount != null)
                    {
                        transactionModel.DebitCustomerAccount = SelectedCustomerAccount;
                        transactionModel.DebitCustomerAccountId = SelectedCustomerAccount.Id;
                        transactionModel.CreditCustomerAccountId = SelectedCustomerAccount.Id;
                        transactionModel.CreditCustomerAccount = SelectedCustomerAccount;
                        transactionModel.DebitChartOfAccountId = SelectedCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId;

                    }


                    if (SelectedTeller != null && !SelectedTeller.IsLocked)
                        transactionModel.CreditChartOfAccountId = SelectedTeller.ChartOfAccountId ?? Guid.Empty;

                    break;


            }
                       
            transactionModel.ValidateAll();

            if (transactionModel.HasErrors)
            {
                ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(SelectedCustomerAccount.Type.ToString());
                return View(SelectedCustomerAccount);
            }

            try
            {
                // Call the asynchronous method to process the customer transaction
                await ProcessCustomerTransactionAsync(transactionModel);

                SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(transactionModel.CustomerAccount.Id, false, true, false, false, GetServiceHeader());

                ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(SelectedCustomerAccount.Type.ToString());

     
              
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {

                TempData["ErrorMesage"] = "bad thing";
                // Handle or log the exception as needed
                Console.WriteLine($"An error occurred: {ex.Message}");

                // Set the transaction type select list for the view in case of an error
                ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(SelectedCustomerAccount.Type.ToString());

                return View();
            }
        }




        [HttpGet]
        public async Task<JsonResult> GetTellersAsync()
        {
            var tellersDTOs = await _channelService.FindTellersAsync(GetServiceHeader());

            return Json(tellersDTOs, JsonRequestBehavior.AllowGet);

        }

        private async Task<TellerDTO> GetCurrentTeller() 
        {

            // Get the current user
            var user = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            
            
                var customers = await _channelService.FindCustomersAsync(GetServiceHeader());
                var targetCustomer = customers.FirstOrDefault(c => c.AddressEmail == user.Email);

                if (targetCustomer != null)
                {
                    var employees = await _channelService.FindEmployeesAsync(GetServiceHeader());
                    SelectedEmployee = employees.FirstOrDefault(e => e.CustomerId == targetCustomer.Id);

                    var teller = await _channelService.FindTellerByEmployeeIdAsync(SelectedEmployee.Id, false, GetServiceHeader());

                    return teller;
                }
                else
                {
                    TempData["Missing Teller"] = "You are working without a Recognized Teller";
                    //transactionModel.Teller = new TellerDTO { BookBalance = 0 };

                    return null;
                }
            
          


        }

        public async Task<JsonResult> GetBankDetailsJson(Guid? id)
        {
            Guid parseId;

            if (!Guid.TryParse(id.ToString(), out parseId))
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            var bank = await _channelService.FindBankAsync(parseId, GetServiceHeader());

            if (bank == null)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            return Json(bank, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetChequeTypesAsync()
        {
            var chequeTypes = await _channelService.FindChequeTypesAsync(GetServiceHeader());

            return Json(chequeTypes, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<JsonResult> FetchKnownCharges(CustomerTransactionModel model, string target)
        {
           
         
            var savingsProduct = await _channelService.FindSavingsProductAsync(model.CustomerAccount.CustomerAccountTypeTargetProductId, GetServiceHeader());
            ObservableCollection<CommissionDTO> commissions = null;

            //default to ordinary savings
            if (savingsProduct == null)
            {
                var products = await _channelService.FindSavingsProductsAsync(GetServiceHeader());

                savingsProduct = products[0];
            }
            
            switch (target.ToLower())
            {
                case "#cashdeposit":
                    commissions = await _channelService.FindCommissionsBySavingsProductIdAsync(savingsProduct.Id, (int)SavingsProductKnownChargeType.CashDeposit, GetServiceHeader());
                    break;

                case "#cashwithdrawal":
                    commissions = await _channelService.FindCommissionsBySavingsProductIdAsync(savingsProduct.Id, (int)SavingsProductKnownChargeType.CashWithdrawal, GetServiceHeader());
                    break;

                case "#chequedeposit":
                    commissions = await _channelService.FindCommissionsBySavingsProductIdAsync(savingsProduct.Id, (int)SavingsProductKnownChargeType.ChequeBookCharges, GetServiceHeader());
                    break;

                default:
                    return Json(new { success = false, message = "Unknown transaction type." });
            }

           
            return Json(new { success = true, data = commissions });
        }




    }
}
    

