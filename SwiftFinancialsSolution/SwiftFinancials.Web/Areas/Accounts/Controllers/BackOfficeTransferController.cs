using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;
using SwiftFinancials.Presentation.Infrastructure.Models;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SwiftFinancials.Web.Areas.Accounts.Controllers
{
    public class BackOfficeTransferController : MasterController
    {
        // GET: Accounts/BackOfficeTransfer
        public ActionResult Index()
        {
            return View();
        }


        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();
            ViewBag.SystemGeneralLedgerAccountCodeSelectList = GetSystemGeneralLedgerAccountCodeSelectList(string.Empty);
            ViewBag.GetApportionToSelectList = GetApportionToSelectList(string.Empty);
            ViewBag.CustomerFilterSelectList = GetCustomerFilterSelectList(string.Empty);
            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            return View();
        }

            [HttpPost]
        public async Task<JsonResult> EntryCreditCustomerAccountLookUp(Guid id)
        {

            if (id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }
            var creditcustomerAccount = await _channelService.FindCustomerAccountAsync(id, true, true, true, false, GetServiceHeader());

            if (creditcustomerAccount != null)
            {
        //            public enum ProductCode
        //{
        //    [Description("Savings")]
        //    Savings = 0x001,
        //    [Description("Loan")]
        //    Loan = 0x001 + 1,
        //    [Description("Investment")]
        //    Investment = 0x001 + 2,
        //}
                if ((ProductCode)creditcustomerAccount.CustomerAccountTypeProductCode != ProductCode.Loan)
                {
                    var apportionment = new ApportionmentWrapper
                    {   
                        CreditCustomerAccountId = creditcustomerAccount.Id,
                        CreditCustomerAccount = creditcustomerAccount,
                        CustomerFullName = creditcustomerAccount.CustomerFullName,
                        FullAccountNumber = creditcustomerAccount.FullAccountNumber,
                        CustomerReference1 = creditcustomerAccount.CustomerReference1,
                         
                        //DebitCustomerAccount = creditcustomerAccount,
                        //DebitCustomerAccountId = creditcustomerAccount.Id, 
                        ProductDescription = creditcustomerAccount.CustomerAccountTypeTargetProductDescription,

                        CarryForwardsBalance = creditcustomerAccount.CarryForwardsBalance, 
                    };

                    return Json(new { success = true, data = apportionment });
                }



            }

            return Json(new { success = false, message = "Customer account not found" });
        }

        [HttpPost]
        public async Task<JsonResult> CustomerAccountLookUp(Guid id)
        {

            if (id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }
            var debitcustomerAccount = await _channelService.FindCustomerAccountAsync(id, true, true, true, false, GetServiceHeader());

            if (debitcustomerAccount != null)
            {
                var transaction = new TransactionModel
                {
                    BranchId = debitcustomerAccount.BranchId,
                    DebitCustomerAccountId = debitcustomerAccount.Id,
                    DebitCustomerAccount = debitcustomerAccount,
                };

                return Json(new { success = true, data = transaction });
            }

            return Json(new { success = false, message = "Customer account not found" });
        }

        [HttpPost]
        public async Task<JsonResult> Add(TransactionModel transactionModel)
        {
            await ServeNavigationMenus();

            // Retrieve the batch entries from the session
            var apportionmentWrappers = Session["apportionmentWrappers"] as ObservableCollection<ApportionmentWrapper>;
            if (apportionmentWrappers == null)
            {
                apportionmentWrappers = new ObservableCollection<ApportionmentWrapper>();
            }

            foreach (var apportionment in transactionModel.Apportionments)
            {

                // Check if an entry with the same Debit and Credit account numbers already exists

                ApportionmentWrapper existingApportionment = null;

                if (apportionment.ApportionTo == 1)
                {

                    existingApportionment = apportionmentWrappers
                 .FirstOrDefault(e =>
                     e.CreditCustomerAccountId == apportionment.CreditCustomerAccountId);
                }

                else
                {

                    existingApportionment = apportionmentWrappers.FirstOrDefault(e => e.ApportionTo == 2 && e.CreditChartOfAccountId != null && apportionment.CreditChartOfAccountId != null && e.CreditChartOfAccountId == apportionment.CreditChartOfAccountId);
                }

                if (existingApportionment != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "A general Ledger with the same Debit and Credit transaction already exists."
                    });
                }
                apportionmentWrappers.Add(apportionment);

                // Calculate the sum of Principal and Interest
                decimal sumAmount = apportionmentWrappers.Sum(e => e.Principal + e.Interest);

                // Check if the total value is exceeded
                if (sumAmount > transactionModel.TotalValue)
                {
                    apportionmentWrappers.Remove(apportionment);
                    decimal exceededAmount = sumAmount - transactionModel.TotalValue;
                    return Json(new
                    {
                        success = false,
                        message = $"The total of principal and interest has exceeded the Total Value by {exceededAmount}."
                    });
                }

                  //else
                  //{
                  //  transactionModel.TotalValue = sumAmount;
                  //}
            }

            var TotalValue = transactionModel.TotalValue;

            // Save the updated entries back to the session
            Session["apportionmentWrappers"] = apportionmentWrappers;
            Session["transactionModel"] = transactionModel;

            return Json(new { success = true, data = apportionmentWrappers, TotalValue });
        }


        [HttpPost]
        public async Task<JsonResult> Remove(Guid id)
        {
            await ServeNavigationMenus();

            var apportionmentWrappers = Session["apportionmentWrappers"] as ObservableCollection<ApportionmentWrapper>;

            decimal sumAmount = apportionmentWrappers?.Sum(e => e.Principal + e.Interest) ?? 0;

            if (apportionmentWrappers != null)
            {
                var entryToRemove = apportionmentWrappers.FirstOrDefault(e => e.Id == id);
                if (entryToRemove != null)
                {
                    apportionmentWrappers.Remove(entryToRemove);

                    sumAmount = apportionmentWrappers.Sum(e => e.Principal + e.Interest);

                    Session["apportionmentWrappers"] = apportionmentWrappers;
                }
            }

            return Json(new { success = true, data = apportionmentWrappers, totalSum = sumAmount });
        }





        [HttpPost]
        public async Task<ActionResult> Create(TransactionModel transactionModel)
        {
            // Retrieve the DTO stored in session
            transactionModel = Session["transactionModel"] as TransactionModel;

            if (transactionModel == null)
            {
                return Json(new
                {
                    success = false,
                    message = "transaction cannot be null."
                });
            }

            // If there are no entries, initialize a new list
            var apportionmentWrappers = Session["apportionmentWrappers"] as ObservableCollection<ApportionmentWrapper>;

            
            //foreach (ApportionmentWrapper apportionmentWraper in apportionmentWrappers)
            //{

            //    if (apportionmentWraper.CreditChartOfAccountId == appo)
            //}

            if (apportionmentWrappers != null)
            {
                transactionModel.Apportionments = new List<ApportionmentWrapper>(apportionmentWrappers);
            }

            decimal SumAmount = apportionmentWrappers.Sum(e => e.Principal + e.Interest);
            decimal totalValue = transactionModel?.TotalValue ?? 0;


            var currentPostingPeriod = await _channelService.FindCurrentPostingPeriodAsync(GetServiceHeader());

           // var currentUser = await _applicationUserManager.FindByEmailAsync("calvince.ochieng@swizzsoft.com");
            var currentUser = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            transactionModel.BranchId =(Guid)currentUser.BranchId;

            // transactionModel.BranchId = (Guid)currentUser.BranchId;

            var selectedBranch = await _channelService.FindBranchAsync(transactionModel.BranchId, GetServiceHeader());

            transactionModel.PostingPeriodId = currentPostingPeriod.Id;

            transactionModel.Reference = "ok";
            transactionModel.PrimaryDescription = "ok";
            transactionModel.SecondaryDescription = string.Format("B{0}/T{1}/#{2}", selectedBranch.Code, currentUser.Email, currentUser.EmployeeId);

            var currentEmployee = await _channelService.FindTellerByEmployeeIdAsync((Guid)currentUser.EmployeeId, true, GetServiceHeader());

           
            transactionModel.DebitChartOfAccountId = (Guid)currentEmployee.ChartOfAccountId;
            transactionModel.CreditChartOfAccountId = (Guid)currentEmployee.ChartOfAccountId;

            transactionModel.ValidateAll();
            if (transactionModel.ErrorMessages.Count != 0)
            {
                return Json(new
                {
                    success = false,
                    message = transactionModel.ErrorMessages
                });
            }


            var dynamicCharges = await _channelService.FindDynamicChargesAsync(GetServiceHeader());
            ObservableCollection<ApportionmentWrapper> apportionmentsObservableCollection = new ObservableCollection<ApportionmentWrapper>(transactionModel.Apportionments);


 
            var journal = _channelService.AddJournalWithApportionmentsAsync(transactionModel, apportionmentsObservableCollection, null, dynamicCharges, GetServiceHeader());
            if (transactionModel.HasErrors)
            {
                return Json(new
                {
                    success = false,
                    message = transactionModel.ErrorMessages
                });
            }

            // Clear session data after successful creation
            Session["apportionmentWrappers"] = null;
            Session["transactionModel"] = null;

            // Return success message in JSON
            return Json(new
            {
                success = true,
                message = "Transfer Success."
            });
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
    }
}