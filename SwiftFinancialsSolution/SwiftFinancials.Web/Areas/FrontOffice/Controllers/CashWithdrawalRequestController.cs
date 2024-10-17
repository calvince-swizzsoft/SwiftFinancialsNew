using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Windows.Forms;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class CashWithdrawalRequestController : MasterController
    {
        // GET: FrontOffice/CashWithdrawalRequest
        public async Task<ActionResult> Index()
        {

            await ServeNavigationMenus();
            return View();
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

           

            var pageCollectionInfo = await _channelService.FindCashDepositRequestsByFilterInPageAsync(startDate, endDate, 1, "", 2, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());


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


            var pageCollectionInfo = await _channelService.FindCashWithdrawalRequestsByFilterInPageAsync(startDate, endDate, 1, "", 2, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());



            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;


                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.PaidDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CashWithdrawalRequestDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);

        }

        [HttpPost]
        public async Task<JsonResult> CashTransferRequestsIndex(JQueryDataTablesModel jQueryDataTablesModel, int? status, int? customerFilter)
        {

            int totalRecordCount = 0;

            int searchRecordCount = 0;

            DateTime startDate = DateTime.MinValue;

            DateTime endDate = DateTime.MaxValue;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            //pageIndex++;


            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            //var pageCollectionInfo = await _channelService.FindCashTransferRequestsByFilterInPageAsync(employeeId, startDate, endDate, status, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            var pageCollectionInfo = await _channelService.FindCashTransferRequestsByStatusAndFilterInPageAsync(startDate, endDate, jQueryDataTablesModel.sSearch, (int)status, (int)customerFilter, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;


                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CashWithdrawalRequestDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);

        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var tellerDTO = await _channelService.FindCashWithdrawalRequestAsync(id);

            return View(tellerDTO);
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {

                //var observableCollection1 = await _channelService.FindCashDepositRequestsAsync(GetServiceHeader());

                //cashWithdrawalRequestDTO.CashDepositRequests = observableCollection.ToList();

                //var model = new CashWithdrawalRequestDTO
                //{
                // Initialize with empty list if no data is available
                // CashDepositRequests = observableCollection1.ToList()
                //};

                ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

                ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(string.Empty);

                ViewBag.CashTransferStatus = GetCashTransferStatusSelectList(string.Empty);
                ViewBag.CustomerFilterSelectList = GetCustomerFilterSelectList(string.Empty);


                return View();
            }


            bool includeBalances = false;
            bool includeProductDescription = false;
            bool includeInterestBalanceForLoanAccounts = false;
            bool considerMaturityPeriodForInvestmentAccounts = false;


            var customer = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());

            CashWithdrawalRequestDTO cashWithdrawalRequestDTO = new CashWithdrawalRequestDTO();

            if (customer != null)
            {
                cashWithdrawalRequestDTO.CustomerAccountCustomerId = customer.Id;
                cashWithdrawalRequestDTO.CustomerAccountId = customer.Id;
                cashWithdrawalRequestDTO.CustomerAccountCustomerIndividualFirstName = customer.CustomerIndividualFirstName;
                //  accountClosureRequestDTO.CustomerAccountCustomerIndividualPayrollNumbers = customer.CustomerIndividualPayrollNumbers;
                cashWithdrawalRequestDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                cashWithdrawalRequestDTO.CustomerAccountCustomerAccountTypeTargetProductCode = customer.CustomerAccountTypeProductCode;
                cashWithdrawalRequestDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                cashWithdrawalRequestDTO.CustomerAccountCustomerReference1 = customer.CustomerReference1;
                cashWithdrawalRequestDTO.CustomerAccountCustomerReference2 = customer.CustomerReference2;
                cashWithdrawalRequestDTO.CustomerAccountCustomerReference3 = customer.CustomerReference3;
                cashWithdrawalRequestDTO.CustomerAccountCustomerSerialNumber = customer.CustomerSerialNumber;
                cashWithdrawalRequestDTO.CustomerAccountCustomerPersonalIdentificationNumber = customer.CustomerPersonalIdentificationNumber;
                cashWithdrawalRequestDTO.CustomerAccountRemarks = customer.Remarks;
                cashWithdrawalRequestDTO.BranchDescription = customer.BranchDescription;
                cashWithdrawalRequestDTO.BranchId = customer.BranchId;
                //cashWithdrawalRequestDTO.CustomerAccountRemarks = customer.Remarks;
            }

            var observableCollection = await _channelService.FindCashDepositRequestsAsync(GetServiceHeader());

            cashWithdrawalRequestDTO.CashDepositRequests = observableCollection.ToList();

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(string.Empty);

            ViewBag.CashTransferStatus = GetCashTransferStatusSelectList(string.Empty);
            ViewBag.CustomerFilterSelectList = GetCustomerFilterSelectList(string.Empty);

            return View(cashWithdrawalRequestDTO);
        }


        [HttpPost]
        public async Task<ActionResult> Create(CashWithdrawalRequestDTO cashWithdrawalRequestDTO)
        {
            cashWithdrawalRequestDTO.ValidateAll();

            if (!cashWithdrawalRequestDTO.HasErrors)
            {
                await _channelService.AddCashWithdrawalRequestAsync(cashWithdrawalRequestDTO, GetServiceHeader());

                return RedirectToAction("Create");
            }
            else
            {
                var errorMessages = cashWithdrawalRequestDTO.ErrorMessages;
                ViewBag.TellerTypeSelectList = GetTellerTypeSelectList(cashWithdrawalRequestDTO.CustomerAccountCustomerTypeDescription.ToString());

                return View(cashWithdrawalRequestDTO);
            }
        }



        //public async Task<ActionResult> Approval()
        //{
        //    await ServeNavigationMenus();
        //    Guid id = new Guid();

        //    ViewBag.BatchAuthOptionSelectList = GetBatchAuthOptionSelectList(string.Empty);


        //    var CashWithdrawal = await _channelService.FindCashWithdrawalRequestAsync(id, GetServiceHeader());

        //    ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);
        //    return View(CashWithdrawal);
        //}

        //public async Task<ActionResult> AuthorizationView (Guid id)
        //{

        //    return View

        //}

        [HttpPost]
        public async Task<ActionResult> HandleRequest(Guid id, int requestType, bool isApproval, String remarks)
        {
            int customerTransactionAuthOption = isApproval ? 1 : 2;
            //var serviceHeader = GetServiceHeader();

            FrontOfficeCashRequestType cashRequestType = (FrontOfficeCashRequestType)requestType;

            switch (cashRequestType)
            {
                case FrontOfficeCashRequestType.CashWithdrawal:
                    var cashWithdrawalRequest = await _channelService.FindCashWithdrawalRequestAsync(id, GetServiceHeader());
                    var withdrawalAuthorization = await _channelService.AuthorizeCashWithdrawalRequestAsync(cashWithdrawalRequest, customerTransactionAuthOption, GetServiceHeader());

                    cashWithdrawalRequest.Remarks = remarks;

                    var withdrawalTxn = (int)FrontOfficeCashRequestType.CashWithdrawal;
                    var withdrawingAccount = await _channelService.FindCustomerAccountAsync((Guid)cashWithdrawalRequest.CustomerAccountId, true, true, true, true, GetServiceHeader());

                    var tellerToCredit = await FindTellerWithRequestDTO(cashWithdrawalRequest);

                    var withdrawalBranch = await _channelService.FindBranchAsync(withdrawingAccount.BranchId, GetServiceHeader());

                    CustomerTransactionModel customerTransactionModel = new CustomerTransactionModel();

                    customerTransactionModel.PrimaryDescription = "Customer Withdrawal From Teller";
                    customerTransactionModel.SecondaryDescription = string.Format("B{0}/T{1}/#{2}", withdrawalBranch.Code, tellerToCredit.Code, tellerToCredit.ItemsCount);

                    customerTransactionModel.Reference = string.Format("{0}", customerTransactionModel.Reference ?? withdrawingAccount.CustomerReference1);
                    
           

                    //var withdrawalRandom = new Random();
                    //var withdrawalCodeString = DateTime.UtcNow.ToString("yyMMddHHmmss") + withdrawalRandom.Next(1000, 9999);
                    //customerTransactionModel.TransactionCode = int.Parse(withdrawalCodeString);



                    customerTransactionModel.CustomerAccount = withdrawingAccount;

                    customerTransactionModel.BranchId = withdrawingAccount.BranchId;


                    customerTransactionModel.DebitCustomerAccount = withdrawingAccount;
                    customerTransactionModel.DebitCustomerAccountId = withdrawingAccount.Id;
                    customerTransactionModel.CreditCustomerAccountId = withdrawingAccount.Id;
                    customerTransactionModel.CreditCustomerAccount = withdrawingAccount;
                    customerTransactionModel.DebitChartOfAccountId = withdrawingAccount.CustomerAccountTypeTargetProductChartOfAccountId;

                    customerTransactionModel.TotalValue = cashWithdrawalRequest.Amount; 

                    customerTransactionModel.CreditChartOfAccountId = (Guid)tellerToCredit.ChartOfAccountId;

                    if (withdrawalAuthorization) 
                    {

                       var withdrawalTariffs = await _channelService.ComputeTellerCashTariffsAsync(withdrawingAccount, customerTransactionModel.TotalValue, withdrawalTxn, GetServiceHeader());
                       await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(customerTransactionModel, withdrawalTariffs, GetServiceHeader());
                    }

                    return HandleAuthorizationResult(withdrawalAuthorization, isApproval);

                case FrontOfficeCashRequestType.CashDeposit:
                    var cashDepositRequest = await _channelService.FindCashDepositRequestAsync(id, GetServiceHeader());
                    var depositAuthorization = await _channelService.AuthorizeCashDepositRequestAsync(cashDepositRequest, customerTransactionAuthOption, GetServiceHeader());
                    //var depositingAccount = await _channelService.FindCustomerAccountAsync(cashDepositRequest.CustomerAccountId, true, true, true, true, GetServiceHeader());

                    //cashDepositRequest.Remarks = remarks;

                    //int txnType = (int)FrontOfficeTransactionType.CashDeposit;

                    //CustomerTransactionModel transactionModel = new CustomerTransactionModel
                    //{
                    //    CustomerAccount = depositingAccount,

                    //    BranchId = depositingAccount.BranchId,

                    //    TotalValue = cashDepositRequest.Amount
                    //};

                    //var depositBranch = await _channelService.FindBranchAsync(transactionModel.BranchId, GetServiceHeader());


                    //var tellerToDebit = await FindTellerWithRequestDTO(cashDepositRequest);

                    //transactionModel.PrimaryDescription = "Customer Deposit To Teller";
                    //transactionModel.SecondaryDescription = string.Format("B{0}/T{1}/#{2}", depositBranch.Code, tellerToDebit.Code, tellerToDebit.ItemsCount);

                    //transactionModel.Reference = string.Format("{0}", transactionModel.Reference ?? depositingAccount.CustomerReference1);

                    //var depositRandom = new Random();
                    //var depositCodeString = DateTime.UtcNow.ToString("yyMMddHHmmss") + depositRandom.Next(1000, 9999);
                    //transactionModel.TransactionCode = int.Parse(depositCodeString);

                    //transactionModel.DebitChartOfAccountId = (Guid)tellerToDebit.ChartOfAccountId;

                    //transactionModel.DebitCustomerAccountId = depositingAccount.Id;
                    //transactionModel.DebitCustomerAccount = depositingAccount;
                    //transactionModel.CreditCustomerAccountId = depositingAccount.Id;
                    //transactionModel.CreditCustomerAccount = depositingAccount;
                    //transactionModel.CreditChartOfAccountId = depositingAccount.CustomerAccountTypeTargetProductChartOfAccountId;

                    //if (depositAuthorization && await _channelService.PostCashDepositRequestAsync(cashDepositRequest, GetServiceHeader())) {

                       

                    //    var depositTariffs = await _channelService.ComputeTellerCashTariffsAsync(transactionModel.CustomerAccount, transactionModel.TotalValue, txnType, GetServiceHeader());
                    //    await _channelService.AddJournalWithCustomerAccountAndTariffsAsync(transactionModel, depositTariffs, GetServiceHeader());
                    //    transactionModel.CustomerAccount.NewAvailableBalance = transactionModel.CustomerAccount.AvailableBalance + transactionModel.TotalValue;
                    //    var result = await _channelService.UpdateCustomerAccountAsync(transactionModel.CustomerAccount, GetServiceHeader());
                    //}

                    return HandleAuthorizationResult(depositAuthorization, isApproval);

                case FrontOfficeCashRequestType.CashTransfer:
                    
                    var cashTransferRequest = await _channelService.FindCashTransferRequestAsync(id, GetServiceHeader());

                    cashTransferRequest.Remarks = remarks;
   
                    int cashTransferRequestAcknowledgeOption = (customerTransactionAuthOption == 1) ? 2 : 3;




                    var transferAuthorization = await _channelService.AcknowledgeCashTransferRequestAsync(cashTransferRequest, cashTransferRequestAcknowledgeOption, GetServiceHeader());

                    return HandleAuthorizationResult(transferAuthorization, isApproval);

                default:
                    return RedirectToAction("Create");
            }
        }

        private ActionResult HandleAuthorizationResult(bool authorization, bool isApproval)
        {
            if (authorization)
            {
                MessageBox.Show(
                    Form.ActiveForm,
                    $"Operation completed successfully: Transaction {(isApproval ? "Authorized" : "Rejected")}.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );
            }
            else
            {
                MessageBox.Show(
                    "An error occurred, please contact Admin",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );
            }

            return RedirectToAction("Create");
        }

        private async Task<TellerDTO> FindTellerWithRequestDTO(CashDepositRequestDTO cashDepositRequestDTO)
        {
            var customers = await _channelService.FindCustomersAsync(GetServiceHeader());
            var customerToDebit = customers.FirstOrDefault(c => c.AddressEmail == cashDepositRequestDTO.CreatedBy);
            var employees = await _channelService.FindEmployeesAsync(GetServiceHeader());
            var targetEmployee = employees.FirstOrDefault(e => e.CustomerId == customerToDebit.Id);
            var tellerToDebit = await _channelService.FindTellerByEmployeeIdAsync(targetEmployee.Id, true, GetServiceHeader());

            return tellerToDebit;
        }

        private async Task<TellerDTO> FindTellerWithRequestDTO(CashWithdrawalRequestDTO cashWithdrawalRequestDTO)
        {
            var customers = await _channelService.FindCustomersAsync(GetServiceHeader());
            var customerToDebit = customers.FirstOrDefault(c => c.AddressEmail == cashWithdrawalRequestDTO.CreatedBy);
            var employees = await _channelService.FindEmployeesAsync(GetServiceHeader());
            var targetEmployee = employees.FirstOrDefault(e => e.CustomerId == customerToDebit.Id);
            var tellerToDebit = await _channelService.FindTellerByEmployeeIdAsync(targetEmployee.Id, true, GetServiceHeader());

            return tellerToDebit;
        }


    }
}