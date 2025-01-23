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
        public async Task<ActionResult> Index()
        {
            await ServeNavigationMenus();

            ViewBag.LoanCaseFilterSelectList = GetLoanCaseFilterTypeSelectList(string.Empty);
            ViewBag.LoanCaseStatusSelectList = GetLoanCaseStatusSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Index(JQueryDataTablesModel jQueryDataTablesModel, int loanCaseStatus, string filterValue, int filterType)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;

            var pageCollectionInfo = await _channelService.FindLoanCasesByStatusAndFilterInPageAsync(
                loanCaseStatus,
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


        public async Task<ActionResult> Create(Guid? id)
        {
            await ServeNavigationMenus();

            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            var details = new LoanCaseDTO();

            var loanCaseDetails = await _channelService.FindLoanCaseAsync(parseId, GetServiceHeader());
            if (loanCaseDetails != null)
            {
                details.CaseNumber = loanCaseDetails.CaseNumber;
                details.Id = loanCaseDetails.Id;
                details.CustomerId = loanCaseDetails.CustomerId;
                details.CustomerIndividualFirstName = loanCaseDetails.CustomerFullName;
                details.CustomerReference1 = loanCaseDetails.CustomerReference1;
                details.CustomerReference2 = loanCaseDetails.CustomerReference2;
                details.CustomerReference3 = loanCaseDetails.CustomerReference3;
                details.LoanProductDescription = loanCaseDetails.LoanProductDescription;
                details.LoanProductId = loanCaseDetails.LoanProductId;
                details.LoanPurposeDescription = loanCaseDetails.LoanPurposeDescription;
                details.LoanPurposeId = loanCaseDetails.LoanPurposeId;
                details.ReceivedDate = Convert.ToDateTime(loanCaseDetails.ReceivedDate).Date;
                details.Reference = loanCaseDetails.Reference;
                details.MaximumAmountPercentage = loanCaseDetails.MaximumAmountPercentage;
                details.AmountApplied = loanCaseDetails.AmountApplied;
                details.LoanRegistrationTermInMonths = loanCaseDetails.LoanRegistrationTermInMonths;
                Session["LoanProductId"] = details.LoanProductId;
                string stringValue = "N/A";
                details.AppraisedAmount = loanCaseDetails.AppraisedAmount;
                details.ApprovedAmount = loanCaseDetails.ApprovedAmount;

                if (loanCaseDetails.AppraisalRemarks == "" || loanCaseDetails.AppraisalRemarks == string.Empty || loanCaseDetails.AppraisalRemarks == null)
                    details.AppraisalRemarks = stringValue;
                else
                    details.AppraisalRemarks = loanCaseDetails.AppraisalRemarks;

                if (loanCaseDetails.ApprovalRemarks == "" || loanCaseDetails.ApprovalRemarks == string.Empty || loanCaseDetails.ApprovalRemarks == null)
                    details.ApprovalRemarks = stringValue;
                else
                    details.ApprovalRemarks = loanCaseDetails.ApprovalRemarks;

                details.BatchNumber = loanCaseDetails.BatchNumber;
                details.DisbursedAmount = loanCaseDetails.DisbursedAmount;

                if (loanCaseDetails.DisbursementRemarks == "" || loanCaseDetails.DisbursementRemarks == string.Empty || loanCaseDetails.DisbursementRemarks == null)
                    details.DisbursementRemarks = stringValue;
                else
                    details.DisbursementRemarks = loanCaseDetails.DisbursementRemarks;

                if (loanCaseDetails.StatusDescription == "" || loanCaseDetails.StatusDescription == string.Empty || loanCaseDetails.StatusDescription == null)
                    details.LoanStatus = stringValue;
                else
                    details.LoanStatus = loanCaseDetails.StatusDescription;

                var loanProductDetails = await _channelService.FindLoanProductAsync(details.LoanProductId, GetServiceHeader());
                if (loanProductDetails != null)
                {
                    details.LoanRegistrationMaximumAmount = loanProductDetails.LoanRegistrationMaximumAmount;
                }

                var loanGuarantors = await _channelService.FindLoanGuarantorsByLoanCaseIdAsync(parseId, GetServiceHeader());
                if (loanGuarantors != null)
                    ViewBag.LoanGuarantors = loanGuarantors;
            }

            return View(details);
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
        public async Task<ActionResult> Add(LoanCaseDTO loancaseDTO)
        {
            await ServeNavigationMenus();

            if (loancaseDTO.LoanProductId == Guid.Empty || loancaseDTO.LoanProductDescription == string.Empty || loancaseDTO.loanProductSection == "")
            {
                TempData["EmptyLoanProduct"] = "Loan Product required to proceed to add Guarantors!";

                return Json(new
                {
                    success = false
                });
            }

            if (loancaseDTO.Guarantor[0].AmountGuaranteed <= 0)
            {
                TempData["AmountGuaranteedLessThan0"] = "Amount Guaranteed cannot be 0 or equal to 0!";

                return Json(new
                {
                    success = false
                });
            }

            var loanguarantorsDTOs = Session["loanguarantorsDTOs"] as ObservableCollection<LoanGuarantorDTO>;

            if (loanguarantorsDTOs == null)
            {
                loanguarantorsDTOs = new ObservableCollection<LoanGuarantorDTO>();
            }

            var totalGuarantorsCount = loanguarantorsDTOs.Count;

            foreach (var guarantorDTO in loancaseDTO.Guarantor)
            {
                var existingEntry = loanguarantorsDTOs.FirstOrDefault(e => e.GuarantorId == guarantorDTO.GuarantorId);

                if (existingEntry != null)
                {
                    TempData["GuarantorExists"] = "The selected Customer has already been added to the guarantors list!";
                    return Json(new
                    {
                        success = false
                    });
                }

                var loanProductDetails = await _channelService.FindLoanProductAsync(loancaseDTO.LoanProductId, GetServiceHeader());
                var isSelfGuarantee = loanProductDetails.LoanRegistrationAllowSelfGuarantee;
                if (guarantorDTO.Id == loancaseDTO.CustomerId && !isSelfGuarantee)
                {
                    TempData["notSelfGuarantee"] = "The selected Loan Product does not allow self Guarantee!";
                    Session["loanguarantorsDTOs"] = null;
                    return Json(new
                    {
                        success = false
                    });
                }

                var maximumGuarantees = loanProductDetails.LoanRegistrationMaximumGuarantees;

                if (loancaseDTO.Guarantor[0].AmountGuaranteed > (loancaseDTO.Guarantor[0].TotalShares - loancaseDTO.Guarantor[0].CommittedShares))
                {
                    TempData["AmountGuaranteedGreater"] = $"Amount Guaranteed must be less than or equal to Total Shares minus Committed Shares" +
                        $" ({loancaseDTO.Guarantor[0].TotalShares} - {loancaseDTO.Guarantor[0].CommittedShares} = " +
                        $"{loancaseDTO.Guarantor[0].TotalShares - loancaseDTO.Guarantor[0].CommittedShares}) and the number of Maximum Guarantees must not be exceeded!";
                    Session["loanguarantorsDTOs"] = null;
                    return Json(new { success = false, message = "Failed to add Loan Guarantor. Amount Guaranteed exceeded Total Shares." });
                }

                if (totalGuarantorsCount > maximumGuarantees)
                {
                    TempData["MaximumGuaranteedExceeded"] = "Maximum Guarantees must not be exceeded!";

                    return Json(new { success = false, message = "Failed to add Loan Guarantor. Amount Guaranteed exceeded Total Shares." });
                }

                loanguarantorsDTOs.Add(guarantorDTO);
            }

            Session["loanguarantorsDTOs"] = loanguarantorsDTOs;
            Session["guarantorDTO"] = loancaseDTO.Guarantor;

            return Json(new { success = true, entries = loanguarantorsDTOs });
        }


        [HttpPost]
        public async Task<ActionResult> Create(LoanCaseDTO loanCaseDTO)
        {

            Session["LoanProductId"] = null;
            return View();
        }
    }
}