using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.FrontOfficeModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using SwiftFinancials.Presentation.Infrastructure.Models;
using Infrastructure.Crosscutting.Framework.Utils;
using System.Net;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Web.Areas.Registry.DocumentsModel;
using System.Data.SqlClient;
using System.Configuration;

namespace SwiftFinancials.Web.Areas.FrontOffice.Controllers
{


    public class CustomerReceiptsController : MasterController
    {
        private readonly string _connectionString;

        public CustomerReceiptsController()
        {
            // Get connection string from Web.config
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }
        private async Task<List<Document>> GetDocumentsAsync(Guid id)
        {
            var documents = new List<Document>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT PassportPhoto, SignaturePhoto, IDCardFrontPhoto, IDCardBackPhoto FROM swiftFin_SpecimenCapture WHERE CustomerId = @CustomerId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerId", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            documents.Add(new Document
                            {
                                PassportPhoto = reader.IsDBNull(0) ? null : (byte[])reader[0],
                                SignaturePhoto = reader.IsDBNull(1) ? null : (byte[])reader[1],
                                IDCardFrontPhoto = reader.IsDBNull(2) ? null : (byte[])reader[2],
                                IDCardBackPhoto = reader.IsDBNull(3) ? null : (byte[])reader[3]
                            });
                        }
                    }
                }
            }

            return documents;
        }


        // GET: FrontOffice/CustomerReceipts
        public async Task<ActionResult> Index()
        {

            await ServeNavigationMenus();
            return View();

        }


        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, DateTime startDate, DateTime endDate)
        {
            var currentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());
            var selectedTeller = await GetCurrentTeller();

            int totalRecordCount = 0;

            int searchRecordCount = 0;

            //DateTime startDate = DateTime.Now;

            //DateTime endDate = DateTime.Now;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;


            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();



            if (selectedTeller != null && !selectedTeller.IsLocked)
            {
                var pageCollectionInfo = await _channelService.FindGeneralLedgerTransactionsByChartOfAccountIdAndDateRangeAndFilterInPageAsync(
                   0, int.MaxValue,
                    (Guid)selectedTeller.ChartOfAccountId,
                    startDate,
                    endDate,
                    jQueryDataTablesModel.sSearch,
                    20,
                    1,
                    true,
                    GetServiceHeader()
                );


                if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
                {


                    var sortedData = pageCollectionInfo.PageCollection.OrderByDescending(gl => gl.JournalCreatedDate).ToList();

                    totalRecordCount = pageCollectionInfo.ItemsCount;

                    //pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.JournalCreatedDate).ToList();


                    var paginatedData = sortedData.Skip(jQueryDataTablesModel.iDisplayStart).Take(jQueryDataTablesModel.iDisplayLength).ToList();

                    searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? sortedData.Count : totalRecordCount;

                    return this.DataTablesJson(items: paginatedData, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
                }
                else return this.DataTablesJson(items: new List<GeneralLedgerTransaction> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);

            }

            return this.DataTablesJson(items: new List<GeneralLedgerTransaction> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);

        }



        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();
            Guid parseId;

            ViewBag.ApportionToSelectList = GetApportionToSelectList(string.Empty);
            ViewBag.CustomerFilterSelectList = GetCustomerFilterSelectList(string.Empty);
            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatusSelectList = GetRecordStatusSelectList(string.Empty);
            ViewBag.CustomerFilterSelectList = GetCustomerFilterSelectList(string.Empty);
            ViewBag.ApportionToSelectList = GetApportionToSelectList(string.Empty);

            TransactionModel model = new TransactionModel();

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
            

                if (id != null)
                {
                    ViewBag.Documents = GetDocumentsAsync(id.Value);
                
                }
                return View(model);
            }

            //customer account uncleared cheques
            var unclearedChequescollection = await _channelService.FindUnClearedExternalChequesByCustomerAccountIdAsync(model.DebitCustomerAccount.Id, GetServiceHeader());
            var _unclearedCheques = unclearedChequescollection.ToList();
            model.CustomerAccountUnclearedCheques = _unclearedCheques;

            //customer account signatories
            var signatoriesCollection = await _channelService.FindCustomerAccountSignatoriesByCustomerAccountIdAsync(model.DebitCustomerAccount.Id, GetServiceHeader());
            var _signatories = signatoriesCollection.ToList();
            model.CustomerAccountSignatories = _signatories;


            //customer acount ministatement
            var miniStatementOrdersCollection = await _channelService.FindElectronicStatementOrdersByCustomerAccountIdAsync(model.DebitCustomerAccount.Id, true, GetServiceHeader());
            var _miniStatement = miniStatementOrdersCollection.ToList();
            model.CustomerAccountMiniStatement = _miniStatement;

           

            return View(model);
        }


        public async Task<JsonResult> GetCustomerDetailsJson(Guid? id)
        {
            Guid parseId;

            if (!Guid.TryParse(id.ToString(), out parseId))
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            var customer = await _channelService.FindCustomerAsync(parseId, GetServiceHeader());
            var documents = await GetDocumentsAsync(customer.Id);
            if (documents.Any())
            {
                var document = documents.First();

                TempData["PassportPhoto"] = document.PassportPhoto;
                TempData["SignaturePhoto"] = document.SignaturePhoto;
                TempData["idCardFront"] = document.IDCardFrontPhoto;
                TempData["idCardBack"] = document.IDCardBackPhoto;

                // Sending the images as Base64 encoded strings to be used in AJAX
                ViewBag.PassportPhoto = document.PassportPhoto != null ? Convert.ToBase64String(document.PassportPhoto) : null;
                ViewBag.SignaturePhoto = document.SignaturePhoto != null ? Convert.ToBase64String(document.SignaturePhoto) : null;
                ViewBag.IDCardFrontPhoto = document.IDCardFrontPhoto != null ? Convert.ToBase64String(document.IDCardFrontPhoto) : null;
                ViewBag.IDCardBackPhoto = document.IDCardBackPhoto != null ? Convert.ToBase64String(document.IDCardBackPhoto) : null;
            }
            if (customer == null)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            return Json(customer, JsonRequestBehavior.AllowGet);
        }


        public async Task<JsonResult> GetCustomerAccountDetailsJson(Guid? id)
        {
            Guid parseId;

            if (!Guid.TryParse(id.ToString(), out parseId))
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            bool includeBalances = true;
            bool includeProductDescription = true;
            bool includeInterestBalanceForLoanAccounts = true;
            bool considerMaturityPeriodForInvestmentAccounts = true;

            var customerAccount = await _channelService.FindCustomerAccountAsync(parseId, includeBalances, includeProductDescription, includeInterestBalanceForLoanAccounts, considerMaturityPeriodForInvestmentAccounts, GetServiceHeader());
            var documents = await GetDocumentsAsync(customerAccount.CustomerId);
            if (documents.Any())
            {
                var document = documents.First();

                TempData["PassportPhoto"] = document.PassportPhoto;
                TempData["SignaturePhoto"] = document.SignaturePhoto;
                TempData["idCardFront"] = document.IDCardFrontPhoto;
                TempData["idCardBack"] = document.IDCardBackPhoto;

                // Sending the images as Base64 encoded strings to be used in AJAX
                ViewBag.PassportPhoto = document.PassportPhoto != null ? Convert.ToBase64String(document.PassportPhoto) : null;
                ViewBag.SignaturePhoto = document.SignaturePhoto != null ? Convert.ToBase64String(document.SignaturePhoto) : null;
                ViewBag.IDCardFrontPhoto = document.IDCardFrontPhoto != null ? Convert.ToBase64String(document.IDCardFrontPhoto) : null;
                ViewBag.IDCardBackPhoto = document.IDCardBackPhoto != null ? Convert.ToBase64String(document.IDCardBackPhoto) : null;
            }
            if (customerAccount == null)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            return Json(customerAccount, JsonRequestBehavior.AllowGet);
        }



        [HttpPost]
        public async Task<ActionResult> Create(TransactionModel model)
        {

            var currentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());

            var currentUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            //var currentTeller = await _channelService.FindTellerByEmployeeIdAsync((Guid)currentUser.EmployeeId, true, GetServiceHeader());

            var currentTeller = await GetCurrentTeller(); 
            
            model.BranchId = currentTeller.EmployeeBranchId;

            var selectedBranch = await _channelService.FindBranchAsync(model.BranchId, GetServiceHeader());

            model.PostingPeriodId = currentPostingPeriod.Id;

 
            model.DebitChartOfAccountId = (Guid)currentTeller.ChartOfAccountId;
            model.CreditChartOfAccountId = (Guid)currentTeller.ChartOfAccountId;

            model.PrimaryDescription = "ok";
            model.SecondaryDescription = string.Format("B{0}/T{1}/#{2}", selectedBranch.Code, currentTeller.Code, currentTeller.ItemsCount);

        
            model.ValidateAll();
    
             if (!model.HasErrors)
             {
              //# TODO refer tariffs with amos
                //var tariffs = await _channelService.ComputeTellerCashTariffsAsync(model.DebitCustomerAccount, model.TotalValue, 2, GetServiceHeader());
                var dynamicCharges = await _channelService.FindDynamicChargesAsync(GetServiceHeader());
                ObservableCollection<ApportionmentWrapper> apportionmentsObservableCollection = new ObservableCollection<ApportionmentWrapper>(model.Apportionments);

                var journal = _channelService.AddJournalWithApportionmentsAsync(model, apportionmentsObservableCollection, null, dynamicCharges, GetServiceHeader());


                if (journal != null && journal.Exception == null)
                {

                    //MessageBox.Show(
                    //                                   "Operation Success",
                    //                                   "Customer Receipts",
                    //                                   MessageBoxButtons.OK,
                    //                                   MessageBoxIcon.Information,
                    //                                   MessageBoxDefaultButton.Button1,
                    //                                   MessageBoxOptions.ServiceNotification
                    //                               );

                    return Json(new { success = true, message = "Operation Success" });
                }

                else
                {
                    string message = string.Join(Environment.NewLine, model.ErrorMessages);

                    //MessageBox.Show(
                    //                                   message,
                    //                                   "Custmer Receipts",
                    //                                   MessageBoxButtons.OK,
                    //                                   MessageBoxIcon.Information,
                    //                                   MessageBoxDefaultButton.Button1,
                    //                                   MessageBoxOptions.ServiceNotification
                    //                               );

                    return Json(new { success = false, message = "Operation Failed" });
                }


            }
            else
            {
                var errorMessages = model.ErrorMessages;
    
           

                return Json(new { success = false, errorMessages = errorMessages });
            }
        }

        [HttpPost]
        public async Task<JsonResult> FetchCustomerAccountsTable(JQueryDataTablesModel jQueryDataTablesModel, int productCode, int customerFilter)
        {

            int totalRecordCount = 0;

            int searchRecordCount = 0;

            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();


            var pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeAndFilterInPageAsync(productCode, jQueryDataTablesModel.sSearch, customerFilter, pageIndex, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());
            //var pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeFilterInPageAsync(productCode, recordStatus, jQueryDataTablesModel.sSearch, 2, pageIndex, jQueryDataTablesModel.iDisplayLength, false, false, false, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                pageCollectionInfo.PageCollection = pageCollectionInfo.PageCollection.OrderByDescending(l => l.CreatedDate).ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerAccountDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
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