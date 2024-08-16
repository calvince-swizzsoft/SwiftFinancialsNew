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


        decimal PreviousTellerBalance;
        decimal NewTellerBalance;

        private bool IsBusy { get; set; } // Property to indicate if an operation is in progress


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
            // Serve navigation menus and initialize select lists
            await ServeNavigationMenus();
            ViewBag.TransactionTypeSelectList = GetFrontOfficeTransactionTypeSelectList(string.Empty);

            // Validate the GUID
            if (id == null || id == Guid.Empty || !Guid.TryParse(id.ToString(), out Guid parseId))
            {
              
                return View();
            }

            // Initialize flags
            bool includeBalances = false;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;

            // Fetch customer details
            var customer = await _channelService.FindCustomerAccountAsync(
                parseId,
                includeBalances,
                includeProductDescription,
                includeInterestBalanceForLoanAccounts,
                considerMaturityPeriodForInvestmentAccounts,
                GetServiceHeader()
            );

            // Create and populate transaction model
            CustomerTransactionModel transactionModel = new CustomerTransactionModel();

            if (customer != null)
            {
                transactionModel.CustomerAccount = new CustomerAccountDTO
                {
                    Id = customer.Id,
                    CustomerId = customer.CustomerId,
                    CustomerIndividualFirstName = customer.CustomerFullName,
                    CustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers,
                    CustomerSerialNumber = customer.CustomerSerialNumber,
                    CustomerReference1 = customer.CustomerReference1,
                    CustomerReference2 = customer.CustomerReference2,
                    CustomerReference3 = customer.CustomerReference3,
                    CustomerIndividualIdentityCardNumber = customer.CustomerIndividualIdentityCardNumber,
                    Remarks = customer.Remarks,
                    CustomerAccountTypeTargetProductDescription = customer.CustomerAccountTypeTargetProductDescription,
                    BranchId = customer.BranchId,
                    BranchDescription = customer.BranchDescription,
                    CustomerAccountTypeTargetProductId = customer.CustomerAccountTypeTargetProductId,
                    CustomerAccountTypeTargetProductCode = customer.CustomerAccountTypeTargetProductCode,
                    CustomerAccountTypeTargetProductParentId = customer.CustomerAccountTypeTargetProductParentId,
                    CustomerAccountTypeProductCode = customer.CustomerAccountTypeProductCode,
                    AvailableBalance = customer.AvailableBalance,
                    NewAvailableBalance = customer.NewAvailableBalance,
                    BookBalance = customer.BookBalance,
                    CustomerAccountTypeTargetProductMaximumAllowedDeposit = customer.CustomerAccountTypeTargetProductMaximumAllowedDeposit
                    
                };



                transactionModel.BranchId = customer.BranchId;

                 _selectedTeller = await GetCurrentTeller();

                _ = _selectedTeller != null ? transactionModel.Teller = _selectedTeller : TempData["Missing Teller"] = "You are working without a Recognized Teller";


                ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
                return View(transactionModel);
            }

            // If no customer is found, return the view with no model
            return View();
        }


        private async Task ProcessCustomerTransactionAsync(CustomerTransactionModel transactionModel)
        {
            
            
            // Assuming these are not needed
            // var proceedAuthorizedCashWithdrawalRequest = default(bool);
            // var proceedCashWithdrawalAuthorizationRequest = default(bool);
               var proceedAuthorizedCashDepositRequest = default(bool);
               var proceedCashDepositAuthorizationRequest = default(bool);



            System.Globalization.NumberFormatInfo _nfi = new CultureInfo("en-US", false).NumberFormat;
     

            try
            {


                int frontOfficeTransactionType = transactionModel.CustomerAccount.Type;

                var tarrifs = await _channelService.ComputeTellerCashTariffsAsync(transactionModel.CustomerAccount, transactionModel.TotalValue, frontOfficeTransactionType, GetServiceHeader());

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
                                var withinLimitsCashDepositJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(transactionModel, tarrifs);

                                transactionModel.CustomerAccount.NewAvailableBalance = transactionModel.CustomerAccount.AvailableBalance + transactionModel.TotalValue;
                                var updateWithinLimitResult = await _channelService.UpdateCustomerAccountAsync(transactionModel.CustomerAccount, GetServiceHeader());

                              
                                if (updateWithinLimitResult)
                                {
                                    TempData["RequestSuccess"] = $"success: customer new balance is {transactionModel.CustomerAccount.NewAvailableBalance}";
                                }

                                //PrintReceipt(withinLimitsCashDepositJournal);
                                break;

                            case CashDepositCategory.AboveMaximumAllowed:

                                //TempData["test"] = "Above limit";

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
                                            targetCashDepositRequest.StatusDescription
                                        );

                                        // Show a message box with Yes/No options
                                        DialogResult result = MessageBox.Show(
                                            message,
                                            "Authorization Request",
                                            MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Question
                                        );

                                        if (result == DialogResult.Yes && !proceedAuthorizedCashDepositRequest)
                                        {
                                            proceedAuthorizedCashDepositRequest = true;

                                            #region Proceed with Authorized Transaction Request

                                            if (targetCashDepositRequest.Status == (int)CashDepositRequestAuthStatus.Authorized)
                                            {
                                                if (await _channelService.PostCashDepositRequestAsync(targetCashDepositRequest, GetServiceHeader()))
                                                {
                                                    var authorizedJournal = await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(transactionModel, tarrifs);

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
                                    // Format the message to display
                                    string message = string.Format(
                                        "{0}.\nDo you want to proceed and place a cash deposit authorization request?",
                                        EnumHelper.GetDescription(cashDepositCategory)
                                    );

                                    // Show the message box with Yes/No options
                                    DialogResult result = MessageBox.Show(
                                        Form.ActiveForm,
                                        message,
                                        "Authorization Request",
                                        MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Question 
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
                                                    MessageBoxIcon.Information
                                                );
                                            }
                                            else
                                            {
                                                // Show failure message
                                                MessageBox.Show(
                                                    Form.ActiveForm,
                                                    "Operation failed!",
                                                    "Error",
                                                    MessageBoxButtons.OK,
                                                    MessageBoxIcon.Error
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
                                                MessageBoxIcon.Error
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

                    default:
                        //throw new InvalidOperationException("Unsupported transaction type.");
                        TempData["Error"] = "You may have entered wrong transaction type";
                        return;
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }


        [HttpPost]
        public async Task<ActionResult> Create(CustomerTransactionModel transactionModel)
        {

            SelectedCustomerAccount = transactionModel.CustomerAccount;
            SelectedCustomerAccount = await _channelService.FindCustomerAccountAsync(transactionModel.CustomerAccount.Id, false, true, false, false, GetServiceHeader());
            SelectedBranch = await _channelService.FindBranchAsync(transactionModel.BranchId, GetServiceHeader());
            _selectedTeller = await GetCurrentTeller();

            transactionModel.PostingPeriodId = Guid.NewGuid();
            transactionModel.DebitChartOfAccountId = Guid.NewGuid();
            transactionModel.PrimaryDescription = "ok";
            transactionModel.SecondaryDescription = string.Format("B{0}/T{1}/#{2}", SelectedBranch.Code, _selectedTeller.Code, _selectedTeller.ItemsCount);
            transactionModel.Reference = string.Format("{0}", SelectedCustomerAccount.CustomerReference1);
            transactionModel.CreditChartOfAccountId = (Guid)transactionModel.Teller.ChartOfAccountId;

            transactionModel.CreditCustomerAccountId = transactionModel.CustomerAccount.Id;
            transactionModel.DebitCustomerAccountId = transactionModel.CustomerAccount.Id;


            
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

                TempData["SuccessMessage"] = "You successfully made a Cash Deposit Authorization Request";
              
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

        public async Task<TellerDTO> GetCurrentTeller() 
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

        
    }
}
    

