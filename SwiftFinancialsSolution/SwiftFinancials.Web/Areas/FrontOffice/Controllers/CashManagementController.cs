using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Models;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using Microsoft.AspNet.Identity;
using Application.MainBoundedContext.DTO.RegistryModule;
using System.Windows.Forms;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class CashManagementController : MasterController
    {

        TreasuryDTO _activeTreasury;

        PostingPeriodDTO _currentPostingPeriod; 


        public TreasuryDTO ActiveTreasury
        {
            get { return _activeTreasury; }
            set
            {
                if (_activeTreasury != value)
                {
                    _activeTreasury = value;

                }
            }
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

        public async Task<ActionResult> Search(Guid? id)
        {
            //string Remarks = "";
            await ServeNavigationMenus();

            ViewBag.WithdrawalNotificationCategorySelectList = GetWithdrawalNotificationCategorySelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }



            var customer = await _channelService.FindEmployeeAsync(parseId, GetServiceHeader());

            FiscalCountDTO fiscalCountDTO = new FiscalCountDTO();

            //WithdrawalNotificationDTOs = TempData["WithdrawalNotificationDTOs"] as ObservableCollection<WithdrawalNotificationDTO>;

            if (customer != null)
            {

                fiscalCountDTO.TellerId = customer.Id;
                fiscalCountDTO.Id = customer.Id;
                fiscalCountDTO.TellerDescription = customer.CustomerFullName;
                fiscalCountDTO.BranchDescription = customer.BranchDescription;
                // fiscalCountDTO.SecondaryDescription = customer.EmployeeTypeDescription;
                //fiscalCountDTO.CustomerIndividualIdentityCardNumber = customer.IndividualIdentityCardNumber;
                //fiscalCountDTO.CustomerStationDescription = customer.StationDescription;
                //fiscalCountDTO.CustomerStationZoneDivisionEmployerDescription = customer.StationZoneDivisionEmployerDescription;
                //Session["Test"] =Request.Form["h"] + "";
                //string mimi = Session["Test"].ToString();
                if (Session["Reference"] != null)
                {
                    fiscalCountDTO.Reference = Session["Reference"].ToString();
                }
                if (Session["TotalAmount"] != null)
                {
                    fiscalCountDTO.TotalAmount = Session["TotalAmount"].ToString();
                }
                if (Session["BranchDescription"] != null)
                {
                    fiscalCountDTO.TellerDescription = Session["BranchDescription"].ToString();
                }
                //
            }


            return View("Create", fiscalCountDTO);
        }

        [HttpPost]
        public ActionResult AssignText(string Reference, string TotalAmount)
        {
            Session["TotalAmount"] = TotalAmount;
            Session["Reference"] = Reference;
            return null;
        }






        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(string.Empty);

            int totalRecordCount = 0;
            int searchRecordCount = 0;
            decimal availableBalanceBroughtForward = 0;
            decimal availableBalanceCarriedForward = 0;
            decimal totalCredits = 0;
            decimal totalDebits = 0;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc";
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var currentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
            var activeUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            ActiveTreasury = await _channelService.FindTreasuryByBranchIdAsync((Guid)activeUser.BranchId, true, GetServiceHeader());

            var pageCollectionInfo = await _channelService.FindGeneralLedgerTransactionsByChartOfAccountIdAndDateRangeAndFilterInPageAsync(
                  pageIndex,
                  jQueryDataTablesModel.iDisplayLength,
                  (Guid)ActiveTreasury.ChartOfAccountId,
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
                availableBalanceBroughtForward = pageCollectionInfo.AvailableBalanceBroughtFoward;
                totalCredits = pageCollectionInfo.TotalCredits;
                totalDebits = pageCollectionInfo.TotalDebits;
                availableBalanceCarriedForward = pageCollectionInfo.AvailableBalanceCarriedForward;
                totalRecordCount = pageCollectionInfo.ItemsCount;
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                var response = new
                {
                    draw = jQueryDataTablesModel.sEcho,
                    recordsTotal = totalRecordCount,
                    recordsFiltered = searchRecordCount,
                    data = pageCollectionInfo.PageCollection,
                    summary = new
                    {
                        AvailableBalanceBroughtForward = availableBalanceBroughtForward,
                        TotalCredits = totalCredits,
                        TotalDebits = totalDebits,
                        AvailableBalanceCarriedForward = availableBalanceCarriedForward
                    }
                };

                return Json(response, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var emptyResponse = new
                {
                    draw = jQueryDataTablesModel.sEcho,
                    recordsTotal = totalRecordCount,
                    recordsFiltered = searchRecordCount,
                    data = new List<GeneralLedgerTransaction>(),
                    summary = new
                    {
                        AvailableBalanceBroughtForward = 0,
                        TotalCredits = 0,
                        TotalDebits = 0,
                        AvailableBalanceCarriedForward = 0
                    }
                };

                return Json(emptyResponse, JsonRequestBehavior.AllowGet);
            }
        }





        public async Task<ActionResult> Create(Guid? id, FiscalCountDTO fiscalCountDTO)
        {
            await ServeNavigationMenus();
            // Retrieve the SelectList from ViewBag
            var selectList = ViewBag.TreasuryTransactionTypeSelectList as SelectList;

            // Check if selectList is not null and has a selected value
            if (selectList != null)
            {
                var selectedValue = selectList.SelectedValue;
                // Now you have the selected value
            }


            ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(string.Empty);


            bool includeBalance = false;

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }


            var bankDTO = await _channelService.FindBankAsync(parseId, GetServiceHeader());

            if (bankDTO != null)
            {

                fiscalCountDTO.BranchId = bankDTO.Id;
                fiscalCountDTO.BranchDescription = bankDTO.Description;
                fiscalCountDTO.Description = bankDTO.Description;

                ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(TreasuryTransactionType.TreasuryToBank.ToString());

                ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(TreasuryTransactionType.BankToTreasury.ToString());
            }


            var tellers = await _channelService.FindTellerAsync(parseId, includeBalance, GetServiceHeader());



            if (tellers != null)
            {
                fiscalCountDTO.TellerId = tellers.EmployeeCustomerId;
                //fiscalCountDTO.Description = tellers.EmployeeCustomerFullName;
                //fiscalCountDTO.BranchId = tellers.EmployeeBranchId;
                ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(TreasuryTransactionType.TreasuryToTeller.ToString());
            }


            ActiveTreasury = await _channelService.FindTreasuryAsync(parseId, includeBalance, GetServiceHeader());



            if (ActiveTreasury != null)
            {
                fiscalCountDTO.TreasuryId = ActiveTreasury.Id;
                fiscalCountDTO.Description = ActiveTreasury.Description;
                fiscalCountDTO.BranchId = ActiveTreasury.BranchId;
                ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(TreasuryTransactionType.TreasuryToTreasury.ToString());
            }

            return View(fiscalCountDTO);
        }



        [HttpPost]
        public async Task<ActionResult> Create(FiscalCountDTO fiscalCountDTO)
        {
            fiscalCountDTO.ValidateAll();

            ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(fiscalCountDTO.TransactionType.ToString());

            if (!fiscalCountDTO.HasErrors)
            {

                int treasuryTransactionType = fiscalCountDTO.TransactionType;

                TransactionModel transactionModel = new TransactionModel();

                CurrentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());

                var activeUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
                ActiveTreasury = await _channelService.FindTreasuryByBranchIdAsync((Guid)activeUser.BranchId, true, GetServiceHeader());

                fiscalCountDTO.BranchId = ActiveTreasury.BranchId;

        

                fiscalCountDTO.PostingPeriodId = CurrentPostingPeriod.Id;

                //fiscalCountDTO.Id = ActiveTreasury.Id;
                fiscalCountDTO.ChartOfAccountId = ActiveTreasury.ChartOfAccountId;

                transactionModel.TotalValue = fiscalCountDTO.TotalValue;


                transactionModel.TransactionCode = fiscalCountDTO.TransactionCode;
                transactionModel.PostingPeriodId = CurrentPostingPeriod.Id;
                transactionModel.PrimaryDescription = fiscalCountDTO.TransactionTypeDescription;
                transactionModel.ValueDate = DateTime.Today;

                switch ((TreasuryTransactionType)treasuryTransactionType)
                {

                    case TreasuryTransactionType.BankToTreasury:


                        var creditChartOfAccountId = ActiveTreasury.ChartOfAccountId;
                        var balance = ActiveTreasury.BookBalance;

                        var sendingBank = await _channelService.FindBankAsync(fiscalCountDTO.Id, GetServiceHeader());

                        var bankLinkages = await _channelService.FindBankLinkagesAsync(GetServiceHeader());

                        var matchingBankLinkage = bankLinkages.FirstOrDefault(linkage => linkage.BankName == sendingBank.Description);



                        transactionModel.DebitChartOfAccountId = matchingBankLinkage.ChartOfAccountId;
                        transactionModel.CreditChartOfAccountId = creditChartOfAccountId;


                        break;

                    case TreasuryTransactionType.TreasuryToTeller:

                        transactionModel.DebitChartOfAccountId = ActiveTreasury.ChartOfAccountId;

                        var teller = await _channelService.FindTellerAsync(fiscalCountDTO.Id, false, GetServiceHeader());

                        transactionModel.CreditChartOfAccountId = (Guid)teller.ChartOfAccountId;

                        break;

                }

                transactionModel.fiscalCountDTO = fiscalCountDTO;

                await ProcessTreasuryTransactionAsync(transactionModel);

                return View(fiscalCountDTO);
            }
            else
            {
                var errorMessages = fiscalCountDTO.ErrorMessages;
                ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(fiscalCountDTO.TransactionType.ToString());
                return View(fiscalCountDTO);
            }
        }



        private async Task ProcessTreasuryTransactionAsync(TransactionModel transactionModel)
        {


            int treasuryTransactionType = transactionModel.fiscalCountDTO.TransactionType;

            var activeUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            ActiveTreasury = await _channelService.FindTreasuryByBranchIdAsync((Guid)activeUser.BranchId, true, GetServiceHeader());

            switch ((TreasuryTransactionType)treasuryTransactionType)
            {


                case TreasuryTransactionType.BankToTreasury:


                   var bankToTreasuryJournal =  await _channelService.AddCashManagementJournalAsync(transactionModel.fiscalCountDTO, transactionModel, GetServiceHeader());

                    var treasuryAccount = await _channelService.FindChartOfAccountAsync(transactionModel.CreditChartOfAccountId, GetServiceHeader());

                    var updateTreasuryAccount = await _channelService.UpdateChartOfAccountAsync(treasuryAccount, GetServiceHeader());

                    if (updateTreasuryAccount)
                    {

                        string message = $"Operation success";

                        MessageBox.Show(
                                          message,
                                          "Treasury to Teller",
                                          MessageBoxButtons.OK,
                                          MessageBoxIcon.Information,
                                          MessageBoxDefaultButton.Button1,
                                          MessageBoxOptions.ServiceNotification
                                      );
                    }

                    break;



                case TreasuryTransactionType.TreasuryToTeller:

                   

                    if (ActiveTreasury.BookBalance < transactionModel.fiscalCountDTO.TotalValue)
                    {
                        // Show a Windows Forms message box to the user
                        System.Windows.Forms.MessageBox.Show(
                            "Insufficient balance. The transaction cannot proceed.",
                            "Error",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Error,
                            System.Windows.Forms.MessageBoxDefaultButton.Button1,
                            System.Windows.Forms.MessageBoxOptions.ServiceNotification
                            );

                        break;
                    }

                    var bankToTellerJournal = await _channelService.AddCashManagementJournalAsync(transactionModel.fiscalCountDTO, transactionModel, GetServiceHeader());


                    var chartOfAccount = await _channelService.FindChartOfAccountAsync(transactionModel.CreditChartOfAccountId, GetServiceHeader());

                    var updateChartOfAccount = await _channelService.UpdateChartOfAccountAsync(chartOfAccount, GetServiceHeader());

                    if (updateChartOfAccount)
                    {


                        string message = $"Operation success";

                        MessageBox.Show(
                                          message,
                                          "Treasury to Teller",
                                          MessageBoxButtons.OK,
                                          MessageBoxIcon.Information,
                                          MessageBoxDefaultButton.Button1,
                                          MessageBoxOptions.ServiceNotification
                                      );
                    }


                    break; 



            }
        }


  
    }
}