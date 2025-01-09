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

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(string.Empty);
            return View();
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



        [HttpPost]
        public async Task<JsonResult> FetchTreasuryTransactions(JQueryDataTablesModel jQueryDataTablesModel, DateTime startDate, DateTime endDate)
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

            var activeUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            ActiveTreasury = await _channelService.FindTreasuryByBranchIdAsync((Guid)activeUser.BranchId, true, GetServiceHeader());

            var missingParameters = new List<string>();

            if (activeUser == null)
            {
                missingParameters.Add("Active User");
            }

            if (ActiveTreasury == null)
            {
                missingParameters.Add("Treasury");
            }

            if (missingParameters.Any())
            {
                var missingMessage = $"Some features may not work due to lack of: {string.Join(", ", missingParameters)}";

                return Json(new { success = false, message = "Operation error: " + missingMessage });
            }



            var pageCollectionInfo = await _channelService.FindGeneralLedgerTransactionsByChartOfAccountIdAndDateRangeAndFilterInPageAsync(
                  0,
                  int.MaxValue,
                  (Guid)ActiveTreasury.ChartOfAccountId,
                  startDate,
                  endDate,
                  jQueryDataTablesModel.sSearch,
                  16,
                  2,
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
                //searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                var sortedData = pageCollectionInfo.PageCollection.OrderByDescending(gl => gl.JournalCreatedDate).ToList();


                var paginatedData = sortedData.Skip(jQueryDataTablesModel.iDisplayStart).Take(jQueryDataTablesModel.iDisplayLength).ToList();


                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? sortedData.Count : totalRecordCount;


                var response = new
                {
                    draw = jQueryDataTablesModel.sEcho,
                    recordsTotal = totalRecordCount,
                    recordsFiltered = searchRecordCount,
                    data = paginatedData,
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
           
            var selectList = ViewBag.TreasuryTransactionTypeSelectList as SelectList;

          
            if (selectList != null)
            {
                var selectedValue = selectList.SelectedValue;
              
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

            if (!fiscalCountDTO.HasErrors)
            {
                int treasuryTransactionType = fiscalCountDTO.TransactionType;
                TransactionModel transactionModel = new TransactionModel();


                CurrentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
                var userId = User.Identity.GetUserId();
                var activeUser = string.IsNullOrEmpty(userId) ? null : await _applicationUserManager.FindByIdAsync(userId);
                ActiveTreasury = activeUser?.BranchId.HasValue == true
                    ? await _channelService.FindTreasuryByBranchIdAsync(activeUser.BranchId.Value, true, GetServiceHeader())
                    : null;


                var missingParameters = new List<string>();

                if (CurrentPostingPeriod == null)
                {
                    missingParameters.Add("Posting Period");
                }
                else
                {
                    fiscalCountDTO.PostingPeriodId = CurrentPostingPeriod.Id;
                }

                if (activeUser == null)
                {
                    missingParameters.Add("Active User");
                }

                if (ActiveTreasury == null)
                {
                    missingParameters.Add("Treasury");
                }
                else
                {
                    fiscalCountDTO.ChartOfAccountId = ActiveTreasury.ChartOfAccountId;
                    fiscalCountDTO.BranchId = ActiveTreasury.BranchId;
                }

                if (missingParameters.Any())
                {
                    var missingMessage = $"The transaction won't proceed. Unable to retrieve {string.Join(", ", missingParameters)}.";

                    return Json(new { success = false, message = "Operation error: " + missingMessage });
                }


                transactionModel.TotalValue = fiscalCountDTO.TotalValue;
                transactionModel.PostingPeriodId = CurrentPostingPeriod.Id;
                transactionModel.PrimaryDescription = fiscalCountDTO.TransactionTypeDescription;
                transactionModel.ValueDate = DateTime.Today;

                ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(string.Empty);

                try
                {
                    switch ((TreasuryTransactionType)treasuryTransactionType)
                    {
                        case TreasuryTransactionType.BankToTreasury:

                            

                            var sendingBank = await _channelService.FindBankAsync(fiscalCountDTO.Id, GetServiceHeader());
                            if (sendingBank == null)
                            {
                             
                                return Json(new { success = false, message = "Operation Failed: Sending bank not found" });
                            }

                            var bankLinkages = await _channelService.FindBankLinkagesAsync(GetServiceHeader());
                            var matchingBankLinkage = bankLinkages.FirstOrDefault(li => li.BankName == sendingBank.Description);
                            if (matchingBankLinkage == null)
                            {
                                return Json(new { success = false, message = "Operation Failed: No matching bank linkage found for selected bank account" });
                            }

                            transactionModel.DebitChartOfAccountId = matchingBankLinkage.ChartOfAccountId;
                            transactionModel.CreditChartOfAccountId = ActiveTreasury.ChartOfAccountId;
                            transactionModel.TransactionCode = (int)SystemTransactionCode.BankToTreasury;
                            break;

                        case TreasuryTransactionType.TreasuryToTeller:
                            transactionModel.DebitChartOfAccountId = ActiveTreasury.ChartOfAccountId;
                            var teller = await _channelService.FindTellerAsync(fiscalCountDTO.Id, false, GetServiceHeader());
                            if (teller == null)
                            {
                            
                                return Json(new { success = false, message = "Operation Failed: Teller Not Found" });
                            }
                            transactionModel.CreditChartOfAccountId = (Guid)teller.ChartOfAccountId;
                            transactionModel.TransactionCode = (int)SystemTransactionCode.TreasuryToTeller;
                            break;

                        case TreasuryTransactionType.TreasuryToBank:
                            transactionModel.DebitChartOfAccountId = ActiveTreasury.ChartOfAccountId;
                            var receivingBank = await _channelService.FindBankAsync(fiscalCountDTO.Id, GetServiceHeader());
                            if (receivingBank == null)
                            {
                               
                                return Json(new { success = false, message = "Operation Failed: Receiving bank not found"});
                            }
                            var linkages = await _channelService.FindBankLinkagesAsync(GetServiceHeader());
                            var linkage = linkages.FirstOrDefault(l => l.BankName == receivingBank.Description);
                            if (linkage == null)
                            {
                               
                                return Json(new { success = false, message = "Operation Failed: No matching bank linkage found for selected bank account." });

                            }
                            transactionModel.CreditChartOfAccountId = linkage.ChartOfAccountId;
                            transactionModel.TransactionCode = (int)SystemTransactionCode.TreasuryToBank;

                            break;

                        case TreasuryTransactionType.TreasuryToTreasury:
                            transactionModel.DebitChartOfAccountId = ActiveTreasury.ChartOfAccountId;
                            var treasury = await _channelService.FindTreasuryAsync(fiscalCountDTO.Id, true, GetServiceHeader());
                            if (treasury == null)
                            {
                                
                                return Json(new { success = false, message = "Operation Failed: Receiving treasury not found" });
                            }
                            transactionModel.CreditChartOfAccountId = treasury.ChartOfAccountId;
                            transactionModel.TransactionCode = (int)SystemTransactionCode.TreasuryToTreasury;
                            break;
                    }

                    transactionModel.fiscalCountDTO = fiscalCountDTO;
                    //await ProcessTreasuryTransactionAsync(transactionModel);

                    switch ((TreasuryTransactionType)treasuryTransactionType)
                    {


                        case TreasuryTransactionType.BankToTreasury:


                            var bankToTreasuryJournal = await _channelService.AddCashManagementJournalAsync(transactionModel.fiscalCountDTO, transactionModel, GetServiceHeader());

                            var treasuryAccount = await _channelService.FindChartOfAccountAsync(transactionModel.CreditChartOfAccountId, GetServiceHeader());

                            var updateTreasuryAccount = await _channelService.UpdateChartOfAccountAsync(treasuryAccount, GetServiceHeader());

                            if (updateTreasuryAccount)
                            {

                                string message = $"Operation success";

                                return Json(new { success = true, message = message });
                            
                            }

                            break;

                        case TreasuryTransactionType.TreasuryToTeller:

                            if (ActiveTreasury.BookBalance < transactionModel.fiscalCountDTO.TotalValue)
                            {
                

                                return Json(new { success = false, message = "Operation Failed: Insufficient Balance" });
                            }

                            var bankToTellerJournal = await _channelService.AddCashManagementJournalAsync(transactionModel.fiscalCountDTO, transactionModel, GetServiceHeader());
                            var chartOfAccount = await _channelService.FindChartOfAccountAsync(transactionModel.CreditChartOfAccountId, GetServiceHeader());
                            var updateChartOfAccount = await _channelService.UpdateChartOfAccountAsync(chartOfAccount, GetServiceHeader());

                            if (updateChartOfAccount)
                            {


                                string message = $"Operation success";

                                return Json(new { success = true, message = message });

                            }


                            break;


                        case TreasuryTransactionType.TreasuryToBank:

                            if (ActiveTreasury.BookBalance < transactionModel.fiscalCountDTO.TotalValue)
                            {
                       
   

                                return Json(new { success = false, message = "Operation Failed: Insufficient Balance" });
                            }


                            var treasuryToBankJournal = await _channelService.AddCashManagementJournalAsync(transactionModel.fiscalCountDTO, transactionModel, GetServiceHeader());

                            var treasuryAcc = await _channelService.FindChartOfAccountAsync(transactionModel.CreditChartOfAccountId, GetServiceHeader());

                            var updateTreasuryAcc = await _channelService.UpdateChartOfAccountAsync(treasuryAcc, GetServiceHeader());

                            if (updateTreasuryAcc)
                            {

                                string message = $"Operation success";
                             
                             return Json(new { success = true, message = message });


                            }


                            break;


                        case TreasuryTransactionType.TreasuryToTreasury:

                            if (ActiveTreasury.BookBalance < transactionModel.fiscalCountDTO.TotalValue)
                            {
                          

                                return Json(new { success = false, message = "Operation Failed: Insufficient Balance" });
                            }


                            var treasuryToTreasuryJournal = await _channelService.AddCashManagementJournalAsync(transactionModel.fiscalCountDTO, transactionModel, GetServiceHeader());

                            var treasuryAc = await _channelService.FindChartOfAccountAsync(transactionModel.CreditChartOfAccountId, GetServiceHeader());

                            var updateTreasuryAc = await _channelService.UpdateChartOfAccountAsync(treasuryAc, GetServiceHeader());

                            if (updateTreasuryAc)
                            {

                                string message = $"Operation success";

                                return Json(new { success = true, message = message });
                            }



                            break;
                    }

                    return Json(new { success = true, message = "Operation Success: Transaction processed successfully!" });
                      
                }
                catch (Exception ex)
                {
    
                    return Json(new { success = false, message = "Operation Failed: " + ex.Message });
                }
            }
            else
            {
     
                return Json(new { success = false, message = "Operation Failed: There are errors in the form" });
            }
        }



  
    }
}