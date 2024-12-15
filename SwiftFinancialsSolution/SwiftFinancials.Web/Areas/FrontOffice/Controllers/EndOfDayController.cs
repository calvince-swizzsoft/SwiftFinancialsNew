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

                MessageBox.Show(missingMessage,
                    "Cash Transaction",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );

                //return Json(new { success = false, message = "Operation error: " + missingMessage });
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
                    var proceedPrintReceipt = default(bool);

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
                        MessageBox.Show(errorMessages, "Error Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    }
                    else if (SelectedTeller.TellerTotalCheques != 0m)
                    {
                        IsBusy = false;

                        MessageBox.Show("Sorry, but you need to first transfer your cheques!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                        return View(cashTransferRequestDTO);
                    }
                    else if (await _channelService.EndOfDayExecutedAsync(SelectedEmployee, GetServiceHeader()))
                    {
                        IsBusy = false;

                        MessageBox.Show("Sorry, but you have already closed your day!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                        return View();
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

                            MessageBox.Show(string.Join(Environment.NewLine, NewFiscalCount.ErrorMessages), "Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                        }
                        else
                        {
                            
                            var proceedResult = MessageBox.Show("Do you want to proceed?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                            if (proceedResult == DialogResult.Yes && !proceedEndOfDayTransaction)
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



                                        await _channelService.AddJournalAsync(model, null, GetServiceHeader());
                                    }

                                    // Replace the _messageService.Show method
                                    MessageBox.Show("Operation completed successfully.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                    // Replace the second _messageService.ShowQuestion method
                                    var printConfirmationResult = MessageBox.Show("Do you want to print?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                    if (printConfirmationResult == DialogResult.Yes)
                                    {
                                        #region compose receipt data

                                        var nfi = new NumberFormatInfo();
                                        nfi.CurrencySymbol = string.Empty;

                                        var receiptDataSB = new StringBuilder();
                                        var topIndent = new StringBuilder();
                                        var leftIndent = new StringBuilder();
                                        var footer = string.Empty;

                                        if (SelectedBranch != null)
                                        {
                                            for (int i = 0; i < SelectedBranch.CompanyTransactionReceiptTopIndentation; i++)
                                                topIndent.Append("\n");

                                            for (int i = 0; i < SelectedBranch.CompanyTransactionReceiptLeftIndentation; i++)
                                                leftIndent.Append("\t");

                                            footer = SelectedBranch.CompanyTransactionReceiptFooter;
                                        }

                                        // Additional receipt data processing can go here...

                                        // Compose the receipt content
                                        receiptDataSB.Append(topIndent);
                                        receiptDataSB.Append(leftIndent);
                                        receiptDataSB.AppendLine("End of Day Transaction Receipt");
                                        receiptDataSB.Append(leftIndent);
                                        receiptDataSB.AppendLine("-------------------------");
                                        receiptDataSB.Append(leftIndent);
                                        receiptDataSB.AppendLine("Date: " + DateTime.Now.ToString("g", CultureInfo.InvariantCulture));
                                        receiptDataSB.Append(leftIndent);
                                        receiptDataSB.AppendLine("Amount: " + string.Format(nfi, "{0:C}", model.TotalValue)); // Example amount
                                        receiptDataSB.Append(leftIndent);
                                        receiptDataSB.AppendLine("-------------------------");
                                        receiptDataSB.Append(leftIndent);
                                        receiptDataSB.AppendLine(footer);

                                        receiptContent = receiptDataSB.ToString();

                                        #endregion

                                        // Print the receipt
                                        PrintReceipt(receiptContent);

                                        #endregion
                                    }
                                }
                                
                            }


                        }
                    }
                }

                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                }
            }
        
            else
            {
                var errorMessages = cashTransferRequestDTO.ErrorMessages;

                // Join the error messages into a single string if there are multiple messages
                var combinedErrorMessages = string.Join(Environment.NewLine, errorMessages);

                // Display the error messages in a MessageBox
                MessageBox.Show(combinedErrorMessages, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);


                return View(cashTransferRequestDTO);
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


        private void PrintReceipt(string receiptContent)
        {
            try
            {
                PrintDocument printDocument = new PrintDocument();
                printDocument.PrinterSettings = new PrinterSettings
                {
                    PrinterName = new PrinterSettings().PrinterName // Gets the default printer
                };
                printDocument.PrintPage += (sender, e) =>
                {
                    // Draw the receipt content onto the print page
                    e.Graphics.DrawString(receiptContent, new Font("Courier New", 10), Brushes.Black, new RectangleF(0, 0, e.PageBounds.Width, e.PageBounds.Height));
                };

                PrintDialog printDialog = new PrintDialog
                {
                    Document = printDocument
                };

                if (printDialog.ShowDialog() == DialogResult.OK)
                {
                    printDocument.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing receipt: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



    }

}