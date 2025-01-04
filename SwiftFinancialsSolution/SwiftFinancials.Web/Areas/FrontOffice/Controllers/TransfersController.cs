using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;


namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{
    public class TransfersController : MasterController
    {
        TellerDTO _selectedTeller;
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

        // GET: FrontOffice/Transfers
        public async Task<ActionResult> Index()
        {

            await ServeNavigationMenus();
            return View();
        }

        public async Task<ActionResult> Create(Guid? id)
        {
            
            await ServeNavigationMenus();
            ViewBag.CashTransaferTransactionTypeSelectList = GetCashTransferTransactionTypeSelectList(string.Empty);

            var model = new CashTransferRequestDTO();

            var currentUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            _selectedTeller = await GetCurrentTeller();
            
            var missingParameters = new List<string>();

            if (currentUser == null)
            {
                missingParameters.Add("Active User");
            }

            if (_selectedTeller == null)
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

            model.EmployeeId = _selectedTeller.EmployeeId;
            model.TotalCredits = _selectedTeller.TotalCredits;
            model.TotalDebits = _selectedTeller.TotalDebits;
            model.BookBalance = _selectedTeller.BookBalance;

            model.OpeningBalance = _selectedTeller.OpeningBalance;
            model.ClosingBalance = _selectedTeller.ClosingBalance;

            model.UntransferredChequesValue = untransferredChequesValue;
            return View(model);
        }


        [HttpPost]
        public async Task<ActionResult> Create(CashTransferRequestDTO cashTransferRequestDTO)
        {

            var currentUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            var selectedTeller = await GetCurrentTeller();

            var missingParameters = new List<string>();



            if (currentUser == null)
            {
                missingParameters.Add("Active User");
            }

            if (selectedTeller == null)
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

            cashTransferRequestDTO.EmployeeId = selectedTeller.EmployeeId;

            if (!cashTransferRequestDTO.HasErrors)
            {
                var successRequest = await _channelService.AddCashTransferRequestAsync(cashTransferRequestDTO, GetServiceHeader());

                //ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(externalChequeDTO.CustomerAccountCustomerType.ToString());
                //ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(string.Empty);

                if (successRequest != null)
                {

                    MessageBox.Show("Cash Transfer Request sent Successfully", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    //return View(cashTransferRequestDTO);
                    return Json(new { success = true, message = "Operation Success" });
                }
                else
                {


                    MessageBox.Show("Cah Transfer Request failed", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                    //return View(cashTransferRequestDTO);
                    return Json(new { success = true, message = "Operation Failed" });
                }
            }
            else
            {
                var errorMessages = cashTransferRequestDTO.ErrorMessages;

                return Json(new { success = false, message = errorMessages });
                //return View(cashTransferRequestDTO);
            }
        }


        [HttpPost]
        public async Task<JsonResult> FetchUnTransferredChequesTable(JQueryDataTablesModel jQueryDataTablesModel)
        {

            var currentUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            var selectedTeller = await GetCurrentTeller();

            var missingParameters = new List<string>();



            if (currentUser == null)
            {
                missingParameters.Add("Active User");
            }

            if (selectedTeller == null)
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

            int totalRecordCount = 0;

            int searchRecordCount = 0;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();


            var pageCollectionInfo = await _channelService.FindUnTransferredExternalChequesByTellerIdAndFilterInPageAsync(selectedTeller.Id, jQueryDataTablesModel.sSearch, 0, int.MaxValue, GetServiceHeader());

           
            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {


                var sortedData = pageCollectionInfo.PageCollection.OrderByDescending(gl => gl.CreatedDate).ToList();

                totalRecordCount = pageCollectionInfo.ItemsCount;

                //pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.JournalCreatedDate).ToList();


                var paginatedData = sortedData.Skip(jQueryDataTablesModel.iDisplayStart).Take(jQueryDataTablesModel.iDisplayLength).ToList();


                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: paginatedData, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }

        [HttpPost]
        public async Task<ActionResult> TransferSelectedChequesAsync (List<ExternalChequeDTO> cheques)
        {
            if (cheques == null || cheques.Count == 0)
            {
                return Json(new { success = false, message = "No cheques were selected for transfer." });
            }

            try
            {
                ObservableCollection<ExternalChequeDTO> selectedCheques = new ObservableCollection<ExternalChequeDTO>(cheques);

                var currentUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
                _selectedTeller = await GetCurrentTeller();


                var chequesInHandChartOfAccountId = await _channelService.GetChartOfAccountMappingForSystemGeneralLedgerAccountCodeAsync((int)SystemGeneralLedgerAccountCode.ExternalChequesInHand, GetServiceHeader());

                if (SelectedTeller != null && SelectedTeller.ChartOfAccountId.HasValue && chequesInHandChartOfAccountId != Guid.Empty) {

                    var transferred = await _channelService.TransferExternalChequesAsync(selectedCheques, SelectedTeller, 0, GetServiceHeader());

                    if (!transferred)
                    {
                        return Json(new { success = false, message = "Transfer failed. Please try again." });
                    }

                    var untransferredCheques = await _channelService.FindUnTransferredExternalChequesByTellerId(SelectedTeller.Id, "", GetServiceHeader());
                    var untransferredChequesValue = untransferredCheques.Sum(cheque => cheque.Amount);

                    //model.EmployeeId = SelectedTeller.EmployeeId;
                    //model.TotalCredits = SelectedTeller.TotalCredits;
                    //model.TotalDebits = SelectedTeller.TotalDebits;
                    //model.BookBalance = SelectedTeller.BookBalance;

                    //model.OpeningBalance = SelectedTeller.OpeningBalance;
                    //model.ClosingBalance = SelectedTeller.ClosingBalance;

                    //model.UntransferredChequesValue = untransferredChequesValue;
                    // Construct a JSON response directly
                    var response = new
                    {
                        success = true,
                        message = "Cheques transferred successfully.",
                        data = new
                        {
                            //EmployeeId = SelectedTeller.EmployeeId,
                            TotalCredits = SelectedTeller.TotalCredits,
                            TotalDebits = SelectedTeller.TotalDebits,
                            BookBalance = SelectedTeller.BookBalance,
                            OpeningBalance = SelectedTeller.OpeningBalance,
                            ClosingBalance = SelectedTeller.ClosingBalance,
                            UntransferredChequesValue = untransferredChequesValue
                        }
                    };

                    return Json(response);

                }

                else
                {

                    var message = "Sorry, but the requisite teller and / or external cheques in hand account has not been setup!";


                    MessageBox.Show(message,
                        "Cash Transaction",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification
                    );

                    return Json(new { success = false, message = "Operation error: " + message });
                }

            }
            catch (Exception ex)
            {
                // Log the error
                return Json(new { success = false, message = "An error occurred while transferring cheques: " + ex.Message });
            }
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



    }
}