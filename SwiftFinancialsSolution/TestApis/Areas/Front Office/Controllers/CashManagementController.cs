using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Application.MainBoundedContext.DTO.HumanResourcesModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Presentation.Infrastructure.Models;
//using SwiftFinancials.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;


namespace TestApis.Controllers
{
    [RoutePrefix("api/cashmanagement")]
    public class CashManagementController : MasterController
    {
        [HttpPost]

        [Route("")]
        public async Task<IHttpActionResult> Create(FiscalCountDTO fiscalCountDTO)
        {
            bool IncludeBalance = false;
            fiscalCountDTO.ValidateAll();

            if (!fiscalCountDTO.HasErrors)
            {
                int treasuryTransactionType = fiscalCountDTO.TransactionType;
                TransactionModel transactionModel = new TransactionModel();


                var CurrentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
                //var userId = User.Identity.GetUserId();
               
                var ActiveTreasury = await _channelService.FindTreasuryByBranchIdAsync(fiscalCountDTO.BranchId, true, GetServiceHeader());

            

                var missingParameters = new List<string>();

                if (CurrentPostingPeriod == null)
                {
                    missingParameters.Add("Posting Period");
                }
                else
                {
                    fiscalCountDTO.PostingPeriodId = CurrentPostingPeriod.Id;
                }

                //if (activeUser == null)
                //{
                //    missingParameters.Add("Active User");
                //}

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
           
                    return BadRequest(missingMessage);
                }


                transactionModel.TotalValue = fiscalCountDTO.TotalValue;
                transactionModel.PostingPeriodId = CurrentPostingPeriod.Id;
                transactionModel.PrimaryDescription = fiscalCountDTO.TransactionTypeDescription;
                transactionModel.ValueDate = DateTime.Today;

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

                            transactionModel.CreditChartOfAccountId = matchingBankLinkage.ChartOfAccountId;
                            transactionModel.DebitChartOfAccountId = ActiveTreasury.ChartOfAccountId;
                            transactionModel.TransactionCode = (int)SystemTransactionCode.BankToTreasury;
                            break;

                        case TreasuryTransactionType.TreasuryToTeller:
                            transactionModel.CreditChartOfAccountId = ActiveTreasury.ChartOfAccountId;
                            var teller = await _channelService.FindTellerAsync(fiscalCountDTO.TellerId, IncludeBalance, GetServiceHeader());
                            if (teller == null)
                            {

                                return Json(new { success = false, message = "Operation Failed: Teller Not Found" });
                            }
                            transactionModel.DebitChartOfAccountId = (Guid)teller.ChartOfAccountId;
                            transactionModel.TransactionCode = (int)SystemTransactionCode.TreasuryToTeller;
                            break;

                        case TreasuryTransactionType.TreasuryToBank:
                            transactionModel.CreditChartOfAccountId = ActiveTreasury.ChartOfAccountId;
                            var receivingBank = await _channelService.FindBankAsync(fiscalCountDTO.Id, GetServiceHeader());
                            if (receivingBank == null)
                            {

                                return Json(new { success = false, message = "Operation Failed: Receiving bank not found" });
                            }
                            var linkages = await _channelService.FindBankLinkagesAsync(GetServiceHeader());
                            var linkage = linkages.FirstOrDefault(l => l.BankName == receivingBank.Description);
                            if (linkage == null)
                            {

                                return Json(new { success = false, message = "Operation Failed: No matching bank linkage found for selected bank account." });

                            }
                            transactionModel.DebitChartOfAccountId = linkage.ChartOfAccountId;
                            transactionModel.TransactionCode = (int)SystemTransactionCode.TreasuryToBank;

                            break;

                        case TreasuryTransactionType.TreasuryToTreasury:
                            transactionModel.CreditChartOfAccountId = ActiveTreasury.ChartOfAccountId;
                            var treasury = await _channelService.FindTreasuryAsync(fiscalCountDTO.Id, IncludeBalance, GetServiceHeader());
                            if (treasury == null)
                            {

                                return Json(new { success = false, message = "Operation Failed: Receiving treasury not found" });
                            }
                            transactionModel.DebitChartOfAccountId = treasury.ChartOfAccountId;
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