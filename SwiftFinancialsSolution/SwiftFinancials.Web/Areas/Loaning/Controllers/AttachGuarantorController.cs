using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Application.MainBoundedContext.DTO;
using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.BackOfficeModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using SwiftFinancials.Web.Controllers;
using SwiftFinancials.Web.Helpers;

using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Windows.Forms;
using Infrastructure.Crosscutting.Framework.Utils;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class AttachGuarantorController : MasterController
    {
        string connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        private bool IsBusy { get; set; }

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, string text, int pageIndex, int pageSize)
        {
            int totalRecordCount = 0;

            int searchRecordCount = 0;

            pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;

            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;

            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomersByFilterInPageAsync(jQueryDataTablesModel.sSearch, (int)CustomerFilter.FirstName, pageIndex, jQueryDataTablesModel.iDisplayLength,
                GetServiceHeader());

            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;

                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch) ? pageCollectionInfo.PageCollection.Count : totalRecordCount;

                return this.DataTablesJson(items: pageCollectionInfo.PageCollection, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
            }
            else return this.DataTablesJson(items: new List<CustomerDTO> { }, totalRecords: totalRecordCount, totalDisplayRecords: searchRecordCount, sEcho: jQueryDataTablesModel.sEcho);
        }


        public async Task<ActionResult> Details(Guid id)
        {
            await ServeNavigationMenus();

            var loanguarantorsDTO = await _channelService.FindLoanGuarantorAsync(id, GetServiceHeader());

            return View(loanguarantorsDTO);
        }


        public async Task<ActionResult> Create(Guid? id, LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            ViewBag.CustomerFilter = GetCustomerFilterSelectList(string.Empty);

            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
            ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(string.Empty);

            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);

            ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View();
            }

            var myloanCases = await _channelService.FindLoanCaseAsync(parseId, GetServiceHeader());

            var loanproductId = myloanCases.LoanProductId;
            Session["LoanProductIdID"] = loanproductId;

            var findProductDetails = await _channelService.FindLoanProductAsync(loanproductId, GetServiceHeader());
            var maximumGuarantees = findProductDetails.LoanRegistrationMaximumGuarantees;

            var findLoanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(myloanCases.Id, GetServiceHeader());
            ViewBag.LoanGuarantors = findLoanGuarantors;

            var existingGuarantorsCount = findLoanGuarantors.Count;

            if (existingGuarantorsCount >= maximumGuarantees)
            {
                MessageBox.Show(Form.ActiveForm, "The selected Loan Case has already satisfied the Maximum Guarantees and therefore no more guarantors can be added.", "Loan Guarantor Attachment",
                      MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                return RedirectToAction("Index", "LoanRegistration");
            }
            else
            {
                var sourceCustomerId = myloanCases.CustomerId;
                var findCustomerAccount = await _channelService.FindCustomerAccountsByCustomerIdAsync(sourceCustomerId, true, true, true, true, GetServiceHeader());

                List<Guid> customerAccountsIDs = new List<Guid>();

                foreach (var accounts in findCustomerAccount)
                {
                    customerAccountsIDs.Add(accounts.Id);
                }

                var sourceCustomerAccountId = customerAccountsIDs[0];

                Session["sourceCustomerAccountId"] = sourceCustomerAccountId;

                if (myloanCases != null)
                {
                    loanGuarantorDTO.LoanCase = myloanCases;

                    Session["loanCases"] = loanGuarantorDTO.LoanCase;

                    Session["status"] = loanGuarantorDTO.LoanCase.Status;
                }


                Session["gGuarantors"] = findLoanGuarantors;

                Session["loanCaseId"] = myloanCases.Id;

                Session["LoaneeId"] = myloanCases.CustomerId;

                return View(loanGuarantorDTO);
            }
        }


        [HttpPost]
        public async Task<JsonResult> CustomerIndex(JQueryDataTablesModel jQueryDataTablesModel, int recordStatus, string text, int customerFilter)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;


            var pageCollectionInfo = await _channelService.FindCustomersByRecordStatusAndFilterInPageAsync((int)RecordStatus.Approved, text, customerFilter, 0, int.MaxValue, GetServiceHeader());


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {

                var sortedData = pageCollectionInfo.PageCollection
                    .OrderByDescending(customer => customer.CreatedDate)
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
                items: new List<CustomerDTO>(),
                totalRecords: totalRecordCount,
                totalDisplayRecords: searchRecordCount,
                sEcho: jQueryDataTablesModel.sEcho
            );
        }


        [HttpPost]
        public async Task<JsonResult> LoanGuarantorLookUp(Guid id)
        {
            if (id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }

            var guarantorLookUp = new LoanGuarantorDTO();

            var loanGuarantor = await _channelService.FindCustomerAsync(id, GetServiceHeader());
            if (loanGuarantor != null)
            {
                Guid LoanProductId = Guid.Empty;

                if (Session["LoanProductIdID"] != null)
                    LoanProductId = (Guid)Session["LoanProductIdID"];

                var products = await _channelService.FindCustomerAccountsByCustomerIdAndProductCodesAsync(id, new[] { (int)ProductCode.Savings, (int)ProductCode.Loan, (int)ProductCode.Investment },
                    true, true, true, true, GetServiceHeader());
                var savingsProducts = products.Where(p => p.CustomerAccountTypeProductCode == (int)ProductCode.Savings).ToList();
                var loanProducts = products.Where(p => p.CustomerAccountTypeProductCode == (int)ProductCode.Loan).ToList();
                var investmentProducts = products.Where(p => p.CustomerAccountTypeProductCode == (int)ProductCode.Investment).ToList();

                List<decimal> sBalance = new List<decimal>();
                List<decimal> iBalance = new List<decimal>();

                foreach (var savingsBalances in savingsProducts)
                {
                    sBalance.Add(savingsBalances.BookBalance);
                }

                foreach (var investmentsBalances in investmentProducts)
                {
                    iBalance.Add(investmentsBalances.BookBalance);
                }

                guarantorLookUp.TotalShares = sBalance.Sum() + iBalance.Sum();

                guarantorLookUp.GuarantorFullName = loanGuarantor.FullName;
                guarantorLookUp.GuarantorId = loanGuarantor.Id;
                guarantorLookUp.GuarantorEmployerDescription = loanGuarantor.StationZoneDivisionEmployerDescription;
                guarantorLookUp.GuarantorStationDescription = loanGuarantor.StationDescription;
                guarantorLookUp.GuarantorSerialNumber = loanGuarantor.SerialNumber;
                guarantorLookUp.GuarantorIdentificationNumber = loanGuarantor.IdentificationNumber;
                guarantorLookUp.GuarantorPayrollNumber = loanGuarantor.IndividualPayrollNumbers;

                guarantorLookUp.AppraisalFactor = await _channelService.GetGuarantorAppraisalFactorAsync(LoanProductId, guarantorLookUp.TotalShares, GetServiceHeader());

                var findAnotherGuarantee = await _channelService.FindLoanGuarantorsByCustomerIdAsync(guarantorLookUp.GuarantorId, GetServiceHeader());

                var totalAmountsGuaranteed = new ObservableCollection<decimal>();

                foreach (var sum in findAnotherGuarantee)
                {
                    totalAmountsGuaranteed.Add(sum.AmountGuaranteed);
                }
                decimal totalSum = totalAmountsGuaranteed.Sum();

                guarantorLookUp.CommittedShares = totalSum;

                Session["GuarantorDetailsAfterLookUp"] = guarantorLookUp;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        GuarantorFullName = guarantorLookUp.GuarantorFullName,
                        GuarantorId = guarantorLookUp.GuarantorId,
                        GuarantorEmployerDescription = guarantorLookUp.GuarantorEmployerDescription,
                        GuarantorStationDescription = guarantorLookUp.GuarantorStationDescription,
                        GuarantorSerialNumber = guarantorLookUp.GuarantorSerialNumber,
                        GuarantorIdentificationNumber = guarantorLookUp.GuarantorIdentificationNumber,
                        GuarantorPayrollNumber = guarantorLookUp.GuarantorPayrollNumber,
                        AppraisalFactor = guarantorLookUp.AppraisalFactor,
                        CommittedShares = guarantorLookUp.CommittedShares,

                        // Return Committed Shares after successful calculation...

                        TotalShares = guarantorLookUp.TotalShares
                    }
                });
            }

            return Json(new { success = false, message = "Customer not found" });
        }





        [HttpPost]
        public async Task<ActionResult> Add(LoanGuarantorDTO loanGuarantorDTO)
        {
            await ServeNavigationMenus();

            if (loanGuarantorDTO.Customer[0].AmountGuaranteed <= 0)
            {
                MessageBox.Show(Form.ActiveForm, "Amount Guaranteed cannot be 0 or equal to 0.", "Loan Guarantor Attachment",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                return Json(new
                {
                    success = false
                });
            }

            Session["AmountGuaranteed"] = loanGuarantorDTO.Customer[0].AmountGuaranteed;

            var loanguarantorsDTOs = Session["loanguarantorsDTOs"] as ObservableCollection<LoanGuarantorDTO>;

            if (loanguarantorsDTOs == null)
            {
                loanguarantorsDTOs = new ObservableCollection<LoanGuarantorDTO>();
            }

            var totalGuarantorsCount = loanguarantorsDTOs.Count;

            foreach (var guarantorDTO in loanGuarantorDTO.Customer)
            {
                var existingEntry = loanguarantorsDTOs.FirstOrDefault(e => e.GuarantorId == guarantorDTO.GuarantorId);

                if (existingEntry != null)
                {
                    MessageBox.Show(Form.ActiveForm, "The selected Customer has already been added to the guarantors list.", "Loan Guarantor Attachment",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);


                    return Json(new
                    {
                        success = false
                    });
                }

                Guid loaneeId = (Guid)Session["LoaneeId"];
                Guid loanProductId = (Guid)Session["LoanProductIdID"];
                Guid loancaseId = (Guid)Session["loanCaseId"];

                var isGuarantor = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(loancaseId, GetServiceHeader());
                var exists = isGuarantor.FirstOrDefault(g => g.CustomerId == guarantorDTO.GuarantorId);

                if (exists != null)
                {
                    MessageBox.Show(Form.ActiveForm, "The selected Customer has already guaranteed the select loanee.", "Loan Guarantor Attachment",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                    return Json(new
                    {
                        success = false
                    });
                }

                loanGuarantorDTO = Session["GuarantorDetailsAfterLookUp"] as LoanGuarantorDTO;
                guarantorDTO.CustomerId = loanGuarantorDTO.GuarantorId;
                guarantorDTO.LoaneeCustomerId = loaneeId;
                guarantorDTO.LoanProductId = loanProductId;

                var findProductDetails = await _channelService.FindLoanProductAsync(loanProductId, GetServiceHeader());
                var maximumGuarantees = findProductDetails.LoanRegistrationMaximumGuarantees;

                var validateAmountGuaranteed = (decimal)Session["AmountGuaranteed"];

                if (validateAmountGuaranteed > (loanGuarantorDTO.TotalShares - loanGuarantorDTO.CommittedShares))
                {
                    MessageBox.Show(Form.ActiveForm, "Amount Guaranteed must be less than or equal to Total Shares minus Committed Shares and the number of Maximum Guarantees must not be exceeded.",
                        "Loan Guarantor Attachment", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    Session["AmountGuaranteed"] = null;

                    return Json(new { success = false, message = "Failed to add Loan Guarantor. Amount Guaranteed exceeded Total Shares." });
                }
                Session["AmountGuaranteed"] = null;

                if (totalGuarantorsCount > maximumGuarantees)
                {
                    MessageBox.Show(Form.ActiveForm, "Maximum Guarantees must not be exceeded.",
                        "Loan Guarantor Attachment", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                    return Json(new { success = false, message = "Failed to add Loan Guarantor. Amount Guaranteed exceeded Total Shares." });
                }

                loanguarantorsDTOs.Add(guarantorDTO);
            }

            Session["loanguarantorsDTOs"] = loanguarantorsDTOs;
            Session["guarantorDTO"] = loanGuarantorDTO;

            return Json(new { success = true, entries = loanguarantorsDTOs });
        }


        [HttpPost]
        public async Task<JsonResult> Remove(Guid id)
        {
            await ServeNavigationMenus();

            var loanguarantorsDTOs = Session["loanguarantorsDTOs"] as ObservableCollection<LoanGuarantorDTO>;

            if (loanguarantorsDTOs != null)
            {
                var entryToRemove = loanguarantorsDTOs.FirstOrDefault(e => e.GuarantorId == id);
                if (entryToRemove != null)
                {
                    loanguarantorsDTOs.Remove(entryToRemove);

                    Session["loanguarantorsDTOs"] = loanguarantorsDTOs;
                }
            }

            return Json(new { success = true, data = loanguarantorsDTOs });
        }





        [HttpPost]
        public async Task<ActionResult> Create(LoanGuarantorDTO loanGuarantorDTO)
        {
            Guid loancaseId = (Guid)Session["loanCaseId"];

            loanGuarantorDTO = Session["guarantorDTO"] as LoanGuarantorDTO;

            loanGuarantorDTO.ValidateAll();

            ObservableCollection<LoanGuarantorDTO> loanguarantors = new ObservableCollection<LoanGuarantorDTO>();

            var loanGuarantorsSubmit = new ObservableCollection<LoanGuarantorDTO>();

            if (Session["loanguarantorsDTOs"] != null)
            {
                loanguarantors = Session["loanguarantorsDTOs"] as ObservableCollection<LoanGuarantorDTO>;
            }

            if (!loanGuarantorDTO.HasErrors)
            {

                string message = string.Format(
                                  "Do you want to proceed?"
                              );

                // Show the message box with Yes/No options
                DialogResult result = MessageBox.Show(
                    message,
                    "Loan Guarantor Attachment",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.ServiceNotification
                );


                if (result == DialogResult.Yes)
                {
                    await _channelService.UpdateLoanGuarantorsByLoanCaseIdAsync(loancaseId, loanguarantors, GetServiceHeader());

                    MessageBox.Show(Form.ActiveForm, "Operation Completed Succeffully", "Loan Guarantor Attachment", MessageBoxButtons.OK, MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                    Session["loanguarantorsDTOs"] = null;
                    Session["loanCaseId"] = null;
                    Session["guarantorDTO"] = null;
                    Session["LoanProductIdID"] = null;
                    Session["GuarantorDetailsAfterLookUp"] = null;
                    Session["LoaneeId"] = null;
                    Session["LoanProductIdID"] = null;

                    return RedirectToAction("Index", "LoanRegistration");
                }
                else
                {

                    await ServeNavigationMenus();

                    ViewBag.recordStatus = GetRecordStatusSelectList(loanGuarantorDTO.RecordStatusDescription.ToString());
                    ViewBag.customerFilter = GetCustomerFilterSelectList(loanGuarantorDTO.CustomerFilterDescription.ToString());

                    MessageBox.Show(Form.ActiveForm, "Operation Cancelled Succeffully", "Loan Guarantor Attachment", MessageBoxButtons.OK, MessageBoxIcon.Warning,
                       MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                    return View(loanGuarantorDTO);
                }
            }

            else
            {
                await ServeNavigationMenus();

                ViewBag.recordStatus = GetRecordStatusSelectList(loanGuarantorDTO.RecordStatusDescription.ToString());
                ViewBag.customerFilter = GetCustomerFilterSelectList(loanGuarantorDTO.CustomerFilterDescription.ToString());

                var errorMessages = loanGuarantorDTO.ErrorMessages;
                string errorMessage = string.Join("\n", errorMessages.Where(msg => !string.IsNullOrWhiteSpace(msg)));

                MessageBox.Show(Form.ActiveForm, $"Operation Unsuccessful: {errorMessage}", "Loan Guarantors", MessageBoxButtons.OK, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                return View(loanGuarantorDTO);
            }
        }
    }
}