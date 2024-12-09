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
using Infrastructure.Crosscutting.Framework.Utils;
using Microsoft.AspNet.Identity;

namespace SwiftFinancials.Web.Areas.Loaning.Controllers
{
    public class LoanRestructuringController : MasterController
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
                (int)LoanCaseStatus.Restructured,
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

        [HttpPost]
        public async Task<JsonResult> CustomerAccountIndex(JQueryDataTablesModel jQueryDataTablesModel, string text, int customerFilter)
        {
            int totalRecordCount = 0;
            int searchRecordCount = 0;
            int pageIndex = jQueryDataTablesModel.iDisplayStart / jQueryDataTablesModel.iDisplayLength;
            int pageSize = jQueryDataTablesModel.iDisplayLength;
            var sortAscending = jQueryDataTablesModel.sSortDir_.First() == "asc" ? true : false;
            var sortedColumns = (from s in jQueryDataTablesModel.GetSortedColumns() select s.PropertyName).ToList();

            var pageCollectionInfo = await _channelService.FindCustomerAccountsByProductCodeAndRecordStatusAndFilterInPageAsync((int)ProductCode.Loan, (int)RecordStatus.Approved, text,
                 customerFilter, pageIndex, pageSize, true, true, true, true, GetServiceHeader());


            if (pageCollectionInfo != null && pageCollectionInfo.PageCollection.Any())
            {
                totalRecordCount = pageCollectionInfo.ItemsCount;
                searchRecordCount = !string.IsNullOrWhiteSpace(jQueryDataTablesModel.sSearch)
                    ? pageCollectionInfo.PageCollection.Count
                    : totalRecordCount;

                var orderedPageCollection = pageCollectionInfo.PageCollection
                    .OrderByDescending(item => item.CreatedDate)
                    .ToList();

                return this.DataTablesJson(
                    items: orderedPageCollection,
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
            else
            {
                return this.DataTablesJson(
                    items: new List<CustomerAccountDTO> { },
                    totalRecords: totalRecordCount,
                    totalDisplayRecords: searchRecordCount,
                    sEcho: jQueryDataTablesModel.sEcho
                );
            }
        }

        public async Task<ActionResult> CustomerAccountLookUp(Guid? id, LoanCaseDTO loanCaseDTO)
        {
            await ServeNavigationMenus();

            Guid parseId;

            if (id == Guid.Empty || !Guid.TryParse(id.ToString(), out parseId))
            {
                return View("create");
            }

            var findCustomerAccount = await _channelService.FindCustomerAccountAsync(parseId, true, true, true, true, GetServiceHeader());
            if (findCustomerAccount != null)
            {
                loanCaseDTO.FullAccountNumber = findCustomerAccount.FullAccountNumber;
                loanCaseDTO.CustomerAccountId = findCustomerAccount.Id;
                loanCaseDTO.AccountStatus = findCustomerAccount.StatusDescription;
                loanCaseDTO.PrincipalBalance = findCustomerAccount.PrincipalBalance;
                loanCaseDTO.InterestBalance = findCustomerAccount.InterestBalance;
                loanCaseDTO.CustomerIndividualFirstName = findCustomerAccount.CustomerFullName;
                loanCaseDTO.CustomerIndividualPayrollNumbers = findCustomerAccount.CustomerIndividualPayrollNumbers;
                loanCaseDTO.CustomerPersonalIdentificationNumber = findCustomerAccount.CustomerPersonalIdentificationNumber;
                loanCaseDTO.CustomerReference1 = findCustomerAccount.CustomerReference1;
                loanCaseDTO.CustomerReference2 = findCustomerAccount.CustomerReference2;
                loanCaseDTO.CustomerReference3 = findCustomerAccount.CustomerReference3;

                var findProduct = await _channelService.FindLoanProductAsync(findCustomerAccount.CustomerAccountTypeTargetProductId, GetServiceHeader());
                if (findProduct != null)
                {
                    loanCaseDTO.LoanProductId = findProduct.Id;
                    loanCaseDTO.LoanProductDescription = findProduct.Description;
                    loanCaseDTO.loanProductSection = findProduct.LoanRegistrationLoanProductSectionDescription;
                    loanCaseDTO.LoanInterestAnnualPercentageRate = findProduct.LoanInterestAnnualPercentageRate;
                    loanCaseDTO.loanProductPaymentFrequencyPerYear = findProduct.LoanRegistrationPaymentFrequencyPerYearDescription;
                    loanCaseDTO.NumberOfPeriods = findProduct.LoanRegistrationTermInMonths;
                }


                return Json(new
                {
                    success = true,
                    data = new
                    {
                        FullAccountNumber = loanCaseDTO.FullAccountNumber,
                        CustomerAccountId = loanCaseDTO.CustomerAccountId,
                        AccountStatus = loanCaseDTO.AccountStatus,
                        PrincipalBalance = loanCaseDTO.PrincipalBalance,
                        InterestBalance = loanCaseDTO.InterestBalance,
                        CustomerIndividualFirstName = loanCaseDTO.CustomerIndividualFirstName,
                        CustomerIndividualPayrollNumbers = loanCaseDTO.CustomerIndividualPayrollNumbers,
                        CustomerPersonalIdentificationNumber = loanCaseDTO.CustomerPersonalIdentificationNumber,
                        CustomerReference1 = loanCaseDTO.CustomerReference1,
                        CustomerReference2 = loanCaseDTO.CustomerReference2,
                        CustomerReference3 = loanCaseDTO.CustomerReference3,

                        LoanProductId = loanCaseDTO.LoanProductId,
                        LoanProductDescription = loanCaseDTO.LoanProductDescription,
                        loanProductSection = loanCaseDTO.loanProductSection,
                        LoanInterestAnnualPercentageRate = loanCaseDTO.LoanInterestAnnualPercentageRate,
                        loanProductPaymentFrequencyPerYear = loanCaseDTO.loanProductPaymentFrequencyPerYear,
                        NumberOfPeriods = loanCaseDTO.NumberOfPeriods
                    }
                });
            }

            return Json(new { success = false, message = "Customer not found" });
        }

        public async Task<ActionResult> Create()
        {
            await ServeNavigationMenus();

            ViewBag.LoanCaseFilterSelectList = GetLoanCaseFilterTypeSelectList(string.Empty);
            ViewBag.LoanCaseStatusSelectList = GetLoanCaseStatusSelectList(string.Empty);

            ViewBag.ProductCode = GetProductCodeSelectList(string.Empty);
            ViewBag.RecordStatus = GetRecordStatusSelectList(string.Empty);

            ViewBag.CustomerFilter = GetCustomerFilterSelectList(string.Empty);

            ViewBag.LoanInterestCalculationModeSelectList = GetLoanInterestCalculationModeSelectList(string.Empty);
            ViewBag.LoanRegistrationLoanProductSectionSelectList = GetLoanRegistrationLoanProductCategorySelectList(string.Empty);
            ViewBag.LoanPaymentFrequencyPerYearSelectList = GetLoanPaymentFrequencyPerYearSelectList(string.Empty);

            ViewBag.recordStatus = GetRecordStatusSelectList(string.Empty);
            ViewBag.customerFilter = GetCustomerFilterSelectList(string.Empty);

            ViewBag.LoanProductSection = GetLoanRegistrationLoanProductSectionsSelectList(string.Empty);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(LoanCaseDTO loanCaseDTO, double? PaymentPerPeriod, double? NumberOfPeriods, string Reference,
            Guid CustomerAccountId, Guid LoanProductId)
        {
            await ServeNavigationMenus();

            ViewBag.customerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());
            ViewBag.CustomerFilter = GetCustomerFilterSelectList(loanCaseDTO.CustomerFilterDescription.ToString());

            ViewBag.LoanCaseFilterSelectList = GetLoanCaseFilterTypeSelectList(loanCaseDTO.filterTextDescription.ToString());

            var userDTO = await _applicationUserManager.FindByIdAsync(User.Identity.GetUserId());
            if (userDTO.BranchId != null)
            {
                loanCaseDTO.BranchId = (Guid)userDTO.BranchId;
            }

            var customerAccountDetails = await _channelService.FindCustomerAccountAsync(loanCaseDTO.CustomerAccountId, true, true, true, true, GetServiceHeader());
            var loanCase = await _channelService.FindLastLoanCaseByCustomerIdAsync(customerAccountDetails.CustomerId, loanCaseDTO.LoanProductId, GetServiceHeader());

            if (PaymentPerPeriod == null || NumberOfPeriods == null || Reference == null || CustomerAccountId == Guid.Empty || LoanProductId == Guid.Empty)
            {
                TempData["Empty"] = "Warning! Make sure that all fields are provided.";
                return View(loanCaseDTO);
            }
            
            var submit = await _channelService.RestructureLoanAsync(loanCaseDTO.BranchId, CustomerAccountId, loanCaseDTO.NumberOfPeriods, loanCaseDTO.PaymentPerPeriod,
                    loanCaseDTO.Reference, 1234, GetServiceHeader());

            return View("Index");
        }
    }
}