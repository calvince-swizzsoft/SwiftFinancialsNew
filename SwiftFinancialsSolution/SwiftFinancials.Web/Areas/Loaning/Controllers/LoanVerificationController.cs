using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Windows.Forms;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using Infrastructure.Crosscutting.Framework.Utils;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

namespace SwiftFinancials.Web.Areas.Loaning
{
    public class LoanVerificationController : MasterController
    {
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync((int)LoanCaseStatus.Approved, jQueryDataTablesModel.sSearch, (int)LoanCaseFilter.CaseNumber, (jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength), jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<LoanCaseDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id, JQueryDataTablesModel jQueryDataTablesModel)
        {
            await ServeNavigationMenus();

            int status = 1, loanCaseFilter = 0;

            string text = "";

            var loanCaseDTO = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync(status, text, loanCaseFilter, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader());

            return View(loanCaseDTO);
        }


        public async Task<ActionResult> Verify(Guid id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();
            ViewBag.LoanAuditOptionSelectList = GetLoanAuditOptionSelectList(string.Empty);

            var loaneeCustomer = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());
            if (loaneeCustomer != null)
            {
                loanCaseDTO = loaneeCustomer as LoanCaseDTO;

                //// Standing Orders
                ObservableCollection<Guid> customerAccountId = new ObservableCollection<Guid>();
                var customerAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(loaneeCustomer.CustomerId, true, true, true, true, GetServiceHeader());

                foreach (var accounts in customerAccounts)
                {
                    customerAccountId.Add(accounts.Id);
                }

                List<StandingOrderDTO> allStandingOrders = new List<StandingOrderDTO>();

                // Iterate through each account ID and collect standing orders
                foreach (var Ids in customerAccountId)
                {
                    var standingOrders = await _channelService.FindStandingOrdersByBeneficiaryCustomerAccountIdAsync(Ids, true, GetServiceHeader());
                    if (standingOrders != null && standingOrders.Any())
                    {
                        allStandingOrders.AddRange(standingOrders); // Add standing orders to the collection
                    }
                    else
                    {
                        TempData["EmptystandingOrders"] = "Selected Customer has no Standing Orders.";
                    }
                }
                ViewBag.StandingOrders = allStandingOrders;


                //// Income History
                //// Payouts
                var payouts = await _channelService.FindLoanDisbursementBatchEntriesByCustomerIdAsync((int)BatchStatus.Posted, loaneeCustomer.CustomerId, GetServiceHeader());
                if (payouts != null)
                {
                    ViewBag.Payouts = payouts;
                }


                ////Salary
                // No method fetching by customerId



                //// Loan Applications
                var loanApplications = await _channelService.FindLoanCasesByCustomerIdInProcessAsync(loaneeCustomer.CustomerId, GetServiceHeader());
                if (loanApplications != null)
                {
                    ViewBag.LoanApplications = loanApplications;
                }

                //// Collaterals...
                // No method fetching by customerId



                // Guarantors
                var loanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(id, GetServiceHeader());
                if (loanGuarantors != null)
                {
                    ViewBag.LoanGuarantors = loanGuarantors;
                }


                // Loan Accounts
                var findloanAccounts = await _channelService.FindCustomerAccountsByCustomerIdAsync(loaneeCustomer.CustomerId, true, true, true, true, GetServiceHeader());
                var LoanAccounts = findloanAccounts.Where(L => L.CustomerAccountTypeProductCode == (int)ProductCode.Loan);
                if (LoanAccounts != null)
                {
                    ViewBag.CustomerAccounts = LoanAccounts;
                }



                // Repayment Schedule .....................................
                ViewBag.APR = loaneeCustomer.LoanInterestAnnualPercentageRate;
                ViewBag.InterestCalculationMode = loaneeCustomer.LoanInterestCalculationModeDescription;
                ViewBag.AmountApplied = loaneeCustomer.AmountApplied;
                ViewBag.TermInMonths = loaneeCustomer.LoanRegistrationTermInMonths;
            }

            return View(loanCaseDTO);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Verify(LoanCaseDTO loanCaseDTO)
        {
            var findLoanCaseDetails = await _channelService.FindLoanCaseAsync(loanCaseDTO.Id, GetServiceHeader());

            loanCaseDTO.LoanProductId = findLoanCaseDetails.LoanProductId;
            loanCaseDTO.LoanPurposeId = findLoanCaseDetails.LoanPurposeId;
            loanCaseDTO.SavingsProductId = findLoanCaseDetails.SavingsProductId;
            loanCaseDTO.AuditedDate = DateTime.Now;

            loanCaseDTO.ValidateAll();

            if (!loanCaseDTO.HasErrors)
            {

                string message = string.Format(
                                  "Do you want to proceed with loan verification for: \n{0}?",
                                   findLoanCaseDetails.CustomerIndividualSalutationDescription.ToUpper() + " " + findLoanCaseDetails.CustomerIndividualFirstName.ToUpper() + " " + findLoanCaseDetails.CustomerIndividualLastName.ToUpper() +
                                  "\nAmount Applied: Kshs. " + loanCaseDTO.AmountApplied + "\nAmount Appraised: Kshs." + loanCaseDTO.AppraisedAmount + "\nVerification Remarks: " + loanCaseDTO.AuditRemarks.ToUpper() +
                                  "\nLoan Product: " + loanCaseDTO.LoanProductDescription.ToUpper() + "\nLoan Purpose: " + loanCaseDTO.LoanPurposeDescription.ToUpper()
                              );

                // Show the message box with Yes/No options
                DialogResult result = MessageBox.Show(
                    message,
                    "Loan Verification",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );

                if (result == DialogResult.Yes)
                {
                    await _channelService.AuditLoanCaseAsync(loanCaseDTO, loanCaseDTO.LoanAuditOption, GetServiceHeader());

                    //TempData["verify"] = "Loan Verification Successful";

                    MessageBox.Show(Form.ActiveForm, "Operation Completed Successfully.", "Loan Verification", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                    return RedirectToAction("Index");
                }
                else
                {
                    await ServeNavigationMenus();

                    ViewBag.LoanAuditOptionSelectList = GetLoanAuditOptionSelectList(loanCaseDTO.LoanAuditOption.ToString());

                    MessageBox.Show(Form.ActiveForm, "Operation Cancelled.", "Loan Verification", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return View(loanCaseDTO);
                }
            }
            else
            {
                await ServeNavigationMenus();

                var errorMessages = loanCaseDTO.ErrorMessages;
                ViewBag.LoanAuditOptionSelectList = GetLoanAuditOptionSelectList(loanCaseDTO.LoanAuditOption.ToString());

                //TempData["verifyError"] = "Loan Verification Unsuccessful";

                MessageBox.Show(Form.ActiveForm, "Operation Unsuccessful.", "Loan Verification", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                return View(loanCaseDTO);
            }
        }

    }
}