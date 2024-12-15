using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Application.MainBoundedContext.DTO.MessagingModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Web.Areas.Registry.DocumentsModel;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Protocols;
using System.Windows.Forms;

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

        private readonly string _connectionString;




        private string receiptContent;
        decimal PreviousTellerBalance;
        decimal NewTellerBalance;
        private PageCollectionInfo<GeneralLedgerTransaction> TellerStatements;



        private bool IsBusy { get; set; } // Property to indicate if an operation is in progress

        public CashDepositController()
        {
            // Get connection string from Web.config
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }
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



        public async Task<System.Web.Mvc.ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }


        private async Task<List<Document>> GetDocumentsAsync(Guid id)
        {
            var documents = new List<Document>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT PassportPhoto, SignaturePhoto, IDCardFrontPhoto, IDCardBackPhoto FROM swiftFin_SpecimenCapture WHERE CustomerId = @CustomerId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerId", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            documents.Add(new Document
                            {
                                PassportPhoto = reader.IsDBNull(0) ? null : (byte[])reader[0],
                                SignaturePhoto = reader.IsDBNull(1) ? null : (byte[])reader[1],
                                IDCardFrontPhoto = reader.IsDBNull(2) ? null : (byte[])reader[2],
                                IDCardBackPhoto = reader.IsDBNull(3) ? null : (byte[])reader[3],

                            }
                            );

                        }
                    }
                }
            }

            return documents;
        }

        [HttpPost]
        public async Task<JsonResult> FetchCustomerAccountsTable(JQueryDataTablesModel jQueryDataTablesModel, int productCode, int customerFilter)
      {


            bool includeBalances = false;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            int totalRecordCount = 0;

            int searchRecordCount = 0;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeAndFilterInPageAsync(productCode, jQueryDataTablesModel.sSearch, customerFilter, pageIndex, jQueryDataTablesModel.iDisplayLength, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
     
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
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, DateTime startDate, DateTime endDate)
        {
            _currentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
            _selectedTeller = await GetCurrentTeller();

            int totalRecordCount = 0;

            int searchRecordCount = 0;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;


            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            if (SelectedTeller != null && !SelectedTeller.IsLocked)
            {
                var pageCollectionInfo = await _channelService.FindGeneralLedgerTransactionsByChartOfAccountIdAndDateRangeAndFilterInPageAsync(
                   0, int.MaxValue,
                    (Guid)SelectedTeller.ChartOfAccountId,
                    startDate,
                    endDate,
                    jQueryDataTablesModel.sSearch,
                    20,
                    1,
                    true,
                    GetServiceHeader()
                );


                if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
                {


                    var sortedData = pageCollectionInfo.PageCollection.OrderByDescending(gl => gl.JournalCreatedDate).ToList();

                    totalRecordCount = pageCollectionInfo.ItemsCount;
         
                    var paginatedData = sortedData.Skip(jQueryDataTablesModel.iDisplayStart).Take(jQueryDataTablesModel.iDisplayLength).ToList();

                    searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? sortedData.Count : totalRecordCount;

                    return this.DataTablesJson(items: paginatedData, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
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

            _currentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
            _selectedTeller = await GetCurrentTeller();

            CustomerTransactionModel transactionModel = new CustomerTransactionModel();

            if (_selectedTeller != null)
            {
                var chartOfAccountId = _selectedTeller.ChartOfAccountId;
                var chartOfAccount = await _channelService.FindChartOfAccountAsync((Guid)chartOfAccountId, GetServiceHeader());
                var generalLedgerAccount = await _channelService.FindGeneralLedgerAccountAsync((Guid)chartOfAccountId, true, GetServiceHeader());


                _selectedEmployee = await _channelService.FindEmployeeAsync((Guid)_selectedTeller.EmployeeId, GetServiceHeader());

                var TellerStatements = await _channelService.FindGeneralLedgerTransactionsByChartOfAccountIdAndDateRangeAndFilterInPageAsync(0, 10, (Guid)SelectedTeller.ChartOfAccountId, CurrentPostingPeriod.DurationStartDate, CurrentPostingPeriod.DurationEndDate, "", 0, 2, true, GetServiceHeader());

                //SelectedTeller.BookBalance = generalLedgerAccount.Balance;
                transactionModel.Teller.BookBalance = SelectedTeller.BookBalance;
                transactionModel.TellerStatements = TellerStatements;
                transactionModel.Teller.ChartOfAccountId = SelectedTeller.ChartOfAccountId;
                transactionModel.PostingPeriodId = _currentPostingPeriod.Id;

            }


            if (id != null)
            {
                var documents = await GetDocumentsAsync(id.Value);
                if (documents.Any())
                {
                    var document = documents.First();

                    TempData["PassportPhoto"] = document.PassportPhoto;
                    TempData["SignaturePhoto"] = document.SignaturePhoto;
                    TempData["idCardFront"] = document.IDCardFrontPhoto;
                    TempData["idCardBack"] = document.IDCardBackPhoto;

                    // Sending the images as Base64 encoded strings to be used in AJAX
                    ViewBag.PassportPhoto = document.PassportPhoto != null ? Convert.ToBase64String(document.PassportPhoto) : null;
                    ViewBag.SignaturePhoto = document.SignaturePhoto != null ? Convert.ToBase64String(document.SignaturePhoto) : null;
                    ViewBag.IDCardFrontPhoto = document.IDCardFrontPhoto != null ? Convert.ToBase64String(document.IDCardFrontPhoto) : null;
                    ViewBag.IDCardBackPhoto = document.IDCardBackPhoto != null ? Convert.ToBase64String(document.IDCardBackPhoto) : null;
                }

            }


            if (id == null || id == Guid.Empty || !Guid.TryParse(id.ToString(), out Guid parseId))
            {

                return View(transactionModel);
            }

            bool includeBalances = true;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

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
                    //NewAvailableBalance = customerAccount.NewAvailableBalance,
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
    
            return View(transactionModel);
        }

        private async Task<Boolean> ProcessCustomerTransactionAsync(CustomerTransactionModel transactionModel)
        {

            var proceedAuthorizedCashWithdrawalRequest = default(bool);
            var proceedCashWithdrawalAuthorizationRequest = default(bool);
            var proceedAuthorizedCashDepositRequest = default(bool);
            var proceedCashDepositAuthorizationRequest = default(bool);

            //SelectedCustomerAccount = transactionModel.CustomerAccount;
            SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(transactionModel.CustomerAccount.Id, true, true, true, true, GetServiceHeader());

            _selectedCustomer = await _channelService.FindCustomerAsync(SelectedCustomerAccount.CustomerId, GetServiceHeader());


            System.Globalization.NumberFormatInfo _nfi = new CultureInfo("en-US", false).NumberFormat;
            var time = System.DateTime.Now.ToString("dd/mm/yyyy");

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


                                    PrintReceipt(withinLimitsCashDepositJournal);

                                    string cashDepositTextTemplate = "Dear customer, your account has been credited with a cash deposit of KES {0} at {1} Branch {2}.";
                                    await SendTextNotificationAsync(cashDepositTextTemplate, SelectedCustomer, SelectedCustomerAccount, transactionModel.TotalValue, transactionModel.Reference, transactionModel.PrimaryDescription);
                                    return true;
                                }

                                else
                                {

                                    MessageBox.Show(
                                                "Sorry, but the authorized cash deposit request could not be marked as posted!",
                                                "Authorization Request",
                                                MessageBoxButtons.OK,
                                                MessageBoxIcon.Exclamation);

                                    return false;
                                }
                        
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
                                            return false;
                                        }

                                        // Set IsBusy to true to indicate an ongoing operation
                                        IsBusy = true;

                                        // Format the message
                                        string message = string.Format(
                                            "Txn Request of {1} is {2} for this customer account.\n\nDo you want to proceed?",
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
                                                    var authorizedJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(transactionModel, tariffs, GetServiceHeader());

                                                    //basic
                                                    //transactionModel.CustomerAccount.NewAvailableBalance = transactionModel.CustomerAccount.AvailableBalance + transactionModel.TotalValue;
                                                    var updateAboveLimitResult = await _channelService.UpdateCustomerAccountAsync(transactionModel.CustomerAccount, GetServiceHeader());


                                                    if (updateAboveLimitResult)
                                                    {
                                                        string successmessage = $"Customer new balance is {transactionModel.CustomerAccount.NewAvailableBalance}";

                                                        // Use MessageBox with ServiceNotification to show the message in any context
                                                        MessageBox.Show(
                                                            successmessage,
                                                            "Success",
                                                            MessageBoxButtons.OK,
                                                            MessageBoxIcon.Information,
                                                            MessageBoxDefaultButton.Button1,
                                                            MessageBoxOptions.ServiceNotification
                                                        );


                                                        string cashDepositAboveLimitTextTemplate = "Dear customer, your account has been credited with a cash deposit of KES {0} at {1} Branch {2}.";
                                                        await SendTextNotificationAsync(cashDepositAboveLimitTextTemplate, SelectedCustomer, SelectedCustomerAccount, transactionModel.TotalValue, transactionModel.Reference, transactionModel.PrimaryDescription);

                                                        PrintReceipt(authorizedJournal);
                                                        return true;
                                                    }
                                                    
                                                    else
                                                    {
                                                        MessageBox.Show(
                                                       "Sorry, but the authorized cash deposit request could not be marked as posted!",
                                                       "Authorization Request",
                                                       MessageBoxButtons.OK,
                                                       MessageBoxIcon.Exclamation
                                                   );


                                                        return false;

                                                    }
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
                                                    return false;
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

                                                return true;
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

                                                return false;
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
                                            return false;
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
                            return false;
                        }

                        else
                        {
                            var cashWithdrawalCategory = CashWithdrawalCategory.WithinLimits;

                            if ((FrontOfficeTransactionType)frontOfficeTransactionType == FrontOfficeTransactionType.CashWithdrawalPaymentVoucher)
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

                                    var actionableCashWithdrawalRequests = await _channelService.FindMatureCashWithdrawalRequestsByCustomerAccountIdAsync
                                        (SelectedCustomerAccount, GetServiceHeader());

                                    //var actionableCashWithdrawalRequests = await _channelService.FindActionableCashWithdrawalRequestsByCustomerAccountAsync(SelectedCustomerAccount, GetServiceHeader());

                                    if (actionableCashWithdrawalRequests != null && actionableCashWithdrawalRequests.Any())
                                    {
                                        if ((actionableCashWithdrawalRequests.Where(x => x.Category == (int)cashWithdrawalCategory)).Any())
                                        {
                                            var targetCashWithdrawalRequest = actionableCashWithdrawalRequests.Where(x => x.Category == (int)cashWithdrawalCategory && x.Amount == transactionModel.TotalValue).FirstOrDefault();

                                            if (targetCashWithdrawalRequest != null)
                                            {
                                                var result = MessageBox.Show(
                                                    string.Format("Transaction Request of {1} is {2} for this customer account.\n\nDo you want to proceed?",
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
                                                            PaymentVoucherDTO paymentVoucherDTO = new PaymentVoucherDTO();

                                                            //SelectedPaymentVoucher.Amount = transactionModel.TotalValue;
                                                            //SelectedPaymentVoucher.Reference = transactionModel.Reference;
                                                            paymentVoucherDTO.Amount = transactionModel.TotalValue;
                                                            paymentVoucherDTO.Reference = transactionModel.Reference;
                                                            paymentVoucherDTO.Id = transactionModel.PaymentVoucher.Id;
                                                            paymentVoucherDTO.WriteDate = transactionModel.PaymentVoucher.WriteDate;
                                                            paymentVoucherDTO.Payee = transactionModel.PaymentVoucher.Payee;

                                                            paymentVoucherDTO.ChequeBookId = transactionModel.PaymentVoucher.ChequeBookId;

                                                            paymentVoucherDTO.ValidateAll();

                                                            if (paymentVoucherDTO.HasErrors)
                                                            {
                                                                MessageBox.Show(
                                                                    string.Join(Environment.NewLine, paymentVoucherDTO.ErrorMessages),
                                                                    "CashWithdrawal Request",
                                                                    MessageBoxButtons.OK,
                                                                    MessageBoxIcon.Exclamation,
                                                                     MessageBoxDefaultButton.Button1,
                                                                     MessageBoxOptions.ServiceNotification
                                                                );

                                                                //ResetView();
                                                                return false;
                                                            }
                                                            else
                                                            {


                                                                bool paymentSuccess = await _channelService.PayCashWithdrawalRequestAsync(targetCashWithdrawalRequest, paymentVoucherDTO, GetServiceHeader());

                                                                if (paymentSuccess)
                                                                {

                                                                    var authorizedJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(transactionModel, tariffs, GetServiceHeader());

                                                                    MessageBox.Show(
                                                                      "The authorized cash withdrawal request succesfully be marked as paid!",
                                                                      "Cashwithdrawal Request",
                                                                      MessageBoxButtons.OK,
                                                                      MessageBoxIcon.Information,
                                                                       MessageBoxDefaultButton.Button1,
                                                                     MessageBoxOptions.ServiceNotification
                                                                  );

                                                                    string cashWithdrawalTextTemplate = "Payment voucher of KES {0} {4}, {5} {2}.";
                                                                    await SendTextNotificationAsync(cashWithdrawalTextTemplate, SelectedCustomer, SelectedCustomerAccount, transactionModel.TotalValue, transactionModel.Reference, transactionModel.PrimaryDescription);
                                                                    PrintReceipt(authorizedJournal);
                                                                    return true;
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
                                                                    return false;
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


                                                                var authorizedJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(transactionModel, tariffs, GetServiceHeader());


                                                                string cashWithdrawalTextTemplate = "Dear customer, your account has been debited with KES {0} at {1} Branch {2}.";
                                                                await SendTextNotificationAsync(cashWithdrawalTextTemplate, SelectedCustomer, SelectedCustomerAccount, transactionModel.TotalValue, transactionModel.Reference, transactionModel.PrimaryDescription);


                                                                PrintReceipt(authorizedJournal);
                                                                return true;
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
                                                                return false;
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
                                                    "Sorry, but the customer's total value unucleared cheques exceed book balance!",
                                                    "CashWithdrawal Request",
                                                    MessageBoxButtons.OK,
                                                    MessageBoxIcon.Exclamation,
                                                     MessageBoxDefaultButton.Button1,
                                                    MessageBoxOptions.ServiceNotification
                                                );

                                                //ResetView();
                                                return false;
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
                                                    PaymentVoucherId = transactionModel.PaymentVoucher.Id,
                                                    PaymentVoucherPayee = transactionModel.PaymentVoucher.Payee
                                                };

                                                // Placeholder for adding a cash withdrawal request

                                                var addRequestResult = await _channelService.AddCashWithdrawalRequestAsync(customerTransactionAuthRequest, GetServiceHeader());
                                              

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

                                                    //var updateTargetPaymentVoucherDetails?

                                                    return true;
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
                                                    return false;
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

                                    //if (!(await
                                    //WaitCustomerVerification())) return;
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

                                    string cashWithdrawalTextTemplate1 = "Dear customer, your account has been debited with KES {0} at {1} Branch {2}.";
                                    await SendTextNotificationAsync(cashWithdrawalTextTemplate1, SelectedCustomer, SelectedCustomerAccount, transactionModel.TotalValue, transactionModel.Reference, transactionModel.PrimaryDescription);

                                    PrintReceipt(withinLimitsJournal);
                                    return true;

                                //break;

                                case CashWithdrawalCategory.Overdraw:

                                    MessageBox.Show("Sorry, but the customer's account will be overdrawn!", "CashWithdrawal Request", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1,
                                              MessageBoxOptions.ServiceNotification);


                                    //ResetView();

                                    return false;

                                //break;

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

                            string message = string.Join(Environment.NewLine, NewExternalCheque.ErrorMessages);
                            //string message = NewExternalCheque.ErrorMessages[0];
                            MessageBox.Show(message, "ChequeDeposit Request", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                            //_messageService.ShowExclamation(string.Join(Environment.NewLine, NewExternalCheque.ErrorMessages), this.DisplayName);

                            //ResetView();
                            return false;
                        }
                        else
                        {
                            transactionModel.PrimaryDescription = string.Format("{0} - {1}", transactionModel.PrimaryDescription, NewExternalCheque.Number);

                            var externalChequeResult = await _channelService.AddExternalChequeAsync(NewExternalCheque, GetServiceHeader());

                            
                            if (externalChequeResult != null)
                            {

                                var ExternalChequePayables = new List<ExternalChequePayableDTO>();


                                foreach (Guid payableId in transactionModel.ChequePayableCustomerAccountIds)
                                {


                                    var externalChequePayable = new ExternalChequePayableDTO
                                    {
                                        ExternalChequeId = externalChequeResult.Id,
                                        ExternalChequeNumber = externalChequeResult.Number,
                                        CustomerAccountId = payableId
                                    };

                                    ExternalChequePayables.Add(externalChequePayable);
                                }


                              

                                if (ExternalChequePayables != null)
                                    await _channelService.UpdateExternalChequePayablesByExternalChequeIdAsync(externalChequeResult.Id, new ObservableCollection<ExternalChequePayableDTO>(ExternalChequePayables), GetServiceHeader());
   
                            }

                            var chequeDepositJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(transactionModel, tariffs, GetServiceHeader());


                            if (chequeDepositJournal != null && !chequeDepositJournal.HasErrors)
                            {
                                MessageBox.Show("Operation Success", "ChequeDeposit Request", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                                #region Send Text Notification

                                if (!string.IsNullOrWhiteSpace(SelectedCustomer.AddressMobileLine) &&
                           Regex.IsMatch(SelectedCustomer.AddressMobileLine, @"^\+(?:[0-9]??){6,14}[0-9]$") &&
                           SelectedCustomer.AddressMobileLine.Length >= 13)
                                {
                                    // Build the SMS body message
                                    var smsBody = new StringBuilder();
                                    smsBody.AppendFormat(
                                        "Dear customer, {0} of {1} has been effected on your fosa account at {2} at Branch {3}",
                                        transactionModel.Reference,
                                        transactionModel.TotalValue,
                                        SelectedCustomerAccount.BranchDescription,
                                        SelectedCustomerAccount.BranchCompanyDescription,
                                        DateTime.Now.ToString("MMMM dd, yyyy")
                                    );


                                    var textAlertDTO = new TextAlertDTO
                                    {
                                        BranchId = SelectedCustomerAccount.BranchId,
                                        TextMessageOrigin = (int)MessageOrigin.Within,
                                        TextMessageRecipient = SelectedCustomer.AddressMobileLine,
                                        TextMessageBody = smsBody.ToString(),
                                        MessageCategory = (int)MessageCategory.SMSAlert,
                                        AppendSignature = false,
                                        TextMessagePriority = (int)QueuePriority.Highest,
                                    };


                                    var textAlertDTOs = new ObservableCollection<TextAlertDTO> { textAlertDTO };


                                    await _channelService.AddTextAlertsAsync(textAlertDTOs, GetServiceHeader());
                                }

                                #endregion


                                PrintReceipt(chequeDepositJournal);
                                return true;
                            }

                            else
                            {
                                MessageBox.Show("Operation failed", "ChequeDeposit Request", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                                return false;
                            }
                        }
                    //break;
                    default:
                        //throw new InvalidOperationException("Unsupported transaction type.");
                        MessageBox.Show("You may have entered the wrong transaction type", "CashWithdrawal Request", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1,
                                             MessageBoxOptions.ServiceNotification);
                        return false;
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }

            return false;
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
            bool includeBalances = true;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = true;
            bool considerMaturityPeriodForInvestmentAccounts = true;

            SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(transactionModel.CustomerAccount.Id, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            if (SelectedCustomerAccount == null) {

                MessageBox.Show("Please select a customer account",
               "Cash Transaction",
               MessageBoxButtons.OK,
               MessageBoxIcon.Information,
               MessageBoxDefaultButton.Button1,
               MessageBoxOptions.ServiceNotification
   );

                return Json(new { success = false, message = "Opration failed" });

            }

            if ((RecordStatus)SelectedCustomerAccount.RecordStatus != RecordStatus.Approved)
            {

                MessageBox.Show("Sorry, account is not approved yet",
           "Cash Transaction",
           MessageBoxButtons.OK,
           MessageBoxIcon.Information,
           MessageBoxDefaultButton.Button1,
           MessageBoxOptions.ServiceNotification
           );

                return Json(new { success = false, message = "Opration failed" });


            }





            SelectedBranch = await _channelService.FindBranchAsync(transactionModel.BranchId, GetServiceHeader());
            _selectedTeller = await GetCurrentTeller();

            if (_selectedTeller == null)
            {

                MessageBox.Show("Teller is Missing",
                "Cash Transaction",
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.ServiceNotification

                );

                //return View(SelectedCustomerAccount);

                return Json(new { success = false, message = "Operation Failed" });


            }

            var postingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
            transactionModel.PostingPeriodId = postingPeriod.Id;
            //transactionModel.DebitChartOfAccountId = SelectedCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId;
            transactionModel.PrimaryDescription = "ok";
            transactionModel.SecondaryDescription = string.Format("B{0}/T{1}/#{2}", SelectedBranch.Code, _selectedTeller.Code, _selectedTeller.ItemsCount);
            transactionModel.Reference = string.Format("{0}", SelectedCustomerAccount.CustomerReference1);
            transactionModel.CreditChartOfAccountId = (Guid)SelectedTeller.ChartOfAccountId;


            transactionModel.Teller = SelectedTeller;

            if (transactionModel.CashWithdrawal.Amount > 0)
            {

                transactionModel.TotalValue = transactionModel.CashWithdrawal.Amount;
            }

            if (transactionModel.ChequeDeposit.Amount > 0)
            {
                transactionModel.TotalValue = transactionModel.ChequeDeposit.Amount;
            }

            if (transactionModel.PaymentVoucher.Amount > 0)
            {   
                transactionModel.TotalValue = transactionModel.PaymentVoucher.Amount;
                transactionModel.Reference = transactionModel.PaymentVoucher.Reference;
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
                case FrontOfficeTransactionType.CashWithdrawalPaymentVoucher:

                
                    if (SelectedTeller.BookBalance - transactionModel.TotalValue < SelectedCustomerAccount.CustomerAccountTypeTargetProductMinimumBalance)
                    {

                        MessageBox.Show("Sorry, this transaction will exceed the allowed minimum balance",
                   "Cash Transaction",
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Information,
                   MessageBoxDefaultButton.Button1,
                   MessageBoxOptions.ServiceNotification
                   );

                        return Json(new { success = false, message = "Operation failed" });


                    }

                    if (SelectedTeller.BookBalance - transactionModel.TotalValue < SelectedTeller.RangeLowerLimit)
                    {
                        MessageBox.Show(
                            "Sorry, the transaction will reduce teller's balance below limit",
                            "Cash Transaction",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.ServiceNotification
                        );

                        return Json(new { success = false, message = "Operation failed" });
                    }

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
                var errorMessages = transactionModel.ErrorMessages
                    .Select(error => error)
                    .ToList();

                // Combine all error messages into a single string
                string combinedErrorMessage = string.Join("; ", errorMessages);

                ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(SelectedCustomerAccount.Type.ToString());

                // Show detailed error in a message box
                MessageBox.Show($"Transaction Error: {combinedErrorMessage}",
                    "Cash Transaction",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );

                // Return JSON response with detailed error message
                return Json(new
                {
                    success = false,
                    message = $"Operation Failed: {combinedErrorMessage}"
                });
            }


            try
            {
                // Call the asynchronous method and check its result
                bool isTransactionSuccessful = await ProcessCustomerTransactionAsync(transactionModel);

                if (isTransactionSuccessful)
                {
   
                    SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(transactionModel.CustomerAccount.Id, false, true, false, false, GetServiceHeader());
                    SelectedCustomer = await _channelService.FindCustomerAsync(SelectedCustomerAccount.CustomerId);

                    ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(SelectedCustomerAccount.Type.ToString());

                    var Teller = await GetCurrentTeller();

                    var response = new
                    {
                        success = true,
                        message = "Operation Success",
                        TellerBookBalance = Teller.BookBalance
                    };

                    // Return success response
                    return Json(response);
                }
                else
                {
                    // Transaction failed, return failure response
                    ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(SelectedCustomerAccount.Type.ToString());

                    // Show error message to the user
                    MessageBox.Show("Transaction Failed",
                        "Cash Transaction",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification);

                    return Json(new { success = false, message = "Operation Failed" });
                }
            }
            catch (Exception ex)
            {
                // Handle unexpected exceptions

                ViewBag.TransactionTypeSelectList = SelectedCustomerAccount != null
                    ? GetFrontOfficeTransactionTypeSelectList(SelectedCustomerAccount.Type.ToString())
                    : Enumerable.Empty<SelectListItem>(); // Handle null case safely

                // Log the exception if needed
                // LogError(ex);

                // Show error message to the user
                MessageBox.Show($"An unexpected error occurred: {ex.Message}",
                    "Cash Transaction",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification);

                // Return error response
                return Json(new { success = false, message = "An unexpected error occurred" });
            }

        }

        [HttpPost]
        public async Task<JsonResult> PayAuthorizedCashDepositRequestAsync(Guid cashDepositRequestId)
        {

            var cashDepositRequest = await _channelService.FindCashDepositRequestAsync(cashDepositRequestId, GetServiceHeader());

            SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(cashDepositRequest.CustomerAccountId, true, true, true, true, GetServiceHeader());

            _selectedTeller = await GetCurrentTeller();

            SelectedBranch = await _channelService.FindBranchAsync(SelectedTeller.EmployeeBranchId, GetServiceHeader());
            ;

            var transactionModel = new CustomerTransactionModel();

            var postingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
            transactionModel.PostingPeriodId = postingPeriod.Id;
            //transactionModel.DebitChartOfAccountId = SelectedCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId;
            transactionModel.PrimaryDescription = "ok";
            transactionModel.SecondaryDescription = string.Format("B{0}/T{1}/#{2}", SelectedBranch.Code, _selectedTeller.Code, _selectedTeller.ItemsCount);
            transactionModel.Reference = string.Format("{0}", SelectedCustomerAccount.CustomerReference1);
            transactionModel.CreditChartOfAccountId = (Guid)SelectedTeller.ChartOfAccountId;

            transactionModel.TotalValue = cashDepositRequest.Amount;

            transactionModel.BranchId = SelectedBranch.Id;

            transactionModel.CashDepositRequest = cashDepositRequest;

            SelectedCustomerAccount.Type = (int)FrontOfficeTransactionType.CashDeposit;

            transactionModel.CustomerAccount = SelectedCustomerAccount;
            transactionModel.Teller = SelectedTeller;



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


            try
            {
                // Call the asynchronous method to process the customer transaction
                bool isTransactionSuccessful = await ProcessCustomerTransactionAsync(transactionModel);

                if (isTransactionSuccessful)
                {
                    //Construct the success response

                    var Teller = await GetCurrentTeller();

                    var response = new
                    {
                        Status = "Success",
                        Message = "Cash deposit request authorized successfully.",
                        Amount = transactionModel.TotalValue,
                        AccountNumber = transactionModel.CashDepositRequest.CustomerAccountFullAccountNumber,
                        TellerBookBalance = Teller.BookBalance, 
                        Timestamp = DateTime.Now,

                    };

                    //// Show the success message
                    //MessageBox.Show(response.Message,
                    //    "Cash Transaction",
                    //    MessageBoxButtons.OK,
                    //    MessageBoxIcon.Information,
                    //    MessageBoxDefaultButton.Button1,
                    //    MessageBoxOptions.ServiceNotification);

                    // Return the success response
                    return Json(response);
                }
                else
                {
                    // Construct the failure response
                    var failureResponse = new
                    {
                        Status = "Error",
                        Message = "Cash deposit request failed. Please try again.",
                        Timestamp = DateTime.Now
                    };

                    // Show the error message
                    MessageBox.Show(failureResponse.Message,
                        "Cash Transaction",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification);

                    // Return the failure response
                    return Json(failureResponse);
                }
            }
            catch (Exception ex)
            {
                // Handle unexpected exceptions
                var errorResponse = new
                {
                    Status = "Error",
                    Message = $"An unexpected error occurred: {ex.Message}",
                    Timestamp = DateTime.Now
                };

                // Show the exception error message
                MessageBox.Show(errorResponse.Message,
                    "Cash Transaction",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification);

                // Return the error response
                return Json(errorResponse);
            }
        }


        [HttpPost]
        public async Task<JsonResult> PayAuthorizedCashWithdrawalRequestAsync(Guid cashWithdrawalRequestId)
        {

            var cashWithdrawalRequest = await _channelService.FindCashWithdrawalRequestAsync(cashWithdrawalRequestId, GetServiceHeader());

            SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync((Guid)cashWithdrawalRequest.CustomerAccountId, true, true, true, true, GetServiceHeader());

            _selectedTeller = await GetCurrentTeller();

            SelectedBranch = await _channelService.FindBranchAsync(SelectedTeller.EmployeeBranchId, GetServiceHeader());
            ;

            var transactionModel = new CustomerTransactionModel();

            var postingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
            transactionModel.PostingPeriodId = postingPeriod.Id;
            //transactionModel.DebitChartOfAccountId = SelectedCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId;
            transactionModel.PrimaryDescription = "ok";
            transactionModel.SecondaryDescription = string.Format("B{0}/T{1}/#{2}", SelectedBranch.Code, _selectedTeller.Code, _selectedTeller.ItemsCount);
            transactionModel.Reference = string.Format("{0}", SelectedCustomerAccount.CustomerReference1);
            transactionModel.CreditChartOfAccountId = (Guid)SelectedTeller.ChartOfAccountId;

            

            transactionModel.TotalValue = cashWithdrawalRequest.Amount;

            transactionModel.CashWithdrawal = cashWithdrawalRequest;

            if (cashWithdrawalRequest.Category == (int)CashWithdrawalCategory.PaymentVoucher)
            {

                //var pv = await _channelService.FindPaymentVoucher
                SelectedCustomerAccount.Type = (int)FrontOfficeTransactionType.CashWithdrawalPaymentVoucher;

                // Fetch Cheque Books
                var chequebooks = await _channelService.FindChequeBooksAsync();
                var targetChequeBook = chequebooks.FirstOrDefault(chq => chq.CustomerAccountId == SelectedCustomerAccount.Id);

                if (targetChequeBook == null)
                {
                    var errorMessage = "Operation failed: Missing Cheque Book associated with the selected customer account.";
                    MessageBox.Show(
                        errorMessage,
                        "Cash Transaction",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification
                    );
                    return Json(new { success = false, message = errorMessage });
                }

                // Fetch Payment Vouchers
                var paymentVouchers = await _channelService.FindPaymentVouchersByChequeBookIdAsync(targetChequeBook.Id, GetServiceHeader());
                if (paymentVouchers == null || !paymentVouchers.Any())
                {
                    var errorMessage = "Operation failed: No Payment Vouchers found for the associated Cheque Book.";
                    MessageBox.Show(
                        errorMessage,
                        "Cash Transaction",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification
                    );
                    return Json(new { success = false, message = errorMessage });
                }

                // Fetch Cash Withdrawal Request
                var currentCashWithdrawalRequest = await _channelService.FindCashWithdrawalRequestAsync(cashWithdrawalRequestId, GetServiceHeader());
                if (currentCashWithdrawalRequest == null)
                {
                    var errorMessage = "Operation failed: Missing Cash Withdrawal Request for the given ID.";
                    MessageBox.Show(
                        errorMessage,
                        "Cash Transaction",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification
                    );
                    return Json(new { success = false, message = errorMessage });
                }

                // Validate Target Payment Voucher
                var targetPaymentVoucher = paymentVouchers.FirstOrDefault(pv => pv.Id == currentCashWithdrawalRequest.PaymentVoucherId);
                if (targetPaymentVoucher == null)
                {
                    var errorMessage = "Operation failed: No Payment Voucher matches the Cash Withdrawal Request.";
                    MessageBox.Show(
                        errorMessage,
                        "Cash Transaction",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification
                    );
                    return Json(new { success = false, message = errorMessage });
                }

                //// Proceed with successful operation
                //var successMessage = "Operation successful.";
                //MessageBox.Show(
                //    successMessage,
                //    "Cash Transaction",
                //    MessageBoxButtons.OK,
                //    MessageBoxIcon.Information,
                //    MessageBoxDefaultButton.Button1,
                //    MessageBoxOptions.ServiceNotification
                //);
                //return Json(new { success = true, message = successMessage });


                // Assign data to the transaction model

                targetPaymentVoucher.Payee = cashWithdrawalRequest.PaymentVoucherPayee;
                transactionModel.PaymentVoucher = targetPaymentVoucher;

                // Continue with additional logic...

            }

            else
            {

                SelectedCustomerAccount.Type = (int)FrontOfficeTransactionType.CashWithdrawal;
            }

            transactionModel.CustomerAccount = SelectedCustomerAccount;

            transactionModel.BranchId = SelectedBranch.Id;


            if (SelectedTeller != null && !SelectedTeller.IsLocked)
                transactionModel.CreditChartOfAccountId = SelectedTeller.ChartOfAccountId ?? Guid.Empty;
            transactionModel.Teller = SelectedTeller;
            if (SelectedCustomerAccount != null)
            {
                transactionModel.DebitCustomerAccountId = SelectedCustomerAccount.Id;
                transactionModel.DebitCustomerAccount = SelectedCustomerAccount;
                transactionModel.CreditCustomerAccountId = SelectedCustomerAccount.Id;
                transactionModel.CreditCustomerAccount = SelectedCustomerAccount;
                transactionModel.DebitChartOfAccountId = SelectedCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId;
            }


            try
            {
                bool isTransactionSuccessful = await ProcessCustomerTransactionAsync(transactionModel);

                if (isTransactionSuccessful)
                {
                    var Teller = await GetCurrentTeller();

                    var response = new
                    {
                        Status = "Success",
                        Message = "Cash withdrawal request authorized successfully.",
                        Amount = transactionModel.TotalValue,
                        AccountNumber = transactionModel.CashWithdrawal.CustomerAccountFullAccountNumber,
                        TellerBookBalance = Teller.BookBalance,
                        Timestamp = DateTime.Now
                    };
                    MessageBox.Show(response.Message,
                        "Cash Transaction",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification);

            
                    return Json(response);
                }
                else
                {
                    var failureResponse = new
                    {
                        Status = "Error",
                        Message = "Cash deposit request failed. Please try again.",
                        Timestamp = DateTime.Now
                    };

                    MessageBox.Show(failureResponse.Message,
                        "Cash Transaction",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification);
                    return Json(failureResponse);
                }
            }
            catch (Exception ex)
            {
          
                var errorResponse = new
                {
                    Status = "Error",
                    Message = $"An unexpected error occurred: {ex.Message}",
                    Timestamp = DateTime.Now
                };

                MessageBox.Show(errorResponse.Message,
                    "Cash Transaction",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification);

                // Return the error response
                return Json(errorResponse);
            }
        }


        [HttpPost]
        public async Task<JsonResult> PayAuthorizedTransferRequestAsync(Guid cashTransferRequestId)

        {
            try
            {

                var cashTransfer = await _channelService.FindCashTransferRequestAsync(cashTransferRequestId, GetServiceHeader());


                if (cashTransfer.Utilized)
                {

                    var response = new
                    {
                        Status = "Fail",
                        Message = "The selected transfer has been utilized",
                        Timestamp = DateTime.Now
                    };


                    MessageBox.Show(response.Message,
                     "Cash Transaction",
                     MessageBoxButtons.OK,
                     MessageBoxIcon.Information,
                     MessageBoxDefaultButton.Button1,
                     MessageBoxOptions.ServiceNotification

                     );

                    return Json(response);

                }

                var success = await _channelService.UtilizeCashTransferRequestAsync(cashTransferRequestId, GetServiceHeader());

                if (success)
                {

                    var Teller = await GetCurrentTeller();

                    var response = new
                    {
                        Status = "Success",
                        Message = "Cash Transfer Utilized successfully.",
                        TellerBookBalance = Teller.BookBalance,
                        Timestamp = DateTime.Now
                    };


                    MessageBox.Show(response.Message,
                     "Cash Transaction",
                     MessageBoxButtons.OK,
                     MessageBoxIcon.Information,
                     MessageBoxDefaultButton.Button1,
                     MessageBoxOptions.ServiceNotification

                     );

                    return Json(response);

                }
                else
                {

                    var response = new
                    {
                        Status = "Fail",
                        Message = "Cash Transfer Utilization failed",
                        Timestamp = DateTime.Now
                    };


                    MessageBox.Show(response.Message,
                     "Cash Transaction",
                     MessageBoxButtons.OK,
                     MessageBoxIcon.Information,
                     MessageBoxDefaultButton.Button1,
                     MessageBoxOptions.ServiceNotification

                     );

                    return Json(response);

                }
            }
            catch (Exception ex)
            {

                var errorResponse = new
                {
                    Status = "Error",
                    Message = ex.Message,
                    Timestamp = DateTime.Now
                };
                MessageBox.Show("Error",
                "Cash Transaction",
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.ServiceNotification

                );
                return Json(errorResponse);
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


            bool includeBalance = true; 
            // Get the current user
            var user = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            var teller = await _channelService.FindTellerByEmployeeIdAsync((Guid)user.EmployeeId, includeBalance, GetServiceHeader());

            if (teller == null)
            {
                TempData["Missing Teller"] = "You are working without a Recognized Teller";
            }

            return teller;

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


            SavingsProductDTO savingsProduct = null;

            

            if (model != null)
            { 
              savingsProduct  = await _channelService.FindSavingsProductAsync(model.CustomerAccount.CustomerAccountTypeTargetProductId, GetServiceHeader());
            }
            ObservableCollection<CommissionDTO> commissions = null;

            //default to ordinary savings
            if (savingsProduct == null)
            {
                var products = await _channelService.FindSavingsProductsAsync(GetServiceHeader());

                savingsProduct = products.FirstOrDefault(product => product.Id == Guid.Parse("4623CC2E-E0BB-E811-A815-000C29142092"));

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

                case "#paymentvoucher":
                    commissions = await _channelService.FindCommissionsBySavingsProductIdAsync(savingsProduct.Id, (int)SavingsProductKnownChargeType.CashWithdrawalPaymentVoucher, GetServiceHeader());
                    break;

                default:
                    //return Json(new { success = false, message = "Unknown transaction type." });
                    commissions = await _channelService.FindCommissionsBySavingsProductIdAsync(savingsProduct.Id, (int)SavingsProductKnownChargeType.CashDeposit, GetServiceHeader());
                    break;
            }


            return Json(new { success = true, data = commissions });
        }


        [HttpPost]
        public async Task<JsonResult> FetchChequeBooksTable(JQueryDataTablesModel jQueryDataTablesModel)
        {

            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindChequeBooksByFilterInPageAsync(jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }

            else return this.DataTablesJson(items: new List<ChequeBookDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        [HttpPost]
        public async Task<JsonResult> FetchPaymentVouchersTable(Guid? chequeBookId, JQueryDataTablesModel jQueryDataTablesModel)
        {

            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            if (chequeBookId != null)
            {

                var pageCollectionInfo = await _channelService.FindPaymentVouchersByChequeBookIdAndFilterInPageAsync((Guid)chequeBookId, jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

                if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
                {
                    totalRecordCount = pageCollectionInfo.ItemsCount;

                    pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.CreatedDate).ToList();

                    searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                    return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
                }

                else return this.DataTablesJson(items: new List<PaymentVoucherDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<PaymentVoucherDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);

        }


        [HttpPost]
        public async Task<JsonResult> CashDepositRequestsIndex(JQueryDataTablesModel jQueryDataTablesModel)
        {

            int totalRecordCount = 0;

            int searchRecordCount = 0;

            DateTime startDate = DateTime.MinValue;

            DateTime endDate = DateTime.MaxValue;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;


            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();



            var pageCollectionInfo = await _channelService.FindCashDepositRequestsByFilterInPageAsync(startDate, endDate, (int)CashDepositRequestAuthStatus.Authorized, "", 2, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());



            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;


                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.PostedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CashDepositRequestDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);

        }


        [HttpPost]
        public async Task<JsonResult> CashWithdrawalRequestsIndex(JQueryDataTablesModel jQueryDataTablesModel)
        {

            int totalRecordCount = 0;

            int searchRecordCount = 0;

            DateTime startDate = DateTime.MinValue;

            DateTime endDate = DateTime.MaxValue;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;


            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();


            var pageCollectionInfo = await _channelService.FindCashWithdrawalRequestsByFilterInPageAsync(startDate, endDate, (int)CashWithdrawalRequestAuthStatus.Authorized, "", 2, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());



            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;


                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CashWithdrawalRequestDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        [HttpPost]
        public async Task<JsonResult> CashTransferRequestsIndex(JQueryDataTablesModel jQueryDataTablesModel, int status)
        {

            int totalRecordCount = 0;

            int searchRecordCount = 0;

            DateTime startDate = DateTime.MinValue;

            DateTime endDate = DateTime.MaxValue;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;


            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();


            _selectedTeller = await GetCurrentTeller();

            if (_selectedTeller == null)
            {

                MessageBox.Show("Teller is Missing",
                "Cash Transaction",
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.ServiceNotification

                );

                return Json(new { success = false, message = "Operation Failed" });

            }

            var pageCollectionInfo = await _channelService.FindCashTransferRequestsByFilterInPageAsync((Guid)SelectedTeller.EmployeeId, startDate, endDate, status, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;


                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CashWithdrawalRequestDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        private void PrintReceipt(JournalDTO journal)
        {
            var printConfirmationResult = MessageBox.Show("Do you want to print?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (printConfirmationResult == DialogResult.Yes)
            {


                try
                {
                    // Build the receipt content from the journal data
                    var receiptContent = BuildReceiptContent(journal);

                    PrintDocument printDocument = new PrintDocument();
                    printDocument.PrinterSettings = new PrinterSettings
                    {
                        PrinterName = "EPSON L3250 Series" // Hardcoding the printer name
                    };

                    printDocument.PrintPage += (sender, e) =>
                    {
                        // Draw the receipt content onto the print page
                        e.Graphics.DrawString(receiptContent, new Font("Courier New", 10), Brushes.Black, new RectangleF(0, 0, e.PageBounds.Width, e.PageBounds.Height));
                    };

                    //PrintDialog printDialog = new PrintDialog
                    //{
                    //    Document = printDocument
                    //};

                    //if (printDialog.ShowDialog() == DialogResult.OK)
                    //{
                    //    printDocument.Print();
                    //}

                    printDocument.Print();
                }

                catch (Exception ex)
                {
                    MessageBox.Show($"Error printing receipt: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }                   
        }

        // Helper method to build the receipt content
        private string BuildReceiptContent(JournalDTO journal)
        {
            var builder = new StringBuilder();

            // Add headers
            builder.AppendLine("===== Transaction Receipt =====");
            builder.AppendLine($"Transaction ID: {journal.Id}");
            builder.AppendLine($"Sequential ID: {journal.SequentialId}");
            builder.AppendLine($"Branch: {journal.BranchDescription}");
            builder.AppendLine($"Posting Period: {journal.PostingPeriodDescription}");
            builder.AppendLine($"Total Value: {journal.TotalValue:C}"); // Format as currency
            builder.AppendLine($"Primary Description: {journal.PrimaryDescription}");
            builder.AppendLine($"Secondary Description: {journal.SecondaryDescription}");
            builder.AppendLine($"Reference: {journal.Reference}");
            builder.AppendLine($"Transaction Date: {journal.CreatedDate:yyyy-MM-dd HH:mm:ss}");

            // Add environment details
            builder.AppendLine("\n===== Environment Details =====");
            builder.AppendLine($"User: {journal.ApplicationUserName}");
            builder.AppendLine($"Machine Name: {journal.EnvironmentMachineName}");
            builder.AppendLine($"IP Address: {journal.EnvironmentIPAddress}");

            // Add a footer
            builder.AppendLine("\n===============================");
            builder.AppendLine("Thank you for using our services!");

            return builder.ToString();
        }


        //[HttpPost]
        //public async Task<ActionResult> GenerateReport(string chartOfAccountId, string startDate, string endDate)
        //{
        //    try
        //    {
        //        var formattedStartDate = DateTime.Parse(startDate);
        //        var formattedEndDate = DateTime.Parse(endDate);

        //        var postingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());

        //        string reportUrl = "http://desktop-rstq8im/ReportServer?/Teller%20Transactions";

        //        string fullReportUrl = $"{reportUrl}&ChartOfAccountID={HttpUtility.UrlEncode(chartOfAccountId)}" +
        //                $"&StartDate={HttpUtility.UrlEncode(formattedStartDate.ToString("yyyy-MM-dd"))}" +
        //                $"&EndDate={HttpUtility.UrlEncode(formattedEndDate.ToString("yyyy-MM-dd"))}" +
        //                $"&PostingPeriod={HttpUtility.UrlEncode(postingPeriod.Id.ToString())}" +
        //                $"&rs:Command=Render&rs:Format=PDF";

        //        using (var client = new HttpClient())
        //        {
        //            var response = await client.GetAsync(fullReportUrl);

        //            if (response.IsSuccessStatusCode)
        //            {
        //                var reportBytes = await response.Content.ReadAsByteArrayAsync();

        //                // Set Content-Disposition header to "inline" directly in the response
        //                Response.Headers.Add("Content-Disposition", "inline; filename=TellerReport.pdf");

        //                return File(reportBytes, "application/pdf");
        //            }
        //            else
        //            {
        //                return Json(new { success = false, message = $"Failed to generate the report. Status Code: {response.StatusCode}" });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        //    }
        //}




        public static async Task SendTextNotificationAsync(string MessageTemplate, CustomerDTO Recipient, CustomerAccountDTO RecipientAccount, decimal Amount, string Reference, string PrimaryDescription)
        {

            if (!string.IsNullOrWhiteSpace(Recipient.AddressMobileLine) &&
                         Regex.IsMatch(Recipient.AddressMobileLine, @"^\+(?:[0-9]??){6,14}[0-9]$") &&
                         Recipient.AddressMobileLine.Length >= 13)
            {
                // Build the SMS body message
                var smsBody = new StringBuilder();
                smsBody.AppendFormat(
                    MessageTemplate,
                    Amount,
                    RecipientAccount.BranchDescription,
                    RecipientAccount.BranchCompanyDescription,
                    DateTime.Now.ToString("MMMM dd, yyyy"),
                    Reference, 
                    PrimaryDescription
                );


                var textAlertDTO = new TextAlertDTO
                {
                    BranchId = RecipientAccount.BranchId,
                    TextMessageOrigin = (int)MessageOrigin.Within,
                    TextMessageRecipient = Recipient.AddressMobileLine,
                    TextMessageBody = smsBody.ToString(),
                    MessageCategory = (int)MessageCategory.SMSAlert,
                    AppendSignature = false,
                    TextMessagePriority = (int)QueuePriority.Highest,
                };


                var textAlertDTOs = new ObservableCollection<TextAlertDTO> { textAlertDTO };

                var masterController = new MasterController();

                await masterController._channelService.AddTextAlertsAsync(textAlertDTOs, masterController.GetServiceHeader());
            }


        }



    }
}


