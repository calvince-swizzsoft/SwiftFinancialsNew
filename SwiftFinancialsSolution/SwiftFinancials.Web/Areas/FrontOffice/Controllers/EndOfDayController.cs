using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using Microsoft.AspNet.Identity;
using System.Windows.Forms;
using SwiftFinancials.Presentation.Infrastructure.Models;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using System.Globalization;
using System.Text;
using System.Drawing.Printing;
using System.Drawing;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class EndOfDayController : MasterController
    {

        TellerDTO _selectedTeller;

        TreasuryDTO _selectedTreasury;

        PostingPeriodDTO _selectedPostingPeriod;

        BranchDTO _selectedBranch;

        EmployeeDTO _selectedEmployee;

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

        public PostingPeriodDTO SelectedPostingPeriod
        {
            get { return _selectedPostingPeriod; }

            set 
            {
                if (_selectedPostingPeriod != value)
                {
                    _selectedPostingPeriod = value;
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

        public TreasuryDTO SelectedTreasury
        {
            get { return _selectedTreasury; }
            set
            {
                if (_selectedTreasury != value)
                {
                    _selectedTreasury = value;

                }


            }
        }

        private bool IsBusy { get; set; } // Property to indicate if an operation is in progress

        private string receiptContent;
        // GET: FrontOffice/EndOfDay
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            return View();
        }

        public async Task<ActionResult> Create(Guid? id)
        {

            await ServeNavigationMenus();
           
            var model = new CashTransferRequestDTO();

            var currentUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            _selectedTeller = await GetCurrentTeller();

            var missingParameters = new List<string>();

            if (currentUser == null)
            {
                missingParameters.Add("Active User");
            }

            if (SelectedTeller == null)
            {
                missingParameters.Add("Teller");
            }

            // Check if any parameter is missing
            if (missingParameters.Any())
            {
                var missingMessage = $"Some features may not work, you are missing {string.Join(", ", missingParameters)}";

                return Json(new { success = false, message = "Operation error: " + missingMessage });
            }
            var untransferredCheques = await _channelService.FindUnTransferredExternalChequesByTellerId(SelectedTeller.Id, "", GetServiceHeader());

            var untransferredChequesValue = untransferredCheques.Sum(cheque => cheque.Amount);

            model.EmployeeId = SelectedTeller.EmployeeId;
            model.TotalCredits = SelectedTeller.TotalCredits;
            model.TotalDebits = SelectedTeller.TotalDebits;
            model.BookBalance = SelectedTeller.BookBalance;

            model.OpeningBalance = SelectedTeller.OpeningBalance;
            model.ClosingBalance = SelectedTeller.ClosingBalance;

            model.UntransferredChequesValue = untransferredChequesValue;
            return View(model);
        }


        [HttpPost]
        public async Task<ActionResult> Create(CashTransferRequestDTO cashTransferRequestDTO)
        {
            /*ashTransferRequestDTO.ValidateAll();*/

            var currentUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
      
           _selectedTeller = await GetCurrentTeller();

            cashTransferRequestDTO.EmployeeId = SelectedTeller.EmployeeId;

            _selectedTeller.TellerTotalCheques = cashTransferRequestDTO.UntransferredChequesValue;
            //TellerCashBalanceStatus = cashTransferRequestDTO.ClosingBalanceStatus;

            _selectedEmployee = await _channelService.FindEmployeeAsync((Guid)SelectedTeller.EmployeeId, GetServiceHeader());

            _selectedBranch = await _channelService.FindBranchAsync(SelectedEmployee.BranchId, GetServiceHeader());

            _selectedPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());

            //maybe let user select a treasury
            _selectedTreasury = await _channelService.FindTreasuryByBranchIdAsync(SelectedBranch.Id, true, GetServiceHeader());

            if (!cashTransferRequestDTO.HasErrors)
            {

                var model = new TransactionModel();

                try
                {

                    IsBusy = true;

                    var proceedEndOfDayTransaction = default(bool);
                    //var proceedPrintReceipt = default(bool);

                    model.TransactionCode = (int)SystemTransactionCode.TellerEndOfDay;

                    model.PrimaryDescription = currentUser.UserName;

                    //TellerCashBalanceStatus = cashTransferRequestDTO.TellerCashBalanceStatusValue;

                    //model.Reference = EnumHelper.GetDescription((TellerCashBalanceStatus)cashTransferRequestDTO.TellerCashBalanceStatusValue);

                    if (cashTransferRequestDTO.TellerCashBalanceStatusValue == 0)
                        model.Reference = TellerCashBalanceStatus.Balanced.ToString();
                    else if (cashTransferRequestDTO.TellerCashBalanceStatusValue < 0)
                        model.Reference = TellerCashBalanceStatus.Shortage.ToString();
                    else
                        model.Reference = TellerCashBalanceStatus.Excess.ToString(); 


                    if (SelectedTeller != null && !SelectedTeller.IsLocked)
                        model.SecondaryDescription = SelectedTeller.Description;

                    //model.ModuleNavigationItemCode = (int)NavigationItemCode.EndOfDayCommand;

                    if (SelectedPostingPeriod != null)
                        model.PostingPeriodId = SelectedPostingPeriod.Id;

                    if (SelectedBranch != null)
                        model.BranchId = SelectedBranch.Id;

                    if (SelectedTreasury != null)
                        model.CreditChartOfAccountId = SelectedTreasury.ChartOfAccountId;

                    if (SelectedTeller != null && !SelectedTeller.IsLocked)
                        model.DebitChartOfAccountId = SelectedTeller.ChartOfAccountId ?? Guid.Empty;


                    model.TotalValue = cashTransferRequestDTO.Amount;
                    model.ValidateAll();

                    if (model.HasErrors)
                    {
                        IsBusy = false;

                        string errorMessages = string.Join(Environment.NewLine, model.ErrorMessages);
                        //MessageBox.Show(errorMessages, "Error Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                        return Json(new { success = false, message = "Operation error: " + errorMessages });
                    }
                    else if (SelectedTeller.TellerTotalCheques != 0m)
                    {
                        IsBusy = false;

                        //MessageBox.Show("Sorry, but you need to first transfer your cheques!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);


                        return Json(new { success = false, message = "Operation error: " + "Sorry, but you need to first transfer your cheques!" });
                        //return View(cashTransferRequestDTO);
                    }
                    else if (await _channelService.EndOfDayExecutedAsync(SelectedEmployee, GetServiceHeader()))
                    {
                        IsBusy = false;

                        //MessageBox.Show("Sorry, but you have already closed your day!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                        return Json(new { success = false, message = "Operation error: " + "Sorry, but you have already closed your day!" });

                        //return View();
                    }
                    else
                    {
                        var NewFiscalCount = new FiscalCountDTO();

                        NewFiscalCount.TransactionCode = (int)SystemTransactionCode.TellerEndOfDay;
                        NewFiscalCount.PostingPeriodId = model.PostingPeriodId;
                        NewFiscalCount.BranchId = model.BranchId;
                        NewFiscalCount.ChartOfAccountId = model.DebitChartOfAccountId;

                        NewFiscalCount.PrimaryDescription = model.PrimaryDescription;
                        NewFiscalCount.SecondaryDescription = model.SecondaryDescription;
                        NewFiscalCount.Reference = model.Reference;

                        //NewFiscalCount.DenominationOneThousandValue = CurrencyCountModel.OneThousandValue;
                        //NewFiscalCount.DenominationFiveHundredValue = CurrencyCountModel.FiveHundredValue;
                        //NewFiscalCount.DenominationTwoHundredValue = CurrencyCountModel.TwoHundredValue;
                        //NewFiscalCount.DenominationOneHundredValue = CurrencyCountModel.OneHundredValue;
                        //NewFiscalCount.DenominationFiftyValue = CurrencyCountModel.FiftyValue;
                        //NewFiscalCount.DenominationFourtyValue = CurrencyCountModel.FourtyValue;
                        //NewFiscalCount.DenominationTwentyValue = CurrencyCountModel.TwentyValue;
                        //NewFiscalCount.DenominationTenValue = CurrencyCountModel.TenValue;
                        //NewFiscalCount.DenominationFiveValue = CurrencyCountModel.FiveValue;
                        //NewFiscalCount.DenominationOneValue = CurrencyCountModel.OneValue;
                        //NewFiscalCount.DenominationFiftyCentValue = CurrencyCountModel.FiftyCentValue;

                        NewFiscalCount.DestinationBranchId = Guid.NewGuid(); /*for passing validation*/

                        NewFiscalCount.ValidateAll();

                        if (NewFiscalCount.HasErrors)
                        {
                            IsBusy = false;
                            return Json(new { success = false, message = "Operation error: " + NewFiscalCount.ErrorMessages });


                        }
                        else
                        {

                                proceedEndOfDayTransaction = true;

                                #region proceed with End Of Day Transaction?

                                var cashManagementResult = await _channelService.AddCashManagementJournalAsync(NewFiscalCount, model, GetServiceHeader());

                            if (cashManagementResult != null)
                            {
                                var postExcessOrShortage = default(bool);

                                switch ((TellerCashBalanceStatus)cashTransferRequestDTO.TellerCashBalanceStatusValue)
                                {
                                    case TellerCashBalanceStatus.Balanced:
                                        break;
                                    case TellerCashBalanceStatus.Shortage:
                                        model.TotalValue = Math.Abs(cashTransferRequestDTO.Amount);
                                        model.CreditChartOfAccountId = SelectedTeller.ChartOfAccountId ?? Guid.Empty;
                                        model.DebitChartOfAccountId = SelectedTeller.ShortageChartOfAccountId ?? Guid.Empty;

                                        postExcessOrShortage = true;

                                        break;
                                    case TellerCashBalanceStatus.Excess:
                                        model.TotalValue = Math.Abs(cashTransferRequestDTO.Amount);
                                        model.CreditChartOfAccountId = SelectedTeller.ExcessChartOfAccountId ?? Guid.Empty;
                                        model.DebitChartOfAccountId = SelectedTeller.ChartOfAccountId ?? Guid.Empty;

                                        postExcessOrShortage = true;

                                        break;
                                    default:
                                        break;
                                }

                                if (postExcessOrShortage)
                                {
                                    model.PrimaryDescription = string.Format("{0}-{1}", "Transaction", EnumHelper.GetDescription((TellerCashBalanceStatus)cashTransferRequestDTO.TellerCashBalanceStatusValue));



                                    var resultJournal = await _channelService.AddJournalAsync(model, null, GetServiceHeader());


                                    #endregion
                                    var response = new
                                    {

                                        success = true,


                                        message = "Operation Success:" + "End of Day Operation Completed Successfully",

                                        journalId = resultJournal.Id,
                                        journalSequentialId = resultJournal.SequentialId,
                                        journalBranchDescription = resultJournal.BranchDescription,
                                        journalPrimaryDescription = resultJournal.PrimaryDescription,
                                        journalSecondaryDescription = resultJournal.SecondaryDescription,
                                        journalPostingPeriodDescription = resultJournal.PostingPeriodDescription,
                                        journalApplicationUserName = resultJournal.ApplicationUserName,
                                        journalCreatedDate = resultJournal.CreatedDate,
                                        journalTotalValue = resultJournal.TotalValue,
                                        journalReference = resultJournal.Reference
                                    };




                                    return Json(response);

                                }

                            }
      


                        }
                    }
                }

                catch (Exception ex)
                {
                  
                    return Json(new { success = false, message = "Operation error: " + ex.Message });

                }
            }
        
            else
            {
                var errorMessages = cashTransferRequestDTO.ErrorMessages;
                var combinedErrorMessages = string.Join(Environment.NewLine, errorMessages);         
                return Json(new { success = false, message = "Operation error: " + combinedErrorMessages });
            }


            return View();
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

    }

}