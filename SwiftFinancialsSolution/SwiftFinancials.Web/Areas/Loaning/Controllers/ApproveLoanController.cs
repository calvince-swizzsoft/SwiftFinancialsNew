using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Contexts;
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

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class ApproveLoanController : MasterController
    {

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();
            ViewBag.LoanCaseFilterSelectList = GetLoanCaseFilterTypeSelectList(string.Empty);
            ViewBag.LoanCaseStatusSelectList = GetLoanCaseStatusSelectList(string.Empty);
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, string filterValue, int filterType)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync(
                (int)LoanCaseStatus.Appraised,
                filterValue,
                filterType,
                0,
                int.MaxValue,
                includeBatchStatus: true,
                GetServiceHeader()
            );

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(loanCase => loanCase.CreatedDate)
                    .ToList();

                totalRecordCount = sortedData.Count;

                var paginatedData = sortedData
                    .Skip(jQueryDataTablesModel.iDisplayStart)
                    .Take(jQueryDataTablesModel.iDisplayLength)
                    .ToList();

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? sortedData.Count
                    : totalRecordCount;

                return this.DataTablesJson(
                    items: paginatedData,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }

            return this.DataTablesJson(
                items: new List<LoanCaseDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }


        public async Task<ActionResult> Details(Guid id, JQueryDataTablesModel jQueryDataTablesModel)
        {
            await ServeNavigationMenus();

            int status = 1, loanCaseFilter = 0;

            string text = "";

            var loanCaseDTO = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync(status, text, loanCaseFilter, jQueryDataTablesModel.iDisplayStart, jQueryDataTablesModel.iDisplayLength, false, GetServiceHeader());

            return View(loanCaseDTO);
        }




        public async Task<ActionResult> Approve(Guid id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            ViewBag.LoanApprovalOptionSelectList = GetLoanApprovalOptionSelectList(string.Empty);

            var loaneeCustomer = await _channelService.FindLoanCaseAsync(id, GetServiceHeader());
            if (loaneeCustomer != null)
            {
                //loanCaseDTO.CaseNumber = loaneeCustomer.CaseNumber;
                //loanCaseDTO.CustomerIndividualFirstName = loaneeCustomer.CustomerIndividualSalutationDescription + " " + loaneeCustomer.CustomerIndividualFirstName + " " +
                //    loaneeCustomer.CustomerIndividualLastName;
                //loanCaseDTO.CustomerReference1 = loaneeCustomer.CustomerReference1;
                //loanCaseDTO.CustomerReference2 = loaneeCustomer.CustomerReference2;
                //loanCaseDTO.CustomerReference3 = loaneeCustomer.CustomerReference3;
                //loanCaseDTO.LoanRegistrationTermInMonths = loaneeCustomer.LoanRegistrationTermInMonths;
                //loanCaseDTO.LoanRegistrationMaximumAmount = loaneeCustomer.LoanRegistrationMaximumAmount;
                //loanCaseDTO.MaximumAmountPercentage = loaneeCustomer.MaximumAmountPercentage;
                //loanCaseDTO.LoanProductDescription = loaneeCustomer.LoanProductDescription;
                //loanCaseDTO.LoanPurposeDescription = loaneeCustomer.LoanPurposeDescription;
                //loanCaseDTO.AmountApplied = loaneeCustomer.AmountApplied;
                //loanCaseDTO.AppraisedAmount = loaneeCustomer.AppraisedAmount;

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
        public async Task<ActionResult> Approve(LoanCaseDTO loanCaseDTO)
        {
            var loanApprovalOption = loanCaseDTO.LoanApprovalOption;

            var loanDTO = await _channelService.FindLoanCaseAsync(loanCaseDTO.Id, GetServiceHeader());

            var findLoanCaseDetails = await _channelService.FindLoanCaseAsync(loanCaseDTO.Id, GetServiceHeader());

            loanCaseDTO.LoanProductId = findLoanCaseDetails.LoanProductId;
            loanCaseDTO.LoanPurposeId = findLoanCaseDetails.LoanPurposeId;
            loanCaseDTO.SavingsProductId = findLoanCaseDetails.SavingsProductId;
            loanCaseDTO.ApprovedDate = DateTime.Now;

            loanCaseDTO.ValidateAll();

            try
            {

                if (!loanCaseDTO.HasErrors)
                {
                    try
                    {

                        string message = string.Format(
                                         "Do you want to proceed with loan approval for: \n{0}?",
                                             findLoanCaseDetails.CustomerIndividualSalutationDescription.ToUpper() + " " + findLoanCaseDetails.CustomerIndividualFirstName.ToUpper() + " " +
                                             findLoanCaseDetails.CustomerIndividualLastName.ToUpper()
                                     );

                        // Show the message box with Yes/No options
                        DialogResult result = MessageBox.Show(
                            message,
                            "Loan Approval",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.ServiceNotification
                        );

                        if (result == DialogResult.Yes)
                        {
                            await _channelService.ApproveLoanCaseAsync(loanCaseDTO, loanApprovalOption, GetServiceHeader());

                            //TempData["approve"] = "Loan Approval Successful";
                            MessageBox.Show(Form.ActiveForm, "Operation Completed Successfully.", "Loan Approval", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                            return RedirectToAction("Index");
                        }
                        else
                        {
                            await ServeNavigationMenus();

                            ViewBag.LoanApprovalOptionSelectList = GetLoanApprovalOptionSelectList(loanApprovalOption.ToString());

                            MessageBox.Show(Form.ActiveForm, "Operation Cancelled.", "Loan Approval", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                            return View(loanCaseDTO);
                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(Form.ActiveForm, $"Operation Unsuccessful Failed: {ex.ToString()}", "Loan Approval", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                        return View(loanCaseDTO);
                    }

                }
                else
                {
                    await ServeNavigationMenus();

                    var errorMessages = loanDTO.ErrorMessages;
                    ViewBag.LoanApprovalOptionSelectList = GetLoanApprovalOptionSelectList(loanCaseDTO.LoanApprovalOption.ToString());

                    // Combine all error messages into a single string
                    string errorMessage = string.Join("\n", errorMessages);

                    MessageBox.Show(Form.ActiveForm, $"Operation Unsuccessful. Errors:\n{errorMessage}", "Loan Approval", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return View(loanCaseDTO);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(Form.ActiveForm, $"Operation Unsuccessful Failed: {ex.ToString()}", "Loan Approval", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                return View(loanCaseDTO);
            }
        }
    }
}