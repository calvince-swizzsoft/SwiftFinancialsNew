using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.AdministrationModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using DistributedServices.MainBoundedContext.Identity;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.TextAlertDispatcher.Celcom.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using TestApis.Controllers;

namespace TestApis.Controllers
{
    [RoutePrefix("api/endofday")]
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

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Create([FromBody] CashTransferRequestDTO cashTransferRequestDTO)
        {

            if (cashTransferRequestDTO.HasErrors)
                return BadRequest("Some validations failed - make sure all fields are included");

            //_selectedTeller = await GetCurrentTeller();
            _selectedTeller = await _channelService.FindTellerAsync(cashTransferRequestDTO.TellerId, true, GetServiceHeader());
            cashTransferRequestDTO.EmployeeId = SelectedTeller.EmployeeId;
            _selectedTeller.TellerTotalCheques = cashTransferRequestDTO.UntransferredChequesValue;
            _selectedEmployee = await _channelService.FindEmployeeAsync((Guid)SelectedTeller.EmployeeId, GetServiceHeader());
            _selectedBranch = await _channelService.FindBranchAsync(SelectedEmployee.BranchId, GetServiceHeader());
            _selectedPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
            _selectedTreasury = await _channelService.FindTreasuryByBranchIdAsync(SelectedBranch.Id, true, GetServiceHeader());


            try
            {

                var model = new TransactionModel();

                IsBusy = true;

                var proceedEndOfDayTransaction = default(bool);

                model.TransactionCode = (int)SystemTransactionCode.TellerEndOfDay;

                model.PrimaryDescription = SelectedTeller.Description;

                if ((TellerCashBalanceStatus)cashTransferRequestDTO.TellerCashBalanceStatusValue == TellerCashBalanceStatus.Balanced)
                    model.Reference = TellerCashBalanceStatus.Balanced.ToString();
                else if ((TellerCashBalanceStatus)cashTransferRequestDTO.TellerCashBalanceStatusValue == TellerCashBalanceStatus.Balanced)
                    model.Reference = TellerCashBalanceStatus.Shortage.ToString();
                else if ((TellerCashBalanceStatus)cashTransferRequestDTO.TellerCashBalanceStatusValue == TellerCashBalanceStatus.Excess)
                    model.Reference = TellerCashBalanceStatus.Excess.ToString();


                if (SelectedTeller != null && !SelectedTeller.IsLocked)
                    model.SecondaryDescription = SelectedTeller.Description;

                if (SelectedPostingPeriod != null)
                    model.PostingPeriodId = SelectedPostingPeriod.Id;

                if (SelectedBranch != null)
                    model.BranchId = SelectedBranch.Id;

                if (SelectedTreasury != null)
                {
                    model.DebitChartOfAccountId = SelectedTreasury.ChartOfAccountId;
                    model.ChartOfAccountId = SelectedTreasury.ChartOfAccountId;
                }

                if (SelectedTeller != null && !SelectedTeller.IsLocked)
                {
                    model.CreditChartOfAccountId = SelectedTeller.ChartOfAccountId ?? Guid.Empty;
                    model.ContraChartOfAccountId = SelectedTeller.ChartOfAccountId ?? Guid.Empty;
                }

                model.TotalValue = cashTransferRequestDTO.ClosingBalance;
                model.ValidateAll();

                if (model.HasErrors)
                {
                    IsBusy = false;

                    string errorMessages = string.Join(Environment.NewLine, model.ErrorMessages);

                    return Json(new { success = false, message = "Operation error: " + errorMessages });
                }
                else if (SelectedTeller.TellerTotalCheques != 0m)
                {
                    IsBusy = false;

                    return Json(new { success = false, message = "Operation error: " + "Sorry, but you need to first transfer your cheques!" });
                }
                else if (await _channelService.EndOfDayExecutedAsync(SelectedEmployee, GetServiceHeader()))
                {
                    IsBusy = false;
                    return Json(new { success = false, message = "Operation error: " + "Sorry, but you have already closed your day!" });


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

                    NewFiscalCount.TotalValue = model.TotalValue;


                    //NewFiscalCount.DestinationBranchId = Guid.NewGuid(); /*for passing validation*/

                    NewFiscalCount.DestinationBranchId = SelectedTreasury.BranchId;
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
                                    model.TotalValue = cashTransferRequestDTO.BookBalance - cashTransferRequestDTO.ClosingBalance;
                                    model.CreditChartOfAccountId = SelectedTeller.ChartOfAccountId ?? Guid.Empty;
                                    model.DebitChartOfAccountId = SelectedTeller.ShortageChartOfAccountId ?? Guid.Empty;

                                    postExcessOrShortage = true;

                                    break;
                                case TellerCashBalanceStatus.Excess:
                                    model.TotalValue = cashTransferRequestDTO.ClosingBalance - cashTransferRequestDTO.BookBalance;
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

                            else
                            {
                                return Json(new { success = false, message = "postExcessOrShortage boolean was false." });
                            }
                        }

                        else
                        {
                            return Json(new { success = false, message = "Failed to add a cash management journal. " });
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                return Json(new { success = false, message = "Operation error: " + ex.Message });
            }
        }

           

        private async Task<TellerDTO> GetCurrentTeller()
        {


            bool includeBalance = true;
            // Get the current user
            //var user = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            var teller = await _channelService.FindTellerByEmployeeIdAsync(Guid.Parse("50BDE4A6-1F50-F111-9B87-C8E2651EF92A"), includeBalance, GetServiceHeader());

            return teller;

        }


        [HttpPost]
        public IHttpActionResult PrintReceipt(JournalDTO journal)
        {
            try
            {
                var printerName = ConfigurationManager.AppSettings["ReceiptPrinterName"];

                if (string.IsNullOrWhiteSpace(printerName))
                    return BadRequest("Printer name is not configured.");

                var receiptContent = BuildReceiptContent(journal);

                using (var printDocument = new PrintDocument())
                {
                    printDocument.PrinterSettings = new PrinterSettings
                    {
                        PrinterName = printerName
                    };

                    printDocument.PrintPage += (sender, e) =>
                    {
                        e.Graphics.DrawString(
                            receiptContent,
                            new Font("Courier New", 10),
                            Brushes.Black,
                            new RectangleF(0, 0, e.PageBounds.Width, e.PageBounds.Height)
                        );
                    };

                    printDocument.Print();
                }

                return Ok(new { success = true, message = "Receipt printed successfully." });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
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