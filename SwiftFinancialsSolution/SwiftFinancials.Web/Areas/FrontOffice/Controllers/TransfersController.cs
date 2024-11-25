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

            _selectedTeller = await _channelService.FindTellerByEmployeeIdAsync((Guid)currentUser.EmployeeId, true, GetServiceHeader());

            var missingParameters = new List<string>();

        

            if (currentUser == null)
            {
                missingParameters.Add("Active User");
            }

            if (SelectedTeller == null)
            {
                missingParameters.Add("Treasury");
            }

            // Check if any parameter is missing
            if (missingParameters.Any())
            {
                var missingMessage = $"Some features are missing due to lack of: {string.Join(", ", missingParameters)}";

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

            _selectedTeller = await _channelService.FindTellerByEmployeeIdAsync((Guid)currentUser.EmployeeId, true, GetServiceHeader());

            cashTransferRequestDTO.EmployeeId = SelectedTeller.EmployeeId;

            if (!cashTransferRequestDTO.HasErrors)
            {

                var successRequest = await _channelService.AddCashTransferRequestAsync(cashTransferRequestDTO, GetServiceHeader());

                //ViewBag.CustomerTypeSelectList = GetCustomerTypeSelectList(externalChequeDTO.CustomerAccountCustomerType.ToString());
                //ViewBag.TreasuryTransactionTypeSelectList = GetTreasuryTransactionTypeSelectList(string.Empty);

                if (successRequest != null)
                {

                    MessageBox.Show("Transfer Success", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return View(cashTransferRequestDTO);
                }
                else
                {


                    MessageBox.Show("Transfer failed", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                    return View(cashTransferRequestDTO);
                }
            }
            else
            {
                var errorMessages = cashTransferRequestDTO.ErrorMessages;

                return View(cashTransferRequestDTO);
            }
        }


        [HttpPost]
        public async Task<JsonResult> FetchUnTransferredChequesTable(JQueryDataTablesModel jQueryDataTablesModel)
        {

            //var currentUser = await _applicationUserManager.FindByEmailAsync("calvince.ochieng@swizzsoft.com");
            var currentUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());

            _selectedTeller = await _channelService.FindTellerByEmployeeIdAsync((Guid)currentUser.EmployeeId, true, GetServiceHeader());


            int totalRecordCount = 0;

            int searchRecordCount = 0;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();


            var pageCollectionInfo = await _channelService.FindUnTransferredExternalChequesByTellerIdAndFilterInPageAsync(SelectedTeller.Id, jQueryDataTablesModel.sSearch, pageIndex, jQueryDataTablesModel.iDisplayLength, GetServiceHeader());

           
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
        public async Task<ActionResult> TransferSelectedChequesAsync (List<ExternalChequeDTO> cheques)
        {
            if (cheques == null || cheques.Count == 0)
            {
                return Json(new { success = false, message = "No cheques were selected for transfer." });
            }

            try
            {
                ObservableCollection<ExternalChequeDTO> selectedCheques = new ObservableCollection<ExternalChequeDTO>(cheques);

                var currentUser = await _applicationUserManager.FindByEmailAsync("calvince.ochieng@swizzsoft.com");
                _selectedTeller = await _channelService.FindTellerByEmployeeIdAsync((Guid)currentUser.EmployeeId, true, GetServiceHeader());


                var transferred = await _channelService.TransferExternalChequesAsync(selectedCheques, SelectedTeller, 0, GetServiceHeader());

                if (!transferred)
                {
                    return Json(new { success = false, message = "Transfer failed. Please try again." });
                }

                return Json(new { success = true, message = "Cheques transferred successfully." });
            }
            catch (Exception ex)
            {
                // Log the error
                return Json(new { success = false, message = "An error occurred while transferring cheques: " + ex.Message });
            }
        }



    }
}