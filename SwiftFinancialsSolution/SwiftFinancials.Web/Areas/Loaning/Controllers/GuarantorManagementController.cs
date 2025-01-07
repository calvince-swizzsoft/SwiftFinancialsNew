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
    public class GuarantorManagementController : MasterController
    {
        string connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        private bool IsBusy { get; set; }

        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            return View();
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

            ViewBag.ProductCode2 = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus2 = GetRecordStatusSelectList(string.Empty);

            return View();
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
        public async Task<JsonResult> LoaneeLookUp(Guid id)
        {
            if (id == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }

            var loaneeLookUp = new LoanGuarantorDTO();

            var loanee = await _channelService.FindCustomerAccountAsync(id, true, true, true, true, GetServiceHeader());
            if (loanee != null)
            {
                if (loanee.CustomerAccountTypeProductCode != (int)ProductCode.Loan)
                {
                    TempData["!Loan"] = "Please select a Loan Account!";
                    return Json(new { success = false, message = "Please select a Loan Account!" });
                }

                Session["LoanProductId"] = loanee.CustomerAccountTypeTargetProductId;
                loaneeLookUp.LoaneeCustomerId = loanee.CustomerId;
                loaneeLookUp.CustomerAccountFullAccountNumber = loanee.FullAccountNumber;
                loaneeLookUp.CustomerAccountAccountStatusDescription = loanee.StatusDescription;
                loaneeLookUp.CustomerAccountAccountRemarks = loanee.Remarks;
                loaneeLookUp.BookBalance = loanee.BookBalance;
                loaneeLookUp.CustomerFullName = loanee.CustomerFullName;
                loaneeLookUp.CustomerAccounntCustomerTypeDescription = loanee.CustomerTypeDescription;
                loaneeLookUp.LoaneeCustomerIndividualPayrollNumbers = loanee.CustomerIndividualPayrollNumbers;
                loaneeLookUp.CustomerPersonalIdentificationNumber = loanee.CustomerPersonalIdentificationNumber;
                loaneeLookUp.CustomerReference1 = loanee.CustomerReference1;
                loaneeLookUp.CustomerReference2 = loanee.CustomerReference2;
                loaneeLookUp.CustomerReference3 = loanee.CustomerReference3;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        LoaneeCustomerId = loaneeLookUp.LoaneeCustomerId,
                        CustomerAccountFullAccountNumber = loaneeLookUp.CustomerAccountFullAccountNumber,
                        CustomerAccountAccountStatusDescription = loaneeLookUp.CustomerAccountAccountStatusDescription,
                        CustomerAccountAccountRemarks = loaneeLookUp.CustomerAccountAccountRemarks,
                        BookBalance = loaneeLookUp.BookBalance,
                        CustomerFullName = loaneeLookUp.CustomerFullName,
                        CustomerAccounntCustomerTypeDescription = loaneeLookUp.CustomerAccounntCustomerTypeDescription,
                        LoaneeCustomerIndividualPayrollNumbers = loaneeLookUp.LoaneeCustomerIndividualPayrollNumbers,
                        CustomerPersonalIdentificationNumber = loaneeLookUp.CustomerPersonalIdentificationNumber,
                        CustomerReference1 = loaneeLookUp.CustomerReference1,
                        CustomerReference2 = loaneeLookUp.CustomerReference2,
                        CustomerReference3 = loaneeLookUp.CustomerReference3,
                    }
                });
            }

            return Json(new { success = false, message = "Customer Account not found" });
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
                guarantorLookUp.GuarantorRef1 = loanGuarantor.Reference1;
                guarantorLookUp.GuarantorRef2 = loanGuarantor.Reference2;
                guarantorLookUp.GuarantorRef3 = loanGuarantor.Reference3;

                if (Session["LoanProductId"] != null)
                    guarantorLookUp.AppraisalFactor = await _channelService.GetGuarantorAppraisalFactorAsync((Guid)Session["LoanProductId"], guarantorLookUp.TotalShares, GetServiceHeader());

                var findAnotherGuarantee = await _channelService.FindLoanGuarantorsByCustomerIdAsync(guarantorLookUp.GuarantorId, GetServiceHeader());

                var totalAmountsGuaranteed = new ObservableCollection<decimal>();

                foreach (var sum in findAnotherGuarantee)
                {
                    totalAmountsGuaranteed.Add(sum.AmountGuaranteed);
                }
                decimal totalSum = totalAmountsGuaranteed.Sum();

                guarantorLookUp.CommittedShares = totalSum;

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
                        GuarantorRef1 = guarantorLookUp.GuarantorRef1,
                        GuarantorRef2 = guarantorLookUp.GuarantorRef2,
                        GuarantorRef3 = guarantorLookUp.GuarantorRef3,

                        TotalShares = guarantorLookUp.TotalShares
                    }
                });
            }

            return Json(new { success = false, message = "Customer not found" });
        }

        [HttpPost]
        public async Task<ActionResult> Create(LoanGuarantorDTO loanGuarantorDTO)
        {

            Session["LoanProductId"] = null;
            return View();
        }
    }
}