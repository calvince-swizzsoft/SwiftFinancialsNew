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

        public class OperationResult
        {
            public bool Success { get; set; }

            public bool Dialog { get; set; }
            public string Message { get; set; }

            public CustomerTransactionModel TransactionData { get; set; }

            public JournalDTO TransactionJournal { get; set; }
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

        private async Task<OperationResult> ProcessCustomerTransactionAsync(CustomerTransactionModel transactionModel)
        {

            bool includeBalances = true;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = true;
            bool considerMaturityPeriodForInvestmentAccounts = true;

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
                                var updateWithinLimitResult = await _channelService.UpdateCustomerAccountAsync(SelectedCustomerAccount, GetServiceHeader());


                                if (updateWithinLimitResult)
                                {
                                    string message = $"Operation success: Customer's new balance is {transactionModel.CustomerAccount.NewAvailableBalance}";

                                    string cashDepositTextTemplate = "Dear customer, your account has been credited with a cash deposit of KES {0} at {1} Branch {2}.";
                                    await SendTextNotificationAsync(cashDepositTextTemplate, SelectedCustomer, SelectedCustomerAccount, transactionModel.TotalValue, transactionModel.Reference, transactionModel.PrimaryDescription);

                                    return new OperationResult
                                    {
                                        Success = true,
                                        Dialog = false,
                                        Message = message,
                                        TransactionJournal = new JournalDTO
                                        {

                                            Id = withinLimitsCashDepositJournal.Id,
                                            SequentialId = withinLimitsCashDepositJournal.SequentialId,
                                            BranchDescription = withinLimitsCashDepositJournal.BranchDescription,
                                            PrimaryDescription = withinLimitsCashDepositJournal.PrimaryDescription,
                                            SecondaryDescription = withinLimitsCashDepositJournal.SecondaryDescription,
                                            PostingPeriodDescription = withinLimitsCashDepositJournal.PostingPeriodDescription,
                                            ApplicationUserName = withinLimitsCashDepositJournal.ApplicationUserName,
                                            CreatedDate = withinLimitsCashDepositJournal.CreatedDate,
                                            TotalValue = withinLimitsCashDepositJournal.TotalValue,
                                            Reference = withinLimitsCashDepositJournal.Reference
                                        }

                                    };
                                 
                                }

                                else
                                {
                                    return new OperationResult
                                    {

                                        Success = false,
                                        Dialog = false, 
                                        Message = "Sorry, but the authorized cash deposit request could not be marked as posted!",

                                    };
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
                                            return new OperationResult
                                            {

                                                Success = false,
                                                Dialog = false,
                                                Message = "Please wait until the current operation is complete.",

                                            };
                                        }

                                        // Set IsBusy to true to indicate an ongoing operation
                                        IsBusy = true;

                                        // Format the message
                                        string message = string.Format(
                                            "Txn Request of {1} is {2} for this customer account.\n\nDo you want to proceed?",
                                            EnumHelper.GetDescription(CashDepositCategory.AboveMaximumAllowed),
                                            string.Format(_nfi, "{0:C}", targetCashDepositRequest.Amount),
                                            targetCashDepositRequest.StatusDescription                             
                                        );


                                        return new OperationResult
                                        {
                                            Success = false,
                                            Dialog = true,
                                            Message = message,
                                            TransactionData = new CustomerTransactionModel {

                                                CreditCustomerAccountId = SelectedCustomerAccount.Id,
                                                CashDepositRequestId = targetCashDepositRequest.Id
                                            
                                            }
                                        };                               
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


                                    return new OperationResult
                                    {
                                        Success = false,
                                        Dialog = true,
                                        Message = message,
                                        TransactionData = new CustomerTransactionModel
                                        {
                                            CreditCustomerAccountId = SelectedCustomerAccount.Id,
                                            TotalValue = transactionModel.TotalValue,
                                            Reference = transactionModel.Reference
                                        } 
                                    };
                   
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
                  
                            return new OperationResult
                            {

                                Success = false,
                                Message = "Sorry, but your teller G/L account has insufficient cash!"
                            };

              
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
                                            string message = string.Format(
                                            "Txn Request of {1} is {2} for this customer account.\n\nDo you want to proceed?",
                                            EnumHelper.GetDescription(CashWithdrawalCategory.AboveMaximumAllowed),
                                            string.Format(_nfi, "{0:C}", targetCashWithdrawalRequest.Amount),
                                            targetCashWithdrawalRequest.StatusDescription
                                            );

                                                var result = new OperationResult
                                                {

                                                    Success = false,
                                                    Dialog = true,
                                                    Message = message,

                                                };

                                                if (cashWithdrawalCategory == CashWithdrawalCategory.PaymentVoucher) {

                                                    result.TransactionData = new CustomerTransactionModel
                                                    {

                                                        DebitCustomerAccountId = SelectedCustomerAccount.Id,
                                                        CashWithdrawalCategory = (int)cashWithdrawalCategory,
                                                        PaymentVoucherId = transactionModel.PaymentVoucher.Id,
                                                        ChequeBookId = transactionModel.PaymentVoucher.ChequeBookId,
                                                        TotalValue = transactionModel.TotalValue,
                                                        Reference = transactionModel.Reference,
                                                        PaymentVoucherWriteDate = transactionModel.PaymentVoucher.WriteDate,
                                                        PaymentVoucherPayee = transactionModel.PaymentVoucher.Payee,
                                                        CashWithdrawalRequestId = targetCashWithdrawalRequest.Id
                                                    };

                                                }

                                                else
                                                {

                                                    result.TransactionData = new CustomerTransactionModel
                                                    {


                                                        DebitCustomerAccountId = SelectedCustomerAccount.Id,
                                                        CashWithdrawalCategory = (int)cashWithdrawalCategory,                                                  
                                                        TotalValue = transactionModel.TotalValue,
                                                        Reference = transactionModel.Reference,
                                                        CashWithdrawalRequestId = targetCashWithdrawalRequest.Id

                                                    };


                                                 }


                                                return result;
                                                
                                                }
                                                                                     
                                            else createNewCashWithdrawalRequest = true;
                                        }
                                        else createNewCashWithdrawalRequest = true;
                                    }
                                    else createNewCashWithdrawalRequest = true;

                                    if (createNewCashWithdrawalRequest)
                                    {
                                       


                                        string message = string.Format(
                 "{0}.\nDo you want to proceed and place a cash withdrawal authorization request?",
                 EnumHelper.GetDescription(cashWithdrawalCategory)
             ) ;


                                        return new OperationResult
                                        {
                                            Success = false,
                                            Dialog = true,
                                            Message = message,
                                            TransactionData = new CustomerTransactionModel
                                            {
                                                DebitCustomerAccountId = SelectedCustomerAccount.Id,
                                                TotalValue = transactionModel.PaymentVoucher.Amount,
                                                Reference = transactionModel.PaymentVoucher.Reference,
                                                PaymentVoucherId = transactionModel.PaymentVoucher.Id, 
                                                PaymentVoucherPayee = transactionModel.PaymentVoucher.Payee,
                                                CashWithdrawalCategory = (int)cashWithdrawalCategory,
                                                PaymentVoucherWriteDate = transactionModel.PaymentVoucher.WriteDate
                                            }
                                        };
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


                                    transactionModel.CustomerAccount.NewAvailableBalance = transactionModel.CustomerAccount.AvailableBalance + transactionModel.TotalValue;
                                    var updateWithinLimitResult = await _channelService.UpdateCustomerAccountAsync(SelectedCustomerAccount, GetServiceHeader());

                                    
                                    if (updateWithinLimitResult)
                                    {
                                        string message = $"Operation success: Customer's new balance is {transactionModel.CustomerAccount.NewAvailableBalance}";

                                        string cashWithdrawalTextTemplate1 = "Dear customer, your account has been debited with KES {0} at {1} Branch {2}.";
                                        await SendTextNotificationAsync(cashWithdrawalTextTemplate1, SelectedCustomer, SelectedCustomerAccount, transactionModel.TotalValue, transactionModel.Reference, transactionModel.PrimaryDescription);

                                        return new OperationResult
                                        {
                                            Success = true,
                                            Dialog = false,
                                            Message = message,
                                            TransactionJournal = new JournalDTO
                                            {

                                                Id = withinLimitsJournal.Id,
                                                SequentialId = withinLimitsJournal.SequentialId,
                                                BranchDescription = withinLimitsJournal.BranchDescription,
                                                PrimaryDescription = withinLimitsJournal.PrimaryDescription,
                                                SecondaryDescription = withinLimitsJournal.SecondaryDescription,
                                                PostingPeriodDescription = withinLimitsJournal.PostingPeriodDescription,
                                                ApplicationUserName = withinLimitsJournal.ApplicationUserName,
                                                CreatedDate = withinLimitsJournal.CreatedDate,
                                                TotalValue = withinLimitsJournal.TotalValue,
                                                Reference = withinLimitsJournal.Reference
                                            }

                                        };

                                    }

                                    else
                                    {
                                        return new OperationResult
                                        {

                                            Success = false,
                                            Dialog = false,
                                            Message = "Sorry, but the authorized cash deposit request could not be marked as posted!",

                                        };
                                    }

                                case CashWithdrawalCategory.Overdraw:



                                    //ResetView();

                                    return new OperationResult
                                    {
                                        Success = false,
                                        Message = "Sorry, but the customer's account will be overdrawn!"
                                    };


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
                            //MessageBox.Show(message, "ChequeDeposit Request", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                            //_messageService.ShowExclamation(string.Join(Environment.NewLine, NewExternalCheque.ErrorMessages), this.DisplayName);

                            return new OperationResult
                            {
                                Success = false,
                                Dialog = false,
                                Message = message
                            };
                            //ResetView
                   
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
                         
                           
                                #region Send Text Notification

                                if (!string.IsNullOrWhiteSpace(SelectedCustomer.AddressMobileLine) &&
                           Regex.IsMatch(SelectedCustomer.AddressMobileLine, @"^\+(?:[0-9]??){6,14}[0-9]$") &&
                           SelectedCustomer.AddressMobileLine.Length >= 13)
                                {
                                  
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

                                SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(SelectedCustomerAccount.Id, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());


                                var updatedTeller = await GetCurrentTeller();
                                string successmessage = $"Customer new balance is {SelectedCustomerAccount.AvailableBalance} and Teller's new balance is {updatedTeller.BookBalance}";


                                return new OperationResult
                                {
                                    Success = true,
                                    Dialog = false,
                                    Message = successmessage,
                                    TransactionJournal = new JournalDTO
                                    {

                                        Id = chequeDepositJournal.Id,
                                        SequentialId = chequeDepositJournal.SequentialId,
                                        BranchDescription = chequeDepositJournal.BranchDescription,
                                        PrimaryDescription = chequeDepositJournal.PrimaryDescription,
                                        SecondaryDescription = chequeDepositJournal.SecondaryDescription,
                                        PostingPeriodDescription = chequeDepositJournal.PostingPeriodDescription,
                                        ApplicationUserName = chequeDepositJournal.ApplicationUserName,
                                        CreatedDate = chequeDepositJournal.CreatedDate,
                                        TotalValue = chequeDepositJournal.TotalValue,
                                        Reference = chequeDepositJournal.Reference
                                    }

                                };
                            }

                            else
                            {

                                return new OperationResult
                                {

                                    Success = false,
                                    Dialog = false,
                                    Message = "Operation failed"
                                };
            
                            }
                        }
                    default:
                      
                       

                        return new OperationResult
                        {

                            Success = false,
                            Dialog = false,
                            Message = "You may have entered the wrong transaction typ"
                        };
                     
                }
            }
            catch (Exception ex)
            {
                return new OperationResult
                {

                    Success = false,
                    Dialog = false,
                    Message = $"An error occurred: {ex.Message}"
                };
              
            }

            return new OperationResult
            {

                Success = false,
                Dialog = false,
                Message = "Operation failed. Please try again"
            };

         
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

                var response = new
                {

                    success = false,
                    message = "Please select a customer account",

                };

                return Json(response);

            }

            if ((RecordStatus)SelectedCustomerAccount.RecordStatus != RecordStatus.Approved)
            {

                var response = new
                {

                    success = false,
                    message = "Sorry, account is not approved yet",

                };

                return Json(response);
            }

            SelectedBranch = await _channelService.FindBranchAsync(transactionModel.BranchId, GetServiceHeader());
            _selectedTeller = await GetCurrentTeller();

            if (_selectedTeller == null)
            {     
                var response = new
                {

                    success = false,
                    message = "Teller is missing",

                };

                return Json(response);

            }

            var postingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
            transactionModel.PostingPeriodId = postingPeriod.Id;
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

                    transactionModel.TransactionCode = (int)SystemTransactionCode.CashDeposit;

                    if (SelectedTeller.BookBalance - transactionModel.TotalValue < SelectedTeller.RangeLowerLimit)
                    {
                        var response = new
                        {
                            success = false,
                            message = "Sorry, the transaction will reduce teller's balance below limit",
                        };

                        return Json(response);
                    }

               
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

                case FrontOfficeTransactionType.ChequeDeposit:

                    transactionModel.TransactionCode = (int)SystemTransactionCode.ChequeDeposit;

                    if (SelectedTeller.BookBalance - transactionModel.TotalValue < SelectedTeller.RangeLowerLimit)
                    {
                        var response = new
                        {
                            success = false,
                            message = "Sorry, the transaction will reduce teller's balance below limit",
                        };

                        return Json(response);
                    }

                    transactionModel.TransactionCode = (int)SystemTransactionCode.ChequeDeposit;

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

                    transactionModel.TransactionCode = (int)SystemTransactionCode.CashWithdrawal;

                    if (SelectedCustomerAccount.BookBalance - transactionModel.TotalValue < SelectedCustomerAccount.CustomerAccountTypeTargetProductMinimumBalance)
                    {

                        var response = new
                        {

                            success = false,
                            message = "Sorry, this transaction will reduce the customer's balance below the minimum balance for product",

                        };

                        return Json(response);

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

                case FrontOfficeTransactionType.CashWithdrawalPaymentVoucher:

                    transactionModel.TransactionCode = (int)SystemTransactionCode.CashWithdrawalPaymentVoucher;

                    if (SelectedCustomerAccount.BookBalance - transactionModel.TotalValue < SelectedCustomerAccount.CustomerAccountTypeTargetProductMinimumBalance)
                    {

                        var response = new
                        {

                            success = false,
                            message = "Sorry, this transaction will reduce the customer balance below the allowed minimum balance for product",

                        };

                        return Json(response);

                    }

                    if (SelectedTeller.BookBalance - transactionModel.TotalValue < SelectedTeller.RangeLowerLimit)
                    {
                        var response = new
                        {
                            success = false,
                            message = "Sorry, the transaction will reduce teller's balance below limit",
                        };

                        return Json(response);
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

                string combinedErrorMessage = string.Join("; ", errorMessages);
                ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(SelectedCustomerAccount.Type.ToString());


                var responseLast = new
                {
                    success = false,
                    message = $"Transaction Error: {combinedErrorMessage}"             
                };

                return Json(responseLast);
            }


            try
            {
                // Call the asynchronous method and check its result
                var result = await ProcessCustomerTransactionAsync(transactionModel);

                SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(transactionModel.CustomerAccount.Id, true, true, false, false, GetServiceHeader());
                SelectedCustomer = await _channelService.FindCustomerAsync(SelectedCustomerAccount.CustomerId);


                if (result.Success)
                {
                    
                    ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(SelectedCustomerAccount.Type.ToString());

                    var Teller = await GetCurrentTeller();

                    var response = new
                    {
                        success = true,
                        message = "Operation Success",
                        TellerBookBalance = Teller.BookBalance,
                        CustomerAvailableBalance = SelectedCustomerAccount.AvailableBalance,
                        journalId = result.TransactionJournal.Id,
                        journalSequentialId = result.TransactionJournal.SequentialId,
                        journalBranchDescription = result.TransactionJournal.BranchDescription,
                        journalPrimaryDescription = result.TransactionJournal.PrimaryDescription,
                        journalSecondaryDescription = result.TransactionJournal.SecondaryDescription,
                        journalPostingPeriodDescription = result.TransactionJournal.PostingPeriodDescription,
                        journalApplicationUserName = result.TransactionJournal.ApplicationUserName,
                        journalCreatedDate = result.TransactionJournal.CreatedDate,
                        journalTotalValue = result.TransactionJournal.TotalValue,
                        journalReference = result.TransactionJournal.Reference

                    };

                    return Json(response);
                }
                else if (!result.Success && result.Dialog)
                {
                    ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(SelectedCustomerAccount.Type.ToString());

                    if (result.TransactionData.CreditCustomerAccountId != null && result.TransactionData.CreditCustomerAccountId != Guid.Empty)
                    {

                        var response = new
                        {
                            isCashDepositRequest = true,
                            success = false,
                            dialog = true,
                            message = result.Message,
                            selectedCustomerAccountId = result.TransactionData.CreditCustomerAccountId,
                            transactionTotalValue = result.TransactionData.TotalValue,
                            transactionReference = result.TransactionData.Reference,
                            cashTransactionRequestId = result.TransactionData.CashDepositRequestId,
                            transactionCategory = result.TransactionData.CashDepositCategory
                        };

                        return Json(response);

                    }

                    else if (result.TransactionData.DebitCustomerAccountId != null && result.TransactionData.DebitCustomerAccountId != Guid.Empty)
                    {
                        var response = new
                        {

                            isCashWithdrawalRequest = true,
                            success = false,
                            dialog = true,
                            message = result.Message,
                            selectedCustomerAccountId = result.TransactionData.DebitCustomerAccountId,
                            transactionTotalValue = result.TransactionData.TotalValue,
                            transactionReference = result.TransactionData.Reference,
                            cashTransactionRequestId = result.TransactionData.CashWithdrawalRequestId,
                            transactionCategory = result.TransactionData.CashWithdrawalCategory,
                            paymentVoucherId = result.TransactionData.PaymentVoucherId,
                            paymentVoucherPayee = result.TransactionData.PaymentVoucherPayee,
                            paymentVoucherChequeBookId = result.TransactionData.ChequeBookId,
                            paymentVoucherWriteDate = result.TransactionData.PaymentVoucherWriteDate
                        };

                        return Json(response);


                    }

                    // Default return for any path not covered by conditions
                    return Json(new
                    {
                        success = false,
                        dialog = false,
                        message = "No valid transaction data found."
                    });
                }

                else
                {

                    ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(SelectedCustomerAccount.Type.ToString());

                    var response = new
                    {

                        success = false,
                        message = result.Message,
                        //selectedCustomerAccountId = result.TransactionData.CreditCustomerAccountId,
                        //transactionTotalValue = result.TransactionData.TotalValue,
                        //transactionReference = result.TransactionData.Reference

                    };

                    return Json(response);


                }
            }
            catch (Exception ex)
            {
                // Handle unexpected exceptions

                ViewBag.TransactionTypeSelectList = SelectedCustomerAccount != null
                    ? GetFrontOfficeTransactionTypeSelectList(SelectedCustomerAccount.Type.ToString())
                    : Enumerable.Empty<SelectListItem>();

                MessageBox.Show($"An unexpected error occurred: {ex.Message}",
                    "Cash Transaction",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification);


                var response = new
                {

                    success = false,
                    message = ex.Message

                };

                return Json(response);
            }

        }


        public async Task<ActionResult> PlaceCashWithdrawalAuthorizationRequestAsync(CustomerTransactionModel customerTransactionModel)
        {

            bool includeBalances = true;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(customerTransactionModel.DebitCustomerAccountId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            SelectedTeller = await GetCurrentTeller();

            if (customerTransactionModel.DialogResult)
            {
                //proceedCashDepositAuthorizationRequest = true;

                #region Proceed to Place Cash Withdrawal Authorization Request?

                var totalValueOfUnclearedCheques = 0m;

                var unclearedExternalCheques = await _channelService.FindUnClearedExternalChequesByCustomerAccountIdAsync(SelectedCustomerAccount.Id, GetServiceHeader());


                if (unclearedExternalCheques != null && unclearedExternalCheques.Any())
                {
                    totalValueOfUnclearedCheques = unclearedExternalCheques.Sum(x => x.Amount);
                }

                if ((SelectedCustomerAccount.BookBalance - totalValueOfUnclearedCheques) < 0m)
                {

                    var response = new
                    {

                        success = false,
                        message = "Sorry, but the customer's total value unucleared cheques exceed book balance!"

                    };

                    return Json(response);

                 }
                else
                {
                    var customerTransactionAuthRequest = new CashWithdrawalRequestDTO
                    {
                        BranchId = SelectedTeller.EmployeeBranchId,
                        CustomerAccountId = SelectedCustomerAccount.Id,
                        CustomerAccountCustomerAccountTypeTargetProductId = SelectedCustomerAccount.CustomerAccountTypeTargetProductId,
                        CustomerAccountCustomerAccountTypeProductCode = SelectedCustomerAccount.CustomerAccountTypeProductCode,
                        CustomerAccountCustomerAccountTypeTargetProductCode = SelectedCustomerAccount.CustomerAccountTypeTargetProductCode,
                        Type = (int)CashWithdrawalRequestType.ImmediateNotice,
                        Category = customerTransactionModel.CashWithdrawalCategory,
                        Amount = customerTransactionModel.TotalValue,
                        Remarks = customerTransactionModel.Reference,
                        PaymentVoucherId = customerTransactionModel.PaymentVoucherId,
                        PaymentVoucherPayee = customerTransactionModel.PaymentVoucherPayee
                    };

                    var addRequestResult = await _channelService.AddCashWithdrawalRequestAsync(customerTransactionAuthRequest, GetServiceHeader());


                    if (addRequestResult != null)
                    {

                        var response = new
                        {

                            success = true,
                            message = "Cash Withdrawal request sent successfully."

                        };


                        return Json(response);
                        //var updateTargetPaymentVoucherDetails?

                    }
                    else
                    {

                        var response = new
                        {

                            success = false,
                            message = "Operation failed!"

                        };

                        return Json(response);

                     
                    }
                }

                #endregion

            }
            else
            {
                IsBusy = false;

                var response = new
                {
                    success = true,
                    message = "Operation Cancelled."

                };

                return Json(response);
            }

        }

        [HttpPost]
        public async Task<ActionResult> PlaceCashDepositAuthorizationRequestAsync(CustomerTransactionModel customerTransactionModel)
        {

            bool includeBalances = true;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(customerTransactionModel.CreditCustomerAccountId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            SelectedTeller = await GetCurrentTeller();

            if (customerTransactionModel.DialogResult)
            {
                //proceedCashDepositAuthorizationRequest = true;

                #region Proceed to Place Cash Deposit Authorization Request

                var customerTransactionAuthRequest = new CashDepositRequestDTO
                {
                    BranchId = SelectedTeller.EmployeeBranchId,
                    CustomerAccountId = SelectedCustomerAccount.Id,
                    CustomerAccountCustomerAccountTypeTargetProductId = SelectedCustomerAccount.CustomerAccountTypeTargetProductId,
                    CustomerAccountCustomerAccountTypeProductCode = SelectedCustomerAccount.CustomerAccountTypeProductCode,
                    CustomerAccountCustomerAccountTypeTargetProductCode = SelectedCustomerAccount.CustomerAccountTypeTargetProductCode,
                    Amount = customerTransactionModel.TotalValue,
                    Remarks = customerTransactionModel.Reference,
                };

                try
                {
                    var opResult = await _channelService.AddCashDepositRequestAsync(customerTransactionAuthRequest);

                    if (opResult != null)
                    {
            
                        var response = new
                        {
                            success = true,
                            message = "Cash Deposit request sent successfully."
                        };

                        return Json(response);


                    }
                    else
                    {

                        var response = new
                        {
                            success = false,
                            message = "Operation failed!"
                        };

                        return Json(response);
        


                    }
                }
                catch (Exception ex)
                {

                    var response = new
                    {
                        success = false,
                        message = $"An error occurred: {ex.Message}"
                    };

                    return Json(response);

                }

                #endregion
            }
            else
            {
                IsBusy = false;

                var response = new
                {
                    success = true,
                    message = "Operation Cancelled."
                };

                return Json(response);
            }

        }

        public async Task<ActionResult> ProcessAuthorizedCashWithdrawalRequestAsync(CustomerTransactionModel customerTransactionModelDTO)
        {
  
            bool includeBalances = true;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            var targetCashWithdrawalRequest = await _channelService.FindCashWithdrawalRequestAsync(customerTransactionModelDTO.CashWithdrawalRequestId, GetServiceHeader());
            SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(
                (Guid)targetCashWithdrawalRequest.CustomerAccountId,
                includeBalances,
                includeProductDescription,
                includeInterestBalanceForLoanAccounts,
                considerMaturityPeriodForInvestmentAccounts,
                GetServiceHeader()
            );

            SelectedTeller = await GetCurrentTeller();
            var postingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
            SelectedBranch = await _channelService.FindBranchAsync(SelectedTeller.EmployeeBranchId, GetServiceHeader());
            SelectedCustomer = await _channelService.FindCustomerAsync(SelectedCustomerAccount.CustomerId, GetServiceHeader());

            var customerTransactionModel = new CustomerTransactionModel
            {
                PostingPeriodId = postingPeriod.Id,
                PrimaryDescription = "ok",
                SecondaryDescription = string.Format("B{0}/T{1}/#{2}", SelectedBranch.Code, _selectedTeller.Code, _selectedTeller.ItemsCount),
                Reference = SelectedCustomerAccount.CustomerReference1,
                CreditChartOfAccountId = SelectedTeller.ChartOfAccountId ?? Guid.Empty,
                TotalValue = targetCashWithdrawalRequest.Amount,
                DebitCustomerAccount = SelectedCustomerAccount,
                CreditCustomerAccount = SelectedCustomerAccount,
                DebitCustomerAccountId = SelectedCustomerAccount.Id,
                CreditCustomerAccountId = SelectedCustomerAccount.Id,
                DebitChartOfAccountId = SelectedCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId,
                BranchId = SelectedBranch.Id,
                TransactionCode = (int)SystemTransactionCode.CashWithdrawal
            };

            // Compute tariffs
            var tariffs = await _channelService.ComputeTellerCashTariffsAsync(
                SelectedCustomerAccount,
                customerTransactionModel.TotalValue,
                (int)FrontOfficeTransactionType.CashDeposit,
                GetServiceHeader()
            );

            if (!customerTransactionModelDTO.DialogResult)
            {

                var response = new
                {

                    success = true,
                    message = "Operation Cancelled!"
                };

                return Json(response);
                       
            }

            if (targetCashWithdrawalRequest.Status != (int)CashWithdrawalRequestAuthStatus.Authorized)
            {

                var response1 = new
                {

                    success = false,
                    message = "Sorry, but the authorized cash withdrawal request could not be marked as posted!"

                };


                return Json(response1);
                
         
            }

            #region Payment Voucher Handling
            if (customerTransactionModelDTO.CashWithdrawalCategory == (int)CashWithdrawalCategory.PaymentVoucher)
            {
                var paymentVoucherDTO = new PaymentVoucherDTO
                {
                    Amount = customerTransactionModelDTO.TotalValue,
                    Reference = customerTransactionModelDTO.Reference,
                    Id = customerTransactionModelDTO.PaymentVoucherId,
                    //WriteDate = customerTransactionModelDTO.wR
                    Payee = customerTransactionModelDTO.PaymentVoucherPayee,
                    ChequeBookId = customerTransactionModelDTO.ChequeBookId                    
                };

                paymentVoucherDTO.ValidateAll();

                if (paymentVoucherDTO.HasErrors)
                {

                    var response2 = new
                    {

                        success = false,
                        message = string.Join(Environment.NewLine, paymentVoucherDTO.ErrorMessages)

                    };



                    return Json(response2);

              
                }

                bool paymentSuccess = await _channelService.PayCashWithdrawalRequestAsync(targetCashWithdrawalRequest, paymentVoucherDTO, GetServiceHeader());
                if (!paymentSuccess)
                {

                    var response3 = new
                    {

                        success = false,
                        message = "Sorry, but the authorized cash withdrawal request could not be marked as paid!"
                    };

                    return Json(response3);
                }

                var authorizedJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(customerTransactionModel, tariffs, GetServiceHeader());


                 var updatedTeller = await GetCurrentTeller();

                string cashWithdrawalTextTemplate = "Payment voucher of KES {0} {4}, {5} {2}.";
                await SendTextNotificationAsync(
                    cashWithdrawalTextTemplate,
                    SelectedCustomer,
                    SelectedCustomerAccount,
                    customerTransactionModel.TotalValue,
                    customerTransactionModel.Reference,
                    customerTransactionModel.PrimaryDescription
                );

                //PrintReceipt(authorizedJournal);

                SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(
               (Guid)targetCashWithdrawalRequest.CustomerAccountId,
               includeBalances,
               includeProductDescription,
               includeInterestBalanceForLoanAccounts,
               considerMaturityPeriodForInvestmentAccounts,
               GetServiceHeader()
           );

                string successmessage1 = $"Customer new balance is {SelectedCustomerAccount.AvailableBalance} and Teller's new balance is {updatedTeller.BookBalance}";



                var response4 = new
                {
                    success = true,
                    message = successmessage1,
                    tellerBookBalance = updatedTeller.BookBalance,
                    journalId = authorizedJournal.Id,
                    journalSequentialId = authorizedJournal.SequentialId,
                    journalBranchDescription = authorizedJournal.BranchDescription,
                    journalPrimaryDescription = authorizedJournal.PrimaryDescription,
                    journalSecondaryDescription = authorizedJournal.SecondaryDescription,
                    journalPostingPeriodDescription = authorizedJournal.PostingPeriodDescription,
                    journalApplicationUserName = authorizedJournal.ApplicationUserName,
                    journalCreatedDate = authorizedJournal.CreatedDate,
                    journalTotalValue = authorizedJournal.TotalValue,
                    journalReference = authorizedJournal.Reference
                };

                return Json(response4);
              
            }
            #endregion

            #region Direct Payment Without Voucher
            //if (SelectedCustomer.BiometricFingerprintTemplateBuffer != null && SelectedBranch.CompanyEnforceBiometricsForCashWithdrawal)
            //{
                // Add biometric verification logic here if needed
            //}

            bool paymentWithoutVoucherSuccess = await _channelService.PayCashWithdrawalRequestAsync(targetCashWithdrawalRequest, null);
            if (!paymentWithoutVoucherSuccess)
            {

                var response5 = new
                {

                    success = false,
                    message = "Sorry, but the authorized cash withdrawal request could not be marked as paid!"

                };

                return Json(response5);
            }

            var directAuthorizedJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(customerTransactionModel, tariffs, GetServiceHeader());


            



            string directCashWithdrawalTextTemplate = "Dear customer, your account has been debited with KES {0} at {1} Branch {2}.";
            await SendTextNotificationAsync(
                directCashWithdrawalTextTemplate,
                SelectedCustomer,
                SelectedCustomerAccount,
                customerTransactionModel.TotalValue,
                customerTransactionModel.Reference,
                customerTransactionModel.PrimaryDescription
            );

            //PrintReceipt(directAuthorizedJournal);

            var updatedTeller1 = await GetCurrentTeller();

            SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(
               (Guid)targetCashWithdrawalRequest.CustomerAccountId,
               includeBalances,
               includeProductDescription,
               includeInterestBalanceForLoanAccounts,
               considerMaturityPeriodForInvestmentAccounts,
               GetServiceHeader()
           );

            string successmessage = $"Customer new balance is {SelectedCustomerAccount.AvailableBalance} and Teller's new balance is {updatedTeller1.BookBalance}";


            var response6 = new {

                success = true,
                message = successmessage,
                tellerBookBalance = updatedTeller1.BookBalance,
                journalId = directAuthorizedJournal.Id,
                journalSequentialId = directAuthorizedJournal.SequentialId,
                journalBranchDescription = directAuthorizedJournal.BranchDescription,
                journalPrimaryDescription = directAuthorizedJournal.PrimaryDescription,
                journalSecondaryDescription = directAuthorizedJournal.SecondaryDescription,
                journalPostingPeriodDescription = directAuthorizedJournal.PostingPeriodDescription,
                journalApplicationUserName = directAuthorizedJournal.ApplicationUserName,
                journalCreatedDate = directAuthorizedJournal.CreatedDate,
                journalTotalValue = directAuthorizedJournal.TotalValue,
                journalReference = directAuthorizedJournal.Reference
            };

            
            return Json(response6);
            #endregion
        }


        [HttpPost]
        public async Task<ActionResult> ProcessAuthorizedCashDepositRequestAsync(CustomerTransactionModel customerTransactionModel)
        {

            bool includeBalances = true;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;


            SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(customerTransactionModel.CreditCustomerAccountId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            SelectedCustomer = await _channelService.FindCustomerAsync(SelectedCustomerAccount.CustomerId, GetServiceHeader());
            SelectedTeller = await GetCurrentTeller();
            var postingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
            SelectedBranch = await _channelService.FindBranchAsync(SelectedTeller.EmployeeBranchId, GetServiceHeader());

            if (customerTransactionModel.DialogResult)
            {
                //proceedAuthorizedCashDepositRequest = true;
                #region Proceed with Authorized Transaction Request

                var targetCashDepositRequest = await _channelService.FindCashDepositRequestAsync(customerTransactionModel.CashDepositRequestId, GetServiceHeader());

                if (targetCashDepositRequest.Status == (int)CashDepositRequestAuthStatus.Authorized)
                {
                    if (await _channelService.PostCashDepositRequestAsync(targetCashDepositRequest, GetServiceHeader()))
                    {
                        CustomerTransactionModel model = new CustomerTransactionModel
                        {

                            PostingPeriodId = postingPeriod.Id,
                            PrimaryDescription = "ok",
                            SecondaryDescription = string.Format("B{0}/T{1}/#{2}", SelectedBranch.Code, _selectedTeller.Code, _selectedTeller.ItemsCount),
                            Reference = string.Format("{0}", SelectedCustomerAccount.CustomerReference1),
                            CreditChartOfAccountId = SelectedCustomerAccount.CustomerAccountTypeTargetProductChartOfAccountId,
                            CreditCustomerAccount = SelectedCustomerAccount,
                            CreditCustomerAccountId = SelectedCustomerAccount.Id,
                            DebitCustomerAccountId = SelectedCustomerAccount.Id,
                            DebitCustomerAccount = SelectedCustomerAccount,
                            DebitChartOfAccountId = SelectedTeller.ChartOfAccountId ?? Guid.Empty,
                            TotalValue = targetCashDepositRequest.Amount,
                            BranchId = SelectedBranch.Id,
                            TransactionCode = (int)SystemTransactionCode.CashDeposit
                        };

                        var tariffs = await _channelService.ComputeTellerCashTariffsAsync(SelectedCustomerAccount, model.TotalValue, (int)FrontOfficeTransactionType.CashDeposit, GetServiceHeader());

                        var authorizedJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(model, tariffs, GetServiceHeader());
                        //basic
                        //transactionModel.CustomerAccount.NewAvailableBalance = transactionModel.CustomerAccount.AvailableBalance + transactionModel.TotalValue;
                        var updateAboveLimitResult = await _channelService.UpdateCustomerAccountAsync(model.CreditCustomerAccount, GetServiceHeader());

                        if (updateAboveLimitResult)
                        {

                            SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(customerTransactionModel.CreditCustomerAccountId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());


                            var updatedTeller = await GetCurrentTeller();
                            string successmessage = $"Customer new balance is {SelectedCustomerAccount.AvailableBalance} and Teller's new balance is {updatedTeller.BookBalance}";

                            string cashDepositAboveLimitTextTemplate = "Dear customer, your account has been credited with a cash deposit of KES {0} at {1} Branch {2}.";
                            await SendTextNotificationAsync(cashDepositAboveLimitTextTemplate, SelectedCustomer, SelectedCustomerAccount, model.TotalValue, customerTransactionModel.Reference, model.PrimaryDescription);
                            //PrintReceipt(authorizedJournal);
                            var response = new
                            {
                                success = true,
                                message = successmessage,
                                tellerBookBalance = updatedTeller.BookBalance,
                                journalId = authorizedJournal.Id,
                                journalSequentialId = authorizedJournal.SequentialId,
                                journalBranchDescription = authorizedJournal.BranchDescription,
                                journalPrimaryDescription = authorizedJournal.PrimaryDescription,
                                journalSecondaryDescription = authorizedJournal.SecondaryDescription,
                                journalPostingPeriodDescription = authorizedJournal.PostingPeriodDescription,
                                journalApplicationUserName = authorizedJournal.ApplicationUserName,
                                journalCreatedDate = authorizedJournal.CreatedDate,
                                journalTotalValue = authorizedJournal.TotalValue,
                                journalReference = authorizedJournal.Reference

                            };


                            return Json(response);
                        }

                        else
                        {

                            var response = new
                            {
                                success = false,
                                message = "Sorry,but the authorized cash deposit request could not be marked as posted!"
                            };

                            return Json(response);
                        }
                    }
                    else
                    {

                        var response = new
                        {

                            success = false,
                            message = "Sorry, but the authorized cash deposit request could not be marked as posted!"
                        };

                        return Json(response);

                    }
                }
                else
                {
                    IsBusy = false;
                    var response = new
                    {

                        success = false,
                        message = "Sorry, the selected cash deposit request is not marked as authorized"
                    };

                    return Json(response);
                }

                #endregion
            }
            else
            {
                IsBusy = false;

                
                 var response = new
                {
                    Success = true,
                    Message = "Operation Cancelled!"
                };


                return Json(response);

            }

        }

        //ProcessAuthorizedCashDepositRequestAsync

        [HttpPost]
        public async Task<JsonResult> PayAuthorizedCashDepositRequestAsync(Guid cashDepositRequestId)
        {

            var cashDepositRequest = await _channelService.FindCashDepositRequestAsync(cashDepositRequestId, GetServiceHeader());

            SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(cashDepositRequest.CustomerAccountId, true, true, true, true, GetServiceHeader());

            _selectedTeller = await GetCurrentTeller();

            SelectedBranch = await _channelService.FindBranchAsync(SelectedTeller.EmployeeBranchId, GetServiceHeader());
            

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
                var result = await ProcessCustomerTransactionAsync(transactionModel);
                
                //this if block is no longer necesary 18.12.2024
                if (result.Success)
                {
                    var Teller = await GetCurrentTeller();

                    var response = new
                    {
                        sucess = true,
                        message = "Cash deposit request authorized successfully.",
                        amount = transactionModel.TotalValue,
                        accountNumber = transactionModel.CashDepositRequest.CustomerAccountFullAccountNumber,
                        tellerBookBalance = Teller.BookBalance, 
                        timestamp = DateTime.Now,

                    };

                    return Json(response);
                }

                else if (!result.Success && result.Dialog)
                {

                    var response = new
                    {

                        success = false,
                        dialog = true,
                        message = result.Message,
                        cashDepositRequestId = result.TransactionData.CashDepositRequestId,
                        selectedCustomerAccountId = result.TransactionData.CreditCustomerAccountId
                        //transactionCategory = result.TransactionData.CashDepositCategory
                    };

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

                    // Show the error message
                  

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
                  
                    return Json(new { success = false, message = errorMessage });
                }

                // Fetch Payment Vouchers
                var paymentVouchers = await _channelService.FindPaymentVouchersByChequeBookIdAsync(targetChequeBook.Id, GetServiceHeader());
                if (paymentVouchers == null || !paymentVouchers.Any())
                {
                    var errorMessage = "Operation failed: No Payment Vouchers found for the associated Cheque Book.";
                   
                    return Json(new { success = false, message = errorMessage });
                }

                // Fetch Cash Withdrawal Request
                var currentCashWithdrawalRequest = await _channelService.FindCashWithdrawalRequestAsync(cashWithdrawalRequestId, GetServiceHeader());
                if (currentCashWithdrawalRequest == null)
                {
                    var errorMessage = "Operation failed: Missing Cash Withdrawal Request for the given ID.";
                
                    return Json(new { success = false, message = errorMessage });
                }

                // Validate Target Payment Voucher
                var targetPaymentVoucher = paymentVouchers.FirstOrDefault(pv => pv.Id == currentCashWithdrawalRequest.PaymentVoucherId);
                if (targetPaymentVoucher == null)
                {
                    var errorMessage = "Operation failed: No Payment Voucher matches the Cash Withdrawal Request.";
            
                    return Json(new { success = false, message = errorMessage });
                }


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
                var result = await ProcessCustomerTransactionAsync(transactionModel);

                if (result.Success)
                {
                    var Teller = await GetCurrentTeller();

                    var response = new
                    {
                        success = true,
                        Status = "Success",
                        message = "Cash withdrawal request authorized successfully.",
                        Amount = transactionModel.TotalValue,
                        AccountNumber = transactionModel.CashWithdrawal.CustomerAccountFullAccountNumber,
                        TellerBookBalance = Teller.BookBalance,
                        Timestamp = DateTime.Now
                    };
                

  
                    return Json(response);
                }

                else if (!result.Success && result.Dialog)
                {

                    var response = new
                    {

                        success = false,
                        dialog = true,
                        message = result.Message,
                        cashTransactionRequestId = result.TransactionData.CashWithdrawalRequestId,
                        selectedCustomerAccountId = result.TransactionData.DebitCustomerAccountId,
                        paymentVoucherId = result.TransactionData.PaymentVoucherId,
                        paymentVoucherPayee = result.TransactionData.PaymentVoucherPayee,
                        transactionTotalValue = result.TransactionData.TotalValue,
                        transactionReference = result.TransactionData.Reference,
                        transactionCategory = result.TransactionData.CashWithdrawalCategory,
                        paymentVoucherChequeBookId = result.TransactionData.ChequeBookId,
                        paymentVoucherWriteDate = result.TransactionData.PaymentVoucherWriteDate
                        //transactionCategory = result.TransactionData.CashDepositCategory
                    };

                    return Json(response);
                }

                else
                {
                    var failureResponse = new
                    {
                        Status = "Error",
                        Message = "Cash withdrawal request failed. Please try again.",
                        Timestamp = DateTime.Now
                    };

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


        [HttpPost]
        public JsonResult PrintReceipt(JournalDTO journal)
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
                  
                    e.Graphics.DrawString(receiptContent, new Font("Courier New", 10), Brushes.Black, new RectangleF(0, 0, e.PageBounds.Width, e.PageBounds.Height));
                };

                printDocument.Print();

                // Return success response
                var response = new
                {

                    success = true,
                    message = "Receipt printed successfully."


                };

                return Json(response);
            }
            catch (Exception ex)
            {

                var response = new
                {

                    success = false,
                    message = $"Error printing receipt: {ex.Message}"

                };
                return Json(response);
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

            //this format cld have issue
            builder.AppendLine($"Transaction Date: {journal.CreatedDate:yyyy-MM-dd HH:mm:ss}");

            // Add environment details
            builder.AppendLine("\n===== Environment Details =====");
            builder.AppendLine($"User: {journal.ApplicationUserName}");
            //builder.AppendLine($"Machine Name: {journal.EnvironmentMachineName}");
            //builder.AppendLine($"IP Address: {journal.EnvironmentIPAddress}");

            // Add a footer
            builder.AppendLine("\n===============================");
            builder.AppendLine("Thank you for using our services!");

            return builder.ToString();
        }


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


